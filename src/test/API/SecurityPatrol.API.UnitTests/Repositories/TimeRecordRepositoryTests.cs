using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using FluentAssertions;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.Core.Models;
using SecurityPatrol.Infrastructure.Persistence;
using SecurityPatrol.Infrastructure.Persistence.Repositories;
using SecurityPatrol.Infrastructure.Persistence.Interceptors;
using SecurityPatrol.TestCommon.Constants;
using SecurityPatrol.API.UnitTests.Setup;

namespace SecurityPatrol.API.UnitTests.Repositories
{
    /// <summary>
    /// Contains unit tests for the TimeRecordRepository class to verify its data access operations for TimeRecord entities.
    /// </summary>
    public class TimeRecordRepositoryTests : IDisposable
    {
        private readonly SecurityPatrolDbContext _dbContext;
        private readonly TimeRecordRepository _repository;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IDateTime> _mockDateTime;

        /// <summary>
        /// Initializes a new instance of the TimeRecordRepositoryTests class with an in-memory database context and repository instance.
        /// </summary>
        public TimeRecordRepositoryTests()
        {
            // Set up in-memory database options with a unique database name
            var dbName = $"InMemoryTimeRecordDb_{Guid.NewGuid()}";
            var options = new DbContextOptionsBuilder<SecurityPatrolDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            // Create mock services for AuditableEntityInterceptor
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockDateTime = new Mock<IDateTime>();
            
            _mockCurrentUserService.Setup(m => m.GetUserId()).Returns("test-user");
            _mockDateTime.Setup(m => m.Now).Returns(DateTime.UtcNow);

            var interceptor = new AuditableEntityInterceptor(_mockCurrentUserService.Object, _mockDateTime.Object);
            
            // Initialize _dbContext with in-memory options and the interceptor
            _dbContext = new SecurityPatrolDbContext(options, interceptor);
            
            // Initialize _repository with the database context
            _repository = new TimeRecordRepository(_dbContext);
        }

        /// <summary>
        /// Cleans up resources after tests are complete.
        /// </summary>
        public void Dispose()
        {
            _dbContext.Dispose();
        }

        /// <summary>
        /// Tests that GetByIdAsync returns the correct time record when given a valid ID.
        /// </summary>
        [Fact]
        public async Task GetByIdAsync_WithValidId_ReturnsTimeRecord()
        {
            // Arrange: Create a test time record with a known ID
            var testRecord = CreateTestTimeRecord(1);
            await _dbContext.TimeRecords.AddAsync(testRecord);
            await _dbContext.SaveChangesAsync();

            // Act: Call _repository.GetByIdAsync with the known ID
            var result = await _repository.GetByIdAsync(1);

            // Assert: Verify the returned time record is not null
            result.Should().NotBeNull();
            // Assert: Verify the returned time record has the expected ID
            result.Id.Should().Be(1);
        }

        /// <summary>
        /// Tests that GetByIdAsync returns null when given an invalid ID.
        /// </summary>
        [Fact]
        public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange: Create a non-existent ID
            var nonExistentId = 999;

            // Act: Call _repository.GetByIdAsync with the non-existent ID
            var result = await _repository.GetByIdAsync(nonExistentId);

            // Assert: Verify the returned result is null
            result.Should().BeNull();
        }

        /// <summary>
        /// Tests that GetByRemoteIdAsync returns the correct time record when given a valid remote ID.
        /// </summary>
        [Fact]
        public async Task GetByRemoteIdAsync_WithValidRemoteId_ReturnsTimeRecord()
        {
            // Arrange: Create a test time record with a known remote ID
            var remoteId = "remote-id-123";
            var testRecord = CreateTestTimeRecord(1, remoteId: remoteId);
            await _dbContext.TimeRecords.AddAsync(testRecord);
            await _dbContext.SaveChangesAsync();

            // Act: Call _repository.GetByRemoteIdAsync with the known remote ID
            var result = await _repository.GetByRemoteIdAsync(remoteId);

            // Assert: Verify the returned time record is not null
            result.Should().NotBeNull();
            // Assert: Verify the returned time record has the expected remote ID
            result.RemoteId.Should().Be(remoteId);
        }

        /// <summary>
        /// Tests that GetByRemoteIdAsync returns null when given an invalid remote ID.
        /// </summary>
        [Fact]
        public async Task GetByRemoteIdAsync_WithInvalidRemoteId_ReturnsNull()
        {
            // Arrange: Create a non-existent remote ID
            var nonExistentRemoteId = "non-existent";

            // Act: Call _repository.GetByRemoteIdAsync with the non-existent remote ID
            var result = await _repository.GetByRemoteIdAsync(nonExistentRemoteId);

            // Assert: Verify the returned result is null
            result.Should().BeNull();
        }

        /// <summary>
        /// Tests that GetAllAsync returns all time records in the database.
        /// </summary>
        [Fact]
        public async Task GetAllAsync_ReturnsAllTimeRecords()
        {
            // Arrange: Create multiple test time records
            var testRecords = new List<TimeRecord>
            {
                CreateTestTimeRecord(1),
                CreateTestTimeRecord(2),
                CreateTestTimeRecord(3)
            };
            
            await _dbContext.TimeRecords.AddRangeAsync(testRecords);
            await _dbContext.SaveChangesAsync();

            // Act: Call _repository.GetAllAsync
            var results = await _repository.GetAllAsync();

            // Assert: Verify the returned collection is not null
            results.Should().NotBeNull();
            // Assert: Verify the returned collection contains the expected number of time records
            results.Should().HaveCount(3);
            // Assert: Verify the returned collection contains all the added time records
            results.Select(r => r.Id).Should().Contain(new[] { 1, 2, 3 });
        }

        /// <summary>
        /// Tests that GetPaginatedAsync returns a paginated list of time records.
        /// </summary>
        [Fact]
        public async Task GetPaginatedAsync_ReturnsPaginatedTimeRecords()
        {
            // Arrange: Create multiple test time records (more than page size)
            var testRecords = new List<TimeRecord>();
            for (int i = 1; i <= 25; i++)
            {
                testRecords.Add(CreateTestTimeRecord(i));
            }
            
            await _dbContext.TimeRecords.AddRangeAsync(testRecords);
            await _dbContext.SaveChangesAsync();

            // Act: Call _repository.GetPaginatedAsync with page number and page size
            var pageNumber = 2;
            var pageSize = 10;
            var result = await _repository.GetPaginatedAsync(pageNumber, pageSize);

            // Assert: Verify the returned paginated list is not null
            result.Should().NotBeNull();
            // Assert: Verify the returned paginated list has the correct page number
            result.PageNumber.Should().Be(pageNumber);
            // Assert: Verify the returned paginated list has the correct page size
            result.Items.Should().HaveCount(pageSize);
            // Assert: Verify the returned paginated list has the correct total count
            result.TotalCount.Should().Be(25);
            // Assert: Verify the returned paginated list contains the expected time records for the requested page
            var expectedIds = Enumerable.Range(11, 10).OrderByDescending(id => testRecords.First(r => r.Id == id).Timestamp).ToList();
            result.Items.Select(r => r.Id).Should().BeEquivalentTo(expectedIds);
        }

        /// <summary>
        /// Tests that GetByUserIdAsync returns only time records for the specified user.
        /// </summary>
        [Fact]
        public async Task GetByUserIdAsync_ReturnsTimeRecordsForSpecificUser()
        {
            // Arrange: Create test time records for multiple users
            var userId1 = "user-1";
            var userId2 = "user-2";
            
            var userRecords = new List<TimeRecord>
            {
                CreateTestTimeRecord(1, userId1),
                CreateTestTimeRecord(2, userId1),
                CreateTestTimeRecord(3, userId2)
            };
            
            await _dbContext.TimeRecords.AddRangeAsync(userRecords);
            await _dbContext.SaveChangesAsync();

            // Act: Call _repository.GetByUserIdAsync with a specific user ID
            var results = await _repository.GetByUserIdAsync(userId1);

            // Assert: Verify the returned collection is not null
            results.Should().NotBeNull();
            // Assert: Verify the returned collection only contains time records for the specified user
            results.All(r => r.UserId == userId1).Should().BeTrue();
            // Assert: Verify the returned collection contains the expected number of time records
            results.Should().HaveCount(2);
        }

        /// <summary>
        /// Tests that GetPaginatedByUserIdAsync returns a paginated list of time records for the specified user.
        /// </summary>
        [Fact]
        public async Task GetPaginatedByUserIdAsync_ReturnsPaginatedTimeRecordsForSpecificUser()
        {
            // Arrange: Create multiple test time records for a specific user (more than page size)
            var userId = "test-user";
            var testRecords = new List<TimeRecord>();
            
            for (int i = 1; i <= 25; i++)
            {
                testRecords.Add(CreateTestTimeRecord(i, userId));
            }
            
            // Add some records for another user
            for (int i = 26; i <= 30; i++)
            {
                testRecords.Add(CreateTestTimeRecord(i, "other-user"));
            }
            
            await _dbContext.TimeRecords.AddRangeAsync(testRecords);
            await _dbContext.SaveChangesAsync();

            // Act: Call _repository.GetPaginatedByUserIdAsync with the user ID, page number, and page size
            var pageNumber = 2;
            var pageSize = 10;
            var result = await _repository.GetPaginatedByUserIdAsync(userId, pageNumber, pageSize);

            // Assert: Verify the returned paginated list is not null
            result.Should().NotBeNull();
            // Assert: Verify the returned paginated list has the correct page number
            result.PageNumber.Should().Be(pageNumber);
            // Assert: Verify the returned paginated list has the correct page size
            result.Items.Should().HaveCount(pageSize);
            // Assert: Verify the returned paginated list has the correct total count
            result.TotalCount.Should().Be(25); // Only the test user's records
            // Assert: Verify the returned paginated list contains only time records for the specified user
            result.Items.All(r => r.UserId == userId).Should().BeTrue();
            // Assert: Verify the returned paginated list contains the expected time records for the requested page
            var expectedIds = testRecords.Where(r => r.UserId == userId)
                                         .OrderByDescending(r => r.Timestamp)
                                         .Skip((pageNumber - 1) * pageSize)
                                         .Take(pageSize)
                                         .Select(r => r.Id)
                                         .ToList();
            result.Items.Select(r => r.Id).Should().BeEquivalentTo(expectedIds);
        }

        /// <summary>
        /// Tests that GetByUserIdAndDateAsync returns only time records for the specified user on the specified date.
        /// </summary>
        [Fact]
        public async Task GetByUserIdAndDateAsync_ReturnsTimeRecordsForSpecificUserAndDate()
        {
            // Arrange: Create a specific date for testing
            var userId = "test-user";
            var testDate = new DateTime(2023, 5, 15);
            
            var testRecords = new List<TimeRecord>
            {
                // Records for the test user on the test date
                CreateTestTimeRecord(1, userId, timestamp: testDate.AddHours(9)),
                CreateTestTimeRecord(2, userId, timestamp: testDate.AddHours(17)),
                
                // Record for the test user on a different date
                CreateTestTimeRecord(3, userId, timestamp: testDate.AddDays(1)),
                
                // Record for a different user on the test date
                CreateTestTimeRecord(4, "other-user", timestamp: testDate.AddHours(10))
            };
            
            await _dbContext.TimeRecords.AddRangeAsync(testRecords);
            await _dbContext.SaveChangesAsync();

            // Act: Call _repository.GetByUserIdAndDateAsync with the user ID and test date
            var results = await _repository.GetByUserIdAndDateAsync(userId, testDate);

            // Assert: Verify the returned collection is not null
            results.Should().NotBeNull();
            // Assert: Verify the returned collection only contains time records for the specified user on the specified date
            results.All(r => r.UserId == userId && r.Timestamp.Date == testDate.Date).Should().BeTrue();
            // Assert: Verify the returned collection contains the expected number of time records
            results.Should().HaveCount(2);
        }

        /// <summary>
        /// Tests that GetByUserIdAndDateRangeAsync returns only time records for the specified user within the specified date range.
        /// </summary>
        [Fact]
        public async Task GetByUserIdAndDateRangeAsync_ReturnsTimeRecordsForSpecificUserWithinDateRange()
        {
            // Arrange: Create a specific date range for testing
            var userId = "test-user";
            var startDate = new DateTime(2023, 5, 15);
            var endDate = new DateTime(2023, 5, 17);
            
            var testRecords = new List<TimeRecord>
            {
                // Records for the test user within the date range
                CreateTestTimeRecord(1, userId, timestamp: startDate),
                CreateTestTimeRecord(2, userId, timestamp: startDate.AddDays(1)),
                CreateTestTimeRecord(3, userId, timestamp: endDate),
                
                // Record for the test user outside the date range
                CreateTestTimeRecord(4, userId, timestamp: startDate.AddDays(-1)),
                CreateTestTimeRecord(5, userId, timestamp: endDate.AddDays(1)),
                
                // Record for a different user within the date range
                CreateTestTimeRecord(6, "other-user", timestamp: startDate.AddDays(1))
            };
            
            await _dbContext.TimeRecords.AddRangeAsync(testRecords);
            await _dbContext.SaveChangesAsync();

            // Act: Call _repository.GetByUserIdAndDateRangeAsync with the user ID, start date, and end date
            var results = await _repository.GetByUserIdAndDateRangeAsync(userId, startDate, endDate);

            // Assert: Verify the returned collection is not null
            results.Should().NotBeNull();
            // Assert: Verify the returned collection only contains time records for the specified user within the specified date range
            results.All(r => r.UserId == userId && 
                            r.Timestamp >= startDate && 
                            r.Timestamp < endDate.AddDays(1)).Should().BeTrue();
            // Assert: Verify the returned collection contains the expected number of time records
            results.Should().HaveCount(3);
        }

        /// <summary>
        /// Tests that GetUnsyncedAsync returns only time records that have not been synced.
        /// </summary>
        [Fact]
        public async Task GetUnsyncedAsync_ReturnsOnlyUnsyncedTimeRecords()
        {
            // Arrange: Create test time records with different sync statuses
            var testRecords = new List<TimeRecord>
            {
                CreateTestTimeRecord(1, isSynced: false),
                CreateTestTimeRecord(2, isSynced: true),
                CreateTestTimeRecord(3, isSynced: false)
            };
            
            await _dbContext.TimeRecords.AddRangeAsync(testRecords);
            await _dbContext.SaveChangesAsync();

            // Act: Call _repository.GetUnsyncedAsync
            var results = await _repository.GetUnsyncedAsync();

            // Assert: Verify the returned collection is not null
            results.Should().NotBeNull();
            // Assert: Verify the returned collection only contains time records where IsSynced is false
            results.All(r => !r.IsSynced).Should().BeTrue();
            // Assert: Verify the returned collection contains the expected number of unsynced time records
            results.Should().HaveCount(2);
        }

        /// <summary>
        /// Tests that GetLatestByUserIdAsync returns the most recent time record for the specified user.
        /// </summary>
        [Fact]
        public async Task GetLatestByUserIdAsync_ReturnsLatestTimeRecordForSpecificUser()
        {
            // Arrange: Create multiple test time records for a specific user with different timestamps
            var userId = "test-user";
            var now = DateTime.UtcNow;
            
            var testRecords = new List<TimeRecord>
            {
                CreateTestTimeRecord(1, userId, timestamp: now.AddHours(-2)),
                CreateTestTimeRecord(2, userId, timestamp: now), // Latest
                CreateTestTimeRecord(3, userId, timestamp: now.AddHours(-1))
            };
            
            await _dbContext.TimeRecords.AddRangeAsync(testRecords);
            await _dbContext.SaveChangesAsync();

            // Act: Call _repository.GetLatestByUserIdAsync with the user ID
            var result = await _repository.GetLatestByUserIdAsync(userId);

            // Assert: Verify the returned time record is not null
            result.Should().NotBeNull();
            // Assert: Verify the returned time record is the one with the most recent timestamp
            result.Id.Should().Be(2);
        }

        /// <summary>
        /// Tests that GetCurrentStatusAsync returns 'in' when the latest time record is a clock-in event.
        /// </summary>
        [Fact]
        public async Task GetCurrentStatusAsync_WithClockInAsLatestRecord_ReturnsIn()
        {
            // Arrange: Create a user ID for testing
            var userId = "test-user";
            // Arrange: Create a test time record with Type='in' and a recent timestamp
            var now = DateTime.UtcNow;
            
            var testRecords = new List<TimeRecord>
            {
                CreateTestTimeRecord(1, userId, type: "out", timestamp: now.AddHours(-2)),
                CreateTestTimeRecord(2, userId, type: "in", timestamp: now) // Latest is clock-in
            };
            
            await _dbContext.TimeRecords.AddRangeAsync(testRecords);
            await _dbContext.SaveChangesAsync();

            // Act: Call _repository.GetCurrentStatusAsync with the user ID
            var result = await _repository.GetCurrentStatusAsync(userId);

            // Assert: Verify the returned status is 'in'
            result.Should().Be("in");
        }

        /// <summary>
        /// Tests that GetCurrentStatusAsync returns 'out' when the latest time record is a clock-out event.
        /// </summary>
        [Fact]
        public async Task GetCurrentStatusAsync_WithClockOutAsLatestRecord_ReturnsOut()
        {
            // Arrange: Create a user ID for testing
            var userId = "test-user";
            // Arrange: Create a test time record with Type='out' and a recent timestamp
            var now = DateTime.UtcNow;
            
            var testRecords = new List<TimeRecord>
            {
                CreateTestTimeRecord(1, userId, type: "in", timestamp: now.AddHours(-2)),
                CreateTestTimeRecord(2, userId, type: "out", timestamp: now) // Latest is clock-out
            };
            
            await _dbContext.TimeRecords.AddRangeAsync(testRecords);
            await _dbContext.SaveChangesAsync();

            // Act: Call _repository.GetCurrentStatusAsync with the user ID
            var result = await _repository.GetCurrentStatusAsync(userId);

            // Assert: Verify the returned status is 'out'
            result.Should().Be("out");
        }

        /// <summary>
        /// Tests that GetCurrentStatusAsync returns 'out' when there are no time records for the user.
        /// </summary>
        [Fact]
        public async Task GetCurrentStatusAsync_WithNoRecords_ReturnsOut()
        {
            // Arrange: Create a user ID for testing with no time records
            var userId = "new-user";

            // Act: Call _repository.GetCurrentStatusAsync with the user ID
            var result = await _repository.GetCurrentStatusAsync(userId);

            // Assert: Verify the returned status is 'out'
            result.Should().Be("out");
        }

        /// <summary>
        /// Tests that AddAsync correctly adds a time record to the database.
        /// </summary>
        [Fact]
        public async Task AddAsync_AddsTimeRecordToDatabase()
        {
            // Arrange: Create a new test time record
            var newRecord = CreateTestTimeRecord(0); // ID will be assigned by database

            // Act: Call _repository.AddAsync with the test time record
            var result = await _repository.AddAsync(newRecord);

            // Assert: Verify the returned time record is not null
            result.Should().NotBeNull();
            // Assert: Verify the returned time record has a non-zero ID
            result.Id.Should().BeGreaterThan(0);
            
            // Assert: Verify the time record exists in the database context
            var dbRecord = await _dbContext.TimeRecords.FindAsync(result.Id);
            dbRecord.Should().NotBeNull();
        }

        /// <summary>
        /// Tests that UpdateAsync correctly updates an existing time record in the database.
        /// </summary>
        [Fact]
        public async Task UpdateAsync_UpdatesExistingTimeRecord()
        {
            // Arrange: Create a test time record
            var testRecord = CreateTestTimeRecord(1);
            await _dbContext.TimeRecords.AddAsync(testRecord);
            await _dbContext.SaveChangesAsync();

            // Arrange: Modify the time record properties
            testRecord.Type = "updated-type";
            testRecord.Latitude = 50.0;
            testRecord.Longitude = 60.0;

            // Act: Call _repository.UpdateAsync with the modified time record
            await _repository.UpdateAsync(testRecord);

            // Act: Retrieve the updated time record from the database
            var updatedRecord = await _dbContext.TimeRecords.FindAsync(testRecord.Id);
            
            // Assert: Verify the retrieved time record has the updated properties
            updatedRecord.Should().NotBeNull();
            updatedRecord.Type.Should().Be("updated-type");
            updatedRecord.Latitude.Should().Be(50.0);
            updatedRecord.Longitude.Should().Be(60.0);
        }

        /// <summary>
        /// Tests that DeleteAsync correctly removes a time record from the database.
        /// </summary>
        [Fact]
        public async Task DeleteAsync_RemovesTimeRecordFromDatabase()
        {
            // Arrange: Create a test time record
            var testRecord = CreateTestTimeRecord(1);
            await _dbContext.TimeRecords.AddAsync(testRecord);
            await _dbContext.SaveChangesAsync();

            // Act: Call _repository.DeleteAsync with the time record's ID
            await _repository.DeleteAsync(testRecord.Id);

            // Act: Attempt to retrieve the deleted time record
            var deletedRecord = await _dbContext.TimeRecords.FindAsync(testRecord.Id);
            
            // Assert: Verify the retrieved time record is null
            deletedRecord.Should().BeNull();
        }

        /// <summary>
        /// Tests that UpdateSyncStatusAsync correctly updates a time record's sync status.
        /// </summary>
        [Fact]
        public async Task UpdateSyncStatusAsync_UpdatesTimeRecordSyncStatus()
        {
            // Arrange: Create a test time record with IsSynced=false
            var testRecord = CreateTestTimeRecord(1, isSynced: false);
            await _dbContext.TimeRecords.AddAsync(testRecord);
            await _dbContext.SaveChangesAsync();

            // Act: Call _repository.UpdateSyncStatusAsync with the time record's ID and true
            await _repository.UpdateSyncStatusAsync(testRecord.Id, true);

            // Act: Retrieve the updated time record
            var updatedRecord = await _dbContext.TimeRecords.FindAsync(testRecord.Id);
            
            // Assert: Verify the time record's IsSynced property is true
            updatedRecord.Should().NotBeNull();
            updatedRecord.IsSynced.Should().BeTrue();
        }

        /// <summary>
        /// Tests that DeleteOlderThanAsync correctly removes time records older than the specified date.
        /// </summary>
        [Fact]
        public async Task DeleteOlderThanAsync_RemovesTimeRecordsOlderThanSpecifiedDate()
        {
            // Arrange: Create a reference date
            var referenceDate = new DateTime(2023, 5, 15);
            
            var testRecords = new List<TimeRecord>
            {
                // Records older than the reference date
                CreateTestTimeRecord(1, timestamp: referenceDate.AddDays(-10)),
                CreateTestTimeRecord(2, timestamp: referenceDate.AddDays(-5)),
                
                // Records newer than the reference date
                CreateTestTimeRecord(3, timestamp: referenceDate.AddDays(1)),
                CreateTestTimeRecord(4, timestamp: referenceDate.AddDays(5))
            };
            
            await _dbContext.TimeRecords.AddRangeAsync(testRecords);
            await _dbContext.SaveChangesAsync();

            // Act: Call _repository.DeleteOlderThanAsync with the reference date and onlySynced=false
            var deletedCount = await _repository.DeleteOlderThanAsync(referenceDate, false);

            // Assert: Verify the returned count matches the number of deleted records
            deletedCount.Should().Be(2); // Two records were older than the reference date
            
            // Assert: Verify only time records older than the reference date were deleted
            var remainingRecords = await _dbContext.TimeRecords.ToListAsync();
            remainingRecords.Should().HaveCount(2);
            remainingRecords.All(r => r.Timestamp >= referenceDate).Should().BeTrue();
            
            // Assert: Verify time records newer than the reference date still exist
            remainingRecords.Any(r => r.Id == 3).Should().BeTrue();
            remainingRecords.Any(r => r.Id == 4).Should().BeTrue();
        }

        /// <summary>
        /// Tests that DeleteOlderThanAsync with onlySynced=true correctly removes only synced time records older than the specified date.
        /// </summary>
        [Fact]
        public async Task DeleteOlderThanAsync_WithOnlySyncedTrue_RemovesOnlySyncedTimeRecordsOlderThanSpecifiedDate()
        {
            // Arrange: Create a reference date
            var referenceDate = new DateTime(2023, 5, 15);
            
            var testRecords = new List<TimeRecord>
            {
                // Synced records older than the reference date
                CreateTestTimeRecord(1, timestamp: referenceDate.AddDays(-10), isSynced: true),
                CreateTestTimeRecord(2, timestamp: referenceDate.AddDays(-5), isSynced: true),
                
                // Unsynced records older than the reference date
                CreateTestTimeRecord(3, timestamp: referenceDate.AddDays(-8), isSynced: false),
                
                // Records newer than the reference date
                CreateTestTimeRecord(4, timestamp: referenceDate.AddDays(1), isSynced: true),
                CreateTestTimeRecord(5, timestamp: referenceDate.AddDays(5), isSynced: false)
            };
            
            await _dbContext.TimeRecords.AddRangeAsync(testRecords);
            await _dbContext.SaveChangesAsync();

            // Act: Call _repository.DeleteOlderThanAsync with the reference date and onlySynced=true
            var deletedCount = await _repository.DeleteOlderThanAsync(referenceDate, true);

            // Assert: Verify the returned count matches the number of deleted records
            deletedCount.Should().Be(2); // Two synced records were older than the reference date
            
            // Assert: Verify only synced time records older than the reference date were deleted
            var remainingRecords = await _dbContext.TimeRecords.ToListAsync();
            remainingRecords.Should().HaveCount(3);
            
            // Assert: Verify unsynced time records older than the reference date still exist
            remainingRecords.Any(r => r.Id == 3).Should().BeTrue();
            
            // Assert: Verify time records newer than the reference date still exist
            remainingRecords.Any(r => r.Id == 4).Should().BeTrue();
            remainingRecords.Any(r => r.Id == 5).Should().BeTrue();
        }

        /// <summary>
        /// Tests that CountAsync returns the correct number of time records in the database.
        /// </summary>
        [Fact]
        public async Task CountAsync_ReturnsCorrectCount()
        {
            // Arrange: Create multiple test time records
            var testRecords = new List<TimeRecord>
            {
                CreateTestTimeRecord(1),
                CreateTestTimeRecord(2),
                CreateTestTimeRecord(3)
            };
            
            await _dbContext.TimeRecords.AddRangeAsync(testRecords);
            await _dbContext.SaveChangesAsync();

            // Act: Call _repository.CountAsync
            var count = await _repository.CountAsync();

            // Assert: Verify the returned count matches the number of added time records
            count.Should().Be(3);
        }

        /// <summary>
        /// Tests that CountByUserIdAsync returns the correct number of time records for the specified user.
        /// </summary>
        [Fact]
        public async Task CountByUserIdAsync_ReturnsCorrectCountForSpecificUser()
        {
            // Arrange: Create test time records for multiple users
            var userId1 = "user-1";
            var userId2 = "user-2";
            
            var testRecords = new List<TimeRecord>
            {
                CreateTestTimeRecord(1, userId1),
                CreateTestTimeRecord(2, userId1),
                CreateTestTimeRecord(3, userId2)
            };
            
            await _dbContext.TimeRecords.AddRangeAsync(testRecords);
            await _dbContext.SaveChangesAsync();

            // Act: Call _repository.CountByUserIdAsync with a specific user ID
            var count = await _repository.CountByUserIdAsync(userId1);

            // Assert: Verify the returned count matches the number of time records for the specified user
            count.Should().Be(2);
        }

        /// <summary>
        /// Helper method to create a test time record with specified or default values.
        /// </summary>
        /// <param name="id">The ID to assign to the time record</param>
        /// <param name="userId">The user ID to assign, or null for a random GUID</param>
        /// <param name="type">The type of time record (in/out)</param>
        /// <param name="timestamp">The timestamp of the record, or null for current time</param>
        /// <param name="latitude">The latitude coordinate</param>
        /// <param name="longitude">The longitude coordinate</param>
        /// <param name="isSynced">Whether the record is synced</param>
        /// <param name="remoteId">The remote ID to assign, or null for a random GUID</param>
        /// <returns>A new TimeRecord instance with the specified or default values</returns>
        private TimeRecord CreateTestTimeRecord(
            int id = 0, 
            string userId = null, 
            string type = "in", 
            DateTime? timestamp = null,
            double latitude = TestConstants.TestLatitude,
            double longitude = TestConstants.TestLongitude,
            bool isSynced = false,
            string remoteId = null)
        {
            return new TimeRecord
            {
                Id = id,
                UserId = userId ?? Guid.NewGuid().ToString(),
                Type = type,
                Timestamp = timestamp ?? DateTime.UtcNow,
                Latitude = latitude,
                Longitude = longitude,
                IsSynced = isSynced,
                RemoteId = remoteId ?? Guid.NewGuid().ToString()
            };
        }
    }
}