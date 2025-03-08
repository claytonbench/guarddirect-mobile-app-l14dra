using SecurityPatrol.Models;
using System.Threading;  // Version 8.0+
using System.Threading.Tasks;  // Version 8.0+

namespace SecurityPatrol.Services
{
    /// <summary>
    /// Interface that defines the contract for time tracking synchronization service.
    /// Responsible for synchronizing clock in/out events between the local database and backend API,
    /// ensuring that time records created offline are properly synchronized when connectivity is available.
    /// </summary>
    public interface ITimeTrackingSyncService
    {
        /// <summary>
        /// Synchronizes all pending time records with the backend API.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>True if all records were synchronized successfully, false otherwise.</returns>
        Task<bool> SyncTimeRecordsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Synchronizes a specific time record with the backend API.
        /// </summary>
        /// <param name="record">The time record to synchronize.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>True if the record was synchronized successfully, false otherwise.</returns>
        Task<bool> SyncRecordAsync(TimeRecordModel record, CancellationToken cancellationToken = default);

        /// <summary>
        /// Synchronizes a time record with the specified ID with the backend API.
        /// </summary>
        /// <param name="recordId">The ID of the time record to synchronize.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>True if the record was synchronized successfully, false otherwise.</returns>
        Task<bool> SyncRecordAsync(int recordId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the count of time records pending synchronization.
        /// </summary>
        /// <returns>The number of unsynchronized time records.</returns>
        Task<int> GetPendingSyncCountAsync();
    }
}