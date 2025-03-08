using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SecurityPatrol.Models;

namespace SecurityPatrol.Services
{
    /// <summary>
    /// Interface that defines the contract for services that provide photo capture, storage, 
    /// and management functionality for the Security Patrol application.
    /// </summary>
    public interface IPhotoService
    {
        /// <summary>
        /// Captures a photo using the device camera, processes it, and stores it in the repository.
        /// </summary>
        /// <returns>A task that returns the captured photo model, or null if capture failed.</returns>
        Task<PhotoModel> CapturePhotoAsync();

        /// <summary>
        /// Retrieves all stored photos from the repository.
        /// </summary>
        /// <returns>A task that returns a list of all stored photo models.</returns>
        Task<List<PhotoModel>> GetStoredPhotosAsync();

        /// <summary>
        /// Retrieves a specific photo by its ID.
        /// </summary>
        /// <param name="id">The ID of the photo to retrieve.</param>
        /// <returns>A task that returns the photo model with the specified ID, or null if not found.</returns>
        Task<PhotoModel> GetPhotoAsync(string id);

        /// <summary>
        /// Retrieves the image file stream for a photo by its ID.
        /// </summary>
        /// <param name="id">The ID of the photo to retrieve the file for.</param>
        /// <returns>A task that returns the image stream for the photo, or null if not found.</returns>
        Task<Stream> GetPhotoFileAsync(string id);

        /// <summary>
        /// Deletes a photo and its associated image file.
        /// </summary>
        /// <param name="id">The ID of the photo to delete.</param>
        /// <returns>A task that returns true if the photo was deleted, false otherwise.</returns>
        Task<bool> DeletePhotoAsync(string id);

        /// <summary>
        /// Deletes photos older than the specified retention period that have been synchronized.
        /// </summary>
        /// <param name="retentionDays">The number of days to retain photos. Photos older than this will be deleted if synced.</param>
        /// <returns>A task that returns the number of photos deleted.</returns>
        Task<int> CleanupOldPhotosAsync(int retentionDays);

        /// <summary>
        /// Retrieves the current storage usage for photos in bytes.
        /// </summary>
        /// <returns>A task that returns the total size of stored photos in bytes.</returns>
        Task<long> GetStorageUsageAsync();
    }
}