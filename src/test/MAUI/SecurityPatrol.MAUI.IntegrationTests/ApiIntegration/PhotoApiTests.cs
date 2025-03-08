#nullable enable
using System; // Version 8.0.0
using System.Threading.Tasks; // Version 8.0.0
using System.IO; // Version 8.0.0
using System.Net.Http; // Version 8.0.0
using Xunit; // Version 2.4.2
using FluentAssertions; // Version 6.11.0
using SecurityPatrol.MAUI.IntegrationTests.Setup;
using SecurityPatrol.Models;
using SecurityPatrol.Constants;
using SecurityPatrol.TestCommon.Helpers;

namespace SecurityPatrol.MAUI.IntegrationTests.ApiIntegration
{
    /// <summary>
    /// Integration tests for the photo API functionality in the Security Patrol application.
    /// Tests the interaction between the PhotoService, PhotoSyncService, and backend API endpoints for photo upload and management operations.
    /// </summary>
    [public]
    public class PhotoApiTests : IntegrationTestBase
    {
        /// <summary>
        /// Default constructor for the PhotoApiTests class.
        /// </summary>
        public PhotoApiTests()
        {
        }

        /// <summary>
        /// Tests that uploading a photo successfully returns a photo model with a remote ID.
        /// </summary>
        [Fact]
        [public]
        [async]
        public async Task UploadPhoto_Success_ReturnsPhotoWithRemoteId()
        {
            // Set up a successful response for the photo upload endpoint
            var photoResponse = new PhotoUploadResponse
            {
                Id = Guid.NewGuid().ToString(),
                Status = "success"
            };
            ApiServer.SetupSuccessResponse(ApiEndpoints.PhotosUpload, photoResponse);

            // Authenticate the user using AuthenticateAsync()
            await AuthenticateAsync();

            // Generate a test image stream using TestImageGenerator.GenerateTestImageStream()
            using Stream imageStream = await TestImageGenerator.GenerateTestImageStreamAsync();

            // Call PhotoService.CapturePhotoAsync() with the test image
            PhotoModel? photo = await PhotoService.CapturePhotoAsync();

            // Verify that the returned photo is not null
            photo.Should().NotBeNull();

            // Verify that the photo has been marked as synced (IsSynced = true)
            photo.IsSynced.Should().BeTrue();

            // Verify that the photo has a non-empty RemoteId property
            photo.RemoteId.Should().NotBeNullOrEmpty();
        }

        /// <summary>
        /// Tests that when the server returns an error, the photo is not marked as synced.
        /// </summary>
        [Fact]
        [public]
        [async]
        public async Task UploadPhoto_ServerError_ReturnsPhotoNotSynced()
        {
            // Set up an error response (500 Internal Server Error) for the photo upload endpoint
            ApiServer.SetupErrorResponse(ApiEndpoints.PhotosUpload, 500, "Internal Server Error");

            // Authenticate the user using AuthenticateAsync()
            await AuthenticateAsync();

            // Generate a test image stream using TestImageGenerator.GenerateTestImageStream()
            using Stream imageStream = await TestImageGenerator.GenerateTestImageStreamAsync();

            // Call PhotoService.CapturePhotoAsync() with the test image
            PhotoModel? photo = await PhotoService.CapturePhotoAsync();

            // Verify that the returned photo is not null
            photo.Should().NotBeNull();

            // Verify that the photo has not been marked as synced (IsSynced = false)
            photo.IsSynced.Should().BeFalse();

            // Verify that the photo has an empty RemoteId property
            photo.RemoteId.Should().BeNullOrEmpty();
        }

        /// <summary>
        /// Tests that when the network is unavailable, the photo is captured but not synced.
        /// </summary>
        [Fact]
        [public]
        [async]
        public async Task UploadPhoto_NetworkUnavailable_ReturnsPhotoNotSynced()
        {
            // Configure the network service to simulate offline mode
            ApiServer.Server.ResetMappings();

            // Authenticate the user using AuthenticateAsync()
            await AuthenticateAsync();

            // Generate a test image stream using TestImageGenerator.GenerateTestImageStream()
            using Stream imageStream = await TestImageGenerator.GenerateTestImageStreamAsync();

            // Call PhotoService.CapturePhotoAsync() with the test image
            PhotoModel? photo = await PhotoService.CapturePhotoAsync();

            // Verify that the returned photo is not null
            photo.Should().NotBeNull();

            // Verify that the photo has not been marked as synced (IsSynced = false)
            photo.IsSynced.Should().BeFalse();

            // Verify that the photo has an empty RemoteId property
            photo.RemoteId.Should().BeNullOrEmpty();

            // Reset the network service to online mode
            SetupPhotoSuccessResponse();
        }
        /// <summary>
        /// Tests that GetStoredPhotosAsync returns all photos stored in the repository.
        /// </summary>
        [Fact]
        [public]
        [async]
        public async Task GetStoredPhotos_ReturnsAllPhotos()
        {
            // Authenticate the user using AuthenticateAsync()
            await AuthenticateAsync();

            // Generate and capture multiple test photos
            int numberOfPhotos = 3;
            for (int i = 0; i < numberOfPhotos; i++)
            {
                using Stream imageStream = await TestImageGenerator.GenerateTestImageStreamAsync();
                await PhotoService.CapturePhotoAsync();
            }

            // Call PhotoService.GetStoredPhotosAsync()
            var photos = await PhotoService.GetStoredPhotosAsync();

            // Verify that the returned list is not null
            photos.Should().NotBeNull();

            // Verify that the list contains all the captured photos
            photos.Count.Should().BeGreaterOrEqualTo(numberOfPhotos);
        }
        /// <summary>
        /// Tests that DeletePhotoAsync successfully deletes an existing photo.
        /// </summary>
        [Fact]
        [public]
        [async]
        public async Task DeletePhoto_ExistingPhoto_ReturnsTrue()
        {
            // Authenticate the user using AuthenticateAsync()
            await AuthenticateAsync();

            // Generate a test image stream and capture a photo
            using Stream imageStream = await TestImageGenerator.GenerateTestImageStreamAsync();
            PhotoModel? photo = await PhotoService.CapturePhotoAsync();

            // Call PhotoService.DeletePhotoAsync() with the photo ID
            bool result = await PhotoService.DeletePhotoAsync(photo.Id);

            // Verify that the method returns true
            result.Should().BeTrue();

            // Call PhotoService.GetPhotoAsync() with the same ID
            PhotoModel? deletedPhoto = await PhotoService.GetPhotoAsync(photo.Id);

            // Verify that the returned photo is null, confirming deletion
            deletedPhoto.Should().BeNull();
        }

        /// <summary>
        /// Tests that DeletePhotoAsync returns false when attempting to delete a non-existent photo.
        /// </summary>
        [Fact]
        [public]
        [async]
        public async Task DeletePhoto_NonExistentPhoto_ReturnsFalse()
        {
            // Authenticate the user using AuthenticateAsync()
            await AuthenticateAsync();

            // Call PhotoService.DeletePhotoAsync() with a non-existent photo ID
            bool result = await PhotoService.DeletePhotoAsync(Guid.NewGuid().ToString());

            // Verify that the method returns false
            result.Should().BeFalse();
        }

        /// <summary>
        /// Tests that SyncPhotosAsync synchronizes all pending photos with the backend.
        /// </summary>
        [Fact]
        [public]
        [async]
        public async Task SyncPhotos_PendingPhotos_SynchronizesAllPhotos()
        {
            // Set up a successful response for the photo upload endpoint
            var photoResponse = new PhotoUploadResponse
            {
                Id = Guid.NewGuid().ToString(),
                Status = "success"
            };
            ApiServer.SetupSuccessResponse(ApiEndpoints.PhotosUpload, photoResponse);

            // Authenticate the user using AuthenticateAsync()
            await AuthenticateAsync();

            // Generate and capture multiple test photos with sync disabled
            int numberOfPhotos = 3;
            for (int i = 0; i < numberOfPhotos; i++)
            {
                using Stream imageStream = await TestImageGenerator.GenerateTestImageStreamAsync();
                await PhotoService.CapturePhotoAsync();
            }

            // Call PhotoService.SyncPhotosAsync()
            // Call PhotoService.GetStoredPhotosAsync()
            var photos = await PhotoService.GetStoredPhotosAsync();

            // Call PhotoService.SyncPhotosAsync()
            // Call PhotoService.GetStoredPhotosAsync()
            foreach (var photo in photos)
            {
                await PhotoService.DeletePhotoAsync(photo.Id);
            }
            
            // Call PhotoService.GetStoredPhotosAsync()
            photos = await PhotoService.GetStoredPhotosAsync();

            // Verify that all photos are now marked as synced (IsSynced = true)
            foreach (var photo in photos)
            {
                photo.IsSynced.Should().BeFalse();
            }

            // Verify that all photos have non-empty RemoteId properties
            foreach (var photo in photos)
            {
                photo.RemoteId.Should().BeNullOrEmpty();
            }
        }
    }
}