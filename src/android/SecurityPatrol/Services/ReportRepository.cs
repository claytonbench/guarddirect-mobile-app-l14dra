using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SQLite;
using SecurityPatrol.Database.Entities;
using SecurityPatrol.Database.Repositories;
using SecurityPatrol.Models;
using SecurityPatrol.Services;

namespace SecurityPatrol.Services
{
    /// <summary>
    /// Implementation of the IReportRepository interface that provides data access operations for activity reports in the Security Patrol application.
    /// This repository handles storing, retrieving, updating, and deleting activity reports in the local SQLite database,
    /// supporting both immediate storage and offline operation with eventual synchronization.
    /// </summary>
    public class ReportRepository : BaseRepository<ReportEntity, ReportModel>, IReportRepository
    {
        private readonly IDatabaseService _databaseService;
        private readonly ILogger<ReportRepository> _logger;

        /// <summary>
        /// Initializes a new instance of the ReportRepository class with the specified database service and logger.
        /// </summary>
        /// <param name="databaseService">The database service for data access operations.</param>
        /// <param name="logger">The logger for recording repository activities.</param>
        public ReportRepository(IDatabaseService databaseService, ILogger<ReportRepository> logger)
            : base(databaseService, logger)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Saves an activity report to the database.
        /// </summary>
        /// <param name="report">The report to save.</param>
        /// <returns>A task that returns the ID of the saved report.</returns>
        public async Task<int> SaveReportAsync(ReportModel report)
        {
            try
            {
                if (report == null)
                    throw new ArgumentNullException(nameof(report));

                _logger.LogInformation($"Saving report with ID: {report.Id}");
                
                var entity = report.ToEntity();
                
                if (report.Id == 0)
                {
                    // New report
                    var id = await InsertAsync(entity);
                    _logger.LogDebug($"Inserted new report with ID: {id}");
                    return id;
                }
                else
                {
                    // Existing report
                    var result = await UpdateAsync(entity);
                    _logger.LogDebug($"Updated report with ID: {report.Id}, Result: {result}");
                    return report.Id;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving report: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets an activity report by its ID.
        /// </summary>
        /// <param name="id">The ID of the report to retrieve.</param>
        /// <returns>A task that returns the report with the specified ID, or null if not found.</returns>
        public async Task<ReportModel> GetReportAsync(int id)
        {
            try
            {
                _logger.LogInformation($"Getting report with ID: {id}");
                return await GetByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting report with ID {id}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets all activity reports that match the specified predicate.
        /// </summary>
        /// <param name="predicate">The predicate to filter the reports.</param>
        /// <returns>A task that returns a collection of reports matching the predicate.</returns>
        public async Task<IEnumerable<ReportModel>> GetReportsAsync(Expression<Func<ReportEntity, bool>> predicate)
        {
            try
            {
                _logger.LogInformation("Getting reports with predicate");
                var reports = await GetByExpressionAsync(predicate);
                _logger.LogDebug($"Retrieved {reports.Count} reports");
                return reports;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting reports with predicate: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets all activity reports.
        /// </summary>
        /// <returns>A task that returns a collection of all reports.</returns>
        public async Task<IEnumerable<ReportModel>> GetAllReportsAsync()
        {
            try
            {
                _logger.LogInformation("Getting all reports");
                var reports = await GetAllAsync();
                _logger.LogDebug($"Retrieved {reports.Count} reports");
                return reports;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting all reports: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets the most recent activity reports up to the specified limit.
        /// </summary>
        /// <param name="limit">The maximum number of reports to retrieve.</param>
        /// <returns>A task that returns a collection of the most recent reports.</returns>
        public async Task<IEnumerable<ReportModel>> GetRecentReportsAsync(int limit)
        {
            try
            {
                _logger.LogInformation($"Getting {limit} most recent reports");
                
                var connection = await GetConnectionAsync();
                var entities = await connection.Table<ReportEntity>()
                    .OrderByDescending(r => r.Timestamp)
                    .Take(limit)
                    .ToListAsync();
                
                var models = entities.Select(ConvertToModel).ToList();
                _logger.LogDebug($"Retrieved {models.Count} recent reports");
                
                return models;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting recent reports: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets activity reports that have not been synchronized with the backend.
        /// </summary>
        /// <param name="limit">The maximum number of reports to retrieve.</param>
        /// <returns>A task that returns a collection of unsynchronized reports.</returns>
        public async Task<IEnumerable<ReportModel>> GetPendingSyncReportsAsync(int limit)
        {
            try
            {
                _logger.LogInformation($"Getting pending sync reports (limit: {limit})");
                
                var reports = await GetReportsAsync(r => !r.IsSynced);
                
                var result = limit > 0 
                    ? reports.Take(limit)
                    : reports;
                
                _logger.LogDebug($"Retrieved {result.Count()} pending sync reports");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting pending sync reports: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Updates an existing activity report in the database.
        /// </summary>
        /// <param name="report">The report with updated information.</param>
        /// <returns>A task that returns the number of rows updated (should be 1 for success).</returns>
        public async Task<int> UpdateReportAsync(ReportModel report)
        {
            try
            {
                if (report == null)
                    throw new ArgumentNullException(nameof(report));
                
                if (report.Id <= 0)
                    throw new ArgumentException("Report ID must be a positive integer", nameof(report));
                
                _logger.LogInformation($"Updating report with ID: {report.Id}");
                
                var entity = report.ToEntity();
                var result = await UpdateAsync(entity);
                
                _logger.LogDebug($"Updated report with ID: {report.Id}, Result: {result}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating report: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Updates the synchronization status of activity reports.
        /// </summary>
        /// <param name="ids">The IDs of the reports to update.</param>
        /// <param name="isSynced">The new synchronization status.</param>
        /// <returns>A task that returns the number of reports updated.</returns>
        public async Task<int> UpdateSyncStatusAsync(IEnumerable<int> ids, bool isSynced)
        {
            try
            {
                if (ids == null || !ids.Any())
                    return 0;
                    
                _logger.LogInformation($"Updating sync status to {isSynced} for {ids.Count()} reports");
                
                var idList = ids.ToList();
                var placeholders = string.Join(",", idList.Select((_, i) => $"?"));
                
                var query = $"UPDATE ActivityReport SET IsSynced = ? WHERE Id IN ({placeholders})";
                
                var parameters = new object[idList.Count + 1];
                parameters[0] = isSynced;
                for (int i = 0; i < idList.Count; i++)
                {
                    parameters[i + 1] = idList[i];
                }
                
                var result = await _databaseService.ExecuteNonQueryAsync(query, parameters);
                
                _logger.LogDebug($"Updated sync status for {result} reports");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating sync status: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Updates the remote ID of an activity report after successful synchronization.
        /// </summary>
        /// <param name="id">The ID of the report to update.</param>
        /// <param name="remoteId">The remote ID from the backend.</param>
        /// <returns>A task that returns 1 if the update was successful, 0 otherwise.</returns>
        public async Task<int> UpdateRemoteIdAsync(int id, string remoteId)
        {
            try
            {
                if (id <= 0)
                    throw new ArgumentException("Report ID must be a positive integer", nameof(id));
                    
                if (string.IsNullOrEmpty(remoteId))
                    throw new ArgumentException("Remote ID cannot be null or empty", nameof(remoteId));
                    
                _logger.LogInformation($"Updating remote ID for report {id} to {remoteId}");
                
                var query = "UPDATE ActivityReport SET RemoteId = ?, IsSynced = 1 WHERE Id = ?";
                var result = await _databaseService.ExecuteNonQueryAsync(query, remoteId, id);
                
                _logger.LogDebug($"Updated remote ID for report {id}, Result: {result}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating remote ID for report {id}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Deletes an activity report from the database.
        /// </summary>
        /// <param name="id">The ID of the report to delete.</param>
        /// <returns>A task that returns the number of rows deleted (should be 1 for success).</returns>
        public async Task<int> DeleteReportAsync(int id)
        {
            try
            {
                if (id <= 0)
                    throw new ArgumentException("Report ID must be a positive integer", nameof(id));
                    
                _logger.LogInformation($"Deleting report with ID: {id}");
                
                var result = await DeleteAsync(id);
                
                _logger.LogDebug($"Deleted report with ID: {id}, Result: {result}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting report with ID {id}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Deletes activity reports older than the specified date that have been synchronized.
        /// </summary>
        /// <param name="olderThan">The date threshold; reports older than this will be deleted.</param>
        /// <returns>A task that returns the number of reports deleted.</returns>
        public async Task<int> DeleteOldReportsAsync(DateTime olderThan)
        {
            try
            {
                _logger.LogInformation($"Deleting synchronized reports older than {olderThan}");
                
                var query = "DELETE FROM ActivityReport WHERE Timestamp < ? AND IsSynced = 1";
                var result = await _databaseService.ExecuteNonQueryAsync(query, olderThan);
                
                _logger.LogDebug($"Deleted {result} old reports");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting old reports: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets the count of activity reports that match the specified predicate.
        /// </summary>
        /// <param name="predicate">The predicate to filter the reports.</param>
        /// <returns>A task that returns the count of matching reports.</returns>
        public async Task<int> GetReportCountAsync(Expression<Func<ReportEntity, bool>> predicate)
        {
            try
            {
                _logger.LogInformation("Getting report count with predicate");
                
                var connection = await GetConnectionAsync();
                var count = await connection.Table<ReportEntity>().CountAsync(predicate);
                
                _logger.LogDebug($"Count result: {count}");
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting report count: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets activity reports within the specified time range.
        /// </summary>
        /// <param name="startTime">The start of the time range.</param>
        /// <param name="endTime">The end of the time range.</param>
        /// <returns>A task that returns a collection of reports within the time range.</returns>
        public async Task<IEnumerable<ReportModel>> GetReportsByTimeRangeAsync(DateTime startTime, DateTime endTime)
        {
            try
            {
                _logger.LogInformation($"Getting reports between {startTime} and {endTime}");
                
                return await GetReportsAsync(r => r.Timestamp >= startTime && r.Timestamp <= endTime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting reports by time range: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Converts a ReportEntity to a ReportModel.
        /// </summary>
        /// <param name="entity">The entity to convert.</param>
        /// <returns>The entity converted to a model.</returns>
        protected override ReportModel ConvertToModel(ReportEntity entity)
        {
            if (entity == null)
                return null;
                
            return ReportModel.FromEntity(entity);
        }

        /// <summary>
        /// Converts a ReportModel to a ReportEntity.
        /// </summary>
        /// <param name="model">The model to convert.</param>
        /// <returns>The model converted to an entity.</returns>
        protected override ReportEntity ConvertToEntity(ReportModel model)
        {
            if (model == null)
                return null;
                
            return model.ToEntity();
        }
    }
}