using DigitalCardsApp.Connectors;
using DigitalCardsApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace DigitalCardsApp
{
    public partial class BusinessLogin : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // Prevent caching to force reloading the page on browser "Back"
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetExpires(DateTime.UtcNow.AddDays(-1));
            Response.Cache.SetNoStore();

            // Clear all session variables
            if (!IsPostBack)
            {
                Session.Clear();
            }
        }

        protected void Login_Click(object sender, EventArgs e)
        {
            try
            {
                // Initialize failed attempts from session or set to 0
                int failedAttempts = Session["BusinessFailedAttempts"] != null ? (int)Session["BusinessFailedAttempts"] : 0;

                // Check if the account is locked
                if (failedAttempts >= 3)
                {
                    failAlert.Visible = true;
                    successAlert.Visible = false;
                    failAlert.InnerText = "La cuenta está bloqueada debido a múltiples intentos fallidos. Inténtelo más tarde.";
                    return;
                }

                var businessDetails = new BusinessDetails
                {
                    BusEmail = tbEmail.Value.Trim(),
                    BusPassword = HashPassword(tbContrasena.Value) // Ensure passwords are hashed
                };

                var authenticatedClient = BusinessConnector.BusinessLogin(businessDetails);

                if (authenticatedClient != null)
                {
                    string activationStatus = LegacyCutoverConnector.GetActivationStatus(authenticatedClient.BusID);
                    if (LegacyCutoverConnector.IsLegacyRetired(activationStatus))
                    {
                        Session.Clear();
                        failAlert.Visible = true;
                        successAlert.Visible = false;
                        failAlert.InnerText = "Este negocio ya opera en app.puntelio.com. Web Forms fue retirado para este negocio.";
                        return;
                    }

                    // Reset failed attempts on successful login
                    Session["BusinessFailedAttempts"] = 0;

                    // Store user data in session variables
                    Session["BusinessID"] = authenticatedClient.BusID;
                    Session["BusinessName"] = authenticatedClient.BusName;
                    Session["BusinessPassword"] = authenticatedClient.BusPassword;
                    Session["BusinessEmail"] = authenticatedClient.BusEmail;
                    Session["BusinessLogo"] = authenticatedClient.BusLogo;
                    Session["BusinessActivationStatus"] = activationStatus;

                    Response.Redirect("BusinessDashboardPage.aspx");
                }
                else
                {
                    // Increment failed attempts
                    failedAttempts++;
                    Session["BusinessFailedAttempts"] = failedAttempts;

                    failAlert.Visible = true;
                    successAlert.Visible = false;

                    if (failedAttempts >= 3)
                    {
                        failAlert.InnerText = "La sesión está bloqueada debido a múltiples intentos fallidos. Inténtelo más tarde.";
                    }
                    else
                    {
                        failAlert.InnerText = "Correo de negocio o contraseña inexistentes. Intentos restantes: " + (3 - failedAttempts);
                    }
                }
            }
            catch (Exception ex)
            {
                failAlert.Visible = true;
                successAlert.Visible = false;
                failAlert.InnerText = "Algo ha salido mal.";
            }
        }
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (var b in bytes)
                {
                    builder.Append(b.ToString("x2")); // Convert to hexadecimal string
                }
                return builder.ToString();
            }
        }
    }
}
