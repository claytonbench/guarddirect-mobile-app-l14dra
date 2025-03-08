using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SecurityPatrol.Constants;
using SecurityPatrol.Models;

namespace SecurityPatrol.Services
{
    /// <summary>
    /// Service that manages the synchronization of photos between the local device storage and the backend API,
    /// including tracking upload progress, handling retries, and managing network connectivity changes.
    /// </summary>
    public class PhotoSyncService : IPhotoSyncService, IDisposable
    {
        private readonly IPhotoRepository _photoRepository;
        private readonly IApiService _apiService;
        private readonly INetworkService _networkService;
        private readonly Dictionary<string, PhotoUploadProgress> _uploadProgress;
        private readonly Dictionary<string, CancellationTokenSource> _uploadCancellationTokens;
        private readonly SemaphoreSlim _syncLock;
        private readonly int _maxConcurrentUploads;
        private readonly int _maxRetryAttempts;
        private bool _isSyncInProgress;

        /// <summary>
        /// Event that is raised when the upload progress of a photo changes.
        /// </summary>
        public event EventHandler<PhotoUploadProgress> UploadProgressChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="PhotoSyncService"/> class.
        /// </summary>
        /// <param name="photoRepository">The repository for photo data access.</param>
        /// <param name="apiService">The service for API communication.</param>
        /// <param name="networkService">The service for monitoring network connectivity.</param>
        /// <exception cref="ArgumentNullException">Thrown if any required dependency is null.</exception>
        public PhotoSyncService(
            IPhotoRepository photoRepository,
            IApiService apiService,
            INetworkService networkService)
        {
            _photoRepository = photoRepository ?? throw new ArgumentNullException(nameof(photoRepository));
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _networkService = networkService ?? throw new ArgumentNullException(nameof(networkService));
            
            _uploadProgress = new Dictionary<string, PhotoUploadProgress>();
            _uploadCancellationTokens = new Dictionary<string, CancellationTokenSource>();
            _syncLock = new SemaphoreSlim(1, 1);
            _maxConcurrentUploads = 3; // Configurable limit for concurrent uploads
            _maxRetryAttempts = 3; // Configurable maximum retry attempts
            _isSyncInProgress = false;
            
            // Subscribe to network connectivity changes
            _networkService.ConnectivityChanged += HandleConnectivityChanged;
        }

        /// <summary>
        /// Synchronizes all unsynchronized photos with the backend API.
        /// </summary>
        /// <returns>True if all photos were synchronized successfully, false otherwise.</returns>
        public async Task<bool> SyncPhotosAsync()
        {
            // Check network connectivity
            if (!_networkService.IsConnected)
            {
                return false;
            }

            // Use a lock to prevent multiple concurrent sync operations
            await _syncLock.WaitAsync();
            try
            {
                // Get all pending photos that need to be synced
                var pendingPhotos = await _photoRepository.GetPendingPhotosAsync();
                if (pendingPhotos == null || pendingPhotos.Count == 0)
                {
                    return true; // No photos to sync
                }

                _isSyncInProgress = true;
                var successCount = 0;
                var tasks = new List<Task<bool>>();
                var semaphore = new SemaphoreSlim(_maxConcurrentUploads);

                // Start uploading photos with concurrency control
                foreach (var photo in pendingPhotos)
                {
                    await semaphore.WaitAsync(); // Wait until we can add another concurrent upload
                    
                    // Start the upload without awaiting its completion here
                    var uploadTask = Task.Run(async () =>
                    {
                        try
                        {
                            var result = await UploadPhotoAsync(photo.Id);
                            if (result)
                            {
                                Interlocked.Increment(ref successCount);
                            }
                            return result;
                        }
                        finally
                        {
                            semaphore.Release(); // Release the semaphore when upload completes
                        }
                    });
                    
                    tasks.Add(uploadTask);
                }

                // Wait for all uploads to complete
                await Task.WhenAll(tasks);
                
                _isSyncInProgress = false;
                return successCount == pendingPhotos.Count;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in SyncPhotosAsync: {ex.Message}");
                _isSyncInProgress = false;
                return false;
            }
            finally
            {
                _syncLock.Release();
            }
        }

        /// <summary>
        /// Uploads a specific photo to the backend API.
        /// </summary>
        /// <param name="id">The ID of the photo to upload.</param>
        /// <returns>True if the photo was uploaded successfully, false otherwise.</returns>
        public async Task<bool> UploadPhotoAsync(string id)
        {
            // Check network connectivity
            if (!_networkService.IsConnected)
            {
                return false;
            }

            // Get the photo from repository
            var photo = await _photoRepository.GetPhotoByIdAsync(id);
            if (photo == null || photo.IsSynced)
            {
                return false;
            }

            // Create or update progress tracking
            var progress = new PhotoUploadProgress(id);
            lock (_uploadProgress)
            {
                _uploadProgress[id] = progress;
            }
            OnUploadProgressChanged(progress);

            // Create cancellation token for this upload
            var cancellationTokenSource = new CancellationTokenSource();
            lock (_uploadCancellationTokens)
            {
                if (_uploadCancellationTokens.ContainsKey(id))
                {
                    _uploadCancellationTokens[id].Dispose();
                }
                _uploadCancellationTokens[id] = cancellationTokenSource;
            }

            Stream photoStream = null;
            try
            {
                // Get the photo file stream
                photoStream = await _photoRepository.GetPhotoStreamAsync(id);
                if (photoStream == null)
                {
                    progress.SetError("Photo file not found");
                    OnUploadProgressChanged(progress);
                    return false;
                }

                // Create multipart form content for the API request
                var content = new MultipartFormDataContent();
                
                // Add photo metadata
                var photoRequest = PhotoUploadRequest.FromPhotoModel(photo);
                content.Add(new StringContent(photoRequest.Timestamp.ToString("o")), "timestamp");
                content.Add(new StringContent(photoRequest.Latitude.ToString()), "latitude");
                content.Add(new StringContent(photoRequest.Longitude.ToString()), "longitude");
                content.Add(new StringContent(photoRequest.UserId), "userId");
                
                // Add the photo file
                var fileContent = new StreamContent(photoStream);
                content.Add(fileContent, "image", $"{id}.jpg");

                // Set initial progress
                progress.UpdateProgress(0);
                OnUploadProgressChanged(progress);
                await _photoRepository.UpdateSyncProgressAsync(id, 0);

                // Upload the photo
                PhotoUploadResponse response;
                try
                {
                    // Since we don't have direct progress reporting from IApiService,
                    // we'll simulate progress updates
                    var simulatedProgress = new Progress<int>(progressValue =>
                    {
                        progress.UpdateProgress(progressValue);
                        OnUploadProgressChanged(progress);
                        _photoRepository.UpdateSyncProgressAsync(id, progressValue).ConfigureAwait(false);
                    });

                    // Start a task to simulate progress updates
                    var progressTask = Task.Run(async () =>
                    {
                        for (int i = 0; i <= 90; i += 10) // Cap at 90% until we know it succeeded
                        {
                            if (cancellationTokenSource.Token.IsCancellationRequested)
                                break;
                                
                            ((IProgress<int>)simulatedProgress).Report(i);
                            await Task.Delay(300, cancellationTokenSource.Token);
                        }
                    }, cancellationTokenSource.Token);

                    // Perform the actual upload
                    response = await _apiService.PostMultipartAsync<PhotoUploadResponse>(
                        ApiEndpoints.PhotosUpload,
                        content,
                        true);
                    
                    // Try to stop the progress simulation
                    try
                    {
                        cancellationTokenSource.Cancel();
                        await Task.WhenAny(progressTask, Task.Delay(100));
                    }
                    catch
                    {
                        // Ignore any exceptions from cancelling the progress task
                    }
                }
                catch (Exception ex)
                {
                    // Handle API exceptions
                    progress.SetError($"Upload failed: {ex.Message}");
                    OnUploadProgressChanged(progress);
                    await _photoRepository.UpdateSyncProgressAsync(id, 0);
                    return false;
                }

                // Process the response
                if (response != null && response.IsSuccess())
                {
                    // Update the photo model with remote ID
                    response.UpdatePhotoModel(photo);
                    
                    // Update the repository
                    await _photoRepository.UpdateSyncStatusAsync(id, true);
                    await _photoRepository.UpdateRemoteIdAsync(id, response.Id);
                    
                    // Update progress to completed
                    progress.UpdateProgress(100);
                    await _photoRepository.UpdateSyncProgressAsync(id, 100);
                    
                    // Clean up resources
                    lock (_uploadCancellationTokens)
                    {
                        if (_uploadCancellationTokens.ContainsKey(id))
                        {
                            _uploadCancellationTokens.Remove(id);
                        }
                    }
                    
                    OnUploadProgressChanged(progress);
                    return true;
                }
                else
                {
                    // Upload failed
                    string errorMessage = response != null ? 
                        $"Upload failed: {response.Status}" : 
                        "Upload failed: No response from server";
                    
                    progress.SetError(errorMessage);
                    OnUploadProgressChanged(progress);
                    return false;
                }
            }
            catch (OperationCanceledException)
            {
                // Upload was cancelled
                progress.Cancel();
                OnUploadProgressChanged(progress);
                return false;
            }
            catch (Exception ex)
            {
                // Handle general exceptions
                progress.SetError($"Exception during upload: {ex.Message}");
                OnUploadProgressChanged(progress);
                return false;
            }
            finally
            {
                // Dispose the file stream
                photoStream?.Dispose();
                
                // Dispose the cancellation token if needed
                if (cancellationTokenSource != null)
                {
                    lock (_uploadCancellationTokens)
                    {
                        if (_uploadCancellationTokens.ContainsKey(id) &&
                            _uploadCancellationTokens[id] == cancellationTokenSource)
                        {
                            _uploadCancellationTokens.Remove(id);
                        }
                    }
                    cancellationTokenSource.Dispose();
                }
            }
        }

        /// <summary>
        /// Cancels an ongoing photo upload.
        /// </summary>
        /// <param name="id">The ID of the photo upload to cancel.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task CancelUploadAsync(string id)
        {
            CancellationTokenSource tokenSource = null;
            
            // Get the cancellation token source for this upload
            lock (_uploadCancellationTokens)
            {
                if (_uploadCancellationTokens.TryGetValue(id, out tokenSource))
                {
                    // Cancel the upload
                    tokenSource.Cancel();
                }
            }
            
            // Update the progress status
            lock (_uploadProgress)
            {
                if (_uploadProgress.TryGetValue(id, out var progress))
                {
                    progress.Cancel();
                    OnUploadProgressChanged(progress);
                }
            }
            
            // Clean up resources
            if (tokenSource != null)
            {
                lock (_uploadCancellationTokens)
                {
                    if (_uploadCancellationTokens.ContainsKey(id))
                    {
                        _uploadCancellationTokens.Remove(id);
                    }
                }
                tokenSource.Dispose();
            }
            
            await Task.CompletedTask; // To fulfill the async contract
        }

        /// <summary>
        /// Retrieves the current upload progress for a specific photo.
        /// </summary>
        /// <param name="id">The ID of the photo.</param>
        /// <returns>The upload progress for the specified photo, or null if not found.</returns>
        public async Task<PhotoUploadProgress> GetUploadProgressAsync(string id)
        {
            lock (_uploadProgress)
            {
                if (_uploadProgress.TryGetValue(id, out var progress))
                {
                    return progress.Clone(); // Return a clone to prevent external modification
                }
            }
            
            return await Task.FromResult<PhotoUploadProgress>(null);
        }

        /// <summary>
        /// Retrieves the upload progress for all photos that are currently being uploaded or queued for upload.
        /// </summary>
        /// <returns>A list of upload progress objects.</returns>
        public async Task<List<PhotoUploadProgress>> GetAllUploadProgressAsync()
        {
            var result = new List<PhotoUploadProgress>();
            
            lock (_uploadProgress)
            {
                foreach (var progress in _uploadProgress.Values)
                {
                    result.Add(progress.Clone()); // Return clones to prevent external modification
                }
            }
            
            return await Task.FromResult(result);
        }

        /// <summary>
        /// Retries uploading photos that previously failed to upload.
        /// </summary>
        /// <returns>True if all retries were successful, false otherwise.</returns>
        public async Task<bool> RetryFailedUploadsAsync()
        {
            // Check network connectivity
            if (!_networkService.IsConnected)
            {
                return false;
            }
            
            // Get failed uploads
            List<string> failedUploadIds;
            lock (_uploadProgress)
            {
                failedUploadIds = _uploadProgress.Values
                    .Where(p => p.IsError())
                    .Select(p => p.Id)
                    .ToList();
            }
            
            if (failedUploadIds.Count == 0)
            {
                return true; // No failed uploads to retry
            }
            
            var successCount = 0;
            
            // Retry each failed upload
            foreach (var id in failedUploadIds)
            {
                lock (_uploadProgress)
                {
                    if (_uploadProgress.TryGetValue(id, out var progress))
                    {
                        progress.Reset(); // Reset the progress status
                        OnUploadProgressChanged(progress);
                    }
                }
                
                // Attempt to upload again
                var result = await UploadPhotoAsync(id);
                if (result)
                {
                    successCount++;
                }
            }
            
            return successCount == failedUploadIds.Count;
        }

        /// <summary>
        /// Checks if any photo synchronization is currently in progress.
        /// </summary>
        /// <returns>True if any synchronization is in progress, false otherwise.</returns>
        public async Task<bool> IsSyncInProgressAsync()
        {
            return await Task.FromResult(_isSyncInProgress);
        }

        /// <summary>
        /// Handles changes in network connectivity.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void HandleConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
        {
            // If connectivity is restored, attempt to sync pending photos
            if (e.IsConnected)
            {
                // Fire and forget - we don't want to block the event handler
                _ = SyncPhotosAsync();
            }
        }

        /// <summary>
        /// Raises the UploadProgressChanged event.
        /// </summary>
        /// <param name="progress">The progress information to include in the event.</param>
        protected virtual void OnUploadProgressChanged(PhotoUploadProgress progress)
        {
            UploadProgressChanged?.Invoke(this, progress);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            // Unsubscribe from events
            _networkService.ConnectivityChanged -= HandleConnectivityChanged;
            
            // Cancel all ongoing uploads
            lock (_uploadCancellationTokens)
            {
                foreach (var tokenSource in _uploadCancellationTokens.Values)
                {
                    tokenSource.Cancel();
                    tokenSource.Dispose();
                }
                _uploadCancellationTokens.Clear();
            }
            
            // Clear progress tracking
            lock (_uploadProgress)
            {
                _uploadProgress.Clear();
            }
            
            // Dispose the sync lock
            _syncLock.Dispose();
        }
    }
}