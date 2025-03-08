using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.Core.Models;

namespace SecurityPatrol.Application.Services
{
    /// <summary>
    /// Implements the IReportService interface to provide business logic for managing activity reports in the Security Patrol application.
    /// </summary>
    public class ReportService : IReportService
    {
        private readonly IReportRepository _reportRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<ReportService> _logger;

        /// <summary>
        /// Initializes a new instance of the ReportService class with required dependencies.
        /// </summary>
        /// <param name="reportRepository">The repository for report data access.</param>
        /// <param name="currentUserService">The service for accessing current user information.</param>
        /// <param name="logger">The logger for recording service activities.</param>
        public ReportService(
            IReportRepository reportRepository,
            ICurrentUserService currentUserService,
            ILogger<ReportService> logger)
        {
            _reportRepository = reportRepository ?? throw new ArgumentNullException(nameof(reportRepository));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates a new activity report based on the provided request data.
        /// </summary>
        /// <param name="request">The report request data containing text, timestamp, and location.</param>
        /// <param name="userId">The identifier of the user creating the report.</param>
        /// <returns>Result containing the response with ID and status of the created report, or error details if creation failed.</returns>
        public async Task<Result<ReportResponse>> CreateReportAsync(ReportRequest request, string userId)
        {
            if (request == null)
            {
                return Result.Failure<ReportResponse>("Report request cannot be null");
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Result.Failure<ReportResponse>("User ID cannot be null or empty");
            }

            _logger.LogInformation("Creating new report for user {UserId}", userId);

            try
            {
                var report = new Report
                {
                    Text = request.Text,
                    Timestamp = request.Timestamp,
                    Latitude = request.Location.Latitude,
                    Longitude = request.Location.Longitude,
                    UserId = userId,
                    IsSynced = false
                };

                var createdReport = await _reportRepository.AddAsync(report);
                
                var response = ReportResponse.CreateSuccess(createdReport.Id.ToString());
                return Result.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating report for user {UserId}: {ErrorMessage}", userId, ex.Message);
                return Result.Failure<ReportResponse>($"Failed to create report: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves a specific report by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the report to retrieve.</param>
        /// <returns>Result containing the report if found, or error details if not found.</returns>
        public async Task<Result<Report>> GetReportByIdAsync(int id)
        {
            if (id <= 0)
            {
                return Result.Failure<Report>("Invalid report ID");
            }

            _logger.LogInformation("Retrieving report with ID {ReportId}", id);

            try
            {
                var report = await _reportRepository.GetByIdAsync(id);
                
                if (report == null)
                {
                    return Result.Failure<Report>("Report not found");
                }

                return Result.Success(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving report with ID {ReportId}: {ErrorMessage}", id, ex.Message);
                return Result.Failure<Report>($"Failed to retrieve report: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves all reports created by a specific user.
        /// </summary>
        /// <param name="userId">The identifier of the user whose reports to retrieve.</param>
        /// <returns>Result containing the collection of reports created by the user.</returns>
        public async Task<Result<IEnumerable<Report>>> GetReportsByUserIdAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Result.Failure<IEnumerable<Report>>("User ID cannot be null or empty");
            }

            _logger.LogInformation("Retrieving reports for user {UserId}", userId);

            try
            {
                var reports = await _reportRepository.GetByUserIdAsync(userId);
                return Result.Success(reports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving reports for user {UserId}: {ErrorMessage}", userId, ex.Message);
                return Result.Failure<IEnumerable<Report>>($"Failed to retrieve reports: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves a paginated list of reports created by a specific user.
        /// </summary>
        /// <param name="userId">The identifier of the user whose reports to retrieve.</param>
        /// <param name="pageNumber">The page number to retrieve (1-based).</param>
        /// <param name="pageSize">The number of reports per page.</param>
        /// <returns>Result containing a paginated list of reports created by the user.</returns>
        public async Task<Result<PaginatedList<Report>>> GetPaginatedReportsByUserIdAsync(string userId, int pageNumber, int pageSize)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Result.Failure<PaginatedList<Report>>("User ID cannot be null or empty");
            }

            if (pageNumber < 1)
            {
                return Result.Failure<PaginatedList<Report>>("Page number must be greater than 0");
            }

            if (pageSize < 1)
            {
                return Result.Failure<PaginatedList<Report>>("Page size must be greater than 0");
            }

            _logger.LogInformation("Retrieving paginated reports for user {UserId}, page {PageNumber}, size {PageSize}", 
                userId, pageNumber, pageSize);

            try
            {
                var paginatedReports = await _reportRepository.GetPaginatedByUserIdAsync(userId, pageNumber, pageSize);
                return Result.Success(paginatedReports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paginated reports for user {UserId}: {ErrorMessage}", userId, ex.Message);
                return Result.Failure<PaginatedList<Report>>($"Failed to retrieve paginated reports: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves all reports in the system.
        /// </summary>
        /// <returns>Result containing the collection of all reports.</returns>
        public async Task<Result<IEnumerable<Report>>> GetAllReportsAsync()
        {
            _logger.LogInformation("Retrieving all reports");

            try
            {
                var reports = await _reportRepository.GetAllAsync();
                return Result.Success(reports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all reports: {ErrorMessage}", ex.Message);
                return Result.Failure<IEnumerable<Report>>($"Failed to retrieve all reports: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves a paginated list of all reports in the system.
        /// </summary>
        /// <param name="pageNumber">The page number to retrieve (1-based).</param>
        /// <param name="pageSize">The number of reports per page.</param>
        /// <returns>Result containing a paginated list of reports.</returns>
        public async Task<Result<PaginatedList<Report>>> GetPaginatedReportsAsync(int pageNumber, int pageSize)
        {
            if (pageNumber < 1)
            {
                return Result.Failure<PaginatedList<Report>>("Page number must be greater than 0");
            }

            if (pageSize < 1)
            {
                return Result.Failure<PaginatedList<Report>>("Page size must be greater than 0");
            }

            _logger.LogInformation("Retrieving paginated reports, page {PageNumber}, size {PageSize}", 
                pageNumber, pageSize);

            try
            {
                var paginatedReports = await _reportRepository.GetPaginatedAsync(pageNumber, pageSize);
                return Result.Success(paginatedReports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paginated reports: {ErrorMessage}", ex.Message);
                return Result.Failure<PaginatedList<Report>>($"Failed to retrieve paginated reports: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves reports created within a specific date range.
        /// </summary>
        /// <param name="startDate">The start date of the range (inclusive).</param>
        /// <param name="endDate">The end date of the range (inclusive).</param>
        /// <returns>Result containing the collection of reports within the date range.</returns>
        public async Task<Result<IEnumerable<Report>>> GetReportsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            if (startDate == default)
            {
                return Result.Failure<IEnumerable<Report>>("Start date must be provided");
            }

            if (endDate == default)
            {
                return Result.Failure<IEnumerable<Report>>("End date must be provided");
            }

            if (startDate > endDate)
            {
                return Result.Failure<IEnumerable<Report>>("Start date must be before or equal to end date");
            }

            _logger.LogInformation("Retrieving reports for date range {StartDate} to {EndDate}", 
                startDate, endDate);

            try
            {
                var reports = await _reportRepository.GetByDateRangeAsync(startDate, endDate);
                return Result.Success(reports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving reports by date range: {ErrorMessage}", ex.Message);
                return Result.Failure<IEnumerable<Report>>($"Failed to retrieve reports by date range: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates an existing report with new information.
        /// </summary>
        /// <param name="id">The unique identifier of the report to update.</param>
        /// <param name="request">The updated report data.</param>
        /// <param name="userId">The identifier of the user performing the update.</param>
        /// <returns>Result indicating success or failure of the update operation.</returns>
        public async Task<Result> UpdateReportAsync(int id, ReportRequest request, string userId)
        {
            if (id <= 0)
            {
                return Result.Failure("Invalid report ID");
            }

            if (request == null)
            {
                return Result.Failure("Report request cannot be null");
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Result.Failure("User ID cannot be null or empty");
            }

            _logger.LogInformation("Updating report with ID {ReportId} for user {UserId}", id, userId);

            try
            {
                var existingReport = await _reportRepository.GetByIdAsync(id);
                
                if (existingReport == null)
                {
                    return Result.Failure("Report not found");
                }

                if (existingReport.UserId != userId)
                {
                    return Result.Failure("You do not have permission to update this report");
                }

                existingReport.Text = request.Text;
                existingReport.Timestamp = request.Timestamp;
                existingReport.Latitude = request.Location.Latitude;
                existingReport.Longitude = request.Location.Longitude;
                existingReport.IsSynced = false; // Mark as needing sync

                await _reportRepository.UpdateAsync(existingReport);
                
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating report with ID {ReportId}: {ErrorMessage}", id, ex.Message);
                return Result.Failure($"Failed to update report: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes a report from the system.
        /// </summary>
        /// <param name="id">The unique identifier of the report to delete.</param>
        /// <param name="userId">The identifier of the user performing the deletion.</param>
        /// <returns>Result indicating success or failure of the delete operation.</returns>
        public async Task<Result> DeleteReportAsync(int id, string userId)
        {
            if (id <= 0)
            {
                return Result.Failure("Invalid report ID");
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Result.Failure("User ID cannot be null or empty");
            }

            _logger.LogInformation("Deleting report with ID {ReportId} for user {UserId}", id, userId);

            try
            {
                var existingReport = await _reportRepository.GetByIdAsync(id);
                
                if (existingReport == null)
                {
                    return Result.Failure("Report not found");
                }

                if (existingReport.UserId != userId)
                {
                    return Result.Failure("You do not have permission to delete this report");
                }

                await _reportRepository.DeleteAsync(id);
                
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting report with ID {ReportId}: {ErrorMessage}", id, ex.Message);
                return Result.Failure($"Failed to delete report: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the synchronization status of a report.
        /// </summary>
        /// <param name="id">The unique identifier of the report to update.</param>
        /// <param name="isSynced">The new synchronization status.</param>
        /// <returns>Result indicating success or failure of the sync status update.</returns>
        public async Task<Result> UpdateSyncStatusAsync(int id, bool isSynced)
        {
            if (id <= 0)
            {
                return Result.Failure("Invalid report ID");
            }

            _logger.LogInformation("Updating sync status for report {ReportId} to {IsSynced}", id, isSynced);

            try
            {
                await _reportRepository.UpdateSyncStatusAsync(id, isSynced);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating sync status for report {ReportId}: {ErrorMessage}", id, ex.Message);
                return Result.Failure($"Failed to update sync status: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves all reports that have not been synchronized with mobile clients.
        /// </summary>
        /// <returns>Result containing the collection of unsynced reports.</returns>
        public async Task<Result<IEnumerable<Report>>> GetUnsyncedReportsAsync()
        {
            _logger.LogInformation("Retrieving unsynced reports");

            try
            {
                var reports = await _reportRepository.GetUnsyncedAsync();
                return Result.Success(reports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving unsynced reports: {ErrorMessage}", ex.Message);
                return Result.Failure<IEnumerable<Report>>($"Failed to retrieve unsynced reports: {ex.Message}");
            }
        }
    }
}