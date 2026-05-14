using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using DigitalCardsApp.Connectors;

namespace DigitalCardsApp
{
    public partial class AdminDisplayPage : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetExpires(DateTime.UtcNow.AddDays(-1));
            Response.Cache.SetNoStore();

            GetDataFromDatabase();

            if (!IsPostBack)
            {
                CheckUser();
            }
        }

        //Check if the user has credentials
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

        //Get Data from Database
        public void GetDataFromDatabase()
        {
            DataTable dt = AdminConnector.GetBusinessData();

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
                if (dc.ColumnName != "BusinessID")  // Exclude BusinessID from the header
                {
                    sbHeader.Append("<th>").Append(dc.ColumnName).Append("</th>");
                }
            }
            sbHeader.Append("<th>Modificar</th>");  // Add the "Modificar" header

            // Table Body
            foreach (DataRow row in dt.Rows)
            {
                sbBody.Append("<tr>");
                string BusNo = "";

                foreach (DataColumn column in dt.Columns)
                {
                    if (column.ColumnName == "BusinessID")
                    {
                        BusNo = row[column.ColumnName].ToString();  // Capture BusinessID
                    }
                    else
                    {
                        sbBody.Append("<td>").Append(row[column.ColumnName]).Append("</td>");  // Add other columns to the table
                    }
                }

                // Add the "Modificar" column with a link
                sbBody.Append("<td>");
                if (!string.IsNullOrEmpty(BusNo))
                {
                    sbBody.Append("<a class='btn btn-success' href='AdminModPage.aspx?BusNo=" + BusNo + "'>Modificar</a>");
                }
                else
                {
                    sbBody.Append("N/A");  // Placeholder if BusinessID is missing
                }
                sbBody.Append("</td>");
                sbBody.Append("</tr>");
            }

            // Set the generated HTML to the literal controls            
            ltTblHeader.Text = sbHeader.ToString();
            ltTblContent.Text = sbBody.ToString();
        }
    }
}