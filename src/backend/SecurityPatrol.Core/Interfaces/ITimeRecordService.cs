using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Core.Models;

namespace SecurityPatrol.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for the service that handles time tracking operations in the Security Patrol application.
    /// This interface provides methods for creating, retrieving, and managing time records (clock in/out events),
    /// enforcing business rules, and abstracting repository operations.
    /// </summary>
    public interface ITimeRecordService
    {
        /// <summary>
        /// Creates a new time record (clock in/out event) based on the provided request.
        /// </summary>
        /// <param name="request">The time record request containing type, timestamp, and location.</param>
        /// <param name="userId">The ID of the user creating the time record.</param>
        /// <returns>Result containing the created time record response or error information.</returns>
        Task<Result<TimeRecordResponse>> CreateTimeRecordAsync(TimeRecordRequest request, string userId);
        
        /// <summary>
        /// Retrieves a time record by its unique identifier.
        /// </summary>
        /// <param name="id">The ID of the time record to retrieve.</param>
        /// <returns>Result containing the time record or error information.</returns>
        Task<Result<TimeRecord>> GetTimeRecordByIdAsync(int id);
        
        /// <summary>
        /// Retrieves the time record history for a specific user with pagination.
        /// </summary>
        /// <param name="userId">The ID of the user whose time records to retrieve.</param>
        /// <param name="pageNumber">The page number to retrieve (1-based).</param>
        /// <param name="pageSize">The number of records per page.</param>
        /// <returns>Result containing a paginated list of time records or error information.</returns>
        Task<Result<PaginatedList<TimeRecord>>> GetTimeRecordHistoryAsync(string userId, int pageNumber, int pageSize);
        
        /// <summary>
        /// Retrieves time records for a specific user within a date range.
        /// </summary>
        /// <param name="userId">The ID of the user whose time records to retrieve.</param>
        /// <param name="startDate">The start date of the range.</param>
        /// <param name="endDate">The end date of the range.</param>
        /// <returns>Result containing the time records within the date range or error information.</returns>
        Task<Result<IEnumerable<TimeRecord>>> GetTimeRecordsByDateRangeAsync(string userId, DateTime startDate, DateTime endDate);
        
        /// <summary>
        /// Determines if a user is currently clocked in or out.
        /// </summary>
        /// <param name="userId">The ID of the user to check.</param>
        /// <returns>Result containing the current status ("in" or "out") or error information.</returns>
        Task<Result<string>> GetCurrentStatusAsync(string userId);
        
        /// <summary>
        /// Retrieves the most recent time record for a specific user.
        /// </summary>
        /// <param name="userId">The ID of the user whose latest time record to retrieve.</param>
        /// <returns>Result containing the most recent time record or error information.</returns>
        Task<Result<TimeRecord>> GetLatestTimeRecordAsync(string userId);
        
        /// <summary>
        /// Deletes a time record by its unique identifier.
        /// </summary>
        /// <param name="id">The ID of the time record to delete.</param>
        /// <param name="userId">The ID of the user requesting the deletion.</param>
        /// <returns>Result indicating success or failure of the deletion operation.</returns>
        Task<Result> DeleteTimeRecordAsync(int id, string userId);
        
        /// <summary>
        /// Deletes time records older than a specified date.
        /// </summary>
        /// <param name="olderThan">The cutoff date for deletion.</param>
        /// <param name="onlySynced">If true, only deletes records that have been synced.</param>
        /// <returns>Result containing the number of records deleted or error information.</returns>
        Task<Result<int>> CleanupOldRecordsAsync(DateTime olderThan, bool onlySynced);
        
        /// <summary>
        /// Updates the sync status of a time record.
        /// </summary>
        /// <param name="id">The ID of the time record to update.</param>
        /// <param name="isSynced">The new sync status.</param>
        /// <returns>Result indicating success or failure of the update operation.</returns>
        Task<Result> UpdateSyncStatusAsync(int id, bool isSynced);
        
        /// <summary>
        /// Retrieves time records that have not been synced with mobile clients.
        /// </summary>
        /// <returns>Result containing the unsynced time records or error information.</returns>
        Task<Result<IEnumerable<TimeRecord>>> GetUnsyncedRecordsAsync();
    }
}