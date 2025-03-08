using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace SecurityPatrol.Converters
{
    /// <summary>
    /// Converts DateTime values to formatted strings for display in the UI.
    /// Supports different format types through converter parameters.
    /// </summary>
    public class DateTimeConverter : IValueConverter
    {
        /// <summary>
        /// Converts a DateTime value to a formatted string based on the specified format parameter.
        /// </summary>
        /// <param name="value">The DateTime value to convert</param>
        /// <param name="targetType">The type to convert to (not used)</param>
        /// <param name="parameter">Format parameter: "Date", "Time", "DateTime", "ShortDate", "ShortTime", or "Relative"</param>
        /// <param name="culture">The culture to use for formatting</param>
        /// <returns>A formatted string representation of the DateTime value</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Check if value is DateTime
            if (value is not DateTime dateTime)
            {
                return string.Empty;
            }

            // Get format parameter (default to DateTime if not specified)
            string format = parameter as string ?? "DateTime";

            return format.ToLowerInvariant() switch
            {
                "date" => dateTime.ToString("MM/dd/yyyy", culture),
                "time" => dateTime.ToString("hh:mm tt", culture),
                "datetime" => dateTime.ToString("MM/dd/yyyy hh:mm tt", culture),
                "shortdate" => dateTime.ToString("MM/dd", culture),
                "shorttime" => dateTime.ToString("hh:mm", culture),
                "relative" => GetRelativeTimeString(dateTime),
                _ => dateTime.ToString("MM/dd/yyyy hh:mm tt", culture)
            };
        }

        /// <summary>
        /// Converts a string back to a DateTime value. This is typically not implemented
        /// as the conversion back is usually not needed for display purposes.
        /// </summary>
        /// <returns>null as this conversion is not supported in this context</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // This converter does not support converting back
            return null;
        }

        /// <summary>
        /// Returns a human-readable relative time string (e.g., "5 minutes ago")
        /// </summary>
        private string GetRelativeTimeString(DateTime dateTime)
        {
            var timeSpan = DateTime.Now - dateTime;

            if (timeSpan.TotalDays > 365)
            {
                int years = (int)(timeSpan.TotalDays / 365);
                return $"{years} {(years == 1 ? "year" : "years")} ago";
            }
            if (timeSpan.TotalDays > 30)
            {
                int months = (int)(timeSpan.TotalDays / 30);
                return $"{months} {(months == 1 ? "month" : "months")} ago";
            }
            if (timeSpan.TotalDays > 1)
            {
                int days = (int)timeSpan.TotalDays;
                return $"{days} {(days == 1 ? "day" : "days")} ago";
            }
            if (timeSpan.TotalHours > 1)
            {
                int hours = (int)timeSpan.TotalHours;
                return $"{hours} {(hours == 1 ? "hour" : "hours")} ago";
            }
            if (timeSpan.TotalMinutes > 1)
            {
                int minutes = (int)timeSpan.TotalMinutes;
                return $"{minutes} {(minutes == 1 ? "minute" : "minutes")} ago";
            }

            return "Just now";
        }
    }
}