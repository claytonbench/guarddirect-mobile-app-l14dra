using System; // Version 8.0.0
using System.Threading.Tasks; // Version 8.0.0
using System.Collections.Generic; // Version 8.0.0
using System.Linq; // Version 8.0.0
using Xunit; // Version 2.4.2
using FluentAssertions; // Version 6.11.0
using SecurityPatrol.E2ETests.Setup;
using SecurityPatrol.Services;
using SecurityPatrol.TestCommon.Data; // TestTimeRecords
using SecurityPatrol.Models; // TimeRecordModel
using SecurityPatrol.Models; // ClockStatus

namespace SecurityPatrol.E2ETests.Flows
{
    /// <summary>
    /// End-to-end tests for the time tracking functionality in the Security Patrol application.
    /// </summary>
    [Collection("E2E Tests")]
    public class TimeTrackingE2ETests : E2ETestBase
    {
        /// <summary>
        /// Default constructor for TimeTrackingE2ETests
        /// </summary>
        public TimeTrackingE2ETests()
        {
        }

        /// <summary>
        /// Tests the clock in functionality to ensure it correctly records a clock in event.
        /// </summary>
        [Fact]
        public async Task ClockInTest()
        {
            // Authenticate the user by calling AuthenticateAsync()
            bool authSuccess = await AuthenticateAsync();
            authSuccess.Should().BeTrue("Authentication should succeed");

            // Get the current clock status before clock in
            ClockStatus initialStatus = await TimeTrackingService.GetCurrentStatus();

            // Verify that the user is not already clocked in
            initialStatus.IsClocked.Should().BeFalse("User should not be clocked in initially");

            // Call TimeTrackingService.ClockIn() to perform clock in operation
            TimeRecordModel clockInRecord = await TimeTrackingService.ClockIn();

            // Get the updated clock status after clock in
            ClockStatus updatedStatus = await TimeTrackingService.GetCurrentStatus();

            // Verify that the user is now clocked in
            updatedStatus.IsClocked.Should().BeTrue("User should be clocked in after clock in");

            // Verify that LastClockInTime is set to a recent timestamp
            updatedStatus.LastClockInTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(30), "LastClockInTime should be set to a recent timestamp");

            // Verify that the returned time record has the correct type ('ClockIn')
            clockInRecord.Type.Should().Be("ClockIn", "Time record type should be ClockIn");

            // Verify that the time record has a valid ID and timestamp
            clockInRecord.Id.Should().BeGreaterThan(0, "Time record should have a valid ID");
            clockInRecord.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(30), "Time record should have a valid timestamp");
        }

        /// <summary>
        /// Tests the clock out functionality to ensure it correctly records a clock out event.
        /// </summary>
        [Fact]
        public async Task ClockOutTest()
        {
            // Authenticate the user by calling AuthenticateAsync()
            bool authSuccess = await AuthenticateAsync();
            authSuccess.Should().BeTrue("Authentication should succeed");

            // Ensure the user is clocked in by calling ClockInAsync()
            bool clockInSuccess = await ClockInAsync();
            clockInSuccess.Should().BeTrue("Clock in should succeed");

            // Get the current clock status before clock out
            ClockStatus initialStatus = await TimeTrackingService.GetCurrentStatus();

            // Verify that the user is clocked in
            initialStatus.IsClocked.Should().BeTrue("User should be clocked in before clock out");

            // Call TimeTrackingService.ClockOut() to perform clock out operation
            TimeRecordModel clockOutRecord = await TimeTrackingService.ClockOut();

            // Get the updated clock status after clock out
            ClockStatus updatedStatus = await TimeTrackingService.GetCurrentStatus();

            // Verify that the user is now clocked out
            updatedStatus.IsClocked.Should().BeFalse("User should be clocked out after clock out");

            // Verify that LastClockOutTime is set to a recent timestamp
            updatedStatus.LastClockOutTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(30), "LastClockOutTime should be set to a recent timestamp");

            // Verify that the returned time record has the correct type ('ClockOut')
            clockOutRecord.Type.Should().Be("ClockOut", "Time record type should be ClockOut");

            // Verify that the time record has a valid ID and timestamp
            clockOutRecord.Id.Should().BeGreaterThan(0, "Time record should have a valid ID");
            clockOutRecord.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(30), "Time record should have a valid timestamp");
        }

        /// <summary>
        /// Tests the retrieval of time tracking history to ensure it returns the correct records.
        /// </summary>
        [Fact]
        public async Task GetHistoryTest()
        {
            // Authenticate the user by calling AuthenticateAsync()
            bool authSuccess = await AuthenticateAsync();
            authSuccess.Should().BeTrue("Authentication should succeed");

            // Setup mock API response for time history endpoint with test time records
            var testTimeRecords = TestTimeRecords.GenerateTimeRecordModels(5);
            TimeTrackingService.SetTimeRecords(testTimeRecords);

            // Call TimeTrackingService.GetHistory(10) to retrieve time records
            IEnumerable<TimeRecordModel> history = await TimeTrackingService.GetHistory(10);

            // Verify that the returned collection is not null
            history.Should().NotBeNull("History should not be null");

            // Verify that the collection contains the expected number of records
            history.Should().HaveCount(5, "History should contain 5 records");

            // Verify that the records are ordered by timestamp (newest first)
            history.Should().BeInDescendingOrder(r => r.Timestamp, "History should be in descending order of timestamp");

            // Verify that the records contain both clock in and clock out events
            history.Should().Contain(r => r.Type == "ClockIn", "History should contain clock in events");
            history.Should().Contain(r => r.Type == "ClockOut", "History should contain clock out events");

            // Verify that each record has valid properties (ID, Type, Timestamp)
            foreach (var record in history)
            {
                record.Id.Should().BeGreaterThan(0, "Record should have a valid ID");
                record.Type.Should().NotBeNullOrEmpty("Record should have a valid Type");
                record.Timestamp.Should().NotBe(DateTime.MinValue, "Record should have a valid Timestamp");
            }
        }

        /// <summary>
        /// Tests a complete sequence of clock in and clock out operations to ensure they work correctly together.
        /// </summary>
        [Fact]
        public async Task ClockInOutSequenceTest()
        {
            // Authenticate the user by calling AuthenticateAsync()
            bool authSuccess = await AuthenticateAsync();
            authSuccess.Should().BeTrue("Authentication should succeed");

            // Get the initial clock status
            ClockStatus initialStatus = await TimeTrackingService.GetCurrentStatus();

            // Verify that the user is not clocked in initially
            initialStatus.IsClocked.Should().BeFalse("User should not be clocked in initially");

            // Perform first clock in operation
            TimeRecordModel clockIn1 = await TimeTrackingService.ClockIn();

            // Verify that the user is now clocked in
            ClockStatus statusAfterClockIn1 = await TimeTrackingService.GetCurrentStatus();
            statusAfterClockIn1.IsClocked.Should().BeTrue("User should be clocked in after first clock in");

            // Perform clock out operation
            TimeRecordModel clockOut1 = await TimeTrackingService.ClockOut();

            // Verify that the user is now clocked out
            ClockStatus statusAfterClockOut1 = await TimeTrackingService.GetCurrentStatus();
            statusAfterClockOut1.IsClocked.Should().BeFalse("User should be clocked out after first clock out");

            // Perform second clock in operation
            TimeRecordModel clockIn2 = await TimeTrackingService.ClockIn();

            // Verify that the user is clocked in again
            ClockStatus statusAfterClockIn2 = await TimeTrackingService.GetCurrentStatus();
            statusAfterClockIn2.IsClocked.Should().BeTrue("User should be clocked in after second clock in");

            // Perform final clock out operation
            TimeRecordModel clockOut2 = await TimeTrackingService.ClockOut();

            // Verify that the user is clocked out again
            ClockStatus statusAfterClockOut2 = await TimeTrackingService.GetCurrentStatus();
            statusAfterClockOut2.IsClocked.Should().BeFalse("User should be clocked out after second clock out");

            // Get time history
            IEnumerable<TimeRecordModel> history = await TimeTrackingService.GetHistory(10);

            // Verify that history contains all four time records in the correct order
            history.Should().HaveCount(4, "History should contain four records");
            history.First().Should().BeEquivalentTo(clockOut2, "Last record should be clockOut2");
            history.Skip(1).First().Should().BeEquivalentTo(clockIn2, "Second last record should be clockIn2");
            history.Skip(2).First().Should().BeEquivalentTo(clockOut1, "Third last record should be clockOut1");
            history.Skip(3).First().Should().BeEquivalentTo(clockIn1, "Fourth last record should be clockIn1");
        }

        /// <summary>
        /// Tests error handling during clock in operation when the API returns an error.
        /// </summary>
        [Fact]
        public async Task ClockInErrorHandlingTest()
        {
            // Authenticate the user by calling AuthenticateAsync()
            bool authSuccess = await AuthenticateAsync();
            authSuccess.Should().BeTrue("Authentication should succeed");

            // Setup API error response for time/clock endpoint with 500 status code
            ApiServer.SetupErrorResponse("/time/clock", 500, "Simulated server error");

            // Try to call TimeTrackingService.ClockIn() and catch any exceptions
            TimeRecordModel clockInRecord = null;
            try
            {
                clockInRecord = await TimeTrackingService.ClockIn();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception caught: {ex.Message}");
            }

            // Get the current clock status
            ClockStatus currentStatus = await TimeTrackingService.GetCurrentStatus();

            // Verify that the user is clocked in locally
            currentStatus.IsClocked.Should().BeTrue("User should be clocked in locally despite API error");

            // Verify that the time record is marked as not synced
            clockInRecord.IsSynced.Should().BeFalse("Time record should be marked as not synced");

            // Reset API to success response
            ApiServer.SetupSuccessResponse("/time/clock", new { Id = Guid.NewGuid().ToString(), Status = "success" });

            // Call SyncService.SyncAll() to retry synchronization
            bool syncSuccess = await SyncService.SyncAll();

            // Verify that the time record is now marked as synced
            syncSuccess.Should().BeTrue("Sync should succeed after API is restored");
        }

        /// <summary>
        /// Tests that attempting to clock in when already clocked in throws the appropriate exception.
        /// </summary>
        [Fact]
        public async Task AlreadyClockedInTest()
        {
            // Authenticate the user by calling AuthenticateAsync()
            bool authSuccess = await AuthenticateAsync();
            authSuccess.Should().BeTrue("Authentication should succeed");

            // Call ClockInAsync() to ensure the user is clocked in
            bool clockInSuccess = await ClockInAsync();
            clockInSuccess.Should().BeTrue("Clock in should succeed");

            // Attempt to call TimeTrackingService.ClockIn() again and expect InvalidOperationException
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await TimeTrackingService.ClockIn();
            });
        }

        /// <summary>
        /// Tests that attempting to clock out when already clocked out throws the appropriate exception.
        /// </summary>
        [Fact]
        public async Task AlreadyClockedOutTest()
        {
            // Authenticate the user by calling AuthenticateAsync()
            bool authSuccess = await AuthenticateAsync();
            authSuccess.Should().BeTrue("Authentication should succeed");

            // Ensure the user is clocked out (default state after authentication)

            // Attempt to call TimeTrackingService.ClockOut() and expect InvalidOperationException
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await TimeTrackingService.ClockOut();
            });
        }

        /// <summary>
        /// Tests that attempting to clock in when not authenticated throws the appropriate exception.
        /// </summary>
        [Fact]
        public async Task UnauthenticatedClockInTest()
        {
            // Skip authentication step

            // Setup API to simulate unauthenticated state
            ApiServer.SetupErrorResponse("/time/clock", 401, "Unauthorized");

            // Attempt to call TimeTrackingService.ClockIn() and expect UnauthorizedAccessException
            await Assert.ThrowsAsync<Exception>(async () =>
            {
                await TimeTrackingService.ClockIn();
            });
        }
    }
}