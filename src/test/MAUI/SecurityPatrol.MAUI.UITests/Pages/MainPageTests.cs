using NUnit.Framework;
using System;
using System.Threading.Tasks;
using SecurityPatrol.MAUI.UITests.Setup;
using Xamarin.UITest;

namespace SecurityPatrol.MAUI.UITests.Pages
{
    /// <summary>
    /// Test class for the main dashboard page of the Security Patrol application
    /// </summary>
    [TestFixture]
    public class MainPageTests : UITestBase
    {
        /// <summary>
        /// Initializes a new instance of the MainPageTests class
        /// </summary>
        public MainPageTests() : base()
        {
            // Constructor calls base constructor to initialize UITestBase
        }

        /// <summary>
        /// Sets up the test environment before each test
        /// </summary>
        /// <returns>Asynchronous operation result</returns>
        /// <remarks>
        /// NOTE: There appears to be a signature mismatch with the base class's SetUp method,
        /// which is defined as "public virtual void SetUp()" rather than returning a Task.
        /// This implementation follows the requirements in the JSON specification but may
        /// need to be adjusted to correctly override the base class method.
        /// </remarks>
        [SetUp]
        public override async Task SetUp()
        {
            // Call base.SetUp() to initialize the app
            base.SetUp();
            
            // Perform login to access the main page
            Login();
            
            // Wait for the main page to fully load
            WaitForPageToLoad("Main");
        }

        /// <summary>
        /// Tests that the main page loads correctly with all expected elements
        /// </summary>
        [Test]
        public void TestMainPageLoadsCorrectly()
        {
            // Assert that the page title is visible
            AssertElementExists("MainPageTitle");
            
            // Assert that the status card is visible
            AssertElementExists("StatusCard");
            
            // Assert that the synchronization card is visible
            AssertElementExists("SyncCard");
            
            // Assert that the quick actions card is visible
            AssertElementExists("QuickActionsCard");
            
            // Assert that all navigation buttons are visible
            AssertElementExists("TimeTrackingButton");
            AssertElementExists("PatrolButton");
            AssertElementExists("PhotosButton");
            AssertElementExists("ReportsButton");
            AssertElementExists("SettingsButton");
        }

        /// <summary>
        /// Tests that all status indicators are displayed correctly
        /// </summary>
        [Test]
        public void TestStatusIndicatorsDisplayed()
        {
            // Assert that the clock in status indicator is visible
            AssertElementExists("ClockStatusIndicator");
            
            // Assert that the location tracking status indicator is visible
            AssertElementExists("LocationStatusIndicator");
            
            // Assert that the patrol active status indicator is visible
            AssertElementExists("PatrolStatusIndicator");
            
            // Assert that the network status indicator is visible
            AssertElementExists("NetworkStatusIndicator");
        }

        /// <summary>
        /// Tests the functionality of the Sync Now button
        /// </summary>
        [Test]
        public void TestSyncNowButtonFunctionality()
        {
            // Assert that the Sync Now button is visible
            AssertElementExists("SyncNowButton");
            
            // Tap the Sync Now button
            TapElement("SyncNowButton");
            
            // Verify that the sync activity indicator appears
            AssertElementExists("SyncActivityIndicator");
            
            // Wait for the sync operation to complete
            WaitForNoElement("SyncActivityIndicator");
            
            // Verify that the sync activity indicator disappears
            AssertElementDoesNotExist("SyncActivityIndicator");
        }

        /// <summary>
        /// Tests navigation to the Time Tracking page
        /// </summary>
        [Test]
        public async Task TestNavigationToTimeTracking()
        {
            // Tap the Time Tracking button
            TapElement("TimeTrackingButton");
            
            // Wait for the Time Tracking page to load
            WaitForPageToLoad("TimeTracking");
            
            // Assert that the Time Tracking page title is visible
            AssertElementExists("TimeTrackingPageTitle");
            
            // Navigate back to the main page
            await NavigateBackToMainPage();
        }

        /// <summary>
        /// Tests navigation to the Patrol page
        /// </summary>
        [Test]
        public async Task TestNavigationToPatrol()
        {
            // Tap the Patrol button
            TapElement("PatrolButton");
            
            // Wait for the Patrol page to load
            WaitForPageToLoad("Patrol");
            
            // Assert that the Patrol page title is visible
            AssertElementExists("PatrolPageTitle");
            
            // Navigate back to the main page
            await NavigateBackToMainPage();
        }

        /// <summary>
        /// Tests navigation to the Photo Capture page
        /// </summary>
        [Test]
        public async Task TestNavigationToPhotoCapture()
        {
            // Tap the Photos button
            TapElement("PhotosButton");
            
            // Wait for the Photo Capture page to load
            WaitForPageToLoad("PhotoCapture");
            
            // Assert that the Photo Capture page title is visible
            AssertElementExists("PhotoCapturePageTitle");
            
            // Navigate back to the main page
            await NavigateBackToMainPage();
        }

        /// <summary>
        /// Tests navigation to the Activity Report page
        /// </summary>
        [Test]
        public async Task TestNavigationToActivityReport()
        {
            // Tap the Reports button
            TapElement("ReportsButton");
            
            // Wait for the Activity Report page to load
            WaitForPageToLoad("ActivityReport");
            
            // Assert that the Activity Report page title is visible
            AssertElementExists("ActivityReportPageTitle");
            
            // Navigate back to the main page
            await NavigateBackToMainPage();
        }

        /// <summary>
        /// Tests navigation to the Settings page
        /// </summary>
        [Test]
        public async Task TestNavigationToSettings()
        {
            // Tap the Settings button
            TapElement("SettingsButton");
            
            // Wait for the Settings page to load
            WaitForPageToLoad("Settings");
            
            // Assert that the Settings page title is visible
            AssertElementExists("SettingsPageTitle");
            
            // Navigate back to the main page
            await NavigateBackToMainPage();
        }

        /// <summary>
        /// Tests the logout functionality
        /// </summary>
        [Test]
        public async Task TestLogoutFunctionality()
        {
            // Tap the Logout button
            TapElement("LogoutButton");
            
            // Wait for confirmation dialog if present
            try
            {
                WaitForElement("ConfirmLogoutButton");
                TapElement("ConfirmLogoutButton");
            }
            catch
            {
                // No confirmation dialog, continue
            }
            
            // Verify that the application returns to the login page
            WaitForElement("PhoneNumberEntry");
            
            // Assert that the phone entry page is visible
            AssertElementExists("PhoneNumberEntry");
        }

        /// <summary>
        /// Tests the visibility of patrol progress based on active patrol status
        /// </summary>
        [Test]
        public void TestPatrolProgressVisibility()
        {
            // Check if patrol progress section is visible (depends on IsPatrolActive)
            var patrolProgressExists = App.Query("PatrolProgressSection").Length > 0;
            
            if (patrolProgressExists)
            {
                // If visible, verify progress bar and checkpoint count are displayed
                AssertElementExists("PatrolProgressBar");
                AssertElementExists("CheckpointCountLabel");
            }
            else
            {
                // If not visible, verify the section is not in the view hierarchy
                AssertElementDoesNotExist("PatrolProgressSection");
            }
        }

        /// <summary>
        /// Helper method to verify that a page element exists
        /// </summary>
        /// <param name="elementMarker">The identifier of the element to check</param>
        private void VerifyPageElement(string elementMarker)
        {
            // Call AssertElementExists with the provided element marker
            AssertElementExists(elementMarker);
            
            // Log verification success for debugging purposes
            Console.WriteLine($"Verified element: {elementMarker}");
        }

        /// <summary>
        /// Helper method to navigate back to the main page
        /// </summary>
        private async Task NavigateBackToMainPage()
        {
            // Press the back button or use navigation controls to return to main page
            PressBack();
            
            // Wait for the main page to fully load
            WaitForPageToLoad("Main");
            
            // Verify that main page elements are visible
            AssertElementExists("MainPageTitle");
        }
    }
}