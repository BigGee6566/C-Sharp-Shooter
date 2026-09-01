<%@ Page Title="Finance Dashboard" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="Dashboard.aspx.cs" Inherits="ForteLogisticsApp.Finance.Dashboard" %>

<asp:Content ID="c1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="max-w-7xl mx-auto px-4 py-8">
        <div class="flex flex-col md:flex-row md:items-center md:justify-between mb-6 gap-3">
            <div>
                <p class="text-xs text-gray-500"><span class="material-symbols-outlined mr-1">home</span> Home / <span class="text-brandRed font-semibold">Finance Dashboard</span></p>
                <h1 class="text-2xl font-black text-brandDark">Billing & Finance</h1>
                <p class="text-gray-600 text-sm">Generate invoices, record payments and monitor revenue.</p>
            </div>
            <a href='<%= ResolveUrl("~/Finance/Invoices.aspx") %>' class="fcl-btn inline-flex items-center gap-2"><span class="material-symbols-outlined">receipt_long</span> Manage Invoices</a>
        </div>

        <div class="grid grid-cols-2 md:grid-cols-4 gap-4 mb-8">
            <div class="fcl-card rounded-xl p-4"><div class="text-xs font-bold uppercase text-gray-500">Total Invoiced</div><div class="text-2xl font-black text-brandDark">R <asp:Literal ID="litInvoiced" runat="server">0</asp:Literal></div></div>
            <div class="fcl-card rounded-xl p-4"><div class="text-xs font-bold uppercase text-gray-500">Paid</div><div class="text-2xl font-black text-green-700">R <asp:Literal ID="litPaid" runat="server">0</asp:Literal></div></div>
            <div class="fcl-card rounded-xl p-4"><div class="text-xs font-bold uppercase text-gray-500">Outstanding</div><div class="text-2xl font-black text-brandRed">R <asp:Literal ID="litOutstanding" runat="server">0</asp:Literal></div></div>
            <div class="fcl-card rounded-xl p-4"><div class="text-xs font-bold uppercase text-gray-500">Uninvoiced Deliveries</div><div class="text-2xl font-black text-amber-600"><asp:Literal ID="litUninvoiced" runat="server">0</asp:Literal></div></div>
        </div>

        <div class="fcl-card rounded-xl p-6 overflow-x-auto">
            <h2 class="font-bold text-lg text-brandDark mb-4">Recent Invoices</h2>
            <asp:Repeater ID="rptInvoices" runat="server">
                <HeaderTemplate>
                    <table class="w-full text-sm text-left">
                        <thead><tr class="text-xs uppercase text-gray-500 border-b">
                            <th class="py-2 pr-3">Invoice #</th><th class="py-2 pr-3">Client</th>
                            <th class="py-2 pr-3">Amount</th><th class="py-2 pr-3">Status</th><th class="py-2 pr-3">Issued</th>
                        </tr></thead><tbody>
                </HeaderTemplate>
                <ItemTemplate>
                    <tr class="border-b border-gray-100">
                        <td class="py-2 pr-3 font-semibold text-brandDark"><%# UiHelper.Enc(Eval("InvoiceNumber")) %></td>
                        <td class="py-2 pr-3"><%# UiHelper.Enc(Eval("ClientName")) %></td>
                        <td class="py-2 pr-3">R <%# Eval("Amount", "{0:N2}") %></td>
                        <td class="py-2 pr-3"><%# ForteLogisticsApp.Services.UiHelper.StatusBadge(Eval("PaymentStatus").ToString()) %></td>
                        <td class="py-2 pr-3 text-gray-500"><%# Eval("IssuedDate", "{0:dd MMM yyyy}") %></td>
                    </tr>
                </ItemTemplate>
                <FooterTemplate></tbody></table></FooterTemplate>
            </asp:Repeater>
            <asp:Panel ID="pnlNone" runat="server" Visible="false" CssClass="text-center text-gray-500 text-sm py-8">No invoices yet.</asp:Panel>
        </div>
    </div>
</asp:Content>
