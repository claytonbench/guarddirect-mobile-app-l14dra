using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SecurityPatrol.API.Controllers;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.Core.Models;
using SecurityPatrol.UnitTests.Helpers;
using Xunit;
using FluentAssertions;

namespace SecurityPatrol.UnitTests.API.Controllers
{
    public class TimeControllerTests : TestBase, IDisposable
    {
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly TimeController _controller;
        private readonly string _userId;

        public TimeControllerTests()
        {
            // Set up the current user service mock
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            
            // Default setup for authenticated user
            _userId = "user1"; // Using a test user ID
            _mockCurrentUserService.Setup(x => x.GetUserId()).Returns(_userId);
            _mockCurrentUserService.Setup(x => x.IsAuthenticated()).Returns(true);

            // Create the controller with mocked dependencies
            _controller = new TimeController(
                MockTimeRecordService.Object,
                _mockCurrentUserService.Object,
                CreateMockLogger<TimeController>().Object
            );
        }

        public void Dispose()
        {
            // Reset mocks between tests
            ResetMocks();
            _mockCurrentUserService.Reset();
        }

        [Fact]
        public async Task Constructor_WithNullTimeRecordService_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = await AssertExceptionAsync<ArgumentNullException>(() =>
                Task.FromResult(new TimeController(null, _mockCurrentUserService.Object, CreateMockLogger<TimeController>().Object)));

            exception.ParamName.Should().Be("timeRecordService");
        }

        [Fact]
        public async Task Constructor_WithNullCurrentUserService_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = await AssertExceptionAsync<ArgumentNullException>(() =>
                Task.FromResult(new TimeController(MockTimeRecordService.Object, null, CreateMockLogger<TimeController>().Object)));

            exception.ParamName.Should().Be("currentUserService");
        }

        [Fact]
        public async Task Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = await AssertExceptionAsync<ArgumentNullException>(() =>
                Task.FromResult(new TimeController(MockTimeRecordService.Object, _mockCurrentUserService.Object, null)));

            exception.ParamName.Should().Be("logger");
        }

        [Fact]
        public async Task Clock_WithValidRequest_ReturnsOkResult()
        {
            // Arrange
            var request = new TimeRecordRequest
            {
                Type = "in",
                Timestamp = DateTime.UtcNow,
                Location = new LocationModel
                {
                    Latitude = 40.7128,
                    Longitude = -74.0060
                }
            };

            var expectedResponse = new TimeRecordResponse
            {
                Id = "time-record-123",
                Status = "success"
            };

            MockTimeRecordService
                .Setup(s => s.CreateTimeRecordAsync(It.IsAny<TimeRecordRequest>(), It.IsAny<string>()))
                .ReturnsAsync(Result.Success(expectedResponse));

            // Act
            var result = await _controller.Clock(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<TimeRecordResponse>().Subject;
            response.Id.Should().Be(expectedResponse.Id);
            response.Status.Should().Be(expectedResponse.Status);

            MockTimeRecordService.Verify(
                s => s.CreateTimeRecordAsync(It.Is<TimeRecordRequest>(r => r == request), _userId),
                Times.Once
            );
        }

        [Fact]
        public async Task Clock_WithNullRequest_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.Clock(null);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            MockTimeRecordService.Verify(
                s => s.CreateTimeRecordAsync(It.IsAny<TimeRecordRequest>(), It.IsAny<string>()),
                Times.Never
            );
        }

        [Fact]
        public async Task Clock_WhenUserNotAuthenticated_ReturnsUnauthorized()
        {
            // Arrange
            _mockCurrentUserService.Setup(x => x.IsAuthenticated()).Returns(false);
            var request = new TimeRecordRequest
            {
                Type = "in",
                Timestamp = DateTime.UtcNow,
                Location = new LocationModel
                {
                    Latitude = 40.7128,
                    Longitude = -74.0060
                }
            };

            // Act
            var result = await _controller.Clock(request);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
            MockTimeRecordService.Verify(
                s => s.CreateTimeRecordAsync(It.IsAny<TimeRecordRequest>(), It.IsAny<string>()),
                Times.Never
            );
        }

        [Fact]
        public async Task Clock_WhenServiceReturnsFailed_ReturnsBadRequest()
        {
            // Arrange
            var request = new TimeRecordRequest
            {
                Type = "in",
                Timestamp = DateTime.UtcNow,
                Location = new LocationModel
                {
                    Latitude = 40.7128,
                    Longitude = -74.0060
                }
            };

            var errorMessage = "Invalid clock type";
            MockTimeRecordService
                .Setup(s => s.CreateTimeRecordAsync(It.IsAny<TimeRecordRequest>(), It.IsAny<string>()))
                .ReturnsAsync(Result.Failure<TimeRecordResponse>(errorMessage));

            // Act
            var result = await _controller.Clock(request);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value.Should().Be(errorMessage);

            MockTimeRecordService.Verify(
                s => s.CreateTimeRecordAsync(It.Is<TimeRecordRequest>(r => r == request), _userId),
                Times.Once
            );
        }

        [Fact]
        public async Task GetHistory_WithValidParameters_ReturnsOkResult()
        {
            // Arrange
            var pageNumber = 1;
            var pageSize = 10;
            
            var timeRecords = new List<TimeRecord>
            {
                new TimeRecord { Id = 1, UserId = _userId, Type = "ClockIn", Timestamp = DateTime.UtcNow.AddHours(-2) },
                new TimeRecord { Id = 2, UserId = _userId, Type = "ClockOut", Timestamp = DateTime.UtcNow.AddHours(-1) }
            };
            
            var paginatedList = PaginatedList<TimeRecord>.Create(timeRecords, pageNumber, pageSize);
            
            MockTimeRecordService
                .Setup(s => s.GetTimeRecordHistoryAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(Result.Success(paginatedList));

            // Act
            var result = await _controller.GetHistory(pageNumber, pageSize);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<PaginatedList<TimeRecord>>().Subject;
            response.Items.Should().HaveCount(2);
            response.PageNumber.Should().Be(pageNumber);

            MockTimeRecordService.Verify(
                s => s.GetTimeRecordHistoryAsync(_userId, pageNumber, pageSize),
                Times.Once
            );
        }

        [Fact]
        public async Task GetHistory_WithInvalidPageNumber_UseDefaultPageNumber()
        {
            // Arrange
            var pageNumber = 0; // Invalid
            var pageSize = 10;
            
            var paginatedList = new PaginatedList<TimeRecord>(new List<TimeRecord>(), 0, 1, pageSize);
            
            MockTimeRecordService
                .Setup(s => s.GetTimeRecordHistoryAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(Result.Success(paginatedList));

            // Act
            var result = await _controller.GetHistory(pageNumber, pageSize);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            
            MockTimeRecordService.Verify(
                s => s.GetTimeRecordHistoryAsync(_userId, 1, pageSize), // Should use default page number 1
                Times.Once
            );
        }

        [Fact]
        public async Task GetHistory_WithInvalidPageSize_UseDefaultPageSize()
        {
            // Arrange
            var pageNumber = 1;
            var pageSize = 0; // Invalid
            
            var paginatedList = new PaginatedList<TimeRecord>(new List<TimeRecord>(), 0, pageNumber, 10);
            
            MockTimeRecordService
                .Setup(s => s.GetTimeRecordHistoryAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(Result.Success(paginatedList));

            // Act
            var result = await _controller.GetHistory(pageNumber, pageSize);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            
            MockTimeRecordService.Verify(
                s => s.GetTimeRecordHistoryAsync(_userId, pageNumber, 10), // Should use default page size 10
                Times.Once
            );
        }

        [Fact]
        public async Task GetHistory_WhenUserNotAuthenticated_ReturnsUnauthorized()
        {
            // Arrange
            _mockCurrentUserService.Setup(x => x.IsAuthenticated()).Returns(false);

            // Act
            var result = await _controller.GetHistory(1, 10);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
            
            MockTimeRecordService.Verify(
                s => s.GetTimeRecordHistoryAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()),
                Times.Never
            );
        }

        [Fact]
        public async Task GetHistory_WhenServiceReturnsFailed_ReturnsBadRequest()
        {
            // Arrange
            var errorMessage = "Failed to retrieve history";
            
            MockTimeRecordService
                .Setup(s => s.GetTimeRecordHistoryAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(Result.Failure<PaginatedList<TimeRecord>>(errorMessage));

            // Act
            var result = await _controller.GetHistory(1, 10);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value.Should().Be(errorMessage);
            
            MockTimeRecordService.Verify(
                s => s.GetTimeRecordHistoryAsync(_userId, 1, 10),
                Times.Once
            );
        }

        [Fact]
        public async Task GetStatus_WhenAuthenticated_ReturnsOkResult()
        {
            // Arrange
            var status = "in";
            
            MockTimeRecordService
                .Setup(s => s.GetCurrentStatusAsync(It.IsAny<string>()))
                .ReturnsAsync(Result.Success(status));

            // Act
            var result = await _controller.GetStatus();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(status);
            
            MockTimeRecordService.Verify(
                s => s.GetCurrentStatusAsync(_userId),
                Times.Once
            );
        }

        [Fact]
        public async Task GetStatus_WhenUserNotAuthenticated_ReturnsUnauthorized()
        {
            // Arrange
            _mockCurrentUserService.Setup(x => x.IsAuthenticated()).Returns(false);

            // Act
            var result = await _controller.GetStatus();

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
            
            MockTimeRecordService.Verify(
                s => s.GetCurrentStatusAsync(It.IsAny<string>()),
                Times.Never
            );
        }

        [Fact]
        public async Task GetStatus_WhenServiceReturnsFailed_ReturnsBadRequest()
        {
            // Arrange
            var errorMessage = "Failed to retrieve status";
            
            MockTimeRecordService
                .Setup(s => s.GetCurrentStatusAsync(It.IsAny<string>()))
                .ReturnsAsync(Result.Failure<string>(errorMessage));

            // Act
            var result = await _controller.GetStatus();

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value.Should().Be(errorMessage);
            
            MockTimeRecordService.Verify(
                s => s.GetCurrentStatusAsync(_userId),
                Times.Once
            );
        }

        [Fact]
        public async Task GetByDateRange_WithValidDates_ReturnsOkResult()
        {
            // Arrange
            var startDate = DateTime.UtcNow.AddDays(-7);
            var endDate = DateTime.UtcNow;
            
            var timeRecords = new List<TimeRecord>
            {
                new TimeRecord { Id = 1, UserId = _userId, Type = "ClockIn", Timestamp = DateTime.UtcNow.AddDays(-6) },
                new TimeRecord { Id = 2, UserId = _userId, Type = "ClockOut", Timestamp = DateTime.UtcNow.AddDays(-5) }
            };
            
            MockTimeRecordService
                .Setup(s => s.GetTimeRecordsByDateRangeAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(Result.Success<IEnumerable<TimeRecord>>(timeRecords));

            // Act
            var result = await _controller.GetByDateRange(startDate, endDate);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<IEnumerable<TimeRecord>>().Subject;
            response.Should().HaveCount(2);
            
            MockTimeRecordService.Verify(
                s => s.GetTimeRecordsByDateRangeAsync(_userId, startDate, endDate),
                Times.Once
            );
        }

        [Fact]
        public async Task GetByDateRange_WithInvalidDateRange_ReturnsBadRequest()
        {
            // Arrange
            var startDate = DateTime.UtcNow;
            var endDate = DateTime.UtcNow.AddDays(-7); // End date is before start date

            // Act
            var result = await _controller.GetByDateRange(startDate, endDate);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            
            MockTimeRecordService.Verify(
                s => s.GetTimeRecordsByDateRangeAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()),
                Times.Never
            );
        }

        [Fact]
        public async Task GetByDateRange_WithDefaultDates_ReturnsBadRequest()
        {
            // Act
            var resultWithDefaultStartDate = await _controller.GetByDateRange(default, DateTime.UtcNow);
            var resultWithDefaultEndDate = await _controller.GetByDateRange(DateTime.UtcNow, default);

            // Assert
            resultWithDefaultStartDate.Should().BeOfType<BadRequestObjectResult>();
            resultWithDefaultEndDate.Should().BeOfType<BadRequestObjectResult>();
            
            MockTimeRecordService.Verify(
                s => s.GetTimeRecordsByDateRangeAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()),
                Times.Never
            );
        }

        [Fact]
        public async Task GetByDateRange_WhenUserNotAuthenticated_ReturnsUnauthorized()
        {
            // Arrange
            _mockCurrentUserService.Setup(x => x.IsAuthenticated()).Returns(false);
            var startDate = DateTime.UtcNow.AddDays(-7);
            var endDate = DateTime.UtcNow;

            // Act
            var result = await _controller.GetByDateRange(startDate, endDate);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
            
            MockTimeRecordService.Verify(
                s => s.GetTimeRecordsByDateRangeAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()),
                Times.Never
            );
        }

        [Fact]
        public async Task GetByDateRange_WhenServiceReturnsFailed_ReturnsBadRequest()
        {
            // Arrange
            var startDate = DateTime.UtcNow.AddDays(-7);
            var endDate = DateTime.UtcNow;
            var errorMessage = "Failed to retrieve records";
            
            MockTimeRecordService
                .Setup(s => s.GetTimeRecordsByDateRangeAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(Result.Failure<IEnumerable<TimeRecord>>(errorMessage));

            // Act
            var result = await _controller.GetByDateRange(startDate, endDate);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value.Should().Be(errorMessage);
            
            MockTimeRecordService.Verify(
                s => s.GetTimeRecordsByDateRangeAsync(_userId, startDate, endDate),
                Times.Once
            );
        }
    }
}