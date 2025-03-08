using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using SecurityPatrol.API.UnitTests.Setup;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Core.Models;
using SecurityPatrol.Application.Services;
using SecurityPatrol.TestCommon.Constants;

namespace SecurityPatrol.API.UnitTests.Services
{
    /// <summary>
    /// Contains unit tests for the ReportService class to verify the correct behavior of report-related operations.
    /// </summary>
    public class ReportServiceTests : TestBase
    {
        private readonly Mock<IReportRepository> _mockReportRepository;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<ILogger<ReportService>> _mockLogger;
        private readonly ReportService _reportService;
        private readonly string _testUserId;

        /// <summary>
        /// Initializes a new instance of the ReportServiceTests class with mocked dependencies.
        /// </summary>
        public ReportServiceTests()
        {
            // Initialize mocks
            _mockReportRepository = new Mock<IReportRepository>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockLogger = new Mock<ILogger<ReportService>>();
            
            // Set up current user service
            _mockCurrentUserService.Setup(x => x.GetUserId()).Returns(TestConstants.TestUserId);
            _mockCurrentUserService.Setup(x => x.IsAuthenticated()).Returns(true);
            
            // Initialize service with mocked dependencies
            _reportService = new ReportService(
                _mockReportRepository.Object,
                _mockCurrentUserService.Object,
                _mockLogger.Object);
            
            // Store test user ID for reuse
            _testUserId = TestConstants.TestUserId;
        }

        /// <summary>
        /// Tests that CreateReportAsync returns a success result when provided with a valid request.
        /// </summary>
        [Fact]
        public async Task CreateReportAsync_WithValidRequest_ReturnsSuccessResult()
        {
            // Arrange: Create a valid ReportRequest with text, timestamp, and location
            var request = new ReportRequest
            {
                Text = "Test report text",
                Timestamp = DateTime.UtcNow,
                Location = new LocationData { Latitude = TestConstants.TestLatitude, Longitude = TestConstants.TestLongitude }
            };
            
            // Arrange: Set up _mockReportRepository.AddAsync to return a Report with Id=1
            var createdReport = new Report
            {
                Id = 1,
                Text = request.Text,
                Timestamp = request.Timestamp,
                Latitude = request.Location.Latitude,
                Longitude = request.Location.Longitude,
                UserId = _testUserId
            };
            
            _mockReportRepository.Setup(x => x.AddAsync(It.IsAny<Report>()))
                .ReturnsAsync(createdReport);
            
            // Act: Call _reportService.CreateReportAsync with the request and _testUserId
            var result = await _reportService.CreateReportAsync(request, _testUserId);
            
            // Assert: Verify the result is successful
            result.Succeeded.Should().BeTrue();
            // Assert: Verify the result contains a ReportResponse with the correct Id
            result.Data.Should().NotBeNull();
            result.Data.Id.Should().Be("1");
            // Assert: Verify _mockReportRepository.AddAsync was called once with the correct parameters
            _mockReportRepository.Verify(x => x.AddAsync(It.Is<Report>(r => 
                r.Text == request.Text && 
                r.Latitude == request.Location.Latitude && 
                r.Longitude == request.Location.Longitude && 
                r.UserId == _testUserId)), 
                Times.Once);
        }

        /// <summary>
        /// Tests that CreateReportAsync returns a failure result when provided with a null request.
        /// </summary>
        [Fact]
        public async Task CreateReportAsync_WithNullRequest_ReturnsFailureResult()
        {
            // Arrange: Set up null request
            ReportRequest request = null;
            
            // Act: Call _reportService.CreateReportAsync with null request and _testUserId
            var result = await _reportService.CreateReportAsync(request, _testUserId);
            
            // Assert: Verify the result is not successful
            result.Succeeded.Should().BeFalse();
            // Assert: Verify the result contains an appropriate error message
            result.Message.Should().Contain("cannot be null");
            // Assert: Verify _mockReportRepository.AddAsync was not called
            _mockReportRepository.Verify(x => x.AddAsync(It.IsAny<Report>()), Times.Never);
        }

        /// <summary>
        /// Tests that CreateReportAsync returns a failure result when provided with a request containing empty text.
        /// </summary>
        [Fact]
        public async Task CreateReportAsync_WithEmptyText_ReturnsFailureResult()
        {
            // Arrange: Create a ReportRequest with empty text
            var request = new ReportRequest
            {
                Text = string.Empty,
                Timestamp = DateTime.UtcNow,
                Location = new LocationData { Latitude = TestConstants.TestLatitude, Longitude = TestConstants.TestLongitude }
            };
            
            // Act: Call _reportService.CreateReportAsync with the request and _testUserId
            var result = await _reportService.CreateReportAsync(request, _testUserId);
            
            // Assert: Verify the result is not successful
            result.Succeeded.Should().BeFalse();
            // Assert: Verify the result contains an appropriate error message
            result.Message.Should().Contain("Text cannot be empty");
            // Assert: Verify _mockReportRepository.AddAsync was not called
            _mockReportRepository.Verify(x => x.AddAsync(It.IsAny<Report>()), Times.Never);
        }

        /// <summary>
        /// Tests that CreateReportAsync returns a failure result when provided with a null user ID.
        /// </summary>
        [Fact]
        public async Task CreateReportAsync_WithNullUserId_ReturnsFailureResult()
        {
            // Arrange: Create a valid ReportRequest
            var request = new ReportRequest
            {
                Text = "Test report text",
                Timestamp = DateTime.UtcNow,
                Location = new LocationData { Latitude = TestConstants.TestLatitude, Longitude = TestConstants.TestLongitude }
            };
            
            // Act: Call _reportService.CreateReportAsync with the request and null userId
            var result = await _reportService.CreateReportAsync(request, null);
            
            // Assert: Verify the result is not successful
            result.Succeeded.Should().BeFalse();
            // Assert: Verify the result contains an appropriate error message
            result.Message.Should().Contain("User ID cannot be null or empty");
            // Assert: Verify _mockReportRepository.AddAsync was not called
            _mockReportRepository.Verify(x => x.AddAsync(It.IsAny<Report>()), Times.Never);
        }

        /// <summary>
        /// Tests that CreateReportAsync returns a failure result when the repository throws an exception.
        /// </summary>
        [Fact]
        public async Task CreateReportAsync_WhenRepositoryThrowsException_ReturnsFailureResult()
        {
            // Arrange: Create a valid ReportRequest
            var request = new ReportRequest
            {
                Text = "Test report text",
                Timestamp = DateTime.UtcNow,
                Location = new LocationData { Latitude = TestConstants.TestLatitude, Longitude = TestConstants.TestLongitude }
            };
            
            // Arrange: Set up _mockReportRepository.AddAsync to throw an exception
            _mockReportRepository.Setup(x => x.AddAsync(It.IsAny<Report>()))
                .ThrowsAsync(new Exception("Database error"));
            
            // Act: Call _reportService.CreateReportAsync with the request and _testUserId
            var result = await _reportService.CreateReportAsync(request, _testUserId);
            
            // Assert: Verify the result is not successful
            result.Succeeded.Should().BeFalse();
            // Assert: Verify the result contains an appropriate error message
            result.Message.Should().Contain("Failed to create report");
            // Assert: Verify _mockLogger.LogError was called with the exception details
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }

        /// <summary>
        /// Tests that GetReportByIdAsync returns a success result with the report when provided with a valid ID.
        /// </summary>
        [Fact]
        public async Task GetReportByIdAsync_WithValidId_ReturnsSuccessResult()
        {
            // Arrange: Create a test report with Id=1
            var reportId = 1;
            var report = new Report
            {
                Id = reportId,
                Text = "Test report",
                Timestamp = DateTime.UtcNow,
                UserId = _testUserId
            };
            
            // Arrange: Set up _mockReportRepository.GetByIdAsync to return the test report
            _mockReportRepository.Setup(x => x.GetByIdAsync(reportId))
                .ReturnsAsync(report);
            
            // Act: Call _reportService.GetReportByIdAsync with Id=1
            var result = await _reportService.GetReportByIdAsync(reportId);
            
            // Assert: Verify the result is successful
            result.Succeeded.Should().BeTrue();
            // Assert: Verify the result contains the correct report
            result.Data.Should().NotBeNull();
            result.Data.Id.Should().Be(reportId);
            // Assert: Verify _mockReportRepository.GetByIdAsync was called once with Id=1
            _mockReportRepository.Verify(x => x.GetByIdAsync(reportId), Times.Once);
        }

        /// <summary>
        /// Tests that GetReportByIdAsync returns a failure result when provided with an invalid ID.
        /// </summary>
        [Fact]
        public async Task GetReportByIdAsync_WithInvalidId_ReturnsFailureResult()
        {
            // Arrange: Set up _mockReportRepository.GetByIdAsync to return null
            _mockReportRepository.Setup(x => x.GetByIdAsync(999))
                .ReturnsAsync((Report)null);
            
            // Act: Call _reportService.GetReportByIdAsync with Id=999
            var result = await _reportService.GetReportByIdAsync(999);
            
            // Assert: Verify the result is not successful
            result.Succeeded.Should().BeFalse();
            // Assert: Verify the result contains an appropriate error message
            result.Message.Should().Contain("Report not found");
            // Assert: Verify _mockReportRepository.GetByIdAsync was called once with Id=999
            _mockReportRepository.Verify(x => x.GetByIdAsync(999), Times.Once);
        }

        /// <summary>
        /// Tests that GetReportByIdAsync returns a failure result when the repository throws an exception.
        /// </summary>
        [Fact]
        public async Task GetReportByIdAsync_WhenRepositoryThrowsException_ReturnsFailureResult()
        {
            // Arrange: Set up _mockReportRepository.GetByIdAsync to throw an exception
            var reportId = 1;
            _mockReportRepository.Setup(x => x.GetByIdAsync(reportId))
                .ThrowsAsync(new Exception("Database error"));
            
            // Act: Call _reportService.GetReportByIdAsync with Id=1
            var result = await _reportService.GetReportByIdAsync(reportId);
            
            // Assert: Verify the result is not successful
            result.Succeeded.Should().BeFalse();
            // Assert: Verify the result contains an appropriate error message
            result.Message.Should().Contain("Failed to retrieve report");
            // Assert: Verify _mockLogger.LogError was called with the exception details
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }

        /// <summary>
        /// Tests that GetReportsByUserIdAsync returns a success result with the reports when provided with a valid user ID.
        /// </summary>
        [Fact]
        public async Task GetReportsByUserIdAsync_WithValidUserId_ReturnsSuccessResult()
        {
            // Arrange: Create a list of test reports for the user
            var reports = new List<Report>
            {
                new Report { Id = 1, Text = "Report 1", UserId = _testUserId },
                new Report { Id = 2, Text = "Report 2", UserId = _testUserId }
            };
            
            // Arrange: Set up _mockReportRepository.GetByUserIdAsync to return the test reports
            _mockReportRepository.Setup(x => x.GetByUserIdAsync(_testUserId))
                .ReturnsAsync(reports);
            
            // Act: Call _reportService.GetReportsByUserIdAsync with _testUserId
            var result = await _reportService.GetReportsByUserIdAsync(_testUserId);
            
            // Assert: Verify the result is successful
            result.Succeeded.Should().BeTrue();
            // Assert: Verify the result contains the correct reports
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(2);
            // Assert: Verify _mockReportRepository.GetByUserIdAsync was called once with _testUserId
            _mockReportRepository.Verify(x => x.GetByUserIdAsync(_testUserId), Times.Once);
        }

        /// <summary>
        /// Tests that GetPaginatedReportsByUserIdAsync returns a success result with paginated reports when provided with valid parameters.
        /// </summary>
        [Fact]
        public async Task GetPaginatedReportsByUserIdAsync_WithValidParameters_ReturnsSuccessResult()
        {
            // Arrange: Create a paginated list of test reports
            var pageNumber = 1;
            var pageSize = 10;
            var reports = new List<Report>
            {
                new Report { Id = 1, Text = "Report 1", UserId = _testUserId },
                new Report { Id = 2, Text = "Report 2", UserId = _testUserId }
            };
            var paginatedList = new PaginatedList<Report>(reports, 2, pageNumber, pageSize);
            
            // Arrange: Set up _mockReportRepository.GetPaginatedByUserIdAsync to return the paginated reports
            _mockReportRepository.Setup(x => x.GetPaginatedByUserIdAsync(_testUserId, pageNumber, pageSize))
                .ReturnsAsync(paginatedList);
            
            // Act: Call _reportService.GetPaginatedReportsByUserIdAsync with _testUserId, pageNumber=1, pageSize=10
            var result = await _reportService.GetPaginatedReportsByUserIdAsync(_testUserId, pageNumber, pageSize);
            
            // Assert: Verify the result is successful
            result.Succeeded.Should().BeTrue();
            // Assert: Verify the result contains the correct paginated reports
            result.Data.Should().NotBeNull();
            result.Data.Items.Should().HaveCount(2);
            result.Data.PageNumber.Should().Be(pageNumber);
            result.Data.TotalPages.Should().Be(1);
            result.Data.TotalCount.Should().Be(2);
            // Assert: Verify _mockReportRepository.GetPaginatedByUserIdAsync was called once with the correct parameters
            _mockReportRepository.Verify(x => x.GetPaginatedByUserIdAsync(_testUserId, pageNumber, pageSize), Times.Once);
        }

        /// <summary>
        /// Tests that GetAllReportsAsync returns a success result with all reports.
        /// </summary>
        [Fact]
        public async Task GetAllReportsAsync_ReturnsSuccessResult()
        {
            // Arrange: Create a list of test reports
            var reports = new List<Report>
            {
                new Report { Id = 1, Text = "Report 1", UserId = _testUserId },
                new Report { Id = 2, Text = "Report 2", UserId = "different-user" }
            };
            
            // Arrange: Set up _mockReportRepository.GetAllAsync to return the test reports
            _mockReportRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(reports);
            
            // Act: Call _reportService.GetAllReportsAsync
            var result = await _reportService.GetAllReportsAsync();
            
            // Assert: Verify the result is successful
            result.Succeeded.Should().BeTrue();
            // Assert: Verify the result contains the correct reports
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(2);
            // Assert: Verify _mockReportRepository.GetAllAsync was called once
            _mockReportRepository.Verify(x => x.GetAllAsync(), Times.Once);
        }

        /// <summary>
        /// Tests that GetPaginatedReportsAsync returns a success result with paginated reports when provided with valid parameters.
        /// </summary>
        [Fact]
        public async Task GetPaginatedReportsAsync_WithValidParameters_ReturnsSuccessResult()
        {
            // Arrange: Create a paginated list of test reports
            var pageNumber = 1;
            var pageSize = 10;
            var reports = new List<Report>
            {
                new Report { Id = 1, Text = "Report 1", UserId = _testUserId },
                new Report { Id = 2, Text = "Report 2", UserId = "different-user" }
            };
            var paginatedList = new PaginatedList<Report>(reports, 2, pageNumber, pageSize);
            
            // Arrange: Set up _mockReportRepository.GetPaginatedAsync to return the paginated reports
            _mockReportRepository.Setup(x => x.GetPaginatedAsync(pageNumber, pageSize))
                .ReturnsAsync(paginatedList);
            
            // Act: Call _reportService.GetPaginatedReportsAsync with pageNumber=1, pageSize=10
            var result = await _reportService.GetPaginatedReportsAsync(pageNumber, pageSize);
            
            // Assert: Verify the result is successful
            result.Succeeded.Should().BeTrue();
            // Assert: Verify the result contains the correct paginated reports
            result.Data.Should().NotBeNull();
            result.Data.Items.Should().HaveCount(2);
            result.Data.PageNumber.Should().Be(pageNumber);
            // Assert: Verify _mockReportRepository.GetPaginatedAsync was called once with the correct parameters
            _mockReportRepository.Verify(x => x.GetPaginatedAsync(pageNumber, pageSize), Times.Once);
        }

        /// <summary>
        /// Tests that GetReportsByDateRangeAsync returns a success result with reports in the date range when provided with valid dates.
        /// </summary>
        [Fact]
        public async Task GetReportsByDateRangeAsync_WithValidDateRange_ReturnsSuccessResult()
        {
            // Arrange: Create a list of test reports within the date range
            var startDate = DateTime.UtcNow.AddDays(-7);
            var endDate = DateTime.UtcNow;
            var reports = new List<Report>
            {
                new Report { Id = 1, Text = "Report 1", Timestamp = DateTime.UtcNow.AddDays(-5), UserId = _testUserId },
                new Report { Id = 2, Text = "Report 2", Timestamp = DateTime.UtcNow.AddDays(-3), UserId = _testUserId }
            };
            
            // Arrange: Set up _mockReportRepository.GetByDateRangeAsync to return the test reports
            _mockReportRepository.Setup(x => x.GetByDateRangeAsync(startDate, endDate))
                .ReturnsAsync(reports);
            
            // Act: Call _reportService.GetReportsByDateRangeAsync with startDate and endDate
            var result = await _reportService.GetReportsByDateRangeAsync(startDate, endDate);
            
            // Assert: Verify the result is successful
            result.Succeeded.Should().BeTrue();
            // Assert: Verify the result contains the correct reports
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(2);
            // Assert: Verify _mockReportRepository.GetByDateRangeAsync was called once with the correct parameters
            _mockReportRepository.Verify(x => x.GetByDateRangeAsync(startDate, endDate), Times.Once);
        }

        /// <summary>
        /// Tests that UpdateReportAsync returns a success result when provided with valid parameters.
        /// </summary>
        [Fact]
        public async Task UpdateReportAsync_WithValidParameters_ReturnsSuccessResult()
        {
            // Arrange: Create a test report with Id=1 and UserId=_testUserId
            var reportId = 1;
            var request = new ReportRequest
            {
                Text = "Updated report text",
                Timestamp = DateTime.UtcNow,
                Location = new LocationData { Latitude = TestConstants.TestLatitude, Longitude = TestConstants.TestLongitude }
            };
            
            var existingReport = new Report
            {
                Id = reportId,
                Text = "Original report text",
                Timestamp = DateTime.UtcNow.AddDays(-1),
                Latitude = 0,
                Longitude = 0,
                UserId = _testUserId
            };
            
            // Arrange: Set up _mockReportRepository.GetByIdAsync to return the test report
            _mockReportRepository.Setup(x => x.GetByIdAsync(reportId))
                .ReturnsAsync(existingReport);
            
            // Arrange: Set up _mockReportRepository.UpdateAsync to complete successfully
            _mockReportRepository.Setup(x => x.UpdateAsync(It.IsAny<Report>()))
                .Returns(Task.CompletedTask);
            
            // Act: Call _reportService.UpdateReportAsync with Id=1, the request, and _testUserId
            var result = await _reportService.UpdateReportAsync(reportId, request, _testUserId);
            
            // Assert: Verify the result is successful
            result.Succeeded.Should().BeTrue();
            // Assert: Verify _mockReportRepository.GetByIdAsync was called once with Id=1
            _mockReportRepository.Verify(x => x.GetByIdAsync(reportId), Times.Once);
            // Assert: Verify _mockReportRepository.UpdateAsync was called once with the updated report
            _mockReportRepository.Verify(x => x.UpdateAsync(It.Is<Report>(r => 
                r.Id == reportId && 
                r.Text == request.Text && 
                r.Latitude == request.Location.Latitude && 
                r.Longitude == request.Location.Longitude &&
                r.IsSynced == false)), 
                Times.Once);
        }

        /// <summary>
        /// Tests that UpdateReportAsync returns a failure result when the report does not exist.
        /// </summary>
        [Fact]
        public async Task UpdateReportAsync_WithNonExistentReport_ReturnsFailureResult()
        {
            // Arrange: Create a valid ReportRequest
            var reportId = 999;
            var request = new ReportRequest
            {
                Text = "Updated report text",
                Timestamp = DateTime.UtcNow,
                Location = new LocationData { Latitude = TestConstants.TestLatitude, Longitude = TestConstants.TestLongitude }
            };
            
            // Arrange: Set up _mockReportRepository.GetByIdAsync to return null
            _mockReportRepository.Setup(x => x.GetByIdAsync(reportId))
                .ReturnsAsync((Report)null);
            
            // Act: Call _reportService.UpdateReportAsync with Id=999, the request, and _testUserId
            var result = await _reportService.UpdateReportAsync(reportId, request, _testUserId);
            
            // Assert: Verify the result is not successful
            result.Succeeded.Should().BeFalse();
            // Assert: Verify the result contains an appropriate error message
            result.Message.Should().Contain("Report not found");
            // Assert: Verify _mockReportRepository.UpdateAsync was not called
            _mockReportRepository.Verify(x => x.UpdateAsync(It.IsAny<Report>()), Times.Never);
        }

        /// <summary>
        /// Tests that UpdateReportAsync returns a failure result when the report belongs to a different user.
        /// </summary>
        [Fact]
        public async Task UpdateReportAsync_WithDifferentUserId_ReturnsFailureResult()
        {
            // Arrange: Create a test report with Id=1 and UserId='different-user'
            var reportId = 1;
            var request = new ReportRequest
            {
                Text = "Updated report text",
                Timestamp = DateTime.UtcNow,
                Location = new LocationData { Latitude = TestConstants.TestLatitude, Longitude = TestConstants.TestLongitude }
            };
            
            var existingReport = new Report
            {
                Id = reportId,
                Text = "Original report text",
                Timestamp = DateTime.UtcNow.AddDays(-1),
                Latitude = 0,
                Longitude = 0,
                UserId = "different-user"
            };
            
            // Arrange: Set up _mockReportRepository.GetByIdAsync to return the test report
            _mockReportRepository.Setup(x => x.GetByIdAsync(reportId))
                .ReturnsAsync(existingReport);
            
            // Act: Call _reportService.UpdateReportAsync with Id=1, the request, and _testUserId
            var result = await _reportService.UpdateReportAsync(reportId, request, _testUserId);
            
            // Assert: Verify the result is not successful
            result.Succeeded.Should().BeFalse();
            // Assert: Verify the result contains an appropriate error message about unauthorized access
            result.Message.Should().Contain("permission");
            // Assert: Verify _mockReportRepository.UpdateAsync was not called
            _mockReportRepository.Verify(x => x.UpdateAsync(It.IsAny<Report>()), Times.Never);
        }

        /// <summary>
        /// Tests that DeleteReportAsync returns a success result when provided with valid parameters.
        /// </summary>
        [Fact]
        public async Task DeleteReportAsync_WithValidParameters_ReturnsSuccessResult()
        {
            // Arrange: Create a test report with Id=1 and UserId=_testUserId
            var reportId = 1;
            var existingReport = new Report
            {
                Id = reportId,
                Text = "Report to delete",
                Timestamp = DateTime.UtcNow,
                UserId = _testUserId
            };
            
            // Arrange: Set up _mockReportRepository.GetByIdAsync to return the test report
            _mockReportRepository.Setup(x => x.GetByIdAsync(reportId))
                .ReturnsAsync(existingReport);
            
            // Arrange: Set up _mockReportRepository.DeleteAsync to complete successfully
            _mockReportRepository.Setup(x => x.DeleteAsync(reportId))
                .Returns(Task.CompletedTask);
            
            // Act: Call _reportService.DeleteReportAsync with Id=1 and _testUserId
            var result = await _reportService.DeleteReportAsync(reportId, _testUserId);
            
            // Assert: Verify the result is successful
            result.Succeeded.Should().BeTrue();
            // Assert: Verify _mockReportRepository.GetByIdAsync was called once with Id=1
            _mockReportRepository.Verify(x => x.GetByIdAsync(reportId), Times.Once);
            // Assert: Verify _mockReportRepository.DeleteAsync was called once with Id=1
            _mockReportRepository.Verify(x => x.DeleteAsync(reportId), Times.Once);
        }

        /// <summary>
        /// Tests that DeleteReportAsync returns a failure result when the report does not exist.
        /// </summary>
        [Fact]
        public async Task DeleteReportAsync_WithNonExistentReport_ReturnsFailureResult()
        {
            // Arrange: Set up _mockReportRepository.GetByIdAsync to return null
            var reportId = 999;
            _mockReportRepository.Setup(x => x.GetByIdAsync(reportId))
                .ReturnsAsync((Report)null);
            
            // Act: Call _reportService.DeleteReportAsync with Id=999 and _testUserId
            var result = await _reportService.DeleteReportAsync(reportId, _testUserId);
            
            // Assert: Verify the result is not successful
            result.Succeeded.Should().BeFalse();
            // Assert: Verify the result contains an appropriate error message
            result.Message.Should().Contain("Report not found");
            // Assert: Verify _mockReportRepository.DeleteAsync was not called
            _mockReportRepository.Verify(x => x.DeleteAsync(It.IsAny<int>()), Times.Never);
        }

        /// <summary>
        /// Tests that DeleteReportAsync returns a failure result when the report belongs to a different user.
        /// </summary>
        [Fact]
        public async Task DeleteReportAsync_WithDifferentUserId_ReturnsFailureResult()
        {
            // Arrange: Create a test report with Id=1 and UserId='different-user'
            var reportId = 1;
            var existingReport = new Report
            {
                Id = reportId,
                Text = "Report to delete",
                Timestamp = DateTime.UtcNow,
                UserId = "different-user"
            };
            
            // Arrange: Set up _mockReportRepository.GetByIdAsync to return the test report
            _mockReportRepository.Setup(x => x.GetByIdAsync(reportId))
                .ReturnsAsync(existingReport);
            
            // Act: Call _reportService.DeleteReportAsync with Id=1 and _testUserId
            var result = await _reportService.DeleteReportAsync(reportId, _testUserId);
            
            // Assert: Verify the result is not successful
            result.Succeeded.Should().BeFalse();
            // Assert: Verify the result contains an appropriate error message about unauthorized access
            result.Message.Should().Contain("permission");
            // Assert: Verify _mockReportRepository.DeleteAsync was not called
            _mockReportRepository.Verify(x => x.DeleteAsync(It.IsAny<int>()), Times.Never);
        }

        /// <summary>
        /// Tests that UpdateSyncStatusAsync returns a success result when provided with valid parameters.
        /// </summary>
        [Fact]
        public async Task UpdateSyncStatusAsync_WithValidParameters_ReturnsSuccessResult()
        {
            // Arrange: Set up _mockReportRepository.UpdateSyncStatusAsync to complete successfully
            var reportId = 1;
            var isSynced = true;
            _mockReportRepository.Setup(x => x.UpdateSyncStatusAsync(reportId, isSynced))
                .Returns(Task.CompletedTask);
            
            // Act: Call _reportService.UpdateSyncStatusAsync with Id=1 and isSynced=true
            var result = await _reportService.UpdateSyncStatusAsync(reportId, isSynced);
            
            // Assert: Verify the result is successful
            result.Succeeded.Should().BeTrue();
            // Assert: Verify _mockReportRepository.UpdateSyncStatusAsync was called once with Id=1 and isSynced=true
            _mockReportRepository.Verify(x => x.UpdateSyncStatusAsync(reportId, isSynced), Times.Once);
        }

        /// <summary>
        /// Tests that GetUnsyncedReportsAsync returns a success result with unsynced reports.
        /// </summary>
        [Fact]
        public async Task GetUnsyncedReportsAsync_ReturnsSuccessResult()
        {
            // Arrange: Create a list of unsynced test reports
            var unsyncedReports = new List<Report>
            {
                new Report { Id = 1, Text = "Unsynced Report 1", IsSynced = false, UserId = _testUserId },
                new Report { Id = 2, Text = "Unsynced Report 2", IsSynced = false, UserId = "different-user" }
            };
            
            // Arrange: Set up _mockReportRepository.GetUnsyncedAsync to return the unsynced reports
            _mockReportRepository.Setup(x => x.GetUnsyncedAsync())
                .ReturnsAsync(unsyncedReports);
            
            // Act: Call _reportService.GetUnsyncedReportsAsync
            var result = await _reportService.GetUnsyncedReportsAsync();
            
            // Assert: Verify the result is successful
            result.Succeeded.Should().BeTrue();
            // Assert: Verify the result contains the correct unsynced reports
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(2);
            // Assert: Verify _mockReportRepository.GetUnsyncedAsync was called once
            _mockReportRepository.Verify(x => x.GetUnsyncedAsync(), Times.Once);
        }
    }
}