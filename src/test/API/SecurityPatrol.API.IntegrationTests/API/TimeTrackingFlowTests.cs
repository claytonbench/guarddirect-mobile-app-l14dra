using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xunit;
using FluentAssertions;
using SecurityPatrol.API.IntegrationTests.Setup;
using SecurityPatrol.Core.Models;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.TestCommon.Constants;

namespace SecurityPatrol.API.IntegrationTests.API
{
    /// <summary>
    /// Integration tests for the time tracking flow in the Security Patrol API, testing
    /// the complete clock in/out process and related operations.
    /// </summary>
    public class TimeTrackingFlowTests : IntegrationTestBase
    {
        /// <summary>
        /// Initializes a new instance of the TimeTrackingFlowTests class with the test factory.
        /// </summary>
        /// <param name="factory">The factory that creates a test server with in-memory database.</param>
        public TimeTrackingFlowTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task Should_Clock_In_Successfully()
        {
            // Arrange: Authenticate the client
            AuthenticateClient();

            // Create time record request for clock in
            var request = new TimeRecordRequest
            {
                Type = "in",
                Timestamp = DateTime.UtcNow,
                Location = new LocationModel
                {
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                }
            };

            // Act: Send request to clock in endpoint
            var response = await Client.PostAsJsonAsync("/api/time/clock", request);

            // Assert: Response should be successful
            response.EnsureSuccessStatusCode();

            // Parse response
            var result = await response.Content.ReadFromJsonAsync<Result<TimeRecordResponse>>();

            // Verify result properties
            result.Should().NotBeNull();
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Id.Should().NotBeNullOrEmpty();
            result.Data.Status.Should().Be("success");
        }

        [Fact]
        public async Task Should_Clock_Out_Successfully()
        {
            // Arrange: Authenticate the client
            AuthenticateClient();

            // First clock in
            var clockInRequest = new TimeRecordRequest
            {
                Type = "in",
                Timestamp = DateTime.UtcNow,
                Location = new LocationModel
                {
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                }
            };
            var clockInResponse = await Client.PostAsJsonAsync("/api/time/clock", clockInRequest);
            clockInResponse.EnsureSuccessStatusCode();

            // Create time record request for clock out
            var clockOutRequest = new TimeRecordRequest
            {
                Type = "out",
                Timestamp = DateTime.UtcNow.AddHours(1), // 1 hour later
                Location = new LocationModel
                {
                    Latitude = TestConstants.TestLatitude + 0.001, // Slightly different location
                    Longitude = TestConstants.TestLongitude + 0.001
                }
            };

            // Act: Send request to clock out endpoint
            var clockOutResponse = await Client.PostAsJsonAsync("/api/time/clock", clockOutRequest);

            // Assert: Response should be successful
            clockOutResponse.EnsureSuccessStatusCode();

            // Parse response
            var result = await clockOutResponse.Content.ReadFromJsonAsync<Result<TimeRecordResponse>>();

            // Verify result properties
            result.Should().NotBeNull();
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Id.Should().NotBeNullOrEmpty();
            result.Data.Status.Should().Be("success");
        }

        [Fact]
        public async Task Should_Prevent_Double_Clock_In()
        {
            // Arrange: Authenticate the client
            AuthenticateClient();

            // Create time record request for clock in
            var request = new TimeRecordRequest
            {
                Type = "in",
                Timestamp = DateTime.UtcNow,
                Location = new LocationModel
                {
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                }
            };

            // First clock in should succeed
            var firstResponse = await Client.PostAsJsonAsync("/api/time/clock", request);
            firstResponse.EnsureSuccessStatusCode();

            // Act: Try to clock in again
            var secondResponse = await Client.PostAsJsonAsync("/api/time/clock", request);

            // Assert: Second attempt should fail with Bad Request
            secondResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);

            // Parse response
            var result = await secondResponse.Content.ReadFromJsonAsync<Result>();

            // Verify result properties
            result.Should().NotBeNull();
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("already clocked in");
        }

        [Fact]
        public async Task Should_Prevent_Clock_Out_Without_Clock_In()
        {
            // Arrange: Authenticate the client
            AuthenticateClient();

            // Create time record request for clock out directly
            var request = new TimeRecordRequest
            {
                Type = "out",
                Timestamp = DateTime.UtcNow,
                Location = new LocationModel
                {
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                }
            };

            // Act: Try to clock out without clocking in
            var response = await Client.PostAsJsonAsync("/api/time/clock", request);

            // Assert: Attempt should fail with Bad Request
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);

            // Parse response
            var result = await response.Content.ReadFromJsonAsync<Result>();

            // Verify result properties
            result.Should().NotBeNull();
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("not clocked in");
        }

        [Fact]
        public async Task Should_Get_Current_Status_Successfully()
        {
            // Arrange: Authenticate the client
            AuthenticateClient();

            // Act: Get initial status
            var initialResponse = await Client.GetAsync("/api/time/status");
            initialResponse.EnsureSuccessStatusCode();
            var initialResult = await initialResponse.Content.ReadFromJsonAsync<Result<string>>();

            // The initial status can be either "in" or "out" depending on test data
            initialResult.Data.Should().NotBeNull();
            (initialResult.Data == "in" || initialResult.Data == "out").Should().BeTrue();

            // Clock in
            var clockInRequest = new TimeRecordRequest
            {
                Type = "in",
                Timestamp = DateTime.UtcNow,
                Location = new LocationModel
                {
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                }
            };
            await Client.PostAsJsonAsync("/api/time/clock", clockInRequest);

            // Get status after clock in
            var afterClockInResponse = await Client.GetAsync("/api/time/status");
            afterClockInResponse.EnsureSuccessStatusCode();
            var afterClockInResult = await afterClockInResponse.Content.ReadFromJsonAsync<Result<string>>();

            // Status should be "in"
            afterClockInResult.Data.Should().Be("in");

            // Clock out
            var clockOutRequest = new TimeRecordRequest
            {
                Type = "out",
                Timestamp = DateTime.UtcNow.AddHours(1),
                Location = new LocationModel
                {
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                }
            };
            await Client.PostAsJsonAsync("/api/time/clock", clockOutRequest);

            // Get status after clock out
            var afterClockOutResponse = await Client.GetAsync("/api/time/status");
            afterClockOutResponse.EnsureSuccessStatusCode();
            var afterClockOutResult = await afterClockOutResponse.Content.ReadFromJsonAsync<Result<string>>();

            // Status should be "out"
            afterClockOutResult.Data.Should().Be("out");
        }

        [Fact]
        public async Task Should_Get_Time_Record_History_Successfully()
        {
            // Arrange: Authenticate the client
            AuthenticateClient();

            // Create some time records first (clock in and out)
            var clockInRequest = new TimeRecordRequest
            {
                Type = "in",
                Timestamp = DateTime.UtcNow,
                Location = new LocationModel
                {
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                }
            };
            await Client.PostAsJsonAsync("/api/time/clock", clockInRequest);

            var clockOutRequest = new TimeRecordRequest
            {
                Type = "out",
                Timestamp = DateTime.UtcNow.AddHours(1),
                Location = new LocationModel
                {
                    Latitude = TestConstants.TestLatitude + 0.001,
                    Longitude = TestConstants.TestLongitude + 0.001
                }
            };
            await Client.PostAsJsonAsync("/api/time/clock", clockOutRequest);

            // Act: Get time record history
            var response = await Client.GetAsync("/api/time/history");

            // Assert: Response should be successful
            response.EnsureSuccessStatusCode();

            // Parse response
            var result = await response.Content.ReadFromJsonAsync<Result<PaginatedList<TimeRecord>>>();

            // Verify result properties
            result.Should().NotBeNull();
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Items.Should().NotBeEmpty();
            result.Data.PageNumber.Should().Be(1);
            result.Data.TotalPages.Should().BeGreaterThanOrEqualTo(1);
            result.Data.TotalCount.Should().BeGreaterThanOrEqualTo(2);
        }

        [Fact]
        public async Task Should_Get_Time_Records_By_Date_Range_Successfully()
        {
            // Arrange: Authenticate the client
            AuthenticateClient();

            // Create some time records first (clock in and out)
            var timestamp = DateTime.UtcNow;
            var clockInRequest = new TimeRecordRequest
            {
                Type = "in",
                Timestamp = timestamp,
                Location = new LocationModel
                {
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                }
            };
            await Client.PostAsJsonAsync("/api/time/clock", clockInRequest);

            var clockOutRequest = new TimeRecordRequest
            {
                Type = "out",
                Timestamp = timestamp.AddHours(1),
                Location = new LocationModel
                {
                    Latitude = TestConstants.TestLatitude + 0.001,
                    Longitude = TestConstants.TestLongitude + 0.001
                }
            };
            await Client.PostAsJsonAsync("/api/time/clock", clockOutRequest);

            // Calculate date range to include the records we just created
            var startDate = timestamp.AddHours(-1).ToString("o");
            var endDate = timestamp.AddHours(2).ToString("o");

            // Act: Get time records by date range
            var response = await Client.GetAsync($"/api/time/range?startDate={startDate}&endDate={endDate}");

            // Assert: Response should be successful
            response.EnsureSuccessStatusCode();

            // Parse response
            var result = await response.Content.ReadFromJsonAsync<Result<IEnumerable<TimeRecord>>>();

            // Verify result properties
            result.Should().NotBeNull();
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().NotBeEmpty();
            
            // All records should be within our date range
            foreach (var record in result.Data)
            {
                record.Timestamp.Should().BeOnOrAfter(timestamp.AddHours(-1));
                record.Timestamp.Should().BeOnOrBefore(timestamp.AddHours(2));
            }
        }

        [Fact]
        public async Task Should_Complete_Full_Clock_In_Out_Flow()
        {
            // Arrange: Authenticate the client
            AuthenticateClient();

            // Act 1: Get initial status
            var initialStatusResponse = await Client.GetAsync("/api/time/status");
            initialStatusResponse.EnsureSuccessStatusCode();

            // Act 2: Clock in
            var clockInRequest = new TimeRecordRequest
            {
                Type = "in",
                Timestamp = DateTime.UtcNow,
                Location = new LocationModel
                {
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                }
            };
            var clockInResponse = await Client.PostAsJsonAsync("/api/time/clock", clockInRequest);
            clockInResponse.EnsureSuccessStatusCode();
            var clockInResult = await clockInResponse.Content.ReadFromJsonAsync<Result<TimeRecordResponse>>();
            clockInResult.Succeeded.Should().BeTrue();

            // Act 3: Get status after clock in
            var statusAfterClockInResponse = await Client.GetAsync("/api/time/status");
            statusAfterClockInResponse.EnsureSuccessStatusCode();
            var statusAfterClockInResult = await statusAfterClockInResponse.Content.ReadFromJsonAsync<Result<string>>();
            statusAfterClockInResult.Data.Should().Be("in");

            // Act 4: Clock out
            var clockOutRequest = new TimeRecordRequest
            {
                Type = "out",
                Timestamp = DateTime.UtcNow.AddHours(1),
                Location = new LocationModel
                {
                    Latitude = TestConstants.TestLatitude + 0.001,
                    Longitude = TestConstants.TestLongitude + 0.001
                }
            };
            var clockOutResponse = await Client.PostAsJsonAsync("/api/time/clock", clockOutRequest);
            clockOutResponse.EnsureSuccessStatusCode();
            var clockOutResult = await clockOutResponse.Content.ReadFromJsonAsync<Result<TimeRecordResponse>>();
            clockOutResult.Succeeded.Should().BeTrue();

            // Act 5: Get status after clock out
            var statusAfterClockOutResponse = await Client.GetAsync("/api/time/status");
            statusAfterClockOutResponse.EnsureSuccessStatusCode();
            var statusAfterClockOutResult = await statusAfterClockOutResponse.Content.ReadFromJsonAsync<Result<string>>();
            statusAfterClockOutResult.Data.Should().Be("out");

            // Act 6: Get history to verify both records
            var historyResponse = await Client.GetAsync("/api/time/history");
            historyResponse.EnsureSuccessStatusCode();
            var historyResult = await historyResponse.Content.ReadFromJsonAsync<Result<PaginatedList<TimeRecord>>>();
            
            // Assert: All steps were successful and history contains our records
            historyResult.Should().NotBeNull();
            historyResult.Succeeded.Should().BeTrue();
            historyResult.Data.Should().NotBeNull();
            historyResult.Data.Items.Should().NotBeEmpty();
            
            // Verify history contains both clock in and clock out records
            historyResult.Data.Items.Should().Contain(r => r.Type == "ClockIn" || r.Type == "in");
            historyResult.Data.Items.Should().Contain(r => r.Type == "ClockOut" || r.Type == "out");
        }
    }
}