using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SecurityPatrol.Models;
using SecurityPatrol.Services;

namespace SecurityPatrol.UnitTests.Helpers.MockServices
{
    /// <summary>
    /// Mock implementation of IPatrolService for unit testing that provides configurable responses for patrol management operations
    /// without accessing actual device GPS or map services, allowing tests to run in isolation and with predictable patrol data.
    /// </summary>
    public class MockPatrolService : IPatrolService
    {
        // Private fields to track state
        private bool _isPatrolActive;
        private int? _currentLocationId;
        private double _proximityThresholdFeet;

        // Configurable data
        public List<LocationModel> Locations { get; private set; }
        public Dictionary<int, List<CheckpointModel>> CheckpointsByLocation { get; private set; }
        public PatrolStatus CurrentPatrolStatus { get; private set; }

        // Configurable results
        public bool StartPatrolResult { get; private set; }
        public bool EndPatrolResult { get; private set; }
        public bool VerifyCheckpointResult { get; private set; }

        // Exception simulation
        public bool ShouldThrowException { get; private set; }
        public Exception ExceptionToThrow { get; private set; }

        // Call counting for verification
        public int GetLocationsCallCount { get; private set; }
        public int GetCheckpointsCallCount { get; private set; }
        public int VerifyCheckpointCallCount { get; private set; }
        public int GetPatrolStatusCallCount { get; private set; }
        public int StartPatrolCallCount { get; private set; }
        public int EndPatrolCallCount { get; private set; }
        public int CheckProximityCallCount { get; private set; }

        // Track verified checkpoints
        public List<int> VerifiedCheckpointIds { get; private set; }
        
        // Checkpoint distances for proximity simulation
        public Dictionary<int, double> CheckpointDistances { get; private set; }

        // IPatrolService properties implementation
        public bool IsPatrolActive => _isPatrolActive;
        public int? CurrentLocationId => _currentLocationId;
        public double ProximityThresholdFeet
        {
            get => _proximityThresholdFeet;
            set => _proximityThresholdFeet = value;
        }

        // IPatrolService event implementation
        public event EventHandler<CheckpointProximityEventArgs> CheckpointProximityChanged;

        /// <summary>
        /// Initializes a new instance of the MockPatrolService class with default settings
        /// </summary>
        public MockPatrolService()
        {
            // Initialize default state
            _isPatrolActive = false;
            _currentLocationId = null;
            _proximityThresholdFeet = 50.0; // Default 50 feet
            
            // Initialize collections
            Locations = new List<LocationModel>();
            CheckpointsByLocation = new Dictionary<int, List<CheckpointModel>>();
            CurrentPatrolStatus = null;
            StartPatrolResult = true;
            EndPatrolResult = true;
            VerifyCheckpointResult = true;
            ShouldThrowException = false;
            ExceptionToThrow = null;
            GetLocationsCallCount = 0;
            GetCheckpointsCallCount = 0;
            VerifyCheckpointCallCount = 0;
            GetPatrolStatusCallCount = 0;
            StartPatrolCallCount = 0;
            EndPatrolCallCount = 0;
            CheckProximityCallCount = 0;
            VerifiedCheckpointIds = new List<int>();
            CheckpointDistances = new Dictionary<int, double>();
        }

        /// <summary>
        /// Mocks retrieving all available patrol locations
        /// </summary>
        /// <returns>A task that returns the configured collection of patrol locations</returns>
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
        /// <returns>A task that returns the configured collection of checkpoints for the specified location</returns>
        public async Task<IEnumerable<CheckpointModel>> GetCheckpoints(int locationId)
        {
            if (ShouldThrowException)
                throw ExceptionToThrow;
            
            GetCheckpointsCallCount++;
            
            if (CheckpointsByLocation.ContainsKey(locationId))
                return CheckpointsByLocation[locationId];
            
            return new List<CheckpointModel>();
        }

        /// <summary>
        /// Mocks verifying a checkpoint as completed
        /// </summary>
        /// <param name="checkpointId">The checkpoint identifier</param>
        /// <returns>A task that returns the configured verification result</returns>
        public async Task<bool> VerifyCheckpoint(int checkpointId)
        {
            if (ShouldThrowException)
                throw ExceptionToThrow;
            
            VerifyCheckpointCallCount++;
            
            if (!_isPatrolActive)
                return false;
            
            if (VerifyCheckpointResult)
            {
                if (!VerifiedCheckpointIds.Contains(checkpointId))
                    VerifiedCheckpointIds.Add(checkpointId);
                
                // Update patrol status if it exists
                if (CurrentPatrolStatus != null)
                {
                    CurrentPatrolStatus.UpdateProgress(VerifiedCheckpointIds.Count);
                }
                
                // Find and mark the checkpoint as verified
                foreach (var checkpoints in CheckpointsByLocation.Values)
                {
                    var checkpoint = checkpoints.FirstOrDefault(c => c.Id == checkpointId);
                    if (checkpoint != null)
                    {
                        checkpoint.MarkAsVerified();
                        break;
                    }
                }
            }
            
            return VerifyCheckpointResult;
        }

        /// <summary>
        /// Mocks retrieving the current status of a patrol for a specific location
        /// </summary>
        /// <param name="locationId">The location identifier</param>
        /// <returns>A task that returns the configured patrol status for the specified location</returns>
        public async Task<PatrolStatus> GetPatrolStatus(int locationId)
        {
            if (ShouldThrowException)
                throw ExceptionToThrow;
            
            GetPatrolStatusCallCount++;
            
            if (CurrentPatrolStatus != null && CurrentPatrolStatus.LocationId == locationId)
                return CurrentPatrolStatus.Clone();
            
            // Create a new status if none exists for this location
            return new PatrolStatus
            {
                LocationId = locationId,
                TotalCheckpoints = 0,
                VerifiedCheckpoints = 0,
                StartTime = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Mocks starting a new patrol for the specified location
        /// </summary>
        /// <param name="locationId">The location identifier</param>
        /// <returns>A task that returns the configured initial patrol status</returns>
        public async Task<PatrolStatus> StartPatrol(int locationId)
        {
            if (ShouldThrowException)
                throw ExceptionToThrow;
            
            StartPatrolCallCount++;
            
            if (StartPatrolResult)
            {
                _isPatrolActive = true;
                _currentLocationId = locationId;
                
                var status = new PatrolStatus
                {
                    LocationId = locationId,
                    StartTime = DateTime.UtcNow,
                    VerifiedCheckpoints = 0
                };
                
                if (CheckpointsByLocation.ContainsKey(locationId))
                {
                    status.TotalCheckpoints = CheckpointsByLocation[locationId].Count;
                }
                
                CurrentPatrolStatus = status;
            }
            
            return CurrentPatrolStatus?.Clone();
        }

        /// <summary>
        /// Mocks ending the current patrol
        /// </summary>
        /// <returns>A task that returns the configured final patrol status</returns>
        public async Task<PatrolStatus> EndPatrol()
        {
            if (ShouldThrowException)
                throw ExceptionToThrow;
            
            EndPatrolCallCount++;
            
            if (!_isPatrolActive)
                return null;
            
            if (EndPatrolResult)
            {
                _isPatrolActive = false;
                
                if (CurrentPatrolStatus != null)
                {
                    CurrentPatrolStatus.CompletePatrol();
                }
                
                var finalStatus = CurrentPatrolStatus?.Clone();
                _currentLocationId = null;
                return finalStatus;
            }
            
            return CurrentPatrolStatus?.Clone();
        }

        /// <summary>
        /// Mocks checking proximity to all checkpoints based on the provided coordinates
        /// </summary>
        /// <param name="latitude">The latitude coordinate</param>
        /// <param name="longitude">The longitude coordinate</param>
        /// <returns>A task that returns the configured IDs of checkpoints within proximity</returns>
        public async Task<IEnumerable<int>> CheckProximity(double latitude, double longitude)
        {
            if (ShouldThrowException)
                throw ExceptionToThrow;
            
            CheckProximityCallCount++;
            
            if (!_isPatrolActive)
                return new List<int>();
            
            var checkpointsInRange = new List<int>();
            
            // Add checkpoints that are configured to be within range
            foreach (var kvp in CheckpointDistances)
            {
                if (kvp.Value <= _proximityThresholdFeet)
                {
                    checkpointsInRange.Add(kvp.Key);
                }
            }
            
            return checkpointsInRange;
        }

        /// <summary>
        /// Simulates a checkpoint proximity change event
        /// </summary>
        /// <param name="checkpointId">The checkpoint ID that triggered the proximity event</param>
        /// <param name="distance">The distance to the checkpoint in feet</param>
        /// <param name="isInRange">Whether the checkpoint is within range</param>
        public void SimulateCheckpointProximityChanged(int checkpointId, double distance, bool isInRange)
        {
            // Update the distance in our dictionary
            CheckpointDistances[checkpointId] = distance;
            
            // Raise the event if there are subscribers
            CheckpointProximityChanged?.Invoke(this, new CheckpointProximityEventArgs(checkpointId, distance, isInRange));
        }

        /// <summary>
        /// Configures the locations to be returned by GetLocations
        /// </summary>
        /// <param name="locations">The list of locations to return</param>
        public void SetupLocations(List<LocationModel> locations)
        {
            Locations.Clear();
            Locations.AddRange(locations);
        }

        /// <summary>
        /// Configures the checkpoints to be returned by GetCheckpoints for a specific location
        /// </summary>
        /// <param name="locationId">The location ID</param>
        /// <param name="checkpoints">The list of checkpoints to return</param>
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
        /// Configures the patrol status to be returned by GetPatrolStatus
        /// </summary>
        /// <param name="status">The patrol status to return</param>
        public void SetupPatrolStatus(PatrolStatus status)
        {
            CurrentPatrolStatus = status;
        }

        /// <summary>
        /// Configures the result for the StartPatrol method
        /// </summary>
        /// <param name="result">The result to return</param>
        public void SetupStartPatrolResult(bool result)
        {
            StartPatrolResult = result;
        }

        /// <summary>
        /// Configures the result for the EndPatrol method
        /// </summary>
        /// <param name="result">The result to return</param>
        public void SetupEndPatrolResult(bool result)
        {
            EndPatrolResult = result;
        }

        /// <summary>
        /// Configures the result for the VerifyCheckpoint method
        /// </summary>
        /// <param name="result">The result to return</param>
        public void SetupVerifyCheckpointResult(bool result)
        {
            VerifyCheckpointResult = result;
        }

        /// <summary>
        /// Configures the distance for a specific checkpoint
        /// </summary>
        /// <param name="checkpointId">The checkpoint ID</param>
        /// <param name="distance">The distance in feet</param>
        public void SetupCheckpointDistance(int checkpointId, double distance)
        {
            if (CheckpointDistances.ContainsKey(checkpointId))
            {
                CheckpointDistances[checkpointId] = distance;
            }
            else
            {
                CheckpointDistances.Add(checkpointId, distance);
            }
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
        /// Gets the IDs of checkpoints that have been verified
        /// </summary>
        /// <returns>The list of verified checkpoint IDs</returns>
        public IReadOnlyList<int> GetVerifiedCheckpointIds()
        {
            return VerifiedCheckpointIds.AsReadOnly();
        }

        /// <summary>
        /// Resets all configurations and call history
        /// </summary>
        public void Reset()
        {
            _isPatrolActive = false;
            _currentLocationId = null;
            _proximityThresholdFeet = 50.0;
            Locations.Clear();
            CheckpointsByLocation.Clear();
            CurrentPatrolStatus = null;
            StartPatrolResult = true;
            EndPatrolResult = true;
            VerifyCheckpointResult = true;
            ShouldThrowException = false;
            ExceptionToThrow = null;
            GetLocationsCallCount = 0;
            GetCheckpointsCallCount = 0;
            VerifyCheckpointCallCount = 0;
            GetPatrolStatusCallCount = 0;
            StartPatrolCallCount = 0;
            EndPatrolCallCount = 0;
            CheckProximityCallCount = 0;
            VerifiedCheckpointIds.Clear();
            CheckpointDistances.Clear();
        }
    }
}