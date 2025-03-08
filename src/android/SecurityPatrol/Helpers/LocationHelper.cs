using System; // Version 8.0+
using System.Threading.Tasks; // Version 8.0+
using Microsoft.Maui.Devices.Sensors; // Version 8.0+
using Microsoft.Extensions.Logging; // Version 8.0+
using SecurityPatrol.Models;
using SecurityPatrol.Helpers;
using SecurityPatrol.Constants;

namespace SecurityPatrol.Helpers
{
    /// <summary>
    /// Helper class that provides utility methods for location-related operations.
    /// </summary>
    public static class LocationHelper
    {
        // Earth radius in meters for Haversine formula calculations
        private const double EarthRadiusMeters = 6371000;

        /// <summary>
        /// Gets the current device location with specified accuracy and timeout.
        /// </summary>
        /// <param name="accuracy">The desired accuracy of the location data.</param>
        /// <param name="timeout">The maximum time to wait for a location update.</param>
        /// <param name="includeHeading">Whether to include heading information.</param>
        /// <param name="logger">Optional logger for recording operation details.</param>
        /// <returns>A task that returns the current location as a LocationModel.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when location permissions are not granted.</exception>
        public static async Task<LocationModel> GetCurrentLocationAsync(
            GeolocationAccuracy accuracy = GeolocationAccuracy.Medium,
            TimeSpan? timeout = null,
            bool includeHeading = false,
            ILogger logger = null)
        {
            try
            {
                logger?.LogInformation("Getting current location with accuracy: {Accuracy}", accuracy);

                // Check if location permissions are granted
                bool hasPermission = await PermissionHelper.CheckLocationPermissionsAsync(logger);
                if (!hasPermission)
                {
                    logger?.LogWarning("Location permissions not granted");
                    throw new UnauthorizedAccessException("Location permissions are required to get current location");
                }

                // Set default timeout if not provided
                timeout ??= TimeSpan.FromSeconds(15);

                // Create geolocation request
                var request = new GeolocationRequest(accuracy, timeout.Value)
                {
                    IncludeHeading = includeHeading
                };

                // Get current location
                var location = await Geolocation.GetLocationAsync(request);
                
                if (location == null)
                {
                    logger?.LogWarning("Failed to get current location");
                    return null;
                }

                logger?.LogInformation("Current location retrieved: Lat={Latitude}, Lon={Longitude}, Accuracy={Accuracy}",
                    location.Latitude, location.Longitude, location.Accuracy);

                // Convert to our LocationModel
                return new LocationModel
                {
                    Latitude = location.Latitude,
                    Longitude = location.Longitude,
                    Accuracy = location.Accuracy,
                    Timestamp = location.Timestamp.UtcDateTime
                };
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error getting current location");
                throw;
            }
        }

        /// <summary>
        /// Calculates the distance between two geographic coordinates using the Haversine formula.
        /// </summary>
        /// <param name="lat1">Latitude of the first point in degrees.</param>
        /// <param name="lon1">Longitude of the first point in degrees.</param>
        /// <param name="lat2">Latitude of the second point in degrees.</param>
        /// <param name="lon2">Longitude of the second point in degrees.</param>
        /// <returns>The distance in meters between the two coordinates.</returns>
        public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            // Convert degrees to radians
            double lat1Rad = lat1 * Math.PI / 180.0;
            double lon1Rad = lon1 * Math.PI / 180.0;
            double lat2Rad = lat2 * Math.PI / 180.0;
            double lon2Rad = lon2 * Math.PI / 180.0;

            // Differences between coordinates
            double dLat = lat2Rad - lat1Rad;
            double dLon = lon2Rad - lon1Rad;

            // Haversine formula
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                      Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                      Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            double distance = EarthRadiusMeters * c;

            return distance;
        }

        /// <summary>
        /// Calculates the distance between two LocationModel objects.
        /// </summary>
        /// <param name="location1">The first location.</param>
        /// <param name="location2">The second location.</param>
        /// <returns>The distance in meters between the two locations.</returns>
        /// <exception cref="ArgumentNullException">Thrown when either location is null.</exception>
        public static double CalculateDistance(LocationModel location1, LocationModel location2)
        {
            if (location1 == null || location2 == null)
            {
                throw new ArgumentNullException(
                    location1 == null ? nameof(location1) : nameof(location2),
                    "Locations cannot be null");
            }

            return CalculateDistance(
                location1.Latitude, location1.Longitude,
                location2.Latitude, location2.Longitude);
        }

        /// <summary>
        /// Converts a distance from meters to feet.
        /// </summary>
        /// <param name="meters">The distance in meters.</param>
        /// <returns>The distance in feet.</returns>
        public static double ConvertMetersToFeet(double meters)
        {
            // 1 meter = 3.28084 feet
            return meters * 3.28084;
        }

        /// <summary>
        /// Converts a distance from feet to meters.
        /// </summary>
        /// <param name="feet">The distance in feet.</param>
        /// <returns>The distance in meters.</returns>
        public static double ConvertFeetToMeters(double feet)
        {
            // 1 foot = 0.3048 meters
            return feet / 3.28084;
        }

        /// <summary>
        /// Formats a distance in meters to a human-readable string.
        /// </summary>
        /// <param name="distanceInMeters">The distance in meters.</param>
        /// <param name="useImperial">If true, displays distance in imperial units (feet/miles), otherwise metric (meters/kilometers).</param>
        /// <returns>A formatted string representing the distance.</returns>
        public static string FormatDistance(double distanceInMeters, bool useImperial = false)
        {
            if (useImperial)
            {
                double feet = ConvertMetersToFeet(distanceInMeters);
                
                if (feet < 1000)
                {
                    return $"{Math.Round(feet)} ft";
                }
                else
                {
                    double miles = feet / 5280;
                    return $"{Math.Round(miles, 2)} mi";
                }
            }
            else
            {
                if (distanceInMeters < 1000)
                {
                    return $"{Math.Round(distanceInMeters)} m";
                }
                else
                {
                    double kilometers = distanceInMeters / 1000;
                    return $"{Math.Round(kilometers, 2)} km";
                }
            }
        }

        /// <summary>
        /// Determines if two coordinates are within a specified distance of each other.
        /// </summary>
        /// <param name="lat1">Latitude of the first point in degrees.</param>
        /// <param name="lon1">Longitude of the first point in degrees.</param>
        /// <param name="lat2">Latitude of the second point in degrees.</param>
        /// <param name="lon2">Longitude of the second point in degrees.</param>
        /// <param name="thresholdInMeters">The maximum distance in meters for the points to be considered "within distance".</param>
        /// <returns>True if the coordinates are within the specified distance, false otherwise.</returns>
        public static bool IsWithinDistance(double lat1, double lon1, double lat2, double lon2, double thresholdInMeters)
        {
            double distance = CalculateDistance(lat1, lon1, lat2, lon2);
            return distance <= thresholdInMeters;
        }

        /// <summary>
        /// Determines if two LocationModel objects are within a specified distance of each other.
        /// </summary>
        /// <param name="location1">The first location.</param>
        /// <param name="location2">The second location.</param>
        /// <param name="thresholdInMeters">The maximum distance in meters for the locations to be considered "within distance".</param>
        /// <returns>True if the locations are within the specified distance, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown when either location is null.</exception>
        public static bool IsWithinDistance(LocationModel location1, LocationModel location2, double thresholdInMeters)
        {
            if (location1 == null || location2 == null)
            {
                throw new ArgumentNullException(
                    location1 == null ? nameof(location1) : nameof(location2),
                    "Locations cannot be null");
            }

            return IsWithinDistance(
                location1.Latitude, location1.Longitude,
                location2.Latitude, location2.Longitude,
                thresholdInMeters);
        }

        /// <summary>
        /// Gets the appropriate location accuracy based on battery optimization settings.
        /// </summary>
        /// <param name="batteryOptimized">If true, returns a lower accuracy to save battery.</param>
        /// <returns>The appropriate accuracy level for location tracking.</returns>
        public static GeolocationAccuracy GetLocationTrackingAccuracy(bool batteryOptimized)
        {
            return batteryOptimized ? GeolocationAccuracy.Medium : GeolocationAccuracy.Best;
        }
    }
}