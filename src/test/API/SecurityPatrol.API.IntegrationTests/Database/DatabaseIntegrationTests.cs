using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using FluentAssertions;
using SecurityPatrol.API.IntegrationTests.Setup;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.Infrastructure.Persistence;
using SecurityPatrol.TestCommon.Data;
using SecurityPatrol.TestCommon.Constants;

namespace SecurityPatrol.API.IntegrationTests.Database
{
    /// <summary>
    /// Integration tests for database operations in the Security Patrol API,
    /// verifying that the database context and repositories work correctly with the in-memory test database.
    /// </summary>
    public class DatabaseIntegrationTests : IntegrationTestBase
    {
        /// <summary>
        /// Initializes a new instance of the DatabaseIntegrationTests class with the test factory.
        /// </summary>
        /// <param name="factory">The factory that creates a test server with in-memory database.</param>
        public DatabaseIntegrationTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task DbContext_ShouldBeConfiguredCorrectly()
        {
            // Arrange
            var dbContext = GetService<SecurityPatrolDbContext>();

            // Assert
            dbContext.Should().NotBeNull();
            dbContext.Users.Should().NotBeNull();
            dbContext.TimeRecords.Should().NotBeNull();
            dbContext.LocationRecords.Should().NotBeNull();
            dbContext.PatrolLocations.Should().NotBeNull();
            dbContext.Checkpoints.Should().NotBeNull();
            dbContext.CheckpointVerifications.Should().NotBeNull();
            dbContext.Photos.Should().NotBeNull();
            dbContext.Reports.Should().NotBeNull();

            // Verify we're using the in-memory database with the expected name
            dbContext.Database.IsInMemory().Should().BeTrue();
            dbContext.Database.GetDbConnection().Database.Should().Be(DatabaseName);
        }

        [Fact]
        public async Task UserRepository_ShouldRetrieveUserById()
        {
            // Arrange
            var repository = GetService<IUserRepository>();
            var testUserId = TestConstants.TestUserId;

            // Act
            var user = await repository.GetByIdAsync(testUserId);

            // Assert
            user.Should().NotBeNull();
            user.Id.Should().Be(testUserId);
            user.PhoneNumber.Should().Be(TestConstants.TestPhoneNumber);
            user.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task UserRepository_ShouldRetrieveUserByPhoneNumber()
        {
            // Arrange
            var repository = GetService<IUserRepository>();
            var testPhoneNumber = TestConstants.TestPhoneNumber;

            // Act
            var user = await repository.GetByPhoneNumberAsync(testPhoneNumber);

            // Assert
            user.Should().NotBeNull();
            user.PhoneNumber.Should().Be(testPhoneNumber);
            user.Id.Should().Be(TestConstants.TestUserId);
        }

        [Fact]
        public async Task UserRepository_ShouldAddNewUser()
        {
            // Arrange
            var repository = GetService<IUserRepository>();
            var newUser = new User
            {
                Id = Guid.NewGuid().ToString(),
                PhoneNumber = "+15551234567",
                LastAuthenticated = DateTime.UtcNow,
                IsActive = true
            };

            // Act
            var result = await repository.AddAsync(newUser);
            var retrievedUser = await repository.GetByIdAsync(newUser.Id);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(newUser.Id);
            
            retrievedUser.Should().NotBeNull();
            retrievedUser.Id.Should().Be(newUser.Id);
            retrievedUser.PhoneNumber.Should().Be(newUser.PhoneNumber);
            retrievedUser.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task UserRepository_ShouldUpdateExistingUser()
        {
            // Arrange
            var repository = GetService<IUserRepository>();
            var testUserId = TestConstants.TestUserId;
            var user = await repository.GetByIdAsync(testUserId);
            user.Should().NotBeNull();
            
            // Modify the user
            var updatedPhoneNumber = "+15559876543";
            user.PhoneNumber = updatedPhoneNumber;

            // Act
            await repository.UpdateAsync(user);
            var updatedUser = await repository.GetByIdAsync(testUserId);

            // Assert
            updatedUser.Should().NotBeNull();
            updatedUser.PhoneNumber.Should().Be(updatedPhoneNumber);
        }

        [Fact]
        public async Task UserRepository_ShouldDeleteUser()
        {
            // Arrange
            var repository = GetService<IUserRepository>();
            var newUser = new User
            {
                Id = Guid.NewGuid().ToString(),
                PhoneNumber = "+15557654321",
                LastAuthenticated = DateTime.UtcNow,
                IsActive = true
            };
            await repository.AddAsync(newUser);

            // Act
            await repository.DeleteAsync(newUser.Id);
            var deletedUser = await repository.GetByIdAsync(newUser.Id);

            // Assert
            deletedUser.Should().BeNull();
        }

        [Fact]
        public async Task TimeRecordRepository_ShouldRetrieveRecordsForUser()
        {
            // Arrange
            var repository = GetService<ITimeRecordRepository>();
            var testUserId = TestConstants.TestUserId;

            // Act
            var records = await repository.GetByUserIdAsync(testUserId);

            // Assert
            records.Should().NotBeNull();
            records.Should().NotBeEmpty();
            records.Should().AllSatisfy(r => r.UserId.Should().Be(testUserId));
        }

        [Fact]
        public async Task PatrolLocationRepository_ShouldRetrieveLocationsWithCheckpoints()
        {
            // Arrange
            var repository = GetService<IPatrolLocationRepository>();

            // Act
            var locations = await repository.GetAllWithCheckpointsAsync();

            // Assert
            locations.Should().NotBeNull();
            locations.Should().NotBeEmpty();
            
            // At least one location should have checkpoints
            locations.Any(l => l.Checkpoints != null && l.Checkpoints.Any()).Should().BeTrue();
            
            // Verify checkpoint properties are loaded
            var locationWithCheckpoints = locations.First(l => l.Checkpoints != null && l.Checkpoints.Any());
            locationWithCheckpoints.Checkpoints.First().Name.Should().NotBeNullOrEmpty();
            locationWithCheckpoints.Checkpoints.First().Latitude.Should().NotBe(0);
            locationWithCheckpoints.Checkpoints.First().Longitude.Should().NotBe(0);
        }

        [Fact]
        public async Task CheckpointRepository_ShouldRetrieveCheckpointsForLocation()
        {
            // Arrange
            var repository = GetService<ICheckpointRepository>();
            var testLocationId = TestConstants.TestLocationId;

            // Act
            var checkpoints = await repository.GetByLocationIdAsync(testLocationId);

            // Assert
            checkpoints.Should().NotBeNull();
            checkpoints.Should().NotBeEmpty();
            checkpoints.Should().AllSatisfy(c => c.LocationId.Should().Be(testLocationId));
        }

        [Fact]
        public async Task CheckpointVerificationRepository_ShouldRetrieveVerificationsForUser()
        {
            // Arrange
            var repository = GetService<ICheckpointVerificationRepository>();
            var testUserId = TestConstants.TestUserId;

            // Act
            var verifications = await repository.GetByUserIdAsync(testUserId);

            // Assert
            verifications.Should().NotBeNull();
            verifications.Should().NotBeEmpty();
            verifications.Should().AllSatisfy(v => v.UserId.Should().Be(testUserId));
        }

        [Fact]
        public async Task EntityRelationships_ShouldBeConfiguredCorrectly()
        {
            // Arrange
            var dbContext = GetService<SecurityPatrolDbContext>();

            // Act - Load a test user with related entities
            var user = await dbContext.Users
                .Include(u => u.TimeRecords)
                .Include(u => u.LocationRecords)
                .Include(u => u.Photos)
                .Include(u => u.Reports)
                .Include(u => u.CheckpointVerifications)
                .FirstOrDefaultAsync(u => u.Id == TestConstants.TestUserId);

            // Load a test location with checkpoints
            var location = await dbContext.PatrolLocations
                .Include(l => l.Checkpoints)
                .FirstOrDefaultAsync(l => l.Id == TestConstants.TestLocationId);

            // Load a test checkpoint with verifications
            var checkpoint = await dbContext.Checkpoints
                .Include(c => c.Verifications)
                .FirstOrDefaultAsync(c => c.Id == TestConstants.TestCheckpointId);

            // Assert
            user.Should().NotBeNull();
            location.Should().NotBeNull();
            checkpoint.Should().NotBeNull();

            // User relationships
            user.TimeRecords.Should().NotBeNull();
            user.LocationRecords.Should().NotBeNull();
            user.Photos.Should().NotBeNull();
            user.Reports.Should().NotBeNull();
            user.CheckpointVerifications.Should().NotBeNull();

            // Location relationships
            location.Checkpoints.Should().NotBeNull();
            location.Checkpoints.Should().NotBeEmpty();

            // Checkpoint relationships
            checkpoint.Verifications.Should().NotBeNull();
        }

        [Fact]
        public async Task DatabaseSeeding_ShouldProvideTestData()
        {
            // Arrange
            var dbContext = GetService<SecurityPatrolDbContext>();

            // Act & Assert
            (await dbContext.Users.CountAsync()).Should().BeGreaterThan(0);
            (await dbContext.TimeRecords.CountAsync()).Should().BeGreaterThan(0);
            (await dbContext.LocationRecords.CountAsync()).Should().BeGreaterThan(0);
            (await dbContext.PatrolLocations.CountAsync()).Should().BeGreaterThan(0);
            (await dbContext.Checkpoints.CountAsync()).Should().BeGreaterThan(0);
            (await dbContext.CheckpointVerifications.CountAsync()).Should().BeGreaterThan(0);
            (await dbContext.Photos.CountAsync()).Should().BeGreaterThan(0);
            (await dbContext.Reports.CountAsync()).Should().BeGreaterThan(0);
        }
    }
}