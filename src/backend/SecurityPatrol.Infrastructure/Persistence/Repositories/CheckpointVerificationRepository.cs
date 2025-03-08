using Microsoft.EntityFrameworkCore; // v8.0.0
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SecurityPatrol.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Implementation of the ICheckpointVerificationRepository interface that provides data access operations 
    /// for CheckpointVerification entities using Entity Framework Core.
    /// </summary>
    public class CheckpointVerificationRepository : ICheckpointVerificationRepository
    {
        private readonly SecurityPatrolDbContext _context;

        /// <summary>
        /// Initializes a new instance of the CheckpointVerificationRepository class with the specified database context.
        /// </summary>
        /// <param name="context">The database context for accessing checkpoint verification data.</param>
        /// <exception cref="ArgumentNullException">Thrown if the context parameter is null.</exception>
        public CheckpointVerificationRepository(SecurityPatrolDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Retrieves a checkpoint verification record by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the checkpoint verification record.</param>
        /// <returns>The checkpoint verification record with the specified ID, or null if not found.</returns>
        public async Task<CheckpointVerification> GetByIdAsync(int id)
        {
            return await _context.CheckpointVerifications.FindAsync(id);
        }

        /// <summary>
        /// Retrieves all checkpoint verification records associated with a specific checkpoint.
        /// </summary>
        /// <param name="checkpointId">The unique identifier of the checkpoint.</param>
        /// <returns>A collection of checkpoint verification records for the specified checkpoint.</returns>
        public async Task<IEnumerable<CheckpointVerification>> GetByCheckpointIdAsync(int checkpointId)
        {
            return await _context.CheckpointVerifications
                .Where(v => v.CheckpointId == checkpointId)
                .ToListAsync();
        }

        /// <summary>
        /// Retrieves all checkpoint verification records associated with a specific user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>A collection of checkpoint verification records for the specified user.</returns>
        public async Task<IEnumerable<CheckpointVerification>> GetByUserIdAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return Enumerable.Empty<CheckpointVerification>();
            }

            return await _context.CheckpointVerifications
                .Where(v => v.UserId == userId)
                .ToListAsync();
        }

        /// <summary>
        /// Retrieves all checkpoint verification records in the system.
        /// </summary>
        /// <returns>A collection of all checkpoint verification records.</returns>
        public async Task<IEnumerable<CheckpointVerification>> GetAllAsync()
        {
            return await _context.CheckpointVerifications.ToListAsync();
        }

        /// <summary>
        /// Adds a new checkpoint verification record to the system.
        /// </summary>
        /// <param name="verification">The checkpoint verification record to add.</param>
        /// <returns>A result containing the ID of the newly created checkpoint verification record if successful.</returns>
        public async Task<Result<int>> AddAsync(CheckpointVerification verification)
        {
            try
            {
                if (verification == null)
                {
                    return Result.Failure<int>("Verification cannot be null");
                }

                if (string.IsNullOrEmpty(verification.UserId))
                {
                    return Result.Failure<int>("User ID is required");
                }

                if (verification.CheckpointId <= 0)
                {
                    return Result.Failure<int>("Valid Checkpoint ID is required");
                }

                // Ensure the timestamp is set if not already
                if (verification.Timestamp == default)
                {
                    verification.Timestamp = DateTime.UtcNow;
                }

                // Set default sync status if not explicitly set
                verification.IsSynced = verification.IsSynced; // Preserve value

                await _context.CheckpointVerifications.AddAsync(verification);
                await _context.SaveChangesAsync();

                return Result.Success(verification.Id, "Checkpoint verification created successfully");
            }
            catch (Exception ex)
            {
                return Result.Failure<int>($"Failed to add checkpoint verification: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates an existing checkpoint verification record in the system.
        /// </summary>
        /// <param name="verification">The checkpoint verification record to update.</param>
        /// <returns>A result indicating success or failure of the update operation.</returns>
        public async Task<Result> UpdateAsync(CheckpointVerification verification)
        {
            try
            {
                if (verification == null)
                {
                    return Result.Failure("Verification cannot be null");
                }

                if (verification.Id <= 0)
                {
                    return Result.Failure("Invalid verification ID");
                }

                bool exists = await ExistsAsync(verification.Id);
                if (!exists)
                {
                    return Result.Failure($"Checkpoint verification with ID {verification.Id} not found");
                }

                _context.CheckpointVerifications.Update(verification);
                await _context.SaveChangesAsync();

                return Result.Success("Checkpoint verification updated successfully");
            }
            catch (Exception ex)
            {
                return Result.Failure($"Failed to update checkpoint verification: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes a checkpoint verification record from the system.
        /// </summary>
        /// <param name="id">The unique identifier of the checkpoint verification record to delete.</param>
        /// <returns>A result indicating success or failure of the delete operation.</returns>
        public async Task<Result> DeleteAsync(int id)
        {
            try
            {
                var verification = await GetByIdAsync(id);
                if (verification == null)
                {
                    return Result.Failure($"Checkpoint verification with ID {id} not found");
                }

                _context.CheckpointVerifications.Remove(verification);
                await _context.SaveChangesAsync();

                return Result.Success("Checkpoint verification deleted successfully");
            }
            catch (Exception ex)
            {
                return Result.Failure($"Failed to delete checkpoint verification: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves checkpoint verification records for a specific user within a date range.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="startDate">The start date of the range.</param>
        /// <param name="endDate">The end date of the range.</param>
        /// <returns>A collection of checkpoint verification records for the specified user and date range.</returns>
        public async Task<IEnumerable<CheckpointVerification>> GetByUserIdAndDateRangeAsync(string userId, DateTime startDate, DateTime endDate)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return Enumerable.Empty<CheckpointVerification>();
            }

            // Ensure the end date includes the full day
            var adjustedEndDate = endDate.Date.AddDays(1).AddTicks(-1);

            return await _context.CheckpointVerifications
                .Where(v => v.UserId == userId && 
                            v.Timestamp >= startDate && 
                            v.Timestamp <= adjustedEndDate)
                .OrderBy(v => v.Timestamp)
                .ToListAsync();
        }

        /// <summary>
        /// Retrieves checkpoint verification records that have not been synchronized.
        /// </summary>
        /// <returns>A collection of checkpoint verification records that need synchronization.</returns>
        public async Task<IEnumerable<CheckpointVerification>> GetPendingSyncAsync()
        {
            return await _context.CheckpointVerifications
                .Where(v => !v.IsSynced)
                .ToListAsync();
        }

        /// <summary>
        /// Updates the synchronization status of checkpoint verification records.
        /// </summary>
        /// <param name="ids">The collection of record IDs to update.</param>
        /// <param name="isSynced">The synchronization status to set.</param>
        /// <returns>A result indicating success or failure of the update operation.</returns>
        public async Task<Result> UpdateSyncStatusAsync(IEnumerable<int> ids, bool isSynced)
        {
            try
            {
                if (ids == null || !ids.Any())
                {
                    return Result.Failure("No verification IDs provided");
                }

                var verifications = await _context.CheckpointVerifications
                    .Where(v => ids.Contains(v.Id))
                    .ToListAsync();

                if (!verifications.Any())
                {
                    return Result.Failure("No matching checkpoint verifications found");
                }

                foreach (var verification in verifications)
                {
                    verification.IsSynced = isSynced;
                }

                await _context.SaveChangesAsync();

                return Result.Success($"Synchronization status updated for {verifications.Count} checkpoint verifications");
            }
            catch (Exception ex)
            {
                return Result.Failure($"Failed to update synchronization status: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if a checkpoint verification record with the specified ID exists in the system.
        /// </summary>
        /// <param name="id">The unique identifier to check.</param>
        /// <returns>True if the checkpoint verification record exists, false otherwise.</returns>
        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.CheckpointVerifications
                .AnyAsync(v => v.Id == id);
        }
    }
}