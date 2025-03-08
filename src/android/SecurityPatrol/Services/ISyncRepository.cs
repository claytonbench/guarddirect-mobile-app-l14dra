using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SecurityPatrol.Models;

namespace SecurityPatrol.Services
{
    /// <summary>
    /// Interface for managing synchronization queue items and tracking synchronization attempts in the local database.
    /// Provides methods for adding, retrieving, updating, and removing items from the synchronization queue,
    /// as well as logging and retrieving synchronization attempts.
    /// </summary>
    public interface ISyncRepository
    {
        /// <summary>
        /// Retrieves pending synchronization items of a specific entity type, ordered by priority and retry count
        /// </summary>
        /// <param name="entityType">The type of entity to retrieve sync items for</param>
        /// <returns>A collection of pending synchronization items for the specified entity type</returns>
        Task<IEnumerable<SyncItem>> GetPendingSync(string entityType);

        /// <summary>
        /// Retrieves all pending synchronization items, ordered by priority and retry count
        /// </summary>
        /// <returns>A collection of all pending synchronization items</returns>
        Task<IEnumerable<SyncItem>> GetAllPendingSync();

        /// <summary>
        /// Adds a new item to the synchronization queue
        /// </summary>
        /// <param name="entityType">The type of entity being synchronized</param>
        /// <param name="entityId">The unique identifier of the entity being synchronized</param>
        /// <param name="priority">The priority of the sync item (higher values indicate higher priority)</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task AddSyncItem(string entityType, string entityId, int priority);

        /// <summary>
        /// Adds a SyncItem to the synchronization queue
        /// </summary>
        /// <param name="item">The SyncItem to add to the queue</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task AddSyncItem(SyncItem item);

        /// <summary>
        /// Updates the status of a synchronization item, typically after a sync attempt
        /// </summary>
        /// <param name="entityType">The type of entity being synchronized</param>
        /// <param name="entityId">The unique identifier of the entity being synchronized</param>
        /// <param name="success">Whether the synchronization attempt was successful</param>
        /// <param name="errorMessage">The error message if the synchronization attempt failed</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task UpdateSyncStatus(string entityType, string entityId, bool success, string errorMessage = null);

        /// <summary>
        /// Removes an item from the synchronization queue
        /// </summary>
        /// <param name="entityType">The type of entity being synchronized</param>
        /// <param name="entityId">The unique identifier of the entity being synchronized</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task RemoveSyncItem(string entityType, string entityId);

        /// <summary>
        /// Logs a synchronization attempt for tracking and analysis purposes
        /// </summary>
        /// <param name="entityType">The type of entity being synchronized</param>
        /// <param name="entityId">The unique identifier of the entity being synchronized</param>
        /// <param name="success">Whether the synchronization attempt was successful</param>
        /// <param name="errorMessage">The error message if the synchronization attempt failed</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task LogSyncAttempt(string entityType, string entityId, bool success, string errorMessage = null);

        /// <summary>
        /// Retrieves the synchronization history for a specific entity
        /// </summary>
        /// <param name="entityType">The type of entity to retrieve sync history for</param>
        /// <param name="entityId">The unique identifier of the entity to retrieve sync history for</param>
        /// <param name="count">The maximum number of history items to retrieve</param>
        /// <returns>A collection of synchronization attempts for the entity</returns>
        Task<IEnumerable<SyncAttempt>> GetSyncHistory(string entityType, string entityId, int count);

        /// <summary>
        /// Clears successfully synchronized items from the queue that are older than the specified age
        /// </summary>
        /// <param name="age">The minimum age of successfully synchronized items to clear</param>
        /// <returns>The number of items cleared from the queue</returns>
        Task<int> ClearSyncedItems(TimeSpan age);

        /// <summary>
        /// Retrieves statistics about the current synchronization queue
        /// </summary>
        /// <returns>A dictionary containing statistics about the sync queue</returns>
        Task<Dictionary<string, int>> GetSyncStatistics();
    }
}