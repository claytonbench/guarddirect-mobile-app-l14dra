using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks; // Version 8.0+
using SecurityPatrol.Database.Entities;
using SecurityPatrol.Models;

namespace SecurityPatrol.Services
{
    /// <summary>
    /// Defines the contract for location data repository operations in the Security Patrol application.
    /// This interface provides methods for storing, retrieving, and managing GPS location data
    /// captured during active shifts, supporting both real-time tracking and offline operation
    /// with eventual synchronization.
    /// </summary>
    public interface ILocationRepository
    {
        /// <summary>
        /// Saves a single location record to the database.
        /// </summary>
        /// <param name="location">The location data to save.</param>
        /// <returns>A task that returns the ID of the saved location record.</returns>
        Task<int> SaveLocationAsync(LocationModel location);

        /// <summary>
        /// Saves a batch of location records to the database in a single transaction.
        /// Used for efficient batch processing of location updates collected during tracking.
        /// </summary>
        /// <param name="locations">The collection of location data to save.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SaveLocationBatchAsync(IEnumerable<LocationModel> locations);

        /// <summary>
        /// Gets a location record by its ID.
        /// </summary>
        /// <param name="id">The ID of the location record to retrieve.</param>
        /// <returns>A task that returns the location record with the specified ID, or null if not found.</returns>
        Task<LocationModel> GetLocationAsync(int id);

        /// <summary>
        /// Gets all location records that match the specified predicate.
        /// </summary>
        /// <param name="predicate">The condition to filter location records.</param>
        /// <returns>A task that returns a collection of location records matching the predicate.</returns>
        Task<IEnumerable<LocationModel>> GetLocationsAsync(Expression<Func<LocationRecordEntity, bool>> predicate);

        /// <summary>
        /// Gets the most recent location records up to the specified limit.
        /// </summary>
        /// <param name="limit">The maximum number of records to retrieve.</param>
        /// <returns>A task that returns a collection of the most recent location records.</returns>
        Task<IEnumerable<LocationModel>> GetRecentLocationsAsync(int limit);

        /// <summary>
        /// Gets location records that have not been synchronized with the backend.
        /// Used by the synchronization service to identify records that need to be uploaded.
        /// </summary>
        /// <param name="limit">The maximum number of records to retrieve.</param>
        /// <returns>A task that returns a collection of unsynchronized location records.</returns>
        Task<IEnumerable<LocationModel>> GetPendingSyncLocationsAsync(int limit);

        /// <summary>
        /// Updates the synchronization status of location records.
        /// Called after successful synchronization with the backend.
        /// </summary>
        /// <param name="ids">The IDs of the location records to update.</param>
        /// <param name="isSynced">The synchronization status to set.</param>
        /// <returns>A task that returns the number of records updated.</returns>
        Task<int> UpdateSyncStatusAsync(IEnumerable<int> ids, bool isSynced);

        /// <summary>
        /// Updates the remote ID of a location record after successful synchronization.
        /// </summary>
        /// <param name="id">The ID of the location record to update.</param>
        /// <param name="remoteId">The remote ID assigned by the backend.</param>
        /// <returns>A task that returns 1 if the update was successful, 0 otherwise.</returns>
        Task<int> UpdateRemoteIdAsync(int id, string remoteId);

        /// <summary>
        /// Deletes location records older than the specified date that have been synchronized.
        /// Implements the 30-day retention policy for location data.
        /// </summary>
        /// <param name="olderThan">The cutoff date for deletion.</param>
        /// <returns>A task that returns the number of records deleted.</returns>
        Task<int> DeleteOldLocationsAsync(DateTime olderThan);

        /// <summary>
        /// Gets the count of location records that match the specified predicate.
        /// </summary>
        /// <param name="predicate">The condition to filter location records.</param>
        /// <returns>A task that returns the count of matching records.</returns>
        Task<int> GetLocationCountAsync(Expression<Func<LocationRecordEntity, bool>> predicate);

        /// <summary>
        /// Gets location records within the specified time range.
        /// Useful for generating reports or visualizing patrol routes for a specific period.
        /// </summary>
        /// <param name="startTime">The start of the time range.</param>
        /// <param name="endTime">The end of the time range.</param>
        /// <returns>A task that returns a collection of location records within the time range.</returns>
        Task<IEnumerable<LocationModel>> GetLocationsByTimeRangeAsync(DateTime startTime, DateTime endTime);
    }
}