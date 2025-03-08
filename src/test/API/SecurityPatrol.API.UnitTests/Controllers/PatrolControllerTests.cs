using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using FluentAssertions;
using SecurityPatrol.API.UnitTests.Setup;
using SecurityPatrol.API.Controllers;
using SecurityPatrol.Core.Models;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Core.Interfaces;

namespace SecurityPatrol.API.UnitTests.Controllers
{
    /// <summary>
    /// Contains unit tests for the PatrolController class to verify the behavior of patrol management API endpoints.
    /// </summary>
    public class PatrolControllerTests : TestBase
    {
        private readonly string TestUserId = "test-user-123";
        private readonly Mock<ICurrentUserService> MockCurrentUserService;

        /// <summary>
        /// Initializes a new instance of the PatrolControllerTests class with test setup
        /// </summary>
        public PatrolControllerTests()
        {
            // Create and configure the current user service mock
            MockCurrentUserService = new Mock<ICurrentUserService>();
            MockCurrentUserService.Setup(m => m.GetUserId()).Returns(TestUserId);
        }

        /// <summary>
        /// Tests that GetLocations endpoint returns all patrol locations
        /// </summary>
        [Fact]
        public async Task GetLocations_ReturnsAllLocations()
        {
            // Arrange: Create a list of test PatrolLocation entities
            var testLocations = new List<PatrolLocation>
            {
                new PatrolLocation { Id = 1, Name = "Location 1", Latitude = 34.0522, Longitude = -118.2437 },
                new PatrolLocation { Id = 2, Name = "Location 2", Latitude = 34.0548, Longitude = -118.2500 }
            };
            
            var successResult = Result<IEnumerable<PatrolLocation>>.Success(testLocations);
            MockPatrolService.Setup(s => s.GetLocationsAsync()).ReturnsAsync(successResult);
            
            var controller = new PatrolController(
                MockPatrolService.Object,
                MockCurrentUserService.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<PatrolController>>());

            // Act: Call controller.GetLocations()
            var result = await controller.GetLocations();

            // Assert: Verify the result is a successful Result<IEnumerable<PatrolLocation>> containing the test locations
            result.Should().NotBeNull();
            result.Value.Should().BeOfType<Result<IEnumerable<PatrolLocation>>>();
            result.Value.Should().NotBeNull();
            result.Value.Succeeded.Should().BeTrue();
            result.Value.Data.Should().BeEquivalentTo(testLocations);
        }

        /// <summary>
        /// Tests that GetLocationById endpoint returns the correct patrol location when given a valid ID
        /// </summary>
        [Fact]
        public async Task GetLocationById_WithValidId_ReturnsLocation()
        {
            // Arrange: Create a test PatrolLocation entity with a specific ID
            int id = 1;
            var testLocation = new PatrolLocation { 
                Id = id, 
                Name = "Test Location", 
                Latitude = 34.0522, 
                Longitude = -118.2437 
            };
            
            var successResult = Result<PatrolLocation>.Success(testLocation);
            MockPatrolService.Setup(s => s.GetLocationByIdAsync(id)).ReturnsAsync(successResult);
            
            var controller = new PatrolController(
                MockPatrolService.Object,
                MockCurrentUserService.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<PatrolController>>());

            // Act: Call controller.GetLocationById(id)
            var result = await controller.GetLocationById(id);

            // Assert: Verify the result is a successful Result<PatrolLocation> containing the test location
            result.Should().NotBeNull();
            result.Value.Should().BeOfType<Result<PatrolLocation>>();
            result.Value.Should().NotBeNull();
            result.Value.Succeeded.Should().BeTrue();
            result.Value.Data.Should().BeEquivalentTo(testLocation);
        }

        /// <summary>
        /// Tests that GetLocationById endpoint returns a not found result when given an invalid ID
        /// </summary>
        [Fact]
        public async Task GetLocationById_WithInvalidId_ReturnsNotFound()
        {
            // Arrange: Setup MockPatrolService to return a failure result for an invalid ID
            int invalidId = 999;
            
            var failureResult = Result<PatrolLocation>.Failure("Location not found");
            MockPatrolService.Setup(s => s.GetLocationByIdAsync(invalidId)).ReturnsAsync(failureResult);
            
            var controller = new PatrolController(
                MockPatrolService.Object,
                MockCurrentUserService.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<PatrolController>>());

            // Act: Call controller.GetLocationById(invalidId)
            var result = await controller.GetLocationById(invalidId);

            // Assert: Verify the result is a failure Result<PatrolLocation>
            result.Should().NotBeNull();
            result.Value.Should().BeOfType<Result<PatrolLocation>>();
            result.Value.Should().NotBeNull();
            result.Value.Succeeded.Should().BeFalse();
            result.Value.Message.Should().Be("Location not found");
        }

        /// <summary>
        /// Tests that GetCheckpointsByLocationId endpoint returns checkpoints for a valid location ID
        /// </summary>
        [Fact]
        public async Task GetCheckpointsByLocationId_WithValidId_ReturnsCheckpoints()
        {
            // Arrange: Create a list of test CheckpointModel objects for a specific location ID
            int locationId = 1;
            var testCheckpoints = new List<CheckpointModel>
            {
                new CheckpointModel { Id = 1, LocationId = locationId, Name = "Checkpoint 1", Latitude = 34.0522, Longitude = -118.2437 },
                new CheckpointModel { Id = 2, LocationId = locationId, Name = "Checkpoint 2", Latitude = 34.0548, Longitude = -118.2500 }
            };
            
            var successResult = Result<IEnumerable<CheckpointModel>>.Success(testCheckpoints);
            MockPatrolService.Setup(s => s.GetCheckpointsByLocationIdAsync(locationId)).ReturnsAsync(successResult);
            
            var controller = new PatrolController(
                MockPatrolService.Object,
                MockCurrentUserService.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<PatrolController>>());

            // Act: Call controller.GetCheckpointsByLocationId(locationId)
            var result = await controller.GetCheckpointsByLocationId(locationId);

            // Assert: Verify the result is a successful Result<IEnumerable<CheckpointModel>> containing the test checkpoints
            result.Should().NotBeNull();
            result.Value.Should().BeOfType<Result<IEnumerable<CheckpointModel>>>();
            result.Value.Should().NotBeNull();
            result.Value.Succeeded.Should().BeTrue();
            result.Value.Data.Should().BeEquivalentTo(testCheckpoints);
        }

        /// <summary>
        /// Tests that GetCheckpointsByLocationId endpoint returns a not found result when given an invalid location ID
        /// </summary>
        [Fact]
        public async Task GetCheckpointsByLocationId_WithInvalidId_ReturnsNotFound()
        {
            // Arrange: Setup MockPatrolService to return a failure result for an invalid location ID
            int invalidLocationId = 999;
            
            var failureResult = Result<IEnumerable<CheckpointModel>>.Failure("Location not found");
            MockPatrolService.Setup(s => s.GetCheckpointsByLocationIdAsync(invalidLocationId)).ReturnsAsync(failureResult);
            
            var controller = new PatrolController(
                MockPatrolService.Object,
                MockCurrentUserService.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<PatrolController>>());

            // Act: Call controller.GetCheckpointsByLocationId(invalidLocationId)
            var result = await controller.GetCheckpointsByLocationId(invalidLocationId);

            // Assert: Verify the result is a failure Result<IEnumerable<CheckpointModel>>
            result.Should().NotBeNull();
            result.Value.Should().BeOfType<Result<IEnumerable<CheckpointModel>>>();
            result.Value.Should().NotBeNull();
            result.Value.Succeeded.Should().BeFalse();
            result.Value.Message.Should().Be("Location not found");
        }

        /// <summary>
        /// Tests that GetCheckpointById endpoint returns the correct checkpoint when given a valid ID
        /// </summary>
        [Fact]
        public async Task GetCheckpointById_WithValidId_ReturnsCheckpoint()
        {
            // Arrange: Create a test CheckpointModel with a specific ID
            int id = 1;
            var testCheckpoint = new CheckpointModel { 
                Id = id, 
                LocationId = 1, 
                Name = "Test Checkpoint", 
                Latitude = 34.0522, 
                Longitude = -118.2437 
            };
            
            var successResult = Result<CheckpointModel>.Success(testCheckpoint);
            MockPatrolService.Setup(s => s.GetCheckpointByIdAsync(id)).ReturnsAsync(successResult);
            
            var controller = new PatrolController(
                MockPatrolService.Object,
                MockCurrentUserService.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<PatrolController>>());

            // Act: Call controller.GetCheckpointById(id)
            var result = await controller.GetCheckpointById(id);

            // Assert: Verify the result is a successful Result<CheckpointModel> containing the test checkpoint
            result.Should().NotBeNull();
            result.Value.Should().BeOfType<Result<CheckpointModel>>();
            result.Value.Should().NotBeNull();
            result.Value.Succeeded.Should().BeTrue();
            result.Value.Data.Should().BeEquivalentTo(testCheckpoint);
        }

        /// <summary>
        /// Tests that GetCheckpointById endpoint returns a not found result when given an invalid ID
        /// </summary>
        [Fact]
        public async Task GetCheckpointById_WithInvalidId_ReturnsNotFound()
        {
            // Arrange: Setup MockPatrolService to return a failure result for an invalid ID
            int invalidId = 999;
            
            var failureResult = Result<CheckpointModel>.Failure("Checkpoint not found");
            MockPatrolService.Setup(s => s.GetCheckpointByIdAsync(invalidId)).ReturnsAsync(failureResult);
            
            var controller = new PatrolController(
                MockPatrolService.Object,
                MockCurrentUserService.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<PatrolController>>());

            // Act: Call controller.GetCheckpointById(invalidId)
            var result = await controller.GetCheckpointById(invalidId);

            // Assert: Verify the result is a failure Result<CheckpointModel>
            result.Should().NotBeNull();
            result.Value.Should().BeOfType<Result<CheckpointModel>>();
            result.Value.Should().NotBeNull();
            result.Value.Succeeded.Should().BeFalse();
            result.Value.Message.Should().Be("Checkpoint not found");
        }

        /// <summary>
        /// Tests that VerifyCheckpoint endpoint returns a successful result when given a valid verification request
        /// </summary>
        [Fact]
        public async Task VerifyCheckpoint_WithValidRequest_ReturnsSuccess()
        {
            // Arrange: Create a valid CheckpointVerificationRequest
            var request = new CheckpointVerificationRequest
            {
                CheckpointId = 1,
                Timestamp = DateTime.UtcNow,
                Location = new LocationModel { Latitude = 34.0522, Longitude = -118.2437 }
            };
            
            // Arrange: Create a CheckpointVerificationResponse for the expected result
            var response = new CheckpointVerificationResponse
            {
                Status = "Success",
                CheckpointId = 1,
                VerificationId = "verification-123"
            };
            
            var successResult = Result<CheckpointVerificationResponse>.Success(response);
            MockPatrolService.Setup(s => s.VerifyCheckpointAsync(request, TestUserId)).ReturnsAsync(successResult);
            
            var controller = new PatrolController(
                MockPatrolService.Object,
                MockCurrentUserService.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<PatrolController>>());

            // Act: Call controller.VerifyCheckpoint(request)
            var result = await controller.VerifyCheckpoint(request);

            // Assert: Verify the result is a successful Result<CheckpointVerificationResponse> containing the expected response
            result.Should().NotBeNull();
            result.Value.Should().BeOfType<Result<CheckpointVerificationResponse>>();
            result.Value.Should().NotBeNull();
            result.Value.Succeeded.Should().BeTrue();
            result.Value.Data.Should().BeEquivalentTo(response);
        }

        /// <summary>
        /// Tests that VerifyCheckpoint endpoint returns unauthorized when no user is authenticated
        /// </summary>
        [Fact]
        public async Task VerifyCheckpoint_WithNoUser_ReturnsUnauthorized()
        {
            // Arrange: Create a valid CheckpointVerificationRequest
            var request = new CheckpointVerificationRequest
            {
                CheckpointId = 1,
                Timestamp = DateTime.UtcNow,
                Location = new LocationModel { Latitude = 34.0522, Longitude = -118.2437 }
            };
            
            // Arrange: Setup MockCurrentUserService to return null for GetUserId()
            MockCurrentUserService.Setup(m => m.GetUserId()).Returns((string)null);
            
            var controller = new PatrolController(
                MockPatrolService.Object,
                MockCurrentUserService.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<PatrolController>>());

            // Act: Call controller.VerifyCheckpoint(request)
            var result = await controller.VerifyCheckpoint(request);

            // Assert: Verify the result is an UnauthorizedResult
            result.Result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        /// <summary>
        /// Tests that VerifyCheckpoint endpoint returns a bad request result when given an invalid verification request
        /// </summary>
        [Fact]
        public async Task VerifyCheckpoint_WithInvalidRequest_ReturnsBadRequest()
        {
            // Arrange: Create an invalid CheckpointVerificationRequest
            var request = new CheckpointVerificationRequest
            {
                // Invalid request with missing required data
                CheckpointId = 0
            };
            
            var failureResult = Result<CheckpointVerificationResponse>.Failure("Invalid checkpoint ID");
            MockPatrolService.Setup(s => s.VerifyCheckpointAsync(request, TestUserId)).ReturnsAsync(failureResult);
            
            var controller = new PatrolController(
                MockPatrolService.Object,
                MockCurrentUserService.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<PatrolController>>());

            // Act: Call controller.VerifyCheckpoint(request)
            var result = await controller.VerifyCheckpoint(request);

            // Assert: Verify the result is a failure Result<CheckpointVerificationResponse>
            result.Should().NotBeNull();
            result.Value.Should().BeOfType<Result<CheckpointVerificationResponse>>();
            result.Value.Should().NotBeNull();
            result.Value.Succeeded.Should().BeFalse();
            result.Value.Message.Should().Be("Invalid checkpoint ID");
        }

        /// <summary>
        /// Tests that GetPatrolStatus endpoint returns the correct patrol status for a valid location ID
        /// </summary>
        [Fact]
        public async Task GetPatrolStatus_WithValidRequest_ReturnsStatus()
        {
            // Arrange: Create a test PatrolStatusModel
            int locationId = 1;
            var status = new PatrolStatusModel
            {
                LocationId = locationId,
                TotalCheckpoints = 5,
                VerifiedCheckpoints = 3
            };
            
            var successResult = Result<PatrolStatusModel>.Success(status);
            MockPatrolService.Setup(s => s.GetPatrolStatusAsync(locationId, TestUserId)).ReturnsAsync(successResult);
            
            var controller = new PatrolController(
                MockPatrolService.Object,
                MockCurrentUserService.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<PatrolController>>());

            // Act: Call controller.GetPatrolStatus(locationId)
            var result = await controller.GetPatrolStatus(locationId);

            // Assert: Verify the result is a successful Result<PatrolStatusModel> containing the test status
            result.Should().NotBeNull();
            result.Value.Should().BeOfType<Result<PatrolStatusModel>>();
            result.Value.Should().NotBeNull();
            result.Value.Succeeded.Should().BeTrue();
            result.Value.Data.Should().BeEquivalentTo(status);
        }

        /// <summary>
        /// Tests that GetPatrolStatus endpoint returns unauthorized when no user is authenticated
        /// </summary>
        [Fact]
        public async Task GetPatrolStatus_WithNoUser_ReturnsUnauthorized()
        {
            // Arrange: Setup MockCurrentUserService to return null for GetUserId()
            int locationId = 1;
            MockCurrentUserService.Setup(m => m.GetUserId()).Returns((string)null);
            
            var controller = new PatrolController(
                MockPatrolService.Object,
                MockCurrentUserService.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<PatrolController>>());

            // Act: Call controller.GetPatrolStatus(locationId)
            var result = await controller.GetPatrolStatus(locationId);

            // Assert: Verify the result is an UnauthorizedResult
            result.Result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        /// <summary>
        /// Tests that GetNearbyCheckpoints endpoint returns checkpoints near the specified coordinates
        /// </summary>
        [Fact]
        public async Task GetNearbyCheckpoints_WithValidParameters_ReturnsCheckpoints()
        {
            // Arrange: Create a list of test CheckpointModel objects
            double latitude = 34.0522;
            double longitude = -118.2437;
            double radiusInMeters = 100;
            
            var testCheckpoints = new List<CheckpointModel>
            {
                new CheckpointModel { Id = 1, LocationId = 1, Name = "Nearby Checkpoint 1", Latitude = 34.0523, Longitude = -118.2438 },
                new CheckpointModel { Id = 2, LocationId = 1, Name = "Nearby Checkpoint 2", Latitude = 34.0524, Longitude = -118.2439 }
            };
            
            var successResult = Result<IEnumerable<CheckpointModel>>.Success(testCheckpoints);
            MockPatrolService.Setup(s => s.GetNearbyCheckpointsAsync(latitude, longitude, radiusInMeters)).ReturnsAsync(successResult);
            
            var controller = new PatrolController(
                MockPatrolService.Object,
                MockCurrentUserService.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<PatrolController>>());

            // Act: Call controller.GetNearbyCheckpoints(latitude, longitude, radiusInMeters)
            var result = await controller.GetNearbyCheckpoints(latitude, longitude, radiusInMeters);

            // Assert: Verify the result is a successful Result<IEnumerable<CheckpointModel>> containing the test checkpoints
            result.Should().NotBeNull();
            result.Value.Should().BeOfType<Result<IEnumerable<CheckpointModel>>>();
            result.Value.Should().NotBeNull();
            result.Value.Succeeded.Should().BeTrue();
            result.Value.Data.Should().BeEquivalentTo(testCheckpoints);
        }

        /// <summary>
        /// Tests that GetNearbyCheckpoints endpoint returns a bad request result when given invalid coordinates
        /// </summary>
        [Fact]
        public async Task GetNearbyCheckpoints_WithInvalidParameters_ReturnsBadRequest()
        {
            // Arrange: Create a PatrolController instance with the mocked services
            double invalidLatitude = 100; // Invalid latitude (beyond -90 to 90 range)
            double invalidLongitude = 200; // Invalid longitude (beyond -180 to 180 range)
            double invalidRadius = -10; // Invalid radius (must be positive)
            
            var controller = new PatrolController(
                MockPatrolService.Object,
                MockCurrentUserService.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<PatrolController>>());

            // Act: Call controller.GetNearbyCheckpoints(invalidLatitude, invalidLongitude, invalidRadius)
            var result = await controller.GetNearbyCheckpoints(invalidLatitude, invalidLongitude, invalidRadius);

            // Assert: Verify the result is a BadRequestObjectResult
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        /// <summary>
        /// Tests that GetUserVerifications endpoint returns verifications for the authenticated user
        /// </summary>
        [Fact]
        public async Task GetUserVerifications_WithValidUser_ReturnsVerifications()
        {
            // Arrange: Create a list of test CheckpointVerification entities
            var testVerifications = new List<CheckpointVerification>
            {
                new CheckpointVerification { Id = 1, UserId = TestUserId, CheckpointId = 1, Timestamp = DateTime.UtcNow.AddHours(-2) },
                new CheckpointVerification { Id = 2, UserId = TestUserId, CheckpointId = 2, Timestamp = DateTime.UtcNow.AddHours(-1) }
            };
            
            var successResult = Result<IEnumerable<CheckpointVerification>>.Success(testVerifications);
            MockPatrolService.Setup(s => s.GetUserVerificationsAsync(TestUserId)).ReturnsAsync(successResult);
            
            var controller = new PatrolController(
                MockPatrolService.Object,
                MockCurrentUserService.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<PatrolController>>());

            // Act: Call controller.GetUserVerifications()
            var result = await controller.GetUserVerifications();

            // Assert: Verify the result is a successful Result<IEnumerable<CheckpointVerification>> containing the test verifications
            result.Should().NotBeNull();
            result.Value.Should().BeOfType<Result<IEnumerable<CheckpointVerification>>>();
            result.Value.Should().NotBeNull();
            result.Value.Succeeded.Should().BeTrue();
            result.Value.Data.Should().BeEquivalentTo(testVerifications);
        }

        /// <summary>
        /// Tests that GetUserVerifications endpoint returns unauthorized when no user is authenticated
        /// </summary>
        [Fact]
        public async Task GetUserVerifications_WithNoUser_ReturnsUnauthorized()
        {
            // Arrange: Setup MockCurrentUserService to return null for GetUserId()
            MockCurrentUserService.Setup(m => m.GetUserId()).Returns((string)null);
            
            var controller = new PatrolController(
                MockPatrolService.Object,
                MockCurrentUserService.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<PatrolController>>());

            // Act: Call controller.GetUserVerifications()
            var result = await controller.GetUserVerifications();

            // Assert: Verify the result is an UnauthorizedResult
            result.Result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        /// <summary>
        /// Tests that GetUserVerificationsByDateRange endpoint returns verifications within the specified date range
        /// </summary>
        [Fact]
        public async Task GetUserVerificationsByDateRange_WithValidParameters_ReturnsVerifications()
        {
            // Arrange: Create a list of test CheckpointVerification entities
            DateTime startDate = DateTime.UtcNow.AddDays(-7);
            DateTime endDate = DateTime.UtcNow;
            
            var testVerifications = new List<CheckpointVerification>
            {
                new CheckpointVerification { Id = 1, UserId = TestUserId, CheckpointId = 1, Timestamp = DateTime.UtcNow.AddDays(-5) },
                new CheckpointVerification { Id = 2, UserId = TestUserId, CheckpointId = 2, Timestamp = DateTime.UtcNow.AddDays(-3) }
            };
            
            var successResult = Result<IEnumerable<CheckpointVerification>>.Success(testVerifications);
            MockPatrolService.Setup(s => s.GetUserVerificationsByDateRangeAsync(TestUserId, startDate, endDate)).ReturnsAsync(successResult);
            
            var controller = new PatrolController(
                MockPatrolService.Object,
                MockCurrentUserService.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<PatrolController>>());

            // Act: Call controller.GetUserVerificationsByDateRange(startDate, endDate)
            var result = await controller.GetUserVerificationsByDateRange(startDate, endDate);

            // Assert: Verify the result is a successful Result<IEnumerable<CheckpointVerification>> containing the test verifications
            result.Should().NotBeNull();
            result.Value.Should().BeOfType<Result<IEnumerable<CheckpointVerification>>>();
            result.Value.Should().NotBeNull();
            result.Value.Succeeded.Should().BeTrue();
            result.Value.Data.Should().BeEquivalentTo(testVerifications);
        }

        /// <summary>
        /// Tests that GetUserVerificationsByDateRange endpoint returns unauthorized when no user is authenticated
        /// </summary>
        [Fact]
        public async Task GetUserVerificationsByDateRange_WithNoUser_ReturnsUnauthorized()
        {
            // Arrange: Setup MockCurrentUserService to return null for GetUserId()
            DateTime startDate = DateTime.UtcNow.AddDays(-7);
            DateTime endDate = DateTime.UtcNow;
            
            MockCurrentUserService.Setup(m => m.GetUserId()).Returns((string)null);
            
            var controller = new PatrolController(
                MockPatrolService.Object,
                MockCurrentUserService.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<PatrolController>>());

            // Act: Call controller.GetUserVerificationsByDateRange(startDate, endDate)
            var result = await controller.GetUserVerificationsByDateRange(startDate, endDate);

            // Assert: Verify the result is an UnauthorizedResult
            result.Result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        /// <summary>
        /// Tests that GetUserVerificationsByDateRange endpoint returns a bad request result when given an invalid date range
        /// </summary>
        [Fact]
        public async Task GetUserVerificationsByDateRange_WithInvalidDateRange_ReturnsBadRequest()
        {
            // Arrange: Create a PatrolController instance with the mocked services
            // Invalid date range where end date is before start date
            DateTime endDate = DateTime.UtcNow.AddDays(-7);
            DateTime startDate = DateTime.UtcNow;
            
            var controller = new PatrolController(
                MockPatrolService.Object,
                MockCurrentUserService.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<PatrolController>>());

            // Act: Call controller.GetUserVerificationsByDateRange(endDate, startDate) with endDate before startDate
            var result = await controller.GetUserVerificationsByDateRange(startDate, endDate);

            // Assert: Verify the result is a BadRequestObjectResult
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        /// <summary>
        /// Tests that GetNearbyLocations endpoint returns locations near the specified coordinates
        /// </summary>
        [Fact]
        public async Task GetNearbyLocations_WithValidParameters_ReturnsLocations()
        {
            // Arrange: Create a list of test PatrolLocation entities
            double latitude = 34.0522;
            double longitude = -118.2437;
            double radiusInMeters = 100;
            
            var testLocations = new List<PatrolLocation>
            {
                new PatrolLocation { Id = 1, Name = "Nearby Location 1", Latitude = 34.0523, Longitude = -118.2438 },
                new PatrolLocation { Id = 2, Name = "Nearby Location 2", Latitude = 34.0524, Longitude = -118.2439 }
            };
            
            var successResult = Result<IEnumerable<PatrolLocation>>.Success(testLocations);
            MockPatrolService.Setup(s => s.GetNearbyLocationsAsync(latitude, longitude, radiusInMeters)).ReturnsAsync(successResult);
            
            var controller = new PatrolController(
                MockPatrolService.Object,
                MockCurrentUserService.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<PatrolController>>());

            // Act: Call controller.GetNearbyLocations(latitude, longitude, radiusInMeters)
            var result = await controller.GetNearbyLocations(latitude, longitude, radiusInMeters);

            // Assert: Verify the result is a successful Result<IEnumerable<PatrolLocation>> containing the test locations
            result.Should().NotBeNull();
            result.Value.Should().BeOfType<Result<IEnumerable<PatrolLocation>>>();
            result.Value.Should().NotBeNull();
            result.Value.Succeeded.Should().BeTrue();
            result.Value.Data.Should().BeEquivalentTo(testLocations);
        }

        /// <summary>
        /// Tests that GetNearbyLocations endpoint returns a bad request result when given invalid coordinates
        /// </summary>
        [Fact]
        public async Task GetNearbyLocations_WithInvalidParameters_ReturnsBadRequest()
        {
            // Arrange: Create a PatrolController instance with the mocked services
            double invalidLatitude = 100; // Invalid latitude (beyond -90 to 90 range)
            double invalidLongitude = 200; // Invalid longitude (beyond -180 to 180 range)
            double invalidRadius = -10; // Invalid radius (must be positive)
            
            var controller = new PatrolController(
                MockPatrolService.Object,
                MockCurrentUserService.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<PatrolController>>());

            // Act: Call controller.GetNearbyLocations(invalidLatitude, invalidLongitude, invalidRadius)
            var result = await controller.GetNearbyLocations(invalidLatitude, invalidLongitude, invalidRadius);

            // Assert: Verify the result is a BadRequestObjectResult
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        /// <summary>
        /// Tests that IsCheckpointVerified endpoint returns the verification status for a checkpoint
        /// </summary>
        [Fact]
        public async Task IsCheckpointVerified_WithValidParameters_ReturnsStatus()
        {
            // Arrange: Setup MockPatrolService to return a successful result with a boolean value
            int checkpointId = 1;
            bool isVerified = true;
            
            var successResult = Result<bool>.Success(isVerified);
            MockPatrolService.Setup(s => s.IsCheckpointVerifiedAsync(checkpointId, TestUserId)).ReturnsAsync(successResult);
            
            var controller = new PatrolController(
                MockPatrolService.Object,
                MockCurrentUserService.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<PatrolController>>());

            // Act: Call controller.IsCheckpointVerified(checkpointId)
            var result = await controller.IsCheckpointVerified(checkpointId);

            // Assert: Verify the result is a successful Result<bool> containing the expected verification status
            result.Should().NotBeNull();
            result.Value.Should().BeOfType<Result<bool>>();
            result.Value.Should().NotBeNull();
            result.Value.Succeeded.Should().BeTrue();
            result.Value.Data.Should().Be(isVerified);
        }

        /// <summary>
        /// Tests that IsCheckpointVerified endpoint returns unauthorized when no user is authenticated
        /// </summary>
        [Fact]
        public async Task IsCheckpointVerified_WithNoUser_ReturnsUnauthorized()
        {
            // Arrange: Setup MockCurrentUserService to return null for GetUserId()
            int checkpointId = 1;
            MockCurrentUserService.Setup(m => m.GetUserId()).Returns((string)null);
            
            var controller = new PatrolController(
                MockPatrolService.Object,
                MockCurrentUserService.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<PatrolController>>());

            // Act: Call controller.IsCheckpointVerified(checkpointId)
            var result = await controller.IsCheckpointVerified(checkpointId);

            // Assert: Verify the result is an UnauthorizedResult
            result.Result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        /// <summary>
        /// Tests that IsCheckpointVerified endpoint returns a not found result when given an invalid checkpoint ID
        /// </summary>
        [Fact]
        public async Task IsCheckpointVerified_WithInvalidCheckpointId_ReturnsNotFound()
        {
            // Arrange: Setup MockPatrolService to return a failure result for an invalid checkpoint ID
            int invalidCheckpointId = 999;
            
            var failureResult = Result<bool>.Failure("Checkpoint not found");
            MockPatrolService.Setup(s => s.IsCheckpointVerifiedAsync(invalidCheckpointId, TestUserId)).ReturnsAsync(failureResult);
            
            var controller = new PatrolController(
                MockPatrolService.Object,
                MockCurrentUserService.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<PatrolController>>());

            // Act: Call controller.IsCheckpointVerified(invalidCheckpointId)
            var result = await controller.IsCheckpointVerified(invalidCheckpointId);

            // Assert: Verify the result is a failure Result<bool>
            result.Should().NotBeNull();
            result.Value.Should().BeOfType<Result<bool>>();
            result.Value.Should().NotBeNull();
            result.Value.Succeeded.Should().BeFalse();
            result.Value.Message.Should().Be("Checkpoint not found");
        }
    }
}