using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;
using SecurityPatrol.Constants;
using SecurityPatrol.Database.Entities;
using SecurityPatrol.Models;
using SQLite;

namespace SecurityPatrol.Services
{
    /// <summary>
    /// Implements the IPhotoRepository interface to provide data access operations for photos in the Security Patrol application.
    /// </summary>
    public class PhotoRepository : IPhotoRepository
    {
        private readonly IDatabaseService _databaseService;
        private readonly ILogger<PhotoRepository> _logger;
        private readonly string _photoStoragePath;

        /// <summary>
        /// Initializes a new instance of the PhotoRepository class with the specified database service and logger.
        /// </summary>
        /// <param name="databaseService">The database service for accessing the SQLite database.</param>
        /// <param name="logger">The logger for logging operations and errors.</param>
        /// <exception cref="ArgumentNullException">Thrown when databaseService or logger is null.</exception>
        public PhotoRepository(IDatabaseService databaseService, ILogger<PhotoRepository> logger)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Initialize the photo storage path in the app's cache directory
            _photoStoragePath = Path.Combine(FileSystem.CacheDirectory, "Photos");
            
            // Ensure the photo storage directory exists
            EnsurePhotoDirectoryExistsAsync().Wait();
        }

        /// <summary>
        /// Saves a photo model and its associated image stream to storage.
        /// </summary>
        /// <param name="photo">The photo model to save.</param>
        /// <param name="imageStream">The image data stream.</param>
        /// <returns>A task that returns the ID of the saved photo.</returns>
        /// <exception cref="ArgumentNullException">Thrown when photo or imageStream is null.</exception>
        /// <exception cref="SQLiteException">Thrown when a database error occurs.</exception>
        /// <exception cref="IOException">Thrown when a file system error occurs.</exception>
        public async Task<string> SavePhotoAsync(PhotoModel photo, Stream imageStream)
        {
            if (photo == null)
                throw new ArgumentNullException(nameof(photo));
            
            if (imageStream == null)
                throw new ArgumentNullException(nameof(imageStream));

            _logger.LogInformation("Saving photo to repository");

            try
            {
                // Generate a unique ID if not provided
                if (string.IsNullOrEmpty(photo.Id))
                {
                    photo.Id = GenerateUniqueId();
                }

                // Set the file path for the photo
                photo.FilePath = GetPhotoFilePath(photo.Id);

                // Ensure the photo directory exists
                await EnsurePhotoDirectoryExistsAsync();

                // Save to the database and file system within a transaction
                await _databaseService.RunInTransactionAsync(async () =>
                {
                    var connection = await _databaseService.GetConnectionAsync();
                    var entity = photo.ToEntity();
                    
                    // Save the entity to the database
                    await connection.InsertOrReplaceAsync(entity);
                    
                    // Save the image to the file system
                    await SaveImageToFileAsync(imageStream, photo.FilePath);
                });

                _logger.LogInformation("Photo saved successfully with ID: {PhotoId}", photo.Id);
                return photo.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving photo: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Retrieves all photos from the database.
        /// </summary>
        /// <returns>A task that returns a list of all photo models.</returns>
        /// <exception cref="SQLiteException">Thrown when a database error occurs.</exception>
        public async Task<List<PhotoModel>> GetPhotosAsync()
        {
            _logger.LogInformation("Retrieving all photos from repository");

            try
            {
                var connection = await _databaseService.GetConnectionAsync();
                var entities = await connection.Table<PhotoEntity>().ToListAsync();
                
                var photos = new List<PhotoModel>();
                foreach (var entity in entities)
                {
                    photos.Add(PhotoModel.FromEntity(entity));
                }

                return photos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving photos: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Retrieves a photo by its ID.
        /// </summary>
        /// <param name="id">The unique identifier of the photo.</param>
        /// <returns>A task that returns the photo model with the specified ID, or null if not found.</returns>
        /// <exception cref="ArgumentException">Thrown when id is null or empty.</exception>
        /// <exception cref="SQLiteException">Thrown when a database error occurs.</exception>
        public async Task<PhotoModel> GetPhotoByIdAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Photo ID cannot be null or empty", nameof(id));

            _logger.LogInformation("Retrieving photo with ID: {PhotoId}", id);

            try
            {
                var connection = await _databaseService.GetConnectionAsync();
                var entity = await connection.Table<PhotoEntity>()
                    .Where(p => p.Id == id)
                    .FirstOrDefaultAsync();

                if (entity != null)
                {
                    return PhotoModel.FromEntity(entity);
                }

                _logger.LogWarning("Photo with ID {PhotoId} not found", id);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving photo with ID {PhotoId}: {Message}", id, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Retrieves photos that have not been synchronized with the backend.
        /// </summary>
        /// <returns>A task that returns a list of unsynchronized photo models.</returns>
        /// <exception cref="SQLiteException">Thrown when a database error occurs.</exception>
        public async Task<List<PhotoModel>> GetPendingPhotosAsync()
        {
            _logger.LogInformation("Retrieving pending photos from repository");

            try
            {
                var connection = await _databaseService.GetConnectionAsync();
                var entities = await connection.Table<PhotoEntity>()
                    .Where(p => !p.IsSynced)
                    .ToListAsync();

                var photos = new List<PhotoModel>();
                foreach (var entity in entities)
                {
                    photos.Add(PhotoModel.FromEntity(entity));
                }

                _logger.LogInformation("Found {Count} pending photos", photos.Count);
                return photos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending photos: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Retrieves the image stream for a photo by its ID.
        /// </summary>
        /// <param name="id">The unique identifier of the photo.</param>
        /// <returns>A task that returns the image stream for the photo, or null if not found.</returns>
        /// <exception cref="ArgumentException">Thrown when id is null or empty.</exception>
        /// <exception cref="SQLiteException">Thrown when a database error occurs.</exception>
        /// <exception cref="IOException">Thrown when a file system error occurs.</exception>
        public async Task<Stream> GetPhotoStreamAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Photo ID cannot be null or empty", nameof(id));

            _logger.LogInformation("Retrieving photo stream for ID: {PhotoId}", id);

            try
            {
                var connection = await _databaseService.GetConnectionAsync();
                var entity = await connection.Table<PhotoEntity>()
                    .Where(p => p.Id == id)
                    .FirstOrDefaultAsync();

                if (entity == null)
                {
                    _logger.LogWarning("Photo with ID {PhotoId} not found", id);
                    return null;
                }

                if (File.Exists(entity.FilePath))
                {
                    return new FileStream(entity.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                }
                else
                {
                    _logger.LogWarning("Photo file not found for ID {PhotoId}: {FilePath}", id, entity.FilePath);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving photo stream for ID {PhotoId}: {Message}", id, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Updates the synchronization status of a photo.
        /// </summary>
        /// <param name="id">The unique identifier of the photo.</param>
        /// <param name="isSynced">A value indicating whether the photo is synchronized with the backend.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">Thrown when id is null or empty.</exception>
        /// <exception cref="SQLiteException">Thrown when a database error occurs.</exception>
        public async Task UpdateSyncStatusAsync(string id, bool isSynced)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Photo ID cannot be null or empty", nameof(id));

            _logger.LogInformation("Updating sync status for photo ID: {PhotoId} to {IsSynced}", id, isSynced);

            try
            {
                var connection = await _databaseService.GetConnectionAsync();
                var entity = await connection.Table<PhotoEntity>()
                    .Where(p => p.Id == id)
                    .FirstOrDefaultAsync();

                if (entity == null)
                {
                    _logger.LogWarning("Cannot update sync status: Photo with ID {PhotoId} not found", id);
                    return;
                }

                entity.IsSynced = isSynced;
                await connection.UpdateAsync(entity);
                
                _logger.LogInformation("Sync status updated successfully for photo ID: {PhotoId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating sync status for photo ID {PhotoId}: {Message}", id, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Updates the synchronization progress of a photo.
        /// </summary>
        /// <param name="id">The unique identifier of the photo.</param>
        /// <param name="progress">The synchronization progress as a percentage (0-100).</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">Thrown when id is null or empty or progress is out of range.</exception>
        /// <exception cref="SQLiteException">Thrown when a database error occurs.</exception>
        public async Task UpdateSyncProgressAsync(string id, int progress)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Photo ID cannot be null or empty", nameof(id));
            
            if (progress < 0 || progress > 100)
                throw new ArgumentException("Progress must be between 0 and 100", nameof(progress));

            _logger.LogInformation("Updating sync progress for photo ID: {PhotoId} to {Progress}%", id, progress);

            try
            {
                var connection = await _databaseService.GetConnectionAsync();
                var entity = await connection.Table<PhotoEntity>()
                    .Where(p => p.Id == id)
                    .FirstOrDefaultAsync();

                if (entity == null)
                {
                    _logger.LogWarning("Cannot update sync progress: Photo with ID {PhotoId} not found", id);
                    return;
                }

                entity.SyncProgress = progress;
                await connection.UpdateAsync(entity);
                
                _logger.LogInformation("Sync progress updated successfully for photo ID: {PhotoId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating sync progress for photo ID {PhotoId}: {Message}", id, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Updates the remote ID of a photo after successful synchronization.
        /// </summary>
        /// <param name="id">The unique identifier of the photo.</param>
        /// <param name="remoteId">The remote identifier assigned by the backend.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">Thrown when id or remoteId is null or empty.</exception>
        /// <exception cref="SQLiteException">Thrown when a database error occurs.</exception>
        public async Task UpdateRemoteIdAsync(string id, string remoteId)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Photo ID cannot be null or empty", nameof(id));
            
            if (string.IsNullOrEmpty(remoteId))
                throw new ArgumentException("Remote ID cannot be null or empty", nameof(remoteId));

            _logger.LogInformation("Updating remote ID for photo ID: {PhotoId} to {RemoteId}", id, remoteId);

            try
            {
                var connection = await _databaseService.GetConnectionAsync();
                var entity = await connection.Table<PhotoEntity>()
                    .Where(p => p.Id == id)
                    .FirstOrDefaultAsync();

                if (entity == null)
                {
                    _logger.LogWarning("Cannot update remote ID: Photo with ID {PhotoId} not found", id);
                    return;
                }

                entity.RemoteId = remoteId;
                await connection.UpdateAsync(entity);
                
                _logger.LogInformation("Remote ID updated successfully for photo ID: {PhotoId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating remote ID for photo ID {PhotoId}: {Message}", id, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Deletes a photo and its associated image file.
        /// </summary>
        /// <param name="id">The unique identifier of the photo.</param>
        /// <returns>A task that returns true if the photo was deleted, false otherwise.</returns>
        /// <exception cref="ArgumentException">Thrown when id is null or empty.</exception>
        /// <exception cref="SQLiteException">Thrown when a database error occurs.</exception>
        /// <exception cref="IOException">Thrown when a file system error occurs.</exception>
        public async Task<bool> DeletePhotoAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Photo ID cannot be null or empty", nameof(id));

            _logger.LogInformation("Deleting photo with ID: {PhotoId}", id);

            try
            {
                var connection = await _databaseService.GetConnectionAsync();
                var entity = await connection.Table<PhotoEntity>()
                    .Where(p => p.Id == id)
                    .FirstOrDefaultAsync();

                if (entity == null)
                {
                    _logger.LogWarning("Cannot delete: Photo with ID {PhotoId} not found", id);
                    return false;
                }

                // Delete within a transaction to ensure consistency
                await _databaseService.RunInTransactionAsync(async () =>
                {
                    // Delete from the database
                    await connection.DeleteAsync<PhotoEntity>(entity.Id);
                    
                    // Delete the file if it exists
                    if (File.Exists(entity.FilePath))
                    {
                        File.Delete(entity.FilePath);
                    }
                });

                _logger.LogInformation("Photo deleted successfully: {PhotoId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting photo with ID {PhotoId}: {Message}", id, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Deletes photos older than the specified retention period that have been synchronized.
        /// </summary>
        /// <param name="retentionDays">The number of days to retain photos.</param>
        /// <returns>A task that returns the number of photos deleted.</returns>
        /// <exception cref="ArgumentException">Thrown when retentionDays is less than or equal to 0.</exception>
        /// <exception cref="SQLiteException">Thrown when a database error occurs.</exception>
        /// <exception cref="IOException">Thrown when a file system error occurs.</exception>
        public async Task<int> CleanupOldPhotosAsync(int retentionDays)
        {
            if (retentionDays <= 0)
                throw new ArgumentException("Retention days must be greater than 0", nameof(retentionDays));

            _logger.LogInformation("Cleaning up photos older than {RetentionDays} days", retentionDays);

            try
            {
                var cutoffDate = DateTime.Now.AddDays(-retentionDays);
                var connection = await _databaseService.GetConnectionAsync();
                
                // Find synced photos older than the cutoff date
                var entities = await connection.Table<PhotoEntity>()
                    .Where(p => p.IsSynced && p.Timestamp < cutoffDate)
                    .ToListAsync();

                if (entities.Count == 0)
                {
                    _logger.LogInformation("No photos found to clean up");
                    return 0;
                }

                // Delete within a transaction to ensure consistency
                await _databaseService.RunInTransactionAsync(async () =>
                {
                    foreach (var entity in entities)
                    {
                        // Delete the file if it exists
                        if (File.Exists(entity.FilePath))
                        {
                            File.Delete(entity.FilePath);
                        }
                    }

                    // Delete all photos from the database at once
                    var photoIds = entities.Select(e => e.Id).ToList();
                    await connection.ExecuteAsync($"DELETE FROM {DatabaseConstants.TablePhoto} WHERE {DatabaseConstants.ColumnId} IN ({string.Join(",", photoIds.Select(id => $"'{id}'"))})");
                });

                _logger.LogInformation("Successfully cleaned up {Count} photos", entities.Count);
                return entities.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up old photos: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Ensures that the photo storage directory exists.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task EnsurePhotoDirectoryExistsAsync()
        {
            try
            {
                if (!Directory.Exists(_photoStoragePath))
                {
                    Directory.CreateDirectory(_photoStoragePath);
                    _logger.LogInformation("Created photo storage directory: {DirectoryPath}", _photoStoragePath);
                }
                await Task.CompletedTask; // To make the method truly async
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring photo directory exists: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Saves an image stream to a file at the specified path.
        /// </summary>
        /// <param name="imageStream">The image stream to save.</param>
        /// <param name="filePath">The path where the image should be saved.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task SaveImageToFileAsync(Stream imageStream, string filePath)
        {
            if (imageStream == null)
                throw new ArgumentNullException(nameof(imageStream));
            
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            try
            {
                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await imageStream.CopyToAsync(fileStream);
                }
                
                _logger.LogInformation("Image saved to file: {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving image to file {FilePath}: {Message}", filePath, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Generates a unique ID for a photo.
        /// </summary>
        /// <returns>A unique ID string.</returns>
        private string GenerateUniqueId()
        {
            return Guid.NewGuid().ToString("N");
        }

        /// <summary>
        /// Gets the file path for a photo with the specified ID.
        /// </summary>
        /// <param name="photoId">The unique identifier of the photo.</param>
        /// <returns>The full file path for the photo.</returns>
        private string GetPhotoFilePath(string photoId)
        {
            if (string.IsNullOrEmpty(photoId))
                throw new ArgumentException("Photo ID cannot be null or empty", nameof(photoId));
                
            return Path.Combine(_photoStoragePath, $"{photoId}.jpg");
        }
    }
}