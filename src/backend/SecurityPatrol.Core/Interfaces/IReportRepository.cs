using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Core.Models;

namespace SecurityPatrol.Core.Interfaces
{
    /// <summary>
    /// Defines the interface for repository operations related to activity reports in the Security Patrol application.
    /// Provides methods for creating, retrieving, updating, and deleting report entries, as well as specialized queries
    /// for report management and synchronization.
    /// </summary>
    public interface IReportRepository
    {
        /// <summary>
        /// Retrieves a report by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the report.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the report if found, otherwise null.</returns>
        Task<Report> GetByIdAsync(int id);

        /// <summary>
        /// Retrieves a report by its remote identifier (assigned by the backend system).
        /// </summary>
        /// <param name="remoteId">The remote identifier of the report.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the report if found, otherwise null.</returns>
        Task<Report> GetByRemoteIdAsync(string remoteId);

        /// <summary>
        /// Retrieves all reports.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a collection of all reports.</returns>
        Task<IEnumerable<Report>> GetAllAsync();

        /// <summary>
        /// Retrieves a paginated list of reports.
        /// </summary>
        /// <param name="pageNumber">The page number to retrieve (1-based).</param>
        /// <param name="pageSize">The number of reports per page.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a paginated list of reports.</returns>
        Task<PaginatedList<Report>> GetPaginatedAsync(int pageNumber, int pageSize);

        /// <summary>
        /// Retrieves all reports for a specific user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a collection of reports for the specified user.</returns>
        Task<IEnumerable<Report>> GetByUserIdAsync(string userId);

        /// <summary>
        /// Retrieves a paginated list of reports for a specific user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="pageNumber">The page number to retrieve (1-based).</param>
        /// <param name="pageSize">The number of reports per page.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a paginated list of reports for the specified user.</returns>
        Task<PaginatedList<Report>> GetPaginatedByUserIdAsync(string userId, int pageNumber, int pageSize);

        /// <summary>
        /// Retrieves reports that have not been synced.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a collection of unsynced reports.</returns>
        Task<IEnumerable<Report>> GetUnsyncedAsync();

        /// <summary>
        /// Retrieves reports created within a specific date range.
        /// </summary>
        /// <param name="startDate">The start date of the range (inclusive).</param>
        /// <param name="endDate">The end date of the range (inclusive).</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a collection of reports created within the specified date range.</returns>
        Task<IEnumerable<Report>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Adds a new report to the repository.
        /// </summary>
        /// <param name="report">The report to add.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the added report with its assigned ID.</returns>
        Task<Report> AddAsync(Report report);

        /// <summary>
        /// Updates an existing report in the repository.
        /// </summary>
        /// <param name="report">The report to update.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task UpdateAsync(Report report);

        /// <summary>
        /// Deletes a report from the repository.
        /// </summary>
        /// <param name="id">The unique identifier of the report to delete.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task DeleteAsync(int id);

        /// <summary>
        /// Updates the sync status of a report.
        /// </summary>
        /// <param name="id">The unique identifier of the report.</param>
        /// <param name="isSynced">The sync status to set.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task UpdateSyncStatusAsync(int id, bool isSynced);

        /// <summary>
        /// Gets the total count of reports in the repository.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the count of reports.</returns>
        Task<int> CountAsync();

        /// <summary>
        /// Gets the count of reports for a specific user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the count of reports for the specified user.</returns>
        Task<int> CountByUserIdAsync(string userId);
    }
}