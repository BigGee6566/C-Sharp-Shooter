<%@ Page Title="My Profile" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="Profile.aspx.cs" Inherits="ForteLogisticsApp.CustomerPortal.Profile" %>

<asp:Content ID="c1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="max-w-3xl mx-auto px-4 py-8">
        <p class="text-xs text-on-surface-variant mb-1"><span class="material-symbols-outlined mr-1">home</span> Home / <a href='<%= ResolveUrl("~/Customer/Dashboard.aspx") %>' class="hover:text-brandRed">Dashboard</a> / <span class="text-brandRed font-semibold">My Profile</span></p>
        <h1 class="text-2xl font-black text-brandDark mb-1">My Profile</h1>
        <p class="text-on-surface-variant text-sm mb-6">Keep your contact details up to date.</p>

        <div class="fcl-card rounded-xl p-6">
            <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                    <label class="block text-xs font-bold text-on-surface uppercase mb-1">Full Name *</label>
                    <asp:TextBox ID="txtFullName" runat="server" CssClass="fcl-input"></asp:TextBox>
                </div>
                <div>
                    <label class="block text-xs font-bold text-on-surface uppercase mb-1">Email (read-only)</label>
                    <asp:TextBox ID="txtEmail" runat="server" CssClass="fcl-input" Enabled="false"></asp:TextBox>
                </div>
                <div>
                    <label class="block text-xs font-bold text-on-surface uppercase mb-1">Phone Number</label>
                    <asp:TextBox ID="txtPhone" runat="server" CssClass="fcl-input"></asp:TextBox>
                </div>
                <div>
                    <label class="block text-xs font-bold text-on-surface uppercase mb-1">Account Type</label>
                    <asp:TextBox ID="txtClientType" runat="server" CssClass="fcl-input" Enabled="false"></asp:TextBox>
                </div>
                <div class="md:col-span-2">
                    <label class="block text-xs font-bold text-on-surface uppercase mb-1">Address</label>
                    <asp:TextBox ID="txtAddress" runat="server" CssClass="fcl-input"></asp:TextBox>
                </div>
            </div>
            <div class="mt-5">
                <asp:Button ID="btnSave" runat="server" Text="Save Details" OnClick="btnSave_Click" CssClass="fcl-btn" />
                <asp:Label ID="lblSaveMsg" runat="server" CssClass="block text-sm mt-3"></asp:Label>
            </div>
        </div>

        <div class="fcl-card rounded-xl p-6 mt-6">
            <h2 class="font-bold text-lg text-brandDark mb-4">Change Password</h2>
            <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
                <div>
                    <label class="block text-xs font-bold text-on-surface uppercase mb-1">Current Password</label>
                    <asp:TextBox ID="txtCurrent" runat="server" TextMode="Password" CssClass="fcl-input"></asp:TextBox>
                </div>
                <div>
                    <label class="block text-xs font-bold text-on-surface uppercase mb-1">New Password</label>
                    <asp:TextBox ID="txtNew" runat="server" TextMode="Password" CssClass="fcl-input"></asp:TextBox>
                </div>
                <div>
                    <label class="block text-xs font-bold text-on-surface uppercase mb-1">Confirm New</label>
                    <asp:TextBox ID="txtConfirm" runat="server" TextMode="Password" CssClass="fcl-input"></asp:TextBox>
                </div>
            </div>
            <div class="mt-5">
                <asp:Button ID="btnChangePw" runat="server" Text="Update Password" OnClick="btnChangePw_Click" CssClass="fcl-btn fcl-btn-dark" />
                <asp:Label ID="lblPwMsg" runat="server" CssClass="block text-sm mt-3"></asp:Label>
            </div>
        </div>
    </div>
</asp:Content>
