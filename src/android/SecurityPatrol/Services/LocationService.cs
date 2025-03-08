using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Devices.Sensors;
using SecurityPatrol.Constants;
using SecurityPatrol.Helpers;
using SecurityPatrol.Models;

namespace SecurityPatrol.Services
{
    /// <summary>
    /// Implementation of the ILocationService interface that provides location tracking functionality for the Security Patrol application.
    /// This service manages GPS location tracking during active shifts, handles location updates, and coordinates with the background service 
    /// for continuous tracking even when the app is minimized.
    /// </summary>
    public class LocationService : ILocationService, IDisposable
    {
        private bool _isTracking;
        private readonly ConcurrentQueue<LocationModel> _locationQueue;
        private readonly ILocationRepository _locationRepository;
        private readonly ILocationSyncService _locationSyncService;
        private readonly BackgroundLocationService _backgroundLocationService;
        private readonly INetworkService _networkService;
        private readonly ISettingsService _settingsService;
        private readonly ILogger<LocationService> _logger;
        private bool _batteryOptimized;

        /// <summary>
        /// Event that is raised when the device's location changes.
        /// </summary>
        public event EventHandler<LocationChangedEventArgs> LocationChanged;

        /// <summary>
        /// Initializes a new instance of the LocationService class with required dependencies.
        /// </summary>
        /// <param name="locationRepository">Repository for storing location data.</param>
        /// <param name="locationSyncService">Service for synchronizing location data with the backend.</param>
        /// <param name="backgroundLocationService">Service for background location tracking.</param>
        /// <param name="networkService">Service for monitoring network connectivity.</param>
        /// <param name="settingsService">Service for storing and retrieving user settings.</param>
        /// <param name="logger">Logger for recording service activity.</param>
        public LocationService(
            ILocationRepository locationRepository,
            ILocationSyncService locationSyncService,
            BackgroundLocationService backgroundLocationService,
            INetworkService networkService,
            ISettingsService settingsService,
            ILogger<LocationService> logger)
        {
            _locationRepository = locationRepository ?? throw new ArgumentNullException(nameof(locationRepository));
            _locationSyncService = locationSyncService ?? throw new ArgumentNullException(nameof(locationSyncService));
            _backgroundLocationService = backgroundLocationService ?? throw new ArgumentNullException(nameof(backgroundLocationService));
            _networkService = networkService ?? throw new ArgumentNullException(nameof(networkService));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _isTracking = false;
            _locationQueue = new ConcurrentQueue<LocationModel>();
            
            // Get battery optimization setting from preferences or use default (true)
            _batteryOptimized = _settingsService.GetValue<bool>("BatteryOptimized", true);

            // Subscribe to events
            _backgroundLocationService.LocationChanged += OnLocationChanged;
            _networkService.ConnectivityChanged += OnConnectivityChanged;

            _logger.LogInformation("LocationService initialized");
        }

        /// <summary>
        /// Starts location tracking.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when location permissions are not granted.</exception>
        public async Task StartTracking()
        {
            _logger.LogInformation("Starting location tracking");

            if (_isTracking)
            {
                _logger.LogInformation("Location tracking is already active");
                return;
            }

            // Check if location permissions are granted
            bool hasPermission = await PermissionHelper.CheckLocationPermissionsAsync(_logger);
            if (!hasPermission)
            {
                hasPermission = await PermissionHelper.RequestLocationPermissionsAsync(true, _logger);
                if (!hasPermission)
                {
                    _logger.LogWarning("Location permission denied. Cannot start tracking");
                    throw new UnauthorizedAccessException(ErrorMessages.LocationPermissionDenied);
                }
            }

            // Check for background location permission on Android 10+
            bool hasBackgroundPermission = await PermissionHelper.CheckBackgroundLocationPermissionAsync(_logger);
            if (!hasBackgroundPermission)
            {
                // Request background permission
                hasBackgroundPermission = await PermissionHelper.RequestBackgroundLocationPermissionAsync(true, _logger);
                if (!hasBackgroundPermission)
                {
                    // Log warning but continue - some functionality will be limited when app is in background
                    _logger.LogWarning("Background location permission denied. Tracking will be limited when app is in background");
                }
            }

            // Start the background service
            await _backgroundLocationService.Start();

            // Update tracking state
            _isTracking = true;

            // Schedule periodic sync
            _locationSyncService.ScheduleSync(TimeSpan.FromMinutes(AppConstants.SyncIntervalMinutes));

            _logger.LogInformation("Location tracking started successfully");
        }

        /// <summary>
        /// Stops location tracking.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task StopTracking()
        {
            _logger.LogInformation("Stopping location tracking");

            if (!_isTracking)
            {
                _logger.LogInformation("Location tracking is not active");
                return;
            }

            // Stop the background service
            await _backgroundLocationService.Stop();

            // Update tracking state
            _isTracking = false;

            // Cancel scheduled sync
            _locationSyncService.CancelScheduledSync();

            // Process any remaining locations in the queue
            await ProcessLocationQueue();

            // Trigger a final sync if network is available
            if (_networkService.IsConnected)
            {
                await _locationSyncService.SyncLocationsAsync(AppConstants.LocationBatchSize);
            }

            _logger.LogInformation("Location tracking stopped successfully");
        }

        /// <summary>
        /// Gets the current device location.
        /// </summary>
        /// <returns>A task that returns the current location.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when location permissions are not granted.</exception>
        /// <exception cref="TimeoutException">Thrown when unable to get location within timeout period.</exception>
        public async Task<LocationModel> GetCurrentLocation()
        {
            _logger.LogInformation("Getting current location");

            try
            {
                // Check if location permissions are granted
                bool hasPermission = await PermissionHelper.CheckLocationPermissionsAsync(_logger);
                if (!hasPermission)
                {
                    hasPermission = await PermissionHelper.RequestLocationPermissionsAsync(true, _logger);
                    if (!hasPermission)
                    {
                        _logger.LogWarning("Location permission denied. Cannot get current location");
                        throw new UnauthorizedAccessException(ErrorMessages.LocationPermissionDenied);
                    }
                }

                // Get current location with appropriate accuracy based on battery optimization
                GeolocationAccuracy accuracy = LocationHelper.GetLocationTrackingAccuracy(_batteryOptimized);
                
                LocationModel location = await LocationHelper.GetCurrentLocationAsync(accuracy);
                
                return location;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current location");
                throw;
            }
        }

        /// <summary>
        /// Gets a value indicating whether location tracking is currently active.
        /// </summary>
        public bool IsTracking => _isTracking;

        /// <summary>
        /// Sets the battery optimization mode for location tracking.
        /// </summary>
        /// <param name="optimize">Whether to optimize for battery life.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SetBatteryOptimization(bool optimize)
        {
            _logger.LogInformation($"Setting battery optimization to: {optimize}");
            
            _batteryOptimized = optimize;
            
            // Save setting
            _settingsService.SetValue<bool>("BatteryOptimized", optimize);
            
            // If tracking is active, restart the background service to apply new settings
            if (_isTracking)
            {
                await _backgroundLocationService.Stop();
                await _backgroundLocationService.Start();
            }
            
            _logger.LogInformation($"Battery optimization set to: {optimize}");
        }

        /// <summary>
        /// Gets the most recent location records.
        /// </summary>
        /// <param name="count">The maximum number of records to retrieve.</param>
        /// <returns>A task that returns a collection of recent locations.</returns>
        public async Task<IEnumerable<LocationModel>> GetRecentLocations(int count)
        {
            return await _locationRepository.GetRecentLocationsAsync(count);
        }

        /// <summary>
        /// Handles location change events from the background service.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Location changed event arguments.</param>
        private async void OnLocationChanged(object sender, LocationChangedEventArgs e)
        {
            if (!_isTracking)
            {
                return;
            }

            try
            {
                // Add location to queue
                _locationQueue.Enqueue(e.Location);
                
                // Raise event for subscribers
                LocationChanged?.Invoke(this, e);
                
                // Process queue if it reaches batch size
                if (_locationQueue.Count >= AppConstants.LocationBatchSize)
                {
                    await ProcessLocationQueue();
                }
                
                _logger.LogDebug($"Location updated: {e.Location.Latitude}, {e.Location.Longitude}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling location change");
            }
        }

        /// <summary>
        /// Handles connectivity change events from the network service.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Connectivity changed event arguments.</param>
        private async void OnConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
        {
            try
            {
                // If connection became available and we are tracking, trigger a sync
                if (e.IsConnected && _isTracking)
                {
                    _logger.LogInformation("Network connection restored. Triggering location sync");
                    await _locationSyncService.SyncLocationsAsync(AppConstants.LocationBatchSize);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling connectivity change");
            }
        }

        /// <summary>
        /// Processes the queued location updates and saves them to the repository.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task ProcessLocationQueue()
        {
            if (_locationQueue.IsEmpty)
            {
                return;
            }

            try
            {
                _logger.LogInformation($"Processing location queue with {_locationQueue.Count} items");
                
                // Get batch of locations from queue up to the batch size
                var locations = new List<LocationModel>();
                LocationModel locationModel;
                int count = 0;
                
                while (_locationQueue.TryDequeue(out locationModel) && count < AppConstants.LocationBatchSize)
                {
                    locations.Add(locationModel);
                    count++;
                }
                
                if (locations.Count > 0)
                {
                    // Save to repository
                    await _locationRepository.SaveLocationBatchAsync(locations);
                    
                    // If network is available, trigger sync
                    if (_networkService.IsConnected)
                    {
                        await _locationSyncService.SyncLocationsAsync(AppConstants.LocationBatchSize);
                    }
                    
                    _logger.LogInformation($"Processed and saved {locations.Count} locations");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing location queue");
            }
        }

        /// <summary>
        /// Disposes the LocationService and releases resources.
        /// </summary>
        public void Dispose()
        {
            try
            {
                // Stop tracking if active
                if (_isTracking)
                {
                    StopTracking().Wait();
                }
                
                // Unsubscribe from events
                _backgroundLocationService.LocationChanged -= OnLocationChanged;
                _networkService.ConnectivityChanged -= OnConnectivityChanged;
                
                _logger.LogInformation("LocationService disposed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing LocationService");
            }
        }
    }
}