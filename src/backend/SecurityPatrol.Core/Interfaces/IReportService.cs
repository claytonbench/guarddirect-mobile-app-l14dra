using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Core.Models;

namespace SecurityPatrol.Core.Interfaces
{
    /// <summary>
    /// Defines the interface for the report service which handles business logic related 
    /// to activity reports in the Security Patrol application.
    /// </summary>
    public interface IReportService
    {
        /// <summary>
        /// Creates a new activity report based on the provided request data.
        /// </summary>
        /// <param name="request">The report request data containing text, timestamp, and location.</param>
        /// <param name="userId">The identifier of the user creating the report.</param>
        /// <returns>Result containing the response with ID and status of the created report, or error details if creation failed.</returns>
        Task<Result<ReportResponse>> CreateReportAsync(ReportRequest request, string userId);

        /// <summary>
        /// Retrieves a specific report by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the report to retrieve.</param>
        /// <returns>Result containing the report if found, or error details if not found.</returns>
        Task<Result<Report>> GetReportByIdAsync(int id);

        /// <summary>
        /// Retrieves all reports created by a specific user.
        /// </summary>
        /// <param name="userId">The identifier of the user whose reports to retrieve.</param>
        /// <returns>Result containing the collection of reports created by the user.</returns>
        Task<Result<IEnumerable<Report>>> GetReportsByUserIdAsync(string userId);

        /// <summary>
        /// Retrieves a paginated list of reports created by a specific user.
        /// </summary>
        /// <param name="userId">The identifier of the user whose reports to retrieve.</param>
        /// <param name="pageNumber">The page number to retrieve (1-based).</param>
        /// <param name="pageSize">The number of reports per page.</param>
        /// <returns>Result containing a paginated list of reports created by the user.</returns>
        Task<Result<PaginatedList<Report>>> GetPaginatedReportsByUserIdAsync(string userId, int pageNumber, int pageSize);

        /// <summary>
        /// Retrieves all reports in the system.
        /// </summary>
        /// <returns>Result containing the collection of all reports.</returns>
        Task<Result<IEnumerable<Report>>> GetAllReportsAsync();

        /// <summary>
        /// Retrieves a paginated list of all reports in the system.
        /// </summary>
        /// <param name="pageNumber">The page number to retrieve (1-based).</param>
        /// <param name="pageSize">The number of reports per page.</param>
        /// <returns>Result containing a paginated list of reports.</returns>
        Task<Result<PaginatedList<Report>>> GetPaginatedReportsAsync(int pageNumber, int pageSize);

        /// <summary>
        /// Retrieves reports created within a specific date range.
        /// </summary>
        /// <param name="startDate">The start date of the range (inclusive).</param>
        /// <param name="endDate">The end date of the range (inclusive).</param>
        /// <returns>Result containing the collection of reports within the date range.</returns>
        Task<Result<IEnumerable<Report>>> GetReportsByDateRangeAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Updates an existing report with new information.
        /// </summary>
        /// <param name="id">The unique identifier of the report to update.</param>
        /// <param name="request">The updated report data.</param>
        /// <param name="userId">The identifier of the user performing the update.</param>
        /// <returns>Result indicating success or failure of the update operation.</returns>
        Task<Result> UpdateReportAsync(int id, ReportRequest request, string userId);

        /// <summary>
        /// Deletes a report from the system.
        /// </summary>
        /// <param name="id">The unique identifier of the report to delete.</param>
        /// <param name="userId">The identifier of the user performing the deletion.</param>
        /// <returns>Result indicating success or failure of the delete operation.</returns>
        Task<Result> DeleteReportAsync(int id, string userId);

        /// <summary>
        /// Updates the synchronization status of a report.
        /// </summary>
        /// <param name="id">The unique identifier of the report to update.</param>
        /// <param name="isSynced">The new synchronization status.</param>
        /// <returns>Result indicating success or failure of the sync status update.</returns>
        Task<Result> UpdateSyncStatusAsync(int id, bool isSynced);

        /// <summary>
        /// Retrieves all reports that have not been synchronized with mobile clients.
        /// </summary>
        /// <returns>Result containing the collection of unsynced reports.</returns>
        Task<Result<IEnumerable<Report>>> GetUnsyncedReportsAsync();
    }
}