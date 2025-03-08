using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Xamarin.UITest;
using SecurityPatrol.MAUI.UITests.Setup;
using SecurityPatrol.TestCommon.Constants;

namespace SecurityPatrol.MAUI.UITests.Pages
{
    /// <summary>
    /// Contains UI tests for the time tracking page components of the Security Patrol application
    /// </summary>
    public class TimeTrackingPageTests : UITestBase
    {
        // UI element identifiers
        private const string TimeTrackingPageTitle = "TimeTrackingPageTitle";
        private const string ClockStatusLabel = "ClockStatusLabel";
        private const string LastClockInLabel = "LastClockInLabel";
        private const string LastClockOutLabel = "LastClockOutLabel";
        private const string ClockInButton = "ClockInButton";
        private const string ClockOutButton = "ClockOutButton";
        private const string ViewHistoryButton = "ViewHistoryButton";
        private const string ActivityIndicator = "TimeTrackingActivityIndicator";

        /// <summary>
        /// Sets up the test environment before each test
        /// </summary>
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            Login();
            NavigateToPage("TimeTracking");
            WaitForPageToLoad("TimeTracking");
        }

        /// <summary>
        /// Tests that the time tracking page loads correctly with all required UI elements
        /// </summary>
        [Test]
        [Order(1)]
        public void TestTimeTrackingPageLoads()
        {
            AssertElementExists(TimeTrackingPageTitle);
            AssertElementExists(ClockStatusLabel);
            AssertElementExists(LastClockInLabel);
            AssertElementExists(LastClockOutLabel);
            AssertElementExists(ClockInButton);
            AssertElementExists(ClockOutButton);
            AssertElementExists(ViewHistoryButton);
        }

        /// <summary>
        /// Tests the initial state of the clock UI elements
        /// </summary>
        [Test]
        [Order(2)]
        public void TestInitialClockState()
        {
            var statusText = App.Query(ClockStatusLabel)[0].Text;
            
            if (statusText.Contains("Clocked In"))
            {
                // If clocked in, clock in button should be disabled and clock out button enabled
                Assert.IsFalse(App.Query(ClockInButton)[0].Enabled, "Clock in button should be disabled when clocked in");
                Assert.IsTrue(App.Query(ClockOutButton)[0].Enabled, "Clock out button should be enabled when clocked in");
            }
            else
            {
                // If clocked out, clock in button should be enabled and clock out button disabled
                Assert.IsTrue(App.Query(ClockInButton)[0].Enabled, "Clock in button should be enabled when clocked out");
                Assert.IsFalse(App.Query(ClockOutButton)[0].Enabled, "Clock out button should be disabled when clocked out");
            }
            
            // Last clock in and out labels should show timestamps or "Not available"
            AssertElementExists(LastClockInLabel);
            AssertElementExists(LastClockOutLabel);
        }

        /// <summary>
        /// Tests that the clock in button is enabled when user is clocked out
        /// </summary>
        [Test]
        [Order(3)]
        public void TestClockInButtonEnabled()
        {
            EnsureUserClockedOut();
            
            AssertElementText(ClockStatusLabel, "Currently Clocked Out");
            Assert.IsTrue(App.Query(ClockInButton)[0].Enabled, "Clock in button should be enabled when clocked out");
            Assert.IsFalse(App.Query(ClockOutButton)[0].Enabled, "Clock out button should be disabled when clocked out");
        }

        /// <summary>
        /// Tests that the clock out button is enabled when user is clocked in
        /// </summary>
        [Test]
        [Order(4)]
        public void TestClockOutButtonEnabled()
        {
            EnsureUserClockedIn();
            
            AssertElementText(ClockStatusLabel, "Currently Clocked In");
            Assert.IsFalse(App.Query(ClockInButton)[0].Enabled, "Clock in button should be disabled when clocked in");
            Assert.IsTrue(App.Query(ClockOutButton)[0].Enabled, "Clock out button should be enabled when clocked in");
        }

        /// <summary>
        /// Tests the clock in operation and UI updates
        /// </summary>
        [Test]
        [Order(5)]
        public void TestClockInOperation()
        {
            EnsureUserClockedOut();
            
            // Record the current last clock in text
            var initialLastClockIn = App.Query(LastClockInLabel)[0].Text;
            
            // Perform clock in
            TapElement(ClockInButton);
            
            // Wait for activity indicator to disappear
            WaitForElement(ActivityIndicator);
            WaitForNoElement(ActivityIndicator);
            
            // Verify status changes
            AssertElementText(ClockStatusLabel, "Currently Clocked In");
            Assert.IsFalse(App.Query(ClockInButton)[0].Enabled, "Clock in button should be disabled after clocking in");
            Assert.IsTrue(App.Query(ClockOutButton)[0].Enabled, "Clock out button should be enabled after clocking in");
            
            // Verify last clock in changed
            var updatedLastClockIn = App.Query(LastClockInLabel)[0].Text;
            Assert.AreNotEqual(initialLastClockIn, updatedLastClockIn, "Last clock in time should update after clocking in");
        }

        /// <summary>
        /// Tests the clock out operation and UI updates
        /// </summary>
        [Test]
        [Order(6)]
        public void TestClockOutOperation()
        {
            EnsureUserClockedIn();
            
            // Record the current last clock out text
            var initialLastClockOut = App.Query(LastClockOutLabel)[0].Text;
            
            // Perform clock out
            TapElement(ClockOutButton);
            
            // Wait for activity indicator to disappear
            WaitForElement(ActivityIndicator);
            WaitForNoElement(ActivityIndicator);
            
            // Verify status changes
            AssertElementText(ClockStatusLabel, "Currently Clocked Out");
            Assert.IsTrue(App.Query(ClockInButton)[0].Enabled, "Clock in button should be enabled after clocking out");
            Assert.IsFalse(App.Query(ClockOutButton)[0].Enabled, "Clock out button should be disabled after clocking out");
            
            // Verify last clock out changed
            var updatedLastClockOut = App.Query(LastClockOutLabel)[0].Text;
            Assert.AreNotEqual(initialLastClockOut, updatedLastClockOut, "Last clock out time should update after clocking out");
        }

        /// <summary>
        /// Tests navigation to the time history page
        /// </summary>
        [Test]
        [Order(7)]
        public void TestViewHistoryNavigation()
        {
            // Tap view history button
            TapElement(ViewHistoryButton);
            
            // Assert that the time history page loads
            WaitForElement("TimeHistoryPageTitle");
            AssertElementExists("TimeHistoryPageTitle");
            
            // Assert history list or empty state is displayed
            bool historyListExists = App.Query("TimeHistoryList").Length > 0;
            bool emptyStateExists = App.Query("NoHistoryMessage").Length > 0;
            
            Assert.IsTrue(historyListExists || emptyStateExists, "Either history list or empty state message should be displayed");
            
            // Navigate back
            PressBack();
            
            // Verify we're back on time tracking page
            WaitForElement(TimeTrackingPageTitle);
            AssertElementExists(TimeTrackingPageTitle);
        }

        /// <summary>
        /// Tests that clock status persists after navigating away and back
        /// </summary>
        [Test]
        [Order(8)]
        public void TestClockStatusPersistence()
        {
            // Record current clock status
            var initialStatus = App.Query(ClockStatusLabel)[0].Text;
            bool initiallyClocked = initialStatus.Contains("Clocked In");
            
            // Navigate to another page (main/home)
            NavigateToPage("Home");
            
            // Navigate back to time tracking
            NavigateToPage("TimeTracking");
            
            // Assert status remains the same
            var currentStatus = App.Query(ClockStatusLabel)[0].Text;
            bool currentlyClocked = currentStatus.Contains("Clocked In");
            
            Assert.AreEqual(initiallyClocked, currentlyClocked, "Clock status should remain the same after navigation");
            
            // Assert appropriate button states
            if (currentlyClocked)
            {
                Assert.IsFalse(App.Query(ClockInButton)[0].Enabled, "Clock in button should remain disabled after navigation when clocked in");
                Assert.IsTrue(App.Query(ClockOutButton)[0].Enabled, "Clock out button should remain enabled after navigation when clocked in");
            }
            else
            {
                Assert.IsTrue(App.Query(ClockInButton)[0].Enabled, "Clock in button should remain enabled after navigation when clocked out");
                Assert.IsFalse(App.Query(ClockOutButton)[0].Enabled, "Clock out button should remain disabled after navigation when clocked out");
            }
        }

        /// <summary>
        /// Tests that the activity indicator is displayed during clock operations
        /// </summary>
        [Test]
        [Order(9)]
        public void TestActivityIndicatorDuringOperation()
        {
            EnsureUserClockedOut();
            
            // Check indicator during clock in
            TapElement(ClockInButton);
            AssertElementExists(ActivityIndicator);
            WaitForNoElement(ActivityIndicator);
            
            // Check indicator during clock out
            TapElement(ClockOutButton);
            AssertElementExists(ActivityIndicator);
            WaitForNoElement(ActivityIndicator);
        }

        /// <summary>
        /// Tests that buttons are disabled during clock operations
        /// </summary>
        [Test]
        [Order(10)]
        public void TestButtonDisabledDuringOperation()
        {
            EnsureUserClockedOut();
            
            // During clock in
            TapElement(ClockInButton);
            
            // Check both buttons are disabled during operation
            Assert.IsFalse(App.Query(ClockInButton)[0].Enabled, "Clock in button should be disabled during clock in operation");
            Assert.IsFalse(App.Query(ClockOutButton)[0].Enabled, "Clock out button should be disabled during clock in operation");
            
            // Wait for operation to complete
            WaitForNoElement(ActivityIndicator);
            
            // After clock in
            Assert.IsFalse(App.Query(ClockInButton)[0].Enabled, "Clock in button should remain disabled after clock in");
            Assert.IsTrue(App.Query(ClockOutButton)[0].Enabled, "Clock out button should be enabled after clock in");
            
            // During clock out
            TapElement(ClockOutButton);
            
            // Check both buttons are disabled during operation
            Assert.IsFalse(App.Query(ClockInButton)[0].Enabled, "Clock in button should be disabled during clock out operation");
            Assert.IsFalse(App.Query(ClockOutButton)[0].Enabled, "Clock out button should be disabled during clock out operation");
            
            // Wait for operation to complete
            WaitForNoElement(ActivityIndicator);
            
            // After clock out
            Assert.IsTrue(App.Query(ClockInButton)[0].Enabled, "Clock in button should be enabled after clock out");
            Assert.IsFalse(App.Query(ClockOutButton)[0].Enabled, "Clock out button should remain disabled after clock out");
        }

        /// <summary>
        /// Helper method to ensure the user is clocked out before a test
        /// </summary>
        private void EnsureUserClockedOut()
        {
            var statusText = App.Query(ClockStatusLabel)[0].Text;
            
            if (statusText.Contains("Clocked In"))
            {
                TapElement(ClockOutButton);
                WaitForElement(ActivityIndicator);
                WaitForNoElement(ActivityIndicator);
                AssertElementText(ClockStatusLabel, "Currently Clocked Out");
            }
        }

        /// <summary>
        /// Helper method to ensure the user is clocked in before a test
        /// </summary>
        private void EnsureUserClockedIn()
        {
            var statusText = App.Query(ClockStatusLabel)[0].Text;
            
            if (statusText.Contains("Clocked Out"))
            {
                TapElement(ClockInButton);
                WaitForElement(ActivityIndicator);
                WaitForNoElement(ActivityIndicator);
                AssertElementText(ClockStatusLabel, "Currently Clocked In");
            }
        }
    }
}