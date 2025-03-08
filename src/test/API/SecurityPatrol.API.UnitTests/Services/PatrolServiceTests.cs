using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Xunit;
using FluentAssertions;
using SecurityPatrol.API.UnitTests.Setup;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Core.Models;
using SecurityPatrol.Application.Services;

namespace SecurityPatrol.API.UnitTests.Services
{
    public class PatrolServiceTests : TestBase
    {
        private Mock<IPatrolLocationRepository> _mockPatrolLocationRepository;
        private Mock<ICheckpointRepository> _mockCheckpointRepository;
        private Mock<ICheckpointVerificationRepository> _mockVerificationRepository;
        private Mock<ICurrentUserService> _mockCurrentUserService;
        private Mock<IDateTime> _mockDateTime;
        private PatrolService _patrolService;

        public PatrolServiceTests()
        {
            // Initialize mocks
            _mockPatrolLocationRepository = new Mock<IPatrolLocationRepository>();
            _mockCheckpointRepository = new Mock<ICheckpointRepository>();
            _mockVerificationRepository = new Mock<ICheckpointVerificationRepository>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockDateTime = new Mock<IDateTime>();

            // Set up default behaviors for mockDateTime
            var fixedDate = DateTime.UtcNow;
            _mockDateTime.Setup(d => d.Now).Returns(fixedDate);
            _mockDateTime.Setup(d => d.UtcNow).Returns(fixedDate);

            // Create the service instance
            _patrolService = new PatrolService(
                _mockPatrolLocationRepository.Object,
                _mockCheckpointRepository.Object,
                _mockVerificationRepository.Object,
                _mockCurrentUserService.Object,
                _mockDateTime.Object);
        }

        [Fact]
        public async Task GetLocationsAsync_ReturnsAllLocations_WhenLocationsExist()
        {
            // Arrange
            var testLocations = new List<PatrolLocation>
            {
                new PatrolLocation { Id = 1, Name = "Location 1" },
                new PatrolLocation { Id = 2, Name = "Location 2" }
            };
            _mockPatrolLocationRepository.Setup(r => r.GetAllAsync())
                .ReturnsAsync(testLocations);

            // Act
            var result = await _patrolService.GetLocationsAsync();

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().BeEquivalentTo(testLocations);
            _mockPatrolLocationRepository.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetLocationsAsync_ReturnsFailure_WhenNoLocationsExist()
        {
            // Arrange
            _mockPatrolLocationRepository.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<PatrolLocation>());

            // Act
            var result = await _patrolService.GetLocationsAsync();

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("No patrol locations found");
            _mockPatrolLocationRepository.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetLocationByIdAsync_ReturnsLocation_WhenLocationExists()
        {
            // Arrange
            int testId = 1;
            var testLocation = new PatrolLocation { Id = testId, Name = "Test Location" };
            _mockPatrolLocationRepository.Setup(r => r.GetByIdAsync(testId))
                .ReturnsAsync(testLocation);

            // Act
            var result = await _patrolService.GetLocationByIdAsync(testId);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().BeEquivalentTo(testLocation);
            _mockPatrolLocationRepository.Verify(r => r.GetByIdAsync(testId), Times.Once);
        }

        [Fact]
        public async Task GetLocationByIdAsync_ReturnsFailure_WhenLocationDoesNotExist()
        {
            // Arrange
            int testId = 1;
            _mockPatrolLocationRepository.Setup(r => r.GetByIdAsync(testId))
                .ReturnsAsync((PatrolLocation)null);

            // Act
            var result = await _patrolService.GetLocationByIdAsync(testId);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("not found");
            _mockPatrolLocationRepository.Verify(r => r.GetByIdAsync(testId), Times.Once);
        }

        [Fact]
        public async Task GetLocationByIdAsync_ReturnsFailure_WhenIdIsInvalid()
        {
            // Arrange - No specific setup needed for invalid ID

            // Act
            var result = await _patrolService.GetLocationByIdAsync(0);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("greater than zero");
            _mockPatrolLocationRepository.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetCheckpointsByLocationIdAsync_ReturnsCheckpoints_WhenLocationExists()
        {
            // Arrange
            int testLocationId = 1;
            string testUserId = "test-user-id";
            var testCheckpoints = new List<Checkpoint>
            {
                new Checkpoint { Id = 1, LocationId = testLocationId, Name = "Checkpoint 1" },
                new Checkpoint { Id = 2, LocationId = testLocationId, Name = "Checkpoint 2" }
            };
            var testVerifications = new List<CheckpointVerification>
            {
                new CheckpointVerification { CheckpointId = 1, UserId = testUserId }
            };

            _mockPatrolLocationRepository.Setup(r => r.ExistsAsync(testLocationId))
                .ReturnsAsync(true);
            _mockCheckpointRepository.Setup(r => r.GetByLocationIdAsync(testLocationId))
                .ReturnsAsync(testCheckpoints);
            _mockCurrentUserService.Setup(s => s.GetUserId())
                .Returns(testUserId);
            _mockVerificationRepository.Setup(r => r.GetByUserAndLocationIdAsync(testUserId, testLocationId))
                .ReturnsAsync(testVerifications);

            // Act
            var result = await _patrolService.GetCheckpointsByLocationIdAsync(testLocationId);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().HaveCount(2);
            result.Data.First(c => c.Id == 1).IsVerified.Should().BeTrue();
            result.Data.First(c => c.Id == 2).IsVerified.Should().BeFalse();
            _mockPatrolLocationRepository.Verify(r => r.ExistsAsync(testLocationId), Times.Once);
            _mockCheckpointRepository.Verify(r => r.GetByLocationIdAsync(testLocationId), Times.Once);
            _mockCurrentUserService.Verify(s => s.GetUserId, Times.Once);
            _mockVerificationRepository.Verify(r => r.GetByUserAndLocationIdAsync(testUserId, testLocationId), Times.Once);
        }

        [Fact]
        public async Task GetCheckpointsByLocationIdAsync_ReturnsFailure_WhenLocationDoesNotExist()
        {
            // Arrange
            int testLocationId = 1;
            _mockPatrolLocationRepository.Setup(r => r.ExistsAsync(testLocationId))
                .ReturnsAsync(false);

            // Act
            var result = await _patrolService.GetCheckpointsByLocationIdAsync(testLocationId);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("not found");
            _mockPatrolLocationRepository.Verify(r => r.ExistsAsync(testLocationId), Times.Once);
            _mockCheckpointRepository.Verify(r => r.GetByLocationIdAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetCheckpointsByLocationIdAsync_ReturnsEmptyList_WhenNoCheckpointsExist()
        {
            // Arrange
            int testLocationId = 1;
            _mockPatrolLocationRepository.Setup(r => r.ExistsAsync(testLocationId))
                .ReturnsAsync(true);
            _mockCheckpointRepository.Setup(r => r.GetByLocationIdAsync(testLocationId))
                .ReturnsAsync(new List<Checkpoint>());

            // Act
            var result = await _patrolService.GetCheckpointsByLocationIdAsync(testLocationId);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().BeEmpty();
            _mockPatrolLocationRepository.Verify(r => r.ExistsAsync(testLocationId), Times.Once);
            _mockCheckpointRepository.Verify(r => r.GetByLocationIdAsync(testLocationId), Times.Once);
        }

        [Fact]
        public async Task GetCheckpointByIdAsync_ReturnsCheckpoint_WhenCheckpointExists()
        {
            // Arrange
            int testCheckpointId = 1;
            string testUserId = "test-user-id";
            var testCheckpoint = new Checkpoint { Id = testCheckpointId, Name = "Test Checkpoint" };
            var testVerification = new CheckpointVerification { 
                CheckpointId = testCheckpointId, 
                UserId = testUserId, 
                Timestamp = DateTime.UtcNow 
            };

            _mockCheckpointRepository.Setup(r => r.GetByIdAsync(testCheckpointId))
                .ReturnsAsync(testCheckpoint);
            _mockCurrentUserService.Setup(s => s.GetUserId())
                .Returns(testUserId);
            _mockVerificationRepository.Setup(r => r.GetByUserAndCheckpointIdAsync(testUserId, testCheckpointId))
                .ReturnsAsync(testVerification);

            // Act
            var result = await _patrolService.GetCheckpointByIdAsync(testCheckpointId);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Id.Should().Be(testCheckpointId);
            result.Data.IsVerified.Should().BeTrue();
            result.Data.VerificationTime.Should().Be(testVerification.Timestamp);
            _mockCheckpointRepository.Verify(r => r.GetByIdAsync(testCheckpointId), Times.Once);
            _mockCurrentUserService.Verify(s => s.GetUserId, Times.Once);
            _mockVerificationRepository.Verify(r => r.GetByUserAndCheckpointIdAsync(testUserId, testCheckpointId), Times.Once);
        }

        [Fact]
        public async Task GetCheckpointByIdAsync_ReturnsFailure_WhenCheckpointDoesNotExist()
        {
            // Arrange
            int testCheckpointId = 1;
            _mockCheckpointRepository.Setup(r => r.GetByIdAsync(testCheckpointId))
                .ReturnsAsync((Checkpoint)null);

            // Act
            var result = await _patrolService.GetCheckpointByIdAsync(testCheckpointId);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("not found");
            _mockCheckpointRepository.Verify(r => r.GetByIdAsync(testCheckpointId), Times.Once);
        }

        [Fact]
        public async Task VerifyCheckpointAsync_ReturnsSuccess_WhenCheckpointExistsAndNotAlreadyVerified()
        {
            // Arrange
            int testCheckpointId = 1;
            string testUserId = "test-user-id";
            DateTime testNow = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            var testRequest = new CheckpointVerificationRequest
            {
                CheckpointId = testCheckpointId,
                Latitude = 34.0522,
                Longitude = -118.2437
            };
            var testCheckpoint = new Checkpoint { Id = testCheckpointId, Name = "Test Checkpoint" };
            var testVerification = new CheckpointVerification
            {
                Id = 1,
                CheckpointId = testCheckpointId,
                UserId = testUserId,
                Timestamp = testNow,
                Latitude = testRequest.Latitude,
                Longitude = testRequest.Longitude
            };

            _mockCheckpointRepository.Setup(r => r.GetByIdAsync(testCheckpointId))
                .ReturnsAsync(testCheckpoint);
            _mockVerificationRepository.Setup(r => r.GetByUserAndCheckpointIdAsync(testUserId, testCheckpointId))
                .ReturnsAsync((CheckpointVerification)null);
            _mockDateTime.Setup(d => d.Now).Returns(testNow);
            _mockVerificationRepository.Setup(r => r.AddAsync(It.IsAny<CheckpointVerification>()))
                .ReturnsAsync(true);
            // After adding the verification, we need to return it when queried again
            _mockVerificationRepository.SetupSequence(r => r.GetByUserAndCheckpointIdAsync(testUserId, testCheckpointId))
                .ReturnsAsync((CheckpointVerification)null)
                .ReturnsAsync(testVerification);

            // Act
            var result = await _patrolService.VerifyCheckpointAsync(testRequest, testUserId);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.CheckpointId.Should().Be(testCheckpointId);
            result.Data.UserId.Should().Be(testUserId);
            result.Data.Timestamp.Should().Be(testNow);
            result.Data.Latitude.Should().Be(testRequest.Latitude);
            result.Data.Longitude.Should().Be(testRequest.Longitude);
            result.Data.Status.Should().Be("Verified");

            _mockCheckpointRepository.Verify(r => r.GetByIdAsync(testCheckpointId), Times.Once);
            _mockVerificationRepository.Verify(r => r.GetByUserAndCheckpointIdAsync(testUserId, testCheckpointId), Times.Exactly(2));
            _mockVerificationRepository.Verify(r => r.AddAsync(It.Is<CheckpointVerification>(v =>
                v.CheckpointId == testCheckpointId &&
                v.UserId == testUserId &&
                v.Latitude == testRequest.Latitude &&
                v.Longitude == testRequest.Longitude)), Times.Once);
        }

        [Fact]
        public async Task VerifyCheckpointAsync_ReturnsExistingVerification_WhenCheckpointAlreadyVerified()
        {
            // Arrange
            int testCheckpointId = 1;
            string testUserId = "test-user-id";
            var testRequest = new CheckpointVerificationRequest
            {
                CheckpointId = testCheckpointId,
                Latitude = 34.0522,
                Longitude = -118.2437
            };
            var testCheckpoint = new Checkpoint { Id = testCheckpointId, Name = "Test Checkpoint" };
            var existingVerification = new CheckpointVerification
            {
                CheckpointId = testCheckpointId,
                UserId = testUserId,
                Timestamp = DateTime.UtcNow,
                Latitude = 34.0522,
                Longitude = -118.2437
            };

            _mockCheckpointRepository.Setup(r => r.GetByIdAsync(testCheckpointId))
                .ReturnsAsync(testCheckpoint);
            _mockVerificationRepository.Setup(r => r.GetByUserAndCheckpointIdAsync(testUserId, testCheckpointId))
                .ReturnsAsync(existingVerification);

            // Act
            var result = await _patrolService.VerifyCheckpointAsync(testRequest, testUserId);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.CheckpointId.Should().Be(testCheckpointId);
            result.Data.UserId.Should().Be(testUserId);
            result.Data.Status.Should().Be("Already Verified");
            _mockCheckpointRepository.Verify(r => r.GetByIdAsync(testCheckpointId), Times.Once);
            _mockVerificationRepository.Verify(r => r.GetByUserAndCheckpointIdAsync(testUserId, testCheckpointId), Times.Once);
            _mockVerificationRepository.Verify(r => r.AddAsync(It.IsAny<CheckpointVerification>()), Times.Never);
        }

        [Fact]
        public async Task VerifyCheckpointAsync_ReturnsFailure_WhenCheckpointDoesNotExist()
        {
            // Arrange
            int testCheckpointId = 1;
            string testUserId = "test-user-id";
            var testRequest = new CheckpointVerificationRequest
            {
                CheckpointId = testCheckpointId,
                Latitude = 34.0522,
                Longitude = -118.2437
            };

            _mockCheckpointRepository.Setup(r => r.GetByIdAsync(testCheckpointId))
                .ReturnsAsync((Checkpoint)null);

            // Act
            var result = await _patrolService.VerifyCheckpointAsync(testRequest, testUserId);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("not found");
            _mockCheckpointRepository.Verify(r => r.GetByIdAsync(testCheckpointId), Times.Once);
            _mockVerificationRepository.Verify(r => r.GetByUserAndCheckpointIdAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
            _mockVerificationRepository.Verify(r => r.AddAsync(It.IsAny<CheckpointVerification>()), Times.Never);
        }

        [Fact]
        public async Task GetPatrolStatusAsync_ReturnsStatus_WhenLocationExists()
        {
            // Arrange
            int testLocationId = 1;
            string testUserId = "test-user-id";
            var testCheckpoints = new List<Checkpoint>
            {
                new Checkpoint { Id = 1, LocationId = testLocationId },
                new Checkpoint { Id = 2, LocationId = testLocationId },
                new Checkpoint { Id = 3, LocationId = testLocationId }
            };
            var testVerifications = new List<CheckpointVerification>
            {
                new CheckpointVerification { CheckpointId = 1, UserId = testUserId, Timestamp = DateTime.UtcNow },
                new CheckpointVerification { CheckpointId = 2, UserId = testUserId, Timestamp = DateTime.UtcNow.AddMinutes(5) }
            };

            _mockPatrolLocationRepository.Setup(r => r.ExistsAsync(testLocationId))
                .ReturnsAsync(true);
            _mockCheckpointRepository.Setup(r => r.GetByLocationIdAsync(testLocationId))
                .ReturnsAsync(testCheckpoints);
            _mockVerificationRepository.Setup(r => r.GetByUserAndLocationIdAsync(testUserId, testLocationId))
                .ReturnsAsync(testVerifications);

            // Act
            var result = await _patrolService.GetPatrolStatusAsync(testLocationId, testUserId);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.LocationId.Should().Be(testLocationId);
            result.Data.TotalCheckpoints.Should().Be(3);
            result.Data.VerifiedCheckpoints.Should().Be(2);
            result.Data.IsComplete.Should().BeFalse();
            _mockPatrolLocationRepository.Verify(r => r.ExistsAsync(testLocationId), Times.Once);
            _mockCheckpointRepository.Verify(r => r.GetByLocationIdAsync(testLocationId), Times.Once);
            _mockVerificationRepository.Verify(r => r.GetByUserAndLocationIdAsync(testUserId, testLocationId), Times.Once);
        }

        [Fact]
        public async Task GetPatrolStatusAsync_ReturnsFailure_WhenLocationDoesNotExist()
        {
            // Arrange
            int testLocationId = 1;
            string testUserId = "test-user-id";
            _mockPatrolLocationRepository.Setup(r => r.ExistsAsync(testLocationId))
                .ReturnsAsync(false);

            // Act
            var result = await _patrolService.GetPatrolStatusAsync(testLocationId, testUserId);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("not found");
            _mockPatrolLocationRepository.Verify(r => r.ExistsAsync(testLocationId), Times.Once);
            _mockCheckpointRepository.Verify(r => r.GetByLocationIdAsync(testLocationId), Times.Never);
            _mockVerificationRepository.Verify(r => r.GetByUserAndLocationIdAsync(testUserId, testLocationId), Times.Never);
        }

        [Fact]
        public async Task GetNearbyCheckpointsAsync_ReturnsCheckpoints_WhenCheckpointsExistWithinRadius()
        {
            // Arrange
            double testLatitude = 34.0522;
            double testLongitude = -118.2437;
            double testRadius = 100.0;
            string testUserId = "test-user-id";
            
            var testCheckpoints = new List<Checkpoint>
            {
                new Checkpoint { Id = 1, Name = "Checkpoint 1" },
                new Checkpoint { Id = 2, Name = "Checkpoint 2" }
            };
            
            var testVerifications = new List<CheckpointVerification>
            {
                new CheckpointVerification { CheckpointId = 1, UserId = testUserId }
            };

            _mockCheckpointRepository.Setup(r => r.GetNearbyCheckpointsAsync(testLatitude, testLongitude, testRadius))
                .ReturnsAsync(testCheckpoints);
            _mockCurrentUserService.Setup(s => s.GetUserId())
                .Returns(testUserId);
            _mockVerificationRepository.Setup(r => r.GetByUserIdAsync(testUserId))
                .ReturnsAsync(testVerifications);

            // Act
            var result = await _patrolService.GetNearbyCheckpointsAsync(testLatitude, testLongitude, testRadius);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().HaveCount(2);
            result.Data.First(c => c.Id == 1).IsVerified.Should().BeTrue();
            result.Data.First(c => c.Id == 2).IsVerified.Should().BeFalse();
            _mockCheckpointRepository.Verify(r => r.GetNearbyCheckpointsAsync(testLatitude, testLongitude, testRadius), Times.Once);
            _mockCurrentUserService.Verify(s => s.GetUserId, Times.Once);
            _mockVerificationRepository.Verify(r => r.GetByUserIdAsync(testUserId), Times.Once);
        }

        [Fact]
        public async Task GetNearbyCheckpointsAsync_ReturnsEmptyList_WhenNoCheckpointsExistWithinRadius()
        {
            // Arrange
            double testLatitude = 34.0522;
            double testLongitude = -118.2437;
            double testRadius = 100.0;

            _mockCheckpointRepository.Setup(r => r.GetNearbyCheckpointsAsync(testLatitude, testLongitude, testRadius))
                .ReturnsAsync(new List<Checkpoint>());

            // Act
            var result = await _patrolService.GetNearbyCheckpointsAsync(testLatitude, testLongitude, testRadius);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().BeEmpty();
            _mockCheckpointRepository.Verify(r => r.GetNearbyCheckpointsAsync(testLatitude, testLongitude, testRadius), Times.Once);
        }

        [Fact]
        public async Task GetNearbyLocationsAsync_ReturnsLocations_WhenLocationsExistWithinRadius()
        {
            // Arrange
            double testLatitude = 34.0522;
            double testLongitude = -118.2437;
            double testRadius = 100.0;
            
            var testLocations = new List<PatrolLocation>
            {
                new PatrolLocation { Id = 1, Name = "Location 1" },
                new PatrolLocation { Id = 2, Name = "Location 2" }
            };

            _mockPatrolLocationRepository.Setup(r => r.GetNearbyLocationsAsync(testLatitude, testLongitude, testRadius))
                .ReturnsAsync(testLocations);

            // Act
            var result = await _patrolService.GetNearbyLocationsAsync(testLatitude, testLongitude, testRadius);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().BeEquivalentTo(testLocations);
            _mockPatrolLocationRepository.Verify(r => r.GetNearbyLocationsAsync(testLatitude, testLongitude, testRadius), Times.Once);
        }

        [Fact]
        public async Task GetNearbyLocationsAsync_ReturnsEmptyList_WhenNoLocationsExistWithinRadius()
        {
            // Arrange
            double testLatitude = 34.0522;
            double testLongitude = -118.2437;
            double testRadius = 100.0;

            _mockPatrolLocationRepository.Setup(r => r.GetNearbyLocationsAsync(testLatitude, testLongitude, testRadius))
                .ReturnsAsync(new List<PatrolLocation>());

            // Act
            var result = await _patrolService.GetNearbyLocationsAsync(testLatitude, testLongitude, testRadius);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().BeEmpty();
            _mockPatrolLocationRepository.Verify(r => r.GetNearbyLocationsAsync(testLatitude, testLongitude, testRadius), Times.Once);
        }

        [Fact]
        public async Task IsCheckpointVerifiedAsync_ReturnsTrue_WhenCheckpointIsVerified()
        {
            // Arrange
            int testCheckpointId = 1;
            string testUserId = "test-user-id";
            var testVerifications = new List<CheckpointVerification>
            {
                new CheckpointVerification { CheckpointId = testCheckpointId, UserId = testUserId }
            };

            _mockCheckpointRepository.Setup(r => r.ExistsAsync(testCheckpointId))
                .ReturnsAsync(true);
            _mockVerificationRepository.Setup(r => r.GetByUserIdAsync(testUserId))
                .ReturnsAsync(testVerifications);

            // Act
            var result = await _patrolService.IsCheckpointVerifiedAsync(testCheckpointId, testUserId);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().BeTrue();
            _mockCheckpointRepository.Verify(r => r.ExistsAsync(testCheckpointId), Times.Once);
            _mockVerificationRepository.Verify(r => r.GetByUserIdAsync(testUserId), Times.Once);
        }

        [Fact]
        public async Task IsCheckpointVerifiedAsync_ReturnsFalse_WhenCheckpointIsNotVerified()
        {
            // Arrange
            int testCheckpointId = 1;
            string testUserId = "test-user-id";
            var testVerifications = new List<CheckpointVerification>
            {
                new CheckpointVerification { CheckpointId = 2, UserId = testUserId }
            };

            _mockCheckpointRepository.Setup(r => r.ExistsAsync(testCheckpointId))
                .ReturnsAsync(true);
            _mockVerificationRepository.Setup(r => r.GetByUserIdAsync(testUserId))
                .ReturnsAsync(testVerifications);

            // Act
            var result = await _patrolService.IsCheckpointVerifiedAsync(testCheckpointId, testUserId);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().BeFalse();
            _mockCheckpointRepository.Verify(r => r.ExistsAsync(testCheckpointId), Times.Once);
            _mockVerificationRepository.Verify(r => r.GetByUserIdAsync(testUserId), Times.Once);
        }

        [Fact]
        public async Task IsCheckpointVerifiedAsync_ReturnsFailure_WhenCheckpointDoesNotExist()
        {
            // Arrange
            int testCheckpointId = 1;
            string testUserId = "test-user-id";
            _mockCheckpointRepository.Setup(r => r.ExistsAsync(testCheckpointId))
                .ReturnsAsync(false);

            // Act
            var result = await _patrolService.IsCheckpointVerifiedAsync(testCheckpointId, testUserId);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("not found");
            _mockCheckpointRepository.Verify(r => r.ExistsAsync(testCheckpointId), Times.Once);
            _mockVerificationRepository.Verify(r => r.GetByUserIdAsync(testUserId), Times.Never);
        }

        [Fact]
        public async Task GetUserVerificationsAsync_ReturnsVerifications_WhenVerificationsExist()
        {
            // Arrange
            string testUserId = "test-user-id";
            var testVerifications = new List<CheckpointVerification>
            {
                new CheckpointVerification { CheckpointId = 1, UserId = testUserId },
                new CheckpointVerification { CheckpointId = 2, UserId = testUserId }
            };

            _mockVerificationRepository.Setup(r => r.GetByUserIdAsync(testUserId))
                .ReturnsAsync(testVerifications);

            // Act
            var result = await _patrolService.GetUserVerificationsAsync(testUserId);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().BeEquivalentTo(testVerifications);
            _mockVerificationRepository.Verify(r => r.GetByUserIdAsync(testUserId), Times.Once);
        }

        [Fact]
        public async Task GetUserVerificationsAsync_ReturnsEmptyList_WhenNoVerificationsExist()
        {
            // Arrange
            string testUserId = "test-user-id";
            _mockVerificationRepository.Setup(r => r.GetByUserIdAsync(testUserId))
                .ReturnsAsync(new List<CheckpointVerification>());

            // Act
            var result = await _patrolService.GetUserVerificationsAsync(testUserId);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().BeEmpty();
            _mockVerificationRepository.Verify(r => r.GetByUserIdAsync(testUserId), Times.Once);
        }

        [Fact]
        public async Task GetUserVerificationsByDateRangeAsync_ReturnsVerifications_WhenVerificationsExistInRange()
        {
            // Arrange
            string testUserId = "test-user-id";
            DateTime startDate = new DateTime(2023, 1, 1);
            DateTime endDate = new DateTime(2023, 1, 31);
            var testVerifications = new List<CheckpointVerification>
            {
                new CheckpointVerification { CheckpointId = 1, UserId = testUserId, Timestamp = new DateTime(2023, 1, 15) },
                new CheckpointVerification { CheckpointId = 2, UserId = testUserId, Timestamp = new DateTime(2023, 1, 20) }
            };

            _mockVerificationRepository.Setup(r => r.GetByUserAndDateRangeAsync(testUserId, startDate, endDate))
                .ReturnsAsync(testVerifications);

            // Act
            var result = await _patrolService.GetUserVerificationsByDateRangeAsync(testUserId, startDate, endDate);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().BeEquivalentTo(testVerifications);
            _mockVerificationRepository.Verify(r => r.GetByUserAndDateRangeAsync(testUserId, startDate, endDate), Times.Once);
        }

        [Fact]
        public async Task GetUserVerificationsByDateRangeAsync_ReturnsEmptyList_WhenNoVerificationsExistInRange()
        {
            // Arrange
            string testUserId = "test-user-id";
            DateTime startDate = new DateTime(2023, 1, 1);
            DateTime endDate = new DateTime(2023, 1, 31);
            _mockVerificationRepository.Setup(r => r.GetByUserAndDateRangeAsync(testUserId, startDate, endDate))
                .ReturnsAsync(new List<CheckpointVerification>());

            // Act
            var result = await _patrolService.GetUserVerificationsByDateRangeAsync(testUserId, startDate, endDate);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().BeEmpty();
            _mockVerificationRepository.Verify(r => r.GetByUserAndDateRangeAsync(testUserId, startDate, endDate), Times.Once);
        }

        [Fact]
        public void CalculateDistance_ReturnsCorrectDistance_BetweenTwoPoints()
        {
            // Arrange
            // New York City coordinates
            double lat1 = 40.7128;
            double lon1 = -74.0060;
            // Los Angeles coordinates
            double lat2 = 34.0522;
            double lon2 = -118.2437;
            // Expected distance in meters (approximately 3935.74 km)
            double expectedDistance = 3935740;
            
            // Act
            // Use reflection to access the private method
            var methodInfo = typeof(PatrolService).GetMethod("CalculateDistance", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var result = (double)methodInfo.Invoke(null, new object[] { lat1, lon1, lat2, lon2 });
            
            // Assert
            // We allow for some error margin due to floating-point calculations
            result.Should().BeApproximately(expectedDistance, expectedDistance * 0.01);
        }
    }
}