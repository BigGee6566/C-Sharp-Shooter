<%@ Page Title="Operations Dashboard" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="Dashboard.aspx.cs" Inherits="ForteLogisticsApp.Ops.Dashboard" %>

<asp:Content ID="c1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="max-w-7xl mx-auto px-4 py-8">
        <div class="mb-6">
            <p class="text-xs text-gray-500"><span class="material-symbols-outlined mr-1">home</span> Home / <span class="text-brandRed font-semibold">Operations Dashboard</span></p>
            <h1 class="text-2xl font-black text-brandDark">Operations Dashboard</h1>
            <p class="text-gray-600 text-sm">Live view of parcel intake, sorting, dispatch and delivery performance.</p>
        </div>

        <div class="grid grid-cols-2 md:grid-cols-5 gap-4 mb-8">
            <div class="fcl-card rounded-xl p-4"><div class="text-xs font-bold uppercase text-gray-500">Registered</div><div class="text-3xl font-black text-brandDark"><asp:Literal ID="litRegistered" runat="server">0</asp:Literal></div></div>
            <div class="fcl-card rounded-xl p-4"><div class="text-xs font-bold uppercase text-gray-500">In Warehouse</div><div class="text-3xl font-black text-purple-700"><asp:Literal ID="litWarehouse" runat="server">0</asp:Literal></div></div>
            <div class="fcl-card rounded-xl p-4"><div class="text-xs font-bold uppercase text-gray-500">In Transit</div><div class="text-3xl font-black text-blue-700"><asp:Literal ID="litTransit" runat="server">0</asp:Literal></div></div>
            <div class="fcl-card rounded-xl p-4"><div class="text-xs font-bold uppercase text-gray-500">Delivered</div><div class="text-3xl font-black text-green-700"><asp:Literal ID="litDelivered" runat="server">0</asp:Literal></div></div>
            <div class="fcl-card rounded-xl p-4"><div class="text-xs font-bold uppercase text-gray-500">Exceptions</div><div class="text-3xl font-black text-brandRed"><asp:Literal ID="litExceptions" runat="server">0</asp:Literal></div></div>
        </div>

        <div class="grid grid-cols-1 md:grid-cols-4 gap-4 mb-8">
            <a href='<%= ResolveUrl("~/Ops/ParcelRegistration.aspx") %>' class="fcl-card rounded-xl p-5 hover:shadow-lg transition block">
                <div class="text-brandRed text-2xl mb-2"><span class="material-symbols-outlined">barcode</span></div>
                <div class="font-bold text-brandDark">Register Parcel</div>
                <div class="text-xs text-gray-500">Intake & barcode generation</div>
            </a>
            <a href='<%= ResolveUrl("~/Ops/Warehouse.aspx") %>' class="fcl-card rounded-xl p-5 hover:shadow-lg transition block">
                <div class="text-purple-700 text-2xl mb-2"><span class="material-symbols-outlined">warehouse</span></div>
                <div class="font-bold text-brandDark">Warehouse Sorting</div>
                <div class="text-xs text-gray-500">Scan-in, sort & flag parcels</div>
            </a>
            <a href='<%= ResolveUrl("~/Ops/RoutePlanning.aspx") %>' class="fcl-card rounded-xl p-5 hover:shadow-lg transition block">
                <div class="text-blue-700 text-2xl mb-2"><span class="material-symbols-outlined">route</span></div>
                <div class="font-bold text-brandDark">Route Planning</div>
                <div class="text-xs text-gray-500">Assign drivers & vehicles</div>
            </a>
            <a href='<%= ResolveUrl("~/Reports/DailyParcelMovement.aspx") %>' class="fcl-card rounded-xl p-5 hover:shadow-lg transition block">
                <div class="text-brandDark text-2xl mb-2"><span class="material-symbols-outlined">show_chart</span></div>
                <div class="font-bold text-brandDark">Reports</div>
                <div class="text-xs text-gray-500">Movement, revenue & more</div>
            </a>
        </div>

        <div class="fcl-card rounded-xl p-6 overflow-x-auto">
            <h2 class="font-bold text-lg text-brandDark mb-4">Recent Shipments</h2>
            <asp:Repeater ID="rptRecent" runat="server">
                <HeaderTemplate>
                    <table class="w-full text-sm text-left">
                        <thead><tr class="text-xs uppercase text-gray-500 border-b">
                            <th class="py-2 pr-3">Waybill</th><th class="py-2 pr-3">Route</th>
                            <th class="py-2 pr-3">Warehouse</th><th class="py-2 pr-3">Delivery</th>
                            <th class="py-2 pr-3">Driver</th><th class="py-2 pr-3">Date</th>
                        </tr></thead><tbody>
                </HeaderTemplate>
                <ItemTemplate>
                    <tr class="border-b border-gray-100">
                        <td class="py-2 pr-3 font-semibold text-brandDark"><%# UiHelper.Enc(Eval("TrackingNumber")) %></td>
                        <td class="py-2 pr-3"><%# UiHelper.Enc(Eval("OriginCity")) %> &rarr; <%# UiHelper.Enc(Eval("DestCity")) %></td>
                        <td class="py-2 pr-3"><%# ForteLogisticsApp.Services.UiHelper.StatusBadge(Eval("WarehouseStatus").ToString()) %></td>
                        <td class="py-2 pr-3"><%# ForteLogisticsApp.Services.UiHelper.StatusBadge(Eval("DeliveryStatus").ToString()) %></td>
                        <td class="py-2 pr-3 text-gray-600"><%# UiHelper.Enc(Eval("DriverName")) %></td>
                        <td class="py-2 pr-3 text-gray-500"><%# Eval("ShipmentDate", "{0:dd MMM}") %></td>
                    </tr>
                </ItemTemplate>
                <FooterTemplate></tbody></table></FooterTemplate>
            </asp:Repeater>
        </div>
    </div>
</asp:Content>
