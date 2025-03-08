using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using SecurityPatrol.API.UnitTests.Setup;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Core.Models;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.Infrastructure.Persistence;
using SecurityPatrol.Infrastructure.Persistence.Repositories;
using SecurityPatrol.Infrastructure.Persistence.Interceptors;
using Xunit;
using FluentAssertions;

namespace SecurityPatrol.API.UnitTests.Repositories
{
    public class PhotoRepositoryTests : TestBase, IDisposable
    {
        private readonly SecurityPatrolDbContext _dbContext;
        private readonly PhotoRepository _repository;

        public PhotoRepositoryTests()
        {
            // Set up an in-memory database for testing
            var options = new DbContextOptionsBuilder<SecurityPatrolDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            // Create a mock for AuditableEntityInterceptor
            var mockUserService = new Mock<ICurrentUserService>();
            mockUserService.Setup(x => x.GetUserId()).Returns("test-user");
            
            var mockDateTime = new Mock<IDateTime>();
            mockDateTime.Setup(x => x.Now).Returns(DateTime.UtcNow);
            
            var auditableEntityInterceptor = new AuditableEntityInterceptor(
                mockUserService.Object, 
                mockDateTime.Object);

            _dbContext = new SecurityPatrolDbContext(options, auditableEntityInterceptor);
            _repository = new PhotoRepository(_dbContext);

            // Seed the database with test data
            SeedDatabase();
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }

        private void SeedDatabase()
        {
            // Create a test user
            var user = new User
            {
                Id = "test-user-id",
                PhoneNumber = "+15555555555",
                IsActive = true,
                LastAuthenticated = DateTime.UtcNow
            };

            _dbContext.Users.Add(user);

            // Create test photos with varying timestamps, locations
            var photos = new List<Photo>
            {
                new Photo
                {
                    Id = 1,
                    UserId = user.Id,
                    Timestamp = DateTime.UtcNow.AddDays(-10),
                    Latitude = 34.0522,
                    Longitude = -118.2437,
                    FilePath = "/photos/test1.jpg",
                    User = user
                },
                new Photo
                {
                    Id = 2,
                    UserId = user.Id,
                    Timestamp = DateTime.UtcNow.AddDays(-5),
                    Latitude = 34.0530,
                    Longitude = -118.2430,
                    FilePath = "/photos/test2.jpg",
                    User = user
                },
                new Photo
                {
                    Id = 3,
                    UserId = user.Id,
                    Timestamp = DateTime.UtcNow.AddDays(-1),
                    Latitude = 34.0550,
                    Longitude = -118.2420,
                    FilePath = "/photos/test3.jpg",
                    User = user
                }
            };

            _dbContext.Photos.AddRange(photos);
            _dbContext.SaveChanges();
        }

        [Fact]
        public async Task GetByIdAsync_WithValidId_ReturnsPhoto()
        {
            // Arrange
            int validId = 1;

            // Act
            var photo = await _repository.GetByIdAsync(validId);

            // Assert
            photo.Should().NotBeNull();
            photo.Id.Should().Be(validId);
            photo.User.Should().NotBeNull();
        }

        [Fact]
        public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            int invalidId = 999;

            // Act
            var photo = await _repository.GetByIdAsync(invalidId);

            // Assert
            photo.Should().BeNull();
        }

        [Fact]
        public async Task GetByUserIdAsync_WithValidUserId_ReturnsUserPhotos()
        {
            // Arrange
            string userId = "test-user-id";

            // Act
            var photos = await _repository.GetByUserIdAsync(userId);

            // Assert
            photos.Should().NotBeNull();
            photos.Should().HaveCount(3);
            photos.All(p => p.UserId == userId).Should().BeTrue();
            photos.Should().BeInDescendingOrder(p => p.Timestamp);
        }

        [Fact]
        public async Task GetByUserIdAsync_WithInvalidUserId_ReturnsEmptyCollection()
        {
            // Arrange
            string invalidUserId = "invalid-user-id";

            // Act
            var photos = await _repository.GetByUserIdAsync(invalidUserId);

            // Assert
            photos.Should().NotBeNull();
            photos.Should().BeEmpty();
        }

        [Fact]
        public async Task GetPaginatedByUserIdAsync_WithValidParameters_ReturnsPaginatedPhotos()
        {
            // Arrange
            string userId = "test-user-id";
            int pageNumber = 1;
            int pageSize = 2;

            // Act
            var paginatedPhotos = await _repository.GetPaginatedByUserIdAsync(userId, pageNumber, pageSize);

            // Assert
            paginatedPhotos.Should().NotBeNull();
            paginatedPhotos.Items.Should().HaveCount(2);
            paginatedPhotos.TotalCount.Should().Be(3);
            paginatedPhotos.TotalPages.Should().Be(2);
            paginatedPhotos.PageNumber.Should().Be(pageNumber);
            paginatedPhotos.Items.All(p => p.UserId == userId).Should().BeTrue();
        }

        [Fact]
        public async Task GetByLocationAsync_WithValidParameters_ReturnsNearbyPhotos()
        {
            // Arrange
            double latitude = 34.0522;
            double longitude = -118.2437;
            double radiusInMeters = 1000; // 1 km radius

            // Act
            var photos = await _repository.GetByLocationAsync(latitude, longitude, radiusInMeters);

            // Assert
            photos.Should().NotBeNull();
            photos.Should().HaveCountGreaterThan(0);
            // The photo with ID 1 is at the exact coordinates, so it should be included
            photos.Should().Contain(p => p.Id == 1);
        }

        [Fact]
        public async Task GetByDateRangeAsync_WithValidDateRange_ReturnsPhotosInRange()
        {
            // Arrange
            var startDate = DateTime.UtcNow.AddDays(-6);
            var endDate = DateTime.UtcNow;

            // Act
            var photos = await _repository.GetByDateRangeAsync(startDate, endDate);

            // Assert
            photos.Should().NotBeNull();
            photos.Should().HaveCount(2); // Expecting photos with IDs 2 and 3 (5 days ago and 1 day ago)
            photos.Should().Contain(p => p.Id == 2);
            photos.Should().Contain(p => p.Id == 3);
            photos.Should().BeInDescendingOrder(p => p.Timestamp);
        }

        [Fact]
        public async Task AddAsync_WithValidPhoto_AddsPhotoAndReturnsId()
        {
            // Arrange
            var photo = new Photo
            {
                UserId = "test-user-id",
                Timestamp = DateTime.UtcNow,
                Latitude = 34.0560,
                Longitude = -118.2410,
                FilePath = "/photos/test4.jpg"
            };

            // Act
            var result = await _repository.AddAsync(photo);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().BeGreaterThan(0);
            
            // Verify the photo exists in the database
            var savedPhoto = await _dbContext.Photos.FindAsync(result.Data);
            savedPhoto.Should().NotBeNull();
            savedPhoto.FilePath.Should().Be(photo.FilePath);
        }

        [Fact]
        public async Task AddAsync_WithNullPhoto_ReturnsFailureResult()
        {
            // Arrange
            Photo photo = null;

            // Act
            var result = await _repository.AddAsync(photo);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().NotBeNullOrEmpty();
            result.Message.Should().Contain("cannot be null");
        }

        [Fact]
        public async Task UpdateAsync_WithValidPhoto_UpdatesPhotoAndReturnsSuccess()
        {
            // Arrange
            var photo = await _dbContext.Photos.FindAsync(1);
            photo.FilePath = "/photos/updated.jpg";

            // Act
            var result = await _repository.UpdateAsync(photo);

            // Assert
            result.Succeeded.Should().BeTrue();
            
            // Verify the photo was updated in the database
            var updatedPhoto = await _dbContext.Photos.FindAsync(1);
            updatedPhoto.FilePath.Should().Be("/photos/updated.jpg");
        }

        [Fact]
        public async Task UpdateAsync_WithNonExistentPhoto_ReturnsFailureResult()
        {
            // Arrange
            var photo = new Photo
            {
                Id = 999, // Non-existent ID
                UserId = "test-user-id",
                Timestamp = DateTime.UtcNow,
                Latitude = 34.0550,
                Longitude = -118.2410,
                FilePath = "/photos/nonexistent.jpg"
            };

            // Act
            var result = await _repository.UpdateAsync(photo);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().NotBeNullOrEmpty();
            result.Message.Should().Contain("not found");
        }

        [Fact]
        public async Task DeleteAsync_WithValidId_DeletesPhotoAndReturnsSuccess()
        {
            // Arrange
            int photoId = 1;

            // Act
            var result = await _repository.DeleteAsync(photoId);

            // Assert
            result.Succeeded.Should().BeTrue();
            
            // Verify the photo no longer exists in the database
            var deletedPhoto = await _dbContext.Photos.FindAsync(photoId);
            deletedPhoto.Should().BeNull();
        }

        [Fact]
        public async Task DeleteAsync_WithInvalidId_ReturnsFailureResult()
        {
            // Arrange
            int invalidId = 999;

            // Act
            var result = await _repository.DeleteAsync(invalidId);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().NotBeNullOrEmpty();
            result.Message.Should().Contain("not found");
        }

        [Fact]
        public async Task ExistsAsync_WithExistingPhoto_ReturnsTrue()
        {
            // Arrange
            int photoId = 1;

            // Act
            var exists = await _repository.ExistsAsync(photoId);

            // Assert
            exists.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsAsync_WithNonExistentPhoto_ReturnsFalse()
        {
            // Arrange
            int invalidId = 999;

            // Act
            var exists = await _repository.ExistsAsync(invalidId);

            // Assert
            exists.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteOlderThanAsync_WithValidDate_DeletesOldPhotosAndReturnsCount()
        {
            // Arrange - Add some more photos with different dates
            var oldPhotos = new List<Photo>
            {
                new Photo
                {
                    Id = 4,
                    UserId = "test-user-id",
                    Timestamp = DateTime.UtcNow.AddDays(-40),
                    Latitude = 34.0522,
                    Longitude = -118.2437,
                    FilePath = "/photos/old1.jpg"
                },
                new Photo
                {
                    Id = 5,
                    UserId = "test-user-id",
                    Timestamp = DateTime.UtcNow.AddDays(-30),
                    Latitude = 34.0530,
                    Longitude = -118.2430,
                    FilePath = "/photos/old2.jpg"
                }
            };

            _dbContext.Photos.AddRange(oldPhotos);
            await _dbContext.SaveChangesAsync();

            var cutoffDate = DateTime.UtcNow.AddDays(-15); // Photos older than 15 days will be deleted

            // Act
            var deleteCount = await _repository.DeleteOlderThanAsync(cutoffDate);

            // Assert
            deleteCount.Should().Be(3); // Should delete the two old photos we just added plus the oldest from our original seed
            
            // Verify the old photos are deleted
            var remainingPhotos = await _dbContext.Photos.ToListAsync();
            remainingPhotos.Should().HaveCount(2); // Only two newer photos should remain
            remainingPhotos.Any(p => p.Id == 1 || p.Id == 4 || p.Id == 5).Should().BeFalse();
            remainingPhotos.Any(p => p.Id == 2 || p.Id == 3).Should().BeTrue();
        }
    }
}