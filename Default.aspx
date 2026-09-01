<%@ Page Title="Home" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="ForteLogisticsApp.Default" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
    <style>
        /* Landing-page only. Shared tokens, .fcl-* components, .hero-glass, .glass-panel,
           .tab-active and .no-scrollbar all come from Site1.Master. */
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
        @keyframes pulse-soft {
            0%, 100% { opacity: 1; }
            50% { opacity: 0.8; }
        }
        .animate-pulse-soft { animation: pulse-soft 3s infinite ease-in-out; }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
        <!-- Main Content -->
        <main>
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
                                <asp:Button ID="btnTrack" runat="server" Text="Track Shipment" OnClick="btnTrack_Click"
                                    CssClass="w-full py-4 primary-gradient-btn text-on-primary font-bold uppercase tracking-widest rounded-xl cursor-pointer border-0" />
                                <asp:Label ID="lblTrackResult" runat="server" CssClass="block text-center font-label-sm text-sm text-primary font-bold" />
                                <p class="text-center text-outline font-label-sm text-xs">Tracking parcels across South Africa in real time.</p>
                            </div>

                            <!-- Quote Form -->
                            <div class="hidden space-y-6" id="form-quote">
                                <div class="grid grid-cols-2 gap-4">
                                    <div>
                                        <label class="font-label-sm text-label-sm text-outline mb-2 block uppercase">Origin</label>
                                        <asp:DropDownList ID="ddlOrigin" runat="server" CssClass="w-full bg-surface-container-lowest border border-outline-variant rounded-xl p-4 text-on-surface focus:outline-none focus:border-primary font-body-md appearance-none">
                                            <asp:ListItem Text="Johannesburg" Value="Johannesburg" />
                                            <asp:ListItem Text="Cape Town" Value="Cape Town" />
                                            <asp:ListItem Text="Durban" Value="Durban" />
                                            <asp:ListItem Text="Gqeberha" Value="Gqeberha" />
                                            <asp:ListItem Text="Bloemfontein" Value="Bloemfontein" />
                                        </asp:DropDownList>
                                    </div>
                                    <div>
                                        <label class="font-label-sm text-label-sm text-outline mb-2 block uppercase">Destination</label>
                                        <asp:DropDownList ID="ddlDestination" runat="server" CssClass="w-full bg-surface-container-lowest border border-outline-variant rounded-xl p-4 text-on-surface focus:outline-none focus:border-primary font-body-md appearance-none">
                                            <asp:ListItem Text="Cape Town" Value="Cape Town" />
                                            <asp:ListItem Text="Johannesburg" Value="Johannesburg" />
                                            <asp:ListItem Text="Durban" Value="Durban" />
                                            <asp:ListItem Text="Gqeberha" Value="Gqeberha" />
                                            <asp:ListItem Text="Bloemfontein" Value="Bloemfontein" />
                                        </asp:DropDownList>
                                    </div>
                                </div>
                                <div>
                                    <label class="font-label-sm text-label-sm text-outline mb-2 block uppercase">Estimated Weight (KG)</label>
                                    <asp:TextBox ID="txtWeight" runat="server" TextMode="Number" CssClass="w-full bg-surface-container-lowest border border-outline-variant rounded-xl p-4 text-on-surface focus:outline-none focus:border-primary font-label-sm" placeholder="0.00" />
                                </div>
                                <asp:Button ID="btnCalculate" runat="server" Text="Calculate Quote" OnClick="btnCalculate_Click"
                                    CssClass="w-full py-4 primary-gradient-btn text-on-primary font-bold uppercase tracking-widest rounded-xl cursor-pointer border-0" />
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
                            <a href="<%= ResolveUrl("~/Register.aspx?role=Business") %>" class="px-12 py-5 primary-gradient-btn text-white font-bold uppercase tracking-widest text-label-sm rounded-xl inline-flex items-center justify-center">
                                Register Business Account
                            </a>
                        </div>
                    </div>
                </div>
            </section>
        </main>

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
                    <a href="<%= ResolveUrl("~/Register.aspx") %>" class="px-8 py-3 primary-gradient-btn text-on-primary font-bold uppercase rounded-lg inline-block">Get Started with this Service</a>
                </div>
            </div>
        </div>
    </form>
</asp:Content>

<asp:Content ID="Content3" ContentPlaceHolderID="ScriptContent" runat="server">
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

    </script>
</asp:Content>
