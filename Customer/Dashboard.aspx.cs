using System;
using System.Collections.Generic;
using System.Web.UI;

namespace LogistiCoreV5
{
    public partial class Dashboard : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                LoadRecentActivity();
            }
        }

        private void LoadRecentActivity()
        {
            var activities = new List<RecentActivityItem>
            {
                new RecentActivityItem
                {
                    TrackingID = "LC-84729104ZA",
                    Destination = "KuGompo, East London (EC)",
                    StatusBadgeHtml = "<span class=\"inline-flex items-center gap-1.5 px-2 py-0.5 rounded text-label-sm bg-[#e6f4ea] text-[#137333]\"><span class=\"w-1.5 h-1.5 rounded-full bg-[#137333]\"></span> Delivered</span>",
                    Date = "Aug 09, 2026"
                },
                new RecentActivityItem
                {
                    TrackingID = "LC-95830211ZA",
                    Destination = "Johannesburg, GP",
                    StatusBadgeHtml = "<span class=\"inline-flex items-center gap-1.5 px-2 py-0.5 rounded text-label-sm bg-primary-fixed text-primary\"><span class=\"w-1.5 h-1.5 rounded-full bg-primary\"></span> In Transit</span>",
                    Date = "Aug 08, 2026"
                },
                new RecentActivityItem
                {
                    TrackingID = "LC-10293847ZA",
                    Destination = "Cape Town, WC",
                    StatusBadgeHtml = "<span class=\"inline-flex items-center gap-1.5 px-2 py-0.5 rounded text-label-sm bg-surface-container-highest text-on-surface-variant\"><span class=\"w-1.5 h-1.5 rounded-full bg-outline\"></span> Pending</span>",
                    Date = "Aug 10, 2026"
                },
                new RecentActivityItem
                {
                    TrackingID = "LC-38291044ZA",
                    Destination = "Gqeberha, EC",
                    StatusBadgeHtml = "<span class=\"inline-flex items-center gap-1.5 px-2 py-0.5 rounded text-label-sm bg-[#e6f4ea] text-[#137333]\"><span class=\"w-1.5 h-1.5 rounded-full bg-[#137333]\"></span> Delivered</span>",
                    Date = "Aug 05, 2026"
                },
                new RecentActivityItem
                {
                    TrackingID = "LC-56473829ZA",
                    Destination = "Durban, KZN",
                    StatusBadgeHtml = "<span class=\"inline-flex items-center gap-1.5 px-2 py-0.5 rounded text-label-sm bg-primary-fixed text-primary\"><span class=\"w-1.5 h-1.5 rounded-full bg-primary\"></span> In Transit</span>",
                    Date = "Aug 07, 2026"
                }
            };

            rptRecentActivity.DataSource = activities;
            rptRecentActivity.DataBind();
        }

        protected void btnHeaderCreateOrder_Click(object sender, EventArgs e)
        {
            Response.Redirect("CreateOrder.aspx");
        }
    }

    public class RecentActivityItem
    {
        public string TrackingID { get; set; }
        public string Destination { get; set; }
        public string StatusBadgeHtml { get; set; }
        public string Date { get; set; }
    }
}