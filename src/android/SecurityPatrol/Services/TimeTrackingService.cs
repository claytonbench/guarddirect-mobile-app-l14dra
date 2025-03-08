using System;  // Version 8.0+
using System.Threading.Tasks;  // Version 8.0+
using System.Collections.Generic;  // Version 8.0+
using Microsoft.Extensions.Logging;  // Version 8.0+
using SecurityPatrol.Models;

namespace SecurityPatrol.Services
{
    /// <summary>
    /// Service that implements the ITimeTrackingService interface to provide time tracking functionality for security personnel.
    /// </summary>
    public class TimeTrackingService : ITimeTrackingService
    {
        /// <summary>
        /// Event that is raised when the clock status changes.
        /// </summary>
        public event EventHandler<ClockStatusChangedEventArgs> StatusChanged;

        private readonly ITimeRecordRepository _timeRecordRepository;
        private readonly ILocationService _locationService;
        private readonly IAuthenticationStateProvider _authStateProvider;
        private readonly ITimeTrackingSyncService _syncService;
        private readonly ILogger<TimeTrackingService> _logger;
        private ClockStatus _currentStatus;

        /// <summary>
        /// Initializes a new instance of the TimeTrackingService class with required dependencies.
        /// </summary>
        /// <param name="timeRecordRepository">Repository for time record persistence.</param>
        /// <param name="locationService">Service for location tracking operations.</param>
        /// <param name="authStateProvider">Provider for authentication state information.</param>
        /// <param name="syncService">Service for synchronizing time records with backend.</param>
        /// <param name="logger">Logger for recording service activities.</param>
        /// <exception cref="ArgumentNullException">Thrown if any required dependency is null.</exception>
        public TimeTrackingService(
            ITimeRecordRepository timeRecordRepository,
            ILocationService locationService,
            IAuthenticationStateProvider authStateProvider,
            ITimeTrackingSyncService syncService,
            ILogger<TimeTrackingService> logger)
        {
            _timeRecordRepository = timeRecordRepository ?? throw new ArgumentNullException(nameof(timeRecordRepository));
            _locationService = locationService ?? throw new ArgumentNullException(nameof(locationService));
            _authStateProvider = authStateProvider ?? throw new ArgumentNullException(nameof(authStateProvider));
            _syncService = syncService ?? throw new ArgumentNullException(nameof(syncService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _currentStatus = new ClockStatus();
            
            // Initialize the status asynchronously
            _ = InitializeStatusAsync();
        }

        /// <summary>
        /// Initializes the current clock status based on the latest clock events in the database.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task InitializeStatusAsync()
        {
            try
            {
                _logger.LogInformation("Initializing clock status from database...");
                
                // Get the latest clock-in and clock-out events
                var latestClockIn = await _timeRecordRepository.GetLatestClockInEventAsync();
                var latestClockOut = await _timeRecordRepository.GetLatestClockOutEventAsync();

                // Determine the current clock status
                if (latestClockIn == null && latestClockOut == null)
                {
                    // No records at all, so definitely not clocked in
                    _currentStatus.IsClocked = false;
                }
                else if (latestClockIn != null && latestClockOut == null)
                {
                    // Have clock-in but no clock-out, so clocked in
                    _currentStatus.IsClocked = true;
                }
                else if (latestClockIn == null && latestClockOut != null)
                {
                    // Have clock-out but no clock-in (shouldn't happen normally), so not clocked in
                    _currentStatus.IsClocked = false;
                }
                else
                {
                    // Both exist, compare timestamps to determine current status
                    _currentStatus.IsClocked = latestClockIn.Timestamp > latestClockOut.Timestamp;
                }

                // Update the last clock in/out times
                _currentStatus.LastClockInTime = latestClockIn?.Timestamp;
                _currentStatus.LastClockOutTime = latestClockOut?.Timestamp;

                _logger.LogInformation("Clock status initialized. Is clocked in: {IsClocked}", _currentStatus.IsClocked);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing clock status: {Message}", ex.Message);
                // Set to safe default - not clocked in
                _currentStatus.IsClocked = false;
            }
        }

        /// <summary>
        /// Records a clock-in event with the current timestamp and location.
        /// </summary>
        /// <returns>A task that returns the created time record.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the user is already clocked in.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown when the user is not authenticated.</exception>
        public async Task<TimeRecordModel> ClockIn()
        {
            // Check if user is authenticated
            var authState = await _authStateProvider.GetCurrentState();
            if (!authState.IsAuthenticated)
            {
                _logger.LogWarning("Attempted to clock in while not authenticated");
                throw new UnauthorizedAccessException("User must be authenticated to clock in");
            }

            // Check if already clocked in
            if (_currentStatus.IsClocked)
            {
                _logger.LogWarning("Attempted to clock in while already clocked in");
                throw new InvalidOperationException("Already clocked in");
            }

            _logger.LogInformation("Processing clock in request for user {UserId}", authState.PhoneNumber);

            // Get current location
            LocationModel location;
            try
            {
                location = await _locationService.GetCurrentLocation();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get current location for clock in. Using default coordinates.");
                // Create a default location if unable to get current location
                location = new LocationModel
                {
                    Latitude = 0.0,
                    Longitude = 0.0,
                    Accuracy = 0.0,
                    Timestamp = DateTime.UtcNow
                };
            }
            
            // Create new clock-in record
            var record = TimeRecordModel.CreateClockIn(
                authState.PhoneNumber,
                location.Latitude,
                location.Longitude);

            // Save to repository
            await _timeRecordRepository.SaveTimeRecordAsync(record);
            _logger.LogInformation("Clock in record saved with ID {RecordId}", record.Id);

            // Start location tracking
            try
            {
                await _locationService.StartTracking();
                _logger.LogInformation("Location tracking started");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start location tracking after clock in. Continuing anyway.");
                // Continue even if location tracking fails - don't block the clock in operation
            }

            // Update status
            _currentStatus.IsClocked = true;
            _currentStatus.LastClockInTime = record.Timestamp;
            
            // Notify subscribers
            OnStatusChanged();

            // Try to sync the record, but don't wait for it to complete
            _ = SyncRecordWithErrorHandling(record);

            _logger.LogInformation("Clock in complete for user {UserId}", authState.PhoneNumber);
            return record;
        }

        /// <summary>
        /// Records a clock-out event with the current timestamp and location.
        /// </summary>
        /// <returns>A task that returns the created time record.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the user is not clocked in.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown when the user is not authenticated.</exception>
        public async Task<TimeRecordModel> ClockOut()
        {
            // Check if user is authenticated
            var authState = await _authStateProvider.GetCurrentState();
            if (!authState.IsAuthenticated)
            {
                _logger.LogWarning("Attempted to clock out while not authenticated");
                throw new UnauthorizedAccessException("User must be authenticated to clock out");
            }

            // Check if already clocked out
            if (!_currentStatus.IsClocked)
            {
                _logger.LogWarning("Attempted to clock out while not clocked in");
                throw new InvalidOperationException("Not clocked in");
            }

            _logger.LogInformation("Processing clock out request for user {UserId}", authState.PhoneNumber);

            // Get current location
            LocationModel location;
            try
            {
                location = await _locationService.GetCurrentLocation();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get current location for clock out. Using default coordinates.");
                // Create a default location if unable to get current location
                location = new LocationModel
                {
                    Latitude = 0.0,
                    Longitude = 0.0,
                    Accuracy = 0.0,
                    Timestamp = DateTime.UtcNow
                };
            }
            
            // Create new clock-out record
            var record = TimeRecordModel.CreateClockOut(
                authState.PhoneNumber,
                location.Latitude,
                location.Longitude);

            // Save to repository
            await _timeRecordRepository.SaveTimeRecordAsync(record);
            _logger.LogInformation("Clock out record saved with ID {RecordId}", record.Id);

            // Stop location tracking
            try
            {
                await _locationService.StopTracking();
                _logger.LogInformation("Location tracking stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop location tracking after clock out. Continuing anyway.");
                // Continue even if stopping location tracking fails - don't block the clock out operation
            }

            // Update status
            _currentStatus.IsClocked = false;
            _currentStatus.LastClockOutTime = record.Timestamp;
            
            // Notify subscribers
            OnStatusChanged();

            // Try to sync the record, but don't wait for it to complete
            _ = SyncRecordWithErrorHandling(record);

            _logger.LogInformation("Clock out complete for user {UserId}", authState.PhoneNumber);
            return record;
        }

        /// <summary>
        /// Gets the current clock status.
        /// </summary>
        /// <returns>A task that returns the current clock status.</returns>
        public async Task<ClockStatus> GetCurrentStatus()
        {
            // Return a clone to prevent external modification
            return _currentStatus.Clone();
        }

        /// <summary>
        /// Gets the time tracking history with a specified number of records.
        /// </summary>
        /// <param name="count">The maximum number of records to retrieve.</param>
        /// <returns>A task that returns a collection of time records.</returns>
        /// <exception cref="ArgumentException">Thrown when count is less than or equal to zero.</exception>
        public async Task<IEnumerable<TimeRecordModel>> GetHistory(int count)
        {
            if (count <= 0)
            {
                throw new ArgumentException("Count must be greater than zero", nameof(count));
            }

            _logger.LogInformation("Retrieving time record history. Count: {Count}", count);
            var records = await _timeRecordRepository.GetTimeRecordsAsync(count);
            _logger.LogInformation("Retrieved {RecordCount} time records", records.Count);
            
            return records;
        }

        /// <summary>
        /// Raises the StatusChanged event with the current status.
        /// </summary>
        protected virtual void OnStatusChanged()
        {
            var status = _currentStatus.Clone();
            var args = new ClockStatusChangedEventArgs(status);
            
            _logger.LogInformation("Raising StatusChanged event. IsClocked: {IsClocked}", status.IsClocked);
            StatusChanged?.Invoke(this, args);
        }

        /// <summary>
        /// Attempts to synchronize a time record with error handling.
        /// </summary>
        /// <param name="record">The time record to synchronize.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task SyncRecordWithErrorHandling(TimeRecordModel record)
        {
            try
            {
                await _syncService.SyncRecordAsync(record);
                _logger.LogInformation("Successfully synced time record {RecordId}", record.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync time record {RecordId}: {Message}", record.Id, ex.Message);
                // Record will remain in local database and will be synced by the background sync service later
            }
        }
    }
}