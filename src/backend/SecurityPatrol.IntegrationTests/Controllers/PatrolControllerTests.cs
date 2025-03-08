using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Core.Models;
using Xunit;

namespace SecurityPatrol.IntegrationTests.Controllers
{
    /// <summary>
    /// Integration tests for the PatrolController in the Security Patrol API, verifying patrol management operations.
    /// </summary>
    public class PatrolControllerTests : TestBase
    {
        private const string BaseUrl = "api/v1/patrol";
        private string _authToken;

        /// <summary>
        /// Initializes a new instance of the PatrolControllerTests class with the test factory.
        /// </summary>
        /// <param name="factory">The web application factory for creating the test server.</param>
        public PatrolControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
            // Set up authentication for tests
            SetupAuthenticationAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Sets up authentication by obtaining a valid token for test requests.
        /// </summary>
        private async Task SetupAuthenticationAsync()
        {
            // Authenticate using the auth controller endpoints
            var authRequest = new { PhoneNumber = TestPhoneNumber };
            var response = await Client.PostAsync("api/v1/auth/verify", CreateJsonContent(authRequest));
            response.EnsureSuccessStatusCode();
            
            var verificationRequest = new { PhoneNumber = TestPhoneNumber, Code = "123456" }; // Test verification code
            var tokenResponse = await Client.PostAsync("api/v1/auth/validate", CreateJsonContent(verificationRequest));
            tokenResponse.EnsureSuccessStatusCode();
            
            _authToken = "test-token"; // Just a placeholder, the actual token comes from TestAuthHandler
            SetAuthToken(_authToken);
        }

        [Fact]
        public async Task GetLocations_ReturnsAllLocations()
        {
            // Act
            var response = await Client.GetAsync($"{BaseUrl}/locations");
            
            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<Result<List<LocationModel>>>(JsonOptions);
            
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeEmpty();
            
            // Verify location properties
            foreach (var location in result.Data)
            {
                location.Id.Should().BeGreaterThan(0);
                location.Name.Should().NotBeNullOrEmpty();
                location.Latitude.Should().NotBe(0);
                location.Longitude.Should().NotBe(0);
            }
        }

        [Fact]
        public async Task GetLocationById_WithValidId_ReturnsLocation()
        {
            // Arrange - Get all locations to find a valid ID
            var locationsResponse = await Client.GetAsync($"{BaseUrl}/locations");
            locationsResponse.EnsureSuccessStatusCode();
            var locationsResult = await locationsResponse.Content.ReadFromJsonAsync<Result<List<LocationModel>>>(JsonOptions);
            var locationId = locationsResult.Data[0].Id;
            
            // Act
            var response = await Client.GetAsync($"{BaseUrl}/locations/{locationId}");
            
            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<Result<LocationModel>>(JsonOptions);
            
            result.Succeeded.Should().BeTrue();
            result.Data.Id.Should().Be(locationId);
            result.Data.Name.Should().NotBeNullOrEmpty();
            result.Data.Latitude.Should().NotBe(0);
            result.Data.Longitude.Should().NotBe(0);
        }

        [Fact]
        public async Task GetLocationById_WithInvalidId_ReturnsNotFound()
        {
            // Act
            var response = await Client.GetAsync($"{BaseUrl}/locations/999999");
            
            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetCheckpointsByLocationId_WithValidId_ReturnsCheckpoints()
        {
            // Arrange - Get all locations to find a valid ID
            var locationsResponse = await Client.GetAsync($"{BaseUrl}/locations");
            locationsResponse.EnsureSuccessStatusCode();
            var locationsResult = await locationsResponse.Content.ReadFromJsonAsync<Result<List<LocationModel>>>(JsonOptions);
            var locationId = locationsResult.Data[0].Id;
            
            // Act
            var response = await Client.GetAsync($"{BaseUrl}/locations/{locationId}/checkpoints");
            
            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<Result<List<CheckpointModel>>>(JsonOptions);
            
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeEmpty();
            
            // Verify checkpoint properties
            foreach (var checkpoint in result.Data)
            {
                checkpoint.Id.Should().BeGreaterThan(0);
                checkpoint.LocationId.Should().Be(locationId);
                checkpoint.Name.Should().NotBeNullOrEmpty();
                checkpoint.Latitude.Should().NotBe(0);
                checkpoint.Longitude.Should().NotBe(0);
            }
        }

        [Fact]
        public async Task GetCheckpointsByLocationId_WithInvalidId_ReturnsNotFound()
        {
            // Act
            var response = await Client.GetAsync($"{BaseUrl}/locations/999999/checkpoints");
            
            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetCheckpointById_WithValidId_ReturnsCheckpoint()
        {
            // Arrange - Get a valid checkpoint ID
            var locationsResponse = await Client.GetAsync($"{BaseUrl}/locations");
            locationsResponse.EnsureSuccessStatusCode();
            var locationsResult = await locationsResponse.Content.ReadFromJsonAsync<Result<List<LocationModel>>>(JsonOptions);
            var locationId = locationsResult.Data[0].Id;
            
            var checkpointsResponse = await Client.GetAsync($"{BaseUrl}/locations/{locationId}/checkpoints");
            checkpointsResponse.EnsureSuccessStatusCode();
            var checkpointsResult = await checkpointsResponse.Content.ReadFromJsonAsync<Result<List<CheckpointModel>>>(JsonOptions);
            var checkpointId = checkpointsResult.Data[0].Id;
            
            // Act
            var response = await Client.GetAsync($"{BaseUrl}/checkpoints/{checkpointId}");
            
            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<Result<CheckpointModel>>(JsonOptions);
            
            result.Succeeded.Should().BeTrue();
            result.Data.Id.Should().Be(checkpointId);
            result.Data.LocationId.Should().Be(locationId);
            result.Data.Name.Should().NotBeNullOrEmpty();
            result.Data.Latitude.Should().NotBe(0);
            result.Data.Longitude.Should().NotBe(0);
        }

        [Fact]
        public async Task GetCheckpointById_WithInvalidId_ReturnsNotFound()
        {
            // Act
            var response = await Client.GetAsync($"{BaseUrl}/checkpoints/999999");
            
            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task VerifyCheckpoint_WithValidRequest_ReturnsSuccess()
        {
            // Arrange - Get a valid checkpoint ID
            var locationsResponse = await Client.GetAsync($"{BaseUrl}/locations");
            locationsResponse.EnsureSuccessStatusCode();
            var locationsResult = await locationsResponse.Content.ReadFromJsonAsync<Result<List<LocationModel>>>(JsonOptions);
            var locationId = locationsResult.Data[0].Id;
            
            var checkpointsResponse = await Client.GetAsync($"{BaseUrl}/locations/{locationId}/checkpoints");
            checkpointsResponse.EnsureSuccessStatusCode();
            var checkpointsResult = await checkpointsResponse.Content.ReadFromJsonAsync<Result<List<CheckpointModel>>>(JsonOptions);
            var checkpointId = checkpointsResult.Data[0].Id;
            
            var verificationRequest = new CheckpointVerificationRequest
            {
                CheckpointId = checkpointId,
                Latitude = checkpointsResult.Data[0].Latitude + 0.0001, // Close to checkpoint
                Longitude = checkpointsResult.Data[0].Longitude + 0.0001,
                Timestamp = DateTime.UtcNow
            };
            
            // Act
            var response = await Client.PostAsync($"{BaseUrl}/verify", CreateJsonContent(verificationRequest));
            
            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<Result<CheckpointVerificationResponse>>(JsonOptions);
            
            result.Succeeded.Should().BeTrue();
            result.Data.CheckpointId.Should().Be(checkpointId);
            result.Data.IsVerified.Should().BeTrue();
        }

        [Fact]
        public async Task VerifyCheckpoint_WithInvalidCheckpointId_ReturnsNotFound()
        {
            // Arrange
            var verificationRequest = new CheckpointVerificationRequest
            {
                CheckpointId = 999999,
                Latitude = 37.7749,
                Longitude = -122.4194,
                Timestamp = DateTime.UtcNow
            };
            
            // Act
            var response = await Client.PostAsync($"{BaseUrl}/verify", CreateJsonContent(verificationRequest));
            
            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task VerifyCheckpoint_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Arrange
            // Clear the authentication token from the client
            Client.DefaultRequestHeaders.Remove("Authorization");
            
            var verificationRequest = new CheckpointVerificationRequest
            {
                CheckpointId = 1,
                Latitude = 37.7749,
                Longitude = -122.4194,
                Timestamp = DateTime.UtcNow
            };
            
            // Act
            var response = await Client.PostAsync($"{BaseUrl}/verify", CreateJsonContent(verificationRequest));
            
            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
            
            // Restore authentication for subsequent tests
            SetAuthToken(_authToken);
        }

        [Fact]
        public async Task GetPatrolStatus_WithValidLocationId_ReturnsStatus()
        {
            // Arrange - Get a valid location ID
            var locationsResponse = await Client.GetAsync($"{BaseUrl}/locations");
            locationsResponse.EnsureSuccessStatusCode();
            var locationsResult = await locationsResponse.Content.ReadFromJsonAsync<Result<List<LocationModel>>>(JsonOptions);
            var locationId = locationsResult.Data[0].Id;
            
            // Act
            var response = await Client.GetAsync($"{BaseUrl}/locations/{locationId}/status");
            
            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<Result<PatrolStatusModel>>(JsonOptions);
            
            result.Succeeded.Should().BeTrue();
            result.Data.LocationId.Should().Be(locationId);
            result.Data.TotalCheckpoints.Should().BeGreaterOrEqualTo(0);
            result.Data.VerifiedCheckpoints.Should().BeGreaterOrEqualTo(0);
            result.Data.CompletionPercentage.Should().BeInRange(0, 100);
        }

        [Fact]
        public async Task GetPatrolStatus_WithInvalidLocationId_ReturnsNotFound()
        {
            // Act
            var response = await Client.GetAsync($"{BaseUrl}/locations/999999/status");
            
            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetPatrolStatus_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Arrange
            // Clear the authentication token from the client
            Client.DefaultRequestHeaders.Remove("Authorization");
            
            // Get a valid location ID
            var locationsResponse = await Client.GetAsync($"{BaseUrl}/locations");
            locationsResponse.EnsureSuccessStatusCode();
            var locationsResult = await locationsResponse.Content.ReadFromJsonAsync<Result<List<LocationModel>>>(JsonOptions);
            var locationId = locationsResult.Data[0].Id;
            
            // Act
            var response = await Client.GetAsync($"{BaseUrl}/locations/{locationId}/status");
            
            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
            
            // Restore authentication for subsequent tests
            SetAuthToken(_authToken);
        }

        [Fact]
        public async Task GetNearbyCheckpoints_WithValidCoordinates_ReturnsCheckpoints()
        {
            // Arrange - Get location coordinates to use as center point
            var locationsResponse = await Client.GetAsync($"{BaseUrl}/locations");
            locationsResponse.EnsureSuccessStatusCode();
            var locationsResult = await locationsResponse.Content.ReadFromJsonAsync<Result<List<LocationModel>>>(JsonOptions);
            var location = locationsResult.Data[0];
            
            // Act
            var response = await Client.GetAsync($"{BaseUrl}/checkpoints/nearby?latitude={location.Latitude}&longitude={location.Longitude}&radiusInMeters=1000");
            
            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<Result<List<CheckpointModel>>>(JsonOptions);
            
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeEmpty();
        }

        [Fact]
        public async Task GetNearbyCheckpoints_WithInvalidCoordinates_ReturnsBadRequest()
        {
            // Act
            var response = await Client.GetAsync($"{BaseUrl}/checkpoints/nearby?latitude=1000&longitude=1000&radiusInMeters=1000");
            
            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GetUserVerifications_ReturnsVerifications()
        {
            // Act
            var response = await Client.GetAsync($"{BaseUrl}/verifications");
            
            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<Result<List<CheckpointVerification>>>(JsonOptions);
            
            result.Succeeded.Should().BeTrue();
            // Note: The result may be empty if no verifications exist yet, so we don't assert on the content
        }

        [Fact]
        public async Task GetUserVerifications_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Arrange
            // Clear the authentication token from the client
            Client.DefaultRequestHeaders.Remove("Authorization");
            
            // Act
            var response = await Client.GetAsync($"{BaseUrl}/verifications");
            
            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
            
            // Restore authentication for subsequent tests
            SetAuthToken(_authToken);
        }

        [Fact]
        public async Task CompletePatrolFlow_Success()
        {
            // Arrange
            // Get all patrol locations
            var locationsResponse = await Client.GetAsync($"{BaseUrl}/locations");
            locationsResponse.EnsureSuccessStatusCode();
            var locationsResult = await locationsResponse.Content.ReadFromJsonAsync<Result<List<LocationModel>>>(JsonOptions);
            var location = locationsResult.Data[0];
            
            // Get checkpoints for the selected location
            var checkpointsResponse = await Client.GetAsync($"{BaseUrl}/locations/{location.Id}/checkpoints");
            checkpointsResponse.EnsureSuccessStatusCode();
            var checkpointsResult = await checkpointsResponse.Content.ReadFromJsonAsync<Result<List<CheckpointModel>>>(JsonOptions);
            var checkpoints = checkpointsResult.Data;
            
            // Act
            // Verify each checkpoint in the location
            foreach (var checkpoint in checkpoints)
            {
                var verificationRequest = new CheckpointVerificationRequest
                {
                    CheckpointId = checkpoint.Id,
                    Latitude = checkpoint.Latitude,
                    Longitude = checkpoint.Longitude,
                    Timestamp = DateTime.UtcNow
                };
                
                var verifyResponse = await Client.PostAsync($"{BaseUrl}/verify", CreateJsonContent(verificationRequest));
                verifyResponse.EnsureSuccessStatusCode();
            }
            
            // Get patrol status after verifications
            var statusResponse = await Client.GetAsync($"{BaseUrl}/locations/{location.Id}/status");
            statusResponse.EnsureSuccessStatusCode();
            var statusResult = await statusResponse.Content.ReadFromJsonAsync<Result<PatrolStatusModel>>(JsonOptions);
            
            // Assert
            statusResult.Succeeded.Should().BeTrue();
            statusResult.Data.VerifiedCheckpoints.Should().Be(statusResult.Data.TotalCheckpoints);
            statusResult.Data.CompletionPercentage.Should().Be(100);
            statusResult.Data.IsComplete.Should().BeTrue();
        }
    }
}