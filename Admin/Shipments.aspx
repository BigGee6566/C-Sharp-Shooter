<%@ Page Title="Shipments" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="Shipments.aspx.cs" Inherits="ForteLogisticsApp.AdminPortal.Shipments" %>

<asp:Content ID="c1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="max-w-7xl mx-auto px-4 py-8">
        <p class="text-xs text-gray-500 mb-1"><span class="material-symbols-outlined mr-1">home</span> Home / <a href='<%= ResolveUrl("~/Admin/Dashboard.aspx") %>' class="hover:text-brandRed">Admin</a> / <span class="text-brandRed font-semibold">Shipments</span></p>

        <!-- ============ LIST VIEW ============ -->
        <asp:Panel ID="pnlList" runat="server">
            <h1 class="text-2xl font-black text-brandDark mb-1">All Shipments</h1>
            <p class="text-gray-600 text-sm mb-6">Every parcel in the system. Click a row to see full details.</p>

            <div class="fcl-card rounded-xl p-4 mb-4 flex items-end gap-2">
                <div class="flex-1">
                    <label class="block text-[11px] font-bold text-gray-600 uppercase mb-1">Search (waybill / city / status)</label>
                    <asp:TextBox ID="txtSearch" runat="server" CssClass="fcl-input" placeholder="e.g. FCL-10000001 or Durban"></asp:TextBox>
                </div>
                <asp:Button ID="btnSearch" runat="server" Text="Search" OnClick="btnSearch_Click" CssClass="fcl-btn" />
            </div>

            <div class="fcl-card rounded-xl p-6 overflow-x-auto">
                <asp:Repeater ID="rptShipments" runat="server">
                    <HeaderTemplate>
                        <table class="w-full text-sm text-left">
                            <thead><tr class="text-xs uppercase text-gray-500 border-b">
                                <th class="py-2 pr-3">Waybill</th><th class="py-2 pr-3">Route</th>
                                <th class="py-2 pr-3">Sender</th><th class="py-2 pr-3">Driver</th>
                                <th class="py-2 pr-3">Warehouse</th><th class="py-2 pr-3">Delivery</th>
                                <th class="py-2 pr-3">Cost</th><th class="py-2 pr-3"></th>
                            </tr></thead><tbody>
                    </HeaderTemplate>
                    <ItemTemplate>
                        <tr class="border-b border-gray-100">
                            <td class="py-2 pr-3 font-semibold text-brandDark"><%# UiHelper.Enc(Eval("TrackingNumber")) %></td>
                            <td class="py-2 pr-3"><%# UiHelper.Enc(Eval("OriginCity")) %> &rarr; <%# UiHelper.Enc(Eval("DestCity")) %></td>
                            <td class="py-2 pr-3 text-gray-600"><%# UiHelper.Enc(Eval("SenderName")) %></td>
                            <td class="py-2 pr-3 text-gray-600"><%# UiHelper.Enc(Eval("DriverName")) %></td>
                            <td class="py-2 pr-3"><%# ForteLogisticsApp.Services.UiHelper.StatusBadge(Eval("WarehouseStatus").ToString()) %></td>
                            <td class="py-2 pr-3"><%# ForteLogisticsApp.Services.UiHelper.StatusBadge(Eval("DeliveryStatus").ToString()) %></td>
                            <td class="py-2 pr-3">R <%# Eval("Cost", "{0:N2}") %></td>
                            <td class="py-2 pr-3"><a href='<%# ResolveUrl("~/Admin/Shipments.aspx?id=" + Eval("ShipmentID")) %>' class="text-brandRed font-bold hover:underline">View</a></td>
                        </tr>
                    </ItemTemplate>
                    <FooterTemplate></tbody></table></FooterTemplate>
                </asp:Repeater>
                <asp:Panel ID="pnlEmpty" runat="server" Visible="false" CssClass="text-center text-gray-500 text-sm py-8">No shipments found.</asp:Panel>
            </div>
        </asp:Panel>

        <!-- ============ DETAIL VIEW ============ -->
        <asp:Panel ID="pnlDetail" runat="server" Visible="false">
            <div class="flex items-center justify-between mb-4">
                <h1 class="text-2xl font-black text-brandDark">Shipment <asp:Literal ID="litWaybill" runat="server"></asp:Literal></h1>
                <a href='<%= ResolveUrl("~/Admin/Shipments.aspx") %>' class="text-sm text-gray-600 hover:text-brandRed"><span class="material-symbols-outlined mr-1">arrow_back</span> Back to list</a>
            </div>

            <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
                <div class="lg:col-span-2 fcl-card rounded-xl p-6">
                    <div class="flex items-center gap-3 mb-4">
                        <asp:Literal ID="litDelStatus" runat="server"></asp:Literal>
                        <asp:Literal ID="litWhStatus" runat="server"></asp:Literal>
                        <asp:Literal ID="litException" runat="server"></asp:Literal>
                    </div>
                    <dl class="grid grid-cols-1 md:grid-cols-2 gap-x-6 gap-y-3 text-sm">
                        <div><dt class="text-xs uppercase text-gray-500">Sender</dt><dd class="font-semibold text-brandDark"><asp:Literal ID="litSender" runat="server"></asp:Literal></dd></div>
                        <div><dt class="text-xs uppercase text-gray-500">Recipient</dt><dd class="font-semibold text-brandDark"><asp:Literal ID="litReceiver" runat="server"></asp:Literal></dd></div>
                        <div><dt class="text-xs uppercase text-gray-500">Pickup Address</dt><dd class="text-gray-700"><asp:Literal ID="litPickup" runat="server"></asp:Literal></dd></div>
                        <div><dt class="text-xs uppercase text-gray-500">Delivery Address</dt><dd class="text-gray-700"><asp:Literal ID="litDelivery" runat="server"></asp:Literal></dd></div>
                        <div><dt class="text-xs uppercase text-gray-500">Route</dt><dd class="text-gray-700"><asp:Literal ID="litRoute" runat="server"></asp:Literal></dd></div>
                        <div><dt class="text-xs uppercase text-gray-500">Weight</dt><dd class="text-gray-700"><asp:Literal ID="litWeight" runat="server"></asp:Literal></dd></div>
                        <div><dt class="text-xs uppercase text-gray-500">Description</dt><dd class="text-gray-700"><asp:Literal ID="litDesc" runat="server"></asp:Literal></dd></div>
                        <div><dt class="text-xs uppercase text-gray-500">Sort Category</dt><dd class="text-gray-700"><asp:Literal ID="litSort" runat="server"></asp:Literal></dd></div>
                        <div><dt class="text-xs uppercase text-gray-500">Driver</dt><dd class="font-semibold text-brandDark"><asp:Literal ID="litDriver" runat="server"></asp:Literal></dd></div>
                        <div><dt class="text-xs uppercase text-gray-500">Delivery Route</dt><dd class="text-gray-700"><asp:Literal ID="litRouteName" runat="server"></asp:Literal></dd></div>
                        <div><dt class="text-xs uppercase text-gray-500">Vehicle</dt><dd class="text-gray-700"><asp:Literal ID="litVehicle" runat="server"></asp:Literal></dd></div>
                        <div><dt class="text-xs uppercase text-gray-500">Cost</dt><dd class="font-semibold text-brandDark">R <asp:Literal ID="litCost" runat="server"></asp:Literal></dd></div>
                        <div><dt class="text-xs uppercase text-gray-500">Registered</dt><dd class="text-gray-700"><asp:Literal ID="litShipDate" runat="server"></asp:Literal></dd></div>
                        <div><dt class="text-xs uppercase text-gray-500">Delivered</dt><dd class="text-gray-700"><asp:Literal ID="litDelDate" runat="server"></asp:Literal></dd></div>
                        <div><dt class="text-xs uppercase text-gray-500">Barcode</dt><dd class="text-gray-700"><asp:Literal ID="litBarcode" runat="server"></asp:Literal></dd></div>
                    </dl>

                    <h3 class="text-xs font-bold uppercase text-gray-500 mt-6 mb-3">Invoice / Payment</h3>
                    <asp:Literal ID="litPayment" runat="server"></asp:Literal>
                </div>

                <div class="fcl-card rounded-xl p-6">
                    <h3 class="text-xs font-bold uppercase text-gray-500 mb-3">Tracking History</h3>
                    <div class="relative border-l-2 border-gray-200 ml-2 pl-6 space-y-5">
                        <asp:Repeater ID="rptTimeline" runat="server">
                            <ItemTemplate>
                                <div class="relative">
                                    <span class="absolute -left-[31px] top-1 w-3.5 h-3.5 rounded-full bg-brandRed border-2 border-white shadow"></span>
                                    <div class="flex items-center gap-2 flex-wrap">
                                        <%# ForteLogisticsApp.Services.UiHelper.StatusBadge(Eval("Status").ToString()) %>
                                        <span class="text-xs text-gray-400"><%# Eval("Timestamp", "{0:dd MMM yyyy HH:mm}") %></span>
                                    </div>
                                    <p class="text-sm text-brandDark font-semibold mt-1"><%# UiHelper.Enc(Eval("Location")) %></p>
                                    <p class="text-xs text-gray-600"><%# UiHelper.Enc(Eval("Remarks")) %></p>
                                </div>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>
                </div>
            </div>
        </asp:Panel>
    </div>
</asp:Content>
