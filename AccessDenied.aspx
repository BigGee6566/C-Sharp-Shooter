<%@ Page Title="Access Denied" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="AccessDenied.aspx.cs" Inherits="ForteLogisticsApp.AccessDeniedPage" %>

<asp:Content ID="c1" ContentPlaceHolderID="MainContent" runat="server">
    <section class="py-24 px-4 text-center max-w-xl mx-auto">
        <div class="text-brandRed text-6xl mb-4"><span class="material-symbols-outlined">block</span></div>
        <h1 class="text-3xl font-black text-brandDark mb-2">Access Denied</h1>
        <p class="text-on-surface-variant text-sm mb-6">You don't have permission to view this page with your current role.
            If you believe this is a mistake, please contact your administrator.</p>
        <a href='<%= ResolveUrl("~/Default.aspx") %>' class="fcl-btn inline-block">Back to Home</a>
    </section>
</asp:Content>
