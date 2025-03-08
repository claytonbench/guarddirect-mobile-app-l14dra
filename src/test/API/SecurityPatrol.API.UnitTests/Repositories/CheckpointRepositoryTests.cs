using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using Moq;
using Xunit;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Core.Models;
using SecurityPatrol.Infrastructure.Persistence;
using SecurityPatrol.Infrastructure.Persistence.Repositories;
using SecurityPatrol.TestCommon.Data;
using SecurityPatrol.Core.Interfaces;

namespace SecurityPatrol.API.UnitTests.Repositories
{
    /// <summary>
    /// Contains unit tests for the CheckpointRepository class to verify its data access operations for Checkpoint entities.
    /// </summary>
    public class CheckpointRepositoryTests : IDisposable
    {
        private readonly DbContextOptions<SecurityPatrolDbContext> _options;
        private readonly SecurityPatrolDbContext _context;
        private readonly CheckpointRepository _repository;

        /// <summary>
        /// Initializes a new instance of the CheckpointRepositoryTests class with an in-memory database context.
        /// </summary>
        public CheckpointRepositoryTests()
        {
            // Create a unique name for the in-memory database for each test run
            var dbName = $"SecurityPatrolTest_{Guid.NewGuid()}";
            
            // Create DbContext options for an in-memory database
            _options = new DbContextOptionsBuilder<SecurityPatrolDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            // Create mocks for AuditableEntityInterceptor dependencies
            var mockCurrentUserService = new Mock<ICurrentUserService>();
            mockCurrentUserService.Setup(m => m.GetUserId()).Returns("test-user");
            
            var mockDateTime = new Mock<IDateTime>();
            mockDateTime.Setup(m => m.Now).Returns(DateTime.UtcNow);
            
            // Create the interceptor with mocked dependencies
            var interceptor = new AuditableEntityInterceptor(
                mockCurrentUserService.Object, 
                mockDateTime.Object);
                
            // Create a new context using the options
            _context = new SecurityPatrolDbContext(_options, interceptor);

            // Create repository with the test context
            _repository = new CheckpointRepository(_context);

            // Seed the database with test data
            SeedDatabase();
        }

        /// <summary>
        /// Cleans up resources after tests are complete.
        /// </summary>
        public void Dispose()
        {
            _context.Dispose();
            _context = null;
        }

        /// <summary>
        /// Seeds the in-memory database with test checkpoint data.
        /// </summary>
        private void SeedDatabase()
        {
            // Create test checkpoints with various locations for testing
            var checkpoints = new List<Checkpoint>
            {
                new Checkpoint
                {
                    Id = 101,
                    LocationId = 1,
                    Name = "Main Entrance Checkpoint",
                    Latitude = 34.0522,
                    Longitude = -118.2437,
                    LastUpdated = DateTime.UtcNow
                },
                new Checkpoint
                {
                    Id = 102,
                    LocationId = 1,
                    Name = "Lobby Checkpoint",
                    Latitude = 34.0523,
                    Longitude = -118.2438,
                    LastUpdated = DateTime.UtcNow
                },
                new Checkpoint
                {
                    Id = 103,
                    LocationId = 1,
                    Name = "Stairwell Checkpoint",
                    Latitude = 34.0524,
                    Longitude = -118.2439,
                    LastUpdated = DateTime.UtcNow
                },
                new Checkpoint
                {
                    Id = 104,
                    LocationId = 2,
                    Name = "Warehouse Entrance",
                    Latitude = 34.0525,
                    Longitude = -118.2440,
                    LastUpdated = DateTime.UtcNow
                },
                new Checkpoint
                {
                    Id = 105,
                    LocationId = 2,
                    Name = "Loading Dock",
                    Latitude = 34.0526,
                    Longitude = -118.2441,
                    LastUpdated = DateTime.UtcNow
                }
            };

            _context.Checkpoints.AddRange(checkpoints);
            _context.SaveChanges();
        }

        /// <summary>
        /// Tests that GetByIdAsync returns the correct checkpoint when given an existing ID.
        /// </summary>
        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsCheckpoint()
        {
            // Arrange
            var testId = 101;
            
            // Act
            var result = await _repository.GetByIdAsync(testId);
            
            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(testId);
            result.Name.Should().Be("Main Entrance Checkpoint");
        }

        /// <summary>
        /// Tests that GetByIdAsync returns null when given a non-existing ID.
        /// </summary>
        [Fact]
        public async Task GetByIdAsync_NonExistingId_ReturnsNull()
        {
            // Arrange
            var nonExistingId = 999;
            
            // Act
            var result = await _repository.GetByIdAsync(nonExistingId);
            
            // Assert
            result.Should().BeNull();
        }

        /// <summary>
        /// Tests that GetByLocationIdAsync returns the correct checkpoints for an existing location ID.
        /// </summary>
        [Fact]
        public async Task GetByLocationIdAsync_ExistingLocationId_ReturnsCheckpoints()
        {
            // Arrange
            var locationId = 1;
            
            // Act
            var result = await _repository.GetByLocationIdAsync(locationId);
            
            // Assert
            result.Should().NotBeNull();
            result.Should().NotBeEmpty();
            result.Should().HaveCount(3);
            result.All(c => c.LocationId == locationId).Should().BeTrue();
        }

        /// <summary>
        /// Tests that GetByLocationIdAsync returns an empty collection for a non-existing location ID.
        /// </summary>
        [Fact]
        public async Task GetByLocationIdAsync_NonExistingLocationId_ReturnsEmptyCollection()
        {
            // Arrange
            var nonExistingLocationId = 999;
            
            // Act
            var result = await _repository.GetByLocationIdAsync(nonExistingLocationId);
            
            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        /// <summary>
        /// Tests that GetAllAsync returns all checkpoints in the database.
        /// </summary>
        [Fact]
        public async Task GetAllAsync_ReturnsAllCheckpoints()
        {
            // Act
            var result = await _repository.GetAllAsync();
            
            // Assert
            result.Should().NotBeNull();
            result.Should().NotBeEmpty();
            result.Should().HaveCount(5); // We added 5 checkpoints in SeedDatabase
        }

        /// <summary>
        /// Tests that AddAsync returns a success result when adding a valid checkpoint.
        /// </summary>
        [Fact]
        public async Task AddAsync_ValidCheckpoint_ReturnsSuccessResult()
        {
            // Arrange
            var newCheckpoint = new Checkpoint
            {
                LocationId = 1,
                Name = "New Test Checkpoint",
                Latitude = 34.0530,
                Longitude = -118.2445,
                LastUpdated = DateTime.UtcNow
            };
            
            // Act
            var result = await _repository.AddAsync(newCheckpoint);
            
            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().BeGreaterThan(0);
            
            // Verify the checkpoint was actually added
            var addedCheckpoint = await _context.Checkpoints.FindAsync(newCheckpoint.Id);
            addedCheckpoint.Should().NotBeNull();
            addedCheckpoint.Name.Should().Be("New Test Checkpoint");
        }

        /// <summary>
        /// Tests that AddAsync returns a failure result when adding an invalid checkpoint.
        /// </summary>
        [Fact]
        public async Task AddAsync_InvalidCheckpoint_ReturnsFailureResult()
        {
            // Arrange
            var invalidCheckpoint = new Checkpoint
            {
                LocationId = 1,
                Name = null, // Invalid: name is required
                Latitude = 34.0522,
                Longitude = -118.2437,
                LastUpdated = DateTime.UtcNow
            };
            
            // Act
            var result = await _repository.AddAsync(invalidCheckpoint);
            
            // Assert
            result.Succeeded.Should().BeFalse();
            result.Errors.Should().NotBeEmpty();
        }

        /// <summary>
        /// Tests that UpdateAsync returns a success result when updating an existing checkpoint.
        /// </summary>
        [Fact]
        public async Task UpdateAsync_ExistingCheckpoint_ReturnsSuccessResult()
        {
            // Arrange
            var existingCheckpoint = await _context.Checkpoints.FirstOrDefaultAsync();
            existingCheckpoint.Name = "Updated Checkpoint Name";
            
            // Act
            var result = await _repository.UpdateAsync(existingCheckpoint);
            
            // Assert
            result.Succeeded.Should().BeTrue();
            
            // Verify the checkpoint was actually updated
            var updatedCheckpoint = await _context.Checkpoints.FindAsync(existingCheckpoint.Id);
            updatedCheckpoint.Name.Should().Be("Updated Checkpoint Name");
        }

        /// <summary>
        /// Tests that UpdateAsync returns a failure result when updating a non-existing checkpoint.
        /// </summary>
        [Fact]
        public async Task UpdateAsync_NonExistingCheckpoint_ReturnsFailureResult()
        {
            // Arrange
            var nonExistingCheckpoint = new Checkpoint
            {
                Id = 999, // Non-existing ID
                LocationId = 1,
                Name = "Non-existing Checkpoint",
                Latitude = 34.0522,
                Longitude = -118.2437,
                LastUpdated = DateTime.UtcNow
            };
            
            // Act
            var result = await _repository.UpdateAsync(nonExistingCheckpoint);
            
            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("not found");
        }

        /// <summary>
        /// Tests that DeleteAsync returns a success result when deleting an existing checkpoint.
        /// </summary>
        [Fact]
        public async Task DeleteAsync_ExistingCheckpoint_ReturnsSuccessResult()
        {
            // Arrange
            var existingCheckpoint = await _context.Checkpoints.FirstOrDefaultAsync();
            var checkpointId = existingCheckpoint.Id;
            
            // Act
            var result = await _repository.DeleteAsync(checkpointId);
            
            // Assert
            result.Succeeded.Should().BeTrue();
            
            // Verify the checkpoint was actually deleted
            var deletedCheckpoint = await _context.Checkpoints.FindAsync(checkpointId);
            deletedCheckpoint.Should().BeNull();
        }

        /// <summary>
        /// Tests that DeleteAsync returns a failure result when deleting a non-existing checkpoint.
        /// </summary>
        [Fact]
        public async Task DeleteAsync_NonExistingCheckpoint_ReturnsFailureResult()
        {
            // Arrange
            var nonExistingId = 999;
            
            // Act
            var result = await _repository.DeleteAsync(nonExistingId);
            
            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("not found");
        }

        /// <summary>
        /// Tests that DeleteAsync returns a failure result when deleting a checkpoint with associated verifications.
        /// </summary>
        [Fact]
        public async Task DeleteAsync_CheckpointWithVerifications_ReturnsFailureResult()
        {
            // Arrange
            var existingCheckpoint = await _context.Checkpoints.FirstOrDefaultAsync();
            var checkpointId = existingCheckpoint.Id;
            
            // Add a verification record for the checkpoint
            _context.CheckpointVerifications.Add(new CheckpointVerification
            {
                CheckpointId = checkpointId,
                UserId = "test-user",
                Timestamp = DateTime.UtcNow,
                Latitude = existingCheckpoint.Latitude,
                Longitude = existingCheckpoint.Longitude
            });
            await _context.SaveChangesAsync();
            
            // Act
            var result = await _repository.DeleteAsync(checkpointId);
            
            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("verifications");
        }

        /// <summary>
        /// Tests that GetNearbyCheckpointsAsync returns checkpoints within the specified radius.
        /// </summary>
        [Fact]
        public async Task GetNearbyCheckpointsAsync_ReturnsCheckpointsWithinRadius()
        {
            // Arrange
            var testLatitude = 34.0522;
            var testLongitude = -118.2437;
            var radiusInMeters = 500.0; // 500 meters radius should include some of our test checkpoints
            
            // Act
            var result = await _repository.GetNearbyCheckpointsAsync(testLatitude, testLongitude, radiusInMeters);
            
            // Assert
            result.Should().NotBeNull();
            result.Should().NotBeEmpty();
            
            // Verify all returned checkpoints are within the specified radius
            foreach (var checkpoint in result)
            {
                var distance = CalculateDistance(
                    testLatitude, testLongitude,
                    checkpoint.Latitude, checkpoint.Longitude);
                distance.Should().BeLessThanOrEqualTo(radiusInMeters);
            }
        }

        /// <summary>
        /// Tests that GetNearbyCheckpointsAsync returns an empty collection when no checkpoints are within the specified radius.
        /// </summary>
        [Fact]
        public async Task GetNearbyCheckpointsAsync_NoCheckpointsWithinRadius_ReturnsEmptyCollection()
        {
            // Arrange
            var farLatitude = 40.7128; // New York latitude
            var farLongitude = -74.0060; // New York longitude
            var smallRadius = 10.0; // 10 meters radius
            
            // Act
            var result = await _repository.GetNearbyCheckpointsAsync(farLatitude, farLongitude, smallRadius);
            
            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        /// <summary>
        /// Tests that ExistsAsync returns true for an existing checkpoint ID.
        /// </summary>
        [Fact]
        public async Task ExistsAsync_ExistingCheckpoint_ReturnsTrue()
        {
            // Arrange
            var existingId = 101;
            
            // Act
            var result = await _repository.ExistsAsync(existingId);
            
            // Assert
            result.Should().BeTrue();
        }

        /// <summary>
        /// Tests that ExistsAsync returns false for a non-existing checkpoint ID.
        /// </summary>
        [Fact]
        public async Task ExistsAsync_NonExistingCheckpoint_ReturnsFalse()
        {
            // Arrange
            var nonExistingId = 999;
            
            // Act
            var result = await _repository.ExistsAsync(nonExistingId);
            
            // Assert
            result.Should().BeFalse();
        }

        // Helper method to calculate distance using Haversine formula
        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double EarthRadius = 6371000; // meters
            double lat1Rad = lat1 * Math.PI / 180;
            double lon1Rad = lon1 * Math.PI / 180;
            double lat2Rad = lat2 * Math.PI / 180;
            double lon2Rad = lon2 * Math.PI / 180;
            
            double dLat = lat2Rad - lat1Rad;
            double dLon = lon2Rad - lon1Rad;
            
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return EarthRadius * c;
        }
    }
}