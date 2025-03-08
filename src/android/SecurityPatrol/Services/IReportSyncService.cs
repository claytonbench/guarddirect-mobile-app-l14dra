using System;
using System.Threading.Tasks;
using SecurityPatrol.Models;

namespace SecurityPatrol.Services
{
    /// <summary>
    /// Interface that defines the contract for the Report Synchronization Service, which is responsible 
    /// for synchronizing activity reports between the local database and the backend API.
    /// This service handles the transmission of reports, tracking of synchronization status, 
    /// and implements retry logic for failed synchronization attempts.
    /// </summary>
    public interface IReportSyncService
    {
        /// <summary>
        /// Event that is raised when the synchronization status changes.
        /// </summary>
        event EventHandler<SyncStatusChangedEventArgs> SyncStatusChanged;

        /// <summary>
        /// Synchronizes all unsynchronized reports with the backend API.
        /// </summary>
        /// <returns>The number of successfully synchronized reports.</returns>
        Task<int> SyncReportsAsync();

        /// <summary>
        /// Synchronizes a specific report with the backend API.
        /// </summary>
        /// <param name="id">The ID of the report to synchronize.</param>
        /// <returns>True if the report was synchronized successfully, false otherwise.</returns>
        Task<bool> SyncReportAsync(int id);

        /// <summary>
        /// Gets the count of reports that are pending synchronization.
        /// </summary>
        /// <returns>The number of reports pending synchronization.</returns>
        Task<int> GetPendingSyncCountAsync();

        /// <summary>
        /// Retries synchronizing reports that previously failed to synchronize.
        /// </summary>
        /// <returns>The number of successfully synchronized reports after retry.</returns>
        Task<int> RetryFailedSyncsAsync();

        /// <summary>
        /// Checks if any report synchronization is currently in progress.
        /// </summary>
        /// <returns>True if any synchronization is in progress, false otherwise.</returns>
        Task<bool> IsSyncInProgressAsync();

        /// <summary>
        /// Synchronizes the deletion of a report with the backend API.
        /// </summary>
        /// <param name="id">The local ID of the report to delete.</param>
        /// <param name="remoteId">The remote ID of the report to delete from the server.</param>
        /// <returns>True if the deletion was synchronized successfully, false otherwise.</returns>
        Task<bool> SyncDeletedReportAsync(int id, string remoteId);
    }
}