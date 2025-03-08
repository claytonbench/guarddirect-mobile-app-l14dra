using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Core.Models;

namespace SecurityPatrol.Core.Interfaces
{
    /// <summary>
    /// Interface for the Patrol Service that provides business logic for patrol management operations
    /// including retrieving patrol locations, managing checkpoints, and processing checkpoint verifications.
    /// </summary>
    public interface IPatrolService
    {
        /// <summary>
        /// Retrieves all patrol locations available in the system
        /// </summary>
        /// <returns>A result containing a collection of patrol locations if successful</returns>
        Task<Result<IEnumerable<PatrolLocation>>> GetLocationsAsync();

        /// <summary>
        /// Retrieves a specific patrol location by its ID
        /// </summary>
        /// <param name="locationId">The ID of the patrol location to retrieve</param>
        /// <returns>A result containing the patrol location if found</returns>
        Task<Result<PatrolLocation>> GetLocationByIdAsync(int locationId);

        /// <summary>
        /// Retrieves all checkpoints for a specific patrol location
        /// </summary>
        /// <param name="locationId">The ID of the patrol location</param>
        /// <returns>A result containing a collection of checkpoint models if successful</returns>
        Task<Result<IEnumerable<CheckpointModel>>> GetCheckpointsByLocationIdAsync(int locationId);

        /// <summary>
        /// Retrieves a specific checkpoint by its ID
        /// </summary>
        /// <param name="checkpointId">The ID of the checkpoint to retrieve</param>
        /// <returns>A result containing the checkpoint model if found</returns>
        Task<Result<CheckpointModel>> GetCheckpointByIdAsync(int checkpointId);

        /// <summary>
        /// Processes a checkpoint verification request from a security officer
        /// </summary>
        /// <param name="request">The verification request containing checkpoint and location information</param>
        /// <param name="userId">The ID of the user verifying the checkpoint</param>
        /// <returns>A result containing the verification response if successful</returns>
        Task<Result<CheckpointVerificationResponse>> VerifyCheckpointAsync(CheckpointVerificationRequest request, string userId);

        /// <summary>
        /// Retrieves the current status of a patrol for a specific location and user
        /// </summary>
        /// <param name="locationId">The ID of the patrol location</param>
        /// <param name="userId">The ID of the user performing the patrol</param>
        /// <returns>A result containing the patrol status model if successful</returns>
        Task<Result<PatrolStatusModel>> GetPatrolStatusAsync(int locationId, string userId);

        /// <summary>
        /// Retrieves checkpoints that are within a specified distance of the user's current location
        /// </summary>
        /// <param name="latitude">The latitude of the user's current position</param>
        /// <param name="longitude">The longitude of the user's current position</param>
        /// <param name="radiusInMeters">The search radius in meters</param>
        /// <returns>A result containing a collection of nearby checkpoint models if successful</returns>
        Task<Result<IEnumerable<CheckpointModel>>> GetNearbyCheckpointsAsync(double latitude, double longitude, double radiusInMeters);

        /// <summary>
        /// Retrieves all checkpoint verifications for a specific user
        /// </summary>
        /// <param name="userId">The ID of the user</param>
        /// <returns>A result containing a collection of checkpoint verifications if successful</returns>
        Task<Result<IEnumerable<CheckpointVerification>>> GetUserVerificationsAsync(string userId);

        /// <summary>
        /// Retrieves checkpoint verifications for a specific user within a date range
        /// </summary>
        /// <param name="userId">The ID of the user</param>
        /// <param name="startDate">The start date of the range</param>
        /// <param name="endDate">The end date of the range</param>
        /// <returns>A result containing a collection of checkpoint verifications if successful</returns>
        Task<Result<IEnumerable<CheckpointVerification>>> GetUserVerificationsByDateRangeAsync(string userId, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Retrieves patrol locations that are within a specified distance of the user's current location
        /// </summary>
        /// <param name="latitude">The latitude of the user's current position</param>
        /// <param name="longitude">The longitude of the user's current position</param>
        /// <param name="radiusInMeters">The search radius in meters</param>
        /// <returns>A result containing a collection of nearby patrol locations if successful</returns>
        Task<Result<IEnumerable<PatrolLocation>>> GetNearbyLocationsAsync(double latitude, double longitude, double radiusInMeters);

        /// <summary>
        /// Checks if a specific checkpoint has been verified by a user
        /// </summary>
        /// <param name="checkpointId">The ID of the checkpoint</param>
        /// <param name="userId">The ID of the user</param>
        /// <returns>A result containing a boolean indicating if the checkpoint is verified</returns>
        Task<Result<bool>> IsCheckpointVerifiedAsync(int checkpointId, string userId);
    }
}