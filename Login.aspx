<%@ Page Title="Login" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="ForteLogisticsApp.LoginPage" %>

<asp:Content ID="c1" ContentPlaceHolderID="MainContent" runat="server">

    <div class="flex items-center justify-center min-h-[70vh]">
        <div class="w-full max-w-md bg-surface-container-lowest border border-outline-variant rounded-xl shadow-lg p-8">

            <!-- Back to Home -->
            <div class="mb-stack-sm">
                <a href="<%= ResolveUrl("~/Default.aspx") %>" class="inline-flex items-center gap-1 font-label-sm text-label-sm text-on-surface-variant hover:text-primary transition-colors">
                    <span class="material-symbols-outlined">arrow_back</span> Back to Home
                </a>
            </div>

            <!-- Header / Logo -->
            <div class="text-center mb-stack-md">
                <div class="flex justify-center items-center mb-stack-sm">
                    <img src='<%= ResolveUrl("~/Images/web/logo.png") %>' alt="LogistiCore Courier Service"
                         class="h-12 w-auto object-contain" />
                </div>
                <h2 class="font-headline-md text-headline-md text-on-surface">Welcome Back</h2>
                <p class="font-body-md text-label-md text-on-surface-variant mt-1">Sign in to manage your shipments and track logistics in real time.</p>
            </div>

            <!-- Portal toggle. Advisory only: the account's real role comes from the database,
                 and the code-behind checks the signed-in role against the portal chosen here. -->
            <asp:HiddenField ID="hfUserRole" runat="server" Value="Client" />
            <div class="bg-surface-container p-1 rounded-xl flex mb-stack-md">
                <button type="button" id="btnRoleClient" onclick="selectRole('Client'); return false;"
                        class="flex-1 py-2 font-label-md text-label-md rounded-lg bg-surface-container-lowest shadow-sm text-primary transition-all">
                    Client Portal
                </button>
                <button type="button" id="btnRoleStaff" onclick="selectRole('Staff'); return false;"
                        class="flex-1 py-2 font-label-md text-label-md rounded-lg text-on-surface-variant hover:text-on-surface transition-all">
                    Staff Portal
                </button>
            </div>

            <!-- Status / error alert -->
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
                        Display="Dynamic" ValidationGroup="Login" ErrorMessage="Email address is required."
                        CssClass="block font-label-sm text-label-sm text-error mt-1" />
                </div>

                <div>
                    <label class="block font-label-sm text-label-sm text-on-surface-variant mb-1" for="<%= txtPassword.ClientID %>">Password</label>
                    <div class="relative">
                        <span class="material-symbols-outlined absolute left-3 top-1/2 -translate-y-1/2 text-on-surface-variant">lock</span>
                        <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" placeholder="Your password" CssClass="fcl-input pl-10 pr-10" />
                        <button type="button" onclick="togglePasswordVisibility(); return false;" aria-label="Show or hide password"
                                class="absolute right-3 top-1/2 -translate-y-1/2 text-on-surface-variant hover:text-on-surface focus:outline-none">
                            <span id="eyeIcon" class="material-symbols-outlined">visibility_off</span>
                        </button>
                    </div>
                    <asp:RequiredFieldValidator ID="rfvPassword" runat="server" ControlToValidate="txtPassword"
                        Display="Dynamic" ValidationGroup="Login" ErrorMessage="Password is required."
                        CssClass="block font-label-sm text-label-sm text-error mt-1" />
                </div>

                <asp:Button ID="btnLogin" runat="server" Text="Login" OnClick="btnLogin_Click" ValidationGroup="Login"
                    CssClass="fcl-btn w-full mt-2" />
            </div>

            <p class="text-center font-body-md text-label-sm text-on-surface-variant mt-stack-md">
                Don't have an account?
                <a class="fcl-link font-semibold" href="<%= ResolveUrl("~/Register.aspx") %>">Register here</a>.
            </p>
        </div>
    </div>
</asp:Content>

<asp:Content ID="c2" ContentPlaceHolderID="ScriptContent" runat="server">
    <script>
        function selectRole(role) {
            document.getElementById('<%= hfUserRole.ClientID %>').value = role;
            var btnClient = document.getElementById('btnRoleClient');
            var btnStaff = document.getElementById('btnRoleStaff');
            var email = document.getElementById('<%= txtEmail.ClientID %>');
            var on = 'flex-1 py-2 font-label-md text-label-md rounded-lg bg-surface-container-lowest shadow-sm text-primary transition-all';
            var off = 'flex-1 py-2 font-label-md text-label-md rounded-lg text-on-surface-variant hover:text-on-surface transition-all';

            if (role === 'Client') {
                btnClient.className = on; btnStaff.className = off;
                email.placeholder = 'client@example.com';
            } else {
                btnStaff.className = on; btnClient.className = off;
                email.placeholder = 'you@fcl.co.za';
            }
        }

        function togglePasswordVisibility() {
            var pwd = document.getElementById('<%= txtPassword.ClientID %>');
            var eye = document.getElementById('eyeIcon');
            if (pwd.type === 'password') { pwd.type = 'text'; eye.textContent = 'visibility'; }
            else { pwd.type = 'password'; eye.textContent = 'visibility_off'; }
        }
    </script>
</asp:Content>
