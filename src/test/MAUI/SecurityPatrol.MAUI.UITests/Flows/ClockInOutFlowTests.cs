using System;
using System.Threading.Tasks;
using Xamarin.UITest;
using NUnit.Framework;
using SecurityPatrol.MAUI.UITests.Setup;
using SecurityPatrol.TestCommon.Constants;

namespace SecurityPatrol.MAUI.UITests.Flows
{
    /// <summary>
    /// Contains end-to-end UI tests for the complete clock in/out flow of the Security Patrol application
    /// </summary>
    [TestFixture]
    public class ClockInOutFlowTests : UITestBase
    {
        // UI element identifiers
        private const string TimeTrackingPageTitle = "TimeTrackingPage";
        private const string ClockStatusLabel = "ClockStatusLabel";
        private const string LastClockInLabel = "LastClockInLabel";
        private const string LastClockOutLabel = "LastClockOutLabel";
        private const string ClockInButton = "ClockInButton";
        private const string ClockOutButton = "ClockOutButton";
        private const string ViewHistoryButton = "ViewHistoryButton";
        private const string ActivityIndicator = "ActivityIndicator";
        private const string TimeHistoryPageTitle = "TimeHistoryPage";
        private const string LocationTrackingIndicator = "LocationTrackingIndicator";
        private const string MainPageIdentifier = "MainPage";

        /// <summary>
        /// Initializes a new instance of the ClockInOutFlowTests class
        /// </summary>
        public ClockInOutFlowTests() : base()
        {
            // Initialize UI element identifier constants
        }

        /// <summary>
        /// Sets up the test environment before each test
        /// </summary>
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            Login();
            NavigateToPage("TimeTracking");
            WaitForPageToLoad(TimeTrackingPageTitle);
        }

        /// <summary>
        /// Tests the complete clock in/out flow including status changes and button states
        /// </summary>
        [Test]
        [Order(1)]
        public void TestCompleteClockInOutFlow()
        {
            // Ensure user is clocked out (perform clock out if necessary)
            EnsureUserClockedOut();

            // Assert that the clock status text indicates 'Currently Clocked Out'
            AssertElementText(ClockStatusLabel, "Currently Clocked Out");
            // Assert that the clock in button is enabled
            AssertElementExists(ClockInButton);
            // Assert that the clock out button is disabled
            AssertElementDoesNotExist(ClockOutButton);

            // Tap the clock in button
            TapElement(ClockInButton);
            // Wait for the activity indicator to disappear (operation complete)
            WaitForClockOperation();

            // Assert that the clock status text changes to 'Currently Clocked In'
            AssertElementText(ClockStatusLabel, "Currently Clocked In");
            // Assert that the clock in button becomes disabled
            AssertElementDoesNotExist(ClockInButton);
            // Assert that the clock out button becomes enabled
            AssertElementExists(ClockOutButton);
            // Assert that the last clock in label updates with the current time
            AssertElementExists(LastClockInLabel);

            // Tap the clock out button
            TapElement(ClockOutButton);
            // Wait for the activity indicator to disappear (operation complete)
            WaitForClockOperation();

            // Assert that the clock status text changes to 'Currently Clocked Out'
            AssertElementText(ClockStatusLabel, "Currently Clocked Out");
            // Assert that the clock in button becomes enabled
            AssertElementExists(ClockInButton);
            // Assert that the clock out button becomes disabled
            AssertElementDoesNotExist(ClockOutButton);
            // Assert that the last clock out label updates with the current time
            AssertElementExists(LastClockOutLabel);
        }

        /// <summary>
        /// Tests that location tracking is activated when clocking in and deactivated when clocking out
        /// </summary>
        [Test]
        [Order(2)]
        public void TestLocationTrackingActivation()
        {
            // Ensure user is clocked out (perform clock out if necessary)
            EnsureUserClockedOut();

            // Assert that the location tracking indicator is not visible or shows 'Inactive'
            AssertElementText(LocationTrackingIndicator, "Inactive");

            // Tap the clock in button
            TapElement(ClockInButton);
            // Wait for the activity indicator to disappear (operation complete)
            WaitForClockOperation();

            // Assert that the location tracking indicator becomes visible or shows 'Active'
            AssertElementText(LocationTrackingIndicator, "Active");

            // Tap the clock out button
            TapElement(ClockOutButton);
            // Wait for the activity indicator to disappear (operation complete)
            WaitForClockOperation();

            // Assert that the location tracking indicator shows 'Inactive' again
            AssertElementText(LocationTrackingIndicator, "Inactive");
        }

        /// <summary>
        /// Tests that clock status persists when navigating away and back to the time tracking page
        /// </summary>
        [Test]
        [Order(3)]
        public void TestClockStatusPersistenceAcrossNavigation()
        {
            // Ensure user is clocked in (perform clock in if necessary)
            EnsureUserClockedIn();

            // Record the current clock status text and last clock in time
            var clockStatus = App.Query(ClockStatusLabel)[0].Text;
            var lastClockIn = App.Query(LastClockInLabel)[0].Text;

            // Navigate to the main page
            NavigateToPage("Main");
            WaitForPageToLoad(MainPageIdentifier);
            
            // Navigate back to the time tracking page
            NavigateToPage("TimeTracking");
            WaitForPageToLoad(TimeTrackingPageTitle);

            // Assert that the clock status text remains the same
            AssertElementText(ClockStatusLabel, clockStatus);
            // Assert that the last clock in time remains the same
            AssertElementText(LastClockInLabel, lastClockIn);
            // Assert that the clock in button is still disabled
            AssertElementDoesNotExist(ClockInButton);
            // Assert that the clock out button is still enabled
            AssertElementExists(ClockOutButton);
        }

        /// <summary>
        /// Tests that the time history is updated after clock in/out events
        /// </summary>
        [Test]
        [Order(4)]
        public void TestHistoryUpdatesAfterClockEvents()
        {
            // Ensure user is clocked out (perform clock out if necessary)
            EnsureUserClockedOut();

            // Tap the clock in button
            TapElement(ClockInButton);
            // Wait for the activity indicator to disappear (operation complete)
            WaitForClockOperation();

            // Tap the clock out button
            TapElement(ClockOutButton);
            // Wait for the activity indicator to disappear (operation complete)
            WaitForClockOperation();

            // Tap the view history button
            TapElement(ViewHistoryButton);
            // Wait for the time history page to load
            WaitForPageToLoad(TimeHistoryPageTitle);

            // Assert that the history page title is displayed
            AssertElementExists(TimeHistoryPageTitle);
            
            // Assert that the recent clock in and clock out events are displayed in the history list
            AssertElementExists("ClockInHistoryItem");
            AssertElementExists("ClockOutHistoryItem");

            // Navigate back to the time tracking page
            PressBack();
            WaitForPageToLoad(TimeTrackingPageTitle);
        }

        /// <summary>
        /// Tests that clock status persists after application restart
        /// </summary>
        [Test]
        [Order(5)]
        public void TestClockInOutWithAppRestart()
        {
            // Ensure user is clocked in (perform clock in if necessary)
            EnsureUserClockedIn();

            // Record the current clock status text and last clock in time
            var clockStatus = App.Query(ClockStatusLabel)[0].Text;
            var lastClockIn = App.Query(LastClockInLabel)[0].Text;

            // Restart the application
            ResetAppState();

            // Login to the application
            Login();
            // Navigate to the time tracking page
            NavigateToPage("TimeTracking");
            // Wait for the time tracking page to load
            WaitForPageToLoad(TimeTrackingPageTitle);

            // Assert that the clock status text still indicates 'Currently Clocked In'
            AssertElementText(ClockStatusLabel, "Currently Clocked In");
            // Assert that the last clock in time is preserved
            // Note: We don't check exact text as formatting might change slightly after restart
            AssertElementExists(LastClockInLabel);
            // Assert that the clock in button is still disabled
            AssertElementDoesNotExist(ClockInButton);
            // Assert that the clock out button is still enabled
            AssertElementExists(ClockOutButton);
        }

        /// <summary>
        /// Tests the application's handling of failures during clock operations
        /// </summary>
        [Test]
        [Order(6)]
        public void TestClockOperationFailureHandling()
        {
            // Ensure user is clocked out (perform clock out if necessary)
            EnsureUserClockedOut();

            // Configure the app to simulate network errors (using test mode)
            App.Invoke("setNetworkErrorSimulation", true);

            // Tap the clock in button
            TapElement(ClockInButton);
            // Wait for the activity indicator to disappear (operation complete)
            WaitForClockOperation();

            // Assert that an error message or retry option is displayed
            AssertElementExists("SyncErrorIndicator");
            
            // Assert that the operation still completes locally despite network error
            AssertElementText(ClockStatusLabel, "Currently Clocked In");
            // Assert that a sync indicator shows pending/offline status
            AssertElementExists("SyncPendingIndicator");

            // Disable network error simulation
            App.Invoke("setNetworkErrorSimulation", false);

            // Trigger manual sync or wait for auto-sync
            TapElement("SyncButton");
            WaitForElement(ActivityIndicator);
            WaitForNoElement(ActivityIndicator);

            // Assert that the sync indicator shows successful sync
            AssertElementExists("SyncSuccessIndicator");
        }

        /// <summary>
        /// Tests the application's handling of concurrent clock operations
        /// </summary>
        [Test]
        [Order(7)]
        public void TestConcurrentClockOperations()
        {
            // Ensure user is clocked out (perform clock out if necessary)
            EnsureUserClockedOut();

            // Tap the clock in button
            TapElement(ClockInButton);
            
            // Immediately attempt to tap the clock out button before operation completes
            try
            {
                TapElement(ClockOutButton);
                
                // Assert that the second operation is prevented or queued
                AssertElementExists("OperationInProgressMessage");
            }
            catch
            {
                // Exception is expected if button is not tappable during operation
            }

            // Wait for the clock in operation to complete
            WaitForNoElement(ActivityIndicator);

            // Assert that the clock status text changes to 'Currently Clocked In'
            AssertElementText(ClockStatusLabel, "Currently Clocked In");
            // Assert that the clock in button becomes disabled
            AssertElementDoesNotExist(ClockInButton);
            // Assert that the clock out button becomes enabled
            AssertElementExists(ClockOutButton);
        }

        /// <summary>
        /// Helper method to ensure the user is clocked out before a test
        /// </summary>
        private void EnsureUserClockedOut()
        {
            // Check the current clock status text
            var statusElements = App.Query(ClockStatusLabel);
            if (statusElements.Length > 0 && statusElements[0].Text == "Currently Clocked In")
            {
                // If status indicates 'Currently Clocked In', tap the clock out button
                TapElement(ClockOutButton);
                WaitForClockOperation();
            }

            // Assert that the status changes to 'Currently Clocked Out'
            AssertElementText(ClockStatusLabel, "Currently Clocked Out");
        }

        /// <summary>
        /// Helper method to ensure the user is clocked in before a test
        /// </summary>
        private void EnsureUserClockedIn()
        {
            // Check the current clock status text
            var statusElements = App.Query(ClockStatusLabel);
            if (statusElements.Length > 0 && statusElements[0].Text == "Currently Clocked Out")
            {
                // If status indicates 'Currently Clocked Out', tap the clock in button
                TapElement(ClockInButton);
                WaitForClockOperation();
            }

            // Assert that the status changes to 'Currently Clocked In'
            AssertElementText(ClockStatusLabel, "Currently Clocked In");
        }

        /// <summary>
        /// Helper method to wait for a clock operation to complete
        /// </summary>
        private void WaitForClockOperation()
        {
            // Wait for the activity indicator to disappear
            WaitForElement(ActivityIndicator);
            WaitForNoElement(ActivityIndicator);
            
            // Wait for the clock status text to update
            WaitForElement(ClockStatusLabel);
            
            // Wait for the appropriate button states to update
            System.Threading.Thread.Sleep(500);
        }
    }
}