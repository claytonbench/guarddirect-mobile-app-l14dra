using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Core.Models;
using Xunit;

namespace SecurityPatrol.IntegrationTests.Controllers
{
    /// <summary>
    /// Integration tests for the LocationController in the Security Patrol API
    /// </summary>
    public class LocationControllerTests : TestBase
    {
        /// <summary>
        /// Initializes a new instance of the LocationControllerTests class with the test factory
        /// </summary>
        /// <param name="factory">The test factory to create the test server</param>
        public LocationControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
            // Set up authentication token for the test user
            SetAuthToken("test-auth-token");
        }

        /// <summary>
        /// Tests that the batch endpoint successfully processes a valid location batch request
        /// </summary>
        [Fact]
        public async Task BatchAsync_WithValidRequest_ReturnsSuccessResponse()
        {
            // Arrange
            var request = CreateTestLocationBatchRequest(TestUserId, 5);

            // Act
            var response = await PostAsync<LocationBatchRequest, LocationSyncResponse>("/api/v1/location/batch", request);

            // Assert
            response.Should().NotBeNull();
            response.SyncedIds.Should().HaveCount(5);
            response.FailedIds.Should().BeEmpty();
        }

        /// <summary>
        /// Tests that the batch endpoint returns a bad request response when the locations collection is empty
        /// </summary>
        [Fact]
        public async Task BatchAsync_WithEmptyLocations_ReturnsBadRequest()
        {
            // Arrange
            var request = new LocationBatchRequest
            {
                UserId = TestUserId,
                Locations = new List<LocationModel>()
            };

            // Act
            var response = await Client.PostAsync("/api/v1/location/batch", CreateJsonContent(request));

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        }

        /// <summary>
        /// Tests that the batch endpoint returns unauthorized when no authentication token is provided
        /// </summary>
        [Fact]
        public async Task BatchAsync_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Arrange
            Client.DefaultRequestHeaders.Remove("Authorization");
            var request = CreateTestLocationBatchRequest(TestUserId, 3);

            // Act
            var response = await Client.PostAsync("/api/v1/location/batch", CreateJsonContent(request));

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
        }

        /// <summary>
        /// Tests that the history endpoint returns location history for a valid time range
        /// </summary>
        [Fact]
        public async Task GetHistoryAsync_WithValidTimeRange_ReturnsLocationHistory()
        {
            // Arrange
            var startTime = DateTime.UtcNow.AddHours(-1);
            var locations = SeedTestLocationRecords(TestUserId, 5, startTime);

            // Act
            var endTime = DateTime.UtcNow;
            var response = await GetAsync<List<LocationModel>>($"/api/v1/location/history?startTime={startTime:o}&endTime={endTime:o}");
            
            // Assert
            response.Should().NotBeNull();
            response.Should().HaveCount(5);
            response[0].Latitude.Should().Be(locations[0].Latitude);
            response[0].Longitude.Should().Be(locations[0].Longitude);
        }

        /// <summary>
        /// Tests that the history endpoint returns a bad request when the time range is invalid
        /// </summary>
        [Fact]
        public async Task GetHistoryAsync_WithInvalidTimeRange_ReturnsBadRequest()
        {
            // Arrange
            var startTime = DateTime.UtcNow;
            var endTime = DateTime.UtcNow.AddHours(-1); // End time before start time

            // Act
            var response = await Client.GetAsync($"/api/v1/location/history?startTime={startTime:o}&endTime={endTime:o}");
            
            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        }

        /// <summary>
        /// Tests that the history endpoint returns unauthorized when no authentication token is provided
        /// </summary>
        [Fact]
        public async Task GetHistoryAsync_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Arrange
            Client.DefaultRequestHeaders.Remove("Authorization");
            var startTime = DateTime.UtcNow.AddHours(-1);
            var endTime = DateTime.UtcNow;

            // Act
            var response = await Client.GetAsync($"/api/v1/location/history?startTime={startTime:o}&endTime={endTime:o}");
            
            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
        }

        /// <summary>
        /// Tests that the current endpoint returns the latest location for the authenticated user
        /// </summary>
        [Fact]
        public async Task GetCurrentAsync_WithExistingLocation_ReturnsLatestLocation()
        {
            // Arrange
            var locations = SeedTestLocationRecords(TestUserId, 3);
            var latestLocation = locations.OrderByDescending(l => l.Timestamp).First();

            // Act
            var response = await GetAsync<LocationModel>("/api/v1/location/current");
            
            // Assert
            response.Should().NotBeNull();
            response.Latitude.Should().Be(latestLocation.Latitude);
            response.Longitude.Should().Be(latestLocation.Longitude);
            response.Timestamp.Should().BeCloseTo(latestLocation.Timestamp, TimeSpan.FromSeconds(1));
        }

        /// <summary>
        /// Tests that the current endpoint returns no content when no location exists for the user
        /// </summary>
        [Fact]
        public async Task GetCurrentAsync_WithNoExistingLocation_ReturnsNoContent()
        {
            // Arrange - ensure no locations exist for the test user
            using var context = Factory.CreateDbContext();
            var existingLocations = context.LocationRecords.Where(l => l.UserId == TestUserId);
            context.LocationRecords.RemoveRange(existingLocations);
            await context.SaveChangesAsync();

            // Act
            var response = await Client.GetAsync("/api/v1/location/current");
            
            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);
        }

        /// <summary>
        /// Tests that the current endpoint returns unauthorized when no authentication token is provided
        /// </summary>
        [Fact]
        public async Task GetCurrentAsync_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Arrange
            Client.DefaultRequestHeaders.Remove("Authorization");

            // Act
            var response = await Client.GetAsync("/api/v1/location/current");
            
            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
        }

        /// <summary>
        /// Helper method to seed the database with test location records for a user
        /// </summary>
        /// <param name="userId">The user ID to create records for</param>
        /// <param name="count">The number of records to create</param>
        /// <param name="startTime">Optional starting time for the records</param>
        /// <returns>The list of created location records</returns>
        private List<LocationRecord> SeedTestLocationRecords(string userId, int count, DateTime? startTime = null)
        {
            using var context = Factory.CreateDbContext();
            var records = new List<LocationRecord>();
            
            startTime ??= DateTime.UtcNow;
            
            for (int i = 0; i < count; i++)
            {
                var record = new LocationRecord
                {
                    UserId = userId,
                    Latitude = 37.7749 + (i * 0.001),
                    Longitude = -122.4194 + (i * 0.001),
                    Accuracy = 10.0,
                    Timestamp = startTime.Value.AddMinutes(i),
                    IsSynced = true
                };
                
                records.Add(record);
                context.LocationRecords.Add(record);
            }
            
            context.SaveChanges();
            return records;
        }

        /// <summary>
        /// Helper method to create a test location batch request
        /// </summary>
        /// <param name="userId">The user ID for the request</param>
        /// <param name="count">The number of locations to include</param>
        /// <returns>A location batch request with test data</returns>
        private LocationBatchRequest CreateTestLocationBatchRequest(string userId, int count)
        {
            var request = new LocationBatchRequest
            {
                UserId = userId,
                Locations = new List<LocationModel>()
            };
            
            for (int i = 0; i < count; i++)
            {
                request.Locations.Add(new LocationModel
                {
                    Latitude = 37.7749 + (i * 0.001),
                    Longitude = -122.4194 + (i * 0.001),
                    Accuracy = 10.0,
                    Timestamp = DateTime.UtcNow.AddMinutes(-i)
                });
            }
            
            return request;
        }
    }
}