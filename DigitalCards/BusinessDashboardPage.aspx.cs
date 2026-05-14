using DigitalCardsApp.Connectors;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace DigitalCardsApp
{
    public partial class BusinessDashboardPage : System.Web.UI.Page
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
                InjectChartData();
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

        private void InjectChartData()
        {
            int BusinessId = (int)Session["BusinessID"];
            DataTable chartData = BusinessConnector.GetYearData(BusinessId);
            string chartJsonData = JsonConvert.SerializeObject(chartData);

            ltScriptData.Text = $"<script>var chartData = {chartJsonData};</script>";
        }

        //Get Data from Database
        public void GetDataFromDatabase()
        {
            int BusinessId = (int)Session["BusinessID"];

            DataTable tableData = BusinessConnector.GetLast5Checks(BusinessId);

            GenerateTableHtml(tableData);
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
    }
}