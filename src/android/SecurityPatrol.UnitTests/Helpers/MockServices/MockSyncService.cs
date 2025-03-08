using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SecurityPatrol.Models;
using SecurityPatrol.Services;

namespace SecurityPatrol.UnitTests.Helpers.MockServices
{
    /// <summary>
    /// Mock implementation of ISyncService for unit testing that provides configurable responses
    /// for synchronization operations without making actual API requests.
    /// </summary>
    public class MockSyncService : ISyncService
    {
        /// <summary>
        /// Gets a value indicating whether a synchronization operation is currently in progress.
        /// </summary>
        public bool IsSyncing { get; private set; }

        /// <summary>
        /// Event that is raised when the synchronization status changes.
        /// </summary>
        public event EventHandler<SyncStatusChangedEventArgs> SyncStatusChanged;

        // Configured results for method responses
        private SyncResult SyncAllResult { get; set; }
        private Dictionary<string, bool> SyncEntityResults { get; set; }
        private Dictionary<string, SyncResult> SyncEntityTypeResults { get; set; }
        private Dictionary<string, int> SyncStatusResults { get; set; }

        // Properties for controlling exception behavior
        private bool ShouldThrowException { get; set; }
        private Exception ExceptionToThrow { get; set; }

        // Call tracking properties
        private int ScheduleSyncCallCount { get; set; }
        private int CancelScheduledSyncCallCount { get; set; }
        private List<TimeSpan> ScheduledIntervals { get; set; }
        private List<string> SyncEntityCalls { get; set; }
        private List<string> SyncEntityTypeCalls { get; set; }
        private int SyncAllCallCount { get; set; }

        /// <summary>
        /// Initializes a new instance of the MockSyncService class with default settings.
        /// </summary>
        public MockSyncService()
        {
            IsSyncing = false;
            SyncAllResult = new SyncResult();
            SyncEntityResults = new Dictionary<string, bool>();
            SyncEntityTypeResults = new Dictionary<string, SyncResult>();
            SyncStatusResults = new Dictionary<string, int>();
            ShouldThrowException = false;
            ExceptionToThrow = null;
            ScheduleSyncCallCount = 0;
            CancelScheduledSyncCallCount = 0;
            ScheduledIntervals = new List<TimeSpan>();
            SyncEntityCalls = new List<string>();
            SyncEntityTypeCalls = new List<string>();
            SyncAllCallCount = 0;
        }

        /// <summary>
        /// Mocks synchronizing all pending data with the backend services.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>The configured mock result for the synchronization operation.</returns>
        public async Task<SyncResult> SyncAll(CancellationToken cancellationToken = default)
        {
            if (ShouldThrowException)
                throw ExceptionToThrow;

            SyncAllCallCount++;
            IsSyncing = true;
            
            OnSyncStatusChanged(new SyncStatusChangedEventArgs(
                "All", 
                "InProgress", 
                0, 
                SyncAllResult.GetTotalCount()));

            // Simulate some delay for async operation
            await Task.Delay(100, cancellationToken);

            IsSyncing = false;
            
            OnSyncStatusChanged(new SyncStatusChangedEventArgs(
                "All", 
                "Completed", 
                SyncAllResult.GetTotalCount(), 
                SyncAllResult.GetTotalCount()));
                
            return SyncAllResult;
        }

        /// <summary>
        /// Mocks synchronizing a specific entity with the backend services.
        /// </summary>
        /// <param name="entityType">The type of entity to synchronize.</param>
        /// <param name="entityId">The ID of the entity to synchronize.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>The configured mock result for the entity synchronization.</returns>
        public async Task<bool> SyncEntity(string entityType, string entityId, CancellationToken cancellationToken = default)
        {
            if (ShouldThrowException)
                throw ExceptionToThrow;

            string key = $"{entityType}:{entityId}";
            SyncEntityCalls.Add(key);
            
            IsSyncing = true;
            
            OnSyncStatusChanged(new SyncStatusChangedEventArgs(
                entityType, 
                "InProgress", 
                0, 
                1));
                
            // Simulate some delay for async operation
            await Task.Delay(50, cancellationToken);
            
            IsSyncing = false;
            
            OnSyncStatusChanged(new SyncStatusChangedEventArgs(
                entityType, 
                "Completed", 
                1, 
                1));
                
            return SyncEntityResults.ContainsKey(key) ? SyncEntityResults[key] : true;
        }

        /// <summary>
        /// Mocks synchronizing all entities of a specific type with the backend services.
        /// </summary>
        /// <param name="entityType">The type of entities to synchronize.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>The configured mock result for the entity type synchronization.</returns>
        public async Task<SyncResult> SyncEntity(string entityType, CancellationToken cancellationToken = default)
        {
            if (ShouldThrowException)
                throw ExceptionToThrow;

            SyncEntityTypeCalls.Add(entityType);
            
            IsSyncing = true;
            
            SyncResult result = SyncEntityTypeResults.ContainsKey(entityType) 
                ? SyncEntityTypeResults[entityType] 
                : new SyncResult();
                
            OnSyncStatusChanged(new SyncStatusChangedEventArgs(
                entityType, 
                "InProgress", 
                0, 
                result.GetTotalCount()));
                
            // Simulate some delay for async operation
            await Task.Delay(75, cancellationToken);
            
            IsSyncing = false;
            
            OnSyncStatusChanged(new SyncStatusChangedEventArgs(
                entityType, 
                "Completed", 
                result.GetTotalCount(), 
                result.GetTotalCount()));
                
            return result;
        }

        /// <summary>
        /// Mocks scheduling automatic synchronization at specified intervals.
        /// </summary>
        /// <param name="interval">The time interval between synchronization attempts.</param>
        public void ScheduleSync(TimeSpan interval)
        {
            if (ShouldThrowException)
                throw ExceptionToThrow;

            ScheduleSyncCallCount++;
            ScheduledIntervals.Add(interval);
        }

        /// <summary>
        /// Mocks cancelling any scheduled automatic synchronization.
        /// </summary>
        public void CancelScheduledSync()
        {
            if (ShouldThrowException)
                throw ExceptionToThrow;

            CancelScheduledSyncCallCount++;
        }

        /// <summary>
        /// Mocks getting the current synchronization status.
        /// </summary>
        /// <returns>The configured mock status for synchronization.</returns>
        public async Task<Dictionary<string, int>> GetSyncStatus()
        {
            if (ShouldThrowException)
                throw ExceptionToThrow;

            // Return a copy to prevent modification of our internal state
            return new Dictionary<string, int>(SyncStatusResults);
        }

        #region Setup Methods

        /// <summary>
        /// Configures the result for the SyncAll method.
        /// </summary>
        /// <param name="result">The result to return from SyncAll calls.</param>
        public void SetupSyncAllResult(SyncResult result)
        {
            SyncAllResult = result;
        }

        /// <summary>
        /// Configures the result for the SyncEntity method for a specific entity.
        /// </summary>
        /// <param name="entityType">The entity type.</param>
        /// <param name="entityId">The entity ID.</param>
        /// <param name="result">The result to return.</param>
        public void SetupSyncEntityResult(string entityType, string entityId, bool result)
        {
            SyncEntityResults[$"{entityType}:{entityId}"] = result;
        }

        /// <summary>
        /// Configures the result for the SyncEntity method for all entities of a specific type.
        /// </summary>
        /// <param name="entityType">The entity type.</param>
        /// <param name="result">The result to return.</param>
        public void SetupSyncEntityTypeResult(string entityType, SyncResult result)
        {
            SyncEntityTypeResults[entityType] = result;
        }

        /// <summary>
        /// Configures the result for the GetSyncStatus method.
        /// </summary>
        /// <param name="status">The status dictionary to return.</param>
        public void SetupSyncStatus(Dictionary<string, int> status)
        {
            SyncStatusResults.Clear();
            foreach (var item in status)
            {
                SyncStatusResults[item.Key] = item.Value;
            }
        }

        /// <summary>
        /// Configures an exception to be thrown by any method.
        /// </summary>
        /// <param name="exception">The exception to throw.</param>
        public void SetupException(Exception exception)
        {
            ShouldThrowException = true;
            ExceptionToThrow = exception;
        }

        /// <summary>
        /// Clears any configured exception.
        /// </summary>
        public void ClearException()
        {
            ShouldThrowException = false;
            ExceptionToThrow = null;
        }

        #endregion

        #region Verification Methods

        /// <summary>
        /// Verifies that SyncAll was called.
        /// </summary>
        /// <returns>True if the method was called, otherwise false.</returns>
        public bool VerifySyncAllCalled()
        {
            return SyncAllCallCount > 0;
        }

        /// <summary>
        /// Verifies that SyncEntity was called for a specific entity.
        /// </summary>
        /// <param name="entityType">The entity type.</param>
        /// <param name="entityId">The entity ID.</param>
        /// <returns>True if the method was called with the specified parameters, otherwise false.</returns>
        public bool VerifySyncEntityCalled(string entityType, string entityId)
        {
            return SyncEntityCalls.Contains($"{entityType}:{entityId}");
        }

        /// <summary>
        /// Verifies that SyncEntity was called for all entities of a specific type.
        /// </summary>
        /// <param name="entityType">The entity type.</param>
        /// <returns>True if the method was called with the specified entity type, otherwise false.</returns>
        public bool VerifySyncEntityTypeCalled(string entityType)
        {
            return SyncEntityTypeCalls.Contains(entityType);
        }

        /// <summary>
        /// Verifies that ScheduleSync was called.
        /// </summary>
        /// <returns>True if the method was called, otherwise false.</returns>
        public bool VerifyScheduleSyncCalled()
        {
            return ScheduleSyncCallCount > 0;
        }

        /// <summary>
        /// Verifies that CancelScheduledSync was called.
        /// </summary>
        /// <returns>True if the method was called, otherwise false.</returns>
        public bool VerifyCancelScheduledSyncCalled()
        {
            return CancelScheduledSyncCallCount > 0;
        }

        #endregion

        /// <summary>
        /// Resets all configurations and call history.
        /// </summary>
        public void Reset()
        {
            IsSyncing = false;
            SyncAllResult = new SyncResult();
            SyncEntityResults.Clear();
            SyncEntityTypeResults.Clear();
            SyncStatusResults.Clear();
            ShouldThrowException = false;
            ExceptionToThrow = null;
            ScheduleSyncCallCount = 0;
            CancelScheduledSyncCallCount = 0;
            ScheduledIntervals.Clear();
            SyncEntityCalls.Clear();
            SyncEntityTypeCalls.Clear();
            SyncAllCallCount = 0;
        }

        /// <summary>
        /// Raises the SyncStatusChanged event.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        protected virtual void OnSyncStatusChanged(SyncStatusChangedEventArgs e)
        {
            SyncStatusChanged?.Invoke(this, e);
        }
    }
}