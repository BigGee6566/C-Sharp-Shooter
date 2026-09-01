<%@ Page Title="Warehouse Sorting" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="Warehouse.aspx.cs" Inherits="ForteLogisticsApp.Ops.Warehouse" %>
<%@ Import Namespace="ForteLogisticsApp.Services" %>

<asp:Content ID="c1" ContentPlaceHolderID="MainContent" runat="server">

    <!-- Header -->
    <header class="flex flex-col md:flex-row justify-between items-start md:items-center mb-stack-lg gap-gutter">
        <div>
            <h1 class="font-headline-lg text-headline-lg-mobile md:text-headline-lg text-on-background mb-base">Warehouse Sorting</h1>
            <p class="font-body-md text-body-md text-on-surface-variant">
                Scan a waybill to receive, sort and dispatch parcels through the hub.</p>
        </div>
        <a href="<%= ResolveUrl("~/Reports/DailyParcelMovement.aspx") %>" class="fcl-btn fcl-btn-dark">
            <span class="material-symbols-outlined">analytics</span> Movement Report
        </a>
    </header>

    <div class="grid grid-cols-1 lg:grid-cols-3 gap-gutter mb-stack-lg">

        <!-- Scan & sort -->
        <section class="lg:col-span-1 fcl-card">
            <h2 class="font-headline-md text-headline-md text-on-background mb-stack-sm">Scan Parcel</h2>

            <label class="block font-label-sm text-label-sm text-on-surface-variant mb-1" for="<%= txtScan.ClientID %>">Waybill / Tracking Number</label>
            <div class="flex gap-base">
                <asp:TextBox ID="txtScan" runat="server" CssClass="fcl-input uppercase" placeholder="e.g. FCL00000123" />
                <asp:Button ID="btnScan" runat="server" Text="Scan" OnClick="btnScan_Click" CssClass="fcl-btn shrink-0" />
            </div>
            <asp:Label ID="lblScanMsg" runat="server" />

            <asp:Panel ID="pnlEdit" runat="server" Visible="false" CssClass="mt-stack-md pt-stack-md border-t border-outline-variant space-y-4">
                <asp:HiddenField ID="hfShipmentId" runat="server" />

                <div>
                    <p class="font-headline-md text-headline-md text-primary">
                        <asp:Literal ID="litParcel" runat="server" /></p>
                    <p class="font-body-md text-label-md text-on-surface-variant">
                        <asp:Literal ID="litRoute" runat="server" /></p>
                </div>

                <div class="fcl-alert fcl-alert-info">
                    <asp:Literal ID="litSortReason" runat="server" />
                </div>

                <div>
                    <label class="block font-label-sm text-label-sm text-on-surface-variant mb-1" for="<%= ddlSort.ClientID %>">Sort Lane</label>
                    <asp:DropDownList ID="ddlSort" runat="server" CssClass="fcl-input" />
                </div>

                <div>
                    <label class="block font-label-sm text-label-sm text-on-surface-variant mb-1" for="<%= ddlWarehouse.ClientID %>">Warehouse Status</label>
                    <asp:DropDownList ID="ddlWarehouse" runat="server" CssClass="fcl-input">
                        <asp:ListItem Value="Received">Received</asp:ListItem>
                        <asp:ListItem Value="Sorted">Sorted</asp:ListItem>
                        <asp:ListItem Value="Dispatched">Dispatched</asp:ListItem>
                    </asp:DropDownList>
                </div>

                <div>
                    <label class="block font-label-sm text-label-sm text-on-surface-variant mb-1" for="<%= ddlException.ClientID %>">Exception</label>
                    <asp:DropDownList ID="ddlException" runat="server" CssClass="fcl-input">
                        <asp:ListItem Value="None">None</asp:ListItem>
                        <asp:ListItem Value="Lost">Lost</asp:ListItem>
                        <asp:ListItem Value="Damaged">Damaged</asp:ListItem>
                    </asp:DropDownList>
                    <p class="font-label-sm text-label-sm text-on-surface-variant mt-1">
                        Flagging an exception escalates it to Dispatch and Management automatically.</p>
                </div>

                <asp:Button ID="btnSave" runat="server" Text="Save Parcel" OnClick="btnSave_Click" CssClass="fcl-btn w-full" />
                <asp:Label ID="lblSaveMsg" runat="server" />
            </asp:Panel>
        </section>

        <!-- Sorting queue -->
        <section class="lg:col-span-2 bg-surface-container-lowest border border-outline-variant rounded-lg overflow-hidden shadow-[0_20px_40px_-15px_rgba(0,51,153,0.04)]">
            <div class="p-stack-md border-b border-outline-variant">
                <div class="flex justify-between items-center mb-stack-sm">
                    <h2 class="font-headline-md text-headline-md text-on-background">Sorting Queue</h2>
                    <span class="font-label-sm text-label-sm text-on-surface-variant">
                        <asp:Literal ID="litQueueCount" runat="server" /> parcel(s)</span>
                </div>

                <div class="grid grid-cols-1 md:grid-cols-2 gap-base">
                    <div class="relative">
                        <span class="material-symbols-outlined absolute left-3 top-1/2 -translate-y-1/2 text-on-surface-variant">search</span>
                        <asp:TextBox ID="txtSearch" runat="server" CssClass="fcl-input pl-10"
                            placeholder="Search waybill or destination..." AutoPostBack="true" OnTextChanged="txtSearch_TextChanged" />
                    </div>
                    <asp:DropDownList ID="ddlLane" runat="server" CssClass="fcl-input" AutoPostBack="true"
                        OnSelectedIndexChanged="ddlLane_SelectedIndexChanged" />
                </div>
            </div>

            <div class="overflow-x-auto">
                <asp:GridView ID="gvSortingQueue" runat="server" AutoGenerateColumns="False"
                    CssClass="report-table" GridLines="None" UseAccessibleHeader="true"
                    DataKeyNames="ShipmentID" OnRowCommand="gvSortingQueue_RowCommand">
                    <Columns>
                        <asp:TemplateField HeaderText="Waybill">
                            <ItemTemplate>
                                <span class="font-semibold text-primary"><%# UiHelper.Enc(Eval("TrackingNumber")) %></span>
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Route">
                            <ItemTemplate>
                                <%# UiHelper.Enc(Eval("OriginCity")) %> &rarr; <%# UiHelper.Enc(Eval("DestCity")) %>
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Sort Lane">
                            <ItemTemplate>
                                <%# UiHelper.Enc(Eval("SortCategory")) %>
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Warehouse">
                            <ItemTemplate>
                                <%# UiHelper.StatusBadge(Eval("WarehouseStatus") as string) %>
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Exception">
                            <ItemTemplate>
                                <%# UiHelper.Enc(Eval("ExceptionType")) %>
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="">
                            <ItemTemplate>
                                <asp:LinkButton ID="btnLoad" runat="server" CommandName="LoadParcel"
                                    CommandArgument='<%# Eval("TrackingNumber") %>'
                                    CssClass="fcl-link font-semibold" Text="Open" />
                            </ItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                </asp:GridView>

                <asp:Panel ID="pnlEmpty" runat="server" Visible="false" CssClass="p-stack-md text-center">
                    <span class="material-symbols-outlined ms-32 text-outline">inventory_2</span>
                    <p class="font-body-md text-body-md text-on-surface-variant mt-base">
                        Nothing is waiting in the sorting queue.</p>
                </asp:Panel>
            </div>
        </section>
    </div>
</asp:Content>
