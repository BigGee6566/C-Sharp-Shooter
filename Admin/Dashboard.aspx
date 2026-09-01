<%@ Page Title="Admin Dashboard" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="Dashboard.aspx.cs" Inherits="ForteLogisticsApp.AdminPortal.Dashboard" %>

<asp:Content ID="c1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="max-w-7xl mx-auto px-4 py-8">
        <div class="mb-6">
            <p class="text-xs text-gray-500"><span class="material-symbols-outlined mr-1">home</span> Home / <span class="text-brandRed font-semibold">Admin Dashboard</span></p>
            <h1 class="text-2xl font-black text-brandDark">System Administration</h1>
            <p class="text-gray-600 text-sm">Manage users, fleet and monitor overall system activity.</p>
        </div>

        <div class="grid grid-cols-2 md:grid-cols-4 gap-4 mb-8">
            <div class="fcl-card rounded-xl p-4"><div class="text-xs font-bold uppercase text-gray-500">Users</div><div class="text-3xl font-black text-brandDark"><asp:Literal ID="litUsers" runat="server">0</asp:Literal></div></div>
            <div class="fcl-card rounded-xl p-4"><div class="text-xs font-bold uppercase text-gray-500">Drivers</div><div class="text-3xl font-black text-blue-700"><asp:Literal ID="litDrivers" runat="server">0</asp:Literal></div></div>
            <div class="fcl-card rounded-xl p-4"><div class="text-xs font-bold uppercase text-gray-500">Vehicles</div><div class="text-3xl font-black text-purple-700"><asp:Literal ID="litVehicles" runat="server">0</asp:Literal></div></div>
            <div class="fcl-card rounded-xl p-4"><div class="text-xs font-bold uppercase text-gray-500">Shipments</div><div class="text-3xl font-black text-green-700"><asp:Literal ID="litShipments" runat="server">0</asp:Literal></div></div>
        </div>

        <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
            <a href='<%= ResolveUrl("~/Admin/Users.aspx") %>' class="fcl-card rounded-xl p-5 hover:shadow-lg transition block">
                <div class="text-brandRed text-2xl mb-2"><span class="material-symbols-outlined">manage_accounts</span></div>
                <div class="font-bold text-brandDark">Manage Users</div>
                <div class="text-xs text-gray-500">Create staff & customer accounts</div>
            </a>
            <a href='<%= ResolveUrl("~/Admin/Fleet.aspx") %>' class="fcl-card rounded-xl p-5 hover:shadow-lg transition block">
                <div class="text-blue-700 text-2xl mb-2"><span class="material-symbols-outlined">local_shipping</span></div>
                <div class="font-bold text-brandDark">Manage Fleet</div>
                <div class="text-xs text-gray-500">Drivers & vehicles</div>
            </a>
            <a href='<%= ResolveUrl("~/Reports/DailyParcelMovement.aspx") %>' class="fcl-card rounded-xl p-5 hover:shadow-lg transition block">
                <div class="text-brandDark text-2xl mb-2"><span class="material-symbols-outlined">bar_chart</span></div>
                <div class="font-bold text-brandDark">Reports</div>
                <div class="text-xs text-gray-500">Operational & financial reports</div>
            </a>
        </div>
    </div>
</asp:Content>
