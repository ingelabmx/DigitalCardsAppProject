using DigitalCardsApp.Connectors;
using DigitalCardsApp.Models;
using Org.BouncyCastle.Crypto.Generators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Services.Description;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace DigitalCardsApp
{
    public partial class ResetPasswordPage : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // Validate if the token exists in the query string
                string token = Request.QueryString["token"];
                if (string.IsNullOrEmpty(token) || !ClientConnector.IsResetTokenValid(token))
                {
                    failAlert.Visible = true;
                    failAlert.InnerText = "El enlace de reinicio de contraseña no es válido o ha expirado.";
                    btEnviar.Visible = false; // Disable the "Enviar" button
                }
            }
        }

        protected void btEnviar_Click(object sender, EventArgs e)
        {
            string newPassword = tbContrasena.Value;
            string confirmPassword = tbContrasena2.Value;
            string token = Request.QueryString["token"];

            // Validate password inputs
            if (string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                failAlert.Visible = true;
                failAlert.InnerText = "Por favor, complete todos los campos.";
                return;
            }

            if (newPassword != confirmPassword)
            {
                failAlert.Visible = true;
                failAlert.InnerText = "Las contraseñas no coinciden.";
                return;
            }

            // Validate token
            string userEmail = ClientConnector.GetEmailByToken(token);
            if (string.IsNullOrEmpty(userEmail))
            {
                failAlert.Visible = true;
                failAlert.InnerText = "El enlace de reinicio de contraseña no es válido o ha expirado.";
                return;
            }

            // Hash the password before saving (using a library like BCrypt.Net or similar)
            string hashedPassword = HashPassword(newPassword);

            // Update the password in the database
            if (ClientConnector.UpdateClientPassword(userEmail, hashedPassword))
            {
                // Remove the used token from the database
                ClientConnector.DeletePasswordResetToken(userEmail);

                successAlert.Visible = true;
                successAlert.InnerText = "Su contraseña ha sido actualizada exitosamente.";

                tbContrasena.Disabled = true;
                tbContrasena2.Disabled = true;
                btEnviar.Disabled = true;
            }
            else
            {
                failAlert.Visible = true;
                failAlert.InnerText = "Ocurrió un error al actualizar la contraseña. Inténtelo de nuevo.";
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