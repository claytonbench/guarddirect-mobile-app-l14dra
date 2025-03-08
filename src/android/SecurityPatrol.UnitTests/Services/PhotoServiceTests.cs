using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SecurityPatrol.Helpers;
using SecurityPatrol.Models;
using SecurityPatrol.Services;
using SecurityPatrol.UnitTests.Helpers;
using Xunit;

namespace SecurityPatrol.UnitTests.Services
{
    public class PhotoServiceTests
    {
        private Mock<ILogger<PhotoService>> _mockLogger;
        private Mock<IPhotoRepository> _mockPhotoRepository;
        private Mock<IPhotoSyncService> _mockPhotoSyncService;
        private Mock<ILocationService> _mockLocationService;
        private Mock<IAuthenticationStateProvider> _mockAuthStateProvider;
        private Mock<CameraHelper> _mockCameraHelper;
        private Mock<ImageCompressor> _mockImageCompressor;
        private Mock<INetworkService> _mockNetworkService;
        private PhotoService _photoService;

        private void Setup()
        {
            // Initialize all mock objects
            _mockLogger = new Mock<ILogger<PhotoService>>();
            _mockPhotoRepository = new Mock<IPhotoRepository>();
            _mockPhotoSyncService = new Mock<IPhotoSyncService>();
            _mockLocationService = new Mock<ILocationService>();
            _mockAuthStateProvider = new Mock<IAuthenticationStateProvider>();
            _mockCameraHelper = new Mock<CameraHelper>();
            _mockImageCompressor = new Mock<ImageCompressor>();
            _mockNetworkService = new Mock<INetworkService>();

            // Create an instance of PhotoService with the mock dependencies
            _photoService = new PhotoService(
                _mockLogger.Object,
                _mockPhotoRepository.Object,
                _mockPhotoSyncService.Object,
                _mockLocationService.Object,
                _mockAuthStateProvider.Object,
                _mockCameraHelper.Object,
                _mockImageCompressor.Object,
                _mockNetworkService.Object);
        }

        [Fact]
        public async Task CapturePhotoAsync_WhenCameraPermissionGranted_ReturnsPhotoModel()
        {
            // Arrange: Set up mock camera helper to return true for permission check
            Setup();
            _mockCameraHelper.Setup(x => x.CheckCameraPermissionAsync()).ReturnsAsync(true);
            
            // Arrange: Set up mock camera helper to return a valid photo stream
            var photoStream = new MemoryStream();
            _mockCameraHelper.Setup(x => x.CapturePhotoAsync()).ReturnsAsync(photoStream);
            
            // Arrange: Set up mock image compressor to return a compressed stream
            var compressedStream = new MemoryStream();
            _mockImageCompressor.Setup(x => x.CompressImageAsync(It.IsAny<Stream>(), null, null, null))
                .ReturnsAsync(compressedStream);
            
            // Arrange: Set up mock auth state provider to return an authenticated state
            var authState = TestDataGenerator.CreateAuthState(true);
            _mockAuthStateProvider.Setup(x => x.GetCurrentState()).ReturnsAsync(authState);
            
            // Arrange: Set up mock location service to return a valid location
            var location = new LocationModel { Latitude = 34.0522, Longitude = -118.2437 };
            _mockLocationService.Setup(x => x.GetCurrentLocation()).ReturnsAsync(location);
            
            // Arrange: Set up mock photo repository to return a valid photo ID
            string expectedPhotoId = Guid.NewGuid().ToString();
            _mockPhotoRepository.Setup(x => x.SavePhotoAsync(It.IsAny<PhotoModel>(), It.IsAny<Stream>()))
                .ReturnsAsync(expectedPhotoId);
            
            // Arrange: Set up mock network service to indicate connected status
            _mockNetworkService.Setup(x => x.IsConnected).Returns(true);
            
            // Act: Call _photoService.CapturePhotoAsync()
            var result = await _photoService.CapturePhotoAsync();
            
            // Assert: Verify result is not null
            result.Should().NotBeNull();
            result.Id.Should().Be(expectedPhotoId);
            result.UserId.Should().Be(authState.PhoneNumber);
            result.Latitude.Should().Be(location.Latitude);
            result.Longitude.Should().Be(location.Longitude);
            result.IsSynced.Should().BeFalse();
            
            // Assert: Verify all expected methods were called with correct parameters
            _mockCameraHelper.Verify(x => x.CheckCameraPermissionAsync(), Times.Once);
            _mockCameraHelper.Verify(x => x.CapturePhotoAsync(), Times.Once);
            _mockImageCompressor.Verify(x => x.CompressImageAsync(It.IsAny<Stream>(), null, null, null), Times.Once);
            _mockAuthStateProvider.Verify(x => x.GetCurrentState(), Times.Once);
            _mockLocationService.Verify(x => x.GetCurrentLocation(), Times.Once);
            _mockPhotoRepository.Verify(x => x.SavePhotoAsync(It.IsAny<PhotoModel>(), It.IsAny<Stream>()), Times.Once);
            _mockPhotoSyncService.Verify(x => x.UploadPhotoAsync(expectedPhotoId), Times.Once);
        }

        [Fact]
        public async Task CapturePhotoAsync_WhenCameraPermissionDenied_ReturnsNull()
        {
            // Arrange: Set up mock camera helper to return false for permission check
            Setup();
            _mockCameraHelper.Setup(x => x.CheckCameraPermissionAsync()).ReturnsAsync(false);
            
            // Arrange: Set up mock camera helper to return false for permission request
            _mockCameraHelper.Setup(x => x.RequestCameraPermissionAsync()).ReturnsAsync(false);
            
            // Act: Call _photoService.CapturePhotoAsync()
            var result = await _photoService.CapturePhotoAsync();
            
            // Assert: Verify result is null
            result.Should().BeNull();
            
            // Assert: Verify permission check was called
            _mockCameraHelper.Verify(x => x.CheckCameraPermissionAsync(), Times.Once);
            
            // Assert: Verify permission request was called
            _mockCameraHelper.Verify(x => x.RequestCameraPermissionAsync(), Times.Once);
            
            // Assert: Verify camera capture was not called
            _mockCameraHelper.Verify(x => x.CapturePhotoAsync(), Times.Never);
        }

        [Fact]
        public async Task CapturePhotoAsync_WhenCameraCaptureFails_ReturnsNull()
        {
            // Arrange: Set up mock camera helper to return true for permission check
            Setup();
            _mockCameraHelper.Setup(x => x.CheckCameraPermissionAsync()).ReturnsAsync(true);
            
            // Arrange: Set up mock camera helper to return null for photo capture
            _mockCameraHelper.Setup(x => x.CapturePhotoAsync()).ReturnsAsync((Stream)null);
            
            // Act: Call _photoService.CapturePhotoAsync()
            var result = await _photoService.CapturePhotoAsync();
            
            // Assert: Verify result is null
            result.Should().BeNull();
            
            // Assert: Verify permission check was called
            _mockCameraHelper.Verify(x => x.CheckCameraPermissionAsync(), Times.Once);
            
            // Assert: Verify camera capture was called
            _mockCameraHelper.Verify(x => x.CapturePhotoAsync(), Times.Once);
            
            // Assert: Verify repository save was not called
            _mockPhotoRepository.Verify(x => x.SavePhotoAsync(It.IsAny<PhotoModel>(), It.IsAny<Stream>()), Times.Never);
        }

        [Fact]
        public async Task CapturePhotoAsync_WhenRepositorySaveFails_ReturnsNull()
        {
            // Arrange: Set up mock camera helper to return true for permission check
            Setup();
            _mockCameraHelper.Setup(x => x.CheckCameraPermissionAsync()).ReturnsAsync(true);
            
            // Arrange: Set up mock camera helper to return a valid photo stream
            var photoStream = new MemoryStream();
            _mockCameraHelper.Setup(x => x.CapturePhotoAsync()).ReturnsAsync(photoStream);
            
            // Arrange: Set up mock image compressor to return a compressed stream
            var compressedStream = new MemoryStream();
            _mockImageCompressor.Setup(x => x.CompressImageAsync(It.IsAny<Stream>(), null, null, null))
                .ReturnsAsync(compressedStream);
            
            // Arrange: Set up mock auth state provider to return an authenticated state
            var authState = TestDataGenerator.CreateAuthState(true);
            _mockAuthStateProvider.Setup(x => x.GetCurrentState()).ReturnsAsync(authState);
            
            // Arrange: Set up mock location service to return a valid location
            var location = new LocationModel { Latitude = 34.0522, Longitude = -118.2437 };
            _mockLocationService.Setup(x => x.GetCurrentLocation()).ReturnsAsync(location);
            
            // Arrange: Set up mock photo repository to throw an exception on save
            _mockPhotoRepository.Setup(x => x.SavePhotoAsync(It.IsAny<PhotoModel>(), It.IsAny<Stream>()))
                .ThrowsAsync(new Exception("Save failed"));
            
            // Act: Call _photoService.CapturePhotoAsync()
            var result = await _photoService.CapturePhotoAsync();
            
            // Assert: Verify result is null
            result.Should().BeNull();
            
            // Assert: Verify repository save was called
            _mockPhotoRepository.Verify(x => x.SavePhotoAsync(It.IsAny<PhotoModel>(), It.IsAny<Stream>()), Times.Once);
            
            // Assert: Verify logger logged the error
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }

        [Fact]
        public async Task CapturePhotoAsync_WhenNetworkAvailable_TriggersPhotoUpload()
        {
            // Arrange: Set up mock camera helper to return true for permission check
            Setup();
            _mockCameraHelper.Setup(x => x.CheckCameraPermissionAsync()).ReturnsAsync(true);
            
            // Arrange: Set up mock camera helper to return a valid photo stream
            var photoStream = new MemoryStream();
            _mockCameraHelper.Setup(x => x.CapturePhotoAsync()).ReturnsAsync(photoStream);
            
            // Arrange: Set up mock image compressor to return a compressed stream
            var compressedStream = new MemoryStream();
            _mockImageCompressor.Setup(x => x.CompressImageAsync(It.IsAny<Stream>(), null, null, null))
                .ReturnsAsync(compressedStream);
            
            // Arrange: Set up mock auth state provider to return an authenticated state
            var authState = TestDataGenerator.CreateAuthState(true);
            _mockAuthStateProvider.Setup(x => x.GetCurrentState()).ReturnsAsync(authState);
            
            // Arrange: Set up mock location service to return a valid location
            var location = new LocationModel { Latitude = 34.0522, Longitude = -118.2437 };
            _mockLocationService.Setup(x => x.GetCurrentLocation()).ReturnsAsync(location);
            
            // Arrange: Set up mock photo repository to return a valid photo ID
            string expectedPhotoId = Guid.NewGuid().ToString();
            _mockPhotoRepository.Setup(x => x.SavePhotoAsync(It.IsAny<PhotoModel>(), It.IsAny<Stream>()))
                .ReturnsAsync(expectedPhotoId);
            
            // Arrange: Set up mock network service to indicate connected status
            _mockNetworkService.Setup(x => x.IsConnected).Returns(true);
            
            // Act: Call _photoService.CapturePhotoAsync()
            var result = await _photoService.CapturePhotoAsync();
            
            // Assert: Verify result is not null
            result.Should().NotBeNull();
            
            // Assert: Verify photo sync service upload method was called with correct ID
            _mockPhotoSyncService.Verify(x => x.UploadPhotoAsync(expectedPhotoId), Times.Once);
        }

        [Fact]
        public async Task CapturePhotoAsync_WhenNetworkUnavailable_DoesNotTriggerPhotoUpload()
        {
            // Arrange: Set up mock camera helper to return true for permission check
            Setup();
            _mockCameraHelper.Setup(x => x.CheckCameraPermissionAsync()).ReturnsAsync(true);
            
            // Arrange: Set up mock camera helper to return a valid photo stream
            var photoStream = new MemoryStream();
            _mockCameraHelper.Setup(x => x.CapturePhotoAsync()).ReturnsAsync(photoStream);
            
            // Arrange: Set up mock image compressor to return a compressed stream
            var compressedStream = new MemoryStream();
            _mockImageCompressor.Setup(x => x.CompressImageAsync(It.IsAny<Stream>(), null, null, null))
                .ReturnsAsync(compressedStream);
            
            // Arrange: Set up mock auth state provider to return an authenticated state
            var authState = TestDataGenerator.CreateAuthState(true);
            _mockAuthStateProvider.Setup(x => x.GetCurrentState()).ReturnsAsync(authState);
            
            // Arrange: Set up mock location service to return a valid location
            var location = new LocationModel { Latitude = 34.0522, Longitude = -118.2437 };
            _mockLocationService.Setup(x => x.GetCurrentLocation()).ReturnsAsync(location);
            
            // Arrange: Set up mock photo repository to return a valid photo ID
            string expectedPhotoId = Guid.NewGuid().ToString();
            _mockPhotoRepository.Setup(x => x.SavePhotoAsync(It.IsAny<PhotoModel>(), It.IsAny<Stream>()))
                .ReturnsAsync(expectedPhotoId);
            
            // Arrange: Set up mock network service to indicate disconnected status
            _mockNetworkService.Setup(x => x.IsConnected).Returns(false);
            
            // Act: Call _photoService.CapturePhotoAsync()
            var result = await _photoService.CapturePhotoAsync();
            
            // Assert: Verify result is not null
            result.Should().NotBeNull();
            
            // Assert: Verify photo sync service upload method was not called
            _mockPhotoSyncService.Verify(x => x.UploadPhotoAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetStoredPhotosAsync_ReturnsPhotosFromRepository()
        {
            // Arrange: Create a list of test photo models
            Setup();
            var testPhotos = TestDataGenerator.CreatePhotoModels(3);
            
            // Arrange: Set up mock photo repository to return the test photos
            _mockPhotoRepository.Setup(x => x.GetPhotosAsync()).ReturnsAsync(testPhotos);
            
            // Act: Call _photoService.GetStoredPhotosAsync()
            var result = await _photoService.GetStoredPhotosAsync();
            
            // Assert: Verify result is not null
            result.Should().NotBeNull();
            
            // Assert: Verify result contains the expected number of photos
            result.Should().HaveCount(3);
            result.Should().BeEquivalentTo(testPhotos);
            
            // Assert: Verify repository GetPhotosAsync was called
            _mockPhotoRepository.Verify(x => x.GetPhotosAsync(), Times.Once);
        }

        [Fact]
        public async Task GetStoredPhotosAsync_WhenRepositoryThrowsException_ReturnsEmptyList()
        {
            // Arrange: Set up mock photo repository to throw an exception
            Setup();
            _mockPhotoRepository.Setup(x => x.GetPhotosAsync())
                .ThrowsAsync(new Exception("Repository error"));
            
            // Act: Call _photoService.GetStoredPhotosAsync()
            var result = await _photoService.GetStoredPhotosAsync();
            
            // Assert: Verify result is not null
            result.Should().NotBeNull();
            
            // Assert: Verify result is an empty list
            result.Should().BeEmpty();
            
            // Assert: Verify logger logged the error
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }

        [Fact]
        public async Task GetPhotoAsync_WithValidId_ReturnsPhoto()
        {
            // Arrange: Create a test photo model
            Setup();
            var photoId = "test-photo-id";
            var testPhoto = TestDataGenerator.CreatePhotoModel(id: photoId);
            
            // Arrange: Set up mock photo repository to return the test photo
            _mockPhotoRepository.Setup(x => x.GetPhotoByIdAsync(photoId)).ReturnsAsync(testPhoto);
            
            // Act: Call _photoService.GetPhotoAsync(photoId)
            var result = await _photoService.GetPhotoAsync(photoId);
            
            // Assert: Verify result is not null
            result.Should().NotBeNull();
            
            // Assert: Verify result has the expected ID
            result.Should().BeEquivalentTo(testPhoto);
            
            // Assert: Verify repository GetPhotoByIdAsync was called with correct ID
            _mockPhotoRepository.Verify(x => x.GetPhotoByIdAsync(photoId), Times.Once);
        }

        [Fact]
        public async Task GetPhotoAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange: Set up mock photo repository to return null
            Setup();
            var invalidId = "invalid-id";
            _mockPhotoRepository.Setup(x => x.GetPhotoByIdAsync(invalidId)).ReturnsAsync((PhotoModel)null);
            
            // Act: Call _photoService.GetPhotoAsync(invalidId)
            var result = await _photoService.GetPhotoAsync(invalidId);
            
            // Assert: Verify result is null
            result.Should().BeNull();
            
            // Assert: Verify repository GetPhotoByIdAsync was called with correct ID
            _mockPhotoRepository.Verify(x => x.GetPhotoByIdAsync(invalidId), Times.Once);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetPhotoAsync_WithNullOrEmptyId_ReturnsNull(string id)
        {
            // Arrange: No specific arrangement needed
            Setup();
            
            // Act: Call _photoService.GetPhotoAsync(id)
            var result = await _photoService.GetPhotoAsync(id);
            
            // Assert: Verify result is null
            result.Should().BeNull();
            
            // Assert: Verify repository GetPhotoByIdAsync was not called
            _mockPhotoRepository.Verify(x => x.GetPhotoByIdAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetPhotoFileAsync_WithValidId_ReturnsStream()
        {
            // Arrange: Create a test memory stream
            Setup();
            var photoId = "test-photo-id";
            var testStream = new MemoryStream();
            
            // Arrange: Set up mock photo repository to return the test stream
            _mockPhotoRepository.Setup(x => x.GetPhotoStreamAsync(photoId)).ReturnsAsync(testStream);
            
            // Act: Call _photoService.GetPhotoFileAsync(photoId)
            var result = await _photoService.GetPhotoFileAsync(photoId);
            
            // Assert: Verify result is not null
            result.Should().NotBeNull();
            
            // Assert: Verify repository GetPhotoStreamAsync was called with correct ID
            _mockPhotoRepository.Verify(x => x.GetPhotoStreamAsync(photoId), Times.Once);
        }

        [Fact]
        public async Task GetPhotoFileAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange: Set up mock photo repository to return null
            Setup();
            var invalidId = "invalid-id";
            _mockPhotoRepository.Setup(x => x.GetPhotoStreamAsync(invalidId)).ReturnsAsync((Stream)null);
            
            // Act: Call _photoService.GetPhotoFileAsync(invalidId)
            var result = await _photoService.GetPhotoFileAsync(invalidId);
            
            // Assert: Verify result is null
            result.Should().BeNull();
            
            // Assert: Verify repository GetPhotoStreamAsync was called with correct ID
            _mockPhotoRepository.Verify(x => x.GetPhotoStreamAsync(invalidId), Times.Once);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetPhotoFileAsync_WithNullOrEmptyId_ReturnsNull(string id)
        {
            // Arrange: No specific arrangement needed
            Setup();
            
            // Act: Call _photoService.GetPhotoFileAsync(id)
            var result = await _photoService.GetPhotoFileAsync(id);
            
            // Assert: Verify result is null
            result.Should().BeNull();
            
            // Assert: Verify repository GetPhotoStreamAsync was not called
            _mockPhotoRepository.Verify(x => x.GetPhotoStreamAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task DeletePhotoAsync_WithValidId_ReturnsTrue()
        {
            // Arrange: Set up mock photo repository to return true
            Setup();
            var photoId = "test-photo-id";
            _mockPhotoRepository.Setup(x => x.DeletePhotoAsync(photoId)).ReturnsAsync(true);
            
            // Act: Call _photoService.DeletePhotoAsync(photoId)
            var result = await _photoService.DeletePhotoAsync(photoId);
            
            // Assert: Verify result is true
            result.Should().BeTrue();
            
            // Assert: Verify repository DeletePhotoAsync was called with correct ID
            _mockPhotoRepository.Verify(x => x.DeletePhotoAsync(photoId), Times.Once);
        }

        [Fact]
        public async Task DeletePhotoAsync_WithInvalidId_ReturnsFalse()
        {
            // Arrange: Set up mock photo repository to return false
            Setup();
            var invalidId = "invalid-id";
            _mockPhotoRepository.Setup(x => x.DeletePhotoAsync(invalidId)).ReturnsAsync(false);
            
            // Act: Call _photoService.DeletePhotoAsync(invalidId)
            var result = await _photoService.DeletePhotoAsync(invalidId);
            
            // Assert: Verify result is false
            result.Should().BeFalse();
            
            // Assert: Verify repository DeletePhotoAsync was called with correct ID
            _mockPhotoRepository.Verify(x => x.DeletePhotoAsync(invalidId), Times.Once);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task DeletePhotoAsync_WithNullOrEmptyId_ReturnsFalse(string id)
        {
            // Arrange: No specific arrangement needed
            Setup();
            
            // Act: Call _photoService.DeletePhotoAsync(id)
            var result = await _photoService.DeletePhotoAsync(id);
            
            // Assert: Verify result is false
            result.Should().BeFalse();
            
            // Assert: Verify repository DeletePhotoAsync was not called
            _mockPhotoRepository.Verify(x => x.DeletePhotoAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task CleanupOldPhotosAsync_WithValidRetentionDays_ReturnsDeletedCount()
        {
            // Arrange: Set up mock photo repository to return a specific count
            Setup();
            int retentionDays = 30;
            int expectedCount = 5;
            _mockPhotoRepository.Setup(x => x.CleanupOldPhotosAsync(retentionDays)).ReturnsAsync(expectedCount);
            
            // Act: Call _photoService.CleanupOldPhotosAsync(30)
            var result = await _photoService.CleanupOldPhotosAsync(retentionDays);
            
            // Assert: Verify result equals the expected count
            result.Should().Be(expectedCount);
            
            // Assert: Verify repository CleanupOldPhotosAsync was called with correct retention days
            _mockPhotoRepository.Verify(x => x.CleanupOldPhotosAsync(retentionDays), Times.Once);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-30)]
        public async Task CleanupOldPhotosAsync_WithInvalidRetentionDays_ReturnsZero(int retentionDays)
        {
            // Arrange: No specific arrangement needed
            Setup();
            
            // Act: Call _photoService.CleanupOldPhotosAsync(retentionDays)
            var result = await _photoService.CleanupOldPhotosAsync(retentionDays);
            
            // Assert: Verify result is zero
            result.Should().Be(0);
            
            // Assert: Verify repository CleanupOldPhotosAsync was not called
            _mockPhotoRepository.Verify(x => x.CleanupOldPhotosAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetStorageUsageAsync_ReturnsStorageUsage()
        {
            // Arrange: Create a list of test photo models with file paths
            Setup();
            var testPhotos = TestDataGenerator.CreatePhotoModels(3);
            
            // Arrange: Set up mock photo repository to return the test photos
            _mockPhotoRepository.Setup(x => x.GetPhotosAsync()).ReturnsAsync(testPhotos);
            
            // Arrange: Set up mock file system to return specific file sizes
            var stream1 = new MemoryStream(new byte[1000]);
            var stream2 = new MemoryStream(new byte[2000]);
            var stream3 = new MemoryStream(new byte[3000]);
            
            _mockPhotoRepository.Setup(x => x.GetPhotoStreamAsync(testPhotos[0].Id)).ReturnsAsync(stream1);
            _mockPhotoRepository.Setup(x => x.GetPhotoStreamAsync(testPhotos[1].Id)).ReturnsAsync(stream2);
            _mockPhotoRepository.Setup(x => x.GetPhotoStreamAsync(testPhotos[2].Id)).ReturnsAsync(stream3);
            
            // Act: Call _photoService.GetStorageUsageAsync()
            var result = await _photoService.GetStorageUsageAsync();
            
            // Assert: Verify result equals the expected total size
            result.Should().Be(6000); // 1000 + 2000 + 3000
            
            // Assert: Verify repository GetPhotosAsync was called
            _mockPhotoRepository.Verify(x => x.GetPhotosAsync(), Times.Once);
            _mockPhotoRepository.Verify(x => x.GetPhotoStreamAsync(testPhotos[0].Id), Times.Once);
            _mockPhotoRepository.Verify(x => x.GetPhotoStreamAsync(testPhotos[1].Id), Times.Once);
            _mockPhotoRepository.Verify(x => x.GetPhotoStreamAsync(testPhotos[2].Id), Times.Once);
        }

        [Fact]
        public async Task GetStorageUsageAsync_WhenRepositoryThrowsException_ReturnsZero()
        {
            // Arrange: Set up mock photo repository to throw an exception
            Setup();
            _mockPhotoRepository.Setup(x => x.GetPhotosAsync())
                .ThrowsAsync(new Exception("Repository error"));
            
            // Act: Call _photoService.GetStorageUsageAsync()
            var result = await _photoService.GetStorageUsageAsync();
            
            // Assert: Verify result is zero
            result.Should().Be(0);
            
            // Assert: Verify logger logged the error
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }
    }
}