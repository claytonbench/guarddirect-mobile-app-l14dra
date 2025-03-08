using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.Infrastructure.Persistence;
using SecurityPatrol.Infrastructure.Persistence.Interceptors;
using SecurityPatrol.Infrastructure.Persistence.Repositories;
using SecurityPatrol.TestCommon.Constants;
using Xunit;

namespace SecurityPatrol.API.UnitTests.Repositories
{
    public class LocationRepositoryTests
    {
        private readonly string DatabaseName;

        public LocationRepositoryTests()
        {
            // Set DatabaseName to a unique name using Guid.NewGuid() to ensure test isolation
            DatabaseName = $"LocationRepositoryTests_{Guid.NewGuid()}";
        }

        private SecurityPatrolDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<SecurityPatrolDbContext>()
                .UseInMemoryDatabase(DatabaseName)
                .Options;

            // Create a mock AuditableEntityInterceptor
            var mockInterceptor = new Mock<AuditableEntityInterceptor>(
                Mock.Of<ICurrentUserService>(),
                Mock.Of<IDateTime>()).Object;

            return new SecurityPatrolDbContext(options, mockInterceptor);
        }

        private ILocationRecordRepository CreateRepository(SecurityPatrolDbContext context)
        {
            return new LocationRecordRepository(context);
        }

        private LocationRecord CreateLocationRecord(int id = 0, string userId = null, bool isSynced = false)
        {
            return new LocationRecord
            {
                Id = id,
                UserId = userId ?? TestConstants.TestUserId,
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude,
                Accuracy = TestConstants.TestAccuracy,
                Timestamp = DateTime.UtcNow,
                IsSynced = isSynced
            };
        }

        [Fact]
        public async Task Test_GetByIdAsync_ReturnsLocationRecord()
        {
            // Arrange
            var context = GetDbContext();
            var testRecord = CreateLocationRecord(id: 1);
            context.LocationRecords.Add(testRecord);
            await context.SaveChangesAsync();

            var repository = CreateRepository(context);

            // Act
            var result = await repository.GetByIdAsync(1);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(1);
            result.UserId.Should().Be(TestConstants.TestUserId);
            result.Latitude.Should().Be(TestConstants.TestLatitude);
            result.Longitude.Should().Be(TestConstants.TestLongitude);
            result.Accuracy.Should().Be(TestConstants.TestAccuracy);
        }

        [Fact]
        public async Task Test_GetByIdAsync_ReturnsNull_WhenRecordDoesNotExist()
        {
            // Arrange
            var context = GetDbContext();
            var repository = CreateRepository(context);

            // Act
            var result = await repository.GetByIdAsync(999);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task Test_GetByUserIdAsync_ReturnsLocationRecords()
        {
            // Arrange
            var context = GetDbContext();
            var testRecords = new List<LocationRecord>
            {
                CreateLocationRecord(id: 1),
                CreateLocationRecord(id: 2),
                CreateLocationRecord(id: 3)
            };
            context.LocationRecords.AddRange(testRecords);
            await context.SaveChangesAsync();

            var repository = CreateRepository(context);

            // Act
            var result = await repository.GetByUserIdAsync(TestConstants.TestUserId, 0);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            result.All(r => r.UserId == TestConstants.TestUserId).Should().BeTrue();
        }

        [Fact]
        public async Task Test_GetByUserIdAsync_WithLimit_ReturnsLimitedRecords()
        {
            // Arrange
            var context = GetDbContext();
            var testRecords = new List<LocationRecord>();
            for (int i = 0; i < 10; i++)
            {
                var record = CreateLocationRecord(id: i + 1);
                record.Timestamp = DateTime.UtcNow.AddMinutes(-i); // Older as i increases
                testRecords.Add(record);
            }
            context.LocationRecords.AddRange(testRecords);
            await context.SaveChangesAsync();

            var repository = CreateRepository(context);

            // Act
            var result = await repository.GetByUserIdAsync(TestConstants.TestUserId, 5);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(5);
            
            // Assert that the records are ordered by timestamp in descending order
            var orderedResults = result.OrderByDescending(r => r.Timestamp).ToList();
            result.Should().Equal(orderedResults);
        }

        [Fact]
        public async Task Test_GetByUserIdAndTimeRangeAsync_ReturnsFilteredRecords()
        {
            // Arrange
            var context = GetDbContext();
            var now = DateTime.UtcNow;
            var testRecords = new List<LocationRecord>
            {
                CreateLocationRecord(id: 1) { Timestamp = now.AddHours(-5) }, // Outside range
                CreateLocationRecord(id: 2) { Timestamp = now.AddHours(-3) }, // In range
                CreateLocationRecord(id: 3) { Timestamp = now.AddHours(-2) }, // In range
                CreateLocationRecord(id: 4) { Timestamp = now.AddHours(-1) }, // In range
                CreateLocationRecord(id: 5) { Timestamp = now.AddHours(1) }   // Outside range
            };
            context.LocationRecords.AddRange(testRecords);
            await context.SaveChangesAsync();

            var repository = CreateRepository(context);
            var startTime = now.AddHours(-4);
            var endTime = now;

            // Act
            var result = await repository.GetByUserIdAndTimeRangeAsync(TestConstants.TestUserId, startTime, endTime);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            result.All(r => r.Timestamp >= startTime && r.Timestamp <= endTime).Should().BeTrue();
            
            // Assert that the records are ordered by timestamp in ascending order
            var orderedResults = result.OrderBy(r => r.Timestamp).ToList();
            result.Should().Equal(orderedResults);
        }

        [Fact]
        public async Task Test_AddAsync_AddsLocationRecord()
        {
            // Arrange
            var context = GetDbContext();
            var repository = CreateRepository(context);
            var testRecord = CreateLocationRecord();

            // Act
            var result = await repository.AddAsync(testRecord);

            // Assert
            result.Should().BeGreaterThan(0);
            var dbRecord = await context.LocationRecords.FindAsync(result);
            dbRecord.Should().NotBeNull();
            dbRecord.UserId.Should().Be(TestConstants.TestUserId);
            dbRecord.Latitude.Should().Be(TestConstants.TestLatitude);
            dbRecord.Longitude.Should().Be(TestConstants.TestLongitude);
            dbRecord.Accuracy.Should().Be(TestConstants.TestAccuracy);
        }

        [Fact]
        public async Task Test_AddRangeAsync_AddsMultipleLocationRecords()
        {
            // Arrange
            var context = GetDbContext();
            var repository = CreateRepository(context);
            var testRecords = new List<LocationRecord>
            {
                CreateLocationRecord(),
                CreateLocationRecord(),
                CreateLocationRecord()
            };

            // Act
            var result = await repository.AddRangeAsync(testRecords);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            
            // Verify the records were added by retrieving them from the context
            var dbRecords = await context.LocationRecords.ToListAsync();
            dbRecords.Should().HaveCount(3);
        }

        [Fact]
        public async Task Test_UpdateAsync_UpdatesLocationRecord()
        {
            // Arrange
            var context = GetDbContext();
            var testRecord = CreateLocationRecord(id: 1);
            context.LocationRecords.Add(testRecord);
            await context.SaveChangesAsync();

            var repository = CreateRepository(context);
            
            // Modify the test record's properties
            testRecord.Latitude = 40.7128; // New York latitude
            testRecord.Longitude = -74.0060; // New York longitude
            testRecord.Accuracy = 5.0; // Improved accuracy
            testRecord.IsSynced = true;

            // Act
            var result = await repository.UpdateAsync(testRecord);

            // Assert
            result.Should().BeTrue();
            
            // Retrieve the updated record from the context
            var dbRecord = await context.LocationRecords.FindAsync(1);
            dbRecord.Should().NotBeNull();
            dbRecord.Latitude.Should().Be(40.7128);
            dbRecord.Longitude.Should().Be(-74.0060);
            dbRecord.Accuracy.Should().Be(5.0);
            dbRecord.IsSynced.Should().BeTrue();
        }

        [Fact]
        public async Task Test_UpdateSyncStatusAsync_UpdatesSyncStatus()
        {
            // Arrange
            var context = GetDbContext();
            var testRecords = new List<LocationRecord>
            {
                CreateLocationRecord(id: 1, isSynced: false),
                CreateLocationRecord(id: 2, isSynced: false),
                CreateLocationRecord(id: 3, isSynced: false)
            };
            context.LocationRecords.AddRange(testRecords);
            await context.SaveChangesAsync();

            var repository = CreateRepository(context);
            var ids = new List<int> { 1, 2, 3 };

            // Act
            var result = await repository.UpdateSyncStatusAsync(ids, true);

            // Assert
            result.Should().BeTrue();
            
            // Retrieve the updated records from the context
            var dbRecords = await context.LocationRecords.ToListAsync();
            dbRecords.All(r => r.IsSynced).Should().BeTrue();
        }

        [Fact]
        public async Task Test_DeleteAsync_DeletesLocationRecord()
        {
            // Arrange
            var context = GetDbContext();
            var testRecord = CreateLocationRecord(id: 1);
            context.LocationRecords.Add(testRecord);
            await context.SaveChangesAsync();

            var repository = CreateRepository(context);

            // Act
            var result = await repository.DeleteAsync(1);

            // Assert
            result.Should().BeTrue();
            
            // Try to retrieve the deleted record from the context
            var dbRecord = await context.LocationRecords.FindAsync(1);
            dbRecord.Should().BeNull();
        }

        [Fact]
        public async Task Test_DeleteAsync_ReturnsFalse_WhenRecordDoesNotExist()
        {
            // Arrange
            var context = GetDbContext();
            var repository = CreateRepository(context);

            // Act
            var result = await repository.DeleteAsync(999);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task Test_DeleteOlderThanAsync_DeletesOldRecords()
        {
            // Arrange
            var context = GetDbContext();
            var now = DateTime.UtcNow;
            var testRecords = new List<LocationRecord>
            {
                CreateLocationRecord(id: 1) { Timestamp = now.AddDays(-2) }, // Old
                CreateLocationRecord(id: 2) { Timestamp = now.AddDays(-1) }, // Old
                CreateLocationRecord(id: 3) { Timestamp = now.AddHours(-1) }, // Recent
                CreateLocationRecord(id: 4) { Timestamp = now } // Recent
            };
            context.LocationRecords.AddRange(testRecords);
            await context.SaveChangesAsync();

            var repository = CreateRepository(context);
            var cutoffDate = now.AddDays(-1).AddHours(12); // 1.5 days ago

            // Act
            var result = await repository.DeleteOlderThanAsync(cutoffDate, false);

            // Assert
            result.Should().Be(2); // Should delete 2 records
            
            // Verify that only records older than the cutoff date were deleted
            var remainingRecords = await context.LocationRecords.ToListAsync();
            remainingRecords.Should().HaveCount(2);
            remainingRecords.All(r => r.Timestamp >= cutoffDate).Should().BeTrue();
        }

        [Fact]
        public async Task Test_DeleteOlderThanAsync_WithOnlySynced_DeletesOnlySyncedOldRecords()
        {
            // Arrange
            var context = GetDbContext();
            var now = DateTime.UtcNow;
            var cutoffDate = now.AddDays(-1);
            var testRecords = new List<LocationRecord>
            {
                CreateLocationRecord(id: 1) { Timestamp = now.AddDays(-2), IsSynced = true }, // Old, synced
                CreateLocationRecord(id: 2) { Timestamp = now.AddDays(-2), IsSynced = false }, // Old, not synced
                CreateLocationRecord(id: 3) { Timestamp = now, IsSynced = true } // Recent, synced
            };
            context.LocationRecords.AddRange(testRecords);
            await context.SaveChangesAsync();

            var repository = CreateRepository(context);

            // Act
            var result = await repository.DeleteOlderThanAsync(cutoffDate, true);

            // Assert
            result.Should().Be(1); // Should delete only the old, synced record
            
            // Verify that only synced records older than the cutoff date were deleted
            var remainingRecords = await context.LocationRecords.ToListAsync();
            remainingRecords.Should().HaveCount(2);
            remainingRecords.Any(r => r.Id == 1).Should().BeFalse(); // Record 1 should be deleted
            remainingRecords.Any(r => r.Id == 2).Should().BeTrue(); // Record 2 should remain
            remainingRecords.Any(r => r.Id == 3).Should().BeTrue(); // Record 3 should remain
        }

        [Fact]
        public async Task Test_GetUnsyncedRecordsAsync_ReturnsUnsyncedRecords()
        {
            // Arrange
            var context = GetDbContext();
            var testRecords = new List<LocationRecord>
            {
                CreateLocationRecord(id: 1, isSynced: false) { Timestamp = DateTime.UtcNow.AddMinutes(-30) },
                CreateLocationRecord(id: 2, isSynced: true) { Timestamp = DateTime.UtcNow.AddMinutes(-20) },
                CreateLocationRecord(id: 3, isSynced: false) { Timestamp = DateTime.UtcNow.AddMinutes(-10) }
            };
            context.LocationRecords.AddRange(testRecords);
            await context.SaveChangesAsync();

            var repository = CreateRepository(context);

            // Act
            var result = await repository.GetUnsyncedRecordsAsync(0);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.All(r => !r.IsSynced).Should().BeTrue();
            
            // Assert that the records are ordered by timestamp in ascending order
            var orderedResults = result.OrderBy(r => r.Timestamp).ToList();
            result.Should().Equal(orderedResults);
        }

        [Fact]
        public async Task Test_GetUnsyncedRecordsAsync_WithLimit_ReturnsLimitedRecords()
        {
            // Arrange
            var context = GetDbContext();
            var testRecords = new List<LocationRecord>();
            for (int i = 0; i < 10; i++)
            {
                var record = CreateLocationRecord(id: i + 1, isSynced: false);
                record.Timestamp = DateTime.UtcNow.AddMinutes(-i); // Older as i increases
                testRecords.Add(record);
            }
            context.LocationRecords.AddRange(testRecords);
            await context.SaveChangesAsync();

            var repository = CreateRepository(context);

            // Act
            var result = await repository.GetUnsyncedRecordsAsync(5);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(5);
            result.All(r => !r.IsSynced).Should().BeTrue();
        }

        [Fact]
        public async Task Test_GetLatestLocationAsync_ReturnsLatestRecord()
        {
            // Arrange
            var context = GetDbContext();
            var now = DateTime.UtcNow;
            var testRecords = new List<LocationRecord>
            {
                CreateLocationRecord(id: 1) { Timestamp = now.AddMinutes(-30) },
                CreateLocationRecord(id: 2) { Timestamp = now.AddMinutes(-15) }, // Most recent
                CreateLocationRecord(id: 3) { Timestamp = now.AddMinutes(-45) }
            };
            context.LocationRecords.AddRange(testRecords);
            await context.SaveChangesAsync();

            var repository = CreateRepository(context);

            // Act
            var result = await repository.GetLatestLocationAsync(TestConstants.TestUserId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(2); // The record with the most recent timestamp
        }

        [Fact]
        public async Task Test_GetLatestLocationAsync_ReturnsNull_WhenNoRecordsExist()
        {
            // Arrange
            var context = GetDbContext();
            var repository = CreateRepository(context);

            // Act
            var result = await repository.GetLatestLocationAsync("non-existent-user");

            // Assert
            result.Should().BeNull();
        }
    }
}