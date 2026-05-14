using DigitalCardsApp.Connectors;
using DigitalCardsApp.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using static Org.BouncyCastle.Asn1.Cmp.Challenge;
using static QRCoder.PayloadGenerator;
using MimeKit;
using MailKit.Net.Smtp;
using System.IO;
using System.Configuration;

namespace DigitalCardsApp
{

    public partial class BusinessInsertionPage : System.Web.UI.Page
    {

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetExpires(DateTime.UtcNow.AddDays(-1));
            Response.Cache.SetNoStore();

            if (!IsPostBack)
            {
                CheckUser();
                GetDataFromDatabase();
                DisplayBusinessLogo();
                DisplayBusinessName();
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

        public void DisplayBusinessName()
        {
            // Asegúrate de que la sesión tenga un valor
            if (Session["BusinessName"] != null)
            {
                BusinessNameLabel.Text = Session["BusinessName"].ToString();
            }
        }

        public void CheckUser()
        {
            if (Session["BusinessID"] != null && Session["BusinessName"] != null)
            {
                int BusinessId = (int)Session["BusinessID"];
                string BusinessName = Session["BusinessName"].ToString();
                string BusinessLogo = Session["BusinessLogo"].ToString();
            }
            else
            {
                Response.Redirect("NotAuthorized.aspx");
            }
        }

        protected void btRegistrar_ServerClick(object sender, EventArgs e)
        {
            try
            {
                Loyalty loyalty = new Loyalty();
                ClientDetails cln = new ClientDetails();
                cln.UsName = tbNombreCliente.Value;

                if (!string.IsNullOrEmpty(cln.UsName))
                {
                    cln = BusinessConnector.GetUserInfo(cln);
                    int UserID = cln.UsID;                    
                    int BusinessID = (int)Session["BusinessID"];
                    string UserEmail = cln.UsEmail;
                    string BusinessName = (string)Session["BusinessName"];
                    string IDBusiness = TextFormats.DeleteSpace(BusinessName);
                    string IDBusinessCG =  TextFormats.GenerateRandomText();
                    string IDBusinessCA = string.Empty;

                    if (UserID > 0 && BusinessID > 0)
                    {
                        int rowsAffected;
                        BusinessConnector.InsertClientData(UserID, BusinessID, IDBusinessCG, out rowsAffected);

                        if (rowsAffected > 0)
                        {
                            GetDataFromDatabase();
                            string date = BusinessConnector.GetCardCreatedTime(cln, BusinessID).ToString("dd/MM/yyyy");
                            // Se agrega tarjeta de Google Wallet de forma automatica

                            string result = loyalty.CreateObject(IDBusinessCG, IDBusiness, BusinessName, cln.UsName, cln.UsFirstName, cln.UsLastName, "1", "1", date);
                            if (result != "Success")
                            {
                                Console.WriteLine("Something goes wrong with the G Card");
                                successAlert.Visible = true;
                                failAlert.Visible = true;
                                failAlert.InnerText = "Ocurrio un problema al generar la tarjeta de Google, favor de reportar a soporte";
                            }
                            else
                            {
                                string linkGoogle = loyalty.CreateJWTExistingObjects(IDBusiness, IDBusinessCG);
                                SendEmailCards(UserEmail, cln.UsName, linkGoogle, BusinessName);
                                successAlert.Visible = true;
                                failAlert.Visible = false;

                                tbNombreCliente.Value = "";
                            }                            
                        }
                        else
                        {
                            successAlert.Visible = false;
                            failAlert.Visible = true;
                            failAlert.InnerText = "Este usuario ya está registrado en el negocio o no es válido.";
                        }
                    }
                    else
                    {
                        successAlert.Visible = false;
                        failAlert.Visible = true;
                        failAlert.InnerText = "Usuario o negocio inválido.";
                    }
                }
                else
                {
                    successAlert.Visible = false;
                    failAlert.Visible = true;
                    failAlert.InnerText = "Ingrese un nombre de usuario.";
                }
            }
            catch (Exception ex)
            {
                failAlert.Visible = true;
                successAlert.Visible = false;
                failAlert.InnerText = "Ha ocurrido un error.";
                // Log the exception for debugging
            }
        }

        //Get Data from Database
        public void GetDataFromDatabase()
        {
            int BusinessId = (int)Session["BusinessID"];

            DataTable dt = BusinessConnector.GetClientCardData(BusinessId);

            GenerateTableHtml(dt);
        }

        //Table Generation
        public void GenerateTableHtml(DataTable dt)
        {
            StringBuilder sbHeader = new StringBuilder();
            StringBuilder sbBody = new StringBuilder();

            // Table Header
            foreach (DataColumn dc in dt.Columns)
            {
                sbHeader.Append("<th>").Append(dc.ColumnName).Append("</th>");
            }

            // Table Body
            foreach (DataRow row in dt.Rows)
            {
                sbBody.Append("<tr>");

                foreach (DataColumn column in dt.Columns)
                {                   
                    sbBody.Append("<td>").Append(row[column.ColumnName]).Append("</td>");
                }                
                sbBody.Append("</tr>");
            }

            // Set the generated HTML to the literal controls            
            ltTblHeader.Text = sbHeader.ToString();
            ltTblContent.Text = sbBody.ToString();
        }

        private void SendEmailCards(string emailUser, string userName, string GoogleURL, string BusinessName)
        {
            // Send the email
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("DigitalCards", GetRequiredSetting("SmtpFrom")));
                message.To.Add(new MailboxAddress("",emailUser));
                message.Subject = "Agrega tu tarjeta de identificación a tu Wallet";

                var bodyHtml = new TextPart("html")
                {
                    Text = $"<body style='font-family: Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 0;'>\r\n    <table align='center' width='600' style='background-color: #ffffff; padding: 20px; border-radius: 8px;'>\r\n        <tr>\r\n            <td style='text-align: center; padding-bottom: 20px;'>\r\n                <h1 style='color: #333;'>¡Tu tarjeta digital está lista!</h1>\r\n            </td>\r\n        </tr>\r\n        <tr>\r\n            <td style='font-size: 16px; color: #333; line-height: 1.5; padding-bottom: 30px;'>\r\n                <p>Estimado usuario {userName},</p>\r\n                <p>Nos complace informarte que tu tarjeta digital de {BusinessName} está lista para ser añadida a tu billetera digital. Puedes agregarla a tu dispositivo móvil usando Google Wallet.</p>\r\n                <p>Haz clic en el boton a continuación para agregar tu tarjeta digital:</p>\r\n            </td>\r\n        </tr>\r\n        <tr>\r\n            <td style='text-align: center;'>\r\n                <!-- Botón con el logo de Google Wallet -->\r\n                <a href='{GoogleURL}'>\r\n                    <img class=\"wallet\" src=\"cid:google_logo\" alt=\"Agregar a la Billetera de Google\">\r\n                </a>\r\n            </td>\r\n        </tr>\r\n        <tr>\r\n            <td style='text-align: center; font-size: 14px; color: #888; padding-top: 30px;'>\r\n                <p>Si tienes alguna pregunta, no dudes en contactarnos.</p>\r\n                <p>Gracias por usar nuestros servicios.</p>\r\n            </td>\r\n        </tr>\r\n    </table>\r\n</body>"
                };

                var image = new MimePart("image", "png")
                {
                    Content = new MimeContent(File.OpenRead(Server.MapPath("~/Resources/GoogleButton/esUS_add_to_google_wallet_add-wallet-badge.png"))),
                    ContentId = "google_logo",
                    ContentDisposition = new ContentDisposition(ContentDisposition.Inline),
                    ContentTransferEncoding = ContentEncoding.Base64,
                };

                var multipart = new Multipart("related")
                {
                    bodyHtml,
                    image
                };

                message.Body = multipart;

                using (var client = new SmtpClient())
                {
                    client.Connect(GetRequiredSetting("SmtpHost"), GetSmtpPort(), MailKit.Security.SecureSocketOptions.SslOnConnect);
                    client.Authenticate(GetRequiredSetting("SmtpUserName"), GetRequiredSetting("SmtpPassword"));
                    client.Send(message);
                    client.Disconnect(true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Hubo un error al enviar el correo");
            }
        }
        private static string GetRequiredSetting(string key)
        {
            string value = Environment.GetEnvironmentVariable(key);
            if (string.IsNullOrWhiteSpace(value))
            {
                value = ConfigurationManager.AppSettings[key];
            }

            if (string.IsNullOrWhiteSpace(value) ||
                value.StartsWith("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"{key} is not configured. Use an environment variable or Web.config appSettings.");
            }

            return value;
        }

        private static int GetSmtpPort()
        {
            string value = Environment.GetEnvironmentVariable("SmtpPort");
            if (string.IsNullOrWhiteSpace(value))
            {
                value = ConfigurationManager.AppSettings["SmtpPort"];
            }

            int port;
            if (!int.TryParse(value, out port))
            {
                throw new InvalidOperationException("SmtpPort is not configured with a valid number.");
            }

            return port;
        }
    }
}