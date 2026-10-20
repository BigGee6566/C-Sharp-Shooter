using System;
using System.Runtime.Caching;

namespace ForteLogisticsApp.Security
{
    /// <summary>
    /// A small fixed-window rate limiter for public, unauthenticated actions (self-registration),
    /// so a script cannot create accounts in bulk, probe which emails are registered at speed, or
    /// burn CPU on password hashing. In-memory per server: enough for this single-server app; a
    /// web farm would need a shared store.
    /// </summary>
    public static class RequestThrottle
    {
        private static readonly MemoryCache Counters = new MemoryCache("lc-throttle");
        private static readonly object Gate = new object();

        /// <summary>
        /// Counts one attempt for <paramref name="key"/> and returns true while the key has made
        /// no more than <paramref name="limit"/> attempts in the current <paramref name="window"/>.
        /// </summary>
        public static bool Allow(string key, int limit, TimeSpan window)
        {
            if (string.IsNullOrEmpty(key)) key = "(unknown)";
            lock (Gate)
            {
                var count = Counters.Get(key) as int[];
                if (count == null)
                {
                    count = new int[1];
                    // Absolute expiry: the window starts at the first attempt and is not extended.
                    Counters.Add(key, count, DateTimeOffset.UtcNow.Add(window));
                }
                count[0]++;
                return count[0] <= limit;
            }
        }

        /// <summary>Attempts recorded for <paramref name="key"/> in its current window (0 when none).</summary>
        public static int Count(string key)
        {
            lock (Gate)
            {
                var count = Counters.Get(key ?? "(unknown)") as int[];
                return count == null ? 0 : count[0];
            }
        }

        /// <summary>Forgets <paramref name="key"/>, e.g. after a successful sign-in.</summary>
        public static void Reset(string key)
        {
            lock (Gate)
            {
                Counters.Remove(key ?? "(unknown)");
            }
        }
    }
}
