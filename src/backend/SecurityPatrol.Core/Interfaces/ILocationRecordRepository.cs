using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SecurityPatrol.Core.Entities;

namespace SecurityPatrol.Core.Interfaces
{
    /// <summary>
    /// Provides data access operations for GPS location tracking data.
    /// This interface supports the location tracking feature of the Security Patrol application,
    /// enabling storage, retrieval, and management of location data points captured during security patrols.
    /// </summary>
    public interface ILocationRecordRepository
    {
        /// <summary>
        /// Retrieves a location record by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the location record.</param>
        /// <returns>The location record with the specified ID, or null if not found.</returns>
        Task<LocationRecord> GetByIdAsync(int id);

        /// <summary>
        /// Retrieves location records for a specific user with optional limit.
        /// Useful for displaying recent location history in the application.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="limit">The maximum number of records to retrieve.</param>
        /// <returns>A collection of location records for the specified user.</returns>
        Task<IEnumerable<LocationRecord>> GetByUserIdAsync(string userId, int limit);

        /// <summary>
        /// Retrieves location records for a specific user within a time range.
        /// Essential for reviewing patrol routes for specific time periods.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="startTime">The start time of the range.</param>
        /// <param name="endTime">The end time of the range.</param>
        /// <returns>A collection of location records for the specified user within the time range.</returns>
        Task<IEnumerable<LocationRecord>> GetByUserIdAndTimeRangeAsync(string userId, DateTime startTime, DateTime endTime);

        /// <summary>
        /// Adds a new location record to the system.
        /// Used for individual location updates during tracking.
        /// </summary>
        /// <param name="locationRecord">The location record to add.</param>
        /// <returns>The ID of the newly created location record if successful.</returns>
        Task<int> AddAsync(LocationRecord locationRecord);

        /// <summary>
        /// Adds multiple location records to the system in a batch operation.
        /// Optimized for efficient storage of location data collected in memory,
        /// supporting the batch processing requirement (50 records or 60 seconds).
        /// </summary>
        /// <param name="locationRecords">The collection of location records to add.</param>
        /// <returns>The IDs of successfully added records.</returns>
        Task<IEnumerable<int>> AddRangeAsync(IEnumerable<LocationRecord> locationRecords);

        /// <summary>
        /// Updates an existing location record in the system.
        /// </summary>
        /// <param name="locationRecord">The location record to update.</param>
        /// <returns>True if the update was successful, false otherwise.</returns>
        Task<bool> UpdateAsync(LocationRecord locationRecord);

        /// <summary>
        /// Updates the synchronization status of location records.
        /// Called after successful synchronization with the backend API.
        /// </summary>
        /// <param name="ids">The IDs of the location records to update.</param>
        /// <param name="isSynced">The new synchronization status.</param>
        /// <returns>True if the update was successful, false otherwise.</returns>
        Task<bool> UpdateSyncStatusAsync(IEnumerable<int> ids, bool isSynced);

        /// <summary>
        /// Deletes a location record from the system.
        /// </summary>
        /// <param name="id">The ID of the location record to delete.</param>
        /// <returns>True if the delete was successful, false otherwise.</returns>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Deletes location records older than a specified date.
        /// Supports the data retention policy of keeping location history for 30 days
        /// and then automatically purging via scheduled job.
        /// </summary>
        /// <param name="date">The cutoff date for deletion.</param>
        /// <param name="onlySynced">If true, only deletes records that have been synchronized to ensure data is not lost.</param>
        /// <returns>The number of records deleted.</returns>
        Task<int> DeleteOlderThanAsync(DateTime date, bool onlySynced);

        /// <summary>
        /// Retrieves location records that have not been synchronized.
        /// Used by the synchronization service to find records that need to be sent to the backend.
        /// </summary>
        /// <param name="limit">The maximum number of records to retrieve.</param>
        /// <returns>A collection of unsynced location records.</returns>
        Task<IEnumerable<LocationRecord>> GetUnsyncedRecordsAsync(int limit);

        /// <summary>
        /// Retrieves the most recent location record for a specific user.
        /// Used to display current user location on maps and for proximity calculations.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>The most recent location record for the specified user, or null if none exists.</returns>
        Task<LocationRecord> GetLatestLocationAsync(string userId);
    }
}