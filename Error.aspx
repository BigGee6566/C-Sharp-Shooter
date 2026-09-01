<%@ Page Title="Something went wrong" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="Error.aspx.cs" Inherits="ForteLogisticsApp.ErrorPage" %>

<asp:Content ID="c1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="max-w-xl mx-auto px-4 py-16 text-center">
        <span class="material-symbols-outlined ms-32 text-brandRed mb-4">warning</span>
        <h1 class="text-2xl font-black text-brandDark mb-2">Something went wrong</h1>
        <p class="text-on-surface-variant text-sm mb-6">
            We could not complete that request. The problem has been recorded and our team
            can look into it. Please try again, or contact support if it keeps happening.
        </p>
        <p class="text-xs text-on-surface-variant mb-6">
            Reference: <span class="font-mono font-bold text-brandDark"><asp:Literal ID="litRef" runat="server"></asp:Literal></span>
        </p>
        <div class="flex items-center justify-center gap-4">
            <a href='<%= ResolveUrl("~/Default.aspx") %>' class="fcl-btn">Back to home</a>
            <a href='<%= ResolveUrl("~/Contact.aspx") %>' class="text-brandRed font-bold text-sm hover:underline">Contact support &rarr;</a>
        </div>
    </div>
</asp:Content>
