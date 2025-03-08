using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Core.Models;

namespace SecurityPatrol.Core.Interfaces
{
    /// <summary>
    /// Repository interface for managing TimeRecord entities.
    /// Provides methods for creating, retrieving, updating, and deleting time records,
    /// as well as specialized queries for filtering and pagination.
    /// </summary>
    public interface ITimeRecordRepository
    {
        /// <summary>
        /// Retrieves a time record by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the time record.</param>
        /// <returns>The time record with the specified ID, or null if not found.</returns>
        Task<TimeRecord> GetByIdAsync(int id);

        /// <summary>
        /// Retrieves a time record by its remote identifier (assigned by the mobile app).
        /// </summary>
        /// <param name="remoteId">The remote identifier of the time record.</param>
        /// <returns>The time record with the specified remote ID, or null if not found.</returns>
        Task<TimeRecord> GetByRemoteIdAsync(string remoteId);

        /// <summary>
        /// Retrieves all time records.
        /// </summary>
        /// <returns>A collection of all time records.</returns>
        Task<IEnumerable<TimeRecord>> GetAllAsync();

        /// <summary>
        /// Retrieves a paginated list of time records.
        /// </summary>
        /// <param name="pageNumber">The page number to retrieve (1-based index).</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>A paginated list of time records.</returns>
        Task<PaginatedList<TimeRecord>> GetPaginatedAsync(int pageNumber, int pageSize);

        /// <summary>
        /// Retrieves all time records for a specific user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>A collection of time records for the specified user.</returns>
        Task<IEnumerable<TimeRecord>> GetByUserIdAsync(string userId);

        /// <summary>
        /// Retrieves a paginated list of time records for a specific user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="pageNumber">The page number to retrieve (1-based index).</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>A paginated list of time records for the specified user.</returns>
        Task<PaginatedList<TimeRecord>> GetPaginatedByUserIdAsync(string userId, int pageNumber, int pageSize);

        /// <summary>
        /// Retrieves time records for a specific user on a specific date.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="date">The date to filter records by.</param>
        /// <returns>A collection of time records for the specified user and date.</returns>
        Task<IEnumerable<TimeRecord>> GetByUserIdAndDateAsync(string userId, DateTime date);

        /// <summary>
        /// Retrieves time records for a specific user within a date range.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="startDate">The start date of the range (inclusive).</param>
        /// <param name="endDate">The end date of the range (inclusive).</param>
        /// <returns>A collection of time records for the specified user within the date range.</returns>
        Task<IEnumerable<TimeRecord>> GetByUserIdAndDateRangeAsync(string userId, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Retrieves time records that have not been synced.
        /// </summary>
        /// <returns>A collection of unsynced time records.</returns>
        Task<IEnumerable<TimeRecord>> GetUnsyncedAsync();

        /// <summary>
        /// Retrieves the most recent time record for a specific user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>The most recent time record for the specified user, or null if none exists.</returns>
        Task<TimeRecord> GetLatestByUserIdAsync(string userId);

        /// <summary>
        /// Determines if a user is currently clocked in or out.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>The current status ("in" or "out") of the user.</returns>
        Task<string> GetCurrentStatusAsync(string userId);

        /// <summary>
        /// Adds a new time record to the repository.
        /// </summary>
        /// <param name="timeRecord">The time record to add.</param>
        /// <returns>The added time record with its assigned ID.</returns>
        Task<TimeRecord> AddAsync(TimeRecord timeRecord);

        /// <summary>
        /// Updates an existing time record in the repository.
        /// </summary>
        /// <param name="timeRecord">The time record to update.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpdateAsync(TimeRecord timeRecord);

        /// <summary>
        /// Deletes a time record from the repository.
        /// </summary>
        /// <param name="id">The ID of the time record to delete.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteAsync(int id);

        /// <summary>
        /// Updates the sync status of a time record.
        /// </summary>
        /// <param name="id">The ID of the time record to update.</param>
        /// <param name="isSynced">The new sync status value.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpdateSyncStatusAsync(int id, bool isSynced);

        /// <summary>
        /// Deletes time records older than a specified date.
        /// </summary>
        /// <param name="date">The date threshold. Records older than this date will be deleted.</param>
        /// <param name="onlySynced">If true, only synced records will be deleted.</param>
        /// <returns>The number of records deleted.</returns>
        Task<int> DeleteOlderThanAsync(DateTime date, bool onlySynced);

        /// <summary>
        /// Gets the total count of time records in the repository.
        /// </summary>
        /// <returns>The total number of time records.</returns>
        Task<int> CountAsync();

        /// <summary>
        /// Gets the count of time records for a specific user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>The number of time records for the specified user.</returns>
        Task<int> CountByUserIdAsync(string userId);
    }
}