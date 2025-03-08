using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Core.Models;

namespace SecurityPatrol.Core.Interfaces
{
    /// <summary>
    /// Interface for the CheckpointVerification repository that provides data access operations 
    /// for CheckpointVerification entities in the Security Patrol application.
    /// </summary>
    public interface ICheckpointVerificationRepository
    {
        /// <summary>
        /// Retrieves a checkpoint verification record by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the checkpoint verification record.</param>
        /// <returns>The checkpoint verification record with the specified ID, or null if not found.</returns>
        Task<CheckpointVerification> GetByIdAsync(int id);

        /// <summary>
        /// Retrieves all checkpoint verification records associated with a specific checkpoint.
        /// </summary>
        /// <param name="checkpointId">The unique identifier of the checkpoint.</param>
        /// <returns>A collection of checkpoint verification records for the specified checkpoint.</returns>
        Task<IEnumerable<CheckpointVerification>> GetByCheckpointIdAsync(int checkpointId);

        /// <summary>
        /// Retrieves all checkpoint verification records associated with a specific user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>A collection of checkpoint verification records for the specified user.</returns>
        Task<IEnumerable<CheckpointVerification>> GetByUserIdAsync(string userId);

        /// <summary>
        /// Retrieves all checkpoint verification records in the system.
        /// </summary>
        /// <returns>A collection of all checkpoint verification records.</returns>
        Task<IEnumerable<CheckpointVerification>> GetAllAsync();

        /// <summary>
        /// Adds a new checkpoint verification record to the system.
        /// </summary>
        /// <param name="verification">The checkpoint verification record to add.</param>
        /// <returns>A result containing the ID of the newly created checkpoint verification record if successful.</returns>
        Task<Result<int>> AddAsync(CheckpointVerification verification);

        /// <summary>
        /// Updates an existing checkpoint verification record in the system.
        /// </summary>
        /// <param name="verification">The checkpoint verification record to update.</param>
        /// <returns>A result indicating success or failure of the update operation.</returns>
        Task<Result> UpdateAsync(CheckpointVerification verification);

        /// <summary>
        /// Deletes a checkpoint verification record from the system.
        /// </summary>
        /// <param name="id">The unique identifier of the checkpoint verification record to delete.</param>
        /// <returns>A result indicating success or failure of the delete operation.</returns>
        Task<Result> DeleteAsync(int id);

        /// <summary>
        /// Retrieves checkpoint verification records for a specific user within a date range.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="startDate">The start date of the range.</param>
        /// <param name="endDate">The end date of the range.</param>
        /// <returns>A collection of checkpoint verification records for the specified user and date range.</returns>
        Task<IEnumerable<CheckpointVerification>> GetByUserIdAndDateRangeAsync(string userId, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Retrieves checkpoint verification records that have not been synchronized.
        /// </summary>
        /// <returns>A collection of checkpoint verification records that need synchronization.</returns>
        Task<IEnumerable<CheckpointVerification>> GetPendingSyncAsync();

        /// <summary>
        /// Updates the synchronization status of checkpoint verification records.
        /// </summary>
        /// <param name="ids">The collection of record IDs to update.</param>
        /// <param name="isSynced">The synchronization status to set.</param>
        /// <returns>A result indicating success or failure of the update operation.</returns>
        Task<Result> UpdateSyncStatusAsync(IEnumerable<int> ids, bool isSynced);

        /// <summary>
        /// Checks if a checkpoint verification record with the specified ID exists in the system.
        /// </summary>
        /// <param name="id">The unique identifier to check.</param>
        /// <returns>True if the checkpoint verification record exists, false otherwise.</returns>
        Task<bool> ExistsAsync(int id);
    }
}