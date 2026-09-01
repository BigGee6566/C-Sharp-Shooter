<%@ Page Title="Login" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="ForteLogisticsApp.LoginPage" %>

<asp:Content ID="c1" ContentPlaceHolderID="MainContent" runat="server">
   
    <script src="https://cdn.tailwindcss.com?plugins=forms,container-queries"></script>
    <link href="https://fonts.googleapis.com/css2?family=Hanken+Grotesk:wght@400;600;700&family=Inter:wght@400;500;600&display=swap" rel="stylesheet"/>
    <link href="https://fonts.googleapis.com/css2?family=Material+Symbols+Outlined:wght,FILL@100..700,0..1&display=swap" rel="stylesheet"/>

    <script id="tailwind-config">
        tailwind.config = {
            theme: {
                extend: {
                    colors: {
                        primary: "#002068",
                        "primary-container": "#003399",
                        "on-primary": "#ffffff",
                        surface: "#f9f9fc",
                        "surface-container": "#eeeef0",
                        "surface-container-lowest": "#ffffff",
                        "on-surface": "#1a1c1e",
                        "on-surface-variant": "#444653",
                        outline: "#747684",
                        "outline-variant": "#c4c5d5",
                        error: "#ba1a1a"
                    }
                }
            }
        }
    </script>
</head>
<body class="bg-surface text-on-surface antialiased flex items-center justify-center min-h-screen p-4">
    <form id="form1" runat="server" class="w-full max-w-md bg-surface-container-lowest border border-outline-variant rounded-2xl shadow-lg p-8 relative">
        
        <!-- Back to Home Button -->
        <div class="mb-4">
            <a href="HomePage.aspx" class="inline-flex items-center gap-1 text-xs font-semibold text-on-surface-variant hover:text-primary transition-colors">
                <span class="material-symbols-outlined text-base">arrow_back</span> Back to Home
            </a>
        </div>

        <!-- Header / Logo -->
        <div class="text-center mb-6">
            <div class="flex justify-center items-center gap-2 mb-2">
                <span class="material-symbols-outlined text-primary text-3xl">local_shipping</span>
                <span class="font-bold text-2xl text-primary font-headline-md">LogistiCore</span>
            </div>
            <h2 class="text-2xl font-bold text-on-surface">Welcome Back</h2>
            <p class="text-sm text-on-surface-variant mt-1">Sign in to manage your shipments and track logistics in real-time.</p>
        </div>

        <!-- Role Toggle (Client vs Staff) -->
        <asp:HiddenField ID="hfUserRole" runat="server" Value="Client" />
        <div class="bg-surface-container p-1 rounded-xl flex mb-6">
            <button type="button" id="btnRoleClient" onclick="selectRole('Client')" class="flex-1 py-2 text-sm font-semibold rounded-lg bg-white shadow-sm text-primary transition-all">
                Client Portal
            </button>
            <button type="button" id="btnRoleStaff" onclick="selectRole('Staff')" class="flex-1 py-2 text-sm font-semibold rounded-lg text-on-surface-variant hover:text-on-surface transition-all">
                Warehouse Staff
            </button>
        </div>

        <!-- Error Alert -->
        <asp:Panel ID="pnlError" runat="server" Visible="false" CssClass="mb-4 p-3 bg-red-50 border border-error/30 text-error rounded-lg text-sm flex items-center gap-2">
            <span class="material-symbols-outlined text-base">error</span>
            <asp:Literal ID="litErrorMessage" runat="server"></asp:Literal>
        </asp:Panel>

        <!-- Form Inputs -->
        <div class="space-y-4">
            <div>
                <label class="block text-xs font-semibold text-on-surface-variant mb-1">Email Address</label>
                <div class="relative">
                    <span class="material-symbols-outlined absolute left-3 top-1/2 -translate-y-1/2 text-on-surface-variant text-xl">mail</span>
                    <asp:TextBox ID="txtEmail" runat="server" TextMode="Email" Placeholder="client@example.com" CssClass="w-full pl-10 pr-4 py-2.5 bg-surface border border-outline-variant rounded-lg text-sm text-on-surface focus:border-primary focus:ring-1 focus:ring-primary outline-none"></asp:TextBox>
                </div>
            </div>

            <div>
                <label class="block text-xs font-semibold text-on-surface-variant mb-1">Password</label>
                <div class="relative">
                    <span class="material-symbols-outlined absolute left-3 top-1/2 -translate-y-1/2 text-on-surface-variant text-xl">lock</span>
                    <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" Placeholder="••••••••••••" CssClass="w-full pl-10 pr-10 py-2.5 bg-surface border border-outline-variant rounded-lg text-sm text-on-surface focus:border-primary focus:ring-1 focus:ring-primary outline-none"></asp:TextBox>
                    <button type="button" onclick="togglePasswordVisibility()" class="absolute right-3 top-1/2 -translate-y-1/2 text-on-surface-variant hover:text-on-surface focus:outline-none">
                        <span id="eyeIcon" class="material-symbols-outlined text-xl">visibility_off</span>
                    </button>
                </div>
            </div>

            <div class="flex justify-between items-center text-xs">
                <label class="flex items-center gap-2 text-on-surface-variant cursor-pointer">
                    <asp:CheckBox ID="chkRememberMe" runat="server" CssClass="rounded border-outline-variant text-primary focus:ring-primary" />
                    <span>Remember me</span>
                </label>
                <a href="#" class="text-primary font-semibold hover:underline">Forgot Password?</a>
            </div>

            <!-- Gradient Login Button -->
            <asp:Button ID="btnLogin" runat="server" Text="LOGIN" OnClick="btnLogin_Click" 
                CssClass="w-full bg-gradient-to-r from-[#001D6E] to-[#800040] hover:opacity-95 text-white font-bold tracking-wider py-3 rounded-lg text-sm shadow-md transition-all cursor-pointer mt-2" />
        </div>

        <div class="relative my-6 text-center">
            <div class="absolute inset-0 flex items-center"><div class="w-full border-t border-outline-variant"></div></div>
            <span class="relative bg-surface-container-lowest px-3 text-xs uppercase text-on-surface-variant">Or continue with</span>
        </div>

        <div class="grid grid-cols-2 gap-3">
            <button type="button" class="flex justify-center items-center gap-2 py-2 border border-outline-variant rounded-lg text-xs font-semibold hover:bg-surface transition-all">
                <img src="https://www.svgrepo.com/show/475656/google-color.svg" class="w-4 h-4" alt="Google"/> Google
            </button>
            <button type="button" class="flex justify-center items-center gap-2 py-2 border border-outline-variant rounded-lg text-xs font-semibold hover:bg-surface transition-all">
                <img src="https://www.svgrepo.com/show/448239/microsoft.svg" class="w-4 h-4" alt="Microsoft"/> Microsoft
            </button>
        </div>

        <p class="text-center text-xs text-on-surface-variant mt-6">
            By logging in, you agree to our <a href="#" class="text-primary hover:underline">Terms of Service</a> and <a href="#" class="text-primary hover:underline">Privacy Policy</a>.
        </p>
    </form>

    <script>
        function selectRole(role) {
            document.getElementById('<%= hfUserRole.ClientID %>').value = role;
            const btnClient = document.getElementById('btnRoleClient');
            const btnStaff = document.getElementById('btnRoleStaff');
            const emailInput = document.getElementById('<%= txtEmail.ClientID %>');

            if (role === 'Client') {
                btnClient.className = 'flex-1 py-2 text-sm font-semibold rounded-lg bg-white shadow-sm text-primary transition-all';
                btnStaff.className = 'flex-1 py-2 text-sm font-semibold rounded-lg text-on-surface-variant hover:text-on-surface transition-all';
                emailInput.placeholder = 'client@example.com';
            } else {
                btnStaff.className = 'flex-1 py-2 text-sm font-semibold rounded-lg bg-white shadow-sm text-primary transition-all';
                btnClient.className = 'flex-1 py-2 text-sm font-semibold rounded-lg text-on-surface-variant hover:text-on-surface transition-all';
                emailInput.placeholder = 'user@logisticore.com';
            }
        }

        function togglePasswordVisibility() {
            const pwdInput = document.getElementById('<%= txtPassword.ClientID %>');
            const eyeIcon = document.getElementById('eyeIcon');
            if (pwdInput.type === 'password') {
                pwdInput.type = 'text';
                eyeIcon.textContent = 'visibility';
            } else {
                pwdInput.type = 'password';
                eyeIcon.textContent = 'visibility_off';
            }
        }
    </script>
</body>
</asp:Content>
