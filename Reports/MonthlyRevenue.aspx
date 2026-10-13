<%@ Page Title="Monthly Revenue" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="MonthlyRevenue.aspx.cs" Inherits="ForteLogisticsApp.Reports.MonthlyRevenue" %>

<asp:Content ID="c1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="max-w-7xl mx-auto px-4 py-8">
        <div class="print-only mb-4" style="border-bottom:2px solid #00164e; padding-bottom:8px">
            <img src='<%= ResolveUrl("~/Images/web/logo.png") %>' alt="Logisticore" style="height:56px" />
            <div style="font-size:11px; color:#444">Logisticore Courier Service &middot; Isando, Gauteng &middot; 0860 LOGISTICORE &middot; support@logisticore.co.za</div>
            <div style="font-weight:800; font-size:16px; margin-top:4px; color:#00164e">Monthly Revenue Report</div>
            <div style="font-size:11px; color:#444">Generated: <%= DateTime.Now.ToString("dd MMM yyyy HH:mm") %></div>
        </div>

        <p class="text-xs text-on-surface-variant mb-1 no-print"><span class="material-symbols-outlined mr-1">home</span> Home / Reports / <span class="text-brandRed font-semibold">Monthly Revenue</span></p>
        <div class="flex flex-col md:flex-row md:items-end md:justify-between gap-4 mb-6">
            <div>
                <h1 class="text-2xl font-black text-brandDark">Monthly Revenue Report</h1>
                <p class="text-on-surface-variant text-sm">Invoiced revenue for the selected month.</p>
            </div>
            <div class="flex items-end gap-2 no-print">
                <div>
                    <label class="block text-[11px] font-bold text-on-surface-variant uppercase mb-1">Month</label>
                    <asp:TextBox ID="txtMonth" runat="server" TextMode="Month" CssClass="fcl-input"></asp:TextBox>
                </div>
                <asp:Button ID="btnApply" runat="server" Text="Apply" OnClick="btnApply_Click" CssClass="fcl-btn" />
                <asp:Button ID="btnExport" runat="server" Text="CSV" OnClick="btnExport_Click" CssClass="fcl-btn fcl-btn-dark" />
                <button type="button" onclick="window.print()" class="fcl-btn fcl-btn-dark"><span class="material-symbols-outlined">print</span></button>
            </div>
        </div>

        <div class="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
            <div class="fcl-card rounded-xl p-4"><div class="text-xs font-bold uppercase text-on-surface-variant">Invoices</div><div class="text-2xl font-black text-brandDark"><asp:Literal ID="litCount" runat="server">0</asp:Literal></div></div>
            <div class="fcl-card rounded-xl p-4"><div class="text-xs font-bold uppercase text-on-surface-variant">Total Invoiced</div><div class="text-2xl font-black text-purple-700">R <asp:Literal ID="litTotal" runat="server">0</asp:Literal></div></div>
            <div class="fcl-card rounded-xl p-4"><div class="text-xs font-bold uppercase text-on-surface-variant">Paid</div><div class="text-2xl font-black text-emerald-700">R <asp:Literal ID="litPaid" runat="server">0</asp:Literal></div></div>
            <div class="fcl-card rounded-xl p-4"><div class="text-xs font-bold uppercase text-on-surface-variant">Outstanding</div><div class="text-2xl font-black text-brandRed">R <asp:Literal ID="litOutstanding" runat="server">0</asp:Literal></div></div>
        </div>

        <div class="fcl-card rounded-xl p-6 overflow-x-auto">
            <asp:GridView ID="gv" runat="server" AutoGenerateColumns="true" CssClass="report-table" GridLines="None" UseAccessibleHeader="true"></asp:GridView>
            <asp:Panel ID="pnlEmpty" runat="server" Visible="false" CssClass="text-center text-on-surface-variant text-sm py-8">No invoices in this month.</asp:Panel>
        </div>
    </div>
</asp:Content>
