using Microsoft.VisualStudio.TestTools.UnitTesting;
using ForteLogisticsApp.Security;

namespace ForteLogisticsApp.Tests.Unit
{
    /// <summary>
    /// Pure, no-database checks that the status whitelist is exactly what we intend. Adding a
    /// new state or edge should force a change here, which is the point of a table-driven test.
    /// </summary>
    [TestClass]
    public class StateMachineTests
    {
        [DataTestMethod]
        // DeliveryStatus
        [DataRow("DeliveryStatus", "Registered", "Sorted", true)]
        [DataRow("DeliveryStatus", "Registered", "Delivered", false)]
        [DataRow("DeliveryStatus", "Sorted", "Assigned", true)]
        [DataRow("DeliveryStatus", "Assigned", "OutForDelivery", true)]
        [DataRow("DeliveryStatus", "Assigned", "Delivered", false)]       // deliver without pickup -> illegal
        [DataRow("DeliveryStatus", "OutForDelivery", "Delivered", true)]
        [DataRow("DeliveryStatus", "OutForDelivery", "Failed", true)]
        [DataRow("DeliveryStatus", "Delivered", "Sorted", false)]         // terminal
        // WarehouseStatus
        [DataRow("WarehouseStatus", "Pending", "Received", true)]
        [DataRow("WarehouseStatus", "Pending", "Dispatched", false)]      // can't skip sorting
        [DataRow("WarehouseStatus", "Sorted", "Dispatched", true)]
        // RouteStatus
        [DataRow("RouteStatus", "Planned", "Active", true)]
        [DataRow("RouteStatus", "Completed", "Active", false)]
        // DriverAvailability
        [DataRow("DriverAvailability", "Available", "On Route", true)]
        [DataRow("DriverAvailability", "Off Duty", "On Route", false)]
        public void CanTransition_matches_whitelist(string machine, string from, string to, bool expected)
        {
            Assert.AreEqual(expected, StateMachine.CanTransition(machine, from, to));
        }

        [TestMethod]
        public void CanTransition_rejects_same_state_unknowns_and_nulls()
        {
            Assert.IsFalse(StateMachine.CanTransition("DeliveryStatus", "Delivered", "Delivered"), "same-state is not a transition");
            Assert.IsFalse(StateMachine.CanTransition("NoSuchMachine", "A", "B"), "unknown machine");
            Assert.IsFalse(StateMachine.CanTransition("DeliveryStatus", "Bogus", "Sorted"), "unknown from-state");
            Assert.IsFalse(StateMachine.CanTransition("DeliveryStatus", null, "Sorted"), "null from");
            Assert.IsFalse(StateMachine.CanTransition("DeliveryStatus", "Assigned", null), "null to");
        }

        [TestMethod]
        public void CanTransition_is_case_insensitive()
        {
            Assert.IsTrue(StateMachine.CanTransition("deliverystatus", "assigned", "outfordelivery"));
        }

        [TestMethod]
        public void IsKnownState_recognises_sources_and_targets()
        {
            Assert.IsTrue(StateMachine.IsKnownState("DeliveryStatus", "Delivered"));   // a target
            Assert.IsTrue(StateMachine.IsKnownState("DeliveryStatus", "Registered"));  // a source
            Assert.IsFalse(StateMachine.IsKnownState("DeliveryStatus", "Teleported"));
        }
    }
}
