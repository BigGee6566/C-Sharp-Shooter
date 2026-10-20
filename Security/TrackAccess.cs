using System;

namespace ForteLogisticsApp.Security
{
    /// <summary>
    /// Who may see and share live positions on the tracking map (TrackApi.aspx). Pure and
    /// unit-tested.
    ///
    /// A waybill number is not a secret - it is printed on the parcel and shared with the
    /// recipient - so knowing one only earns the public view: route, status and a simulated
    /// position. Real GPS positions are personal data: the sender's shared location and the
    /// driver's live position are shown only to the parties to that shipment (the sender, the
    /// assigned driver) and to office staff and admins.
    /// </summary>
    public static class TrackAccess
    {
        /// <summary>True when the caller may see the live (GPS) positions on this shipment.</summary>
        public static bool CanSeeLivePositions(int userId, string role, bool isDriverUser, int senderId, int driverId)
        {
            if (userId <= 0) return false;
            if (senderId > 0 && senderId == userId) return true;
            if (driverId > 0 && driverId == userId) return true;
            if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase)) return true;
            // Office staff run operations; a driver only sees their own parcels.
            return string.Equals(role, "Staff", StringComparison.OrdinalIgnoreCase) && !isDriverUser;
        }

        /// <summary>
        /// True when a position ping is acceptable: the caller shares as the role they actually
        /// hold on this shipment, and the shipment is still on its way (a finished delivery has
        /// nothing left to track).
        /// </summary>
        public static bool CanPing(string pingRole, int userId, int senderId, int driverId, string deliveryStatus)
        {
            if (userId <= 0 || IsFinished(deliveryStatus)) return false;
            if (pingRole == "Customer") return senderId > 0 && senderId == userId;
            if (pingRole == "Driver")
            {
                // A driver shares a position only while the parcel is actually with them.
                return driverId > 0 && driverId == userId &&
                       (deliveryStatus == "Assigned" || deliveryStatus == "OutForDelivery");
            }
            return false;
        }

        private static bool IsFinished(string deliveryStatus)
        {
            return deliveryStatus == "Delivered" || deliveryStatus == "Failed";
        }

        /// <summary>Live positions older than this are not served: the sharer has stopped, or the parcel moved on.</summary>
        public const int FreshPingMinutes = 15;

        /// <summary>
        /// Cross-site request forgery check for state-changing requests that are not ASP.NET
        /// postbacks (which carry ViewState/event validation): the request must come from a
        /// page on this same site.
        ///   1. Sec-Fetch-Site, set by the browser itself and impossible for a page to forge,
        ///      decides when present.
        ///   2. Otherwise Origin (always sent on a cross-site POST), else Referer, must name this
        ///      host. Only the HOST is compared: behind a TLS-terminating proxy the app sees
        ///      http:// on an internal port while the browser's origin is https://, and a
        ///      cross-site attacker cannot make a victim's browser claim our host name.
        /// A request carrying no evidence at all is refused.
        /// </summary>
        public static bool IsSameOrigin(string origin, string referer, string secFetchSite, Uri requestUrl)
        {
            if (requestUrl == null) return false;

            if (!string.IsNullOrEmpty(secFetchSite))
            {
                return string.Equals(secFetchSite, "same-origin", StringComparison.OrdinalIgnoreCase);
            }

            if (!string.IsNullOrEmpty(origin))
            {
                // "null" is sent by sandboxed frames and some redirects - it never parses as a host.
                return SameHost(origin, requestUrl);
            }
            return !string.IsNullOrEmpty(referer) && SameHost(referer, requestUrl);
        }

        private static bool SameHost(string url, Uri requestUrl)
        {
            Uri parsed;
            return Uri.TryCreate(url.Trim(), UriKind.Absolute, out parsed) &&
                   (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps) &&
                   string.Equals(parsed.Host, requestUrl.Host, StringComparison.OrdinalIgnoreCase);
        }
    }
}
