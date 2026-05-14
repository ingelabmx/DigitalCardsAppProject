using MySql.Data.MySqlClient;
using System;
using System.Configuration;
using System.Web;
using System.Web.UI;

namespace DigitalCardsApp.Connectors
{
    public static class LegacyCutoverConnector
    {
        private const string LegacyOnly = "LegacyOnly";
        private const string ModernPrimary = "ModernPrimary";
        private const string LegacyRetired = "LegacyRetired";

        private static readonly string ConnStr = ConfigurationManager.ConnectionStrings["DCConnectionString"].ConnectionString;

        public static string ModernAppUrl
        {
            get
            {
                string configured = ConfigurationManager.AppSettings["ModernAppUrl"];
                return string.IsNullOrWhiteSpace(configured)
                    ? "https://app.puntelio.com"
                    : configured.Trim().TrimEnd('/');
            }
        }

        public static string GetActivationStatus(int businessId)
        {
            if (businessId <= 0)
            {
                return LegacyOnly;
            }

            try
            {
                using (var connection = new MySqlConnection(ConnStr))
                using (var command = new MySqlCommand(
                    "select ActivationStatus from ModernPilotBusiness where BusinessID = @BusinessID limit 1;",
                    connection))
                {
                    command.Parameters.AddWithValue("@BusinessID", businessId);
                    connection.Open();
                    var value = command.ExecuteScalar() as string;
                    return NormalizeActivationStatus(value);
                }
            }
            catch (MySqlException)
            {
                return LegacyOnly;
            }
            catch (ConfigurationErrorsException)
            {
                return LegacyOnly;
            }
        }

        public static bool IsLegacyRetired(string activationStatus)
        {
            return string.Equals(activationStatus, LegacyRetired, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsModernPrimary(string activationStatus)
        {
            return string.Equals(activationStatus, ModernPrimary, StringComparison.OrdinalIgnoreCase);
        }

        public static void EnforceBusinessPage(Page page, int businessId)
        {
            var activationStatus = GetActivationStatus(businessId);
            page.Session["BusinessActivationStatus"] = activationStatus;

            if (IsLegacyRetired(activationStatus))
            {
                page.Session.Clear();
                page.Response.Redirect("NotAuthorized.aspx?modern=legacy-retired", endResponse: true);
                return;
            }

            RegisterModernPrimaryNotice(page, activationStatus);
        }

        public static void RegisterModernPrimaryNotice(Page page, string activationStatus)
        {
            if (!IsModernPrimary(activationStatus))
            {
                return;
            }

            var message = HttpUtility.JavaScriptStringEncode(
                "Este negocio esta en Moderno principal. Usa app.puntelio.com; Web Forms queda como fallback temporal.");
            var url = HttpUtility.JavaScriptStringEncode(ModernAppUrl);
            var script = $@"
document.addEventListener('DOMContentLoaded', function () {{
  var target = document.querySelector('.container-fluid') || document.querySelector('.body-wrapper') || document.body;
  if (!target || document.querySelector('[data-testid=""legacy-modern-primary-notice""]')) {{
    return;
  }}
  var alert = document.createElement('div');
  alert.className = 'alert alert-warning m-3';
  alert.setAttribute('role', 'status');
  alert.setAttribute('data-testid', 'legacy-modern-primary-notice');
  alert.innerHTML = '{message} <a href=""' + '{url}' + '"" class=""fw-bold"">Abrir moderno</a>.';
  target.insertBefore(alert, target.firstChild);
}});";

            page.ClientScript.RegisterStartupScript(
                page.GetType(),
                "legacy-modern-primary-notice",
                script,
                addScriptTags: true);
        }

        private static string NormalizeActivationStatus(string value)
        {
            if (string.Equals(value, ModernPrimary, StringComparison.OrdinalIgnoreCase))
            {
                return ModernPrimary;
            }

            if (string.Equals(value, LegacyRetired, StringComparison.OrdinalIgnoreCase))
            {
                return LegacyRetired;
            }

            return LegacyOnly;
        }
    }
}
