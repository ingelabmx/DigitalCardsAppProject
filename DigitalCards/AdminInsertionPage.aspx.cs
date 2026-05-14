using Org.BouncyCastle.Asn1.Cmp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using DigitalCardsApp.Models;
using DigitalCardsApp.Connectors;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace DigitalCardsApp
{
    public partial class AdminInsertionPage : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetExpires(DateTime.UtcNow.AddDays(-1));
            Response.Cache.SetNoStore();

            if (!IsPostBack)
            {
                CheckUser();
            }
        }

        public void CheckUser()
        {
            if (Session["UserID"] != null && Session["UserName"] != null && (int)Session["RoleID"] == 1)
            {
                int userId = (int)Session["UserID"];
                string userName = Session["UserName"].ToString();
            }
            else
            {
                Response.Redirect("NotAuthorized.aspx");
            }
        }

        protected void btAgregar_ServerClick(object sender, EventArgs e)
        {
            try
            {
                AdminDetails adm = new AdminDetails();
                Loyalty loyalty = new Loyalty();

                // Validate input fields
                if (string.IsNullOrEmpty(tbNombreNegocio.Value) ||
                    string.IsNullOrEmpty(tbContraNegocio.Value) ||
                    string.IsNullOrEmpty(tbContraNegocio2.Value) ||
                    string.IsNullOrEmpty(tbEmailNegocio.Value))
                {
                    throw new Exception("Todos los campos son obligatorios.");
                }

                // Validate password match
                if (tbContraNegocio.Value != tbContraNegocio2.Value)
                {
                    throw new Exception("Las contraseñas no coinciden.");
                }

                // Validate email format
                if (!IsValidEmail(tbEmailNegocio.Value.Trim()))
                {
                    throw new Exception("El formato del correo electrónico es inválido.");
                }

                // Assign business info
                adm.BusName = tbNombreNegocio.Value;
                adm.BusPassword = HashPassword(tbContraNegocio.Value);
                adm.BusEmail = tbEmailNegocio.Value.Trim();

                // File handling
                if (Request.Files["tbLogoNegocio2"] != null && Request.Files["tbLogoNegocio2"].ContentLength > 0)
                {
                    HttpPostedFile uploadedFile = Request.Files["tbLogoNegocio2"];
                    string extension = Path.GetExtension(uploadedFile.FileName).ToLower();

                    // Validate file type
                    if (extension != ".jpg" && extension != ".png" && extension != ".jpeg" && extension != ".gif")
                    {
                        throw new Exception("Solo se permiten imágenes en formato JPG, PNG, JPEG o GIF.");
                    }

                    // Validate size (5MB max)
                    if (uploadedFile.ContentLength > 5 * 1024 * 1024)
                    {
                        throw new Exception("El archivo supera el tamaño permitido (5MB).");
                    }

                    // Ensure directory exists
                    string logoFolder = Server.MapPath("~/Logos/");
                    if (!Directory.Exists(logoFolder))
                    {
                        Directory.CreateDirectory(logoFolder);
                    }

                    // Sanitize and save
                    string fileName = Path.GetFileName(uploadedFile.FileName);
                    string safeFileName = Path.GetFileNameWithoutExtension(fileName).Replace(" ", "_") + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + extension;
                    string fullPath = Path.Combine(logoFolder, safeFileName);
                    uploadedFile.SaveAs(fullPath);

                    // Save relative path
                    adm.BusLogo = "~/Logos/" + safeFileName;
                }
                else
                {
                    throw new Exception("Debe subir un logo para el negocio.");
                }

                // Save to DB
                AdminConnector.InsertBusinessData(adm);

                // Google Wallet
                loyalty.CreateClass(TextFormats.DeleteSpace(adm.BusName));

                // UI feedback
                successAlert.Visible = true;
                failAlert.Visible = false;
            }
            catch (Exception ex)
            {
                failAlert.Visible = true;
                failAlert.InnerText = ex.Message;
                successAlert.Visible = false;
            }
        }

        private string SanitizeFileName(string originalFileName)
        {
            string nameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
            string extension = Path.GetExtension(originalFileName).ToLower();

            // Replace spaces and invalid characters with underscores
            string safeName = Regex.Replace(nameWithoutExtension, @"[^a-zA-Z0-9_-]", "_");

            // Append timestamp to avoid name conflicts
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");

            return $"{safeName}_{timestamp}{extension}";
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