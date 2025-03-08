using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using SecurityPatrol.Application.Services;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.Core.Models;
using SecurityPatrol.Core.Entities;
using FluentValidation;

namespace SecurityPatrol.API.UnitTests.Services
{
    /// <summary>
    /// Contains unit tests for the LocationService class to verify its behavior for processing, retrieving, and managing location data.
    /// </summary>
    public class LocationServiceTests
    {
        private readonly Mock<ILocationRecordRepository> _mockLocationRepository;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IDateTime> _mockDateTime;
        private readonly LocationService _locationService;
        private readonly DateTime _fixedDateTime;
        private readonly string _userId;

        /// <summary>
        /// Initializes a new instance of the LocationServiceTests class with mocked dependencies.
        /// </summary>
        public LocationServiceTests()
        {
            // Initialize mocks
            _mockLocationRepository = new Mock<ILocationRecordRepository>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockDateTime = new Mock<IDateTime>();

            // Set up fixed test data
            _fixedDateTime = new DateTime(2023, 1, 1, 12, 0, 0);
            _userId = "test-user-id";

            // Configure mock behaviors
            _mockDateTime.Setup(d => d.UtcNow()).Returns(_fixedDateTime);
            _mockCurrentUserService.Setup(u => u.GetUserId()).Returns(_userId);
            _mockCurrentUserService.Setup(u => u.IsAuthenticated()).Returns(true);

            // Create the service instance with mocked dependencies
            _locationService = new LocationService(
                _mockLocationRepository.Object,
                _mockCurrentUserService.Object,
                _mockDateTime.Object);
        }

        [Fact]
        public async Task ProcessLocationBatchAsync_WithValidRequest_ShouldReturnSuccessResponse()
        {
            // Arrange
            var testLocations = new List<LocationModel>
            {
                new LocationModel { Latitude = 40.7128, Longitude = -74.0060, Accuracy = 10.0, Timestamp = _fixedDateTime.AddMinutes(-5) },
                new LocationModel { Latitude = 40.7130, Longitude = -74.0062, Accuracy = 8.5, Timestamp = _fixedDateTime.AddMinutes(-3) }
            };

            var request = new LocationBatchRequest
            {
                UserId = _userId,
                Locations = testLocations
            };

            var savedIds = new List<int> { 1, 2 };
            _mockLocationRepository.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<LocationRecord>>()))
                .ReturnsAsync(savedIds);

            // Act
            var result = await _locationService.ProcessLocationBatchAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.SyncedIds.Should().BeEquivalentTo(savedIds);
            result.FailedIds.Should().BeEmpty();
            _mockLocationRepository.Verify(r => r.AddRangeAsync(It.Is<IEnumerable<LocationRecord>>(
                records => records.Count() == testLocations.Count())), Times.Once);
        }

        [Fact]
        public async Task ProcessLocationBatchAsync_WithInvalidRequest_ShouldThrowValidationException()
        {
            // Arrange
            var request = new LocationBatchRequest
            {
                UserId = _userId,
                Locations = null // Invalid: Locations cannot be null
            };

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() => _locationService.ProcessLocationBatchAsync(request));
        }

        [Fact]
        public async Task GetLocationHistoryAsync_WithValidParameters_ShouldReturnLocationModels()
        {
            // Arrange
            var startTime = _fixedDateTime.AddHours(-1);
            var endTime = _fixedDateTime;

            var testRecords = new List<LocationRecord>
            {
                new LocationRecord { Id = 1, UserId = _userId, Latitude = 40.7128, Longitude = -74.0060, Accuracy = 10.0, Timestamp = _fixedDateTime.AddMinutes(-45) },
                new LocationRecord { Id = 2, UserId = _userId, Latitude = 40.7130, Longitude = -74.0062, Accuracy = 8.5, Timestamp = _fixedDateTime.AddMinutes(-30) }
            };

            _mockLocationRepository.Setup(r => r.GetByUserIdAndTimeRangeAsync(_userId, startTime, endTime))
                .ReturnsAsync(testRecords);

            // Act
            var result = await _locationService.GetLocationHistoryAsync(_userId, startTime, endTime);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(testRecords.Count);
            result.Select(l => l.Id).Should().BeEquivalentTo(testRecords.Select(r => r.Id));
            _mockLocationRepository.Verify(r => r.GetByUserIdAndTimeRangeAsync(_userId, startTime, endTime), Times.Once);
        }

        [Fact]
        public async Task GetLocationHistoryAsync_WithInvalidTimeRange_ShouldThrowArgumentException()
        {
            // Arrange
            var startTime = _fixedDateTime;
            var endTime = _fixedDateTime.AddHours(-1); // End time before start time

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _locationService.GetLocationHistoryAsync(_userId, startTime, endTime));
        }

        [Fact]
        public async Task GetLatestLocationAsync_WithValidUserId_ShouldReturnLocationModel()
        {
            // Arrange
            var testRecord = new LocationRecord 
            { 
                Id = 1, 
                UserId = _userId, 
                Latitude = 40.7128, 
                Longitude = -74.0060, 
                Accuracy = 10.0, 
                Timestamp = _fixedDateTime.AddMinutes(-5) 
            };

            _mockLocationRepository.Setup(r => r.GetLatestLocationAsync(_userId))
                .ReturnsAsync(testRecord);

            // Act
            var result = await _locationService.GetLatestLocationAsync(_userId);

            // Assert
            result.Should().NotBeNull();
            result.Latitude.Should().Be(testRecord.Latitude);
            result.Longitude.Should().Be(testRecord.Longitude);
            result.Accuracy.Should().Be(testRecord.Accuracy);
            result.Timestamp.Should().Be(testRecord.Timestamp);
            _mockLocationRepository.Verify(r => r.GetLatestLocationAsync(_userId), Times.Once);
        }

        [Fact]
        public async Task GetLatestLocationAsync_WithNoLocationData_ShouldReturnNull()
        {
            // Arrange
            _mockLocationRepository.Setup(r => r.GetLatestLocationAsync(_userId))
                .ReturnsAsync((LocationRecord)null);

            // Act
            var result = await _locationService.GetLatestLocationAsync(_userId);

            // Assert
            result.Should().BeNull();
            _mockLocationRepository.Verify(r => r.GetLatestLocationAsync(_userId), Times.Once);
        }

        [Fact]
        public async Task GetLocationsByUserIdAsync_WithValidParameters_ShouldReturnLocationModels()
        {
            // Arrange
            var limit = 10;
            var testRecords = new List<LocationRecord>
            {
                new LocationRecord { Id = 1, UserId = _userId, Latitude = 40.7128, Longitude = -74.0060, Accuracy = 10.0, Timestamp = _fixedDateTime.AddMinutes(-45) },
                new LocationRecord { Id = 2, UserId = _userId, Latitude = 40.7130, Longitude = -74.0062, Accuracy = 8.5, Timestamp = _fixedDateTime.AddMinutes(-30) }
            };

            _mockLocationRepository.Setup(r => r.GetByUserIdAsync(_userId, limit))
                .ReturnsAsync(testRecords);

            // Act
            var result = await _locationService.GetLocationsByUserIdAsync(_userId, limit);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(testRecords.Count);
            result.Select(l => l.Id).Should().BeEquivalentTo(testRecords.Select(r => r.Id));
            _mockLocationRepository.Verify(r => r.GetByUserIdAsync(_userId, limit), Times.Once);
        }

        [Fact]
        public async Task GetLocationsByUserIdAsync_WithInvalidLimit_ShouldThrowArgumentException()
        {
            // Arrange
            var invalidLimit = 0; // Limit must be greater than zero

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _locationService.GetLocationsByUserIdAsync(_userId, invalidLimit));
        }

        [Fact]
        public async Task CleanupLocationDataAsync_WithValidParameters_ShouldDeleteOldRecords()
        {
            // Arrange
            var thresholdDate = _fixedDateTime.AddDays(-30);
            var onlySynced = true;
            var deletedCount = 15;

            _mockLocationRepository.Setup(r => r.DeleteOlderThanAsync(thresholdDate, onlySynced))
                .ReturnsAsync(deletedCount);

            // Act
            var result = await _locationService.CleanupLocationDataAsync(thresholdDate, onlySynced);

            // Assert
            result.Should().Be(deletedCount);
            _mockLocationRepository.Verify(r => r.DeleteOlderThanAsync(thresholdDate, onlySynced), Times.Once);
        }

        [Fact]
        public async Task CleanupLocationDataAsync_WithDefaultDate_ShouldThrowArgumentException()
        {
            // Arrange
            var defaultDate = default(DateTime);
            var onlySynced = true;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _locationService.CleanupLocationDataAsync(defaultDate, onlySynced));
        }

        [Fact]
        public async Task SyncPendingLocationsAsync_WithUnsyncedRecords_ShouldSyncAndReturnResponse()
        {
            // Arrange
            var batchSize = 50;
            var unsyncedRecords = new List<LocationRecord>
            {
                new LocationRecord { Id = 1, UserId = _userId, Latitude = 40.7128, Longitude = -74.0060, IsSynced = false },
                new LocationRecord { Id = 2, UserId = _userId, Latitude = 40.7130, Longitude = -74.0062, IsSynced = false }
            };

            _mockLocationRepository.Setup(r => r.GetUnsyncedRecordsAsync(batchSize))
                .ReturnsAsync(unsyncedRecords);

            _mockLocationRepository.Setup(r => r.UpdateSyncStatusAsync(It.IsAny<IEnumerable<int>>(), true))
                .ReturnsAsync(true);

            // Act
            var result = await _locationService.SyncPendingLocationsAsync(batchSize);

            // Assert
            result.Should().NotBeNull();
            result.SyncedIds.Should().HaveCount(unsyncedRecords.Count);
            result.SyncedIds.Should().BeEquivalentTo(unsyncedRecords.Select(r => r.Id));
            result.FailedIds.Should().BeEmpty();
            _mockLocationRepository.Verify(r => r.GetUnsyncedRecordsAsync(batchSize), Times.Once);
            _mockLocationRepository.Verify(r => r.UpdateSyncStatusAsync(It.IsAny<IEnumerable<int>>(), true), Times.Once);
        }

        [Fact]
        public async Task SyncPendingLocationsAsync_WithNoUnsyncedRecords_ShouldReturnEmptyResponse()
        {
            // Arrange
            var batchSize = 50;
            var emptyRecords = new List<LocationRecord>();

            _mockLocationRepository.Setup(r => r.GetUnsyncedRecordsAsync(batchSize))
                .ReturnsAsync(emptyRecords);

            // Act
            var result = await _locationService.SyncPendingLocationsAsync(batchSize);

            // Assert
            result.Should().NotBeNull();
            result.SyncedIds.Should().BeEmpty();
            result.FailedIds.Should().BeEmpty();
            _mockLocationRepository.Verify(r => r.GetUnsyncedRecordsAsync(batchSize), Times.Once);
            _mockLocationRepository.Verify(r => r.UpdateSyncStatusAsync(It.IsAny<IEnumerable<int>>(), true), Times.Never);
        }

        [Fact]
        public async Task SyncPendingLocationsAsync_WithInvalidBatchSize_ShouldThrowArgumentException()
        {
            // Arrange
            var invalidBatchSize = 0; // Batch size must be greater than zero

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _locationService.SyncPendingLocationsAsync(invalidBatchSize));
        }

        [Fact]
        public async Task SyncPendingLocationsAsync_WithPartialFailures_ShouldTrackFailedIds()
        {
            // Arrange
            var batchSize = 50;
            var unsyncedRecords = new List<LocationRecord>
            {
                new LocationRecord { Id = 1, UserId = _userId, Latitude = 40.7128, Longitude = -74.0060, IsSynced = false },
                new LocationRecord { Id = 2, UserId = _userId, Latitude = 40.7130, Longitude = -74.0062, IsSynced = false },
                new LocationRecord { Id = 3, UserId = _userId, Latitude = 40.7135, Longitude = -74.0065, IsSynced = false }
            };

            _mockLocationRepository.Setup(r => r.GetUnsyncedRecordsAsync(batchSize))
                .ReturnsAsync(unsyncedRecords);
            
            // Simulate a situation where we throw an exception for record with Id=2
            _mockLocationRepository.Setup(r => r.UpdateSyncStatusAsync(It.Is<IEnumerable<int>>(ids => ids.Contains(2)), true))
                .ThrowsAsync(new Exception("Simulated failure for record 2"));

            // Act
            var result = await _locationService.SyncPendingLocationsAsync(batchSize);

            // Assert
            result.Should().NotBeNull();
            result.SyncedIds.Should().Contain(new[] { 1, 3 });
            result.FailedIds.Should().Contain(2);
            result.HasFailures().Should().BeTrue();
            result.GetSuccessCount().Should().Be(2);
            result.GetFailureCount().Should().Be(1);
        }
    }
}