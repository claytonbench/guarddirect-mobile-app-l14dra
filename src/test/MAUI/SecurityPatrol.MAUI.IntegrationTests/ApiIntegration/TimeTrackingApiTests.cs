using System; // Version 8.0.0
using System.Threading.Tasks; // Version 8.0.0
using System.Collections.Generic; // Version 8.0.0
using System.Linq; // Version 8.0.0
using System.Text.Json; // Version 8.0.0
using Xunit; // Version 2.4.2
using FluentAssertions; // Version 6.11.0
using SecurityPatrol.MAUI.IntegrationTests.Setup;
using SecurityPatrol.TestCommon.Constants;
using SecurityPatrol.Models;
using SecurityPatrol.Constants;

namespace SecurityPatrol.MAUI.IntegrationTests.ApiIntegration
{
    /// <summary>
    /// Integration tests for the time tracking API functionality in the Security Patrol application.
    /// Tests the interaction between the TimeTrackingService and the backend time tracking API endpoints,
    /// verifying that clock in/out operations, status retrieval, and history retrieval work correctly.
    /// </summary>
    public class TimeTrackingApiTests : IntegrationTestBase
    {
        /// <summary>
        /// Initializes a new instance of the TimeTrackingApiTests class
        /// </summary>
        public TimeTrackingApiTests()
        {
            // Call base constructor to initialize the IntegrationTestBase
        }

        /// <summary>
        /// Tests that clocking in when authenticated returns a valid time record
        /// </summary>
        [Fact]
        public async Task ClockIn_WhenAuthenticated_ShouldReturnTimeRecord()
        {
            // Call AuthenticateAsync to authenticate the user
            await AuthenticateAsync();

            // Set up a success response for the time/clock endpoint with a valid TimeRecordResponse
            ApiServer.SetupSuccessResponse(ApiEndpoints.TimeClock, new TimeRecordResponse
            {
                Id = Guid.NewGuid().ToString(),
                Status = "success"
            });

            // Call TimeTrackingService.ClockIn()
            var result = await TimeTrackingService.ClockIn();

            // Assert that the result is not null
            result.Should().NotBeNull();

            // Assert that the result is a TimeRecordModel
            result.Should().BeOfType<TimeRecordModel>();

            // Assert that the result.IsClockIn() is true
            result.IsClockIn().Should().BeTrue();

            // Verify that the API server received a request to the time/clock endpoint
            ApiServer.GetRequestCount(ApiEndpoints.TimeClock).Should().Be(1);

            // Get the current status and verify that IsClocked is true
            var status = await TimeTrackingService.GetCurrentStatus();
            status.IsClocked.Should().BeTrue();

            // Verify that LastClockInTime is set to a recent timestamp
            status.LastClockInTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        /// <summary>
        /// Tests that attempting to clock in when not authenticated throws an UnauthorizedAccessException
        /// </summary>
        [Fact]
        public async Task ClockIn_WhenNotAuthenticated_ShouldThrowUnauthorizedException()
        {
            // Set up a success response for the time/clock endpoint with a valid TimeRecordResponse
            ApiServer.SetupSuccessResponse(ApiEndpoints.TimeClock, new TimeRecordResponse
            {
                Id = Guid.NewGuid().ToString(),
                Status = "success"
            });

            // Call TimeTrackingService.ClockIn() without first authenticating
            Func<Task> clockInAction = async () => await TimeTrackingService.ClockIn();

            // Assert that the operation throws an UnauthorizedAccessException
            await clockInAction.Should().ThrowAsync<UnauthorizedAccessException>();

            // Verify that no request was made to the time/clock endpoint
            ApiServer.GetRequestCount(ApiEndpoints.TimeClock).Should().Be(0);
        }

        /// <summary>
        /// Tests that attempting to clock in when already clocked in throws an InvalidOperationException
        /// </summary>
        [Fact]
        public async Task ClockIn_WhenAlreadyClockedIn_ShouldThrowInvalidOperationException()
        {
            // Call AuthenticateAsync to authenticate the user
            await AuthenticateAsync();

            // Call ClockInAsync to clock in the user
            await ClockInAsync();

            // Call TimeTrackingService.ClockIn() again
            Func<Task> clockInAction = async () => await TimeTrackingService.ClockIn();

            // Assert that the operation throws an InvalidOperationException
            await clockInAction.Should().ThrowAsync<InvalidOperationException>();

            // Verify that only one request was made to the time/clock endpoint
            ApiServer.GetRequestCount(ApiEndpoints.TimeClock).Should().Be(1);
        }

        /// <summary>
        /// Tests that the time tracking service handles API errors during clock in gracefully
        /// </summary>
        [Fact]
        public async Task ClockIn_WithApiError_ShouldHandleGracefully()
        {
            // Call AuthenticateAsync to authenticate the user
            await AuthenticateAsync();

            // Set up an error response for the time/clock endpoint with a 500 status code
            ApiServer.SetupErrorResponse(ApiEndpoints.TimeClock, 500, "Internal Server Error");

            // Call TimeTrackingService.ClockIn()
            Func<Task> clockInAction = async () => await TimeTrackingService.ClockIn();

            // Assert that the operation throws an exception
            await clockInAction.Should().ThrowAsync<Exception>();

            // Verify that the API server received a request to the time/clock endpoint
            ApiServer.GetRequestCount(ApiEndpoints.TimeClock).Should().Be(1);

            // Get the current status and verify that IsClocked is still false
            var status = await TimeTrackingService.GetCurrentStatus();
            status.IsClocked.Should().BeFalse();
        }

        /// <summary>
        /// Tests that clocking out when clocked in returns a valid time record
        /// </summary>
        [Fact]
        public async Task ClockOut_WhenClockedIn_ShouldReturnTimeRecord()
        {
            // Call AuthenticateAsync to authenticate the user
            await AuthenticateAsync();

            // Call ClockInAsync to clock in the user
            await ClockInAsync();

            // Set up a success response for the time/clock endpoint with a valid TimeRecordResponse
            ApiServer.SetupSuccessResponse(ApiEndpoints.TimeClock, new TimeRecordResponse
            {
                Id = Guid.NewGuid().ToString(),
                Status = "success"
            });

            // Call TimeTrackingService.ClockOut()
            var result = await TimeTrackingService.ClockOut();

            // Assert that the result is not null
            result.Should().NotBeNull();

            // Assert that the result is a TimeRecordModel
            result.Should().BeOfType<TimeRecordModel>();

            // Assert that the result.IsClockOut() is true
            result.IsClockOut().Should().BeTrue();

            // Verify that the API server received a request to the time/clock endpoint
            ApiServer.GetRequestCount(ApiEndpoints.TimeClock).Should().Be(2);

            // Get the current status and verify that IsClocked is false
            var status = await TimeTrackingService.GetCurrentStatus();
            status.IsClocked.Should().BeFalse();

            // Verify that LastClockOutTime is set to a recent timestamp
            status.LastClockOutTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        /// <summary>
        /// Tests that attempting to clock out when not authenticated throws an UnauthorizedAccessException
        /// </summary>
        [Fact]
        public async Task ClockOut_WhenNotAuthenticated_ShouldThrowUnauthorizedException()
        {
            // Set up a success response for the time/clock endpoint with a valid TimeRecordResponse
            ApiServer.SetupSuccessResponse(ApiEndpoints.TimeClock, new TimeRecordResponse
            {
                Id = Guid.NewGuid().ToString(),
                Status = "success"
            });

            // Call TimeTrackingService.ClockOut() without first authenticating
            Func<Task> clockOutAction = async () => await TimeTrackingService.ClockOut();

            // Assert that the operation throws an UnauthorizedAccessException
            await clockOutAction.Should().ThrowAsync<UnauthorizedAccessException>();

            // Verify that no request was made to the time/clock endpoint
            ApiServer.GetRequestCount(ApiEndpoints.TimeClock).Should().Be(0);
        }

        /// <summary>
        /// Tests that attempting to clock out when not clocked in throws an InvalidOperationException
        /// </summary>
        [Fact]
        public async Task ClockOut_WhenNotClockedIn_ShouldThrowInvalidOperationException()
        {
            // Call AuthenticateAsync to authenticate the user
            await AuthenticateAsync();

            // Call TimeTrackingService.ClockOut() without first clocking in
            Func<Task> clockOutAction = async () => await TimeTrackingService.ClockOut();

            // Assert that the operation throws an InvalidOperationException
            await clockOutAction.Should().ThrowAsync<InvalidOperationException>();

            // Verify that no request was made to the time/clock endpoint
            ApiServer.GetRequestCount(ApiEndpoints.TimeClock).Should().Be(0);
        }

        /// <summary>
        /// Tests that the time tracking service handles API errors during clock out gracefully
        /// </summary>
        [Fact]
        public async Task ClockOut_WithApiError_ShouldHandleGracefully()
        {
            // Call AuthenticateAsync to authenticate the user
            await AuthenticateAsync();

            // Call ClockInAsync to clock in the user
            await ClockInAsync();

            // Set up an error response for the time/clock endpoint with a 500 status code
            ApiServer.SetupErrorResponse(ApiEndpoints.TimeClock, 500, "Internal Server Error");

            // Call TimeTrackingService.ClockOut()
            Func<Task> clockOutAction = async () => await TimeTrackingService.ClockOut();

            // Assert that the operation throws an exception
            await clockOutAction.Should().ThrowAsync<Exception>();

            // Verify that the API server received a request to the time/clock endpoint
            ApiServer.GetRequestCount(ApiEndpoints.TimeClock).Should().Be(2);

            // Get the current status and verify that IsClocked is still true
            var status = await TimeTrackingService.GetCurrentStatus();
            status.IsClocked.Should().BeTrue();
        }

        /// <summary>
        /// Tests that getting the current status returns the correct clock status
        /// </summary>
        [Fact]
        public async Task GetCurrentStatus_ShouldReturnCurrentClockStatus()
        {
            // Call AuthenticateAsync to authenticate the user
            await AuthenticateAsync();

            // Call TimeTrackingService.GetCurrentStatus()
            var status = await TimeTrackingService.GetCurrentStatus();

            // Assert that the result is not null
            status.Should().NotBeNull();

            // Assert that the result is a ClockStatus
            status.Should().BeOfType<ClockStatus>();

            // Assert that the result.IsClocked is false
            status.IsClocked.Should().BeFalse();

            // Call ClockInAsync to clock in the user
            await ClockInAsync();

            // Call TimeTrackingService.GetCurrentStatus() again
            status = await TimeTrackingService.GetCurrentStatus();

            // Assert that the result.IsClocked is now true
            status.IsClocked.Should().BeTrue();

            // Assert that the result.LastClockInTime is set to a recent timestamp
            status.LastClockInTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

            // Call ClockOutAsync to clock out the user
            await ClockOutAsync();

            // Call TimeTrackingService.GetCurrentStatus() again
            status = await TimeTrackingService.GetCurrentStatus();

            // Assert that the result.IsClocked is now false
            status.IsClocked.Should().BeFalse();

            // Assert that the result.LastClockOutTime is set to a recent timestamp
            status.LastClockOutTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        /// <summary>
        /// Tests that getting the time tracking history returns the correct time records
        /// </summary>
        [Fact]
        public async Task GetHistory_ShouldReturnTimeRecords()
        {
            // Call AuthenticateAsync to authenticate the user
            await AuthenticateAsync();

            // Set up a success response for the time/history endpoint with sample time records
            ApiServer.SetupSuccessResponse(ApiEndpoints.TimeHistory, new[]
            {
                new
                {
                    Id = Guid.NewGuid().ToString(),
                    Type = "clockIn",
                    Timestamp = DateTime.UtcNow.AddHours(-8).ToString("o"),
                    Location = new
                    {
                        Latitude = TestConstants.TestLatitude,
                        Longitude = TestConstants.TestLongitude
                    }
                },
                new
                {
                    Id = Guid.NewGuid().ToString(),
                    Type = "clockOut",
                    Timestamp = DateTime.UtcNow.AddHours(-1).ToString("o"),
                    Location = new
                    {
                        Latitude = TestConstants.TestLatitude + 0.01,
                        Longitude = TestConstants.TestLongitude - 0.01
                    }
                }
            });

            // Call TimeTrackingService.GetHistory(10)
            var result = await TimeTrackingService.GetHistory(10);

            // Assert that the result is not null
            result.Should().NotBeNull();

            // Assert that the result is an IEnumerable<TimeRecordModel>
            result.Should().BeAssignableTo<IEnumerable<TimeRecordModel>>();

            // Assert that the result contains the expected number of records
            result.Count().Should().Be(2);

            // Verify that the API server received a request to the time/history endpoint
            ApiServer.GetRequestCount(ApiEndpoints.TimeHistory).Should().Be(1);
        }

        /// <summary>
        /// Tests that getting the time tracking history with an invalid count throws an ArgumentException
        /// </summary>
        [Fact]
        public async Task GetHistory_WithInvalidCount_ShouldThrowArgumentException()
        {
            // Call AuthenticateAsync to authenticate the user
            await AuthenticateAsync();

            // Call TimeTrackingService.GetHistory(0)
            Func<Task> getHistoryWithZero = async () => await TimeTrackingService.GetHistory(0);

            // Assert that the operation throws an ArgumentException
            await getHistoryWithZero.Should().ThrowAsync<ArgumentException>();

            // Call TimeTrackingService.GetHistory(-1)
            Func<Task> getHistoryWithNegative = async () => await TimeTrackingService.GetHistory(-1);

            // Assert that the operation throws an ArgumentException
            await getHistoryWithNegative.Should().ThrowAsync<ArgumentException>();

            // Verify that no request was made to the time/history endpoint
            ApiServer.GetRequestCount(ApiEndpoints.TimeHistory).Should().Be(0);
        }

        /// <summary>
        /// Tests that the time tracking service handles API errors during history retrieval gracefully
        /// </summary>
        [Fact]
        public async Task GetHistory_WithApiError_ShouldHandleGracefully()
        {
            // Call AuthenticateAsync to authenticate the user
            await AuthenticateAsync();

            // Set up an error response for the time/history endpoint with a 500 status code
            ApiServer.SetupErrorResponse(ApiEndpoints.TimeHistory, 500, "Internal Server Error");

            // Call TimeTrackingService.GetHistory(10)
            Func<Task> getHistoryAction = async () => await TimeTrackingService.GetHistory(10);

            // Assert that the operation throws an exception
            await getHistoryAction.Should().ThrowAsync<Exception>();

            // Verify that the API server received a request to the time/history endpoint
            ApiServer.GetRequestCount(ApiEndpoints.TimeHistory).Should().Be(1);
        }

        /// <summary>
        /// Tests the complete time tracking flow from clock in to clock out to history retrieval
        /// </summary>
        [Fact]
        public async Task CompleteTimeTrackingFlow_ShouldSucceed()
        {
            // Call AuthenticateAsync to authenticate the user
            await AuthenticateAsync();

            // Set up success responses for all time tracking endpoints
            ApiServer.SetupSuccessResponse(ApiEndpoints.TimeClock, new TimeRecordResponse
            {
                Id = Guid.NewGuid().ToString(),
                Status = "success"
            });

            ApiServer.SetupSuccessResponse(ApiEndpoints.TimeHistory, new[]
            {
                new
                {
                    Id = Guid.NewGuid().ToString(),
                    Type = "clockIn",
                    Timestamp = DateTime.UtcNow.AddHours(-8).ToString("o"),
                    Location = new
                    {
                        Latitude = TestConstants.TestLatitude,
                        Longitude = TestConstants.TestLongitude
                    }
                },
                new
                {
                    Id = Guid.NewGuid().ToString(),
                    Type = "clockOut",
                    Timestamp = DateTime.UtcNow.AddHours(-1).ToString("o"),
                    Location = new
                    {
                        Latitude = TestConstants.TestLatitude + 0.01,
                        Longitude = TestConstants.TestLongitude - 0.01
                    }
                }
            });

            // Call TimeTrackingService.GetCurrentStatus()
            var status = await TimeTrackingService.GetCurrentStatus();

            // Assert that the result.IsClocked is false
            status.IsClocked.Should().BeFalse();

            // Call TimeTrackingService.ClockIn()
            var clockInResult = await TimeTrackingService.ClockIn();

            // Assert that the result is a TimeRecordModel with IsClockIn() true
            clockInResult.Should().BeOfType<TimeRecordModel>();
            clockInResult.IsClockIn().Should().BeTrue();

            // Call TimeTrackingService.GetCurrentStatus() again
            status = await TimeTrackingService.GetCurrentStatus();

            // Assert that the result.IsClocked is now true
            status.IsClocked.Should().BeTrue();

            // Call TimeTrackingService.ClockOut()
            var clockOutResult = await TimeTrackingService.ClockOut();

            // Assert that the result is a TimeRecordModel with IsClockOut() true
            clockOutResult.Should().BeOfType<TimeRecordModel>();
            clockOutResult.IsClockOut().Should().BeTrue();

            // Call TimeTrackingService.GetCurrentStatus() again
            status = await TimeTrackingService.GetCurrentStatus();

            // Assert that the result.IsClocked is now false
            status.IsClocked.Should().BeFalse();

            // Call TimeTrackingService.GetHistory(10)
            var history = await TimeTrackingService.GetHistory(10);

            // Assert that the result contains at least 2 records (clock in and clock out)
            history.Count().Should().BeGreaterOrEqualTo(2);

            // Verify that the API server received requests to all time tracking endpoints
            ApiServer.GetRequestCount(ApiEndpoints.TimeClock).Should().Be(2);
            ApiServer.GetRequestCount(ApiEndpoints.TimeHistory).Should().Be(1);
        }
    }
}