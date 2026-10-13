<%@ Page Title="Pay for Shipment" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="Payment.aspx.cs" Inherits="ForteLogisticsApp.CustomerPortal.Payment" %>

<asp:Content ID="c1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="max-w-2xl mx-auto px-4 py-8">
        <p class="text-xs text-on-surface-variant mb-1"><span class="material-symbols-outlined mr-1">home</span> Home / <a href='<%= ResolveUrl("~/Customer/Dashboard.aspx") %>' class="hover:text-brandRed">Dashboard</a> / <span class="text-brandRed font-semibold">Payment</span></p>
        <h1 class="text-2xl font-black text-brandDark mb-1">Payment required</h1>
        <p class="text-on-surface-variant text-sm mb-6">Your parcel will not be dispatched until this invoice is paid.</p>

        <asp:HiddenField ID="hfIdempotencyKey" runat="server" />
        <asp:HiddenField ID="hfShipmentId" runat="server" />

        <asp:Panel ID="pnlNotEligible" runat="server" Visible="false" CssClass="fcl-card rounded-xl p-6">
            <p class="text-sm text-on-surface-variant"><asp:Literal ID="litNotEligibleMsg" runat="server" /></p>
            <a href='<%= ResolveUrl("~/Customer/Invoices.aspx") %>' class="inline-block mt-4 text-brandRed font-bold hover:underline">Go to My Invoices</a>
        </asp:Panel>

        <asp:Panel ID="pnlPay" runat="server" CssClass="fcl-card rounded-xl p-6">
            <div class="rounded-lg bg-surface-container p-4 mb-5 text-sm space-y-1">
                <div class="flex justify-between"><span class="text-on-surface-variant">Waybill</span><span class="font-semibold"><asp:Literal ID="litTracking" runat="server" /></span></div>
                <div class="flex justify-between"><span class="text-on-surface-variant">Invoice</span><span class="font-semibold"><asp:Literal ID="litInvoiceNumber" runat="server" /></span></div>
                <div class="flex justify-between"><span class="text-on-surface-variant">Route</span><span><asp:Literal ID="litRoute" runat="server" /></span></div>
                <div class="flex justify-between text-base pt-2 border-t border-outline-variant mt-2"><span class="font-bold">Total payable</span><span class="font-black text-brandDark"><asp:Literal ID="litAmount" runat="server" /></span></div>
            </div>

            <asp:Label ID="lblStatus" runat="server" CssClass="block text-sm mb-4"></asp:Label>

            <div class="mb-4">
                <label class="block text-label-sm font-semibold text-on-surface-variant mb-2" for="<%= ddlMethod.ClientID %>">Payment method</label>
                <asp:DropDownList ID="ddlMethod" runat="server" CssClass="fcl-input w-full" onchange="fclShowMethod(this.value)">
                    <asp:ListItem Text="Card" Value="Card" />
                    <asp:ListItem Text="EFT / Bank transfer" Value="EFT" />
                    <asp:ListItem Text="Mobile wallet" Value="Wallet" />
                </asp:DropDownList>
            </div>

            <!-- Card details. Card number and CVV are deliberately kept out of ViewState
                 (EnableViewState="false") so they are never round-tripped or persisted; the
                 server keeps only a masked "ending 1234" summary. -->
            <div id="pm-card" class="fcl-pm-panel space-y-3 mb-4">
                <div>
                    <label class="block text-label-sm font-semibold text-on-surface-variant mb-1" for="<%= txtCardName.ClientID %>">Cardholder name</label>
                    <asp:TextBox ID="txtCardName" runat="server" CssClass="fcl-input w-full" autocomplete="off" placeholder="Name as printed on the card" />
                </div>
                <div>
                    <label class="block text-label-sm font-semibold text-on-surface-variant mb-1" for="<%= txtCardNumber.ClientID %>">Card number</label>
                    <asp:TextBox ID="txtCardNumber" runat="server" CssClass="fcl-input w-full" MaxLength="23" EnableViewState="false" autocomplete="off" inputmode="numeric" placeholder="4242 4242 4242 4242" />
                </div>
                <div class="grid grid-cols-2 gap-3">
                    <div>
                        <label class="block text-label-sm font-semibold text-on-surface-variant mb-1" for="<%= txtCardExpiry.ClientID %>">Expiry (MM/YY)</label>
                        <asp:TextBox ID="txtCardExpiry" runat="server" CssClass="fcl-input w-full" MaxLength="5" autocomplete="off" inputmode="numeric" placeholder="MM/YY" />
                    </div>
                    <div>
                        <label class="block text-label-sm font-semibold text-on-surface-variant mb-1" for="<%= txtCardCvv.ClientID %>">CVV</label>
                        <asp:TextBox ID="txtCardCvv" runat="server" CssClass="fcl-input w-full" MaxLength="4" EnableViewState="false" autocomplete="off" inputmode="numeric" placeholder="123" />
                    </div>
                </div>
            </div>

            <!-- EFT / bank transfer details. Account number is kept out of ViewState
                 (EnableViewState="false"); only a masked summary is ever stored. -->
            <div id="pm-eft" class="fcl-pm-panel space-y-3 mb-4" style="display:none">
                <div>
                    <label class="block text-label-sm font-semibold text-on-surface-variant mb-1" for="<%= ddlBank.ClientID %>">Your bank</label>
                    <asp:DropDownList ID="ddlBank" runat="server" CssClass="fcl-input w-full">
                        <asp:ListItem Text="ABSA" Value="ABSA" />
                        <asp:ListItem Text="Capitec" Value="Capitec" />
                        <asp:ListItem Text="FNB" Value="FNB" />
                        <asp:ListItem Text="Nedbank" Value="Nedbank" />
                        <asp:ListItem Text="Standard Bank" Value="Standard Bank" />
                        <asp:ListItem Text="TymeBank" Value="TymeBank" />
                    </asp:DropDownList>
                </div>
                <div>
                    <label class="block text-label-sm font-semibold text-on-surface-variant mb-1" for="<%= txtEftHolder.ClientID %>">Account holder</label>
                    <asp:TextBox ID="txtEftHolder" runat="server" CssClass="fcl-input w-full" autocomplete="off" placeholder="Name on the account" />
                </div>
                <div class="grid grid-cols-2 gap-3">
                    <div>
                        <label class="block text-label-sm font-semibold text-on-surface-variant mb-1" for="<%= txtEftAccount.ClientID %>">Account number</label>
                        <asp:TextBox ID="txtEftAccount" runat="server" CssClass="fcl-input w-full" MaxLength="20" EnableViewState="false" autocomplete="off" inputmode="numeric" placeholder="1234567890" />
                    </div>
                    <div>
                        <label class="block text-label-sm font-semibold text-on-surface-variant mb-1" for="<%= txtEftBranch.ClientID %>">Branch code</label>
                        <asp:TextBox ID="txtEftBranch" runat="server" CssClass="fcl-input w-full" MaxLength="8" autocomplete="off" inputmode="numeric" placeholder="250655" />
                    </div>
                </div>
                <p class="text-xs text-on-surface-variant">A unique payment reference is generated for your transfer once you confirm.</p>
            </div>

            <!-- Mobile wallet details -->
            <div id="pm-wallet" class="fcl-pm-panel space-y-3 mb-4" style="display:none">
                <div>
                    <label class="block text-label-sm font-semibold text-on-surface-variant mb-1" for="<%= ddlWallet.ClientID %>">Wallet provider</label>
                    <asp:DropDownList ID="ddlWallet" runat="server" CssClass="fcl-input w-full">
                        <asp:ListItem Text="SnapScan" Value="SnapScan" />
                        <asp:ListItem Text="Zapper" Value="Zapper" />
                        <asp:ListItem Text="MTN MoMo" Value="MTN MoMo" />
                        <asp:ListItem Text="Apple Pay" Value="Apple Pay" />
                    </asp:DropDownList>
                </div>
                <div>
                    <label class="block text-label-sm font-semibold text-on-surface-variant mb-1" for="<%= txtWalletPhone.ClientID %>">Mobile number</label>
                    <asp:TextBox ID="txtWalletPhone" runat="server" TextMode="Phone" CssClass="fcl-input w-full" autocomplete="off" placeholder="071 234 5678" />
                </div>
            </div>

            <div class="mb-1 rounded-md bg-amber-50 text-amber-900 text-xs px-3 py-2">
                Sandbox / test payment only - no real money moves. Card details are validated but never
                stored: only a masked summary (e.g. "card ending 4242") is kept. Use card
                <strong>4000 0000 0000 0002</strong> to test a declined payment.
            </div>

            <asp:Button ID="btnPay" runat="server" Text="Pay now (sandbox)" OnClick="btnPay_Click" OnClientClick="return fclValidatePayment();" CssClass="fcl-btn fcl-btn-primary w-full mt-4" />
        </asp:Panel>

        <asp:Panel ID="pnlReceipt" runat="server" Visible="false" CssClass="fcl-card rounded-xl p-6">
            <div class="text-center mb-4">
                <span class="material-symbols-outlined text-emerald-600" style="font-size:48px;">check_circle</span>
                <h2 class="text-xl font-black text-brandDark mt-2">Payment successful</h2>
                <p class="text-on-surface-variant text-sm">Your shipment is confirmed and ready for processing.</p>
            </div>
            <div class="rounded-lg bg-surface-container p-4 text-sm space-y-1">
                <div class="flex justify-between"><span class="text-on-surface-variant">Waybill</span><span class="font-semibold"><asp:Literal ID="litReceiptTracking" runat="server" /></span></div>
                <div class="flex justify-between"><span class="text-on-surface-variant">Invoice</span><span class="font-semibold"><asp:Literal ID="litReceiptInvoice" runat="server" /></span></div>
                <div class="flex justify-between"><span class="text-on-surface-variant">Reference</span><span class="font-semibold"><asp:Literal ID="litReceiptReference" runat="server" /></span></div>
                <div class="flex justify-between"><span class="text-on-surface-variant">Method</span><span class="font-semibold"><asp:Literal ID="litReceiptMethod" runat="server" /></span></div>
                <div class="flex justify-between"><span class="text-on-surface-variant">Amount paid</span><span class="font-semibold"><asp:Literal ID="litReceiptAmount" runat="server" /></span></div>
            </div>
            <a href='<%= ResolveUrl("~/Customer/Track.aspx") %>' class="inline-block mt-5 text-brandRed font-bold hover:underline">Track your parcel</a>
        </asp:Panel>
    </div>

    <script>
        // Show only the detail panel for the chosen method.
        function fclShowMethod(method) {
            var panels = { 'Card': 'pm-card', 'EFT': 'pm-eft', 'Wallet': 'pm-wallet' };
            for (var key in panels) {
                var el = document.getElementById(panels[key]);
                if (el) el.style.display = (key === method) ? '' : 'none';
            }
        }

        // Luhn checksum - the standard card-number sanity check.
        function fclLuhn(digits) {
            var sum = 0, alt = false;
            for (var i = digits.length - 1; i >= 0; i--) {
                var n = parseInt(digits.charAt(i), 10);
                if (alt) { n *= 2; if (n > 9) n -= 9; }
                sum += n; alt = !alt;
            }
            return digits.length > 0 && (sum % 10 === 0);
        }

        // Client-side guard. The server re-validates everything in btnPay_Click, so this is
        // only to catch mistakes early - it is never the security boundary.
        function fclValidatePayment() {
            var method = document.getElementById('<%= ddlMethod.ClientID %>').value;
            function val(id) { var e = document.getElementById(id); return e ? e.value.trim() : ''; }
            function warn(msg) { alert(msg); return false; }

            if (method === 'Card') {
                if (val('<%= txtCardName.ClientID %>').length < 2) return warn('Please enter the cardholder name.');
                var num = val('<%= txtCardNumber.ClientID %>').replace(/[\s-]/g, '');
                if (!/^\d{13,19}$/.test(num) || !fclLuhn(num)) return warn('Please enter a valid card number.');
                var exp = val('<%= txtCardExpiry.ClientID %>');
                var m = exp.match(/^(\d{2})\/(\d{2})$/);
                if (!m || +m[1] < 1 || +m[1] > 12) return warn('Please enter the expiry as MM/YY.');
                var now = new Date(), yr = 2000 + (+m[2]), exd = new Date(yr, +m[1], 1);
                if (exd <= now) return warn('That card has expired.');
                if (!/^\d{3,4}$/.test(val('<%= txtCardCvv.ClientID %>'))) return warn('Please enter the 3 or 4 digit CVV.');
            } else if (method === 'EFT') {
                if (val('<%= txtEftHolder.ClientID %>').length < 2) return warn('Please enter the account holder name.');
                if (!/^\d{6,20}$/.test(val('<%= txtEftAccount.ClientID %>').replace(/\s/g, ''))) return warn('Please enter a valid account number.');
                if (!/^\d{4,8}$/.test(val('<%= txtEftBranch.ClientID %>').replace(/\s/g, ''))) return warn('Please enter a valid branch code.');
            } else if (method === 'Wallet') {
                var phone = val('<%= txtWalletPhone.ClientID %>').replace(/[\s-]/g, '');
                if (!/^\+?\d{9,15}$/.test(phone)) return warn('Please enter the mobile number for your wallet.');
            }
            return true;
        }

        // Reflect the current selection on load (handles a postback that kept a selection).
        (function () {
            var sel = document.getElementById('<%= ddlMethod.ClientID %>');
            if (sel) fclShowMethod(sel.value);
        })();
    </script>
</asp:Content>
