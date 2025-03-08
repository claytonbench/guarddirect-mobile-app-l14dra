using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SecurityPatrol.Models;
using SecurityPatrol.Services;
using SecurityPatrol.TestCommon.Constants;

namespace SecurityPatrol.TestCommon.Mocks
{
    /// <summary>
    /// Mock implementation of ISyncService for testing purposes that simulates synchronization 
    /// functionality without accessing actual backend services.
    /// </summary>
    public class MockSyncService : ISyncService
    {
        // Properties to control behavior
        public bool IsSyncing { get; private set; }
        public bool ShouldSucceed { get; set; }
        public bool ShouldThrowException { get; set; }
        
        // Tracking properties
        private Dictionary<string, int> _pendingCounts;
        private Dictionary<string, List<string>> _entityIds;
        public TimeSpan LastScheduledInterval { get; private set; }
        public bool IsScheduled { get; private set; }
        public string LastSyncedEntityType { get; private set; }
        public string LastSyncedEntityId { get; private set; }
        public int SyncAllCallCount { get; private set; }
        public int SyncEntityCallCount { get; private set; }
        
        // Events
        public event EventHandler<SyncStatusChangedEventArgs> SyncStatusChanged;
        
        /// <summary>
        /// Initializes a new instance of the MockSyncService class with default test values.
        /// </summary>
        public MockSyncService()
        {
            // Initialize with default values
            Reset();
        }
        
        /// <summary>
        /// Simulates synchronizing all pending data with the backend services.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation, containing the results of the synchronization.</returns>
        public async Task<SyncResult> SyncAll(CancellationToken cancellationToken = default)
        {
            SyncAllCallCount++;
            
            if (ShouldThrowException)
            {
                throw new Exception("Simulated exception in SyncAll");
            }
            
            if (IsSyncing)
            {
                return new SyncResult { PendingCount = GetTotalPendingCount() };
            }
            
            IsSyncing = true;
            
            var result = new SyncResult();
            OnSyncStatusChanged(new SyncStatusChangedEventArgs("All", "Starting", 0, GetTotalPendingCount()));
            
            foreach (var entityType in _pendingCounts.Keys)
            {
                if (ShouldSucceed)
                {
                    foreach (var entityId in _entityIds[entityType])
                    {
                        result.AddSuccess(entityType, entityId);
                    }
                    _pendingCounts[entityType] = 0;
                    _entityIds[entityType].Clear();
                }
                else
                {
                    foreach (var entityId in _entityIds[entityType])
                    {
                        result.AddFailure(entityType, entityId, "Simulated failure");
                    }
                }
                
                OnSyncStatusChanged(new SyncStatusChangedEventArgs(
                    entityType, 
                    ShouldSucceed ? "Success" : "Failed", 
                    ShouldSucceed ? _entityIds[entityType].Count : 0, 
                    _entityIds[entityType].Count));
            }
            
            IsSyncing = false;
            OnSyncStatusChanged(new SyncStatusChangedEventArgs("All", "Completed", result.SuccessCount, result.GetTotalCount()));
            
            return result;
        }
        
        /// <summary>
        /// Simulates synchronizing a specific entity with the backend services.
        /// </summary>
        /// <param name="entityType">The type of entity to synchronize.</param>
        /// <param name="entityId">The ID of the entity to synchronize.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation, returning true if synchronization was successful.</returns>
        public async Task<bool> SyncEntity(string entityType, string entityId, CancellationToken cancellationToken = default)
        {
            SyncEntityCallCount++;
            LastSyncedEntityType = entityType;
            LastSyncedEntityId = entityId;
            
            if (ShouldThrowException)
            {
                throw new Exception("Simulated exception in SyncEntity");
            }
            
            if (!_pendingCounts.ContainsKey(entityType))
            {
                return false;
            }
            
            OnSyncStatusChanged(new SyncStatusChangedEventArgs(entityType, "Starting", 0, 1));
            
            if (ShouldSucceed)
            {
                if (_entityIds[entityType].Contains(entityId))
                {
                    _entityIds[entityType].Remove(entityId);
                    _pendingCounts[entityType]--;
                }
                
                OnSyncStatusChanged(new SyncStatusChangedEventArgs(entityType, "Success", 1, 1));
                return true;
            }
            else
            {
                OnSyncStatusChanged(new SyncStatusChangedEventArgs(entityType, "Failed", 0, 1));
                return false;
            }
        }
        
        /// <summary>
        /// Simulates synchronizing all entities of a specific type with the backend services.
        /// </summary>
        /// <param name="entityType">The type of entities to synchronize.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation, containing the results of the synchronization.</returns>
        public async Task<SyncResult> SyncEntity(string entityType, CancellationToken cancellationToken = default)
        {
            SyncEntityCallCount++;
            LastSyncedEntityType = entityType;
            
            if (ShouldThrowException)
            {
                throw new Exception("Simulated exception in SyncEntity");
            }
            
            if (!_pendingCounts.ContainsKey(entityType))
            {
                return new SyncResult();
            }
            
            var result = new SyncResult();
            int totalCount = _entityIds[entityType].Count;
            
            OnSyncStatusChanged(new SyncStatusChangedEventArgs(entityType, "Starting", 0, totalCount));
            
            if (ShouldSucceed)
            {
                foreach (var entityId in _entityIds[entityType])
                {
                    result.AddSuccess(entityType, entityId);
                }
                
                _entityIds[entityType].Clear();
                _pendingCounts[entityType] = 0;
                
                OnSyncStatusChanged(new SyncStatusChangedEventArgs(entityType, "Success", totalCount, totalCount));
            }
            else
            {
                foreach (var entityId in _entityIds[entityType])
                {
                    result.AddFailure(entityType, entityId, "Simulated failure");
                }
                
                OnSyncStatusChanged(new SyncStatusChangedEventArgs(entityType, "Failed", 0, totalCount));
            }
            
            return result;
        }
        
        /// <summary>
        /// Simulates scheduling automatic synchronization at specified intervals.
        /// </summary>
        /// <param name="interval">The time interval between synchronization attempts.</param>
        public void ScheduleSync(TimeSpan interval)
        {
            LastScheduledInterval = interval;
            IsScheduled = true;
        }
        
        /// <summary>
        /// Simulates cancelling any scheduled automatic synchronization.
        /// </summary>
        public void CancelScheduledSync()
        {
            IsScheduled = false;
        }
        
        /// <summary>
        /// Gets the current synchronization status, including pending items count by entity type.
        /// </summary>
        /// <returns>A dictionary containing the count of pending items for each entity type.</returns>
        public async Task<Dictionary<string, int>> GetSyncStatus()
        {
            if (ShouldThrowException)
            {
                throw new Exception("Simulated exception in GetSyncStatus");
            }
            
            // Return a copy of the dictionary to prevent external modification
            return new Dictionary<string, int>(_pendingCounts);
        }
        
        /// <summary>
        /// Sets the number of pending items for a specific entity type for testing scenarios.
        /// </summary>
        /// <param name="entityType">The type of entity to set pending items for.</param>
        /// <param name="count">The number of pending items to set.</param>
        public void SetPendingItems(string entityType, int count)
        {
            if (_pendingCounts.ContainsKey(entityType))
            {
                _pendingCounts[entityType] = count;
            }
            else
            {
                _pendingCounts.Add(entityType, count);
            }
            
            // Generate entity IDs
            if (!_entityIds.ContainsKey(entityType))
            {
                _entityIds[entityType] = new List<string>();
            }
            else
            {
                _entityIds[entityType].Clear();
            }
            
            for (int i = 0; i < count; i++)
            {
                _entityIds[entityType].Add($"{entityType}_{i + 1}");
            }
        }
        
        /// <summary>
        /// Adds a specific entity ID to the pending items for a given entity type.
        /// </summary>
        /// <param name="entityType">The type of entity to add.</param>
        /// <param name="entityId">The ID of the entity to add.</param>
        public void AddPendingItem(string entityType, string entityId)
        {
            if (!_pendingCounts.ContainsKey(entityType))
            {
                _pendingCounts.Add(entityType, 0);
            }
            
            if (!_entityIds.ContainsKey(entityType))
            {
                _entityIds[entityType] = new List<string>();
            }
            
            if (!_entityIds[entityType].Contains(entityId))
            {
                _entityIds[entityType].Add(entityId);
                _pendingCounts[entityType]++;
            }
        }
        
        /// <summary>
        /// Resets the mock service to its initial state.
        /// </summary>
        public void Reset()
        {
            IsSyncing = false;
            ShouldSucceed = true;
            ShouldThrowException = false;
            
            _pendingCounts = new Dictionary<string, int>();
            _entityIds = new Dictionary<string, List<string>>();
            
            // Add default entity types
            _pendingCounts["TimeRecord"] = 0;
            _pendingCounts["Location"] = 0;
            _pendingCounts["Photo"] = 0;
            _pendingCounts["Report"] = 0;
            _pendingCounts["Checkpoint"] = 0;
            
            _entityIds["TimeRecord"] = new List<string>();
            _entityIds["Location"] = new List<string>();
            _entityIds["Photo"] = new List<string>();
            _entityIds["Report"] = new List<string>();
            _entityIds["Checkpoint"] = new List<string>();
            
            LastScheduledInterval = TimeSpan.Zero;
            IsScheduled = false;
            LastSyncedEntityType = null;
            LastSyncedEntityId = null;
            SyncAllCallCount = 0;
            SyncEntityCallCount = 0;
        }
        
        /// <summary>
        /// Raises the SyncStatusChanged event.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        protected virtual void OnSyncStatusChanged(SyncStatusChangedEventArgs e)
        {
            SyncStatusChanged?.Invoke(this, e);
        }
        
        /// <summary>
        /// Gets the total count of pending items across all entity types.
        /// </summary>
        /// <returns>The total count of pending items.</returns>
        private int GetTotalPendingCount()
        {
            int total = 0;
            foreach (var count in _pendingCounts.Values)
            {
                total += count;
            }
            return total;
        }
    }
}