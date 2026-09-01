<%@ Page Title="Customer Dashboard" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="Dashboard.aspx.cs" Inherits="ForteLogisticsApp.CustomerPortal.Dashboard" %>

<asp:Content ID="c1" ContentPlaceHolderID="MainContent" runat="server">
      <!-- Quick Actions Header -->
    <header class="flex flex-col md:flex-row justify-between items-start md:items-center mb-stack-lg gap-gutter">
        <div>
            <h1 class="font-headline-lg text-headline-lg-mobile md:text-headline-lg text-on-background mb-base" id="page-title">Business Dashboard</h1>
            <p class="font-body-md text-body-md text-on-surface-variant" id="page-subtitle">Overview of your enterprise logistics operations.</p>
        </div>
        <div class="flex flex-wrap gap-base items-center">
            <div class="flex items-center gap-1 bg-surface-container-high p-1 rounded-lg border border-outline-variant mr-2">
                <button type="button" class="px-3 py-1.5 rounded text-label-sm font-label-md bg-primary text-on-primary shadow-sm transition-colors cursor-pointer" id="btn-role-business" onclick="setRole('business')">Business</button>
                <button type="button" class="px-3 py-1.5 rounded text-label-sm font-label-md text-on-surface-variant hover:bg-surface-variant transition-colors cursor-pointer" id="btn-role-individual" onclick="setRole('individual')">Individual</button>
            </div>
            <asp:Button ID="btnHeaderCreateOrder" runat="server" Text="+ Create New Order" CssClass="bg-primary text-on-primary font-label-md text-label-md px-4 py-2 rounded-DEFAULT hover:bg-on-primary-fixed-variant transition-colors flex items-center gap-2 cursor-pointer" OnClick="btnHeaderCreateOrder_Click" />
            <button type="button" class="border border-primary text-primary font-label-md text-label-md px-4 py-2 rounded-DEFAULT hover:bg-surface-variant transition-colors flex items-center gap-2 disabled:opacity-50 disabled:cursor-not-allowed disabled:hover:bg-transparent" id="btn-print">
                <span class="material-symbols-outlined text-[18px]">print</span>
                Print Labels
            </button>
            <button type="button" class="border border-primary text-primary font-label-md text-label-md px-4 py-2 rounded-DEFAULT hover:bg-surface-variant transition-colors flex items-center gap-2 disabled:opacity-50 disabled:cursor-not-allowed disabled:hover:bg-transparent" id="btn-collect">
                <span class="material-symbols-outlined text-[18px]">local_shipping</span>
                Request Collection
            </button>
        </div>
    </header>

    <!-- Metrics Overview Bento Grid -->
    <section class="grid grid-cols-1 md:grid-cols-3 gap-gutter mb-stack-lg">
        <!-- Active Shipments Card -->
        <div class="bg-surface-container-lowest border border-outline-variant p-stack-md rounded-lg relative overflow-hidden shadow-[0_20px_40px_-15px_rgba(0,51,153,0.04)]">
            <div class="flex justify-between items-start mb-stack-sm">
                <h3 class="font-label-md text-label-md text-on-surface-variant">Active Shipments</h3>
                <span class="material-symbols-outlined text-primary">flight_takeoff</span>
            </div>
            <p class="font-headline-xl text-headline-xl text-on-background mb-base">
                <asp:Literal ID="litActiveShipments" runat="server" ClientIDMode="Static" Text="67"></asp:Literal>
            </p>
            <p class="font-label-sm text-label-sm text-secondary flex items-center gap-1">
                <span class="material-symbols-outlined text-[14px]">arrow_upward</span>
                12% from last week
            </p>
            <div class="absolute -bottom-4 -right-4 opacity-5 pointer-events-none">
                <span class="material-symbols-outlined text-[120px]">local_shipping</span>
            </div>
        </div>

        <!-- Credit / Wallet Balance Card -->
        <div class="bg-surface-container-lowest border border-outline-variant p-stack-md rounded-lg relative overflow-hidden shadow-[0_20px_40px_-15px_rgba(0,51,153,0.04)]">
            <div class="flex justify-between items-start mb-stack-sm">
                <h3 class="font-label-md text-label-md text-on-surface-variant" id="card2-title">Available Credit</h3>
                <span class="material-symbols-outlined text-primary">account_balance_wallet</span>
            </div>
            <p class="font-headline-xl text-headline-xl text-on-background mb-base" id="card2-value">R2,000</p>
            <p class="font-label-sm text-label-sm text-on-surface-variant flex items-center gap-1" id="card2-sub">
                Total Line: R5,000
            </p>
        </div>

        <!-- Monthly / Recent Spend Card -->
        <div class="bg-surface-container-lowest border border-outline-variant p-stack-md rounded-lg relative overflow-hidden shadow-[0_20px_40px_-15px_rgba(0,51,153,0.04)]">
            <div class="flex justify-between items-start mb-stack-sm">
                <h3 class="font-label-md text-label-md text-on-surface-variant" id="card3-title">Monthly Spend</h3>
                <span class="material-symbols-outlined text-primary">trending_up</span>
            </div>
            <p class="font-headline-xl text-headline-xl text-on-background mb-base" id="card3-value">R12,450</p>
            <div class="w-full bg-surface-variant h-2 rounded-full mt-stack-sm overflow-hidden">
                <div class="bg-primary h-full rounded-full" id="card3-bar" style="width: 65%;"></div>
            </div>
        </div>
    </section>

    <!-- Main Content Area: Activity & Sidebar Card -->
    <div class="grid grid-cols-1 lg:grid-cols-3 gap-gutter mb-stack-lg">
        <!-- Recent Activity Table -->
        <section class="lg:col-span-2 bg-surface-container-lowest border border-outline-variant rounded-lg overflow-hidden shadow-[0_20px_40px_-15px_rgba(0,51,153,0.04)]">
            <div class="p-stack-md border-b border-outline-variant flex justify-between items-center">
                <h2 class="font-headline-md text-headline-md text-on-background">Recent Activity</h2>
                <a href="CustomerReport.aspx" class="font-label-md text-label-md text-primary hover:text-primary-fixed-dim transition-colors">View All</a>
            </div>
            <div class="overflow-x-auto">
                <asp:Repeater ID="rptRecentActivity" runat="server">
                    <HeaderTemplate>
                        <table class="w-full text-left border-collapse">
                            <thead>
                                <tr class="border-b border-outline-variant bg-surface">
                                    <th class="p-stack-sm font-label-md text-label-md text-on-surface-variant font-semibold">Tracking ID</th>
                                    <th class="p-stack-sm font-label-md text-label-md text-on-surface-variant font-semibold">Destination</th>
                                    <th class="p-stack-sm font-label-md text-label-md text-on-surface-variant font-semibold">Status</th>
                                    <th class="p-stack-sm font-label-md text-label-md text-on-surface-variant font-semibold">Date</th>
                                </tr>
                            </thead>
                            <tbody class="font-body-md text-body-md text-on-background divide-y divide-outline-variant">
                    </HeaderTemplate>
                    <ItemTemplate>
                        <tr class="hover:bg-surface-container-low transition-colors">
                            <td class="p-stack-sm font-medium text-primary"><%# Eval("TrackingID") %></td>
                            <td class="p-stack-sm"><%# Eval("Destination") %></td>
                            <td class="p-stack-sm"><%# Eval("StatusBadgeHtml") %></td>
                            <td class="p-stack-sm text-on-surface-variant"><%# Eval("Date") %></td>
                        </tr>
                    </ItemTemplate>
                    <FooterTemplate>
                            </tbody>
                        </table>
                    </FooterTemplate>
                </asp:Repeater>
            </div>
        </section>

        <!-- Credit Line Status Card (Business Mode) -->
        <aside class="bg-primary text-on-primary rounded-lg p-stack-md flex flex-col justify-between relative overflow-hidden shadow-[0_20px_40px_-15px_rgba(0,32,104,0.4)]" id="sidebar-business">
            <div class="relative z-10">
                <h3 class="font-headline-md text-headline-md mb-base">Credit Line Due</h3>
                <p class="font-body-md text-body-md text-primary-fixed-dim mb-stack-md">Your next statement is ready. Please ensure payment to maintain uninterrupted logistics services.</p>
                <div class="bg-on-primary-fixed-variant rounded p-stack-sm mb-stack-md border border-primary-container">
                    <div class="flex justify-between items-center mb-1">
                        <span class="font-label-sm text-label-sm text-primary-fixed-dim">Amount Due</span>
                        <span class="font-label-md text-label-md font-bold">R3,000.00</span>
                    </div>
                    <div class="flex justify-between items-center">
                        <span class="font-label-sm text-label-sm text-primary-fixed-dim">Due Date</span>
                        <span class="font-label-sm text-label-sm">August 27, 2026</span>
                    </div>
                </div>
            </div>
            <button type="button" class="relative z-10 w-full bg-secondary text-on-secondary font-label-md text-label-md py-3 rounded-DEFAULT hover:bg-secondary-container transition-colors shadow-sm cursor-pointer">
                Pay Statement
            </button>
            <div class="absolute top-0 right-0 -mr-8 -mt-8 w-32 h-32 bg-primary-container rounded-full opacity-50 blur-2xl"></div>
            <div class="absolute bottom-0 left-0 -ml-8 -mb-8 w-24 h-24 bg-secondary rounded-full opacity-20 blur-xl"></div>
        </aside>

        <!-- Promo Card (Individual Mode) -->
        <aside class="hidden bg-surface-container-low border border-outline-variant text-on-surface rounded-lg p-stack-md flex flex-col justify-between relative overflow-hidden shadow-[0_20px_40px_-15px_rgba(0,32,104,0.04)]" id="sidebar-individual">
            <div class="relative z-10">
                <div class="flex items-center gap-2 mb-base">
                    <span class="material-symbols-outlined text-primary">storefront</span>
                    <h3 class="font-headline-md text-headline-md text-on-background">Upgrade to Business</h3>
                </div>
                <p class="font-body-md text-body-md text-on-surface-variant mb-stack-md">Unlock enterprise features like bulk label printing, scheduled collections, and flexible credit lines designed to scale your operations.</p>
                <ul class="space-y-2 mb-stack-md">
                    <li class="flex items-center gap-2 text-label-sm font-label-sm text-on-surface">
                        <span class="material-symbols-outlined text-[16px] text-primary">check_circle</span> Volume Discounts
                    </li>
                    <li class="flex items-center gap-2 text-label-sm font-label-sm text-on-surface">
                        <span class="material-symbols-outlined text-[16px] text-primary">check_circle</span> Dedicated Account Manager
                    </li>
                    <li class="flex items-center gap-2 text-label-sm font-label-sm text-on-surface">
                        <span class="material-symbols-outlined text-[16px] text-primary">check_circle</span> API Access
                    </li>
                </ul>
            </div>
            <button type="button" class="relative z-10 w-full bg-primary text-on-primary font-label-md text-label-md py-3 rounded-DEFAULT hover:bg-on-primary-fixed-variant transition-colors shadow-sm cursor-pointer">
                View Pricing
            </button>
        </aside>
    </div>

    <script>
        function setRole(role) {
            const isBusiness = role === 'business';
            
            // Toggle header button styling
            document.getElementById('btn-role-business').className = isBusiness ? "px-3 py-1.5 rounded text-label-sm font-label-md bg-primary text-on-primary shadow-sm transition-colors cursor-pointer" : "px-3 py-1.5 rounded text-label-sm font-label-md text-on-surface-variant hover:bg-surface-variant transition-colors cursor-pointer";
            document.getElementById('btn-role-individual').className = !isBusiness ? "px-3 py-1.5 rounded text-label-sm font-label-md bg-primary text-on-primary shadow-sm transition-colors cursor-pointer" : "px-3 py-1.5 rounded text-label-sm font-label-md text-on-surface-variant hover:bg-surface-variant transition-colors cursor-pointer";
            
            // Header Titles
            document.getElementById('page-title').innerText = isBusiness ? 'Business Dashboard' : 'Individual Dashboard';
            document.getElementById('page-subtitle').innerText = isBusiness ? 'Overview of your enterprise logistics operations.' : 'Overview of your personal shipments.';
            
            // Action Buttons State
            const btnPrint = document.getElementById('btn-print');
            const btnCollect = document.getElementById('btn-collect');
            btnPrint.disabled = !isBusiness;
            btnCollect.disabled = !isBusiness;
            
            btnPrint.title = !isBusiness ? "Business Feature Only" : "";
            btnCollect.title = !isBusiness ? "Business Feature Only" : "";
            
            // Metric Card 1: Active Shipments
            const activeElem = document.getElementById('litActiveShipments');
            if (activeElem) {
                activeElem.innerText = isBusiness ? '67' : '2';
            }

            // Metric Card 2: Available Credit / Wallet Balance
            document.getElementById('card2-title').innerText = isBusiness ? 'Available Credit' : 'Wallet Balance';
            document.getElementById('card2-value').innerText = isBusiness ? 'R2,000' : 'R400';
            document.getElementById('card2-sub').innerText = isBusiness ? 'Total Line: R5,000' : 'Available for next shipment';
            
            // Metric Card 3: Monthly Spend / Recent Spending
            document.getElementById('card3-title').innerText = isBusiness ? 'Monthly Spend' : 'Recent Spending';
            document.getElementById('card3-value').innerText = isBusiness ? 'R12,450' : 'R1,250';
            document.getElementById('card3-bar').style.width = isBusiness ? '65%' : '25%';
            
            // Sidebar Panel Visibility
            if (isBusiness) {
                document.getElementById('sidebar-business').classList.remove('hidden');
                document.getElementById('sidebar-individual').classList.add('hidden');
            } else {
                document.getElementById('sidebar-business').classList.add('hidden');
                document.getElementById('sidebar-individual').classList.remove('hidden');
            }
        }

        // Set default view based on session role on load
        window.addEventListener('DOMContentLoaded', () => {
            const sessionUserType = "<%= Session["UserType"] %>";
            if (sessionUserType === "Individual") {
                setRole('individual');
            } else {
                setRole('business');
            }
        });
    </script>
</asp:Content>