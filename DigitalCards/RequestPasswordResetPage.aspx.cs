using DigitalCardsApp.Connectors;
using DigitalCardsApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using MimeKit;
using MailKit.Net.Smtp;
using System.IO;
using System.Configuration;

namespace DigitalCardsApp
{
    public partial class RequestPasswordResetPage : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack)
            {
                
            }
        }

        private void SendEmail(string emailUser, string resetToken)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("DigitalCards", GetRequiredSetting("SmtpFrom")));
                message.To.Add(new MailboxAddress("", emailUser));
                message.Subject = "Cambio de contraseña";

                // Generate the reset URL with the token
                string resetUrl = $"{Request.Url.GetLeftPart(UriPartial.Authority)}/ResetPasswordPage.aspx?token={resetToken}";
                //string resetUrl = $"https://ResetPasswordPage.aspx?token={HttpUtility.UrlEncode(resetToken)}";

                var bodyHtml = new TextPart("html")
                {
                    Text = $@"
            <body style='font-family: Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 0;'>
                <table align='center' width='600' style='background-color: #ffffff; padding: 20px; border-radius: 8px;'>
                    <tr>
                        <td style='text-align: center; padding-bottom: 20px;'>
                            <h1 style='color: #333;'>Cambio de contraseña</h1>
                        </td>
                    </tr>
                    <tr>
                        <td style='font-size: 16px; color: #333; line-height: 1.5; padding-bottom: 30px;'>
                            <p>Estimado usuario,</p>
                            <p>Se ha solicitado un cambio de contraseña. En caso de que no haya sido solicitado por usted, ignore este mensaje.</p>
                            <p>Haz clic en el siguiente enlace para realizar el cambio:</p>
                        </td>
                    </tr>
                    <tr>
                        <td style='text-align: center;'>
                            <a href='{resetUrl}' style='text-decoration: none; background-color: #4CAF50; color: white; padding: 10px 20px; border-radius: 5px;'>Cambiar Contraseña</a>
                        </td>
                    </tr>
                    <tr>
                        <td style='text-align: center; font-size: 14px; color: #888; padding-top: 30px;'>
                            <p>Si tienes alguna pregunta, no dudes en contactarnos.</p>
                            <p>Gracias por usar nuestros servicios.</p>
                        </td>
                    </tr>
                </table>
            </body>"
                };

                message.Body = bodyHtml;

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
                Console.WriteLine("Hubo un error al enviar el correo: " + ex.Message);
            }
        }

        protected void btEnviar_Click(object sender, EventArgs e)
        {
            string email = tbCorreo.Value;

            // Validate email input
            if (string.IsNullOrEmpty(email))
            {
                failAlert.Visible = true;
                failAlert.InnerText = "Por favor, ingrese un correo válido.";
                return;
            }

            // Check if the email exists in the database
            if (ClientConnector.DoesEmailExist(email))
            {
                try
                {
                    // Generate a unique reset token
                    string resetToken = Guid.NewGuid().ToString();
                    DateTime expirationTime = DateTime.UtcNow.AddHours(1); // Set expiration time

                    // Store the reset token in the database
                    ClientConnector.StorePasswordResetToken(email, resetToken, expirationTime);

                    // Send the reset email
                    SendEmail(email, resetToken);

                    // Display success message
                    successAlert.Visible = true;
                    successAlert.InnerText = "Se ha enviado un enlace de restablecimiento a tu correo electrónico.";
                    failAlert.Visible = false;
                }
                catch (Exception ex)
                {
                    failAlert.Visible = true;
                    failAlert.InnerText = "Hubo un error al enviar el correo: " + ex.Message;
                }
            }
            else
            {
                // Email does not exist
                failAlert.Visible = true;
                failAlert.InnerText = "El correo ingresado no existe.";
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