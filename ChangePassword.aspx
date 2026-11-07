<%@ Page Title="Change Password" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="ChangePassword.aspx.cs" Inherits="ForteLogisticsApp.ChangePasswordPage" %>

<asp:Content ID="c1" ContentPlaceHolderID="MainContent" runat="server">

    <div class="flex items-center justify-center min-h-[70vh]">
        <div class="w-full max-w-md bg-surface-container-lowest border border-outline-variant rounded-xl shadow-lg p-8">

            <div class="text-center mb-stack-md">
                <div class="flex justify-center items-center mb-stack-sm">
                    <img src='<%= ResolveUrl("~/Images/web/logo.png") %>' alt="LogistiCore Courier Service"
                         class="h-12 w-auto object-contain" />
                </div>
                <h2 class="font-headline-md text-headline-md text-on-surface">Choose a New Password</h2>
            </div>

            <asp:Panel ID="pnlForm" runat="server">
                <p class="font-body-md text-label-md text-on-surface-variant text-center mb-stack-md">
                    Your temporary password has now been used and won't work again.
                    Choose your own password to continue.
                </p>

                <asp:Panel ID="pnlError" runat="server" Visible="false" CssClass="mb-stack-sm">
                    <asp:Label ID="lblStatus" runat="server" />
                </asp:Panel>

                <div class="space-y-4">
                    <div>
                        <label class="block font-label-sm text-label-sm text-on-surface-variant mb-1" for="<%= txtNew.ClientID %>">New Password</label>
                        <div class="relative">
                            <span class="material-symbols-outlined absolute left-3 top-1/2 -translate-y-1/2 text-on-surface-variant">lock</span>
                            <asp:TextBox ID="txtNew" runat="server" TextMode="Password" autocomplete="new-password" CssClass="fcl-input pl-10" />
                        </div>
                        <asp:RequiredFieldValidator ID="rfvNew" runat="server" ControlToValidate="txtNew"
                            Display="Dynamic" ValidationGroup="Change" ErrorMessage="A new password is required."
                            CssClass="block font-label-sm text-label-sm text-error mt-1" />
                    </div>

                    <div>
                        <label class="block font-label-sm text-label-sm text-on-surface-variant mb-1" for="<%= txtConfirm.ClientID %>">Confirm New Password</label>
                        <div class="relative">
                            <span class="material-symbols-outlined absolute left-3 top-1/2 -translate-y-1/2 text-on-surface-variant">lock</span>
                            <asp:TextBox ID="txtConfirm" runat="server" TextMode="Password" autocomplete="new-password" CssClass="fcl-input pl-10" />
                        </div>
                        <asp:RequiredFieldValidator ID="rfvConfirm" runat="server" ControlToValidate="txtConfirm"
                            Display="Dynamic" ValidationGroup="Change" ErrorMessage="Please confirm your new password."
                            CssClass="block font-label-sm text-label-sm text-error mt-1" />
                    </div>

                    <asp:Button ID="btnSave" runat="server" Text="Save New Password" OnClick="btnSave_Click"
                        ValidationGroup="Change" CssClass="fcl-btn w-full mt-2" />
                </div>

                <p class="text-center mt-stack-md">
                    <asp:LinkButton ID="btnSignOut" runat="server" OnClick="btnSignOut_Click" CausesValidation="false"
                        CssClass="fcl-link font-label-sm text-label-sm">Sign out</asp:LinkButton>
                </p>
            </asp:Panel>

            <asp:Panel ID="pnlDone" runat="server" Visible="false">
                <div class="mb-stack-md">
                    <asp:Label ID="lblDone" runat="server" />
                </div>
                <asp:HyperLink ID="lnkDashboard" runat="server" Text="Go to my dashboard"
                    CssClass="fcl-btn w-full block text-center no-underline" />
            </asp:Panel>
        </div>
    </div>
</asp:Content>
