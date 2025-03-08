using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using SecurityPatrol.Models;
using SecurityPatrol.Services;
using SecurityPatrol.Constants;
using SecurityPatrol.UnitTests.Helpers;

namespace SecurityPatrol.UnitTests.Services
{
    /// <summary>
    /// Contains unit tests for the ReportService class to verify its functionality for managing activity reports.
    /// </summary>
    public class ReportServiceTests
    {
        private readonly Mock<IReportRepository> _mockReportRepository;
        private readonly Mock<IReportSyncService> _mockReportSyncService;
        private readonly Mock<INetworkService> _mockNetworkService;
        private readonly Mock<IAuthenticationStateProvider> _mockAuthStateProvider;
        private readonly Mock<ILogger<ReportService>> _mockLogger;
        private readonly ReportService _reportService;
        private readonly string _testUserId = "test-user-id";

        /// <summary>
        /// Initializes a new instance of the ReportServiceTests class
        /// </summary>
        public ReportServiceTests()
        {
            // Initialize mocks
            _mockReportRepository = new Mock<IReportRepository>();
            _mockReportSyncService = new Mock<IReportSyncService>();
            _mockNetworkService = new Mock<INetworkService>();
            _mockAuthStateProvider = new Mock<IAuthenticationStateProvider>();
            _mockLogger = new Mock<ILogger<ReportService>>();

            // Set up authentication state for a logged-in user
            _mockAuthStateProvider.Setup(x => x.GetCurrentState())
                .Returns(Task.FromResult(AuthState.CreateAuthenticated(_testUserId)));

            // Create the service with mocked dependencies
            _reportService = new ReportService(
                _mockReportRepository.Object,
                _mockReportSyncService.Object,
                _mockNetworkService.Object,
                _mockAuthStateProvider.Object,
                _mockLogger.Object);
        }

        /// <summary>
        /// Sets up the mock objects with default behaviors for testing
        /// </summary>
        private void SetupMocks()
        {
            // Set up network connectivity
            _mockNetworkService.Setup(x => x.IsConnected).Returns(true);

            // Set up authenticated state
            _mockAuthStateProvider.Setup(x => x.GetCurrentState())
                .Returns(Task.FromResult(AuthState.CreateAuthenticated(_testUserId)));

            // Set up repository methods
            _mockReportRepository.Setup(x => x.SaveReportAsync(It.IsAny<ReportModel>()))
                .Returns(Task.FromResult(1));
            
            _mockReportRepository.Setup(x => x.GetReportAsync(It.IsAny<int>()))
                .Returns(Task.FromResult(TestDataGenerator.CreateReportModel()));
            
            _mockReportRepository.Setup(x => x.GetAllReportsAsync())
                .Returns(Task.FromResult<IEnumerable<ReportModel>>(TestDataGenerator.CreateReportModels()));
            
            _mockReportRepository.Setup(x => x.GetRecentReportsAsync(It.IsAny<int>()))
                .Returns(Task.FromResult<IEnumerable<ReportModel>>(TestDataGenerator.CreateReportModels(3)));
            
            _mockReportRepository.Setup(x => x.UpdateReportAsync(It.IsAny<ReportModel>()))
                .Returns(Task.FromResult(1));
            
            _mockReportRepository.Setup(x => x.DeleteReportAsync(It.IsAny<int>()))
                .Returns(Task.FromResult(1));
            
            _mockReportRepository.Setup(x => x.GetReportsByTimeRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(Task.FromResult<IEnumerable<ReportModel>>(TestDataGenerator.CreateReportModels()));
            
            _mockReportRepository.Setup(x => x.DeleteOldReportsAsync(It.IsAny<DateTime>()))
                .Returns(Task.FromResult(5));

            // Set up sync service methods
            _mockReportSyncService.Setup(x => x.SyncReportAsync(It.IsAny<int>()))
                .Returns(Task.FromResult(true));
            
            _mockReportSyncService.Setup(x => x.SyncReportsAsync())
                .Returns(Task.FromResult(3));
            
            _mockReportSyncService.Setup(x => x.SyncDeletedReportAsync(It.IsAny<int>(), It.IsAny<string>()))
                .Returns(Task.FromResult(true));
        }

        /// <summary>
        /// Tests that CreateReportAsync creates and returns a report when given valid input
        /// </summary>
        [Fact]
        public async Task CreateReportAsync_ValidInput_CreatesAndReturnsReport()
        {
            // Arrange
            SetupMocks();
            string text = "Test report";
            double latitude = 12.34;
            double longitude = 56.78;

            // Act
            var result = await _reportService.CreateReportAsync(text, latitude, longitude);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(text, result.Text);
            Assert.Equal(latitude, result.Latitude);
            Assert.Equal(longitude, result.Longitude);
            Assert.Equal(_testUserId, result.UserId);
            
            _mockReportRepository.Verify(x => x.SaveReportAsync(
                It.Is<ReportModel>(r => 
                    r.Text == text && 
                    r.Latitude == latitude && 
                    r.Longitude == longitude && 
                    r.UserId == _testUserId)), 
                Times.Once);
            
            _mockReportSyncService.Verify(x => x.SyncReportAsync(result.Id), Times.Once);
        }

        /// <summary>
        /// Tests that CreateReportAsync throws ArgumentException when given empty text
        /// </summary>
        [Fact]
        public async Task CreateReportAsync_EmptyText_ThrowsArgumentException()
        {
            // Arrange
            SetupMocks();
            string text = string.Empty;
            double latitude = 12.34;
            double longitude = 56.78;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _reportService.CreateReportAsync(text, latitude, longitude));
            
            Assert.Contains(ErrorMessages.ReportEmpty, exception.Message);
            
            _mockReportRepository.Verify(x => x.SaveReportAsync(It.IsAny<ReportModel>()), Times.Never);
            _mockReportSyncService.Verify(x => x.SyncReportAsync(It.IsAny<int>()), Times.Never);
        }

        /// <summary>
        /// Tests that CreateReportAsync throws ArgumentException when text exceeds maximum length
        /// </summary>
        [Fact]
        public async Task CreateReportAsync_TextTooLong_ThrowsArgumentException()
        {
            // Arrange
            SetupMocks();
            string text = new string('A', AppConstants.ReportMaxLength + 1);
            double latitude = 12.34;
            double longitude = 56.78;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _reportService.CreateReportAsync(text, latitude, longitude));
            
            Assert.Contains(ErrorMessages.ReportTooLong, exception.Message);
            
            _mockReportRepository.Verify(x => x.SaveReportAsync(It.IsAny<ReportModel>()), Times.Never);
            _mockReportSyncService.Verify(x => x.SyncReportAsync(It.IsAny<int>()), Times.Never);
        }

        /// <summary>
        /// Tests that CreateReportAsync does not attempt to sync when offline
        /// </summary>
        [Fact]
        public async Task CreateReportAsync_OfflineMode_DoesNotSync()
        {
            // Arrange
            SetupMocks();
            _mockNetworkService.Setup(x => x.IsConnected).Returns(false);
            
            string text = "Test report";
            double latitude = 12.34;
            double longitude = 56.78;

            // Act
            var result = await _reportService.CreateReportAsync(text, latitude, longitude);

            // Assert
            Assert.NotNull(result);
            _mockReportRepository.Verify(x => x.SaveReportAsync(It.IsAny<ReportModel>()), Times.Once);
            _mockReportSyncService.Verify(x => x.SyncReportAsync(It.IsAny<int>()), Times.Never);
        }

        /// <summary>
        /// Tests that GetReportAsync returns a report when given a valid ID
        /// </summary>
        [Fact]
        public async Task GetReportAsync_ValidId_ReturnsReport()
        {
            // Arrange
            SetupMocks();
            int id = 1;
            var expectedReport = TestDataGenerator.CreateReportModel(id);
            _mockReportRepository.Setup(x => x.GetReportAsync(id))
                .Returns(Task.FromResult(expectedReport));

            // Act
            var result = await _reportService.GetReportAsync(id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(id, result.Id);
            _mockReportRepository.Verify(x => x.GetReportAsync(id), Times.Once);
        }

        /// <summary>
        /// Tests that GetReportAsync throws ArgumentException when given an invalid ID
        /// </summary>
        [Fact]
        public async Task GetReportAsync_InvalidId_ThrowsArgumentException()
        {
            // Arrange
            SetupMocks();
            int id = 0;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _reportService.GetReportAsync(id));
            
            _mockReportRepository.Verify(x => x.GetReportAsync(It.IsAny<int>()), Times.Never);
        }

        /// <summary>
        /// Tests that GetReportAsync returns null when the report does not exist
        /// </summary>
        [Fact]
        public async Task GetReportAsync_NonexistentId_ReturnsNull()
        {
            // Arrange
            SetupMocks();
            int id = 999;
            _mockReportRepository.Setup(x => x.GetReportAsync(id))
                .Returns(Task.FromResult<ReportModel>(null));

            // Act
            var result = await _reportService.GetReportAsync(id);

            // Assert
            Assert.Null(result);
            _mockReportRepository.Verify(x => x.GetReportAsync(id), Times.Once);
        }

        /// <summary>
        /// Tests that GetAllReportsAsync returns all reports
        /// </summary>
        [Fact]
        public async Task GetAllReportsAsync_ReturnsAllReports()
        {
            // Arrange
            SetupMocks();
            var reports = TestDataGenerator.CreateReportModels();
            _mockReportRepository.Setup(x => x.GetAllReportsAsync())
                .Returns(Task.FromResult<IEnumerable<ReportModel>>(reports));

            // Act
            var result = await _reportService.GetAllReportsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(reports.Count, result.Count());
            _mockReportRepository.Verify(x => x.GetAllReportsAsync(), Times.Once);
        }

        /// <summary>
        /// Tests that GetRecentReportsAsync returns the specified number of recent reports
        /// </summary>
        [Fact]
        public async Task GetRecentReportsAsync_ValidLimit_ReturnsRecentReports()
        {
            // Arrange
            SetupMocks();
            int limit = 5;
            var reports = TestDataGenerator.CreateReportModels(limit);
            _mockReportRepository.Setup(x => x.GetRecentReportsAsync(limit))
                .Returns(Task.FromResult<IEnumerable<ReportModel>>(reports));

            // Act
            var result = await _reportService.GetRecentReportsAsync(limit);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(reports.Count, result.Count());
            _mockReportRepository.Verify(x => x.GetRecentReportsAsync(limit), Times.Once);
        }

        /// <summary>
        /// Tests that GetRecentReportsAsync throws ArgumentException when given an invalid limit
        /// </summary>
        [Fact]
        public async Task GetRecentReportsAsync_InvalidLimit_ThrowsArgumentException()
        {
            // Arrange
            SetupMocks();
            int limit = 0;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _reportService.GetRecentReportsAsync(limit));
            
            _mockReportRepository.Verify(x => x.GetRecentReportsAsync(It.IsAny<int>()), Times.Never);
        }

        /// <summary>
        /// Tests that UpdateReportAsync updates a report and returns true when given a valid report
        /// </summary>
        [Fact]
        public async Task UpdateReportAsync_ValidReport_UpdatesAndReturnsTrue()
        {
            // Arrange
            SetupMocks();
            var report = TestDataGenerator.CreateReportModel(1, text: "Updated report");
            _mockReportRepository.Setup(x => x.UpdateReportAsync(It.IsAny<ReportModel>()))
                .Returns(Task.FromResult(1));

            // Act
            var result = await _reportService.UpdateReportAsync(report);

            // Assert
            Assert.True(result);
            _mockReportRepository.Verify(x => x.UpdateReportAsync(It.IsAny<ReportModel>()), Times.Once);
            _mockReportSyncService.Verify(x => x.SyncReportAsync(report.Id), Times.Once);
        }

        /// <summary>
        /// Tests that UpdateReportAsync throws ArgumentNullException when given a null report
        /// </summary>
        [Fact]
        public async Task UpdateReportAsync_NullReport_ThrowsArgumentNullException()
        {
            // Arrange
            SetupMocks();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _reportService.UpdateReportAsync(null));
            
            _mockReportRepository.Verify(x => x.UpdateReportAsync(It.IsAny<ReportModel>()), Times.Never);
            _mockReportSyncService.Verify(x => x.SyncReportAsync(It.IsAny<int>()), Times.Never);
        }

        /// <summary>
        /// Tests that UpdateReportAsync throws ArgumentException when given a report with an invalid ID
        /// </summary>
        [Fact]
        public async Task UpdateReportAsync_InvalidId_ThrowsArgumentException()
        {
            // Arrange
            SetupMocks();
            var report = TestDataGenerator.CreateReportModel(0, text: "Invalid report");

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _reportService.UpdateReportAsync(report));
            
            _mockReportRepository.Verify(x => x.UpdateReportAsync(It.IsAny<ReportModel>()), Times.Never);
            _mockReportSyncService.Verify(x => x.SyncReportAsync(It.IsAny<int>()), Times.Never);
        }

        /// <summary>
        /// Tests that UpdateReportAsync throws ArgumentException when given a report with empty text
        /// </summary>
        [Fact]
        public async Task UpdateReportAsync_EmptyText_ThrowsArgumentException()
        {
            // Arrange
            SetupMocks();
            var report = TestDataGenerator.CreateReportModel(1, text: string.Empty);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _reportService.UpdateReportAsync(report));
            
            Assert.Contains(ErrorMessages.ReportEmpty, exception.Message);
            
            _mockReportRepository.Verify(x => x.UpdateReportAsync(It.IsAny<ReportModel>()), Times.Never);
            _mockReportSyncService.Verify(x => x.SyncReportAsync(It.IsAny<int>()), Times.Never);
        }

        /// <summary>
        /// Tests that UpdateReportAsync throws ArgumentException when given a report with text exceeding maximum length
        /// </summary>
        [Fact]
        public async Task UpdateReportAsync_TextTooLong_ThrowsArgumentException()
        {
            // Arrange
            SetupMocks();
            var report = TestDataGenerator.CreateReportModel(1, text: new string('A', AppConstants.ReportMaxLength + 1));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _reportService.UpdateReportAsync(report));
            
            Assert.Contains(ErrorMessages.ReportTooLong, exception.Message);
            
            _mockReportRepository.Verify(x => x.UpdateReportAsync(It.IsAny<ReportModel>()), Times.Never);
            _mockReportSyncService.Verify(x => x.SyncReportAsync(It.IsAny<int>()), Times.Never);
        }

        /// <summary>
        /// Tests that UpdateReportAsync does not attempt to sync when offline
        /// </summary>
        [Fact]
        public async Task UpdateReportAsync_OfflineMode_DoesNotSync()
        {
            // Arrange
            SetupMocks();
            _mockNetworkService.Setup(x => x.IsConnected).Returns(false);
            
            var report = TestDataGenerator.CreateReportModel(1, text: "Updated report");
            _mockReportRepository.Setup(x => x.UpdateReportAsync(It.IsAny<ReportModel>()))
                .Returns(Task.FromResult(1));

            // Act
            var result = await _reportService.UpdateReportAsync(report);

            // Assert
            Assert.True(result);
            _mockReportRepository.Verify(x => x.UpdateReportAsync(It.IsAny<ReportModel>()), Times.Once);
            _mockReportSyncService.Verify(x => x.SyncReportAsync(It.IsAny<int>()), Times.Never);
        }

        /// <summary>
        /// Tests that DeleteReportAsync deletes a report and returns true when given a valid ID
        /// </summary>
        [Fact]
        public async Task DeleteReportAsync_ValidId_DeletesAndReturnsTrue()
        {
            // Arrange
            SetupMocks();
            int id = 1;
            var report = TestDataGenerator.CreateReportModel(id);
            report.RemoteId = "remote-1";
            
            _mockReportRepository.Setup(x => x.GetReportAsync(id))
                .Returns(Task.FromResult(report));
            
            _mockReportRepository.Setup(x => x.DeleteReportAsync(id))
                .Returns(Task.FromResult(1));

            // Act
            var result = await _reportService.DeleteReportAsync(id);

            // Assert
            Assert.True(result);
            _mockReportRepository.Verify(x => x.GetReportAsync(id), Times.Once);
            _mockReportRepository.Verify(x => x.DeleteReportAsync(id), Times.Once);
            _mockReportSyncService.Verify(x => x.SyncDeletedReportAsync(id, report.RemoteId), Times.Once);
        }

        /// <summary>
        /// Tests that DeleteReportAsync throws ArgumentException when given an invalid ID
        /// </summary>
        [Fact]
        public async Task DeleteReportAsync_InvalidId_ThrowsArgumentException()
        {
            // Arrange
            SetupMocks();
            int id = 0;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _reportService.DeleteReportAsync(id));
            
            _mockReportRepository.Verify(x => x.GetReportAsync(It.IsAny<int>()), Times.Never);
            _mockReportRepository.Verify(x => x.DeleteReportAsync(It.IsAny<int>()), Times.Never);
            _mockReportSyncService.Verify(x => x.SyncDeletedReportAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        /// <summary>
        /// Tests that DeleteReportAsync returns false when the report does not exist
        /// </summary>
        [Fact]
        public async Task DeleteReportAsync_NonexistentId_ReturnsFalse()
        {
            // Arrange
            SetupMocks();
            int id = 999;
            _mockReportRepository.Setup(x => x.GetReportAsync(id))
                .Returns(Task.FromResult<ReportModel>(null));

            // Act
            var result = await _reportService.DeleteReportAsync(id);

            // Assert
            Assert.False(result);
            _mockReportRepository.Verify(x => x.GetReportAsync(id), Times.Once);
            _mockReportRepository.Verify(x => x.DeleteReportAsync(It.IsAny<int>()), Times.Never);
            _mockReportSyncService.Verify(x => x.SyncDeletedReportAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        /// <summary>
        /// Tests that DeleteReportAsync does not attempt to sync when offline
        /// </summary>
        [Fact]
        public async Task DeleteReportAsync_OfflineMode_DoesNotSync()
        {
            // Arrange
            SetupMocks();
            _mockNetworkService.Setup(x => x.IsConnected).Returns(false);
            
            int id = 1;
            var report = TestDataGenerator.CreateReportModel(id);
            report.RemoteId = "remote-1";
            
            _mockReportRepository.Setup(x => x.GetReportAsync(id))
                .Returns(Task.FromResult(report));
            
            _mockReportRepository.Setup(x => x.DeleteReportAsync(id))
                .Returns(Task.FromResult(1));

            // Act
            var result = await _reportService.DeleteReportAsync(id);

            // Assert
            Assert.True(result);
            _mockReportRepository.Verify(x => x.DeleteReportAsync(id), Times.Once);
            _mockReportSyncService.Verify(x => x.SyncDeletedReportAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        /// <summary>
        /// Tests that SyncReportAsync syncs a report and returns true when given a valid ID
        /// </summary>
        [Fact]
        public async Task SyncReportAsync_ValidId_SyncsAndReturnsTrue()
        {
            // Arrange
            SetupMocks();
            int id = 1;
            _mockReportSyncService.Setup(x => x.SyncReportAsync(id))
                .Returns(Task.FromResult(true));

            // Act
            var result = await _reportService.SyncReportAsync(id);

            // Assert
            Assert.True(result);
            _mockReportSyncService.Verify(x => x.SyncReportAsync(id), Times.Once);
        }

        /// <summary>
        /// Tests that SyncReportAsync throws ArgumentException when given an invalid ID
        /// </summary>
        [Fact]
        public async Task SyncReportAsync_InvalidId_ThrowsArgumentException()
        {
            // Arrange
            SetupMocks();
            int id = 0;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _reportService.SyncReportAsync(id));
            
            _mockReportSyncService.Verify(x => x.SyncReportAsync(It.IsAny<int>()), Times.Never);
        }

        /// <summary>
        /// Tests that SyncReportAsync returns false when offline
        /// </summary>
        [Fact]
        public async Task SyncReportAsync_OfflineMode_ReturnsFalse()
        {
            // Arrange
            SetupMocks();
            _mockNetworkService.Setup(x => x.IsConnected).Returns(false);
            
            int id = 1;

            // Act
            var result = await _reportService.SyncReportAsync(id);

            // Assert
            Assert.False(result);
            _mockReportSyncService.Verify(x => x.SyncReportAsync(It.IsAny<int>()), Times.Never);
        }

        /// <summary>
        /// Tests that SyncAllReportsAsync syncs all reports and returns the count when online
        /// </summary>
        [Fact]
        public async Task SyncAllReportsAsync_Online_SyncsAndReturnsCount()
        {
            // Arrange
            SetupMocks();
            int expectedCount = 3;
            _mockReportSyncService.Setup(x => x.SyncReportsAsync())
                .Returns(Task.FromResult(expectedCount));

            // Act
            var result = await _reportService.SyncAllReportsAsync();

            // Assert
            Assert.Equal(expectedCount, result);
            _mockReportSyncService.Verify(x => x.SyncReportsAsync(), Times.Once);
        }

        /// <summary>
        /// Tests that SyncAllReportsAsync returns zero when offline
        /// </summary>
        [Fact]
        public async Task SyncAllReportsAsync_Offline_ReturnsZero()
        {
            // Arrange
            SetupMocks();
            _mockNetworkService.Setup(x => x.IsConnected).Returns(false);

            // Act
            var result = await _reportService.SyncAllReportsAsync();

            // Assert
            Assert.Equal(0, result);
            _mockReportSyncService.Verify(x => x.SyncReportsAsync(), Times.Never);
        }

        /// <summary>
        /// Tests that GetReportsByDateRangeAsync returns reports within the specified date range
        /// </summary>
        [Fact]
        public async Task GetReportsByDateRangeAsync_ValidDateRange_ReturnsReports()
        {
            // Arrange
            SetupMocks();
            var startDate = DateTime.UtcNow.AddDays(-7);
            var endDate = DateTime.UtcNow;
            
            var reports = TestDataGenerator.CreateReportModels();
            _mockReportRepository.Setup(x => x.GetReportsByTimeRangeAsync(startDate, endDate))
                .Returns(Task.FromResult<IEnumerable<ReportModel>>(reports));

            // Act
            var result = await _reportService.GetReportsByDateRangeAsync(startDate, endDate);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(reports.Count, result.Count());
            _mockReportRepository.Verify(x => x.GetReportsByTimeRangeAsync(startDate, endDate), Times.Once);
        }

        /// <summary>
        /// Tests that GetReportsByDateRangeAsync throws ArgumentException when given an invalid date range
        /// </summary>
        [Fact]
        public async Task GetReportsByDateRangeAsync_InvalidDateRange_ThrowsArgumentException()
        {
            // Arrange
            SetupMocks();
            var startDate = DateTime.UtcNow;
            var endDate = DateTime.UtcNow.AddDays(-7); // End date before start date

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _reportService.GetReportsByDateRangeAsync(startDate, endDate));
            
            _mockReportRepository.Verify(x => x.GetReportsByTimeRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never);
        }

        /// <summary>
        /// Tests that CleanupOldReportsAsync deletes old reports and returns the count
        /// </summary>
        [Fact]
        public async Task CleanupOldReportsAsync_ValidRetentionDays_DeletesAndReturnsCount()
        {
            // Arrange
            SetupMocks();
            int retentionDays = 30;
            int expectedCount = 5;
            _mockReportRepository.Setup(x => x.DeleteOldReportsAsync(It.IsAny<DateTime>()))
                .Returns(Task.FromResult(expectedCount));

            // Act
            var result = await _reportService.CleanupOldReportsAsync(retentionDays);

            // Assert
            Assert.Equal(expectedCount, result);
            _mockReportRepository.Verify(x => x.DeleteOldReportsAsync(It.Is<DateTime>(d => 
                d <= DateTime.UtcNow.AddDays(-retentionDays + 1) && // Add small margin for test execution time
                d >= DateTime.UtcNow.AddDays(-retentionDays - 1))), 
                Times.Once);
        }

        /// <summary>
        /// Tests that CleanupOldReportsAsync uses the default retention period when given an invalid value
        /// </summary>
        [Fact]
        public async Task CleanupOldReportsAsync_InvalidRetentionDays_UsesDefaultValue()
        {
            // Arrange
            SetupMocks();
            int retentionDays = 0;
            int expectedCount = 5;
            _mockReportRepository.Setup(x => x.DeleteOldReportsAsync(It.IsAny<DateTime>()))
                .Returns(Task.FromResult(expectedCount));

            // Act
            var result = await _reportService.CleanupOldReportsAsync(retentionDays);

            // Assert
            Assert.Equal(expectedCount, result);
            _mockReportRepository.Verify(x => x.DeleteOldReportsAsync(It.Is<DateTime>(d => 
                d <= DateTime.UtcNow.AddDays(-AppConstants.ReportRetentionDays + 1) && // Add small margin for test execution time
                d >= DateTime.UtcNow.AddDays(-AppConstants.ReportRetentionDays - 1))), 
                Times.Once);
        }
    }
}