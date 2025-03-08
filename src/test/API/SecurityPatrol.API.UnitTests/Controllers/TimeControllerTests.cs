using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using SecurityPatrol.API.Controllers;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.Core.Models;
using SecurityPatrol.API.UnitTests.Setup;
using SecurityPatrol.Core.Entities;

namespace SecurityPatrol.API.UnitTests.Controllers
{
    public class TimeControllerTests : TestBase
    {
        [Fact]
        public async Task Clock_WithValidRequest_ReturnsSuccess()
        {
            // Arrange
            var request = new TimeRecordRequest
            {
                Type = "in",
                Timestamp = DateTime.UtcNow,
                Location = new LocationModel { Latitude = 34.0522, Longitude = -118.2437 }
            };
            
            var userId = "test-user-id";
            var response = new TimeRecordResponse { Id = "123", Status = "success" };
            
            var mockCurrentUserService = new Mock<ICurrentUserService>();
            mockCurrentUserService.Setup(s => s.GetUserId()).Returns(userId);
            mockCurrentUserService.Setup(s => s.IsAuthenticated()).Returns(true);
            
            MockTimeRecordService.Setup(s => s.CreateTimeRecordAsync(request, userId))
                .ReturnsAsync(Result<TimeRecordResponse>.Success(response));
            
            var controller = new TimeController(
                MockTimeRecordService.Object,
                mockCurrentUserService.Object,
                Mock.Of<ILogger<TimeController>>()
            );
            
            SetupHttpContext(controller);
            
            // Act
            var result = await controller.Clock(request);
            
            // Assert
            AssertActionResult(result, response);
            MockTimeRecordService.Verify(s => s.CreateTimeRecordAsync(request, userId), Times.Once);
        }
        
        [Fact]
        public async Task Clock_WithNullRequest_ReturnsBadRequest()
        {
            // Arrange
            var mockCurrentUserService = new Mock<ICurrentUserService>();
            mockCurrentUserService.Setup(s => s.IsAuthenticated()).Returns(true);
            
            var controller = new TimeController(
                MockTimeRecordService.Object,
                mockCurrentUserService.Object,
                Mock.Of<ILogger<TimeController>>()
            );
            
            SetupHttpContext(controller);
            
            // Act
            var result = await controller.Clock(null);
            
            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            MockTimeRecordService.Verify(s => s.CreateTimeRecordAsync(It.IsAny<TimeRecordRequest>(), It.IsAny<string>()), Times.Never);
        }
        
        [Fact]
        public async Task Clock_WhenUserNotAuthenticated_ReturnsUnauthorized()
        {
            // Arrange
            var request = new TimeRecordRequest();
            
            var mockCurrentUserService = new Mock<ICurrentUserService>();
            mockCurrentUserService.Setup(s => s.IsAuthenticated()).Returns(false);
            
            var controller = new TimeController(
                MockTimeRecordService.Object,
                mockCurrentUserService.Object,
                Mock.Of<ILogger<TimeController>>()
            );
            
            SetupHttpContext(controller);
            
            // Act
            var result = await controller.Clock(request);
            
            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
            MockTimeRecordService.Verify(s => s.CreateTimeRecordAsync(It.IsAny<TimeRecordRequest>(), It.IsAny<string>()), Times.Never);
        }
        
        [Fact]
        public async Task Clock_WhenServiceReturnsFailure_ReturnsBadRequest()
        {
            // Arrange
            var request = new TimeRecordRequest();
            var userId = "test-user-id";
            var errorMessage = "Invalid clock operation";
            
            var mockCurrentUserService = new Mock<ICurrentUserService>();
            mockCurrentUserService.Setup(s => s.GetUserId()).Returns(userId);
            mockCurrentUserService.Setup(s => s.IsAuthenticated()).Returns(true);
            
            MockTimeRecordService.Setup(s => s.CreateTimeRecordAsync(request, userId))
                .ReturnsAsync(Result<TimeRecordResponse>.Failure(errorMessage));
            
            var controller = new TimeController(
                MockTimeRecordService.Object,
                mockCurrentUserService.Object,
                Mock.Of<ILogger<TimeController>>()
            );
            
            SetupHttpContext(controller);
            
            // Act
            var result = await controller.Clock(request);
            
            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value.Should().Be(errorMessage);
            MockTimeRecordService.Verify(s => s.CreateTimeRecordAsync(request, userId), Times.Once);
        }
        
        [Fact]
        public async Task GetHistory_WithValidParameters_ReturnsSuccess()
        {
            // Arrange
            var userId = "test-user-id";
            var pageNumber = 1;
            var pageSize = 10;
            var timeRecords = new PaginatedList<TimeRecord>(
                new List<TimeRecord> { new TimeRecord { Id = 1, Type = "in" } }, 1, pageNumber, pageSize);
            
            var mockCurrentUserService = new Mock<ICurrentUserService>();
            mockCurrentUserService.Setup(s => s.GetUserId()).Returns(userId);
            mockCurrentUserService.Setup(s => s.IsAuthenticated()).Returns(true);
            
            MockTimeRecordService.Setup(s => s.GetTimeRecordHistoryAsync(userId, pageNumber, pageSize))
                .ReturnsAsync(Result<PaginatedList<TimeRecord>>.Success(timeRecords));
            
            var controller = new TimeController(
                MockTimeRecordService.Object,
                mockCurrentUserService.Object,
                Mock.Of<ILogger<TimeController>>()
            );
            
            SetupHttpContext(controller);
            
            // Act
            var result = await controller.GetHistory(pageNumber, pageSize);
            
            // Assert
            AssertActionResult(result, timeRecords);
            MockTimeRecordService.Verify(s => s.GetTimeRecordHistoryAsync(userId, pageNumber, pageSize), Times.Once);
        }
        
        [Fact]
        public async Task GetHistory_WithInvalidPageParameters_UseDefaultValues()
        {
            // Arrange
            var userId = "test-user-id";
            var defaultPageNumber = 1;
            var defaultPageSize = 10;
            var timeRecords = new PaginatedList<TimeRecord>(
                new List<TimeRecord>(), 0, defaultPageNumber, defaultPageSize);
            
            var mockCurrentUserService = new Mock<ICurrentUserService>();
            mockCurrentUserService.Setup(s => s.GetUserId()).Returns(userId);
            mockCurrentUserService.Setup(s => s.IsAuthenticated()).Returns(true);
            
            MockTimeRecordService.Setup(s => s.GetTimeRecordHistoryAsync(userId, defaultPageNumber, defaultPageSize))
                .ReturnsAsync(Result<PaginatedList<TimeRecord>>.Success(timeRecords));
            
            var controller = new TimeController(
                MockTimeRecordService.Object,
                mockCurrentUserService.Object,
                Mock.Of<ILogger<TimeController>>()
            );
            
            SetupHttpContext(controller);
            
            // Act
            var result = await controller.GetHistory(0, 0);
            
            // Assert
            result.Should().BeOfType<OkObjectResult>();
            MockTimeRecordService.Verify(s => s.GetTimeRecordHistoryAsync(userId, defaultPageNumber, defaultPageSize), Times.Once);
        }
        
        [Fact]
        public async Task GetHistory_WhenUserNotAuthenticated_ReturnsUnauthorized()
        {
            // Arrange
            var mockCurrentUserService = new Mock<ICurrentUserService>();
            mockCurrentUserService.Setup(s => s.IsAuthenticated()).Returns(false);
            
            var controller = new TimeController(
                MockTimeRecordService.Object,
                mockCurrentUserService.Object,
                Mock.Of<ILogger<TimeController>>()
            );
            
            SetupHttpContext(controller);
            
            // Act
            var result = await controller.GetHistory(1, 10);
            
            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
            MockTimeRecordService.Verify(s => s.GetTimeRecordHistoryAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }
        
        [Fact]
        public async Task GetHistory_WhenServiceReturnsFailure_ReturnsBadRequest()
        {
            // Arrange
            var userId = "test-user-id";
            var pageNumber = 1;
            var pageSize = 10;
            var errorMessage = "Failed to retrieve history";
            
            var mockCurrentUserService = new Mock<ICurrentUserService>();
            mockCurrentUserService.Setup(s => s.GetUserId()).Returns(userId);
            mockCurrentUserService.Setup(s => s.IsAuthenticated()).Returns(true);
            
            MockTimeRecordService.Setup(s => s.GetTimeRecordHistoryAsync(userId, pageNumber, pageSize))
                .ReturnsAsync(Result<PaginatedList<TimeRecord>>.Failure(errorMessage));
            
            var controller = new TimeController(
                MockTimeRecordService.Object,
                mockCurrentUserService.Object,
                Mock.Of<ILogger<TimeController>>()
            );
            
            SetupHttpContext(controller);
            
            // Act
            var result = await controller.GetHistory(pageNumber, pageSize);
            
            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value.Should().Be(errorMessage);
            MockTimeRecordService.Verify(s => s.GetTimeRecordHistoryAsync(userId, pageNumber, pageSize), Times.Once);
        }
        
        [Fact]
        public async Task GetStatus_WhenAuthenticated_ReturnsSuccess()
        {
            // Arrange
            var userId = "test-user-id";
            var status = "in";
            
            var mockCurrentUserService = new Mock<ICurrentUserService>();
            mockCurrentUserService.Setup(s => s.GetUserId()).Returns(userId);
            mockCurrentUserService.Setup(s => s.IsAuthenticated()).Returns(true);
            
            MockTimeRecordService.Setup(s => s.GetCurrentStatusAsync(userId))
                .ReturnsAsync(Result<string>.Success(status));
            
            var controller = new TimeController(
                MockTimeRecordService.Object,
                mockCurrentUserService.Object,
                Mock.Of<ILogger<TimeController>>()
            );
            
            SetupHttpContext(controller);
            
            // Act
            var result = await controller.GetStatus();
            
            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(status);
            MockTimeRecordService.Verify(s => s.GetCurrentStatusAsync(userId), Times.Once);
        }
        
        [Fact]
        public async Task GetStatus_WhenUserNotAuthenticated_ReturnsUnauthorized()
        {
            // Arrange
            var mockCurrentUserService = new Mock<ICurrentUserService>();
            mockCurrentUserService.Setup(s => s.IsAuthenticated()).Returns(false);
            
            var controller = new TimeController(
                MockTimeRecordService.Object,
                mockCurrentUserService.Object,
                Mock.Of<ILogger<TimeController>>()
            );
            
            SetupHttpContext(controller);
            
            // Act
            var result = await controller.GetStatus();
            
            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
            MockTimeRecordService.Verify(s => s.GetCurrentStatusAsync(It.IsAny<string>()), Times.Never);
        }
        
        [Fact]
        public async Task GetStatus_WhenServiceReturnsFailure_ReturnsBadRequest()
        {
            // Arrange
            var userId = "test-user-id";
            var errorMessage = "Failed to retrieve status";
            
            var mockCurrentUserService = new Mock<ICurrentUserService>();
            mockCurrentUserService.Setup(s => s.GetUserId()).Returns(userId);
            mockCurrentUserService.Setup(s => s.IsAuthenticated()).Returns(true);
            
            MockTimeRecordService.Setup(s => s.GetCurrentStatusAsync(userId))
                .ReturnsAsync(Result<string>.Failure(errorMessage));
            
            var controller = new TimeController(
                MockTimeRecordService.Object,
                mockCurrentUserService.Object,
                Mock.Of<ILogger<TimeController>>()
            );
            
            SetupHttpContext(controller);
            
            // Act
            var result = await controller.GetStatus();
            
            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value.Should().Be(errorMessage);
            MockTimeRecordService.Verify(s => s.GetCurrentStatusAsync(userId), Times.Once);
        }
        
        [Fact]
        public async Task GetByDateRange_WithValidDates_ReturnsSuccess()
        {
            // Arrange
            var userId = "test-user-id";
            var startDate = DateTime.UtcNow.AddDays(-7);
            var endDate = DateTime.UtcNow;
            var timeRecords = new List<TimeRecord> { new TimeRecord { Id = 1, Type = "in" } };
            
            var mockCurrentUserService = new Mock<ICurrentUserService>();
            mockCurrentUserService.Setup(s => s.GetUserId()).Returns(userId);
            mockCurrentUserService.Setup(s => s.IsAuthenticated()).Returns(true);
            
            MockTimeRecordService.Setup(s => s.GetTimeRecordsByDateRangeAsync(userId, startDate, endDate))
                .ReturnsAsync(Result<IEnumerable<TimeRecord>>.Success(timeRecords));
            
            var controller = new TimeController(
                MockTimeRecordService.Object,
                mockCurrentUserService.Object,
                Mock.Of<ILogger<TimeController>>()
            );
            
            SetupHttpContext(controller);
            
            // Act
            var result = await controller.GetByDateRange(startDate, endDate);
            
            // Assert
            AssertActionResult(result, timeRecords);
            MockTimeRecordService.Verify(s => s.GetTimeRecordsByDateRangeAsync(userId, startDate, endDate), Times.Once);
        }
        
        [Fact]
        public async Task GetByDateRange_WithInvalidDateRange_ReturnsBadRequest()
        {
            // Arrange
            var startDate = DateTime.UtcNow;
            var endDate = DateTime.UtcNow.AddDays(-7);  // End date is before start date
            
            var mockCurrentUserService = new Mock<ICurrentUserService>();
            mockCurrentUserService.Setup(s => s.IsAuthenticated()).Returns(true);
            
            var controller = new TimeController(
                MockTimeRecordService.Object,
                mockCurrentUserService.Object,
                Mock.Of<ILogger<TimeController>>()
            );
            
            SetupHttpContext(controller);
            
            // Act
            var result = await controller.GetByDateRange(startDate, endDate);
            
            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            MockTimeRecordService.Verify(s => s.GetTimeRecordsByDateRangeAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never);
        }

        [Fact]
        public async Task GetByDateRange_WithDefaultDates_ReturnsBadRequest()
        {
            // Arrange
            var mockCurrentUserService = new Mock<ICurrentUserService>();
            mockCurrentUserService.Setup(s => s.IsAuthenticated()).Returns(true);
            
            var controller = new TimeController(
                MockTimeRecordService.Object,
                mockCurrentUserService.Object,
                Mock.Of<ILogger<TimeController>>()
            );
            
            SetupHttpContext(controller);
            
            // Act
            var result = await controller.GetByDateRange(default(DateTime), default(DateTime));
            
            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            MockTimeRecordService.Verify(s => s.GetTimeRecordsByDateRangeAsync(
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()), 
                Times.Never);
        }
        
        [Fact]
        public async Task GetByDateRange_WhenUserNotAuthenticated_ReturnsUnauthorized()
        {
            // Arrange
            var startDate = DateTime.UtcNow.AddDays(-7);
            var endDate = DateTime.UtcNow;
            
            var mockCurrentUserService = new Mock<ICurrentUserService>();
            mockCurrentUserService.Setup(s => s.IsAuthenticated()).Returns(false);
            
            var controller = new TimeController(
                MockTimeRecordService.Object,
                mockCurrentUserService.Object,
                Mock.Of<ILogger<TimeController>>()
            );
            
            SetupHttpContext(controller);
            
            // Act
            var result = await controller.GetByDateRange(startDate, endDate);
            
            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
            MockTimeRecordService.Verify(s => s.GetTimeRecordsByDateRangeAsync(
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()), 
                Times.Never);
        }
        
        [Fact]
        public async Task GetByDateRange_WhenServiceReturnsFailure_ReturnsBadRequest()
        {
            // Arrange
            var userId = "test-user-id";
            var startDate = DateTime.UtcNow.AddDays(-7);
            var endDate = DateTime.UtcNow;
            var errorMessage = "Failed to retrieve time records";
            
            var mockCurrentUserService = new Mock<ICurrentUserService>();
            mockCurrentUserService.Setup(s => s.GetUserId()).Returns(userId);
            mockCurrentUserService.Setup(s => s.IsAuthenticated()).Returns(true);
            
            MockTimeRecordService.Setup(s => s.GetTimeRecordsByDateRangeAsync(userId, startDate, endDate))
                .ReturnsAsync(Result<IEnumerable<TimeRecord>>.Failure(errorMessage));
            
            var controller = new TimeController(
                MockTimeRecordService.Object,
                mockCurrentUserService.Object,
                Mock.Of<ILogger<TimeController>>()
            );
            
            SetupHttpContext(controller);
            
            // Act
            var result = await controller.GetByDateRange(startDate, endDate);
            
            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value.Should().Be(errorMessage);
            MockTimeRecordService.Verify(s => s.GetTimeRecordsByDateRangeAsync(userId, startDate, endDate), Times.Once);
        }
    }
}