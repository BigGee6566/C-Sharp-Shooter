<%@ Page Title="Customer Dashboard" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="Dashboard.aspx.cs" Inherits="ForteLogisticsApp.CustomerPortal.Dashboard" %>
<%@ Import Namespace="ForteLogisticsApp.Services" %>

<asp:Content ID="c1" ContentPlaceHolderID="MainContent" runat="server">

    <!-- Header -->
    <header class="flex flex-col md:flex-row justify-between items-start md:items-center mb-stack-lg gap-gutter">
        <div>
            <h1 class="font-headline-lg text-headline-lg-mobile md:text-headline-lg text-on-background mb-base">
                <asp:Literal ID="litTitle" runat="server" /></h1>
            <p class="font-body-md text-body-md text-on-surface-variant">
                Welcome back, <asp:Literal ID="litName" runat="server" />.</p>
        </div>
        <div class="flex flex-wrap gap-base items-center">
            <asp:Button ID="btnHeaderCreateOrder" runat="server" Text="+ Create New Order"
                CssClass="fcl-btn" OnClick="btnHeaderCreateOrder_Click" />
            <a href="<%= ResolveUrl("~/Customer/Track.aspx") %>" class="fcl-btn fcl-btn-dark">
                <span class="material-symbols-outlined">search</span> Track a Parcel
            </a>
        </div>
    </header>

    <!-- Metrics -->
    <section class="grid grid-cols-1 md:grid-cols-3 gap-gutter mb-stack-lg">
        <div class="bg-surface-container-lowest border border-outline-variant p-stack-md rounded-lg relative overflow-hidden shadow-[0_20px_40px_-15px_rgba(0,51,153,0.04)]">
            <div class="flex justify-between items-start mb-stack-sm">
                <h3 class="font-label-md text-label-md text-on-surface-variant">Active Shipments</h3>
                <span class="material-symbols-outlined text-primary">flight_takeoff</span>
            </div>
            <p class="font-headline-xl text-headline-xl text-on-background mb-base">
                <asp:Literal ID="litActiveShipments" runat="server" /></p>
            <p class="font-label-sm text-label-sm text-on-surface-variant">Assigned, in transit or out for delivery</p>
            <div class="absolute -bottom-4 -right-4 opacity-5 pointer-events-none">
                <span class="material-symbols-outlined text-[120px]">local_shipping</span>
            </div>
        </div>

        <div class="bg-surface-container-lowest border border-outline-variant p-stack-md rounded-lg relative overflow-hidden shadow-[0_20px_40px_-15px_rgba(0,51,153,0.04)]">
            <div class="flex justify-between items-start mb-stack-sm">
                <h3 class="font-label-md text-label-md text-on-surface-variant">Delivered</h3>
                <span class="material-symbols-outlined text-primary">task_alt</span>
            </div>
            <p class="font-headline-xl text-headline-xl text-on-background mb-base">
                <asp:Literal ID="litDelivered" runat="server" /></p>
            <p class="font-label-sm text-label-sm text-on-surface-variant">
                Out of <asp:Literal ID="litTotal" runat="server" /> shipment(s) booked</p>
        </div>

        <div class="bg-surface-container-lowest border border-outline-variant p-stack-md rounded-lg relative overflow-hidden shadow-[0_20px_40px_-15px_rgba(0,51,153,0.04)]">
            <div class="flex justify-between items-start mb-stack-sm">
                <h3 class="font-label-md text-label-md text-on-surface-variant">Outstanding Balance</h3>
                <span class="material-symbols-outlined text-primary">account_balance_wallet</span>
            </div>
            <p class="font-headline-xl text-headline-xl text-on-background mb-base">
                R <asp:Literal ID="litOutstanding" runat="server" /></p>
            <p class="font-label-sm text-label-sm text-on-surface-variant">Unpaid invoices on your account</p>
        </div>
    </section>

    <!-- Activity + notifications -->
    <div class="grid grid-cols-1 lg:grid-cols-3 gap-gutter mb-stack-lg">

        <section class="lg:col-span-2 bg-surface-container-lowest border border-outline-variant rounded-lg overflow-hidden shadow-[0_20px_40px_-15px_rgba(0,51,153,0.04)]">
            <div class="p-stack-md border-b border-outline-variant flex justify-between items-center">
                <h2 class="font-headline-md text-headline-md text-on-background">Recent Activity</h2>
                <a href="<%= ResolveUrl("~/Customer/Track.aspx") %>" class="font-label-md text-label-md text-primary hover:underline">View All</a>
            </div>
            <div class="overflow-x-auto">
                <asp:Repeater ID="rptShipments" runat="server">
                    <HeaderTemplate>
                        <table class="report-table">
                            <thead>
                                <tr>
                                    <th>Tracking Number</th>
                                    <th>Destination</th>
                                    <th>Status</th>
                                    <th>Date</th>
                                </tr>
                            </thead>
                            <tbody>
                    </HeaderTemplate>
                    <ItemTemplate>
                        <tr>
                            <td class="font-medium text-primary"><%# UiHelper.Enc(Eval("TrackingNumber")) %></td>
                            <td><%# UiHelper.Enc(Eval("DestCity")) %></td>
                            <td><%# UiHelper.StatusBadge(Eval("DeliveryStatus") as string) %></td>
                            <td class="text-on-surface-variant"><%# Eval("ShipmentDate", "{0:dd MMM yyyy}") %></td>
                        </tr>
                    </ItemTemplate>
                    <FooterTemplate>
                            </tbody>
                        </table>
                    </FooterTemplate>
                </asp:Repeater>

                <asp:Panel ID="pnlNoShipments" runat="server" Visible="false" CssClass="p-stack-md text-center">
                    <span class="material-symbols-outlined ms-32 text-outline">inbox</span>
                    <p class="font-body-md text-body-md text-on-surface-variant mt-base">
                        You haven't booked any shipments yet.</p>
                    <a href="<%= ResolveUrl("~/Customer/BookShipment.aspx") %>" class="fcl-btn mt-stack-sm">Book your first shipment</a>
                </asp:Panel>
            </div>
        </section>

        <aside class="bg-surface-container-lowest border border-outline-variant rounded-lg p-stack-md shadow-[0_20px_40px_-15px_rgba(0,51,153,0.04)]">
            <div class="flex justify-between items-center mb-stack-sm">
                <h3 class="font-headline-md text-headline-md text-on-background">Notifications</h3>
                <a href="<%= ResolveUrl("~/Notifications.aspx") %>" class="font-label-md text-label-md text-primary hover:underline">All</a>
            </div>

            <asp:Repeater ID="rptNotifications" runat="server">
                <ItemTemplate>
                    <div class="py-stack-sm border-b border-outline-variant last:border-0">
                        <p class="font-body-md text-label-md text-on-surface"><%# UiHelper.Enc(Eval("Message")) %></p>
                        <p class="font-label-sm text-label-sm text-on-surface-variant mt-1">
                            <%# UiHelper.Enc(Eval("Type")) %> &middot; <%# Eval("CreatedDate", "{0:dd MMM yyyy HH:mm}") %></p>
                    </div>
                </ItemTemplate>
            </asp:Repeater>

            <asp:Panel ID="pnlNoNotifs" runat="server" Visible="false" CssClass="text-center py-stack-md">
                <span class="material-symbols-outlined ms-32 text-outline">notifications_off</span>
                <p class="font-body-md text-label-md text-on-surface-variant mt-base">No notifications yet.</p>
            </asp:Panel>
        </aside>
    </div>

    <!-- Outstanding balance callout, shown only when something is actually owed. -->
    <asp:Panel ID="pnlBalanceDue" runat="server" Visible="false"
        CssClass="bg-primary text-on-primary rounded-lg p-stack-md relative overflow-hidden shadow-[0_20px_40px_-15px_rgba(0,32,104,0.4)]">
        <div class="relative z-10 flex flex-col md:flex-row md:items-center justify-between gap-gutter">
            <div>
                <h3 class="font-headline-md text-headline-md mb-base">Payment Due</h3>
                <p class="font-body-md text-body-md text-primary-fixed-dim">
                    You have <strong>R <asp:Literal ID="litBalanceDue" runat="server" /></strong> outstanding across
                    <asp:Literal ID="litUnpaidCount" runat="server" /> unpaid invoice(s).</p>
            </div>
            <a href="<%= ResolveUrl("~/Customer/Invoices.aspx") %>"
               class="bg-secondary text-on-secondary font-label-md text-label-md py-3 px-6 rounded-DEFAULT hover:bg-secondary-container transition-colors shadow-sm shrink-0 text-center">
                View Invoices
            </a>
        </div>
        <div class="absolute top-0 right-0 -mr-8 -mt-8 w-32 h-32 bg-primary-container rounded-full opacity-50 blur-2xl"></div>
    </asp:Panel>
</asp:Content>
