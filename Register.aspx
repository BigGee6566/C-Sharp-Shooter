<%@ Page Title="Register" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="Register.aspx.cs" Inherits="ForteLogisticsApp.RegisterPage" %>

<asp:Content ID="c1" ContentPlaceHolderID="MainContent" runat="server">
       <script src="https://cdn.tailwindcss.com?plugins=forms,container-queries"></script>
    <link href="https://fonts.googleapis.com/css2?family=Hanken+Grotesk:wght@400;600;700&family=Inter:wght@400;500;600&display=swap" rel="stylesheet">
    <link href="https://fonts.googleapis.com/css2?family=Material+Symbols+Outlined:wght,FILL@100..700,0..1" rel="stylesheet">

    <script id="tailwind-config">
        tailwind.config = {
            darkMode: "class",
            theme: {
                extend: {
                    colors: {
                        "primary": "#002068",
                        "primary-container": "#003399",
                        "secondary": "#b50062",
                        "background": "#f9f9fc",
                        "surface": "#f9f9fc",
                        "surface-container": "#eeeef0",
                        "surface-container-high": "#e8e8ea",
                        "surface-container-lowest": "#ffffff",
                        "on-surface": "#1a1c1e",
                        "on-surface-variant": "#444653",
                        "outline": "#747684",
                        "outline-variant": "#c4c5d5",
                        "error": "#ba1a1a"
                    },
                    borderRadius: {
                        "lg": "0.25rem",
                        "xl": "0.5rem",
                        "full": "0.75rem"
                    },
                    spacing: {
                        "stack-sm": "12px",
                        "stack-md": "24px",
                        "stack-lg": "48px",
                        "margin-desktop": "40px",
                        "margin-mobile": "16px",
                        "container-max": "1280px"
                    },
                    fontFamily: {
                        "headline-lg": ["Hanken Grotesk"],
                        "headline-md": ["Hanken Grotesk"],
                        "body-lg": ["Inter"],
                        "body-md": ["Inter"],
                        "label-md": ["Inter"],
                        "label-sm": ["Inter"]
                    }
                }
            }
        }
    </script>
    <style>
        .hero-glass {
            background: rgba(255, 255, 255, 0.7);
            backdrop-filter: blur(20px);
            -webkit-backdrop-filter: blur(20px);
            border: 1px solid rgba(255, 255, 255, 0.5);
            box-shadow: 0 8px 32px 0 rgba(0, 32, 104, 0.05);
        }
        .bg-pattern {
            background-image: radial-gradient(#e2e2e5 1px, transparent 1px);
            background-size: 24px 24px;
        }
        .gradient-btn {
            background: linear-gradient(135deg, #003399 0%, #e2007c 100%);
            transition: all 0.3s ease;
        }
        .gradient-btn:hover {
            box-shadow: 0 10px 20px -10px rgba(226, 0, 124, 0.5);
            transform: translateY(-1px);
        }
        .role-tab {
            transition: all 0.2s ease;
        }
        .role-tab.active {
            background-color: #ffffff;
            color: #002068;
            box-shadow: 0 2px 4px rgba(0,0,0,0.05);
            border-color: #e2e2e5;
        }
    </style>
</head>
<body class="bg-background bg-pattern text-on-surface font-body-md min-h-screen flex flex-col antialiased">

    <!-- Top Navigation -->
    <header class="w-full h-20 flex items-center justify-center absolute top-0 z-50">
        <a href="Default.aspx" class="text-headline-md font-headline-md font-bold text-primary flex items-center gap-2">
            <span class="material-symbols-outlined text-3xl">local_shipping</span> LogistiCore
        </a>
    </header>

    <!-- Main Content Area -->
    <main class="flex-grow flex items-center justify-center px-margin-mobile md:px-margin-desktop py-stack-lg relative mt-20">
        <div class="hero-glass rounded-xl w-full max-w-[560px] p-8 md:p-12 relative overflow-hidden z-10">
            
            <!-- Back to Home Button -->
            <div class="mb-4">
                <a href="HomePage.aspx" class="inline-flex items-center gap-1 text-xs font-semibold text-on-surface-variant hover:text-primary transition-colors">
                    <span class="material-symbols-outlined text-base">arrow_back</span> Back to Home
                </a>
            </div>

            <div class="text-center mb-stack-md">
                <h1 class="font-headline-lg text-2xl md:text-3xl text-on-surface mb-2 font-bold">Join LogistiCore</h1>
                <p class="font-body-lg text-on-surface-variant">Start shipping with precision and speed.</p>
            </div>

            <!-- Role Selector Tabs -->
            <div class="flex bg-surface-container-high rounded-lg p-1 mb-stack-md border border-outline-variant/30">
                <button type="button" class="role-tab active flex-1 py-2 px-4 rounded-md font-label-md text-sm font-semibold text-on-surface-variant text-center" id="tab-individual" onclick="switchRole('individual')">Individual</button>
                <button type="button" class="role-tab flex-1 py-2 px-4 rounded-md font-label-md text-sm font-semibold text-on-surface-variant text-center" id="tab-business" onclick="switchRole('business')">Business</button>
                <button type="button" class="role-tab flex-1 py-2 px-4 rounded-md font-label-md text-sm font-semibold text-on-surface-variant text-center" id="tab-staff" onclick="switchRole('staff')">Staff</button>
            </div>

            <!-- Social Signup -->
            <div class="flex flex-col gap-3 mb-stack-md">
                <button type="button" class="w-full flex items-center justify-center gap-3 py-3 px-4 border border-outline-variant rounded-lg bg-surface hover:bg-surface-container-high transition-colors font-label-md text-sm text-on-surface">
                    <img alt="Google" class="w-5 h-5" src="https://www.svgrepo.com/show/475656/google-color.svg">
                    Sign up with Google
                </button>
                <button type="button" class="w-full flex items-center justify-center gap-3 py-3 px-4 border border-outline-variant rounded-lg bg-surface hover:bg-surface-container-high transition-colors font-label-md text-sm text-on-surface">
                    <img alt="Microsoft" class="w-5 h-5" src="https://www.svgrepo.com/show/448239/microsoft.svg">
                    Sign up with Microsoft
                </button>
            </div>

            <div class="flex items-center gap-4 mb-stack-md">
                <div class="flex-1 h-px bg-outline-variant/50"></div>
                <span class="font-label-sm text-xs text-on-surface-variant uppercase tracking-wider">or sign up with email</span>
                <div class="flex-1 h-px bg-outline-variant/50"></div>
            </div>

            <!-- Registration Form -->
            <form id="form1" runat="server" class="flex flex-col gap-4">
                <asp:HiddenField ID="hfRole" runat="server" Value="individual" />

                <!-- Common Fields -->
                <div class="flex flex-col gap-1">
                    <label class="font-label-sm text-xs text-on-surface font-semibold" for="txtFullName">Full Name</label>
                    <asp:TextBox ID="txtFullName" runat="server" CssClass="w-full border border-outline-variant rounded-lg px-4 py-3 bg-surface focus:outline-none focus:border-primary-container focus:ring-1 focus:ring-primary-container transition-colors" Placeholder="Jane Doe"></asp:TextBox>
                </div>

                <div class="flex flex-col gap-1">
                    <label class="font-label-sm text-xs text-on-surface font-semibold" for="txtEmail">Email Address</label>
                    <asp:TextBox ID="txtEmail" runat="server" TextMode="Email" CssClass="w-full border border-outline-variant rounded-lg px-4 py-3 bg-surface focus:outline-none focus:border-primary-container focus:ring-1 focus:ring-primary-container transition-colors" Placeholder="jane@example.com"></asp:TextBox>
                </div>

                <div class="flex flex-col gap-1">
                    <label class="font-label-sm text-xs text-on-surface font-semibold" for="txtPhone">Phone Number</label>
                    <asp:TextBox ID="txtPhone" runat="server" TextMode="Phone" CssClass="w-full border border-outline-variant rounded-lg px-4 py-3 bg-surface focus:outline-none focus:border-primary-container focus:ring-1 focus:ring-primary-container transition-colors" Placeholder="+1 (555) 000-0000"></asp:TextBox>
                </div>

                <div class="flex flex-col gap-1">
                    <label class="font-label-sm text-xs text-on-surface font-semibold" for="txtPassword">Password</label>
                    <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" CssClass="w-full border border-outline-variant rounded-lg px-4 py-3 bg-surface focus:outline-none focus:border-primary-container focus:ring-1 focus:ring-primary-container transition-colors" Placeholder="••••••••"></asp:TextBox>
                </div>

                <!-- Business Fields -->
                <div class="hidden flex-col gap-4" id="business-fields">
                    <div class="flex flex-col gap-1">
                        <label class="font-label-sm text-xs text-on-surface font-semibold">Company Name</label>
                        <asp:TextBox ID="txtCompanyName" runat="server" CssClass="w-full border border-outline-variant rounded-lg px-4 py-3 bg-surface focus:outline-none focus:border-primary-container focus:ring-1 focus:ring-primary-container transition-colors" Placeholder="Acme Logistics"></asp:TextBox>
                    </div>
                    <div class="flex flex-col gap-1">
                        <label class="font-label-sm text-xs text-on-surface font-semibold">Industry</label>
                        <asp:DropDownList ID="ddlIndustry" runat="server" CssClass="w-full border border-outline-variant rounded-lg px-4 py-3 bg-surface focus:outline-none focus:border-primary-container focus:ring-1 focus:ring-primary-container transition-colors text-on-surface-variant">
                            <asp:ListItem Value="">Select Industry</asp:ListItem>
                            <asp:ListItem Value="ecommerce">E-commerce</asp:ListItem>
                            <asp:ListItem Value="manufacturing">Manufacturing</asp:ListItem>
                            <asp:ListItem Value="retail">Retail</asp:ListItem>
                            <asp:ListItem Value="other">Other</asp:ListItem>
                        </asp:DropDownList>
                    </div>
                    <div class="flex flex-col gap-1">
                        <label class="font-label-sm text-xs text-on-surface font-semibold">Estimated Monthly Volume</label>
                        <asp:DropDownList ID="ddlVolume" runat="server" CssClass="w-full border border-outline-variant rounded-lg px-4 py-3 bg-surface focus:outline-none focus:border-primary-container focus:ring-1 focus:ring-primary-container transition-colors text-on-surface-variant">
                            <asp:ListItem Value="">Select Volume</asp:ListItem>
                            <asp:ListItem Value="low">1 - 100 parcels</asp:ListItem>
                            <asp:ListItem Value="medium">101 - 1,000 parcels</asp:ListItem>
                            <asp:ListItem Value="high">1,000+ parcels</asp:ListItem>
                        </asp:DropDownList>
                    </div>
                </div>

                <!-- Staff Fields -->
                <div class="hidden flex-col gap-4" id="staff-fields">
                    <div class="flex flex-col gap-1">
                        <label class="font-label-sm text-xs text-on-surface font-semibold">Employee ID</label>
                        <asp:TextBox ID="txtEmployeeId" runat="server" CssClass="w-full border border-outline-variant rounded-lg px-4 py-3 bg-surface focus:outline-none focus:border-primary-container focus:ring-1 focus:ring-primary-container transition-colors" Placeholder="LC-00000"></asp:TextBox>
                    </div>
                    <div class="flex flex-col gap-1">
                        <label class="font-label-sm text-xs text-on-surface font-semibold">Work Email Address</label>
                        <asp:TextBox ID="txtWorkEmail" runat="server" TextMode="Email" CssClass="w-full border border-outline-variant rounded-lg px-4 py-3 bg-surface focus:outline-none focus:border-primary-container focus:ring-1 focus:ring-primary-container transition-colors" Placeholder="name@logisticore.com"></asp:TextBox>
                    </div>
                </div>

                <!-- Terms Checkbox -->
                <div class="flex items-start gap-3 mt-2">
                    <asp:CheckBox ID="chkTerms" runat="server" CssClass="w-4 h-4 rounded border-outline-variant text-primary-container focus:ring-primary-container mt-1" />
                    <label class="font-body-md text-sm text-on-surface-variant" for="chkTerms">
                        I agree to the <a class="text-primary-container hover:underline" href="#">Terms of Service</a> and <a class="text-primary-container hover:underline" href="#">Privacy Policy</a>
                    </label>
                </div>

                <!-- Error Message Label -->
                <asp:Label ID="lblError" runat="server" CssClass="text-error font-body-md text-xs font-semibold p-3 bg-red-50 rounded-lg border border-red-200 block" Visible="false"></asp:Label>

                <!-- Submit Button -->
                <asp:Button ID="btnRegister" runat="server" Text="CREATE ACCOUNT" OnClick="btnRegister_Click" CssClass="gradient-btn w-full py-4 mt-4 rounded-xl text-white font-label-md text-xs font-bold uppercase tracking-wider cursor-pointer" />
            </form>

            <!-- Footer Link -->
            <div class="mt-8 text-center">
                <p class="font-body-md text-sm text-on-surface-variant">
                    Already have an account? <a class="text-primary-container font-semibold hover:underline" href="Login.aspx">Log In</a>
                </p>
            </div>
        </div>
    </main>

    <!-- Footer -->
    <footer class="w-full py-8 px-margin-desktop grid grid-cols-1 md:grid-cols-4 gap-gutter max-w-container-max mx-auto bg-surface-container-lowest border-t border-outline-variant/30 mt-auto">
        <div class="col-span-1 md:col-span-4 flex flex-col md:flex-row justify-between items-center gap-4">
            <div class="text-headline-md font-headline-md font-bold text-primary flex items-center gap-2">
                <span class="material-symbols-outlined">local_shipping</span> LogistiCore
            </div>
            <div class="flex flex-wrap justify-center gap-6">
                <a class="text-xs text-on-surface-variant hover:text-primary transition-colors" href="#">Privacy Policy</a>
                <a class="text-xs text-on-surface-variant hover:text-primary transition-colors" href="#">Terms of Service</a>
                <a class="text-xs text-on-surface-variant hover:text-primary transition-colors" href="#">Carrier Terms</a>
                <a class="text-xs text-on-surface-variant hover:text-primary transition-colors" href="#">Global Network</a>
                <a class="text-xs text-on-surface-variant hover:text-primary transition-colors" href="#">24/7 Support</a>
            </div>
            <div class="text-xs text-on-surface-variant">
                © 2026 LogistiCore Global. All rights reserved.
            </div>
        </div>
    </footer>

    <script>
        function switchRole(role) {
            const indTab = document.getElementById('tab-individual');
            const busTab = document.getElementById('tab-business');
            const staffTab = document.getElementById('tab-staff');

            const busFields = document.getElementById('business-fields');
            const staffFields = document.getElementById('staff-fields');
            const hfRole = document.getElementById('<%= hfRole.ClientID %>');

            [indTab, busTab, staffTab].forEach(t => t.classList.remove('active'));
            busFields.classList.add('hidden');
            busFields.classList.remove('flex');
            staffFields.classList.add('hidden');
            staffFields.classList.remove('flex');

            hfRole.value = role;

            if (role === 'business') {
                busTab.classList.add('active');
                busFields.classList.remove('hidden');
                busFields.classList.add('flex');
            } else if (role === 'staff') {
                staffTab.classList.add('active');
                staffFields.classList.remove('hidden');
                staffFields.classList.add('flex');
            } else {
                indTab.classList.add('active');
            }
        }

        // Auto switch tab if URL contains ?role=business
        window.addEventListener('DOMContentLoaded', () => {
            const urlParams = new URLSearchParams(window.location.search);
            if (urlParams.get('role') === 'business') {
                switchRole('business');
            }
        });
    </script>
</body>
</asp:Content>
