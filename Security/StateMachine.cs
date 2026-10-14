using System;
using System.Collections.Generic;

namespace ForteLogisticsApp.Security
{
    /// <summary>
    /// The single source of truth for which status transitions are legal. Business logic
    /// asks <see cref="CanTransition"/> before changing any status column; anything not in
    /// the whitelist is rejected. Whitelist, never blacklist - a status we have never heard
    /// of cannot move anywhere.
    ///
    /// Machines (case-insensitive names):
    ///   DeliveryStatus, WarehouseStatus, RouteStatus, DriverAvailability.
    /// </summary>
    public static class StateMachine
    {
        private static readonly Dictionary<string, Dictionary<string, HashSet<string>>> Machines =
            new Dictionary<string, Dictionary<string, HashSet<string>>>(StringComparer.OrdinalIgnoreCase)
            {
                // Shipment.DeliveryStatus
                ["DeliveryStatus"] = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Registered"]     = Set("Sorted", "Failed"),
                    ["Sorted"]         = Set("Assigned"),
                    ["Assigned"]       = Set("OutForDelivery", "Sorted"),   // Sorted = returned to the pool
                    ["OutForDelivery"] = Set("Delivered", "Sorted", "Failed"),
                    ["Delivered"]      = Set(),                             // terminal
                    ["Failed"]         = Set("Sorted"),                     // only an Admin reopen
                },
                // Shipment.WarehouseStatus
                ["WarehouseStatus"] = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Pending"]    = Set("Received", "Sorted"),             // scan-and-sort in one step is allowed
                    ["Received"]   = Set("Sorted"),
                    ["Sorted"]     = Set("Dispatched"),
                    ["Dispatched"] = Set(),                                 // terminal
                },
                // Route.Status
                ["RouteStatus"] = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Planned"]   = Set("Active", "Cancelled"),
                    ["Active"]    = Set("Completed", "Cancelled"),
                    ["Completed"] = Set(),
                    ["Cancelled"] = Set(),
                },
                // Driver.AvailabilityStatus
                ["DriverAvailability"] = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Available"] = Set("On Route", "Off Duty"),
                    ["On Route"]  = Set("Available"),
                    ["Off Duty"]  = Set("Available"),
                },
            };

        private static HashSet<string> Set(params string[] states)
        {
            return new HashSet<string>(states, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>True only if <paramref name="to"/> is a declared legal successor of
        /// <paramref name="from"/> on <paramref name="machine"/>. Unknown machine, unknown
        /// from-state, or an undeclared target all return false. Same-state is never a legal
        /// transition.</summary>
        public static bool CanTransition(string machine, string from, string to)
        {
            if (string.IsNullOrEmpty(machine) || string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
                return false;

            Dictionary<string, HashSet<string>> m;
            if (!Machines.TryGetValue(machine, out m)) return false;

            HashSet<string> allowed;
            if (!m.TryGetValue(from, out allowed)) return false;

            return allowed.Contains(to);
        }

        /// <summary>True if <paramref name="state"/> is a state this machine knows at all
        /// (as a source or a declared target).</summary>
        public static bool IsKnownState(string machine, string state)
        {
            if (string.IsNullOrEmpty(machine) || string.IsNullOrEmpty(state)) return false;

            Dictionary<string, HashSet<string>> m;
            if (!Machines.TryGetValue(machine, out m)) return false;

            if (m.ContainsKey(state)) return true;
            foreach (var targets in m.Values)
                if (targets.Contains(state)) return true;
            return false;
        }
    }
}
