<%@ Page Title="Business Performance Report" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="Performance.aspx.cs" Inherits="ForteLogisticsApp.Reports.Performance" %>
<%@ Register Src="~/Controls/ReportHeader.ascx" TagPrefix="lc" TagName="ReportHeader" %>
<%@ Register Src="~/Controls/ReportFooter.ascx" TagPrefix="lc" TagName="ReportFooter" %>

<asp:Content ID="c1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="lc-report max-w-7xl mx-auto">
        <p class="text-xs text-on-surface-variant mb-3 no-print">
            <span class="material-symbols-outlined mr-1" aria-hidden="true">home</span>
            Home / Reports / <span class="text-brandRed font-semibold">Business Performance</span>
        </p>

        <lc:ReportHeader ID="rptHeader" runat="server"
            ReportTitle="Business Performance Report"
            ReportType="Performance report"
            Description="Shipments, deliveries, revenue and customers for the chosen period, each compared with the previous period of the same length." />

        <div class="lc-filterbar no-print" role="search" aria-label="Choose the report period">
            <div class="lc-field">
                <label for="<%= ddlPreset.ClientID %>">Quick range</label>
                <asp:DropDownList ID="ddlPreset" runat="server" CssClass="fcl-input" />
            </div>
            <div class="lc-field">
                <label for="<%= txtFrom.ClientID %>">From</label>
                <asp:TextBox ID="txtFrom" runat="server" TextMode="Date" CssClass="fcl-input" />
            </div>
            <div class="lc-field">
                <label for="<%= txtTo.ClientID %>">To</label>
                <asp:TextBox ID="txtTo" runat="server" TextMode="Date" CssClass="fcl-input" />
            </div>
            <div class="lc-field">
                <label for="<%= ddlGroup.ClientID %>">Group by</label>
                <asp:DropDownList ID="ddlGroup" runat="server" CssClass="fcl-input" />
            </div>
            <div class="lc-actions">
                <asp:Button ID="btnApply" runat="server" Text="Apply" OnClick="btnApply_Click" CssClass="fcl-btn" />
                <asp:Button ID="btnExport" runat="server" Text="Export CSV" OnClick="btnExport_Click" CssClass="fcl-btn fcl-btn-dark" />
                <button type="button" onclick="window.print()" class="fcl-btn fcl-btn-dark">
                    <span class="material-symbols-outlined" aria-hidden="true">print</span>Print
                </button>
            </div>
            <asp:Label ID="lblError" runat="server" CssClass="lc-filter-error" role="alert" EnableViewState="false" />
        </div>

        <asp:Literal ID="litBody" runat="server" EnableViewState="false" />

        <lc:ReportFooter ID="rptFooter" runat="server"
            Note="Failed delivery attempts include parcels that were re-queued for another try. Revenue collected is the part of this period's invoices that has since been paid." />
    </div>

    <script>
        // Editing a date means the user wants those dates, not the quick range.
        (function () {
            var preset = document.getElementById('<%= ddlPreset.ClientID %>');
            ['<%= txtFrom.ClientID %>', '<%= txtTo.ClientID %>'].forEach(function (id) {
                var el = document.getElementById(id);
                if (el && preset) el.addEventListener('change', function () { preset.value = 'custom'; });
            });
        })();
    </script>
</asp:Content>
