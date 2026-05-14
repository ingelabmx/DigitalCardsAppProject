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
    public partial class Registry : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetExpires(DateTime.UtcNow.AddDays(-1));
            Response.Cache.SetNoStore();
        }
        protected void BtnRegistrar_Click(object sender, EventArgs e)
        {
            try
            {
                ClientDetails cln = new ClientDetails();

                cln.UsName = tbNombreUsuario.Value.Trim();
                cln.UsPassword = tbContrasena.Value;
                cln.UsEmail = tbEmail.Value.Trim();
                cln.UsFirstName = tbNombre.Value;
                cln.UsLastName = tbApellido.Value;

                // Basic input validations
                if (!string.IsNullOrEmpty(cln.UsName) &&
                    !string.IsNullOrEmpty(cln.UsPassword) &&
                    !string.IsNullOrEmpty(tbContrasena2.Value) &&
                    !string.IsNullOrEmpty(cln.UsEmail) &&
                    !string.IsNullOrEmpty(cln.UsFirstName) &&
                    !string.IsNullOrEmpty(cln.UsLastName))
                {
                    // Validate email format
                    if (!IsValidEmail(cln.UsEmail))
                    {
                        successAlert.Visible = false;
                        failAlert.Visible = true;
                        failAlert.InnerText = "El formato del correo electrónico es inválido.";
                        return;
                    }

                    // Validate passwords match
                    if (cln.UsPassword == tbContrasena2.Value)
                    {
                        // Hash the password before saving
                        cln.UsPassword = HashPassword(cln.UsPassword);

                        // Insert data into the database
                        ClientConnector.InsertClientData(cln);
                        successAlert.Visible = true;
                        failAlert.Visible = false;
                    }
                    else
                    {
                        successAlert.Visible = false;
                        failAlert.Visible = true;
                        failAlert.InnerText = "Las contraseñas no coinciden.";
                    }
                }
                else
                {
                    successAlert.Visible = false;
                    failAlert.Visible = true;
                    failAlert.InnerText = "Hay que llenar todos los campos.";
                }
            }
            catch (Exception)
            {
                failAlert.Visible = true;
                successAlert.Visible = false;
                failAlert.InnerText = "Correo o nombre de negocio no disponible.";
            }
        }

        // Helper method to validate email format
        private bool IsValidEmail(string email)
        {
            try
            {
                var emailRegex = new System.Text.RegularExpressions.Regex(
                    @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                    System.Text.RegularExpressions.RegexOptions.Compiled);

                return emailRegex.IsMatch(email);
            }
            catch
            {
                return false;
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