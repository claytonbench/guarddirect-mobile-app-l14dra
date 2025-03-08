using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SecurityPatrol.Models;

namespace SecurityPatrol.Services
{
    /// <summary>
    /// Interface that defines the contract for the Report Service, which manages activity reports 
    /// in the Security Patrol application. This service provides methods for creating, retrieving, 
    /// updating, and deleting activity reports, as well as synchronizing them with the backend API.
    /// </summary>
    public interface IReportService
    {
        /// <summary>
        /// Creates a new activity report with the specified text and location.
        /// </summary>
        /// <param name="text">The content of the activity report.</param>
        /// <param name="latitude">The latitude coordinate where the report was created.</param>
        /// <param name="longitude">The longitude coordinate where the report was created.</param>
        /// <returns>A task that returns the created report model.</returns>
        Task<ReportModel> CreateReportAsync(string text, double latitude, double longitude);

        /// <summary>
        /// Gets an activity report by its ID.
        /// </summary>
        /// <param name="id">The unique identifier of the report to retrieve.</param>
        /// <returns>A task that returns the report with the specified ID, or null if not found.</returns>
        Task<ReportModel> GetReportAsync(int id);

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
        /// Updates an existing activity report.
        /// </summary>
        /// <param name="report">The report model with updated values.</param>
        /// <returns>A task that returns true if the update was successful, false otherwise.</returns>
        Task<bool> UpdateReportAsync(ReportModel report);

        /// <summary>
        /// Deletes an activity report.
        /// </summary>
        /// <param name="id">The unique identifier of the report to delete.</param>
        /// <returns>A task that returns true if the deletion was successful, false otherwise.</returns>
        Task<bool> DeleteReportAsync(int id);

        /// <summary>
        /// Synchronizes a specific report with the backend API.
        /// </summary>
        /// <param name="id">The unique identifier of the report to synchronize.</param>
        /// <returns>A task that returns true if the synchronization was successful, false otherwise.</returns>
        Task<bool> SyncReportAsync(int id);

        /// <summary>
        /// Synchronizes all unsynchronized reports with the backend API.
        /// </summary>
        /// <returns>A task that returns the number of successfully synchronized reports.</returns>
        Task<int> SyncAllReportsAsync();

        /// <summary>
        /// Gets activity reports within the specified date range.
        /// </summary>
        /// <param name="startDate">The start date of the range (inclusive).</param>
        /// <param name="endDate">The end date of the range (inclusive).</param>
        /// <returns>A task that returns a collection of reports within the date range.</returns>
        Task<IEnumerable<ReportModel>> GetReportsByDateRangeAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Deletes synchronized reports older than the specified retention period.
        /// </summary>
        /// <param name="retentionDays">The number of days to retain reports.</param>
        /// <returns>A task that returns the number of deleted reports.</returns>
        Task<int> CleanupOldReportsAsync(int retentionDays);
    }
}