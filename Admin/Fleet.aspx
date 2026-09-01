<%@ Page Title="Manage Fleet" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="Fleet.aspx.cs" Inherits="ForteLogisticsApp.AdminPortal.Fleet" %>

<asp:Content ID="c1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="max-w-7xl mx-auto px-4 py-8">
        <p class="text-xs text-on-surface-variant mb-1"><span class="material-symbols-outlined mr-1">home</span> Home / <a href='<%= ResolveUrl("~/Admin/Dashboard.aspx") %>' class="hover:text-brandRed">Admin</a> / <span class="text-brandRed font-semibold">Fleet</span></p>
        <h1 class="text-2xl font-black text-brandDark mb-1">Fleet Management</h1>
        <p class="text-on-surface-variant text-sm mb-6">Register vehicles and review driver availability.</p>

        <asp:Label ID="lblMsg" runat="server" CssClass="block text-sm mb-4"></asp:Label>

        <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
            <!-- Add vehicle -->
            <div class="fcl-card rounded-xl p-6">
                <h2 class="font-bold text-lg text-brandDark mb-4">Add a Vehicle</h2>
                <div class="space-y-3">
                    <div>
                        <label class="block text-xs font-bold text-on-surface uppercase mb-1">Registration *</label>
                        <asp:TextBox ID="txtReg" runat="server" CssClass="fcl-input" placeholder="FCL 003 GP"></asp:TextBox>
                    </div>
                    <div>
                        <label class="block text-xs font-bold text-on-surface uppercase mb-1">Vehicle Type</label>
                        <asp:DropDownList ID="ddlType" runat="server" CssClass="fcl-input">
                            <asp:ListItem>Panel Van</asp:ListItem><asp:ListItem>1-Ton Truck</asp:ListItem>
                            <asp:ListItem>3-Ton Truck</asp:ListItem><asp:ListItem>Motorbike</asp:ListItem>
                        </asp:DropDownList>
                    </div>
                    <div>
                        <label class="block text-xs font-bold text-on-surface uppercase mb-1">Assign Driver</label>
                        <asp:DropDownList ID="ddlDriver" runat="server" CssClass="fcl-input"></asp:DropDownList>
                    </div>
                    <asp:Button ID="btnAddVehicle" runat="server" Text="Add Vehicle" OnClick="btnAddVehicle_Click" CssClass="fcl-btn w-full" />
                </div>
            </div>

            <!-- Lists -->
            <div class="lg:col-span-2 space-y-6">
                <div class="fcl-card rounded-xl p-6 overflow-x-auto">
                    <h2 class="font-bold text-lg text-brandDark mb-4">Vehicles</h2>
                    <asp:Repeater ID="rptVehicles" runat="server">
                        <HeaderTemplate>
                            <table class="w-full text-sm text-left">
                                <thead><tr class="text-xs uppercase text-on-surface-variant border-b">
                                    <th class="py-2 pr-3">Registration</th><th class="py-2 pr-3">Type</th><th class="py-2 pr-3">Driver</th>
                                </tr></thead><tbody>
                        </HeaderTemplate>
                        <ItemTemplate>
                            <tr class="border-b border-outline-variant">
                                <td class="py-2 pr-3 font-semibold text-brandDark"><%# UiHelper.Enc(Eval("RegistrationNumber")) %></td>
                                <td class="py-2 pr-3"><%# UiHelper.Enc(Eval("VehicleType")) %></td>
                                <td class="py-2 pr-3 text-on-surface-variant"><%# UiHelper.Enc(Eval("DriverName")) %></td>
                            </tr>
                        </ItemTemplate>
                        <FooterTemplate></tbody></table></FooterTemplate>
                    </asp:Repeater>
                </div>

                <div class="fcl-card rounded-xl p-6 overflow-x-auto">
                    <h2 class="font-bold text-lg text-brandDark mb-4">Drivers</h2>
                    <asp:Repeater ID="rptDrivers" runat="server">
                        <HeaderTemplate>
                            <table class="w-full text-sm text-left">
                                <thead><tr class="text-xs uppercase text-on-surface-variant border-b">
                                    <th class="py-2 pr-3">Name</th><th class="py-2 pr-3">License</th>
                                    <th class="py-2 pr-3">Employee #</th><th class="py-2 pr-3">Availability</th>
                                </tr></thead><tbody>
                        </HeaderTemplate>
                        <ItemTemplate>
                            <tr class="border-b border-outline-variant">
                                <td class="py-2 pr-3 font-semibold text-brandDark"><%# UiHelper.Enc(Eval("FullName")) %></td>
                                <td class="py-2 pr-3 text-on-surface-variant"><%# UiHelper.Enc(Eval("LicenseNumber")) %></td>
                                <td class="py-2 pr-3 text-on-surface-variant"><%# UiHelper.Enc(Eval("EmployeeNumber")) %></td>
                                <td class="py-2 pr-3"><%# ForteLogisticsApp.Services.UiHelper.StatusBadge(Eval("AvailabilityStatus").ToString()) %></td>
                            </tr>
                        </ItemTemplate>
                        <FooterTemplate></tbody></table></FooterTemplate>
                    </asp:Repeater>
                </div>
            </div>
        </div>
    </div>
</asp:Content>
