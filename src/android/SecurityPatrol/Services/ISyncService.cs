using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SecurityPatrol.Models;

namespace SecurityPatrol.Services
{
    /// <summary>
    /// Interface that defines the contract for the synchronization service that manages data synchronization between the mobile application and backend services.
    /// Provides methods for synchronizing all data types, scheduling automatic synchronization, and monitoring synchronization status.
    /// </summary>
    public interface ISyncService
    {
        /// <summary>
        /// Gets a value indicating whether a synchronization operation is currently in progress.
        /// </summary>
        bool IsSyncing { get; }

        /// <summary>
        /// Event that is raised when the synchronization status changes.
        /// </summary>
        event EventHandler<SyncStatusChangedEventArgs> SyncStatusChanged;

        /// <summary>
        /// Synchronizes all pending data with the backend services.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation, containing the results of the synchronization.</returns>
        Task<SyncResult> SyncAll(CancellationToken cancellationToken = default);

        /// <summary>
        /// Synchronizes a specific entity with the backend services.
        /// </summary>
        /// <param name="entityType">The type of entity to synchronize (e.g., 'Location', 'TimeRecord', 'Photo', 'Report').</param>
        /// <param name="entityId">The ID of the entity to synchronize.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation, returning true if synchronization was successful.</returns>
        Task<bool> SyncEntity(string entityType, string entityId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Synchronizes all entities of a specific type with the backend services.
        /// </summary>
        /// <param name="entityType">The type of entities to synchronize (e.g., 'Location', 'TimeRecord', 'Photo', 'Report').</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation, containing the results of the synchronization.</returns>
        Task<SyncResult> SyncEntity(string entityType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Schedules automatic synchronization at specified intervals.
        /// </summary>
        /// <param name="interval">The time interval between synchronization attempts.</param>
        void ScheduleSync(TimeSpan interval);

        /// <summary>
        /// Cancels any scheduled automatic synchronization.
        /// </summary>
        void CancelScheduledSync();

        /// <summary>
        /// Gets the current synchronization status, including pending items count by entity type.
        /// </summary>
        /// <returns>A dictionary containing the count of pending items for each entity type.</returns>
        Task<Dictionary<string, int>> GetSyncStatus();
    }
}