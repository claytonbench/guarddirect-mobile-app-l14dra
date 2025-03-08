using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using FluentAssertions;
using SecurityPatrol.API.IntegrationTests.Setup;
using SecurityPatrol.Core.Models;
using SecurityPatrol.TestCommon.Constants;

namespace SecurityPatrol.API.IntegrationTests.API
{
    /// <summary>
    /// Integration tests for the location tracking flow in the Security Patrol API, testing the complete process
    /// of submitting location batches, retrieving location history, and accessing current location information.
    /// </summary>
    public class LocationTrackingFlowTests : IntegrationTestBase
    {
        /// <summary>
        /// Initializes a new instance of the LocationTrackingFlowTests class with the test factory.
        /// </summary>
        /// <param name="factory">The factory that creates a test server with in-memory database.</param>
        public LocationTrackingFlowTests(CustomWebApplicationFactory factory) 
            : base(factory)
        {
            // Authenticate the client for protected endpoint access
            AuthenticateClient();
        }
        
        /// <summary>
        /// Tests that the /location/batch endpoint successfully accepts a batch of location data points and returns a sync response.
        /// </summary>
        [Fact]
        public async Task Should_Submit_Location_Batch_Successfully()
        {
            // Arrange
            var request = CreateTestLocationBatch(5);
            
            // Act
            var response = await PostAsync<LocationBatchRequest, LocationSyncResponse>("/api/v1/location/batch", request);
            
            // Assert
            response.Should().NotBeNull();
            response.SyncedIds.Should().NotBeEmpty();
            response.FailedIds.Should().BeEmpty();
        }
        
        /// <summary>
        /// Tests that the /location/history endpoint successfully retrieves location history for a specified time range.
        /// </summary>
        [Fact]
        public async Task Should_Retrieve_Location_History_Successfully()
        {
            // Arrange - Submit some location data first to ensure history exists
            var request = CreateTestLocationBatch(5);
            await PostAsync<LocationBatchRequest, LocationSyncResponse>("/api/v1/location/batch", request);
            
            // Set time range for history
            var startTime = DateTime.UtcNow.AddDays(-1).ToString("o");
            var endTime = DateTime.UtcNow.AddDays(1).ToString("o");
            
            // Act
            var response = await GetAsync<List<LocationModel>>($"/api/v1/location/history?startTime={startTime}&endTime={endTime}");
            
            // Assert
            response.Should().NotBeNull();
            response.Should().NotBeEmpty();
            response.All(l => l.Timestamp >= DateTime.UtcNow.AddDays(-1) && l.Timestamp <= DateTime.UtcNow.AddDays(1))
                .Should().BeTrue("all locations should fall within the specified time range");
            
            // Validate coordinates
            response.All(l => l.Latitude != 0 && l.Longitude != 0).Should().BeTrue("all locations should have valid coordinates");
        }
        
        /// <summary>
        /// Tests that the /location/current endpoint successfully retrieves the latest location for the authenticated user.
        /// </summary>
        [Fact]
        public async Task Should_Retrieve_Current_Location_Successfully()
        {
            // Arrange - Submit some location data first
            var request = CreateTestLocationBatch(3);
            await PostAsync<LocationBatchRequest, LocationSyncResponse>("/api/v1/location/batch", request);
            
            // Act
            var response = await GetAsync<LocationModel>("/api/v1/location/current");
            
            // Assert
            response.Should().NotBeNull();
            response.Latitude.Should().NotBe(0);
            response.Longitude.Should().NotBe(0);
            // Timestamp should be recent
            response.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromHours(1));
        }
        
        /// <summary>
        /// Tests that the /location/current endpoint returns a 204 No Content response when no location data exists for the user.
        /// </summary>
        [Fact]
        public async Task Should_Return_NoContent_When_No_Current_Location()
        {
            // Arrange - Create a new test client with a clean database
            var newClient = Factory.CreateClient();
            // Authenticate with a user that has no location data
            newClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-token-new-user");
            
            // Act
            var response = await newClient.GetAsync("/api/v1/location/current");
            
            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);
        }
        
        /// <summary>
        /// Tests that the /location/batch endpoint rejects requests from unauthenticated clients.
        /// </summary>
        [Fact]
        public async Task Should_Reject_Unauthenticated_Location_Batch()
        {
            // Arrange
            var request = CreateTestLocationBatch(2);
            var unauthenticatedClient = Factory.CreateClient(); // Client without auth token
            
            // Act
            var jsonContent = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(request, JsonOptions), 
                System.Text.Encoding.UTF8, 
                "application/json");
            var response = await unauthenticatedClient.PostAsync("/api/v1/location/batch", jsonContent);
            
            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
        }
        
        /// <summary>
        /// Tests the complete location tracking flow from submitting location batches to retrieving history and current location.
        /// </summary>
        [Fact]
        public async Task Should_Complete_Full_Location_Tracking_Flow()
        {
            // Arrange - Create test data with different timestamps
            var batch1 = CreateTestLocationBatch(3, DateTime.UtcNow.AddHours(-2));
            var batch2 = CreateTestLocationBatch(3, DateTime.UtcNow.AddHours(-1));
            var batch3 = CreateTestLocationBatch(3); // Current time
            
            // Act - Submit batches
            var response1 = await PostAsync<LocationBatchRequest, LocationSyncResponse>("/api/v1/location/batch", batch1);
            var response2 = await PostAsync<LocationBatchRequest, LocationSyncResponse>("/api/v1/location/batch", batch2);
            var response3 = await PostAsync<LocationBatchRequest, LocationSyncResponse>("/api/v1/location/batch", batch3);
            
            // Verify all submissions succeeded
            response1.SyncedIds.Should().HaveCount(3);
            response2.SyncedIds.Should().HaveCount(3);
            response3.SyncedIds.Should().HaveCount(3);
            
            // Get history for the entire period
            var startTime = DateTime.UtcNow.AddHours(-3).ToString("o");
            var endTime = DateTime.UtcNow.AddHours(1).ToString("o");
            var history = await GetAsync<List<LocationModel>>($"/api/v1/location/history?startTime={startTime}&endTime={endTime}");
            
            // Verify all submitted locations are in history
            history.Should().HaveCountGreaterThanOrEqualTo(9); // We submitted 9 points total
            
            // Get current location
            var currentLocation = await GetAsync<LocationModel>("/api/v1/location/current");
            
            // Verify it matches our most recent submission
            currentLocation.Should().NotBeNull();
            
            // Test filtering by time range
            var middleRangeStart = DateTime.UtcNow.AddHours(-1.5).ToString("o");
            var middleRangeEnd = DateTime.UtcNow.AddHours(-0.5).ToString("o");
            var filteredHistory = await GetAsync<List<LocationModel>>($"/api/v1/location/history?startTime={middleRangeStart}&endTime={middleRangeEnd}");
            
            // This should return approximately batch2 locations
            filteredHistory.Should().HaveCountGreaterThanOrEqualTo(3);
            filteredHistory.Count.Should().BeLessThan(history.Count);
        }
        
        /// <summary>
        /// Tests that the /location/batch endpoint properly handles an empty batch of location data.
        /// </summary>
        [Fact]
        public async Task Should_Handle_Empty_Location_Batch()
        {
            // Arrange
            var request = new LocationBatchRequest
            {
                UserId = TestConstants.TestUserId,
                Locations = new List<LocationModel>()
            };
            
            // Act
            var response = await PostAsync<LocationBatchRequest, LocationSyncResponse>("/api/v1/location/batch", request);
            
            // Assert
            response.Should().NotBeNull();
            response.SyncedIds.Should().BeEmpty();
            response.FailedIds.Should().BeEmpty();
        }
        
        /// <summary>
        /// Helper method to create a batch of test location data points.
        /// </summary>
        /// <param name="count">The number of location points to create.</param>
        /// <param name="startTime">The base timestamp for the location points (optional).</param>
        /// <returns>A location batch request with the specified number of test locations.</returns>
        private LocationBatchRequest CreateTestLocationBatch(int count, DateTime? startTime = null)
        {
            var request = new LocationBatchRequest
            {
                UserId = TestConstants.TestUserId,
                Locations = new List<LocationModel>()
            };
            
            var baseTime = startTime ?? DateTime.UtcNow;
            var random = new Random();
            
            for (int i = 0; i < count; i++)
            {
                // Add some random variation to make the data more realistic
                var latOffset = (random.NextDouble() * 0.001) % 0.01;
                var lngOffset = (random.NextDouble() * 0.001) % 0.01;
                
                request.Locations.Add(new LocationModel
                {
                    Timestamp = baseTime.AddMinutes(i),
                    Latitude = TestConstants.TestLatitude + latOffset,
                    Longitude = TestConstants.TestLongitude + lngOffset,
                    Accuracy = TestConstants.TestAccuracy + (random.NextDouble() * 5.0) // Vary accuracy between base and +5m
                });
            }
            
            return request;
        }
    }
}