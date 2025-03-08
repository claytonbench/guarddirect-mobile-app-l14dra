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
    /// Implementation of the IPatrolService interface that provides patrol management functionality,
    /// including checkpoint proximity detection, verification, and patrol status tracking.
    /// </summary>
    public class PatrolService : IPatrolService, IDisposable
    {
        private readonly ICheckpointRepository _checkpointRepository;
        private readonly ILocationService _locationService;
        private readonly IGeofenceService _geofenceService;
        private readonly IMapService _mapService;
        private readonly ILogger<PatrolService> _logger;
        
        private List<CheckpointModel> _activeCheckpoints;
        private PatrolStatus _currentPatrolStatus;
        private Dictionary<int, bool> _checkpointProximityStatus;

        /// <summary>
        /// Gets a value indicating whether a patrol is currently active.
        /// </summary>
        public bool IsPatrolActive { get; private set; }

        /// <summary>
        /// Gets the ID of the current patrol location, or null if no patrol is active.
        /// </summary>
        public int? CurrentLocationId { get; private set; }

        /// <summary>
        /// Gets or sets the proximity threshold distance in feet (default is 50 feet).
        /// </summary>
        public double ProximityThresholdFeet { get; set; } = 50.0;

        /// <summary>
        /// Event that is raised when the proximity status to a checkpoint changes.
        /// </summary>
        public event EventHandler<CheckpointProximityEventArgs> CheckpointProximityChanged;

        /// <summary>
        /// Initializes a new instance of the PatrolService class with required dependencies.
        /// </summary>
        /// <param name="checkpointRepository">Repository for checkpoint data access.</param>
        /// <param name="locationService">Service for location tracking.</param>
        /// <param name="geofenceService">Service for geofencing operations.</param>
        /// <param name="mapService">Service for map display and updates.</param>
        /// <param name="logger">Logger for diagnostic information.</param>
        public PatrolService(
            ICheckpointRepository checkpointRepository,
            ILocationService locationService,
            IGeofenceService geofenceService,
            IMapService mapService,
            ILogger<PatrolService> logger)
        {
            _checkpointRepository = checkpointRepository ?? throw new ArgumentNullException(nameof(checkpointRepository));
            _locationService = locationService ?? throw new ArgumentNullException(nameof(locationService));
            _geofenceService = geofenceService ?? throw new ArgumentNullException(nameof(geofenceService));
            _mapService = mapService ?? throw new ArgumentNullException(nameof(mapService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _activeCheckpoints = new List<CheckpointModel>();
            _checkpointProximityStatus = new Dictionary<int, bool>();
            
            IsPatrolActive = false;
            CurrentLocationId = null;
            
            // Subscribe to events
            _geofenceService.ProximityChanged += OnGeofenceProximityChanged;
            _locationService.LocationChanged += OnLocationChanged;
        }

        /// <summary>
        /// Retrieves all available patrol locations.
        /// </summary>
        /// <returns>A task that returns a collection of patrol locations.</returns>
        public async Task<IEnumerable<LocationModel>> GetLocations()
        {
            _logger.LogInformation("Getting all patrol locations");
            try
            {
                // This method implementation depends on how locations are stored and retrieved in the application.
                // We would typically fetch this data from a database or API via the repository.
                
                // For the purposes of this implementation, we'll assume that we can retrieve all distinct
                // locations by extracting them from the checkpoints in the repository
                var allCheckpoints = await _checkpointRepository.GetAllCheckpointsAsync();
                var locationIds = allCheckpoints.Select(c => c.LocationId).Distinct();
                
                // Convert to location models - in a real implementation, we would fetch the actual location data
                // This is a simplified approach
                var locations = new List<LocationModel>();
                
                foreach (var locationId in locationIds)
                {
                    // Get the first checkpoint for this location to extract location coordinates
                    var checkpoint = allCheckpoints.FirstOrDefault(c => c.LocationId == locationId);
                    if (checkpoint != null)
                    {
                        locations.Add(new LocationModel
                        {
                            Id = locationId,
                            Name = $"Location {locationId}", // Placeholder name
                            Latitude = checkpoint.Latitude,  // Using first checkpoint's coordinates as location center
                            Longitude = checkpoint.Longitude
                        });
                    }
                }
                
                _logger.LogInformation("Retrieved {Count} patrol locations", locations.Count);
                return locations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting patrol locations");
                throw;
            }
        }

        /// <summary>
        /// Retrieves all checkpoints for a specific patrol location.
        /// </summary>
        /// <param name="locationId">The location identifier.</param>
        /// <returns>A task that returns a collection of checkpoints for the specified location.</returns>
        public async Task<IEnumerable<CheckpointModel>> GetCheckpoints(int locationId)
        {
            _logger.LogInformation("Getting checkpoints for location {LocationId}", locationId);
            
            if (locationId <= 0)
                throw new ArgumentException("Location ID must be greater than zero", nameof(locationId));
            
            try
            {
                var checkpoints = await _checkpointRepository.GetCheckpointsByLocationIdAsync(locationId);
                _logger.LogInformation("Retrieved {Count} checkpoints for location {LocationId}", 
                    checkpoints?.Count() ?? 0, locationId);
                
                return checkpoints;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting checkpoints for location {LocationId}", locationId);
                throw;
            }
        }

        /// <summary>
        /// Verifies a checkpoint as completed, recording the verification with current location and timestamp.
        /// </summary>
        /// <param name="checkpointId">The checkpoint identifier.</param>
        /// <returns>A task that returns true if verification was successful, false otherwise.</returns>
        public async Task<bool> VerifyCheckpoint(int checkpointId)
        {
            _logger.LogInformation("Verifying checkpoint {CheckpointId}", checkpointId);
            
            if (checkpointId <= 0)
                throw new ArgumentException("Checkpoint ID must be greater than zero", nameof(checkpointId));
            
            if (!IsPatrolActive)
            {
                _logger.LogWarning("Cannot verify checkpoint: no active patrol");
                return false;
            }
            
            try
            {
                // Find the checkpoint in the active checkpoints list
                var checkpoint = _activeCheckpoints.FirstOrDefault(c => c.Id == checkpointId);
                
                if (checkpoint == null)
                {
                    _logger.LogError("Checkpoint {CheckpointId} not found in active checkpoints", checkpointId);
                    return false;
                }
                
                // Check if checkpoint is already verified
                if (checkpoint.IsVerified)
                {
                    _logger.LogInformation("Checkpoint {CheckpointId} is already verified", checkpointId);
                    return true;
                }
                
                // Get current location
                var currentLocation = await _locationService.GetCurrentLocation();
                
                // Ensure user is within proximity threshold
                double distanceMeters = checkpoint.CalculateDistance(currentLocation.Latitude, currentLocation.Longitude);
                double distanceFeet = LocationHelper.ConvertMetersToFeet(distanceMeters);
                
                if (distanceFeet > ProximityThresholdFeet)
                {
                    _logger.LogWarning(
                        "Cannot verify checkpoint {CheckpointId}: user is not within proximity threshold " +
                        "({Distance:F1} feet > {Threshold:F1} feet)",
                        checkpointId, distanceFeet, ProximityThresholdFeet);
                    return false;
                }
                
                // Mark checkpoint as verified
                checkpoint.MarkAsVerified();
                
                // Create and save checkpoint status
                var checkpointStatus = new CheckpointStatus
                {
                    CheckpointId = checkpointId,
                    IsVerified = true,
                    VerificationTime = DateTime.UtcNow,
                    Latitude = currentLocation.Latitude,
                    Longitude = currentLocation.Longitude
                };
                
                await _checkpointRepository.SaveCheckpointStatusAsync(checkpointStatus);
                
                // Update checkpoint status on map
                await _mapService.UpdateCheckpointStatus(checkpointId, true);
                
                // Update patrol status
                int verifiedCount = _activeCheckpoints.Count(c => c.IsVerified);
                _currentPatrolStatus.UpdateProgress(verifiedCount);
                
                // Check if all checkpoints are verified to complete the patrol
                if (_currentPatrolStatus.IsComplete())
                {
                    _currentPatrolStatus.CompletePatrol();
                    _logger.LogInformation("All checkpoints verified. Patrol complete.");
                }
                
                _logger.LogInformation(
                    "Checkpoint {CheckpointId} verified successfully. Progress: {VerifiedCount}/{TotalCount}",
                    checkpointId, _currentPatrolStatus.VerifiedCheckpoints, _currentPatrolStatus.TotalCheckpoints);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying checkpoint {CheckpointId}", checkpointId);
                return false;
            }
        }

        /// <summary>
        /// Retrieves the current status of a patrol for a specific location, including completion statistics.
        /// </summary>
        /// <param name="locationId">The location identifier.</param>
        /// <returns>A task that returns the patrol status for the specified location.</returns>
        public async Task<PatrolStatus> GetPatrolStatus(int locationId)
        {
            _logger.LogInformation("Getting patrol status for location {LocationId}", locationId);
            
            if (locationId <= 0)
                throw new ArgumentException("Location ID must be greater than zero", nameof(locationId));
            
            try
            {
                // If there's an active patrol for the requested location, return its status
                if (IsPatrolActive && CurrentLocationId == locationId)
                {
                    return _currentPatrolStatus.Clone();
                }
                
                // Otherwise, create a new status with available information
                var patrolStatus = new PatrolStatus
                {
                    LocationId = locationId
                };
                
                // Get the total checkpoint count
                var checkpoints = await _checkpointRepository.GetCheckpointsByLocationIdAsync(locationId);
                patrolStatus.TotalCheckpoints = checkpoints?.Count() ?? 0;
                
                // Get the verified checkpoint count
                var checkpointStatuses = await _checkpointRepository.GetCheckpointStatusesAsync(locationId);
                patrolStatus.VerifiedCheckpoints = checkpointStatuses?.Count(s => s.IsVerified) ?? 0;
                
                _logger.LogInformation(
                    "Patrol status for location {LocationId}: {VerifiedCount}/{TotalCount} checkpoints verified",
                    locationId, patrolStatus.VerifiedCheckpoints, patrolStatus.TotalCheckpoints);
                
                return patrolStatus;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting patrol status for location {LocationId}", locationId);
                throw;
            }
        }

        /// <summary>
        /// Starts a new patrol for the specified location, initializing checkpoint monitoring and status tracking.
        /// </summary>
        /// <param name="locationId">The location identifier.</param>
        /// <returns>A task that returns the initial patrol status.</returns>
        public async Task<PatrolStatus> StartPatrol(int locationId)
        {
            _logger.LogInformation("Starting patrol for location {LocationId}", locationId);
            
            if (locationId <= 0)
                throw new ArgumentException("Location ID must be greater than zero", nameof(locationId));
            
            // If there's already an active patrol, end it first
            if (IsPatrolActive)
            {
                _logger.LogInformation("Ending existing patrol before starting new one");
                await EndPatrol();
            }
            
            try
            {
                // Get checkpoints for the location
                var checkpoints = await _checkpointRepository.GetCheckpointsByLocationIdAsync(locationId);
                
                if (checkpoints == null || !checkpoints.Any())
                {
                    _logger.LogWarning("No checkpoints found for location {LocationId}", locationId);
                    throw new InvalidOperationException($"No checkpoints found for location {locationId}");
                }
                
                // Clear any existing checkpoint statuses for the location
                await _checkpointRepository.ClearCheckpointStatusesAsync(locationId);
                
                // Initialize active checkpoints and proximity status
                _activeCheckpoints = checkpoints.ToList();
                _checkpointProximityStatus = _activeCheckpoints.ToDictionary(c => c.Id, c => false);
                
                // Create new patrol status
                _currentPatrolStatus = new PatrolStatus
                {
                    LocationId = locationId,
                    TotalCheckpoints = _activeCheckpoints.Count,
                    VerifiedCheckpoints = 0,
                    StartTime = DateTime.UtcNow
                };
                
                // Update service state
                IsPatrolActive = true;
                CurrentLocationId = locationId;
                
                // Set geofence service proximity threshold to match this service
                _geofenceService.ProximityThresholdFeet = ProximityThresholdFeet;
                
                // Start monitoring checkpoint proximity
                await _geofenceService.StartMonitoring(_activeCheckpoints);
                
                // Display checkpoints on the map
                await _mapService.DisplayCheckpoints(_activeCheckpoints);
                
                _logger.LogInformation("Patrol started for location {LocationId} with {CheckpointCount} checkpoints", 
                    locationId, _activeCheckpoints.Count);
                
                return _currentPatrolStatus.Clone();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting patrol for location {LocationId}", locationId);
                throw;
            }
        }

        /// <summary>
        /// Ends the current patrol, finalizing status and stopping checkpoint monitoring.
        /// </summary>
        /// <returns>A task that returns the final patrol status.</returns>
        public async Task<PatrolStatus> EndPatrol()
        {
            _logger.LogInformation("Ending current patrol");
            
            if (!IsPatrolActive)
            {
                _logger.LogWarning("No active patrol to end");
                return null;
            }
            
            try
            {
                // Stop checkpoint monitoring
                await _geofenceService.StopMonitoring();
                
                // Complete the patrol status
                _currentPatrolStatus.CompletePatrol();
                
                // Clear the map
                _mapService.ClearCheckpoints();
                
                // Store the final status before clearing state
                var finalStatus = _currentPatrolStatus.Clone();
                
                // Clear active data
                _activeCheckpoints.Clear();
                _checkpointProximityStatus.Clear();
                
                // Update service state
                IsPatrolActive = false;
                CurrentLocationId = null;
                
                _logger.LogInformation("Patrol ended with {VerifiedCount}/{TotalCount} checkpoints verified",
                    finalStatus.VerifiedCheckpoints, finalStatus.TotalCheckpoints);
                
                return finalStatus;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ending patrol");
                throw;
            }
        }

        /// <summary>
        /// Manually checks proximity to all checkpoints based on the provided coordinates.
        /// </summary>
        /// <param name="latitude">The latitude coordinate.</param>
        /// <param name="longitude">The longitude coordinate.</param>
        /// <returns>A task that returns the IDs of checkpoints within proximity.</returns>
        public async Task<IEnumerable<int>> CheckProximity(double latitude, double longitude)
        {
            _logger.LogInformation("Checking proximity to checkpoints at coordinates {Latitude},{Longitude}", 
                latitude, longitude);
            
            if (!IsPatrolActive)
            {
                _logger.LogWarning("No active patrol for proximity check");
                return Enumerable.Empty<int>();
            }
            
            try
            {
                // Convert feet to meters for proximity calculation
                double thresholdMeters = LocationHelper.ConvertFeetToMeters(ProximityThresholdFeet);
                
                // Use the geofence service to check proximity
                var checkpointsInRange = await _geofenceService.CheckProximity(latitude, longitude);
                
                _logger.LogInformation("Found {Count} checkpoints within proximity threshold", 
                    checkpointsInRange.Count());
                
                return checkpointsInRange;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking proximity to checkpoints");
                return Enumerable.Empty<int>();
            }
        }

        /// <summary>
        /// Handles proximity change events from the geofence service.
        /// </summary>
        private void OnGeofenceProximityChanged(object sender, CheckpointProximityEventArgs e)
        {
            _logger.LogInformation("Proximity changed for checkpoint {CheckpointId}: {Distance:F1} feet, in range: {IsInRange}",
                e.CheckpointId, e.Distance, e.IsInRange);
            
            if (!IsPatrolActive)
                return;
            
            // Update proximity status
            _checkpointProximityStatus[e.CheckpointId] = e.IsInRange;
            
            // Highlight/unhighlight the checkpoint on the map
            _mapService.HighlightCheckpoint(e.CheckpointId, e.IsInRange);
            
            // Notify subscribers
            CheckpointProximityChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Handles location change events from the location service.
        /// </summary>
        private async void OnLocationChanged(object sender, LocationChangedEventArgs e)
        {
            if (!IsPatrolActive)
                return;
            
            // Check proximity to checkpoints with the updated location
            await CheckProximity(e.Location.Latitude, e.Location.Longitude);
        }

        /// <summary>
        /// Disposes resources used by the patrol service.
        /// </summary>
        public void Dispose()
        {
            // End any active patrol
            if (IsPatrolActive)
            {
                _ = EndPatrol();
            }
            
            // Unsubscribe from events
            _geofenceService.ProximityChanged -= OnGeofenceProximityChanged;
            _locationService.LocationChanged -= OnLocationChanged;
        }
    }
}