using System;
using System.Data;
using System.Data.SqlClient;

namespace LogistiCoreV5
{
    public partial class HomePage : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
        }

        protected void btnClientLogin_Click(object sender, EventArgs e)
        {
            Response.Redirect("Login.aspx");
        }

        protected void btnBookShipment_Click(object sender, EventArgs e)
        {
            Response.Redirect("Login.aspx");
        }

        protected void btnTrackShipment_Click(object sender, EventArgs e)
        {
            string trackingNum = txtTrackingNumber.Text.Trim();

            if (string.IsNullOrEmpty(trackingNum))
            {
                lblTrackResult.Text = "Please enter a tracking number.";
                return;
            }

            string query = "SELECT Destination, Status FROM Shipments WHERE TrackingNumber = @TrackingNumber";
            SqlParameter[] parameters = {
                new SqlParameter("@TrackingNumber", trackingNum)
            };

            DataTable dt = DbHelper.GetData(query, parameters);

            if (dt.Rows.Count > 0)
            {
                string status = dt.Rows[0]["Status"].ToString();
                string destination = dt.Rows[0]["Destination"].ToString();
                lblTrackResult.Text = $"Status: {status} | Destination: {destination}";
            }
            else
            {
                lblTrackResult.Text = "Shipment not found. Check tracking number.";
            }
        }

        protected void btnCalculateQuote_Click(object sender, EventArgs e)
        {
            if (double.TryParse(txtWeight.Text, out double weight))
            {
                double estimatedCost = weight * 12.5; // Example rate calculation
                lblQuoteResult.Text = $"Estimated Shipping Cost: ${estimatedCost:F2}";
            }
            else
            {
                lblQuoteResult.Text = "Please enter a valid weight in KG.";
            }
        }
    }
}