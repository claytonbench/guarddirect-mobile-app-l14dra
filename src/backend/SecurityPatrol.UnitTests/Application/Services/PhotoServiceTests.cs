using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using SecurityPatrol.Application.Services;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Core.Models;
using SecurityPatrol.UnitTests.Helpers;

namespace SecurityPatrol.UnitTests.Application.Services
{
    /// <summary>
    /// Contains unit tests for the PhotoService class to verify its functionality for managing photos in the Security Patrol application.
    /// </summary>
    public class PhotoServiceTests : TestBase
    {
        private readonly Mock<IStorageService> _mockStorageService;
        private readonly PhotoService _photoService;

        /// <summary>
        /// Initializes a new instance of the PhotoServiceTests class with mocked dependencies.
        /// </summary>
        public PhotoServiceTests()
        {
            _mockStorageService = new Mock<IStorageService>();
            _photoService = new PhotoService(MockPhotoRepository.Object, _mockStorageService.Object);
        }

        /// <summary>
        /// Tests that UploadPhotoAsync returns a successful result when provided with a valid request and photo stream.
        /// </summary>
        [Fact]
        public async Task UploadPhotoAsync_WithValidRequest_ReturnsSuccess()
        {
            // Arrange
            var request = new PhotoUploadRequest
            {
                UserId = "user1",
                Timestamp = DateTime.UtcNow,
                Latitude = 40.7128,
                Longitude = -74.0060
            };
            
            var photoStream = new MemoryStream(new byte[] { 0, 1, 2, 3, 4 });
            var contentType = "image/jpeg";
            var filePath = "/storage/photos/photo123.jpg";
            
            _mockStorageService.Setup(s => s.StoreFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(Result.Success(filePath));
                
            MockPhotoRepository.Setup(r => r.AddAsync(It.IsAny<Photo>()))
                .ReturnsAsync(Result.Success(new Photo { Id = 123 }));
            
            // Act
            var result = await _photoService.UploadPhotoAsync(request, photoStream, contentType);
            
            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Id.Should().Be("123");
            result.Data.Status.Should().Be("success");
            
            _mockStorageService.Verify(s => s.StoreFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), contentType), Times.Once);
            MockPhotoRepository.Verify(r => r.AddAsync(It.Is<Photo>(p => 
                p.UserId == request.UserId && 
                p.Latitude == request.Latitude && 
                p.Longitude == request.Longitude && 
                p.FilePath == filePath)), Times.Once);
        }

        /// <summary>
        /// Tests that UploadPhotoAsync throws an ArgumentNullException when the request is null.
        /// </summary>
        [Fact]
        public async Task UploadPhotoAsync_WithNullRequest_ThrowsArgumentNullException()
        {
            // Arrange
            var photoStream = new MemoryStream(new byte[] { 0, 1, 2, 3, 4 });
            var contentType = "image/jpeg";
            
            // Act & Assert
            var ex = await AssertExceptionAsync<ArgumentNullException>(() => 
                _photoService.UploadPhotoAsync(null, photoStream, contentType));
            
            ex.ParamName.Should().Be("request");
        }

        /// <summary>
        /// Tests that UploadPhotoAsync throws an ArgumentNullException when the photo stream is null.
        /// </summary>
        [Fact]
        public async Task UploadPhotoAsync_WithNullStream_ThrowsArgumentNullException()
        {
            // Arrange
            var request = new PhotoUploadRequest
            {
                UserId = "user1",
                Timestamp = DateTime.UtcNow,
                Latitude = 40.7128,
                Longitude = -74.0060
            };
            var contentType = "image/jpeg";
            
            // Act & Assert
            var ex = await AssertExceptionAsync<ArgumentNullException>(() => 
                _photoService.UploadPhotoAsync(request, null, contentType));
            
            ex.ParamName.Should().Be("photoStream");
        }

        /// <summary>
        /// Tests that UploadPhotoAsync throws an ArgumentNullException when the content type is null.
        /// </summary>
        [Fact]
        public async Task UploadPhotoAsync_WithNullContentType_ThrowsArgumentNullException()
        {
            // Arrange
            var request = new PhotoUploadRequest
            {
                UserId = "user1",
                Timestamp = DateTime.UtcNow,
                Latitude = 40.7128,
                Longitude = -74.0060
            };
            var photoStream = new MemoryStream(new byte[] { 0, 1, 2, 3, 4 });
            
            // Act & Assert
            var ex = await AssertExceptionAsync<ArgumentNullException>(() => 
                _photoService.UploadPhotoAsync(request, photoStream, null));
            
            ex.ParamName.Should().Be("contentType");
        }

        /// <summary>
        /// Tests that UploadPhotoAsync returns a failure result when the storage service fails to store the file.
        /// </summary>
        [Fact]
        public async Task UploadPhotoAsync_WhenStorageServiceFails_ReturnsFailure()
        {
            // Arrange
            var request = new PhotoUploadRequest
            {
                UserId = "user1",
                Timestamp = DateTime.UtcNow,
                Latitude = 40.7128,
                Longitude = -74.0060
            };
            
            var photoStream = new MemoryStream(new byte[] { 0, 1, 2, 3, 4 });
            var contentType = "image/jpeg";
            
            _mockStorageService.Setup(s => s.StoreFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(Result.Failure<string>("Storage failure"));
            
            // Act
            var result = await _photoService.UploadPhotoAsync(request, photoStream, contentType);
            
            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("Failed to store photo");
            
            _mockStorageService.Verify(s => s.StoreFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), contentType), Times.Once);
            MockPhotoRepository.Verify(r => r.AddAsync(It.IsAny<Photo>()), Times.Never);
        }

        /// <summary>
        /// Tests that UploadPhotoAsync deletes the stored file and returns a failure result when the repository fails to add the photo.
        /// </summary>
        [Fact]
        public async Task UploadPhotoAsync_WhenRepositoryFails_DeletesStoredFileAndReturnsFailure()
        {
            // Arrange
            var request = new PhotoUploadRequest
            {
                UserId = "user1",
                Timestamp = DateTime.UtcNow,
                Latitude = 40.7128,
                Longitude = -74.0060
            };
            
            var photoStream = new MemoryStream(new byte[] { 0, 1, 2, 3, 4 });
            var contentType = "image/jpeg";
            var filePath = "/storage/photos/photo123.jpg";
            
            _mockStorageService.Setup(s => s.StoreFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(Result.Success(filePath));
                
            MockPhotoRepository.Setup(r => r.AddAsync(It.IsAny<Photo>()))
                .ReturnsAsync(Result.Failure<Photo>("Repository failure"));
                
            _mockStorageService.Setup(s => s.DeleteFileAsync(filePath))
                .ReturnsAsync(Result.Success());
            
            // Act
            var result = await _photoService.UploadPhotoAsync(request, photoStream, contentType);
            
            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("Failed to save photo metadata");
            
            _mockStorageService.Verify(s => s.StoreFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), contentType), Times.Once);
            MockPhotoRepository.Verify(r => r.AddAsync(It.IsAny<Photo>()), Times.Once);
            _mockStorageService.Verify(s => s.DeleteFileAsync(filePath), Times.Once);
        }

        /// <summary>
        /// Tests that GetPhotoAsync returns a photo when provided with a valid ID.
        /// </summary>
        [Fact]
        public async Task GetPhotoAsync_WithValidId_ReturnsPhoto()
        {
            // Arrange
            var testPhoto = TestData.GetTestPhotoById(1);
            MockPhotoRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(testPhoto);
            
            // Act
            var result = await _photoService.GetPhotoAsync(1);
            
            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().BeSameAs(testPhoto);
            
            MockPhotoRepository.Verify(r => r.GetByIdAsync(1), Times.Once);
        }

        /// <summary>
        /// Tests that GetPhotoAsync returns a failure result when provided with an invalid ID.
        /// </summary>
        [Fact]
        public async Task GetPhotoAsync_WithInvalidId_ReturnsFailure()
        {
            // Arrange
            MockPhotoRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((Photo)null);
            
            // Act
            var result = await _photoService.GetPhotoAsync(0);
            
            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("Photo not found");
            
            MockPhotoRepository.Verify(r => r.GetByIdAsync(0), Times.Once);
        }

        /// <summary>
        /// Tests that GetPhotoStreamAsync returns a photo stream when provided with a valid ID.
        /// </summary>
        [Fact]
        public async Task GetPhotoStreamAsync_WithValidId_ReturnsPhotoStream()
        {
            // Arrange
            var testPhoto = TestData.GetTestPhotoById(1);
            var testStream = new MemoryStream(new byte[] { 0, 1, 2, 3, 4 });
            
            MockPhotoRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(testPhoto);
                
            _mockStorageService.Setup(s => s.FileExistsAsync(testPhoto.FilePath))
                .ReturnsAsync(true);
                
            _mockStorageService.Setup(s => s.GetFileAsync(testPhoto.FilePath))
                .ReturnsAsync(Result.Success<Stream>(testStream));
            
            // Act
            var result = await _photoService.GetPhotoStreamAsync(1);
            
            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().BeSameAs(testStream);
            
            MockPhotoRepository.Verify(r => r.GetByIdAsync(1), Times.Once);
            _mockStorageService.Verify(s => s.FileExistsAsync(testPhoto.FilePath), Times.Once);
            _mockStorageService.Verify(s => s.GetFileAsync(testPhoto.FilePath), Times.Once);
        }

        /// <summary>
        /// Tests that GetPhotoStreamAsync returns a failure result when provided with an invalid ID.
        /// </summary>
        [Fact]
        public async Task GetPhotoStreamAsync_WithInvalidId_ReturnsFailure()
        {
            // Arrange
            MockPhotoRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((Photo)null);
            
            // Act
            var result = await _photoService.GetPhotoStreamAsync(0);
            
            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("Photo not found");
            
            MockPhotoRepository.Verify(r => r.GetByIdAsync(0), Times.Once);
            _mockStorageService.Verify(s => s.FileExistsAsync(It.IsAny<string>()), Times.Never);
            _mockStorageService.Verify(s => s.GetFileAsync(It.IsAny<string>()), Times.Never);
        }

        /// <summary>
        /// Tests that GetPhotoStreamAsync returns a failure result when the photo file does not exist.
        /// </summary>
        [Fact]
        public async Task GetPhotoStreamAsync_WhenFileDoesNotExist_ReturnsFailure()
        {
            // Arrange
            var testPhoto = TestData.GetTestPhotoById(1);
            
            MockPhotoRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(testPhoto);
                
            _mockStorageService.Setup(s => s.FileExistsAsync(testPhoto.FilePath))
                .ReturnsAsync(false);
            
            // Act
            var result = await _photoService.GetPhotoStreamAsync(1);
            
            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("Photo file not found");
            
            MockPhotoRepository.Verify(r => r.GetByIdAsync(1), Times.Once);
            _mockStorageService.Verify(s => s.FileExistsAsync(testPhoto.FilePath), Times.Once);
            _mockStorageService.Verify(s => s.GetFileAsync(It.IsAny<string>()), Times.Never);
        }

        /// <summary>
        /// Tests that GetPhotoStreamAsync returns a failure result when the storage service fails to retrieve the file.
        /// </summary>
        [Fact]
        public async Task GetPhotoStreamAsync_WhenStorageServiceFails_ReturnsFailure()
        {
            // Arrange
            var testPhoto = TestData.GetTestPhotoById(1);
            
            MockPhotoRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(testPhoto);
                
            _mockStorageService.Setup(s => s.FileExistsAsync(testPhoto.FilePath))
                .ReturnsAsync(true);
                
            _mockStorageService.Setup(s => s.GetFileAsync(testPhoto.FilePath))
                .ReturnsAsync(Result.Failure<Stream>("Storage service failure"));
            
            // Act
            var result = await _photoService.GetPhotoStreamAsync(1);
            
            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("Failed to retrieve photo file");
            
            MockPhotoRepository.Verify(r => r.GetByIdAsync(1), Times.Once);
            _mockStorageService.Verify(s => s.FileExistsAsync(testPhoto.FilePath), Times.Once);
            _mockStorageService.Verify(s => s.GetFileAsync(testPhoto.FilePath), Times.Once);
        }

        /// <summary>
        /// Tests that GetPhotosByUserIdAsync returns photos for a valid user ID.
        /// </summary>
        [Fact]
        public async Task GetPhotosByUserIdAsync_WithValidUserId_ReturnsPhotos()
        {
            // Arrange
            var userId = "user1";
            var testPhotos = TestData.GetTestPhotos().Where(p => p.UserId == userId).ToList();
            
            MockPhotoRepository.Setup(r => r.GetByUserIdAsync(userId))
                .ReturnsAsync(testPhotos);
            
            // Act
            var result = await _photoService.GetPhotosByUserIdAsync(userId);
            
            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().BeEquivalentTo(testPhotos);
            
            MockPhotoRepository.Verify(r => r.GetByUserIdAsync(userId), Times.Once);
        }

        /// <summary>
        /// Tests that GetPhotosByUserIdAsync returns an empty collection when provided with an invalid user ID.
        /// </summary>
        [Fact]
        public async Task GetPhotosByUserIdAsync_WithInvalidUserId_ReturnsEmptyCollection()
        {
            // Arrange
            var userId = "nonexistent";
            var emptyList = new List<Photo>();
            
            MockPhotoRepository.Setup(r => r.GetByUserIdAsync(userId))
                .ReturnsAsync(emptyList);
            
            // Act
            var result = await _photoService.GetPhotosByUserIdAsync(userId);
            
            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().BeEmpty();
            
            MockPhotoRepository.Verify(r => r.GetByUserIdAsync(userId), Times.Once);
        }

        /// <summary>
        /// Tests that GetPaginatedPhotosByUserIdAsync returns a paginated list of photos when provided with valid parameters.
        /// </summary>
        [Fact]
        public async Task GetPaginatedPhotosByUserIdAsync_WithValidParameters_ReturnsPaginatedPhotos()
        {
            // Arrange
            var userId = "user1";
            var pageNumber = 1;
            var pageSize = 10;
            var testPhotos = TestData.GetTestPhotos().Where(p => p.UserId == userId).ToList();
            var paginatedPhotos = new PaginatedList<Photo>(testPhotos, testPhotos.Count, pageNumber, pageSize);
            
            MockPhotoRepository.Setup(r => r.GetPaginatedByUserIdAsync(userId, pageNumber, pageSize))
                .ReturnsAsync(paginatedPhotos);
            
            // Act
            var result = await _photoService.GetPaginatedPhotosByUserIdAsync(userId, pageNumber, pageSize);
            
            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().BeSameAs(paginatedPhotos);
            
            MockPhotoRepository.Verify(r => r.GetPaginatedByUserIdAsync(userId, pageNumber, pageSize), Times.Once);
        }

        /// <summary>
        /// Tests that GetPhotosByLocationAsync returns photos when provided with valid location parameters.
        /// </summary>
        [Fact]
        public async Task GetPhotosByLocationAsync_WithValidParameters_ReturnsPhotos()
        {
            // Arrange
            var latitude = 40.7128;
            var longitude = -74.0060;
            var radiusInMeters = 100.0;
            var testPhotos = TestData.GetTestPhotos().Take(2).ToList();
            
            MockPhotoRepository.Setup(r => r.GetByLocationAsync(latitude, longitude, radiusInMeters))
                .ReturnsAsync(testPhotos);
            
            // Act
            var result = await _photoService.GetPhotosByLocationAsync(latitude, longitude, radiusInMeters);
            
            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().BeEquivalentTo(testPhotos);
            
            MockPhotoRepository.Verify(r => r.GetByLocationAsync(latitude, longitude, radiusInMeters), Times.Once);
        }

        /// <summary>
        /// Tests that GetPhotosByDateRangeAsync returns photos when provided with a valid date range.
        /// </summary>
        [Fact]
        public async Task GetPhotosByDateRangeAsync_WithValidDateRange_ReturnsPhotos()
        {
            // Arrange
            var startDate = DateTime.UtcNow.AddDays(-7);
            var endDate = DateTime.UtcNow;
            var testPhotos = TestData.GetTestPhotos().Take(2).ToList();
            
            MockPhotoRepository.Setup(r => r.GetByDateRangeAsync(startDate, endDate))
                .ReturnsAsync(testPhotos);
            
            // Act
            var result = await _photoService.GetPhotosByDateRangeAsync(startDate, endDate);
            
            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().BeEquivalentTo(testPhotos);
            
            MockPhotoRepository.Verify(r => r.GetByDateRangeAsync(startDate, endDate), Times.Once);
        }

        /// <summary>
        /// Tests that DeletePhotoAsync returns a successful result when provided with a valid ID.
        /// </summary>
        [Fact]
        public async Task DeletePhotoAsync_WithValidId_ReturnsSuccess()
        {
            // Arrange
            var testPhoto = TestData.GetTestPhotoById(1);
            
            MockPhotoRepository.Setup(r => r.ExistsAsync(1))
                .ReturnsAsync(true);
                
            MockPhotoRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(testPhoto);
                
            _mockStorageService.Setup(s => s.DeleteFileAsync(testPhoto.FilePath))
                .ReturnsAsync(Result.Success());
                
            MockPhotoRepository.Setup(r => r.DeleteAsync(1))
                .ReturnsAsync(Result.Success());
            
            // Act
            var result = await _photoService.DeletePhotoAsync(1);
            
            // Assert
            result.Succeeded.Should().BeTrue();
            
            MockPhotoRepository.Verify(r => r.ExistsAsync(1), Times.Once);
            MockPhotoRepository.Verify(r => r.GetByIdAsync(1), Times.Once);
            _mockStorageService.Verify(s => s.DeleteFileAsync(testPhoto.FilePath), Times.Once);
            MockPhotoRepository.Verify(r => r.DeleteAsync(1), Times.Once);
        }

        /// <summary>
        /// Tests that DeletePhotoAsync returns a failure result when provided with an invalid ID.
        /// </summary>
        [Fact]
        public async Task DeletePhotoAsync_WithInvalidId_ReturnsFailure()
        {
            // Arrange
            MockPhotoRepository.Setup(r => r.ExistsAsync(0))
                .ReturnsAsync(false);
            
            // Act
            var result = await _photoService.DeletePhotoAsync(0);
            
            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("Photo not found");
            
            MockPhotoRepository.Verify(r => r.ExistsAsync(0), Times.Once);
            MockPhotoRepository.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
            _mockStorageService.Verify(s => s.DeleteFileAsync(It.IsAny<string>()), Times.Never);
            MockPhotoRepository.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
        }

        /// <summary>
        /// Tests that DeletePhotoAsync continues with database deletion even when the storage service fails to delete the file.
        /// </summary>
        [Fact]
        public async Task DeletePhotoAsync_WhenStorageServiceFails_ContinuesWithDatabaseDeletion()
        {
            // Arrange
            var testPhoto = TestData.GetTestPhotoById(1);
            
            MockPhotoRepository.Setup(r => r.ExistsAsync(1))
                .ReturnsAsync(true);
                
            MockPhotoRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(testPhoto);
                
            _mockStorageService.Setup(s => s.DeleteFileAsync(testPhoto.FilePath))
                .ReturnsAsync(Result.Failure("Storage deletion failed"));
                
            MockPhotoRepository.Setup(r => r.DeleteAsync(1))
                .ReturnsAsync(Result.Success());
            
            // Act
            var result = await _photoService.DeletePhotoAsync(1);
            
            // Assert
            result.Succeeded.Should().BeTrue();
            
            MockPhotoRepository.Verify(r => r.ExistsAsync(1), Times.Once);
            MockPhotoRepository.Verify(r => r.GetByIdAsync(1), Times.Once);
            _mockStorageService.Verify(s => s.DeleteFileAsync(testPhoto.FilePath), Times.Once);
            MockPhotoRepository.Verify(r => r.DeleteAsync(1), Times.Once);
        }

        /// <summary>
        /// Tests that DeletePhotoAsync returns a failure result when the repository fails to delete the photo.
        /// </summary>
        [Fact]
        public async Task DeletePhotoAsync_WhenRepositoryFails_ReturnsFailure()
        {
            // Arrange
            var testPhoto = TestData.GetTestPhotoById(1);
            
            MockPhotoRepository.Setup(r => r.ExistsAsync(1))
                .ReturnsAsync(true);
                
            MockPhotoRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(testPhoto);
                
            _mockStorageService.Setup(s => s.DeleteFileAsync(testPhoto.FilePath))
                .ReturnsAsync(Result.Success());
                
            MockPhotoRepository.Setup(r => r.DeleteAsync(1))
                .ReturnsAsync(Result.Failure("Repository deletion failed"));
            
            // Act
            var result = await _photoService.DeletePhotoAsync(1);
            
            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("Failed to delete photo record");
            
            MockPhotoRepository.Verify(r => r.ExistsAsync(1), Times.Once);
            MockPhotoRepository.Verify(r => r.GetByIdAsync(1), Times.Once);
            _mockStorageService.Verify(s => s.DeleteFileAsync(testPhoto.FilePath), Times.Once);
            MockPhotoRepository.Verify(r => r.DeleteAsync(1), Times.Once);
        }
    }
}