<%@ Page Title="Track a Parcel" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="Track.aspx.cs" Inherits="ForteLogisticsApp.CustomerPortal.Track" %>

<asp:Content ID="c1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="max-w-6xl mx-auto px-4 py-8">
        <p class="text-xs text-on-surface-variant mb-1"><span class="material-symbols-outlined mr-1">home</span> Home / <a href='<%= ResolveUrl("~/Customer/Dashboard.aspx") %>' class="hover:text-brandRed">Dashboard</a> / <span class="text-brandRed font-semibold">Track Parcel</span></p>
        <h1 class="text-2xl font-black text-brandDark mb-1">Track a Parcel</h1>
        <p class="text-on-surface-variant text-sm mb-6">Enter a waybill number to see its live status and full journey.</p>

        <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
            <!-- Search + result -->
            <div class="lg:col-span-2 space-y-6">
                <div class="fcl-card rounded-xl p-6">
                    <label class="block text-xs font-bold text-on-surface uppercase mb-1">Waybill Number</label>
                    <div class="flex gap-2">
                        <asp:TextBox ID="txtWaybill" runat="server" CssClass="fcl-input" placeholder="e.g. FCL-10000001"></asp:TextBox>
                        <asp:Button ID="btnTrack" runat="server" Text="Track" OnClick="btnTrack_Click" CssClass="fcl-btn" />
                    </div>
                    <asp:Label ID="lblMsg" runat="server" CssClass="block text-xs mt-2"></asp:Label>
                </div>

                <asp:Panel ID="pnlResult" runat="server" Visible="false" CssClass="fcl-card rounded-xl p-6">
                    <div class="flex items-center justify-between mb-1">
                        <h2 class="font-black text-lg text-brandDark"><asp:Literal ID="litWaybill" runat="server"></asp:Literal></h2>
                        <asp:Literal ID="litStatus" runat="server"></asp:Literal>
                    </div>
                    <p class="text-sm text-on-surface-variant mb-5"><asp:Literal ID="litRoute" runat="server"></asp:Literal></p>

                    <!-- Live map -->
                    <asp:HiddenField ID="hfTrackWb" runat="server" />
                    <div id="trackMapWrap" class="mb-6" style="display:none">
                        <div class="flex items-center justify-between mb-2">
                            <h3 class="text-xs font-bold uppercase text-on-surface-variant">Live Map</h3>
                            <button type="button" id="btnShareLoc" class="text-xs font-semibold text-brandRed hover:underline inline-flex items-center gap-1">
                                <span class="material-symbols-outlined text-sm">my_location</span> Share my location
                            </button>
                        </div>
                        <div id="trackMap" style="height:400px;border-radius:12px;overflow:hidden;z-index:0;"></div>
                        <div class="flex flex-wrap gap-4 mt-2 text-xs text-on-surface-variant">
                            <span><span style="color:#003399;font-weight:700;">D</span> Destination</span>
                            <span>&#128666; Driver</span>
                            <span>&#128205; You</span>
                        </div>
                    </div>

                    <h3 class="text-xs font-bold uppercase text-on-surface-variant mb-3">Tracking History</h3>
                    <div class="relative border-l-2 border-outline-variant ml-2 pl-6 space-y-5">
                        <asp:Repeater ID="rptTimeline" runat="server">
                            <ItemTemplate>
                                <div class="relative">
                                    <span class="absolute -left-[31px] top-1 w-3.5 h-3.5 rounded-full bg-brandRed border-2 border-white shadow"></span>
                                    <div class="flex items-center gap-2 flex-wrap">
                                        <%# ForteLogisticsApp.Services.UiHelper.StatusBadge(Eval("Status").ToString()) %>
                                        <span class="text-xs text-outline"><%# Eval("Timestamp", "{0:dd MMM yyyy HH:mm}") %></span>
                                    </div>
                                    <p class="text-sm text-brandDark font-semibold mt-1"><%# UiHelper.Enc(Eval("Location")) %></p>
                                    <p class="text-xs text-on-surface-variant"><%# UiHelper.Enc(Eval("Remarks")) %></p>
                                </div>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>

                    <!-- Rate the driver (delivered shipments only) -->
                    <asp:Panel ID="pnlRate" runat="server" Visible="false" CssClass="mt-6 border-t border-outline-variant pt-4">
                        <h3 class="text-xs font-bold uppercase text-on-surface-variant mb-3">Rate your delivery</h3>
                        <div class="flex flex-col sm:flex-row gap-3 sm:items-end">
                            <div>
                                <label class="block text-xs font-bold text-on-surface uppercase mb-1">Score</label>
                                <asp:DropDownList ID="ddlScore" runat="server" CssClass="fcl-input">
                                    <asp:ListItem Text="5 - Excellent" Value="5"></asp:ListItem>
                                    <asp:ListItem Text="4 - Good" Value="4"></asp:ListItem>
                                    <asp:ListItem Text="3 - Average" Value="3"></asp:ListItem>
                                    <asp:ListItem Text="2 - Poor" Value="2"></asp:ListItem>
                                    <asp:ListItem Text="1 - Very poor" Value="1"></asp:ListItem>
                                </asp:DropDownList>
                            </div>
                            <div class="flex-1">
                                <label class="block text-xs font-bold text-on-surface uppercase mb-1">Comment (optional)</label>
                                <asp:TextBox ID="txtComment" runat="server" CssClass="fcl-input"></asp:TextBox>
                            </div>
                            <asp:HiddenField ID="hfDriverId" runat="server" />
                            <asp:Button ID="btnRate" runat="server" Text="Submit Rating" OnClick="btnRate_Click" CssClass="fcl-btn" />
                        </div>
                        <asp:Label ID="lblRateMsg" runat="server" CssClass="block text-xs mt-2"></asp:Label>
                    </asp:Panel>
                </asp:Panel>
            </div>

            <!-- My parcels quick list -->
            <div class="fcl-card rounded-xl p-6">
                <h2 class="font-bold text-lg text-brandDark mb-4">My Parcels</h2>
                <asp:Repeater ID="rptMine" runat="server">
                    <ItemTemplate>
                        <a href='<%# ResolveUrl("~/Customer/Track.aspx?wb=" + Eval("TrackingNumber")) %>' class="block border-b border-outline-variant py-2 hover:bg-black/5 rounded px-1 transition">
                            <div class="flex items-center justify-between">
                                <span class="font-semibold text-brandDark text-sm"><%# UiHelper.Enc(Eval("TrackingNumber")) %></span>
                                <%# ForteLogisticsApp.Services.UiHelper.StatusBadge(Eval("DeliveryStatus").ToString()) %>
                            </div>
                            <span class="text-xs text-on-surface-variant"><%# UiHelper.Enc(Eval("DestCity")) %></span>
                        </a>
                    </ItemTemplate>
                </asp:Repeater>
                <asp:Panel ID="pnlNoneMine" runat="server" Visible="false" CssClass="text-center text-on-surface-variant text-sm py-6">
                    You have no parcels yet.
                </asp:Panel>
            </div>
        </div>
    </div>

    <script src='<%= ResolveUrl("~/Scripts/livetrack.js") %>'></script>
    <script>
        (function () {
            var hf = document.getElementById('<%= hfTrackWb.ClientID %>');
            var wb = hf ? hf.value : '';
            if (!wb || !window.L) return;

            var API = '<%= ResolveUrl("~/TrackApi.aspx") %>';
            document.getElementById('trackMapWrap').style.display = '';
            FclTrack.init('trackMap');
            FclTrack.load({ apiUrl: API, wb: wb });

            // Only the sender may share their own location.
            fetch(API + '?wb=' + encodeURIComponent(wb), { credentials: 'same-origin' })
                .then(function (r) { return r.json(); })
                .then(function (d) {
                    var b = document.getElementById('btnShareLoc');
                    if (!d.ok || !d.mine) { b.style.display = 'none'; return; }
                    var sharing = false;
                    b.addEventListener('click', function () {
                        if (!sharing) {
                            if (FclTrack.startShare('Customer')) { sharing = true; b.innerHTML = '<span class="material-symbols-outlined text-sm">location_on</span> Stop sharing'; }
                        } else {
                            FclTrack.stopShare(); sharing = false;
                            b.innerHTML = '<span class="material-symbols-outlined text-sm">my_location</span> Share my location';
                        }
                    });
                }).catch(function () { });
        })();
    </script>
</asp:Content>
