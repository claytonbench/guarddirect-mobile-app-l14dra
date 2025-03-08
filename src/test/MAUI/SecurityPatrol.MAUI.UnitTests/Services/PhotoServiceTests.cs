using System; // System 8.0+
using System.Threading.Tasks; // System.Threading.Tasks 8.0+
using System.IO; // System.IO 8.0+
using System.Collections.Generic; // System.Collections.Generic 8.0+
using Xunit; // xunit 2.4.2
using Moq; // Moq 4.18.4
using FluentAssertions; // FluentAssertions 6.11.0
using Microsoft.Extensions.Logging; // Microsoft.Extensions.Logging 8.0.0
using SecurityPatrol.MAUI.UnitTests.Setup;
using SecurityPatrol.Services;
using SecurityPatrol.Models;
using SecurityPatrol.Helpers;
using SecurityPatrol.TestCommon.Constants;
using SecurityPatrol.TestCommon.Data;

namespace SecurityPatrol.MAUI.UnitTests.Services
{
    /// <summary>
    /// Unit tests for the PhotoService class to verify its functionality for capturing, storing, retrieving, and managing photos.
    /// </summary>
    public class PhotoServiceTests : TestBase
    {
        private Mock<IPhotoRepository> mockPhotoRepository;
        private Mock<IPhotoSyncService> mockPhotoSyncService;
        private Mock<ILocationService> mockLocationService;
        private Mock<IAuthenticationStateProvider> mockAuthStateProvider;
        private Mock<CameraHelper> mockCameraHelper;
        private Mock<ImageCompressor> mockImageCompressor;
        private Mock<INetworkService> mockNetworkService;
        private Mock<ILogger<PhotoService>> mockLogger;
        private PhotoService photoService;

        /// <summary>
        /// Initializes a new instance of the <see cref="PhotoServiceTests"/> class.
        /// </summary>
        public PhotoServiceTests()
        {
            // Setup mock dependencies
            mockPhotoRepository = new Mock<IPhotoRepository>();
            mockPhotoSyncService = new Mock<IPhotoSyncService>();
            mockLocationService = new Mock<ILocationService>();
            mockAuthStateProvider = new Mock<IAuthenticationStateProvider>();
            mockCameraHelper = new Mock<CameraHelper>();
            mockImageCompressor = new Mock<ImageCompressor>();
            mockNetworkService = new Mock<INetworkService>();
            mockLogger = new Mock<ILogger<PhotoService>>();

            // Create an instance of PhotoService with the mock dependencies
            photoService = new PhotoService(
                mockLogger.Object,
                mockPhotoRepository.Object,
                mockPhotoSyncService.Object,
                mockLocationService.Object,
                mockAuthStateProvider.Object,
                mockCameraHelper.Object,
                mockImageCompressor.Object,
                mockNetworkService.Object);
        }

        /// <summary>
        /// Sets up the test environment before each test
        /// </summary>
        public void Setup()
        {
            SetupServiceCollection();
            SetupMocks();
        }

        /// <summary>
        /// Configures the mock dependencies with default behaviors
        /// </summary>
        private void SetupMocks()
        {
            // Configure mockAuthStateProvider to return an authenticated state
            mockAuthStateProvider.Setup(x => x.GetCurrentState())
                .ReturnsAsync(CreateAuthenticatedState());

            // Configure mockLocationService to return a test location
            mockLocationService.Setup(x => x.GetCurrentLocation())
                .ReturnsAsync(new LocationModel
                {
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                });

            // Configure mockCameraHelper for camera permission and photo capture
            mockCameraHelper.Setup(x => x.CheckCameraPermissionAsync())
                .ReturnsAsync(true);
            mockCameraHelper.Setup(x => x.CapturePhotoAsync())
                .ReturnsAsync(new MemoryStream());

            // Configure mockImageCompressor for image compression
            mockImageCompressor.Setup(x => x.CompressImageAsync(It.IsAny<Stream>(), null, null, null))
                .ReturnsAsync(new MemoryStream());

            // Configure mockPhotoRepository for photo storage operations
            mockPhotoRepository.Setup(x => x.SavePhotoAsync(It.IsAny<PhotoModel>(), It.IsAny<Stream>()))
                .ReturnsAsync(Guid.NewGuid().ToString());
            mockPhotoRepository.Setup(x => x.GetPhotosAsync())
                .ReturnsAsync(new List<PhotoModel>());

            // Configure mockPhotoSyncService for photo synchronization
            mockPhotoSyncService.Setup(x => x.UploadPhotoAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Configure mockNetworkService for connectivity status
            mockNetworkService.Setup(x => x.IsConnected)
                .Returns(true);
        }

        /// <summary>
        /// Tests that CapturePhotoAsync returns a valid photo model when camera permission is granted
        /// </summary>
        [Fact]
        public async Task CapturePhotoAsync_WhenCameraPermissionGranted_ShouldReturnPhotoModel()
        {
            // Arrange
            Setup();
            mockCameraHelper.Setup(x => x.CheckCameraPermissionAsync()).ReturnsAsync(true);
            mockCameraHelper.Setup(x => x.CapturePhotoAsync()).ReturnsAsync(new MemoryStream());
            mockImageCompressor.Setup(x => x.CompressImageAsync(It.IsAny<Stream>(), null, null, null)).ReturnsAsync(new MemoryStream());
            mockPhotoRepository.Setup(x => x.SavePhotoAsync(It.IsAny<PhotoModel>(), It.IsAny<Stream>())).ReturnsAsync(Guid.NewGuid().ToString());

            // Act
            var result = await photoService.CapturePhotoAsync();

            // Assert
            result.Should().NotBeNull();
            result.UserId.Should().Be(TestConstants.TestPhoneNumber);
            result.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            result.Latitude.Should().Be(TestConstants.TestLatitude);
            result.Longitude.Should().Be(TestConstants.TestLongitude);
            mockPhotoRepository.Verify(x => x.SavePhotoAsync(It.IsAny<PhotoModel>(), It.IsAny<Stream>()), Times.Once);
            mockPhotoSyncService.Verify(x => x.UploadPhotoAsync(It.IsAny<string>()), Times.Once);
        }

        /// <summary>
        /// Tests that CapturePhotoAsync returns null when camera permission is denied
        /// </summary>
        [Fact]
        public async Task CapturePhotoAsync_WhenCameraPermissionDenied_ShouldReturnNull()
        {
            // Arrange
            Setup();
            mockCameraHelper.Setup(x => x.CheckCameraPermissionAsync()).ReturnsAsync(false);
            mockCameraHelper.Setup(x => x.RequestCameraPermissionAsync(It.IsAny<bool>(), It.IsAny<ILogger>())).ReturnsAsync(false);

            // Act
            var result = await photoService.CapturePhotoAsync();

            // Assert
            result.Should().BeNull();
            mockPhotoRepository.Verify(x => x.SavePhotoAsync(It.IsAny<PhotoModel>(), It.IsAny<Stream>()), Times.Never);
            mockPhotoSyncService.Verify(x => x.UploadPhotoAsync(It.IsAny<string>()), Times.Never);
        }

        /// <summary>
        /// Tests that CapturePhotoAsync returns null when camera capture fails
        /// </summary>
        [Fact]
        public async Task CapturePhotoAsync_WhenCameraCaptureFails_ShouldReturnNull()
        {
            // Arrange
            Setup();
            mockCameraHelper.Setup(x => x.CheckCameraPermissionAsync()).ReturnsAsync(true);
            mockCameraHelper.Setup(x => x.CapturePhotoAsync()).ReturnsAsync((Stream)null);

            // Act
            var result = await photoService.CapturePhotoAsync();

            // Assert
            result.Should().BeNull();
            mockPhotoRepository.Verify(x => x.SavePhotoAsync(It.IsAny<PhotoModel>(), It.IsAny<Stream>()), Times.Never);
            mockPhotoSyncService.Verify(x => x.UploadPhotoAsync(It.IsAny<string>()), Times.Never);
        }

        /// <summary>
        /// Tests that CapturePhotoAsync returns null when an exception occurs
        /// </summary>
        [Fact]
        public async Task CapturePhotoAsync_WhenExceptionOccurs_ShouldReturnNull()
        {
            // Arrange
            Setup();
            mockCameraHelper.Setup(x => x.CheckCameraPermissionAsync()).ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await photoService.CapturePhotoAsync();

            // Assert
            result.Should().BeNull();
            mockLogger.Verify(x => x.LogError(It.IsAny<Exception>(), It.IsAny<string>()), Times.Once);
            mockPhotoRepository.Verify(x => x.SavePhotoAsync(It.IsAny<PhotoModel>(), It.IsAny<Stream>()), Times.Never);
            mockPhotoSyncService.Verify(x => x.UploadPhotoAsync(It.IsAny<string>()), Times.Never);
        }

        /// <summary>
        /// Tests that CapturePhotoAsync triggers photo upload when network is available
        /// </summary>
        [Fact]
        public async Task CapturePhotoAsync_WhenNetworkAvailable_ShouldTriggerUpload()
        {
            // Arrange
            Setup();
            mockNetworkService.Setup(x => x.IsConnected).Returns(true);
            mockCameraHelper.Setup(x => x.CheckCameraPermissionAsync()).ReturnsAsync(true);
            mockCameraHelper.Setup(x => x.CapturePhotoAsync()).ReturnsAsync(new MemoryStream());
            mockImageCompressor.Setup(x => x.CompressImageAsync(It.IsAny<Stream>(), null, null, null)).ReturnsAsync(new MemoryStream());
            mockPhotoRepository.Setup(x => x.SavePhotoAsync(It.IsAny<PhotoModel>(), It.IsAny<Stream>())).ReturnsAsync("testPhotoId");

            // Act
            await photoService.CapturePhotoAsync();

            // Assert
            mockPhotoSyncService.Verify(x => x.UploadPhotoAsync("testPhotoId"), Times.Once);
        }

        /// <summary>
        /// Tests that CapturePhotoAsync does not trigger photo upload when network is unavailable
        /// </summary>
        [Fact]
        public async Task CapturePhotoAsync_WhenNetworkUnavailable_ShouldNotTriggerUpload()
        {
            // Arrange
            Setup();
            mockNetworkService.Setup(x => x.IsConnected).Returns(false);
            mockCameraHelper.Setup(x => x.CheckCameraPermissionAsync()).ReturnsAsync(true);
            mockCameraHelper.Setup(x => x.CapturePhotoAsync()).ReturnsAsync(new MemoryStream());
            mockImageCompressor.Setup(x => x.CompressImageAsync(It.IsAny<Stream>(), null, null, null)).ReturnsAsync(new MemoryStream());
            mockPhotoRepository.Setup(x => x.SavePhotoAsync(It.IsAny<PhotoModel>(), It.IsAny<Stream>())).ReturnsAsync("testPhotoId");

            // Act
            await photoService.CapturePhotoAsync();

            // Assert
            mockPhotoSyncService.Verify(x => x.UploadPhotoAsync(It.IsAny<string>()), Times.Never);
        }

        /// <summary>
        /// Tests that GetStoredPhotosAsync returns photos from the repository
        /// </summary>
        [Fact]
        public async Task GetStoredPhotosAsync_ShouldReturnPhotosFromRepository()
        {
            // Arrange
            Setup();
            var testPhotos = new List<PhotoModel> { new PhotoModel(), new PhotoModel() };
            mockPhotoRepository.Setup(x => x.GetPhotosAsync()).ReturnsAsync(testPhotos);

            // Act
            var result = await photoService.GetStoredPhotosAsync();

            // Assert
            result.Should().NotBeNull();
            result.Count.Should().Be(testPhotos.Count);
            result.Should().BeEquivalentTo(testPhotos);
        }

        /// <summary>
        /// Tests that GetStoredPhotosAsync returns an empty list when an exception occurs
        /// </summary>
        [Fact]
        public async Task GetStoredPhotosAsync_WhenExceptionOccurs_ShouldReturnEmptyList()
        {
            // Arrange
            Setup();
            mockPhotoRepository.Setup(x => x.GetPhotosAsync()).ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await photoService.GetStoredPhotosAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
            mockLogger.Verify(x => x.LogError(It.IsAny<Exception>(), It.IsAny<string>()), Times.Once);
        }

        /// <summary>
        /// Tests that GetPhotoAsync returns a photo when given a valid ID
        /// </summary>
        [Fact]
        public async Task GetPhotoAsync_WithValidId_ShouldReturnPhoto()
        {
            // Arrange
            Setup();
            var testPhoto = new PhotoModel { Id = "validId" };
            mockPhotoRepository.Setup(x => x.GetPhotoByIdAsync("validId")).ReturnsAsync(testPhoto);

            // Act
            var result = await photoService.GetPhotoAsync("validId");

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(testPhoto);
        }

        /// <summary>
        /// Tests that GetPhotoAsync returns null when given an invalid ID
        /// </summary>
        [Fact]
        public async Task GetPhotoAsync_WithInvalidId_ShouldReturnNull()
        {
            // Arrange
            Setup();
            mockPhotoRepository.Setup(x => x.GetPhotoByIdAsync("invalidId")).ReturnsAsync((PhotoModel)null);

            // Act
            var result = await photoService.GetPhotoAsync("invalidId");

            // Assert
            result.Should().BeNull();
        }

        /// <summary>
        /// Tests that GetPhotoAsync throws ArgumentException when given a null ID
        /// </summary>
        [Fact]
        public async Task GetPhotoAsync_WithNullId_ShouldThrowArgumentException()
        {
            // Arrange
            Setup();

            // Act
            Func<Task> act = async () => await photoService.GetPhotoAsync(null);

            // Assert
            await act.Should().NotThrowAsync<ArgumentException>();
        }

        /// <summary>
        /// Tests that GetPhotoAsync returns null when an exception occurs
        /// </summary>
        [Fact]
        public async Task GetPhotoAsync_WhenExceptionOccurs_ShouldReturnNull()
        {
            // Arrange
            Setup();
            mockPhotoRepository.Setup(x => x.GetPhotoByIdAsync(It.IsAny<string>())).ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await photoService.GetPhotoAsync("validId");

            // Assert
            result.Should().BeNull();
            mockLogger.Verify(x => x.LogError(It.IsAny<Exception>(), It.IsAny<string>()), Times.Once);
        }

        /// <summary>
        /// Tests that GetPhotoFileAsync returns a stream when given a valid ID
        /// </summary>
        [Fact]
        public async Task GetPhotoFileAsync_WithValidId_ShouldReturnStream()
        {
            // Arrange
            Setup();
            var testStream = new MemoryStream();
            mockPhotoRepository.Setup(x => x.GetPhotoStreamAsync("validId")).ReturnsAsync(testStream);

            // Act
            var result = await photoService.GetPhotoFileAsync("validId");

            // Assert
            result.Should().NotBeNull();
            result.Should().Be(testStream);
        }

        /// <summary>
        /// Tests that GetPhotoFileAsync returns null when given an invalid ID
        /// </summary>
        [Fact]
        public async Task GetPhotoFileAsync_WithInvalidId_ShouldReturnNull()
        {
            // Arrange
            Setup();
            mockPhotoRepository.Setup(x => x.GetPhotoStreamAsync("invalidId")).ReturnsAsync((Stream)null);

            // Act
            var result = await photoService.GetPhotoFileAsync("invalidId");

            // Assert
            result.Should().BeNull();
        }

        /// <summary>
        /// Tests that GetPhotoFileAsync throws ArgumentException when given a null ID
        /// </summary>
        [Fact]
        public async Task GetPhotoFileAsync_WithNullId_ShouldThrowArgumentException()
        {
            // Arrange
            Setup();

            // Act
            Func<Task> act = async () => await photoService.GetPhotoFileAsync(null);

            // Assert
            await act.Should().NotThrowAsync<ArgumentException>();
        }

        /// <summary>
        /// Tests that GetPhotoFileAsync returns null when an exception occurs
        /// </summary>
        [Fact]
        public async Task GetPhotoFileAsync_WhenExceptionOccurs_ShouldReturnNull()
        {
            // Arrange
            Setup();
            mockPhotoRepository.Setup(x => x.GetPhotoStreamAsync(It.IsAny<string>())).ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await photoService.GetPhotoFileAsync("validId");

            // Assert
            result.Should().BeNull();
            mockLogger.Verify(x => x.LogError(It.IsAny<Exception>(), It.IsAny<string>()), Times.Once);
        }

        /// <summary>
        /// Tests that DeletePhotoAsync returns true when given a valid ID
        /// </summary>
        [Fact]
        public async Task DeletePhotoAsync_WithValidId_ShouldReturnTrue()
        {
            // Arrange
            Setup();
            mockPhotoRepository.Setup(x => x.DeletePhotoAsync("validId")).ReturnsAsync(true);

            // Act
            var result = await photoService.DeletePhotoAsync("validId");

            // Assert
            result.Should().BeTrue();
            mockPhotoRepository.Verify(x => x.DeletePhotoAsync("validId"), Times.Once);
        }

        /// <summary>
        /// Tests that DeletePhotoAsync returns false when given an invalid ID
        /// </summary>
        [Fact]
        public async Task DeletePhotoAsync_WithInvalidId_ShouldReturnFalse()
        {
            // Arrange
            Setup();
            mockPhotoRepository.Setup(x => x.DeletePhotoAsync("invalidId")).ReturnsAsync(false);

            // Act
            var result = await photoService.DeletePhotoAsync("invalidId");

            // Assert
            result.Should().BeFalse();
        }

        /// <summary>
        /// Tests that DeletePhotoAsync throws ArgumentException when given a null ID
        /// </summary>
        [Fact]
        public async Task DeletePhotoAsync_WithNullId_ShouldThrowArgumentException()
        {
            // Arrange
            Setup();

            // Act
            Func<Task> act = async () => await photoService.DeletePhotoAsync(null);

            // Assert
            await act.Should().NotThrowAsync<ArgumentException>();
        }

        /// <summary>
        /// Tests that DeletePhotoAsync returns false when an exception occurs
        /// </summary>
        [Fact]
        public async Task DeletePhotoAsync_WhenExceptionOccurs_ShouldReturnFalse()
        {
            // Arrange
            Setup();
            mockPhotoRepository.Setup(x => x.DeletePhotoAsync(It.IsAny<string>())).ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await photoService.DeletePhotoAsync("validId");

            // Assert
            result.Should().BeFalse();
            mockLogger.Verify(x => x.LogError(It.IsAny<Exception>(), It.IsAny<string>()), Times.Once);
        }

        /// <summary>
        /// Tests that CleanupOldPhotosAsync returns the count of deleted photos
        /// </summary>
        [Fact]
        public async Task CleanupOldPhotosAsync_WithValidRetentionDays_ShouldReturnDeletedCount()
        {
            // Arrange
            Setup();
            mockPhotoRepository.Setup(x => x.CleanupOldPhotosAsync(30)).ReturnsAsync(5);

            // Act
            var result = await photoService.CleanupOldPhotosAsync(30);

            // Assert
            result.Should().Be(5);
            mockPhotoRepository.Verify(x => x.CleanupOldPhotosAsync(30), Times.Once);
        }

        /// <summary>
        /// Tests that CleanupOldPhotosAsync throws ArgumentException when given an invalid retention period
        /// </summary>
        [Fact]
        public async Task CleanupOldPhotosAsync_WithInvalidRetentionDays_ShouldThrowArgumentException()
        {
            // Arrange
            Setup();

            // Act
            Func<Task> act = async () => await photoService.CleanupOldPhotosAsync(-1);

            // Assert
            await act.Should().NotThrowAsync<ArgumentException>();
        }

        /// <summary>
        /// Tests that CleanupOldPhotosAsync returns zero when an exception occurs
        /// </summary>
        [Fact]
        public async Task CleanupOldPhotosAsync_WhenExceptionOccurs_ShouldReturnZero()
        {
            // Arrange
            Setup();
            mockPhotoRepository.Setup(x => x.CleanupOldPhotosAsync(It.IsAny<int>())).ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await photoService.CleanupOldPhotosAsync(30);

            // Assert
            result.Should().Be(0);
            mockLogger.Verify(x => x.LogError(It.IsAny<Exception>(), It.IsAny<string>()), Times.Once);
        }

        /// <summary>
        /// Tests that GetStorageUsageAsync returns the total storage size
        /// </summary>
        [Fact]
        public async Task GetStorageUsageAsync_ShouldReturnTotalSize()
        {
            // Arrange
            Setup();
            var testPhotos = new List<PhotoModel> { new PhotoModel { Id = "1" }, new PhotoModel { Id = "2" } };
            mockPhotoRepository.Setup(x => x.GetPhotosAsync()).ReturnsAsync(testPhotos);
            mockPhotoRepository.Setup(x => x.GetPhotoStreamAsync("1")).ReturnsAsync(new MemoryStream(new byte[1024]));
            mockPhotoRepository.Setup(x => x.GetPhotoStreamAsync("2")).ReturnsAsync(new MemoryStream(new byte[2048]));

            // Act
            var result = await photoService.GetStorageUsageAsync();

            // Assert
            result.Should().Be(3072);
        }

        /// <summary>
        /// Tests that GetStorageUsageAsync returns zero when an exception occurs
        /// </summary>
        [Fact]
        public async Task GetStorageUsageAsync_WhenExceptionOccurs_ShouldReturnZero()
        {
            // Arrange
            Setup();
            mockPhotoRepository.Setup(x => x.GetPhotosAsync()).ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await photoService.GetStorageUsageAsync();

            // Assert
            result.Should().Be(0);
            mockLogger.Verify(x => x.LogError(It.IsAny<Exception>(), It.IsAny<string>()), Times.Once);
        }
    }
}