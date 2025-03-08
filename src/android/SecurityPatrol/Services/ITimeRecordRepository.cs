using System.Collections.Generic;  // Version 8.0+
using System.Threading.Tasks;  // Version 8.0+
using SecurityPatrol.Models;

namespace SecurityPatrol.Services
{
    /// <summary>
    /// Interface for the time record repository that provides data access operations for time records.
    /// Supports both online and offline operation with synchronization capabilities for the Security Patrol application.
    /// </summary>
    public interface ITimeRecordRepository
    {
        /// <summary>
        /// Saves a time record to the database. If the record has an ID of 0, it will be inserted as a new record;
        /// otherwise, it will be updated.
        /// </summary>
        /// <param name="record">The time record to save.</param>
        /// <returns>A task that returns the ID of the saved record.</returns>
        /// <exception cref="SQLiteException">Thrown when a database error occurs.</exception>
        Task<int> SaveTimeRecordAsync(TimeRecordModel record);

        /// <summary>
        /// Retrieves a specified number of time records, ordered by timestamp descending.
        /// </summary>
        /// <param name="count">The maximum number of records to retrieve.</param>
        /// <returns>A task that returns a list of time records.</returns>
        /// <exception cref="SQLiteException">Thrown when a database error occurs.</exception>
        Task<List<TimeRecordModel>> GetTimeRecordsAsync(int count);

        /// <summary>
        /// Retrieves a time record by its ID.
        /// </summary>
        /// <param name="id">The ID of the time record to retrieve.</param>
        /// <returns>A task that returns the time record with the specified ID, or null if not found.</returns>
        /// <exception cref="SQLiteException">Thrown when a database error occurs.</exception>
        Task<TimeRecordModel> GetTimeRecordByIdAsync(int id);

        /// <summary>
        /// Retrieves time records that have not been synchronized with the backend.
        /// </summary>
        /// <returns>A task that returns a list of unsynchronized time records.</returns>
        /// <exception cref="SQLiteException">Thrown when a database error occurs.</exception>
        Task<List<TimeRecordModel>> GetPendingRecordsAsync();

        /// <summary>
        /// Retrieves the most recent clock event (in or out).
        /// </summary>
        /// <returns>A task that returns the most recent time record, or null if none exists.</returns>
        /// <exception cref="SQLiteException">Thrown when a database error occurs.</exception>
        Task<TimeRecordModel> GetLatestClockEventAsync();

        /// <summary>
        /// Retrieves the most recent clock-in event.
        /// </summary>
        /// <returns>A task that returns the most recent clock-in record, or null if none exists.</returns>
        /// <exception cref="SQLiteException">Thrown when a database error occurs.</exception>
        Task<TimeRecordModel> GetLatestClockInEventAsync();

        /// <summary>
        /// Retrieves the most recent clock-out event.
        /// </summary>
        /// <returns>A task that returns the most recent clock-out record, or null if none exists.</returns>
        /// <exception cref="SQLiteException">Thrown when a database error occurs.</exception>
        Task<TimeRecordModel> GetLatestClockOutEventAsync();

        /// <summary>
        /// Updates the synchronization status of a time record.
        /// </summary>
        /// <param name="id">The ID of the time record to update.</param>
        /// <param name="isSynced">The new synchronization status.</param>
        /// <returns>A task that returns the number of records updated (should be 1 for success).</returns>
        /// <exception cref="SQLiteException">Thrown when a database error occurs.</exception>
        Task<int> UpdateSyncStatusAsync(int id, bool isSynced);

        /// <summary>
        /// Updates the remote ID of a time record after successful synchronization.
        /// </summary>
        /// <param name="id">The ID of the time record to update.</param>
        /// <param name="remoteId">The remote ID assigned by the backend API.</param>
        /// <returns>A task that returns the number of records updated (should be 1 for success).</returns>
        /// <exception cref="SQLiteException">Thrown when a database error occurs.</exception>
        Task<int> UpdateRemoteIdAsync(int id, string remoteId);

        /// <summary>
        /// Deletes a time record from the database.
        /// </summary>
        /// <param name="id">The ID of the time record to delete.</param>
        /// <returns>A task that returns the number of records deleted (should be 1 for success).</returns>
        /// <exception cref="SQLiteException">Thrown when a database error occurs.</exception>
        Task<int> DeleteTimeRecordAsync(int id);

        /// <summary>
        /// Deletes time records older than the specified retention period (90 days by default) that have been synchronized.
        /// This implements the application's data retention policy for time records.
        /// </summary>
        /// <param name="retentionDays">The number of days to retain records (default is 90).</param>
        /// <returns>A task that returns the number of records deleted.</returns>
        /// <exception cref="SQLiteException">Thrown when a database error occurs.</exception>
        Task<int> CleanupOldRecordsAsync(int retentionDays = 90);
    }
}