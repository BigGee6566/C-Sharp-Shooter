using System;
using System.Text;
using System.Web.UI;
using ForteLogisticsApp.Security;
using ForteLogisticsApp.Services;

namespace ForteLogisticsApp
{
    /// <summary>
    /// Application shell: the LogistiCore V5 sidebar chrome.
    ///
    /// The navigation is built here rather than in markup because it depends on the
    /// signed-in role, and - for the driver links - on membership of dbo.[Driver],
    /// which is a database question rather than a role question.
    ///
    /// This master deliberately performs NO authentication redirect. Access control is
    /// each page's own job via SecurePage.RequireRole(); a redirect here would also fire
    /// on Login.aspx itself, which uses this master, and send it to itself in a loop.
    /// </summary>
    public partial class SiteMaster : MasterPage
    {
        private string Role
        {
            get { return (Session["Role"] as string) ?? string.Empty; }
        }

        private bool LoggedIn
        {
            get { return Session != null && Session["UserID"] != null; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            // The marketing footer belongs to the public pages only; inside the portal the
            // sidebar is the chrome and a footer just pushes content around.
            litFooter.Text = LoggedIn ? string.Empty : BuildFooter();

            if (!IsPostBack)
            {
                RenderChrome();
            }
        }

        private void RenderChrome()
        {
            pnlAuthActions.Visible = LoggedIn;
            pnlAnonActions.Visible = !LoggedIn;
            pnlAnonLinks.Visible = !LoggedIn;
            btnLogout.Visible = LoggedIn;

            // "Create Order" is a customer action; staff and admins register parcels through Ops.
            btnCreateOrderNav.Visible = LoggedIn &&
                string.Equals(Role, "Customer", StringComparison.OrdinalIgnoreCase);

            if (LoggedIn)
            {
                string fullName = (Session["FullName"] as string) ?? "User";
                litNavUserName.Text = Server.HtmlEncode(fullName);
                litNavUserType.Text = Server.HtmlEncode(DescribeRole(Role));
                litAvatar.Text = Server.HtmlEncode(Initials(fullName));
                litBell.Text = BuildBell();
            }
            else
            {
                litBell.Text = string.Empty;
            }

            litNav.Text = BuildNav();
        }

        /// <summary>Human-readable account descriptor shown under the user's name.</summary>
        private static string DescribeRole(string role)
        {
            switch (role)
            {
                case "Customer": return "Customer Account";
                case "Staff": return "Operations Staff";
                case "Admin": return "Administrator";
                default: return "Account";
            }
        }

        /// <summary>Up to two initials for the avatar circle.</summary>
        private static string Initials(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return "?";

            string[] parts = fullName.Trim().Split(
                new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 1)
            {
                return parts[0].Substring(0, 1).ToUpperInvariant();
            }
            return (parts[0].Substring(0, 1) + parts[parts.Length - 1].Substring(0, 1))
                .ToUpperInvariant();
        }

        private string BuildBell()
        {
            int unread = NotificationService.UnreadCount(SecurePage.CurrentUserId());
            string badge = unread > 0
                ? "<span class='ml-auto bg-secondary text-on-secondary text-[10px] font-black rounded-full px-1.5 py-0.5 leading-none'>" +
                  (unread > 99 ? "99+" : unread.ToString()) + "</span>"
                : string.Empty;

            return "<a class='fcl-nav-link' href='" + ResolveUrl("~/Notifications.aspx") +
                   "'><span class='material-symbols-outlined'>notifications</span>" +
                   "<span>Notifications</span>" + badge + "</a>";
        }

        private void AddLink(StringBuilder sb, string url, string icon, string text)
        {
            sb.Append("<a class='fcl-nav-link")
              .Append(IsActiveLink(url) ? " fcl-nav-link-active" : string.Empty)
              .Append("' href='").Append(ResolveUrl(url)).Append("'>")
              .Append("<span class='material-symbols-outlined'>").Append(icon).Append("</span>")
              .Append("<span>").Append(text).Append("</span></a>");
        }

        private static void AddSection(StringBuilder sb, string title)
        {
            sb.Append("<div class='fcl-nav-section'>").Append(title).Append("</div>");
        }

        private bool IsActiveLink(string url)
        {
            // Anchors point within a page, so they never own the "current page" highlight.
            if (url.Contains("#")) return false;

            string current = Request.AppRelativeCurrentExecutionFilePath;
            if (string.IsNullOrEmpty(current)) return false;

            if (string.Equals(current, url, StringComparison.OrdinalIgnoreCase)) return true;

            // The nav carries one "Reports" entry; keep it lit on every report page.
            if (url.StartsWith("~/Reports/", StringComparison.OrdinalIgnoreCase) &&
                current.StartsWith("~/Reports/", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            return false;
        }

        private string BuildNav()
        {
            var sb = new StringBuilder();

            if (!LoggedIn)
            {
                AddLink(sb, "~/Default.aspx", "home", "Home");
                AddLink(sb, "~/Default.aspx#services", "local_shipping", "Services");
                AddLink(sb, "~/Default.aspx#track", "search", "Track Parcel");
                AddLink(sb, "~/Default.aspx#quote", "calculate", "Get a Quote");
                AddLink(sb, "~/Contact.aspx", "support_agent", "Contact Us");
                return sb.ToString();
            }

            switch (Role)
            {
                case "Customer":
                    AddLink(sb, "~/Customer/Dashboard.aspx", "dashboard", "Dashboard");
                    AddLink(sb, "~/Customer/BookShipment.aspx", "add_box", "Book Shipment");
                    AddLink(sb, "~/Customer/Track.aspx", "search", "Track");
                    AddLink(sb, "~/Customer/Invoices.aspx", "receipt_long", "My Invoices");
                    AddLink(sb, "~/Customer/Profile.aspx", "person", "My Profile");
                    break;

                case "Staff":
                    AddSection(sb, "Operations");
                    AddLink(sb, "~/Ops/Dashboard.aspx", "dashboard", "Dashboard");
                    AddLink(sb, "~/Ops/ParcelRegistration.aspx", "add_box", "Register Parcel");
                    AddLink(sb, "~/Ops/Warehouse.aspx", "warehouse", "Warehouse");
                    AddLink(sb, "~/Ops/RoutePlanning.aspx", "route", "Route Planning");
                    AddLink(sb, "~/Admin/Fleet.aspx", "local_shipping", "Fleet");

                    // Drivers are Staff who also hold a row in dbo.[Driver]; the registry,
                    // not the role, is what decides whether these links belong here.
                    if (SecurePage.IsDriver(SecurePage.CurrentUserId()))
                    {
                        AddSection(sb, "My Deliveries");
                        AddLink(sb, "~/Driver/Dashboard.aspx", "assignment", "My Manifest");
                        AddLink(sb, "~/Driver/DeliveryConfirmation.aspx", "task_alt", "Confirm Delivery");
                    }

                    AddSection(sb, "Reports");
                    AddLink(sb, "~/Reports/DailyParcelMovement.aspx", "analytics", "Reports");
                    break;

                case "Admin":
                    AddSection(sb, "Administration");
                    AddLink(sb, "~/Admin/Dashboard.aspx", "dashboard", "Dashboard");
                    AddLink(sb, "~/Admin/Shipments.aspx", "local_shipping", "Shipments");
                    AddLink(sb, "~/Admin/Users.aspx", "group", "Users");
                    AddLink(sb, "~/Admin/Fleet.aspx", "directions_car", "Fleet");

                    AddSection(sb, "Operations");
                    AddLink(sb, "~/Ops/Dashboard.aspx", "inventory", "Ops Dashboard");
                    AddLink(sb, "~/Ops/ParcelRegistration.aspx", "add_box", "Register Parcel");
                    AddLink(sb, "~/Ops/Warehouse.aspx", "warehouse", "Warehouse");
                    AddLink(sb, "~/Ops/RoutePlanning.aspx", "route", "Route Planning");

                    AddSection(sb, "Finance");
                    AddLink(sb, "~/Finance/Dashboard.aspx", "payments", "Finance");
                    AddLink(sb, "~/Finance/Invoices.aspx", "receipt_long", "Billing");

                    AddSection(sb, "Reports");
                    AddLink(sb, "~/Reports/DailyParcelMovement.aspx", "analytics", "Parcel Movement");
                    AddLink(sb, "~/Reports/DriverPerformance.aspx", "speed", "Driver Performance");
                    AddLink(sb, "~/Reports/MonthlyRevenue.aspx", "trending_up", "Monthly Revenue");
                    AddLink(sb, "~/Reports/LostDamaged.aspx", "report_problem", "Lost &amp; Damaged");
                    break;
            }
            return sb.ToString();
        }

        protected void btnCreateOrder_Click(object sender, EventArgs e)
        {
            Response.Redirect(ResolveUrl("~/Customer/BookShipment.aspx"));
        }

        protected void btnLogout_Click(object sender, EventArgs e)
        {
            Session.Clear();
            Session.Abandon();
            Response.Redirect(ResolveUrl("~/Login.aspx"));
        }

        private string BuildFooter()
        {
            string home = ResolveUrl("~/Default.aspx");
            string register = ResolveUrl("~/Register.aspx");
            string contact = ResolveUrl("~/Contact.aspx");

            return
"<footer class='bg-on-primary-fixed text-surface-variant font-body-md text-label-sm mt-auto border-t border-outline-variant no-print'>" +
"  <div class='max-w-container-max mx-auto px-margin-mobile md:px-margin-desktop py-stack-lg grid grid-cols-1 md:grid-cols-4 gap-gutter'>" +
"    <div>" +
"      <div class='text-on-primary font-headline-md text-headline-md font-bold mb-base'>LOGISTICORE COURIER SERVICE</div>" +
"      <p class='leading-relaxed mb-stack-sm'>Providing specialized end-to-end supply chain and courier services across Southern Africa.</p>" +
"      <p class='text-outline'>&copy; 2026 Logisticore Courier Service (Pty) Ltd.</p>" +
"    </div>" +
"    <div>" +
"      <h4 class='text-on-primary font-label-md text-label-md mb-stack-sm uppercase tracking-wider'>Services</h4>" +
"      <ul class='space-y-2'>" +
"        <li><a class='hover:text-on-primary transition' href='" + home + "#services'>Same Day Express</a></li>" +
"        <li><a class='hover:text-on-primary transition' href='" + home + "#services'>Next Day Regional</a></li>" +
"        <li><a class='hover:text-on-primary transition' href='" + home + "#services'>Warehousing &amp; Fulfillment</a></li>" +
"      </ul>" +
"    </div>" +
"    <div>" +
"      <h4 class='text-on-primary font-label-md text-label-md mb-stack-sm uppercase tracking-wider'>Quick Links</h4>" +
"      <ul class='space-y-2'>" +
"        <li><a class='hover:text-on-primary transition' href='" + home + "#track'>Track a Parcel</a></li>" +
"        <li><a class='hover:text-on-primary transition' href='" + register + "'>Register an Account</a></li>" +
"        <li><a class='hover:text-on-primary transition' href='" + contact + "'>Contact Us</a></li>" +
"      </ul>" +
"    </div>" +
"    <div>" +
"      <h4 class='text-on-primary font-label-md text-label-md mb-stack-sm uppercase tracking-wider'>Contact</h4>" +
"      <p class='mb-2'><span class='material-symbols-outlined mr-2'>call</span> 0860 LOGISTICORE</p>" +
"      <p class='mb-2'><span class='material-symbols-outlined mr-2'>mail</span> support@logisticore.co.za</p>" +
"      <p class='mb-2'><span class='material-symbols-outlined mr-2'>location_on</span> Isando, Gauteng</p>" +
"    </div>" +
"  </div>" +
"</footer>";
        }
    }
}
