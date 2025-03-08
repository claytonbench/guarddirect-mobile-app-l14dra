using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
    public class ReportControllerTests : TestBase
    {
        private ReportController _controller;
        private Mock<ICurrentUserService> _mockCurrentUserService;
        private string _testUserId;

        public ReportControllerTests()
        {
            _testUserId = "user1";
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockCurrentUserService.Setup(m => m.GetUserId()).Returns(_testUserId);

            var mockLogger = CreateMockLogger<ReportController>();
            
            _controller = new ReportController(
                MockReportService.Object,
                _mockCurrentUserService.Object,
                mockLogger.Object);
        }

        private void Setup()
        {
            // Reset mocks to ensure clean state for each test
            ResetMocks();
            _mockCurrentUserService.Setup(m => m.GetUserId()).Returns(_testUserId);
            
            // Recreate controller with fresh mocks
            var mockLogger = CreateMockLogger<ReportController>();
            _controller = new ReportController(
                MockReportService.Object,
                _mockCurrentUserService.Object,
                mockLogger.Object);
        }

        [Fact]
        public async Task CreateReport_ValidRequest_ReturnsOkResult()
        {
            // Arrange
            Setup();
            var request = new ReportRequest
            {
                Text = "Test report",
                Timestamp = DateTime.UtcNow,
                Location = new LocationData { Latitude = 40.7128, Longitude = -74.0060 }
            };

            MockReportService.Setup(s => s.CreateReportAsync(It.IsAny<ReportRequest>(), It.IsAny<string>()))
                .ReturnsAsync(Result.Success(new ReportResponse { Id = "report-123", Status = "success" }));
            
            // Act
            var result = await _controller.CreateReport(request);
            
            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var reportResponse = Assert.IsType<ReportResponse>(okResult.Value);
            Assert.Equal("report-123", reportResponse.Id);
            Assert.Equal("success", reportResponse.Status);
            
            // Verify service was called with correct parameters
            MockReportService.Verify(s => s.CreateReportAsync(request, _testUserId), Times.Once);
        }

        [Fact]
        public async Task CreateReport_InvalidRequest_ReturnsBadRequest()
        {
            // Arrange
            Setup();
            
            // Act - null request
            var nullResult = await _controller.CreateReport(null);
            
            // Assert
            Assert.IsType<BadRequestObjectResult>(nullResult);
            
            // Arrange - empty text
            var emptyTextRequest = new ReportRequest { Text = "" };
            
            // Act
            var emptyTextResult = await _controller.CreateReport(emptyTextRequest);
            
            // Assert
            Assert.IsType<BadRequestObjectResult>(emptyTextResult);
            
            // Arrange - text too long
            var longTextRequest = new ReportRequest { Text = new string('x', 501) };
            
            // Act
            var longTextResult = await _controller.CreateReport(longTextRequest);
            
            // Assert
            Assert.IsType<BadRequestObjectResult>(longTextResult);
            
            // Verify service was never called
            MockReportService.Verify(s => s.CreateReportAsync(It.IsAny<ReportRequest>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task CreateReport_ServiceFailure_ReturnsInternalServerError()
        {
            // Arrange
            Setup();
            var request = new ReportRequest
            {
                Text = "Test report",
                Timestamp = DateTime.UtcNow,
                Location = new LocationData { Latitude = 40.7128, Longitude = -74.0060 }
            };
            
            MockReportService.Setup(s => s.CreateReportAsync(It.IsAny<ReportRequest>(), It.IsAny<string>()))
                .ReturnsAsync(Result.Failure<ReportResponse>("Service error"));
            
            // Act
            var result = await _controller.CreateReport(request);
            
            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("Service error", statusCodeResult.Value);
            
            // Verify service was called
            MockReportService.Verify(s => s.CreateReportAsync(request, _testUserId), Times.Once);
        }

        [Fact]
        public async Task GetReports_ValidRequest_ReturnsOkResultWithReports()
        {
            // Arrange
            Setup();
            var pageNumber = 1;
            var pageSize = 10;
            var reports = TestData.GetTestReports().Where(r => r.UserId == _testUserId).ToList();
            var paginatedList = PaginatedList<Report>.Create(reports, pageNumber, pageSize);
            
            MockReportService.Setup(s => s.GetPaginatedReportsByUserIdAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(Result.Success(paginatedList));
            
            // Act
            var result = await _controller.GetReports(pageNumber, pageSize);
            
            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedList = Assert.IsType<PaginatedList<Report>>(okResult.Value);
            Assert.Equal(reports.Count, returnedList.Items.Count);
            Assert.Equal(pageNumber, returnedList.PageNumber);
            
            // Verify service was called with correct parameters
            MockReportService.Verify(s => s.GetPaginatedReportsByUserIdAsync(_testUserId, pageNumber, pageSize), Times.Once);
        }

        [Fact]
        public async Task GetReports_ServiceFailure_ReturnsInternalServerError()
        {
            // Arrange
            Setup();
            var pageNumber = 1;
            var pageSize = 10;
            
            MockReportService.Setup(s => s.GetPaginatedReportsByUserIdAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(Result.Failure<PaginatedList<Report>>("Service error"));
            
            // Act
            var result = await _controller.GetReports(pageNumber, pageSize);
            
            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            
            // Verify service was called
            MockReportService.Verify(s => s.GetPaginatedReportsByUserIdAsync(_testUserId, pageNumber, pageSize), Times.Once);
        }

        [Fact]
        public async Task GetReportById_ExistingId_ReturnsOkResultWithReport()
        {
            // Arrange
            Setup();
            var reportId = 1;
            var report = TestData.GetTestReportById(reportId);
            report.UserId = _testUserId; // Ensure it belongs to the test user
            
            MockReportService.Setup(s => s.GetReportByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(Result.Success(report));
            
            // Act
            var result = await _controller.GetReportById(reportId);
            
            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedReport = Assert.IsType<Report>(okResult.Value);
            Assert.Equal(reportId, returnedReport.Id);
            Assert.Equal(_testUserId, returnedReport.UserId);
            
            // Verify service was called with correct parameter
            MockReportService.Verify(s => s.GetReportByIdAsync(reportId), Times.Once);
        }

        [Fact]
        public async Task GetReportById_NonExistingId_ReturnsNotFound()
        {
            // Arrange
            Setup();
            var reportId = 999; // Non-existing ID
            
            MockReportService.Setup(s => s.GetReportByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(Result.Failure<Report>("Report not found"));
            
            // Act
            var result = await _controller.GetReportById(reportId);
            
            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
            
            // Verify service was called
            MockReportService.Verify(s => s.GetReportByIdAsync(reportId), Times.Once);
        }

        [Fact]
        public async Task GetReportById_UnauthorizedAccess_ReturnsUnauthorized()
        {
            // Arrange
            Setup();
            var reportId = 1;
            var report = TestData.GetTestReportById(reportId);
            report.UserId = "differentUser"; // Set to a different user ID
            
            MockReportService.Setup(s => s.GetReportByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(Result.Success(report));
            
            // Act
            var result = await _controller.GetReportById(reportId);
            
            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result);
            
            // Verify service was called
            MockReportService.Verify(s => s.GetReportByIdAsync(reportId), Times.Once);
        }

        [Fact]
        public async Task GetReportsByDateRange_ValidDates_ReturnsOkResultWithReports()
        {
            // Arrange
            Setup();
            var startDate = DateTime.UtcNow.AddDays(-7);
            var endDate = DateTime.UtcNow;
            var reports = TestData.GetTestReports();
            
            MockReportService.Setup(s => s.GetReportsByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(Result.Success<IEnumerable<Report>>(reports));
            
            // Act
            var result = await _controller.GetReportsByDateRange(startDate, endDate);
            
            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedReports = Assert.IsAssignableFrom<IEnumerable<Report>>(okResult.Value);
            
            // Verify all returned reports belong to the test user
            foreach (var report in returnedReports)
            {
                Assert.Equal(_testUserId, report.UserId);
            }
            
            // Verify service was called with correct parameters
            MockReportService.Verify(s => s.GetReportsByDateRangeAsync(startDate, endDate), Times.Once);
        }

        [Fact]
        public async Task GetReportsByDateRange_InvalidDates_ReturnsBadRequest()
        {
            // Arrange
            Setup();
            var startDate = DateTime.UtcNow;
            var endDate = DateTime.UtcNow.AddDays(-7); // End date before start date
            
            // Act
            var result = await _controller.GetReportsByDateRange(startDate, endDate);
            
            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
            
            // Verify service was not called
            MockReportService.Verify(s => s.GetReportsByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never);
        }

        [Fact]
        public async Task UpdateReport_ValidRequest_ReturnsOkResult()
        {
            // Arrange
            Setup();
            var reportId = 1;
            var request = new ReportRequest
            {
                Text = "Updated report text",
                Timestamp = DateTime.UtcNow,
                Location = new LocationData { Latitude = 40.7128, Longitude = -74.0060 }
            };
            
            MockReportService.Setup(s => s.UpdateReportAsync(It.IsAny<int>(), It.IsAny<ReportRequest>(), It.IsAny<string>()))
                .ReturnsAsync(Result.Success());
            
            // Act
            var result = await _controller.UpdateReport(reportId, request);
            
            // Assert
            Assert.IsType<OkObjectResult>(result);
            
            // Verify service was called with correct parameters
            MockReportService.Verify(s => s.UpdateReportAsync(reportId, request, _testUserId), Times.Once);
        }

        [Fact]
        public async Task UpdateReport_NonExistingId_ReturnsNotFound()
        {
            // Arrange
            Setup();
            var reportId = 999; // Non-existing ID
            var request = new ReportRequest
            {
                Text = "Updated report text",
                Timestamp = DateTime.UtcNow,
                Location = new LocationData { Latitude = 40.7128, Longitude = -74.0060 }
            };
            
            MockReportService.Setup(s => s.UpdateReportAsync(It.IsAny<int>(), It.IsAny<ReportRequest>(), It.IsAny<string>()))
                .ReturnsAsync(Result.Failure("Report not found"));
            
            // Act
            var result = await _controller.UpdateReport(reportId, request);
            
            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
            
            // Verify service was called
            MockReportService.Verify(s => s.UpdateReportAsync(reportId, request, _testUserId), Times.Once);
        }

        [Fact]
        public async Task UpdateReport_UnauthorizedAccess_ReturnsUnauthorized()
        {
            // Arrange
            Setup();
            var reportId = 1;
            var request = new ReportRequest
            {
                Text = "Updated report text",
                Timestamp = DateTime.UtcNow,
                Location = new LocationData { Latitude = 40.7128, Longitude = -74.0060 }
            };
            
            MockReportService.Setup(s => s.UpdateReportAsync(It.IsAny<int>(), It.IsAny<ReportRequest>(), It.IsAny<string>()))
                .ReturnsAsync(Result.Failure("User is not authorized to update this report"));
            
            // Act
            var result = await _controller.UpdateReport(reportId, request);
            
            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result);
            
            // Verify service was called
            MockReportService.Verify(s => s.UpdateReportAsync(reportId, request, _testUserId), Times.Once);
        }

        [Fact]
        public async Task DeleteReport_ExistingId_ReturnsOkResult()
        {
            // Arrange
            Setup();
            var reportId = 1;
            
            MockReportService.Setup(s => s.DeleteReportAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(Result.Success());
            
            // Act
            var result = await _controller.DeleteReport(reportId);
            
            // Assert
            Assert.IsType<OkObjectResult>(result);
            
            // Verify service was called with correct parameters
            MockReportService.Verify(s => s.DeleteReportAsync(reportId, _testUserId), Times.Once);
        }

        [Fact]
        public async Task DeleteReport_NonExistingId_ReturnsNotFound()
        {
            // Arrange
            Setup();
            var reportId = 999; // Non-existing ID
            
            MockReportService.Setup(s => s.DeleteReportAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(Result.Failure("Report not found"));
            
            // Act
            var result = await _controller.DeleteReport(reportId);
            
            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
            
            // Verify service was called
            MockReportService.Verify(s => s.DeleteReportAsync(reportId, _testUserId), Times.Once);
        }

        [Fact]
        public async Task DeleteReport_UnauthorizedAccess_ReturnsUnauthorized()
        {
            // Arrange
            Setup();
            var reportId = 1;
            
            MockReportService.Setup(s => s.DeleteReportAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(Result.Failure("User is not authorized to delete this report"));
            
            // Act
            var result = await _controller.DeleteReport(reportId);
            
            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result);
            
            // Verify service was called
            MockReportService.Verify(s => s.DeleteReportAsync(reportId, _testUserId), Times.Once);
        }
    }
}