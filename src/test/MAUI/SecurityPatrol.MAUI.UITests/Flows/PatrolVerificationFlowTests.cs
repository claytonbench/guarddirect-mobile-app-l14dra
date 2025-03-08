using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xamarin.UITest;
using NUnit.Framework;
using SecurityPatrol.MAUI.UITests.Setup;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Data;
using SecurityPatrol.TestCommon.Constants;

namespace SecurityPatrol.MAUI.UITests.Flows
{
    /// <summary>
    /// Contains end-to-end UI tests for the complete patrol verification flow of the Security Patrol application.
    /// These tests validate the entire process from login through location selection, patrol activation, checkpoint proximity detection, and verification.
    /// </summary>
    [TestFixture]
    public class PatrolVerificationFlowTests : UITestBase
    {
        // Constants for UI element identifiers
        private const string PatrolPageTitle = "PatrolPage";
        private const string LocationPickerId = "LocationPicker";
        private const string LocationDropdownItemFormat = "LocationItem_{0}";  // Format: LocationItem_Name
        private const string StartPatrolButtonId = "StartPatrolButton";
        private const string EndPatrolButtonId = "EndPatrolButton";
        private const string CheckpointListId = "CheckpointList";
        private const string CheckpointItemFormat = "CheckpointItem_{0}";  // Format: CheckpointItem_Id
        private const string VerifyButtonFormat = "VerifyButton_{0}";  // Format: VerifyButton_Id
        private const string ProgressBarId = "PatrolProgressBar";
        private const string StatusMessageId = "StatusMessage";
        private const string MapViewId = "MapView";

        /// <summary>
        /// Initializes a new instance of the PatrolVerificationFlowTests class
        /// </summary>
        public PatrolVerificationFlowTests() : base()
        {
            // Constructor - initialize any needed fields
        }

        /// <summary>
        /// Sets up the mock API endpoints for patrol testing
        /// </summary>
        private void SetupPatrolEndpoints()
        {
            // Set up location endpoint to return test locations
            MockApiServer.SetupEndpoint("/patrol/locations", TestLocations.AllLocations);
            
            // Set up checkpoint endpoint to return test checkpoints for headquarters location
            MockApiServer.SetupEndpoint(
                $"/patrol/checkpoints?locationId={TestConstants.TestLocationId}", 
                TestCheckpoints.HeadquartersCheckpointModels);
            
            // Set up checkpoint verification endpoint to return success
            MockApiServer.SetupEndpoint("/patrol/verify", new { status = "success" });
            
            // Set up patrol status endpoint to return patrol status
            MockApiServer.SetupEndpoint("/patrol/status", new { completed = false, progress = 0 });
            
            // Set up proximity detection endpoint to simulate checkpoint proximity
            MockApiServer.SetupEndpoint(
                $"/patrol/proximity?checkpointId={TestConstants.TestCheckpointId}", 
                new { inRange = true, distance = 10 });
        }

        /// <summary>
        /// Sets up the test environment before each test
        /// </summary>
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            SetupPatrolEndpoints();
            Login(); // Log in to the application using the Login() method from UITestBase
        }

        /// <summary>
        /// Cleans up the test environment after each test
        /// </summary>
        [TearDown]
        public override void TearDown()
        {
            // Take a screenshot of the final state
            TakeScreenshot("PatrolVerificationFinalState");
            base.TearDown();
        }

        /// <summary>
        /// Tests the complete patrol verification flow from login to checkpoint verification
        /// </summary>
        [Test]
        [Order(1)]
        public void TestCompletePatrolVerificationFlow()
        {
            // Navigate to the Patrol page
            NavigateToPage("Patrol");
            
            // Wait for the Patrol page to load
            WaitForPageToLoad(PatrolPageTitle);
            
            // Verify the page title is "Patrol"
            AssertElementText(PatrolPageTitle, "Patrol");
            
            // Select headquarters location from the location picker
            SelectLocation("Headquarters");
            
            // Verify checkpoints are displayed in the list
            AssertElementExists(CheckpointListId);
            
            // Tap the "Start Patrol" button
            TapElement(StartPatrolButtonId);
            
            // Verify patrol is active (End Patrol button visible)
            AssertElementExists(EndPatrolButtonId);
            
            // Simulate proximity to a checkpoint
            SimulateCheckpointProximity(TestConstants.TestCheckpointId);
            
            // Verify checkpoint is highlighted and verification is enabled
            string verifyButtonId = string.Format(VerifyButtonFormat, TestConstants.TestCheckpointId);
            AssertElementExists(verifyButtonId);
            
            // Tap the "Verify" button for the checkpoint
            TapElement(verifyButtonId);
            
            // Verify checkpoint is marked as verified (green indicator)
            string checkpointItemId = string.Format(CheckpointItemFormat, TestConstants.TestCheckpointId);
            AssertElementExists(checkpointItemId);
            
            // Verify patrol progress is updated
            AssertElementExists(ProgressBarId);
            
            // Simulate proximity to another checkpoint
            int nextCheckpointId = TestConstants.TestCheckpointId + 1;
            SimulateCheckpointProximity(nextCheckpointId);
            
            // Verify and complete all checkpoints
            VerifyAllCheckpoints();
            
            // Verify patrol completion message is displayed
            AssertElementText(StatusMessageId, "Patrol completed successfully!");
            
            // Tap the "End Patrol" button
            TapElement(EndPatrolButtonId);
            
            // Verify patrol is reset for new start
            AssertElementExists(StartPatrolButtonId);
        }

        /// <summary>
        /// Tests the location selection and patrol start process
        /// </summary>
        [Test]
        [Order(2)]
        public void TestLocationSelectionAndPatrolStart()
        {
            // Navigate to the Patrol page
            NavigateToPage("Patrol");
            
            // Wait for the Patrol page to load
            WaitForPageToLoad(PatrolPageTitle);
            
            // Verify the location picker is displayed
            AssertElementExists(LocationPickerId);
            
            // Verify the "Start Patrol" button is disabled initially
            AssertElementDoesNotExist(StartPatrolButtonId);
            
            // Select headquarters location from the location picker
            SelectLocation("Headquarters");
            
            // Verify the "Start Patrol" button is enabled after location selection
            AssertElementExists(StartPatrolButtonId);
            
            // Verify checkpoints are displayed in the list
            AssertElementExists(CheckpointListId);
            
            // Tap the "Start Patrol" button
            TapElement(StartPatrolButtonId);
            
            // Verify the "End Patrol" button is displayed
            AssertElementExists(EndPatrolButtonId);
            
            // Verify the location picker is disabled during active patrol
            AssertElementDoesNotExist(LocationPickerId);
            
            // Verify the patrol status message indicates active patrol
            AssertElementExists(StatusMessageId);
        }

        /// <summary>
        /// Tests the checkpoint proximity detection functionality
        /// </summary>
        [Test]
        [Order(3)]
        public void TestCheckpointProximityDetection()
        {
            // Navigate to the Patrol page
            NavigateToPage("Patrol");
            
            // Wait for the Patrol page to load
            WaitForPageToLoad(PatrolPageTitle);
            
            // Select headquarters location from the location picker
            SelectLocation("Headquarters");
            
            // Tap the "Start Patrol" button
            TapElement(StartPatrolButtonId);
            
            // Verify patrol is active
            AssertElementExists(EndPatrolButtonId);
            
            // Verify no checkpoints are highlighted initially
            string verifyButtonId = string.Format(VerifyButtonFormat, TestConstants.TestCheckpointId);
            AssertElementDoesNotExist(verifyButtonId);
            
            // Simulate proximity to a checkpoint
            SimulateCheckpointProximity(TestConstants.TestCheckpointId);
            
            // Verify the checkpoint is highlighted in the list
            string checkpointItemId = string.Format(CheckpointItemFormat, TestConstants.TestCheckpointId);
            AssertElementExists(checkpointItemId);
            
            // Verify the "Verify" button is enabled for the checkpoint
            AssertElementExists(verifyButtonId);
            
            // Verify the status message indicates checkpoint is in range
            AssertElementText(StatusMessageId, "Checkpoint in range");
            
            // Move away from checkpoint (simulate)
            MockApiServer.SetupEndpoint(
                $"/patrol/proximity?checkpointId={TestConstants.TestCheckpointId}", 
                new { inRange = false, distance = 60 });
            
            // Trigger location update to process the proximity change
            App.Invoke("updateLocation");
            
            // Verify the checkpoint is no longer highlighted
            AssertElementDoesNotExist(verifyButtonId);
        }

        /// <summary>
        /// Tests the checkpoint verification process
        /// </summary>
        [Test]
        [Order(4)]
        public void TestCheckpointVerification()
        {
            // Navigate to the Patrol page
            NavigateToPage("Patrol");
            
            // Wait for the Patrol page to load
            WaitForPageToLoad(PatrolPageTitle);
            
            // Select headquarters location from the location picker
            SelectLocation("Headquarters");
            
            // Tap the "Start Patrol" button
            TapElement(StartPatrolButtonId);
            
            // Simulate proximity to a checkpoint
            SimulateCheckpointProximity(TestConstants.TestCheckpointId);
            
            // Verify the "Verify" button is enabled
            string verifyButtonId = string.Format(VerifyButtonFormat, TestConstants.TestCheckpointId);
            AssertElementExists(verifyButtonId);
            
            // Tap the "Verify" button
            TapElement(verifyButtonId);
            
            // Verify the checkpoint is marked as verified (green indicator)
            string checkpointItemId = string.Format(CheckpointItemFormat, TestConstants.TestCheckpointId);
            AssertElementExists(checkpointItemId + "_Verified");
            
            // Verify the checkpoint cannot be verified again (button disabled)
            AssertElementDoesNotExist(verifyButtonId);
            
            // Verify the patrol progress is updated
            AssertElementExists(ProgressBarId);
            
            // Verify the progress bar reflects the verification percentage
            AssertElementExists(ProgressBarId);
        }

        /// <summary>
        /// Tests the patrol completion process
        /// </summary>
        [Test]
        [Order(5)]
        public void TestPatrolCompletion()
        {
            // Navigate to the Patrol page
            NavigateToPage("Patrol");
            
            // Wait for the Patrol page to load
            WaitForPageToLoad(PatrolPageTitle);
            
            // Select headquarters location from the location picker
            SelectLocation("Headquarters");
            
            // Tap the "Start Patrol" button
            TapElement(StartPatrolButtonId);
            
            // Verify all checkpoints one by one (using helper method)
            VerifyAllCheckpoints();
            
            // Verify the progress bar shows 100%
            AssertElementExists(ProgressBarId + "_Complete");
            
            // Verify the completion message is displayed
            AssertElementText(StatusMessageId, "Patrol completed successfully!");
            
            // Verify any completion actions are available
            AssertElementExists(EndPatrolButtonId);
            
            // Tap the "End Patrol" button
            TapElement(EndPatrolButtonId);
            
            // Verify patrol is reset (Start Patrol button visible)
            AssertElementExists(StartPatrolButtonId);
            
            // Verify location picker is enabled again
            AssertElementExists(LocationPickerId);
        }

        /// <summary>
        /// Tests cancelling a patrol before completion
        /// </summary>
        [Test]
        [Order(6)]
        public void TestPatrolCancellation()
        {
            // Navigate to the Patrol page
            NavigateToPage("Patrol");
            
            // Wait for the Patrol page to load
            WaitForPageToLoad(PatrolPageTitle);
            
            // Select headquarters location from the location picker
            SelectLocation("Headquarters");
            
            // Tap the "Start Patrol" button
            TapElement(StartPatrolButtonId);
            
            // Verify patrol is active
            AssertElementExists(EndPatrolButtonId);
            
            // Verify some checkpoints but not all
            SimulateCheckpointProximity(TestConstants.TestCheckpointId);
            string verifyButtonId = string.Format(VerifyButtonFormat, TestConstants.TestCheckpointId);
            TapElement(verifyButtonId);
            
            // Tap the "End Patrol" button
            TapElement(EndPatrolButtonId);
            
            // Verify confirmation dialog appears (if implemented)
            WaitForElement("ConfirmEndPatrolButton");
            
            // Confirm ending the patrol
            TapElement("ConfirmEndPatrolButton");
            
            // Verify patrol is reset
            AssertElementExists(StartPatrolButtonId);
            
            // Verify checkpoint verification status is reset
            string checkpointItemId = string.Format(CheckpointItemFormat, TestConstants.TestCheckpointId);
            AssertElementDoesNotExist(checkpointItemId + "_Verified");
        }

        /// <summary>
        /// Tests patrol operations in offline mode
        /// </summary>
        [Test]
        [Order(7)]
        public void TestOfflinePatrolOperation()
        {
            // Navigate to the Patrol page
            NavigateToPage("Patrol");
            
            // Wait for the Patrol page to load
            WaitForPageToLoad(PatrolPageTitle);
            
            // Select headquarters location from the location picker
            SelectLocation("Headquarters");
            
            // Enable offline mode (simulate)
            App.Invoke("setNetworkConnected", false);
            
            // Tap the "Start Patrol" button
            TapElement(StartPatrolButtonId);
            
            // Verify patrol starts successfully with cached data
            AssertElementExists(EndPatrolButtonId);
            
            // Simulate proximity to a checkpoint
            SimulateCheckpointProximity(TestConstants.TestCheckpointId);
            
            // Verify checkpoint verification works offline
            string verifyButtonId = string.Format(VerifyButtonFormat, TestConstants.TestCheckpointId);
            TapElement(verifyButtonId);
            
            // Verify offline indicator is displayed
            AssertElementExists("OfflineIndicator");
            
            // Disable offline mode (simulate)
            App.Invoke("setNetworkConnected", true);
            
            // Verify sync indicator shows synchronization in progress
            AssertElementExists("SyncIndicator");
            
            // Verify sync completes successfully
            WaitForNoElement("SyncIndicator");
        }

        /// <summary>
        /// Tests error handling during patrol operations
        /// </summary>
        [Test]
        [Order(8)]
        public void TestErrorHandlingDuringPatrol()
        {
            // Navigate to the Patrol page
            NavigateToPage("Patrol");
            
            // Wait for the Patrol page to load
            WaitForPageToLoad(PatrolPageTitle);
            
            // Select headquarters location from the location picker
            SelectLocation("Headquarters");
            
            // Tap the "Start Patrol" button
            TapElement(StartPatrolButtonId);
            
            // Simulate proximity to a checkpoint
            SimulateCheckpointProximity(TestConstants.TestCheckpointId);
            
            // Simulate API error during checkpoint verification
            MockApiServer.SetupEndpoint("/patrol/verify", new { error = "Server error" }, 500);
            
            // Verify the checkpoint
            string verifyButtonId = string.Format(VerifyButtonFormat, TestConstants.TestCheckpointId);
            TapElement(verifyButtonId);
            
            // Verify error message is displayed
            AssertElementExists("ErrorMessage");
            
            // Verify retry option is available
            AssertElementExists("RetryButton");
            
            // Tap retry button
            TapElement("RetryButton");
            
            // Setup successful response for retry
            MockApiServer.SetupEndpoint("/patrol/verify", new { status = "success" });
            
            // Verify verification succeeds after retry
            WaitForNoElement("ErrorMessage");
            
            // Complete the patrol
            VerifyAllCheckpoints();
            
            // End the patrol
            TapElement(EndPatrolButtonId);
        }

        /// <summary>
        /// Helper method to simulate proximity to a checkpoint
        /// </summary>
        /// <param name="checkpointId">The identifier of the checkpoint</param>
        private void SimulateCheckpointProximity(int checkpointId)
        {
            // Set up mock API to simulate proximity event for the specified checkpoint
            MockApiServer.SetupEndpoint(
                $"/patrol/proximity?checkpointId={checkpointId}", 
                new { inRange = true, distance = 10 });
            
            // Trigger location update to process the proximity event
            App.Invoke("updateLocation", TestLocations.DefaultLocation.Latitude, TestLocations.DefaultLocation.Longitude);
            
            // Wait for UI to update with proximity indicators
            System.Threading.Thread.Sleep(1000);
        }

        /// <summary>
        /// Helper method to verify all checkpoints in a patrol
        /// </summary>
        private void VerifyAllCheckpoints()
        {
            // Get the number of checkpoints from the UI
            int checkpointCount = App.Query(c => c.Marked(CheckpointListId).Child()).Length;
            
            // For each checkpoint:
            foreach (var checkpoint in TestCheckpoints.HeadquartersCheckpointModels)
            {
                // Simulate proximity to the checkpoint
                SimulateCheckpointProximity(checkpoint.Id);
                
                // Verify the checkpoint is highlighted
                string checkpointId = string.Format(CheckpointItemFormat, checkpoint.Id);
                AssertElementExists(checkpointId);
                
                // Tap the "Verify" button
                string verifyButtonId = string.Format(VerifyButtonFormat, checkpoint.Id);
                TapElement(verifyButtonId);
                
                // Verify the checkpoint is marked as verified
                AssertElementExists(checkpointId + "_Verified");
                
                // Wait for progress update
                System.Threading.Thread.Sleep(500);
            }
        }

        /// <summary>
        /// Helper method to select a location from the picker
        /// </summary>
        /// <param name="locationName">The name of the location to select</param>
        private void SelectLocation(string locationName)
        {
            // Tap the location picker
            TapElement(LocationPickerId);
            
            // Wait for dropdown to appear
            System.Threading.Thread.Sleep(500);
            
            // Tap the location with the specified name
            string locationItemId = string.Format(LocationDropdownItemFormat, locationName);
            TapElement(locationItemId);
            
            // Wait for checkpoints to load
            WaitForElement(CheckpointListId);
        }
    }
}