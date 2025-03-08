using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace SecurityPatrol.Converters
{
    /// <summary>
    /// Converts boolean values to their inverse (true to false, false to true) for UI bindings.
    /// This is useful for scenarios where the visual state should be the opposite of a boolean property.
    /// </summary>
    public class InverseBoolConverter : IValueConverter
    {
        /// <summary>
        /// Converts a boolean value to its inverse (true to false, false to true).
        /// </summary>
        /// <param name="value">The boolean value to convert</param>
        /// <param name="targetType">The type to convert to</param>
        /// <param name="parameter">Optional parameter (not used)</param>
        /// <param name="culture">Culture information (not used)</param>
        /// <returns>The inverse of the input boolean value</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            
            return false;
        }

        /// <summary>
        /// Converts a boolean value back to its inverse. Since the operation is symmetrical,
        /// this performs the same inversion as Convert.
        /// </summary>
        /// <param name="value">The boolean value to convert back</param>
        /// <param name="targetType">The type to convert to</param>
        /// <param name="parameter">Optional parameter (not used)</param>
        /// <param name="culture">Culture information (not used)</param>
        /// <returns>The inverse of the input boolean value</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            
            return false;
        }
    }
}