<%@ Page Title="Route Planning" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="RoutePlanning.aspx.cs" Inherits="ForteLogisticsApp.Ops.RoutePlanning" %>

<asp:Content ID="c1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="max-w-7xl mx-auto px-4 py-8">
        <p class="text-xs text-on-surface-variant mb-1"><span class="material-symbols-outlined mr-1">home</span> Home / <a href='<%= ResolveUrl("~/Ops/Dashboard.aspx") %>' class="hover:text-brandRed">Operations</a> / <span class="text-brandRed font-semibold">Route Planning</span></p>
        <h1 class="text-2xl font-black text-brandDark mb-1">Route Planning & Driver Assignment</h1>
        <p class="text-on-surface-variant text-sm mb-6">Create delivery routes, assign a driver and vehicle, then allocate sorted parcels to the route.</p>

        <div class="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-6">
            <!-- Create route -->
            <div class="fcl-card rounded-xl p-6">
                <h2 class="font-bold text-lg text-brandDark mb-4"><span class="material-symbols-outlined text-brandRed mr-1">route</span> Create a Route</h2>
                <div class="space-y-3">
                    <div>
                        <label class="block text-xs font-bold text-on-surface uppercase mb-1">Route Name *</label>
                        <asp:TextBox ID="txtRouteName" runat="server" CssClass="fcl-input" placeholder="e.g. JHB North Loop"></asp:TextBox>
                    </div>
                    <div class="grid grid-cols-2 gap-3">
                        <div>
                            <label class="block text-xs font-bold text-on-surface uppercase mb-1">Driver</label>
                            <asp:DropDownList ID="ddlDriver" runat="server" CssClass="fcl-input"></asp:DropDownList>
                        </div>
                        <div>
                            <label class="block text-xs font-bold text-on-surface uppercase mb-1">Vehicle</label>
                            <asp:DropDownList ID="ddlVehicle" runat="server" CssClass="fcl-input"></asp:DropDownList>
                        </div>
                    </div>
                    <div>
                        <label class="block text-xs font-bold text-on-surface uppercase mb-1">Service Area *</label>
                        <asp:DropDownList ID="ddlServiceArea" runat="server" CssClass="fcl-input"></asp:DropDownList>
                        <p class="text-xs text-on-surface-variant mt-1">Only parcels destined for this lane can be allocated to the route.</p>
                    </div>
                    <asp:Button ID="btnCreateRoute" runat="server" Text="Create Route" OnClick="btnCreateRoute_Click" CssClass="fcl-btn" />
                    <asp:Label ID="lblRouteMsg" runat="server" CssClass="block text-xs mt-1"></asp:Label>
                </div>
            </div>

            <!-- Allocate parcels -->
            <div class="fcl-card rounded-xl p-6">
                <h2 class="font-bold text-lg text-brandDark mb-4"><span class="material-symbols-outlined text-brandRed mr-1">inventory</span> Allocate Parcels to a Route</h2>
                <div class="mb-3">
                    <label class="block text-xs font-bold text-on-surface uppercase mb-1">Select Route</label>
                    <asp:DropDownList ID="ddlRoute" runat="server" CssClass="fcl-input"
                        AutoPostBack="True" OnSelectedIndexChanged="ddlRoute_Changed"></asp:DropDownList>
                    <p class="text-xs text-on-surface-variant mt-1"><asp:Literal ID="litRouteServiceArea" runat="server" /></p>
                </div>
                <%-- Filter to one sort lane, tick them all at once, then confirm with Assign. --%>
                <div class="mb-3">
                    <label class="block text-xs font-bold text-on-surface uppercase mb-1">Filter by Sort Lane</label>
                    <asp:DropDownList ID="ddlFilterCategory" runat="server" CssClass="fcl-input"
                        AutoPostBack="True" OnSelectedIndexChanged="ddlFilterCategory_Changed"></asp:DropDownList>
                </div>
                <div class="flex items-center justify-between mb-1">
                    <label class="block text-xs font-bold text-on-surface uppercase">Unassigned Sorted Parcels</label>
                    <asp:LinkButton ID="btnSelectAll" runat="server" OnClick="btnSelectAll_Click"
                        CssClass="text-xs text-brandRed font-bold hover:underline">Select all</asp:LinkButton>
                </div>
                <div class="max-h-52 overflow-y-auto border border-outline-variant rounded p-2 mb-3">
                    <asp:CheckBoxList ID="cblParcels" runat="server" CssClass="text-sm space-y-1"></asp:CheckBoxList>
                    <asp:Panel ID="pnlNoParcels" runat="server" Visible="false" CssClass="text-xs text-on-surface-variant py-2">
                        No sorted parcels are waiting for allocation. Sort parcels in the Warehouse first.
                    </asp:Panel>
                </div>
                <asp:Button ID="btnAssign" runat="server" Text="Assign Selected to Route" OnClick="btnAssign_Click" CssClass="fcl-btn" />
                <asp:Label ID="lblAssignMsg" runat="server" CssClass="block text-xs mt-1"></asp:Label>
            </div>
        </div>

        <!-- Routes table -->
        <div class="fcl-card rounded-xl p-6 overflow-x-auto">
            <h2 class="font-bold text-lg text-brandDark mb-4">Active Routes</h2>
            <asp:Repeater ID="rptRoutes" runat="server" OnItemCommand="rptRoutes_ItemCommand">
                <HeaderTemplate>
                    <table class="w-full text-sm text-left">
                        <thead><tr class="text-xs uppercase text-on-surface-variant border-b">
                            <th class="py-2 pr-3">Route</th><th class="py-2 pr-3">Service Area</th><th class="py-2 pr-3">Driver</th>
                            <th class="py-2 pr-3">Vehicle</th><th class="py-2 pr-3">Parcels</th>
                            <th class="py-2 pr-3">Date</th><th class="py-2 pr-3">Status</th><th class="py-2 pr-3"></th>
                        </tr></thead><tbody>
                </HeaderTemplate>
                <ItemTemplate>
                    <tr class="border-b border-outline-variant">
                        <td class="py-2 pr-3 font-semibold text-brandDark"><%# UiHelper.Enc(Eval("RouteName")) %></td>
                        <td class="py-2 pr-3 text-on-surface-variant"><%# UiHelper.Enc(Eval("ServiceArea")) %></td>
                        <td class="py-2 pr-3"><%# UiHelper.Enc(Eval("DriverName")) %></td>
                        <td class="py-2 pr-3 text-on-surface-variant"><%# UiHelper.Enc(Eval("Vehicle")) %></td>
                        <td class="py-2 pr-3"><%# Eval("ParcelCount") %></td>
                        <td class="py-2 pr-3 text-on-surface-variant"><%# Eval("RouteDate", "{0:dd MMM yyyy}") %></td>
                        <td class="py-2 pr-3"><%# ForteLogisticsApp.Services.UiHelper.StatusBadge(Eval("Status").ToString()) %></td>
                        <td class="py-2 pr-3 space-x-2 whitespace-nowrap">
                            <asp:LinkButton runat="server" CommandName="toggle" CommandArgument='<%# Eval("RouteID") %>'
                                Visible='<%# Eval("Status").ToString() != "Completed" %>'
                                CssClass="text-on-surface-variant hover:underline"
                                OnClientClick="return confirm('Change this route\'s status?');">
                                <%# Eval("Status").ToString() == "Inactive" ? "Activate" : "Deactivate" %>
                            </asp:LinkButton>
                            <asp:LinkButton runat="server" CommandName="delete" CommandArgument='<%# Eval("RouteID") %>'
                                CssClass="text-brandRed font-bold hover:underline"
                                OnClientClick="return confirm('Delete this route? This is only possible if it has never carried a parcel.');">Delete</asp:LinkButton>
                        </td>
                    </tr>
                </ItemTemplate>
                <FooterTemplate></tbody></table></FooterTemplate>
            </asp:Repeater>
            <asp:Panel ID="pnlNoRoutes" runat="server" Visible="false" CssClass="text-center text-on-surface-variant text-sm py-8">
                No routes created yet.
            </asp:Panel>
        </div>
    </div>
</asp:Content>
