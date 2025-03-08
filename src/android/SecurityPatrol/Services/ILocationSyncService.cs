using System;
using System.Threading.Tasks;
using SecurityPatrol.Models;

namespace SecurityPatrol.Services
{
    /// <summary>
    /// Interface that defines the contract for location data synchronization services in the Security Patrol application.
    /// This interface provides methods for synchronizing location data with the backend API, scheduling periodic synchronization,
    /// and notifying subscribers of synchronization status changes.
    /// </summary>
    public interface ILocationSyncService
    {
        /// <summary>
        /// Synchronizes pending location records with the backend API.
        /// </summary>
        /// <param name="batchSize">The maximum number of location records to synchronize in a single batch.</param>
        /// <returns>A task that returns true if synchronization was successful, false otherwise.</returns>
        Task<bool> SyncLocationsAsync(int batchSize);

        /// <summary>
        /// Schedules periodic synchronization of location data.
        /// </summary>
        /// <param name="interval">The time interval between synchronization operations.</param>
        void ScheduleSync(TimeSpan interval);

        /// <summary>
        /// Cancels any scheduled synchronization.
        /// </summary>
        void CancelScheduledSync();

        /// <summary>
        /// Gets a value indicating whether a synchronization operation is currently in progress.
        /// </summary>
        bool IsSyncing { get; }

        /// <summary>
        /// Event that is raised when the synchronization status changes.
        /// </summary>
        event EventHandler<SyncStatusChangedEventArgs> SyncStatusChanged;
    }
}