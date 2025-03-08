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

namespace SecurityPatrol.Services
{
    /// <summary>
    /// Repository implementation for time records that provides data access operations for
    /// clock in/out events in the Security Patrol application.
    /// </summary>
    public class TimeRecordRepository : BaseRepository<TimeRecordEntity, TimeRecordModel>, ITimeRecordRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TimeRecordRepository"/> class.
        /// </summary>
        /// <param name="databaseService">The database service for data access operations.</param>
        /// <param name="logger">The logger for recording repository activities.</param>
        public TimeRecordRepository(IDatabaseService databaseService, ILogger<TimeRecordRepository> logger)
            : base(databaseService, logger)
        {
            _logger.LogInformation("TimeRecordRepository initialized");
        }

        /// <summary>
        /// Saves a time record to the database. If the record has an ID of 0, it will be inserted as a new record;
        /// otherwise, it will be updated.
        /// </summary>
        /// <param name="record">The time record to save.</param>
        /// <returns>A task that returns the ID of the saved record.</returns>
        /// <exception cref="ArgumentNullException">Thrown when record is null.</exception>
        /// <exception cref="SQLiteException">Thrown when a database error occurs.</exception>
        public async Task<int> SaveTimeRecordAsync(TimeRecordModel record)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));

            try
            {
                var entity = ConvertToEntity(record);
                
                if (entity.Id == 0)
                {
                    _logger.LogInformation($"Inserting new time record of type {record.Type}");
                    return await InsertAsync(entity);
                }
                else
                {
                    _logger.LogInformation($"Updating time record with ID {record.Id}");
                    await UpdateAsync(entity);
                    return entity.Id;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving time record: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Retrieves a specified number of time records, ordered by timestamp descending.
        /// </summary>
        /// <param name="count">The maximum number of records to retrieve.</param>
        /// <returns>A task that returns a list of time records.</returns>
        /// <exception cref="ArgumentException">Thrown when count is less than or equal to 0.</exception>
        /// <exception cref="SQLiteException">Thrown when a database error occurs.</exception>
        public async Task<List<TimeRecordModel>> GetTimeRecordsAsync(int count)
        {
            if (count <= 0)
                throw new ArgumentException("Count must be greater than 0", nameof(count));

            try
            {
                _logger.LogInformation($"Retrieving {count} time records");
                var connection = await GetConnectionAsync();
                var entities = await connection.Table<TimeRecordEntity>()
                    .OrderByDescending(x => x.Timestamp)
                    .Take(count)
                    .ToListAsync();

                _logger.LogDebug($"Retrieved {entities.Count} time records");
                return entities.Select(ConvertToModel).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving time records: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Retrieves a time record by its ID.
        /// </summary>
        /// <param name="id">The ID of the time record to retrieve.</param>
        /// <returns>A task that returns the time record with the specified ID, or null if not found.</returns>
        /// <exception cref="ArgumentException">Thrown when id is less than or equal to 0.</exception>
        /// <exception cref="SQLiteException">Thrown when a database error occurs.</exception>
        public async Task<TimeRecordModel> GetTimeRecordByIdAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("ID must be greater than 0", nameof(id));

            try
            {
                return await GetByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving time record with ID {id}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Retrieves time records that have not been synchronized with the backend.
        /// </summary>
        /// <returns>A task that returns a list of unsynchronized time records.</returns>
        /// <exception cref="SQLiteException">Thrown when a database error occurs.</exception>
        public async Task<List<TimeRecordModel>> GetPendingRecordsAsync()
        {
            try
            {
                _logger.LogInformation("Retrieving pending time records");
                Expression<Func<TimeRecordEntity, bool>> predicate = e => !e.IsSynced;
                return await GetByExpressionAsync(predicate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving pending time records: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Retrieves the most recent clock event (in or out).
        /// </summary>
        /// <returns>A task that returns the most recent time record, or null if none exists.</returns>
        /// <exception cref="SQLiteException">Thrown when a database error occurs.</exception>
        public async Task<TimeRecordModel> GetLatestClockEventAsync()
        {
            try
            {
                _logger.LogInformation("Retrieving latest clock event");
                var connection = await GetConnectionAsync();
                var entity = await connection.Table<TimeRecordEntity>()
                    .OrderByDescending(x => x.Timestamp)
                    .FirstOrDefaultAsync();

                if (entity == null)
                {
                    _logger.LogDebug("No clock events found");
                    return null;
                }

                _logger.LogDebug($"Retrieved latest clock event of type {entity.Type} from {entity.Timestamp}");
                return ConvertToModel(entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving latest clock event: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Retrieves the most recent clock-in event.
        /// </summary>
        /// <returns>A task that returns the most recent clock-in record, or null if none exists.</returns>
        /// <exception cref="SQLiteException">Thrown when a database error occurs.</exception>
        public async Task<TimeRecordModel> GetLatestClockInEventAsync()
        {
            try
            {
                _logger.LogInformation("Retrieving latest clock-in event");
                var connection = await GetConnectionAsync();
                var entity = await connection.Table<TimeRecordEntity>()
                    .Where(x => x.Type == "ClockIn")
                    .OrderByDescending(x => x.Timestamp)
                    .FirstOrDefaultAsync();

                if (entity == null)
                {
                    _logger.LogDebug("No clock-in events found");
                    return null;
                }

                _logger.LogDebug($"Retrieved latest clock-in event from {entity.Timestamp}");
                return ConvertToModel(entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving latest clock-in event: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Retrieves the most recent clock-out event.
        /// </summary>
        /// <returns>A task that returns the most recent clock-out record, or null if none exists.</returns>
        /// <exception cref="SQLiteException">Thrown when a database error occurs.</exception>
        public async Task<TimeRecordModel> GetLatestClockOutEventAsync()
        {
            try
            {
                _logger.LogInformation("Retrieving latest clock-out event");
                var connection = await GetConnectionAsync();
                var entity = await connection.Table<TimeRecordEntity>()
                    .Where(x => x.Type == "ClockOut")
                    .OrderByDescending(x => x.Timestamp)
                    .FirstOrDefaultAsync();

                if (entity == null)
                {
                    _logger.LogDebug("No clock-out events found");
                    return null;
                }

                _logger.LogDebug($"Retrieved latest clock-out event from {entity.Timestamp}");
                return ConvertToModel(entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving latest clock-out event: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Updates the synchronization status of a time record.
        /// </summary>
        /// <param name="id">The ID of the time record to update.</param>
        /// <param name="isSynced">The new synchronization status.</param>
        /// <returns>A task that returns the number of records updated (should be 1 for success).</returns>
        /// <exception cref="ArgumentException">Thrown when id is less than or equal to 0.</exception>
        /// <exception cref="SQLiteException">Thrown when a database error occurs.</exception>
        public async Task<int> UpdateSyncStatusAsync(int id, bool isSynced)
        {
            if (id <= 0)
                throw new ArgumentException("ID must be greater than 0", nameof(id));

            try
            {
                _logger.LogInformation($"Updating sync status for time record {id} to {isSynced}");
                var connection = await GetConnectionAsync();
                return await connection.ExecuteAsync(
                    $"UPDATE {TableName} SET IsSynced = ? WHERE Id = ?",
                    isSynced, id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating sync status for time record {id}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Updates the remote ID of a time record after successful synchronization.
        /// </summary>
        /// <param name="id">The ID of the time record to update.</param>
        /// <param name="remoteId">The remote ID assigned by the backend API.</param>
        /// <returns>A task that returns the number of records updated (should be 1 for success).</returns>
        /// <exception cref="ArgumentException">Thrown when id is less than or equal to 0 or remoteId is null or empty.</exception>
        /// <exception cref="SQLiteException">Thrown when a database error occurs.</exception>
        public async Task<int> UpdateRemoteIdAsync(int id, string remoteId)
        {
            if (id <= 0)
                throw new ArgumentException("ID must be greater than 0", nameof(id));
            if (string.IsNullOrEmpty(remoteId))
                throw new ArgumentException("Remote ID cannot be null or empty", nameof(remoteId));

            try
            {
                _logger.LogInformation($"Updating remote ID for time record {id} to {remoteId}");
                var connection = await GetConnectionAsync();
                return await connection.ExecuteAsync(
                    $"UPDATE {TableName} SET RemoteId = ? WHERE Id = ?",
                    remoteId, id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating remote ID for time record {id}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Deletes a time record from the database.
        /// </summary>
        /// <param name="id">The ID of the time record to delete.</param>
        /// <returns>A task that returns the number of records deleted (should be 1 for success).</returns>
        /// <exception cref="ArgumentException">Thrown when id is less than or equal to 0.</exception>
        /// <exception cref="SQLiteException">Thrown when a database error occurs.</exception>
        public async Task<int> DeleteTimeRecordAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("ID must be greater than 0", nameof(id));

            try
            {
                return await DeleteAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting time record with ID {id}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Deletes time records older than the specified retention period (90 days by default) that have been synchronized.
        /// This implements the application's data retention policy for time records.
        /// </summary>
        /// <param name="retentionDays">The number of days to retain records (default is 90).</param>
        /// <returns>A task that returns the number of records deleted.</returns>
        /// <exception cref="ArgumentException">Thrown when retentionDays is less than or equal to 0.</exception>
        /// <exception cref="SQLiteException">Thrown when a database error occurs.</exception>
        public async Task<int> CleanupOldRecordsAsync(int retentionDays = 90)
        {
            if (retentionDays <= 0)
                throw new ArgumentException("Retention days must be greater than 0", nameof(retentionDays));

            try
            {
                _logger.LogInformation($"Cleaning up time records older than {retentionDays} days");
                var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
                var connection = await GetConnectionAsync();
                var result = await connection.ExecuteAsync(
                    $"DELETE FROM {TableName} WHERE Timestamp < ? AND IsSynced = 1",
                    cutoffDate);

                _logger.LogInformation($"Deleted {result} old time records");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error cleaning up old time records: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Converts a TimeRecordEntity to a TimeRecordModel.
        /// </summary>
        /// <param name="entity">The entity to convert.</param>
        /// <returns>The entity converted to a model.</returns>
        protected override TimeRecordModel ConvertToModel(TimeRecordEntity entity)
        {
            return TimeRecordModel.FromEntity(entity);
        }

        /// <summary>
        /// Converts a TimeRecordModel to a TimeRecordEntity.
        /// </summary>
        /// <param name="model">The model to convert.</param>
        /// <returns>The model converted to an entity.</returns>
        protected override TimeRecordEntity ConvertToEntity(TimeRecordModel model)
        {
            return model.ToEntity();
        }
    }
}