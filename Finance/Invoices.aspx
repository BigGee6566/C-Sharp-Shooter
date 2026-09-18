<%@ Page Title="Invoices & Billing" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="Invoices.aspx.cs" Inherits="ForteLogisticsApp.Finance.Invoices" %>

<asp:Content ID="c1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="max-w-7xl mx-auto px-4 py-8">
        <p class="text-xs text-on-surface-variant mb-1"><span class="material-symbols-outlined mr-1">home</span> Home / <a href='<%= ResolveUrl("~/Finance/Dashboard.aspx") %>' class="hover:text-brandRed">Finance</a> / <span class="text-brandRed font-semibold">Invoices</span></p>
        <h1 class="text-2xl font-black text-brandDark mb-1">Invoices & Billing</h1>
        <p class="text-on-surface-variant text-sm mb-6">Generate invoices for delivered shipments and record payments.</p>

        <asp:Label ID="lblMsg" runat="server" CssClass="block text-sm mb-4"></asp:Label>

        <!-- Uninvoiced -->
        <div class="fcl-card rounded-xl p-6 mb-6 overflow-x-auto">
            <div class="flex items-center justify-between mb-4">
                <h2 class="font-bold text-lg text-brandDark">Delivered - Awaiting Invoice</h2>
                <asp:Button ID="btnInvoiceAll" runat="server" Text="Invoice All" OnClick="btnInvoiceAll_Click" CssClass="fcl-btn fcl-btn-dark" />
            </div>
            <asp:Repeater ID="rptUninvoiced" runat="server" OnItemCommand="rptUninvoiced_ItemCommand">
                <HeaderTemplate>
                    <table class="w-full text-sm text-left">
                        <thead><tr class="text-xs uppercase text-on-surface-variant border-b">
                            <th class="py-2 pr-3">Waybill</th><th class="py-2 pr-3">Client</th>
                            <th class="py-2 pr-3">Route</th><th class="py-2 pr-3">Amount</th><th class="py-2 pr-3"></th>
                        </tr></thead><tbody>
                </HeaderTemplate>
                <ItemTemplate>
                    <tr class="border-b border-outline-variant">
                        <td class="py-2 pr-3 font-semibold text-brandDark"><%# UiHelper.Enc(Eval("TrackingNumber")) %></td>
                        <td class="py-2 pr-3"><%# UiHelper.Enc(Eval("ClientName")) %></td>
                        <td class="py-2 pr-3 text-on-surface-variant"><%# UiHelper.Enc(Eval("OriginCity")) %> &rarr; <%# UiHelper.Enc(Eval("DestCity")) %></td>
                        <td class="py-2 pr-3">R <%# Eval("Cost", "{0:N2}") %></td>
                        <td class="py-2 pr-3">
                            <asp:LinkButton runat="server" CommandName="gen" CommandArgument='<%# Eval("ShipmentID") %>' CssClass="text-brandRed font-bold hover:underline">Generate Invoice</asp:LinkButton>
                        </td>
                    </tr>
                </ItemTemplate>
                <FooterTemplate></tbody></table></FooterTemplate>
            </asp:Repeater>
            <asp:Panel ID="pnlNoUninvoiced" runat="server" Visible="false" CssClass="text-center text-on-surface-variant text-sm py-6">All delivered shipments have been invoiced.</asp:Panel>
        </div>

        <!-- Invoices -->
        <div class="fcl-card rounded-xl p-6 overflow-x-auto">
            <h2 class="font-bold text-lg text-brandDark mb-4">All Invoices</h2>
            <asp:Repeater ID="rptInvoices" runat="server" OnItemCommand="rptInvoices_ItemCommand">
                <HeaderTemplate>
                    <table class="w-full text-sm text-left">
                        <thead><tr class="text-xs uppercase text-on-surface-variant border-b">
                            <th class="py-2 pr-3">Invoice #</th><th class="py-2 pr-3">Client</th>
                            <th class="py-2 pr-3">Amount</th><th class="py-2 pr-3">Status</th>
                            <th class="py-2 pr-3">Issued</th><th class="py-2 pr-3"></th>
                        </tr></thead><tbody>
                </HeaderTemplate>
                <ItemTemplate>
                    <tr class="border-b border-outline-variant">
                        <td class="py-2 pr-3 font-semibold text-brandDark"><%# UiHelper.Enc(Eval("InvoiceNumber")) %></td>
                        <td class="py-2 pr-3"><%# UiHelper.Enc(Eval("ClientName")) %></td>
                        <td class="py-2 pr-3">R <%# Eval("Amount", "{0:N2}") %></td>
                        <td class="py-2 pr-3"><%# ForteLogisticsApp.Services.UiHelper.StatusBadge(Eval("PaymentStatus").ToString()) %></td>
                        <td class="py-2 pr-3 text-on-surface-variant"><%# Eval("IssuedDate", "{0:dd MMM yyyy}") %></td>
                        <td class="py-2 pr-3 space-x-3">
                            <a href='<%# ResolveUrl("~/Invoice.aspx?id=" + Eval("PaymentID")) %>' class="text-on-surface-variant hover:underline">Print</a>
                            <asp:LinkButton runat="server" CommandName="paid" CommandArgument='<%# Eval("PaymentID") %>' Visible='<%# Eval("PaymentStatus").ToString() == "Unpaid" %>' CssClass="text-emerald-700 font-bold hover:underline">Mark Paid</asp:LinkButton>
                        </td>
                    </tr>
                </ItemTemplate>
                <FooterTemplate></tbody></table></FooterTemplate>
            </asp:Repeater>
            <asp:Panel ID="pnlNoInvoices" runat="server" Visible="false" CssClass="text-center text-on-surface-variant text-sm py-6">No invoices generated yet.</asp:Panel>
        </div>
    </div>
</asp:Content>
