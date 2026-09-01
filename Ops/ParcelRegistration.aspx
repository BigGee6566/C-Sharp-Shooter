<%@ Page Title="Parcel Registration" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="ParcelRegistration.aspx.cs" Inherits="ForteLogisticsApp.Ops.ParcelRegistration" %>

<asp:Content ID="c1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="max-w-5xl mx-auto px-4 py-8">
        <p class="text-xs text-on-surface-variant mb-1"><span class="material-symbols-outlined mr-1">home</span> Home / <a href='<%= ResolveUrl("~/Ops/Dashboard.aspx") %>' class="hover:text-brandRed">Operations</a> / <span class="text-brandRed font-semibold">Parcel Registration</span></p>
        <h1 class="text-2xl font-black text-brandDark mb-1">Parcel Registration & Barcode</h1>
        <p class="text-on-surface-variant text-sm mb-6">Capture a walk-in parcel at intake. A unique waybill and scannable barcode are generated automatically.</p>

        <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
            <div class="lg:col-span-2 fcl-card rounded-xl p-6">
                <%-- Matching the sender to an account is what lets automatic invoicing and
                     status notifications reach them, exactly as for an online booking. --%>
                <div class="mb-4 pb-4 border-b border-outline-variant">
                    <label class="block text-xs font-bold text-on-surface uppercase mb-1">Sender Account Lookup (email or phone)</label>
                    <div class="flex gap-2">
                        <asp:TextBox ID="txtSenderLookup" runat="server" CssClass="fcl-input"
                            placeholder="customer@fcl.co.za or 0821234567"></asp:TextBox>
                        <asp:Button ID="btnLookupSender" runat="server" Text="Match" CausesValidation="false"
                            OnClick="btnLookupSender_Click" CssClass="fcl-btn fcl-btn-dark" />
                    </div>
                    <asp:HiddenField ID="hfSenderId" runat="server" />
                    <asp:Label ID="lblSenderMatch" runat="server" CssClass="block text-xs mt-1"></asp:Label>
                </div>

                <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <div>
                        <label class="block text-xs font-bold text-on-surface uppercase mb-1">Sender Name *</label>
                        <asp:TextBox ID="txtSender" runat="server" CssClass="fcl-input"></asp:TextBox>
                        <asp:RequiredFieldValidator runat="server" ControlToValidate="txtSender" ValidationGroup="reg"
                            Display="Dynamic" ErrorMessage="Sender name is required."
                            CssClass="block text-xs mt-1 text-error font-bold" />
                    </div>
                    <div>
                        <label class="block text-xs font-bold text-on-surface uppercase mb-1">Recipient Name *</label>
                        <asp:TextBox ID="txtReceiver" runat="server" CssClass="fcl-input"></asp:TextBox>
                        <asp:RequiredFieldValidator runat="server" ControlToValidate="txtReceiver" ValidationGroup="reg"
                            Display="Dynamic" ErrorMessage="Recipient name is required."
                            CssClass="block text-xs mt-1 text-error font-bold" />
                    </div>
                    <div>
                        <label class="block text-xs font-bold text-on-surface uppercase mb-1">Pickup Address</label>
                        <asp:TextBox ID="txtPickup" runat="server" CssClass="fcl-input"></asp:TextBox>
                    </div>
                    <div>
                        <label class="block text-xs font-bold text-on-surface uppercase mb-1">Delivery Address *</label>
                        <asp:TextBox ID="txtDelivery" runat="server" CssClass="fcl-input"></asp:TextBox>
                        <asp:RequiredFieldValidator runat="server" ControlToValidate="txtDelivery" ValidationGroup="reg"
                            Display="Dynamic" ErrorMessage="Delivery address is required."
                            CssClass="block text-xs mt-1 text-error font-bold" />
                    </div>
                    <div>
                        <label class="block text-xs font-bold text-on-surface uppercase mb-1">Origin City</label>
                        <asp:DropDownList ID="ddlOrigin" runat="server" CssClass="fcl-input"></asp:DropDownList>
                    </div>
                    <div>
                        <label class="block text-xs font-bold text-on-surface uppercase mb-1">Destination City</label>
                        <asp:DropDownList ID="ddlDest" runat="server" CssClass="fcl-input"></asp:DropDownList>
                    </div>
                    <div>
                        <label class="block text-xs font-bold text-on-surface uppercase mb-1">Weight (KG) *</label>
                        <asp:TextBox ID="txtWeight" runat="server" TextMode="Number" Text="1" CssClass="fcl-input"></asp:TextBox>
                        <%-- A RangeValidator parses its bounds with the server culture; under en-ZA
                         "0.1" is not a valid number and the page throws. A pattern check is
                         culture-proof and accepts either decimal separator. --%>
                    <asp:RegularExpressionValidator runat="server" ControlToValidate="txtWeight" ValidationGroup="reg"
                        Display="Dynamic" ValidationExpression="^\d{1,4}([.,]\d{1,3})?$"
                        ErrorMessage="Enter a weight in kg, e.g. 3.5"
                        CssClass="block text-xs mt-1 text-error font-bold" />
                    </div>
                    <div>
                        <label class="block text-xs font-bold text-on-surface uppercase mb-1">Parcel Description</label>
                        <asp:TextBox ID="txtDescription" runat="server" CssClass="fcl-input"></asp:TextBox>
                    </div>
                </div>
                <div class="mt-6 flex items-center gap-3">
                    <asp:Button ID="btnRegister" runat="server" Text="Register Parcel" OnClick="btnRegister_Click"
                        ValidationGroup="reg" CssClass="fcl-btn" />
                    <asp:Label ID="lblStatus" runat="server" CssClass="text-sm"></asp:Label>
                </div>
            </div>

            <!-- Printable label -->
            <div class="fcl-card rounded-xl p-6">
                <h2 class="font-bold text-lg text-brandDark mb-3">Parcel Label</h2>
                <asp:Panel ID="pnlLabel" runat="server" Visible="false">
                    <div id="labelArea" class="border-2 border-dashed border-outline-variant rounded-lg p-4 text-center">
                        <div class="bg-brandRed text-white font-black inline-block px-2 py-1 rounded text-sm mb-2">FCL</div>
                        <div class="text-xs text-on-surface-variant uppercase">Waybill</div>
                        <div class="font-black text-brandDark text-lg mb-2"><asp:Literal ID="litWaybill" runat="server"></asp:Literal></div>
                        <svg id="barcode"></svg>
                        <div class="text-xs text-on-surface-variant mt-2"><asp:Literal ID="litRoute" runat="server"></asp:Literal></div>
                    </div>
                    <button type="button" onclick="printLabel()" class="fcl-btn fcl-btn-dark w-full mt-4"><span class="material-symbols-outlined mr-1">print</span> Print Label</button>
                    <script>
                        (function () {
                            var v = '<asp:Literal id="litBarcodeVal" runat="server"></asp:Literal>';
                            if (v && window.JsBarcode) { JsBarcode('#barcode', v, { format: 'CODE128', height: 55, displayValue: false }); }
                        })();
                        function printLabel() {
                            var w = window.open('', '', 'width=420,height=360');
                            w.document.write('<html><head><title>FCL Label</title></head><body style="text-align:center;font-family:sans-serif">' + document.getElementById('labelArea').outerHTML + '</body></html>');
                            w.document.close(); w.focus(); w.print();
                        }
                    </script>
                </asp:Panel>
                <asp:Panel ID="pnlNoLabel" runat="server" CssClass="text-center text-outline text-sm py-8">
                    <span class="material-symbols-outlined ms-32 mb-2">barcode</span>
                    <p>Register a parcel to generate its barcode label.</p>
                </asp:Panel>
            </div>
        </div>
    </div>
</asp:Content>
