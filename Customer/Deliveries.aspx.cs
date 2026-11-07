using System;
using ForteLogisticsApp.Controls;
using ForteLogisticsApp.Security;
using ForteLogisticsApp.Services;

namespace ForteLogisticsApp.CustomerPortal
{
    public partial class Deliveries : SecurePage
    {
        protected DeliveryCards cards;

        protected void Page_Load(object sender, EventArgs e)
        {
            RequireRole("Customer");
            if (!IsPostBack)
            {
                cards.Show(IndividualDashboard.Active(UserId), "You have no deliveries on the way.");
            }
        }
    }
}
