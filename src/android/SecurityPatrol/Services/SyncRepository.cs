using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SecurityPatrol.Constants;
using SecurityPatrol.Database.Entities;
using SecurityPatrol.Models;
using SQLite;

namespace SecurityPatrol.Services
{
    /// <summary>
    /// Implementation of the ISyncRepository interface that manages synchronization queue items and tracks
    /// synchronization attempts in the local SQLite database.
    /// </summary>
    public class SyncRepository : ISyncRepository
    {
        private readonly IDatabaseService _databaseService;
        private readonly ILogger<SyncRepository> _logger;
        
        // In-memory cache for sync attempt history
        private readonly Dictionary<string, List<SyncAttempt>> _syncAttemptCache = new Dictionary<string, List<SyncAttempt>>();
        private const int MaxCachedAttemptsPerEntity = 20;

        /// <summary>
        /// Initializes a new instance of the SyncRepository class with the specified database service and logger.
        /// </summary>
        /// <param name="databaseService">The database service used for data access.</param>
        /// <param name="logger">The logger for logging repository operations.</param>
        public SyncRepository(IDatabaseService databaseService, ILogger<SyncRepository> logger)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Retrieves pending synchronization items of a specific entity type, ordered by priority and retry count.
        /// </summary>
        /// <param name="entityType">The type of entity to retrieve sync items for</param>
        /// <returns>A collection of pending synchronization items for the specified entity type</returns>
        public async Task<IEnumerable<SyncItem>> GetPendingSync(string entityType)
        {
            try
            {
                _logger.LogDebug("Retrieving pending sync items for entity type: {EntityType}", entityType);
                
                var connection = await _databaseService.GetConnectionAsync();
                
                var entities = await connection.Table<SyncQueueEntity>()
                    .Where(e => e.EntityType == entityType)
                    .OrderByDescending(e => e.Priority)
                    .ThenBy(e => e.RetryCount)
                    .ToListAsync();
                
                return entities.Select(e => SyncItem.FromEntity(e));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending sync items for entity type: {EntityType}", entityType);
                return Enumerable.Empty<SyncItem>();
            }
        }

        /// <summary>
        /// Retrieves all pending synchronization items, ordered by priority and retry count.
        /// </summary>
        /// <returns>A collection of all pending synchronization items</returns>
        public async Task<IEnumerable<SyncItem>> GetAllPendingSync()
        {
            try
            {
                _logger.LogDebug("Retrieving all pending sync items");
                
                var connection = await _databaseService.GetConnectionAsync();
                
                var entities = await connection.Table<SyncQueueEntity>()
                    .OrderByDescending(e => e.Priority)
                    .ThenBy(e => e.RetryCount)
                    .ToListAsync();
                
                return entities.Select(e => SyncItem.FromEntity(e));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all pending sync items");
                return Enumerable.Empty<SyncItem>();
            }
        }

        /// <summary>
        /// Adds a new item to the synchronization queue.
        /// </summary>
        /// <param name="entityType">The type of entity being synchronized</param>
        /// <param name="entityId">The unique identifier of the entity being synchronized</param>
        /// <param name="priority">The priority of the sync item (higher values indicate higher priority)</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task AddSyncItem(string entityType, string entityId, int priority)
        {
            try
            {
                _logger.LogDebug("Adding sync item for entity type: {EntityType}, entity ID: {EntityId}", entityType, entityId);
                
                var syncItem = new SyncItem(entityType, entityId, priority);
                await AddSyncItem(syncItem);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding sync item for entity type: {EntityType}, entity ID: {EntityId}", entityType, entityId);
            }
        }

        /// <summary>
        /// Adds a SyncItem to the synchronization queue.
        /// </summary>
        /// <param name="item">The SyncItem to add to the queue</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task AddSyncItem(SyncItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            try
            {
                _logger.LogDebug("Adding sync item: {SyncItem}", item);
                
                var connection = await _databaseService.GetConnectionAsync();
                
                // Check if item already exists
                var existingItem = await connection.Table<SyncQueueEntity>()
                    .Where(e => e.EntityType == item.EntityType && e.EntityId == item.EntityId)
                    .FirstOrDefaultAsync();
                
                if (existingItem != null)
                {
                    // Update priority if the new priority is higher
                    if (item.Priority > existingItem.Priority)
                    {
                        existingItem.Priority = item.Priority;
                        await connection.UpdateAsync(existingItem);
                        _logger.LogDebug("Updated priority for existing sync item: {SyncItem}", item);
                    }
                    else
                    {
                        _logger.LogDebug("Sync item already exists, no update needed: {SyncItem}", item);
                    }
                }
                else
                {
                    // Insert new item
                    var entity = item.ToEntity();
                    await connection.InsertAsync(entity);
                    _logger.LogDebug("Added new sync item: {SyncItem}", item);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding sync item: {SyncItem}", item);
                throw;
            }
        }

        /// <summary>
        /// Updates the status of a synchronization item, typically after a sync attempt.
        /// </summary>
        /// <param name="entityType">The type of entity being synchronized</param>
        /// <param name="entityId">The unique identifier of the entity being synchronized</param>
        /// <param name="success">Whether the synchronization attempt was successful</param>
        /// <param name="errorMessage">The error message if the synchronization attempt failed</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task UpdateSyncStatus(string entityType, string entityId, bool success, string errorMessage = null)
        {
            try
            {
                _logger.LogDebug("Updating sync status for entity type: {EntityType}, entity ID: {EntityId}, success: {Success}", 
                    entityType, entityId, success);
                
                var connection = await _databaseService.GetConnectionAsync();
                
                if (success)
                {
                    // If successful, remove the item from the sync queue
                    await connection.ExecuteAsync(
                        $"DELETE FROM {DatabaseConstants.TableSyncQueue} " +
                        $"WHERE {DatabaseConstants.ColumnEntityType} = ? AND {DatabaseConstants.ColumnEntityId} = ?",
                        entityType, entityId);
                    
                    _logger.LogDebug("Successfully synced and removed item from queue: {EntityType}:{EntityId}", entityType, entityId);
                }
                else
                {
                    // If failed, increment retry count and update last attempt timestamp
                    await connection.ExecuteAsync(
                        $"UPDATE {DatabaseConstants.TableSyncQueue} " +
                        $"SET {DatabaseConstants.ColumnRetryCount} = {DatabaseConstants.ColumnRetryCount} + 1, " +
                        $"{DatabaseConstants.ColumnLastAttempt} = ?, " +
                        $"{DatabaseConstants.ColumnErrorMessage} = ? " +
                        $"WHERE {DatabaseConstants.ColumnEntityType} = ? AND {DatabaseConstants.ColumnEntityId} = ?",
                        DateTime.UtcNow, errorMessage, entityType, entityId);
                    
                    _logger.LogDebug("Updated retry count for failed sync: {EntityType}:{EntityId}, error: {ErrorMessage}", 
                        entityType, entityId, errorMessage);
                }
                
                // Log the sync attempt
                await LogSyncAttempt(entityType, entityId, success, errorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating sync status for entity type: {EntityType}, entity ID: {EntityId}", 
                    entityType, entityId);
            }
        }

        /// <summary>
        /// Removes an item from the synchronization queue.
        /// </summary>
        /// <param name="entityType">The type of entity being synchronized</param>
        /// <param name="entityId">The unique identifier of the entity being synchronized</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task RemoveSyncItem(string entityType, string entityId)
        {
            try
            {
                _logger.LogDebug("Removing sync item for entity type: {EntityType}, entity ID: {EntityId}", entityType, entityId);
                
                var connection = await _databaseService.GetConnectionAsync();
                
                await connection.ExecuteAsync(
                    $"DELETE FROM {DatabaseConstants.TableSyncQueue} " +
                    $"WHERE {DatabaseConstants.ColumnEntityType} = ? AND {DatabaseConstants.ColumnEntityId} = ?",
                    entityType, entityId);
                
                _logger.LogDebug("Successfully removed sync item: {EntityType}:{EntityId}", entityType, entityId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing sync item for entity type: {EntityType}, entity ID: {EntityId}", 
                    entityType, entityId);
            }
        }

        /// <summary>
        /// Logs a synchronization attempt for tracking and analysis purposes.
        /// </summary>
        /// <param name="entityType">The type of entity being synchronized</param>
        /// <param name="entityId">The unique identifier of the entity being synchronized</param>
        /// <param name="success">Whether the synchronization attempt was successful</param>
        /// <param name="errorMessage">The error message if the synchronization attempt failed</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task LogSyncAttempt(string entityType, string entityId, bool success, string errorMessage = null)
        {
            try
            {
                _logger.LogDebug("Recording sync attempt for entity type: {EntityType}, entity ID: {EntityId}, success: {Success}", 
                    entityType, entityId, success);
                
                var syncAttempt = SyncAttempt.Create(entityType, entityId, success, errorMessage);
                
                // Log the attempt details
                _logger.LogInformation("Sync attempt: {EntityType}:{EntityId}, Success: {Success}, Error: {ErrorMessage}, Time: {Timestamp}",
                    entityType, entityId, success, errorMessage, syncAttempt.Timestamp);
                
                // Cache the attempt for history retrieval
                var key = $"{entityType}:{entityId}";
                
                lock (_syncAttemptCache)
                {
                    if (!_syncAttemptCache.ContainsKey(key))
                    {
                        _syncAttemptCache[key] = new List<SyncAttempt>();
                    }
                    
                    _syncAttemptCache[key].Insert(0, syncAttempt);
                    
                    // Trim cache if it exceeds the maximum size
                    if (_syncAttemptCache[key].Count > MaxCachedAttemptsPerEntity)
                    {
                        _syncAttemptCache[key].RemoveAt(_syncAttemptCache[key].Count - 1);
                    }
                }
                
                // Complete the task
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording sync attempt for entity type: {EntityType}, entity ID: {EntityId}", 
                    entityType, entityId);
            }
        }

        /// <summary>
        /// Retrieves the synchronization history for a specific entity.
        /// </summary>
        /// <param name="entityType">The type of entity to retrieve sync history for</param>
        /// <param name="entityId">The unique identifier of the entity to retrieve sync history for</param>
        /// <param name="count">The maximum number of history items to retrieve</param>
        /// <returns>A collection of synchronization attempts for the entity</returns>
        public async Task<IEnumerable<SyncAttempt>> GetSyncHistory(string entityType, string entityId, int count)
        {
            try
            {
                _logger.LogDebug("Retrieving sync history for entity type: {EntityType}, entity ID: {EntityId}, count: {Count}", 
                    entityType, entityId, count);
                
                var key = $"{entityType}:{entityId}";
                
                lock (_syncAttemptCache)
                {
                    if (_syncAttemptCache.TryGetValue(key, out var attempts))
                    {
                        return attempts.Take(count).ToList();
                    }
                }
                
                _logger.LogDebug("No sync history found for entity type: {EntityType}, entity ID: {EntityId}", entityType, entityId);
                return Enumerable.Empty<SyncAttempt>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sync history for entity type: {EntityType}, entity ID: {EntityId}", 
                    entityType, entityId);
                return Enumerable.Empty<SyncAttempt>();
            }
        }

        /// <summary>
        /// Clears successfully synchronized items from the queue that are older than the specified age.
        /// </summary>
        /// <param name="age">The minimum age of successfully synchronized items to clear</param>
        /// <returns>The number of items cleared from the queue</returns>
        public async Task<int> ClearSyncedItems(TimeSpan age)
        {
            try
            {
                _logger.LogDebug("Clearing synced items older than: {Age}", age);
                
                var cutoffTime = DateTime.UtcNow.Subtract(age);
                
                var connection = await _databaseService.GetConnectionAsync();
                
                // For this implementation, we consider items with a high retry count
                // and old last attempt as candidates for cleanup
                var result = await connection.ExecuteAsync(
                    $"DELETE FROM {DatabaseConstants.TableSyncQueue} " +
                    $"WHERE {DatabaseConstants.ColumnRetryCount} > 5 AND " +
                    $"{DatabaseConstants.ColumnLastAttempt} < ?",
                    cutoffTime);
                
                _logger.LogDebug("Cleared {Count} old sync items", result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing synced items older than: {Age}", age);
                return 0;
            }
        }

        /// <summary>
        /// Retrieves statistics about the current synchronization queue.
        /// </summary>
        /// <returns>A dictionary containing statistics about the sync queue</returns>
        public async Task<Dictionary<string, int>> GetSyncStatistics()
        {
            try
            {
                _logger.LogDebug("Retrieving sync statistics");
                
                var statistics = new Dictionary<string, int>();
                var connection = await _databaseService.GetConnectionAsync();
                
                // Total count
                var totalCount = await connection.ExecuteScalarAsync<int>(
                    $"SELECT COUNT(*) FROM {DatabaseConstants.TableSyncQueue}");
                statistics["TotalItems"] = totalCount;
                
                // Counts by entity type
                var entityTypeCounts = await _databaseService.ExecuteQueryAsync<EntityTypeCount>(
                    $"SELECT {DatabaseConstants.ColumnEntityType} as EntityType, COUNT(*) as Count " +
                    $"FROM {DatabaseConstants.TableSyncQueue} " +
                    $"GROUP BY {DatabaseConstants.ColumnEntityType}");
                
                foreach (var item in entityTypeCounts)
                {
                    statistics[$"EntityType_{item.EntityType}"] = item.Count;
                }
                
                // Counts by priority
                var priorityCounts = await _databaseService.ExecuteQueryAsync<PriorityCount>(
                    $"SELECT {DatabaseConstants.ColumnPriority} as Priority, COUNT(*) as Count " +
                    $"FROM {DatabaseConstants.TableSyncQueue} " +
                    $"GROUP BY {DatabaseConstants.ColumnPriority}");
                
                foreach (var item in priorityCounts)
                {
                    statistics[$"Priority_{item.Priority}"] = item.Count;
                }
                
                // Counts by retry count ranges
                statistics["RetryCount_0"] = await connection.ExecuteScalarAsync<int>(
                    $"SELECT COUNT(*) FROM {DatabaseConstants.TableSyncQueue} " +
                    $"WHERE {DatabaseConstants.ColumnRetryCount} = 0");
                
                statistics["RetryCount_1_3"] = await connection.ExecuteScalarAsync<int>(
                    $"SELECT COUNT(*) FROM {DatabaseConstants.TableSyncQueue} " +
                    $"WHERE {DatabaseConstants.ColumnRetryCount} BETWEEN 1 AND 3");
                
                statistics["RetryCount_4_10"] = await connection.ExecuteScalarAsync<int>(
                    $"SELECT COUNT(*) FROM {DatabaseConstants.TableSyncQueue} " +
                    $"WHERE {DatabaseConstants.ColumnRetryCount} BETWEEN 4 AND 10");
                
                statistics["RetryCount_10Plus"] = await connection.ExecuteScalarAsync<int>(
                    $"SELECT COUNT(*) FROM {DatabaseConstants.TableSyncQueue} " +
                    $"WHERE {DatabaseConstants.ColumnRetryCount} > 10");
                
                _logger.LogDebug("Retrieved sync statistics: {@Statistics}", statistics);
                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sync statistics");
                return new Dictionary<string, int>();
            }
        }

        // Helper classes for statistics queries
        private class EntityTypeCount
        {
            public string EntityType { get; set; }
            public int Count { get; set; }
        }

        private class PriorityCount
        {
            public int Priority { get; set; }
            public int Count { get; set; }
        }
    }
}