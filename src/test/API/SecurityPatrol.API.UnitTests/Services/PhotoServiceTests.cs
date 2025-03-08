using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SecurityPatrol.Application.Services;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.Core.Models;
using SecurityPatrol.TestCommon.Helpers;
using Xunit;

namespace SecurityPatrol.API.UnitTests.Services
{
    public class PhotoServiceTests
    {
        private readonly Mock<IPhotoRepository> _mockPhotoRepository;
        private readonly Mock<IStorageService> _mockStorageService;
        private readonly PhotoService _photoService;

        public PhotoServiceTests()
        {
            _mockPhotoRepository = new Mock<IPhotoRepository>();
            _mockStorageService = new Mock<IStorageService>();
            _photoService = new PhotoService(_mockPhotoRepository.Object, _mockStorageService.Object);
        }

        [Fact]
        public async Task UploadPhotoAsync_WithValidRequest_ReturnsSuccess()
        {
            // Arrange
            var request = new PhotoUploadRequest
            {
                UserId = "user123",
                Timestamp = DateTime.UtcNow,
                Latitude = 34.0522,
                Longitude = -118.2437
            };
            
            var photoStream = await TestImageGenerator.GenerateTestImageAsync();
            var contentType = "image/jpeg";
            var filePath = "uploads/photos/123456.jpg";
            var photoId = 1;
            
            _mockStorageService.Setup(s => s.StoreFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(Result.Success(filePath));
            
            _mockPhotoRepository.Setup(r => r.AddAsync(It.IsAny<Photo>()))
                .ReturnsAsync(Result.Success(photoId));
            
            // Act
            var result = await _photoService.UploadPhotoAsync(request, photoStream, contentType);
            
            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Id.Should().Be(photoId.ToString());
            result.Data.Status.Should().Be("success");
            
            _mockStorageService.Verify(s => s.StoreFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), contentType), Times.Once);
            _mockPhotoRepository.Verify(r => r.AddAsync(It.Is<Photo>(p => 
                p.UserId == request.UserId && 
                p.Timestamp == request.Timestamp && 
                p.Latitude == request.Latitude && 
                p.Longitude == request.Longitude && 
                p.FilePath == filePath)), Times.Once);
        }

        [Fact]
        public async Task UploadPhotoAsync_WithNullRequest_ReturnsFailure()
        {
            // Arrange
            PhotoUploadRequest request = null;
            var photoStream = await TestImageGenerator.GenerateTestImageAsync();
            var contentType = "image/jpeg";
            
            // Act
            var result = await _photoService.UploadPhotoAsync(request, photoStream, contentType);
            
            // Assert
            result.Succeeded.Should().BeFalse();
            _mockStorageService.Verify(s => s.StoreFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _mockPhotoRepository.Verify(r => r.AddAsync(It.IsAny<Photo>()), Times.Never);
        }

        [Fact]
        public async Task UploadPhotoAsync_WithNullStream_ReturnsFailure()
        {
            // Arrange
            var request = new PhotoUploadRequest
            {
                UserId = "user123",
                Timestamp = DateTime.UtcNow,
                Latitude = 34.0522,
                Longitude = -118.2437
            };
            
            Stream photoStream = null;
            var contentType = "image/jpeg";
            
            // Act
            var result = await _photoService.UploadPhotoAsync(request, photoStream, contentType);
            
            // Assert
            result.Succeeded.Should().BeFalse();
            _mockStorageService.Verify(s => s.StoreFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _mockPhotoRepository.Verify(r => r.AddAsync(It.IsAny<Photo>()), Times.Never);
        }

        [Fact]
        public async Task UploadPhotoAsync_WhenStorageServiceFails_ReturnsFailure()
        {
            // Arrange
            var request = new PhotoUploadRequest
            {
                UserId = "user123",
                Timestamp = DateTime.UtcNow,
                Latitude = 34.0522,
                Longitude = -118.2437
            };
            
            var photoStream = await TestImageGenerator.GenerateTestImageAsync();
            var contentType = "image/jpeg";
            
            _mockStorageService.Setup(s => s.StoreFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(Result.Failure<string>("Storage service error"));
            
            // Act
            var result = await _photoService.UploadPhotoAsync(request, photoStream, contentType);
            
            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("Failed to store photo");
            
            _mockStorageService.Verify(s => s.StoreFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), contentType), Times.Once);
            _mockPhotoRepository.Verify(r => r.AddAsync(It.IsAny<Photo>()), Times.Never);
        }

        [Fact]
        public async Task UploadPhotoAsync_WhenRepositoryFails_DeletesStoredFileAndReturnsFailure()
        {
            // Arrange
            var request = new PhotoUploadRequest
            {
                UserId = "user123",
                Timestamp = DateTime.UtcNow,
                Latitude = 34.0522,
                Longitude = -118.2437
            };
            
            var photoStream = await TestImageGenerator.GenerateTestImageAsync();
            var contentType = "image/jpeg";
            var filePath = "uploads/photos/123456.jpg";
            
            _mockStorageService.Setup(s => s.StoreFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(Result.Success(filePath));
            
            _mockPhotoRepository.Setup(r => r.AddAsync(It.IsAny<Photo>()))
                .ReturnsAsync(Result.Failure<int>("Repository error"));
            
            _mockStorageService.Setup(s => s.DeleteFileAsync(filePath))
                .ReturnsAsync(Result.Success());
            
            // Act
            var result = await _photoService.UploadPhotoAsync(request, photoStream, contentType);
            
            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("Failed to save photo metadata");
            
            _mockStorageService.Verify(s => s.StoreFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), contentType), Times.Once);
            _mockPhotoRepository.Verify(r => r.AddAsync(It.IsAny<Photo>()), Times.Once);
            _mockStorageService.Verify(s => s.DeleteFileAsync(filePath), Times.Once);
        }

        [Fact]
        public async Task GetPhotoAsync_WithValidId_ReturnsPhoto()
        {
            // Arrange
            var photoId = 1;
            var photo = new Photo
            {
                Id = photoId,
                UserId = "user123",
                Timestamp = DateTime.UtcNow,
                Latitude = 34.0522,
                Longitude = -118.2437,
                FilePath = "uploads/photos/123456.jpg"
            };
            
            _mockPhotoRepository.Setup(r => r.GetByIdAsync(photoId))
                .ReturnsAsync(photo);
            
            // Act
            var result = await _photoService.GetPhotoAsync(photoId);
            
            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().BeEquivalentTo(photo);
            _mockPhotoRepository.Verify(r => r.GetByIdAsync(photoId), Times.Once);
        }

        [Fact]
        public async Task GetPhotoAsync_WithInvalidId_ReturnsFailure()
        {
            // Arrange
            var photoId = 999;
            
            _mockPhotoRepository.Setup(r => r.GetByIdAsync(photoId))
                .ReturnsAsync((Photo)null);
            
            // Act
            var result = await _photoService.GetPhotoAsync(photoId);
            
            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Be("Photo not found");
            _mockPhotoRepository.Verify(r => r.GetByIdAsync(photoId), Times.Once);
        }

        [Fact]
        public async Task GetPhotoStreamAsync_WithValidId_ReturnsPhotoStream()
        {
            // Arrange
            var photoId = 1;
            var photo = new Photo
            {
                Id = photoId,
                UserId = "user123",
                Timestamp = DateTime.UtcNow,
                Latitude = 34.0522,
                Longitude = -118.2437,
                FilePath = "uploads/photos/123456.jpg"
            };
            
            var testStream = new MemoryStream();
            
            _mockPhotoRepository.Setup(r => r.GetByIdAsync(photoId))
                .ReturnsAsync(photo);
            
            _mockStorageService.Setup(s => s.FileExistsAsync(photo.FilePath))
                .ReturnsAsync(true);
            
            _mockStorageService.Setup(s => s.GetFileAsync(photo.FilePath))
                .ReturnsAsync(Result.Success<Stream>(testStream));
            
            // Act
            var result = await _photoService.GetPhotoStreamAsync(photoId);
            
            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().BeSameAs(testStream);
            
            _mockPhotoRepository.Verify(r => r.GetByIdAsync(photoId), Times.Once);
            _mockStorageService.Verify(s => s.FileExistsAsync(photo.FilePath), Times.Once);
            _mockStorageService.Verify(s => s.GetFileAsync(photo.FilePath), Times.Once);
        }

        [Fact]
        public async Task GetPhotoStreamAsync_WithInvalidId_ReturnsFailure()
        {
            // Arrange
            var photoId = 999;
            
            _mockPhotoRepository.Setup(r => r.GetByIdAsync(photoId))
                .ReturnsAsync((Photo)null);
            
            // Act
            var result = await _photoService.GetPhotoStreamAsync(photoId);
            
            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Be("Photo not found");
            
            _mockPhotoRepository.Verify(r => r.GetByIdAsync(photoId), Times.Once);
            _mockStorageService.Verify(s => s.FileExistsAsync(It.IsAny<string>()), Times.Never);
            _mockStorageService.Verify(s => s.GetFileAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetPhotoStreamAsync_WhenFileDoesNotExist_ReturnsFailure()
        {
            // Arrange
            var photoId = 1;
            var photo = new Photo
            {
                Id = photoId,
                UserId = "user123",
                Timestamp = DateTime.UtcNow,
                Latitude = 34.0522,
                Longitude = -118.2437,
                FilePath = "uploads/photos/123456.jpg"
            };
            
            _mockPhotoRepository.Setup(r => r.GetByIdAsync(photoId))
                .ReturnsAsync(photo);
            
            _mockStorageService.Setup(s => s.FileExistsAsync(photo.FilePath))
                .ReturnsAsync(false);
            
            // Act
            var result = await _photoService.GetPhotoStreamAsync(photoId);
            
            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Be("Photo file not found");
            
            _mockPhotoRepository.Verify(r => r.GetByIdAsync(photoId), Times.Once);
            _mockStorageService.Verify(s => s.FileExistsAsync(photo.FilePath), Times.Once);
            _mockStorageService.Verify(s => s.GetFileAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetPhotosByUserIdAsync_WithValidUserId_ReturnsPhotos()
        {
            // Arrange
            var userId = "user123";
            var photos = new List<Photo>
            {
                new Photo
                {
                    Id = 1,
                    UserId = userId,
                    Timestamp = DateTime.UtcNow.AddDays(-1),
                    Latitude = 34.0522,
                    Longitude = -118.2437,
                    FilePath = "uploads/photos/1.jpg"
                },
                new Photo
                {
                    Id = 2,
                    UserId = userId,
                    Timestamp = DateTime.UtcNow,
                    Latitude = 34.0523,
                    Longitude = -118.2438,
                    FilePath = "uploads/photos/2.jpg"
                }
            };
            
            _mockPhotoRepository.Setup(r => r.GetByUserIdAsync(userId))
                .ReturnsAsync(photos);
            
            // Act
            var result = await _photoService.GetPhotosByUserIdAsync(userId);
            
            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().BeEquivalentTo(photos);
            _mockPhotoRepository.Verify(r => r.GetByUserIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetPhotosByUserIdAsync_WithInvalidUserId_ReturnsEmptyCollection()
        {
            // Arrange
            var userId = "nonexistentUser";
            var emptyList = new List<Photo>();
            
            _mockPhotoRepository.Setup(r => r.GetByUserIdAsync(userId))
                .ReturnsAsync(emptyList);
            
            // Act
            var result = await _photoService.GetPhotosByUserIdAsync(userId);
            
            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().BeEmpty();
            _mockPhotoRepository.Verify(r => r.GetByUserIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetPaginatedPhotosByUserIdAsync_WithValidParameters_ReturnsPaginatedPhotos()
        {
            // Arrange
            var userId = "user123";
            var pageNumber = 1;
            var pageSize = 10;
            
            var photos = new List<Photo>
            {
                new Photo
                {
                    Id = 1,
                    UserId = userId,
                    Timestamp = DateTime.UtcNow.AddDays(-1),
                    Latitude = 34.0522,
                    Longitude = -118.2437,
                    FilePath = "uploads/photos/1.jpg"
                },
                new Photo
                {
                    Id = 2,
                    UserId = userId,
                    Timestamp = DateTime.UtcNow,
                    Latitude = 34.0523,
                    Longitude = -118.2438,
                    FilePath = "uploads/photos/2.jpg"
                }
            };
            
            var paginatedList = new PaginatedList<Photo>(photos, photos.Count, pageNumber, pageSize);
            
            _mockPhotoRepository.Setup(r => r.GetPaginatedByUserIdAsync(userId, pageNumber, pageSize))
                .ReturnsAsync(paginatedList);
            
            // Act
            var result = await _photoService.GetPaginatedPhotosByUserIdAsync(userId, pageNumber, pageSize);
            
            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Items.Should().BeEquivalentTo(photos);
            result.Data.PageNumber.Should().Be(pageNumber);
            result.Data.TotalPages.Should().Be(1);
            result.Data.TotalCount.Should().Be(photos.Count);
            
            _mockPhotoRepository.Verify(r => r.GetPaginatedByUserIdAsync(userId, pageNumber, pageSize), Times.Once);
        }

        [Fact]
        public async Task GetPhotosByLocationAsync_WithValidParameters_ReturnsPhotosNearLocation()
        {
            // Arrange
            double latitude = 34.0522;
            double longitude = -118.2437;
            double radiusInMeters = 1000;
            
            var photos = new List<Photo>
            {
                new Photo
                {
                    Id = 1,
                    UserId = "user123",
                    Timestamp = DateTime.UtcNow.AddDays(-1),
                    Latitude = latitude + 0.001,
                    Longitude = longitude - 0.001,
                    FilePath = "uploads/photos/1.jpg"
                },
                new Photo
                {
                    Id = 2,
                    UserId = "user456",
                    Timestamp = DateTime.UtcNow,
                    Latitude = latitude - 0.001,
                    Longitude = longitude + 0.001,
                    FilePath = "uploads/photos/2.jpg"
                }
            };
            
            _mockPhotoRepository.Setup(r => r.GetByLocationAsync(latitude, longitude, radiusInMeters))
                .ReturnsAsync(photos);
            
            // Act
            var result = await _photoService.GetPhotosByLocationAsync(latitude, longitude, radiusInMeters);
            
            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().BeEquivalentTo(photos);
            
            _mockPhotoRepository.Verify(r => r.GetByLocationAsync(latitude, longitude, radiusInMeters), Times.Once);
        }

        [Fact]
        public async Task GetPhotosByDateRangeAsync_WithValidDateRange_ReturnsPhotosInRange()
        {
            // Arrange
            var startDate = DateTime.UtcNow.AddDays(-7);
            var endDate = DateTime.UtcNow;
            
            var photos = new List<Photo>
            {
                new Photo
                {
                    Id = 1,
                    UserId = "user123",
                    Timestamp = startDate.AddDays(1),
                    Latitude = 34.0522,
                    Longitude = -118.2437,
                    FilePath = "uploads/photos/1.jpg"
                },
                new Photo
                {
                    Id = 2,
                    UserId = "user456",
                    Timestamp = endDate.AddDays(-1),
                    Latitude = 34.0523,
                    Longitude = -118.2438,
                    FilePath = "uploads/photos/2.jpg"
                }
            };
            
            _mockPhotoRepository.Setup(r => r.GetByDateRangeAsync(startDate, endDate))
                .ReturnsAsync(photos);
            
            // Act
            var result = await _photoService.GetPhotosByDateRangeAsync(startDate, endDate);
            
            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().BeEquivalentTo(photos);
            
            _mockPhotoRepository.Verify(r => r.GetByDateRangeAsync(startDate, endDate), Times.Once);
        }

        [Fact]
        public async Task DeletePhotoAsync_WithValidId_DeletesPhotoAndReturnsSuccess()
        {
            // Arrange
            var photoId = 1;
            var photo = new Photo
            {
                Id = photoId,
                UserId = "user123",
                Timestamp = DateTime.UtcNow,
                Latitude = 34.0522,
                Longitude = -118.2437,
                FilePath = "uploads/photos/123456.jpg"
            };
            
            _mockPhotoRepository.Setup(r => r.ExistsAsync(photoId))
                .ReturnsAsync(true);
            
            _mockPhotoRepository.Setup(r => r.GetByIdAsync(photoId))
                .ReturnsAsync(photo);
            
            _mockStorageService.Setup(s => s.DeleteFileAsync(photo.FilePath))
                .ReturnsAsync(Result.Success());
            
            _mockPhotoRepository.Setup(r => r.DeleteAsync(photoId))
                .ReturnsAsync(Result.Success());
            
            // Act
            var result = await _photoService.DeletePhotoAsync(photoId);
            
            // Assert
            result.Succeeded.Should().BeTrue();
            
            _mockPhotoRepository.Verify(r => r.ExistsAsync(photoId), Times.Once);
            _mockPhotoRepository.Verify(r => r.GetByIdAsync(photoId), Times.Once);
            _mockStorageService.Verify(s => s.DeleteFileAsync(photo.FilePath), Times.Once);
            _mockPhotoRepository.Verify(r => r.DeleteAsync(photoId), Times.Once);
        }

        [Fact]
        public async Task DeletePhotoAsync_WithInvalidId_ReturnsFailure()
        {
            // Arrange
            var photoId = 999;
            
            _mockPhotoRepository.Setup(r => r.ExistsAsync(photoId))
                .ReturnsAsync(false);
            
            // Act
            var result = await _photoService.DeletePhotoAsync(photoId);
            
            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Be("Photo not found");
            
            _mockPhotoRepository.Verify(r => r.ExistsAsync(photoId), Times.Once);
            _mockPhotoRepository.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
            _mockStorageService.Verify(s => s.DeleteFileAsync(It.IsAny<string>()), Times.Never);
            _mockPhotoRepository.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task DeletePhotoAsync_WhenStorageServiceFails_ContinuesWithDatabaseDeletion()
        {
            // Arrange
            var photoId = 1;
            var photo = new Photo
            {
                Id = photoId,
                UserId = "user123",
                Timestamp = DateTime.UtcNow,
                Latitude = 34.0522,
                Longitude = -118.2437,
                FilePath = "uploads/photos/123456.jpg"
            };
            
            _mockPhotoRepository.Setup(r => r.ExistsAsync(photoId))
                .ReturnsAsync(true);
            
            _mockPhotoRepository.Setup(r => r.GetByIdAsync(photoId))
                .ReturnsAsync(photo);
            
            _mockStorageService.Setup(s => s.DeleteFileAsync(photo.FilePath))
                .ReturnsAsync(Result.Failure("Storage deletion error"));
            
            _mockPhotoRepository.Setup(r => r.DeleteAsync(photoId))
                .ReturnsAsync(Result.Success());
            
            // Act
            var result = await _photoService.DeletePhotoAsync(photoId);
            
            // Assert
            result.Succeeded.Should().BeTrue();
            
            _mockPhotoRepository.Verify(r => r.ExistsAsync(photoId), Times.Once);
            _mockPhotoRepository.Verify(r => r.GetByIdAsync(photoId), Times.Once);
            _mockStorageService.Verify(s => s.DeleteFileAsync(photo.FilePath), Times.Once);
            _mockPhotoRepository.Verify(r => r.DeleteAsync(photoId), Times.Once);
        }

        [Fact]
        public async Task DeletePhotoAsync_WhenRepositoryDeleteFails_ReturnsFailure()
        {
            // Arrange
            var photoId = 1;
            var photo = new Photo
            {
                Id = photoId,
                UserId = "user123",
                Timestamp = DateTime.UtcNow,
                Latitude = 34.0522,
                Longitude = -118.2437,
                FilePath = "uploads/photos/123456.jpg"
            };
            
            _mockPhotoRepository.Setup(r => r.ExistsAsync(photoId))
                .ReturnsAsync(true);
            
            _mockPhotoRepository.Setup(r => r.GetByIdAsync(photoId))
                .ReturnsAsync(photo);
            
            _mockStorageService.Setup(s => s.DeleteFileAsync(photo.FilePath))
                .ReturnsAsync(Result.Success());
            
            _mockPhotoRepository.Setup(r => r.DeleteAsync(photoId))
                .ReturnsAsync(Result.Failure("Repository deletion error"));
            
            // Act
            var result = await _photoService.DeletePhotoAsync(photoId);
            
            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("Failed to delete photo record");
            
            _mockPhotoRepository.Verify(r => r.ExistsAsync(photoId), Times.Once);
            _mockPhotoRepository.Verify(r => r.GetByIdAsync(photoId), Times.Once);
            _mockStorageService.Verify(s => s.DeleteFileAsync(photo.FilePath), Times.Once);
            _mockPhotoRepository.Verify(r => r.DeleteAsync(photoId), Times.Once);
        }
    }
}