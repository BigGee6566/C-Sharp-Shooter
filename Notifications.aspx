<%@ Page Title="Notifications" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="Notifications.aspx.cs" Inherits="ForteLogisticsApp.NotificationsPage" %>

<asp:Content ID="c1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="max-w-3xl mx-auto px-4 py-8">
        <p class="text-xs text-on-surface-variant mb-1"><span class="material-symbols-outlined mr-1">home</span> Home / <span class="text-brandRed font-semibold">Notifications</span></p>
        <h1 class="text-2xl font-black text-brandDark mb-1">Notifications</h1>
        <p class="text-on-surface-variant text-sm mb-6">Updates the system raised for you automatically as parcels move.</p>

        <div class="fcl-card rounded-xl p-6">
            <asp:Repeater ID="rptNotifications" runat="server">
                <HeaderTemplate><ul class="divide-y divide-outline-variant"></HeaderTemplate>
                <ItemTemplate>
                    <li class='py-3 <%# Convert.ToBoolean(Eval("IsRead")) ? "" : "bg-amber-50/60 -mx-2 px-2 rounded" %>'>
                        <div class="flex items-start justify-between gap-3">
                            <div>
                                <p class='text-sm <%# Convert.ToBoolean(Eval("IsRead")) ? "text-on-surface" : "text-brandDark font-bold" %>'>
                                    <%# Server.HtmlEncode(Eval("Message").ToString()) %>
                                </p>
                                <p class="text-xs text-on-surface-variant mt-1">
                                    <%# ForteLogisticsApp.Services.UiHelper.StatusBadge(Eval("Type") == null ? "System" : Eval("Type").ToString()) %>
                                    <span class="ml-2"><%# Convert.ToDateTime(Eval("CreatedDate")).ToString("dd MMM yyyy HH:mm") %></span>
                                </p>
                            </div>
                            <asp:HyperLink runat="server" CssClass="text-brandRed font-bold text-xs hover:underline whitespace-nowrap"
                                NavigateUrl='<%# TrackUrl(Eval("ShipmentID")) %>'
                                Visible='<%# Eval("ShipmentID") != System.DBNull.Value %>'>View parcel &rarr;</asp:HyperLink>
                        </div>
                    </li>
                </ItemTemplate>
                <FooterTemplate></ul></FooterTemplate>
            </asp:Repeater>

            <asp:Panel ID="pnlNone" runat="server" Visible="false" CssClass="text-center text-on-surface-variant text-sm py-10">
                <span class="material-symbols-outlined ms-32 mb-2">notifications_off</span>
                <p>You have no notifications yet.</p>
            </asp:Panel>
        </div>
    </div>
</asp:Content>
