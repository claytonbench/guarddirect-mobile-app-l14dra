using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using SecurityPatrol.Constants;
using SecurityPatrol.Helpers;

namespace SecurityPatrol.Helpers
{
    /// <summary>
    /// Static helper class that provides centralized permission management for the Security Patrol application
    /// </summary>
    public static class PermissionHelper
    {
        /// <summary>
        /// Checks if location permissions are granted
        /// </summary>
        /// <param name="logger">Logger for recording operation details</param>
        /// <returns>True if location permissions are granted, false otherwise</returns>
        public static async Task<bool> CheckLocationPermissionsAsync(ILogger logger)
        {
            try
            {
                logger?.LogInformation("Checking location permissions");
                
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                
                if (status == PermissionStatus.Granted)
                {
                    logger?.LogInformation("Location permission is granted");
                    return true;
                }
                
                logger?.LogInformation($"Location permission is not granted: {status}");
                return false;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error checking location permissions");
                return false;
            }
        }

        /// <summary>
        /// Checks if background location permission is granted
        /// </summary>
        /// <param name="logger">Logger for recording operation details</param>
        /// <returns>True if background location permission is granted, false otherwise</returns>
        public static async Task<bool> CheckBackgroundLocationPermissionAsync(ILogger logger)
        {
            try
            {
                logger?.LogInformation("Checking background location permission");
                
                // Background location permissions are only required on Android 10+
                if (!IsAndroidVersionAtLeast(10))
                {
                    logger?.LogInformation("Android version < 10, no separate background location permission needed");
                    return true; // Earlier versions don't need separate background permission
                }
                
                var status = await Permissions.CheckStatusAsync<Permissions.LocationAlways>();
                
                if (status == PermissionStatus.Granted)
                {
                    logger?.LogInformation("Background location permission is granted");
                    return true;
                }
                
                logger?.LogInformation($"Background location permission is not granted: {status}");
                return false;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error checking background location permission");
                return false;
            }
        }

        /// <summary>
        /// Checks if camera permission is granted
        /// </summary>
        /// <param name="logger">Logger for recording operation details</param>
        /// <returns>True if camera permission is granted, false otherwise</returns>
        public static async Task<bool> CheckCameraPermissionAsync(ILogger logger)
        {
            try
            {
                logger?.LogInformation("Checking camera permission");
                
                var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
                
                if (status == PermissionStatus.Granted)
                {
                    logger?.LogInformation("Camera permission is granted");
                    return true;
                }
                
                logger?.LogInformation($"Camera permission is not granted: {status}");
                return false;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error checking camera permission");
                return false;
            }
        }

        /// <summary>
        /// Checks if storage permissions are granted
        /// </summary>
        /// <param name="logger">Logger for recording operation details</param>
        /// <returns>True if storage permissions are granted, false otherwise</returns>
        public static async Task<bool> CheckStoragePermissionsAsync(ILogger logger)
        {
            try
            {
                logger?.LogInformation("Checking storage permissions");
                
                // Android 13+ doesn't require explicit storage permissions for app-specific storage
                if (IsAndroidVersionAtLeast(13))
                {
                    logger?.LogInformation("Android version >= 13, no explicit storage permissions needed for app-specific storage");
                    return true;
                }
                
                var readStatus = await Permissions.CheckStatusAsync<Permissions.StorageRead>();
                var writeStatus = await Permissions.CheckStatusAsync<Permissions.StorageWrite>();
                
                if (readStatus == PermissionStatus.Granted && writeStatus == PermissionStatus.Granted)
                {
                    logger?.LogInformation("Storage permissions are granted");
                    return true;
                }
                
                logger?.LogInformation($"Storage permissions are not granted: Read={readStatus}, Write={writeStatus}");
                return false;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error checking storage permissions");
                return false;
            }
        }

        /// <summary>
        /// Requests location permissions from the user
        /// </summary>
        /// <param name="showRationale">Whether to show permission rationale dialog</param>
        /// <param name="logger">Logger for recording operation details</param>
        /// <returns>True if location permissions are granted, false otherwise</returns>
        public static async Task<bool> RequestLocationPermissionsAsync(bool showRationale, ILogger logger)
        {
            try
            {
                logger?.LogInformation("Requesting location permissions");
                
                // Check if already granted
                if (await CheckLocationPermissionsAsync(logger))
                {
                    return true;
                }
                
                // Show rationale if requested
                if (showRationale)
                {
                    var acknowledged = await ShowPermissionRationaleAsync(PermissionConstants.AccessFineLocation, logger);
                    if (!acknowledged)
                    {
                        logger?.LogInformation("User declined to proceed with location permission request");
                        return false;
                    }
                }
                
                // Request permission
                var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                
                if (status == PermissionStatus.Granted)
                {
                    logger?.LogInformation("Location permission granted");
                    return true;
                }
                
                logger?.LogInformation($"Location permission request denied: {status}");
                return false;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error requesting location permissions");
                return false;
            }
        }

        /// <summary>
        /// Requests background location permission from the user
        /// </summary>
        /// <param name="showRationale">Whether to show permission rationale dialog</param>
        /// <param name="logger">Logger for recording operation details</param>
        /// <returns>True if background location permission is granted, false otherwise</returns>
        public static async Task<bool> RequestBackgroundLocationPermissionAsync(bool showRationale, ILogger logger)
        {
            try
            {
                logger?.LogInformation("Requesting background location permission");
                
                // Background location permissions are only required on Android 10+
                if (!IsAndroidVersionAtLeast(10))
                {
                    logger?.LogInformation("Android version < 10, no separate background location permission needed");
                    return true; // Earlier versions don't need separate background permission
                }
                
                // Must have foreground location permission first
                if (!await CheckLocationPermissionsAsync(logger))
                {
                    logger?.LogWarning("Cannot request background location without foreground location permission");
                    return false;
                }
                
                // Check if already granted
                if (await CheckBackgroundLocationPermissionAsync(logger))
                {
                    return true;
                }
                
                // Show rationale if requested
                if (showRationale)
                {
                    var acknowledged = await ShowPermissionRationaleAsync(PermissionConstants.AccessBackgroundLocation, logger);
                    if (!acknowledged)
                    {
                        logger?.LogInformation("User declined to proceed with background location permission request");
                        return false;
                    }
                }
                
                // Request permission
                var status = await Permissions.RequestAsync<Permissions.LocationAlways>();
                
                if (status == PermissionStatus.Granted)
                {
                    logger?.LogInformation("Background location permission granted");
                    return true;
                }
                
                logger?.LogInformation($"Background location permission request denied: {status}");
                return false;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error requesting background location permission");
                return false;
            }
        }

        /// <summary>
        /// Requests camera permission from the user
        /// </summary>
        /// <param name="showRationale">Whether to show permission rationale dialog</param>
        /// <param name="logger">Logger for recording operation details</param>
        /// <returns>True if camera permission is granted, false otherwise</returns>
        public static async Task<bool> RequestCameraPermissionAsync(bool showRationale, ILogger logger)
        {
            try
            {
                logger?.LogInformation("Requesting camera permission");
                
                // Check if already granted
                if (await CheckCameraPermissionAsync(logger))
                {
                    return true;
                }
                
                // Show rationale if requested
                if (showRationale)
                {
                    var acknowledged = await ShowPermissionRationaleAsync(PermissionConstants.Camera, logger);
                    if (!acknowledged)
                    {
                        logger?.LogInformation("User declined to proceed with camera permission request");
                        return false;
                    }
                }
                
                // Request permission
                var status = await Permissions.RequestAsync<Permissions.Camera>();
                
                if (status == PermissionStatus.Granted)
                {
                    logger?.LogInformation("Camera permission granted");
                    return true;
                }
                
                logger?.LogInformation($"Camera permission request denied: {status}");
                return false;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error requesting camera permission");
                return false;
            }
        }

        /// <summary>
        /// Requests storage permissions from the user
        /// </summary>
        /// <param name="showRationale">Whether to show permission rationale dialog</param>
        /// <param name="logger">Logger for recording operation details</param>
        /// <returns>True if storage permissions are granted, false otherwise</returns>
        public static async Task<bool> RequestStoragePermissionsAsync(bool showRationale, ILogger logger)
        {
            try
            {
                logger?.LogInformation("Requesting storage permissions");
                
                // Android 13+ doesn't require explicit storage permissions for app-specific storage
                if (IsAndroidVersionAtLeast(13))
                {
                    logger?.LogInformation("Android version >= 13, no explicit storage permissions needed for app-specific storage");
                    return true;
                }
                
                // Check if already granted
                if (await CheckStoragePermissionsAsync(logger))
                {
                    return true;
                }
                
                // Show rationale if requested
                if (showRationale)
                {
                    var acknowledged = await ShowPermissionRationaleAsync(PermissionConstants.ReadExternalStorage, logger);
                    if (!acknowledged)
                    {
                        logger?.LogInformation("User declined to proceed with storage permission request");
                        return false;
                    }
                }
                
                // Request permissions
                var readStatus = await Permissions.RequestAsync<Permissions.StorageRead>();
                var writeStatus = await Permissions.RequestAsync<Permissions.StorageWrite>();
                
                if (readStatus == PermissionStatus.Granted && writeStatus == PermissionStatus.Granted)
                {
                    logger?.LogInformation("Storage permissions granted");
                    return true;
                }
                
                logger?.LogInformation($"Storage permission request denied: Read={readStatus}, Write={writeStatus}");
                return false;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error requesting storage permissions");
                return false;
            }
        }

        /// <summary>
        /// Shows a dialog explaining why a permission is needed
        /// </summary>
        /// <param name="permissionType">The type of permission to explain</param>
        /// <param name="logger">Logger for recording operation details</param>
        /// <returns>True if user acknowledges the rationale, false otherwise</returns>
        private static async Task<bool> ShowPermissionRationaleAsync(string permissionType, ILogger logger)
        {
            try
            {
                return await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    string title = "Permission Required";
                    string message = string.Empty;
                    string accept = "Continue";
                    string cancel = "Cancel";
                    
                    // Check permission type against constants
                    if (permissionType == PermissionConstants.AccessFineLocation || 
                        permissionType == PermissionConstants.AccessCoarseLocation ||
                        permissionType == PermissionConstants.LocationWhenInUse)
                    {
                        title = "Location Permission Required";
                        message = "The Security Patrol app needs access to your location to track patrol activities. " +
                                 "This helps verify your presence at checkpoints and maintain accurate patrol records.";
                    }
                    else if (permissionType == PermissionConstants.AccessBackgroundLocation || 
                             permissionType == PermissionConstants.LocationAlways)
                    {
                        title = "Background Location Permission Required";
                        message = "The Security Patrol app needs to track your location even when the app is in the background. " +
                                 "This is essential for continuous location tracking during active shifts.";
                    }
                    else if (permissionType == PermissionConstants.Camera)
                    {
                        title = "Camera Permission Required";
                        message = "The Security Patrol app needs access to your camera to capture photos during patrols. " +
                                 "These photos are part of the official patrol documentation.";
                    }
                    else if (permissionType == PermissionConstants.ReadExternalStorage || 
                             permissionType == PermissionConstants.WriteExternalStorage)
                    {
                        title = "Storage Permission Required";
                        message = "The Security Patrol app needs access to device storage to save photos and reports. " +
                                 "This ensures that patrol documentation is properly saved and can be accessed when needed.";
                    }
                    else
                    {
                        message = "This permission is required for the app to function properly.";
                    }
                    
                    return await DialogHelper.DisplayConfirmationAsync(title, message, accept, cancel);
                });
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error showing permission rationale dialog");
                return false;
            }
        }

        /// <summary>
        /// Checks if the current Android version is at least the specified version
        /// </summary>
        /// <param name="majorVersion">The major version to check against</param>
        /// <returns>True if the device is running Android with at least the specified version, false otherwise</returns>
        private static bool IsAndroidVersionAtLeast(int majorVersion)
        {
            try
            {
                // Check if running on Android
                if (DeviceInfo.Platform != DevicePlatform.Android)
                {
                    return false;
                }
                
                // Parse the version
                string versionString = DeviceInfo.VersionString;
                if (string.IsNullOrEmpty(versionString))
                {
                    return false;
                }
                
                // Extract major version
                int currentVersion;
                if (int.TryParse(versionString.Split('.')[0], out currentVersion))
                {
                    return currentVersion >= majorVersion;
                }
                
                return false;
            }
            catch
            {
                // If there's any error, assume version requirement is not met
                return false;
            }
        }
    }
}