<%@ Page Title="My Invoices" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="Invoices.aspx.cs" Inherits="ForteLogisticsApp.CustomerPortal.Invoices" %>

<asp:Content ID="c1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="max-w-6xl mx-auto px-4 py-8">
        <p class="text-xs text-on-surface-variant mb-1"><span class="material-symbols-outlined mr-1">home</span> Home / <a href='<%= ResolveUrl("~/Customer/Dashboard.aspx") %>' class="hover:text-brandRed">Dashboard</a> / <span class="text-brandRed font-semibold">My Invoices</span></p>
        <h1 class="text-2xl font-black text-brandDark mb-1">My Invoices</h1>
        <p class="text-on-surface-variant text-sm mb-6">
            <asp:Literal ID="litScopeHint" runat="server" />
        </p>

        <asp:HiddenField ID="hfIdempotencyKey" runat="server" />

        <div class="fcl-card rounded-xl p-6 overflow-x-auto">
            <asp:Label ID="lblMsg" runat="server" CssClass="block text-sm mb-3"></asp:Label>

            <asp:Panel ID="pnlReceipt" runat="server" Visible="false" CssClass="rounded-lg bg-emerald-50 text-emerald-900 text-sm p-4 mb-4">
                <span class="material-symbols-outlined align-middle mr-1">check_circle</span>
                <asp:Literal ID="litReceipt" runat="server" />
            </asp:Panel>

            <asp:Repeater ID="rptInvoices" runat="server" OnItemDataBound="rptInvoices_ItemDataBound">
                <HeaderTemplate>
                    <table class="w-full text-sm text-left">
                        <thead>
                            <tr class="text-xs uppercase text-on-surface-variant border-b">
                                <th class="py-2 pr-3"><input type="checkbox" id="chkSelectAll" onclick="fclInvoices.toggleAll(this)" /></th>
                                <th class="py-2 pr-3">Invoice #</th>
                                <th class="py-2 pr-3">Waybill</th>
                                <th class="py-2 pr-3">Client</th>
                                <th class="py-2 pr-3">Amount</th>
                                <th class="py-2 pr-3">Status</th>
                                <th class="py-2 pr-3">Issued</th>
                                <th class="py-2 pr-3"></th>
                            </tr>
                        </thead>
                        <tbody>
                </HeaderTemplate>
                <ItemTemplate>
                    <tr class="border-b border-outline-variant">
                        <td class="py-2 pr-3">
                            <asp:HiddenField ID="hfPaymentId" runat="server" Value='<%# Eval("PaymentID") %>' />
                            <asp:CheckBox ID="chkSelect" runat="server"
                                Enabled='<%# Eval("PaymentStatus").ToString() == "Unpaid" %>'
                                CssClass="fcl-invoice-check"
                                onclick="fclInvoices.recalc()" />
                        </td>
                        <td class="py-2 pr-3 font-semibold text-brandDark"><%# UiHelper.Enc(Eval("InvoiceNumber")) %></td>
                        <td class="py-2 pr-3"><%# UiHelper.Enc(Eval("TrackingNumber")) %></td>
                        <td class="py-2 pr-3 text-on-surface-variant"><%# UiHelper.Enc(Eval("ClientName")) %></td>
                        <td class="py-2 pr-3">R <%# Eval("Amount", "{0:N2}") %></td>
                        <td class="py-2 pr-3"><%# ForteLogisticsApp.Services.UiHelper.StatusBadge(Eval("PaymentStatus").ToString()) %></td>
                        <td class="py-2 pr-3 text-on-surface-variant"><%# Eval("IssuedDate", "{0:dd MMM yyyy}") %></td>
                        <td class="py-2 pr-3">
                            <a href='<%# ResolveUrl("~/Invoice.aspx?id=" + Eval("PaymentID")) %>' target="_blank" class="text-on-surface-variant hover:underline">View</a>
                        </td>
                    </tr>
                </ItemTemplate>
                <FooterTemplate></tbody></table></FooterTemplate>
            </asp:Repeater>

            <asp:Panel ID="pnlNone" runat="server" Visible="false" CssClass="text-center text-on-surface-variant text-sm py-8">
                <span class="material-symbols-outlined ms-32 mb-2">description</span>
                <p>You have no invoices yet.</p>
            </asp:Panel>

            <asp:Panel ID="pnlPayBar" runat="server" CssClass="mt-6 pt-4 border-t border-outline-variant flex flex-wrap items-center gap-4">
                <div class="text-sm">
                    <span id="fclSelectedCount" class="font-bold text-brandDark">0</span> selected &middot;
                    Total <span id="fclSelectedTotal" class="font-bold text-brandDark">R 0.00</span>
                </div>
                <div class="flex items-center gap-2">
                    <label class="text-xs font-bold text-on-surface uppercase">Method</label>
                    <asp:DropDownList ID="ddlMethod" runat="server" CssClass="fcl-input">
                        <asp:ListItem Text="Card" Value="Card" />
                        <asp:ListItem Text="EFT" Value="EFT" />
                        <asp:ListItem Text="Wallet" Value="Wallet" />
                    </asp:DropDownList>
                </div>
                <asp:Button ID="btnClearSelection" runat="server" Text="Clear Selection" UseSubmitBehavior="false" OnClientClick="fclInvoices.clearAll(); return false;" CssClass="fcl-btn fcl-btn-dark" />
                <asp:Button ID="btnPaySelected" runat="server" Text="Pay Selected" OnClick="btnPaySelected_Click" CssClass="fcl-btn fcl-btn-primary" />
                <asp:Button ID="btnPayAllUnpaid" runat="server" Text="Pay All Unpaid" OnClick="btnPayAllUnpaid_Click" CssClass="fcl-btn" CausesValidation="false" />
            </asp:Panel>
            <p class="text-xs text-on-surface-variant mt-2">Sandbox / test payment only - no card details are collected or stored, and no real money moves.</p>
        </div>
    </div>

    <script>
        var fclInvoices = {
            checks: function () { return document.querySelectorAll('.fcl-invoice-check'); },
            toggleAll: function (master) {
                var boxes = this.checks();
                for (var i = 0; i < boxes.length; i++) {
                    if (!boxes[i].disabled) boxes[i].checked = master.checked;
                }
                this.recalc();
            },
            clearAll: function () {
                var boxes = this.checks();
                for (var i = 0; i < boxes.length; i++) boxes[i].checked = false;
                var master = document.getElementById('chkSelectAll');
                if (master) master.checked = false;
                this.recalc();
            },
            recalc: function () {
                var boxes = this.checks();
                var count = 0, total = 0;
                for (var i = 0; i < boxes.length; i++) {
                    if (boxes[i].checked) {
                        count++;
                        total += parseFloat(boxes[i].getAttribute('data-amount')) || 0;
                    }
                }
                var countEl = document.getElementById('fclSelectedCount');
                var totalEl = document.getElementById('fclSelectedTotal');
                if (countEl) countEl.textContent = count;
                if (totalEl) totalEl.textContent = 'R ' + total.toFixed(2);
            }
        };
        document.addEventListener('DOMContentLoaded', function () { fclInvoices.recalc(); });
    </script>
</asp:Content>
