using System;
using System.Globalization;
using Microsoft.Maui.Controls; // Version 8.0.0
using SecurityPatrol.Helpers;

namespace SecurityPatrol.Converters
{
    /// <summary>
    /// Converts distance values (in meters) to human-readable string representations with appropriate units.
    /// Supports different format options through converter parameters.
    /// </summary>
    public class DistanceConverter : IValueConverter
    {
        /// <summary>
        /// Converts a distance value (in meters) to a formatted string with appropriate units
        /// based on the specified format parameter.
        /// </summary>
        /// <param name="value">The distance value in meters</param>
        /// <param name="targetType">The type to convert to (not used)</param>
        /// <param name="parameter">Format parameter: "Feet", "Meters", "Auto", "Short", or "Verbose"</param>
        /// <param name="culture">The culture for formatting (not used)</param>
        /// <returns>A formatted string representation of the distance value</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Check if value is a valid numeric type
            if (value == null)
                return string.Empty;

            double distanceInMeters;
            
            if (value is double doubleValue)
                distanceInMeters = doubleValue;
            else if (value is float floatValue)
                distanceInMeters = floatValue;
            else if (value is int intValue)
                distanceInMeters = intValue;
            else if (!double.TryParse(value.ToString(), out distanceInMeters))
                return string.Empty;

            // Get format parameter
            string format = parameter?.ToString() ?? "Feet";

            // Format based on parameter
            switch (format.ToLowerInvariant())
            {
                case "feet":
                    double feet = LocationHelper.ConvertMetersToFeet(distanceInMeters);
                    return FormatWithUnits(feet, "ft");
                
                case "meters":
                    return FormatWithUnits(distanceInMeters, "m");
                
                case "auto":
                    // Use feet for shorter distances, meters/kilometers for longer distances
                    if (distanceInMeters < 1000)
                    {
                        double feet = LocationHelper.ConvertMetersToFeet(distanceInMeters);
                        return FormatWithUnits(feet, "ft");
                    }
                    else
                    {
                        double kilometers = distanceInMeters / 1000;
                        return FormatWithUnits(kilometers, "km");
                    }
                
                case "short":
                    // Minimal format for space-constrained UI
                    if (distanceInMeters < 1000)
                    {
                        double feet = LocationHelper.ConvertMetersToFeet(distanceInMeters);
                        return Math.Round(feet) + " ft";
                    }
                    else
                    {
                        double kilometers = distanceInMeters / 1000;
                        return Math.Round(kilometers, 1) + " km";
                    }
                
                case "verbose":
                    // Descriptive format for accessibility
                    if (distanceInMeters < 1000)
                    {
                        double feet = LocationHelper.ConvertMetersToFeet(distanceInMeters);
                        return $"{Math.Round(feet)} feet away";
                    }
                    else
                    {
                        double kilometers = distanceInMeters / 1000;
                        return $"{Math.Round(kilometers, 1)} kilometers away";
                    }
                
                default:
                    // Default to feet
                    double defaultFeet = LocationHelper.ConvertMetersToFeet(distanceInMeters);
                    return FormatWithUnits(defaultFeet, "ft");
            }
        }

        /// <summary>
        /// Formats a numeric distance value with appropriate rounding and unit suffix.
        /// </summary>
        private string FormatWithUnits(double distance, string unit)
        {
            // Adjust decimal places based on the magnitude and add a space between value and unit
            if (distance < 10)
                return Math.Round(distance, 1) + " " + unit;
            else
                return Math.Round(distance) + " " + unit;
        }

        /// <summary>
        /// Converts a formatted distance string back to a numeric value in meters.
        /// This method is not implemented as this conversion is typically not needed.
        /// </summary>
        /// <returns>Always returns null as this conversion is not supported</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // This conversion is not supported
            return null;
        }
    }
}