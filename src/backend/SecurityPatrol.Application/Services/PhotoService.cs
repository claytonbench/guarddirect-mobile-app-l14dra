using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.Core.Models;

namespace SecurityPatrol.Application.Services
{
    /// <summary>
    /// Implementation of the IPhotoService interface that provides photo management
    /// functionality for the Security Patrol application.
    /// </summary>
    public class PhotoService : IPhotoService
    {
        private readonly IPhotoRepository _photoRepository;
        private readonly IStorageService _storageService;

        /// <summary>
        /// Initializes a new instance of the PhotoService class with required dependencies.
        /// </summary>
        /// <param name="photoRepository">The repository for photo data access operations.</param>
        /// <param name="storageService">The service for file storage operations.</param>
        public PhotoService(IPhotoRepository photoRepository, IStorageService storageService)
        {
            _photoRepository = photoRepository ?? throw new ArgumentNullException(nameof(photoRepository));
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        }

        /// <summary>
        /// Uploads a photo with the provided metadata and binary data.
        /// </summary>
        /// <param name="request">The metadata for the photo being uploaded.</param>
        /// <param name="photoStream">The binary stream containing the photo data.</param>
        /// <param name="contentType">The MIME content type of the photo (e.g., "image/jpeg").</param>
        /// <returns>A result containing the upload response with ID and status if successful.</returns>
        public async Task<Result<PhotoUploadResponse>> UploadPhotoAsync(PhotoUploadRequest request, Stream photoStream, string contentType)
        {
            if (request == null)
                return Result.Failure<PhotoUploadResponse>("Upload request cannot be null");

            if (photoStream == null || !photoStream.CanRead)
                return Result.Failure<PhotoUploadResponse>("Photo stream cannot be null or unreadable");

            if (string.IsNullOrWhiteSpace(contentType))
                return Result.Failure<PhotoUploadResponse>("Content type must be specified");

            // Generate a unique filename based on timestamp and user ID
            var fileName = $"{request.Timestamp:yyyyMMddHHmmss}_{request.UserId}_{Guid.NewGuid():N}.jpg";

            // Store the photo file using the storage service
            var storageResult = await _storageService.StoreFileAsync(photoStream, fileName, contentType);
            if (!storageResult.Succeeded)
                return Result.Failure<PhotoUploadResponse>($"Failed to store photo: {storageResult.Message}");

            // Create a new photo entity with the metadata from the request
            var photo = new Photo
            {
                UserId = request.UserId,
                Timestamp = request.Timestamp,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                FilePath = storageResult.Data
            };

            // Add the photo to the repository
            var addResult = await _photoRepository.AddAsync(photo);
            if (!addResult.Succeeded)
            {
                // If adding to the repository fails, try to clean up the stored file
                await _storageService.DeleteFileAsync(storageResult.Data);
                return Result.Failure<PhotoUploadResponse>($"Failed to save photo metadata: {addResult.Message}");
            }

            // Create and return the response
            var response = new PhotoUploadResponse
            {
                Id = addResult.Data.ToString(),
                Status = "success"
            };

            return Result.Success(response);
        }

        /// <summary>
        /// Retrieves a photo by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the photo.</param>
        /// <returns>A result containing the photo if found.</returns>
        public async Task<Result<Photo>> GetPhotoAsync(int id)
        {
            if (id <= 0)
                return Result.Failure<Photo>("Invalid photo ID");

            var photo = await _photoRepository.GetByIdAsync(id);
            if (photo == null)
                return Result.Failure<Photo>("Photo not found");

            return Result.Success(photo);
        }

        /// <summary>
        /// Retrieves the binary data of a photo by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the photo.</param>
        /// <returns>A result containing the photo binary data if found.</returns>
        public async Task<Result<Stream>> GetPhotoStreamAsync(int id)
        {
            if (id <= 0)
                return Result.Failure<Stream>("Invalid photo ID");

            var photo = await _photoRepository.GetByIdAsync(id);
            if (photo == null)
                return Result.Failure<Stream>("Photo not found");

            // Check if the file exists
            bool fileExists = await _storageService.FileExistsAsync(photo.FilePath);
            if (!fileExists)
                return Result.Failure<Stream>("Photo file not found");

            // Get the file stream
            var fileResult = await _storageService.GetFileAsync(photo.FilePath);
            if (!fileResult.Succeeded)
                return Result.Failure<Stream>($"Failed to retrieve photo file: {fileResult.Message}");

            return Result.Success(fileResult.Data);
        }

        /// <summary>
        /// Retrieves all photos for a specific user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>A result containing a collection of photos belonging to the specified user.</returns>
        public async Task<Result<IEnumerable<Photo>>> GetPhotosByUserIdAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return Result.Failure<IEnumerable<Photo>>("User ID cannot be null or empty");

            var photos = await _photoRepository.GetByUserIdAsync(userId);
            return Result.Success(photos);
        }

        /// <summary>
        /// Retrieves a paginated list of photos for a specific user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="pageNumber">The page number to retrieve (1-based).</param>
        /// <param name="pageSize">The number of photos per page.</param>
        /// <returns>A result containing a paginated list of photos belonging to the specified user.</returns>
        public async Task<Result<PaginatedList<Photo>>> GetPaginatedPhotosByUserIdAsync(string userId, int pageNumber, int pageSize)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return Result.Failure<PaginatedList<Photo>>("User ID cannot be null or empty");

            if (pageNumber <= 0)
                return Result.Failure<PaginatedList<Photo>>("Page number must be greater than 0");

            if (pageSize <= 0)
                return Result.Failure<PaginatedList<Photo>>("Page size must be greater than 0");

            var paginatedPhotos = await _photoRepository.GetPaginatedByUserIdAsync(userId, pageNumber, pageSize);
            return Result.Success(paginatedPhotos);
        }

        /// <summary>
        /// Retrieves photos within a specified radius of a geographic location.
        /// </summary>
        /// <param name="latitude">The latitude coordinate of the center point.</param>
        /// <param name="longitude">The longitude coordinate of the center point.</param>
        /// <param name="radiusInMeters">The search radius in meters.</param>
        /// <returns>A result containing a collection of photos within the specified radius of the location.</returns>
        public async Task<Result<IEnumerable<Photo>>> GetPhotosByLocationAsync(double latitude, double longitude, double radiusInMeters)
        {
            // Validate latitude and longitude
            if (latitude < -90 || latitude > 90)
                return Result.Failure<IEnumerable<Photo>>("Latitude must be between -90 and 90 degrees");

            if (longitude < -180 || longitude > 180)
                return Result.Failure<IEnumerable<Photo>>("Longitude must be between -180 and 180 degrees");

            if (radiusInMeters <= 0)
                return Result.Failure<IEnumerable<Photo>>("Radius must be greater than 0 meters");

            var photos = await _photoRepository.GetByLocationAsync(latitude, longitude, radiusInMeters);
            return Result.Success(photos);
        }

        /// <summary>
        /// Retrieves photos captured within a specified date range.
        /// </summary>
        /// <param name="startDate">The start date and time of the range.</param>
        /// <param name="endDate">The end date and time of the range.</param>
        /// <returns>A result containing a collection of photos captured within the specified date range.</returns>
        public async Task<Result<IEnumerable<Photo>>> GetPhotosByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            if (startDate == default)
                return Result.Failure<IEnumerable<Photo>>("Start date cannot be the default value");

            if (endDate == default)
                return Result.Failure<IEnumerable<Photo>>("End date cannot be the default value");

            if (startDate > endDate)
                return Result.Failure<IEnumerable<Photo>>("Start date must be before or equal to end date");

            var photos = await _photoRepository.GetByDateRangeAsync(startDate, endDate);
            return Result.Success(photos);
        }

        /// <summary>
        /// Deletes a photo by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the photo to delete.</param>
        /// <returns>A result indicating success or failure of the delete operation.</returns>
        public async Task<Result> DeletePhotoAsync(int id)
        {
            if (id <= 0)
                return Result.Failure("Invalid photo ID");

            // Check if the photo exists
            bool exists = await _photoRepository.ExistsAsync(id);
            if (!exists)
                return Result.Failure("Photo not found");

            // Get the photo to retrieve its file path
            var photo = await _photoRepository.GetByIdAsync(id);
            
            // Delete the photo file
            var fileDeleteResult = await _storageService.DeleteFileAsync(photo.FilePath);
            if (!fileDeleteResult.Succeeded)
            {
                // Log the error but continue with database deletion
                // In a real application, you would use a proper logging system
                Console.WriteLine($"Failed to delete photo file: {fileDeleteResult.Message}");
            }

            // Delete the photo record from the database
            var deleteResult = await _photoRepository.DeleteAsync(id);
            if (!deleteResult.Succeeded)
                return Result.Failure($"Failed to delete photo record: {deleteResult.Message}");

            return Result.Success();
        }
    }
}