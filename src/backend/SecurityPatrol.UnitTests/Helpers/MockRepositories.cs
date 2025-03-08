using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.UnitTests.Helpers;

namespace SecurityPatrol.UnitTests.Helpers
{
    /// <summary>
    /// Static class providing factory methods to create mock repository implementations for unit testing
    /// </summary>
    public static class MockRepositories
    {
        /// <summary>
        /// Private constructor to prevent instantiation of static class
        /// </summary>
        private MockRepositories() { }

        /// <summary>
        /// Creates a mock implementation of IUserRepository with predefined test data and behavior
        /// </summary>
        /// <returns>A configured mock of IUserRepository</returns>
        public static Mock<IUserRepository> CreateMockUserRepository()
        {
            var mock = new Mock<IUserRepository>();
            var users = TestData.GetTestUsers();

            // Setup GetByIdAsync to return a user by ID from test data
            mock.Setup(repo => repo.GetByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => TestData.GetTestUserById(id));

            // Setup GetByPhoneNumberAsync to return a user by phone number from test data
            mock.Setup(repo => repo.GetByPhoneNumberAsync(It.IsAny<string>()))
                .ReturnsAsync((string phoneNumber) => TestData.GetTestUserByPhoneNumber(phoneNumber));

            // Setup GetAllAsync to return all test users
            mock.Setup(repo => repo.GetAllAsync())
                .ReturnsAsync(users);

            // Setup AddAsync to return the added user with a generated ID
            mock.Setup(repo => repo.AddAsync(It.IsAny<User>()))
                .ReturnsAsync((User user) =>
                {
                    if (string.IsNullOrEmpty(user.Id))
                    {
                        user.Id = Guid.NewGuid().ToString();
                    }
                    return user;
                });

            // Setup UpdateAsync to return a completed task
            mock.Setup(repo => repo.UpdateAsync(It.IsAny<User>()))
                .Returns(Task.CompletedTask);

            // Setup DeleteAsync to return a completed task
            mock.Setup(repo => repo.DeleteAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Setup UpdateLastAuthenticatedAsync to return a completed task
            mock.Setup(repo => repo.UpdateLastAuthenticatedAsync(It.IsAny<string>(), It.IsAny<DateTime>()))
                .Returns(Task.CompletedTask);

            return mock;
        }

        /// <summary>
        /// Creates a mock implementation of ITimeRecordRepository with predefined test data and behavior
        /// </summary>
        /// <returns>A configured mock of ITimeRecordRepository</returns>
        public static Mock<ITimeRecordRepository> CreateMockTimeRecordRepository()
        {
            var mock = new Mock<ITimeRecordRepository>();
            var timeRecords = TestData.GetTestTimeRecords();

            // Setup GetByIdAsync to return a time record by ID from test data
            mock.Setup(repo => repo.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) => TestData.GetTestTimeRecordById(id));

            // Setup GetByUserIdAsync to return time records for a specific user
            mock.Setup(repo => repo.GetByUserIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string userId) => timeRecords.Where(t => t.UserId == userId).ToList());

            // Setup GetAllAsync to return all test time records
            mock.Setup(repo => repo.GetAllAsync())
                .ReturnsAsync(timeRecords);

            // Setup AddAsync to return the added time record with a generated ID
            mock.Setup(repo => repo.AddAsync(It.IsAny<TimeRecord>()))
                .ReturnsAsync((TimeRecord record) =>
                {
                    if (record.Id == 0)
                    {
                        record.Id = timeRecords.Max(t => t.Id) + 1;
                    }
                    return record;
                });

            // Setup UpdateAsync to return a completed task
            mock.Setup(repo => repo.UpdateAsync(It.IsAny<TimeRecord>()))
                .Returns(Task.CompletedTask);

            // Setup DeleteAsync to return a completed task
            mock.Setup(repo => repo.DeleteAsync(It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            // Setup GetLatestByUserIdAsync to return the most recent time record for a user
            mock.Setup(repo => repo.GetLatestByUserIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string userId) => timeRecords
                    .Where(t => t.UserId == userId)
                    .OrderByDescending(t => t.Timestamp)
                    .FirstOrDefault());

            // Setup GetCurrentStatusAsync to return the current clock status for a user
            mock.Setup(repo => repo.GetCurrentStatusAsync(It.IsAny<string>()))
                .ReturnsAsync((string userId) =>
                {
                    var latestRecord = timeRecords
                        .Where(t => t.UserId == userId)
                        .OrderByDescending(t => t.Timestamp)
                        .FirstOrDefault();

                    return latestRecord?.Type == "ClockIn" ? "in" : "out";
                });

            // Setup GetUnsyncedAsync to return unsynced time records
            mock.Setup(repo => repo.GetUnsyncedAsync())
                .ReturnsAsync(timeRecords.Where(t => !t.IsSynced).ToList());

            // Setup UpdateSyncStatusAsync to return a completed task
            mock.Setup(repo => repo.UpdateSyncStatusAsync(It.IsAny<int>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            return mock;
        }

        /// <summary>
        /// Creates a mock implementation of ILocationRecordRepository with predefined test data and behavior
        /// </summary>
        /// <returns>A configured mock of ILocationRecordRepository</returns>
        public static Mock<ILocationRecordRepository> CreateMockLocationRecordRepository()
        {
            var mock = new Mock<ILocationRecordRepository>();
            var locationRecords = TestData.GetTestLocationRecords();

            // Setup GetByIdAsync to return a location record by ID from test data
            mock.Setup(repo => repo.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) => TestData.GetTestLocationRecordById(id));

            // Setup GetByUserIdAsync to return location records for a specific user
            mock.Setup(repo => repo.GetByUserIdAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync((string userId, int limit) => locationRecords
                    .Where(l => l.UserId == userId)
                    .OrderByDescending(l => l.Timestamp)
                    .Take(limit > 0 ? limit : locationRecords.Count)
                    .ToList());

            // Setup GetAllAsync to return all test location records
            mock.Setup(repo => repo.GetAllAsync())
                .ReturnsAsync(locationRecords);

            // Setup AddAsync to return the added location record with a generated ID
            mock.Setup(repo => repo.AddAsync(It.IsAny<LocationRecord>()))
                .ReturnsAsync((LocationRecord record) =>
                {
                    if (record.Id == 0)
                    {
                        record.Id = locationRecords.Max(l => l.Id) + 1;
                    }
                    return record.Id;
                });

            // Setup AddRangeAsync to return a list of added location records with generated IDs
            mock.Setup(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<LocationRecord>>()))
                .ReturnsAsync((IEnumerable<LocationRecord> records) =>
                {
                    var ids = new List<int>();
                    var nextId = locationRecords.Max(l => l.Id) + 1;

                    foreach (var record in records)
                    {
                        if (record.Id == 0)
                        {
                            record.Id = nextId++;
                        }
                        ids.Add(record.Id);
                    }

                    return ids;
                });

            // Setup GetUnsyncedAsync to return unsynced location records
            mock.Setup(repo => repo.GetUnsyncedAsync())
                .ReturnsAsync(locationRecords.Where(l => !l.IsSynced).ToList());

            // Setup UpdateSyncStatusAsync to return a completed task
            mock.Setup(repo => repo.UpdateSyncStatusAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<bool>()))
                .ReturnsAsync(true);

            return mock;
        }

        /// <summary>
        /// Creates a mock implementation of ICheckpointRepository with predefined test data and behavior
        /// </summary>
        /// <returns>A configured mock of ICheckpointRepository</returns>
        public static Mock<ICheckpointRepository> CreateMockCheckpointRepository()
        {
            var mock = new Mock<ICheckpointRepository>();
            var checkpoints = TestData.GetTestCheckpoints();

            // Setup GetByIdAsync to return a checkpoint by ID from test data
            mock.Setup(repo => repo.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) => TestData.GetTestCheckpointById(id));

            // Setup GetByLocationIdAsync to return checkpoints for a specific location
            mock.Setup(repo => repo.GetByLocationIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int locationId) => checkpoints.Where(c => c.LocationId == locationId).ToList());

            // Setup GetAllAsync to return all test checkpoints
            mock.Setup(repo => repo.GetAllAsync())
                .ReturnsAsync(checkpoints);

            // Setup AddAsync to return the added checkpoint with a generated ID
            mock.Setup(repo => repo.AddAsync(It.IsAny<Checkpoint>()))
                .ReturnsAsync((Checkpoint checkpoint) =>
                {
                    if (checkpoint.Id == 0)
                    {
                        checkpoint.Id = checkpoints.Max(c => c.Id) + 1;
                    }
                    return checkpoint;
                });

            // Setup UpdateAsync to return a completed task
            mock.Setup(repo => repo.UpdateAsync(It.IsAny<Checkpoint>()))
                .Returns(Task.CompletedTask);

            // Setup DeleteAsync to return a completed task
            mock.Setup(repo => repo.DeleteAsync(It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            return mock;
        }

        /// <summary>
        /// Creates a mock implementation of ICheckpointVerificationRepository with predefined test data and behavior
        /// </summary>
        /// <returns>A configured mock of ICheckpointVerificationRepository</returns>
        public static Mock<ICheckpointVerificationRepository> CreateMockCheckpointVerificationRepository()
        {
            var mock = new Mock<ICheckpointVerificationRepository>();
            var verifications = TestData.GetTestCheckpointVerifications();

            // Setup GetByIdAsync to return a checkpoint verification by ID from test data
            mock.Setup(repo => repo.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) => TestData.GetTestCheckpointVerificationById(id));

            // Setup GetByCheckpointIdAsync to return verifications for a specific checkpoint
            mock.Setup(repo => repo.GetByCheckpointIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int checkpointId) => verifications.Where(v => v.CheckpointId == checkpointId).ToList());

            // Setup GetByUserIdAsync to return verifications for a specific user
            mock.Setup(repo => repo.GetByUserIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string userId) => verifications.Where(v => v.UserId == userId).ToList());

            // Setup GetAllAsync to return all test checkpoint verifications
            mock.Setup(repo => repo.GetAllAsync())
                .ReturnsAsync(verifications);

            // Setup AddAsync to return the added verification with a generated ID
            mock.Setup(repo => repo.AddAsync(It.IsAny<CheckpointVerification>()))
                .ReturnsAsync((CheckpointVerification verification) =>
                {
                    if (verification.Id == 0)
                    {
                        verification.Id = verifications.Max(v => v.Id) + 1;
                    }
                    return verification;
                });

            // Setup UpdateAsync to return a completed task
            mock.Setup(repo => repo.UpdateAsync(It.IsAny<CheckpointVerification>()))
                .Returns(Task.CompletedTask);

            // Setup GetUnsyncedAsync to return unsynced verifications
            mock.Setup(repo => repo.GetUnsyncedAsync())
                .ReturnsAsync(verifications.Where(v => !v.IsSynced).ToList());

            // Setup UpdateSyncStatusAsync to return a completed task
            mock.Setup(repo => repo.UpdateSyncStatusAsync(It.IsAny<int>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            return mock;
        }

        /// <summary>
        /// Creates a mock implementation of IPatrolLocationRepository with predefined test data and behavior
        /// </summary>
        /// <returns>A configured mock of IPatrolLocationRepository</returns>
        public static Mock<IPatrolLocationRepository> CreateMockPatrolLocationRepository()
        {
            var mock = new Mock<IPatrolLocationRepository>();
            var patrolLocations = TestData.GetTestPatrolLocations();

            // Setup GetByIdAsync to return a patrol location by ID from test data
            mock.Setup(repo => repo.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) => TestData.GetTestPatrolLocationById(id));

            // Setup GetAllAsync to return all test patrol locations
            mock.Setup(repo => repo.GetAllAsync())
                .ReturnsAsync(patrolLocations);

            // Setup AddAsync to return the added patrol location with a generated ID
            mock.Setup(repo => repo.AddAsync(It.IsAny<PatrolLocation>()))
                .ReturnsAsync((PatrolLocation location) =>
                {
                    if (location.Id == 0)
                    {
                        location.Id = patrolLocations.Max(p => p.Id) + 1;
                    }
                    return location;
                });

            // Setup UpdateAsync to return a completed task
            mock.Setup(repo => repo.UpdateAsync(It.IsAny<PatrolLocation>()))
                .Returns(Task.CompletedTask);

            // Setup DeleteAsync to return a completed task
            mock.Setup(repo => repo.DeleteAsync(It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            return mock;
        }

        /// <summary>
        /// Creates a mock implementation of IPhotoRepository with predefined test data and behavior
        /// </summary>
        /// <returns>A configured mock of IPhotoRepository</returns>
        public static Mock<IPhotoRepository> CreateMockPhotoRepository()
        {
            var mock = new Mock<IPhotoRepository>();
            var photos = TestData.GetTestPhotos();

            // Setup GetByIdAsync to return a photo by ID from test data
            mock.Setup(repo => repo.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) => TestData.GetTestPhotoById(id));

            // Setup GetByUserIdAsync to return photos for a specific user
            mock.Setup(repo => repo.GetByUserIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string userId) => photos.Where(p => p.UserId == userId).ToList());

            // Setup GetAllAsync to return all test photos
            mock.Setup(repo => repo.GetAllAsync())
                .ReturnsAsync(photos);

            // Setup AddAsync to return the added photo with a generated ID
            mock.Setup(repo => repo.AddAsync(It.IsAny<Photo>()))
                .ReturnsAsync((Photo photo) =>
                {
                    if (photo.Id == 0)
                    {
                        photo.Id = photos.Max(p => p.Id) + 1;
                    }
                    return photo;
                });

            // Setup UpdateAsync to return a completed task
            mock.Setup(repo => repo.UpdateAsync(It.IsAny<Photo>()))
                .Returns(Task.CompletedTask);

            // Setup DeleteAsync to return a completed task
            mock.Setup(repo => repo.DeleteAsync(It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            // Setup GetUnsyncedAsync to return unsynced photos
            mock.Setup(repo => repo.GetUnsyncedAsync())
                .ReturnsAsync(photos.Where(p => !p.IsSynced).ToList());

            // Setup UpdateSyncStatusAsync to return a completed task
            mock.Setup(repo => repo.UpdateSyncStatusAsync(It.IsAny<int>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            return mock;
        }

        /// <summary>
        /// Creates a mock implementation of IReportRepository with predefined test data and behavior
        /// </summary>
        /// <returns>A configured mock of IReportRepository</returns>
        public static Mock<IReportRepository> CreateMockReportRepository()
        {
            var mock = new Mock<IReportRepository>();
            var reports = TestData.GetTestReports();

            // Setup GetByIdAsync to return a report by ID from test data
            mock.Setup(repo => repo.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) => TestData.GetTestReportById(id));

            // Setup GetByUserIdAsync to return reports for a specific user
            mock.Setup(repo => repo.GetByUserIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string userId) => reports.Where(r => r.UserId == userId).ToList());

            // Setup GetAllAsync to return all test reports
            mock.Setup(repo => repo.GetAllAsync())
                .ReturnsAsync(reports);

            // Setup AddAsync to return the added report with a generated ID
            mock.Setup(repo => repo.AddAsync(It.IsAny<Report>()))
                .ReturnsAsync((Report report) =>
                {
                    if (report.Id == 0)
                    {
                        report.Id = reports.Max(r => r.Id) + 1;
                    }
                    return report;
                });

            // Setup UpdateAsync to return a completed task
            mock.Setup(repo => repo.UpdateAsync(It.IsAny<Report>()))
                .Returns(Task.CompletedTask);

            // Setup DeleteAsync to return a completed task
            mock.Setup(repo => repo.DeleteAsync(It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            // Setup GetUnsyncedAsync to return unsynced reports
            mock.Setup(repo => repo.GetUnsyncedAsync())
                .ReturnsAsync(reports.Where(r => !r.IsSynced).ToList());

            // Setup UpdateSyncStatusAsync to return a completed task
            mock.Setup(repo => repo.UpdateSyncStatusAsync(It.IsAny<int>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            return mock;
        }
    }
}