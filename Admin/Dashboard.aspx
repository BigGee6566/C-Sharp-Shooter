<%@ Page Title="Admin Dashboard" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="Dashboard.aspx.cs" Inherits="ForteLogisticsApp.AdminPortal.Dashboard" %>

<asp:Content ID="c1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="max-w-7xl mx-auto px-4 py-8">
        <div class="mb-6">
            <p class="text-xs text-on-surface-variant"><span class="material-symbols-outlined mr-1">home</span> Home / <span class="text-brandRed font-semibold">Admin Dashboard</span></p>
            <h1 class="text-2xl font-black text-brandDark">System Administration</h1>
            <p class="text-on-surface-variant text-sm">Manage users, fleet and monitor overall system activity.</p>
        </div>

        <div class="lc-dashboard mb-8">
            <asp:Literal ID="litSummary" runat="server" EnableViewState="false" />
        </div>

        <h2 class="lc-section-title"><span class="material-symbols-outlined" aria-hidden="true">apps</span>Manage</h2>
        <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
            <a href='<%= ResolveUrl("~/Admin/Users.aspx") %>' class="fcl-card rounded-xl p-5 hover:shadow-lg transition block">
                <div class="text-brandRed text-2xl mb-2"><span class="material-symbols-outlined">manage_accounts</span></div>
                <div class="font-bold text-brandDark">Manage Users</div>
                <div class="text-xs text-on-surface-variant">Create staff & customer accounts</div>
            </a>
            <a href='<%= ResolveUrl("~/Admin/Fleet.aspx") %>' class="fcl-card rounded-xl p-5 hover:shadow-lg transition block">
                <div class="text-blue-700 text-2xl mb-2"><span class="material-symbols-outlined">local_shipping</span></div>
                <div class="font-bold text-brandDark">Manage Fleet</div>
                <div class="text-xs text-on-surface-variant">Drivers & vehicles</div>
            </a>
            <a href='<%= ResolveUrl("~/Reports/Performance.aspx") %>' class="fcl-card rounded-xl p-5 hover:shadow-lg transition block">
                <div class="text-brandDark text-2xl mb-2"><span class="material-symbols-outlined">bar_chart</span></div>
                <div class="font-bold text-brandDark">Reports</div>
                <div class="text-xs text-on-surface-variant">Operational & financial reports</div>
            </a>
        </div>
    </div>
</asp:Content>
