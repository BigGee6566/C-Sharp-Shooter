using System;
using System.Data;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ForteLogisticsApp.Data;
using ForteLogisticsApp.Services;

namespace ForteLogisticsApp.Controls
{
    /// <summary>The Individual customer's parcel card list: dashboard, My Deliveries and History.</summary>
    public partial class DeliveryCards : UserControl
    {
        protected Repeater rptCards;
        protected Panel pnlEmpty;
        protected Literal litEmpty;

        public void Show(DataTable parcels, string emptyText)
        {
            rptCards.DataSource = parcels;
            rptCards.DataBind();
            pnlEmpty.Visible = parcels.Rows.Count == 0;
            litEmpty.Text = HttpUtility.HtmlEncode(emptyText);
        }

        protected string TrackUrl(object trackingNumber)
        {
            return ResolveUrl("~/Customer/Track.aspx?wb=" + HttpUtility.UrlEncode(Db.ToStr(trackingNumber)));
        }

        protected string Badge(object status)
        {
            string value = Db.ToStr(status);
            return UiHelper.StatusBadge(value, IndividualDashboard.StatusLabel(value));
        }

        protected string Note(object dataItem)
        {
            DataRow row = ((DataRowView)dataItem).Row;
            DateTime? delivered = row["DeliveryDate"] == DBNull.Value ? (DateTime?)null : (DateTime)row["DeliveryDate"];

            return UiHelper.Enc(IndividualDashboard.DeliveryNote(
                Db.ToStr(row["DeliveryStatus"]), (DateTime)row["ShipmentDate"], delivered,
                Db.ToStr(row["DestCity"]), DateTime.Now));
        }
    }
}
