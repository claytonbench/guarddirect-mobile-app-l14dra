using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using SecurityPatrol.Database.Entities;
using SecurityPatrol.Models;

namespace SecurityPatrol.Services
{
    /// <summary>
    /// Interface that defines methods for data access operations on activity reports in the Security Patrol application.
    /// Provides operations for storing, retrieving, updating, and deleting reports in the local SQLite database.
    /// </summary>
    public interface IReportRepository
    {
        /// <summary>
        /// Saves an activity report to the database.
        /// </summary>
        /// <param name="report">The report to save.</param>
        /// <returns>A task that returns the ID of the saved report.</returns>
        Task<int> SaveReportAsync(ReportModel report);

        /// <summary>
        /// Gets an activity report by its ID.
        /// </summary>
        /// <param name="id">The ID of the report to retrieve.</param>
        /// <returns>A task that returns the report with the specified ID, or null if not found.</returns>
        Task<ReportModel> GetReportAsync(int id);

        /// <summary>
        /// Gets all activity reports that match the specified predicate.
        /// </summary>
        /// <param name="predicate">The predicate to filter the reports.</param>
        /// <returns>A task that returns a collection of reports matching the predicate.</returns>
        Task<IEnumerable<ReportModel>> GetReportsAsync(Expression<Func<ReportEntity, bool>> predicate);

        /// <summary>
        /// Gets all activity reports.
        /// </summary>
        /// <returns>A task that returns a collection of all reports.</returns>
        Task<IEnumerable<ReportModel>> GetAllReportsAsync();

        /// <summary>
        /// Gets the most recent activity reports up to the specified limit.
        /// </summary>
        /// <param name="limit">The maximum number of reports to retrieve.</param>
        /// <returns>A task that returns a collection of the most recent reports.</returns>
        Task<IEnumerable<ReportModel>> GetRecentReportsAsync(int limit);

        /// <summary>
        /// Gets activity reports that have not been synchronized with the backend.
        /// </summary>
        /// <param name="limit">The maximum number of reports to retrieve.</param>
        /// <returns>A task that returns a collection of unsynchronized reports.</returns>
        Task<IEnumerable<ReportModel>> GetPendingSyncReportsAsync(int limit);

        /// <summary>
        /// Updates an existing activity report in the database.
        /// </summary>
        /// <param name="report">The report with updated information.</param>
        /// <returns>A task that returns the number of rows updated (should be 1 for success).</returns>
        Task<int> UpdateReportAsync(ReportModel report);

        /// <summary>
        /// Updates the synchronization status of activity reports.
        /// </summary>
        /// <param name="ids">The IDs of the reports to update.</param>
        /// <param name="isSynced">The new synchronization status.</param>
        /// <returns>A task that returns the number of reports updated.</returns>
        Task<int> UpdateSyncStatusAsync(IEnumerable<int> ids, bool isSynced);

        /// <summary>
        /// Updates the remote ID of an activity report after successful synchronization.
        /// </summary>
        /// <param name="id">The ID of the report to update.</param>
        /// <param name="remoteId">The remote ID from the backend.</param>
        /// <returns>A task that returns 1 if the update was successful, 0 otherwise.</returns>
        Task<int> UpdateRemoteIdAsync(int id, string remoteId);

        /// <summary>
        /// Deletes an activity report from the database.
        /// </summary>
        /// <param name="id">The ID of the report to delete.</param>
        /// <returns>A task that returns the number of rows deleted (should be 1 for success).</returns>
        Task<int> DeleteReportAsync(int id);

        /// <summary>
        /// Deletes activity reports older than the specified date that have been synchronized.
        /// </summary>
        /// <param name="olderThan">The date threshold; reports older than this will be deleted.</param>
        /// <returns>A task that returns the number of reports deleted.</returns>
        Task<int> DeleteOldReportsAsync(DateTime olderThan);

        /// <summary>
        /// Gets the count of activity reports that match the specified predicate.
        /// </summary>
        /// <param name="predicate">The predicate to filter the reports.</param>
        /// <returns>A task that returns the count of matching reports.</returns>
        Task<int> GetReportCountAsync(Expression<Func<ReportEntity, bool>> predicate);

        /// <summary>
        /// Gets activity reports within the specified time range.
        /// </summary>
        /// <param name="startTime">The start of the time range.</param>
        /// <param name="endTime">The end of the time range.</param>
        /// <returns>A task that returns a collection of reports within the time range.</returns>
        Task<IEnumerable<ReportModel>> GetReportsByTimeRangeAsync(DateTime startTime, DateTime endTime);
    }
}