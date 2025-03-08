using System; // Version 8.0+
using System.Collections.Generic; // Version 8.0+
using System.Threading.Tasks; // Version 8.0+
using SecurityPatrol.Models;

namespace SecurityPatrol.Services
{
    /// <summary>
    /// Interface that defines the contract for patrol services in the Security Patrol application.
    /// It provides methods for retrieving patrol locations, managing checkpoints,
    /// verifying checkpoint completion, and tracking patrol status.
    /// </summary>
    public interface IPatrolService
    {
        /// <summary>
        /// Gets a value indicating whether a patrol is currently active.
        /// </summary>
        bool IsPatrolActive { get; }

        /// <summary>
        /// Gets the ID of the current patrol location, or null if no patrol is active.
        /// </summary>
        int? CurrentLocationId { get; }

        /// <summary>
        /// Gets or sets the proximity threshold distance in feet (default is 50 feet).
        /// </summary>
        double ProximityThresholdFeet { get; set; }

        /// <summary>
        /// Event that is raised when the proximity status to a checkpoint changes.
        /// </summary>
        event EventHandler<CheckpointProximityEventArgs> CheckpointProximityChanged;

        /// <summary>
        /// Retrieves all available patrol locations.
        /// </summary>
        /// <returns>A task that returns a collection of patrol locations.</returns>
        Task<IEnumerable<LocationModel>> GetLocations();

        /// <summary>
        /// Retrieves all checkpoints for a specific patrol location.
        /// </summary>
        /// <param name="locationId">The location identifier.</param>
        /// <returns>A task that returns a collection of checkpoints for the specified location.</returns>
        Task<IEnumerable<CheckpointModel>> GetCheckpoints(int locationId);

        /// <summary>
        /// Verifies a checkpoint as completed, recording the verification with current location and timestamp.
        /// </summary>
        /// <param name="checkpointId">The checkpoint identifier.</param>
        /// <returns>A task that returns true if verification was successful, false otherwise.</returns>
        Task<bool> VerifyCheckpoint(int checkpointId);

        /// <summary>
        /// Retrieves the current status of a patrol for a specific location, including completion statistics.
        /// </summary>
        /// <param name="locationId">The location identifier.</param>
        /// <returns>A task that returns the patrol status for the specified location.</returns>
        Task<PatrolStatus> GetPatrolStatus(int locationId);

        /// <summary>
        /// Starts a new patrol for the specified location, initializing checkpoint monitoring and status tracking.
        /// </summary>
        /// <param name="locationId">The location identifier.</param>
        /// <returns>A task that returns the initial patrol status.</returns>
        Task<PatrolStatus> StartPatrol(int locationId);

        /// <summary>
        /// Ends the current patrol, finalizing status and stopping checkpoint monitoring.
        /// </summary>
        /// <returns>A task that returns the final patrol status.</returns>
        Task<PatrolStatus> EndPatrol();

        /// <summary>
        /// Manually checks proximity to all checkpoints based on the provided coordinates.
        /// </summary>
        /// <param name="latitude">The latitude coordinate.</param>
        /// <param name="longitude">The longitude coordinate.</param>
        /// <returns>A task that returns the IDs of checkpoints within proximity.</returns>
        Task<IEnumerable<int>> CheckProximity(double latitude, double longitude);
    }
}