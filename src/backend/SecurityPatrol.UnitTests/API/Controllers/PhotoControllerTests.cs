using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Xunit;
using Moq;
using FluentAssertions;
using SecurityPatrol.API.Controllers;
using SecurityPatrol.Core.Models;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.UnitTests.Helpers;

namespace SecurityPatrol.UnitTests.API.Controllers
{
    public class PhotoControllerTests : TestBase
    {
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly PhotoController _controller;

        public PhotoControllerTests()
        {
            // Setup current user service mock
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockCurrentUserService.Setup(x => x.UserId).Returns("user1");

            // Create controller with dependencies
            _controller = new PhotoController(
                MockPhotoService.Object,
                _mockCurrentUserService.Object,
                CreateMockLogger<PhotoController>().Object
            );
        }

        [Fact]
        public async Task Upload_WithValidRequest_ReturnsOkResult()
        {
            // Arrange
            var request = new PhotoUploadRequest
            {
                Timestamp = DateTime.UtcNow,
                Latitude = 40.7128,
                Longitude = -74.0060,
                UserId = "user1"
            };
            
            var mockFile = new Mock<IFormFile>();
            var content = "test image content";
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write(content);
            writer.Flush();
            ms.Position = 0;
            
            mockFile.Setup(f => f.Length).Returns(ms.Length);
            mockFile.Setup(f => f.OpenReadStream()).Returns(ms);
            
            MockPhotoService.Setup(s => s.UploadPhotoAsync(
                It.IsAny<PhotoUploadRequest>(), 
                It.IsAny<Stream>(),
                It.IsAny<string>()))
                .ReturnsAsync(Result.Success(new PhotoUploadResponse { Id = "photo-123", Status = "success" }));
            
            // Act
            var result = await _controller.Upload(request, mockFile.Object);
            
            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeOfType<Result<PhotoUploadResponse>>().Subject;
            returnValue.Success.Should().BeTrue();
            returnValue.Data.Id.Should().Be("photo-123");
            
            MockPhotoService.Verify(s => s.UploadPhotoAsync(
                It.Is<PhotoUploadRequest>(r => r.UserId == "user1"), 
                It.IsAny<Stream>(),
                It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Upload_WithNullFile_ReturnsBadRequest()
        {
            // Arrange
            var request = new PhotoUploadRequest
            {
                Timestamp = DateTime.UtcNow,
                Latitude = 40.7128,
                Longitude = -74.0060
            };
            
            // Act
            var result = await _controller.Upload(request, null);
            
            // Assert
            var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var returnValue = badRequestResult.Value.Should().BeOfType<Result<PhotoUploadResponse>>().Subject;
            returnValue.Success.Should().BeFalse();
            
            MockPhotoService.Verify(s => s.UploadPhotoAsync(
                It.IsAny<PhotoUploadRequest>(), 
                It.IsAny<Stream>(),
                It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Upload_WithEmptyFile_ReturnsBadRequest()
        {
            // Arrange
            var request = new PhotoUploadRequest
            {
                Timestamp = DateTime.UtcNow,
                Latitude = 40.7128,
                Longitude = -74.0060
            };
            
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(0);
            
            // Act
            var result = await _controller.Upload(request, mockFile.Object);
            
            // Assert
            var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var returnValue = badRequestResult.Value.Should().BeOfType<Result<PhotoUploadResponse>>().Subject;
            returnValue.Success.Should().BeFalse();
            
            MockPhotoService.Verify(s => s.UploadPhotoAsync(
                It.IsAny<PhotoUploadRequest>(), 
                It.IsAny<Stream>(),
                It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Upload_WhenServiceThrowsException_ThrowsException()
        {
            // Arrange
            var request = new PhotoUploadRequest
            {
                Timestamp = DateTime.UtcNow,
                Latitude = 40.7128,
                Longitude = -74.0060
            };
            
            var mockFile = new Mock<IFormFile>();
            var content = "test image content";
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write(content);
            writer.Flush();
            ms.Position = 0;
            
            mockFile.Setup(f => f.Length).Returns(ms.Length);
            mockFile.Setup(f => f.OpenReadStream()).Returns(ms);
            
            MockPhotoService.Setup(s => s.UploadPhotoAsync(
                It.IsAny<PhotoUploadRequest>(), 
                It.IsAny<Stream>(),
                It.IsAny<string>()))
                .ThrowsAsync(new Exception("Test exception"));
            
            // Act & Assert
            await AssertExceptionAsync<Exception>(() => _controller.Upload(request, mockFile.Object));
        }

        [Fact]
        public async Task GetById_WithValidId_ReturnsOkResult()
        {
            // Arrange
            var photoId = 1;
            var photo = TestData.GetTestPhotoById(photoId);
            
            MockPhotoService.Setup(s => s.GetPhotoAsync(photoId))
                .ReturnsAsync(Result.Success(photo));
            
            // Act
            var result = await _controller.GetById(photoId);
            
            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeOfType<Result<Photo>>().Subject;
            returnValue.Success.Should().BeTrue();
            returnValue.Data.Should().Be(photo);
            
            MockPhotoService.Verify(s => s.GetPhotoAsync(photoId), Times.Once);
        }

        [Fact]
        public async Task GetById_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var photoId = 999;
            
            MockPhotoService.Setup(s => s.GetPhotoAsync(photoId))
                .ReturnsAsync(Result.Failure<Photo>("Photo not found"));
            
            // Act
            var result = await _controller.GetById(photoId);
            
            // Assert
            result.Result.Should().BeOfType<NotFoundResult>();
            
            MockPhotoService.Verify(s => s.GetPhotoAsync(photoId), Times.Once);
        }

        [Fact]
        public async Task GetPhotoFile_WithValidId_ReturnsFileResult()
        {
            // Arrange
            var photoId = 1;
            var stream = new MemoryStream(new byte[] { 0, 1, 2, 3 });
            var contentType = "image/jpeg";
            var fileName = "test.jpg";
            
            MockPhotoService.Setup(s => s.GetPhotoStreamAsync(photoId))
                .ReturnsAsync(Result.Success<(Stream, string, string)>((stream, contentType, fileName)));
            
            // Act
            var result = await _controller.GetPhotoFile(photoId);
            
            // Assert
            var fileResult = result.Should().BeOfType<FileStreamResult>().Subject;
            fileResult.FileStream.Should().BeSameAs(stream);
            fileResult.ContentType.Should().Be(contentType);
            fileResult.FileDownloadName.Should().Be(fileName);
            
            MockPhotoService.Verify(s => s.GetPhotoStreamAsync(photoId), Times.Once);
        }

        [Fact]
        public async Task GetPhotoFile_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var photoId = 999;
            
            MockPhotoService.Setup(s => s.GetPhotoStreamAsync(photoId))
                .ReturnsAsync(Result.Failure<(Stream, string, string)>("Photo file not found"));
            
            // Act
            var result = await _controller.GetPhotoFile(photoId);
            
            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
            
            MockPhotoService.Verify(s => s.GetPhotoStreamAsync(photoId), Times.Once);
        }

        [Fact]
        public async Task GetMyPhotos_ReturnsOkResult()
        {
            // Arrange
            var userId = "user1";
            var photos = new List<Photo> 
            { 
                new Photo { Id = 1, UserId = userId }, 
                new Photo { Id = 2, UserId = userId } 
            };
            
            MockPhotoService.Setup(s => s.GetPhotosByUserIdAsync(userId))
                .ReturnsAsync(Result.Success<IEnumerable<Photo>>(photos));
            
            // Act
            var result = await _controller.GetMyPhotos();
            
            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeOfType<Result<IEnumerable<Photo>>>().Subject;
            returnValue.Success.Should().BeTrue();
            returnValue.Data.Should().BeEquivalentTo(photos);
            
            MockPhotoService.Verify(s => s.GetPhotosByUserIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetMyPhotosPaginated_ReturnsOkResult()
        {
            // Arrange
            var userId = "user1";
            var pageNumber = 1;
            var pageSize = 10;
            var photos = new List<Photo> 
            { 
                new Photo { Id = 1, UserId = userId }, 
                new Photo { Id = 2, UserId = userId } 
            };
            var paginatedList = new PaginatedList<Photo>(photos, photos.Count, pageNumber, pageSize);
            
            MockPhotoService.Setup(s => s.GetPaginatedPhotosByUserIdAsync(userId, pageNumber, pageSize))
                .ReturnsAsync(Result.Success(paginatedList));
            
            // Act
            var result = await _controller.GetMyPhotosPaginated(pageNumber, pageSize);
            
            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeOfType<Result<PaginatedList<Photo>>>().Subject;
            returnValue.Success.Should().BeTrue();
            returnValue.Data.Should().Be(paginatedList);
            
            MockPhotoService.Verify(s => s.GetPaginatedPhotosByUserIdAsync(userId, pageNumber, pageSize), Times.Once);
        }

        [Fact]
        public async Task GetPhotosByLocation_WithValidParameters_ReturnsOkResult()
        {
            // Arrange
            var latitude = 40.7128;
            var longitude = -74.0060;
            var radiusInMeters = 100.0;
            var photos = new List<Photo> 
            { 
                new Photo { Id = 1, Latitude = latitude, Longitude = longitude },
                new Photo { Id = 2, Latitude = latitude + 0.001, Longitude = longitude - 0.001 } 
            };
            
            MockPhotoService.Setup(s => s.GetPhotosByLocationAsync(latitude, longitude, radiusInMeters))
                .ReturnsAsync(Result.Success<IEnumerable<Photo>>(photos));
            
            // Act
            var result = await _controller.GetPhotosByLocation(latitude, longitude, radiusInMeters);
            
            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeOfType<Result<IEnumerable<Photo>>>().Subject;
            returnValue.Success.Should().BeTrue();
            returnValue.Data.Should().BeEquivalentTo(photos);
            
            MockPhotoService.Verify(s => s.GetPhotosByLocationAsync(latitude, longitude, radiusInMeters), Times.Once);
        }

        [Fact]
        public async Task GetPhotosByLocation_WithInvalidParameters_ReturnsBadRequest()
        {
            // Arrange
            var invalidLatitude = 100.0; // Outside valid range (-90 to 90)
            var longitude = -74.0060;
            var radiusInMeters = 100.0;
            
            // Act
            var result = await _controller.GetPhotosByLocation(invalidLatitude, longitude, radiusInMeters);
            
            // Assert
            var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var returnValue = badRequestResult.Value.Should().BeOfType<Result<IEnumerable<Photo>>>().Subject;
            returnValue.Success.Should().BeFalse();
            
            MockPhotoService.Verify(s => s.GetPhotosByLocationAsync(
                It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()), Times.Never);
        }

        [Fact]
        public async Task GetPhotosByDateRange_WithValidParameters_ReturnsOkResult()
        {
            // Arrange
            var startDate = DateTime.UtcNow.AddDays(-7);
            var endDate = DateTime.UtcNow;
            var photos = new List<Photo> 
            { 
                new Photo { Id = 1, Timestamp = DateTime.UtcNow.AddDays(-5) },
                new Photo { Id = 2, Timestamp = DateTime.UtcNow.AddDays(-2) } 
            };
            
            MockPhotoService.Setup(s => s.GetPhotosByDateRangeAsync(startDate, endDate))
                .ReturnsAsync(Result.Success<IEnumerable<Photo>>(photos));
            
            // Act
            var result = await _controller.GetPhotosByDateRange(startDate, endDate);
            
            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeOfType<Result<IEnumerable<Photo>>>().Subject;
            returnValue.Success.Should().BeTrue();
            returnValue.Data.Should().BeEquivalentTo(photos);
            
            MockPhotoService.Verify(s => s.GetPhotosByDateRangeAsync(startDate, endDate), Times.Once);
        }

        [Fact]
        public async Task GetPhotosByDateRange_WithInvalidParameters_ReturnsBadRequest()
        {
            // Arrange
            var startDate = DateTime.UtcNow;
            var endDate = DateTime.UtcNow.AddDays(-7); // End date before start date
            
            // Act
            var result = await _controller.GetPhotosByDateRange(startDate, endDate);
            
            // Assert
            var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var returnValue = badRequestResult.Value.Should().BeOfType<Result<IEnumerable<Photo>>>().Subject;
            returnValue.Success.Should().BeFalse();
            
            MockPhotoService.Verify(s => s.GetPhotosByDateRangeAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never);
        }

        [Fact]
        public async Task DeletePhoto_WithValidId_ReturnsOkResult()
        {
            // Arrange
            var photoId = 1;
            
            MockPhotoService.Setup(s => s.DeletePhotoAsync(photoId))
                .ReturnsAsync(Result.Success());
            
            // Act
            var result = await _controller.DeletePhoto(photoId);
            
            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeOfType<Result>().Subject;
            returnValue.Success.Should().BeTrue();
            
            MockPhotoService.Verify(s => s.DeletePhotoAsync(photoId), Times.Once);
        }

        [Fact]
        public async Task DeletePhoto_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var photoId = 999;
            
            MockPhotoService.Setup(s => s.DeletePhotoAsync(photoId))
                .ReturnsAsync(Result.Failure("Photo not found"));
            
            // Act
            var result = await _controller.DeletePhoto(photoId);
            
            // Assert
            var notFoundResult = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
            var returnValue = notFoundResult.Value.Should().BeOfType<Result>().Subject;
            returnValue.Success.Should().BeFalse();
            
            MockPhotoService.Verify(s => s.DeletePhotoAsync(photoId), Times.Once);
        }
    }
}