<%@ Page Title="Driver Manifest" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="Dashboard.aspx.cs" Inherits="ForteLogisticsApp.DriverPortal.Dashboard" %>

<asp:Content ID="c1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="max-w-6xl mx-auto px-4 py-8">
        <div class="flex flex-col md:flex-row md:items-center md:justify-between mb-6 gap-3">
            <div>
                <p class="text-xs text-on-surface-variant"><span class="material-symbols-outlined mr-1">home</span> Home / <span class="text-brandRed font-semibold">My Manifest</span></p>
                <h1 class="text-2xl font-black text-brandDark">Delivery Manifest</h1>
                <p class="text-on-surface-variant text-sm">Parcels assigned to you for delivery today.</p>
            </div>
            <a href='<%= ResolveUrl("~/Driver/DeliveryConfirmation.aspx") %>' class="fcl-btn inline-flex items-center gap-2"><span class="material-symbols-outlined">fact_check</span> Confirm a Delivery</a>
        </div>

        <div class="grid grid-cols-3 gap-4 mb-8">
            <div class="fcl-card rounded-xl p-4"><div class="text-xs font-bold uppercase text-on-surface-variant">Assigned</div><div class="text-3xl font-black text-brandDark"><asp:Literal ID="litAssigned" runat="server">0</asp:Literal></div></div>
            <div class="fcl-card rounded-xl p-4"><div class="text-xs font-bold uppercase text-on-surface-variant">Delivered</div><div class="text-3xl font-black text-emerald-700"><asp:Literal ID="litDelivered" runat="server">0</asp:Literal></div></div>
            <div class="fcl-card rounded-xl p-4"><div class="text-xs font-bold uppercase text-on-surface-variant">Failed</div><div class="text-3xl font-black text-brandRed"><asp:Literal ID="litFailed" runat="server">0</asp:Literal></div></div>
        </div>

        <div class="fcl-card rounded-xl p-6 overflow-x-auto">
            <div class="flex flex-wrap items-center justify-between gap-2 mb-4">
                <h2 class="font-bold text-lg text-brandDark">My Parcels (collect &amp; deliver)</h2>
                <%-- Collects the whole manifest in one action instead of one click per parcel. --%>
                <asp:Button ID="btnCollectAll" runat="server" Text="Collect All Assigned"
                    OnClick="btnCollectAll_Click" CssClass="fcl-btn text-sm" />
            </div>
            <asp:Label ID="lblMsg" runat="server" CssClass="block text-sm mb-3"></asp:Label>
            <asp:Repeater ID="rptManifest" runat="server" OnItemCommand="rptManifest_ItemCommand">
                <HeaderTemplate>
                    <table class="w-full text-sm text-left">
                        <thead><tr class="text-xs uppercase text-on-surface-variant border-b">
                            <th class="py-2 pr-3">Waybill</th><th class="py-2 pr-3">Recipient</th>
                            <th class="py-2 pr-3">Delivery Address</th><th class="py-2 pr-3">Phone</th>
                            <th class="py-2 pr-3">Status</th><th class="py-2 pr-3">Action</th>
                        </tr></thead><tbody>
                </HeaderTemplate>
                <ItemTemplate>
                    <tr class="border-b border-outline-variant">
                        <td class="py-2 pr-3 font-semibold text-brandDark"><%# UiHelper.Enc(Eval("TrackingNumber")) %></td>
                        <td class="py-2 pr-3"><%# UiHelper.Enc(Eval("ReceiverName")) %></td>
                        <td class="py-2 pr-3 text-on-surface-variant"><%# UiHelper.Enc(Eval("DeliveryAddress")) %></td>
                        <td class="py-2 pr-3 text-on-surface-variant"><%# UiHelper.Enc(Eval("ReceiverPhone")) %></td>
                        <td class="py-2 pr-3"><%# ForteLogisticsApp.Services.UiHelper.StatusBadge(Eval("DeliveryStatus").ToString()) %></td>
                        <td class="py-2 pr-3">
                            <asp:LinkButton runat="server" CommandName="pickup" CommandArgument='<%# Eval("ShipmentID") %>' Visible='<%# Eval("DeliveryStatus").ToString() == "Assigned" %>' CssClass="text-brandRed font-bold hover:underline">Confirm Pickup</asp:LinkButton>
                            <a href='<%# ResolveUrl("~/Driver/DeliveryConfirmation.aspx?wb=" + Server.UrlEncode(Convert.ToString(Eval("TrackingNumber")))) %>' class="text-brandRed font-bold hover:underline" style='<%# Eval("DeliveryStatus").ToString() == "OutForDelivery" ? "" : "display:none" %>'>Confirm Delivery</a>
                        </td>
                    </tr>
                </ItemTemplate>
                <FooterTemplate></tbody></table></FooterTemplate>
            </asp:Repeater>
            <asp:Panel ID="pnlEmpty" runat="server" Visible="false" CssClass="text-center text-on-surface-variant text-sm py-8">
                <span class="material-symbols-outlined ms-32 mb-2">local_shipping</span>
                <p>No parcels are currently assigned to you.</p>
            </asp:Panel>
        </div>
    </div>
</asp:Content>
