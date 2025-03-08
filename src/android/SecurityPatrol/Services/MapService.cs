using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Maps;
using SecurityPatrol.Models;
using SecurityPatrol.Views.Controls;

namespace SecurityPatrol.Services
{
    /// <summary>
    /// Implementation of the IMapService interface that provides map functionality for patrol operations.
    /// </summary>
    public class MapService : IMapService
    {
        private readonly ILogger<MapService> _logger;
        private LocationMapView _mapView;
        private bool _isInitialized;
        private Location _userPosition;

        /// <summary>
        /// Initializes a new instance of the MapService class with the specified logger.
        /// </summary>
        /// <param name="logger">The logger for recording operations and errors.</param>
        public MapService(ILogger<MapService> logger)
        {
            _logger = logger;
            _isInitialized = false;
        }

        /// <summary>
        /// Initializes the map control with default settings.
        /// </summary>
        /// <param name="mapView">The map view object to initialize.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task InitializeMap(object mapView)
        {
            _logger.LogInformation("Initializing map control");
            
            if (mapView == null)
            {
                throw new ArgumentNullException(nameof(mapView), "Map view cannot be null");
            }
            
            if (!(mapView is LocationMapView))
            {
                throw new ArgumentException("Map view must be of type LocationMapView", nameof(mapView));
            }
            
            _mapView = (LocationMapView)mapView;
            _isInitialized = true;
            
            _logger.LogInformation("Map initialization successful");
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Enables or disables the display of the user's current location on the map.
        /// </summary>
        /// <param name="enabled">Whether to show the user's location.</param>
        public void ShowUserLocation(bool enabled)
        {
            _logger.LogInformation($"Setting user location display: {enabled}");
            
            CheckIfInitialized();
            
            _mapView.IsShowingUser = enabled;
            
            _logger.LogInformation($"User location display set to: {enabled}");
        }

        /// <summary>
        /// Displays the provided checkpoints on the map.
        /// </summary>
        /// <param name="checkpoints">Collection of checkpoint models to display.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task DisplayCheckpoints(IEnumerable<CheckpointModel> checkpoints)
        {
            _logger.LogInformation("Displaying checkpoints on map");
            
            CheckIfInitialized();
            
            _mapView.Checkpoints = checkpoints;
            
            _logger.LogInformation("Checkpoints displayed successfully");
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Centers the map on the specified coordinates with the given radius.
        /// </summary>
        /// <param name="latitude">The latitude to center on.</param>
        /// <param name="longitude">The longitude to center on.</param>
        /// <param name="radius">The visible radius in kilometers.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task CenterMap(double latitude, double longitude, double radius)
        {
            _logger.LogInformation($"Centering map on coordinates: {latitude}, {longitude} with radius: {radius}km");
            
            CheckIfInitialized();
            
            _mapView.CenterMapOnLocation(latitude, longitude, radius);
            
            _logger.LogInformation("Map centered successfully");
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Highlights or un-highlights a specific checkpoint on the map.
        /// </summary>
        /// <param name="checkpointId">The ID of the checkpoint to highlight.</param>
        /// <param name="highlight">Whether to highlight or un-highlight the checkpoint.</param>
        public void HighlightCheckpoint(int checkpointId, bool highlight)
        {
            _logger.LogInformation($"Highlighting checkpoint {checkpointId}: {highlight}");
            
            CheckIfInitialized();
            
            _mapView.HighlightCheckpoint(checkpointId, highlight);
            
            _logger.LogInformation($"Checkpoint {checkpointId} highlight set to: {highlight}");
        }

        /// <summary>
        /// Updates the user's location marker on the map.
        /// </summary>
        /// <param name="latitude">The new latitude.</param>
        /// <param name="longitude">The new longitude.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task UpdateUserLocation(double latitude, double longitude)
        {
            _logger.LogInformation($"Updating user location: {latitude}, {longitude}");
            
            CheckIfInitialized();
            
            _userPosition = new Location(latitude, longitude);
            
            if (_mapView.IsShowingUser)
            {
                _mapView.CenterMapOnLocation(latitude, longitude, 0.05); // 50 meters approx.
            }
            
            _logger.LogInformation("User location updated successfully");
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Updates the visual status of a checkpoint (e.g., to show it as verified).
        /// </summary>
        /// <param name="checkpointId">The ID of the checkpoint to update.</param>
        /// <param name="isVerified">Whether the checkpoint is verified.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task UpdateCheckpointStatus(int checkpointId, bool isVerified)
        {
            _logger.LogInformation($"Updating checkpoint {checkpointId} status to verified: {isVerified}");
            
            CheckIfInitialized();
            
            _mapView.UpdateCheckpointStatus(checkpointId, isVerified);
            
            _logger.LogInformation($"Checkpoint {checkpointId} status updated to verified: {isVerified}");
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Removes all checkpoints from the map.
        /// </summary>
        public void ClearCheckpoints()
        {
            _logger.LogInformation("Clearing all checkpoints from map");
            
            CheckIfInitialized();
            
            _mapView.Checkpoints = null;
            
            _logger.LogInformation("Checkpoints cleared successfully");
        }
        
        /// <summary>
        /// Checks if the map is initialized and throws an exception if not.
        /// </summary>
        private void CheckIfInitialized()
        {
            if (!_isInitialized || _mapView == null)
            {
                throw new InvalidOperationException("Map has not been initialized. Call InitializeMap first.");
            }
        }
    }
}