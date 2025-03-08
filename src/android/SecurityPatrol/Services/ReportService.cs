using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using SecurityPatrol.Models;
using SecurityPatrol.Helpers;
using SecurityPatrol.Constants;

namespace SecurityPatrol.Services
{
    /// <summary>
    /// Implementation of the IReportService interface that provides functionality for creating, retrieving, 
    /// updating, and deleting activity reports in the Security Patrol application.
    /// This service manages the lifecycle of activity reports, including local storage and synchronization with the backend API.
    /// </summary>
    public class ReportService : IReportService
    {
        private readonly IReportRepository _reportRepository;
        private readonly IReportSyncService _reportSyncService;
        private readonly INetworkService _networkService;
        private readonly IAuthenticationStateProvider _authStateProvider;
        private readonly ILogger<ReportService> _logger;

        /// <summary>
        /// Initializes a new instance of the ReportService class with required dependencies
        /// </summary>
        /// <param name="reportRepository">Repository for report data access operations</param>
        /// <param name="reportSyncService">Service for report synchronization with the backend</param>
        /// <param name="networkService">Service for checking network connectivity</param>
        /// <param name="authStateProvider">Provider for accessing authentication state</param>
        /// <param name="logger">Logger for logging service activities</param>
        public ReportService(
            IReportRepository reportRepository,
            IReportSyncService reportSyncService,
            INetworkService networkService,
            IAuthenticationStateProvider authStateProvider,
            ILogger<ReportService> logger)
        {
            _reportRepository = reportRepository ?? throw new ArgumentNullException(nameof(reportRepository));
            _reportSyncService = reportSyncService ?? throw new ArgumentNullException(nameof(reportSyncService));
            _networkService = networkService ?? throw new ArgumentNullException(nameof(networkService));
            _authStateProvider = authStateProvider ?? throw new ArgumentNullException(nameof(authStateProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<ReportModel> CreateReportAsync(string text, double latitude, double longitude)
        {
            _logger.LogInformation("Creating new report with text length: {TextLength}", text?.Length ?? 0);
            
            // Validate report text
            var (isValid, errorMessage) = ValidationHelper.ValidateReportText(text);
            if (!isValid)
            {
                _logger.LogWarning("Report validation failed: {ErrorMessage}", errorMessage);
                throw new ArgumentException(errorMessage);
            }

            try
            {
                // Get current user ID from authentication state
                var authState = await _authStateProvider.GetCurrentState();
                if (!authState.IsAuthenticated)
                {
                    _logger.LogWarning("Attempted to create report while not authenticated");
                    throw new InvalidOperationException("User must be authenticated to create reports.");
                }

                // Create new report
                var report = new ReportModel
                {
                    UserId = authState.PhoneNumber, // Using phone number as user ID
                    Text = text,
                    Timestamp = DateTime.UtcNow,
                    Latitude = latitude,
                    Longitude = longitude,
                    IsSynced = false
                };

                // Save to repository
                int id = await _reportRepository.SaveReportAsync(report);
                report.Id = id;

                _logger.LogInformation("Report created with ID: {ReportId}", report.Id);

                // Try to sync immediately if network is available
                if (_networkService.IsConnected)
                {
                    _logger.LogInformation("Network available, attempting to sync report {ReportId}", report.Id);
                    await _reportSyncService.SyncReportAsync(report.Id);
                }
                else
                {
                    _logger.LogInformation("Network unavailable, report {ReportId} will be synced later", report.Id);
                }

                return report;
            }
            catch (Exception ex) when (ex is not ArgumentException)
            {
                _logger.LogError(ex, "Error creating report: {Message}", ex.Message);
                throw new InvalidOperationException(ErrorMessages.ReportSubmissionFailed, ex);
            }
        }

        /// <inheritdoc/>
        public async Task<ReportModel> GetReportAsync(int id)
        {
            _logger.LogInformation("Getting report with ID: {ReportId}", id);

            if (id <= 0)
            {
                _logger.LogWarning("Invalid report ID: {ReportId}", id);
                throw new ArgumentException("Report ID must be greater than zero.", nameof(id));
            }

            try
            {
                var report = await _reportRepository.GetReportAsync(id);
                if (report == null)
                {
                    _logger.LogInformation("Report not found with ID: {ReportId}", id);
                }
                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving report {ReportId}: {Message}", id, ex.Message);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ReportModel>> GetAllReportsAsync()
        {
            _logger.LogInformation("Getting all reports");

            try
            {
                var reports = await _reportRepository.GetAllReportsAsync();
                _logger.LogInformation("Retrieved {Count} reports", reports?.Count() ?? 0);
                return reports;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all reports: {Message}", ex.Message);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ReportModel>> GetRecentReportsAsync(int limit)
        {
            _logger.LogInformation("Getting {Limit} recent reports", limit);

            if (limit <= 0)
            {
                _logger.LogWarning("Invalid limit: {Limit}", limit);
                throw new ArgumentException("Limit must be greater than zero.", nameof(limit));
            }

            try
            {
                var reports = await _reportRepository.GetRecentReportsAsync(limit);
                _logger.LogInformation("Retrieved {Count} recent reports", reports?.Count() ?? 0);
                return reports;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recent reports: {Message}", ex.Message);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> UpdateReportAsync(ReportModel report)
        {
            _logger.LogInformation("Updating report with ID: {ReportId}", report?.Id);

            if (report == null)
            {
                _logger.LogWarning("Report is null");
                throw new ArgumentNullException(nameof(report));
            }

            if (report.Id <= 0)
            {
                _logger.LogWarning("Invalid report ID: {ReportId}", report.Id);
                throw new ArgumentException("Report ID must be greater than zero.", nameof(report));
            }

            // Validate report text
            var (isValid, errorMessage) = ValidationHelper.ValidateReportText(report.Text);
            if (!isValid)
            {
                _logger.LogWarning("Report validation failed: {ErrorMessage}", errorMessage);
                throw new ArgumentException(errorMessage);
            }

            try
            {
                // Get existing report to verify it exists
                var existingReport = await _reportRepository.GetReportAsync(report.Id);
                if (existingReport == null)
                {
                    _logger.LogWarning("Report not found with ID: {ReportId}", report.Id);
                    return false;
                }

                // Preserve original user ID and creation timestamp
                report.UserId = existingReport.UserId;
                report.Timestamp = existingReport.Timestamp;
                
                // Mark as not synced to ensure it gets synchronized
                report.IsSynced = false;

                // Update in repository
                int rowsAffected = await _reportRepository.UpdateReportAsync(report);
                bool success = rowsAffected > 0;

                if (success)
                {
                    _logger.LogInformation("Report updated with ID: {ReportId}", report.Id);

                    // Try to sync immediately if network is available
                    if (_networkService.IsConnected)
                    {
                        _logger.LogInformation("Network available, attempting to sync updated report {ReportId}", report.Id);
                        await _reportSyncService.SyncReportAsync(report.Id);
                    }
                    else
                    {
                        _logger.LogInformation("Network unavailable, updated report {ReportId} will be synced later", report.Id);
                    }
                }
                else
                {
                    _logger.LogWarning("Failed to update report with ID: {ReportId}", report.Id);
                }

                return success;
            }
            catch (Exception ex) when (ex is not ArgumentException && ex is not ArgumentNullException)
            {
                _logger.LogError(ex, "Error updating report {ReportId}: {Message}", report.Id, ex.Message);
                throw new InvalidOperationException("Failed to update report.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteReportAsync(int id)
        {
            _logger.LogInformation("Deleting report with ID: {ReportId}", id);

            if (id <= 0)
            {
                _logger.LogWarning("Invalid report ID: {ReportId}", id);
                throw new ArgumentException("Report ID must be greater than zero.", nameof(id));
            }

            try
            {
                // Get the report first to check if it exists and get its RemoteId for API deletion
                var report = await _reportRepository.GetReportAsync(id);
                if (report == null)
                {
                    _logger.LogWarning("Report not found with ID: {ReportId}", id);
                    return false;
                }

                // Store RemoteId for potential API deletion
                string remoteId = report.RemoteId;

                // Delete from repository
                int rowsAffected = await _reportRepository.DeleteReportAsync(id);
                bool success = rowsAffected > 0;

                if (success)
                {
                    _logger.LogInformation("Report deleted with ID: {ReportId}", id);

                    // If report was previously synced with the API, we need to sync the deletion
                    if (!string.IsNullOrEmpty(remoteId) && _networkService.IsConnected)
                    {
                        _logger.LogInformation("Network available, syncing deletion of report {ReportId} with RemoteId {RemoteId}", id, remoteId);
                        await _reportSyncService.SyncDeletedReportAsync(id, remoteId);
                    }
                }
                else
                {
                    _logger.LogWarning("Failed to delete report with ID: {ReportId}", id);
                }

                return success;
            }
            catch (Exception ex) when (ex is not ArgumentException)
            {
                _logger.LogError(ex, "Error deleting report {ReportId}: {Message}", id, ex.Message);
                throw new InvalidOperationException("Failed to delete report.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<bool> SyncReportAsync(int id)
        {
            _logger.LogInformation("Syncing report with ID: {ReportId}", id);

            if (id <= 0)
            {
                _logger.LogWarning("Invalid report ID: {ReportId}", id);
                throw new ArgumentException("Report ID must be greater than zero.", nameof(id));
            }

            // Check network connectivity
            if (!_networkService.IsConnected)
            {
                _logger.LogInformation("Network unavailable, cannot sync report {ReportId}", id);
                return false;
            }

            try
            {
                bool syncResult = await _reportSyncService.SyncReportAsync(id);
                if (syncResult)
                {
                    _logger.LogInformation("Successfully synced report {ReportId}", id);
                }
                else
                {
                    _logger.LogWarning("Failed to sync report {ReportId}", id);
                }
                return syncResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing report {ReportId}: {Message}", id, ex.Message);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<int> SyncAllReportsAsync()
        {
            _logger.LogInformation("Syncing all reports");

            // Check network connectivity
            if (!_networkService.IsConnected)
            {
                _logger.LogInformation("Network unavailable, cannot sync reports");
                return 0;
            }

            try
            {
                int syncCount = await _reportSyncService.SyncReportsAsync();
                _logger.LogInformation("Successfully synced {Count} reports", syncCount);
                return syncCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing all reports: {Message}", ex.Message);
                return 0;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ReportModel>> GetReportsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            _logger.LogInformation("Getting reports between {StartDate} and {EndDate}", startDate, endDate);

            if (startDate > endDate)
            {
                _logger.LogWarning("Invalid date range: start date {StartDate} is after end date {EndDate}", startDate, endDate);
                throw new ArgumentException("Start date cannot be after end date.");
            }

            try
            {
                var reports = await _reportRepository.GetReportsByTimeRangeAsync(startDate, endDate);
                _logger.LogInformation("Retrieved {Count} reports in date range", reports?.Count() ?? 0);
                return reports;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving reports by date range: {Message}", ex.Message);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<int> CleanupOldReportsAsync(int retentionDays)
        {
            _logger.LogInformation("Cleaning up reports older than {RetentionDays} days", retentionDays);

            if (retentionDays <= 0)
            {
                _logger.LogWarning("Invalid retention days: {RetentionDays}, using default", retentionDays);
                retentionDays = AppConstants.ReportRetentionDays;
            }

            try
            {
                // Calculate cutoff date
                DateTime cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
                _logger.LogInformation("Cutoff date for report cleanup: {CutoffDate}", cutoffDate);

                // Delete old reports
                int deletedCount = await _reportRepository.DeleteOldReportsAsync(cutoffDate);
                _logger.LogInformation("Deleted {Count} old reports", deletedCount);
                return deletedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up old reports: {Message}", ex.Message);
                throw;
            }
        }
    }
}