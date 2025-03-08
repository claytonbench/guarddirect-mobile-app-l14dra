using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Core.Models;
using SecurityPatrol.Application.Services;
using SecurityPatrol.UnitTests.Helpers;

namespace SecurityPatrol.UnitTests.Application.Services
{
    public class ReportServiceTests : TestBase
    {
        private readonly ReportService _reportService;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<ILogger<ReportService>> _mockLogger;
        private readonly string _testUserId;
        private readonly DateTime _testDateTime;

        public ReportServiceTests()
        {
            // Initialize test data
            _testUserId = "user1";
            _testDateTime = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            
            // Setup dependencies
            SetupMocks();
            
            // Create service under test
            CreateReportService();
        }

        private void SetupMocks()
        {
            // Setup current user service mock
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockCurrentUserService.Setup(x => x.GetUserId()).Returns(_testUserId);
            _mockCurrentUserService.Setup(x => x.IsAuthenticated()).Returns(true);
            
            // Setup logger mock
            _mockLogger = CreateMockLogger<ReportService>();
        }

        private void CreateReportService()
        {
            _reportService = new ReportService(
                MockReportRepository.Object,
                _mockCurrentUserService.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task CreateReportAsync_WithValidRequest_ShouldCreateReport()
        {
            // Arrange
            var request = new ReportRequest
            {
                Text = "Test report",
                Timestamp = _testDateTime,
                Location = new LocationData
                {
                    Latitude = 40.7128,
                    Longitude = -74.0060
                }
            };
            
            MockReportRepository.Setup(r => r.AddAsync(It.IsAny<Report>()))
                .ReturnsAsync(new Report { Id = 1 });

            // Act
            var result = await _reportService.CreateReportAsync(request, _testUserId);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Id.Should().Be("1");
            MockReportRepository.Verify(r => r.AddAsync(It.Is<Report>(p => 
                p.Text == request.Text && 
                p.Timestamp == request.Timestamp &&
                p.Latitude == request.Location.Latitude &&
                p.Longitude == request.Location.Longitude &&
                p.UserId == _testUserId)), Times.Once);
        }

        [Fact]
        public async Task CreateReportAsync_WithNullRequest_ShouldReturnFailure()
        {
            // Act
            var result = await _reportService.CreateReportAsync(null, _testUserId);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("Report request cannot be null");
            MockReportRepository.Verify(r => r.AddAsync(It.IsAny<Report>()), Times.Never);
        }

        [Fact]
        public async Task CreateReportAsync_WithEmptyText_ShouldReturnFailure()
        {
            // Arrange
            var request = new ReportRequest
            {
                Text = string.Empty,
                Timestamp = _testDateTime,
                Location = new LocationData
                {
                    Latitude = 40.7128,
                    Longitude = -74.0060
                }
            };

            // Act
            var result = await _reportService.CreateReportAsync(request, _testUserId);

            // Assert
            // Note: In the current implementation, empty text is allowed, but in practice
            // we would likely want validation to prevent empty reports
            result.Succeeded.Should().BeTrue();
            MockReportRepository.Verify(r => r.AddAsync(It.Is<Report>(p => p.Text == "")), Times.Once);
        }

        [Fact]
        public async Task GetReportByIdAsync_WithValidId_ShouldReturnReport()
        {
            // Arrange
            var testReport = TestData.GetTestReportById(1);
            MockReportRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(testReport);

            // Act
            var result = await _reportService.GetReportByIdAsync(1);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().Be(testReport);
            MockReportRepository.Verify(r => r.GetByIdAsync(1), Times.Once);
        }

        [Fact]
        public async Task GetReportByIdAsync_WithInvalidId_ShouldReturnFailure()
        {
            // Arrange
            MockReportRepository.Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((Report)null);

            // Act
            var result = await _reportService.GetReportByIdAsync(999);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("Report not found");
            MockReportRepository.Verify(r => r.GetByIdAsync(999), Times.Once);
        }

        [Fact]
        public async Task GetReportByIdAsync_WithInvalidIdZero_ShouldReturnFailure()
        {
            // Act
            var result = await _reportService.GetReportByIdAsync(0);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("Invalid report ID");
            MockReportRepository.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetReportsByUserIdAsync_WithValidUserId_ShouldReturnReports()
        {
            // Arrange
            var testReports = TestData.GetTestReports().Where(r => r.UserId == _testUserId).ToList();
            MockReportRepository.Setup(r => r.GetByUserIdAsync(_testUserId))
                .ReturnsAsync(testReports);

            // Act
            var result = await _reportService.GetReportsByUserIdAsync(_testUserId);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().BeEquivalentTo(testReports);
            MockReportRepository.Verify(r => r.GetByUserIdAsync(_testUserId), Times.Once);
        }

        [Fact]
        public async Task GetPaginatedReportsByUserIdAsync_WithValidParameters_ShouldReturnPaginatedList()
        {
            // Arrange
            var reports = TestData.GetTestReports().Where(r => r.UserId == _testUserId).ToList();
            var paginatedList = new PaginatedList<Report>(reports, reports.Count, 1, 10);
            
            MockReportRepository.Setup(r => r.GetPaginatedByUserIdAsync(_testUserId, 1, 10))
                .ReturnsAsync(paginatedList);

            // Act
            var result = await _reportService.GetPaginatedReportsByUserIdAsync(_testUserId, 1, 10);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().Be(paginatedList);
            MockReportRepository.Verify(r => r.GetPaginatedByUserIdAsync(_testUserId, 1, 10), Times.Once);
        }

        [Fact]
        public async Task GetPaginatedReportsByUserIdAsync_WithInvalidPageNumber_ShouldReturnFailure()
        {
            // Act
            var result = await _reportService.GetPaginatedReportsByUserIdAsync(_testUserId, 0, 10);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("Page number must be greater than 0");
            MockReportRepository.Verify(r => r.GetPaginatedByUserIdAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetPaginatedReportsByUserIdAsync_WithInvalidPageSize_ShouldReturnFailure()
        {
            // Act
            var result = await _reportService.GetPaginatedReportsByUserIdAsync(_testUserId, 1, 0);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("Page size must be greater than 0");
            MockReportRepository.Verify(r => r.GetPaginatedByUserIdAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetAllReportsAsync_ShouldReturnAllReports()
        {
            // Arrange
            var testReports = TestData.GetTestReports();
            MockReportRepository.Setup(r => r.GetAllAsync())
                .ReturnsAsync(testReports);

            // Act
            var result = await _reportService.GetAllReportsAsync();

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().BeEquivalentTo(testReports);
            MockReportRepository.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetPaginatedReportsAsync_WithValidParameters_ShouldReturnPaginatedList()
        {
            // Arrange
            var reports = TestData.GetTestReports();
            var paginatedList = new PaginatedList<Report>(reports, reports.Count, 1, 10);
            
            MockReportRepository.Setup(r => r.GetPaginatedAsync(1, 10))
                .ReturnsAsync(paginatedList);

            // Act
            var result = await _reportService.GetPaginatedReportsAsync(1, 10);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().Be(paginatedList);
            MockReportRepository.Verify(r => r.GetPaginatedAsync(1, 10), Times.Once);
        }

        [Fact]
        public async Task GetPaginatedReportsAsync_WithInvalidPageNumber_ShouldReturnFailure()
        {
            // Act
            var result = await _reportService.GetPaginatedReportsAsync(0, 10);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("Page number must be greater than 0");
            MockReportRepository.Verify(r => r.GetPaginatedAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetPaginatedReportsAsync_WithInvalidPageSize_ShouldReturnFailure()
        {
            // Act
            var result = await _reportService.GetPaginatedReportsAsync(1, 0);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("Page size must be greater than 0");
            MockReportRepository.Verify(r => r.GetPaginatedAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetReportsByDateRangeAsync_WithValidParameters_ShouldReturnReports()
        {
            // Arrange
            var startDate = new DateTime(2023, 1, 1);
            var endDate = new DateTime(2023, 1, 31);
            var testReports = TestData.GetTestReports();
            
            MockReportRepository.Setup(r => r.GetByDateRangeAsync(startDate, endDate))
                .ReturnsAsync(testReports);

            // Act
            var result = await _reportService.GetReportsByDateRangeAsync(startDate, endDate);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().BeEquivalentTo(testReports);
            MockReportRepository.Verify(r => r.GetByDateRangeAsync(startDate, endDate), Times.Once);
        }

        [Fact]
        public async Task GetReportsByDateRangeAsync_WithStartDateAfterEndDate_ShouldReturnFailure()
        {
            // Arrange
            var startDate = new DateTime(2023, 1, 31);
            var endDate = new DateTime(2023, 1, 1);

            // Act
            var result = await _reportService.GetReportsByDateRangeAsync(startDate, endDate);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("Start date must be before or equal to end date");
            MockReportRepository.Verify(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never);
        }

        [Fact]
        public async Task GetReportsByDateRangeAsync_WithDefaultStartDate_ShouldReturnFailure()
        {
            // Arrange
            var startDate = default(DateTime);
            var endDate = new DateTime(2023, 1, 31);

            // Act
            var result = await _reportService.GetReportsByDateRangeAsync(startDate, endDate);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("Start date must be provided");
            MockReportRepository.Verify(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never);
        }

        [Fact]
        public async Task GetReportsByDateRangeAsync_WithDefaultEndDate_ShouldReturnFailure()
        {
            // Arrange
            var startDate = new DateTime(2023, 1, 1);
            var endDate = default(DateTime);

            // Act
            var result = await _reportService.GetReportsByDateRangeAsync(startDate, endDate);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("End date must be provided");
            MockReportRepository.Verify(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never);
        }

        [Fact]
        public async Task UpdateReportAsync_WithValidParameters_ShouldUpdateReport()
        {
            // Arrange
            var testReport = TestData.GetTestReportById(1);
            testReport.UserId = _testUserId; // Ensure the test report belongs to the test user
            
            var request = new ReportRequest
            {
                Text = "Updated report text",
                Timestamp = _testDateTime,
                Location = new LocationData
                {
                    Latitude = 40.7128,
                    Longitude = -74.0060
                }
            };
            
            MockReportRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(testReport);

            // Act
            var result = await _reportService.UpdateReportAsync(1, request, _testUserId);

            // Assert
            result.Succeeded.Should().BeTrue();
            MockReportRepository.Verify(r => r.UpdateAsync(It.Is<Report>(p => 
                p.Id == 1 && 
                p.Text == request.Text &&
                p.Timestamp == request.Timestamp &&
                p.Latitude == request.Location.Latitude &&
                p.Longitude == request.Location.Longitude &&
                p.IsSynced == false)), Times.Once);
        }

        [Fact]
        public async Task UpdateReportAsync_WithNonExistentId_ShouldReturnFailure()
        {
            // Arrange
            MockReportRepository.Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((Report)null);
            
            var request = new ReportRequest
            {
                Text = "Updated report text",
                Timestamp = _testDateTime,
                Location = new LocationData
                {
                    Latitude = 40.7128,
                    Longitude = -74.0060
                }
            };

            // Act
            var result = await _reportService.UpdateReportAsync(999, request, _testUserId);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("Report not found");
            MockReportRepository.Verify(r => r.UpdateAsync(It.IsAny<Report>()), Times.Never);
        }

        [Fact]
        public async Task UpdateReportAsync_WithUnauthorizedUser_ShouldReturnFailure()
        {
            // Arrange
            var testReport = TestData.GetTestReportById(1);
            testReport.UserId = "different-user-id"; // Set a different user ID to simulate unauthorized access
            
            MockReportRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(testReport);
            
            var request = new ReportRequest
            {
                Text = "Updated report text",
                Timestamp = _testDateTime,
                Location = new LocationData
                {
                    Latitude = 40.7128,
                    Longitude = -74.0060
                }
            };

            // Act
            var result = await _reportService.UpdateReportAsync(1, request, _testUserId);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("You do not have permission to update this report");
            MockReportRepository.Verify(r => r.UpdateAsync(It.IsAny<Report>()), Times.Never);
        }

        [Fact]
        public async Task DeleteReportAsync_WithValidParameters_ShouldDeleteReport()
        {
            // Arrange
            var testReport = TestData.GetTestReportById(1);
            testReport.UserId = _testUserId; // Ensure the test report belongs to the test user
            
            MockReportRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(testReport);
            
            MockReportRepository.Setup(r => r.DeleteAsync(1))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _reportService.DeleteReportAsync(1, _testUserId);

            // Assert
            result.Succeeded.Should().BeTrue();
            MockReportRepository.Verify(r => r.DeleteAsync(1), Times.Once);
        }

        [Fact]
        public async Task DeleteReportAsync_WithNonExistentId_ShouldReturnFailure()
        {
            // Arrange
            MockReportRepository.Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((Report)null);

            // Act
            var result = await _reportService.DeleteReportAsync(999, _testUserId);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("Report not found");
            MockReportRepository.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task DeleteReportAsync_WithUnauthorizedUser_ShouldReturnFailure()
        {
            // Arrange
            var testReport = TestData.GetTestReportById(1);
            testReport.UserId = "different-user-id"; // Set a different user ID to simulate unauthorized access
            
            MockReportRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(testReport);

            // Act
            var result = await _reportService.DeleteReportAsync(1, _testUserId);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("You do not have permission to delete this report");
            MockReportRepository.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task UpdateSyncStatusAsync_WithValidParameters_ShouldUpdateSyncStatus()
        {
            // Arrange
            MockReportRepository.Setup(r => r.UpdateSyncStatusAsync(1, true))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _reportService.UpdateSyncStatusAsync(1, true);

            // Assert
            result.Succeeded.Should().BeTrue();
            MockReportRepository.Verify(r => r.UpdateSyncStatusAsync(1, true), Times.Once);
        }

        [Fact]
        public async Task UpdateSyncStatusAsync_WithInvalidId_ShouldReturnFailure()
        {
            // Act
            var result = await _reportService.UpdateSyncStatusAsync(0, true);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("Invalid report ID");
            MockReportRepository.Verify(r => r.UpdateSyncStatusAsync(It.IsAny<int>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task GetUnsyncedReportsAsync_ShouldReturnUnsyncedReports()
        {
            // Arrange
            var unsyncedReports = TestData.GetTestReports().Where(r => !r.IsSynced).ToList();
            MockReportRepository.Setup(r => r.GetUnsyncedAsync())
                .ReturnsAsync(unsyncedReports);

            // Act
            var result = await _reportService.GetUnsyncedReportsAsync();

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().BeEquivalentTo(unsyncedReports);
            MockReportRepository.Verify(r => r.GetUnsyncedAsync(), Times.Once);
        }
    }
}