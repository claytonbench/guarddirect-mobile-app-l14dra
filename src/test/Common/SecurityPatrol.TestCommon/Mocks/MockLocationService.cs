using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SecurityPatrol.Models;
using SecurityPatrol.Services;
using SecurityPatrol.TestCommon.Constants;
using SecurityPatrol.TestCommon.Data;
using SecurityPatrol.TestCommon.Helpers;

namespace SecurityPatrol.TestCommon.Mocks
{
    /// <summary>
    /// Mock implementation of ILocationService for testing purposes that simulates location tracking functionality
    /// without accessing actual device GPS.
    /// </summary>
    public class MockLocationService : ILocationService, IDisposable
    {
        private bool _isTracking;
        
        /// <summary>
        /// Gets or sets the current simulated location.
        /// </summary>
        public LocationModel CurrentLocation { get; private set; }
        
        private readonly LocationSimulator _locationSimulator;
        
        /// <summary>
        /// Gets or sets a value indicating whether operations should succeed.
        /// </summary>
        public bool ShouldSucceed { get; set; } = true;
        
        /// <summary>
        /// Gets or sets a value indicating whether operations should throw an exception.
        /// </summary>
        public bool ShouldThrowException { get; set; } = false;
        
        /// <summary>
        /// Gets or sets the exception to throw when ShouldThrowException is true.
        /// </summary>
        public Exception ExceptionToThrow { get; set; }
        
        /// <summary>
        /// Gets the list of locations that have been simulated.
        /// </summary>
        public List<LocationModel> LocationHistory { get; } = new List<LocationModel>();
        
        /// <summary>
        /// Gets the number of times StartTracking has been called.
        /// </summary>
        public int StartTrackingCallCount { get; private set; }
        
        /// <summary>
        /// Gets the number of times StopTracking has been called.
        /// </summary>
        public int StopTrackingCallCount { get; private set; }
        
        /// <summary>
        /// Gets the number of times GetCurrentLocation has been called.
        /// </summary>
        public int GetCurrentLocationCallCount { get; private set; }
        
        /// <summary>
        /// Event that is raised when the device's location changes.
        /// </summary>
        public event EventHandler<LocationChangedEventArgs> LocationChanged;
        
        /// <summary>
        /// Initializes a new instance of the MockLocationService class with default settings.
        /// </summary>
        public MockLocationService()
        {
            _isTracking = false;
            
            // Initialize with default location
            CurrentLocation = TestLocations.DefaultLocationModel ?? 
                new LocationModel
                {
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude,
                    Accuracy = TestConstants.TestAccuracy,
                    Timestamp = DateTime.UtcNow
                };
            
            _locationSimulator = new LocationSimulator();
            _locationSimulator.LocationChanged += OnLocationChanged;
        }
        
        /// <summary>
        /// Mocks starting location tracking.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task StartTracking()
        {
            if (ShouldThrowException && ExceptionToThrow != null)
            {
                throw ExceptionToThrow;
            }
            
            StartTrackingCallCount++;
            
            if (ShouldSucceed)
            {
                _isTracking = true;
                _locationSimulator.StartSimulation();
            }
            
            await Task.CompletedTask;
        }
        
        /// <summary>
        /// Mocks stopping location tracking.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task StopTracking()
        {
            if (ShouldThrowException && ExceptionToThrow != null)
            {
                throw ExceptionToThrow;
            }
            
            StopTrackingCallCount++;
            
            if (ShouldSucceed)
            {
                _isTracking = false;
                _locationSimulator.StopSimulation();
            }
            
            await Task.CompletedTask;
        }
        
        /// <summary>
        /// Mocks getting the current device location.
        /// </summary>
        /// <returns>A task that returns the current mock location.</returns>
        public async Task<LocationModel> GetCurrentLocation()
        {
            if (ShouldThrowException && ExceptionToThrow != null)
            {
                throw ExceptionToThrow;
            }
            
            GetCurrentLocationCallCount++;
            
            return await Task.FromResult(CurrentLocation);
        }
        
        /// <summary>
        /// Gets a value indicating whether location tracking is currently active.
        /// </summary>
        public bool IsTracking => _isTracking;
        
        /// <summary>
        /// Handles location change events from the location simulator.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnLocationChanged(object sender, LocationChangedEventArgs e)
        {
            if (!_isTracking)
                return;
                
            CurrentLocation = e.Location;
            LocationHistory.Add(e.Location);
            
            LocationChanged?.Invoke(this, e);
        }
        
        /// <summary>
        /// Simulates a location change event.
        /// </summary>
        /// <param name="location">The new location.</param>
        public void SimulateLocationChanged(LocationModel location)
        {
            if (location == null)
                throw new ArgumentNullException(nameof(location));
                
            CurrentLocation = location;
            LocationHistory.Add(location);
            
            if (LocationChanged != null && _isTracking)
            {
                LocationChanged(this, new LocationChangedEventArgs(location));
            }
        }
        
        /// <summary>
        /// Simulates movement from current location to a target point.
        /// </summary>
        /// <param name="targetLatitude">The target latitude.</param>
        /// <param name="targetLongitude">The target longitude.</param>
        /// <param name="steps">The number of steps to take.</param>
        /// <returns>The generated sequence of location points.</returns>
        public List<LocationModel> SimulateMovementToPoint(double targetLatitude, double targetLongitude, int steps)
        {
            return _locationSimulator.SimulateMovementToPoint(targetLatitude, targetLongitude, steps);
        }
        
        /// <summary>
        /// Configures an exception to be thrown by any method.
        /// </summary>
        /// <param name="exception">The exception to throw.</param>
        public void SetupException(Exception exception)
        {
            ShouldThrowException = true;
            ExceptionToThrow = exception;
        }
        
        /// <summary>
        /// Clears any configured exception.
        /// </summary>
        public void ClearException()
        {
            ShouldThrowException = false;
            ExceptionToThrow = null;
        }
        
        /// <summary>
        /// Configures the current location to be returned.
        /// </summary>
        /// <param name="location">The location to return.</param>
        public void SetupCurrentLocation(LocationModel location)
        {
            CurrentLocation = location;
        }
        
        /// <summary>
        /// Verifies that StartTracking was called.
        /// </summary>
        /// <returns>True if the method was called, otherwise false.</returns>
        public bool VerifyStartTrackingCalled()
        {
            return StartTrackingCallCount > 0;
        }
        
        /// <summary>
        /// Verifies that StopTracking was called.
        /// </summary>
        /// <returns>True if the method was called, otherwise false.</returns>
        public bool VerifyStopTrackingCalled()
        {
            return StopTrackingCallCount > 0;
        }
        
        /// <summary>
        /// Verifies that GetCurrentLocation was called.
        /// </summary>
        /// <returns>True if the method was called, otherwise false.</returns>
        public bool VerifyGetCurrentLocationCalled()
        {
            return GetCurrentLocationCallCount > 0;
        }
        
        /// <summary>
        /// Gets the history of locations that have been simulated.
        /// </summary>
        /// <returns>The list of historical locations.</returns>
        public IReadOnlyList<LocationModel> GetLocationHistory()
        {
            return LocationHistory.AsReadOnly();
        }
        
        /// <summary>
        /// Resets all configurations and call history.
        /// </summary>
        public void Reset()
        {
            _isTracking = false;
            CurrentLocation = TestLocations.DefaultLocationModel;
            _locationSimulator.StopSimulation();
            ShouldSucceed = true;
            ShouldThrowException = false;
            ExceptionToThrow = null;
            LocationHistory.Clear();
            StartTrackingCallCount = 0;
            StopTrackingCallCount = 0;
            GetCurrentLocationCallCount = 0;
        }
        
        /// <summary>
        /// Disposes the MockLocationService and releases resources.
        /// </summary>
        public void Dispose()
        {
            _locationSimulator.LocationChanged -= OnLocationChanged;
            _locationSimulator.StopSimulation();
        }
    }
}