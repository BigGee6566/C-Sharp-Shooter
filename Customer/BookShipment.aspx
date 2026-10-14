<%@ Page Title="Book a Shipment" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="BookShipment.aspx.cs" Inherits="ForteLogisticsApp.CustomerPortal.BookShipment" %>


<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
    <style>
        /* The radio itself is visually hidden, so the card carries every state. */
        .size-card-input:checked + .size-card {
            border-color: #003399;
            background-color: rgba(0, 51, 153, 0.04);
            box-shadow: 0 0 0 2px #003399;
        }

        /* Selection must not be conveyed by colour alone. */
        .size-card-check {
            display: none;
        }
        .size-card-input:checked + .size-card .size-card-check {
            display: inline-flex;
        }
        .size-card-input:checked + .size-card .size-card-icon {
            display: none;
        }

        /* Keyboard focus has to be visible even though the input is sr-only. */
        .size-card-input:focus-visible + .size-card {
            outline: 3px solid #8aa4ff;
            outline-offset: 2px;
        }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class="p-4 md:p-10 max-w-[1000px] mx-auto w-full flex-1">
        
        <!-- Wizard Step Tracker Header -->
        <div class="flex items-center justify-between mb-8 px-4 max-w-2xl mx-auto">
            <div class="flex flex-col items-center">
                <asp:Panel ID="pnlStep1Indicator" runat="server" CssClass="w-8 h-8 rounded-full bg-primary text-on-primary flex items-center justify-center font-bold text-sm">1</asp:Panel>
                <span class="text-xs font-semibold text-primary mt-2">Parcel Details</span>
            </div>
            <div class="flex-1 h-0.5 bg-outline-variant/50 mx-2 mb-5"></div>
            <div class="flex flex-col items-center">
                <asp:Panel ID="pnlStep2Indicator" runat="server" CssClass="w-8 h-8 rounded-full bg-surface-container-highest text-on-surface-variant flex items-center justify-center font-bold text-sm">2</asp:Panel>
                <span class="text-xs font-medium text-on-surface-variant mt-2">Addresses</span>
            </div>
            <div class="flex-1 h-0.5 bg-outline-variant/50 mx-2 mb-5"></div>
            <div class="flex flex-col items-center">
                <asp:Panel ID="pnlStep3Indicator" runat="server" CssClass="w-8 h-8 rounded-full bg-surface-container-highest text-on-surface-variant flex items-center justify-center font-bold text-sm">3</asp:Panel>
                <span class="text-xs font-medium text-on-surface-variant mt-2">Payment</span>
            </div>
            <div class="flex-1 h-0.5 bg-outline-variant/50 mx-2 mb-5"></div>
            <div class="flex flex-col items-center">
                <asp:Panel ID="pnlStep4Indicator" runat="server" CssClass="w-8 h-8 rounded-full bg-surface-container-highest text-on-surface-variant flex items-center justify-center font-bold text-sm">4</asp:Panel>
                <span class="text-xs font-medium text-on-surface-variant mt-2">Confirmation</span>
            </div>
        </div>

        <!-- Main Card Container -->
        <div class="bg-surface-container-lowest p-6 md:p-8 rounded-xl border border-outline-variant shadow-sm">
            <asp:MultiView ID="mvBookShipment" ActiveViewIndex="0" runat="server">
                
                <!-- STEP 1: PARCEL SPECIFICATIONS -->
                <asp:View ID="vwParcelDetails" runat="server">
                    <h2 class="text-2xl font-bold text-on-surface mb-1">Step 1: Parcel Specifications</h2>
                    <p class="text-sm text-on-surface-variant mb-6">Select a preset size that best fits your parcel.</p>

                    <asp:HiddenField ID="hfSelectedSize" runat="server" Value="Small" />

                    <fieldset class="border-0 p-0 m-0">
                        <legend class="sr-only">Parcel size</legend>

                        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
                            <asp:Repeater ID="rptParcelSizes" runat="server">
                                <ItemTemplate>
                                    <label class="cursor-pointer relative block h-full">
                                        <input type="radio" name="ParcelSizeGroup"
                                               value='<%# Eval("Code") %>'
                                               <%# CheckedAttr(Eval("Code")) %>
                                               class="size-card-input sr-only"
                                               onchange='<%# "setSize(\"" + Eval("Code") + "\")" %>' />
                                        <div class="size-card p-4 rounded-xl border border-outline-variant hover:border-primary transition-all h-full flex flex-col justify-between">
                                            <div>
                                                <div class="flex justify-between items-center mb-2">
                                                    <span class="font-bold text-on-surface text-base"><%# Eval("Name") %></span>
                                                    <span class="material-symbols-outlined text-primary size-card-icon"><%# Eval("Icon") %></span>
                                                    <span class="material-symbols-outlined text-primary size-card-check" aria-hidden="true">check_circle</span>
                                                </div>
                                                <p class="text-xs text-on-surface-variant mb-3"><%# Eval("Examples") %></p>
                                                <div class="text-xs space-y-1 text-on-surface-variant">
                                                    <div><strong>Max Weight:</strong> <%# Eval("MaxWeightText") %></div>
                                                    <div><strong>Dimensions:</strong> <%# Eval("DimensionsText") %></div>
                                                </div>
                                            </div>
                                            <div class="mt-4 pt-3 border-t border-outline-variant/50 text-right">
                                                <span class="text-xs text-on-surface-variant">Est. from</span>
                                                <div class="text-lg font-bold text-primary"><%# Eval("FromPriceText") %></div>
                                            </div>
                                        </div>
                                    </label>
                                </ItemTemplate>
                            </asp:Repeater>
                        </div>
                    </fieldset>

                    <p class="text-xs text-on-surface-variant mt-3 mb-8">
                        From-prices shown are for a local delivery. Your final price depends on the
                        collection and delivery cities, and is confirmed at step 3.
                    </p>

                    <div class="flex justify-end">
                        <asp:Button ID="btnNextAddress" runat="server" Text="Next: Addresses ->" OnClick="btnNextAddress_Click" CssClass="fcl-btn" />
                    </div>
                </asp:View>

                <!-- STEP 2: ADDRESSES -->
                <asp:View ID="vwAddresses" runat="server">
                    <h2 class="text-2xl font-bold text-on-surface mb-1">Step 2: Pickup & Delivery Addresses</h2>
                    <p class="text-sm text-on-surface-variant mb-6">Enter where we should collect and deliver your package.</p>

                    <div class="grid grid-cols-1 md:grid-cols-2 gap-6 mb-8">
                        <!-- Collection Address -->
                        <div class="p-4 bg-surface-container-low rounded-xl border border-outline-variant">
                            <h3 class="font-bold text-on-surface mb-3 flex items-center gap-2">
                                <span class="material-symbols-outlined text-primary">location_on</span> Collection Address
                            </h3>
                            <div class="space-y-3">
                                <div>
                                    <label class="block text-xs font-semibold text-on-surface-variant mb-1">Street Address</label>
                                    <asp:TextBox ID="txtPickupStreet" runat="server" CssClass="fcl-input" placeholder="6 St Georges Rd" />
                                </div>
                                <div class="grid grid-cols-2 gap-2">
                                    <div>
                                        <label class="block text-xs font-semibold text-on-surface-variant mb-1" for="<%= txtPickupCity.ClientID %>">City</label>
                                        <asp:TextBox ID="txtPickupCity" runat="server" CssClass="fcl-input" list="cityOptions" autocomplete="off" placeholder="Select or type a city" />
                                    </div>
                                    <div>
                                        <label class="block text-xs font-semibold text-on-surface-variant mb-1">Postal Code</label>
                                        <asp:TextBox ID="txtPickupPostal" runat="server" CssClass="fcl-input" placeholder="2000" />
                                    </div>
                                </div>
                            </div>
                        </div>

                        <!-- Delivery Address -->
                        <div class="p-4 bg-surface-container-low rounded-xl border border-outline-variant">
                            <h3 class="font-bold text-on-surface mb-3 flex items-center gap-2">
                                <span class="material-symbols-outlined text-primary">flag</span> Delivery Address
                            </h3>
                            <div class="space-y-3">
                                <div>
                                    <label class="block text-xs font-semibold text-on-surface-variant mb-1">Street Address</label>
                                    <asp:TextBox ID="txtDeliveryStreet" runat="server" CssClass="fcl-input" placeholder="50 Church Street" />
                                </div>
                                <div class="grid grid-cols-2 gap-2">
                                    <div>
                                        <label class="block text-xs font-semibold text-on-surface-variant mb-1" for="<%= txtDeliveryCity.ClientID %>">City</label>
                                        <asp:TextBox ID="txtDeliveryCity" runat="server" CssClass="fcl-input" list="cityOptions" autocomplete="off" placeholder="Select or type a city" />
                                    </div>
                                    <div>
                                        <label class="block text-xs font-semibold text-on-surface-variant mb-1">Postal Code</label>
                                        <asp:TextBox ID="txtDeliveryPostal" runat="server" CssClass="fcl-input" placeholder="8001" />
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>

                    <!-- Shared suggestions for both city fields. A datalist lets the customer
                         pick a known city OR type any other destination we deliver to. -->
                    <datalist id="cityOptions"><asp:Literal ID="litCityOptions" runat="server" /></datalist>

                    <asp:Label ID="lblAddrStatus" runat="server" CssClass="block mb-3" />

                    <div class="flex justify-between items-center">
                        <asp:Button ID="btnBackToParcel" runat="server" Text="<- Back" OnClick="btnBackToParcel_Click" CssClass="fcl-btn fcl-btn-dark" />
                        <asp:Button ID="btnNextPayment" runat="server" Text="Next: Recipient ->" OnClick="btnNextPayment_Click" CssClass="fcl-btn" />
                    </div>
                </asp:View>

                <!-- STEP 3: PAYMENT -->
                <asp:View ID="vwPayment" runat="server">
                    <!-- One-time token: a double-click or F5-resubmit of Confirm carries a spent
                         token and is rejected server-side, so a booking is created at most once. -->
                    <asp:HiddenField ID="hfSubmitToken" runat="server" />
                    <h2 class="text-2xl font-bold text-on-surface mb-1">Step 3: Recipient &amp; Confirm</h2>
                    <p class="text-sm text-on-surface-variant mb-6">Confirm the recipient and review your quote.</p>

                    <div class="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
                        
                        <!-- Order Summary Card -->
                        <div class="p-4 bg-surface-container-low rounded-xl border border-outline-variant">
                            <h3 class="font-bold text-on-surface mb-3 border-b border-outline-variant/60 pb-2">Shipment Summary</h3>
                            <div class="space-y-2 text-xs text-on-surface-variant">
                                <div class="flex justify-between">
                                    <span>Parcel Size:</span>
                                    <asp:Label ID="lblSummarySize" runat="server" Font-Bold="true" CssClass="text-on-surface" Text="Small" />
                                </div>
                                <div class="flex justify-between">
                                    <span>Collection:</span>
                                    <asp:Label ID="lblSummaryPickup" runat="server" CssClass="text-on-surface" />
                                </div>
                                <div class="flex justify-between">
                                    <span>Delivery:</span>
                                    <asp:Label ID="lblSummaryDelivery" runat="server" CssClass="text-on-surface" />
                                </div>
                            </div>
                            <div class="mt-4 pt-3 border-t border-outline-variant/60 flex justify-between items-center">
                                <span class="font-bold text-sm text-on-surface">Total Due:</span>
                                <asp:Label ID="lblSummaryTotal" runat="server" Font-Bold="true" CssClass="text-lg text-primary" Text="R 100.00" />
                            </div>
                        </div>

                        <!-- Recipient & billing -->
                        <div class="md:col-span-2 p-4 bg-surface-container-low rounded-xl border border-outline-variant">
                            <h3 class="font-bold text-on-surface mb-3 flex items-center gap-2">
                                <span class="material-symbols-outlined text-primary">person</span> Recipient Details
                            </h3>
                            <div class="space-y-3">
                                <div>
                                    <label class="block font-label-sm text-label-sm text-on-surface-variant mb-1" for="<%= txtReceiver.ClientID %>">Recipient Name *</label>
                                    <asp:TextBox ID="txtReceiver" runat="server" CssClass="fcl-input" placeholder="Full name of the person receiving the parcel" />
                                    <asp:RequiredFieldValidator ID="rfvReceiver" runat="server" ControlToValidate="txtReceiver"
                                        Display="Dynamic" ValidationGroup="Book" ErrorMessage="Recipient name is required."
                                        CssClass="block font-label-sm text-label-sm text-error mt-1" />
                                </div>
                                <div>
                                    <label class="block font-label-sm text-label-sm text-on-surface-variant mb-1" for="<%= txtReceiverPhone.ClientID %>">Recipient Phone</label>
                                    <asp:TextBox ID="txtReceiverPhone" runat="server" TextMode="Phone" CssClass="fcl-input" placeholder="071 234 5678" />
                                </div>
                                <div>
                                    <label class="block font-label-sm text-label-sm text-on-surface-variant mb-1" for="<%= txtDescription.ClientID %>">Parcel Description</label>
                                    <asp:TextBox ID="txtDescription" runat="server" TextMode="MultiLine" Rows="2" CssClass="fcl-input" placeholder="e.g. Documents, laptop, clothing" />
                                </div>
                            </div>

                            <div class="fcl-alert fcl-alert-info mt-stack-sm">
                                <span class="material-symbols-outlined mr-1">receipt_long</span>
                                Confirming this booking raises an invoice on your account. You can settle it at any
                                time under <strong>My Invoices</strong> - no card details are needed now.
                            </div>

                            <asp:Label ID="lblStatus" runat="server" CssClass="mt-stack-sm" />
                        </div>

                    </div>

                    <div class="flex justify-between items-center">
                        <asp:Button ID="btnBackToAddresses" runat="server" Text="<- Back" OnClick="btnBackToAddresses_Click" CssClass="fcl-btn fcl-btn-dark" />
                        <asp:Button ID="btnConfirmPayment" runat="server" Text="Confirm Booking" OnClick="btnConfirmPayment_Click" ValidationGroup="Book" CssClass="fcl-btn" />
                    </div>
                </asp:View>

                <!-- STEP 4: CONFIRMATION & WAYBILL -->
<asp:View ID="vwConfirmation" runat="server">
    <style>
        @media print {
            body * { visibility: hidden; }
            #waybill-printable, #waybill-printable * { visibility: visible; }
            #waybill-printable { position: absolute; left: 0; top: 0; width: 100%; }
            .no-print { display: none !important; }
        }
    </style>

    <div class="text-center mb-6 no-print">
        <div class="w-12 h-12 bg-[#e6f4ea] text-[#137333] rounded-full flex items-center justify-center mx-auto mb-2">
            <span class="material-symbols-outlined text-2xl">check_circle</span>
        </div>
        <h2 class="text-2xl font-bold text-on-surface">Shipment Booked Successfully!</h2>
        <p class="text-xs text-on-surface-variant">Your waybill has been generated below. Print or save a copy for your courier driver.</p>
    </div>

    <!-- Official Waybill Document Card -->
    <div id="waybill-printable" class="bg-white p-6 rounded-xl border-2 border-primary shadow-sm max-w-2xl mx-auto text-on-surface font-sans">
        
        <!-- Waybill Header -->
        <div class="flex justify-between items-start border-b-2 border-primary pb-4 mb-4">
            <div>
                <img src='<%= ResolveUrl("~/Images/web/logo.png") %>' alt="Logisticore Courier Service" class="h-10 w-auto object-contain mb-1" />
                <p class="text-xs text-on-surface-variant font-semibold">WAYBILL & PROOF OF DELIVERY</p>
            </div>
            <div class="text-right">
                <span class="text-xs text-on-surface-variant block uppercase tracking-wider">Date Booked</span>
                <asp:Label ID="lblWaybillDate" runat="server" CssClass="text-sm font-bold text-on-surface" />
            </div>
        </div>

        <!-- Barcode Section -->
        <div class="text-center bg-surface-container-low p-4 rounded-lg border border-outline-variant mb-6">
            <div class="text-xs text-on-surface-variant font-semibold mb-1 uppercase tracking-wider">Tracking Number</div>
            <asp:Label ID="lblWaybillTracking" runat="server" Font-Bold="true" CssClass="font-mono text-2xl tracking-widest text-on-surface block mb-2" />
            
            <!-- Generated Barcode Image -->
            <svg id="waybillBarcode" class="mx-auto h-16 max-w-full"></svg>
        </div>

        <!-- Addresses Grid -->
        <div class="grid grid-cols-2 gap-4 border-b border-outline-variant pb-4 mb-4">
            <!-- Collection Address -->
            <div class="bg-surface-container-low p-3 rounded-lg border border-outline-variant">
                <span class="text-[10px] font-bold uppercase text-on-surface-variant block mb-1">1. Collection Address (From)</span>
                <div class="font-bold text-sm text-on-surface">
                    <asp:Label ID="lblWaybillSenderName" runat="server" Text="Sender" /><br />
                </div>
                <div class="text-xs text-on-surface mt-1 space-y-0.5">
                    <asp:Label ID="lblWaybillPickupStreet" runat="server" CssClass="block" /><br />
                    <asp:Label ID="lblWaybillPickupCity" runat="server" /> <asp:Label ID="lblWaybillPickupPostal" runat="server" />
                </div>
            </div>

            <!-- Delivery Address -->
            <div class="bg-surface-container-low p-3 rounded-lg border border-outline-variant">
                <span class="text-[10px] font-bold uppercase text-on-surface-variant block mb-1">2. Delivery Address (To)</span>
                <div class="font-bold text-sm text-on-surface">
                    <asp:Label ID="lblWaybillReceiverName" runat="server" Text="Recipient" /><br />
                </div>
                <div class="text-xs text-on-surface mt-1 space-y-0.5">
                    <asp:Label ID="lblWaybillDeliveryStreet" runat="server" CssClass="block" /><br />
                    <asp:Label ID="lblWaybillDeliveryCity" runat="server" /> <asp:Label ID="lblWaybillDeliveryPostal" runat="server" />
                </div>
            </div>
        </div>

        <!-- Parcel & Service Details Table -->
        <div class="grid grid-cols-2 md:grid-cols-4 gap-3 text-xs text-on-surface mb-6">
            <div class="p-2 border border-outline-variant rounded">
                <span class="text-[10px] font-semibold text-on-surface-variant block">Parcel Category</span>
                <asp:Label ID="lblWaybillSize" runat="server" Font-Bold="true" CssClass="text-on-surface" />
            </div>
            <div class="p-2 border border-outline-variant rounded">
                <span class="text-[10px] font-semibold text-on-surface-variant block">Dimensions</span>
                <asp:Label ID="lblWaybillDimensions" runat="server" Font-Bold="true" CssClass="text-on-surface" />
            </div>
            <div class="p-2 border border-outline-variant rounded">
                <span class="text-[10px] font-semibold text-on-surface-variant block">Payment Status</span>
                <span class="font-bold text-amber-600 uppercase">Invoiced &ndash; Unpaid</span>
            </div>
            <div class="p-2 border border-outline-variant rounded">
                <span class="text-[10px] font-semibold text-on-surface-variant block">Amount Due</span>
                <asp:Label ID="lblWaybillAmount" runat="server" Font-Bold="true" CssClass="text-on-surface" />
            </div>
        </div>

        <!-- Driver Signature Box -->
        <div class="grid grid-cols-2 gap-4 pt-2 border-t border-dashed border-outline-variant text-xs text-on-surface-variant">
            <div>
                <span class="block text-[10px] uppercase font-semibold">Driver Signature:</span>
                <div class="h-8 border-b border-outline mt-1"></div>
            </div>
            <div>
                <span class="block text-[10px] uppercase font-semibold">Receiver Signature:</span>
                <div class="h-8 border-b border-outline mt-1"></div>
            </div>
        </div>
    </div>

    <!-- Actions Bar (Hidden on print) -->
    <div class="flex justify-center gap-4 mt-6 no-print">
        <button type="button" onclick="window.print()" class="px-5 py-2.5 bg-surface-container-high border border-outline-variant text-on-surface font-semibold text-sm rounded-lg hover:bg-surface-container-highest transition-colors cursor-pointer flex items-center gap-2">
            <span class="material-symbols-outlined text-base">print</span> Print Waybill
        </button>
        <a href="Dashboard.aspx" class="px-6 py-2.5 bg-[#1d2b45] text-white font-semibold text-sm rounded-lg hover:bg-[#152035] transition-colors shadow-sm flex items-center gap-2">
            Return to Dashboard
        </a>
    </div>
</asp:View>

            </asp:MultiView>
        </div>
    </div>

    <script>
        function setSize(val) {
            document.getElementById('<%= hfSelectedSize.ClientID %>').value = val;
        }
    </script>
</asp:Content>