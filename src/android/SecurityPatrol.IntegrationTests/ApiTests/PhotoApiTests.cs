using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Moq;
using SecurityPatrol.IntegrationTests.Helpers;
using SecurityPatrol.Services;
using SecurityPatrol.Models;
using SecurityPatrol.Constants;

namespace SecurityPatrol.IntegrationTests.ApiTests
{
    /// <summary>
    /// Integration tests for the photo API functionality in the Security Patrol application.
    /// </summary>
    public class PhotoApiTests : IDisposable
    {
        private readonly MockApiServer _mockApiServer;
        private readonly TestDatabaseInitializer _databaseInitializer;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PhotoApiTests> _logger;

        /// <summary>
        /// Initializes a new instance of the PhotoApiTests class and sets up the test environment.
        /// </summary>
        public PhotoApiTests()
        {
            // Set up logger
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });
            _logger = loggerFactory.CreateLogger<PhotoApiTests>();

            // Initialize database helper and mock API server
            _databaseInitializer = new TestDatabaseInitializer(loggerFactory.CreateLogger<TestDatabaseInitializer>());
            _mockApiServer = new MockApiServer(loggerFactory.CreateLogger<MockApiServer>());
            _mockApiServer.Start();

            // Set up service provider with test services
            _serviceProvider = SetupServices();
        }

        /// <summary>
        /// Cleans up resources used by the tests.
        /// </summary>
        public void Dispose()
        {
            _mockApiServer.Stop();
            _mockApiServer.Dispose();
        }

        /// <summary>
        /// Sets up the service provider with required services for testing.
        /// </summary>
        /// <returns>The configured service provider.</returns>
        private IServiceProvider SetupServices()
        {
            var services = new ServiceCollection();

            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            // Add test services
            services.AddSingleton(_mockApiServer);
            services.AddSingleton(_databaseInitializer);

            // Mock authentication state provider
            var authStateMock = new Mock<IAuthenticationStateProvider>();
            authStateMock.Setup(x => x.GetCurrentState()).ReturnsAsync(
                new AuthState(true, "+15551234567", DateTime.UtcNow));
            services.AddSingleton(authStateMock.Object);

            // Mock location service
            var locationServiceMock = new Mock<ILocationService>();
            locationServiceMock.Setup(x => x.GetCurrentLocation())
                .ReturnsAsync(new LocationModel { Latitude = 37.7749, Longitude = -122.4194 });
            services.AddSingleton(locationServiceMock.Object);

            // Mock network service
            var networkServiceMock = new Mock<INetworkService>();
            networkServiceMock.Setup(x => x.IsConnected).Returns(true);
            networkServiceMock.Setup(x => x.ShouldAttemptOperation(It.IsAny<NetworkOperationType>())).Returns(true);
            services.AddSingleton(networkServiceMock.Object);

            // Mock photo repository
            var photoRepositoryMock = new Mock<IPhotoRepository>();
            photoRepositoryMock.Setup(x => x.GetPhotoByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => new PhotoModel
                {
                    Id = id,
                    UserId = "+15551234567",
                    Timestamp = DateTime.UtcNow,
                    Latitude = 37.7749,
                    Longitude = -122.4194,
                    FilePath = $"/test/photos/{id}.jpg",
                    IsSynced = false
                });
            photoRepositoryMock.Setup(x => x.GetPhotoStreamAsync(It.IsAny<string>()))
                .ReturnsAsync(new MemoryStream(new byte[1024]));
            photoRepositoryMock.Setup(x => x.UpdateSyncStatusAsync(It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);
            photoRepositoryMock.Setup(x => x.UpdateRemoteIdAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            photoRepositoryMock.Setup(x => x.UpdateSyncProgressAsync(It.IsAny<string>(), It.IsAny<int>()))
                .Returns(Task.CompletedTask);
            photoRepositoryMock.Setup(x => x.DeletePhotoAsync(It.IsAny<string>()))
                .ReturnsAsync(true);
            services.AddSingleton(photoRepositoryMock.Object);

            // Add actual ApiService with mock server URL
            services.AddTransient<ApiService>(sp =>
            {
                // Create an HTTP client that points to our mock server
                var httpClient = new HttpClient
                {
                    BaseAddress = new Uri(_mockApiServer.GetBaseUrl())
                };

                // Create a mock token manager
                var tokenManagerMock = new Mock<ITokenManager>();
                tokenManagerMock.Setup(x => x.IsTokenValid()).ReturnsAsync(true);
                tokenManagerMock.Setup(x => x.RetrieveToken()).ReturnsAsync("mock_token");

                // Create a mock telemetry service
                var telemetryServiceMock = new Mock<ITelemetryService>();

                return new ApiService(
                    httpClient,
                    tokenManagerMock.Object,
                    networkServiceMock.Object,
                    telemetryServiceMock.Object);
            });

            // Mock camera helper and image compressor for photo service
            var cameraHelperMock = new Mock<CameraHelper>(MockBehavior.Loose, new object[] 
            { 
                new Mock<ILogger<CameraHelper>>().Object 
            });
            cameraHelperMock.Setup(x => x.CapturePhotoAsync())
                .ReturnsAsync(new MemoryStream(new byte[1024]));
            cameraHelperMock.Setup(x => x.CheckCameraPermissionAsync())
                .ReturnsAsync(true);
            services.AddSingleton(cameraHelperMock.Object);

            var imageCompressorMock = new Mock<ImageCompressor>(MockBehavior.Loose, new object[] 
            { 
                new Mock<ILogger<ImageCompressor>>().Object 
            });
            imageCompressorMock.Setup(x => x.CompressImageAsync(It.IsAny<Stream>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>()))
                .ReturnsAsync((Stream s, int? q, int? w, int? h) => new MemoryStream(new byte[512]));
            services.AddSingleton(imageCompressorMock.Object);

            // Add real service implementations for testing
            services.AddTransient<PhotoService>();
            services.AddTransient<PhotoSyncService>();

            return services.BuildServiceProvider();
        }

        /// <summary>
        /// Initializes the test environment before each test.
        /// </summary>
        private async Task InitializeTestAsync()
        {
            await _databaseInitializer.ResetDatabaseAsync();
            _mockApiServer.ResetMappings();
        }

        /// <summary>
        /// Creates a test photo model for use in tests.
        /// </summary>
        /// <returns>A test photo model.</returns>
        private PhotoModel CreateTestPhotoModel()
        {
            return new PhotoModel
            {
                Id = Guid.NewGuid().ToString(),
                UserId = "+15551234567",
                Timestamp = DateTime.UtcNow,
                Latitude = 37.7749,
                Longitude = -122.4194,
                FilePath = $"/test/photos/test-photo.jpg",
                IsSynced = false
            };
        }

        /// <summary>
        /// Tests that uploading a photo succeeds when the API returns a successful response.
        /// </summary>
        [Fact]
        public async Task Test_UploadPhoto_Success()
        {
            // Arrange
            await InitializeTestAsync();
            var photo = CreateTestPhotoModel();

            // Setup mock API response
            var responseObj = new PhotoUploadResponse
            {
                Id = "server_photo_123",
                Status = "Success"
            };
            _mockApiServer.SetupSuccessResponse(
                ExtractPathFromUrl(ApiEndpoints.PhotosUpload), 
                responseObj);

            // Get the photo sync service
            var photoSyncService = _serviceProvider.GetRequiredService<PhotoSyncService>();
            var photoRepositoryMock = _serviceProvider.GetRequiredService<IPhotoRepository>();

            // Mock specific behaviors for this test
            Mock.Get(photoRepositoryMock)
                .Setup(x => x.GetPhotoByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(photo);

            // Act
            var result = await photoSyncService.UploadPhotoAsync(photo.Id);

            // Assert
            Assert.True(result);
            Assert.Equal(1, _mockApiServer.GetRequestCount(ExtractPathFromUrl(ApiEndpoints.PhotosUpload)));
            
            // Verify the photo was marked as synced and has a remote ID
            Mock.Get(photoRepositoryMock).Verify(
                x => x.UpdateSyncStatusAsync(photo.Id, true), 
                Times.Once);
            
            Mock.Get(photoRepositoryMock).Verify(
                x => x.UpdateRemoteIdAsync(photo.Id, "server_photo_123"), 
                Times.Once);
        }

        /// <summary>
        /// Tests that uploading a photo fails when the API returns an error response.
        /// </summary>
        [Fact]
        public async Task Test_UploadPhoto_Failure()
        {
            // Arrange
            await InitializeTestAsync();
            var photo = CreateTestPhotoModel();

            // Setup mock API error response
            _mockApiServer.SetupErrorResponse(
                ExtractPathFromUrl(ApiEndpoints.PhotosUpload), 
                500, 
                "Internal Server Error");

            // Get the photo sync service
            var photoSyncService = _serviceProvider.GetRequiredService<PhotoSyncService>();
            var photoRepositoryMock = _serviceProvider.GetRequiredService<IPhotoRepository>();

            // Mock specific behaviors for this test
            Mock.Get(photoRepositoryMock)
                .Setup(x => x.GetPhotoByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(photo);

            // Act
            var result = await photoSyncService.UploadPhotoAsync(photo.Id);

            // Assert
            Assert.False(result);
            Assert.Equal(1, _mockApiServer.GetRequestCount(ExtractPathFromUrl(ApiEndpoints.PhotosUpload)));
            
            // Verify the photo was NOT marked as synced and has NO remote ID
            Mock.Get(photoRepositoryMock).Verify(
                x => x.UpdateSyncStatusAsync(photo.Id, true), 
                Times.Never);
            
            Mock.Get(photoRepositoryMock).Verify(
                x => x.UpdateRemoteIdAsync(It.IsAny<string>(), It.IsAny<string>()), 
                Times.Never);
        }

        /// <summary>
        /// Tests that synchronizing all photos succeeds when the API returns successful responses.
        /// </summary>
        [Fact]
        public async Task Test_SyncPhotos_Success()
        {
            // Arrange
            await InitializeTestAsync();
            var photo1 = CreateTestPhotoModel();
            var photo2 = CreateTestPhotoModel();
            var photo3 = CreateTestPhotoModel();

            var photos = new List<PhotoModel> { photo1, photo2, photo3 };

            // Setup mock API response
            var responseObj = new PhotoUploadResponse
            {
                Id = "server_photo_batch",
                Status = "Success"
            };
            _mockApiServer.SetupSuccessResponse(
                ExtractPathFromUrl(ApiEndpoints.PhotosUpload), 
                responseObj);

            // Get the photo sync service
            var photoSyncService = _serviceProvider.GetRequiredService<PhotoSyncService>();
            var photoRepositoryMock = _serviceProvider.GetRequiredService<IPhotoRepository>();

            // Mock specific behaviors for this test
            Mock.Get(photoRepositoryMock)
                .Setup(x => x.GetPendingPhotosAsync())
                .ReturnsAsync(photos);

            foreach (var photo in photos)
            {
                Mock.Get(photoRepositoryMock)
                    .Setup(x => x.GetPhotoByIdAsync(photo.Id))
                    .ReturnsAsync(photo);
            }

            // Act
            var result = await photoSyncService.SyncPhotosAsync();

            // Assert
            Assert.True(result);
            Assert.Equal(3, _mockApiServer.GetRequestCount(ExtractPathFromUrl(ApiEndpoints.PhotosUpload)));
            
            // Verify all photos were marked as synced
            foreach (var photo in photos)
            {
                Mock.Get(photoRepositoryMock).Verify(
                    x => x.UpdateSyncStatusAsync(photo.Id, true), 
                    Times.Once);
                
                Mock.Get(photoRepositoryMock).Verify(
                    x => x.UpdateRemoteIdAsync(photo.Id, "server_photo_batch"), 
                    Times.Once);
            }
        }

        /// <summary>
        /// Tests that synchronizing photos handles partial failures correctly.
        /// </summary>
        [Fact]
        public async Task Test_SyncPhotos_PartialFailure()
        {
            // Arrange
            await InitializeTestAsync();
            var photo1 = CreateTestPhotoModel();
            var photo2 = CreateTestPhotoModel();
            var photo3 = CreateTestPhotoModel();

            var photos = new List<PhotoModel> { photo1, photo2, photo3 };

            // Setup mock API responses - make photo2 fail
            var successResponseObj = new PhotoUploadResponse
            {
                Id = "server_photo_success",
                Status = "Success"
            };

            // Get the photo sync service
            var photoSyncService = _serviceProvider.GetRequiredService<PhotoSyncService>();
            var photoRepositoryMock = _serviceProvider.GetRequiredService<IPhotoRepository>();

            // Mock specific behaviors for this test
            Mock.Get(photoRepositoryMock)
                .Setup(x => x.GetPendingPhotosAsync())
                .ReturnsAsync(photos);

            // Set up a custom API mock for each photo
            // Success for photo1
            Mock.Get(photoRepositoryMock)
                .Setup(x => x.GetPhotoByIdAsync(photo1.Id))
                .ReturnsAsync(photo1);
                
            _mockApiServer.SetupSuccessResponse(
                ExtractPathFromUrl(ApiEndpoints.PhotosUpload), 
                successResponseObj);

            // Error for photo2, this is tricky with the shared mock server, rely on partial failure
            Mock.Get(photoRepositoryMock)
                .Setup(x => x.GetPhotoByIdAsync(photo2.Id))
                .ReturnsAsync((string id) => null); // Return null to cause a failure

            // Success for photo3
            Mock.Get(photoRepositoryMock)
                .Setup(x => x.GetPhotoByIdAsync(photo3.Id))
                .ReturnsAsync(photo3);

            // Act
            var result = await photoSyncService.SyncPhotosAsync();

            // Assert
            Assert.False(result); // Should be false due to partial failure
            
            // Verify successful photos are marked as synced
            Mock.Get(photoRepositoryMock).Verify(x => x.UpdateSyncStatusAsync(photo1.Id, true), Times.Once);
            Mock.Get(photoRepositoryMock).Verify(x => x.UpdateSyncStatusAsync(photo2.Id, true), Times.Never);
            Mock.Get(photoRepositoryMock).Verify(x => x.UpdateSyncStatusAsync(photo3.Id, true), Times.Once);
        }

        /// <summary>
        /// Tests that upload progress is correctly tracked during photo upload.
        /// </summary>
        [Fact]
        public async Task Test_UploadProgress_Tracking()
        {
            // Arrange
            await InitializeTestAsync();
            var photo = CreateTestPhotoModel();

            // Setup mock API response
            var responseObj = new PhotoUploadResponse
            {
                Id = "server_photo_123",
                Status = "Success"
            };
            _mockApiServer.SetupSuccessResponse(
                ExtractPathFromUrl(ApiEndpoints.PhotosUpload), 
                responseObj);

            // Get the photo sync service
            var photoSyncService = _serviceProvider.GetRequiredService<PhotoSyncService>();
            var photoRepositoryMock = _serviceProvider.GetRequiredService<IPhotoRepository>();

            // Mock specific behaviors for this test
            Mock.Get(photoRepositoryMock)
                .Setup(x => x.GetPhotoByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(photo);

            // Set up progress tracking
            var progressUpdates = new List<int>();
            var finalStatus = string.Empty;

            photoSyncService.UploadProgressChanged += (sender, progress) => 
            {
                if (progress.Id == photo.Id)
                {
                    progressUpdates.Add(progress.Progress);
                    finalStatus = progress.Status;
                }
            };

            // Act
            var result = await photoSyncService.UploadPhotoAsync(photo.Id);

            // Assert
            Assert.True(result);
            Assert.NotEmpty(progressUpdates);
            Assert.Equal(100, progressUpdates.Last()); // Final progress should be 100%
            Assert.Equal("Completed", finalStatus);
            
            // Verify progress updates were sent to the repository
            Mock.Get(photoRepositoryMock).Verify(
                x => x.UpdateSyncProgressAsync(photo.Id, It.IsAny<int>()), 
                Times.AtLeast(2)); // At least initial (0%) and final (100%)
        }

        /// <summary>
        /// Tests that photo upload fails when network is unavailable.
        /// </summary>
        [Fact]
        public async Task Test_NetworkUnavailable_UploadFails()
        {
            // Arrange
            await InitializeTestAsync();
            var photo = CreateTestPhotoModel();

            // Configure network service to report no connectivity
            var networkServiceMock = _serviceProvider.GetRequiredService<INetworkService>();
            Mock.Get(networkServiceMock)
                .Setup(x => x.IsConnected)
                .Returns(false);

            // Get the photo sync service
            var photoSyncService = _serviceProvider.GetRequiredService<PhotoSyncService>();
            var photoRepositoryMock = _serviceProvider.GetRequiredService<IPhotoRepository>();

            // Mock specific behaviors for this test
            Mock.Get(photoRepositoryMock)
                .Setup(x => x.GetPhotoByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(photo);

            // Act
            var result = await photoSyncService.UploadPhotoAsync(photo.Id);

            // Assert
            Assert.False(result);
            Assert.Equal(0, _mockApiServer.GetRequestCount(ExtractPathFromUrl(ApiEndpoints.PhotosUpload)));
            
            // Verify the photo was NOT marked as synced
            Mock.Get(photoRepositoryMock).Verify(
                x => x.UpdateSyncStatusAsync(photo.Id, true), 
                Times.Never);
        }

        /// <summary>
        /// Tests the full flow from photo capture to upload.
        /// </summary>
        [Fact]
        public async Task Test_PhotoCapture_AndUpload_Integration()
        {
            // Arrange
            await InitializeTestAsync();

            // Setup mock camera to return test image
            var cameraHelperMock = _serviceProvider.GetRequiredService<CameraHelper>();
            Mock.Get(cameraHelperMock)
                .Setup(x => x.CapturePhotoAsync())
                .ReturnsAsync(new MemoryStream(new byte[1024]));

            // Setup mock API response
            var responseObj = new PhotoUploadResponse
            {
                Id = "server_photo_123",
                Status = "Success"
            };
            _mockApiServer.SetupSuccessResponse(
                ExtractPathFromUrl(ApiEndpoints.PhotosUpload), 
                responseObj);

            // Get the services
            var photoService = _serviceProvider.GetRequiredService<PhotoService>();
            var photoSyncService = _serviceProvider.GetRequiredService<PhotoSyncService>();
            var photoRepositoryMock = _serviceProvider.GetRequiredService<IPhotoRepository>();

            // Mock repository to save the photo and return a valid ID
            string capturedPhotoId = null;
            Mock.Get(photoRepositoryMock)
                .Setup(x => x.SavePhotoAsync(It.IsAny<PhotoModel>(), It.IsAny<Stream>()))
                .ReturnsAsync((PhotoModel p, Stream s) => 
                {
                    capturedPhotoId = Guid.NewGuid().ToString();
                    return capturedPhotoId;
                });

            Mock.Get(photoRepositoryMock)
                .Setup(x => x.GetPhotoByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => new PhotoModel
                {
                    Id = id,
                    UserId = "+15551234567",
                    Timestamp = DateTime.UtcNow,
                    Latitude = 37.7749,
                    Longitude = -122.4194,
                    FilePath = $"/test/photos/{id}.jpg",
                    IsSynced = false
                });

            // Act - capture photo
            var capturedPhoto = await photoService.CapturePhotoAsync();
            Assert.NotNull(capturedPhoto);
            Assert.Equal(capturedPhotoId, capturedPhoto.Id);

            // Now upload the photo
            var uploadResult = await photoSyncService.UploadPhotoAsync(capturedPhoto.Id);

            // Assert
            Assert.True(uploadResult);
            Assert.Equal(1, _mockApiServer.GetRequestCount(ExtractPathFromUrl(ApiEndpoints.PhotosUpload)));
            
            // Verify the photo was marked as synced
            Mock.Get(photoRepositoryMock).Verify(
                x => x.UpdateSyncStatusAsync(capturedPhoto.Id, true), 
                Times.Once);
        }

        /// <summary>
        /// Tests that deleting a photo removes it from the repository.
        /// </summary>
        [Fact]
        public async Task Test_DeletePhoto_RemovesFromRepository()
        {
            // Arrange
            await InitializeTestAsync();
            var photo = CreateTestPhotoModel();

            // Get the photo service
            var photoService = _serviceProvider.GetRequiredService<PhotoService>();
            var photoRepositoryMock = _serviceProvider.GetRequiredService<IPhotoRepository>();

            // Mock specific behaviors for this test
            Mock.Get(photoRepositoryMock)
                .Setup(x => x.GetPhotoByIdAsync(photo.Id))
                .ReturnsAsync(photo);

            Mock.Get(photoRepositoryMock)
                .Setup(x => x.DeletePhotoAsync(photo.Id))
                .ReturnsAsync(true);

            // After deletion, return null when queried
            Mock.Get(photoRepositoryMock)
                .Setup(x => x.GetPhotoByIdAsync(photo.Id))
                .ReturnsAsync((string id) => null);

            // Act
            var result = await photoService.DeletePhotoAsync(photo.Id);

            // Assert
            Assert.True(result);
            var deletedPhoto = await photoService.GetPhotoAsync(photo.Id);
            Assert.Null(deletedPhoto);

            // Verify delete was called on the repository
            Mock.Get(photoRepositoryMock).Verify(
                x => x.DeletePhotoAsync(photo.Id), 
                Times.Once);
        }

        /// <summary>
        /// Helper method to extract the path from a URL.
        /// </summary>
        private string ExtractPathFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return string.Empty;

            try
            {
                var uri = new Uri(url);
                return uri.PathAndQuery;
            }
            catch (UriFormatException)
            {
                return url;
            }
        }
    }
}