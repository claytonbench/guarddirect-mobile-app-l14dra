using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using SecurityPatrol.Services;
using SecurityPatrol.Models;
using SecurityPatrol.TestCommon.Data;
using SecurityPatrol.Helpers;

namespace SecurityPatrol.TestCommon.Mocks
{
    /// <summary>
    /// Mock implementation of IGeofenceService for testing purposes that simulates geofencing functionality
    /// without accessing actual device GPS.
    /// </summary>
    public class MockGeofenceService : IGeofenceService
    {
        // IGeofenceService interface implementation
        public event EventHandler<CheckpointProximityEventArgs> ProximityChanged;
        public double ProximityThresholdFeet { get; set; } = 50; // Default value

        // Internal state
        private List<CheckpointModel> _checkpoints = new List<CheckpointModel>();
        private Dictionary<int, bool> _proximityStatus = new Dictionary<int, bool>();
        private bool _isMonitoring = false;

        // Test configuration
        public bool ShouldSucceed { get; set; } = true;
        public bool ShouldThrowException { get; set; } = false;
        public Exception ExceptionToThrow { get; set; } = null;

        // Call counters for verification
        public int StartMonitoringCallCount { get; private set; } = 0;
        public int StopMonitoringCallCount { get; private set; } = 0;
        public int CheckProximityCallCount { get; private set; } = 0;

        // Checkpoint distance simulation
        public Dictionary<int, double> CheckpointDistances { get; set; } = new Dictionary<int, double>();

        /// <summary>
        /// Mocks starting checkpoint proximity monitoring
        /// </summary>
        /// <param name="checkpoints">The collection of checkpoints to monitor</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task StartMonitoring(IEnumerable<CheckpointModel> checkpoints)
        {
            if (ShouldThrowException && ExceptionToThrow != null)
            {
                throw ExceptionToThrow;
            }

            StartMonitoringCallCount++;

            if (ShouldSucceed)
            {
                _checkpoints.Clear();
                _checkpoints.AddRange(checkpoints);
                _proximityStatus.Clear();
                
                // Initialize all checkpoints as not in proximity
                foreach (var checkpoint in _checkpoints)
                {
                    _proximityStatus[checkpoint.Id] = false;
                }
                
                _isMonitoring = true;
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Mocks stopping checkpoint proximity monitoring
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task StopMonitoring()
        {
            if (ShouldThrowException && ExceptionToThrow != null)
            {
                throw ExceptionToThrow;
            }

            StopMonitoringCallCount++;

            if (ShouldSucceed)
            {
                _isMonitoring = false;
                _checkpoints.Clear();
                _proximityStatus.Clear();
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Mocks checking proximity to all monitored checkpoints based on the provided coordinates
        /// </summary>
        /// <param name="latitude">The latitude coordinate to check</param>
        /// <param name="longitude">The longitude coordinate to check</param>
        /// <returns>A task that returns the IDs of checkpoints within proximity</returns>
        public async Task<IEnumerable<int>> CheckProximity(double latitude, double longitude)
        {
            if (ShouldThrowException && ExceptionToThrow != null)
            {
                throw ExceptionToThrow;
            }

            CheckProximityCallCount++;

            if (!_isMonitoring)
            {
                return Enumerable.Empty<int>();
            }

            var proximalCheckpoints = new List<int>();
            double proximityThresholdMeters = LocationHelper.ConvertFeetToMeters(ProximityThresholdFeet);

            foreach (var checkpoint in _checkpoints)
            {
                // Get the distance - either from configured distances or calculate it
                double distance;
                if (CheckpointDistances.ContainsKey(checkpoint.Id))
                {
                    distance = CheckpointDistances[checkpoint.Id];
                }
                else
                {
                    distance = LocationHelper.CalculateDistance(checkpoint.Latitude, checkpoint.Longitude, latitude, longitude);
                }

                // Check if checkpoint is within range (converted to meters for comparison)
                bool isInRange = distance <= proximityThresholdMeters;
                
                // Check if proximity status changed
                if (_proximityStatus.ContainsKey(checkpoint.Id) && _proximityStatus[checkpoint.Id] != isInRange)
                {
                    // Raise proximity changed event
                    OnProximityChanged(checkpoint.Id, distance, isInRange);
                }
                
                // Update proximity status
                _proximityStatus[checkpoint.Id] = isInRange;
                
                // Add to result if in range
                if (isInRange)
                {
                    proximalCheckpoints.Add(checkpoint.Id);
                }
            }

            return proximalCheckpoints;
        }

        /// <summary>
        /// Raises the ProximityChanged event with the specified checkpoint ID, distance, and proximity status
        /// </summary>
        /// <param name="checkpointId">The ID of the checkpoint</param>
        /// <param name="distance">The distance to the checkpoint</param>
        /// <param name="isInRange">Whether the checkpoint is within range</param>
        private void OnProximityChanged(int checkpointId, double distance, bool isInRange)
        {
            var handler = ProximityChanged;
            if (handler != null)
            {
                var args = new CheckpointProximityEventArgs(checkpointId, distance, isInRange);
                handler(this, args);
            }
        }

        /// <summary>
        /// Gets a value indicating whether checkpoint monitoring is currently active
        /// </summary>
        /// <returns>True if monitoring is active, false otherwise</returns>
        public bool IsMonitoring()
        {
            return _isMonitoring;
        }

        /// <summary>
        /// Configures an exception to be thrown by any method
        /// </summary>
        /// <param name="exception">The exception to throw</param>
        public void SetupException(Exception exception)
        {
            ShouldThrowException = true;
            ExceptionToThrow = exception;
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
        /// Configures the distance to be returned for a specific checkpoint
        /// </summary>
        /// <param name="checkpointId">The ID of the checkpoint</param>
        /// <param name="distance">The distance to return</param>
        public void SetupCheckpointDistance(int checkpointId, double distance)
        {
            CheckpointDistances[checkpointId] = distance;
        }

        /// <summary>
        /// Configures the proximity threshold distance in feet
        /// </summary>
        /// <param name="thresholdFeet">The threshold value in feet</param>
        public void SetupProximityThreshold(double thresholdFeet)
        {
            ProximityThresholdFeet = thresholdFeet;
        }

        /// <summary>
        /// Simulates a proximity change event for a checkpoint
        /// </summary>
        /// <param name="checkpointId">The ID of the checkpoint</param>
        /// <param name="distance">The distance to the checkpoint</param>
        /// <param name="isInRange">Whether the checkpoint is within range</param>
        public void SimulateProximityChanged(int checkpointId, double distance, bool isInRange)
        {
            _proximityStatus[checkpointId] = isInRange;
            OnProximityChanged(checkpointId, distance, isInRange);
        }

        /// <summary>
        /// Verifies that StartMonitoring was called
        /// </summary>
        /// <returns>True if the method was called, otherwise false</returns>
        public bool VerifyStartMonitoringCalled()
        {
            return StartMonitoringCallCount > 0;
        }

        /// <summary>
        /// Verifies that StopMonitoring was called
        /// </summary>
        /// <returns>True if the method was called, otherwise false</returns>
        public bool VerifyStopMonitoringCalled()
        {
            return StopMonitoringCallCount > 0;
        }

        /// <summary>
        /// Verifies that CheckProximity was called
        /// </summary>
        /// <returns>True if the method was called, otherwise false</returns>
        public bool VerifyCheckProximityCalled()
        {
            return CheckProximityCallCount > 0;
        }

        /// <summary>
        /// Gets the list of checkpoints currently being monitored
        /// </summary>
        /// <returns>The list of monitored checkpoints</returns>
        public IReadOnlyList<CheckpointModel> GetMonitoredCheckpoints()
        {
            return _checkpoints.AsReadOnly();
        }

        /// <summary>
        /// Gets the current proximity status for a specific checkpoint
        /// </summary>
        /// <param name="checkpointId">The ID of the checkpoint</param>
        /// <returns>True if the checkpoint is in proximity, otherwise false</returns>
        public bool GetProximityStatus(int checkpointId)
        {
            return _proximityStatus.ContainsKey(checkpointId) && _proximityStatus[checkpointId];
        }

        /// <summary>
        /// Resets all configurations and call history
        /// </summary>
        public void Reset()
        {
            _isMonitoring = false;
            _checkpoints.Clear();
            _proximityStatus.Clear();
            CheckpointDistances.Clear();
            ProximityThresholdFeet = 50;
            ShouldSucceed = true;
            ShouldThrowException = false;
            ExceptionToThrow = null;
            StartMonitoringCallCount = 0;
            StopMonitoringCallCount = 0;
            CheckProximityCallCount = 0;
        }
    }
}