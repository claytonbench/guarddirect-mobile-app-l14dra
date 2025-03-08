using System;  // Version 8.0.0
using System.Collections.Generic;  // Version 8.0.0
using System.Threading.Tasks;  // Version 8.0.0
using SecurityPatrol.Models;
using SecurityPatrol.Services;

namespace SecurityPatrol.UnitTests.Helpers.MockServices
{
    /// <summary>
    /// Mock implementation of ILocationService for unit testing that provides configurable responses for location tracking operations
    /// without accessing actual device GPS, allowing tests to run in isolation and with predictable location data.
    /// </summary>
    public class MockLocationService : ILocationService
    {
        private bool _isTracking;
        
        /// <summary>
        /// Gets or sets the current mock location that will be returned by GetCurrentLocation
        /// </summary>
        public LocationModel CurrentLocation { get; private set; }
        
        /// <summary>
        /// Gets or sets whether StartTracking should succeed in changing the tracking state
        /// </summary>
        public bool StartTrackingResult { get; private set; }
        
        /// <summary>
        /// Gets or sets whether StopTracking should succeed in changing the tracking state
        /// </summary>
        public bool StopTrackingResult { get; private set; }
        
        /// <summary>
        /// Gets or sets whether methods should throw an exception
        /// </summary>
        public bool ShouldThrowException { get; private set; }
        
        /// <summary>
        /// Gets or sets the exception that should be thrown if ShouldThrowException is true
        /// </summary>
        public Exception ExceptionToThrow { get; private set; }
        
        /// <summary>
        /// Gets the history of locations that have been simulated
        /// </summary>
        public List<LocationModel> LocationHistory { get; private set; }
        
        /// <summary>
        /// Gets the number of times StartTracking has been called
        /// </summary>
        public int StartTrackingCallCount { get; private set; }
        
        /// <summary>
        /// Gets the number of times StopTracking has been called
        /// </summary>
        public int StopTrackingCallCount { get; private set; }
        
        /// <summary>
        /// Gets the number of times GetCurrentLocation has been called
        /// </summary>
        public int GetCurrentLocationCallCount { get; private set; }
        
        /// <summary>
        /// Event that is raised when a location change is simulated
        /// </summary>
        public event EventHandler<LocationChangedEventArgs> LocationChanged;

        /// <summary>
        /// Initializes a new instance of the MockLocationService class with default settings
        /// </summary>
        public MockLocationService()
        {
            _isTracking = false;
            CurrentLocation = new LocationModel { Latitude = 0, Longitude = 0, Accuracy = 10, Timestamp = DateTime.UtcNow };
            StartTrackingResult = true;
            StopTrackingResult = true;
            ShouldThrowException = false;
            ExceptionToThrow = null;
            LocationHistory = new List<LocationModel>();
            StartTrackingCallCount = 0;
            StopTrackingCallCount = 0;
            GetCurrentLocationCallCount = 0;
        }

        /// <summary>
        /// Mocks starting location tracking
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task StartTracking()
        {
            if (ShouldThrowException && ExceptionToThrow != null)
                throw ExceptionToThrow;
                
            StartTrackingCallCount++;
            
            if (StartTrackingResult)
                _isTracking = true;
                
            await Task.CompletedTask;
        }

        /// <summary>
        /// Mocks stopping location tracking
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task StopTracking()
        {
            if (ShouldThrowException && ExceptionToThrow != null)
                throw ExceptionToThrow;
                
            StopTrackingCallCount++;
            
            if (StopTrackingResult)
                _isTracking = false;
                
            await Task.CompletedTask;
        }

        /// <summary>
        /// Mocks getting the current device location
        /// </summary>
        /// <returns>A task that returns the current mock location</returns>
        public async Task<LocationModel> GetCurrentLocation()
        {
            if (ShouldThrowException && ExceptionToThrow != null)
                throw ExceptionToThrow;
                
            GetCurrentLocationCallCount++;
            
            return await Task.FromResult(CurrentLocation);
        }

        /// <summary>
        /// Gets a value indicating whether location tracking is currently active
        /// </summary>
        public bool IsTracking
        {
            get { return _isTracking; }
        }

        /// <summary>
        /// Simulates a location change event
        /// </summary>
        /// <param name="location">The new location</param>
        public void SimulateLocationChanged(LocationModel location)
        {
            if (location == null)
                throw new ArgumentNullException(nameof(location));
                
            CurrentLocation = location;
            LocationHistory.Add(location);
            
            if (_isTracking && LocationChanged != null)
            {
                LocationChanged(this, new LocationChangedEventArgs(location));
            }
        }
        
        /// <summary>
        /// Configures the result for the StartTracking method
        /// </summary>
        /// <param name="result">Whether StartTracking should succeed</param>
        public void SetupStartTrackingResult(bool result)
        {
            StartTrackingResult = result;
        }
        
        /// <summary>
        /// Configures the result for the StopTracking method
        /// </summary>
        /// <param name="result">Whether StopTracking should succeed</param>
        public void SetupStopTrackingResult(bool result)
        {
            StopTrackingResult = result;
        }
        
        /// <summary>
        /// Configures the current location to be returned
        /// </summary>
        /// <param name="location">The location to return</param>
        public void SetupCurrentLocation(LocationModel location)
        {
            CurrentLocation = location ?? throw new ArgumentNullException(nameof(location));
        }
        
        /// <summary>
        /// Configures an exception to be thrown by any method
        /// </summary>
        /// <param name="exception">The exception to throw</param>
        public void SetupException(Exception exception)
        {
            ShouldThrowException = true;
            ExceptionToThrow = exception ?? throw new ArgumentNullException(nameof(exception));
        }
        
        /// <summary>
        /// Clears any configured exception
        /// </summary>
        public void ClearException()
        {
            ShouldThrowException = false;
            ExceptionToThrow = null;
        }
        
        /// <summary>
        /// Verifies that StartTracking was called
        /// </summary>
        /// <returns>True if the method was called, otherwise false</returns>
        public bool VerifyStartTrackingCalled()
        {
            return StartTrackingCallCount > 0;
        }
        
        /// <summary>
        /// Verifies that StopTracking was called
        /// </summary>
        /// <returns>True if the method was called, otherwise false</returns>
        public bool VerifyStopTrackingCalled()
        {
            return StopTrackingCallCount > 0;
        }
        
        /// <summary>
        /// Verifies that GetCurrentLocation was called
        /// </summary>
        /// <returns>True if the method was called, otherwise false</returns>
        public bool VerifyGetCurrentLocationCalled()
        {
            return GetCurrentLocationCallCount > 0;
        }
        
        /// <summary>
        /// Gets the history of locations that have been simulated
        /// </summary>
        /// <returns>The list of historical locations</returns>
        public IReadOnlyList<LocationModel> GetLocationHistory()
        {
            return LocationHistory.AsReadOnly();
        }
        
        /// <summary>
        /// Resets all configurations and call history
        /// </summary>
        public void Reset()
        {
            _isTracking = false;
            CurrentLocation = new LocationModel { Latitude = 0, Longitude = 0, Accuracy = 10, Timestamp = DateTime.UtcNow };
            StartTrackingResult = true;
            StopTrackingResult = true;
            ShouldThrowException = false;
            ExceptionToThrow = null;
            LocationHistory.Clear();
            StartTrackingCallCount = 0;
            StopTrackingCallCount = 0;
            GetCurrentLocationCallCount = 0;
        }
    }
}