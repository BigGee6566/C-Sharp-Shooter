<%@ Page Title="Confirm Delivery" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="DeliveryConfirmation.aspx.cs" Inherits="ForteLogisticsApp.DriverPortal.DeliveryConfirmation" %>

<asp:Content ID="c1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="max-w-2xl mx-auto px-4 py-8">
        <p class="text-xs text-on-surface-variant mb-1"><span class="material-symbols-outlined mr-1">home</span> Home / <a href='<%= ResolveUrl("~/Driver/Dashboard.aspx") %>' class="hover:text-brandRed">Manifest</a> / <span class="text-brandRed font-semibold">Confirm Delivery</span></p>
        <h1 class="text-2xl font-black text-brandDark mb-1">Delivery Confirmation</h1>
        <p class="text-on-surface-variant text-sm mb-6">Scan or enter the waybill, then record proof of delivery.</p>

        <div class="fcl-card rounded-xl p-6">
            <label class="block text-xs font-bold text-on-surface uppercase mb-1">Waybill Number</label>
            <%-- Scanning the waybill loads the parcel on its own - no separate Load click. --%>
            <asp:Panel ID="pnlScanBox" runat="server" DefaultButton="btnScan" CssClass="flex gap-2">
                <asp:TextBox ID="txtScan" runat="server" CssClass="fcl-input" placeholder="FCL-00000000"
                    AutoPostBack="True" OnTextChanged="btnScan_Click"></asp:TextBox>
                <asp:Button ID="btnScan" runat="server" Text="Load" OnClick="btnScan_Click" CssClass="fcl-btn" />
            </asp:Panel>
            <asp:Label ID="lblScanMsg" runat="server" CssClass="block text-xs mt-2"></asp:Label>

            <asp:Panel ID="pnlConfirm" runat="server" Visible="false" CssClass="mt-5 border-t border-outline-variant pt-4 space-y-3">
                <div class="bg-surface-container-low rounded-lg p-3 text-sm">
                    <div class="font-bold text-brandDark"><asp:Literal ID="litParcel" runat="server"></asp:Literal></div>
                    <div class="text-on-surface-variant"><asp:Literal ID="litDetails" runat="server"></asp:Literal></div>
                </div>
                <div>
                    <label class="block text-xs font-bold text-on-surface uppercase mb-1">Outcome</label>
                    <asp:DropDownList ID="ddlOutcome" runat="server" CssClass="fcl-input">
                        <asp:ListItem Value="Delivered">Delivered</asp:ListItem>
                        <asp:ListItem Value="Failed">Failed / Unable to deliver</asp:ListItem>
                    </asp:DropDownList>
                </div>
                <div>
                    <label class="block text-xs font-bold text-on-surface uppercase mb-1">Received By (name)</label>
                    <asp:TextBox ID="txtReceivedBy" runat="server" CssClass="fcl-input"></asp:TextBox>
                </div>
                <div>
                    <label class="block text-xs font-bold text-on-surface uppercase mb-1">Proof of Delivery / Notes</label>
                    <asp:TextBox ID="txtNotes" runat="server" CssClass="fcl-input" TextMode="MultiLine" Rows="3" placeholder="e.g. Left with reception, signed by J. Smith"></asp:TextBox>
                </div>
                <asp:HiddenField ID="hfShipmentId" runat="server" />
                <asp:Button ID="btnConfirm" runat="server" Text="Submit Confirmation" OnClick="btnConfirm_Click" CssClass="fcl-btn w-full" />
            </asp:Panel>

            <asp:Panel ID="pnlDone" runat="server" Visible="false" CssClass="mt-5 p-4 rounded-lg bg-emerald-50 border border-emerald-200 text-center">
                <p class="text-emerald-800 font-bold"><span class="material-symbols-outlined mr-1">check_circle</span> <asp:Literal ID="litDone" runat="server"></asp:Literal></p>
                <a href='<%= ResolveUrl("~/Driver/Dashboard.aspx") %>' class="text-brandRed font-bold text-sm hover:underline">Back to manifest &rarr;</a>
            </asp:Panel>
        </div>
    </div>
</asp:Content>
