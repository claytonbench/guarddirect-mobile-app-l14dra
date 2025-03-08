using NUnit.Framework;
using System;
using System.Threading.Tasks;
using SecurityPatrol.MAUI.UITests.Setup;
using SecurityPatrol.TestCommon.Constants;

namespace SecurityPatrol.MAUI.UITests.Pages
{
    /// <summary>
    /// Tests for the Settings page UI and functionality
    /// </summary>
    [TestFixture]
    public class SettingsPageTests : UITestBase
    {
        /// <summary>
        /// Initializes a new instance of the SettingsPageTests class
        /// </summary>
        public SettingsPageTests() : base()
        {
            // Constructor calls base constructor to initialize UITestBase
        }

        /// <summary>
        /// Sets up the test environment before each test
        /// </summary>
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            Login();
            NavigateToPage("Settings");
        }

        /// <summary>
        /// Tests that the Settings page loads correctly with all expected elements
        /// </summary>
        [Test]
        public void TestSettingsPageLoads()
        {
            AssertElementExists("SettingsTitle");
            AssertElementText("SettingsTitle", "Settings");
            AssertElementExists("AppVersionLabel");
            AssertElementExists("UserPhoneNumberLabel");
            AssertElementExists("BackgroundTrackingSwitch");
            AssertElementExists("OfflineModeSwitch");
            AssertElementExists("TelemetrySwitch");
            AssertElementExists("LocationTrackingModePicker");
            AssertElementExists("SaveSettingsButton");
            AssertElementExists("ClearDataButton");
            AssertElementExists("LogoutButton");
        }

        /// <summary>
        /// Tests that user information is correctly displayed on the Settings page
        /// </summary>
        [Test]
        public void TestUserInfoDisplay()
        {
            AssertElementExists("UserPhoneNumberLabel");
            AssertElementText("UserPhoneNumberLabel", TestConstants.TestPhoneNumber);
            AssertElementExists("AppVersionLabel");
            // Cannot assert exact app version as it will change with builds
            // But can verify it exists and has content
            var element = App.Query("AppVersionLabel")[0];
            Assert.IsNotNull(element, "App version label not found");
            Assert.IsFalse(string.IsNullOrEmpty(element.Text), "App version should not be empty");
        }

        /// <summary>
        /// Tests toggling the background tracking setting
        /// </summary>
        [Test]
        public void TestToggleBackgroundTracking()
        {
            // Get initial state of the switch
            var initialState = App.Query("BackgroundTrackingSwitch")[0].Text;
            
            // Toggle the switch
            TapElement("BackgroundTrackingSwitch");
            
            // Save settings
            TapElement("SaveSettingsButton");
            
            // Wait for success message
            WaitForElement("SettingsSavedMessage");
            
            // Navigate away and back to verify persistence
            NavigateToPage("Home");
            NavigateToPage("Settings");
            
            // Verify the setting was saved
            var newState = App.Query("BackgroundTrackingSwitch")[0].Text;
            Assert.AreNotEqual(initialState, newState, "Background tracking switch state should change");
        }

        /// <summary>
        /// Tests toggling the offline mode setting
        /// </summary>
        [Test]
        public void TestToggleOfflineMode()
        {
            // Get initial state of the switch
            var initialState = App.Query("OfflineModeSwitch")[0].Text;
            
            // Toggle the switch
            TapElement("OfflineModeSwitch");
            
            // Save settings
            TapElement("SaveSettingsButton");
            
            // Wait for success message
            WaitForElement("SettingsSavedMessage");
            
            // Navigate away and back to verify persistence
            NavigateToPage("Home");
            NavigateToPage("Settings");
            
            // Verify the setting was saved
            var newState = App.Query("OfflineModeSwitch")[0].Text;
            Assert.AreNotEqual(initialState, newState, "Offline mode switch state should change");
        }

        /// <summary>
        /// Tests toggling the telemetry setting
        /// </summary>
        [Test]
        public void TestToggleTelemetry()
        {
            // Get initial state of the switch
            var initialState = App.Query("TelemetrySwitch")[0].Text;
            
            // Toggle the switch
            TapElement("TelemetrySwitch");
            
            // Save settings
            TapElement("SaveSettingsButton");
            
            // Wait for success message
            WaitForElement("SettingsSavedMessage");
            
            // Navigate away and back to verify persistence
            NavigateToPage("Home");
            NavigateToPage("Settings");
            
            // Verify the setting was saved
            var newState = App.Query("TelemetrySwitch")[0].Text;
            Assert.AreNotEqual(initialState, newState, "Telemetry switch state should change");
        }

        /// <summary>
        /// Tests changing the location tracking mode
        /// </summary>
        [Test]
        public void TestChangeLocationTrackingMode()
        {
            // Get current mode
            var currentMode = App.Query("LocationTrackingModePicker")[0].Text;
            
            // Tap on picker to show options
            TapElement("LocationTrackingModePicker");
            
            // Wait for picker options to appear
            WaitForElement("PickerOptions");
            
            // Select a different mode based on current selection
            string newMode;
            if (currentMode.Contains("High Accuracy"))
            {
                newMode = "Balanced";
            }
            else if (currentMode.Contains("Balanced"))
            {
                newMode = "Low Power";
            }
            else
            {
                newMode = "High Accuracy";
            }
            
            TapElement(newMode);
            
            // Save settings
            TapElement("SaveSettingsButton");
            
            // Wait for success message
            WaitForElement("SettingsSavedMessage");
            
            // Navigate away and back to verify persistence
            NavigateToPage("Home");
            NavigateToPage("Settings");
            
            // Verify mode has changed
            var actualMode = App.Query("LocationTrackingModePicker")[0].Text;
            Assert.IsTrue(actualMode.Contains(newMode), $"Location tracking mode should be changed to {newMode}");
        }

        /// <summary>
        /// Tests the save settings functionality
        /// </summary>
        [Test]
        public void TestSaveSettings()
        {
            // Make changes to multiple settings
            TapElement("BackgroundTrackingSwitch");
            TapElement("OfflineModeSwitch");
            TapElement("TelemetrySwitch");
            
            // Tap the location tracking mode picker
            TapElement("LocationTrackingModePicker");
            WaitForElement("PickerOptions");
            TapElement("Balanced"); // Select a specific mode
            
            // Save settings
            TapElement("SaveSettingsButton");
            
            // Wait for success message
            WaitForElement("SettingsSavedMessage");
            
            // Navigate away and back to verify persistence
            NavigateToPage("Home");
            NavigateToPage("Settings");
            
            // Verify settings page loads with all elements
            AssertElementExists("BackgroundTrackingSwitch");
            AssertElementExists("OfflineModeSwitch");
            AssertElementExists("TelemetrySwitch");
            AssertElementExists("LocationTrackingModePicker");
            
            // Check that tracking mode was set to Balanced
            var actualMode = App.Query("LocationTrackingModePicker")[0].Text;
            Assert.IsTrue(actualMode.Contains("Balanced"), "Location tracking mode should be set to Balanced");
        }

        /// <summary>
        /// Tests the clear data functionality
        /// </summary>
        [Test]
        public void TestClearData()
        {
            // Scroll to the clear data button if needed
            ScrollTo("ClearDataButton");
            
            // Tap clear data
            TapElement("ClearDataButton");
            
            // Confirm the action
            WaitForElement("ConfirmClearDataDialog");
            TapElement("ConfirmClearDataButton");
            
            // Check success message
            WaitForElement("DataClearedMessage");
            
            // Verify settings are reset to default values
            // Note: Specific verification will depend on the default values in the app
            AssertElementExists("BackgroundTrackingSwitch");
            AssertElementExists("OfflineModeSwitch");
            AssertElementExists("TelemetrySwitch");
            AssertElementExists("LocationTrackingModePicker");
            
            // Check defaults have been applied (assuming defaults are known)
            // For example, if default tracking mode is "Balanced":
            var actualMode = App.Query("LocationTrackingModePicker")[0].Text;
            Assert.IsTrue(actualMode.Contains("Balanced"), "Location tracking mode should be reset to default");
        }

        /// <summary>
        /// Tests the logout functionality
        /// </summary>
        [Test]
        public void TestLogout()
        {
            // Scroll to the logout button if needed
            ScrollTo("LogoutButton");
            
            // Tap logout
            TapElement("LogoutButton");
            
            // Confirm logout
            WaitForElement("ConfirmLogoutDialog");
            TapElement("ConfirmLogoutButton");
            
            // Verify we're on login page
            WaitForElement("PhoneNumberEntry");
            AssertElementExists("PhoneNumberEntry");
        }

        /// <summary>
        /// Tests canceling the logout operation
        /// </summary>
        [Test]
        public void TestCancelLogout()
        {
            // Scroll to the logout button if needed
            ScrollTo("LogoutButton");
            
            // Tap logout
            TapElement("LogoutButton");
            
            // Cancel logout
            WaitForElement("ConfirmLogoutDialog");
            TapElement("CancelLogoutButton");
            
            // Verify we're still on settings page
            AssertElementExists("SettingsTitle");
            AssertElementExists("LogoutButton");
        }
    }
}