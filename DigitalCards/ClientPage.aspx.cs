using DigitalCardsApp.Connectors;
using QRCoder;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace DigitalCardsApp
{
    public partial class ClientPage2 : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetExpires(DateTime.UtcNow.AddDays(-1));
            Response.Cache.SetNoStore();

            Response.ContentEncoding = System.Text.Encoding.UTF8;

            if (!IsPostBack)
            {
                CheckUser();
                DisplayUserName();
            }
        }

        public void CheckUser()
        {
            if (Session["UserID"] != null && Session["UserName"] != null && (int)Session["RoleID"] == 2)
            {
                string userName = Session["UserName"].ToString();
                GenerateQrCode(userName);
                BindCardList(); // Llama a las tarjetas desde la base de datos
            }
            else
            {
                Response.Redirect("NotAuthorized.aspx");
            }
        }
        public void DisplayUserName()
        {
            // Asegúrate de que la sesión tenga un valor
            if (Session["UserName"] != null)
            {
                UserNameLabel.Text = Session["UserName"].ToString();
            }
        }

        public void GenerateQrCode(string qrText)
        {
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            {
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrText, QRCodeGenerator.ECCLevel.Q);
                using (QRCode qrCode = new QRCode(qrCodeData))
                {
                    using (Bitmap qrCodeImage = qrCode.GetGraphic(20))
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            qrCodeImage.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                            string base64Image = Convert.ToBase64String(ms.ToArray());
                            ViewState["QrCodeImage"] = $"data:image/png;base64,{base64Image}";
                        }
                    }
                }
            }
        }

        public void BindCardList()
        {
            int userId = (int)Session["UserID"];
            List<ClientConnector.Card> cards = ClientConnector.GetClientCards(userId);
            CardListRepeater.DataSource = cards;
            CardListRepeater.DataBind();
        }
    }
}