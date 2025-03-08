using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SecurityPatrol.Models;

namespace SecurityPatrol.Services
{
    /// <summary>
    /// Interface for a repository that manages photo data storage and retrieval operations in the Security Patrol application.
    /// </summary>
    public interface IPhotoRepository
    {
        /// <summary>
        /// Saves a photo model and its associated image stream to storage.
        /// </summary>
        /// <param name="photo">The photo model to save.</param>
        /// <param name="imageStream">The image data stream.</param>
        /// <returns>A task that returns the ID of the saved photo.</returns>
        Task<string> SavePhotoAsync(PhotoModel photo, Stream imageStream);

        /// <summary>
        /// Retrieves all photos from the database.
        /// </summary>
        /// <returns>A task that returns a list of all photo models.</returns>
        Task<List<PhotoModel>> GetPhotosAsync();

        /// <summary>
        /// Retrieves a photo by its ID.
        /// </summary>
        /// <param name="id">The unique identifier of the photo.</param>
        /// <returns>A task that returns the photo model with the specified ID, or null if not found.</returns>
        Task<PhotoModel> GetPhotoByIdAsync(string id);

        /// <summary>
        /// Retrieves photos that have not been synchronized with the backend.
        /// </summary>
        /// <returns>A task that returns a list of unsynchronized photo models.</returns>
        Task<List<PhotoModel>> GetPendingPhotosAsync();

        /// <summary>
        /// Retrieves the image stream for a photo by its ID.
        /// </summary>
        /// <param name="id">The unique identifier of the photo.</param>
        /// <returns>A task that returns the image stream for the photo, or null if not found.</returns>
        Task<Stream> GetPhotoStreamAsync(string id);

        /// <summary>
        /// Updates the synchronization status of a photo.
        /// </summary>
        /// <param name="id">The unique identifier of the photo.</param>
        /// <param name="isSynced">A value indicating whether the photo is synchronized with the backend.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpdateSyncStatusAsync(string id, bool isSynced);

        /// <summary>
        /// Updates the synchronization progress of a photo.
        /// </summary>
        /// <param name="id">The unique identifier of the photo.</param>
        /// <param name="progress">The synchronization progress as a percentage (0-100).</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpdateSyncProgressAsync(string id, int progress);

        /// <summary>
        /// Updates the remote ID of a photo after successful synchronization.
        /// </summary>
        /// <param name="id">The unique identifier of the photo.</param>
        /// <param name="remoteId">The remote identifier assigned by the backend.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpdateRemoteIdAsync(string id, string remoteId);

        /// <summary>
        /// Deletes a photo and its associated image file.
        /// </summary>
        /// <param name="id">The unique identifier of the photo.</param>
        /// <returns>A task that returns true if the photo was deleted, false otherwise.</returns>
        Task<bool> DeletePhotoAsync(string id);

        /// <summary>
        /// Deletes photos older than the specified retention period that have been synchronized.
        /// </summary>
        /// <param name="retentionDays">The number of days to retain photos.</param>
        /// <returns>A task that returns the number of photos deleted.</returns>
        Task<int> CleanupOldPhotosAsync(int retentionDays);
    }
}