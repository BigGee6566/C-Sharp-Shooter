using System;
using System.Data;
using System.Diagnostics;
using ForteLogisticsApp.Data;

namespace ForteLogisticsApp.Services
{
    /// <summary>
    /// Simulates customer notifications (Focus E). Instead of a real SMS/email
    /// gateway it records a notification row that the recipient sees in-app.
    /// </summary>
    public static class NotificationService
    {
        public static void Send(int userId, int? shipmentId, string message, string type)
        {
            if (userId <= 0) return;
            Db.NonQuery(
                @"INSERT INTO dbo.[Notification] (UserID, ShipmentID, Message, Type)
                  VALUES (@u, @s, @m, @t)",
                "@u", userId, "@s", (object)shipmentId, "@m", message, "@t", type);
        }

        /// <summary>
        /// As <see cref="Send"/>, for use AFTER the real work has committed: a failed
        /// notification is logged and swallowed, so it can never turn a booking that was saved
        /// into an error page (and tempt the user into booking it again).
        /// </summary>
        public static void TrySend(int userId, int? shipmentId, string message, string type)
        {
            try
            {
                Send(userId, shipmentId, message, type);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Notification to user " + userId + " failed: " + ex);
            }
        }

        /// <summary>
        /// Fans a message out to every user holding a role ("Staff", "Admin" or "Customer").
        /// Note this does NOT deduplicate: two calls naming the same role deliver twice.
        /// One INSERT..SELECT - no loop, no round trip per recipient.
        /// </summary>
        public static int SendToRole(string role, int? shipmentId, string message, string type)
        {
            if (string.IsNullOrEmpty(role)) return 0;
            return Db.NonQuery(
                @"INSERT INTO dbo.[Notification] (UserID, ShipmentID, Message, Type)
                  SELECT UserID, @s, @m, @t FROM dbo.[User] WHERE Role = @r",
                "@r", role, "@s", (object)shipmentId, "@m", message, "@t", type);
        }

        /// <summary>
        /// Sends only if this user has never received a notification carrying the same
        /// DedupeKey. Lets automation re-run safely without spamming the recipient.
        /// Returns true if a notification was actually created.
        /// </summary>
        public static bool SendOnce(int userId, int? shipmentId, string message, string type, string dedupeKey)
        {
            if (userId <= 0 || string.IsNullOrEmpty(dedupeKey)) return false;
            return Db.NonQuery(
                @"INSERT INTO dbo.[Notification] (UserID, ShipmentID, Message, Type, DedupeKey)
                  SELECT @u, @s, @m, @t, @k
                  WHERE NOT EXISTS (SELECT 1 FROM dbo.[Notification]
                                    WHERE UserID = @u AND DedupeKey = @k)",
                "@u", userId, "@s", (object)shipmentId, "@m", message, "@t", type, "@k", dedupeKey) > 0;
        }

        public static int UnreadCount(int userId)
        {
            if (userId <= 0) return 0;
            return Db.ToInt(Db.Scalar(
                "SELECT COUNT(*) FROM dbo.[Notification] WHERE UserID = @u AND IsRead = 0", "@u", userId));
        }

        public static void MarkAllRead(int userId)
        {
            if (userId <= 0) return;
            Db.NonQuery(
                "UPDATE dbo.[Notification] SET IsRead = 1 WHERE UserID = @u AND IsRead = 0", "@u", userId);
        }

        public static DataTable Recent(int userId, int top = 10)
        {
            return Db.Query(
                @"SELECT TOP (@n) NotificationID, ShipmentID, Message, Type, IsRead, CreatedDate
                  FROM dbo.[Notification] WHERE UserID = @u ORDER BY CreatedDate DESC",
                "@n", top, "@u", userId);
        }
    }
}
