# src/android/SecurityPatrol.IntegrationTests/DatabaseTests/RepositoryIntegrationTests.cs
```csharp
using System; // Version 8.0+
using System.Collections.Generic; // Version 8.0+
using System.Linq; // Version 8.0+
using System.Threading.Tasks; // Version 8.0+
using FluentAssertions; // FluentAssertions v6.0+
using Microsoft.Extensions.Logging; // Microsoft.Extensions.Logging v8.0+
using Microsoft.Extensions.Logging.Abstractions;
using SecurityPatrol.Constants; // Ensure DatabaseConstants is used correctly
using SecurityPatrol.Database.Entities; // Ensure entities are used correctly
using SecurityPatrol.IntegrationTests.Helpers; // Ensure TestDatabaseInitializer is used correctly
using SecurityPatrol.Models; // Ensure models are used correctly
using SecurityPatrol.Services; // Ensure repositories are used correctly
using SQLite; // SQLite-net-pcl v1.8+
using Xunit; // Xunit v2.4+

namespace SecurityPatrol.IntegrationTests.DatabaseTests
{
    /// <summary>
    /// Contains integration tests for repository implementations in the Security Patrol application.
    /// These tests verify that repository classes correctly interact with the SQLite database, perform CRUD operations,
    /// and handle data transformations between entities and models.
    /// </summary>
    public class RepositoryIntegrationTests : IDisposable
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<RepositoryIntegrationTests> _logger;
        private TestDatabaseInitializer _databaseInitializer;
        private TimeRecordRepository _timeRecordRepository;
        private LocationRepository _locationRepository;
        private PhotoRepository _photoRepository;
        private ReportRepository _reportRepository;
        private CheckpointRepository _checkpointRepository;

        private const string TestUserId = "test-user";
        private const double TestLatitude = 37.7749;
        private const double TestLongitude = -122.4194;

        /// <summary>
        /// Initializes a new instance of the RepositoryIntegrationTests class with required dependencies.
        /// </summary>
        public RepositoryIntegrationTests()
        {
            // Initialize _loggerFactory using LoggerFactory.Create
            _loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole(); // Add console logger for visibility
                builder.SetMinimumLevel(LogLevel.Debug); // Set minimum log level
            });

            // Create _logger using _loggerFactory
            _logger = _loggerFactory.CreateLogger<RepositoryIntegrationTests>();

            // Initialize _databaseInitializer with a new TestDatabaseInitializer instance using _logger
            _databaseInitializer = new TestDatabaseInitializer(_logger);

            // Initialize all repository instances with database service and appropriate loggers
            _timeRecordRepository = new TimeRecordRepository(_databaseInitializer, _loggerFactory.CreateLogger<TimeRecordRepository>());
            _locationRepository = new LocationRepository(_databaseInitializer, new MockAuthenticationStateProvider(true, TestUserId), _loggerFactory.CreateLogger<LocationRepository>());
            _photoRepository = new PhotoRepository(_databaseInitializer, _loggerFactory.CreateLogger<PhotoRepository>());
            _reportRepository = new ReportRepository(_databaseInitializer, _loggerFactory.CreateLogger<ReportRepository>());
            _checkpointRepository = new CheckpointRepository(_databaseInitializer, _loggerFactory.CreateLogger<CheckpointRepository>());

            // Call InitializeAsync on _databaseInitializer to ensure database is ready for tests
            _databaseInitializer.InitializeAsync().Wait();
        }

        /// <summary>
        /// Cleans up resources used by the test class.
        /// </summary>
        public void Dispose()
        {
            // Dispose _loggerFactory if it implements IDisposable
            if (_loggerFactory is IDisposable disposableLoggerFactory)
            {
                disposableLoggerFactory.Dispose();
            }

            // Set all repository instances to null
            _timeRecordRepository = null;
            _locationRepository = null;
            _photoRepository = null;
            _reportRepository = null;
            _checkpointRepository = null;

            // Set _databaseInitializer to null
            _databaseInitializer = null;

            // Set _logger to null
            _logger = null;

            // Set _loggerFactory to null
            _loggerFactory = null;
        }

        /// <summary>
        /// Tests that the TimeRecordRepository can save and retrieve time records.
        /// </summary>
        [Fact]
        public async Task TestTimeRecordRepository_SaveAndRetrieve()
        {
            // Reset the database to ensure a clean state
            await _databaseInitializer.ResetDatabaseAsync();

            // Create a new TimeRecordModel with test data
            var timeRecord = CreateTestTimeRecord();

            // Save the time record using _timeRecordRepository.SaveTimeRecordAsync
            int id = await _timeRecordRepository.SaveTimeRecordAsync(timeRecord);

            // Verify that the returned ID is greater than 0
            Assert.True(id > 0);

            // Retrieve the time record using _timeRecordRepository.GetTimeRecordByIdAsync
            var retrievedRecord = await _timeRecordRepository.GetTimeRecordByIdAsync(id);

            // Verify that the retrieved record matches the saved record
            Assert.NotNull(retrievedRecord);
            Assert.Equal(timeRecord.Type, retrievedRecord.Type);
            Assert.Equal(timeRecord.UserId, retrievedRecord.UserId);
            Assert.Equal(timeRecord.Latitude, retrievedRecord.Latitude);
            Assert.Equal(timeRecord.Longitude, retrievedRecord.Longitude);

            // Retrieve all time records using _timeRecordRepository.GetTimeRecordsAsync
            var allRecords = await _timeRecordRepository.GetTimeRecordsAsync(10);

            // Verify that the list contains the saved record
            Assert.Contains(retrievedRecord, allRecords);
        }

        /// <summary>
        /// Tests that the TimeRecordRepository can update existing time records.
        /// </summary>
        [Fact]
        public async Task TestTimeRecordRepository_Update()
        {
            // Reset the database to ensure a clean state
            await _databaseInitializer.ResetDatabaseAsync();

            // Create and save a new TimeRecordModel with test data
            var timeRecord = CreateTestTimeRecord();
            int id = await _timeRecordRepository.SaveTimeRecordAsync(timeRecord);

            // Modify the saved record (change Type from 'ClockIn' to 'ClockOut')
            timeRecord.Type = "ClockOut";

            // Update the record using _timeRecordRepository.SaveTimeRecordAsync
            await _timeRecordRepository.SaveTimeRecordAsync(timeRecord);

            // Retrieve the updated record using _timeRecordRepository.GetTimeRecordByIdAsync
            var updatedRecord = await _timeRecordRepository.GetTimeRecordByIdAsync(id);

            // Verify that the retrieved record reflects the changes
            Assert.NotNull(updatedRecord);
            Assert.Equal("ClockOut", updatedRecord.Type);
        }

        /// <summary>
        /// Tests that the TimeRecordRepository can delete time records.
        /// </summary>
        [Fact]
        public async Task TestTimeRecordRepository_Delete()
        {
            // Reset the database to ensure a clean state
            await _databaseInitializer.ResetDatabaseAsync();

            // Create and save a new TimeRecordModel with test data
            var timeRecord = CreateTestTimeRecord();
            int id = await _timeRecordRepository.SaveTimeRecordAsync(timeRecord);

            // Delete the record using _timeRecordRepository.DeleteTimeRecordAsync
            await _timeRecordRepository.DeleteTimeRecordAsync(id);

            // Attempt to retrieve the deleted record using _timeRecordRepository.GetTimeRecordByIdAsync
            var deletedRecord = await _timeRecordRepository.GetTimeRecordByIdAsync(id);

            // Verify that the retrieved record is null
            Assert.Null(deletedRecord);
        }

        /// <summary>
        /// Tests that the TimeRecordRepository can retrieve unsynchronized time records.
        /// </summary>
        [Fact]
        public async Task TestTimeRecordRepository_GetPendingRecords()
        {
            // Reset the database to ensure a clean state
            await _databaseInitializer.ResetDatabaseAsync();

            // Create and save multiple TimeRecordModel instances with different IsSynced values
            await _timeRecordRepository.SaveTimeRecordAsync(CreateTestTimeRecord(isSynced: true));
            await _timeRecordRepository.SaveTimeRecordAsync(CreateTestTimeRecord(isSynced: false));
            await _timeRecordRepository.SaveTimeRecordAsync(CreateTestTimeRecord(isSynced: false));

            // Retrieve pending records using _timeRecordRepository.GetPendingRecordsAsync
            var pendingRecords = await _timeRecordRepository.GetPendingRecordsAsync();

            // Verify that only records with IsSynced=false are returned
            Assert.Equal(2, pendingRecords.Count);
            Assert.All(pendingRecords, record => Assert.False(record.IsSynced));
        }

        /// <summary>
        /// Tests that the TimeRecordRepository can update the synchronization status of time records.
        /// </summary>
        [Fact]
        public async Task TestTimeRecordRepository_UpdateSyncStatus()
        {
            // Reset the database to ensure a clean state
            await _databaseInitializer.ResetDatabaseAsync();

            // Create and save a new TimeRecordModel with IsSynced=false
            var timeRecord = CreateTestTimeRecord(isSynced: false);
            int id = await _timeRecordRepository.SaveTimeRecordAsync(timeRecord);

            // Update the sync status to true using _timeRecordRepository.UpdateSyncStatusAsync
            await _timeRecordRepository.UpdateSyncStatusAsync(id, true);

            // Retrieve the updated record using _timeRecordRepository.GetTimeRecordByIdAsync
            var updatedRecord = await _timeRecordRepository.GetTimeRecordByIdAsync(id);

            // Verify that the IsSynced property is now true
            Assert.NotNull(updatedRecord);
            Assert.True(updatedRecord.IsSynced);
        }

        /// <summary>
        /// Tests that the LocationRepository can save and retrieve location records.
        /// </summary>
        [Fact]
        public async Task TestLocationRepository_SaveAndRetrieve()
        {
            // Reset the database to ensure a clean state
            await _databaseInitializer.ResetDatabaseAsync();

            // Create a new LocationModel with test data
            var location = CreateTestLocationRecord();

            // Save the location record using _locationRepository.SaveLocationAsync
            int id = await _locationRepository.SaveLocationAsync(location);

            // Verify that the returned ID is greater than 0
            Assert.True(id > 0);

            // Retrieve all location records using _locationRepository.GetLocationsAsync
            var allLocations = await _locationRepository.GetLocationsAsync(l => l.UserId == TestUserId);

            // Verify that the list contains the saved record
            Assert.Contains(location, allLocations);
        }

        /// <summary>
        /// Tests that the LocationRepository can retrieve unsynchronized location records.
        /// </summary>
        [Fact]
        public async Task TestLocationRepository_GetPendingLocations()
        {
            // Reset the database to ensure a clean state
            await _databaseInitializer.ResetDatabaseAsync();

            // Create and save multiple LocationModel instances with different IsSynced values
            await _locationRepository.SaveLocationAsync(CreateTestLocationRecord(isSynced: true));
            await _locationRepository.SaveLocationAsync(CreateTestLocationRecord(isSynced: false));
            await _locationRepository.SaveLocationAsync(CreateTestLocationRecord(isSynced: false));

            // Retrieve pending locations using _locationRepository.GetPendingSyncLocationsAsync
            var pendingLocations = await _locationRepository.GetPendingSyncLocationsAsync(10);

            // Verify that only records with IsSynced=false are returned
            Assert.Equal(2, pendingLocations.Count());
            Assert.All(pendingLocations, location => Assert.False(location.IsSynced));
        }

        /// <summary>
        /// Tests that the PhotoRepository can save and retrieve photo records.
        /// </summary>
        [Fact]
        public async Task TestPhotoRepository_SaveAndRetrieve()
        {
            // Reset the database to ensure a clean state
            await _databaseInitializer.ResetDatabaseAsync();

            // Create a new PhotoModel with test data
            var photo = CreateTestPhoto();

            // Save the photo record using _photoRepository.SavePhotoAsync
            string id = await _photoRepository.SavePhotoAsync(photo, new MemoryStream());

            // Verify that the returned ID is greater than 0
            Assert.NotNull(id);

            // Retrieve the photo record using _photoRepository.GetPhotoByIdAsync
            var retrievedPhoto = await _photoRepository.GetPhotoByIdAsync(id);

            // Verify that the retrieved record matches the saved record
            Assert.NotNull(retrievedPhoto);
            Assert.Equal(photo.UserId, retrievedPhoto.UserId);
            Assert.Equal(photo.Latitude, retrievedPhoto.Latitude);
            Assert.Equal(photo.Longitude, retrievedPhoto.Longitude);
            Assert.Equal(photo.FilePath, retrievedPhoto.FilePath);

            // Retrieve all photo records using _photoRepository.GetPhotosAsync
            var allPhotos = await _photoRepository.GetPhotosAsync();

            // Verify that the list contains the saved record
            Assert.Contains(retrievedPhoto, allPhotos);
        }

        /// <summary>
        /// Tests that the PhotoRepository can update the synchronization progress of photo records.
        /// </summary>
        [Fact]
        public async Task TestPhotoRepository_UpdateSyncProgress()
        {
            // Reset the database to ensure a clean state
            await _databaseInitializer.ResetDatabaseAsync();

            // Create and save a new PhotoModel with SyncProgress=0
            var photo = CreateTestPhoto();
            string id = await _photoRepository.SavePhotoAsync(photo, new MemoryStream());

            // Update the sync progress to 50 using _photoRepository.UpdateSyncProgressAsync
            await _photoRepository.UpdateSyncProgressAsync(id, 50);

            // Retrieve the updated record using _photoRepository.GetPhotoByIdAsync
            var updatedPhoto = await _photoRepository.GetPhotoByIdAsync(id);

            // Verify that the SyncProgress property is now 50
            Assert.NotNull(updatedPhoto);
            Assert.Equal(50, updatedPhoto.SyncProgress);
        }

        /// <summary>
        /// Tests that the ReportRepository can save and retrieve activity reports.
        /// </summary>
        [Fact]
        public async Task TestReportRepository_SaveAndRetrieve()
        {
            // Reset the database to ensure a clean state
            await _databaseInitializer.ResetDatabaseAsync();

            // Create a new ReportModel with test data
            var report = CreateTestReport();

            // Save the report using _reportRepository.SaveReportAsync
            int id = await _reportRepository.SaveReportAsync(report);

            // Verify that the returned ID is greater than 0
            Assert.True(id > 0);

            // Retrieve the report using _reportRepository.GetReportByIdAsync
            var retrievedReport = await _reportRepository.GetReportAsync(id);

            // Verify that the retrieved report matches the saved report
            Assert.NotNull(retrievedReport);
            Assert.Equal(report.UserId, retrievedReport.UserId);
            Assert.Equal(report.Text, retrievedReport.Text);
            Assert.Equal(report.Latitude, retrievedReport.Latitude);
            Assert.Equal(report.Longitude, retrievedReport.Longitude);

            // Retrieve all reports using _reportRepository.GetReportsAsync
            var allReports = await _reportRepository.GetAllReportsAsync();

            // Verify that the list contains the saved report
            Assert.Contains(retrievedReport, allReports);
        }

        /// <summary>
        /// Tests that the CheckpointRepository can save and retrieve checkpoints.
        /// </summary>
        [Fact]
        public async Task TestCheckpointRepository_SaveAndRetrieve()
        {
            // Reset the database to ensure a clean state
            await _databaseInitializer.ResetDatabaseAsync();

            // Create a new CheckpointModel with test data and a predefined location ID
            int locationId = 1; // Predefined location ID
            var checkpoint = CreateTestCheckpoint(locationId);

            // Save the checkpoint using _checkpointRepository.SaveCheckpointAsync
            int id = await _checkpointRepository.SaveCheckpointAsync(checkpoint);

            // Verify that the returned ID is greater than 0
            Assert.True(id > 0);

            // Retrieve all checkpoints using _checkpointRepository.GetAllCheckpointsAsync
            var allCheckpoints = await _checkpointRepository.GetAllCheckpointsAsync();

            // Verify that the list contains the saved checkpoint
            Assert.Contains(checkpoint, allCheckpoints);

            // Retrieve checkpoints by location ID using _checkpointRepository.GetCheckpointsByLocationIdAsync
            var checkpointsByLocation = await _checkpointRepository.GetCheckpointsByLocationIdAsync(locationId);

            // Verify that the list contains only checkpoints for the specified location
            Assert.All(checkpointsByLocation, c => Assert.Equal(locationId, c.LocationId));
        }

        /// <summary>
        /// Helper method to create a test time record model.
        /// </summary>
        /// <param name="userId">The user ID to assign</param>
        /// <param name="type">The type of time record (ClockIn/ClockOut)</param>
        /// <param name="isSynced">Whether the record should be marked as synced</param>
        /// <returns>A new time record model with test data</returns>
        private TimeRecordModel CreateTestTimeRecord(string userId = TestUserId, string type = "ClockIn", bool isSynced = false)
        {
            // Create a new TimeRecordModel with the specified parameters
            var timeRecord = new TimeRecordModel
            {
                UserId = userId,
                Type = type,
                Timestamp = DateTime.Now,
                Latitude = TestLatitude,
                Longitude = TestLongitude,
                IsSynced = isSynced
            };

            // Return the created model
            return timeRecord;
        }

        /// <summary>
        /// Helper method to create a test location record model.
        /// </summary>
        /// <param name="userId">The user ID to assign</param>
        /// <param name="isSynced">Whether the record should be marked as synced</param>
        /// <returns>A new location record model with test data</returns>
        private LocationModel CreateTestLocationRecord(string userId = TestUserId, bool isSynced = false)
        {
            // Create a new LocationModel with the specified userId
            var location = new LocationModel
            {
                UserId = userId,
                Timestamp = DateTime.Now,
                Latitude = TestLatitude,
                Longitude = TestLongitude,
                Accuracy = 10.0,
                IsSynced = isSynced
            };

            // Return the created model
            return location;
        }

        /// <summary>
        /// Helper method to create a test photo model.
        /// </summary>
        /// <param name="userId">The user ID to assign</param>
        /// <param name="isSynced">Whether the record should be marked as synced</param>
        /// <returns>A new photo model with test data</returns>
        private PhotoModel CreateTestPhoto(string userId = TestUserId, bool isSynced = false)
        {
            // Create a new PhotoModel with the specified userId
            var photo = new PhotoModel
            {
                UserId = userId,
                Timestamp = DateTime.Now,
                Latitude = TestLatitude,
                Longitude = TestLongitude,
                FilePath = "/test/path/photo.jpg",
                IsSynced = isSynced,
                SyncProgress = 0
            };

            // Return the created model
            return photo;
        }

        /// <summary>
        /// Helper method to create a test activity report model.
        /// </summary>
        /// <param name="userId">The user ID to assign</param>
        /// <param name="isSynced">Whether the record should be marked as synced</param>
        /// <returns>A new report model with test data</returns>
        private ReportModel CreateTestReport(string userId = TestUserId, bool isSynced = false)
        {
            // Create a new ReportModel with the specified userId
            var report = new ReportModel
            {
                UserId = userId,
                Text = "Test activity report",
                Timestamp = DateTime.Now,
                Latitude = TestLatitude,
                Longitude = TestLongitude,
                IsSynced = isSynced
            };

            // Return the created model
            return report;
        }

        /// <summary>
        /// Helper method to create a test checkpoint model.
        /// </summary>
        /// <param name="locationId">The location ID to assign</param>
        /// <param name="name">The name of the checkpoint</param>
        /// <returns>A new checkpoint model with test data</returns>
        private CheckpointModel CreateTestCheckpoint(int locationId, string name = "Test Checkpoint")
        {
            // Create a new CheckpointModel
            var checkpoint = new CheckpointModel
            {
                LocationId = locationId,
                Name = name,
                Latitude = TestLatitude,
                Longitude = TestLongitude
            };

            // Return the created model
            return checkpoint;
        }
    }

    /// <summary>
    /// Mock implementation of IAuthenticationStateProvider for testing purposes.
    /// </summary>
    public class MockAuthenticationStateProvider : IAuthenticationStateProvider
    {
        private readonly bool _isAuthenticated;
        private readonly string _phoneNumber;

        /// <summary>
        /// Initializes a new instance of the <see cref="MockAuthenticationStateProvider"/> class.
        /// </summary>
        /// <param name="isAuthenticated">Whether the user is authenticated.</param>
        /// <param name="phoneNumber">The user's phone number.</param>
        public MockAuthenticationStateProvider(bool isAuthenticated, string phoneNumber)
        {
            _isAuthenticated = isAuthenticated;
            _phoneNumber = phoneNumber;
        }

        /// <inheritdoc />
        public event EventHandler StateChanged;

        /// <inheritdoc />
        public Task<AuthState> GetCurrentState()
        {
            return Task.FromResult(new AuthState(_isAuthenticated, _phoneNumber));
        }

        /// <inheritdoc />
        public void UpdateState(AuthState state)
        {
            // Not implemented for mock
        }

        /// <inheritdoc />
        public Task<bool> IsAuthenticated()
        {
            return Task.FromResult(_isAuthenticated);
        }

        /// <inheritdoc />
        public void NotifyStateChanged()
        {
            // Not implemented for mock
        }
    }
}