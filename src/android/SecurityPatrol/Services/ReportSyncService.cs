using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SecurityPatrol.Constants;
using SecurityPatrol.Models;

namespace SecurityPatrol.Services
{
    /// <summary>
    /// Implementation of the IReportSyncService interface that handles synchronization 
    /// of activity reports between the local database and the backend API.
    /// This service is responsible for transmitting reports, tracking synchronization status,
    /// and implementing retry logic for failed synchronization attempts.
    /// </summary>
    public class ReportSyncService : IReportSyncService
    {
        private bool _isSyncing;
        private readonly SemaphoreSlim _syncLock = new SemaphoreSlim(1, 1);
        private readonly IReportRepository _reportRepository;
        private readonly IApiService _apiService;
        private readonly INetworkService _networkService;
        private readonly ILogger<ReportSyncService> _logger;

        /// <summary>
        /// Event that is raised when the synchronization status changes.
        /// </summary>
        public event EventHandler<SyncStatusChangedEventArgs> SyncStatusChanged;

        /// <summary>
        /// Initializes a new instance of the ReportSyncService class with the required dependencies.
        /// </summary>
        /// <param name="reportRepository">Repository for accessing report data.</param>
        /// <param name="apiService">Service for making API requests.</param>
        /// <param name="networkService">Service for checking network connectivity.</param>
        /// <param name="logger">Logger for recording synchronization activities.</param>
        public ReportSyncService(
            IReportRepository reportRepository,
            IApiService apiService,
            INetworkService networkService,
            ILogger<ReportSyncService> logger)
        {
            _reportRepository = reportRepository ?? throw new ArgumentNullException(nameof(reportRepository));
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _networkService = networkService ?? throw new ArgumentNullException(nameof(networkService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _isSyncing = false;
        }

        /// <summary>
        /// Synchronizes all unsynchronized reports with the backend API.
        /// </summary>
        /// <returns>The number of successfully synchronized reports.</returns>
        public async Task<int> SyncReportsAsync()
        {
            if (!_networkService.IsConnected)
            {
                _logger.LogWarning("Cannot sync reports: No network connection available");
                return 0;
            }

            try
            {
                await _syncLock.WaitAsync();
                _isSyncing = true;

                _logger.LogInformation("Starting report synchronization");

                // Get all reports that need to be synced
                var pendingReports = await _reportRepository.GetPendingSyncReportsAsync(100);
                var reportsList = pendingReports.ToList();

                if (!reportsList.Any())
                {
                    _logger.LogInformation("No pending reports to synchronize");
                    return 0;
                }

                int successCount = 0;
                int totalCount = reportsList.Count;

                // Raise initial sync status event
                OnSyncStatusChanged(new SyncStatusChangedEventArgs(
                    "Report", 
                    "Syncing", 
                    successCount, 
                    totalCount));

                foreach (var report in reportsList)
                {
                    try
                    {
                        bool success = await SyncReportAsync(report.Id);
                        if (success)
                        {
                            successCount++;
                        }

                        // Update sync progress
                        OnSyncStatusChanged(new SyncStatusChangedEventArgs(
                            "Report", 
                            "Syncing", 
                            successCount, 
                            totalCount));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error synchronizing report {ReportId}", report.Id);
                    }
                }

                _logger.LogInformation("Completed report synchronization. Successfully synced {SuccessCount} of {TotalCount} reports",
                    successCount, totalCount);

                return successCount;
            }
            finally
            {
                _isSyncing = false;
                _syncLock.Release();
            }
        }

        /// <summary>
        /// Synchronizes a specific report with the backend API.
        /// </summary>
        /// <param name="id">The ID of the report to synchronize.</param>
        /// <returns>True if the report was synchronized successfully, false otherwise.</returns>
        public async Task<bool> SyncReportAsync(int id)
        {
            if (!_networkService.IsConnected)
            {
                _logger.LogWarning("Cannot sync report {ReportId}: No network connection available", id);
                return false;
            }

            // Get report from repository
            var report = await _reportRepository.GetReportAsync(id);
            if (report == null)
            {
                _logger.LogWarning("Cannot sync report {ReportId}: Report not found", id);
                return false;
            }

            // If already synced, return success
            if (report.IsSynced)
            {
                _logger.LogInformation("Report {ReportId} is already synced", id);
                return true;
            }

            _logger.LogInformation("Synchronizing report {ReportId}", id);

            try
            {
                // Create API request from report model
                var request = ReportRequest.FromReportModel(report);

                // Send to API
                var response = await _apiService.PostAsync<ReportResponse>(ApiEndpoints.Reports, request);

                // Update local record with remote ID and sync status
                if (!string.IsNullOrEmpty(response.Id))
                {
                    await _reportRepository.UpdateRemoteIdAsync(id, response.Id);
                    await _reportRepository.UpdateSyncStatusAsync(new[] { id }, true);

                    _logger.LogInformation("Successfully synchronized report {ReportId} with remote ID {RemoteId}", id, response.Id);
                    return true;
                }
                else
                {
                    _logger.LogWarning("API returned success but no remote ID for report {ReportId}", id);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error synchronizing report {ReportId}: {ErrorMessage}", id, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Gets the count of reports that are pending synchronization.
        /// </summary>
        /// <returns>The number of reports pending synchronization.</returns>
        public async Task<int> GetPendingSyncCountAsync()
        {
            try
            {
                return await _reportRepository.GetReportCountAsync(e => !e.IsSynced);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending sync count: {ErrorMessage}", ex.Message);
                return 0;
            }
        }

        /// <summary>
        /// Retries synchronizing reports that previously failed to synchronize.
        /// </summary>
        /// <returns>The number of successfully synchronized reports after retry.</returns>
        public async Task<int> RetryFailedSyncsAsync()
        {
            _logger.LogInformation("Retrying failed report synchronizations");
            return await SyncReportsAsync();
        }

        /// <summary>
        /// Checks if any report synchronization is currently in progress.
        /// </summary>
        /// <returns>True if any synchronization is in progress, false otherwise.</returns>
        public Task<bool> IsSyncInProgressAsync()
        {
            return Task.FromResult(_isSyncing);
        }

        /// <summary>
        /// Synchronizes the deletion of a report with the backend API.
        /// </summary>
        /// <param name="id">The local ID of the report to delete.</param>
        /// <param name="remoteId">The remote ID of the report to delete from the server.</param>
        /// <returns>True if the deletion was synchronized successfully, false otherwise.</returns>
        public async Task<bool> SyncDeletedReportAsync(int id, string remoteId)
        {
            if (!_networkService.IsConnected)
            {
                _logger.LogWarning("Cannot sync report deletion {ReportId}: No network connection available", id);
                return false;
            }

            // If no remote ID, nothing to delete on server
            if (string.IsNullOrEmpty(remoteId))
            {
                _logger.LogInformation("Report {ReportId} has no remote ID, no need to delete from server", id);
                return true;
            }

            _logger.LogInformation("Synchronizing deletion of report {ReportId} with remote ID {RemoteId}", id, remoteId);

            try
            {
                // Delete from API
                var response = await _apiService.DeleteAsync<ReportResponse>($"{ApiEndpoints.Reports}/{remoteId}");

                _logger.LogInformation("Successfully deleted report {ReportId} from server", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting report {ReportId} from server: {ErrorMessage}", id, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Raises the SyncStatusChanged event.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        protected virtual void OnSyncStatusChanged(SyncStatusChangedEventArgs e)
        {
            SyncStatusChanged?.Invoke(this, e);
        }
    }
}