<%@ Page Title="Forgot Password" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="ForgotPassword.aspx.cs" Inherits="ForteLogisticsApp.ForgotPasswordPage" %>

<asp:Content ID="c1" ContentPlaceHolderID="MainContent" runat="server">

    <div class="flex items-center justify-center min-h-[70vh]">
        <div class="w-full max-w-md bg-surface-container-lowest border border-outline-variant rounded-xl shadow-lg p-8">

            <div class="mb-stack-sm">
                <a href="<%= ResolveUrl("~/Login.aspx") %>" class="inline-flex items-center gap-1 font-label-sm text-label-sm text-on-surface-variant hover:text-primary transition-colors">
                    <span class="material-symbols-outlined">arrow_back</span> Back to Login
                </a>
            </div>

            <div class="text-center mb-stack-md">
                <div class="flex justify-center items-center mb-stack-sm">
                    <img src='<%= ResolveUrl("~/Images/web/logo.png") %>' alt="LogistiCore Courier Service"
                         class="h-12 w-auto object-contain" />
                </div>
                <h2 class="font-headline-md text-headline-md text-on-surface">Forgot Password</h2>
            </div>

            <asp:Panel ID="pnlRequest" runat="server">
                <p class="font-body-md text-label-md text-on-surface-variant text-center mb-stack-md">
                    Enter your account email and we'll give you a temporary password.
                </p>

                <asp:Panel ID="pnlError" runat="server" Visible="false" CssClass="mb-stack-sm">
                    <asp:Label ID="lblStatus" runat="server" />
                </asp:Panel>

                <div class="space-y-4">
                    <div>
                        <label class="block font-label-sm text-label-sm text-on-surface-variant mb-1" for="<%= txtEmail.ClientID %>">Email Address</label>
                        <div class="relative">
                            <span class="material-symbols-outlined absolute left-3 top-1/2 -translate-y-1/2 text-on-surface-variant">mail</span>
                            <asp:TextBox ID="txtEmail" runat="server" TextMode="Email" placeholder="client@example.com" CssClass="fcl-input pl-10" />
                        </div>
                        <asp:RequiredFieldValidator ID="rfvEmail" runat="server" ControlToValidate="txtEmail"
                            Display="Dynamic" ValidationGroup="Forgot" ErrorMessage="Email address is required."
                            CssClass="block font-label-sm text-label-sm text-error mt-1" />
                    </div>

                    <asp:Button ID="btnRequest" runat="server" Text="Get Temporary Password" OnClick="btnRequest_Click"
                        ValidationGroup="Forgot" CssClass="fcl-btn w-full mt-2" />
                </div>

                <p class="text-center font-body-md text-label-sm text-on-surface-variant mt-stack-md">
                    Staff accounts are reset by an administrator.
                </p>
            </asp:Panel>

            <asp:Panel ID="pnlResult" runat="server" Visible="false">
                <p class="font-body-md text-label-md text-on-surface-variant text-center mb-stack-sm">
                    Here is your temporary password. Copy it now, because it won't be shown again.
                </p>

                <div class="bg-surface-container rounded-lg p-4 flex items-center justify-between gap-2 mb-stack-sm">
                    <code id="tempPassword" class="font-headline-md text-headline-md text-on-surface break-all select-all"><asp:Literal ID="litTemporary" runat="server" EnableViewState="false" /></code>
                    <button type="button" onclick="copyTemporary(this); return false;" aria-label="Copy temporary password"
                            class="text-on-surface-variant hover:text-primary shrink-0">
                        <span class="material-symbols-outlined">content_copy</span>
                    </button>
                </div>

                <ul class="font-body-md text-label-sm text-on-surface-variant list-disc pl-5 space-y-1 mb-stack-md">
                    <li>It expires at <strong><asp:Literal ID="litExpires" runat="server" EnableViewState="false" /></strong> (<%= ForteLogisticsApp.Services.AuthService.TemporaryPasswordMinutes %> minutes from now).</li>
                    <li>It works for one sign-in only.</li>
                    <li>You will choose a new password straight after you sign in.</li>
                </ul>

                <a class="fcl-btn w-full block text-center no-underline" href="<%= ResolveUrl("~/Login.aspx") %>">Go to Login</a>
            </asp:Panel>
        </div>
    </div>
</asp:Content>

<asp:Content ID="c2" ContentPlaceHolderID="ScriptContent" runat="server">
    <script>
        function copyTemporary(button) {
            var text = document.getElementById('tempPassword').textContent;
            var icon = button.querySelector('span');
            if (!navigator.clipboard) return;
            navigator.clipboard.writeText(text).then(function () { icon.textContent = 'check'; });
        }
    </script>
</asp:Content>
