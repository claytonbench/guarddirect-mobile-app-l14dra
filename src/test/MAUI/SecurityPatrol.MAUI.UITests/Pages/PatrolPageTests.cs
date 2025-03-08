using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Xamarin.UITest;
using SecurityPatrol.MAUI.UITests.Setup;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Data;

namespace SecurityPatrol.MAUI.UITests.Pages
{
    /// <summary>
    /// Contains UI tests for the Patrol page of the Security Patrol application
    /// </summary>
    public class PatrolPageTests : UITestBase
    {
        /// <summary>
        /// Initializes a new instance of the PatrolPageTests class
        /// </summary>
        public PatrolPageTests() : base()
        {
            // Call base constructor to initialize UITestBase
        }

        /// <summary>
        /// Sets up the mock API endpoints for patrol testing
        /// </summary>
        private void SetupPatrolEndpoints()
        {
            // Set up location endpoint to return test locations
            MockApiServer.SetupEndpoint("/patrol/locations", new[] {
                TestLocations.HeadquartersLocation,
                TestLocations.WarehouseLocation
            });
            
            // Set up checkpoint endpoint to return test checkpoints for headquarters location
            MockApiServer.SetupEndpoint("/patrol/checkpoints", TestCheckpoints.HeadquartersCheckpointModels);
            
            // Set up checkpoint verification endpoint to return success
            MockApiServer.SetupEndpoint("/patrol/verify", new { status = "success" });

            // Set up patrol status endpoint to return patrol status
            MockApiServer.SetupEndpoint("/patrol/status", new { 
                locationId = TestLocations.HeadquartersLocation.Id,
                totalCheckpoints = TestCheckpoints.HeadquartersCheckpoints.Count,
                verifiedCheckpoints = 0
            });
        }

        /// <summary>
        /// Sets up the test environment before each test
        /// </summary>
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            
            // Login to the application
            Login();
            
            // Set up mock API endpoints for patrol operations
            SetupPatrolEndpoints();
            
            // Navigate to the patrol page
            NavigateToPage("Patrol");
        }

        /// <summary>
        /// Cleans up the test environment after each test
        /// </summary>
        [TearDown]
        public override void TearDown()
        {
            // Take a screenshot of the final state
            TakeScreenshot("PatrolPageFinal");
            
            base.TearDown();
        }

        /// <summary>
        /// Tests that the Patrol page loads correctly with all expected elements
        /// </summary>
        [Test]
        [Order(1)]
        public void TestPatrolPageLoads()
        {
            // Verify page title is "Patrol"
            AssertElementExists("PatrolPageTitle");
            AssertElementText("PatrolPageTitle", "Patrol");
            
            // Verify location picker is displayed
            AssertElementExists("LocationPicker");
            
            // Verify map view is displayed
            AssertElementExists("PatrolMapView");
            
            // Verify "Start Patrol" button is displayed but disabled (no location selected)
            AssertElementExists("StartPatrolButton");
            
            // Verify checkpoint list is not displayed initially
            AssertElementDoesNotExist("CheckpointsList");
        }

        /// <summary>
        /// Tests the location selection functionality
        /// </summary>
        [Test]
        [Order(2)]
        public void TestLocationSelection()
        {
            // Tap on the location picker
            TapElement("LocationPicker");
            
            // Select headquarters location from dropdown
            TapElement("LocationItem_" + TestLocations.HeadquartersLocation.Id);
            
            // Verify "Start Patrol" button is enabled
            AssertElementExists("StartPatrolButton");
            
            // Verify checkpoint list is displayed
            AssertElementExists("CheckpointsList");
            
            // Verify map is centered on the selected location
            AssertElementExists("MapCentered");
            
            // Verify checkpoints are displayed on the map
            AssertElementExists("CheckpointsOnMap");
        }

        /// <summary>
        /// Tests starting a patrol for a selected location
        /// </summary>
        [Test]
        [Order(3)]
        public void TestStartPatrol()
        {
            // Select headquarters location from dropdown
            TapElement("LocationPicker");
            TapElement("LocationItem_" + TestLocations.HeadquartersLocation.Id);
            
            // Tap the "Start Patrol" button
            TapElement("StartPatrolButton");
            
            // Verify "Start Patrol" button is replaced with "End Patrol" button
            AssertElementDoesNotExist("StartPatrolButton");
            AssertElementExists("EndPatrolButton");
            
            // Verify patrol progress section is displayed
            AssertElementExists("PatrolProgressSection");
            
            // Verify progress bar is displayed with 0% progress
            AssertElementExists("PatrolProgressBar");
            
            // Verify status message indicates patrol is active
            AssertElementExists("PatrolStatusMessage");
            AssertElementText("PatrolStatusMessage", "Patrol in progress");
            
            // Verify location picker is disabled during active patrol
            AssertElementExists("LocationPicker_Disabled");
        }

        /// <summary>
        /// Tests the checkpoint verification functionality
        /// </summary>
        [Test]
        [Order(4)]
        public void TestCheckpointVerification()
        {
            // Select headquarters location from dropdown
            TapElement("LocationPicker");
            TapElement("LocationItem_" + TestLocations.HeadquartersLocation.Id);
            
            // Start a patrol
            TapElement("StartPatrolButton");
            
            // Simulate proximity to a checkpoint
            SimulateCheckpointProximity(TestCheckpoints.HeadquartersCheckpoints[0].Id);
            
            // Verify checkpoint is highlighted in the list
            AssertElementExists("CheckpointItem_" + TestCheckpoints.HeadquartersCheckpoints[0].Id + "_Highlighted");
            
            // Verify "Verify" button is enabled for the checkpoint
            AssertElementExists("VerifyButton_" + TestCheckpoints.HeadquartersCheckpoints[0].Id);
            
            // Tap the "Verify" button
            TapElement("VerifyButton_" + TestCheckpoints.HeadquartersCheckpoints[0].Id);
            
            // Verify checkpoint is marked as verified (green indicator)
            AssertElementExists("CheckpointItem_" + TestCheckpoints.HeadquartersCheckpoints[0].Id + "_Verified");
            
            // Verify patrol progress is updated
            AssertElementExists("PatrolProgressBar");
            
            // Verify progress bar reflects the verification
            AssertElementExists("PatrolProgressUpdated");
        }

        /// <summary>
        /// Tests ending an active patrol
        /// </summary>
        [Test]
        [Order(5)]
        public void TestEndPatrol()
        {
            // Select headquarters location from dropdown
            TapElement("LocationPicker");
            TapElement("LocationItem_" + TestLocations.HeadquartersLocation.Id);
            
            // Start a patrol
            TapElement("StartPatrolButton");
            
            // Verify patrol is active
            AssertElementExists("EndPatrolButton");
            AssertElementExists("PatrolProgressSection");
            
            // Tap the "End Patrol" button
            TapElement("EndPatrolButton");
            
            // Verify "End Patrol" button is replaced with "Start Patrol" button
            AssertElementDoesNotExist("EndPatrolButton");
            AssertElementExists("StartPatrolButton");
            
            // Verify patrol progress section is hidden
            AssertElementDoesNotExist("PatrolProgressSection");
            
            // Verify location picker is enabled again
            AssertElementDoesNotExist("LocationPicker_Disabled");
            
            // Verify status message is reset
            AssertElementDoesNotExist("PatrolStatusMessage");
        }

        /// <summary>
        /// Tests the "View List" button to navigate to the checkpoint list page
        /// </summary>
        [Test]
        [Order(6)]
        public void TestViewCheckpointList()
        {
            // Select headquarters location from dropdown
            TapElement("LocationPicker");
            TapElement("LocationItem_" + TestLocations.HeadquartersLocation.Id);
            
            // Verify "View List" button is displayed
            AssertElementExists("ViewListButton");
            
            // Tap the "View List" button
            TapElement("ViewListButton");
            
            // Verify navigation to checkpoint list page
            AssertElementExists("CheckpointListPage");
            
            // Verify checkpoint list page displays checkpoints for the selected location
            AssertElementExists("CheckpointDetailsList");
            
            // Navigate back to patrol page
            PressBack();
            
            // Verify return to patrol page with selected location maintained
            AssertElementExists("PatrolPage");
            AssertElementExists("LocationSelected_" + TestLocations.HeadquartersLocation.Id);
        }

        /// <summary>
        /// Tests the patrol completion workflow
        /// </summary>
        [Test]
        [Order(7)]
        public void TestPatrolCompletion()
        {
            // Select headquarters location from dropdown
            TapElement("LocationPicker");
            TapElement("LocationItem_" + TestLocations.HeadquartersLocation.Id);
            
            // Start a patrol
            TapElement("StartPatrolButton");
            
            // Verify all checkpoints one by one
            foreach (var checkpoint in TestCheckpoints.HeadquartersCheckpoints)
            {
                SimulateCheckpointProximity(checkpoint.Id);
                WaitForElement("VerifyButton_" + checkpoint.Id);
                TapElement("VerifyButton_" + checkpoint.Id);
                WaitForElement("CheckpointItem_" + checkpoint.Id + "_Verified");
            }
            
            // Verify progress reaches 100%
            AssertElementExists("PatrolProgressComplete");
            
            // Verify completion message is displayed
            AssertElementExists("PatrolCompletionMessage");
            AssertElementText("PatrolCompletionMessage", "Patrol complete!");
            
            // Verify patrol summary or report option is displayed
            AssertElementExists("PatrolSummaryButton");
            
            // End the patrol
            TapElement("EndPatrolButton");
            
            // Verify patrol is reset for new start
            AssertElementExists("StartPatrolButton");
            AssertElementDoesNotExist("PatrolProgressSection");
        }

        /// <summary>
        /// Tests selecting checkpoints from the list
        /// </summary>
        [Test]
        [Order(8)]
        public void TestCheckpointSelection()
        {
            // Select headquarters location from dropdown
            TapElement("LocationPicker");
            TapElement("LocationItem_" + TestLocations.HeadquartersLocation.Id);
            
            // Verify checkpoint list is displayed
            AssertElementExists("CheckpointsList");
            
            // Tap on a checkpoint in the list
            TapElement("CheckpointItem_" + TestCheckpoints.HeadquartersCheckpoints[0].Id);
            
            // Verify checkpoint is highlighted in the list
            AssertElementExists("CheckpointItem_" + TestCheckpoints.HeadquartersCheckpoints[0].Id + "_Selected");
            
            // Verify map centers on the selected checkpoint
            AssertElementExists("MapCenteredOnCheckpoint_" + TestCheckpoints.HeadquartersCheckpoints[0].Id);
            
            // Tap on a different checkpoint
            TapElement("CheckpointItem_" + TestCheckpoints.HeadquartersCheckpoints[1].Id);
            
            // Verify new checkpoint is highlighted and previous is not
            AssertElementExists("CheckpointItem_" + TestCheckpoints.HeadquartersCheckpoints[1].Id + "_Selected");
            AssertElementDoesNotExist("CheckpointItem_" + TestCheckpoints.HeadquartersCheckpoints[0].Id + "_Selected");
            
            // Verify map centers on the newly selected checkpoint
            AssertElementExists("MapCenteredOnCheckpoint_" + TestCheckpoints.HeadquartersCheckpoints[1].Id);
        }

        /// <summary>
        /// Helper method to simulate proximity to a checkpoint
        /// </summary>
        /// <param name="checkpointId">The ID of the checkpoint to simulate proximity to</param>
        private void SimulateCheckpointProximity(int checkpointId)
        {
            // Set up mock API to simulate proximity event for the specified checkpoint
            MockApiServer.SetupEndpoint("/checkpoint/proximity", new {
                checkpointId = checkpointId,
                inProximity = true,
                distance = 15.0 // 15 meters, within the 50 feet threshold
            });
            
            // Trigger location update to process the proximity event
            App.Invoke("updateLocation");
            
            // Wait for UI to update with proximity indicators
            WaitForElement("CheckpointProximityIndicator_" + checkpointId);
        }
    }
}