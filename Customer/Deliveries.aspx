<%@ Page Title="My Deliveries" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="Deliveries.aspx.cs" Inherits="ForteLogisticsApp.CustomerPortal.Deliveries" %>
<%@ Register Src="~/Controls/DeliveryCards.ascx" TagPrefix="lc" TagName="DeliveryCards" %>

<asp:Content ID="c1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="max-w-3xl mx-auto">
        <header class="mb-stack-md">
            <h1 class="font-headline-lg text-headline-lg-mobile md:text-headline-lg text-on-background mb-base">My Deliveries</h1>
            <p class="font-body-md text-body-md text-on-surface-variant">Parcels that are still on their way. Select one to see its journey.</p>
        </header>
        <section class="bg-surface-container-lowest border border-outline-variant rounded-lg overflow-hidden">
            <lc:DeliveryCards ID="cards" runat="server" />
        </section>
    </div>
</asp:Content>
