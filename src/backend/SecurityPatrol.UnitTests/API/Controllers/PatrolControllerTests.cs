using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using SecurityPatrol.API.Controllers;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.Core.Models;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.UnitTests.Helpers;

namespace SecurityPatrol.UnitTests.API.Controllers
{
    public class PatrolControllerTests : TestBase
    {
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly PatrolController _controller;

        public PatrolControllerTests()
        {
            // Initialize mock current user service
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            
            // Setup the mock to return a valid user ID when GetUserId is called
            _mockCurrentUserService.Setup(s => s.GetUserId()).Returns("user1");
            
            // Create the controller with our mock dependencies
            _controller = new PatrolController(
                MockPatrolService.Object,
                _mockCurrentUserService.Object,
                CreateMockLogger<PatrolController>().Object);
        }

        [Fact]
        public async Task GetLocations_ReturnsAllLocations()
        {
            // Arrange
            var testLocations = TestData.GetTestPatrolLocations();
            MockPatrolService.Setup(s => s.GetLocationsAsync())
                .ReturnsAsync(Result.Success<IEnumerable<PatrolLocation>>(testLocations));

            // Act
            var result = await _controller.GetLocations();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeAssignableTo<Result<IEnumerable<PatrolLocation>>>().Subject;
            returnValue.Succeeded.Should().BeTrue();
            returnValue.Data.Should().BeEquivalentTo(testLocations);
        }

        [Fact]
        public async Task GetLocationById_WithValidId_ReturnsLocation()
        {
            // Arrange
            var testLocation = TestData.GetTestPatrolLocationById(1);
            MockPatrolService.Setup(s => s.GetLocationByIdAsync(1))
                .ReturnsAsync(Result.Success(testLocation));

            // Act
            var result = await _controller.GetLocationById(1);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeAssignableTo<Result<PatrolLocation>>().Subject;
            returnValue.Succeeded.Should().BeTrue();
            returnValue.Data.Should().BeEquivalentTo(testLocation);
        }

        [Fact]
        public async Task GetLocationById_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            MockPatrolService.Setup(s => s.GetLocationByIdAsync(999))
                .ReturnsAsync(Result.Failure<PatrolLocation>("Location not found"));

            // Act
            var result = await _controller.GetLocationById(999);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeAssignableTo<Result<PatrolLocation>>().Subject;
            returnValue.Succeeded.Should().BeFalse();
            returnValue.Message.Should().Contain("not found");
        }

        [Fact]
        public async Task GetCheckpointsByLocationId_WithValidId_ReturnsCheckpoints()
        {
            // Arrange
            var testCheckpoints = TestData.GetTestCheckpoints()
                .Where(c => c.LocationId == 1)
                .Select(c => new CheckpointModel
                {
                    Id = c.Id,
                    LocationId = c.LocationId,
                    Name = c.Name,
                    Latitude = c.Latitude,
                    Longitude = c.Longitude,
                    IsVerified = false
                });
            
            MockPatrolService.Setup(s => s.GetCheckpointsByLocationIdAsync(1))
                .ReturnsAsync(Result.Success<IEnumerable<CheckpointModel>>(testCheckpoints));

            // Act
            var result = await _controller.GetCheckpointsByLocationId(1);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeAssignableTo<Result<IEnumerable<CheckpointModel>>>().Subject;
            returnValue.Succeeded.Should().BeTrue();
            returnValue.Data.Should().BeEquivalentTo(testCheckpoints);
        }

        [Fact]
        public async Task GetCheckpointsByLocationId_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            MockPatrolService.Setup(s => s.GetCheckpointsByLocationIdAsync(999))
                .ReturnsAsync(Result.Failure<IEnumerable<CheckpointModel>>("Location not found"));

            // Act
            var result = await _controller.GetCheckpointsByLocationId(999);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeAssignableTo<Result<IEnumerable<CheckpointModel>>>().Subject;
            returnValue.Succeeded.Should().BeFalse();
            returnValue.Message.Should().Contain("not found");
        }

        [Fact]
        public async Task GetCheckpointById_WithValidId_ReturnsCheckpoint()
        {
            // Arrange
            var testCheckpoint = new CheckpointModel
            {
                Id = 1,
                LocationId = 1,
                Name = "Test Checkpoint",
                Latitude = 40.7128,
                Longitude = -74.0060,
                IsVerified = false
            };
            
            MockPatrolService.Setup(s => s.GetCheckpointByIdAsync(1))
                .ReturnsAsync(Result.Success<CheckpointModel>(testCheckpoint));

            // Act
            var result = await _controller.GetCheckpointById(1);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeAssignableTo<Result<CheckpointModel>>().Subject;
            returnValue.Succeeded.Should().BeTrue();
            returnValue.Data.Should().BeEquivalentTo(testCheckpoint);
        }

        [Fact]
        public async Task GetCheckpointById_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            MockPatrolService.Setup(s => s.GetCheckpointByIdAsync(999))
                .ReturnsAsync(Result.Failure<CheckpointModel>("Checkpoint not found"));

            // Act
            var result = await _controller.GetCheckpointById(999);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeAssignableTo<Result<CheckpointModel>>().Subject;
            returnValue.Succeeded.Should().BeFalse();
            returnValue.Message.Should().Contain("not found");
        }

        [Fact]
        public async Task VerifyCheckpoint_WithValidRequest_ReturnsSuccess()
        {
            // Arrange
            var request = new CheckpointVerificationRequest
            {
                CheckpointId = 1,
                Latitude = 40.7128,
                Longitude = -74.0060
            };
            
            _mockCurrentUserService.Setup(s => s.GetUserId()).Returns("user1");
            MockPatrolService.Setup(s => s.VerifyCheckpointAsync(It.IsAny<CheckpointVerificationRequest>(), "user1"))
                .ReturnsAsync(Result.Success(new CheckpointVerificationResponse { Status = "success" }));

            // Act
            var result = await _controller.VerifyCheckpoint(request);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeAssignableTo<Result<CheckpointVerificationResponse>>().Subject;
            returnValue.Succeeded.Should().BeTrue();
            returnValue.Data.Status.Should().Be("success");
            MockPatrolService.Verify(s => s.VerifyCheckpointAsync(It.IsAny<CheckpointVerificationRequest>(), "user1"), Times.Once);
        }

        [Fact]
        public async Task VerifyCheckpoint_WithNoUser_ReturnsUnauthorized()
        {
            // Arrange
            var request = new CheckpointVerificationRequest
            {
                CheckpointId = 1,
                Latitude = 40.7128,
                Longitude = -74.0060
            };
            
            _mockCurrentUserService.Setup(s => s.GetUserId()).Returns((string)null);

            // Act
            var result = await _controller.VerifyCheckpoint(request);

            // Assert
            result.Result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task GetPatrolStatus_WithValidRequest_ReturnsStatus()
        {
            // Arrange
            var patrolStatus = new PatrolStatusModel
            {
                LocationId = 1,
                TotalCheckpoints = 3,
                VerifiedCheckpoints = 2
            };
            
            _mockCurrentUserService.Setup(s => s.GetUserId()).Returns("user1");
            MockPatrolService.Setup(s => s.GetPatrolStatusAsync(1, "user1"))
                .ReturnsAsync(Result.Success(patrolStatus));

            // Act
            var result = await _controller.GetPatrolStatus(1);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeAssignableTo<Result<PatrolStatusModel>>().Subject;
            returnValue.Succeeded.Should().BeTrue();
            returnValue.Data.Should().BeEquivalentTo(patrolStatus);
        }

        [Fact]
        public async Task GetPatrolStatus_WithNoUser_ReturnsUnauthorized()
        {
            // Arrange
            _mockCurrentUserService.Setup(s => s.GetUserId()).Returns((string)null);

            // Act
            var result = await _controller.GetPatrolStatus(1);

            // Assert
            result.Result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task GetNearbyCheckpoints_WithValidParameters_ReturnsCheckpoints()
        {
            // Arrange
            double latitude = 40.7128;
            double longitude = -74.0060;
            double radiusInMeters = 100;
            
            var checkpoints = new List<CheckpointModel>
            {
                new CheckpointModel
                {
                    Id = 1,
                    LocationId = 1,
                    Name = "Test Checkpoint",
                    Latitude = 40.7129,
                    Longitude = -74.0061,
                    IsVerified = false
                }
            };
            
            MockPatrolService.Setup(s => s.GetNearbyCheckpointsAsync(latitude, longitude, radiusInMeters))
                .ReturnsAsync(Result.Success<IEnumerable<CheckpointModel>>(checkpoints));

            // Act
            var result = await _controller.GetNearbyCheckpoints(latitude, longitude, radiusInMeters);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeAssignableTo<Result<IEnumerable<CheckpointModel>>>().Subject;
            returnValue.Succeeded.Should().BeTrue();
            returnValue.Data.Should().BeEquivalentTo(checkpoints);
        }

        [Fact]
        public async Task GetNearbyCheckpoints_WithInvalidParameters_ReturnsBadRequest()
        {
            // Arrange
            double invalidLatitude = -100; // Invalid latitude (less than -90)
            double longitude = -74.0060;
            double radiusInMeters = 100;

            // Act
            var result = await _controller.GetNearbyCheckpoints(invalidLatitude, longitude, radiusInMeters);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task GetUserVerifications_WithValidUser_ReturnsVerifications()
        {
            // Arrange
            var verifications = TestData.GetTestCheckpointVerifications()
                .Where(v => v.UserId == "user1")
                .ToList();
            
            _mockCurrentUserService.Setup(s => s.GetUserId()).Returns("user1");
            MockPatrolService.Setup(s => s.GetUserVerificationsAsync("user1"))
                .ReturnsAsync(Result.Success<IEnumerable<CheckpointVerification>>(verifications));

            // Act
            var result = await _controller.GetUserVerifications();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeAssignableTo<Result<IEnumerable<CheckpointVerification>>>().Subject;
            returnValue.Succeeded.Should().BeTrue();
            returnValue.Data.Should().BeEquivalentTo(verifications);
        }

        [Fact]
        public async Task GetUserVerifications_WithNoUser_ReturnsUnauthorized()
        {
            // Arrange
            _mockCurrentUserService.Setup(s => s.GetUserId()).Returns((string)null);

            // Act
            var result = await _controller.GetUserVerifications();

            // Assert
            result.Result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task GetUserVerificationsByDateRange_WithValidParameters_ReturnsVerifications()
        {
            // Arrange
            DateTime startDate = DateTime.UtcNow.AddDays(-7);
            DateTime endDate = DateTime.UtcNow;
            
            var verifications = TestData.GetTestCheckpointVerifications()
                .Where(v => v.UserId == "user1" && v.Timestamp >= startDate && v.Timestamp <= endDate)
                .ToList();
            
            _mockCurrentUserService.Setup(s => s.GetUserId()).Returns("user1");
            MockPatrolService.Setup(s => s.GetUserVerificationsByDateRangeAsync("user1", startDate, endDate))
                .ReturnsAsync(Result.Success<IEnumerable<CheckpointVerification>>(verifications));

            // Act
            var result = await _controller.GetUserVerificationsByDateRange(startDate, endDate);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeAssignableTo<Result<IEnumerable<CheckpointVerification>>>().Subject;
            returnValue.Succeeded.Should().BeTrue();
            returnValue.Data.Should().BeEquivalentTo(verifications);
        }

        [Fact]
        public async Task GetUserVerificationsByDateRange_WithNoUser_ReturnsUnauthorized()
        {
            // Arrange
            DateTime startDate = DateTime.UtcNow.AddDays(-7);
            DateTime endDate = DateTime.UtcNow;
            
            _mockCurrentUserService.Setup(s => s.GetUserId()).Returns((string)null);

            // Act
            var result = await _controller.GetUserVerificationsByDateRange(startDate, endDate);

            // Assert
            result.Result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task GetUserVerificationsByDateRange_WithInvalidDateRange_ReturnsBadRequest()
        {
            // Arrange
            DateTime endDate = DateTime.UtcNow.AddDays(-7); // End date before start date
            DateTime startDate = DateTime.UtcNow;
            
            _mockCurrentUserService.Setup(s => s.GetUserId()).Returns("user1");

            // Act
            var result = await _controller.GetUserVerificationsByDateRange(startDate, endDate);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task GetNearbyLocations_WithValidParameters_ReturnsLocations()
        {
            // Arrange
            double latitude = 40.7128;
            double longitude = -74.0060;
            double radiusInMeters = 1000;
            
            var locations = new List<PatrolLocation>
            {
                new PatrolLocation
                {
                    Id = 1,
                    Name = "Test Location",
                    Latitude = 40.7129,
                    Longitude = -74.0061
                }
            };
            
            MockPatrolService.Setup(s => s.GetNearbyLocationsAsync(latitude, longitude, radiusInMeters))
                .ReturnsAsync(Result.Success<IEnumerable<PatrolLocation>>(locations));

            // Act
            var result = await _controller.GetNearbyLocations(latitude, longitude, radiusInMeters);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeAssignableTo<Result<IEnumerable<PatrolLocation>>>().Subject;
            returnValue.Succeeded.Should().BeTrue();
            returnValue.Data.Should().BeEquivalentTo(locations);
        }

        [Fact]
        public async Task GetNearbyLocations_WithInvalidParameters_ReturnsBadRequest()
        {
            // Arrange
            double invalidLatitude = -100; // Invalid latitude (less than -90)
            double longitude = -74.0060;
            double radiusInMeters = 1000;

            // Act
            var result = await _controller.GetNearbyLocations(invalidLatitude, longitude, radiusInMeters);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task IsCheckpointVerified_WithValidParameters_ReturnsStatus()
        {
            // Arrange
            int checkpointId = 1;
            
            _mockCurrentUserService.Setup(s => s.GetUserId()).Returns("user1");
            MockPatrolService.Setup(s => s.IsCheckpointVerifiedAsync(checkpointId, "user1"))
                .ReturnsAsync(Result.Success(true));

            // Act
            var result = await _controller.IsCheckpointVerified(checkpointId);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeAssignableTo<Result<bool>>().Subject;
            returnValue.Succeeded.Should().BeTrue();
            returnValue.Data.Should().BeTrue();
        }

        [Fact]
        public async Task IsCheckpointVerified_WithNoUser_ReturnsUnauthorized()
        {
            // Arrange
            int checkpointId = 1;
            
            _mockCurrentUserService.Setup(s => s.GetUserId()).Returns((string)null);

            // Act
            var result = await _controller.IsCheckpointVerified(checkpointId);

            // Assert
            result.Result.Should().BeOfType<UnauthorizedObjectResult>();
        }
    }
}