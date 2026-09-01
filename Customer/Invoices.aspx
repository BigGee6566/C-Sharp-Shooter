<%@ Page Title="My Invoices" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="Invoices.aspx.cs" Inherits="ForteLogisticsApp.CustomerPortal.Invoices" %>

<asp:Content ID="c1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="max-w-6xl mx-auto px-4 py-8">
        <p class="text-xs text-on-surface-variant mb-1"><span class="material-symbols-outlined mr-1">home</span> Home / <a href='<%= ResolveUrl("~/Customer/Dashboard.aspx") %>' class="hover:text-brandRed">Dashboard</a> / <span class="text-brandRed font-semibold">My Invoices</span></p>
        <h1 class="text-2xl font-black text-brandDark mb-1">My Invoices</h1>
        <p class="text-on-surface-variant text-sm mb-6">View and print invoices for your shipments.</p>

        <div class="fcl-card rounded-xl p-6 overflow-x-auto">
            <asp:Label ID="lblMsg" runat="server" CssClass="block text-sm mb-3"></asp:Label>
            <asp:Repeater ID="rptInvoices" runat="server" OnItemCommand="rptInvoices_ItemCommand">
                <HeaderTemplate>
                    <table class="w-full text-sm text-left">
                        <thead>
                            <tr class="text-xs uppercase text-on-surface-variant border-b">
                                <th class="py-2 pr-3">Invoice #</th>
                                <th class="py-2 pr-3">Waybill</th>
                                <th class="py-2 pr-3">Amount</th>
                                <th class="py-2 pr-3">Status</th>
                                <th class="py-2 pr-3">Issued</th>
                                <th class="py-2 pr-3"></th>
                            </tr>
                        </thead>
                        <tbody>
                </HeaderTemplate>
                <ItemTemplate>
                    <tr class="border-b border-outline-variant">
                        <td class="py-2 pr-3 font-semibold text-brandDark"><%# UiHelper.Enc(Eval("InvoiceNumber")) %></td>
                        <td class="py-2 pr-3"><%# UiHelper.Enc(Eval("TrackingNumber")) %></td>
                        <td class="py-2 pr-3">R <%# Eval("Amount", "{0:N2}") %></td>
                        <td class="py-2 pr-3"><%# ForteLogisticsApp.Services.UiHelper.StatusBadge(Eval("PaymentStatus").ToString()) %></td>
                        <td class="py-2 pr-3 text-on-surface-variant"><%# Eval("IssuedDate", "{0:dd MMM yyyy}") %></td>
                        <td class="py-2 pr-3 space-x-3">
                            <a href='<%# ResolveUrl("~/Invoice.aspx?id=" + Eval("PaymentID")) %>' target="_blank" class="text-on-surface-variant hover:underline">View / Print</a>
                            <asp:LinkButton runat="server" CommandName="pay" CommandArgument='<%# Eval("PaymentID") %>' Visible='<%# Eval("PaymentStatus").ToString() == "Unpaid" %>' CssClass="text-brandRed font-bold hover:underline">Pay Now</asp:LinkButton>
                        </td>
                    </tr>
                </ItemTemplate>
                <FooterTemplate></tbody></table></FooterTemplate>
            </asp:Repeater>
            <asp:Panel ID="pnlNone" runat="server" Visible="false" CssClass="text-center text-on-surface-variant text-sm py-8">
                <span class="material-symbols-outlined ms-32 mb-2">description</span>
                <p>You have no invoices yet.</p>
            </asp:Panel>
        </div>
    </div>
</asp:Content>
