using System; // Version 8.0.0
using System.Collections.Generic; // Version 8.0.0
using System.Linq; // Version 8.0.0
using System.Net.Http; // Version 8.0.0
using System.Threading.Tasks; // Version 8.0.0
using SecurityPatrol.API.IntegrationTests.Setup;
using SecurityPatrol.Core.Models;
using Xunit; // Version 2.4.0
using FluentAssertions; // Version 6.0.0
using SecurityPatrol.TestCommon.Data; // MockDataGenerator
using System.Net;
using System.Text.Json;

namespace SecurityPatrol.API.IntegrationTests.Controllers
{
    /// <summary>
    /// Integration tests for the Location Controller API endpoints, verifying the functionality of location data submission, retrieval, and batch processing.
    /// </summary>
    [public]
    public class LocationControllerIntegrationTests : IntegrationTestBase
    {
        /// <summary>
        /// Initializes a new instance of the LocationControllerIntegrationTests class with the test factory.
        /// </summary>
        /// <param name="factory">The factory that creates a test server with in-memory database.</param>
        public LocationControllerIntegrationTests(CustomWebApplicationFactory factory) : base(factory)
        {
            // Call the base constructor with the factory parameter
            // Authenticate the HTTP client for subsequent requests
            AuthenticateClient();
        }

        /// <summary>
        /// Tests that the batch endpoint successfully processes a valid location batch request.
        /// </summary>
        [Fact]
        [async]
        public async Task BatchAsync_WithValidRequest_ShouldReturnSuccess()
        {
            // Create a list of test location models using MockDataGenerator
            List<LocationModel> testLocations = new List<LocationModel>
            {
                MockDataGenerator.CreateLocationModel(1),
                MockDataGenerator.CreateLocationModel(2),
                MockDataGenerator.CreateLocationModel(3)
            };

            // Create a LocationBatchRequest with the test locations
            LocationBatchRequest batchRequest = new LocationBatchRequest
            {
                UserId = TestConstants.TestUserId,
                Locations = testLocations
            };

            // Send a POST request to the batch endpoint
            var response = await PostAsync<LocationBatchRequest, LocationSyncResponse>("/api/v1/Location/batch", batchRequest);

            // Verify that the response is successful
            response.Should().NotBeNull();

            // Verify that the response contains the expected number of synced IDs
            response.GetSuccessCount().Should().Be(testLocations.Count);

            // Verify that there are no failed IDs
            response.HasFailures().Should().BeFalse();
        }

        /// <summary>
        /// Tests that the batch endpoint returns a bad request response when the locations collection is empty.
        /// </summary>
        [Fact]
        [async]
        public async Task BatchAsync_WithEmptyLocations_ShouldReturnBadRequest()
        {
            // Create a LocationBatchRequest with an empty locations collection
            LocationBatchRequest batchRequest = new LocationBatchRequest
            {
                UserId = TestConstants.TestUserId,
                Locations = new List<LocationModel>()
            };

            // Send a POST request to the batch endpoint
            var response = await Client.PostAsync("/api/v1/Location/batch", JsonContent.Create(batchRequest));

            // Verify that the response status code is BadRequest (400)
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        /// <summary>
        /// Tests that the batch endpoint can handle partially invalid location data, processing valid entries and reporting failures.
        /// </summary>
        [Fact]
        [async]
        public async Task BatchAsync_WithInvalidLocationData_ShouldReturnPartialSuccess()
        {
            // Create a list of test location models with some invalid data (e.g., extreme coordinates)
            List<LocationModel> testLocations = new List<LocationModel>
            {
                MockDataGenerator.CreateLocationModel(1),
                new LocationModel { Latitude = 91, Longitude = 181, Accuracy = 10, Timestamp = DateTime.UtcNow }, // Invalid coordinates
                MockDataGenerator.CreateLocationModel(3)
            };

            // Create a LocationBatchRequest with the mixed valid/invalid locations
            LocationBatchRequest batchRequest = new LocationBatchRequest
            {
                UserId = TestConstants.TestUserId,
                Locations = testLocations
            };

            // Send a POST request to the batch endpoint
            var response = await PostAsync<LocationBatchRequest, LocationSyncResponse>("/api/v1/Location/batch", batchRequest);

            // Verify that the response is successful
            response.Should().NotBeNull();

            // Verify that some locations were processed successfully
            response.GetSuccessCount().Should().BeGreaterThan(0);

            // Verify that some locations failed processing
            response.HasFailures().Should().BeTrue();
        }

        /// <summary>
        /// Tests that the current endpoint returns the latest location for the authenticated user.
        /// </summary>
        [Fact]
        [async]
        public async Task GetCurrentAsync_ShouldReturnLatestLocation()
        {
            // First submit a batch of location data to ensure there's data to retrieve
            List<LocationModel> testLocations = new List<LocationModel>
            {
                MockDataGenerator.CreateLocationModel(1),
                MockDataGenerator.CreateLocationModel(2)
            };

            LocationBatchRequest batchRequest = new LocationBatchRequest
            {
                UserId = TestConstants.TestUserId,
                Locations = testLocations
            };

            await PostAsync<LocationBatchRequest, LocationSyncResponse>("/api/v1/Location/batch", batchRequest);

            // Send a GET request to the current endpoint
            var response = await GetAsync<Result<LocationModel>>("/api/v1/Location/current");

            // Verify that the response is successful
            response.Should().NotBeNull();
            response.Succeeded.Should().BeTrue();
            response.Data.Should().NotBeNull();

            // Verify that the returned location has valid coordinates
            response.Data.Latitude.Should().BeInRange(-90, 90);
            response.Data.Longitude.Should().BeInRange(-180, 180);
        }

        /// <summary>
        /// Tests that the history endpoint returns location history for a specified time range.
        /// </summary>
        [Fact]
        [async]
        public async Task GetHistoryAsync_WithValidTimeRange_ShouldReturnLocationHistory()
        {
            // First submit a batch of location data with timestamps in the desired range
            List<LocationModel> testLocations = new List<LocationModel>
            {
                new LocationModel { Latitude = 34.0522, Longitude = -118.2437, Accuracy = 10, Timestamp = DateTime.UtcNow.AddHours(-2) },
                new LocationModel { Latitude = 34.0523, Longitude = -118.2438, Accuracy = 10, Timestamp = DateTime.UtcNow.AddHours(-1) },
                new LocationModel { Latitude = 34.0524, Longitude = -118.2439, Accuracy = 10, Timestamp = DateTime.UtcNow }
            };

            LocationBatchRequest batchRequest = new LocationBatchRequest
            {
                UserId = TestConstants.TestUserId,
                Locations = testLocations
            };

            await PostAsync<LocationBatchRequest, LocationSyncResponse>("/api/v1/Location/batch", batchRequest);

            // Define a time range for the history query
            DateTime startTime = DateTime.UtcNow.AddHours(-3);
            DateTime endTime = DateTime.UtcNow.AddHours(1);

            // Send a GET request to the history endpoint with the time range parameters
            var response = await Client.GetAsync($"/api/v1/Location/history?startTime={startTime:O}&endTime={endTime:O}");

            // Verify that the response is successful
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Deserialize the response content
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<Result<List<LocationModel>>>(content, options);

            // Verify that the returned collection contains location data
            result.Should().NotBeNull();
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().NotBeEmpty();

            // Verify that all returned locations are within the specified time range
            foreach (var location in result.Data)
            {
                location.Timestamp.Should().BeOnOrAfter(startTime);
                location.Timestamp.Should().BeOnOrBefore(endTime);
            }
        }

        /// <summary>
        /// Tests that the history endpoint returns a bad request response when the time range is invalid.
        /// </summary>
        [Fact]
        [async]
        public async Task GetHistoryAsync_WithInvalidTimeRange_ShouldReturnBadRequest()
        {
            // Define an invalid time range where end time is before start time
            DateTime startTime = DateTime.UtcNow;
            DateTime endTime = DateTime.UtcNow.AddHours(-1);

            // Send a GET request to the history endpoint with the invalid time range
            var response = await Client.GetAsync($"/api/v1/Location/history?startTime={startTime:O}&endTime={endTime:O}");

            // Verify that the response status code is BadRequest (400)
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        /// <summary>
        /// Tests that unauthenticated requests to protected endpoints return unauthorized responses.
        /// </summary>
        [Fact]
        [async]
        public async Task Unauthenticated_Requests_ShouldReturnUnauthorized()
        {
            // Create a new HttpClient without authentication
            var unauthenticatedClient = Factory.CreateClient();

            // Send a GET request to the current endpoint
            var currentResponse = await unauthenticatedClient.GetAsync("/api/v1/Location/current");

            // Verify that the response status code is Unauthorized (401)
            currentResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            // Send a GET request to the history endpoint
            var historyResponse = await unauthenticatedClient.GetAsync("/api/v1/Location/history");

            // Verify that the response status code is Unauthorized (401)
            historyResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            // Create a LocationBatchRequest with test data
            LocationBatchRequest batchRequest = new LocationBatchRequest
            {
                UserId = TestConstants.TestUserId,
                Locations = new List<LocationModel> { MockDataGenerator.CreateLocationModel(1) }
            };

            // Send a POST request to the batch endpoint
            var batchResponse = await unauthenticatedClient.PostAsync("/api/v1/Location/batch", JsonContent.Create(batchRequest));

            // Verify that the response status code is Unauthorized (401)
            batchResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}