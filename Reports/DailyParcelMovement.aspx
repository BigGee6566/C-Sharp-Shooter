<%@ Page Title="Daily Parcel Movement" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="DailyParcelMovement.aspx.cs" Inherits="ForteLogisticsApp.Reports.DailyParcelMovement" %>

<asp:Content ID="c1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="max-w-7xl mx-auto px-4 py-8">
        <!-- Print-only branded header -->
        <div class="print-only mb-4" style="border-bottom:2px solid #00164e; padding-bottom:8px">
            <img src='<%= ResolveUrl("~/Images/logisticore-logo.jpg") %>' alt="Logisticore" style="height:56px" />
            <div style="font-size:11px; color:#444">Logisticore Courier Service &middot; Isando, Gauteng &middot; 0860 LOGISTICORE &middot; support@logisticore.co.za</div>
            <div style="font-weight:800; font-size:16px; margin-top:4px; color:#00164e">Daily Parcel Movement Report</div>
            <div style="font-size:11px; color:#444">Generated: <%= DateTime.Now.ToString("dd MMM yyyy HH:mm") %></div>
        </div>

        <p class="text-xs text-gray-500 mb-1 no-print"><span class="material-symbols-outlined mr-1">home</span> Home / Reports / <span class="text-brandRed font-semibold">Daily Parcel Movement</span></p>
        <div class="flex flex-col md:flex-row md:items-end md:justify-between gap-4 mb-6">
            <div>
                <h1 class="text-2xl font-black text-brandDark">Daily Parcel Movement Report</h1>
                <p class="text-gray-600 text-sm">All parcel tracking events recorded within the selected period.</p>
            </div>
            <div class="flex items-end gap-2 no-print">
                <div>
                    <label class="block text-[11px] font-bold text-gray-600 uppercase mb-1">From</label>
                    <asp:TextBox ID="txtFrom" runat="server" TextMode="Date" CssClass="fcl-input"></asp:TextBox>
                </div>
                <div>
                    <label class="block text-[11px] font-bold text-gray-600 uppercase mb-1">To</label>
                    <asp:TextBox ID="txtTo" runat="server" TextMode="Date" CssClass="fcl-input"></asp:TextBox>
                </div>
                <asp:Button ID="btnApply" runat="server" Text="Apply" OnClick="btnApply_Click" CssClass="fcl-btn" />
                <asp:Button ID="btnExport" runat="server" Text="CSV" OnClick="btnExport_Click" CssClass="fcl-btn fcl-btn-dark" />
                <button type="button" onclick="window.print()" class="fcl-btn fcl-btn-dark"><span class="material-symbols-outlined">print</span></button>
            </div>
        </div>

        <div class="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
            <div class="fcl-card rounded-xl p-4"><div class="text-xs font-bold uppercase text-gray-500">Registered</div><div class="text-2xl font-black text-brandDark"><asp:Literal ID="litReg" runat="server">0</asp:Literal></div></div>
            <div class="fcl-card rounded-xl p-4"><div class="text-xs font-bold uppercase text-gray-500">Dispatched</div><div class="text-2xl font-black text-blue-700"><asp:Literal ID="litDisp" runat="server">0</asp:Literal></div></div>
            <div class="fcl-card rounded-xl p-4"><div class="text-xs font-bold uppercase text-gray-500">Delivered</div><div class="text-2xl font-black text-green-700"><asp:Literal ID="litDel" runat="server">0</asp:Literal></div></div>
            <div class="fcl-card rounded-xl p-4"><div class="text-xs font-bold uppercase text-gray-500">Total Events</div><div class="text-2xl font-black text-purple-700"><asp:Literal ID="litTotal" runat="server">0</asp:Literal></div></div>
        </div>

        <div class="fcl-card rounded-xl p-6 overflow-x-auto">
            <asp:GridView ID="gv" runat="server" AutoGenerateColumns="true" CssClass="report-table" GridLines="None" UseAccessibleHeader="true"></asp:GridView>
            <asp:Panel ID="pnlEmpty" runat="server" Visible="false" CssClass="text-center text-gray-500 text-sm py-8">No parcel movements in this period.</asp:Panel>
        </div>
    </div>
</asp:Content>
