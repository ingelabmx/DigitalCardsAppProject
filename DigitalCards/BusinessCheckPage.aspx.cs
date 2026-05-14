using DigitalCardsApp.Connectors;
using DigitalCardsApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace DigitalCardsApp
{
    public partial class BusinessCheckPage : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetExpires(DateTime.UtcNow.AddDays(-1));
            Response.Cache.SetNoStore();

            if (!IsPostBack)
            {
                CheckUser();
                DisplayBusinessLogo();
                DisplayBusinessName();
            }
        }
        public void CheckUser()
        {
            if (Session["BusinessID"] != null && Session["BusinessName"] != null)
            {
                int BusinessId = (int)Session["BusinessID"];
                string BusinessName = Session["BusinessName"].ToString();
                string BusinessLogo = Session["BusinessLogo"].ToString();
                LegacyCutoverConnector.EnforceBusinessPage(this, BusinessId);
            }
            else
            {
                Response.Redirect("NotAuthorized.aspx");
                return;
            }
        }

        public void DisplayBusinessName()
        {
            // Asegúrate de que la sesión tenga un valor
            if (Session["BusinessName"] != null)
            {
                BusinessNameLabel.Text = Session["BusinessName"].ToString();
            }
        }

        private void DisplayBusinessLogo()
        {
            if (Session["BusinessLogo"] != null)
            {
                string logoPath = Session["BusinessLogo"].ToString();

                // Set the src attribute of the <img> tag dynamically
                imgBusinessLogo.Attributes["src"] = ResolveUrl(logoPath);
            }
        }

        protected void btChecar_ServerClick(object sender, EventArgs e)
        {
            try
            {
                ClientDetails cln = new ClientDetails();
                CardsDetails crd = new CardsDetails();
                Loyalty loyalty = new Loyalty();

                cln.UsName = tbCliente.Value;

                if (!string.IsNullOrEmpty(cln.UsName))
                {
                    int UserID = BusinessConnector.GetUserID(cln);

                    int BusinessID = (int)Session["BusinessID"];

                    if (UserID > 0 && BusinessID > 0)
                    {
                        BusinessConnector.IncreaseCheckQTY(UserID, BusinessID);
                        crd = BusinessConnector.GetCheckQTY(UserID, BusinessID);
                        loyalty.PatchObject(crd.CardIDGoogle, crd.CheckQTY.ToString(), crd.HistoricCheckQTY.ToString());

                        successAlert.Visible = true;
                        failAlert.Visible = false;

                        tbCliente.Value = "";
                    }
                    else
                    {
                        successAlert.Visible = false;
                        failAlert.Visible = true;
                        failAlert.InnerText = "Nombre de usuario inexistente.";
                    }
                }
                else
                {
                    successAlert.Visible = false;
                    failAlert.Visible = true;
                    failAlert.InnerText = "Hay que ingresar el nombre de usuario del cliente.";
                }
            }
            catch (Exception)
            {
                failAlert.Visible = true;
                successAlert.Visible = false;
                failAlert.InnerText = "Cliente no válido.";
            }
        }        
    }
}
