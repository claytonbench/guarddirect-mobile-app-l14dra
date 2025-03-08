using SecurityPatrol.Constants;
using SecurityPatrol.Models;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SecurityPatrol.Services
{
    /// <summary>
    /// Service responsible for synchronizing time tracking records between the local database and backend API,
    /// with support for offline operation and resilient error handling.
    /// </summary>
    public class TimeTrackingSyncService : ITimeTrackingSyncService
    {
        private readonly ITimeRecordRepository _repository;
        private readonly IApiService _apiService;
        private readonly INetworkService _networkService;
        private readonly ITelemetryService _telemetryService;
        private readonly int _maxRetryAttempts;
        private readonly TimeSpan _initialRetryDelay;
        private readonly SemaphoreSlim _syncLock;

        /// <summary>
        /// Initializes a new instance of the TimeTrackingSyncService class with required dependencies
        /// </summary>
        /// <param name="repository">The repository for accessing time records in the local database</param>
        /// <param name="apiService">The service for making API requests to the backend</param>
        /// <param name="networkService">The service for checking network connectivity status</param>
        /// <param name="telemetryService">The service for logging and telemetry</param>
        /// <exception cref="ArgumentNullException">Thrown when any required dependency is null</exception>
        public TimeTrackingSyncService(
            ITimeRecordRepository repository,
            IApiService apiService,
            INetworkService networkService,
            ITelemetryService telemetryService)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _networkService = networkService ?? throw new ArgumentNullException(nameof(networkService));
            _telemetryService = telemetryService ?? throw new ArgumentNullException(nameof(telemetryService));
            
            _maxRetryAttempts = 3;
            _initialRetryDelay = TimeSpan.FromSeconds(2);
            _syncLock = new SemaphoreSlim(1, 1);
        }

        /// <summary>
        /// Synchronizes all pending time records with the backend API
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation</param>
        /// <returns>True if all records were synchronized successfully, false otherwise</returns>
        public async Task<bool> SyncTimeRecordsAsync(CancellationToken cancellationToken = default)
        {
            if (!_networkService.IsConnected)
            {
                _telemetryService.Log(LogLevel.Information, "Cannot synchronize time records: Not connected to network");
                return false;
            }

            if (!_networkService.ShouldAttemptOperation(NetworkOperationType.Standard))
            {
                _telemetryService.Log(LogLevel.Information, "Skipping time records synchronization due to poor network conditions");
                return false;
            }

            try
            {
                await _syncLock.WaitAsync(cancellationToken);

                var pendingRecords = await _repository.GetPendingRecordsAsync();
                
                if (pendingRecords == null || pendingRecords.Count == 0)
                {
                    _telemetryService.Log(LogLevel.Information, "No pending time records to synchronize");
                    return true;
                }

                _telemetryService.TrackEvent("SyncTimeRecordsStarted", new Dictionary<string, string>
                {
                    { "PendingRecordsCount", pendingRecords.Count.ToString() }
                });

                int successCount = 0;
                int failureCount = 0;

                foreach (var record in pendingRecords)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _telemetryService.Log(LogLevel.Information, "Time record synchronization was cancelled");
                        break;
                    }

                    bool success = await SyncRecordInternalAsync(record, cancellationToken);
                    
                    if (success)
                    {
                        successCount++;
                    }
                    else
                    {
                        failureCount++;
                    }
                }

                _telemetryService.TrackEvent("SyncTimeRecordsCompleted", new Dictionary<string, string>
                {
                    { "SuccessCount", successCount.ToString() },
                    { "FailureCount", failureCount.ToString() },
                    { "TotalCount", pendingRecords.Count.ToString() }
                });

                return failureCount == 0;
            }
            catch (OperationCanceledException)
            {
                _telemetryService.Log(LogLevel.Information, "Time record synchronization was cancelled");
                return false;
            }
            catch (Exception ex)
            {
                _telemetryService.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "SyncTimeRecordsAsync" }
                });
                return false;
            }
            finally
            {
                _syncLock.Release();
            }
        }

        /// <summary>
        /// Synchronizes a specific time record with the backend API
        /// </summary>
        /// <param name="record">The time record to synchronize</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation</param>
        /// <returns>True if the record was synchronized successfully, false otherwise</returns>
        /// <exception cref="ArgumentNullException">Thrown when record is null</exception>
        public async Task<bool> SyncRecordAsync(TimeRecordModel record, CancellationToken cancellationToken = default)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));

            if (!_networkService.IsConnected)
            {
                _telemetryService.Log(LogLevel.Information, $"Cannot synchronize time record {record.Id}: Not connected to network");
                return false;
            }

            if (!_networkService.ShouldAttemptOperation(NetworkOperationType.Standard))
            {
                _telemetryService.Log(LogLevel.Information, $"Skipping time record {record.Id} synchronization due to poor network conditions");
                return false;
            }

            try
            {
                await _syncLock.WaitAsync(cancellationToken);
                return await SyncRecordInternalAsync(record, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _telemetryService.Log(LogLevel.Information, $"Time record {record.Id} synchronization was cancelled");
                return false;
            }
            catch (Exception ex)
            {
                _telemetryService.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "SyncRecordAsync" },
                    { "RecordId", record.Id.ToString() }
                });
                return false;
            }
            finally
            {
                _syncLock.Release();
            }
        }

        /// <summary>
        /// Synchronizes a time record with the specified ID with the backend API
        /// </summary>
        /// <param name="recordId">The ID of the time record to synchronize</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation</param>
        /// <returns>True if the record was synchronized successfully, false otherwise</returns>
        public async Task<bool> SyncRecordAsync(int recordId, CancellationToken cancellationToken = default)
        {
            if (!_networkService.IsConnected)
            {
                _telemetryService.Log(LogLevel.Information, $"Cannot synchronize time record {recordId}: Not connected to network");
                return false;
            }

            if (!_networkService.ShouldAttemptOperation(NetworkOperationType.Standard))
            {
                _telemetryService.Log(LogLevel.Information, $"Skipping time record {recordId} synchronization due to poor network conditions");
                return false;
            }

            try
            {
                await _syncLock.WaitAsync(cancellationToken);
                
                var record = await _repository.GetTimeRecordByIdAsync(recordId);
                
                if (record == null)
                {
                    _telemetryService.Log(LogLevel.Warning, $"Cannot synchronize time record {recordId}: Record not found");
                    return false;
                }

                if (record.IsSynced)
                {
                    _telemetryService.Log(LogLevel.Information, $"Time record {recordId} is already synchronized");
                    return true;
                }

                return await SyncRecordInternalAsync(record, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _telemetryService.Log(LogLevel.Information, $"Time record {recordId} synchronization was cancelled");
                return false;
            }
            catch (Exception ex)
            {
                _telemetryService.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "SyncRecordAsync" },
                    { "RecordId", recordId.ToString() }
                });
                return false;
            }
            finally
            {
                _syncLock.Release();
            }
        }

        /// <summary>
        /// Gets the count of time records pending synchronization
        /// </summary>
        /// <returns>The number of unsynchronized time records</returns>
        public async Task<int> GetPendingSyncCountAsync()
        {
            try
            {
                var pendingRecords = await _repository.GetPendingRecordsAsync();
                return pendingRecords?.Count ?? 0;
            }
            catch (Exception ex)
            {
                _telemetryService.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "GetPendingSyncCountAsync" }
                });
                return 0;
            }
        }

        /// <summary>
        /// Internal method that handles the actual synchronization of a time record with retry logic
        /// </summary>
        /// <param name="record">The time record to synchronize</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation</param>
        /// <returns>True if the record was synchronized successfully, false otherwise</returns>
        private async Task<bool> SyncRecordInternalAsync(TimeRecordModel record, CancellationToken cancellationToken)
        {
            var request = TimeRecordRequest.FromTimeRecordModel(record);
            int retryCount = 0;

            while (retryCount <= _maxRetryAttempts)
            {
                try
                {
                    _telemetryService.Log(LogLevel.Information, 
                        $"Attempting to sync time record {record.Id} (Attempt {retryCount + 1} of {_maxRetryAttempts + 1})");

                    var startTime = DateTime.UtcNow;
                    var response = await _apiService.PostAsync<TimeRecordResponse>(
                        ApiEndpoints.TimeClock, 
                        request, 
                        true);
                    var duration = DateTime.UtcNow - startTime;

                    _telemetryService.TrackApiCall(ApiEndpoints.TimeClock, duration, response?.IsSuccess() ?? false);

                    if (response != null && response.IsSuccess())
                    {
                        await _repository.UpdateSyncStatusAsync(record.Id, true);
                        await _repository.UpdateRemoteIdAsync(record.Id, response.Id);

                        _telemetryService.TrackEvent("TimeRecordSynced", new Dictionary<string, string>
                        {
                            { "RecordId", record.Id.ToString() },
                            { "RemoteId", response.Id },
                            { "Type", record.Type },
                            { "Timestamp", record.Timestamp.ToString("o") }
                        });

                        return true;
                    }
                    else
                    {
                        string status = response?.Status ?? "Unknown error";
                        _telemetryService.Log(LogLevel.Warning, 
                            $"Failed to sync time record {record.Id}: {status}");

                        retryCount++;
                        if (retryCount > _maxRetryAttempts)
                            break;

                        // Use exponential backoff for retries
                        TimeSpan delay = TimeSpan.FromMilliseconds(_initialRetryDelay.TotalMilliseconds * Math.Pow(2, retryCount - 1));
                        await Task.Delay(delay, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _telemetryService.TrackException(ex, new Dictionary<string, string>
                    {
                        { "Operation", "SyncRecordInternalAsync" },
                        { "RecordId", record.Id.ToString() },
                        { "RetryCount", retryCount.ToString() }
                    });

                    retryCount++;
                    if (retryCount > _maxRetryAttempts)
                        break;

                    // Use exponential backoff for retries
                    TimeSpan delay = TimeSpan.FromMilliseconds(_initialRetryDelay.TotalMilliseconds * Math.Pow(2, retryCount - 1));
                    await Task.Delay(delay, cancellationToken);
                }
            }

            return false;
        }
    }
}