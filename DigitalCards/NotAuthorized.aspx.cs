using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace DigitalCardsApp
{
    public partial class NotAuthorized : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetExpires(DateTime.UtcNow.AddDays(-1));
            Response.Cache.SetNoStore();

            if (string.Equals(Request.QueryString["modern"], "legacy-retired", StringComparison.OrdinalIgnoreCase))
            {
                failAlert.Visible = true;
                failAlert.InnerText = "Este negocio ya opera en app.puntelio.com. Web Forms fue retirado para este negocio.";

                ClientScript.RegisterStartupScript(
                    GetType(),
                    "legacy-retired-message",
                    "document.addEventListener('DOMContentLoaded', function () { var title = document.querySelector('h1'); if (title) { title.textContent = 'Usa app.puntelio.com para continuar'; } });",
                    addScriptTags: true);
            }
        }
    }
}
