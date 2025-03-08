using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SecurityPatrol.Models;

namespace SecurityPatrol.Services
{
    /// <summary>
    /// Interface that defines the contract for map services in the Security Patrol application.
    /// It provides methods for initializing maps, displaying user location, visualizing checkpoints, 
    /// and updating checkpoint status during patrol operations.
    /// </summary>
    public interface IMapService
    {
        /// <summary>
        /// Initializes the map control with default settings
        /// </summary>
        /// <param name="mapView">The map view object to initialize</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task InitializeMap(object mapView);

        /// <summary>
        /// Enables or disables the display of the user's current location on the map
        /// </summary>
        /// <param name="enabled">Whether to show the user's location</param>
        void ShowUserLocation(bool enabled);

        /// <summary>
        /// Displays the provided checkpoints on the map
        /// </summary>
        /// <param name="checkpoints">Collection of checkpoint models to display</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task DisplayCheckpoints(IEnumerable<CheckpointModel> checkpoints);

        /// <summary>
        /// Centers the map on the specified coordinates with the given radius
        /// </summary>
        /// <param name="latitude">The latitude to center on</param>
        /// <param name="longitude">The longitude to center on</param>
        /// <param name="radius">The visible radius in meters</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task CenterMap(double latitude, double longitude, double radius);

        /// <summary>
        /// Highlights or un-highlights a specific checkpoint on the map
        /// </summary>
        /// <param name="checkpointId">The ID of the checkpoint to highlight</param>
        /// <param name="highlight">Whether to highlight or un-highlight the checkpoint</param>
        void HighlightCheckpoint(int checkpointId, bool highlight);

        /// <summary>
        /// Updates the user's location marker on the map
        /// </summary>
        /// <param name="latitude">The new latitude</param>
        /// <param name="longitude">The new longitude</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task UpdateUserLocation(double latitude, double longitude);

        /// <summary>
        /// Updates the visual status of a checkpoint (e.g., to show it as verified)
        /// </summary>
        /// <param name="checkpointId">The ID of the checkpoint to update</param>
        /// <param name="isVerified">Whether the checkpoint is verified</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task UpdateCheckpointStatus(int checkpointId, bool isVerified);

        /// <summary>
        /// Removes all checkpoints from the map
        /// </summary>
        void ClearCheckpoints();
    }
}