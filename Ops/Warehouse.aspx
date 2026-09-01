<%@ Page Title="Warehouse & Sorting" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="Warehouse.aspx.cs" Inherits="ForteLogisticsApp.Ops.Warehouse" %>

<asp:Content ID="c1" ContentPlaceHolderID="MainContent" runat="server">
        <div class="max-w-7xl mx-auto space-y-6 p-4 sm:p-6 lg:p-8">

        <!-- Employee Account Governance Section (Admin Only) -->
        <asp:Panel ID="pnlAdminGovernance" runat="server" Visible="false" CssClass="bg-white rounded-xl border border-indigo-100 shadow-sm overflow-hidden mb-6">

            <!-- Governance Panel Header -->
            <div class="flex flex-col sm:flex-row justify-between items-start sm:items-center p-5 bg-gradient-to-r from-slate-50 to-indigo-50/30 border-b border-gray-200 gap-3">
                <div class="flex items-start gap-3">
                    <div class="p-2 bg-indigo-100 text-indigo-700 rounded-lg shrink-0 mt-0.5">
                        <span class="material-symbols-outlined text-xl leading-none">admin_panel_settings</span>
                    </div>
                    <div>
                        <h3 class="text-base font-semibold text-gray-900">Employee Account Governance</h3>
                        <p class="text-xs text-gray-500 mt-0.5">Review staff registrations, approve portal logins, and manage security permissions.</p>
                    </div>
                </div>
                <div>
                    <span class="inline-flex items-center px-3 py-1 bg-indigo-50 text-indigo-700 border border-indigo-200/60 rounded-full text-xs font-medium">
                        <span class="w-1.5 h-1.5 rounded-full bg-indigo-600 mr-2 animate-pulse"></span>
                        Admin Governance Active
                    </span>
                </div>
            </div>

            <div class="p-5 space-y-4">
                <!-- Interactive Action Alert -->
                <asp:Panel ID="pnlGovernanceAlert" runat="server" Visible="false" CssClass="p-3.5 rounded-lg text-xs font-medium border border-blue-200 bg-blue-50 text-blue-800 flex items-center justify-between">
                    <asp:Literal ID="litGovernanceAlert" runat="server"></asp:Literal>
                </asp:Panel>

                <!-- User Account Management Table -->
                <div class="overflow-x-auto rounded-lg border border-gray-200">
                    <asp:GridView ID="gvUserGovernance" runat="server" AutoGenerateColumns="False" OnRowCommand="gvUserGovernance_RowCommand" 
                        CssClass="min-w-full divide-y divide-gray-200 text-left text-sm" GridLines="None">
                        <HeaderStyle CssClass="bg-gray-50 font-semibold text-xs text-gray-500 uppercase tracking-wider" />
                        <RowStyle CssClass="hover:bg-slate-50 transition-colors divide-x-0" />
                        <Columns>
                            <asp:BoundField DataField="UserId" HeaderText="ID" ItemStyle-CssClass="px-4 py-3.5 font-semibold text-xs text-gray-400" HeaderStyle-CssClass="px-4 py-3" />

                            <asp:TemplateField HeaderText="Employee Details" HeaderStyle-CssClass="px-4 py-3">
                                <ItemTemplate>
                                    <div class="px-4 py-1">
                                        <div class="font-medium text-gray-900"><%# Eval("FirstName") %> <%# Eval("LastName") %></div>
                                        <div class="text-xs text-gray-500"><%# Eval("Email") %></div>
                                    </div>
                                </ItemTemplate>
                            </asp:TemplateField>

                            <asp:BoundField DataField="RoleName" HeaderText="Role" ItemStyle-CssClass="px-4 py-3 text-xs font-medium text-gray-700" HeaderStyle-CssClass="px-4 py-3" />

                            <asp:TemplateField HeaderText="Status" HeaderStyle-CssClass="px-4 py-3">
                                <ItemTemplate>
                                    <div class="px-4 py-1">
                                        <span class='<%# GetStatusBadgeCss(Eval("AccountStatus").ToString()) %>'>
                                            <%# Eval("AccountStatus") %>
                                        </span>
                                    </div>
                                </ItemTemplate>
                            </asp:TemplateField>

                            <asp:TemplateField HeaderText="Actions" HeaderStyle-CssClass="px-4 py-3 text-right">
                                <ItemStyle CssClass="px-4 py-3 text-right space-x-1.5" />
                                <ItemTemplate>
                                    <!-- Approve Employee -->
                                    <asp:Button ID="btnApprove" runat="server" CommandName="ApproveUser" CommandArgument='<%# Eval("UserId") %>' 
                                        Text="Approve" Visible='<%# Eval("AccountStatus").ToString() == "Pending" %>' 
                                        CssClass="px-3 py-1.5 bg-emerald-600 hover:bg-emerald-700 text-white rounded-md text-xs font-medium transition-colors cursor-pointer" />

                                    <!-- Disapprove -->
                                    <asp:Button ID="btnDisapprove" runat="server" CommandName="DisapproveUser" CommandArgument='<%# Eval("UserId") %>' 
                                        Text="Disapprove" Visible='<%# Eval("AccountStatus").ToString() == "Pending" %>' 
                                        CssClass="px-3 py-1.5 bg-rose-600 hover:bg-rose-700 text-white rounded-md text-xs font-medium transition-colors cursor-pointer" />

                                    <!-- Suspend -->
                                    <asp:Button ID="btnSuspend" runat="server" CommandName="SuspendUser" CommandArgument='<%# Eval("UserId") %>' 
                                        Text="Suspend" Visible='<%# Eval("AccountStatus").ToString() == "Approved" %>' 
                                        CssClass="px-3 py-1.5 bg-amber-600 hover:bg-amber-700 text-white rounded-md text-xs font-medium transition-colors cursor-pointer" />

                                    <!-- Reactivate -->
                                    <asp:Button ID="btnReactivate" runat="server" CommandName="ReactivateUser" CommandArgument='<%# Eval("UserId") %>' 
                                        Text="Reactivate" Visible='<%# Eval("AccountStatus").ToString() == "Suspended" || Eval("AccountStatus").ToString() == "Disapproved" %>' 
                                        CssClass="px-3 py-1.5 bg-indigo-600 hover:bg-indigo-700 text-white rounded-md text-xs font-medium transition-colors cursor-pointer" />

                                    <!-- Permanently Delete -->
                                    <asp:Button ID="btnRemove" runat="server" CommandName="RemoveUser" CommandArgument='<%# Eval("UserId") %>' 
                                        Text="Remove" OnClientClick="return confirm('Are you sure you want to permanently remove this employee account?');" 
                                        CssClass="px-3 py-1.5 border border-red-300 text-red-600 hover:bg-red-50 rounded-md text-xs font-medium transition-colors cursor-pointer" />
                                </ItemTemplate>
                            </asp:TemplateField>
                        </Columns>
                    </asp:GridView>
                </div>
            </div>
        </asp:Panel>

        <!-- Main Dashboard View Panel -->
        <asp:Panel ID="pnlMainDashboard" runat="server" CssClass="space-y-6">

            <!-- Header Section -->
            <section class="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4 border-b border-gray-200 pb-5">
                <div>
                    <h2 class="text-2xl sm:text-3xl font-bold tracking-tight text-gray-900">Daily Parcel Sorting Queue</h2>
                    <p class="text-sm text-gray-500 mt-1">Manage incoming shipments, sorting workflow, and driver assignments.</p>
                </div>
                <div>
                    <asp:Button ID="btnGenerateReport" runat="server" Text="Generate Movement Report" OnClick="btnGenerateReport_Click" 
                        CssClass="inline-flex items-center justify-center bg-indigo-600 hover:bg-indigo-700 text-white font-medium text-sm py-2.5 px-5 rounded-lg shadow-sm transition-all duration-150 cursor-pointer focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2" />
                </div>
            </section>

            <!-- Main Dashboard Bento Layout -->
            <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">

                <!-- Sorting Queue Table Card (Spans 2 columns) -->
                <div class="lg:col-span-2 bg-white rounded-xl border border-gray-200 p-5 shadow-sm flex flex-col justify-between space-y-4">
                    <div>
                        <!-- Search & Priority Filters -->
                        <div class="flex flex-col sm:flex-row gap-3 mb-5">
                            <div class="relative flex-1">
                                <span class="material-symbols-outlined absolute left-3 top-1/2 -translate-y-1/2 text-gray-400 text-xl pointer-events-none">search</span>
                                <asp:TextBox ID="txtSearch" runat="server" CssClass="w-full pl-10 pr-4 py-2 bg-gray-50 border border-gray-300 rounded-lg text-sm text-gray-900 focus:bg-white focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 outline-none transition-all" placeholder="Search tracking ID or destination..." AutoPostBack="true" OnTextChanged="txtSearch_TextChanged"></asp:TextBox>
                            </div>
                            <div class="relative min-w-[160px]">
                                <asp:DropDownList ID="ddlPriority" runat="server" AutoPostBack="true" OnSelectedIndexChanged="ddlPriority_SelectedIndexChanged" CssClass="w-full pl-3 pr-8 py-2 bg-gray-50 border border-gray-300 rounded-lg text-sm text-gray-900 focus:bg-white focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 outline-none appearance-none cursor-pointer">
                                    <asp:ListItem Value="">All Priorities</asp:ListItem>
                                    <asp:ListItem Value="high">High Priority</asp:ListItem>
                                    <asp:ListItem Value="standard">Standard</asp:ListItem>
                                    <asp:ListItem Value="economy">Economy</asp:ListItem>
                                </asp:DropDownList>
                                <span class="material-symbols-outlined absolute right-2.5 top-1/2 -translate-y-1/2 text-gray-400 text-xl pointer-events-none">expand_more</span>
                            </div>
                        </div>

                        <!-- Parcels Grid -->
                        <div class="overflow-x-auto rounded-lg border border-gray-200">
                            <asp:GridView ID="gvSortingQueue" runat="server" AutoGenerateColumns="False" OnRowCommand="gvSortingQueue_RowCommand" CssClass="min-w-full divide-y divide-gray-200 text-left text-sm" GridLines="None">
                                <HeaderStyle CssClass="bg-gray-50 font-semibold text-xs text-gray-500 uppercase tracking-wider" />
                                <RowStyle CssClass="hover:bg-slate-50 transition-colors" />
                                <Columns>
                                    <asp:TemplateField HeaderText="Tracking ID" HeaderStyle-CssClass="px-4 py-3">
                                        <ItemTemplate>
                                            <div class="px-4 py-3 font-semibold text-gray-900"><%# Eval("TrackingID") %></div>
                                        </ItemTemplate>
                                    </asp:TemplateField>

                                    <asp:TemplateField HeaderText="Destination" HeaderStyle-CssClass="px-4 py-3">
                                        <ItemTemplate>
                                            <div class="px-4 py-3 text-gray-700"><%# Eval("Destination") %></div>
                                        </ItemTemplate>
                                    </asp:TemplateField>

                                    <asp:TemplateField HeaderText="Priority" HeaderStyle-CssClass="px-4 py-3">
                                        <ItemTemplate>
                                            <div class="px-4 py-3">
                                                <asp:Panel ID="pnlPriority" runat="server">
                                                    <%# Eval("Priority").ToString() == "High" ? 
                                                        "<span class='inline-flex items-center px-2.5 py-0.5 rounded-full bg-red-100 text-red-800 text-xs font-medium'><span class='w-1.5 h-1.5 rounded-full bg-red-600 mr-1.5'></span>High</span>" : 
                                                        "<span class='inline-flex items-center px-2.5 py-0.5 rounded-full bg-gray-100 text-gray-800 text-xs font-medium'>" + Eval("Priority") + "</span>" %>
                                                </asp:Panel>
                                            </div>
                                        </ItemTemplate>
                                    </asp:TemplateField>

                                    <asp:TemplateField HeaderText="Status" HeaderStyle-CssClass="px-4 py-3">
                                        <ItemTemplate>
                                            <div class="px-4 py-3 text-gray-600 text-xs font-medium"><%# Eval("Status") %></div>
                                        </ItemTemplate>
                                    </asp:TemplateField>

                                    <asp:TemplateField HeaderText="Action" HeaderStyle-CssClass="px-4 py-3 text-right">
                                        <ItemStyle CssClass="px-4 py-3 text-right" />
                                        <ItemTemplate>
                                            <asp:Button ID="btnAction" runat="server" 
                                                CommandName='<%# Eval("Status").ToString() == "Pending Scan" ? "ScanIn" : "AssignDriver" %>' 
                                                CommandArgument='<%# Eval("TrackingID") %>' 
                                                Text='<%# Eval("Status").ToString() == "Pending Scan" ? "Scan In" : "Assign Driver" %>' 
                                                CssClass='<%# Eval("Status").ToString() == "Pending Scan" ? "px-3.5 py-1.5 bg-indigo-600 hover:bg-indigo-700 text-white rounded-md text-xs font-medium transition-colors cursor-pointer" : "px-3.5 py-1.5 border border-indigo-600 text-indigo-600 hover:bg-indigo-50 rounded-md text-xs font-medium transition-colors cursor-pointer" %>' />
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                </Columns>
                            </asp:GridView>
                        </div>
                    </div>

                    <!-- Queue Footer Pagination -->
                    <div class="pt-3 border-t border-gray-100 flex justify-between items-center text-xs text-gray-500">
                        <span>Showing <span class="font-medium text-gray-700"><asp:Literal ID="litCurrentCount" runat="server" Text="3"></asp:Literal></span> of <span class="font-medium text-gray-700"><asp:Literal ID="litTotalCount" runat="server" Text="42"></asp:Literal></span> parcels</span>
                        <div class="flex items-center gap-1">
                            <asp:LinkButton ID="btnPrevPage" runat="server" OnClick="btnPrevPage_Click" CssClass="p-1 text-gray-500 hover:text-gray-700 hover:bg-gray-100 rounded transition-colors">
                                <span class="material-symbols-outlined text-lg">chevron_left</span>
                            </asp:LinkButton>
                            <asp:LinkButton ID="btnNextPage" runat="server" OnClick="btnNextPage_Click" CssClass="p-1 text-gray-500 hover:text-gray-700 hover:bg-gray-100 rounded transition-colors">
                                <span class="material-symbols-outlined text-lg">chevron_right</span>
                            </asp:LinkButton>
                        </div>
                    </div>
                </div>

                <!-- Right Column Widgets (Map & Queue Metrics) -->
                <div class="flex flex-col gap-6">

                    <!-- Map Widget -->
                    <div class="bg-white rounded-xl border border-gray-200 overflow-hidden shadow-sm flex-1 flex flex-col min-h-[280px]">
                        <div class="p-4 border-b border-gray-200 flex justify-between items-center bg-gray-50">
                            <h3 class="font-semibold text-sm text-gray-900">Active Fleet Routes</h3>
                            <span class="material-symbols-outlined text-gray-400 text-xl">map</span>
                        </div>
                        <div class="relative flex-1 bg-slate-100 min-h-[200px]">
                            <img class="absolute inset-0 w-full h-full object-cover" alt="City Map Logistics Network" src="https://lh3.googleusercontent.com/aida-public/AB6AXuCvaozEjqDRXq4Hp8KKD6jwXNRKgcluOkVH8QcYsrTBpw7WlfcOWFZNhT3ll-Ci__BTC2e9WVC0RHz-xevtOyFWcpsOMJyMsEFux2HtP8GuG9ZNponHXHWeeGyMpppnaCn_8M-c9q5e8-qVDSESk4eSR_8CZJwZB0acbBuiPD1Cs1T_6IkjvAuT6C1lEF2IHI-29ju5bhc0U6u7rWSjfB7k_VRnAuwL88KDMya-CNiN3HvdrBWDppx7"/>

                            <!-- Floating Metrics Badge -->
                            <div class="absolute bottom-3 left-3 right-3 bg-white/95 backdrop-blur-md p-3 rounded-lg border border-gray-200 shadow-sm flex justify-around items-center">
                                <div class="text-center">
                                    <div class="text-xl font-bold text-indigo-600">
                                        <asp:Literal ID="litActiveDrivers" runat="server" Text="12"></asp:Literal>
                                    </div>
                                    <div class="text-[11px] font-medium text-gray-500 uppercase tracking-wider">Active Drivers</div>
                                </div>
                                <div class="w-px h-8 bg-gray-200"></div>
                                <div class="text-center">
                                    <div class="text-xl font-bold text-emerald-600">
                                        <asp:Literal ID="litRouteEfficiency" runat="server" Text="84%"></asp:Literal>
                                    </div>
                                    <div class="text-[11px] font-medium text-gray-500 uppercase tracking-wider">Efficiency</div>
                                </div>
                            </div>
                        </div>
                    </div>

                    <!-- Daily Progress Widget -->
                    <div class="bg-indigo-900 text-white rounded-xl p-5 shadow-sm relative overflow-hidden">
                        <div class="absolute -right-10 -top-10 w-36 h-36 bg-indigo-500/20 rounded-full blur-2xl pointer-events-none"></div>

                        <h3 class="text-xs font-semibold text-indigo-200 uppercase tracking-wider mb-1 relative z-10">Queue Status</h3>
                        <div class="text-3xl font-extrabold tracking-tight mb-4 relative z-10">
                            <asp:Literal ID="litPendingCount" runat="server" Text="42"></asp:Literal> 
                            <span class="text-base font-normal text-indigo-200">pending</span>
                        </div>

                        <div class="w-full bg-indigo-950/60 h-2.5 rounded-full overflow-hidden p-0.5 border border-indigo-700/50 relative z-10">
                            <div id="divProgressBar" runat="server" class="bg-emerald-400 h-full rounded-full transition-all duration-300" style="width: 65%;"></div>
                        </div>

                        <div class="flex justify-between mt-2 text-xs text-indigo-200 relative z-10">
                            <span><asp:Literal ID="litProcessedPct" runat="server" Text="65%"></asp:Literal> Processed</span>
                            <span><asp:Literal ID="litRemainingPct" runat="server" Text="35%"></asp:Literal> Remaining</span>
                        </div>
                    </div>

                </div>
            </div>

        </asp:Panel>

    </div>
</asp:Content>
