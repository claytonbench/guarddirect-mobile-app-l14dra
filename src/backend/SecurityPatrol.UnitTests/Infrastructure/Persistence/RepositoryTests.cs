using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using FluentAssertions;
using Moq;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.Infrastructure.Persistence;
using SecurityPatrol.Infrastructure.Persistence.Repositories;
using SecurityPatrol.Infrastructure.Persistence.Interceptors;
using SecurityPatrol.UnitTests.Helpers;

namespace SecurityPatrol.UnitTests.Infrastructure.Persistence
{
    /// <summary>
    /// Contains unit tests for repository implementations using an in-memory database
    /// </summary>
    public class RepositoryTests : IDisposable
    {
        private SecurityPatrolDbContext _dbContext;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IDateTime> _mockDateTime;
        private readonly AuditableEntityInterceptor _auditableEntityInterceptor;

        /// <summary>
        /// Initializes a new instance of the RepositoryTests class with an in-memory database context
        /// </summary>
        public RepositoryTests()
        {
            // Set up mock dependencies for the database context
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockCurrentUserService.Setup(s => s.GetUserId()).Returns("test-user");

            _mockDateTime = new Mock<IDateTime>();
            _mockDateTime.Setup(d => d.Now).Returns(DateTime.UtcNow);

            // Initialize the auditable entity interceptor with mock dependencies
            _auditableEntityInterceptor = new AuditableEntityInterceptor(
                _mockCurrentUserService.Object,
                _mockDateTime.Object);

            // Configure the database context with the in-memory provider and interceptor
            var dbName = $"SecurityPatrolDb_{Guid.NewGuid()}";
            var options = new DbContextOptionsBuilder<SecurityPatrolDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            _dbContext = new SecurityPatrolDbContext(options, _auditableEntityInterceptor);
        }

        /// <summary>
        /// Disposes the database context after tests are complete
        /// </summary>
        public void Dispose()
        {
            if (_dbContext != null)
            {
                _dbContext.Dispose();
                _dbContext = null;
            }
        }

        [Fact]
        public async Task UserRepository_GetByIdAsync_ReturnsUser()
        {
            // Arrange
            await SeedTestData();
            var repository = new UserRepository(_dbContext);
            
            // Act
            var result = await repository.GetByIdAsync("user1");
            
            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be("user1");
            result.PhoneNumber.Should().Be("+15551234567");
        }

        [Fact]
        public async Task UserRepository_GetByIdAsync_ReturnsNull_WhenUserNotFound()
        {
            // Arrange
            await SeedTestData();
            var repository = new UserRepository(_dbContext);
            
            // Act
            var result = await repository.GetByIdAsync("non-existent-user");
            
            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task UserRepository_GetByPhoneNumberAsync_ReturnsUser()
        {
            // Arrange
            await SeedTestData();
            var repository = new UserRepository(_dbContext);
            
            // Act
            var result = await repository.GetByPhoneNumberAsync("+15551234567");
            
            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be("user1");
            result.PhoneNumber.Should().Be("+15551234567");
        }

        [Fact]
        public async Task UserRepository_AddAsync_AddsUserToDatabase()
        {
            // Arrange
            var repository = new UserRepository(_dbContext);
            var newUser = new User
            {
                Id = "newUser",
                PhoneNumber = "+15555555555",
                IsActive = true,
                LastAuthenticated = DateTime.UtcNow
            };
            
            // Act
            var result = await repository.AddAsync(newUser);
            
            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be("newUser");
            
            // Verify user was added to the database
            var userInDb = await _dbContext.Users.FindAsync("newUser");
            userInDb.Should().NotBeNull();
            userInDb.PhoneNumber.Should().Be("+15555555555");
        }

        [Fact]
        public async Task UserRepository_UpdateAsync_UpdatesUserInDatabase()
        {
            // Arrange
            await SeedTestData();
            var repository = new UserRepository(_dbContext);
            var user = await repository.GetByIdAsync("user1");
            user.PhoneNumber = "+15550000000";
            
            // Act
            await repository.UpdateAsync(user);
            
            // Assert
            var updatedUser = await _dbContext.Users.FindAsync("user1");
            updatedUser.Should().NotBeNull();
            updatedUser.PhoneNumber.Should().Be("+15550000000");
        }

        [Fact]
        public async Task UserRepository_DeleteAsync_RemovesUserFromDatabase()
        {
            // Arrange
            await SeedTestData();
            var repository = new UserRepository(_dbContext);
            
            // Act
            await repository.DeleteAsync("user1");
            
            // Assert
            var deletedUser = await _dbContext.Users.FindAsync("user1");
            deletedUser.Should().BeNull();
        }

        [Fact]
        public async Task TimeRecordRepository_GetByUserIdAsync_ReturnsRecordsForUser()
        {
            // Arrange
            await SeedTestData();
            var repository = new TimeRecordRepository(_dbContext);
            
            // Act
            var results = await repository.GetByUserIdAsync("user1");
            
            // Assert
            results.Should().NotBeNull();
            results.Should().HaveCountGreaterThanOrEqualTo(2);
            results.All(tr => tr.UserId == "user1").Should().BeTrue();
        }

        [Fact]
        public async Task LocationRecordRepository_AddRangeAsync_AddsBatchOfRecords()
        {
            // Arrange
            var repository = new LocationRecordRepository(_dbContext);
            var locationRecords = new List<LocationRecord>
            {
                new LocationRecord
                {
                    UserId = "user1",
                    Timestamp = DateTime.UtcNow.AddMinutes(-5),
                    Latitude = 40.7128,
                    Longitude = -74.0060,
                    Accuracy = 10.0
                },
                new LocationRecord
                {
                    UserId = "user1",
                    Timestamp = DateTime.UtcNow.AddMinutes(-4),
                    Latitude = 40.7129,
                    Longitude = -74.0061,
                    Accuracy = 9.5
                },
                new LocationRecord
                {
                    UserId = "user1",
                    Timestamp = DateTime.UtcNow.AddMinutes(-3),
                    Latitude = 40.7130,
                    Longitude = -74.0062,
                    Accuracy = 9.0
                }
            };
            
            // Act
            var ids = await repository.AddRangeAsync(locationRecords);
            
            // Assert
            ids.Should().NotBeNull();
            ids.Should().HaveCount(3);
            
            // Verify records were added to the database
            var recordsInDb = await _dbContext.LocationRecords.ToListAsync();
            recordsInDb.Should().HaveCount(3);
            recordsInDb.All(lr => lr.UserId == "user1").Should().BeTrue();
        }

        [Fact]
        public async Task LocationRecordRepository_GetUnsyncedRecordsAsync_ReturnsOnlyUnsyncedRecords()
        {
            // Arrange
            await SeedTestData();
            var repository = new LocationRecordRepository(_dbContext);
            
            // Act
            var results = await repository.GetUnsyncedAsync();
            
            // Assert
            results.Should().NotBeNull();
            results.All(lr => !lr.IsSynced).Should().BeTrue();
        }

        [Fact]
        public async Task LocationRecordRepository_UpdateSyncStatusAsync_UpdatesSyncStatusForMultipleRecords()
        {
            // Arrange
            await SeedTestData();
            var repository = new LocationRecordRepository(_dbContext);
            var recordIds = new List<int> { 3, 4 };
            
            // Act
            var result = await repository.UpdateSyncStatusAsync(recordIds, true);
            
            // Assert
            result.Should().BeTrue();
            
            // Verify sync status was updated
            var updatedRecords = await _dbContext.LocationRecords
                .Where(lr => recordIds.Contains(lr.Id))
                .ToListAsync();
                
            updatedRecords.Should().HaveCount(2);
            updatedRecords.All(lr => lr.IsSynced).Should().BeTrue();
        }

        [Fact]
        public async Task PatrolLocationRepository_GetAllAsync_ReturnsAllLocations()
        {
            // Arrange
            await SeedTestData();
            var repository = new PatrolLocationRepository(_dbContext);
            
            // Act
            var results = await repository.GetAllAsync();
            
            // Assert
            results.Should().NotBeNull();
            results.Should().HaveCountGreaterThanOrEqualTo(3);
        }

        [Fact]
        public async Task CheckpointRepository_GetByLocationIdAsync_ReturnsCheckpointsForLocation()
        {
            // Arrange
            await SeedTestData();
            var repository = new CheckpointRepository(_dbContext);
            
            // Act
            var results = await repository.GetByLocationIdAsync(1);
            
            // Assert
            results.Should().NotBeNull();
            results.Should().HaveCountGreaterThanOrEqualTo(3);
            results.All(c => c.LocationId == 1).Should().BeTrue();
        }

        [Fact]
        public async Task CheckpointVerificationRepository_GetByCheckpointIdAsync_ReturnsVerificationsForCheckpoint()
        {
            // Arrange
            await SeedTestData();
            var repository = new CheckpointVerificationRepository(_dbContext);
            
            // Act
            var results = await repository.GetByCheckpointIdAsync(1);
            
            // Assert
            results.Should().NotBeNull();
            results.Should().HaveCountGreaterThan(0);
            results.All(cv => cv.CheckpointId == 1).Should().BeTrue();
        }

        [Fact]
        public async Task PhotoRepository_GetByUserIdAsync_ReturnsPhotosForUser()
        {
            // Arrange
            await SeedTestData();
            var repository = new PhotoRepository(_dbContext);
            
            // Act
            var results = await repository.GetByUserIdAsync("user1");
            
            // Assert
            results.Should().NotBeNull();
            results.Should().HaveCountGreaterThan(0);
            results.All(p => p.UserId == "user1").Should().BeTrue();
        }

        [Fact]
        public async Task ReportRepository_GetUnsyncedAsync_ReturnsOnlyUnsyncedReports()
        {
            // Arrange
            await SeedTestData();
            var repository = new ReportRepository(_dbContext);
            
            // Act
            var results = await repository.GetUnsyncedAsync();
            
            // Assert
            results.Should().NotBeNull();
            results.All(r => !r.IsSynced).Should().BeTrue();
        }

        /// <summary>
        /// Seeds the in-memory database with test data for all entity types
        /// </summary>
        private async Task SeedTestData()
        {
            // Add test users
            var users = TestData.GetTestUsers();
            await _dbContext.Users.AddRangeAsync(users);

            // Add test time records
            var timeRecords = TestData.GetTestTimeRecords();
            await _dbContext.TimeRecords.AddRangeAsync(timeRecords);

            // Add test location records
            var locationRecords = TestData.GetTestLocationRecords();
            await _dbContext.LocationRecords.AddRangeAsync(locationRecords);

            // Add test patrol locations
            var patrolLocations = TestData.GetTestPatrolLocations();
            await _dbContext.PatrolLocations.AddRangeAsync(patrolLocations);

            // Add test checkpoints
            var checkpoints = TestData.GetTestCheckpoints();
            await _dbContext.Checkpoints.AddRangeAsync(checkpoints);

            // Add test checkpoint verifications
            var checkpointVerifications = TestData.GetTestCheckpointVerifications();
            await _dbContext.CheckpointVerifications.AddRangeAsync(checkpointVerifications);

            // Add test photos
            var photos = TestData.GetTestPhotos();
            await _dbContext.Photos.AddRangeAsync(photos);

            // Add test reports
            var reports = TestData.GetTestReports();
            await _dbContext.Reports.AddRangeAsync(reports);

            // Save changes to the in-memory database
            await _dbContext.SaveChangesAsync();
        }
    }
}