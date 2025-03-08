using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SecurityPatrol.Models;

namespace SecurityPatrol.Services
{
    /// <summary>
    /// Defines the contract for photo synchronization services in the Security Patrol application.
    /// This service is responsible for managing the synchronization of photos between 
    /// the local device storage and the backend API, including tracking upload progress,
    /// handling retries for failed uploads, and managing network connectivity changes.
    /// </summary>
    public interface IPhotoSyncService
    {
        /// <summary>
        /// Event that is raised when the upload progress of a photo changes.
        /// </summary>
        event EventHandler<PhotoUploadProgress> UploadProgressChanged;

        /// <summary>
        /// Synchronizes all unsynchronized photos with the backend API.
        /// </summary>
        /// <returns>True if all photos were synchronized successfully, false otherwise.</returns>
        Task<bool> SyncPhotosAsync();

        /// <summary>
        /// Uploads a specific photo to the backend API.
        /// </summary>
        /// <param name="id">The ID of the photo to upload.</param>
        /// <returns>True if the photo was uploaded successfully, false otherwise.</returns>
        Task<bool> UploadPhotoAsync(string id);

        /// <summary>
        /// Cancels an ongoing photo upload.
        /// </summary>
        /// <param name="id">The ID of the photo upload to cancel.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task CancelUploadAsync(string id);

        /// <summary>
        /// Retrieves the current upload progress for a specific photo.
        /// </summary>
        /// <param name="id">The ID of the photo.</param>
        /// <returns>The upload progress for the specified photo, or null if not found.</returns>
        Task<PhotoUploadProgress> GetUploadProgressAsync(string id);

        /// <summary>
        /// Retrieves the upload progress for all photos that are currently being uploaded or queued for upload.
        /// </summary>
        /// <returns>A list of upload progress objects.</returns>
        Task<List<PhotoUploadProgress>> GetAllUploadProgressAsync();

        /// <summary>
        /// Retries uploading photos that previously failed to upload.
        /// </summary>
        /// <returns>True if all retries were successful, false otherwise.</returns>
        Task<bool> RetryFailedUploadsAsync();

        /// <summary>
        /// Checks if any photo synchronization is currently in progress.
        /// </summary>
        /// <returns>True if any synchronization is in progress, false otherwise.</returns>
        Task<bool> IsSyncInProgressAsync();
    }
}