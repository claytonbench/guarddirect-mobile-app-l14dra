using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xunit;
using FluentAssertions;
using SecurityPatrol.API.IntegrationTests.Setup;
using SecurityPatrol.TestCommon.Data;
using SecurityPatrol.TestCommon.Constants;
using SecurityPatrol.Core.Models;

namespace SecurityPatrol.API.IntegrationTests.Controllers
{
    /// <summary>
    /// Integration tests for the TimeController API endpoints using a real HTTP client against an in-memory test server.
    /// </summary>
    public class TimeControllerIntegrationTests : IntegrationTestBase
    {
        /// <summary>
        /// Initializes a new instance of the TimeControllerIntegrationTests class with the test factory.
        /// </summary>
        /// <param name="factory">The factory that creates a test server with in-memory database.</param>
        public TimeControllerIntegrationTests(CustomWebApplicationFactory factory) : base(factory)
        {
            // Authenticate the HTTP client for authorized requests
            AuthenticateClient();
        }

        /// <summary>
        /// Tests that a valid clock in request returns a successful response.
        /// </summary>
        /// <returns>Asynchronous task representing the test execution.</returns>
        [Fact]
        public async Task Clock_WithValidClockInRequest_ReturnsSuccess()
        {
            // Arrange
            var request = new
            {
                Type = "ClockIn",
                Timestamp = DateTime.UtcNow,
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/time/clock", request);
            
            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<TimeRecordResponse>();
            result.Should().NotBeNull();
            result.Id.Should().NotBeNullOrEmpty();
            result.Status.Should().Be("Success");
        }

        /// <summary>
        /// Tests that a valid clock out request returns a successful response.
        /// </summary>
        /// <returns>Asynchronous task representing the test execution.</returns>
        [Fact]
        public async Task Clock_WithValidClockOutRequest_ReturnsSuccess()
        {
            // Arrange
            var request = new
            {
                Type = "ClockOut",
                Timestamp = DateTime.UtcNow,
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/time/clock", request);
            
            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<TimeRecordResponse>();
            result.Should().NotBeNull();
            result.Id.Should().NotBeNullOrEmpty();
            result.Status.Should().Be("Success");
        }

        /// <summary>
        /// Tests that an invalid clock request returns a bad request response.
        /// </summary>
        /// <returns>Asynchronous task representing the test execution.</returns>
        [Fact]
        public async Task Clock_WithInvalidRequest_ReturnsBadRequest()
        {
            // Arrange
            var request = new
            {
                Type = "", // Invalid type
                Timestamp = DateTime.UtcNow,
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/time/clock", request);
            
            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        /// <summary>
        /// Tests that the history endpoint returns time records with pagination.
        /// </summary>
        /// <returns>Asynchronous task representing the test execution.</returns>
        [Fact]
        public async Task GetHistory_ReturnsTimeRecords()
        {
            // Act
            var response = await Client.GetAsync("/api/time/history?pageNumber=1&pageSize=10");
            
            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<Result<IEnumerable<TimeRecord>>>();
            result.Should().NotBeNull();
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
        }

        /// <summary>
        /// Tests that the history endpoint returns bad request for invalid pagination parameters.
        /// </summary>
        /// <returns>Asynchronous task representing the test execution.</returns>
        [Fact]
        public async Task GetHistory_WithInvalidPagination_ReturnsBadRequest()
        {
            // Act
            var response = await Client.GetAsync("/api/time/history?pageNumber=0&pageSize=0");
            
            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        /// <summary>
        /// Tests that the status endpoint returns the current clock status.
        /// </summary>
        /// <returns>Asynchronous task representing the test execution.</returns>
        [Fact]
        public async Task GetStatus_ReturnsCurrentStatus()
        {
            // Act
            var response = await Client.GetAsync("/api/time/status");
            
            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<Result<ClockStatus>>();
            result.Should().NotBeNull();
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
        }

        /// <summary>
        /// Tests that the date range endpoint returns time records within the specified date range.
        /// </summary>
        /// <returns>Asynchronous task representing the test execution.</returns>
        [Fact]
        public async Task GetByDateRange_WithValidDates_ReturnsTimeRecords()
        {
            // Arrange
            var startDate = DateTime.UtcNow.AddDays(-7);
            var endDate = DateTime.UtcNow;
            
            // Act
            var response = await Client.GetAsync($"/api/time/range?startDate={startDate:o}&endDate={endDate:o}");
            
            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<Result<IEnumerable<TimeRecord>>>();
            result.Should().NotBeNull();
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
        }

        /// <summary>
        /// Tests that the date range endpoint returns bad request for invalid date parameters.
        /// </summary>
        /// <returns>Asynchronous task representing the test execution.</returns>
        [Fact]
        public async Task GetByDateRange_WithInvalidDates_ReturnsBadRequest()
        {
            // Arrange
            var endDate = DateTime.UtcNow.AddDays(-7);
            var startDate = DateTime.UtcNow; // Start date after end date
            
            // Act
            var response = await Client.GetAsync($"/api/time/range?startDate={startDate:o}&endDate={endDate:o}");
            
            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        /// <summary>
        /// Tests that unauthorized requests to protected endpoints return 401 Unauthorized.
        /// </summary>
        /// <returns>Asynchronous task representing the test execution.</returns>
        [Fact]
        public async Task Unauthorized_Requests_ReturnUnauthorized()
        {
            // Arrange
            var unauthenticatedClient = Factory.CreateClient(); // Client without authentication
            
            // Act - Try to get history
            var historyResponse = await unauthenticatedClient.GetAsync("/api/time/history");
            
            // Assert
            historyResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            
            // Act - Try to get status
            var statusResponse = await unauthenticatedClient.GetAsync("/api/time/status");
            
            // Assert
            statusResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            
            // Act - Try to clock in
            var clockRequest = new
            {
                Type = "ClockIn",
                Timestamp = DateTime.UtcNow,
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude
            };
            var clockResponse = await unauthenticatedClient.PostAsJsonAsync("/api/time/clock", clockRequest);
            
            // Assert
            clockResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}