using System; // System 8.0.0
using System.Text.RegularExpressions; // System.Text.RegularExpressions 8.0.0
using System.Globalization; // System.Globalization 8.0.0
using SecurityPatrol.Constants;

namespace SecurityPatrol.Helpers
{
    /// <summary>
    /// Static helper class that provides validation methods for various data inputs in the Security Patrol application.
    /// </summary>
    public static class ValidationHelper
    {
        /// <summary>
        /// Validates a phone number to ensure it matches the expected format with country code
        /// </summary>
        /// <param name="phoneNumber">The phone number to validate</param>
        /// <returns>Tuple containing validation result (true if valid) and error message (null if valid)</returns>
        public static (bool isValid, string errorMessage) ValidatePhoneNumber(string phoneNumber)
        {
            if (IsNullOrEmpty(phoneNumber))
                return (false, ErrorMessages.InvalidPhoneNumber);

            // Regular expression for phone number validation
            // Format should be +[country code][number], e.g., +12345678901
            var regex = new Regex(@"^\+[1-9]\d{1,14}$");
            if (!regex.IsMatch(phoneNumber))
                return (false, ErrorMessages.InvalidPhoneNumber);

            return (true, null);
        }

        /// <summary>
        /// Validates a verification code to ensure it is exactly 6 digits
        /// </summary>
        /// <param name="code">The verification code to validate</param>
        /// <returns>Tuple containing validation result (true if valid) and error message (null if valid)</returns>
        public static (bool isValid, string errorMessage) ValidateVerificationCode(string code)
        {
            if (IsNullOrEmpty(code))
                return (false, ErrorMessages.InvalidVerificationCode);

            if (code.Length != 6)
                return (false, ErrorMessages.InvalidVerificationCode);

            if (!IsDigitsOnly(code))
                return (false, ErrorMessages.InvalidVerificationCode);

            return (true, null);
        }

        /// <summary>
        /// Validates report text to ensure it is not empty and does not exceed maximum length
        /// </summary>
        /// <param name="text">The report text to validate</param>
        /// <returns>Tuple containing validation result (true if valid) and error message (null if valid)</returns>
        public static (bool isValid, string errorMessage) ValidateReportText(string text)
        {
            if (IsNullOrEmpty(text))
                return (false, ErrorMessages.ReportEmpty);

            if (text.Length > AppConstants.ReportMaxLength)
                return (false, ErrorMessages.ReportTooLong);

            return (true, null);
        }

        /// <summary>
        /// Validates if the user is within the required proximity to a checkpoint
        /// </summary>
        /// <param name="userLatitude">User's latitude</param>
        /// <param name="userLongitude">User's longitude</param>
        /// <param name="checkpointLatitude">Checkpoint's latitude</param>
        /// <param name="checkpointLongitude">Checkpoint's longitude</param>
        /// <returns>Tuple containing validation result (true if within proximity) and error message (null if within proximity)</returns>
        public static (bool isValid, string errorMessage) ValidateCheckpointProximity(double userLatitude, double userLongitude, 
                                                                              double checkpointLatitude, double checkpointLongitude)
        {
            // Calculate distance in meters
            double distanceMeters = CalculateDistance(userLatitude, userLongitude, checkpointLatitude, checkpointLongitude);
            
            // Convert distance to feet (1 meter = 3.28084 feet)
            double distanceFeet = distanceMeters * 3.28084;
            
            if (distanceFeet <= AppConstants.CheckpointProximityThresholdFeet)
                return (true, null);
                
            return (false, ErrorMessages.CheckpointTooFar);
        }

        /// <summary>
        /// Calculates the distance in meters between two geographic coordinates using the Haversine formula
        /// </summary>
        /// <param name="lat1">First point latitude</param>
        /// <param name="lon1">First point longitude</param>
        /// <param name="lat2">Second point latitude</param>
        /// <param name="lon2">Second point longitude</param>
        /// <returns>Distance in meters between the two coordinates</returns>
        public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            // Earth radius in meters
            const double earthRadiusMeters = 6371000;

            // Convert latitude and longitude from degrees to radians
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
            double distance = earthRadiusMeters * c;

            return distance;
        }

        /// <summary>
        /// Checks if a string is null or empty
        /// </summary>
        /// <param name="value">The string to check</param>
        /// <returns>True if the string is null or empty, false otherwise</returns>
        public static bool IsNullOrEmpty(string value)
        {
            return string.IsNullOrEmpty(value);
        }

        /// <summary>
        /// Checks if a string contains only digit characters
        /// </summary>
        /// <param name="value">The string to check</param>
        /// <returns>True if the string contains only digits, false otherwise</returns>
        public static bool IsDigitsOnly(string value)
        {
            if (IsNullOrEmpty(value))
                return false;

            foreach (char c in value)
            {
                if (!char.IsDigit(c))
                    return false;
            }

            return true;
        }
    }
}