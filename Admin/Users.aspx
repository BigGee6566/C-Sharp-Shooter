<%@ Page Title="Manage Users" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="Users.aspx.cs" Inherits="ForteLogisticsApp.AdminPortal.Users" %>

<asp:Content ID="c1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="max-w-7xl mx-auto px-4 py-8">
        <p class="text-xs text-gray-500 mb-1"><span class="material-symbols-outlined mr-1">home</span> Home / <a href='<%= ResolveUrl("~/Admin/Dashboard.aspx") %>' class="hover:text-brandRed">Admin</a> / <span class="text-brandRed font-semibold">Users</span></p>
        <h1 class="text-2xl font-black text-brandDark mb-1">Manage Users</h1>
        <p class="text-gray-600 text-sm mb-6">Create, edit and review all system users.</p>

        <!-- Edit user panel (shown when an Edit action is chosen) -->
        <asp:Panel ID="pnlEdit" runat="server" Visible="false" CssClass="fcl-card rounded-xl p-6 mb-6 border-l-4 border-brandRed">
            <h2 class="font-bold text-lg text-brandDark mb-4">Edit User</h2>
            <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                    <label class="block text-xs font-bold text-gray-700 uppercase mb-1">Full Name *</label>
                    <asp:TextBox ID="txtEName" runat="server" CssClass="fcl-input"></asp:TextBox>
                </div>
                <div>
                    <label class="block text-xs font-bold text-gray-700 uppercase mb-1">Email (read-only)</label>
                    <asp:TextBox ID="txtEEmail" runat="server" CssClass="fcl-input" Enabled="false"></asp:TextBox>
                </div>
                <div>
                    <label class="block text-xs font-bold text-gray-700 uppercase mb-1">Role *</label>
                    <asp:DropDownList ID="ddlERole" runat="server" CssClass="fcl-input">
                        <asp:ListItem>Customer</asp:ListItem><asp:ListItem>Staff</asp:ListItem>
                        <asp:ListItem>Admin</asp:ListItem>
                    </asp:DropDownList>
                </div>
                <div>
                    <label class="block text-xs font-bold text-gray-700 uppercase mb-1">Phone</label>
                    <asp:TextBox ID="txtEPhone" runat="server" CssClass="fcl-input"></asp:TextBox>
                </div>
                <div class="md:col-span-2">
                    <label class="flex items-center gap-2 text-xs font-bold text-gray-700 uppercase">
                        <asp:CheckBox ID="chkEIsDriver" runat="server" />
                        Is a driver (adds them to the fleet and makes them assignable on routes)
                    </label>
                </div>
                <div class="md:col-span-2">
                    <label class="block text-xs font-bold text-gray-700 uppercase mb-1">Address</label>
                    <asp:TextBox ID="txtEAddress" runat="server" CssClass="fcl-input"></asp:TextBox>
                </div>
                <div class="md:col-span-2">
                    <label class="block text-xs font-bold text-gray-700 uppercase mb-1">Reset Password (leave blank to keep unchanged)</label>
                    <asp:TextBox ID="txtEResetPw" runat="server" CssClass="fcl-input"></asp:TextBox>
                </div>
            </div>
            <asp:HiddenField ID="hfEditId" runat="server" />
            <div class="mt-4 flex items-center gap-3">
                <asp:Button ID="btnSaveEdit" runat="server" Text="Save Changes" OnClick="btnSaveEdit_Click" CssClass="fcl-btn" />
                <asp:Button ID="btnCancelEdit" runat="server" Text="Cancel" OnClick="btnCancelEdit_Click" CssClass="fcl-btn fcl-btn-dark" CausesValidation="false" />
                <asp:Label ID="lblEditMsg" runat="server" CssClass="text-xs"></asp:Label>
            </div>
        </asp:Panel>

        <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
            <!-- Add user -->
            <div class="fcl-card rounded-xl p-6">
                <h2 class="font-bold text-lg text-brandDark mb-4">Add a User</h2>
                <div class="space-y-3">
                    <div>
                        <label class="block text-xs font-bold text-gray-700 uppercase mb-1">Full Name *</label>
                        <asp:TextBox ID="txtName" runat="server" CssClass="fcl-input"></asp:TextBox>
                    </div>
                    <div>
                        <label class="block text-xs font-bold text-gray-700 uppercase mb-1">Email *</label>
                        <asp:TextBox ID="txtEmail" runat="server" CssClass="fcl-input" TextMode="Email"></asp:TextBox>
                    </div>
                    <div>
                        <label class="block text-xs font-bold text-gray-700 uppercase mb-1">Role *</label>
                        <asp:DropDownList ID="ddlRole" runat="server" CssClass="fcl-input">
                            <asp:ListItem>Customer</asp:ListItem><asp:ListItem>Staff</asp:ListItem>
                            <asp:ListItem>Admin</asp:ListItem>
                        </asp:DropDownList>
                    </div>
                    <div>
                        <label class="flex items-center gap-2 text-xs font-bold text-gray-700 uppercase">
                            <asp:CheckBox ID="chkIsDriver" runat="server" />
                            Is a driver
                        </label>
                    </div>
                    <div>
                        <label class="block text-xs font-bold text-gray-700 uppercase mb-1">Phone</label>
                        <asp:TextBox ID="txtPhone" runat="server" CssClass="fcl-input"></asp:TextBox>
                    </div>
                    <div>
                        <label class="block text-xs font-bold text-gray-700 uppercase mb-1">Temp Password</label>
                        <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" CssClass="fcl-input"></asp:TextBox>
                        <p class="text-xs text-gray-500 mt-1">Leave blank and the system generates one, then sends it to the new user.</p>
                    </div>
                    <asp:Button ID="btnAdd" runat="server" Text="Create User" OnClick="btnAdd_Click" CssClass="fcl-btn w-full" />
                    <asp:Label ID="lblMsg" runat="server" CssClass="block text-xs mt-1"></asp:Label>
                </div>
            </div>

            <!-- Users list -->
            <div class="lg:col-span-2 fcl-card rounded-xl p-6 overflow-x-auto">
                <h2 class="font-bold text-lg text-brandDark mb-4">All Users</h2>
                <asp:Repeater ID="rptUsers" runat="server" OnItemCommand="rptUsers_ItemCommand">
                    <HeaderTemplate>
                        <table class="w-full text-sm text-left">
                            <thead><tr class="text-xs uppercase text-gray-500 border-b">
                                <th class="py-2 pr-3">Name</th><th class="py-2 pr-3">Email</th>
                                <th class="py-2 pr-3">Role</th><th class="py-2 pr-3">Joined</th><th class="py-2 pr-3"></th>
                            </tr></thead><tbody>
                    </HeaderTemplate>
                    <ItemTemplate>
                        <tr class="border-b border-gray-100">
                            <td class="py-2 pr-3 font-semibold text-brandDark"><%# UiHelper.Enc(Eval("FullName")) %></td>
                            <td class="py-2 pr-3 text-gray-600"><%# UiHelper.Enc(Eval("Email")) %></td>
                            <td class="py-2 pr-3"><%# ForteLogisticsApp.Services.UiHelper.StatusBadge(Eval("Role").ToString()) %></td>
                            <td class="py-2 pr-3 text-gray-500"><%# Eval("CreatedDate", "{0:dd MMM yyyy}") %></td>
                            <td class="py-2 pr-3"><asp:LinkButton runat="server" CommandName="edit" CommandArgument='<%# Eval("UserID") %>' CssClass="text-brandRed font-bold hover:underline">Edit</asp:LinkButton></td>
                        </tr>
                    </ItemTemplate>
                    <FooterTemplate></tbody></table></FooterTemplate>
                </asp:Repeater>
            </div>
        </div>
    </div>
</asp:Content>
