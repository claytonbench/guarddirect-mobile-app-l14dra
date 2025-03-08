using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using FluentAssertions;
using SecurityPatrol.API.Controllers;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.Core.Models;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.API.UnitTests.Setup;

namespace SecurityPatrol.API.UnitTests.Controllers
{
    /// <summary>
    /// Contains unit tests for the ReportController class to verify the functionality of API endpoints for activity reports.
    /// </summary>
    public class ReportControllerTests : TestBase
    {
        /// <summary>
        /// Initializes a new instance of the ReportControllerTests class
        /// </summary>
        public ReportControllerTests() : base()
        {
            // Base constructor initializes mock services
        }

        /// <summary>
        /// Tests that the CreateReport endpoint returns an OK result with the expected response when given a valid request
        /// </summary>
        [Fact]
        public async Task CreateReport_WithValidRequest_ReturnsOkResult()
        {
            // Arrange
            var request = new ReportRequest
            {
                Text = "Test report",
                Timestamp = DateTime.UtcNow,
                Location = new LocationData { Latitude = 34.0522, Longitude = -118.2437 }
            };

            var response = new ReportResponse
            {
                Id = "123",
                Status = "Success"
            };

            MockReportService
                .Setup(service => service.CreateReportAsync(It.IsAny<ReportRequest>(), It.IsAny<string>()))
                .ReturnsAsync(Result<ReportResponse>.Success(response));

            var mockCurrentUserService = new Mock<ICurrentUserService>();
            mockCurrentUserService.Setup(service => service.GetUserId()).Returns("test-user-123");

            var controller = CreateReportController();
            var controllerField = typeof(ReportController).GetField("_currentUserService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            controllerField.SetValue(controller, mockCurrentUserService.Object);

            SetupHttpContext(controller);

            // Act
            var result = await controller.CreateReport(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeOfType<ReportResponse>().Subject;
            returnValue.Id.Should().Be("123");
            returnValue.Status.Should().Be("Success");

            MockReportService.Verify(
                service => service.CreateReportAsync(
                    It.Is<ReportRequest>(r => r.Text == request.Text),
                    It.Is<string>(id => id == "test-user-123")),
                Times.Once);
        }

        /// <summary>
        /// Tests that the CreateReport endpoint returns a BadRequest result when given an invalid request
        /// </summary>
        [Fact]
        public async Task CreateReport_WithInvalidRequest_ReturnsBadRequest()
        {
            // Arrange
            ReportRequest request = null;

            var mockCurrentUserService = new Mock<ICurrentUserService>();
            mockCurrentUserService.Setup(service => service.GetUserId()).Returns("test-user-123");

            var controller = CreateReportController();
            var controllerField = typeof(ReportController).GetField("_currentUserService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            controllerField.SetValue(controller, mockCurrentUserService.Object);

            SetupHttpContext(controller);

            // Act
            var result = await controller.CreateReport(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();

            MockReportService.Verify(
                service => service.CreateReportAsync(It.IsAny<ReportRequest>(), It.IsAny<string>()),
                Times.Never);
        }

        /// <summary>
        /// Tests that the GetReports endpoint returns an OK result with paginated reports when given valid parameters
        /// </summary>
        [Fact]
        public async Task GetReports_WithValidParameters_ReturnsOkResultWithPaginatedReports()
        {
            // Arrange
            int pageNumber = 1;
            int pageSize = 10;

            var reports = new List<Report>
            {
                new Report { Id = 1, Text = "Report 1", UserId = "test-user-123", Timestamp = DateTime.UtcNow },
                new Report { Id = 2, Text = "Report 2", UserId = "test-user-123", Timestamp = DateTime.UtcNow }
            };

            var paginatedReports = new PaginatedList<Report>(reports, 2, pageNumber, pageSize);

            MockReportService
                .Setup(service => service.GetPaginatedReportsByUserIdAsync(It.IsAny<string>(), pageNumber, pageSize))
                .ReturnsAsync(Result<PaginatedList<Report>>.Success(paginatedReports));

            var mockCurrentUserService = new Mock<ICurrentUserService>();
            mockCurrentUserService.Setup(service => service.GetUserId()).Returns("test-user-123");

            var controller = CreateReportController();
            var controllerField = typeof(ReportController).GetField("_currentUserService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            controllerField.SetValue(controller, mockCurrentUserService.Object);

            SetupHttpContext(controller);

            // Act
            var result = await controller.GetReports(pageNumber, pageSize);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeOfType<PaginatedList<Report>>().Subject;
            returnValue.Items.Should().HaveCount(2);
            returnValue.PageNumber.Should().Be(pageNumber);

            MockReportService.Verify(
                service => service.GetPaginatedReportsByUserIdAsync(
                    It.Is<string>(id => id == "test-user-123"),
                    It.Is<int>(p => p == pageNumber),
                    It.Is<int>(p => p == pageSize)),
                Times.Once);
        }

        /// <summary>
        /// Tests that the GetReportById endpoint returns an OK result with the requested report when given a valid ID
        /// </summary>
        [Fact]
        public async Task GetReportById_WithValidId_ReturnsOkResultWithReport()
        {
            // Arrange
            int reportId = 1;
            var report = new Report
            {
                Id = reportId,
                Text = "Test report",
                UserId = "test-user-123",
                Timestamp = DateTime.UtcNow,
                Latitude = 34.0522,
                Longitude = -118.2437
            };

            MockReportService
                .Setup(service => service.GetReportByIdAsync(reportId))
                .ReturnsAsync(Result<Report>.Success(report));

            var mockCurrentUserService = new Mock<ICurrentUserService>();
            mockCurrentUserService.Setup(service => service.GetUserId()).Returns("test-user-123");

            var controller = CreateReportController();
            var controllerField = typeof(ReportController).GetField("_currentUserService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            controllerField.SetValue(controller, mockCurrentUserService.Object);

            SetupHttpContext(controller);

            // Act
            var result = await controller.GetReportById(reportId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeOfType<Report>().Subject;
            returnValue.Id.Should().Be(reportId);
            returnValue.Text.Should().Be("Test report");
            returnValue.UserId.Should().Be("test-user-123");

            MockReportService.Verify(
                service => service.GetReportByIdAsync(It.Is<int>(id => id == reportId)),
                Times.Once);
        }

        /// <summary>
        /// Tests that the GetReportById endpoint returns a NotFound result when given an ID that doesn't exist
        /// </summary>
        [Fact]
        public async Task GetReportById_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            int reportId = 999;

            MockReportService
                .Setup(service => service.GetReportByIdAsync(reportId))
                .ReturnsAsync(Result<Report>.Failure("Report not found"));

            var mockCurrentUserService = new Mock<ICurrentUserService>();
            mockCurrentUserService.Setup(service => service.GetUserId()).Returns("test-user-123");

            var controller = CreateReportController();
            var controllerField = typeof(ReportController).GetField("_currentUserService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            controllerField.SetValue(controller, mockCurrentUserService.Object);

            SetupHttpContext(controller);

            // Act
            var result = await controller.GetReportById(reportId);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();

            MockReportService.Verify(
                service => service.GetReportByIdAsync(It.Is<int>(id => id == reportId)),
                Times.Once);
        }

        /// <summary>
        /// Tests that the GetReportById endpoint returns an Unauthorized result when the report belongs to another user
        /// </summary>
        [Fact]
        public async Task GetReportById_WithReportBelongingToAnotherUser_ReturnsUnauthorized()
        {
            // Arrange
            int reportId = 1;
            var report = new Report
            {
                Id = reportId,
                Text = "Test report",
                UserId = "other-user-123",
                Timestamp = DateTime.UtcNow
            };

            MockReportService
                .Setup(service => service.GetReportByIdAsync(reportId))
                .ReturnsAsync(Result<Report>.Success(report));

            var mockCurrentUserService = new Mock<ICurrentUserService>();
            mockCurrentUserService.Setup(service => service.GetUserId()).Returns("test-user-123");

            var controller = CreateReportController();
            var controllerField = typeof(ReportController).GetField("_currentUserService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            controllerField.SetValue(controller, mockCurrentUserService.Object);

            SetupHttpContext(controller);

            // Act
            var result = await controller.GetReportById(reportId);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();

            MockReportService.Verify(
                service => service.GetReportByIdAsync(It.Is<int>(id => id == reportId)),
                Times.Once);
        }

        /// <summary>
        /// Tests that the GetReportsByDateRange endpoint returns an OK result with reports in the date range
        /// </summary>
        [Fact]
        public async Task GetReportsByDateRange_WithValidDates_ReturnsOkResultWithReports()
        {
            // Arrange
            DateTime startDate = DateTime.UtcNow.AddDays(-7);
            DateTime endDate = DateTime.UtcNow;

            var reports = new List<Report>
            {
                new Report { Id = 1, Text = "Report 1", UserId = "test-user-123", Timestamp = DateTime.UtcNow.AddDays(-5) },
                new Report { Id = 2, Text = "Report 2", UserId = "test-user-123", Timestamp = DateTime.UtcNow.AddDays(-3) },
                new Report { Id = 3, Text = "Report 3", UserId = "other-user-456", Timestamp = DateTime.UtcNow.AddDays(-2) }
            };

            MockReportService
                .Setup(service => service.GetReportsByDateRangeAsync(startDate, endDate))
                .ReturnsAsync(Result<IEnumerable<Report>>.Success(reports));

            var mockCurrentUserService = new Mock<ICurrentUserService>();
            mockCurrentUserService.Setup(service => service.GetUserId()).Returns("test-user-123");

            var controller = CreateReportController();
            var controllerField = typeof(ReportController).GetField("_currentUserService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            controllerField.SetValue(controller, mockCurrentUserService.Object);

            SetupHttpContext(controller);

            // Act
            var result = await controller.GetReportsByDateRange(startDate, endDate);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeAssignableTo<IEnumerable<Report>>().Subject;
            returnValue.Should().HaveCount(2); // Only the reports belonging to test-user-123

            MockReportService.Verify(
                service => service.GetReportsByDateRangeAsync(
                    It.Is<DateTime>(d => d == startDate),
                    It.Is<DateTime>(d => d == endDate)),
                Times.Once);
        }

        /// <summary>
        /// Tests that the UpdateReport endpoint returns an OK result when given a valid request
        /// </summary>
        [Fact]
        public async Task UpdateReport_WithValidRequest_ReturnsOkResult()
        {
            // Arrange
            int reportId = 1;
            var request = new ReportRequest
            {
                Text = "Updated report",
                Timestamp = DateTime.UtcNow,
                Location = new LocationData { Latitude = 34.0522, Longitude = -118.2437 }
            };

            MockReportService
                .Setup(service => service.UpdateReportAsync(reportId, It.IsAny<ReportRequest>(), It.IsAny<string>()))
                .ReturnsAsync(Result.Success());

            var mockCurrentUserService = new Mock<ICurrentUserService>();
            mockCurrentUserService.Setup(service => service.GetUserId()).Returns("test-user-123");

            var controller = CreateReportController();
            var controllerField = typeof(ReportController).GetField("_currentUserService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            controllerField.SetValue(controller, mockCurrentUserService.Object);

            SetupHttpContext(controller);

            // Act
            var result = await controller.UpdateReport(reportId, request);

            // Assert
            result.Should().BeOfType<OkObjectResult>();

            MockReportService.Verify(
                service => service.UpdateReportAsync(
                    It.Is<int>(id => id == reportId),
                    It.Is<ReportRequest>(r => r.Text == request.Text),
                    It.Is<string>(id => id == "test-user-123")),
                Times.Once);
        }

        /// <summary>
        /// Tests that the UpdateReport endpoint returns a NotFound result when given an ID that doesn't exist
        /// </summary>
        [Fact]
        public async Task UpdateReport_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            int reportId = 999;
            var request = new ReportRequest
            {
                Text = "Updated report",
                Timestamp = DateTime.UtcNow,
                Location = new LocationData { Latitude = 34.0522, Longitude = -118.2437 }
            };

            MockReportService
                .Setup(service => service.UpdateReportAsync(reportId, It.IsAny<ReportRequest>(), It.IsAny<string>()))
                .ReturnsAsync(Result.Failure("Report not found"));

            var mockCurrentUserService = new Mock<ICurrentUserService>();
            mockCurrentUserService.Setup(service => service.GetUserId()).Returns("test-user-123");

            var controller = CreateReportController();
            var controllerField = typeof(ReportController).GetField("_currentUserService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            controllerField.SetValue(controller, mockCurrentUserService.Object);

            SetupHttpContext(controller);

            // Act
            var result = await controller.UpdateReport(reportId, request);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();

            MockReportService.Verify(
                service => service.UpdateReportAsync(
                    It.Is<int>(id => id == reportId),
                    It.IsAny<ReportRequest>(),
                    It.IsAny<string>()),
                Times.Once);
        }

        /// <summary>
        /// Tests that the DeleteReport endpoint returns an OK result when given a valid ID
        /// </summary>
        [Fact]
        public async Task DeleteReport_WithValidId_ReturnsOkResult()
        {
            // Arrange
            int reportId = 1;

            MockReportService
                .Setup(service => service.DeleteReportAsync(reportId, It.IsAny<string>()))
                .ReturnsAsync(Result.Success());

            var mockCurrentUserService = new Mock<ICurrentUserService>();
            mockCurrentUserService.Setup(service => service.GetUserId()).Returns("test-user-123");

            var controller = CreateReportController();
            var controllerField = typeof(ReportController).GetField("_currentUserService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            controllerField.SetValue(controller, mockCurrentUserService.Object);

            SetupHttpContext(controller);

            // Act
            var result = await controller.DeleteReport(reportId);

            // Assert
            result.Should().BeOfType<OkObjectResult>();

            MockReportService.Verify(
                service => service.DeleteReportAsync(
                    It.Is<int>(id => id == reportId),
                    It.Is<string>(id => id == "test-user-123")),
                Times.Once);
        }

        /// <summary>
        /// Tests that the DeleteReport endpoint returns a NotFound result when given an ID that doesn't exist
        /// </summary>
        [Fact]
        public async Task DeleteReport_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            int reportId = 999;

            MockReportService
                .Setup(service => service.DeleteReportAsync(reportId, It.IsAny<string>()))
                .ReturnsAsync(Result.Failure("Report not found"));

            var mockCurrentUserService = new Mock<ICurrentUserService>();
            mockCurrentUserService.Setup(service => service.GetUserId()).Returns("test-user-123");

            var controller = CreateReportController();
            var controllerField = typeof(ReportController).GetField("_currentUserService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            controllerField.SetValue(controller, mockCurrentUserService.Object);

            SetupHttpContext(controller);

            // Act
            var result = await controller.DeleteReport(reportId);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();

            MockReportService.Verify(
                service => service.DeleteReportAsync(
                    It.Is<int>(id => id == reportId),
                    It.IsAny<string>()),
                Times.Once);
        }
    }
}