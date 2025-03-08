using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using SecurityPatrol.Models;
using SecurityPatrol.Services;

namespace SecurityPatrol.Services
{
    /// <summary>
    /// Implementation of the ISyncService interface that orchestrates data synchronization 
    /// between the mobile application and backend services.
    /// </summary>
    public class SyncService : ISyncService, IDisposable
    {
        private readonly ISyncRepository _syncRepository;
        private readonly INetworkService _networkService;
        private readonly ITimeTrackingSyncService _timeTrackingSyncService;
        private readonly ILocationSyncService _locationSyncService;
        private readonly IPhotoSyncService _photoSyncService;
        private readonly IReportSyncService _reportSyncService;
        private readonly ILogger<SyncService> _logger;
        private Timer _syncTimer;
        private readonly SemaphoreSlim _syncLock = new SemaphoreSlim(1, 1);
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly Dictionary<string, int> _entityPriorities;

        /// <summary>
        /// Gets a value indicating whether a synchronization operation is currently in progress.
        /// </summary>
        public bool IsSyncing { get; private set; }

        /// <summary>
        /// Event that is raised when the synchronization status changes.
        /// </summary>
        public event EventHandler<SyncStatusChangedEventArgs> SyncStatusChanged;

        /// <summary>
        /// Initializes a new instance of the SyncService class with the required dependencies.
        /// </summary>
        public SyncService(
            ISyncRepository syncRepository,
            INetworkService networkService,
            ITimeTrackingSyncService timeTrackingSyncService,
            ILocationSyncService locationSyncService,
            IPhotoSyncService photoSyncService,
            IReportSyncService reportSyncService,
            ILogger<SyncService> logger)
        {
            _syncRepository = syncRepository ?? throw new ArgumentNullException(nameof(syncRepository));
            _networkService = networkService ?? throw new ArgumentNullException(nameof(networkService));
            _timeTrackingSyncService = timeTrackingSyncService ?? throw new ArgumentNullException(nameof(timeTrackingSyncService));
            _locationSyncService = locationSyncService ?? throw new ArgumentNullException(nameof(locationSyncService));
            _photoSyncService = photoSyncService ?? throw new ArgumentNullException(nameof(photoSyncService));
            _reportSyncService = reportSyncService ?? throw new ArgumentNullException(nameof(reportSyncService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Set up priority order for entity synchronization (higher number = higher priority)
            _entityPriorities = new Dictionary<string, int>
            {
                { "TimeRecord", 100 },       // Highest priority - critical for time tracking
                { "Checkpoint", 90 },        // High priority - critical for patrol verification
                { "Location", 80 },          // Medium-high priority - important for location tracking
                { "Report", 70 },            // Medium priority - user-generated content
                { "Photo", 60 }              // Lower priority - typically larger data, sync last
            };

            // Subscribe to network connectivity changes
            _networkService.ConnectivityChanged += OnConnectivityChanged;
            IsSyncing = false;
        }

        /// <summary>
        /// Synchronizes all pending data with the backend services.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation, containing the results of the synchronization.</returns>
        public async Task<SyncResult> SyncAll(CancellationToken cancellationToken = default)
        {
            // Check if sync is already in progress
            if (IsSyncing)
            {
                _logger.LogInformation("Synchronization already in progress, skipping request");
                return new SyncResult { PendingCount = 1 };
            }

            // Check if network is connected
            if (!_networkService.IsConnected)
            {
                _logger.LogWarning("Cannot start synchronization - device is offline");
                return new SyncResult { PendingCount = 1 };
            }

            IsSyncing = true;
            var result = new SyncResult();

            try
            {
                // Create a linked token to combine the provided token and our internal one
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken, _cancellationTokenSource.Token);
                var linkedToken = linkedCts.Token;

                // Acquire the sync lock to prevent concurrent sync operations
                await _syncLock.WaitAsync(linkedToken);

                try
                {
                    _logger.LogInformation("Starting synchronization of all data");

                    // Raise initial status event
                    OnSyncStatusChanged(new SyncStatusChangedEventArgs("All", "Starting", 0, 1));

                    // Synchronize data in priority order:
                    // 1. Time records (critical for time tracking)
                    await SyncTimeRecordsAsync(result, linkedToken);

                    // 2. Location data (batched for efficiency)
                    await SyncLocationsAsync(result, linkedToken);

                    // 3. Activity reports (user-generated content)
                    await SyncReportsAsync(result, linkedToken);

                    // 4. Photos (largest data, synchronized last)
                    await SyncPhotosAsync(result, linkedToken);

                    _logger.LogInformation("Completed synchronization of all data: {Result}", result);

                    // Raise completed status event
                    OnSyncStatusChanged(new SyncStatusChangedEventArgs(
                        "All", 
                        "Completed", 
                        result.SuccessCount + result.FailureCount, 
                        result.SuccessCount + result.FailureCount + result.PendingCount));
                }
                finally
                {
                    _syncLock.Release();
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Synchronization was canceled");
                result.PendingCount++;
                
                OnSyncStatusChanged(new SyncStatusChangedEventArgs(
                    "All", 
                    "Canceled", 
                    result.SuccessCount + result.FailureCount, 
                    result.SuccessCount + result.FailureCount + result.PendingCount));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during synchronization: {Message}", ex.Message);
                result.FailureCount++;
                
                OnSyncStatusChanged(new SyncStatusChangedEventArgs(
                    "All", 
                    "Error", 
                    result.SuccessCount + result.FailureCount, 
                    result.SuccessCount + result.FailureCount + result.PendingCount));
            }
            finally
            {
                IsSyncing = false;
            }

            return result;
        }

        /// <summary>
        /// Synchronizes a specific entity with the backend services.
        /// </summary>
        /// <param name="entityType">The type of entity to synchronize.</param>
        /// <param name="entityId">The ID of the entity to synchronize.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation, returning true if synchronization was successful.</returns>
        public async Task<bool> SyncEntity(string entityType, string entityId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(entityType))
                throw new ArgumentException("Entity type cannot be null or empty", nameof(entityType));
            
            if (string.IsNullOrEmpty(entityId))
                throw new ArgumentException("Entity ID cannot be null or empty", nameof(entityId));

            // Check if network is connected
            if (!_networkService.IsConnected)
            {
                _logger.LogWarning("Cannot sync entity {EntityType}:{EntityId} - device is offline", entityType, entityId);
                return false;
            }

            bool success = false;
            
            try
            {
                _logger.LogInformation("Starting synchronization of {EntityType}:{EntityId}", entityType, entityId);
                
                // Create retry policy for resilient synchronization
                var retryPolicy = Policy
                    .Handle<Exception>(ex => !(ex is OperationCanceledException))
                    .WaitAndRetryAsync(
                        3, 
                        attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                        (ex, timeSpan, attempt, context) => 
                        {
                            _logger.LogWarning(ex, 
                                "Sync attempt {Attempt} failed for {EntityType}:{EntityId}. Retrying in {RetryTime}s.", 
                                attempt, entityType, entityId, timeSpan.TotalSeconds);
                        });

                // Execute the appropriate sync method based on entity type
                switch (entityType)
                {
                    case "TimeRecord":
                        success = await retryPolicy.ExecuteAsync(async () => 
                            await _timeTrackingSyncService.SyncRecordAsync(int.Parse(entityId), cancellationToken));
                        break;
                        
                    case "Photo":
                        success = await retryPolicy.ExecuteAsync(async () => 
                            await _photoSyncService.UploadPhotoAsync(entityId));
                        break;
                        
                    case "Report":
                        success = await retryPolicy.ExecuteAsync(async () => 
                            await _reportSyncService.SyncReportAsync(int.Parse(entityId)));
                        break;
                        
                    default:
                        _logger.LogWarning("Unsupported entity type for direct synchronization: {EntityType}", entityType);
                        return false;
                }

                // Update sync status in repository
                await _syncRepository.UpdateSyncStatus(entityType, entityId, success, 
                    success ? null : "Sync operation failed");

                // Raise status event
                OnSyncStatusChanged(new SyncStatusChangedEventArgs(
                    entityType,
                    success ? "Completed" : "Failed",
                    success ? 1 : 0,
                    1));

                _logger.LogInformation("Synchronization of {EntityType}:{EntityId} completed with status: {Success}", 
                    entityType, entityId, success);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Synchronization was canceled for {EntityType}:{EntityId}", entityType, entityId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during synchronization of {EntityType}:{EntityId}: {Message}", 
                    entityType, entityId, ex.Message);
                
                // Update sync status with error
                await _syncRepository.UpdateSyncStatus(entityType, entityId, false, ex.Message);
                
                // Raise status event with error
                OnSyncStatusChanged(new SyncStatusChangedEventArgs(
                    entityType,
                    "Error",
                    0,
                    1));
            }

            return success;
        }

        /// <summary>
        /// Synchronizes all entities of a specific type with the backend services.
        /// </summary>
        /// <param name="entityType">The type of entities to synchronize.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation, containing the results of the synchronization.</returns>
        public async Task<SyncResult> SyncEntity(string entityType, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(entityType))
                throw new ArgumentException("Entity type cannot be null or empty", nameof(entityType));

            var result = new SyncResult();

            // Check if network is connected
            if (!_networkService.IsConnected)
            {
                _logger.LogWarning("Cannot sync entity type {EntityType} - device is offline", entityType);
                result.PendingCount = 1;  // At least one pending item
                return result;
            }

            try
            {
                _logger.LogInformation("Starting synchronization of all {EntityType} entities", entityType);
                
                int totalItems = 0;
                int completedItems = 0;

                // Execute the appropriate sync method based on entity type
                switch (entityType)
                {
                    case "TimeRecord":
                        var timeRecordCount = await _timeTrackingSyncService.GetPendingSyncCountAsync();
                        if (timeRecordCount > 0)
                        {
                            // Notify starting synchronization
                            OnSyncStatusChanged(new SyncStatusChangedEventArgs(
                                entityType, "Starting", 0, timeRecordCount));

                            totalItems = timeRecordCount;
                            bool success = await _timeTrackingSyncService.SyncTimeRecordsAsync(cancellationToken);
                            
                            if (success)
                            {
                                result.SuccessCount = timeRecordCount;
                                completedItems = timeRecordCount;
                            }
                            else
                            {
                                // Assume partial success/failure
                                result.SuccessCount = timeRecordCount / 2;  // Estimate
                                result.FailureCount = timeRecordCount - result.SuccessCount;
                                completedItems = timeRecordCount;
                            }
                        }
                        break;

                    case "Location":
                        // Use a larger batch size for location data
                        const int locationBatchSize = 100;
                        bool locationSuccess = await _locationSyncService.SyncLocationsAsync(locationBatchSize);
                        
                        // For location sync, we don't have exact counts, so we estimate
                        if (locationSuccess)
                        {
                            result.SuccessCount = locationBatchSize;  // Estimate
                            totalItems = locationBatchSize;
                            completedItems = locationBatchSize;
                        }
                        else
                        {
                            result.SuccessCount = locationBatchSize / 2;  // Estimate
                            result.FailureCount = locationBatchSize / 2;  // Estimate
                            totalItems = locationBatchSize;
                            completedItems = locationBatchSize;
                        }
                        break;

                    case "Photo":
                        bool photoSyncInProgress = await _photoSyncService.IsSyncInProgressAsync();
                        if (!photoSyncInProgress)
                        {
                            bool photoSuccess = await _photoSyncService.SyncPhotosAsync();
                            // For photo sync, success is binary (true/false) without counts
                            // We can get progress updates through the PhotoSyncService events
                            if (photoSuccess)
                            {
                                result.SuccessCount = 1;
                            }
                            else
                            {
                                result.FailureCount = 1;
                            }
                            totalItems = 1;
                            completedItems = 1;
                        }
                        else
                        {
                            _logger.LogInformation("Photo synchronization already in progress");
                            result.PendingCount = 1;
                        }
                        break;

                    case "Report":
                        var reportCount = await _reportSyncService.GetPendingSyncCountAsync();
                        if (reportCount > 0)
                        {
                            // Notify starting synchronization
                            OnSyncStatusChanged(new SyncStatusChangedEventArgs(
                                entityType, "Starting", 0, reportCount));

                            int syncedReports = await _reportSyncService.SyncReportsAsync();
                            
                            totalItems = reportCount;
                            completedItems = syncedReports;
                            result.SuccessCount = syncedReports;
                            result.FailureCount = reportCount - syncedReports;
                        }
                        break;

                    default:
                        _logger.LogWarning("Unsupported entity type for batch synchronization: {EntityType}", entityType);
                        result.PendingCount = 1;  // At least one pending item
                        break;
                }

                // Final status notification
                if (totalItems > 0)
                {
                    OnSyncStatusChanged(new SyncStatusChangedEventArgs(
                        entityType,
                        completedItems == totalItems ? "Completed" : "Partial",
                        completedItems,
                        totalItems));
                }

                _logger.LogInformation("Completed synchronization of {EntityType} entities: {Result}", 
                    entityType, result);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Synchronization was canceled for {EntityType}", entityType);
                result.PendingCount++;
                
                OnSyncStatusChanged(new SyncStatusChangedEventArgs(
                    entityType, "Canceled", result.SuccessCount, 
                    result.SuccessCount + result.FailureCount + result.PendingCount));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during synchronization of {EntityType}: {Message}", 
                    entityType, ex.Message);
                
                result.FailureCount++;
                
                OnSyncStatusChanged(new SyncStatusChangedEventArgs(
                    entityType, "Error", result.SuccessCount, 
                    result.SuccessCount + result.FailureCount + result.PendingCount));
            }

            return result;
        }

        /// <summary>
        /// Schedules automatic synchronization at specified intervals.
        /// </summary>
        /// <param name="interval">The time interval between synchronization attempts.</param>
        public void ScheduleSync(TimeSpan interval)
        {
            if (interval <= TimeSpan.Zero)
                throw new ArgumentException("Interval must be greater than zero", nameof(interval));

            // Cancel any existing timer
            CancelScheduledSync();

            // Create a new timer that will execute the SyncAll method at the specified interval
            _syncTimer = new Timer(
                async _ => 
                {
                    try 
                    {
                        await SyncAll();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during scheduled synchronization: {Message}", ex.Message);
                    }
                },
                null, 
                interval, 
                interval);

            _logger.LogInformation("Scheduled automatic synchronization every {Interval} seconds", 
                interval.TotalSeconds);
        }

        /// <summary>
        /// Cancels any scheduled automatic synchronization.
        /// </summary>
        public void CancelScheduledSync()
        {
            if (_syncTimer != null)
            {
                _syncTimer.Dispose();
                _syncTimer = null;
                _logger.LogInformation("Canceled scheduled synchronization");
            }
        }

        /// <summary>
        /// Gets the current synchronization status, including pending items count by entity type.
        /// </summary>
        /// <returns>A dictionary containing the count of pending items for each entity type.</returns>
        public async Task<Dictionary<string, int>> GetSyncStatus()
        {
            _logger.LogInformation("Retrieving sync status");
            return await _syncRepository.GetSyncStatistics();
        }

        /// <summary>
        /// Handles connectivity change events from the network service.
        /// </summary>
        private async void OnConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
        {
            _logger.LogInformation("Network connectivity changed. Connected: {IsConnected}, Type: {ConnectionType}", 
                e.IsConnected, e.ConnectionType);

            // If connectivity is restored, check for pending items and trigger sync
            if (e.IsConnected)
            {
                try
                {
                    bool hasPendingItems = await CheckPendingItemsAsync();
                    
                    if (hasPendingItems && !IsSyncing)
                    {
                        _logger.LogInformation("Network connectivity restored with pending items. Triggering automatic sync.");
                        
                        // Add a small delay to allow network to stabilize
                        await Task.Delay(TimeSpan.FromSeconds(5));
                        
                        // Trigger sync in background
                        _ = Task.Run(async () => 
                        {
                            try
                            {
                                await SyncAll();
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error during automatic sync after connectivity restored: {Message}", 
                                    ex.Message);
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking pending items after connectivity change: {Message}", 
                        ex.Message);
                }
            }
        }

        /// <summary>
        /// Releases all resources used by the SyncService.
        /// </summary>
        public void Dispose()
        {
            CancelScheduledSync();
            
            // Unsubscribe from events
            if (_networkService != null)
            {
                _networkService.ConnectivityChanged -= OnConnectivityChanged;
            }

            // Cancel any ongoing operations
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            
            // Dispose of sync lock
            _syncLock.Dispose();
            
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Raises the SyncStatusChanged event.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        protected virtual void OnSyncStatusChanged(SyncStatusChangedEventArgs e)
        {
            SyncStatusChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Checks if there are any items pending synchronization.
        /// </summary>
        /// <returns>True if there are pending items, false otherwise.</returns>
        private async Task<bool> CheckPendingItemsAsync()
        {
            // Check for pending time records
            int pendingTimeRecords = await _timeTrackingSyncService.GetPendingSyncCountAsync();
            if (pendingTimeRecords > 0)
                return true;

            // Check for pending locations
            if (_locationSyncService.IsSyncing)
                return true;

            // Check for pending photos
            bool photoSyncInProgress = await _photoSyncService.IsSyncInProgressAsync();
            if (photoSyncInProgress)
                return true;

            // Check for pending reports
            int pendingReports = await _reportSyncService.GetPendingSyncCountAsync();
            if (pendingReports > 0)
                return true;

            return false;
        }

        // Private methods for synchronizing specific data types

        /// <summary>
        /// Synchronizes time tracking records with the backend.
        /// </summary>
        private async Task SyncTimeRecordsAsync(SyncResult result, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting synchronization of time records");
            
            int pendingCount = await _timeTrackingSyncService.GetPendingSyncCountAsync();
            
            if (pendingCount > 0)
            {
                // Notify that time record sync is starting
                OnSyncStatusChanged(new SyncStatusChangedEventArgs(
                    "TimeRecord", "Starting", 0, pendingCount));
                
                // Attempt to sync time records
                bool success = await _timeTrackingSyncService.SyncTimeRecordsAsync(cancellationToken);
                
                if (success)
                {
                    result.SuccessCount += pendingCount;
                    
                    // All records were synced successfully
                    OnSyncStatusChanged(new SyncStatusChangedEventArgs(
                        "TimeRecord", "Completed", pendingCount, pendingCount));
                }
                else
                {
                    // Some records may have failed - check again for remaining pending items
                    int remainingCount = await _timeTrackingSyncService.GetPendingSyncCountAsync();
                    int syncedCount = pendingCount - remainingCount;
                    
                    result.SuccessCount += syncedCount;
                    result.FailureCount += remainingCount;
                    
                    OnSyncStatusChanged(new SyncStatusChangedEventArgs(
                        "TimeRecord", "Partial", syncedCount, pendingCount));
                }
            }
        }

        /// <summary>
        /// Synchronizes location data with the backend.
        /// </summary>
        private async Task SyncLocationsAsync(SyncResult result, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting synchronization of location data");
            
            // Skip if location sync is already in progress
            if (_locationSyncService.IsSyncing)
            {
                _logger.LogInformation("Location synchronization already in progress, skipping");
                return;
            }
            
            // For location data, we use a larger batch size since these are typically numerous and small
            const int locationBatchSize = 100;
            
            // Notify that location sync is starting
            OnSyncStatusChanged(new SyncStatusChangedEventArgs(
                "Location", "Starting", 0, locationBatchSize));
            
            // Attempt to sync locations
            bool success = await _locationSyncService.SyncLocationsAsync(locationBatchSize);
            
            if (success)
            {
                // We don't know exactly how many were synced, so we estimate based on batch size
                result.SuccessCount += locationBatchSize;
                
                OnSyncStatusChanged(new SyncStatusChangedEventArgs(
                    "Location", "Completed", locationBatchSize, locationBatchSize));
            }
            else
            {
                // Assume partial success
                result.SuccessCount += locationBatchSize / 2;  // Estimate
                result.FailureCount += locationBatchSize / 2;  // Estimate
                
                OnSyncStatusChanged(new SyncStatusChangedEventArgs(
                    "Location", "Partial", locationBatchSize / 2, locationBatchSize));
            }
        }

        /// <summary>
        /// Synchronizes photos with the backend.
        /// </summary>
        private async Task SyncPhotosAsync(SyncResult result, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting synchronization of photos");
            
            // Check if photo sync is already in progress
            bool syncInProgress = await _photoSyncService.IsSyncInProgressAsync();
            
            if (!syncInProgress)
            {
                // Notify that photo sync is starting
                OnSyncStatusChanged(new SyncStatusChangedEventArgs("Photo", "Starting", 0, 1));
                
                // Attempt to sync photos
                bool success = await _photoSyncService.SyncPhotosAsync();
                
                if (success)
                {
                    result.SuccessCount += 1;  // We don't know the exact count
                    
                    OnSyncStatusChanged(new SyncStatusChangedEventArgs(
                        "Photo", "Completed", 1, 1));
                }
                else
                {
                    result.FailureCount += 1;  // We don't know the exact count
                    
                    OnSyncStatusChanged(new SyncStatusChangedEventArgs(
                        "Photo", "Failed", 0, 1));
                }
            }
            else
            {
                _logger.LogInformation("Photo synchronization already in progress, skipping");
                result.PendingCount += 1;  // Mark as pending
            }
        }

        /// <summary>
        /// Synchronizes activity reports with the backend.
        /// </summary>
        private async Task SyncReportsAsync(SyncResult result, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting synchronization of activity reports");
            
            int pendingCount = await _reportSyncService.GetPendingSyncCountAsync();
            
            if (pendingCount > 0)
            {
                // Notify that report sync is starting
                OnSyncStatusChanged(new SyncStatusChangedEventArgs(
                    "Report", "Starting", 0, pendingCount));
                
                // Attempt to sync reports
                int syncedCount = await _reportSyncService.SyncReportsAsync();
                
                result.SuccessCount += syncedCount;
                result.FailureCount += pendingCount - syncedCount;
                
                string status = (syncedCount == pendingCount) ? "Completed" : "Partial";
                
                OnSyncStatusChanged(new SyncStatusChangedEventArgs(
                    "Report", status, syncedCount, pendingCount));
            }
        }
    }
}