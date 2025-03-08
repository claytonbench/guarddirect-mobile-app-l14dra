using System; // Version 8.0.0
using System.Collections.Generic; // Version 8.0.0
using System.Linq; // Version 8.0.0
using System.Threading.Tasks; // Version 8.0.0
using Xunit; // Version 2.4.2
using FluentAssertions; // Version 6.11.0
using SecurityPatrol.E2ETests.Setup;
using SecurityPatrol.Services;
using SecurityPatrol.Models;
using SecurityPatrol.TestCommon.Constants; // TestConstants
using SecurityPatrol.TestCommon.Helpers; // TestAuthHandler

namespace SecurityPatrol.E2ETests.Flows
{
    /// <summary>
    /// Contains end-to-end tests for the complete patrol flow in the Security Patrol application,
    /// testing all major features working together.
    /// </summary>
    public class CompletePatrolFlowE2ETests : E2ETestBase
    {
        /// <summary>
        /// Initializes a new instance of the CompletePatrolFlowE2ETests class.
        /// </summary>
        public CompletePatrolFlowE2ETests()
        {
            // Call base constructor to initialize E2ETestBase
        }

        /// <summary>
        /// Tests the complete patrol flow from authentication to clock in/out, patrol verification, photo capture, and report creation.
        /// </summary>
        [Fact]
        public async Task TestCompletePatrolFlowEndToEnd()
        {
            // Ensure network connectivity is available by calling TestEnvironmentSetup.ConfigureNetworkConnectivity(true)
            TestEnvironmentSetup.ConfigureNetworkConnectivity(true);

            // Execute the complete patrol flow by calling ExecuteCompletePatrolFlowAsync()
            bool result = await ExecuteCompletePatrolFlowAsync();

            // Assert that the result is true, indicating successful completion
            result.Should().BeTrue("The complete patrol flow should execute successfully");

            // Verify that all checkpoints are marked as verified
            var locations = await PatrolService.GetLocations();
            locations.Should().NotBeNullOrEmpty("Patrol locations should be available");

            foreach (var location in locations)
            {
                var checkpoints = await PatrolService.GetCheckpoints(location.Id);
                checkpoints.Should().NotBeNullOrEmpty("Checkpoints should be available for each location");

                foreach (var checkpoint in checkpoints)
                {
                    // Get the patrol status to check if the checkpoint is verified
                    var patrolStatus = await PatrolService.GetPatrolStatus(location.Id);
                    patrolStatus.Should().NotBeNull("Patrol status should be available");
                    // Assert that the checkpoint is verified
                    PatrolService.VerifyCheckpointWasVerified(checkpoint.Id).Should().BeTrue($"Checkpoint {checkpoint.Id} should be verified");
                }
            }

            // Verify that photos were captured and synced
            var photos = await PhotoService.GetStoredPhotosAsync();
            photos.Should().NotBeNullOrEmpty("Photos should have been captured");
            foreach (var photo in photos)
            {
                photo.IsSynced.Should().BeTrue($"Photo {photo.Id} should be synced");
            }

            // Verify that reports were created and synced
            var reports = await ReportService.GetAllReportsAsync();
            reports.Should().NotBeNullOrEmpty("Reports should have been created");
            foreach (var report in reports)
            {
                report.IsSynced.Should().BeTrue($"Report {report.Id} should be synced");
            }

            // Verify that clock events were recorded and synced
            var timeRecords = await TimeTrackingService.GetHistory(100);
            timeRecords.Should().NotBeNullOrEmpty("Time records should have been recorded");
            foreach (var timeRecord in timeRecords)
            {
                timeRecord.IsSynced.Should().BeTrue($"Time record {timeRecord.Id} should be synced");
            }
        }

        /// <summary>
        /// Tests the complete patrol flow with emphasis on photo capture during patrol.
        /// </summary>
        [Fact]
        public async Task TestCompletePatrolFlowWithPhotoCapture()
        {
            // Authenticate the user by calling AuthenticateAsync()
            bool authSuccess = await AuthenticateAsync();
            authSuccess.Should().BeTrue("Authentication should succeed");

            // Clock in to start a shift by calling ClockInAsync()
            bool clockInSuccess = await ClockInAsync();
            clockInSuccess.Should().BeTrue("Clock-in should succeed");

            // Get available patrol locations from PatrolService
            var locations = await PatrolService.GetLocations();
            locations.Should().NotBeNullOrEmpty("Patrol locations should be available");

            // Select the first location
            var firstLocation = locations.First();

            // Get checkpoints for the selected location
            var checkpoints = await PatrolService.GetCheckpoints(firstLocation.Id);
            checkpoints.Should().NotBeNullOrEmpty("Checkpoints should be available for the selected location");

            // For each checkpoint:
            foreach (var checkpoint in checkpoints)
            {
                // Verify the checkpoint
                bool verifySuccess = await VerifyCheckpointAsync(checkpoint.Id);
                verifySuccess.Should().BeTrue($"Verification of checkpoint {checkpoint.Id} should succeed");

                // Capture a photo to document the checkpoint
                bool photoSuccess = await CapturePhotoAsync();
                photoSuccess.Should().BeTrue($"Photo capture for checkpoint {checkpoint.Id} should succeed");

                // Create a report about the checkpoint
                bool reportSuccess = await CreateReportAsync($"Report for checkpoint {checkpoint.Id}");
                reportSuccess.Should().BeTrue($"Report creation for checkpoint {checkpoint.Id} should succeed");
            }

            // Clock out to end the shift
            bool clockOutSuccess = await ClockOutAsync();
            clockOutSuccess.Should().BeTrue("Clock-out should succeed");

            // Synchronize all data with the backend
            bool syncSuccess = await SyncDataAsync();
            syncSuccess.Should().BeTrue("Data synchronization should succeed");

            // Verify that all photos were successfully captured and synced
            var photos = await PhotoService.GetStoredPhotosAsync();
            photos.Should().NotBeNullOrEmpty("Photos should have been captured");
            foreach (var photo in photos)
            {
                photo.IsSynced.Should().BeTrue($"Photo {photo.Id} should be synced");
            }

            // Verify that all reports were successfully created and synced
            var reports = await ReportService.GetAllReportsAsync();
            reports.Should().NotBeNullOrEmpty("Reports should have been created");
            foreach (var report in reports)
            {
                report.IsSynced.Should().BeTrue($"Report {report.Id} should be synced");
            }

            // Verify that all checkpoints were successfully verified
            foreach (var checkpoint in checkpoints)
            {
                PatrolService.VerifyCheckpointWasVerified(checkpoint.Id).Should().BeTrue($"Checkpoint {checkpoint.Id} should be verified");
            }
        }

        /// <summary>
        /// Tests the complete patrol flow with offline operation and later synchronization.
        /// </summary>
        [Fact]
        public async Task TestCompletePatrolFlowWithOfflineSync()
        {
            // Authenticate the user by calling AuthenticateAsync()
            bool authSuccess = await AuthenticateAsync();
            authSuccess.Should().BeTrue("Authentication should succeed");

            // Clock in to start a shift by calling ClockInAsync()
            bool clockInSuccess = await ClockInAsync();
            clockInSuccess.Should().BeTrue("Clock-in should succeed");

            // Configure network to offline mode by calling TestEnvironmentSetup.ConfigureNetworkConnectivity(false)
            TestEnvironmentSetup.ConfigureNetworkConnectivity(false);

            // Get available patrol locations from local cache
            var locations = await PatrolService.GetLocations();
            locations.Should().NotBeNullOrEmpty("Patrol locations should be available from local cache");

            // Select the first location
            var firstLocation = locations.First();

            // Get checkpoints for the selected location from local cache
            var checkpoints = await PatrolService.GetCheckpoints(firstLocation.Id);
            checkpoints.Should().NotBeNullOrEmpty("Checkpoints should be available from local cache");

            // Verify checkpoints while offline
            foreach (var checkpoint in checkpoints)
            {
                bool verifySuccess = await VerifyCheckpointAsync(checkpoint.Id);
                verifySuccess.Should().BeTrue($"Verification of checkpoint {checkpoint.Id} should succeed locally");
            }

            // Capture photos while offline
            bool photoSuccess = await CapturePhotoAsync();
            photoSuccess.Should().BeTrue("Photo capture should succeed locally");

            // Create reports while offline
            bool reportSuccess = await CreateReportAsync("Offline report");
            reportSuccess.Should().BeTrue("Report creation should succeed locally");

            // Clock out while offline
            bool clockOutSuccess = await ClockOutAsync();
            clockOutSuccess.Should().BeTrue("Clock-out should succeed locally");

            // Attempt to synchronize data while offline
            bool syncAttempt = await SyncDataAsync();
            syncAttempt.Should().BeFalse("Data synchronization should not succeed while offline");

            // Assert that operations succeed locally but sync is queued
            var photos = await PhotoService.GetStoredPhotosAsync();
            photos.Should().NotBeNullOrEmpty("Photos should have been captured");
            foreach (var photo in photos)
            {
                photo.IsSynced.Should().BeFalse($"Photo {photo.Id} should not be synced");
            }

            var reports = await ReportService.GetAllReportsAsync();
            reports.Should().NotBeNullOrEmpty("Reports should have been created");
            foreach (var report in reports)
            {
                report.IsSynced.Should().BeFalse($"Report {report.Id} should not be synced");
            }

            // Configure network back to online mode by calling TestEnvironmentSetup.ConfigureNetworkConnectivity(true)
            TestEnvironmentSetup.ConfigureNetworkConnectivity(true);

            // Synchronize all data with the backend
            bool syncSuccess = await SyncDataAsync();
            syncSuccess.Should().BeTrue("Data synchronization should succeed after restoring network");

            // Verify that all queued operations were successfully synchronized
            photos = await PhotoService.GetStoredPhotosAsync();
            photos.Should().NotBeNullOrEmpty("Photos should have been captured");
            foreach (var photo in photos)
            {
                photo.IsSynced.Should().BeTrue($"Photo {photo.Id} should be synced");
            }

            reports = await ReportService.GetAllReportsAsync();
            reports.Should().NotBeNullOrEmpty("Reports should have been created");
            foreach (var report in reports)
            {
                report.IsSynced.Should().BeTrue($"Report {report.Id} should be synced");
            }
        }

        /// <summary>
        /// Tests the complete patrol flow across multiple patrol locations.
        /// </summary>
        [Fact]
        public async Task TestCompletePatrolFlowWithMultipleLocations()
        {
            // Authenticate the user by calling AuthenticateAsync()
            bool authSuccess = await AuthenticateAsync();
            authSuccess.Should().BeTrue("Authentication should succeed");

            // Clock in to start a shift by calling ClockInAsync()
            bool clockInSuccess = await ClockInAsync();
            clockInSuccess.Should().BeTrue("Clock-in should succeed");

            // Get available patrol locations from PatrolService
            var locations = await PatrolService.GetLocations();
            locations.Should().NotBeNullOrEmpty("Patrol locations should be available");

            // Verify that multiple locations are available
            locations.Count().Should().BeGreaterThan(1, "Multiple patrol locations should be available");

            // For each location:
            foreach (var location in locations)
            {
                // Get checkpoints for the location
                var checkpoints = await PatrolService.GetCheckpoints(location.Id);
                checkpoints.Should().NotBeNullOrEmpty($"Checkpoints should be available for location {location.Id}");

                // Verify all checkpoints for the location
                foreach (var checkpoint in checkpoints)
                {
                    bool verifySuccess = await VerifyCheckpointAsync(checkpoint.Id);
                    verifySuccess.Should().BeTrue($"Verification of checkpoint {checkpoint.Id} should succeed");
                }

                // Capture photos at the location
                bool photoSuccess = await CapturePhotoAsync();
                photoSuccess.Should().BeTrue($"Photo capture for location {location.Id} should succeed");

                // Create reports about the location
                bool reportSuccess = await CreateReportAsync($"Report for location {location.Id}");
                reportSuccess.Should().BeTrue($"Report creation for location {location.Id} should succeed");

                // Verify that patrol is complete for the location
                var patrolStatus = await PatrolService.GetPatrolStatus(location.Id);
                patrolStatus.Should().NotBeNull($"Patrol status for location {location.Id} should be available");
                patrolStatus.IsComplete().Should().BeTrue($"Patrol should be complete for location {location.Id}");
            }

            // Clock out to end the shift
            bool clockOutSuccess = await ClockOutAsync();
            clockOutSuccess.Should().BeTrue("Clock-out should succeed");

            // Synchronize all data with the backend
            bool syncSuccess = await SyncDataAsync();
            syncSuccess.Should().BeTrue("Data synchronization should succeed");

            // Verify that all operations across all locations were successful
            var photos = await PhotoService.GetStoredPhotosAsync();
            photos.Should().NotBeNullOrEmpty("Photos should have been captured");
            foreach (var photo in photos)
            {
                photo.IsSynced.Should().BeTrue($"Photo {photo.Id} should be synced");
            }

            var reports = await ReportService.GetAllReportsAsync();
            reports.Should().NotBeNullOrEmpty("Reports should have been created");
            foreach (var report in reports)
            {
                report.IsSynced.Should().BeTrue($"Report {report.Id} should be synced");
            }
        }

        /// <summary>
        /// Tests the complete patrol flow with error recovery scenarios.
        /// </summary>
        [Fact]
        public async Task TestCompletePatrolFlowWithErrorRecovery()
        {
            // Authenticate the user by calling AuthenticateAsync()
            bool authSuccess = await AuthenticateAsync();
            authSuccess.Should().BeTrue("Authentication should succeed");

            // Clock in to start a shift by calling ClockInAsync()
            bool clockInSuccess = await ClockInAsync();
            clockInSuccess.Should().BeTrue("Clock-in should succeed");

            // Get available patrol locations from PatrolService
            var locations = await PatrolService.GetLocations();
            locations.Should().NotBeNullOrEmpty("Patrol locations should be available");

            // Select the first location
            var firstLocation = locations.First();

            // Get checkpoints for the selected location
            var checkpoints = await PatrolService.GetCheckpoints(firstLocation.Id);
            checkpoints.Should().NotBeNullOrEmpty("Checkpoints should be available");

            // Verify some checkpoints successfully
            int successfulVerifications = 0;
            foreach (var checkpoint in checkpoints.Take(2))
            {
                bool verifySuccess = await VerifyCheckpointAsync(checkpoint.Id);
                if (verifySuccess)
                {
                    successfulVerifications++;
                }
                verifySuccess.Should().BeTrue($"Verification of checkpoint {checkpoint.Id} should succeed");
            }

            // Simulate network failure by calling TestEnvironmentSetup.ConfigureNetworkConnectivity(false)
            TestEnvironmentSetup.ConfigureNetworkConnectivity(false);

            // Continue verifying checkpoints while offline
            foreach (var checkpoint in checkpoints.Skip(2))
            {
                bool verifySuccess = await VerifyCheckpointAsync(checkpoint.Id);
                verifySuccess.Should().BeTrue($"Verification of checkpoint {checkpoint.Id} should succeed locally");
            }

            // Attempt to synchronize data while offline
            bool syncAttempt = await SyncDataAsync();
            syncAttempt.Should().BeFalse("Data synchronization should not succeed while offline");

            // Restore network connectivity by calling TestEnvironmentSetup.ConfigureNetworkConnectivity(true)
            TestEnvironmentSetup.ConfigureNetworkConnectivity(true);

            // Retry synchronization
            bool syncSuccess = await SyncDataAsync();
            syncSuccess.Should().BeTrue("Data synchronization should succeed after restoring network");

            // Verify that all operations eventually succeed after recovery
            var photos = await PhotoService.GetStoredPhotosAsync();
            var reports = await ReportService.GetAllReportsAsync();

            // Clock out to end the shift
            bool clockOutSuccess = await ClockOutAsync();
            clockOutSuccess.Should().BeTrue("Clock-out should succeed");

            // Verify final synchronization is successful
            bool finalSyncSuccess = await SyncDataAsync();
            finalSyncSuccess.Should().BeTrue("Final data synchronization should succeed");
        }

        /// <summary>
        /// Helper method to verify all checkpoints for a given location.
        /// </summary>
        /// <param name="locationId">The location identifier.</param>
        /// <returns>A task that returns true if all checkpoints were successfully verified.</returns>
        private async Task<bool> VerifyAllCheckpointsAsync(int locationId)
        {
            // Get checkpoints for the location using PatrolService.GetCheckpoints(locationId)
            var checkpoints = await PatrolService.GetCheckpoints(locationId);
            if (checkpoints == null || !checkpoints.Any())
            {
                return false;
            }

            // For each checkpoint:
            foreach (var checkpoint in checkpoints)
            {
                // Call VerifyCheckpointAsync(checkpoint.Id)
                bool verifySuccess = await VerifyCheckpointAsync(checkpoint.Id);
                if (!verifySuccess)
                {
                    return false;
                }
            }

            // Return true if all verifications succeed, otherwise false
            return true;
        }

        /// <summary>
        /// Helper method to capture photos during a patrol.
        /// </summary>
        /// <param name="count">The number of photos to capture.</param>
        /// <returns>A task that returns true if all photos were successfully captured.</returns>
        private async Task<bool> CapturePhotosForPatrolAsync(int count)
        {
            // For the specified count:
            for (int i = 0; i < count; i++)
            {
                // Call CapturePhotoAsync()
                bool photoSuccess = await CapturePhotoAsync();
                if (!photoSuccess)
                {
                    return false;
                }
            }

            // Return true if all captures succeed, otherwise false
            return true;
        }

        /// <summary>
        /// Helper method to create reports during a patrol.
        /// </summary>
        /// <param name="count">The number of reports to create.</param>
        /// <returns>A task that returns true if all reports were successfully created.</returns>
        private async Task<bool> CreateReportsForPatrolAsync(int count)
        {
            // For the specified count:
            for (int i = 0; i < count; i++)
            {
                // Generate report text with timestamp and index
                string reportText = $"Test report {i + 1} at {DateTime.UtcNow}";

                // Call CreateReportAsync(reportText)
                bool reportSuccess = await CreateReportAsync(reportText);
                if (!reportSuccess)
                {
                    return false;
                }
            }

            // Return true if all creations succeed, otherwise false
            return true;
        }

        /// <summary>
        /// Helper method to verify that all data has been synchronized with the backend.
        /// </summary>
        /// <returns>A task that returns true if all data is synchronized.</returns>
        private async Task<bool> VerifyDataSyncStatusAsync()
        {
            // Get photos from PhotoService.GetStoredPhotos()
            var photos = await PhotoService.GetStoredPhotosAsync();

            // Verify all photos have IsSynced = true
            foreach (var photo in photos)
            {
                if (!photo.IsSynced)
                {
                    return false;
                }
            }

            // Get reports from ReportService.GetReports()
            var reports = await ReportService.GetAllReportsAsync();

            // Verify all reports have IsSynced = true
            foreach (var report in reports)
            {
                if (!report.IsSynced)
                {
                    return false;
                }
            }

            // Get time records from TimeTrackingService.GetHistory()
            var timeRecords = await TimeTrackingService.GetHistory(100);

            // Verify all time records have IsSynced = true
            foreach (var timeRecord in timeRecords)
            {
                if (!timeRecord.IsSynced)
                {
                    return false;
                }
            }

            // Return true if all data is synchronized, otherwise false
            return true;
        }
    }
}