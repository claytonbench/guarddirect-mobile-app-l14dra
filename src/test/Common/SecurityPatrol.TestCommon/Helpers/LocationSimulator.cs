using System; // Version 8.0+
using System.Collections.Generic; // Version 8.0+
using System.Threading; // Version 8.0+
using System.Threading.Tasks; // Version 8.0+
using SecurityPatrol.Models;
using SecurityPatrol.TestCommon.Constants;
using SecurityPatrol.TestCommon.Data;

namespace SecurityPatrol.TestCommon.Helpers
{
    /// <summary>
    /// A class that simulates location updates and movement patterns for testing location-dependent features
    /// of the Security Patrol application.
    /// </summary>
    public class LocationSimulator
    {
        // Current location properties
        public double CurrentLatitude { get; private set; }
        public double CurrentLongitude { get; private set; }
        public double CurrentAccuracy { get; private set; }
        
        // Simulation control properties
        public bool IsSimulating { get; private set; }
        public int UpdateIntervalMs { get; set; }
        
        // Private fields
        private Timer _timer;
        private Queue<LocationModel> _locationQueue;
        private Random _random;
        
        // Event for location changes
        public event EventHandler<LocationChangedEventArgs> LocationChanged;
        
        /// <summary>
        /// Initializes a new instance of the LocationSimulator class with default or specified coordinates
        /// </summary>
        /// <param name="latitude">Initial latitude (defaults to TestConstants.TestLatitude if 0)</param>
        /// <param name="longitude">Initial longitude (defaults to TestConstants.TestLongitude if 0)</param>
        /// <param name="accuracy">Initial accuracy in meters (defaults to TestConstants.TestAccuracy if 0)</param>
        /// <param name="updateIntervalMs">Interval between location updates in milliseconds (defaults to 1000)</param>
        public LocationSimulator(double latitude = 0, double longitude = 0, double accuracy = 0, int updateIntervalMs = 1000)
        {
            CurrentLatitude = latitude == 0 ? TestConstants.TestLatitude : latitude;
            CurrentLongitude = longitude == 0 ? TestConstants.TestLongitude : longitude;
            CurrentAccuracy = accuracy == 0 ? TestConstants.TestAccuracy : accuracy;
            UpdateIntervalMs = updateIntervalMs;
            
            _random = new Random();
            _locationQueue = new Queue<LocationModel>();
            IsSimulating = false;
        }
        
        /// <summary>
        /// Starts the location simulation with the current settings
        /// </summary>
        public void StartSimulation()
        {
            if (IsSimulating) return;
            
            IsSimulating = true;
            _timer = new Timer(state => ProcessLocationQueue(), null, 0, UpdateIntervalMs);
        }
        
        /// <summary>
        /// Stops the ongoing location simulation
        /// </summary>
        public void StopSimulation()
        {
            if (!IsSimulating) return;
            
            IsSimulating = false;
            _timer?.Dispose();
            _timer = null;
            _locationQueue.Clear();
        }
        
        /// <summary>
        /// Adds a sequence of locations to the simulation queue
        /// </summary>
        /// <param name="locations">The list of location models to queue</param>
        public void QueueLocations(List<LocationModel> locations)
        {
            if (locations == null) throw new ArgumentNullException(nameof(locations));
            
            foreach (var location in locations)
            {
                _locationQueue.Enqueue(location);
            }
        }
        
        /// <summary>
        /// Simulates movement from current location to a target point with specified steps
        /// </summary>
        /// <param name="targetLatitude">The target latitude</param>
        /// <param name="targetLongitude">The target longitude</param>
        /// <param name="steps">The number of steps to take between current and target location</param>
        /// <returns>The generated sequence of location points</returns>
        public List<LocationModel> SimulateMovementToPoint(double targetLatitude, double targetLongitude, int steps)
        {
            var result = new List<LocationModel>();
            
            double latDiff = (targetLatitude - CurrentLatitude) / steps;
            double lonDiff = (targetLongitude - CurrentLongitude) / steps;
            
            for (int i = 1; i <= steps; i++)
            {
                double lat = CurrentLatitude + (latDiff * i);
                double lon = CurrentLongitude + (lonDiff * i);
                
                var location = new LocationModel
                {
                    Latitude = lat,
                    Longitude = lon,
                    Accuracy = CurrentAccuracy,
                    Timestamp = DateTime.UtcNow.AddSeconds(i * (UpdateIntervalMs / 1000.0))
                };
                
                result.Add(location);
                _locationQueue.Enqueue(location);
            }
            
            return result;
        }
        
        /// <summary>
        /// Simulates movement along a path defined by multiple waypoints
        /// </summary>
        /// <param name="waypoints">The list of waypoints (latitude, longitude) defining the path</param>
        /// <param name="stepsPerSegment">The number of steps to take between each pair of waypoints</param>
        /// <returns>The generated sequence of location points</returns>
        public List<LocationModel> SimulateMovementAlongPath(List<(double latitude, double longitude)> waypoints, int stepsPerSegment)
        {
            if (waypoints == null || waypoints.Count < 2)
                throw new ArgumentException("At least two waypoints are required for path simulation", nameof(waypoints));
            
            var result = new List<LocationModel>();
            
            // Start from current location to first waypoint
            result.AddRange(SimulateMovementToPoint(waypoints[0].latitude, waypoints[0].longitude, stepsPerSegment));
            
            // Continue through remaining waypoints
            for (int i = 1; i < waypoints.Count; i++)
            {
                var fromPoint = waypoints[i - 1];
                var toPoint = waypoints[i];
                
                var stepLocations = SimulateMovementToPoint(toPoint.latitude, toPoint.longitude, stepsPerSegment);
                result.AddRange(stepLocations);
            }
            
            return result;
        }
        
        /// <summary>
        /// Simulates random movement within a specified radius from the current location
        /// </summary>
        /// <param name="radiusInMeters">The maximum distance to move in meters</param>
        /// <param name="numberOfPoints">The number of random points to generate</param>
        /// <returns>The generated sequence of random location points</returns>
        public List<LocationModel> SimulateRandomMovement(double radiusInMeters, int numberOfPoints)
        {
            var result = new List<LocationModel>();
            
            for (int i = 0; i < numberOfPoints; i++)
            {
                var location = GenerateRandomLocationNearby(radiusInMeters);
                result.Add(location);
                _locationQueue.Enqueue(location);
            }
            
            return result;
        }
        
        /// <summary>
        /// Simulates approaching a checkpoint from the current location
        /// </summary>
        /// <param name="checkpointLatitude">The latitude of the checkpoint</param>
        /// <param name="checkpointLongitude">The longitude of the checkpoint</param>
        /// <param name="steps">The number of steps to take for the approach</param>
        /// <param name="finalDistanceInMeters">The final distance to stop from the checkpoint in meters</param>
        /// <returns>The generated sequence of location points approaching the checkpoint</returns>
        public List<LocationModel> SimulateCheckpointApproach(double checkpointLatitude, double checkpointLongitude, int steps, double finalDistanceInMeters)
        {
            // Calculate bearing to checkpoint (simplified approach)
            double dLon = checkpointLongitude - CurrentLongitude;
            double y = Math.Sin(dLon) * Math.Cos(checkpointLatitude);
            double x = Math.Cos(CurrentLatitude) * Math.Sin(checkpointLatitude) -
                      Math.Sin(CurrentLatitude) * Math.Cos(checkpointLatitude) * Math.Cos(dLon);
            double bearingRadians = Math.Atan2(y, x);
            
            // Calculate distance to checkpoint (simplified approximation using Euclidean distance for testing)
            double latDiff = checkpointLatitude - CurrentLatitude;
            double lonDiff = checkpointLongitude - CurrentLongitude;
            double distance = Math.Sqrt(latDiff * latDiff + lonDiff * lonDiff) * 111000; // Rough conversion to meters
            
            // Calculate approach distance (distance - final distance)
            double approachDistance = Math.Max(0, distance - finalDistanceInMeters);
            
            // Generate waypoints along the approach path
            var waypoints = new List<(double latitude, double longitude)>();
            
            // Starting point
            waypoints.Add((CurrentLatitude, CurrentLongitude));
            
            // Ending point (finalDistanceInMeters away from checkpoint)
            double ratio = approachDistance / distance;
            double endLat = CurrentLatitude + (latDiff * ratio);
            double endLon = CurrentLongitude + (lonDiff * ratio);
            waypoints.Add((endLat, endLon));
            
            // Use path movement to simulate the approach
            return SimulateMovementAlongPath(waypoints, steps);
        }
        
        /// <summary>
        /// Gets the current simulated location as a LocationModel
        /// </summary>
        /// <returns>A LocationModel representing the current simulated location</returns>
        public LocationModel GetCurrentLocation()
        {
            return new LocationModel
            {
                Latitude = CurrentLatitude,
                Longitude = CurrentLongitude,
                Accuracy = CurrentAccuracy,
                Timestamp = DateTime.UtcNow
            };
        }
        
        /// <summary>
        /// Emits a LocationChanged event with the current or specified location
        /// </summary>
        /// <param name="location">Optional location to emit; if null, uses current location</param>
        public void EmitLocationChanged(LocationModel location = null)
        {
            if (location == null)
            {
                location = GetCurrentLocation();
            }
            
            // Update current location
            CurrentLatitude = location.Latitude;
            CurrentLongitude = location.Longitude;
            
            // Emit event if there are subscribers
            LocationChanged?.Invoke(this, new LocationChangedEventArgs(location));
        }
        
        /// <summary>
        /// Processes the next location in the queue and emits a location change event
        /// </summary>
        private void ProcessLocationQueue()
        {
            LocationModel location;
            
            if (_locationQueue.Count == 0)
            {
                // If queue is empty, generate a small random movement
                location = GenerateRandomLocationNearby(10); // 10 meters
            }
            else
            {
                // Otherwise, get the next location from the queue
                location = _locationQueue.Dequeue();
            }
            
            EmitLocationChanged(location);
        }
        
        /// <summary>
        /// Generates a random location near the current location
        /// </summary>
        /// <param name="maxDistanceInMeters">The maximum distance to generate in meters</param>
        /// <returns>A randomly generated location near the current location</returns>
        private LocationModel GenerateRandomLocationNearby(double maxDistanceInMeters)
        {
            // Generate random angle and distance
            double angle = _random.NextDouble() * 2 * Math.PI; // Random angle in radians
            double distance = _random.NextDouble() * maxDistanceInMeters; // Random distance up to max
            
            // Convert to latitude/longitude offsets
            double latOffset = ConvertMetersToLatitudeDegrees(distance * Math.Cos(angle));
            double lonOffset = ConvertMetersToLongitudeDegrees(distance * Math.Sin(angle), CurrentLatitude);
            
            // Create location
            return new LocationModel
            {
                Latitude = CurrentLatitude + latOffset,
                Longitude = CurrentLongitude + lonOffset,
                Accuracy = CurrentAccuracy,
                Timestamp = DateTime.UtcNow
            };
        }
        
        /// <summary>
        /// Converts a distance in meters to approximate latitude degrees
        /// </summary>
        /// <param name="meters">The distance in meters</param>
        /// <returns>The equivalent latitude degrees</returns>
        private static double ConvertMetersToLatitudeDegrees(double meters)
        {
            // Approximate conversion (1 degree latitude = ~111,111 meters)
            return meters / 111111.0;
        }
        
        /// <summary>
        /// Converts a distance in meters to approximate longitude degrees at the current latitude
        /// </summary>
        /// <param name="meters">The distance in meters</param>
        /// <param name="latitude">The latitude at which to calculate longitude conversion</param>
        /// <returns>The equivalent longitude degrees</returns>
        private static double ConvertMetersToLongitudeDegrees(double meters, double latitude)
        {
            // Approximate conversion (1 degree longitude = ~111,111 * cos(latitude) meters)
            double latitudeRadians = latitude * Math.PI / 180.0;
            return meters / (111111.0 * Math.Cos(latitudeRadians));
        }
    }
}