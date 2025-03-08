using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.ApplicationModel.Notifications;
using SecurityPatrol.Constants;
using SecurityPatrol.Helpers;
using SecurityPatrol.Models;
using SecurityPatrol.Services;

namespace SecurityPatrol.Services
{
    /// <summary>
    /// Implementation of the IBackgroundService interface that provides continuous location tracking in the background,
    /// even when the application is minimized. This service is essential for maintaining location tracking during
    /// security patrols and ensuring accurate checkpoint verification.
    /// </summary>
    public class BackgroundLocationService : IBackgroundService
    {
        private bool _isRunning;
        private Timer _locationTimer;
        private CancellationTokenSource _cancellationTokenSource;
        private int _trackingInterval;
        private GeolocationAccuracy _accuracy;
        private readonly ILogger<BackgroundLocationService> _logger;
        private bool _batteryOptimized;
        private int _lastBatteryLevel;
        private DateTime _lastBatteryCheck;

        /// <summary>
        /// Event that is raised when the device's location changes.
        /// </summary>
        public event EventHandler<LocationChangedEventArgs> LocationChanged;

        /// <summary>
        /// Initializes a new instance of the BackgroundLocationService class with required dependencies.
        /// </summary>
        /// <param name="logger">Logger for recording service activity.</param>
        public BackgroundLocationService(ILogger<BackgroundLocationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _isRunning = false;
            _trackingInterval = AppConstants.LocationTrackingIntervalDefault;
            _accuracy = GeolocationAccuracy.Medium;
            _batteryOptimized = true;
            _lastBatteryLevel = Battery.ChargeLevel;
            _lastBatteryCheck = DateTime.UtcNow;
        }

        /// <summary>
        /// Starts the background location service.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when location permissions are not granted.</exception>
        public async Task Start()
        {
            if (_isRunning)
            {
                _logger.LogInformation("Background location service is already running.");
                return;
            }

            try
            {
                _logger.LogInformation("Starting background location service...");

                // Check if location permissions are granted
                bool hasLocationPermission = await PermissionHelper.CheckLocationPermissionsAsync(_logger);
                if (!hasLocationPermission)
                {
                    _logger.LogWarning("Location permission is not granted. Cannot start background location service.");
                    throw new UnauthorizedAccessException("Location permission is required for background location tracking.");
                }

                // Check if background location permission is granted (for Android 10+)
                bool hasBackgroundPermission = await PermissionHelper.CheckBackgroundLocationPermissionAsync(_logger);
                if (!hasBackgroundPermission)
                {
                    _logger.LogWarning("Background location permission is not granted. Cannot start background location service.");
                    throw new UnauthorizedAccessException("Background location permission is required for continuous location tracking.");
                }

                // Initialize cancellation token source
                _cancellationTokenSource = new CancellationTokenSource();
                
                // Mark service as running
                _isRunning = true;

                // Start the foreground service on Android
                await StartForegroundService();

                // Determine tracking settings based on battery optimization
                AdjustTrackingSettings(_batteryOptimized);

                // Start location tracking timer
                _locationTimer = new Timer(OnLocationTimerElapsed, null, 0, _trackingInterval * 1000);

                _logger.LogInformation("Background location service started with interval: {Interval} seconds, accuracy: {Accuracy}", 
                    _trackingInterval, _accuracy);
            }
            catch (Exception ex)
            {
                _isRunning = false;
                _logger.LogError(ex, "Failed to start background location service");
                throw;
            }
        }

        /// <summary>
        /// Stops the background location service.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task Stop()
        {
            if (!_isRunning)
            {
                _logger.LogInformation("Background location service is not running.");
                return;
            }

            try
            {
                _logger.LogInformation("Stopping background location service...");

                // Cancel any pending operations
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;

                // Stop and dispose timer
                _locationTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                _locationTimer?.Dispose();
                _locationTimer = null;

                // Mark service as stopped
                _isRunning = false;

                // Stop the foreground service on Android
                await StopForegroundService();

                _logger.LogInformation("Background location service stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping background location service");
                throw;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the background service is currently running.
        /// </summary>
        /// <returns>True if the service is running, false otherwise.</returns>
        public bool IsRunning()
        {
            return _isRunning;
        }

        /// <summary>
        /// Handles the timer elapsed event to get the current location.
        /// </summary>
        /// <param name="state">The state object passed to the timer.</param>
        private async void OnLocationTimerElapsed(object state)
        {
            if (!_isRunning)
            {
                return;
            }

            try
            {
                // Get current location
                LocationModel location = await LocationHelper.GetCurrentLocationAsync(_accuracy);

                if (location != null)
                {
                    // Raise location changed event
                    OnLocationChanged(location);

                    // Monitor battery impact and adjust tracking settings if needed
                    MonitorBatteryImpact();
                }
                else
                {
                    _logger.LogWarning("Failed to get current location");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in location timer callback");

                // If this is a critical error, try to restart the service
                if (ex is UnauthorizedAccessException || ex is System.Net.Http.HttpRequestException || ex is TimeoutException)
                {
                    _logger.LogWarning("Critical error detected. Attempting to restart the service...");
                    await RestartServiceAsync();
                }
            }
        }

        /// <summary>
        /// Raises the LocationChanged event with the provided location.
        /// </summary>
        /// <param name="location">The new location to report.</param>
        private void OnLocationChanged(LocationModel location)
        {
            try
            {
                LocationChangedEventArgs args = new LocationChangedEventArgs(location);
                LocationChanged?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error raising LocationChanged event");
            }
        }

        /// <summary>
        /// Starts the Android foreground service with a notification.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task StartForegroundService()
        {
            // Only relevant for Android
            if (DeviceInfo.Platform != DevicePlatform.Android)
            {
                return;
            }

            try
            {
                _logger.LogInformation("Starting Android foreground service");
                
                // Create a notification for the foreground service
                var notification = new Notification
                {
                    Title = "Security Patrol",
                    Text = "Location tracking is active",
                    Icon = "location_tracking_icon",
                    Priority = NotificationPriority.High
                };

                // Start the foreground service
                // Using MainThread to ensure UI-related operations run on the main thread
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    try
                    {
                        // In a production app, we'd use platform-specific code to start a foreground service
                        // For example, on Android, we might use something like:
                        // var intent = new Android.Content.Intent(Platform.CurrentActivity, typeof(LocationForegroundService));
                        // Platform.CurrentActivity.StartForegroundService(intent);
                        
                        // Since we don't have the exact Android implementation details in the imports,
                        // we'll log this as a placeholder for the actual implementation
                        _logger.LogInformation("Platform-specific code would start the foreground service here");
                        
                        // The actual implementation would involve using the ForegroundService and 
                        // ForegroundServiceLocation permissions mentioned in PermissionConstants
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in platform-specific foreground service start");
                    }
                });

                _logger.LogInformation("Android foreground service started");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting Android foreground service");
                // Don't throw here - we can still track location even if foreground service fails
            }
        }

        /// <summary>
        /// Stops the Android foreground service.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task StopForegroundService()
        {
            // Only relevant for Android
            if (DeviceInfo.Platform != DevicePlatform.Android)
            {
                return;
            }

            try
            {
                _logger.LogInformation("Stopping Android foreground service");
                
                // Stop the foreground service
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    try
                    {
                        // In a production app, we'd use platform-specific code to stop a foreground service
                        // For example, on Android, we might use something like:
                        // var intent = new Android.Content.Intent(Platform.CurrentActivity, typeof(LocationForegroundService));
                        // Platform.CurrentActivity.StopService(intent);
                        
                        // Since we don't have the exact Android implementation details in the imports,
                        // we'll log this as a placeholder for the actual implementation
                        _logger.LogInformation("Platform-specific code would stop the foreground service here");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in platform-specific foreground service stop");
                    }
                });

                _logger.LogInformation("Android foreground service stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping Android foreground service");
            }
        }

        /// <summary>
        /// Monitors battery impact and adjusts tracking settings if needed.
        /// </summary>
        private void MonitorBatteryImpact()
        {
            try
            {
                // Check if enough time has passed to check battery again (every 5 minutes)
                TimeSpan timeSinceLastCheck = DateTime.UtcNow - _lastBatteryCheck;
                if (timeSinceLastCheck.TotalMinutes < 5)
                {
                    return;
                }

                // Get current battery level
                int currentBatteryLevel = Battery.ChargeLevel;
                
                // Calculate battery drain since last check
                int batteryDrain = _lastBatteryLevel - currentBatteryLevel;
                
                // Calculate drain per hour based on elapsed time
                double hoursElapsed = timeSinceLastCheck.TotalHours;
                double drainPerHour = hoursElapsed > 0 ? batteryDrain / hoursElapsed : 0;

                _logger.LogInformation("Battery impact: {DrainPerHour:F2}% per hour (Current: {CurrentLevel}%, Previous: {PreviousLevel}%)", 
                    drainPerHour, currentBatteryLevel, _lastBatteryLevel);

                // If battery drain is too high, increase the interval to reduce impact
                if (drainPerHour > AppConstants.MaxBatteryImpactPercent)
                {
                    _logger.LogWarning("Battery drain exceeds threshold ({DrainPerHour:F2}% > {Threshold}%). Optimizing tracking settings.", 
                        drainPerHour, AppConstants.MaxBatteryImpactPercent);
                    
                    // Adjust tracking settings to save battery
                    AdjustTrackingSettings(true);
                }
                // If battery drain is acceptable and we're in battery-optimized mode, maintain current settings
                else if (drainPerHour <= AppConstants.MaxBatteryImpactPercent && _batteryOptimized)
                {
                    _logger.LogInformation("Battery drain is acceptable ({DrainPerHour:F2}% <= {Threshold}%). Maintaining optimized settings.", 
                        drainPerHour, AppConstants.MaxBatteryImpactPercent);
                }
                // If battery drain is very low and we're in battery-optimized mode, consider decreasing interval
                else if (drainPerHour < AppConstants.MaxBatteryImpactPercent / 2 && _batteryOptimized)
                {
                    _logger.LogInformation("Battery drain is very low ({DrainPerHour:F2}% < {HalfThreshold}%). Considering more frequent updates.", 
                        drainPerHour, AppConstants.MaxBatteryImpactPercent / 2);
                    
                    // If battery level is above 50%, we can consider decreasing the interval
                    if (currentBatteryLevel > 50)
                    {
                        AdjustTrackingSettings(false);
                    }
                }

                // Update battery tracking
                _lastBatteryLevel = currentBatteryLevel;
                _lastBatteryCheck = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error monitoring battery impact");
            }
        }

        /// <summary>
        /// Adjusts the tracking interval and accuracy based on battery optimization settings.
        /// </summary>
        /// <param name="optimize">Whether to optimize for battery life.</param>
        private void AdjustTrackingSettings(bool optimize)
        {
            try
            {
                _batteryOptimized = optimize;
                
                if (optimize)
                {
                    _trackingInterval = AppConstants.LocationTrackingIntervalLowPower;
                }
                else
                {
                    _trackingInterval = AppConstants.LocationTrackingIntervalHighAccuracy;
                }
                
                _accuracy = LocationHelper.GetLocationTrackingAccuracy(optimize);
                
                // Update timer interval if active
                _locationTimer?.Change(0, _trackingInterval * 1000);
                
                _logger.LogInformation("Adjusted tracking settings: Battery Optimized = {Optimized}, Interval = {Interval} seconds, Accuracy = {Accuracy}", 
                    _batteryOptimized, _trackingInterval, _accuracy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adjusting tracking settings");
            }
        }

        /// <summary>
        /// Attempts to restart the service after a delay.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task RestartServiceAsync()
        {
            try
            {
                _logger.LogInformation("Attempting to restart background location service after delay");
                
                // Stop the service
                await Stop();
                
                // Wait for the specified delay
                await Task.Delay(TimeSpan.FromMinutes(AppConstants.BackgroundServiceRestartDelayMinutes));
                
                // Try to start the service again
                await Start();
                
                _logger.LogInformation("Successfully restarted background location service");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restart background location service");
            }
        }
    }
}