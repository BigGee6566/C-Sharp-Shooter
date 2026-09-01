<%@ Page Title="Lost / Damaged Parcels" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="LostDamaged.aspx.cs" Inherits="ForteLogisticsApp.Reports.LostDamaged" %>

<asp:Content ID="c1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="max-w-7xl mx-auto px-4 py-8">
        <div class="print-only mb-4" style="border-bottom:2px solid #00164e; padding-bottom:8px">
            <img src='<%= ResolveUrl("~/Images/logisticore-logo.jpg") %>' alt="Logisticore" style="height:56px" />
            <div style="font-size:11px; color:#444">Logisticore Courier Service &middot; Isando, Gauteng &middot; 0860 LOGISTICORE &middot; support@logisticore.co.za</div>
            <div style="font-weight:800; font-size:16px; margin-top:4px; color:#00164e">Lost / Damaged Parcel Exceptions</div>
            <div style="font-size:11px; color:#444">Generated: <%= DateTime.Now.ToString("dd MMM yyyy HH:mm") %></div>
        </div>

        <p class="text-xs text-gray-500 mb-1 no-print"><span class="material-symbols-outlined mr-1">home</span> Home / Reports / <span class="text-brandRed font-semibold">Lost / Damaged</span></p>
        <div class="flex flex-col md:flex-row md:items-center md:justify-between gap-4 mb-6">
            <div>
                <h1 class="text-2xl font-black text-brandDark">Lost / Damaged Parcel Exceptions</h1>
                <p class="text-gray-600 text-sm">Parcels flagged as lost or damaged that require attention.</p>
            </div>
            <div class="flex items-center gap-2 no-print">
                <asp:Button ID="btnExport" runat="server" Text="Export CSV" OnClick="btnExport_Click" CssClass="fcl-btn fcl-btn-dark" />
                <button type="button" onclick="window.print()" class="fcl-btn fcl-btn-dark"><span class="material-symbols-outlined">print</span></button>
            </div>
        </div>

        <div class="grid grid-cols-2 md:grid-cols-3 gap-4 mb-6">
            <div class="fcl-card rounded-xl p-4"><div class="text-xs font-bold uppercase text-gray-500">Damaged</div><div class="text-2xl font-black text-amber-600"><asp:Literal ID="litDamaged" runat="server">0</asp:Literal></div></div>
            <div class="fcl-card rounded-xl p-4"><div class="text-xs font-bold uppercase text-gray-500">Lost</div><div class="text-2xl font-black text-brandRed"><asp:Literal ID="litLost" runat="server">0</asp:Literal></div></div>
            <div class="fcl-card rounded-xl p-4"><div class="text-xs font-bold uppercase text-gray-500">Total Exceptions</div><div class="text-2xl font-black text-brandDark"><asp:Literal ID="litTotal" runat="server">0</asp:Literal></div></div>
        </div>

        <div class="fcl-card rounded-xl p-6 overflow-x-auto">
            <asp:GridView ID="gv" runat="server" AutoGenerateColumns="true" CssClass="report-table" GridLines="None" UseAccessibleHeader="true"></asp:GridView>
            <asp:Panel ID="pnlEmpty" runat="server" Visible="false" CssClass="text-center text-gray-500 text-sm py-8">
                <span class="material-symbols-outlined ms-24 text-green-600 mb-2">check_circle</span>
                <p>No lost or damaged parcels. Everything is on track.</p>
            </asp:Panel>
        </div>
    </div>
</asp:Content>
