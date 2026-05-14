using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using DigitalCardsApp.Connectors;
using MySql.Data.MySqlClient;
using System.Configuration;
using System.Text;
using DigitalCardsApp.Models;
using System.Security.Cryptography;

namespace DigitalCardsApp
{
    public partial class Login : System.Web.UI.Page
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
                // Get the current number of failed attempts from the session
                int failedAttempts = Session["FailedAttempts"] != null ? (int)Session["FailedAttempts"] : 0;

                // Check if the user is locked out
                if (failedAttempts >= 3)
                {
                    failAlert.Visible = true;
                    successAlert.Visible = false;
                    failAlert.InnerText = "La sesión está bloqueada debido a múltiples intentos fallidos. Inténtelo más tarde.";
                    return;
                }

                var clientDetails = new ClientDetails
                {
                    UsName = tbUsuario.Value.Trim(),
                    UsPassword = HashPassword(tbContrasena.Value) // Ensure passwords are hashed
                };

                var authenticatedClient = ClientConnector.ClientLogin(clientDetails);

                if (authenticatedClient != null)
                {
                    // Reset failed attempts on successful login
                    Session["FailedAttempts"] = 0;

                    // Store user data in session variables
                    Session["UserID"] = authenticatedClient.UsID;
                    Session["UserName"] = authenticatedClient.UsName;
                    Session["FirstName"] = authenticatedClient.UsFirstName;
                    Session["LastName"] = authenticatedClient.UsLastName;
                    Session["Email"] = authenticatedClient.UsEmail;
                    Session["RoleID"] = authenticatedClient.UsRole;

                    // Redirect based on role
                    if (authenticatedClient.UsRole == 1) // Administrator
                    {
                        Response.Redirect("AdminInsertionPage.aspx");
                    }
                    else if (authenticatedClient.UsRole == 2) // Business User
                    {
                        Response.Redirect("ClientPage.aspx");
                    }
                }
                else
                {
                    // Increment failed attempts
                    failedAttempts++;
                    Session["FailedAttempts"] = failedAttempts;

                    failAlert.Visible = true;
                    successAlert.Visible = false;

                    if (failedAttempts >= 3)
                    {
                        failAlert.InnerText = "La cuenta está bloqueada debido a múltiples intentos fallidos. Inténtelo más tarde.";
                    }
                    else
                    {
                        failAlert.InnerText = "Nombre de usuario o contraseña inexistentes. Intentos restantes: " + (3 - failedAttempts);
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