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

        /* ===== Hero tab bar =====
           switchTab() (unchanged) toggles the shared .tab-active / .text-outline classes -
           only what "active" looks like is redefined here, scoped to #hero-tool, so the
           Site1.Master .tab-active rule used elsewhere is untouched. This replaces the boxy
           border-bottom treatment with bold text plus a short pink underline beneath the
           label - no rectangle around either tab, active or not. */
        #hero-tool .hero-tab {
            position: relative;
            border: 0;
            background: none;
        }
        #hero-tool .hero-tab.tab-active {
            border-bottom: none;
            color: #ffffff;
            font-weight: 800;
        }
        #hero-tool .hero-tab.tab-active::after {
            content: "";
            position: absolute;
            left: 50%;
            bottom: -1px;
            transform: translateX(-50%);
            width: 56px;
            height: 4px;
            border-radius: 2px;
            background: #e2007c;
        }
        #hero-tool .hero-tab:hover { opacity: 0.85; }
        #hero-tool .hero-tab:focus-visible {
            outline: 2px solid #e2007c;
            outline-offset: 4px;
            border-radius: 6px;
        }

        /* ===== Quotation cards =====
           Same selection mechanism as Customer/BookShipment.aspx step 1 (native radio input,
           sr-only, sibling-combinator styling of the visible .size-card) - kept because a real
           <input type="radio"> already gives screen readers the correct role and checked
           state for free, which is better than reproducing it with ARIA. Only the visual
           design changes here. */
        .size-card {
            position: relative;
            padding: 20px 18px 16px;
            transition: transform 0.25s ease, box-shadow 0.25s ease, background 0.25s ease, border-color 0.25s ease;
        }
        .size-card:hover {
            transform: translateY(-2px);
            box-shadow: 0 10px 24px rgba(0, 32, 104, 0.12);
        }
        .size-card-input:checked + .size-card {
            background: linear-gradient(180deg, #004aad 0%, #002068 100%);
            border-color: #004aad;
            border-width: 2px;
        }
        .size-card-input:checked + .size-card:hover {
            transform: none;
            box-shadow: 0 10px 28px rgba(0, 32, 104, 0.28);
        }
        .size-card-input:checked + .size-card .qcard-title,
        .size-card-input:checked + .size-card .qcard-price {
            color: #ffffff;
        }
        .size-card-input:checked + .size-card .qcard-weight,
        .size-card-input:checked + .size-card .qcard-est-label {
            color: rgba(255, 255, 255, 0.7);
        }
        .size-card-input:checked + .size-card .qcard-divider {
            border-color: rgba(255, 255, 255, 0.15);
        }
        .size-card-input:focus-visible + .size-card {
            outline: 3px solid #8aa4ff;
            outline-offset: 2px;
        }

        /* Radio indicator, top-right of every card: an outlined ring when unselected, a
           filled pink circle with a white check when selected. Replaces the old per-card
           category icon entirely - one consistent indicator for all four cards. */
        .qcard-radio-fill { display: none; }
        .size-card-input:checked + .size-card .qcard-radio-ring { display: none; }
        .size-card-input:checked + .size-card .qcard-radio-fill { display: flex; }

        /* Helper note pill under the cards. */
        .qcard-note {
            background: rgba(255, 255, 255, 0.08);
        }

        /* Continue-to-Booking CTA: the arrow is a decorative overlay (asp:Button renders as
           <input type="submit">, which cannot contain child markup), so it sits in a sibling
           span positioned over the button rather than inside it - the button's own centred
           label and padding are untouched. */
        .qcard-cta-wrap { position: relative; }
        .qcard-cta {
            box-shadow: 0 12px 28px rgba(226, 0, 124, 0.35);
        }
        .qcard-cta-arrow {
            position: absolute;
            right: 20px;
            top: 50%;
            transform: translateY(-50%);
            color: #ffffff;
            pointer-events: none;
        }

        /* ===== Enterprise Logistics Services cards ===== */

        /* Card shell: soft brand-tinted shadow at rest, deeper on hover with a 4px lift and a
           brand-gradient ring that fades in (masked so only the ring itself is visible, the
           existing 1px border stays crisp underneath). */
        .fcl-service-card {
            position: relative;
            box-shadow: 0 12px 32px rgba(0, 32, 104, 0.10);
            transition: transform 0.35s ease, box-shadow 0.35s ease;
        }
        .fcl-service-card:hover,
        .fcl-service-card:focus-within {
            transform: translateY(-4px);
            box-shadow: 0 20px 48px rgba(0, 32, 104, 0.16);
        }
        .fcl-service-card::before {
            content: "";
            position: absolute;
            inset: 0;
            border-radius: inherit;
            padding: 1.5px;
            background: linear-gradient(135deg, #004aad, #e2165f);
            -webkit-mask: linear-gradient(#000 0 0) content-box, linear-gradient(#000 0 0);
            -webkit-mask-composite: xor;
            mask-composite: exclude;
            opacity: 0;
            pointer-events: none;
            transition: opacity 0.35s ease;
        }
        .fcl-service-card:hover::before,
        .fcl-service-card:focus-within::before {
            opacity: 1;
        }

        /* Image area: a permanent brand-gradient fallback sits behind the <img>, so a missing
           file (e.g. parcel-collection.jpg) never renders broken-image alt text - onerror just
           fades the <img> out and the gradient already underneath shows through. */
        .fcl-media-fallback {
            background: linear-gradient(135deg, #004aad, #e2165f);
        }
        .fcl-service-img {
            transition: transform 0.5s ease, opacity 0.25s ease;
        }
        .fcl-service-card:hover .fcl-service-img {
            transform: scale(1.05);
        }
        .fcl-media-overlay {
            background: linear-gradient(to top, rgba(0, 32, 104, 0.55) 0%, rgba(0, 32, 104, 0) 55%);
        }

        /* Icon chip deliberately overlaps the image/body seam: it is the first child of the
           body wrapper (which is not clipped), pulled up by half its own height, so the
           overlap holds at any card width without knowing the image's rendered pixel height. */
        .fcl-icon-chip {
            background: linear-gradient(135deg, #004aad, #e2165f);
            box-shadow: 0 10px 24px rgba(0, 32, 104, 0.25);
        }

        /* "Most Used" ribbon on the featured (Warehousing) card. */
        .fcl-featured-ribbon {
            background: #e2165f;
            box-shadow: 0 4px 14px rgba(226, 22, 95, 0.35);
        }

        /* View-mode segmented control (placeholder). Buttons are disabled - it is not wired to
           anything yet, so it must not sit in the tab order or imply it can be activated. */
        .fcl-view-toggle button[aria-pressed="true"] {
            background: linear-gradient(135deg, #004aad, #e2165f);
            color: #fff;
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
                                <button type="button" class="hero-tab flex-1 pb-4 font-headline-md text-headline-md tab-active transition-all" id="tab-track" onclick="switchTab('track')">Track Parcel</button>
                                <button type="button" class="hero-tab flex-1 pb-4 font-headline-md text-headline-md text-outline transition-all" id="tab-quote" onclick="switchTab('quote')">Get Quote</button>
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
                                    <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
                                        <asp:Repeater ID="rptQuoteSizes" runat="server">
                                            <ItemTemplate>
                                                <label class="cursor-pointer relative block h-full">
                                                    <input type="radio" name="QuoteSizeGroup"
                                                           value='<%# Eval("Code") %>'
                                                           <%# QuoteCheckedAttr(Eval("Code")) %>
                                                           class="size-card-input sr-only"
                                                           onchange='<%# "setQuoteSize(\"" + Eval("Code") + "\")" %>' />
                                                    <div class="size-card rounded-xl border border-outline-variant bg-surface-container-lowest h-full flex flex-col justify-between">
                                                        <div class="flex items-start justify-between gap-2 pr-8">
                                                            <div>
                                                                <div class="qcard-title font-bold text-on-surface text-sm"><%# Eval("Name") %></div>
                                                                <div class="qcard-weight text-[11px] text-on-surface-variant mt-1"><%# Eval("MaxWeightText") %></div>
                                                            </div>
                                                        </div>

                                                        <!-- Single consistent selection indicator, top-right - replaces the old
                                                             per-card category icon entirely. -->
                                                        <span class="qcard-radio-ring absolute top-4 right-4 w-6 h-6 rounded-full border-2 border-outline-variant" aria-hidden="true"></span>
                                                        <span class="qcard-radio-fill absolute top-4 right-4 w-6 h-6 rounded-full bg-[#e2007c] text-white items-center justify-center" aria-hidden="true">
                                                            <span class="material-symbols-outlined text-sm font-bold">check</span>
                                                        </span>

                                                        <div class="qcard-divider mt-3 pt-3 border-t border-outline-variant/40 flex items-end justify-between">
                                                            <span class="qcard-est-label text-[10px] font-bold uppercase tracking-wide text-on-surface-variant">Est. from</span>
                                                            <span class="qcard-price text-2xl font-black leading-none text-primary"><%# Eval("FromPriceText") %></span>
                                                        </div>
                                                    </div>
                                                </label>
                                            </ItemTemplate>
                                        </asp:Repeater>
                                    </div>
                                </fieldset>
                                <div class="qcard-note flex items-start gap-3 rounded-xl px-4 py-3">
                                    <span class="flex-shrink-0 flex items-center justify-center w-5 h-5 rounded-full border border-white/60 text-white/85 text-[11px] font-bold mt-0.5" aria-hidden="true">i</span>
                                    <p class="text-[13px] leading-snug text-white/85">
                                        From-prices are for a local delivery.<br />
                                        Final price is confirmed from the collection and delivery cities when you book.
                                    </p>
                                </div>
                                <div class="qcard-cta-wrap">
                                    <asp:Button ID="btnContinueToBooking" runat="server" Text="Continue to Booking" OnClick="btnContinueToBooking_Click"
                                        CssClass="qcard-cta w-full py-4 primary-gradient-btn text-on-primary font-bold uppercase tracking-widest rounded-xl cursor-pointer border-0" />
                                    <span class="material-symbols-outlined qcard-cta-arrow" aria-hidden="true">arrow_forward</span>
                                </div>
                                <p class="text-center text-white/70 font-label-sm text-xs">Sign in or register to complete your booking - your selected size comes with you.</p>
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
                <div class="flex flex-wrap justify-between items-end gap-6 mb-12">
                    <div class="max-w-2xl">
                        <h2 class="font-display-lg text-headline-lg text-on-surface mb-4 uppercase tracking-tight">Enterprise Logistics <span class="logo-gradient">Services</span></h2>
                        <p class="font-body-lg text-body-lg text-on-surface-variant">Scalable solutions from warehouse storage to door-to-door distribution, integrated into a single glass-pane command center.</p>
                    </div>
                    <div class="hidden md:flex items-center gap-5">
                        <!-- Placeholder view-mode switch: not wired to anything yet, so both
                             buttons are disabled - inert to click and keyboard alike - rather
                             than implying a Map view that does not exist. -->
                        <div class="fcl-view-toggle inline-flex items-center gap-1 rounded-full bg-surface-container p-1 border border-outline-variant/50" role="group" aria-label="View mode (coming soon)">
                            <button type="button" class="px-4 py-1.5 rounded-full font-label-sm text-label-sm font-bold uppercase tracking-wide text-on-surface-variant" aria-pressed="true" disabled>Cards</button>
                            <button type="button" class="px-4 py-1.5 rounded-full font-label-sm text-label-sm font-bold uppercase tracking-wide text-on-surface-variant" aria-pressed="false" disabled>Map</button>
                        </div>
                        <span class="material-symbols-outlined text-display-lg text-outline-variant/30">grid_view</span>
                    </div>
                </div>
                <!-- Service cards. The image bleeds to the card edge and the description flex-grows,
                     so the three "View workflow" rows sit on one baseline whatever the copy length. -->
                <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-gutter items-stretch">
                    <!-- Service Card 1: Parcel Collection -->
                    <div class="fcl-service-card group flex flex-col overflow-hidden rounded-[28px] bg-surface-container-lowest border border-outline-variant/60">
                        <div class="relative aspect-[4/3] w-full overflow-hidden">
                            <div class="fcl-media-fallback absolute inset-0" aria-hidden="true"></div>
                            <img src='<%= ResolveUrl("~/Images/web/parcel-collection.jpg") %>' alt="Courier handing over sealed parcels at a collection point"
                                 loading="lazy" class="fcl-service-img absolute inset-0 h-full w-full object-cover"
                                 onerror="this.onerror=null; this.style.opacity='0';" />
                            <div class="fcl-media-overlay absolute inset-0" aria-hidden="true"></div>

                            <div class="absolute top-4 left-4 z-10 min-w-[44px] h-11 px-3 rounded-full bg-white flex items-center justify-center text-primary font-extrabold font-label-md text-label-md shadow-[0_4px_14px_rgba(0,32,104,0.18)]" aria-hidden="true">01</div>
                        </div>
                        <div class="relative flex flex-col flex-grow px-glass-padding pb-glass-padding pt-10">
                            <div class="fcl-icon-chip absolute -top-6 left-6 w-[52px] h-[52px] rounded-[14px] flex items-center justify-center text-white" aria-hidden="true">
                                <span class="material-symbols-outlined">package_2</span>
                            </div>

                            <span class="inline-flex self-start items-center rounded-full bg-[#eef2ff] text-[#004aad] font-label-sm text-label-sm font-bold uppercase tracking-wide px-3 py-1 mb-3">Pickup</span>
                            <h3 class="font-headline-md text-headline-md text-on-surface mb-3">Parcel Collection</h3>
                            <p class="font-body-md text-body-md text-on-surface-variant mb-6 flex-grow">Automated scheduling and pickup optimization for high-volume corporate senders.</p>

                            <div class="grid grid-cols-2 gap-3 mb-2">
                                <div class="bg-surface-container rounded-xl p-3">
                                    <div class="font-headline-sm text-headline-sm font-extrabold text-primary">2h</div>
                                    <div class="font-label-sm text-[10px] font-bold uppercase tracking-wide text-on-surface-variant">Avg Pickup</div>
                                </div>
                                <div class="bg-surface-container rounded-xl p-3">
                                    <div class="font-headline-sm text-headline-sm font-extrabold text-primary">99%</div>
                                    <div class="font-label-sm text-[10px] font-bold uppercase tracking-wide text-on-surface-variant">On Time</div>
                                </div>
                            </div>

                            <button type="button" class="fcl-card-action" onclick="openModal('parcel')">
                                <span>View Full Workflow</span>
                                <span class="material-symbols-outlined">arrow_forward</span>
                            </button>
                        </div>
                    </div>
                    <!-- Service Card 2: Warehousing (featured) -->
                    <div class="fcl-service-card group flex flex-col overflow-hidden rounded-[28px] bg-surface-container-lowest border border-outline-variant/60">
                        <div class="relative aspect-[4/3] w-full overflow-hidden">
                            <div class="fcl-media-fallback absolute inset-0" aria-hidden="true"></div>
                            <img src='<%= ResolveUrl("~/Images/web/warehousing.jpg") %>' alt="Racked warehouse aisle used for fulfillment storage"
                                 loading="lazy" class="fcl-service-img absolute inset-0 h-full w-full object-cover"
                                 onerror="this.onerror=null; this.style.opacity='0';" />
                            <div class="fcl-media-overlay absolute inset-0" aria-hidden="true"></div>

                            <div class="absolute top-4 left-4 z-10 min-w-[44px] h-11 px-3 rounded-full bg-white flex items-center justify-center text-primary font-extrabold font-label-md text-label-md shadow-[0_4px_14px_rgba(0,32,104,0.18)]" aria-hidden="true">02</div>
                            <div class="fcl-featured-ribbon absolute top-4 right-4 z-10 flex items-center gap-1 rounded-full px-3 py-1.5 text-white font-label-sm text-label-sm font-bold uppercase tracking-wide">
                                <span class="material-symbols-outlined text-sm">star</span> Most Used
                            </div>
                        </div>
                        <div class="relative flex flex-col flex-grow px-glass-padding pb-glass-padding pt-10">
                            <div class="fcl-icon-chip absolute -top-6 left-6 w-[52px] h-[52px] rounded-[14px] flex items-center justify-center text-white" aria-hidden="true">
                                <span class="material-symbols-outlined">warehouse</span>
                            </div>

                            <span class="inline-flex self-start items-center rounded-full bg-[#fdeef5] text-[#e2165f] font-label-sm text-label-sm font-bold uppercase tracking-wide px-3 py-1 mb-3">Storage</span>
                            <h3 class="font-headline-md text-headline-md text-on-surface mb-3">Warehousing</h3>
                            <p class="font-body-md text-body-md text-on-surface-variant mb-6 flex-grow">Smart fulfillment centers with real-time inventory tracking and multi-node routing.</p>

                            <div class="grid grid-cols-2 gap-3 mb-2">
                                <div class="bg-surface-container rounded-xl p-3">
                                    <div class="font-headline-sm text-headline-sm font-extrabold text-primary">45+</div>
                                    <div class="font-label-sm text-[10px] font-bold uppercase tracking-wide text-on-surface-variant">Hubs</div>
                                </div>
                                <div class="bg-surface-container rounded-xl p-3">
                                    <div class="font-headline-sm text-headline-sm font-extrabold text-primary">24/7</div>
                                    <div class="font-label-sm text-[10px] font-bold uppercase tracking-wide text-on-surface-variant">Tracking</div>
                                </div>
                            </div>

                            <button type="button" class="fcl-card-action" onclick="openModal('warehousing')">
                                <span>View Full Workflow</span>
                                <span class="material-symbols-outlined">arrow_forward</span>
                            </button>
                        </div>
                    </div>
                    <!-- Service Card 3: Delivery Services -->
                    <div class="fcl-service-card group flex flex-col overflow-hidden rounded-[28px] bg-surface-container-lowest border border-outline-variant/60">
                        <div class="relative aspect-[4/3] w-full overflow-hidden">
                            <div class="fcl-media-fallback absolute inset-0" aria-hidden="true"></div>
                            <img src='<%= ResolveUrl("~/Images/web/delivery-services.jpg") %>' alt="Delivery driver loading a parcel van for a last-mile run"
                                 loading="lazy" class="fcl-service-img absolute inset-0 h-full w-full object-cover"
                                 onerror="this.onerror=null; this.style.opacity='0';" />
                            <div class="fcl-media-overlay absolute inset-0" aria-hidden="true"></div>

                            <div class="absolute top-4 left-4 z-10 min-w-[44px] h-11 px-3 rounded-full bg-white flex items-center justify-center text-primary font-extrabold font-label-md text-label-md shadow-[0_4px_14px_rgba(0,32,104,0.18)]" aria-hidden="true">03</div>
                        </div>
                        <div class="relative flex flex-col flex-grow px-glass-padding pb-glass-padding pt-10">
                            <div class="fcl-icon-chip absolute -top-6 left-6 w-[52px] h-[52px] rounded-[14px] flex items-center justify-center text-white" aria-hidden="true">
                                <span class="material-symbols-outlined">local_shipping</span>
                            </div>

                            <span class="inline-flex self-start items-center rounded-full bg-emerald-50 text-emerald-700 font-label-sm text-label-sm font-bold uppercase tracking-wide px-3 py-1 mb-3">Last Mile</span>
                            <h3 class="font-headline-md text-headline-md text-on-surface mb-3">Delivery Services</h3>
                            <p class="font-body-md text-body-md text-on-surface-variant mb-6 flex-grow">Last-mile precision with predictive arrival windows and secure proof-of-delivery.</p>

                            <div class="grid grid-cols-2 gap-3 mb-2">
                                <div class="bg-surface-container rounded-xl p-3">
                                    <div class="font-headline-sm text-headline-sm font-extrabold text-primary">99.9%</div>
                                    <div class="font-label-sm text-[10px] font-bold uppercase tracking-wide text-on-surface-variant">Precision</div>
                                </div>
                                <div class="bg-surface-container rounded-xl p-3">
                                    <div class="font-headline-sm text-headline-sm font-extrabold text-primary">1.8k</div>
                                    <div class="font-label-sm text-[10px] font-bold uppercase tracking-wide text-on-surface-variant">Fleet</div>
                                </div>
                            </div>

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
