using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace SecurityPatrol.Converters
{
    /// <summary>
    /// Converts boolean values to visibility states for UI elements.
    /// When true, the element is visible; when false, the element is collapsed or hidden.
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Converts a boolean value to a visibility state (visible when true, collapsed when false).
        /// </summary>
        /// <param name="value">The boolean value to convert</param>
        /// <param name="targetType">The type of the target property</param>
        /// <param name="parameter">Optional parameter to invert the logic if "invert" is specified</param>
        /// <param name="culture">The culture information</param>
        /// <returns>A boolean value representing the visibility state</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool invert = IsInversionRequested(parameter);
            bool result = ToBool(value);
            return invert ? !result : result;
        }

        /// <summary>
        /// Converts a visibility state back to a boolean value.
        /// </summary>
        /// <param name="value">The visibility state to convert</param>
        /// <param name="targetType">The type of the target property</param>
        /// <param name="parameter">Optional parameter to invert the logic if "invert" is specified</param>
        /// <param name="culture">The culture information</param>
        /// <returns>A boolean value (true for visible, false for collapsed/hidden)</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool invert = IsInversionRequested(parameter);
            bool result = ToBool(value);
            return invert ? !result : result;
        }

        private bool ToBool(object value)
        {
            if (value == null)
                return false;

            if (value is bool boolValue)
                return boolValue;

            // Try to convert to boolean if it's not already
            if (bool.TryParse(value.ToString(), out bool parsedValue))
                return parsedValue;

            return false;
        }

        private bool IsInversionRequested(object parameter)
        {
            if (parameter == null)
                return false;

            // Check string parameters
            if (parameter is string paramString)
            {
                string normalizedParam = paramString.ToLowerInvariant();
                return normalizedParam == "invert" || 
                       normalizedParam == "true" || 
                       normalizedParam == "!";
            }

            // Check boolean parameters
            if (parameter is bool boolParam)
                return boolParam;

            return false;
        }
    }
}