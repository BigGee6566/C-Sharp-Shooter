<%@ Page Title="Pay for Shipment" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="Payment.aspx.cs" Inherits="ForteLogisticsApp.CustomerPortal.Payment" %>

<asp:Content ID="c1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="max-w-2xl mx-auto px-4 py-8">
        <p class="text-xs text-on-surface-variant mb-1"><span class="material-symbols-outlined mr-1">home</span> Home / <a href='<%= ResolveUrl("~/Customer/Dashboard.aspx") %>' class="hover:text-brandRed">Dashboard</a> / <span class="text-brandRed font-semibold">Payment</span></p>
        <h1 class="text-2xl font-black text-brandDark mb-1">Payment required</h1>
        <p class="text-on-surface-variant text-sm mb-6">Your parcel will not be dispatched until this invoice is paid.</p>

        <asp:HiddenField ID="hfIdempotencyKey" runat="server" />
        <asp:HiddenField ID="hfShipmentId" runat="server" />

        <asp:Panel ID="pnlNotEligible" runat="server" Visible="false" CssClass="fcl-card rounded-xl p-6">
            <p class="text-sm text-on-surface-variant"><asp:Literal ID="litNotEligibleMsg" runat="server" /></p>
            <a href='<%= ResolveUrl("~/Customer/Invoices.aspx") %>' class="inline-block mt-4 text-brandRed font-bold hover:underline">Go to My Invoices</a>
        </asp:Panel>

        <asp:Panel ID="pnlPay" runat="server" CssClass="fcl-card rounded-xl p-6">
            <div class="rounded-lg bg-surface-container p-4 mb-5 text-sm space-y-1">
                <div class="flex justify-between"><span class="text-on-surface-variant">Waybill</span><span class="font-semibold"><asp:Literal ID="litTracking" runat="server" /></span></div>
                <div class="flex justify-between"><span class="text-on-surface-variant">Invoice</span><span class="font-semibold"><asp:Literal ID="litInvoiceNumber" runat="server" /></span></div>
                <div class="flex justify-between"><span class="text-on-surface-variant">Route</span><span><asp:Literal ID="litRoute" runat="server" /></span></div>
                <div class="flex justify-between text-base pt-2 border-t border-outline-variant mt-2"><span class="font-bold">Total payable</span><span class="font-black text-brandDark"><asp:Literal ID="litAmount" runat="server" /></span></div>
            </div>

            <asp:Label ID="lblStatus" runat="server" CssClass="block text-sm mb-4"></asp:Label>

            <div class="mb-4">
                <label class="block text-label-sm font-semibold text-on-surface-variant mb-2">Payment method (sandbox)</label>
                <asp:DropDownList ID="ddlMethod" runat="server" CssClass="fcl-input w-full">
                    <asp:ListItem Text="Card" Value="Card" />
                    <asp:ListItem Text="EFT" Value="EFT" />
                    <asp:ListItem Text="Wallet" Value="Wallet" />
                </asp:DropDownList>
            </div>

            <div class="mb-1 rounded-md bg-amber-50 text-amber-900 text-xs px-3 py-2">
                Sandbox / test payment only - no card details are collected or stored, and no real money moves.
            </div>

            <asp:Button ID="btnPay" runat="server" Text="Pay now (sandbox)" OnClick="btnPay_Click" CssClass="fcl-btn fcl-btn-primary w-full mt-4" />
        </asp:Panel>

        <asp:Panel ID="pnlReceipt" runat="server" Visible="false" CssClass="fcl-card rounded-xl p-6">
            <div class="text-center mb-4">
                <span class="material-symbols-outlined text-emerald-600" style="font-size:48px;">check_circle</span>
                <h2 class="text-xl font-black text-brandDark mt-2">Payment successful</h2>
                <p class="text-on-surface-variant text-sm">Your shipment is confirmed and ready for processing.</p>
            </div>
            <div class="rounded-lg bg-surface-container p-4 text-sm space-y-1">
                <div class="flex justify-between"><span class="text-on-surface-variant">Waybill</span><span class="font-semibold"><asp:Literal ID="litReceiptTracking" runat="server" /></span></div>
                <div class="flex justify-between"><span class="text-on-surface-variant">Invoice</span><span class="font-semibold"><asp:Literal ID="litReceiptInvoice" runat="server" /></span></div>
                <div class="flex justify-between"><span class="text-on-surface-variant">Reference</span><span class="font-semibold"><asp:Literal ID="litReceiptReference" runat="server" /></span></div>
                <div class="flex justify-between"><span class="text-on-surface-variant">Amount paid</span><span class="font-semibold"><asp:Literal ID="litReceiptAmount" runat="server" /></span></div>
            </div>
            <a href='<%= ResolveUrl("~/Customer/Track.aspx") %>' class="inline-block mt-5 text-brandRed font-bold hover:underline">Track your parcel</a>
        </asp:Panel>
    </div>
</asp:Content>
