using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Core.Models;

namespace SecurityPatrol.Core.Interfaces
{
    /// <summary>
    /// Interface for the PatrolLocation repository that provides data access operations 
    /// for PatrolLocation entities in the Security Patrol application.
    /// </summary>
    public interface IPatrolLocationRepository
    {
        /// <summary>
        /// Retrieves a patrol location by its unique identifier
        /// </summary>
        /// <param name="id">The ID of the patrol location to retrieve</param>
        /// <returns>The patrol location with the specified ID, or null if not found</returns>
        Task<PatrolLocation> GetByIdAsync(int id);

        /// <summary>
        /// Retrieves all patrol locations in the system
        /// </summary>
        /// <returns>A collection of all patrol locations</returns>
        Task<IEnumerable<PatrolLocation>> GetAllAsync();

        /// <summary>
        /// Retrieves a patrol location by ID including its associated checkpoints
        /// </summary>
        /// <param name="id">The ID of the patrol location to retrieve</param>
        /// <returns>The patrol location with checkpoints included, or null if not found</returns>
        Task<PatrolLocation> GetWithCheckpointsAsync(int id);

        /// <summary>
        /// Retrieves all patrol locations including their associated checkpoints
        /// </summary>
        /// <returns>A collection of all patrol locations with their checkpoints</returns>
        Task<IEnumerable<PatrolLocation>> GetAllWithCheckpointsAsync();

        /// <summary>
        /// Adds a new patrol location to the system
        /// </summary>
        /// <param name="location">The patrol location to add</param>
        /// <returns>A result containing the ID of the newly created patrol location if successful</returns>
        Task<Result<int>> AddAsync(PatrolLocation location);

        /// <summary>
        /// Updates an existing patrol location in the system
        /// </summary>
        /// <param name="location">The patrol location to update</param>
        /// <returns>A result indicating success or failure of the update operation</returns>
        Task<Result> UpdateAsync(PatrolLocation location);

        /// <summary>
        /// Deletes a patrol location from the system
        /// </summary>
        /// <param name="id">The ID of the patrol location to delete</param>
        /// <returns>A result indicating success or failure of the delete operation</returns>
        Task<Result> DeleteAsync(int id);

        /// <summary>
        /// Retrieves patrol locations within a specified distance of a given location
        /// </summary>
        /// <param name="latitude">The latitude coordinate of the center point</param>
        /// <param name="longitude">The longitude coordinate of the center point</param>
        /// <param name="radiusInMeters">The radius in meters within which to find patrol locations</param>
        /// <returns>A collection of patrol locations within the specified radius</returns>
        Task<IEnumerable<PatrolLocation>> GetNearbyLocationsAsync(double latitude, double longitude, double radiusInMeters);

        /// <summary>
        /// Checks if a patrol location with the specified ID exists in the system
        /// </summary>
        /// <param name="id">The ID of the patrol location to check</param>
        /// <returns>True if the patrol location exists, false otherwise</returns>
        Task<bool> ExistsAsync(int id);
    }
}