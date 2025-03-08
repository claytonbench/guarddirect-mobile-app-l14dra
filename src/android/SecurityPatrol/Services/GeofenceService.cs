using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging; // Version 8.0+
using SecurityPatrol.Helpers;
using SecurityPatrol.Models;

namespace SecurityPatrol.Services
{
    /// <summary>
    /// Implementation of the IGeofenceService interface that monitors proximity to checkpoints during patrol operations.
    /// </summary>
    public class GeofenceService : IGeofenceService
    {
        /// <summary>
        /// Event that is raised when the proximity status to a checkpoint changes.
        /// </summary>
        public event EventHandler<CheckpointProximityEventArgs> ProximityChanged;

        /// <summary>
        /// Gets or sets the proximity threshold distance in feet (default is 50 feet).
        /// </summary>
        public double ProximityThresholdFeet { get; set; } = 50;

        private readonly List<CheckpointModel> _checkpoints = new List<CheckpointModel>();
        private readonly Dictionary<int, bool> _proximityStatus = new Dictionary<int, bool>();
        private readonly ILogger<GeofenceService> _logger;
        private bool _isMonitoring;

        /// <summary>
        /// Initializes a new instance of the <see cref="GeofenceService"/> class.
        /// </summary>
        /// <param name="logger">The logger instance for recording service operations.</param>
        public GeofenceService(ILogger<GeofenceService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Starts monitoring proximity to the specified checkpoints.
        /// </summary>
        /// <param name="checkpoints">The collection of checkpoints to monitor.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task StartMonitoring(IEnumerable<CheckpointModel> checkpoints)
        {
            _logger.LogInformation("Starting checkpoint monitoring with {Count} checkpoints", checkpoints?.Count() ?? 0);
            
            if (checkpoints == null)
            {
                throw new ArgumentNullException(nameof(checkpoints), "Checkpoints collection cannot be null");
            }

            _checkpoints.Clear();
            _checkpoints.AddRange(checkpoints);
            
            _proximityStatus.Clear();
            foreach (var checkpoint in _checkpoints)
            {
                _proximityStatus[checkpoint.Id] = false;
            }
            
            _isMonitoring = true;
            _logger.LogInformation("Checkpoint monitoring started");
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Stops monitoring checkpoint proximity.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task StopMonitoring()
        {
            _logger.LogInformation("Stopping checkpoint monitoring");
            
            _isMonitoring = false;
            _checkpoints.Clear();
            _proximityStatus.Clear();
            
            _logger.LogInformation("Checkpoint monitoring stopped");
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Checks proximity to all monitored checkpoints based on the provided coordinates
        /// and returns IDs of checkpoints within the proximity threshold.
        /// </summary>
        /// <param name="latitude">The latitude coordinate to check.</param>
        /// <param name="longitude">The longitude coordinate to check.</param>
        /// <returns>A task that returns the IDs of checkpoints within proximity.</returns>
        public Task<IEnumerable<int>> CheckProximity(double latitude, double longitude)
        {
            _logger.LogInformation("Checking proximity to checkpoints at coordinates: Lat={Latitude}, Lon={Longitude}", latitude, longitude);
            
            if (!_isMonitoring)
            {
                _logger.LogWarning("Proximity check requested but monitoring is not active");
                return Task.FromResult<IEnumerable<int>>(new List<int>());
            }
            
            var checkpointsInProximity = new List<int>();
            var thresholdMeters = LocationHelper.ConvertFeetToMeters(ProximityThresholdFeet);
            
            foreach (var checkpoint in _checkpoints)
            {
                var distance = checkpoint.CalculateDistance(latitude, longitude);
                var distanceFeet = LocationHelper.ConvertMetersToFeet(distance);
                var isInRange = distance <= thresholdMeters;
                
                // Check if proximity status has changed
                if (_proximityStatus.TryGetValue(checkpoint.Id, out bool wasInRange) && wasInRange != isInRange)
                {
                    _logger.LogInformation("Proximity status changed for checkpoint {CheckpointId}: {StatusChange}, Distance: {Distance:F1} feet",
                        checkpoint.Id, isInRange ? "Entered proximity" : "Left proximity", distanceFeet);
                    
                    OnProximityChanged(checkpoint.Id, distanceFeet, isInRange);
                }
                
                _proximityStatus[checkpoint.Id] = isInRange;
                
                if (isInRange)
                {
                    checkpointsInProximity.Add(checkpoint.Id);
                }
            }
            
            return Task.FromResult<IEnumerable<int>>(checkpointsInProximity);
        }
        
        /// <summary>
        /// Gets a value indicating whether checkpoint monitoring is currently active.
        /// </summary>
        /// <returns>True if monitoring is active, false otherwise.</returns>
        public bool IsMonitoring()
        {
            return _isMonitoring;
        }

        /// <summary>
        /// Raises the ProximityChanged event with the specified checkpoint ID, distance, and proximity status.
        /// </summary>
        /// <param name="checkpointId">The ID of the checkpoint.</param>
        /// <param name="distance">The distance to the checkpoint in feet.</param>
        /// <param name="isInRange">Whether the checkpoint is within the proximity threshold.</param>
        private void OnProximityChanged(int checkpointId, double distance, bool isInRange)
        {
            var handler = ProximityChanged;
            if (handler != null)
            {
                var args = new CheckpointProximityEventArgs(checkpointId, distance, isInRange);
                handler(this, args);
            }
            
            _logger.LogInformation("Proximity event raised for checkpoint {CheckpointId}: {Status}, Distance: {Distance:F1} feet",
                checkpointId, isInRange ? "In Range" : "Out of Range", distance);
        }
    }
}