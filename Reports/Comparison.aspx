<%@ Page Title="Comparison Report" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="Comparison.aspx.cs" Inherits="ForteLogisticsApp.Reports.Comparison" %>
<%@ Register Src="~/Controls/ReportHeader.ascx" TagPrefix="lc" TagName="ReportHeader" %>
<%@ Register Src="~/Controls/ReportFooter.ascx" TagPrefix="lc" TagName="ReportFooter" %>

<asp:Content ID="c1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="lc-report max-w-7xl mx-auto">
        <nav aria-label="Breadcrumb" class="lc-breadcrumb no-print">
            <ol>
                <li><a class="fcl-link" href='<%= ResolveUrl("~/Admin/Dashboard.aspx") %>'>Home</a></li>
                <li>Reports</li>
                <li><span aria-current="page">Comparison</span></li>
            </ol>
        </nav>

        <asp:Panel ID="pnlError" runat="server" Visible="false" CssClass="lc-error-summary no-print" role="alert" tabindex="-1">
            <span class="material-symbols-outlined" aria-hidden="true">error</span>
            <span><asp:Literal ID="litError" runat="server" EnableViewState="false" /></span>
        </asp:Panel>

        <lc:ReportHeader ID="rptHeader" runat="server" ReportTitle="Comparison Report" ReportType="Comparison report" />

        <nav class="lc-tabs no-print" aria-label="Comparison type">
            <asp:Literal ID="litTabs" runat="server" EnableViewState="false" />
        </nav>

        <asp:Panel ID="pnlPeriods" runat="server" DefaultButton="btnApplyPeriods" CssClass="lc-filterbar no-print"
            role="group" aria-label="Periods to compare">
            <div class="lc-field lc-field-wide">
                <label for="<%= ddlPair.ClientID %>">Quick comparison</label>
                <asp:DropDownList ID="ddlPair" runat="server" CssClass="fcl-input" aria-describedby="pairHint" />
                <span id="pairHint" class="lc-hint">Or edit any date to compare custom periods.</span>
            </div>
            <fieldset class="lc-fieldset">
                <legend>Period A (baseline)</legend>
                <div class="lc-field">
                    <label for="<%= txtAFrom.ClientID %>">From</label>
                    <asp:TextBox ID="txtAFrom" runat="server" TextMode="Date" CssClass="fcl-input lc-period-date" />
                </div>
                <div class="lc-field">
                    <label for="<%= txtATo.ClientID %>">To</label>
                    <asp:TextBox ID="txtATo" runat="server" TextMode="Date" CssClass="fcl-input lc-period-date" />
                </div>
            </fieldset>
            <fieldset class="lc-fieldset">
                <legend>Period B</legend>
                <div class="lc-field">
                    <label for="<%= txtBFrom.ClientID %>">From</label>
                    <asp:TextBox ID="txtBFrom" runat="server" TextMode="Date" CssClass="fcl-input lc-period-date" />
                </div>
                <div class="lc-field">
                    <label for="<%= txtBTo.ClientID %>">To</label>
                    <asp:TextBox ID="txtBTo" runat="server" TextMode="Date" CssClass="fcl-input lc-period-date" />
                </div>
            </fieldset>
            <div class="lc-actions">
                <asp:Button ID="btnApplyPeriods" runat="server" Text="Compare" OnClick="btnApplyPeriods_Click" CssClass="fcl-btn" />
                <asp:Button ID="btnExportPeriods" runat="server" Text="Export (CSV)" OnClick="btnExportPeriods_Click" CssClass="fcl-btn fcl-btn-dark" />
                <button type="button" onclick="window.print()" class="fcl-btn fcl-btn-dark">
                    <span class="material-symbols-outlined" aria-hidden="true">print</span>Print
                </button>
            </div>
            <asp:HiddenField ID="hfShownPeriods" runat="server" />
        </asp:Panel>

        <asp:Panel ID="pnlGroups" runat="server" DefaultButton="btnApplyGroups" CssClass="lc-filterbar no-print"
            role="group" aria-label="Period and grouping">
            <div class="lc-field">
                <label for="<%= ddlGroupBy.ClientID %>">Compare by</label>
                <asp:DropDownList ID="ddlGroupBy" runat="server" CssClass="fcl-input" />
            </div>
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
            <div class="lc-actions">
                <asp:Button ID="btnApplyGroups" runat="server" Text="Compare" OnClick="btnApplyGroups_Click" CssClass="fcl-btn" />
                <asp:Button ID="btnExportGroups" runat="server" Text="Export (CSV)" OnClick="btnExportGroups_Click" CssClass="fcl-btn fcl-btn-dark" />
                <button type="button" onclick="window.print()" class="fcl-btn fcl-btn-dark">
                    <span class="material-symbols-outlined" aria-hidden="true">print</span>Print
                </button>
            </div>
            <asp:HiddenField ID="hfShownFrom" runat="server" />
            <asp:HiddenField ID="hfShownTo" runat="server" />
        </asp:Panel>

        <asp:Literal ID="litBody" runat="server" EnableViewState="false" />

        <lc:ReportFooter ID="rptFooter" runat="server"
            Note="Changes are Period B measured against Period A. Rates are compared in percentage points. Failed delivery attempts include parcels re-queued for another try." />
    </div>

    <script>
        (function () {
            var err = document.getElementById('<%= pnlError.ClientID %>');
            if (err) err.focus();

            // Editing a date means custom periods, so the quick-comparison label never contradicts the dates.
            var pair = document.getElementById('<%= ddlPair.ClientID %>');
            if (pair) {
                var dates = document.querySelectorAll('.lc-period-date');
                for (var i = 0; i < dates.length; i++) {
                    dates[i].addEventListener('change', function () { pair.value = 'custom'; });
                }
            }
        })();
    </script>
</asp:Content>
