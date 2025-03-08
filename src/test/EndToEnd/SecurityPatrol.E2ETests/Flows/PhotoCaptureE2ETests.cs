using System; // Version 8.0.0
using System.Threading.Tasks; // Version 8.0.0
using System.IO; // Version 8.0.0
using System.Linq; // Version 8.0.0
using Xunit; // Version 2.4.2
using FluentAssertions; // Version 6.11.0
using SecurityPatrol.E2ETests.Setup;
using SecurityPatrol.Services;
using SecurityPatrol.TestCommon.Helpers; // TestImageGenerator
using SecurityPatrol.Models; // PhotoModel

namespace SecurityPatrol.E2ETests.Flows
{
    /// <summary>
    /// End-to-end tests for the photo capture functionality in the Security Patrol application.
    /// </summary>
    [Collection("E2E Tests")]
    public class PhotoCaptureE2ETests : E2ETestBase
    {
        /// <summary>
        /// Initializes a new instance of the PhotoCaptureE2ETests class.
        /// </summary>
        public PhotoCaptureE2ETests()
        {
            // Call base constructor to initialize test environment
        }

        /// <summary>
        /// Tests that a photo can be captured and stored locally.
        /// </summary>
        [Fact]
        public async Task TestPhotoCaptureAndStorageAsync()
        {
            // Authenticate the user by calling AuthenticateAsync()
            bool authSuccess = await AuthenticateAsync();
            authSuccess.Should().BeTrue("Authentication should succeed");

            // Clock in to start a shift by calling ClockInAsync()
            bool clockInSuccess = await ClockInAsync();
            clockInSuccess.Should().BeTrue("Clock-in should succeed");

            // Capture a photo by calling CapturePhotoAsync()
            bool captureSuccess = await CapturePhotoAsync();
            captureSuccess.Should().BeTrue("Photo capture should succeed");

            // Retrieve all stored photos using PhotoService.GetStoredPhotosAsync()
            var storedPhotos = await PhotoService.GetStoredPhotosAsync();

            // Verify that there is at least one photo in the stored photos collection
            storedPhotos.Should().NotBeNullOrEmpty("There should be at least one stored photo");

            // Get the captured photo
            var capturedPhoto = storedPhotos.FirstOrDefault();

            // Verify that the photo was captured successfully (not null)
            capturedPhoto.Should().NotBeNull("The captured photo should not be null");

            // Verify that the photo has a valid ID
            capturedPhoto.Id.Should().NotBeNullOrEmpty("The photo should have a valid ID");

            // Verify that the photo has a valid file path
            capturedPhoto.FilePath.Should().NotBeNullOrEmpty("The photo should have a valid file path");

            // Verify that the photo file exists by retrieving it using PhotoService.GetPhotoFileAsync()
            Stream photoFileStream = await PhotoService.GetPhotoFileAsync(capturedPhoto.Id);

            // Assert that the photo file stream is not null
            photoFileStream.Should().NotBeNull("The photo file stream should not be null");
        }

        /// <summary>
        /// Tests that captured photos are properly synchronized with the backend API.
        /// </summary>
        [Fact]
        public async Task TestPhotoSynchronizationAsync()
        {
            // Authenticate the user by calling AuthenticateAsync()
            bool authSuccess = await AuthenticateAsync();
            authSuccess.Should().BeTrue("Authentication should succeed");

            // Clock in to start a shift by calling ClockInAsync()
            bool clockInSuccess = await ClockInAsync();
            clockInSuccess.Should().BeTrue("Clock-in should succeed");

            // Capture a photo by calling CapturePhotoAsync()
            bool captureSuccess = await CapturePhotoAsync();
            captureSuccess.Should().BeTrue("Photo capture should succeed");

            // Retrieve all stored photos using PhotoService.GetStoredPhotosAsync()
            var storedPhotos = await PhotoService.GetStoredPhotosAsync();

            // Verify that there is at least one photo in the stored photos collection
            storedPhotos.Should().NotBeNullOrEmpty("There should be at least one stored photo");

            // Get the captured photo
            var capturedPhoto = storedPhotos.FirstOrDefault();

            // Verify that the photo was captured successfully (not null)
            capturedPhoto.Should().NotBeNull("The captured photo should not be null");

            // Verify that the photo is initially not synced (IsSynced = false)
            capturedPhoto.IsSynced.Should().BeFalse("The photo should initially not be synced");

            // Synchronize data with the backend by calling SyncDataAsync()
            bool syncSuccess = await SyncDataAsync();
            syncSuccess.Should().BeTrue("Data synchronization should succeed");

            // Retrieve the photo again using PhotoService.GetPhotoAsync()
            var syncedPhoto = await PhotoService.GetPhotoAsync(capturedPhoto.Id);

            // Verify that the photo is now marked as synced (IsSynced = true)
            syncedPhoto.IsSynced.Should().BeTrue("The photo should now be synced");
        }

        /// <summary>
        /// Tests that multiple photos can be captured and stored in sequence.
        /// </summary>
        [Fact]
        public async Task TestMultiplePhotoCaptureAsync()
        {
            // Authenticate the user by calling AuthenticateAsync()
            bool authSuccess = await AuthenticateAsync();
            authSuccess.Should().BeTrue("Authentication should succeed");

            // Clock in to start a shift by calling ClockInAsync()
            bool clockInSuccess = await ClockInAsync();
            clockInSuccess.Should().BeTrue("Clock-in should succeed");

            // Capture first photo by calling CapturePhotoAsync()
            bool captureSuccess1 = await CapturePhotoAsync();
            captureSuccess1.Should().BeTrue("First photo capture should succeed");

            // Capture second photo by calling CapturePhotoAsync()
            bool captureSuccess2 = await CapturePhotoAsync();
            captureSuccess2.Should().BeTrue("Second photo capture should succeed");

            // Capture third photo by calling CapturePhotoAsync()
            bool captureSuccess3 = await CapturePhotoAsync();
            captureSuccess3.Should().BeTrue("Third photo capture should succeed");

            // Retrieve all stored photos using PhotoService.GetStoredPhotosAsync()
            var storedPhotos = await PhotoService.GetStoredPhotosAsync();

            // Verify that there are at least 3 photos in the collection
            storedPhotos.Count.Should().BeGreaterOrEqualTo(3, "There should be at least 3 stored photos");

            // Synchronize data with the backend by calling SyncDataAsync()
            bool syncSuccess = await SyncDataAsync();
            syncSuccess.Should().BeTrue("Data synchronization should succeed");

            // Retrieve all stored photos again
            var syncedPhotos = await PhotoService.GetStoredPhotosAsync();

            // Verify that all photos are now marked as synced
            syncedPhotos.All(p => p.IsSynced).Should().BeTrue("All photos should now be synced");
        }

        /// <summary>
        /// Tests that photos can be deleted from local storage.
        /// </summary>
        [Fact]
        public async Task TestPhotoDeleteAsync()
        {
            // Authenticate the user by calling AuthenticateAsync()
            bool authSuccess = await AuthenticateAsync();
            authSuccess.Should().BeTrue("Authentication should succeed");

            // Clock in to start a shift by calling ClockInAsync()
            bool clockInSuccess = await ClockInAsync();
            clockInSuccess.Should().BeTrue("Clock-in should succeed");

            // Capture a photo by calling CapturePhotoAsync()
            bool captureSuccess = await CapturePhotoAsync();
            captureSuccess.Should().BeTrue("Photo capture should succeed");

            // Retrieve all stored photos using PhotoService.GetStoredPhotosAsync()
            var storedPhotos = await PhotoService.GetStoredPhotosAsync();

            // Verify that there is at least one photo in the stored photos collection
            storedPhotos.Should().NotBeNullOrEmpty("There should be at least one stored photo");

            // Get the captured photo
            var capturedPhoto = storedPhotos.FirstOrDefault();

            // Verify that the photo was captured successfully
            capturedPhoto.Should().NotBeNull("The captured photo should not be null");

            // Delete the photo using PhotoService.DeletePhotoAsync()
            bool deleteSuccess = await PhotoService.DeletePhotoAsync(capturedPhoto.Id);
            deleteSuccess.Should().BeTrue("Photo deletion should succeed");

            // Retrieve all stored photos again
            var remainingPhotos = await PhotoService.GetStoredPhotosAsync();

            // Verify that the deleted photo no longer exists in the collection
            remainingPhotos.Should().NotContain(capturedPhoto, "The deleted photo should no longer exist in the collection");

            // Attempt to retrieve the deleted photo file using PhotoService.GetPhotoFileAsync()
            Stream deletedPhotoFileStream = await PhotoService.GetPhotoFileAsync(capturedPhoto.Id);

            // Verify that the photo file is null (no longer exists)
            deletedPhotoFileStream.Should().BeNull("The photo file stream should be null (no longer exists)");
        }

        /// <summary>
        /// Tests that photo storage usage is correctly tracked.
        /// </summary>
        [Fact]
        public async Task TestPhotoStorageUsageAsync()
        {
            // Authenticate the user by calling AuthenticateAsync()
            bool authSuccess = await AuthenticateAsync();
            authSuccess.Should().BeTrue("Authentication should succeed");

            // Clock in to start a shift by calling ClockInAsync()
            bool clockInSuccess = await ClockInAsync();
            clockInSuccess.Should().BeTrue("Clock-in should succeed");

            // Get initial storage usage using PhotoService.GetStorageUsageAsync()
            long initialStorageUsage = await PhotoService.GetStorageUsageAsync();

            // Capture a photo by calling CapturePhotoAsync()
            bool captureSuccess1 = await CapturePhotoAsync();
            captureSuccess1.Should().BeTrue("First photo capture should succeed");

            // Get updated storage usage
            long storageUsage1 = await PhotoService.GetStorageUsageAsync();

            // Verify that storage usage has increased after capturing a photo
            storageUsage1.Should().BeGreaterThan(initialStorageUsage, "Storage usage should increase after capturing a photo");

            // Capture another photo
            bool captureSuccess2 = await CapturePhotoAsync();
            captureSuccess2.Should().BeTrue("Second photo capture should succeed");

            // Get updated storage usage again
            long storageUsage2 = await PhotoService.GetStorageUsageAsync();

            // Verify that storage usage has increased further
            storageUsage2.Should().BeGreaterThan(storageUsage1, "Storage usage should increase further after capturing another photo");

            // Retrieve all stored photos using PhotoService.GetStoredPhotosAsync()
            var storedPhotos = await PhotoService.GetStoredPhotosAsync();

            // Get the captured photo
            var capturedPhoto = storedPhotos.FirstOrDefault();

            // Delete one photo using PhotoService.DeletePhotoAsync()
            bool deleteSuccess = await PhotoService.DeletePhotoAsync(capturedPhoto.Id);
            deleteSuccess.Should().BeTrue("Photo deletion should succeed");

            // Get final storage usage
            long finalStorageUsage = await PhotoService.GetStorageUsageAsync();

            // Verify that storage usage has decreased after deleting a photo
            finalStorageUsage.Should().BeLessThan(storageUsage2, "Storage usage should decrease after deleting a photo");
        }

        /// <summary>
        /// Tests that old synced photos are properly cleaned up.
        /// </summary>
        [Fact]
        public async Task TestPhotoCleanupAsync()
        {
            // Authenticate the user by calling AuthenticateAsync()
            bool authSuccess = await AuthenticateAsync();
            authSuccess.Should().BeTrue("Authentication should succeed");

            // Clock in to start a shift by calling ClockInAsync()
            bool clockInSuccess = await ClockInAsync();
            clockInSuccess.Should().BeTrue("Clock-in should succeed");

            // Capture multiple photos
            int numPhotos = 5;
            for (int i = 0; i < numPhotos; i++)
            {
                bool captureSuccess = await CapturePhotoAsync();
                captureSuccess.Should().BeTrue($"Photo capture {i + 1} should succeed");
            }

            // Synchronize data with the backend by calling SyncDataAsync()
            bool syncSuccess = await SyncDataAsync();
            syncSuccess.Should().BeTrue("Data synchronization should succeed");

            // Retrieve all stored photos using PhotoService.GetStoredPhotosAsync()
            var storedPhotos = await PhotoService.GetStoredPhotosAsync();

            // Verify that there are at least 5 photos in the collection
            storedPhotos.Count.Should().BeGreaterOrEqualTo(numPhotos, "There should be at least 5 stored photos");

            // Manually set some photos to have older timestamps (to simulate old photos)
            int numOldPhotos = 2;
            for (int i = 0; i < numOldPhotos; i++)
            {
                storedPhotos[i].Timestamp = DateTime.UtcNow.AddDays(-31); // Older than 30 days
            }

            // Get initial photo count
            int initialPhotoCount = storedPhotos.Count;

            // Call PhotoService.CleanupOldPhotosAsync() with a retention period
            int retentionDays = 30;
            int deletedCount = await PhotoService.CleanupOldPhotosAsync(retentionDays);

            // Verify that old synced photos were deleted
            deletedCount.Should().Be(numOldPhotos, "Old synced photos should be deleted");

            // Get updated photo count
            var remainingPhotos = await PhotoService.GetStoredPhotosAsync();
            int updatedPhotoCount = remainingPhotos.Count;

            // Verify that recent photos are still present
            updatedPhotoCount.Should().Be(initialPhotoCount - numOldPhotos, "Recent photos should still be present");
        }

        /// <summary>
        /// Tests that photo capture fails when the user is not authenticated.
        /// </summary>
        [Fact]
        public async Task TestPhotoCaptureWithoutAuthenticationAsync()
        {
            // Skip authentication step

            // Attempt to capture a photo by calling CapturePhotoAsync()
            Func<Task> act = async () => await CapturePhotoAsync();

            // Verify that the photo capture operation returns null or throws an appropriate exception
            await act.Should().ThrowAsync<NullReferenceException>("Photo capture should throw an exception when not authenticated");
        }

        /// <summary>
        /// Tests that photo capture works even when the user is not clocked in.
        /// </summary>
        [Fact]
        public async Task TestPhotoCaptureWithoutClockInAsync()
        {
            // Authenticate the user by calling AuthenticateAsync()
            bool authSuccess = await AuthenticateAsync();
            authSuccess.Should().BeTrue("Authentication should succeed");

            // Skip clock in step

            // Capture a photo by calling CapturePhotoAsync()
            bool captureSuccess = await CapturePhotoAsync();
            captureSuccess.Should().BeTrue("Photo capture should succeed");

            // Retrieve all stored photos using PhotoService.GetStoredPhotosAsync()
            var storedPhotos = await PhotoService.GetStoredPhotosAsync();

            // Verify that there is at least one photo in the stored photos collection
            storedPhotos.Should().NotBeNullOrEmpty("There should be at least one stored photo");

            // Get the captured photo
            var capturedPhoto = storedPhotos.FirstOrDefault();

            // Verify that the photo was captured successfully
            capturedPhoto.Should().NotBeNull("The captured photo should not be null");
        }

        /// <summary>
        /// Tests that photos captured offline are synchronized when connectivity is restored.
        /// </summary>
        [Fact]
        public async Task TestOfflinePhotoSynchronizationAsync()
        {
            // Authenticate the user by calling AuthenticateAsync()
            bool authSuccess = await AuthenticateAsync();
            authSuccess.Should().BeTrue("Authentication should succeed");

            // Clock in to start a shift by calling ClockInAsync()
            bool clockInSuccess = await ClockInAsync();
            clockInSuccess.Should().BeTrue("Clock-in should succeed");

            // Configure mock network service to simulate offline mode
            TestEnvironmentSetup.ConfigureNetworkConnectivity(false);

            // Capture photos while offline
            int numPhotos = 3;
            for (int i = 0; i < numPhotos; i++)
            {
                bool captureSuccess = await CapturePhotoAsync();
                captureSuccess.Should().BeTrue($"Photo capture {i + 1} should succeed");
            }

            // Retrieve all stored photos using PhotoService.GetStoredPhotosAsync()
            var storedPhotos = await PhotoService.GetStoredPhotosAsync();

            // Verify that there are at least 3 photos in the collection
            storedPhotos.Count.Should().BeGreaterOrEqualTo(numPhotos, "There should be at least 3 stored photos");

            // Verify that photos are stored locally but not synced
            storedPhotos.All(p => !p.IsSynced).Should().BeTrue("Photos should be stored locally but not synced");

            // Configure mock network service to simulate online mode
            TestEnvironmentSetup.ConfigureNetworkConnectivity(true);

            // Synchronize data with the backend by calling SyncDataAsync()
            bool syncSuccess = await SyncDataAsync();
            syncSuccess.Should().BeTrue("Data synchronization should succeed");

            // Retrieve all stored photos again
            var syncedPhotos = await PhotoService.GetStoredPhotosAsync();

            // Verify that previously offline photos are now synced
            syncedPhotos.All(p => p.IsSynced).Should().BeTrue("Previously offline photos should now be synced");
        }
    }
}