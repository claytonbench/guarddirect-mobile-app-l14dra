# src/test/EndToEnd/SecurityPatrol.E2ETests/Flows/PatrolVerificationE2ETests.cs
using System; // Version 8.0.0
using System.Collections.Generic; // Version 8.0.0
using System.Linq; // Version 8.0.0
using System.Threading.Tasks; // Version 8.0.0
using Xunit; // Version 2.4.2
using FluentAssertions; // Version 6.11.0
using SecurityPatrol.E2ETests.Setup;
using SecurityPatrol.Models;
using SecurityPatrol.TestCommon.Data;
using SecurityPatrol.TestCommon.Helpers;

namespace SecurityPatrol.E2ETests.Flows
{
    /// <summary>
    /// Contains end-to-end tests for the patrol verification flow in the Security Patrol application.
    /// </summary>
    [public]
    public class PatrolVerificationE2ETests : E2ETestBase
    {
        /// <summary>
        /// Initializes a new instance of the PatrolVerificationE2ETests class.
        /// </summary>
        public PatrolVerificationE2ETests()
        {
            // Call base constructor to initialize E2ETestBase
        }

        /// <summary>
        /// Tests the complete patrol verification flow from authentication to checkpoint verification and completion.
        /// </summary>
        [public]
        [async]
        [Fact]
        public async Task TestCompletePatrolVerificationFlow()
        {
            // Ensure network connectivity is available by calling TestEnvironmentSetup.ConfigureNetworkConnectivity(true)
            TestEnvironmentSetup.ConfigureNetworkConnectivity(true);

            // Execute the complete patrol flow by calling ExecuteCompletePatrolFlowAsync()
            bool result = await ExecuteCompletePatrolFlowAsync();

            // Assert that the result is true, indicating successful completion
            result.Should().BeTrue();

            // Get patrol status for the verified location
            var patrolStatus = await PatrolService.GetPatrolStatus(TestConstants.TestLocationId);

            // Assert that the patrol is marked as complete
            patrolStatus.IsComplete().Should().BeTrue();

            // Assert that the completion percentage is 100%
            patrolStatus.CalculateCompletionPercentage().Should().Be(100);
        }

        /// <summary>
        /// Tests verifying multiple checkpoints in a patrol and tracking progress correctly.
        /// </summary>
        [public]
        [async]
        [Fact]
        public async Task TestPatrolVerificationWithMultipleCheckpoints()
        {
            // Authenticate the user by calling AuthenticateAsync()
            bool authSuccess = await AuthenticateAsync();
            authSuccess.Should().BeTrue();

            // Clock in to start a shift by calling ClockInAsync()
            bool clockInSuccess = await ClockInAsync();
            clockInSuccess.Should().BeTrue();

            // Get available patrol locations from PatrolService
            var locations = await PatrolService.GetLocations();
            locations.Should().NotBeNullOrEmpty();

            // Select the first location
            var firstLocation = locations.First();

            // Get checkpoints for the selected location
            var checkpoints = await PatrolService.GetCheckpoints(firstLocation.Id);

            // Assert that there are multiple checkpoints available
            checkpoints.Should().NotBeNullOrEmpty();
            var checkpointList = checkpoints.ToList();
            checkpointList.Count.Should().BeGreaterThan(1);

            // Verify each checkpoint one by one
            double progressPerCheckpoint = 100.0 / checkpointList.Count;
            double currentProgress = 0;

            foreach (var checkpoint in checkpointList)
            {
                // Verify each checkpoint one by one
                bool verifySuccess = await PatrolService.VerifyCheckpoint(checkpoint.Id);
                verifySuccess.Should().BeTrue();

                // After each verification, get patrol status and check progress percentage
                var patrolStatus = await PatrolService.GetPatrolStatus(firstLocation.Id);

                // Assert that progress increases with each verification
                currentProgress += progressPerCheckpoint;
                patrolStatus.CalculateCompletionPercentage().Should().BeGreaterOrEqualTo(currentProgress - 0.01); // Allow for slight rounding errors
            }

            // After verifying all checkpoints, assert that patrol is complete
            var finalPatrolStatus = await PatrolService.GetPatrolStatus(firstLocation.Id);
            finalPatrolStatus.IsComplete().Should().BeTrue();

            // Clock out to end the shift
            bool clockOutSuccess = await ClockOutAsync();
            clockOutSuccess.Should().BeTrue();

            // Synchronize data with the backend
            bool syncSuccess = await SyncDataAsync();
            syncSuccess.Should().BeTrue();

            // Assert that all operations completed successfully
        }

        /// <summary>
        /// Tests that checkpoint verification requires proximity to the checkpoint location.
        /// </summary>
        [public]
        [async]
        [Fact]
        public async Task TestPatrolVerificationWithProximityCheck()
        {
            // Authenticate the user by calling AuthenticateAsync()
            bool authSuccess = await AuthenticateAsync();
            authSuccess.Should().BeTrue();

            // Clock in to start a shift by calling ClockInAsync()
            bool clockInSuccess = await ClockInAsync();
            clockInSuccess.Should().BeTrue();

            // Get available patrol locations from PatrolService
            var locations = await PatrolService.GetLocations();
            locations.Should().NotBeNullOrEmpty();

            // Select the first location
            var firstLocation = locations.First();

            // Get checkpoints for the selected location
            var checkpoints = await PatrolService.GetCheckpoints(firstLocation.Id);
            checkpoints.Should().NotBeNullOrEmpty();
            var firstCheckpoint = checkpoints.First();

            // Set current location far from checkpoint (outside proximity threshold)
            LocationService.SetupCurrentLocation(new LocationModel
            {
                Latitude = firstCheckpoint.Latitude + 0.1, // Far away
                Longitude = firstCheckpoint.Longitude + 0.1
            });

            // Attempt to verify checkpoint
            bool verifyFail = await PatrolService.VerifyCheckpoint(firstCheckpoint.Id);

            // Assert that verification fails due to proximity check
            verifyFail.Should().BeFalse();

            // Set current location near checkpoint (within proximity threshold)
            LocationService.SetupCurrentLocation(new LocationModel
            {
                Latitude = firstCheckpoint.Latitude, // Close
                Longitude = firstCheckpoint.Longitude
            });

            // Attempt to verify checkpoint again
            bool verifySuccess = await PatrolService.VerifyCheckpoint(firstCheckpoint.Id);

            // Assert that verification succeeds
            verifySuccess.Should().BeTrue();

            // Clock out to end the shift
            bool clockOutSuccess = await ClockOutAsync();
            clockOutSuccess.Should().BeTrue();

            // Synchronize data with the backend
            bool syncSuccess = await SyncDataAsync();
            syncSuccess.Should().BeTrue();
        }

        /// <summary>
        /// Tests that patrol verification works in offline mode and synchronizes correctly when back online.
        /// </summary>
        [public]
        [async]
        [Fact]
        public async Task TestPatrolVerificationOfflineMode()
        {
            // Authenticate the user by calling AuthenticateAsync()
            bool authSuccess = await AuthenticateAsync();
            authSuccess.Should().BeTrue();

            // Clock in to start a shift by calling ClockInAsync()
            bool clockInSuccess = await ClockInAsync();
            clockInSuccess.Should().BeTrue();

            // Get available patrol locations from PatrolService
            var locations = await PatrolService.GetLocations();
            locations.Should().NotBeNullOrEmpty();

            // Select the first location
            var firstLocation = locations.First();

            // Get checkpoints for the selected location
            var checkpoints = await PatrolService.GetCheckpoints(firstLocation.Id);
            checkpoints.Should().NotBeNullOrEmpty();
            var firstCheckpoint = checkpoints.First();

            // Configure network to offline mode by calling TestEnvironmentSetup.ConfigureNetworkConnectivity(false)
            TestEnvironmentSetup.ConfigureNetworkConnectivity(false);

            // Verify checkpoints while offline
            bool verifySuccess = await PatrolService.VerifyCheckpoint(firstCheckpoint.Id);

            // Assert that local verification succeeds
            verifySuccess.Should().BeTrue();

            // Attempt to synchronize data while offline
            bool syncFail = await SyncDataAsync();

            // Assert that synchronization is queued but not completed
            syncFail.Should().BeFalse();

            // Configure network back to online mode by calling TestEnvironmentSetup.ConfigureNetworkConnectivity(true)
            TestEnvironmentSetup.ConfigureNetworkConnectivity(true);

            // Synchronize data with the backend
            bool syncSuccess = await SyncDataAsync();

            // Assert that queued verifications are successfully synchronized
            syncSuccess.Should().BeTrue();

            // Clock out to end the shift
            bool clockOutSuccess = await ClockOutAsync();
            clockOutSuccess.Should().BeTrue();
        }

        /// <summary>
        /// Tests that attempting to verify a non-existent checkpoint fails appropriately.
        /// </summary>
        [public]
        [async]
        [Fact]
        public async Task TestPatrolVerificationWithInvalidCheckpoint()
        {
            // Authenticate the user by calling AuthenticateAsync()
            bool authSuccess = await AuthenticateAsync();
            authSuccess.Should().BeTrue();

            // Clock in to start a shift by calling ClockInAsync()
            bool clockInSuccess = await ClockInAsync();
            clockInSuccess.Should().BeTrue();

            // Attempt to verify a checkpoint with an invalid ID (e.g., -1)
            bool verifyFail = await PatrolService.VerifyCheckpoint(-1);

            // Assert that verification fails with appropriate error
            verifyFail.Should().BeFalse();

            // Clock out to end the shift
            bool clockOutSuccess = await ClockOutAsync();
            clockOutSuccess.Should().BeTrue();
        }

        /// <summary>
        /// Tests that patrol verification requires the user to be clocked in.
        /// </summary>
        [public]
        [async]
        [Fact]
        public async Task TestPatrolVerificationWithoutClockIn()
        {
            // Authenticate the user by calling AuthenticateAsync()
            bool authSuccess = await AuthenticateAsync();
            authSuccess.Should().BeTrue();

            // Get available patrol locations from PatrolService
            var locations = await PatrolService.GetLocations();
            locations.Should().NotBeNullOrEmpty();

            // Select the first location
            var firstLocation = locations.First();

            // Get checkpoints for the selected location
            var checkpoints = await PatrolService.GetCheckpoints(firstLocation.Id);
            checkpoints.Should().NotBeNullOrEmpty();
            var firstCheckpoint = checkpoints.First();

            // Attempt to verify checkpoint without clocking in
            bool verifyFail = await PatrolService.VerifyCheckpoint(firstCheckpoint.Id);

            // Assert that verification fails with appropriate error about clock status
            verifyFail.Should().BeFalse();

            // Clock in to start a shift
            bool clockInSuccess = await ClockInAsync();
            clockInSuccess.Should().BeTrue();

            // Attempt to verify checkpoint again
            bool verifySuccess = await PatrolService.VerifyCheckpoint(firstCheckpoint.Id);

            // Assert that verification succeeds
            verifySuccess.Should().BeTrue();

            // Clock out to end the shift
            bool clockOutSuccess = await ClockOutAsync();
            clockOutSuccess.Should().BeTrue();
        }

        /// <summary>
        /// Tests that attempting to verify an already verified checkpoint is handled appropriately.
        /// </summary>
        [public]
        [async]
        [Fact]
        public async Task TestReVerifyingAlreadyVerifiedCheckpoint()
        {
            // Authenticate the user by calling AuthenticateAsync()
            bool authSuccess = await AuthenticateAsync();
            authSuccess.Should().BeTrue();

            // Clock in to start a shift by calling ClockInAsync()
            bool clockInSuccess = await ClockInAsync();
            clockInSuccess.Should().BeTrue();

            // Get available patrol locations from PatrolService
            var locations = await PatrolService.GetLocations();
            locations.Should().NotBeNullOrEmpty();

            // Select the first location
            var firstLocation = locations.First();

            // Get checkpoints for the selected location
            var checkpoints = await PatrolService.GetCheckpoints(firstLocation.Id);
            checkpoints.Should().NotBeNullOrEmpty();
            var firstCheckpoint = checkpoints.First();

            // Verify a checkpoint
            bool verifySuccess = await PatrolService.VerifyCheckpoint(firstCheckpoint.Id);

            // Assert that verification succeeds
            verifySuccess.Should().BeTrue();

            // Attempt to verify the same checkpoint again
            bool reVerifyResult = await PatrolService.VerifyCheckpoint(firstCheckpoint.Id);

            // Assert that re-verification is handled appropriately (either prevented or idempotent)
            reVerifyResult.Should().BeTrue(); // Assuming idempotent behavior

            // Clock out to end the shift
            bool clockOutSuccess = await ClockOutAsync();
            clockOutSuccess.Should().BeTrue();
        }
    }
}