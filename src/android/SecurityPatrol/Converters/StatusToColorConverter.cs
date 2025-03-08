using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using SecurityPatrol.Models;

namespace SecurityPatrol.Converters
{
    /// <summary>
    /// Converts various status values to appropriate colors for visual representation in the UI.
    /// Handles different types of status objects including ClockStatus, CheckpointStatus, SyncItem, and PatrolStatus,
    /// as well as boolean values.
    /// </summary>
    public class StatusToColorConverter : IValueConverter
    {
        /// <summary>
        /// Converts a status value to an appropriate color based on the status type and state.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">Additional parameter for the converter. Not used.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A Color object representing the appropriate color for the status.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return Application.Current.Resources["InactiveColor"];

            // Handle boolean values
            if (value is bool boolValue)
                return boolValue ? Application.Current.Resources["SuccessColor"] : Application.Current.Resources["ErrorColor"];

            // Handle ClockStatus objects
            if (value is ClockStatus clockStatus)
                return clockStatus.IsClocked ? Application.Current.Resources["ClockInColor"] : Application.Current.Resources["ClockOutColor"];

            // Handle CheckpointStatus objects
            if (value is CheckpointStatus checkpointStatus)
                return checkpointStatus.IsVerified ? Application.Current.Resources["CheckpointCompletedColor"] : Application.Current.Resources["CheckpointPendingColor"];

            // Handle SyncItem objects
            if (value is SyncItem syncItem)
            {
                if (syncItem.RetryCount > 0 && !string.IsNullOrEmpty(syncItem.ErrorMessage))
                    return Application.Current.Resources["SyncFailedColor"];
                
                return syncItem.RetryCount == 0 ? Application.Current.Resources["SyncCompletedColor"] : Application.Current.Resources["SyncPendingColor"];
            }

            // Handle PatrolStatus objects
            if (value is PatrolStatus patrolStatus)
            {
                if (patrolStatus.IsComplete())
                    return Application.Current.Resources["SuccessColor"];
                
                double completion = patrolStatus.CalculateCompletionPercentage();
                return completion > 0 && completion < 100 ? Application.Current.Resources["WarningColor"] : Application.Current.Resources["InactiveColor"];
            }

            // Handle string values
            if (value is string statusString)
            {
                var lowercaseStatus = statusString.ToLowerInvariant().Trim();
                
                if (lowercaseStatus == "active")
                    return Application.Current.Resources["LocationTrackingActiveColor"];
                
                if (lowercaseStatus == "inactive")
                    return Application.Current.Resources["LocationTrackingInactiveColor"];
                
                if (lowercaseStatus == "proximity")
                    return Application.Current.Resources["CheckpointProximityColor"];
            }

            // Default fallback
            return Application.Current.Resources["InactiveColor"];
        }

        /// <summary>
        /// Converts a color back to a status value. This operation is not supported and throws NotImplementedException.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">Additional parameter for the converter. Not used.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>This method always throws NotImplementedException.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("Converting from color to status is not supported.");
        }
    }
}