using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using DigitalCardsApp.Connectors;
using DigitalCardsApp.Models;

namespace DigitalCardsApp
{
    public partial class AdminModPage : System.Web.UI.Page
    {
        public string BusNoValue { get; set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetExpires(DateTime.UtcNow.AddDays(-1));
            Response.Cache.SetNoStore();

            CheckQueryString();

            if (!IsPostBack)
            {
                LoadValues();
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

        public void CheckQueryString()
        {
            try
            {
                if (!String.IsNullOrEmpty(Request.QueryString["BusNo"]))
                {

                    BusNoValue = Request.QueryString["BusNo"];

                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void LoadValues()
        {
            DataTable datatable = new DataTable();
            datatable = AdminConnector.GetBusinessDataDetails(BusNoValue);

            if (datatable.Rows.Count > 0)
            {
                tbNombreNegocio.Value = datatable.Rows[0]["BusinessName"].ToString();
                tbContraNegocio.Value = datatable.Rows[0]["BusinessPassword"].ToString();
                tbEmailNegocio.Value = datatable.Rows[0]["BusinessEmail"].ToString();
            }
            else
            {
                tbNombreNegocio.Value = "No data";
                tbContraNegocio.Value = "No data";
                tbEmailNegocio.Value = "No data";
            }
        }

        protected void btActualizar_ServerClick(object sender, EventArgs e)
        {
            try
            {
                AdminDetails adm = new AdminDetails();

                // Basic input validations
                if (string.IsNullOrEmpty(tbNombreNegocio.Value) ||
                    string.IsNullOrEmpty(tbContraNegocio.Value) ||
                    string.IsNullOrEmpty(tbContraNegocio2.Value) ||
                    string.IsNullOrEmpty(tbEmailNegocio.Value))
                {
                    throw new Exception("Todos los campos son obligatorios.");
                }

                // Validate passwords match
                if (tbContraNegocio.Value != tbContraNegocio2.Value)
                {
                    throw new Exception("Las contraseñas no coinciden.");
                }

                // Validate email format
                if (!IsValidEmail(tbEmailNegocio.Value.Trim()))
                {
                    throw new Exception("El formato del correo electrónico es inválido.");
                }

                adm.BusName = tbNombreNegocio.Value;
                adm.BusPassword = HashPassword(tbContraNegocio.Value);
                adm.BusEmail = tbEmailNegocio.Value.Trim();

                // Check if a new logo file is provided
                if (Request.Files["tbLogoNegocio2"] != null && Request.Files["tbLogoNegocio2"].ContentLength > 0)
                {
                    HttpPostedFile uploadedFile = Request.Files["tbLogoNegocio2"];
                    string extension = Path.GetExtension(uploadedFile.FileName).ToLower();

                    // Validate the file type
                    if (extension != ".jpg" && extension != ".png" && extension != ".jpeg" && extension != ".gif")
                    {
                        throw new Exception("Solo se permiten imágenes en formato JPG, PNG, JPEG o GIF.");
                    }

                    // Validate file size (5MB max)
                    if (uploadedFile.ContentLength > 5 * 1024 * 1024) // 5 MB
                    {
                        throw new Exception("El archivo supera el tamaño permitido (5MB).");
                    }

                    // Save the file to the server
                    string fileName = Path.GetFileName(uploadedFile.FileName);
                    string savePath = Server.MapPath("~/Logos/") + fileName;
                    uploadedFile.SaveAs(savePath);

                    // Store the relative path
                    adm.BusLogo = "~/Logos/" + fileName;
                }
                else
                {
                    // If no file is uploaded, set BusLogo to null
                    adm.BusLogo = null;
                }

                // Insert data into the database
                AdminConnector.ModifyBusinessData(BusNoValue, adm);
                successAlert.Visible = true;
                failAlert.Visible = false;
            }
            catch (Exception ex)
            {
                // Handle errors
                failAlert.Visible = true;
                failAlert.InnerText = ex.Message;
                successAlert.Visible = false;
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


        protected void btBorrar_ServerClick(object sender, EventArgs e)
        {
            try
            {

                AdminDetails adm = new AdminDetails();
                adm.BusName = tbNombreNegocio.Value;
                adm.BusEmail = tbEmailNegocio.Value.Trim();

                if (!string.IsNullOrEmpty(adm.BusName) && !string.IsNullOrEmpty(adm.BusEmail))
                {
                    AdminConnector.DeleteBusinessData(BusNoValue, adm);
                    successAlert.Visible = true;
                    successAlert.InnerText = "Eliminación exitosa.";
                    failAlert.Visible = false;
                    
                }
                else
                {
                    successAlert.Visible = false;
                    failAlert.Visible = true;
                    failAlert.InnerText = "Fallo en la eliminación.";
                }
            }
            catch
            {
                successAlert.Visible = false;
                failAlert.Visible = true;
                failAlert.InnerText = "Fallo en la eliminación.";
            }
        }
    }
}