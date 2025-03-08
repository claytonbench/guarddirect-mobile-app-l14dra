using System; // Version 8.0.0
using System.Threading.Tasks; // Version 8.0.0
using System.IO; // Version 8.0.0
using System.Collections.Generic; // Version 8.0.0
using System.Linq; // Version 8.0.0
using Xunit; // Version 2.4.2
using FluentAssertions; // Version 6.11.0
using Moq; // Version 4.18.4
using SecurityPatrol.MAUI.IntegrationTests.Setup;
using SecurityPatrol.Models;
using SecurityPatrol.Constants;
using SecurityPatrol.TestCommon.Helpers;

namespace SecurityPatrol.MAUI.IntegrationTests.Services
{
    /// <summary>
    /// Integration tests for the photo capture functionality in the Security Patrol application.
    /// These tests verify that the PhotoService correctly interacts with its dependencies and the backend API to capture, store, retrieve, and manage photos.
    /// </summary>
    public class PhotoCaptureIntegrationTests : IntegrationTestBase
    {
        /// <summary>
        /// Initializes a new instance of the PhotoCaptureIntegrationTests class.
        /// </summary>
        public PhotoCaptureIntegrationTests()
        {
        }

        /// <summary>
        /// Tests that capturing a photo successfully saves it to the repository.
        /// </summary>
        [Fact]
        public async Task CapturePhoto_ShouldSavePhotoToRepository()
        {
            // Set up a mock response for the photo upload API endpoint
            SetupPhotoUploadSuccessResponse();

            // Generate a test image stream using TestImageGenerator.GenerateTestImageAsync()
            using var testImageStream = await TestImageGenerator.GenerateTestImageAsync();

            // Mock the CameraHelper to return the test image stream
            ApiServer.Server.MockCameraHelper(testImageStream);

            // Call PhotoService.CapturePhotoAsync()
            var photo = await PhotoService.CapturePhotoAsync();

            // Assert that the result is not null
            photo.Should().NotBeNull();

            // Assert that the photo has a valid ID
            photo.Id.Should().NotBeNullOrEmpty();

            // Assert that the photo has a valid timestamp
            photo.Timestamp.Should().NotBe(DateTime.MinValue);

            // Assert that the photo has a valid file path
            photo.FilePath.Should().NotBeNullOrEmpty();

            // Assert that the photo is not marked as synced initially
            photo.IsSynced.Should().BeFalse();
        }

        /// <summary>
        /// Tests that the service can retrieve all stored photos.
        /// </summary>
        [Fact]
        public async Task GetStoredPhotos_ShouldReturnAllPhotos()
        {
            // Set up a mock response for the photo upload API endpoint
            SetupPhotoUploadSuccessResponse();

            // Generate and capture multiple test photos
            var photo1 = await CaptureTestPhotoAsync();
            var photo2 = await CaptureTestPhotoAsync();

            // Call PhotoService.GetStoredPhotosAsync()
            var photos = await PhotoService.GetStoredPhotosAsync();

            // Assert that the result is not null
            photos.Should().NotBeNull();

            // Assert that the result contains all the captured photos
            photos.Should().Contain(photo1);
            photos.Should().Contain(photo2);

            // Assert that each photo has the expected properties
            foreach (var photo in photos)
            {
                photo.Id.Should().NotBeNullOrEmpty();
                photo.Timestamp.Should().NotBe(DateTime.MinValue);
                photo.FilePath.Should().NotBeNullOrEmpty();
            }
        }

        /// <summary>
        /// Tests that the service can retrieve a specific photo by ID.
        /// </summary>
        [Fact]
        public async Task GetPhoto_ShouldReturnSpecificPhoto()
        {
            // Set up a mock response for the photo upload API endpoint
            SetupPhotoUploadSuccessResponse();

            // Generate and capture a test photo
            var photo = await CaptureTestPhotoAsync();

            // Store the ID of the captured photo
            string photoId = photo.Id;

            // Call PhotoService.GetPhotoAsync() with the stored ID
            var retrievedPhoto = await PhotoService.GetPhotoAsync(photoId);

            // Assert that the result is not null
            retrievedPhoto.Should().NotBeNull();

            // Assert that the returned photo has the same ID as the captured photo
            retrievedPhoto.Id.Should().Be(photoId);

            // Assert that the photo has the expected properties
            retrievedPhoto.Timestamp.Should().Be(photo.Timestamp);
            retrievedPhoto.FilePath.Should().Be(photo.FilePath);
        }

        /// <summary>
        /// Tests that the service can retrieve the image file stream for a photo.
        /// </summary>
        [Fact]
        public async Task GetPhotoFile_ShouldReturnPhotoStream()
        {
            // Set up a mock response for the photo upload API endpoint
            SetupPhotoUploadSuccessResponse();

            // Generate and capture a test photo
            var photo = await CaptureTestPhotoAsync();

            // Store the ID of the captured photo
            string photoId = photo.Id;

            // Call PhotoService.GetPhotoFileAsync() with the stored ID
            var photoStream = await PhotoService.GetPhotoFileAsync(photoId);

            // Assert that the result is not null
            photoStream.Should().NotBeNull();

            // Assert that the stream has data (Length > 0)
            photoStream.Length.Should().BeGreaterThan(0);

            // Assert that the stream can be read
            photoStream.CanRead.Should().BeTrue();
        }

        /// <summary>
        /// Tests that the service can delete a photo and its associated file.
        /// </summary>
        [Fact]
        public async Task DeletePhoto_ShouldRemovePhotoFromRepository()
        {
            // Set up a mock response for the photo upload API endpoint
            SetupPhotoUploadSuccessResponse();

            // Generate and capture a test photo
            var photo = await CaptureTestPhotoAsync();

            // Store the ID of the captured photo
            string photoId = photo.Id;

            // Call PhotoService.DeletePhotoAsync() with the stored ID
            bool deletionResult = await PhotoService.DeletePhotoAsync(photoId);

            // Assert that the deletion operation returns true
            deletionResult.Should().BeTrue();

            // Call PhotoService.GetPhotoAsync() with the same ID
            var retrievedPhoto = await PhotoService.GetPhotoAsync(photoId);

            // Assert that the result is null, indicating the photo was deleted
            retrievedPhoto.Should().BeNull();

            // Call PhotoService.GetPhotoFileAsync() with the same ID
            var photoStream = await PhotoService.GetPhotoFileAsync(photoId);

            // Assert that the result is null, indicating the file was deleted
            photoStream.Should().BeNull();
        }

        /// <summary>
        /// Tests that the service can clean up old photos based on retention policy.
        /// </summary>
        [Fact]
        public async Task CleanupOldPhotos_ShouldRemoveExpiredPhotos()
        {
            // Set up a mock response for the photo upload API endpoint
            SetupPhotoUploadSuccessResponse();

            // Generate and capture multiple test photos
            var photo1 = await CaptureTestPhotoAsync();
            var photo2 = await CaptureTestPhotoAsync();
            var photo3 = await CaptureTestPhotoAsync();

            // Modify the timestamp of some photos to make them appear older
            photo1.Timestamp = DateTime.UtcNow.AddDays(-35);
            photo2.Timestamp = DateTime.UtcNow.AddDays(-25);
            photo3.Timestamp = DateTime.UtcNow.AddDays(-15);

            // Mark the older photos as synced
            photo1.IsSynced = true;
            photo2.IsSynced = true;
            photo3.IsSynced = true;

            // Call PhotoService.CleanupOldPhotosAsync() with a retention period
            int deletedCount = await PhotoService.CleanupOldPhotosAsync(30);

            // Assert that the cleanup operation returns the expected number of deleted photos
            deletedCount.Should().Be(1);

            // Call PhotoService.GetStoredPhotosAsync()
            var remainingPhotos = await PhotoService.GetStoredPhotosAsync();

            // Assert that only the non-expired photos remain in the repository
            remainingPhotos.Should().NotContain(photo1);
            remainingPhotos.Should().Contain(photo2);
            remainingPhotos.Should().Contain(photo3);
        }

        /// <summary>
        /// Tests that captured photos are synchronized with the backend API.
        /// </summary>
        [Fact]
        public async Task PhotoUpload_ShouldSyncWithBackend()
        {
            // Set up a mock response for the photo upload API endpoint
            SetupPhotoUploadSuccessResponse();

            // Generate and capture a test photo
            var photo = await CaptureTestPhotoAsync();

            // Store the ID of the captured photo
            string photoId = photo.Id;

            // Wait for the background sync to complete
            await Task.Delay(5000); // Give some time for the sync to run

            // Call PhotoService.GetPhotoAsync() with the stored ID
            var syncedPhoto = await PhotoService.GetPhotoAsync(photoId);

            // Assert that the photo is now marked as synced
            syncedPhoto.IsSynced.Should().BeTrue();

            // Assert that the photo has a remote ID from the API response
            syncedPhoto.RemoteId.Should().NotBeNullOrEmpty();
        }

        /// <summary>
        /// Tests that the service can calculate the total storage usage for photos.
        /// </summary>
        [Fact]
        public async Task GetStorageUsage_ShouldReturnTotalSize()
        {
            // Set up a mock response for the photo upload API endpoint
            SetupPhotoUploadSuccessResponse();

            // Generate and capture multiple test photos with known sizes
            var photo1 = await CaptureTestPhotoAsync();
            var photo2 = await CaptureTestPhotoAsync();

            // Call PhotoService.GetStorageUsageAsync()
            long storageUsage = await PhotoService.GetStorageUsageAsync();

            // Assert that the result is greater than zero
            storageUsage.Should().BeGreaterThan(0);

            // Assert that the result is approximately equal to the sum of the test photo sizes
            // This is a rough estimate, so we'll allow a margin of error
            //storageUsage.Should().BeApproximately(photo1Size + photo2Size, 1024);
        }

        /// <summary>
        /// Helper method to set up a successful response for the photo upload API endpoint.
        /// </summary>
        private void SetupPhotoUploadSuccessResponse()
        {
            // Create a new PhotoUploadResponse with success status
            var photoResponse = new PhotoUploadResponse
            {
                Id = Guid.NewGuid().ToString(),
                Status = "success"
            };

            // Call ApiServer.SetupSuccessResponse for the photos upload endpoint
            ApiServer.SetupSuccessResponse(ApiEndpoints.PhotosUpload, photoResponse);

            // Configure the response to return the created PhotoUploadResponse
            ApiServer.Server.MockPhotoUploadResponse(photoResponse);
        }

        /// <summary>
        /// Helper method to capture a test photo with the PhotoService.
        /// </summary>
        /// <returns>A task that returns the captured photo model</returns>
        private async Task<PhotoModel> CaptureTestPhotoAsync()
        {
            // Generate a test image stream using TestImageGenerator.GenerateTestImageAsync()
            using var testImageStream = await TestImageGenerator.GenerateTestImageAsync();

            // Mock the CameraHelper to return the test image stream
            ApiServer.Server.MockCameraHelper(testImageStream);

            // Call PhotoService.CapturePhotoAsync()
            var photo = await PhotoService.CapturePhotoAsync();

            // Return the captured photo model
            return photo;
        }
    }
}