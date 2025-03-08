using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SecurityPatrol.Models;

namespace SecurityPatrol.Services
{
    /// <summary>
    /// Provides geofencing functionality to monitor user proximity to checkpoints during patrol operations.
    /// This interface is responsible for starting and stopping checkpoint monitoring, checking proximity
    /// to checkpoints, and raising events when proximity status changes.
    /// </summary>
    public interface IGeofenceService
    {
        /// <summary>
        /// Event that is raised when the proximity status to a checkpoint changes.
        /// </summary>
        event EventHandler<CheckpointProximityEventArgs> ProximityChanged;

        /// <summary>
        /// Gets or sets the proximity threshold distance in feet (default is 50 feet).
        /// </summary>
        double ProximityThresholdFeet { get; set; }

        /// <summary>
        /// Starts monitoring proximity to the specified checkpoints.
        /// </summary>
        /// <param name="checkpoints">The collection of checkpoints to monitor.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task StartMonitoring(IEnumerable<CheckpointModel> checkpoints);

        /// <summary>
        /// Stops monitoring checkpoint proximity.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task StopMonitoring();

        /// <summary>
        /// Checks proximity to all monitored checkpoints based on the provided coordinates
        /// and returns IDs of checkpoints within the proximity threshold.
        /// </summary>
        /// <param name="latitude">The latitude coordinate to check.</param>
        /// <param name="longitude">The longitude coordinate to check.</param>
        /// <returns>A task that returns the IDs of checkpoints within proximity.</returns>
        Task<IEnumerable<int>> CheckProximity(double latitude, double longitude);
    }
}