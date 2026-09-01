<%@ Page Title="Contact Us" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="Contact.aspx.cs" Inherits="ForteLogisticsApp.ContactPage" %>

<asp:Content ID="c1" ContentPlaceHolderID="MainContent" runat="server">
    <section class="py-14 px-4 max-w-5xl mx-auto">
        <div class="text-center mb-10">
            <span class="text-brandRed font-bold text-xs uppercase tracking-widest">Get in touch</span>
            <h1 class="text-3xl font-black text-brandDark mt-1">Contact Logisticore Courier Service</h1>
            <p class="text-gray-600 text-sm mt-2">Questions about a delivery, a quote or a business account? We're here to help.</p>
        </div>

        <div class="grid grid-cols-1 md:grid-cols-3 gap-8">
            <div class="fcl-card rounded-xl p-6 md:col-span-2">
                <h2 class="font-bold text-lg text-brandDark mb-4">Send us a message</h2>
                <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <div>
                        <label class="block text-xs font-bold text-gray-700 uppercase mb-1">Your Name</label>
                        <asp:TextBox ID="txtName" runat="server" CssClass="fcl-input"></asp:TextBox>
                        <asp:RequiredFieldValidator runat="server" ControlToValidate="txtName" ValidationGroup="contact"
                            Display="Dynamic" ErrorMessage="Please enter your name."
                            CssClass="block text-xs mt-1 text-red-600 font-bold" />
                    </div>
                    <div>
                        <label class="block text-xs font-bold text-gray-700 uppercase mb-1">Email</label>
                        <asp:TextBox ID="txtEmail" runat="server" CssClass="fcl-input" TextMode="Email"></asp:TextBox>
                        <asp:RequiredFieldValidator runat="server" ControlToValidate="txtEmail" ValidationGroup="contact"
                            Display="Dynamic" ErrorMessage="We need an email address to reply to."
                            CssClass="block text-xs mt-1 text-red-600 font-bold" />
                        <asp:RegularExpressionValidator runat="server" ControlToValidate="txtEmail" ValidationGroup="contact"
                            Display="Dynamic" ValidationExpression="^[^@\s]+@[^@\s]+\.[^@\s]{2,}$"
                            ErrorMessage="Enter a valid email address."
                            CssClass="block text-xs mt-1 text-red-600 font-bold" />
                    </div>
                </div>
                <div class="mt-4">
                    <label class="block text-xs font-bold text-gray-700 uppercase mb-1">Message</label>
                    <asp:TextBox ID="txtMessage" runat="server" CssClass="fcl-input" TextMode="MultiLine" Rows="5"></asp:TextBox>
                    <asp:RequiredFieldValidator runat="server" ControlToValidate="txtMessage" ValidationGroup="contact"
                        Display="Dynamic" ErrorMessage="Please enter a message."
                        CssClass="block text-xs mt-1 text-red-600 font-bold" />
                </div>
                <div class="mt-4">
                    <asp:Button ID="btnSend" runat="server" Text="Send Message" OnClick="btnSend_Click"
                        ValidationGroup="contact" CssClass="fcl-btn" />
                    <asp:Label ID="lblStatus" runat="server" CssClass="block text-xs mt-3"></asp:Label>
                </div>
            </div>

            <div class="fcl-card rounded-xl p-6 space-y-4 text-sm">
                <div>
                    <div class="text-xs font-bold uppercase text-gray-500">Call Centre</div>
                    <p class="text-brandDark font-semibold"><span class="material-symbols-outlined text-brandRed mr-2">call</span>0860 LOGISTICORE</p>
                </div>
                <div>
                    <div class="text-xs font-bold uppercase text-gray-500">Email</div>
                    <p class="text-brandDark font-semibold"><span class="material-symbols-outlined text-brandRed mr-2">mail</span>support@logisticore.co.za</p>
                </div>
                <div>
                    <div class="text-xs font-bold uppercase text-gray-500">Head Office</div>
                    <p class="text-brandDark font-semibold"><span class="material-symbols-outlined text-brandRed mr-2">location_on</span>Isando, Gauteng</p>
                </div>
                <div>
                    <div class="text-xs font-bold uppercase text-gray-500">Hours</div>
                    <p class="text-brandDark font-semibold"><span class="material-symbols-outlined text-brandRed mr-2">schedule</span>Mon - Fri: 07:00 - 18:00</p>
                </div>
            </div>
        </div>
    </section>
</asp:Content>
