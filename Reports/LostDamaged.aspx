<%@ Page Title="Lost and Damaged Parcels Report" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="LostDamaged.aspx.cs" Inherits="ForteLogisticsApp.Reports.LostDamaged" %>
<%@ Register Src="~/Controls/ReportHeader.ascx" TagPrefix="lc" TagName="ReportHeader" %>
<%@ Register Src="~/Controls/ReportFooter.ascx" TagPrefix="lc" TagName="ReportFooter" %>

<asp:Content ID="c1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="lc-report max-w-7xl mx-auto">
        <nav aria-label="Breadcrumb" class="lc-breadcrumb no-print">
            <ol>
                <li><a class="fcl-link" href='<%= HomeUrl %>'>Home</a></li>
                <li>Reports</li>
                <li><span aria-current="page">Lost and Damaged</span></li>
            </ol>
        </nav>

        <asp:Panel ID="pnlError" runat="server" Visible="false" CssClass="lc-error-summary no-print" role="alert" tabindex="-1">
            <span class="material-symbols-outlined" aria-hidden="true">error</span>
            <span><asp:Literal ID="litError" runat="server" EnableViewState="false" /></span>
        </asp:Panel>

        <lc:ReportHeader ID="rptHeader" runat="server" ReportTitle="Lost and Damaged Parcels Report" ReportType="Exceptions report" />

        <asp:Panel ID="pnlFilters" runat="server" DefaultButton="btnApply" CssClass="lc-filterbar no-print"
            role="group" aria-label="Report period and actions">
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
                <asp:Button ID="btnExport" runat="server" Text="Export exceptions (CSV)" OnClick="btnExport_Click" CssClass="fcl-btn fcl-btn-dark" />
                <button type="button" onclick="window.print()" class="fcl-btn fcl-btn-dark">
                    <span class="material-symbols-outlined" aria-hidden="true">print</span>Print
                </button>
            </div>
            <asp:HiddenField ID="hfShownFrom" runat="server" />
            <asp:HiddenField ID="hfShownTo" runat="server" />
            <span id="lcPresetStatus" class="lc-sr-only" aria-live="polite"></span>
        </asp:Panel>

        <asp:Literal ID="litBody" runat="server" EnableViewState="false" />

        <lc:ReportFooter ID="rptFooter" runat="server"
            Note="Exceptions are counted on the booking date of the parcel, so the exception rate is lost or damaged parcels among the shipments booked in the period." />
    </div>

    <script>
        (function () {
            // Editing a date means the user wants those dates. The server works this out on its
            // own (so it also works without JavaScript); this only keeps the Quick range in step
            // and tells screen-reader users about the change.
            var preset = document.getElementById('<%= ddlPreset.ClientID %>');
            var status = document.getElementById('lcPresetStatus');
            ['<%= txtFrom.ClientID %>', '<%= txtTo.ClientID %>'].forEach(function (id) {
                var el = document.getElementById(id);
                if (!el || !preset) return;
                el.addEventListener('change', function () {
                    if (preset.value !== 'custom') {
                        preset.value = 'custom';
                        if (status) status.textContent = 'Quick range set to Custom dates.';
                    }
                });
            });

            // A period problem is reported at the top; move focus there so it is heard.
            var err = document.getElementById('<%= pnlError.ClientID %>');
            if (err) err.focus();
        })();
    </script>
</asp:Content>
