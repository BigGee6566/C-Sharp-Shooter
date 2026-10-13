<%@ Page Title="Invoice" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="Invoice.aspx.cs" Inherits="ForteLogisticsApp.InvoicePage" %>

<asp:Content ID="c1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="max-w-3xl mx-auto px-4 py-8">
        <div class="no-print flex justify-between items-center mb-4">
            <a href="javascript:history.back()" class="text-sm text-on-surface-variant hover:text-brandRed"><span class="material-symbols-outlined mr-1">arrow_back</span> Back</a>
            <button type="button" onclick="window.print()" class="fcl-btn"><span class="material-symbols-outlined mr-1">print</span> Print</button>
        </div>

        <asp:Panel ID="pnlInvoice" runat="server" CssClass="fcl-card rounded-xl p-8 bg-white">
            <div class="flex justify-between items-start border-b border-outline-variant pb-4 mb-6">
                <div>
                    <img src='<%= ResolveUrl("~/Images/web/logo.png") %>' alt="Logisticore Courier Service" class="h-16 w-auto object-contain" />
                    <div class="text-xs text-on-surface-variant mt-1">Isando, Gauteng &middot; support@logisticore.co.za</div>
                </div>
                <div class="text-right">
                    <div class="text-2xl font-black text-brandDark">INVOICE</div>
                    <div class="text-sm text-on-surface-variant"><asp:Literal ID="litInvoiceNo" runat="server"></asp:Literal></div>
                    <div class="text-xs text-on-surface-variant"><asp:Literal ID="litDate" runat="server"></asp:Literal></div>
                </div>
            </div>

            <div class="grid grid-cols-2 gap-6 mb-6 text-sm">
                <div>
                    <div class="text-xs font-bold uppercase text-on-surface-variant mb-1">Billed To</div>
                    <div class="font-semibold text-brandDark"><asp:Literal ID="litClient" runat="server"></asp:Literal></div>
                    <div class="text-on-surface-variant"><asp:Literal ID="litClientEmail" runat="server"></asp:Literal></div>
                </div>
                <div class="text-right">
                    <div class="text-xs font-bold uppercase text-on-surface-variant mb-1">Status</div>
                    <asp:Literal ID="litStatus" runat="server"></asp:Literal>
                </div>
            </div>

            <table class="w-full text-sm mb-6">
                <thead>
                    <tr class="text-xs uppercase text-on-surface-variant border-b">
                        <th class="py-2 text-left">Description</th>
                        <th class="py-2 text-left">Waybill</th>
                        <th class="py-2 text-right">Amount</th>
                    </tr>
                </thead>
                <tbody>
                    <tr class="border-b border-outline-variant">
                        <td class="py-3">
                            <asp:Literal ID="litDesc" runat="server"></asp:Literal>
                            <span class="block text-xs text-on-surface-variant mt-1">
                                <asp:Literal ID="litParcelSpec" runat="server"></asp:Literal></span>
                        </td>
                        <td class="py-3"><asp:Literal ID="litWaybill" runat="server"></asp:Literal></td>
                        <td class="py-3 text-right">R <asp:Literal ID="litAmount" runat="server"></asp:Literal></td>
                    </tr>
                </tbody>
            </table>

            <div class="flex justify-end">
                <div class="w-64 text-sm">
                    <div class="flex justify-between py-1"><span class="text-on-surface-variant">Subtotal</span><span>R <asp:Literal ID="litSubtotal" runat="server"></asp:Literal></span></div>
                    <div class="flex justify-between py-1 font-black text-brandDark border-t border-outline-variant mt-1 pt-2"><span>Total Due</span><span>R <asp:Literal ID="litTotal" runat="server"></asp:Literal></span></div>
                </div>
            </div>
        </asp:Panel>

        <asp:Panel ID="pnlNotFound" runat="server" Visible="false" CssClass="text-center text-on-surface-variant py-16">
            <span class="material-symbols-outlined ms-32 mb-3">plagiarism</span>
            <p>Invoice not found or you don't have permission to view it.</p>
        </asp:Panel>
    </div>
</asp:Content>
