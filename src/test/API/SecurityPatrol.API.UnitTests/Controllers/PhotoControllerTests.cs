using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;
using FluentAssertions;
using SecurityPatrol.API.UnitTests.Setup;
using SecurityPatrol.TestCommon.Constants;
using SecurityPatrol.API.Controllers;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Core.Models;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.Core.Exceptions;

namespace SecurityPatrol.API.UnitTests.Controllers
{
    public class PhotoControllerTests : TestBase
    {
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;

        public PhotoControllerTests()
        {
            // Initialize mock current user service
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockCurrentUserService.Setup(x => x.GetUserId()).Returns(TestConstants.TestUserId);
        }

        [Fact]
        public async Task Upload_WithValidRequest_ReturnsPhotoUploadResponse()
        {
            // Arrange
            var request = new PhotoUploadRequest
            {
                Timestamp = DateTime.UtcNow,
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude,
                UserId = TestConstants.TestUserId
            };

            var file = new Mock<IFormFile>();
            var ms = new MemoryStream();
            file.Setup(f => f.OpenReadStream()).Returns(ms);
            file.Setup(f => f.FileName).Returns("test.jpg");
            file.Setup(f => f.Length).Returns(ms.Length);

            var expectedResponse = new PhotoUploadResponse
            {
                Id = "1",
                Status = "Success"
            };

            MockPhotoService.Setup(s => s.UploadPhotoAsync(
                    It.IsAny<PhotoUploadRequest>(), 
                    It.IsAny<Stream>(), 
                    It.IsAny<string>()))
                .ReturnsAsync(Result<PhotoUploadResponse>.Success(expectedResponse));

            var controller = CreatePhotoController(_mockCurrentUserService.Object);
            SetupHttpContext(controller);

            // Act
            var result = await controller.Upload(request, file.Object);

            // Assert
            AssertActionResult<Result<PhotoUploadResponse>>(result.Result, Result<PhotoUploadResponse>.Success(expectedResponse));
            MockPhotoService.Verify(s => s.UploadPhotoAsync(
                It.IsAny<PhotoUploadRequest>(), 
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
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude,
                UserId = TestConstants.TestUserId
            };

            var controller = CreatePhotoController(_mockCurrentUserService.Object);
            SetupHttpContext(controller);

            // Act
            var result = await controller.Upload(request, null);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
            MockPhotoService.Verify(s => s.UploadPhotoAsync(
                It.IsAny<PhotoUploadRequest>(), 
                It.IsAny<Stream>(), 
                It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Upload_WithInvalidRequest_ThrowsValidationException()
        {
            // Arrange
            var request = new PhotoUploadRequest(); // Missing required fields

            var file = new Mock<IFormFile>();
            var ms = new MemoryStream();
            file.Setup(f => f.OpenReadStream()).Returns(ms);
            file.Setup(f => f.FileName).Returns("test.jpg");
            file.Setup(f => f.Length).Returns(ms.Length);

            MockPhotoService.Setup(s => s.UploadPhotoAsync(
                    It.IsAny<PhotoUploadRequest>(), 
                    It.IsAny<Stream>(), 
                    It.IsAny<string>()))
                .ThrowsAsync(new ValidationException("Invalid request"));

            var controller = CreatePhotoController(_mockCurrentUserService.Object);
            SetupHttpContext(controller);

            // Act & Assert
            await AssertExceptionAsync<ValidationException>(async () => 
                await controller.Upload(request, file.Object));

            MockPhotoService.Verify(s => s.UploadPhotoAsync(
                It.IsAny<PhotoUploadRequest>(), 
                It.IsAny<Stream>(), 
                It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task GetById_WithValidId_ReturnsPhoto()
        {
            // Arrange
            int photoId = 1;
            var expectedPhoto = new Photo
            {
                Id = photoId,
                UserId = TestConstants.TestUserId,
                Timestamp = DateTime.UtcNow,
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude,
                FilePath = "photos/test.jpg"
            };

            MockPhotoService.Setup(s => s.GetPhotoAsync(photoId))
                .ReturnsAsync(expectedPhoto);

            var controller = CreatePhotoController(_mockCurrentUserService.Object);
            SetupHttpContext(controller);

            // Act
            var result = await controller.GetById(photoId);

            // Assert
            AssertActionResult<Result<Photo>>(result.Result, Result<Photo>.Success(expectedPhoto));
            MockPhotoService.Verify(s => s.GetPhotoAsync(photoId), Times.Once);
        }

        [Fact]
        public async Task GetById_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            int photoId = 999;

            MockPhotoService.Setup(s => s.GetPhotoAsync(photoId))
                .ThrowsAsync(new NotFoundException($"Photo with ID {photoId} not found"));

            var controller = CreatePhotoController(_mockCurrentUserService.Object);
            SetupHttpContext(controller);

            // Act & Assert
            await AssertExceptionAsync<NotFoundException>(async () => 
                await controller.GetById(photoId));

            MockPhotoService.Verify(s => s.GetPhotoAsync(photoId), Times.Once);
        }

        [Fact]
        public async Task GetPhotoFile_WithValidId_ReturnsFileStreamResult()
        {
            // Arrange
            int photoId = 1;
            var memoryStream = new MemoryStream();
            string contentType = "image/jpeg";
            string fileName = "test.jpg";

            MockPhotoService.Setup(s => s.GetPhotoStreamAsync(photoId))
                .ReturnsAsync((memoryStream, contentType, fileName));

            var controller = CreatePhotoController(_mockCurrentUserService.Object);
            SetupHttpContext(controller);

            // Act
            var result = await controller.GetPhotoFile(photoId);

            // Assert
            result.Should().BeOfType<FileStreamResult>();
            var fileResult = result as FileStreamResult;
            fileResult.ContentType.Should().Be(contentType);
            fileResult.FileDownloadName.Should().Be(fileName);
            
            MockPhotoService.Verify(s => s.GetPhotoStreamAsync(photoId), Times.Once);
        }

        [Fact]
        public async Task GetPhotoFile_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            int photoId = 999;

            MockPhotoService.Setup(s => s.GetPhotoStreamAsync(photoId))
                .ThrowsAsync(new NotFoundException($"Photo with ID {photoId} not found"));

            var controller = CreatePhotoController(_mockCurrentUserService.Object);
            SetupHttpContext(controller);

            // Act & Assert
            await AssertExceptionAsync<NotFoundException>(async () => 
                await controller.GetPhotoFile(photoId));

            MockPhotoService.Verify(s => s.GetPhotoStreamAsync(photoId), Times.Once);
        }

        [Fact]
        public async Task GetMyPhotos_ReturnsUserPhotos()
        {
            // Arrange
            var photos = new List<Photo>
            {
                new Photo { Id = 1, UserId = TestConstants.TestUserId },
                new Photo { Id = 2, UserId = TestConstants.TestUserId }
            };

            MockPhotoService.Setup(s => s.GetPhotosByUserIdAsync(TestConstants.TestUserId))
                .ReturnsAsync(photos);

            var controller = CreatePhotoController(_mockCurrentUserService.Object);
            SetupHttpContext(controller);

            // Act
            var result = await controller.GetMyPhotos();

            // Assert
            AssertActionResult<Result<IEnumerable<Photo>>>(result.Result, Result<IEnumerable<Photo>>.Success(photos));
            _mockCurrentUserService.Verify(s => s.GetUserId(), Times.AtLeastOnce);
            MockPhotoService.Verify(s => s.GetPhotosByUserIdAsync(TestConstants.TestUserId), Times.Once);
        }

        [Fact]
        public async Task GetMyPhotosPaginated_ReturnsPaginatedUserPhotos()
        {
            // Arrange
            int pageNumber = 1;
            int pageSize = 10;
            var photos = new List<Photo>
            {
                new Photo { Id = 1, UserId = TestConstants.TestUserId },
                new Photo { Id = 2, UserId = TestConstants.TestUserId }
            };
            var paginatedPhotos = new PaginatedList<Photo>(photos, photos.Count, pageNumber, pageSize);

            MockPhotoService.Setup(s => s.GetPaginatedPhotosByUserIdAsync(TestConstants.TestUserId, pageNumber, pageSize))
                .ReturnsAsync(paginatedPhotos);

            var controller = CreatePhotoController(_mockCurrentUserService.Object);
            SetupHttpContext(controller);

            // Act
            var result = await controller.GetMyPhotosPaginated(pageNumber, pageSize);

            // Assert
            AssertActionResult<Result<PaginatedList<Photo>>>(result.Result, Result<PaginatedList<Photo>>.Success(paginatedPhotos));
            _mockCurrentUserService.Verify(s => s.GetUserId(), Times.AtLeastOnce);
            MockPhotoService.Verify(s => s.GetPaginatedPhotosByUserIdAsync(TestConstants.TestUserId, pageNumber, pageSize), Times.Once);
        }

        [Fact]
        public async Task GetPhotosByLocation_WithValidParameters_ReturnsPhotos()
        {
            // Arrange
            double latitude = TestConstants.TestLatitude;
            double longitude = TestConstants.TestLongitude;
            double radiusInMeters = 100.0;
            var photos = new List<Photo>
            {
                new Photo { Id = 1, Latitude = latitude, Longitude = longitude },
                new Photo { Id = 2, Latitude = latitude + 0.001, Longitude = longitude - 0.001 }
            };

            MockPhotoService.Setup(s => s.GetPhotosByLocationAsync(latitude, longitude, radiusInMeters))
                .ReturnsAsync(photos);

            var controller = CreatePhotoController(_mockCurrentUserService.Object);
            SetupHttpContext(controller);

            // Act
            var result = await controller.GetPhotosByLocation(latitude, longitude, radiusInMeters);

            // Assert
            AssertActionResult<Result<IEnumerable<Photo>>>(result.Result, Result<IEnumerable<Photo>>.Success(photos));
            MockPhotoService.Verify(s => s.GetPhotosByLocationAsync(latitude, longitude, radiusInMeters), Times.Once);
        }

        [Fact]
        public async Task GetPhotosByLocation_WithInvalidParameters_ThrowsValidationException()
        {
            // Arrange
            double latitude = TestConstants.TestLatitude;
            double longitude = TestConstants.TestLongitude;
            double radiusInMeters = -10.0;  // Invalid radius

            MockPhotoService.Setup(s => s.GetPhotosByLocationAsync(latitude, longitude, radiusInMeters))
                .ThrowsAsync(new ValidationException("Invalid radius"));

            var controller = CreatePhotoController(_mockCurrentUserService.Object);
            SetupHttpContext(controller);

            // Act & Assert
            await AssertExceptionAsync<ValidationException>(async () => 
                await controller.GetPhotosByLocation(latitude, longitude, radiusInMeters));

            MockPhotoService.Verify(s => s.GetPhotosByLocationAsync(latitude, longitude, radiusInMeters), Times.Once);
        }

        [Fact]
        public async Task GetPhotosByDateRange_WithValidDates_ReturnsPhotos()
        {
            // Arrange
            DateTime startDate = DateTime.UtcNow.AddDays(-7);
            DateTime endDate = DateTime.UtcNow;
            var photos = new List<Photo>
            {
                new Photo { Id = 1, Timestamp = DateTime.UtcNow.AddDays(-5) },
                new Photo { Id = 2, Timestamp = DateTime.UtcNow.AddDays(-2) }
            };

            MockPhotoService.Setup(s => s.GetPhotosByDateRangeAsync(startDate, endDate))
                .ReturnsAsync(photos);

            var controller = CreatePhotoController(_mockCurrentUserService.Object);
            SetupHttpContext(controller);

            // Act
            var result = await controller.GetPhotosByDateRange(startDate, endDate);

            // Assert
            AssertActionResult<Result<IEnumerable<Photo>>>(result.Result, Result<IEnumerable<Photo>>.Success(photos));
            MockPhotoService.Verify(s => s.GetPhotosByDateRangeAsync(startDate, endDate), Times.Once);
        }

        [Fact]
        public async Task GetPhotosByDateRange_WithInvalidDates_ThrowsValidationException()
        {
            // Arrange
            DateTime startDate = DateTime.UtcNow;
            DateTime endDate = DateTime.UtcNow.AddDays(-7);  // End date before start date

            MockPhotoService.Setup(s => s.GetPhotosByDateRangeAsync(startDate, endDate))
                .ThrowsAsync(new ValidationException("End date must be after start date"));

            var controller = CreatePhotoController(_mockCurrentUserService.Object);
            SetupHttpContext(controller);

            // Act & Assert
            await AssertExceptionAsync<ValidationException>(async () => 
                await controller.GetPhotosByDateRange(startDate, endDate));

            MockPhotoService.Verify(s => s.GetPhotosByDateRangeAsync(startDate, endDate), Times.Once);
        }

        [Fact]
        public async Task DeletePhoto_WithValidId_ReturnsSuccess()
        {
            // Arrange
            int photoId = 1;

            MockPhotoService.Setup(s => s.DeletePhotoAsync(photoId))
                .ReturnsAsync(true);

            var controller = CreatePhotoController(_mockCurrentUserService.Object);
            SetupHttpContext(controller);

            // Act
            var result = await controller.DeletePhoto(photoId);

            // Assert
            AssertActionResult<Result>(result.Result, Result.Success());
            MockPhotoService.Verify(s => s.DeletePhotoAsync(photoId), Times.Once);
        }

        [Fact]
        public async Task DeletePhoto_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            int photoId = 999;

            MockPhotoService.Setup(s => s.DeletePhotoAsync(photoId))
                .ThrowsAsync(new NotFoundException($"Photo with ID {photoId} not found"));

            var controller = CreatePhotoController(_mockCurrentUserService.Object);
            SetupHttpContext(controller);

            // Act & Assert
            await AssertExceptionAsync<NotFoundException>(async () => 
                await controller.DeletePhoto(photoId));

            MockPhotoService.Verify(s => s.DeletePhotoAsync(photoId), Times.Once);
        }
    }
}