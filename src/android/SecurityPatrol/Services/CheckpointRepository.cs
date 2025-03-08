using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging; // Version 8.0+
using SQLite; // Version 1.8+
using SecurityPatrol.Database.Entities;
using SecurityPatrol.Database.Repositories;
using SecurityPatrol.Models;
using SecurityPatrol.Services;
using SecurityPatrol.Constants;

namespace SecurityPatrol.Services
{
    /// <summary>
    /// Implementation of the ICheckpointRepository interface that provides data access operations
    /// for checkpoint and checkpoint verification data in the Security Patrol application.
    /// </summary>
    public class CheckpointRepository : BaseRepository<CheckpointEntity, CheckpointModel>, ICheckpointRepository
    {
        /// <summary>
        /// Initializes a new instance of the CheckpointRepository class.
        /// </summary>
        /// <param name="databaseService">The database service for data access operations</param>
        /// <param name="logger">The logger for recording repository activities</param>
        public CheckpointRepository(IDatabaseService databaseService, ILogger<CheckpointRepository> logger) 
            : base(databaseService, logger)
        {
        }

        /// <summary>
        /// Retrieves all checkpoints from the database.
        /// </summary>
        /// <returns>A task that returns a collection of all checkpoints.</returns>
        public async Task<IEnumerable<CheckpointModel>> GetAllCheckpointsAsync()
        {
            return await GetAllAsync();
        }

        /// <summary>
        /// Retrieves a checkpoint by its ID.
        /// </summary>
        /// <param name="checkpointId">The ID of the checkpoint to retrieve.</param>
        /// <returns>A task that returns the checkpoint with the specified ID, or null if not found.</returns>
        public async Task<CheckpointModel> GetCheckpointByIdAsync(int checkpointId)
        {
            return await GetByIdAsync(checkpointId);
        }

        /// <summary>
        /// Retrieves all checkpoints for a specific patrol location.
        /// </summary>
        /// <param name="locationId">The ID of the patrol location.</param>
        /// <returns>A task that returns a collection of checkpoints for the specified location.</returns>
        public async Task<IEnumerable<CheckpointModel>> GetCheckpointsByLocationIdAsync(int locationId)
        {
            if (locationId <= 0)
            {
                throw new ArgumentException("Location ID must be greater than zero.", nameof(locationId));
            }

            return await GetByExpressionAsync(c => c.LocationId == locationId);
        }

        /// <summary>
        /// Saves a collection of checkpoints to the database, updating existing ones and inserting new ones.
        /// </summary>
        /// <param name="checkpoints">The collection of checkpoints to save.</param>
        /// <returns>A task that returns the number of checkpoints saved.</returns>
        public async Task<int> SaveCheckpointsAsync(IEnumerable<CheckpointModel> checkpoints)
        {
            if (checkpoints == null)
            {
                throw new ArgumentNullException(nameof(checkpoints));
            }

            int savedCount = 0;

            await ExecuteInTransactionAsync(async () =>
            {
                foreach (var checkpoint in checkpoints)
                {
                    var entity = ConvertToEntity(checkpoint);
                    
                    if (entity.Id > 0)
                    {
                        await UpdateAsync(entity);
                    }
                    else
                    {
                        await InsertAsync(entity);
                    }
                    
                    savedCount++;
                }
            });

            return savedCount;
        }

        /// <summary>
        /// Saves a single checkpoint to the database, updating if it exists or inserting if it's new.
        /// </summary>
        /// <param name="checkpoint">The checkpoint to save.</param>
        /// <returns>A task that returns the ID of the saved checkpoint.</returns>
        public async Task<int> SaveCheckpointAsync(CheckpointModel checkpoint)
        {
            if (checkpoint == null)
            {
                throw new ArgumentNullException(nameof(checkpoint));
            }

            var entity = ConvertToEntity(checkpoint);
            
            if (entity.Id > 0)
            {
                await UpdateAsync(entity);
                return entity.Id;
            }
            else
            {
                return await InsertAsync(entity);
            }
        }

        /// <summary>
        /// Deletes a checkpoint from the database by its ID.
        /// </summary>
        /// <param name="checkpointId">The ID of the checkpoint to delete.</param>
        /// <returns>A task that returns true if the checkpoint was deleted, false if it wasn't found.</returns>
        public async Task<bool> DeleteCheckpointAsync(int checkpointId)
        {
            var result = await DeleteAsync(checkpointId);
            return result > 0;
        }

        /// <summary>
        /// Deletes all checkpoints for a specific patrol location.
        /// </summary>
        /// <param name="locationId">The ID of the patrol location.</param>
        /// <returns>A task that returns the number of checkpoints deleted.</returns>
        public async Task<int> DeleteAllCheckpointsForLocationAsync(int locationId)
        {
            if (locationId <= 0)
            {
                throw new ArgumentException("Location ID must be greater than zero.", nameof(locationId));
            }

            var connection = await GetConnectionAsync();
            return await connection.ExecuteAsync(
                $"DELETE FROM {DatabaseConstants.TableCheckpoint} WHERE {DatabaseConstants.ColumnLocationId} = ?", 
                locationId);
        }

        /// <summary>
        /// Retrieves the verification status of all checkpoints for a specific patrol location.
        /// </summary>
        /// <param name="locationId">The ID of the patrol location.</param>
        /// <returns>A task that returns a collection of checkpoint statuses for the specified location.</returns>
        public async Task<IEnumerable<CheckpointStatus>> GetCheckpointStatusesAsync(int locationId)
        {
            if (locationId <= 0)
            {
                throw new ArgumentException("Location ID must be greater than zero.", nameof(locationId));
            }

            var connection = await GetConnectionAsync();
            
            // Get all checkpoints for the location
            var checkpoints = await GetCheckpointsByLocationIdAsync(locationId);
            
            // Create a list to store statuses
            var statuses = new List<CheckpointStatus>();
            
            foreach (var checkpoint in checkpoints)
            {
                // Create a status for each checkpoint
                var status = new CheckpointStatus
                {
                    CheckpointId = checkpoint.Id,
                    IsVerified = false,
                    VerificationTime = null
                };
                
                // Get the most recent verification for this checkpoint
                var verification = await connection.Table<CheckpointVerificationEntity>()
                    .Where(v => v.CheckpointId == checkpoint.Id)
                    .OrderByDescending(v => v.Timestamp)
                    .FirstOrDefaultAsync();
                
                if (verification != null)
                {
                    status.IsVerified = true;
                    status.VerificationTime = verification.Timestamp;
                    status.Latitude = verification.Latitude;
                    status.Longitude = verification.Longitude;
                }
                
                statuses.Add(status);
            }
            
            return statuses;
        }

        /// <summary>
        /// Saves the verification status of a checkpoint.
        /// </summary>
        /// <param name="status">The checkpoint status to save.</param>
        /// <returns>A task that returns true if the status was saved successfully.</returns>
        public async Task<bool> SaveCheckpointStatusAsync(CheckpointStatus status)
        {
            if (status == null || !status.IsVerified)
            {
                return false;
            }
            
            var connection = await GetConnectionAsync();
            
            // Create a verification entity
            var verification = new CheckpointVerificationEntity
            {
                CheckpointId = status.CheckpointId,
                Timestamp = status.VerificationTime ?? DateTime.UtcNow,
                Latitude = status.Latitude,
                Longitude = status.Longitude,
                UserId = "current-user-id", // This should be replaced with actual user ID from authentication service
                IsSynced = false
            };
            
            var id = await connection.InsertAsync(verification);
            return id > 0;
        }

        /// <summary>
        /// Saves the verification status of multiple checkpoints.
        /// </summary>
        /// <param name="statuses">The collection of checkpoint statuses to save.</param>
        /// <returns>A task that returns the number of statuses saved.</returns>
        public async Task<int> SaveCheckpointStatusesAsync(IEnumerable<CheckpointStatus> statuses)
        {
            if (statuses == null)
            {
                throw new ArgumentNullException(nameof(statuses));
            }
            
            int savedCount = 0;
            
            await ExecuteInTransactionAsync(async () =>
            {
                foreach (var status in statuses)
                {
                    if (status.IsVerified)
                    {
                        var success = await SaveCheckpointStatusAsync(status);
                        if (success)
                        {
                            savedCount++;
                        }
                    }
                }
            });
            
            return savedCount;
        }

        /// <summary>
        /// Clears all checkpoint verification statuses for a specific patrol location.
        /// </summary>
        /// <param name="locationId">The ID of the patrol location.</param>
        /// <returns>A task that returns the number of statuses cleared.</returns>
        public async Task<int> ClearCheckpointStatusesAsync(int locationId)
        {
            if (locationId <= 0)
            {
                throw new ArgumentException("Location ID must be greater than zero.", nameof(locationId));
            }
            
            var connection = await GetConnectionAsync();
            
            // Get all checkpoint IDs for the location
            var checkpoints = await connection.Table<CheckpointEntity>()
                .Where(c => c.LocationId == locationId)
                .ToListAsync();
            
            if (checkpoints.Count == 0)
            {
                return 0;
            }
            
            int deletedCount = 0;
            
            // Delete verifications for each checkpoint
            await ExecuteInTransactionAsync(async () =>
            {
                foreach (var checkpoint in checkpoints)
                {
                    var result = await connection.ExecuteAsync(
                        $"DELETE FROM {DatabaseConstants.TableCheckpointVerification} WHERE {DatabaseConstants.ColumnCheckpointId} = ?", 
                        checkpoint.Id);
                        
                    deletedCount += result;
                }
            });
            
            return deletedCount;
        }

        /// <summary>
        /// Converts a CheckpointEntity to a CheckpointModel.
        /// </summary>
        /// <param name="entity">The entity to convert.</param>
        /// <returns>The entity converted to a model.</returns>
        protected override CheckpointModel ConvertToModel(CheckpointEntity entity)
        {
            if (entity == null)
            {
                return null;
            }
            
            return CheckpointModel.FromEntity(entity);
        }

        /// <summary>
        /// Converts a CheckpointModel to a CheckpointEntity.
        /// </summary>
        /// <param name="model">The model to convert.</param>
        /// <returns>The model converted to an entity.</returns>
        protected override CheckpointEntity ConvertToEntity(CheckpointModel model)
        {
            if (model == null)
            {
                return null;
            }
            
            return model.ToEntity();
        }
    }
}