<%@ Page Title="Live Map" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="Map.aspx.cs" Inherits="ForteLogisticsApp.DriverPortal.Map" %>

<asp:Content ID="c1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="max-w-6xl mx-auto px-4 py-8">
        <div class="mb-6">
            <p class="text-xs text-on-surface-variant"><span class="material-symbols-outlined mr-1">home</span> Home / <span class="text-brandRed font-semibold">Live Map</span></p>
            <h1 class="text-2xl font-black text-brandDark">Live Delivery Map</h1>
            <p class="text-on-surface-variant text-sm">Pick a parcel to see the delivery point and the customer's shared location. Share your live location so the customer can watch you approach.</p>
        </div>

        <asp:Panel ID="pnlEmpty" runat="server" Visible="false" CssClass="fcl-card rounded-xl p-8 text-center text-on-surface-variant">
            <span class="material-symbols-outlined text-4xl mb-2">location_off</span>
            <p>You have no parcels out for delivery or awaiting collection right now.</p>
            <a href='<%= ResolveUrl("~/Driver/Dashboard.aspx") %>' class="fcl-btn inline-block mt-4">Back to My Manifest</a>
        </asp:Panel>

        <asp:Panel ID="pnlMap" runat="server" CssClass="fcl-card rounded-xl p-5">
            <div class="flex flex-col md:flex-row md:items-end gap-3 mb-4">
                <div class="flex-1">
                    <label class="block text-xs font-semibold text-on-surface-variant mb-1" for="<%= ddlParcel.ClientID %>">Parcel on your manifest</label>
                    <asp:DropDownList ID="ddlParcel" runat="server" CssClass="fcl-input w-full" />
                </div>
                <button type="button" id="btnShare" class="fcl-btn fcl-btn-primary inline-flex items-center gap-2">
                    <span class="material-symbols-outlined text-base">my_location</span> Share my live location
                </button>
                <button type="button" id="btnSim" class="fcl-btn fcl-btn-dark inline-flex items-center gap-2">
                    <span class="material-symbols-outlined text-base">play_arrow</span> Simulate drive
                </button>
            </div>

            <p id="mapStatus" class="text-sm text-on-surface-variant mb-3"></p>
            <div id="map-container">
                <div id="driverMap" style="height:440px;border-radius:12px;overflow:hidden;z-index:0;"></div>
            </div>

            <!-- BusNova-style map fallback block -->
            <div id="map-fallback" class="bg-surface-container-lowest border border-outline-variant rounded-xl text-center p-6 my-3 shadow-sm" hidden>
                <div class="text-4xl mb-2">🗺️</div>
                <p class="font-bold text-base text-on-background mb-1">Map tiles are unavailable.</p>
                <p class="text-xs text-on-surface-variant mb-4">The live tracking data is still updating, but the map background couldn't load.</p>
                <button type="button" class="fcl-btn fcl-btn-primary" data-role="map-retry">
                    Retry Map
                </button>
            </div>

            <div class="flex flex-wrap gap-4 mt-3 text-xs text-on-surface-variant">
                <span><span style="color:#1d2b45;font-weight:700;">O</span> Depot</span>
                <span><span style="color:#003399;font-weight:700;">D</span> Delivery point</span>
                <span>&#128666; You (driver)</span>
                <span>&#128205; Customer</span>
            </div>
        </asp:Panel>
    </div>

    <script src='<%= ResolveUrl("~/Scripts/livetrack.js?v=2") %>'></script>
    <script>
        (function () {
            var API = '<%= ResolveUrl("~/TrackApi.aspx") %>';
            var ddl = document.getElementById('<%= ddlParcel.ClientID %>');
            if (!ddl || !window.L) return;
            FclTrack.init('driverMap');

            function wb() { return ddl ? ddl.value : ''; }
            function loadSel() {
                if (!wb()) return;
                FclTrack.load({
                    apiUrl: API, wb: wb(),
                    onStatus: function (s) { document.getElementById('mapStatus').textContent = 'Parcel status: ' + s; }
                });
            }
            ddl.addEventListener('change', loadSel);
            loadSel();

            var sharing = false, btn = document.getElementById('btnShare');
            btn.addEventListener('click', function () {
                if (!sharing) { if (FclTrack.startShare('Driver')) { sharing = true; btn.innerHTML = '<span class="material-symbols-outlined text-base">location_on</span> Stop sharing'; } }
                else { FclTrack.stopShare(); sharing = false; btn.innerHTML = '<span class="material-symbols-outlined text-base">my_location</span> Share my live location'; }
            });

            document.getElementById('btnSim').addEventListener('click', function () {
                if (!wb()) return;
                fetch(API + '?wb=' + encodeURIComponent(wb()), { credentials: 'same-origin' })
                    .then(function (r) { return r.json(); })
                    .then(function (d) {
                        if (d.ok) { FclTrack.simulate('Driver', [d.origin.lat, d.origin.lng], [d.dest.lat, d.dest.lng], 40); }
                    });
            });
        })();
    </script>
</asp:Content>
