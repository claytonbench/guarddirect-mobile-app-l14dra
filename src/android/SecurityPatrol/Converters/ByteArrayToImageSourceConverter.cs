using System;
using System.Globalization;
using System.IO;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace SecurityPatrol.Converters
{
    /// <summary>
    /// Converts byte array data to ImageSource objects for display in Image controls.
    /// Handles null values and invalid data gracefully.
    /// </summary>
    public class ByteArrayToImageSourceConverter : IValueConverter
    {
        /// <summary>
        /// Converts a byte array to an ImageSource object for display in an Image control.
        /// </summary>
        /// <param name="value">The byte array to convert</param>
        /// <param name="targetType">The type of the binding target property</param>
        /// <param name="parameter">Optional converter parameter (not used)</param>
        /// <param name="culture">Culture information (not used)</param>
        /// <returns>An ImageSource object created from the byte array, or null if conversion fails</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is byte[] imageData && imageData.Length > 0)
                {
                    return ImageSource.FromStream(() => new MemoryStream(imageData));
                }
                
                return null;
            }
            catch (Exception ex)
            {
                // Log the exception but don't crash the application
                System.Diagnostics.Debug.WriteLine($"Error converting byte array to ImageSource: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Converts an ImageSource back to a byte array. This operation is not supported in this implementation.
        /// </summary>
        /// <param name="value">The ImageSource to convert</param>
        /// <param name="targetType">The type of the binding target property</param>
        /// <param name="parameter">Optional converter parameter (not used)</param>
        /// <param name="culture">Culture information (not used)</param>
        /// <returns>Always returns null as this operation is not supported</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // ConvertBack operation is not supported - cannot reliably convert ImageSource back to byte array
            return null;
        }
    }
}