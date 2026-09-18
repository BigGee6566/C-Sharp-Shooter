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

        /* Service card footer action. A flat row rather than a filled button: the card itself
           is the affordance, so a heavy button competes with it. */
        .fcl-card-action {
            display: flex; align-items: center; justify-content: space-between; gap: 8px;
            width: 100%; padding: 12px 0 0; margin-top: auto;
            border: 0; border-top: 1px solid rgba(196, 197, 213, 0.5);
            background: none; cursor: pointer; color: #002068;
            font-family: "Inter", sans-serif; font-size: 12px; line-height: 16px;
            font-weight: 700; letter-spacing: 0.06em; text-transform: uppercase;
            transition: color 0.2s ease;
        }
        .fcl-card-action .material-symbols-outlined { transition: transform 0.25s ease; }
        .fcl-card-action:hover { color: #e2007c; }
        .fcl-card-action:hover .material-symbols-outlined { transform: translateX(4px); }
        .fcl-card-action:focus-visible { outline: 2px solid #dce1ff; outline-offset: 3px; }

        /* Quotation cards - identical selection mechanism to Customer/BookShipment.aspx step
           1 (same ParcelSize source, same radio-driven card), sized for this narrower panel. */
        .size-card-input:checked + .size-card {
            border-color: #003399;
            background-color: rgba(0, 51, 153, 0.04);
            box-shadow: 0 0 0 2px #003399;
        }
        .size-card-check { display: none; }
        .size-card-input:checked + .size-card .size-card-check { display: inline-flex; }
        .size-card-input:checked + .size-card .size-card-icon { display: none; }
        .size-card-input:focus-visible + .size-card {
            outline: 3px solid #8aa4ff;
            outline-offset: 2px;
        }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
        <!-- Main Content -->
        <main>
            <!-- Hero Section -->
            <section class="relative min-h-[921px] flex items-center px-margin-desktop overflow-hidden">
                <div class="absolute inset-0 z-0">
                    <div class="absolute inset-0 bg-gradient-to-t from-surface via-surface/80 to-surface/40"></div>
                    <div class="absolute inset-0 bg-cover bg-center mix-blend-multiply opacity-10"
                         style="background-image: url('<%= ResolveUrl("~/Images/web/warehousing.jpg") %>')"></div>
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

                    <!-- Hero Tool (Tabs Target Section).
                         #track and #quote are the nav's deep links into the two tabs; applyHash()
                         below selects the matching tab, since the tabs are not hash-driven. -->
                    <span id="track"></span>
                    <span id="quote"></span>
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

                            <!-- Quote Form: the same preset parcel-size cards used at Book Shipment
                                 step 1 (same ParcelSize catalogue, same server-computed price) - not
                                 a separate manual quote estimator. -->
                            <div class="hidden space-y-4" id="form-quote">
                                <asp:HiddenField ID="hfQuoteSize" runat="server" Value="Small" />
                                <fieldset class="border-0 p-0 m-0">
                                    <legend class="sr-only">Parcel size</legend>
                                    <div class="grid grid-cols-2 gap-3 max-h-72 overflow-y-auto pr-1">
                                        <asp:Repeater ID="rptQuoteSizes" runat="server">
                                            <ItemTemplate>
                                                <label class="cursor-pointer relative block h-full">
                                                    <input type="radio" name="QuoteSizeGroup"
                                                           value='<%# Eval("Code") %>'
                                                           <%# QuoteCheckedAttr(Eval("Code")) %>
                                                           class="size-card-input sr-only"
                                                           onchange='<%# "setQuoteSize(\"" + Eval("Code") + "\")" %>' />
                                                    <div class="size-card p-3 rounded-xl border border-outline-variant hover:border-primary transition-all h-full flex flex-col justify-between bg-surface-container-lowest">
                                                        <div class="flex justify-between items-center mb-1">
                                                            <span class="font-bold text-on-surface text-sm"><%# Eval("Name") %></span>
                                                            <span class="material-symbols-outlined text-primary text-base size-card-icon"><%# Eval("Icon") %></span>
                                                            <span class="material-symbols-outlined text-primary text-base size-card-check" aria-hidden="true">check_circle</span>
                                                        </div>
                                                        <div class="text-[11px] text-on-surface-variant"><%# Eval("MaxWeightText") %></div>
                                                        <div class="mt-2 pt-2 border-t border-outline-variant/50 text-right">
                                                            <span class="text-[10px] text-on-surface-variant">Est. from</span>
                                                            <div class="text-sm font-bold text-primary"><%# Eval("FromPriceText") %></div>
                                                        </div>
                                                    </div>
                                                </label>
                                            </ItemTemplate>
                                        </asp:Repeater>
                                    </div>
                                </fieldset>
                                <p class="text-[11px] text-outline">
                                    From-prices are for a local delivery; your final price is confirmed from the
                                    collection and delivery cities when you book.
                                </p>
                                <asp:Button ID="btnContinueToBooking" runat="server" Text="Continue to Booking" OnClick="btnContinueToBooking_Click"
                                    CssClass="w-full py-4 primary-gradient-btn text-on-primary font-bold uppercase tracking-widest rounded-xl cursor-pointer border-0" />
                                <p class="text-center text-outline font-label-sm text-xs">Sign in or register to complete your booking - your selected size comes with you.</p>
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
                        <div class="absolute inset-0 bg-cover bg-center" style="background-image: url('<%= ResolveUrl("~/Images/image.png") %>')"></div>
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
                <div class="flex justify-between items-end mb-12">
                    <div class="max-w-2xl">
                        <h2 class="font-display-lg text-headline-lg text-on-surface mb-4 uppercase tracking-tight">Enterprise Logistics <span class="logo-gradient">Services</span></h2>
                        <p class="font-body-lg text-body-lg text-on-surface-variant">Scalable solutions from warehouse storage to door-to-door distribution, integrated into a single glass-pane command center.</p>
                    </div>
                    <div class="hidden md:block">
                        <span class="material-symbols-outlined text-display-lg text-outline-variant/30">grid_view</span>
                    </div>
                </div>
                <!-- Service cards. The image bleeds to the card edge and the description flex-grows,
                     so the three "View workflow" rows sit on one baseline whatever the copy length. -->
                <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-gutter items-stretch">
                    <!-- Service Card 1 -->
                    <div class="group flex flex-col overflow-hidden rounded-2xl bg-surface-container-lowest border border-outline-variant/60 shadow-sm hover:shadow-lg hover:border-primary/40 hover:-translate-y-1 transition-all duration-300">
                        <div class="relative aspect-[4/3] w-full overflow-hidden bg-surface-container">
                            <img src='<%= ResolveUrl("~/Images/web/parcel-collection.jpg") %>' alt="Courier handing over sealed parcels at a collection point"
                                 loading="lazy" class="absolute inset-0 h-full w-full object-cover group-hover:scale-105 transition-transform duration-500" />

                            <div class="absolute top-4 left-4 bg-surface-container-lowest/90 backdrop-blur-md p-2 rounded-lg border border-primary/20 shadow-sm">
                                <span class="material-symbols-outlined text-primary">package_2</span>
                            </div>
                        </div>
                        <div class="flex flex-col flex-grow p-glass-padding">
                            <h3 class="font-headline-md text-headline-md text-on-surface mb-3">Parcel Collection</h3>
                            <p class="font-body-md text-body-md text-on-surface-variant mb-6 flex-grow">Automated scheduling and pickup optimization for high-volume corporate senders.</p>
                            <button type="button" class="fcl-card-action" onclick="openModal('parcel')">
                                <span>View Full Workflow</span>
                                <span class="material-symbols-outlined">arrow_forward</span>
                            </button>
                        </div>
                    </div>
                    <!-- Service Card 2 -->
                    <div class="group flex flex-col overflow-hidden rounded-2xl bg-surface-container-lowest border border-outline-variant/60 shadow-sm hover:shadow-lg hover:border-primary/40 hover:-translate-y-1 transition-all duration-300">
                        <div class="relative aspect-[4/3] w-full overflow-hidden bg-surface-container">
                            <img src='<%= ResolveUrl("~/Images/web/warehousing.jpg") %>' alt="Racked warehouse aisle used for fulfillment storage"
                                 loading="lazy" class="absolute inset-0 h-full w-full object-cover group-hover:scale-105 transition-transform duration-500" />

                            <div class="absolute top-4 left-4 bg-surface-container-lowest/90 backdrop-blur-md p-2 rounded-lg border border-secondary/20 shadow-sm">
                                <span class="material-symbols-outlined text-secondary">warehouse</span>
                            </div>
                        </div>
                        <div class="flex flex-col flex-grow p-glass-padding">
                            <h3 class="font-headline-md text-headline-md text-on-surface mb-3">Warehousing</h3>
                            <p class="font-body-md text-body-md text-on-surface-variant mb-6 flex-grow">Smart fulfillment centers with real-time inventory tracking and multi-node routing.</p>
                            <button type="button" class="fcl-card-action" onclick="openModal('warehousing')">
                                <span>View Full Workflow</span>
                                <span class="material-symbols-outlined">arrow_forward</span>
                            </button>
                        </div>
                    </div>
                    <!-- Service Card 3 -->
                    <div class="group flex flex-col overflow-hidden rounded-2xl bg-surface-container-lowest border border-outline-variant/60 shadow-sm hover:shadow-lg hover:border-primary/40 hover:-translate-y-1 transition-all duration-300">
                        <div class="relative aspect-[4/3] w-full overflow-hidden bg-surface-container">
                            <img src='<%= ResolveUrl("~/Images/web/delivery-services.jpg") %>' alt="Delivery driver loading a parcel van for a last-mile run"
                                 loading="lazy" class="absolute inset-0 h-full w-full object-cover group-hover:scale-105 transition-transform duration-500" />

                            <div class="absolute top-4 left-4 bg-surface-container-lowest/90 backdrop-blur-md p-2 rounded-lg border border-tertiary/20 shadow-sm">
                                <span class="material-symbols-outlined text-tertiary">local_shipping</span>
                            </div>
                        </div>
                        <div class="flex flex-col flex-grow p-glass-padding">
                            <h3 class="font-headline-md text-headline-md text-on-surface mb-3">Delivery Services</h3>
                            <p class="font-body-md text-body-md text-on-surface-variant mb-6 flex-grow">Last-mile precision with predictive arrival windows and secure proof-of-delivery.</p>
                            <button type="button" class="fcl-card-action" onclick="openModal('delivery')">
                                <span>View Full Workflow</span>
                                <span class="material-symbols-outlined">arrow_forward</span>
                            </button>
                        </div>
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
        function setQuoteSize(val) {
            document.getElementById('<%= hfQuoteSize.ClientID %>').value = val;
        }

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

        // The nav's "Track Parcel" / "Get a Quote" links arrive as #track / #quote. The tabs
        // are click-driven, so translate the hash into a tab selection and scroll the tool in.
        function applyHash() {
            const type = window.location.hash === '#track' ? 'track'
                       : window.location.hash === '#quote' ? 'quote'
                       : null;
            if (!type) return;

            switchTab(type);
            const tool = document.getElementById('hero-tool');
            if (tool) tool.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }

        window.addEventListener('hashchange', applyHash);
        applyHash();

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
