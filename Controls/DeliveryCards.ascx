<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="DeliveryCards.ascx.cs" Inherits="ForteLogisticsApp.Controls.DeliveryCards" %>
<%@ Import Namespace="ForteLogisticsApp.Services" %>

<asp:Repeater ID="rptCards" runat="server">
    <ItemTemplate>
        <a href='<%# TrackUrl(Eval("TrackingNumber")) %>'
           class="block p-stack-md border-b border-outline-variant last:border-0 hover:bg-surface-container-low transition-colors">
            <div class="flex flex-wrap items-center justify-between gap-base mb-1">
                <span class="font-headline-md text-body-lg font-semibold text-on-background break-all"><%# UiHelper.Enc(Eval("TrackingNumber")) %></span>
                <%# Badge(Eval("DeliveryStatus")) %>
            </div>
            <p class="font-body-md text-label-md font-normal text-on-surface-variant">
                <%# UiHelper.Enc(Eval("OriginCity")) %> &rarr; <%# UiHelper.Enc(Eval("DestCity")) %>
            </p>
            <p class="font-body-md text-label-md font-normal text-on-surface-variant mt-1 flex items-center gap-base">
                <span class="material-symbols-outlined text-primary" aria-hidden="true">local_shipping</span>
                <span><%# Note(Container.DataItem) %></span>
            </p>
        </a>
    </ItemTemplate>
</asp:Repeater>

<asp:Panel ID="pnlEmpty" runat="server" Visible="false" CssClass="p-stack-md text-center">
    <span class="material-symbols-outlined ms-32 text-outline" aria-hidden="true">inbox</span>
    <p class="font-body-md text-body-md text-on-surface-variant mt-base"><asp:Literal ID="litEmpty" runat="server" /></p>
    <a href='<%= ResolveUrl("~/Customer/BookShipment.aspx") %>' class="fcl-btn mt-stack-sm">Book a delivery</a>
</asp:Panel>
