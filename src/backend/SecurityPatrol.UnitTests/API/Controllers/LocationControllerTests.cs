using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using Moq;
using FluentAssertions;
using SecurityPatrol.UnitTests.Helpers;
using SecurityPatrol.API.Controllers;
using SecurityPatrol.Core.Models;
using SecurityPatrol.Core.Interfaces;

namespace SecurityPatrol.UnitTests.API.Controllers
{
    /// <summary>
    /// Test class for the LocationController that verifies its behavior for handling location data operations
    /// </summary>
    public class LocationControllerTests : TestBase, IDisposable
    {
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly LocationController _controller;
        private readonly string _testUserId;

        /// <summary>
        /// Initializes a new instance of the LocationControllerTests class with test setup
        /// </summary>
        public LocationControllerTests()
        {
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockCurrentUserService.Setup(x => x.IsAuthenticated()).Returns(true);
            _testUserId = "user1";
            _mockCurrentUserService.Setup(x => x.GetUserId()).Returns(_testUserId);
            
            _controller = new LocationController(
                MockLocationService.Object,
                _mockCurrentUserService.Object,
                CreateMockLogger<LocationController>().Object);
        }

        /// <summary>
        /// Cleans up test resources after each test
        /// </summary>
        public void Dispose()
        {
            ResetMocks();
            _mockCurrentUserService.Reset();
        }

        [Fact]
        public async Task BatchAsync_WithValidRequest_ReturnsOkResult()
        {
            // Arrange
            var request = new LocationBatchRequest
            {
                UserId = _testUserId,
                Locations = new List<LocationModel>
                {
                    new LocationModel
                    {
                        Latitude = 40.7128,
                        Longitude = -74.0060,
                        Timestamp = DateTime.UtcNow
                    }
                }
            };

            MockLocationService.Setup(s => s.ProcessLocationBatchAsync(It.IsAny<LocationBatchRequest>()))
                .ReturnsAsync(new LocationSyncResponse
                {
                    SyncedIds = new List<int> { 1, 2, 3 },
                    FailedIds = new List<int>()
                });

            // Act
            var result = await _controller.BatchAsync(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeOfType<LocationSyncResponse>();
            MockLocationService.Verify(s => s.ProcessLocationBatchAsync(It.IsAny<LocationBatchRequest>()), Times.Once);
        }

        [Fact]
        public async Task BatchAsync_WithNullRequest_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.BatchAsync(null);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            MockLocationService.Verify(s => s.ProcessLocationBatchAsync(It.IsAny<LocationBatchRequest>()), Times.Never);
        }

        [Fact]
        public async Task BatchAsync_WhenUserNotAuthenticated_ReturnsUnauthorized()
        {
            // Arrange
            _mockCurrentUserService.Setup(x => x.IsAuthenticated()).Returns(false);
            var request = new LocationBatchRequest
            {
                UserId = _testUserId,
                Locations = new List<LocationModel>()
            };

            // Act
            var result = await _controller.BatchAsync(request);

            // Assert
            result.Should().BeOfType<UnauthorizedResult>();
            MockLocationService.Verify(s => s.ProcessLocationBatchAsync(It.IsAny<LocationBatchRequest>()), Times.Never);
        }

        [Fact]
        public async Task BatchAsync_WhenServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var request = new LocationBatchRequest
            {
                UserId = _testUserId,
                Locations = new List<LocationModel>()
            };

            MockLocationService.Setup(s => s.ProcessLocationBatchAsync(It.IsAny<LocationBatchRequest>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.BatchAsync(request);

            // Assert
            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
            MockLocationService.Verify(s => s.ProcessLocationBatchAsync(It.IsAny<LocationBatchRequest>()), Times.Once);
        }

        [Fact]
        public async Task GetHistoryAsync_WithValidParameters_ReturnsOkResult()
        {
            // Arrange
            var startTime = DateTime.UtcNow.AddHours(-1);
            var endTime = DateTime.UtcNow;
            var expectedLocations = new List<LocationModel>
            {
                new LocationModel
                {
                    Id = 1,
                    Latitude = 40.7128,
                    Longitude = -74.0060,
                    Timestamp = DateTime.UtcNow.AddMinutes(-30)
                }
            };

            MockLocationService.Setup(s => s.GetLocationHistoryAsync(_testUserId, startTime, endTime))
                .ReturnsAsync(expectedLocations);

            // Act
            var result = await _controller.GetHistoryAsync(startTime, endTime);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeAssignableTo<IEnumerable<LocationModel>>();
            MockLocationService.Verify(s => s.GetLocationHistoryAsync(_testUserId, startTime, endTime), Times.Once);
        }

        [Fact]
        public async Task GetHistoryAsync_WithInvalidTimeRange_ReturnsBadRequest()
        {
            // Arrange
            var startTime = DateTime.UtcNow;
            var endTime = DateTime.UtcNow.AddHours(-1); // End time before start time

            // Act
            var result = await _controller.GetHistoryAsync(startTime, endTime);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            MockLocationService.Verify(s => s.GetLocationHistoryAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never);
        }

        [Fact]
        public async Task GetHistoryAsync_WhenUserNotAuthenticated_ReturnsUnauthorized()
        {
            // Arrange
            _mockCurrentUserService.Setup(x => x.IsAuthenticated()).Returns(false);
            var startTime = DateTime.UtcNow.AddHours(-1);
            var endTime = DateTime.UtcNow;

            // Act
            var result = await _controller.GetHistoryAsync(startTime, endTime);

            // Assert
            result.Should().BeOfType<UnauthorizedResult>();
            MockLocationService.Verify(s => s.GetLocationHistoryAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never);
        }

        [Fact]
        public async Task GetCurrentAsync_WithValidUser_ReturnsOkResult()
        {
            // Arrange
            var expectedLocation = new LocationModel
            {
                Id = 1,
                Latitude = 40.7128,
                Longitude = -74.0060,
                Timestamp = DateTime.UtcNow
            };

            MockLocationService.Setup(s => s.GetLatestLocationAsync(_testUserId))
                .ReturnsAsync(expectedLocation);

            // Act
            var result = await _controller.GetCurrentAsync();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeOfType<LocationModel>();
            MockLocationService.Verify(s => s.GetLatestLocationAsync(_testUserId), Times.Once);
        }

        [Fact]
        public async Task GetCurrentAsync_WhenNoLocationFound_ReturnsNoContent()
        {
            // Arrange
            MockLocationService.Setup(s => s.GetLatestLocationAsync(_testUserId))
                .ReturnsAsync((LocationModel)null);

            // Act
            var result = await _controller.GetCurrentAsync();

            // Assert
            result.Should().BeOfType<NoContentResult>();
            MockLocationService.Verify(s => s.GetLatestLocationAsync(_testUserId), Times.Once);
        }

        [Fact]
        public async Task GetCurrentAsync_WhenUserNotAuthenticated_ReturnsUnauthorized()
        {
            // Arrange
            _mockCurrentUserService.Setup(x => x.IsAuthenticated()).Returns(false);

            // Act
            var result = await _controller.GetCurrentAsync();

            // Assert
            result.Should().BeOfType<UnauthorizedResult>();
            MockLocationService.Verify(s => s.GetLatestLocationAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetCurrentByUserIdAsync_WithValidUserId_ReturnsOkResult()
        {
            // Arrange
            var userId = "user2";
            var expectedLocation = new LocationModel
            {
                Id = 1,
                Latitude = 34.0522,
                Longitude = -118.2437,
                Timestamp = DateTime.UtcNow
            };

            MockLocationService.Setup(s => s.GetLatestLocationAsync(userId))
                .ReturnsAsync(expectedLocation);

            // Act
            var result = await _controller.GetCurrentByUserIdAsync(userId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeOfType<LocationModel>();
            MockLocationService.Verify(s => s.GetLatestLocationAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetCurrentByUserIdAsync_WithNullUserId_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetCurrentByUserIdAsync(null);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            MockLocationService.Verify(s => s.GetLatestLocationAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetCurrentByUserIdAsync_WhenNoLocationFound_ReturnsNoContent()
        {
            // Arrange
            var userId = "user3";
            MockLocationService.Setup(s => s.GetLatestLocationAsync(userId))
                .ReturnsAsync((LocationModel)null);

            // Act
            var result = await _controller.GetCurrentByUserIdAsync(userId);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            MockLocationService.Verify(s => s.GetLatestLocationAsync(userId), Times.Once);
        }
    }
}