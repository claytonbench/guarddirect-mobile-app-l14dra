using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Core.Models;

namespace SecurityPatrol.Core.Interfaces
{
    /// <summary>
    /// Interface for the Checkpoint repository that provides data access operations for Checkpoint entities in the Security Patrol application.
    /// </summary>
    public interface ICheckpointRepository
    {
        /// <summary>
        /// Retrieves a checkpoint by its unique identifier
        /// </summary>
        /// <param name="id">The checkpoint ID to retrieve</param>
        /// <returns>The checkpoint with the specified ID, or null if not found</returns>
        Task<Checkpoint> GetByIdAsync(int id);

        /// <summary>
        /// Retrieves all checkpoints associated with a specific patrol location
        /// </summary>
        /// <param name="locationId">The ID of the patrol location</param>
        /// <returns>A collection of checkpoints for the specified location</returns>
        Task<IEnumerable<Checkpoint>> GetByLocationIdAsync(int locationId);

        /// <summary>
        /// Retrieves all checkpoints in the system
        /// </summary>
        /// <returns>A collection of all checkpoints</returns>
        Task<IEnumerable<Checkpoint>> GetAllAsync();

        /// <summary>
        /// Adds a new checkpoint to the system
        /// </summary>
        /// <param name="checkpoint">The checkpoint entity to add</param>
        /// <returns>A result containing the ID of the newly created checkpoint if successful</returns>
        Task<Result<int>> AddAsync(Checkpoint checkpoint);

        /// <summary>
        /// Updates an existing checkpoint in the system
        /// </summary>
        /// <param name="checkpoint">The checkpoint entity with updated values</param>
        /// <returns>A result indicating success or failure of the update operation</returns>
        Task<Result> UpdateAsync(Checkpoint checkpoint);

        /// <summary>
        /// Deletes a checkpoint from the system
        /// </summary>
        /// <param name="id">The ID of the checkpoint to delete</param>
        /// <returns>A result indicating success or failure of the delete operation</returns>
        Task<Result> DeleteAsync(int id);

        /// <summary>
        /// Retrieves checkpoints within a specified distance of a given location
        /// </summary>
        /// <param name="latitude">The latitude coordinate of the center point</param>
        /// <param name="longitude">The longitude coordinate of the center point</param>
        /// <param name="radiusInMeters">The radius in meters to search within</param>
        /// <returns>A collection of checkpoints within the specified radius</returns>
        Task<IEnumerable<Checkpoint>> GetNearbyCheckpointsAsync(double latitude, double longitude, double radiusInMeters);

        /// <summary>
        /// Checks if a checkpoint with the specified ID exists in the system
        /// </summary>
        /// <param name="id">The checkpoint ID to check</param>
        /// <returns>True if the checkpoint exists, false otherwise</returns>
        Task<bool> ExistsAsync(int id);
    }
}