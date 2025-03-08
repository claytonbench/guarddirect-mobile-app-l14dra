using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging; // v8.0.0
using SecurityPatrol.Helpers;
using SecurityPatrol.Models;

namespace SecurityPatrol.Services
{
    /// <summary>
    /// Service that implements the IPhotoService interface to provide photo capture, storage,
    /// and management functionality for the Security Patrol application.
    /// </summary>
    public class PhotoService : IPhotoService
    {
        private readonly ILogger<PhotoService> _logger;
        private readonly IPhotoRepository _photoRepository;
        private readonly IPhotoSyncService _photoSyncService;
        private readonly ILocationService _locationService;
        private readonly IAuthenticationStateProvider _authStateProvider;
        private readonly CameraHelper _cameraHelper;
        private readonly ImageCompressor _imageCompressor;
        private readonly INetworkService _networkService;

        /// <summary>
        /// Initializes a new instance of the PhotoService class with required dependencies.
        /// </summary>
        /// <param name="logger">Logger for recording service operations and errors</param>
        /// <param name="photoRepository">Repository for photo data persistence</param>
        /// <param name="photoSyncService">Service for synchronizing photos with the backend</param>
        /// <param name="locationService">Service for accessing location information</param>
        /// <param name="authStateProvider">Provider for authentication state information</param>
        /// <param name="cameraHelper">Helper for camera operations</param>
        /// <param name="imageCompressor">Helper for image compression</param>
        /// <param name="networkService">Service for checking network connectivity</param>
        public PhotoService(
            ILogger<PhotoService> logger,
            IPhotoRepository photoRepository,
            IPhotoSyncService photoSyncService,
            ILocationService locationService,
            IAuthenticationStateProvider authStateProvider,
            CameraHelper cameraHelper,
            ImageCompressor imageCompressor,
            INetworkService networkService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _photoRepository = photoRepository ?? throw new ArgumentNullException(nameof(photoRepository));
            _photoSyncService = photoSyncService ?? throw new ArgumentNullException(nameof(photoSyncService));
            _locationService = locationService ?? throw new ArgumentNullException(nameof(locationService));
            _authStateProvider = authStateProvider ?? throw new ArgumentNullException(nameof(authStateProvider));
            _cameraHelper = cameraHelper ?? throw new ArgumentNullException(nameof(cameraHelper));
            _imageCompressor = imageCompressor ?? throw new ArgumentNullException(nameof(imageCompressor));
            _networkService = networkService ?? throw new ArgumentNullException(nameof(networkService));

            _logger.LogInformation("PhotoService initialized");
        }

        /// <summary>
        /// Captures a photo using the device camera, processes it, and stores it in the repository.
        /// </summary>
        /// <returns>A task that returns the captured photo model, or null if capture failed.</returns>
        public async Task<PhotoModel> CapturePhotoAsync()
        {
            try
            {
                _logger.LogInformation("Starting photo capture operation");

                // Check camera permission
                if (!await EnsureCameraPermissionAsync())
                {
                    return null;
                }

                // Capture photo using camera
                Stream photoStream = await _cameraHelper.CapturePhotoAsync();
                if (photoStream == null)
                {
                    _logger.LogInformation("Photo capture failed or was cancelled");
                    return null;
                }

                // Compress the photo to reduce storage size and bandwidth
                Stream compressedStream = await _imageCompressor.CompressImageAsync(photoStream);
                photoStream.Dispose(); // Dispose the original stream

                // Get current user from authentication state
                var authState = await _authStateProvider.GetCurrentState();
                if (!authState.IsAuthenticated)
                {
                    _logger.LogWarning("Cannot capture photo: User not authenticated");
                    compressedStream.Dispose();
                    return null;
                }

                // Get current location
                LocationModel location = await GetCurrentLocationSafeAsync();

                // Create a new photo model
                var photo = new PhotoModel
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = authState.PhoneNumber,
                    Timestamp = DateTime.UtcNow,
                    Latitude = location.Latitude,
                    Longitude = location.Longitude,
                    IsSynced = false,
                    SyncProgress = 0
                };

                // Save photo to repository
                string photoId = await _photoRepository.SavePhotoAsync(photo, compressedStream);
                compressedStream.Dispose();

                if (string.IsNullOrEmpty(photoId))
                {
                    _logger.LogError("Failed to save photo to repository");
                    return null;
                }

                // Update photo model with the ID returned from the repository
                photo.Id = photoId;

                // If network is available, begin synchronization in the background
                if (_networkService.IsConnected)
                {
                    _logger.LogInformation("Network available, triggering background photo upload for {PhotoId}", photoId);
                    // Fire and forget - don't await this
                    #pragma warning disable CS4014
                    _photoSyncService.UploadPhotoAsync(photoId);
                    #pragma warning restore CS4014
                }
                else
                {
                    _logger.LogInformation("Network unavailable, photo {PhotoId} will be synchronized later", photoId);
                }

                return photo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during photo capture: {Message}", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Retrieves all stored photos from the repository.
        /// </summary>
        /// <returns>A task that returns a list of all stored photo models.</returns>
        public async Task<List<PhotoModel>> GetStoredPhotosAsync()
        {
            try
            {
                _logger.LogInformation("Retrieving all stored photos");
                var photos = await _photoRepository.GetPhotosAsync();
                _logger.LogInformation("Retrieved {Count} photos", photos.Count);
                return photos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving stored photos: {Message}", ex.Message);
                return new List<PhotoModel>();
            }
        }

        /// <summary>
        /// Retrieves a specific photo by its ID.
        /// </summary>
        /// <param name="id">The ID of the photo to retrieve.</param>
        /// <returns>A task that returns the photo model with the specified ID, or null if not found.</returns>
        public async Task<PhotoModel> GetPhotoAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                _logger.LogWarning("Cannot get photo: ID is null or empty");
                return null;
            }

            try
            {
                _logger.LogInformation("Retrieving photo with ID: {PhotoId}", id);
                var photo = await _photoRepository.GetPhotoByIdAsync(id);
                
                if (photo == null)
                {
                    _logger.LogWarning("Photo with ID {PhotoId} not found", id);
                }
                
                return photo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving photo with ID {PhotoId}: {Message}", id, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Retrieves the image file stream for a photo by its ID.
        /// </summary>
        /// <param name="id">The ID of the photo to retrieve the file for.</param>
        /// <returns>A task that returns the image stream for the photo, or null if not found.</returns>
        public async Task<Stream> GetPhotoFileAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                _logger.LogWarning("Cannot get photo file: ID is null or empty");
                return null;
            }

            try
            {
                _logger.LogInformation("Retrieving photo file with ID: {PhotoId}", id);
                var stream = await _photoRepository.GetPhotoStreamAsync(id);
                
                if (stream == null)
                {
                    _logger.LogWarning("Photo file with ID {PhotoId} not found", id);
                }
                else
                {
                    _logger.LogInformation("Retrieved photo file {PhotoId} with size {Size} bytes", id, stream.Length);
                }
                
                return stream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving photo file with ID {PhotoId}: {Message}", id, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Deletes a photo and its associated image file.
        /// </summary>
        /// <param name="id">The ID of the photo to delete.</param>
        /// <returns>A task that returns true if the photo was deleted, false otherwise.</returns>
        public async Task<bool> DeletePhotoAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                _logger.LogWarning("Cannot delete photo: ID is null or empty");
                return false;
            }

            try
            {
                _logger.LogInformation("Deleting photo with ID: {PhotoId}", id);
                bool result = await _photoRepository.DeletePhotoAsync(id);
                
                if (result)
                {
                    _logger.LogInformation("Successfully deleted photo {PhotoId}", id);
                }
                else
                {
                    _logger.LogWarning("Failed to delete photo {PhotoId}", id);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting photo with ID {PhotoId}: {Message}", id, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Deletes photos older than the specified retention period that have been synchronized.
        /// </summary>
        /// <param name="retentionDays">The number of days to retain photos. Photos older than this will be deleted if synced.</param>
        /// <returns>A task that returns the number of photos deleted.</returns>
        public async Task<int> CleanupOldPhotosAsync(int retentionDays)
        {
            if (retentionDays <= 0)
            {
                _logger.LogWarning("Cannot cleanup photos: Invalid retention period ({RetentionDays} days)", retentionDays);
                return 0;
            }

            try
            {
                _logger.LogInformation("Cleaning up photos older than {RetentionDays} days", retentionDays);
                int deletedCount = await _photoRepository.CleanupOldPhotosAsync(retentionDays);
                _logger.LogInformation("Deleted {DeletedCount} old photos", deletedCount);
                return deletedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up old photos: {Message}", ex.Message);
                return 0;
            }
        }

        /// <summary>
        /// Retrieves the current storage usage for photos in bytes.
        /// </summary>
        /// <returns>A task that returns the total size of stored photos in bytes.</returns>
        public async Task<long> GetStorageUsageAsync()
        {
            try
            {
                _logger.LogInformation("Calculating photo storage usage");
                
                // Get all photos from repository
                var photos = await _photoRepository.GetPhotosAsync();
                _logger.LogInformation("Calculating storage for {Count} photos", photos.Count);
                
                long totalSize = 0;
                
                // Sum the size of each photo file
                foreach (var photo in photos)
                {
                    try
                    {
                        using var stream = await _photoRepository.GetPhotoStreamAsync(photo.Id);
                        if (stream != null)
                        {
                            totalSize += stream.Length;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error getting size for photo {PhotoId}: {Message}", photo.Id, ex.Message);
                        // Continue with next photo
                    }
                }
                
                _logger.LogInformation("Total photo storage usage: {TotalSize} bytes", totalSize);
                return totalSize;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating photo storage usage: {Message}", ex.Message);
                return 0;
            }
        }

        #region Helper Methods

        /// <summary>
        /// Ensures that camera permissions are granted, requesting them if necessary.
        /// </summary>
        /// <returns>True if permissions are granted, false otherwise.</returns>
        private async Task<bool> EnsureCameraPermissionAsync()
        {
            // Check if camera permission is granted
            bool hasPermission = await _cameraHelper.CheckCameraPermissionAsync();
            if (!hasPermission)
            {
                _logger.LogInformation("Camera permission not granted, requesting permission");
                hasPermission = await _cameraHelper.RequestCameraPermissionAsync();
                
                if (!hasPermission)
                {
                    _logger.LogWarning("Camera permission denied");
                    return false;
                }
            }
            
            return true;
        }

        /// <summary>
        /// Gets the current location safely, returning a default location if there's an error.
        /// </summary>
        /// <returns>The current location, or a default location if an error occurs.</returns>
        private async Task<LocationModel> GetCurrentLocationSafeAsync()
        {
            try
            {
                return await _locationService.GetCurrentLocation();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get current location for photo. Using default location.");
                return new LocationModel(); // Default location with just a timestamp
            }
        }

        #endregion
    }
}