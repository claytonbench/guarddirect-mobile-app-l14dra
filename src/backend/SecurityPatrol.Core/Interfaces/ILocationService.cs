using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SecurityPatrol.Core.Models;

namespace SecurityPatrol.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for the location service, which provides business logic for location tracking
    /// functionality. This service handles processing location data from mobile clients, retrieving location
    /// history, and managing the location data lifecycle.
    /// </summary>
    public interface ILocationService
    {
        /// <summary>
        /// Processes a batch of location data points received from a mobile client.
        /// </summary>
        /// <param name="request">The batch request containing user ID and location data points.</param>
        /// <returns>A response indicating which location records were successfully processed and which ones failed.</returns>
        Task<LocationSyncResponse> ProcessLocationBatchAsync(LocationBatchRequest request);

        /// <summary>
        /// Retrieves location history for a specific user within a time range.
        /// </summary>
        /// <param name="userId">The ID of the user whose location history is being requested.</param>
        /// <param name="startTime">The start of the time range (inclusive).</param>
        /// <param name="endTime">The end of the time range (inclusive).</param>
        /// <returns>A collection of location data points for the specified user within the time range.</returns>
        Task<IEnumerable<LocationModel>> GetLocationHistoryAsync(string userId, DateTime startTime, DateTime endTime);

        /// <summary>
        /// Retrieves the most recent location for a specific user.
        /// </summary>
        /// <param name="userId">The ID of the user whose location is being requested.</param>
        /// <returns>The most recent location data point for the specified user, or null if none exists.</returns>
        Task<LocationModel> GetLatestLocationAsync(string userId);

        /// <summary>
        /// Retrieves a specified number of recent locations for a specific user.
        /// </summary>
        /// <param name="userId">The ID of the user whose locations are being requested.</param>
        /// <param name="limit">The maximum number of location records to retrieve.</param>
        /// <returns>A collection of recent location data points for the specified user.</returns>
        Task<IEnumerable<LocationModel>> GetLocationsByUserIdAsync(string userId, int limit);

        /// <summary>
        /// Deletes location data older than a specified date.
        /// Implements the data retention policy (30 days as specified in requirements).
        /// </summary>
        /// <param name="olderThan">The cutoff date - records older than this will be deleted.</param>
        /// <param name="onlySynced">If true, only deletes records that have been synchronized.</param>
        /// <returns>The number of location records deleted.</returns>
        Task<int> CleanupLocationDataAsync(DateTime olderThan, bool onlySynced = true);

        /// <summary>
        /// Synchronizes pending location records with external systems.
        /// Supports the batch processing approach defined in the requirements.
        /// </summary>
        /// <param name="batchSize">The maximum number of records to process in a single batch.</param>
        /// <returns>A response indicating which location records were successfully synchronized and which ones failed.</returns>
        Task<LocationSyncResponse> SyncPendingLocationsAsync(int batchSize = 50);
    }
}