using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SecurityPatrol.Models;
using SecurityPatrol.Services;
using SecurityPatrol.TestCommon.Constants;
using SecurityPatrol.TestCommon.Data;

namespace SecurityPatrol.TestCommon.Mocks
{
    /// <summary>
    /// Mock implementation of IPatrolService for testing purposes that simulates patrol management functionality
    /// without accessing actual repositories or services.
    /// </summary>
    public class MockPatrolService : IPatrolService, IDisposable
    {
        private bool _isPatrolActive;
        private int? _currentLocationId;
        private double _proximityThresholdFeet = 50.0;

        // Test data
        public List<LocationModel> Locations { get; set; }
        public Dictionary<int, List<CheckpointModel>> CheckpointsByLocation { get; set; }
        public List<CheckpointModel> ActiveCheckpoints { get; set; }
        public PatrolStatus CurrentPatrolStatus { get; set; }
        public Dictionary<int, bool> CheckpointProximityStatus { get; set; }

        // Configuration properties
        public bool ShouldSucceed { get; set; } = true;
        public bool ShouldThrowException { get; set; } = false;
        public Exception ExceptionToThrow { get; set; }

        // Call counters
        public int GetLocationsCallCount { get; private set; }
        public int GetCheckpointsCallCount { get; private set; }
        public int VerifyCheckpointCallCount { get; private set; }
        public int GetPatrolStatusCallCount { get; private set; }
        public int StartPatrolCallCount { get; private set; }
        public int EndPatrolCallCount { get; private set; }
        public int CheckProximityCallCount { get; private set; }

        // Tracked data
        public List<int> VerifiedCheckpoints { get; private set; }

        // IPatrolService implementation
        public bool IsPatrolActive => _isPatrolActive;
        public int? CurrentLocationId => _currentLocationId;
        public double ProximityThresholdFeet
        {
            get => _proximityThresholdFeet;
            set => _proximityThresholdFeet = value;
        }

        public event EventHandler<CheckpointProximityEventArgs> CheckpointProximityChanged;

        /// <summary>
        /// Initializes a new instance of the MockPatrolService class with default settings
        /// </summary>
        public MockPatrolService()
        {
            // Initialize properties
            _isPatrolActive = false;
            _currentLocationId = null;
            Locations = TestLocations.LocationModels ?? new List<LocationModel>();
            CheckpointsByLocation = new Dictionary<int, List<CheckpointModel>>();
            ActiveCheckpoints = new List<CheckpointModel>();
            CurrentPatrolStatus = null;
            CheckpointProximityStatus = new Dictionary<int, bool>();
            VerifiedCheckpoints = new List<int>();

            // Reset call counters
            GetLocationsCallCount = 0;
            GetCheckpointsCallCount = 0;
            VerifyCheckpointCallCount = 0;
            GetPatrolStatusCallCount = 0;
            StartPatrolCallCount = 0;
            EndPatrolCallCount = 0;
            CheckProximityCallCount = 0;

            // Set up test data for each location
            foreach (var location in Locations)
            {
                CheckpointsByLocation[location.Id] = TestCheckpoints.GenerateCheckpointModels(location.Id, 5);
            }
        }

        /// <summary>
        /// Mocks retrieving all available patrol locations
        /// </summary>
        /// <returns>A task that returns a collection of patrol locations</returns>
        public async Task<IEnumerable<LocationModel>> GetLocations()
        {
            if (ShouldThrowException)
                throw ExceptionToThrow;

            GetLocationsCallCount++;
            return Locations;
        }

        /// <summary>
        /// Mocks retrieving all checkpoints for a specific patrol location
        /// </summary>
        /// <param name="locationId">The location identifier</param>
        /// <returns>A task that returns a collection of checkpoints for the specified location</returns>
        public async Task<IEnumerable<CheckpointModel>> GetCheckpoints(int locationId)
        {
            if (ShouldThrowException)
                throw ExceptionToThrow;

            GetCheckpointsCallCount++;

            if (CheckpointsByLocation.ContainsKey(locationId))
            {
                return CheckpointsByLocation[locationId];
            }

            return new List<CheckpointModel>();
        }

        /// <summary>
        /// Mocks verifying a checkpoint as completed
        /// </summary>
        /// <param name="checkpointId">The checkpoint identifier</param>
        /// <returns>A task that returns true if verification was successful, false otherwise</returns>
        public async Task<bool> VerifyCheckpoint(int checkpointId)
        {
            if (ShouldThrowException)
                throw ExceptionToThrow;

            VerifyCheckpointCallCount++;

            // Can't verify if patrol is not active
            if (!IsPatrolActive)
                return false;

            // Find the checkpoint in active checkpoints
            var checkpoint = ActiveCheckpoints.FirstOrDefault(c => c.Id == checkpointId);
            if (checkpoint == null)
                return false;

            // Already verified
            if (checkpoint.IsVerified)
                return true;

            // Configuration to fail
            if (!ShouldSucceed)
                return false;

            // Mark as verified
            checkpoint.MarkAsVerified();
            VerifiedCheckpoints.Add(checkpointId);

            // Update patrol status
            if (CurrentPatrolStatus != null)
            {
                CurrentPatrolStatus.VerifiedCheckpoints++;
                
                // Complete patrol if all checkpoints are verified
                if (CurrentPatrolStatus.VerifiedCheckpoints >= CurrentPatrolStatus.TotalCheckpoints)
                {
                    CurrentPatrolStatus.CompletePatrol();
                }
            }

            return true;
        }

        /// <summary>
        /// Mocks retrieving the current status of a patrol for a specific location
        /// </summary>
        /// <param name="locationId">The location identifier</param>
        /// <returns>A task that returns the patrol status for the specified location</returns>
        public async Task<PatrolStatus> GetPatrolStatus(int locationId)
        {
            if (ShouldThrowException)
                throw ExceptionToThrow;

            GetPatrolStatusCallCount++;

            if (IsPatrolActive && CurrentLocationId == locationId && CurrentPatrolStatus != null)
            {
                return CurrentPatrolStatus.Clone();
            }

            // Create a new patrol status with current verification data
            var status = new PatrolStatus
            {
                LocationId = locationId,
                TotalCheckpoints = CheckpointsByLocation.ContainsKey(locationId) ? 
                    CheckpointsByLocation[locationId].Count : 0,
                VerifiedCheckpoints = CheckpointsByLocation.ContainsKey(locationId) ? 
                    CheckpointsByLocation[locationId].Count(c => c.IsVerified) : 0,
                StartTime = DateTime.UtcNow,
                EndTime = null
            };

            return status;
        }

        /// <summary>
        /// Mocks starting a new patrol for the specified location
        /// </summary>
        /// <param name="locationId">The location identifier</param>
        /// <returns>A task that returns the initial patrol status</returns>
        public async Task<PatrolStatus> StartPatrol(int locationId)
        {
            if (ShouldThrowException)
                throw ExceptionToThrow;

            StartPatrolCallCount++;

            // End any active patrol first
            if (IsPatrolActive)
            {
                await EndPatrol();
            }

            if (!ShouldSucceed)
                return null;

            // Get checkpoints for the location
            var checkpoints = CheckpointsByLocation.ContainsKey(locationId) ?
                CheckpointsByLocation[locationId] : new List<CheckpointModel>();

            if (checkpoints.Count == 0)
                return null;

            // Set active checkpoints
            ActiveCheckpoints = checkpoints.Select(c => c.Clone()).ToList();
            
            // Initialize proximity status
            CheckpointProximityStatus.Clear();
            foreach (var checkpoint in ActiveCheckpoints)
            {
                CheckpointProximityStatus[checkpoint.Id] = false;
            }

            // Create new patrol status
            CurrentPatrolStatus = new PatrolStatus
            {
                LocationId = locationId,
                TotalCheckpoints = ActiveCheckpoints.Count,
                VerifiedCheckpoints = 0,
                StartTime = DateTime.UtcNow,
                EndTime = null
            };

            // Update state
            _isPatrolActive = true;
            _currentLocationId = locationId;
            VerifiedCheckpoints.Clear();

            return CurrentPatrolStatus.Clone();
        }

        /// <summary>
        /// Mocks ending the current patrol
        /// </summary>
        /// <returns>A task that returns the final patrol status</returns>
        public async Task<PatrolStatus> EndPatrol()
        {
            if (ShouldThrowException)
                throw ExceptionToThrow;

            EndPatrolCallCount++;

            if (!IsPatrolActive)
                return null;

            if (!ShouldSucceed)
                return null;

            // Complete the patrol
            if (CurrentPatrolStatus != null)
            {
                CurrentPatrolStatus.CompletePatrol();
            }

            // Store final status before clearing
            var finalStatus = CurrentPatrolStatus?.Clone();

            // Clear active state
            ActiveCheckpoints.Clear();
            CheckpointProximityStatus.Clear();
            _isPatrolActive = false;
            _currentLocationId = null;

            return finalStatus;
        }

        /// <summary>
        /// Mocks checking proximity to all checkpoints based on the provided coordinates
        /// </summary>
        /// <param name="latitude">The latitude coordinate</param>
        /// <param name="longitude">The longitude coordinate</param>
        /// <returns>A task that returns the IDs of checkpoints within proximity</returns>
        public async Task<IEnumerable<int>> CheckProximity(double latitude, double longitude)
        {
            if (ShouldThrowException)
                throw ExceptionToThrow;

            CheckProximityCallCount++;

            var checkpointsInRange = new List<int>();

            if (!IsPatrolActive || !ShouldSucceed)
                return checkpointsInRange;

            // For each active checkpoint, check if it's within the proximity threshold
            foreach (var checkpoint in ActiveCheckpoints)
            {
                // Calculate distance using a simple formula for testing purposes
                double distance = CalculateDistance(latitude, longitude, checkpoint.Latitude, checkpoint.Longitude);
                
                // Convert meters to feet for comparison with threshold
                double distanceFeet = distance * 3.28084;
                
                bool wasInRange = CheckpointProximityStatus.ContainsKey(checkpoint.Id) && 
                                  CheckpointProximityStatus[checkpoint.Id];
                
                bool isInRange = distanceFeet <= ProximityThresholdFeet;
                
                // Update proximity status
                CheckpointProximityStatus[checkpoint.Id] = isInRange;
                
                // If proximity status changed, raise the event
                if (wasInRange != isInRange)
                {
                    OnCheckpointProximityChanged(new CheckpointProximityEventArgs(
                        checkpoint.Id, distanceFeet, isInRange));
                }
                
                // Add to the list if in range
                if (isInRange)
                {
                    checkpointsInRange.Add(checkpoint.Id);
                }
            }

            return checkpointsInRange;
        }

        /// <summary>
        /// Simulates a proximity change event for a specific checkpoint
        /// </summary>
        /// <param name="checkpointId">The checkpoint ID</param>
        /// <param name="distance">The distance in feet</param>
        /// <param name="isInRange">Whether the checkpoint is in range</param>
        public void SimulateProximityChanged(int checkpointId, double distance, bool isInRange)
        {
            if (!IsPatrolActive)
                return;

            // Update proximity status
            CheckpointProximityStatus[checkpointId] = isInRange;
            
            // Raise event
            OnCheckpointProximityChanged(new CheckpointProximityEventArgs(checkpointId, distance, isInRange));
        }

        // Helper method to raise the proximity changed event
        protected virtual void OnCheckpointProximityChanged(CheckpointProximityEventArgs e)
        {
            CheckpointProximityChanged?.Invoke(this, e);
        }

        // Helper method to calculate distance between coordinates (simplified for testing)
        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            // This is a very simplified distance calculation for testing purposes
            // In a real implementation, you would use a proper haversine formula
            
            const double earthRadius = 6371000; // meters
            
            // Convert to radians
            double lat1Rad = lat1 * Math.PI / 180;
            double lon1Rad = lon1 * Math.PI / 180;
            double lat2Rad = lat2 * Math.PI / 180;
            double lon2Rad = lon2 * Math.PI / 180;
            
            // Calculate differences
            double dLat = lat2Rad - lat1Rad;
            double dLon = lon2Rad - lon1Rad;
            
            // Haversine formula
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            double distance = earthRadius * c;
            
            return distance;
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
        /// Configures the locations to be returned
        /// </summary>
        /// <param name="locations">The locations to return</param>
        public void SetupLocations(List<LocationModel> locations)
        {
            Locations = locations;
            
            // Update checkpoint mappings
            CheckpointsByLocation.Clear();
            foreach (var location in locations)
            {
                CheckpointsByLocation[location.Id] = TestCheckpoints.GenerateCheckpointModels(location.Id, 5);
            }
        }

        /// <summary>
        /// Configures the checkpoints for a specific location
        /// </summary>
        /// <param name="locationId">The location ID</param>
        /// <param name="checkpoints">The checkpoints to return</param>
        public void SetupCheckpoints(int locationId, List<CheckpointModel> checkpoints)
        {
            if (CheckpointsByLocation.ContainsKey(locationId))
            {
                CheckpointsByLocation[locationId] = checkpoints;
            }
            else
            {
                CheckpointsByLocation.Add(locationId, checkpoints);
            }
        }

        /// <summary>
        /// Configures the current patrol status
        /// </summary>
        /// <param name="patrolStatus">The patrol status to use</param>
        public void SetupPatrolStatus(PatrolStatus patrolStatus)
        {
            CurrentPatrolStatus = patrolStatus;
            
            if (patrolStatus != null)
            {
                _isPatrolActive = true;
                _currentLocationId = patrolStatus.LocationId;
            }
            else
            {
                _isPatrolActive = false;
                _currentLocationId = null;
            }
        }

        /// <summary>
        /// Verifies that GetLocations was called
        /// </summary>
        /// <returns>True if the method was called, otherwise false</returns>
        public bool VerifyGetLocationsCalled()
        {
            return GetLocationsCallCount > 0;
        }

        /// <summary>
        /// Verifies that GetCheckpoints was called
        /// </summary>
        /// <returns>True if the method was called, otherwise false</returns>
        public bool VerifyGetCheckpointsCalled()
        {
            return GetCheckpointsCallCount > 0;
        }

        /// <summary>
        /// Verifies that VerifyCheckpoint was called
        /// </summary>
        /// <returns>True if the method was called, otherwise false</returns>
        public bool VerifyVerifyCheckpointCalled()
        {
            return VerifyCheckpointCallCount > 0;
        }

        /// <summary>
        /// Verifies that GetPatrolStatus was called
        /// </summary>
        /// <returns>True if the method was called, otherwise false</returns>
        public bool VerifyGetPatrolStatusCalled()
        {
            return GetPatrolStatusCallCount > 0;
        }

        /// <summary>
        /// Verifies that StartPatrol was called
        /// </summary>
        /// <returns>True if the method was called, otherwise false</returns>
        public bool VerifyStartPatrolCalled()
        {
            return StartPatrolCallCount > 0;
        }

        /// <summary>
        /// Verifies that EndPatrol was called
        /// </summary>
        /// <returns>True if the method was called, otherwise false</returns>
        public bool VerifyEndPatrolCalled()
        {
            return EndPatrolCallCount > 0;
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
        /// Verifies that a specific checkpoint was verified
        /// </summary>
        /// <param name="checkpointId">The checkpoint ID</param>
        /// <returns>True if the checkpoint was verified, otherwise false</returns>
        public bool VerifyCheckpointWasVerified(int checkpointId)
        {
            return VerifiedCheckpoints.Contains(checkpointId);
        }

        /// <summary>
        /// Resets all configurations and call history
        /// </summary>
        public void Reset()
        {
            _isPatrolActive = false;
            _currentLocationId = null;
            _proximityThresholdFeet = 50.0;
            
            Locations = TestLocations.LocationModels ?? new List<LocationModel>();
            CheckpointsByLocation.Clear();
            ActiveCheckpoints.Clear();
            CurrentPatrolStatus = null;
            CheckpointProximityStatus.Clear();
            
            ShouldSucceed = true;
            ShouldThrowException = false;
            ExceptionToThrow = null;
            
            VerifiedCheckpoints.Clear();
            
            GetLocationsCallCount = 0;
            GetCheckpointsCallCount = 0;
            VerifyCheckpointCallCount = 0;
            GetPatrolStatusCallCount = 0;
            StartPatrolCallCount = 0;
            EndPatrolCallCount = 0;
            CheckProximityCallCount = 0;
            
            // Set up test data for each location
            foreach (var location in Locations)
            {
                CheckpointsByLocation[location.Id] = TestCheckpoints.GenerateCheckpointModels(location.Id, 5);
            }
        }

        /// <summary>
        /// Disposes the MockPatrolService and releases resources
        /// </summary>
        public void Dispose()
        {
            // Clean up event handlers to prevent memory leaks
            CheckpointProximityChanged = null;
            
            // Clear collections
            Locations?.Clear();
            CheckpointsByLocation?.Clear();
            ActiveCheckpoints?.Clear();
            CheckpointProximityStatus?.Clear();
            VerifiedCheckpoints?.Clear();
        }
    }
}