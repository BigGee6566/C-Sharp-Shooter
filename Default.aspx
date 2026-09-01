<%@ Page Title="Home" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="ForteLogisticsApp.Default" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
        <title>LOGISTICORE | Forte Courier &amp; Logistics - Precision in Motion</title>
    
    <!-- Google Fonts -->
    <link href="https://fonts.googleapis.com" rel="preconnect"/>
    <link crossorigin="" href="https://fonts.gstatic.com" rel="preconnect"/>
    <link href="https://fonts.googleapis.com/css2?family=Hanken+Grotesk:wght@400;500;600&amp;family=JetBrains+Mono:wght@500&amp;family=Montserrat:wght@400;500;600;700;800;900&amp;display=swap" rel="stylesheet"/>
    
    <!-- Material Symbols -->
    <link href="https://fonts.googleapis.com/css2?family=Material+Symbols+Outlined:wght,FILL@100..700,0..1&amp;display=swap" rel="stylesheet"/>
    
    <!-- Tailwind CSS -->
    <script src="https://cdn.tailwindcss.com?plugins=forms,container-queries"></script>
    <script id="tailwind-config">
        tailwind.config = {
            darkMode: "class",
            theme: {
                extend: {
                    "colors": {
                        "secondary-fixed-dim": "#ffb2be",
                        "surface-tint": "#215abd",
                        "surface-container-lowest": "#ffffff",
                        "surface-container": "#ebeef0",
                        "error-container": "#ffdad6",
                        "secondary-container": "#e2165f",
                        "on-error": "#ffffff",
                        "background": "#f7fafc",
                        "on-primary": "#ffffff",
                        "on-primary-container": "#a9c1ff",
                        "on-tertiary-container": "#c3c1c0",
                        "surface-container-high": "#e5e9eb",
                        "secondary-fixed": "#ffd9de",
                        "on-tertiary-fixed-variant": "#474646",
                        "tertiary": "#393838",
                        "tertiary-fixed-dim": "#c8c6c5",
                        "secondary": "#b80049",
                        "on-tertiary-fixed": "#1c1b1b",
                        "primary-fixed-dim": "#b0c6ff",
                        "surface-bright": "#f7fafc",
                        "inverse-on-surface": "#eef1f3",
                        "surface-variant": "#e0e3e5",
                        "surface-container-low": "#f1f4f6",
                        "tertiary-fixed": "#e5e2e1",
                        "on-tertiary": "#ffffff",
                        "on-error-container": "#93000a",
                        "inverse-primary": "#b0c6ff",
                        "on-surface": "#181c1e",
                        "on-secondary-fixed-variant": "#900038",
                        "on-primary-fixed-variant": "#00429b",
                        "error": "#ba1a1a",
                        "on-secondary": "#ffffff",
                        "surface-container-highest": "#e0e3e5",
                        "on-surface-variant": "#434653",
                        "on-secondary-fixed": "#400014",
                        "primary": "#00357f",
                        "outline": "#737784",
                        "surface-dim": "#d7dadc",
                        "outline-variant": "#c3c6d5",
                        "tertiary-container": "#504f4f",
                        "surface": "#f7fafc",
                        "primary-fixed": "#d9e2ff",
                        "primary-container": "#004aad",
                        "inverse-surface": "#2d3133",
                        "on-primary-fixed": "#001945",
                        "on-secondary-container": "#fffbff",
                        "on-background": "#181c1e"
                    },
                    "borderRadius": {
                        "DEFAULT": "0.125rem",
                        "lg": "0.25rem",
                        "xl": "0.5rem",
                        "full": "0.75rem"
                    },
                    "spacing": {
                        "container-max": "1280px",
                        "margin-desktop": "40px",
                        "margin-mobile": "16px",
                        "unit": "8px",
                        "gutter": "24px",
                        "glass-padding": "20px"
                    },
                    "fontFamily": {
                        "headline-md": ["Montserrat"],
                        "headline-lg": ["Montserrat"],
                        "body-lg": ["Montserrat"],
                        "body-md": ["Montserrat"],
                        "label-bold": ["Montserrat"],
                        "headline-xl": ["Montserrat"],
                        "headline-lg-mobile": ["Montserrat"],
                        "label-sm": ["Montserrat"],
                        "display-lg": ["Montserrat"]
                    },
                    "fontSize": {
                        "headline-md": ["24px", { "lineHeight": "32px", "fontWeight": "600" }],
                        "headline-lg": ["32px", { "lineHeight": "40px", "letterSpacing": "-0.01em", "fontWeight": "700" }],
                        "body-lg": ["18px", { "lineHeight": "28px", "fontWeight": "400" }],
                        "body-md": ["16px", { "lineHeight": "24px", "fontWeight": "400" }],
                        "label-bold": ["14px", { "lineHeight": "20px", "fontWeight": "700" }],
                        "headline-xl": ["48px", { "lineHeight": "56px", "letterSpacing": "-0.02em", "fontWeight": "800" }],
                        "headline-lg-mobile": ["28px", { "lineHeight": "36px", "fontWeight": "700" }],
                        "label-sm": ["12px", { "lineHeight": "16px", "fontWeight": "500" }],
                        "display-lg": ["56px", { "lineHeight": "1.1", "letterSpacing": "-0.02em", "fontWeight": "900" }]
                    }
                },
            },
        }
    </script>
    <style>
        .material-symbols-outlined {
            font-variation-settings: 'FILL' 0, 'wght' 400, 'GRAD' 0, 'opsz' 24;
        }
        
        .glass-panel {
            backdrop-filter: blur(20px);
            background: rgba(255, 255, 255, 0.7);
            border: 1px solid rgba(0, 53, 127, 0.1);
        }

        .hero-glass {
            backdrop-filter: blur(40px);
            background: linear-gradient(135deg, rgba(255, 255, 255, 0.9) 0%, rgba(247, 250, 252, 0.8) 100%);
            border: 1px solid rgba(0, 53, 127, 0.15);
            box-shadow: 0 25px 50px -12px rgba(0, 53, 127, 0.1);
        }

        .primary-gradient-btn {
            background: linear-gradient(45deg, #004aad, #e2165f);
            transition: all 0.3s ease;
        }

        .primary-gradient-btn:hover {
            opacity: 0.9;
            transform: translateY(-2px);
            box-shadow: 0 0 20px rgba(0, 74, 173, 0.3);
        }

        .logo-gradient {
            background: linear-gradient(90deg, #004aad 0%, #e2165f 100%);
            background-clip: text;
            -webkit-background-clip: text;
            -webkit-text-fill-color: transparent;
        }

        .tab-active {
            border-bottom: 2px solid #004aad;
            color: #004aad;
        }

        .no-scrollbar::-webkit-scrollbar { display: none; }
        .no-scrollbar { -ms-overflow-style: none; scrollbar-width: none; }

        @keyframes pulse-soft {
            0%, 100% { opacity: 1; }
            50% { opacity: 0.8; }
        }
        .animate-pulse-soft { animation: pulse-soft 3s infinite ease-in-out; }
    </style>
</head>
<body class="bg-surface text-on-surface selection:bg-primary selection:text-on-primary">
    <form id="form1" runat="server">
        <!-- Utility Bar -->
        <div class="bg-surface-container-lowest w-full h-8 flex items-center justify-end gap-gutter px-margin-desktop border-b border-outline-variant/30">
            <div class="flex gap-6 font-label-sm text-label-sm text-outline">
                <span class="cursor-pointer hover:text-secondary transition-colors">+1 (800) FORTE-LOG</span>
                <span class="cursor-pointer hover:text-secondary transition-colors">support@fortecourier.com</span>
                <span class="cursor-pointer hover:text-secondary transition-colors">Global Network</span>
            </div>
        </div>

        <!-- TopNavBar -->
        <header class="fixed top-8 w-full z-50 bg-surface/90 backdrop-blur-xl border-b border-outline-variant/20 shadow-sm transition-all duration-300 ease-in-out">
            <div class="flex justify-between items-center px-margin-desktop py-4 max-w-[1440px] mx-auto">
                <div class="font-display-lg text-headline-md tracking-tighter text-primary">
                    <img alt="LOGISTICORE" class="h-10 object-contain" src="https://lh3.googleusercontent.com/aida-public/AB6AXuA6P8N-MGJmHsSPiDVb87YafUt3HPsXeDhLL_qN_VEKHZyc9CyaSbF-73btkeMmjmh7ljbGLQRF_1DZPqOHzP4llUvTq82IomQbDVC8KUEKvBu92XriInhLnDLYvD4hdTJFbH9Yh-duiXGe5PNmcBEFED8VHYxG9uRgw-_iMtlkk95Cxc0lTBG7AVwbOOoM5jnYFAaWcLapEOtyDNNmB0CvEbeRDrJfgd0wu6qD3DY846KeIuteorYLy5zPy0W_5uAgTsI"/>
                </div>
                <!-- Navigation links with section targets -->
                <nav class="hidden md:flex items-center gap-8 font-body-md text-body-md">
                    <a class="text-on-surface-variant hover:text-primary transition-colors" href="#services">Services</a>
                    <a class="text-on-surface-variant hover:text-primary transition-colors" href="#solutions">Solutions</a>
                    <a class="text-on-surface-variant hover:text-primary transition-colors" href="#hero-tool" onclick="switchTab('track')">Track Parcel</a>
                    <a class="text-on-surface-variant hover:text-primary transition-colors" href="#hero-tool" onclick="switchTab('quote')">Get a Quote</a>
                    <a class="text-on-surface-variant hover:text-primary transition-colors" href="#contact">Contact Us</a>
                </nav>
                <div class="flex gap-4 items-center">
                    <a href="Login.aspx" class="px-6 py-2 text-primary font-bold uppercase tracking-widest text-label-sm hover:text-secondary transition-colors flex items-center">
                        Login
                    </a>
                    <a href="Register.aspx" class="px-6 py-2 primary-gradient-btn text-white font-bold uppercase tracking-widest text-label-sm rounded-lg inline-flex items-center justify-center">
                        Register
                    </a>
                </div>
            </div>
        </header>

        <!-- Main Content -->
        <main class="pt-24">
            <!-- Hero Section -->
            <section class="relative min-h-[921px] flex items-center px-margin-desktop overflow-hidden">
                <div class="absolute inset-0 z-0">
                    <div class="absolute inset-0 bg-gradient-to-t from-surface via-surface/80 to-surface/40"></div>
                    <div class="absolute inset-0 bg-cover bg-center mix-blend-multiply opacity-10"></div>
                </div>
                <div class="relative z-10 grid grid-cols-1 lg:grid-cols-2 gap-20 w-full max-w-[1440px] mx-auto">
                    <div class="flex flex-col justify-center">
                        <h1 class="font-display-lg text-display-lg text-on-surface mb-6">
                            PRECISION <br/> <span class="logo-gradient">IN MOTION.</span>
                        </h1>
                        <p class="font-body-lg text-body-lg text-on-surface-variant max-w-lg mb-10">
                            Next-gen courier and logistics infrastructure designed for global enterprises. We bridge the gap between complex supply chains and rapid-response delivery.
                        </p>
                        <div class="flex gap-4">
                            <a href="#services" class="px-8 py-4 primary-gradient-btn text-white font-bold uppercase tracking-widest text-label-sm rounded-xl flex items-center gap-2">
                                Explore Services <span class="material-symbols-outlined">arrow_forward</span>
                            </a>
                            <a href="#solutions" class="px-8 py-4 border border-outline text-primary font-bold uppercase tracking-widest text-label-sm rounded-xl hover:bg-primary/5 transition-all flex items-center">
                                View Solutions
                            </a>
                        </div>
                    </div>

                    <!-- Hero Tool (Tabs Target Section) -->
                    <div class="flex flex-col justify-center" id="hero-tool">
                        <div class="hero-glass p-8 rounded-3xl w-full max-w-xl self-end">
                            <div class="flex border-b border-outline-variant/30 mb-8">
                                <button type="button" class="flex-1 pb-4 font-headline-md text-headline-md tab-active transition-all" id="tab-track" onclick="switchTab('track')">Track Parcel</button>
                                <button type="button" class="flex-1 pb-4 font-headline-md text-headline-md text-outline hover:text-primary transition-all" id="tab-quote" onclick="switchTab('quote')">Get Quote</button>
                            </div>

                            <!-- Track Form -->
                            <div class="space-y-6" id="form-track">
                                <div>
                                    <label class="font-label-sm text-label-sm text-outline mb-2 block uppercase tracking-widest">Waybill / Tracking Number</label>
                                    <asp:TextBox ID="txtTrackingNumber" runat="server" CssClass="w-full bg-surface-container-lowest border border-outline-variant rounded-xl p-4 text-on-surface focus:outline-none focus:border-primary focus:ring-1 focus:ring-primary font-label-sm text-lg uppercase" placeholder="e.g. FORTE-9283-492" />
                                </div>
                                <button type="button" class="w-full py-4 primary-gradient-btn text-white font-bold uppercase tracking-widest rounded-xl flex justify-center items-center gap-2">
                                    <span class="material-symbols-outlined">my_location</span> Track Shipment
                                </button>
                                <asp:Label ID="lblTrackResult" runat="server" CssClass="block text-center font-label-sm text-sm text-primary font-bold" />
                                <p class="text-center text-outline font-label-sm text-xs">Tracking global shipments across 180+ countries in real-time.</p>
                            </div>

                            <!-- Quote Form -->
                            <div class="hidden space-y-6" id="form-quote">
                                <div class="grid grid-cols-2 gap-4">
                                    <div>
                                        <label class="font-label-sm text-label-sm text-outline mb-2 block uppercase">Origin</label>
                                        <asp:DropDownList ID="ddlOrigin" runat="server" CssClass="w-full bg-surface-container-lowest border border-outline-variant rounded-xl p-4 text-on-surface focus:outline-none focus:border-primary font-body-md appearance-none">
                                            <asp:ListItem Text="New York, USA" Value="NY" />
                                            <asp:ListItem Text="London, UK" Value="LDN" />
                                            <asp:ListItem Text="Singapore" Value="SG" />
                                        </asp:DropDownList>
                                    </div>
                                    <div>
                                        <label class="font-label-sm text-label-sm text-outline mb-2 block uppercase">Destination</label>
                                        <asp:DropDownList ID="ddlDestination" runat="server" CssClass="w-full bg-surface-container-lowest border border-outline-variant rounded-xl p-4 text-on-surface focus:outline-none focus:border-primary font-body-md appearance-none">
                                            <asp:ListItem Text="Dubai, UAE" Value="DXB" />
                                            <asp:ListItem Text="Berlin, Germany" Value="BER" />
                                            <asp:ListItem Text="Tokyo, Japan" Value="TYO" />
                                        </asp:DropDownList>
                                    </div>
                                </div>
                                <div>
                                    <label class="font-label-sm text-label-sm text-outline mb-2 block uppercase">Estimated Weight (KG)</label>
                                    <asp:TextBox ID="txtWeight" runat="server" TextMode="Number" CssClass="w-full bg-surface-container-lowest border border-outline-variant rounded-xl p-4 text-on-surface focus:outline-none focus:border-primary font-label-sm" placeholder="0.00" />
                                </div>
                                <button type="button" class="w-full py-4 primary-gradient-btn text-white font-bold uppercase tracking-widest rounded-xl">Calculate Quote</button>
                                <asp:Label ID="lblQuoteResult" runat="server" CssClass="block text-center font-label-sm text-sm text-primary font-bold" />
                            </div>
                        </div>
                    </div>
                </div>
            </section>

            <!-- Stats Section -->
            <section class="py-24 bg-surface-container-lowest relative">
                <div class="max-w-[1440px] mx-auto px-margin-desktop">
                    <div class="grid grid-cols-2 md:grid-cols-4 gap-12 text-on-surface">
                        <div class="flex flex-col items-center text-center">
                            <span class="font-display-lg text-display-lg mb-2 text-primary">45+</span>
                            <span class="font-label-sm text-label-sm uppercase tracking-tighter text-outline">Operational Hubs</span>
                        </div>
                        <div class="flex flex-col items-center text-center">
                            <span class="font-display-lg text-display-lg mb-2 text-primary">1800+</span>
                            <span class="font-label-sm text-label-sm uppercase tracking-tighter text-outline">Global Fleet</span>
                        </div>
                        <div class="flex flex-col items-center text-center">
                            <span class="font-display-lg text-display-lg mb-2 text-primary">99.9%</span>
                            <span class="font-label-sm text-label-sm uppercase tracking-tighter text-outline">On-Time Precision</span>
                        </div>
                        <div class="flex flex-col items-center text-center">
                            <span class="font-display-lg text-display-lg mb-2 text-primary">24/7</span>
                            <span class="font-label-sm text-label-sm uppercase tracking-tighter text-outline">Command Center</span>
                        </div>
                    </div>
                </div>
            </section>

            <!-- Carousel Banner -->
            <section class="relative h-[600px] w-full overflow-hidden">
                <div class="flex transition-transform duration-700 ease-in-out h-full" id="carousel">
                    <div class="min-w-full h-full relative">
                        <div class="absolute inset-0 bg-cover bg-center" style="background-image: url('https://lh3.googleusercontent.com/aida-public/AB6AXuD4iDuA4PKxQDL8SwNQ_5WNYEtzsUYoqDSEsCok5LkPmvsqGNo8H7YuuxKKRzRpH5g9varrX0ipRH1jW663hxM9QzV8V8_YcIqMnkYafTnYzECs9Vui7Jy5iIB0h_qsXqNVlV_GCJOeYX0lzuUaP5mj31MiuO8LNPFsT2oWk88QFXO-hJ9teQX3TqfCam_42xkpcIDmwy2gYzaDPdNU-nbm2ok0UTiEblw-sWftWSotYrnF3_Nt7tkTwg')"></div>
                        <div class="absolute inset-0 bg-gradient-to-r from-surface to-transparent flex items-center px-margin-desktop">
                            <div class="max-w-xl">
                                <h2 class="font-display-lg text-display-lg text-on-surface mb-4 uppercase">Fleet Excellence</h2>
                                <p class="font-body-lg text-body-lg text-on-surface-variant mb-8">Operating the world's most advanced air and ground fleet for mission-critical shipments.</p>
                                <a href="#services" class="px-8 py-3 border-2 border-primary text-primary font-bold uppercase rounded-lg hover:bg-primary/10 inline-block">Learn More</a>
                            </div>
                        </div>
                    </div>
                </div>
            </section>

            <!-- Services Grid Target Section -->
            <section id="services" class="py-32 px-margin-desktop max-w-[1440px] mx-auto">
                <div class="flex justify-between items-end mb-20">
                    <div class="max-w-2xl">
                        <h2 class="font-display-lg text-headline-lg text-on-surface mb-4 uppercase tracking-tight">Enterprise Logistics <span class="logo-gradient">Services</span></h2>
                        <p class="font-body-lg text-body-lg text-on-surface-variant">Scalable solutions from warehouse storage to door-to-door distribution, integrated into a single glass-pane command center.</p>
                    </div>
                    <div class="hidden md:block">
                        <span class="material-symbols-outlined text-display-lg text-outline-variant/30">grid_view</span>
                    </div>
                </div>
                <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-gutter">
                    <!-- Service Card 1 -->
                    <div class="glass-panel p-glass-padding rounded-2xl flex flex-col group cursor-pointer hover:border-primary/40 transition-all bg-surface-container-lowest">
                        <div class="h-48 w-full rounded-xl mb-6 overflow-hidden relative bg-surface-container">
                            <div class="absolute inset-0 bg-cover bg-center group-hover:scale-110 transition-transform duration-500"></div>
                            <div class="absolute inset-0 bg-gradient-to-t from-surface-container to-transparent opacity-50"></div>
                            <div class="absolute top-4 left-4 bg-primary/10 backdrop-blur-md p-2 rounded-lg border border-primary/20">
                                <span class="material-symbols-outlined text-primary">package_2</span>
                            </div>
                        </div>
                        <h3 class="font-headline-md text-headline-md text-on-surface mb-3">Parcel Collection</h3>
                        <p class="font-body-md text-body-md text-on-surface-variant mb-6 flex-grow">Automated scheduling and pickup optimization for high-volume corporate senders.</p>
                        <button type="button" class="w-full py-3 bg-surface hover:bg-primary-container text-primary hover:text-white font-bold uppercase text-label-sm rounded-lg transition-all border border-outline-variant/30" onclick="openModal('parcel')">View Full Workflow</button>
                    </div>
                    <!-- Service Card 2 -->
                    <div class="glass-panel p-glass-padding rounded-2xl flex flex-col group cursor-pointer hover:border-primary/40 transition-all bg-surface-container-lowest">
                        <div class="h-48 w-full rounded-xl mb-6 overflow-hidden relative bg-surface-container">
                            <div class="absolute inset-0 bg-cover bg-center group-hover:scale-110 transition-transform duration-500"></div>
                            <div class="absolute inset-0 bg-gradient-to-t from-surface-container to-transparent opacity-50"></div>
                            <div class="absolute top-4 left-4 bg-secondary/10 backdrop-blur-md p-2 rounded-lg border border-secondary/20">
                                <span class="material-symbols-outlined text-secondary">warehouse</span>
                            </div>
                        </div>
                        <h3 class="font-headline-md text-headline-md text-on-surface mb-3">Warehousing</h3>
                        <p class="font-body-md text-body-md text-on-surface-variant mb-6 flex-grow">Smart fulfillment centers with real-time inventory tracking and multi-node routing.</p>
                        <button type="button" class="w-full py-3 bg-surface hover:bg-primary-container text-primary hover:text-white font-bold uppercase text-label-sm rounded-lg transition-all border border-outline-variant/30" onclick="openModal('warehousing')">View Full Workflow</button>
                    </div>
                    <!-- Service Card 3 -->
                    <div class="glass-panel p-glass-padding rounded-2xl flex flex-col group cursor-pointer hover:border-primary/40 transition-all bg-surface-container-lowest">
                        <div class="h-48 w-full rounded-xl mb-6 overflow-hidden relative bg-surface-container">
                            <div class="absolute inset-0 bg-cover bg-center group-hover:scale-110 transition-transform duration-500"></div>
                            <div class="absolute inset-0 bg-gradient-to-t from-surface-container to-transparent opacity-50"></div>
                            <div class="absolute top-4 left-4 bg-tertiary-container/10 backdrop-blur-md p-2 rounded-lg border border-tertiary-container/20">
                                <span class="material-symbols-outlined text-tertiary">local_shipping</span>
                            </div>
                        </div>
                        <h3 class="font-headline-md text-headline-md text-on-surface mb-3">Delivery Services</h3>
                        <p class="font-body-md text-body-md text-on-surface-variant mb-6 flex-grow">Last-mile precision with predictive arrival windows and secure proof-of-delivery.</p>
                        <button type="button" class="w-full py-3 bg-surface hover:bg-primary-container text-primary hover:text-white font-bold uppercase text-label-sm rounded-lg transition-all border border-outline-variant/30" onclick="openModal('delivery')">View Full Workflow</button>
                    </div>
                </div>
            </section>

            <!-- Corporate / Solutions Target Section -->
            <section id="solutions" class="py-24 bg-surface-container">
                <div class="max-w-[1440px] mx-auto px-margin-desktop">
                    <div class="glass-panel p-12 rounded-3xl flex flex-col lg:flex-row items-center justify-between gap-12 overflow-hidden relative bg-surface-container-lowest">
                        <div class="absolute top-0 right-0 w-1/3 h-full opacity-5">
                            <span class="material-symbols-outlined text-[300px] translate-x-1/2 translate-y-1/4 text-primary">corporate_fare</span>
                        </div>
                        <div class="relative z-10 max-w-2xl">
                            <h2 class="font-display-lg text-headline-lg text-on-surface mb-4 uppercase">Corporate Solutions</h2>
                            <p class="font-body-lg text-body-lg text-on-surface-variant mb-0">Experience priority dispatch, bulk pricing, and dedicated account managers. Scale your logistics with our API-integrated platform.</p>
                        </div>
                        <div class="relative z-10">
                            <a href="Register.aspx?role=business" class="px-12 py-5 primary-gradient-btn text-white font-bold uppercase tracking-widest text-label-sm rounded-xl inline-flex items-center justify-center">
                                Register Business Account
                            </a>
                        </div>
                    </div>
                </div>
            </section>
        </main>

        <!-- Footer / Contact Us Target Section -->
        <footer id="contact" class="w-full py-12 border-t border-outline-variant/30 bg-surface-container-lowest">
            <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-gutter px-margin-desktop max-w-[1440px] mx-auto">
                <div class="col-span-1 md:col-span-2 lg:col-span-1">
                    <div class="mb-6">
                        <img alt="LOGISTICORE" class="h-8 object-contain" src="https://lh3.googleusercontent.com/aida-public/AB6AXuAXrTknoGMdU9oqiRf-Ae1rHOe_yKL263ej4vYtOUIu7UZaKLdTayvUNzya2_Pdel1w19QuynKDWYPYhlKGRx3BueuRdu6DYKChAgqmKD4GhL8BZw90YfFkSpgMajwpJnYYKhYSx3PCllkXtdMRiC8RUJ5toueA_yxuT3SdFN2nZF8hFsScK5lCJE-jorSSdtcIal5ZWdLeYwI5pDPAJ5JAhEo8uBHlvf-4hlncAGEmkHhzJcODsFlFYfzRXqfTY6jV6uM"/>
                    </div>
                    <p class="font-body-md text-body-md text-on-surface-variant mb-8 max-w-xs">Precision engineering for global movement. The backbone of your supply chain.</p>
                    <div class="flex gap-4">
                        <span class="material-symbols-outlined text-outline hover:text-primary cursor-pointer transition-colors">hub</span>
                        <span class="material-symbols-outlined text-outline hover:text-primary cursor-pointer transition-colors">satellite_alt</span>
                        <span class="material-symbols-outlined text-outline hover:text-primary cursor-pointer transition-colors">public</span>
                    </div>
                </div>
                <div>
                    <h4 class="font-headline-md text-headline-md text-on-surface mb-6">Quick Links</h4>
                    <ul class="space-y-3 font-body-md text-body-md">
                        <li><a class="text-on-surface-variant hover:text-primary transition-all" href="#services">Services</a></li>
                        <li><a class="text-on-surface-variant hover:text-primary transition-all" href="#solutions">Solutions</a></li>
                        <li><a class="text-on-surface-variant hover:text-primary transition-all" href="#hero-tool" onclick="switchTab('track')">Track Parcel</a></li>
                        <li><a class="text-on-surface-variant hover:text-primary transition-all" href="#hero-tool" onclick="switchTab('quote')">Get a Quote</a></li>
                    </ul>
                </div>
                <div>
                    <h4 class="font-headline-md text-headline-md text-on-surface mb-6">Support</h4>
                    <ul class="space-y-3 font-body-md text-body-md">
                        <li><a class="text-on-surface-variant hover:text-primary transition-all" href="#contact">Contact Us</a></li>
                        <li><a class="text-on-surface-variant hover:text-primary transition-all" href="#">Privacy Policy</a></li>
                        <li><a class="text-on-surface-variant hover:text-primary transition-all" href="#">Terms of Service</a></li>
                        <li><a class="text-on-surface-variant hover:text-primary transition-all" href="#">Compliance</a></li>
                    </ul>
                </div>
                <div>
                    <h4 class="font-headline-md text-headline-md text-on-surface mb-6">Join Us</h4>
                    <p class="font-body-md text-body-md text-on-surface-variant mb-4">Are you a fleet owner?</p>
                    <a class="text-primary font-bold uppercase tracking-widest text-label-sm border-b-2 border-primary/20 hover:border-primary transition-all" href="#">Carrier Portal</a>
                </div>
            </div>
            <div class="max-w-[1440px] mx-auto px-margin-desktop mt-20 pt-8 border-t border-outline-variant/30 text-center">
                <p class="font-body-md text-body-md text-on-surface-variant">© 2024 Forte Courier &amp; Logistics. All Rights Reserved. Precision in Motion.</p>
            </div>
        </footer>

        <!-- Service Detail Modal -->
        <div class="fixed inset-0 z-[100] hidden flex items-center justify-center p-4" id="serviceModal">
            <div class="absolute inset-0 bg-surface/80 backdrop-blur-md" onclick="closeModal()"></div>
            <div class="bg-surface-container-lowest max-w-4xl w-full max-h-[921px] rounded-3xl relative z-10 overflow-hidden flex flex-col shadow-2xl border border-outline-variant/20">
                <div class="p-8 border-b border-outline-variant/30 flex justify-between items-center">
                    <h2 class="font-display-lg text-headline-lg text-on-surface uppercase" id="modalTitle">Service Details</h2>
                    <button type="button" class="text-on-surface-variant hover:text-on-surface transition-colors" onclick="closeModal()">
                        <span class="material-symbols-outlined text-4xl">close</span>
                    </button>
                </div>
                <div class="p-8 overflow-y-auto no-scrollbar">
                    <div class="grid grid-cols-1 md:grid-cols-2 gap-12 mb-12">
                        <div>
                            <h3 class="font-headline-md text-headline-md text-primary mb-4">Core Features</h3>
                            <ul class="space-y-4 font-body-md text-body-md text-on-surface-variant">
                                <li class="flex gap-3"><span class="material-symbols-outlined text-secondary">check_circle</span> Real-time telemetry integration</li>
                                <li class="flex gap-3"><span class="material-symbols-outlined text-secondary">check_circle</span> Predictive AI route optimization</li>
                                <li class="flex gap-3"><span class="material-symbols-outlined text-secondary">check_circle</span> Secure multi-factor authentication for recipients</li>
                                <li class="flex gap-3"><span class="material-symbols-outlined text-secondary">check_circle</span> Automated digital manifest generation</li>
                            </ul>
                        </div>
                        <div>
                            <h3 class="font-headline-md text-headline-md text-tertiary mb-4">Service Timeline</h3>
                            <div class="space-y-4">
                                <div class="flex gap-4">
                                    <div class="font-label-sm text-outline w-24">Immediate</div>
                                    <div class="text-on-surface font-body-md">Pickup Request Confirmation</div>
                                </div>
                                <div class="flex gap-4">
                                    <div class="font-label-sm text-outline w-24">T + 2 hrs</div>
                                    <div class="text-on-surface font-body-md">Fleet Dispatch &amp; Sorting</div>
                                </div>
                                <div class="flex gap-4">
                                    <div class="font-label-sm text-outline w-24">T + 6 hrs</div>
                                    <div class="text-on-surface font-body-md">Hub Processing &amp; Linehaul</div>
                                </div>
                            </div>
                        </div>
                    </div>
                    <div class="bg-surface-container p-8 rounded-2xl border border-outline-variant/20">
                        <h3 class="font-headline-md text-headline-md text-on-surface mb-8 text-center">4-Step Precision Workflow</h3>
                        <div class="grid grid-cols-1 md:grid-cols-4 gap-8">
                            <div class="text-center">
                                <div class="w-12 h-12 bg-primary/10 rounded-full flex items-center justify-center mx-auto mb-4 border border-primary/20 text-primary font-bold">1</div>
                                <p class="font-label-sm text-label-sm uppercase text-on-surface">Injection</p>
                            </div>
                            <div class="text-center">
                                <div class="w-12 h-12 bg-secondary/10 rounded-full flex items-center justify-center mx-auto mb-4 border border-secondary/20 text-secondary font-bold">2</div>
                                <p class="font-label-sm text-label-sm uppercase text-on-surface">Classification</p>
                            </div>
                            <div class="text-center">
                                <div class="w-12 h-12 bg-tertiary/10 rounded-full flex items-center justify-center mx-auto mb-4 border border-tertiary/20 text-tertiary font-bold">3</div>
                                <p class="font-label-sm text-label-sm uppercase text-on-surface">Transit</p>
                            </div>
                            <div class="text-center">
                                <div class="w-12 h-12 bg-on-surface-variant/10 rounded-full flex items-center justify-center mx-auto mb-4 border border-outline/20 text-on-surface font-bold">4</div>
                                <p class="font-label-sm text-label-sm uppercase text-on-surface">Validation</p>
                            </div>
                        </div>
                    </div>
                </div>
                <div class="p-8 border-t border-outline-variant/30 flex justify-end bg-surface">
                    <button type="button" class="px-8 py-3 primary-gradient-btn text-white font-bold uppercase rounded-lg">Get Started with this Service</button>
                </div>
            </div>
        </div>
    </form>

    <script>
        // Tab switching logic
        function switchTab(type) {
            const trackBtn = document.getElementById('tab-track');
            const quoteBtn = document.getElementById('tab-quote');
            const trackForm = document.getElementById('form-track');
            const quoteForm = document.getElementById('form-quote');

            if (type === 'track') {
                trackBtn.classList.add('tab-active');
                trackBtn.classList.remove('text-outline');
                quoteBtn.classList.remove('tab-active');
                quoteBtn.classList.add('text-outline');
                trackForm.classList.remove('hidden');
                quoteForm.classList.add('hidden');
            } else {
                quoteBtn.classList.add('tab-active');
                quoteBtn.classList.remove('text-outline');
                trackBtn.classList.remove('tab-active');
                trackBtn.classList.add('text-outline');
                quoteForm.classList.remove('hidden');
                trackForm.classList.add('hidden');
            }
        }

        // Modal logic
        function openModal(service) {
            const modal = document.getElementById('serviceModal');
            modal.classList.remove('hidden');
            document.body.style.overflow = 'hidden';
        }

        function closeModal() {
            const modal = document.getElementById('serviceModal');
            modal.classList.add('hidden');
            document.body.style.overflow = 'auto';
        }

        // Scroll effect for Navbar
        window.addEventListener('scroll', () => {
            const header = document.querySelector('header');
            if (window.scrollY > 20) {
                header.classList.add('top-0');
                header.classList.remove('top-8');
            } else {
                header.classList.remove('top-0');
                header.classList.add('top-8');
            }
        });
    </script>
</body>
</asp:Content>