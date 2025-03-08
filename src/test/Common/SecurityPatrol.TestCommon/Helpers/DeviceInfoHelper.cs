using System; // System 8.0+
using System.Collections.Generic; // System.Collections.Generic 8.0+
using System.Threading.Tasks; // System.Threading.Tasks 8.0+
using SecurityPatrol.TestCommon.Constants; // For TestConstants

namespace SecurityPatrol.TestCommon.Helpers
{
    /// <summary>
    /// Static helper class that provides methods for simulating and accessing device information during testing.
    /// </summary>
    public static class DeviceInfoHelper
    {
        /// <summary>
        /// Gets a device model name for testing purposes
        /// </summary>
        /// <param name="customModel">Optional custom model name to use</param>
        /// <returns>The custom model if provided, otherwise the default test device model</returns>
        public static string GetDeviceModel(string customModel = null)
        {
            return !string.IsNullOrEmpty(customModel) 
                ? customModel 
                : TestConstants.DefaultDeviceModel;
        }

        /// <summary>
        /// Gets an OS version string for testing purposes
        /// </summary>
        /// <param name="customVersion">Optional custom OS version to use</param>
        /// <returns>The custom version if provided, otherwise the default test OS version</returns>
        public static string GetOsVersion(string customVersion = null)
        {
            return !string.IsNullOrEmpty(customVersion) 
                ? customVersion 
                : TestConstants.DefaultOsVersion;
        }

        /// <summary>
        /// Simulates a specific battery level for testing
        /// </summary>
        /// <param name="level">The battery level to simulate (0-100)</param>
        /// <returns>The simulated battery level (clamped between 0 and 100)</returns>
        public static int SimulateBatteryLevel(int level)
        {
            if (level < 0) return 0;
            if (level > 100) return 100;
            return level;
        }

        /// <summary>
        /// Simulates whether the device is charging
        /// </summary>
        /// <param name="isCharging">Whether the device is charging</param>
        /// <returns>The simulated charging state</returns>
        public static bool SimulateBatteryCharging(bool isCharging)
        {
            return isCharging;
        }

        /// <summary>
        /// Simulates available storage space on the device
        /// </summary>
        /// <param name="bytes">Available storage in bytes</param>
        /// <returns>The simulated available storage in bytes (minimum 0)</returns>
        public static long SimulateAvailableStorage(long bytes)
        {
            return bytes < 0 ? 0 : bytes;
        }

        /// <summary>
        /// Simulates total storage capacity of the device
        /// </summary>
        /// <param name="bytes">Total storage in bytes</param>
        /// <returns>The simulated total storage in bytes (minimum 0)</returns>
        public static long SimulateTotalStorage(long bytes)
        {
            return bytes < 0 ? 0 : bytes;
        }

        /// <summary>
        /// Simulates current memory usage of the application
        /// </summary>
        /// <param name="bytes">Memory usage in bytes</param>
        /// <returns>The simulated memory usage in bytes (minimum 0)</returns>
        public static long SimulateMemoryUsage(long bytes)
        {
            return bytes < 0 ? 0 : bytes;
        }

        /// <summary>
        /// Simulates available memory on the device
        /// </summary>
        /// <param name="bytes">Available memory in bytes</param>
        /// <returns>The simulated available memory in bytes (minimum 0)</returns>
        public static long SimulateAvailableMemory(long bytes)
        {
            return bytes < 0 ? 0 : bytes;
        }

        /// <summary>
        /// Simulates the screen size of the device
        /// </summary>
        /// <param name="width">Screen width in pixels</param>
        /// <param name="height">Screen height in pixels</param>
        /// <returns>A tuple containing the simulated screen width and height</returns>
        public static Tuple<double, double> SimulateScreenSize(double width, double height)
        {
            // Default values if invalid dimensions are provided
            const double defaultWidth = 1080;
            const double defaultHeight = 1920;
            
            width = width <= 0 ? defaultWidth : width;
            height = height <= 0 ? defaultHeight : height;
            
            return new Tuple<double, double>(width, height);
        }

        /// <summary>
        /// Simulates the screen density (DPI) of the device
        /// </summary>
        /// <param name="density">Screen density factor</param>
        /// <returns>The simulated screen density (minimum 1.0)</returns>
        public static double SimulateScreenDensity(double density)
        {
            return density < 1.0 ? 1.0 : density;
        }

        /// <summary>
        /// Simulates the current orientation of the device
        /// </summary>
        /// <param name="orientation">Device orientation (Portrait or Landscape)</param>
        /// <returns>The simulated device orientation (Portrait or Landscape)</returns>
        public static string SimulateDeviceOrientation(string orientation)
        {
            if (orientation != "Portrait" && orientation != "Landscape")
            {
                return "Portrait"; // Default to Portrait if invalid
            }
            return orientation;
        }

        /// <summary>
        /// Simulates whether location services are available on the device
        /// </summary>
        /// <param name="isAvailable">Whether location services are available</param>
        /// <returns>The simulated location service availability</returns>
        public static bool SimulateLocationServiceAvailability(bool isAvailable)
        {
            return isAvailable;
        }

        /// <summary>
        /// Simulates whether the camera is available on the device
        /// </summary>
        /// <param name="isAvailable">Whether the camera is available</param>
        /// <returns>The simulated camera availability</returns>
        public static bool SimulateCameraAvailability(bool isAvailable)
        {
            return isAvailable;
        }

        /// <summary>
        /// Creates a comprehensive device profile with all device characteristics
        /// </summary>
        /// <returns>A dictionary containing all device characteristics</returns>
        public static Dictionary<string, object> CreateDeviceProfile()
        {
            return new Dictionary<string, object>
            {
                { "DeviceModel", TestConstants.DefaultDeviceModel },
                { "OsVersion", TestConstants.DefaultOsVersion },
                { "BatteryLevel", 100 },
                { "IsCharging", true },
                { "AvailableStorage", 1073741824L }, // 1GB
                { "TotalStorage", 8589934592L }, // 8GB
                { "MemoryUsage", 104857600L }, // 100MB
                { "AvailableMemory", 1073741824L }, // 1GB
                { "ScreenWidth", 1080.0 },
                { "ScreenHeight", 1920.0 },
                { "ScreenDensity", 2.0 },
                { "Orientation", "Portrait" },
                { "LocationServiceAvailable", true },
                { "CameraAvailable", true }
            };
        }

        /// <summary>
        /// Creates a device profile representing a low-resource device
        /// </summary>
        /// <returns>A dictionary containing device characteristics for a low-resource device</returns>
        public static Dictionary<string, object> CreateLowResourceDeviceProfile()
        {
            return new Dictionary<string, object>
            {
                { "DeviceModel", "Low-End Android" },
                { "OsVersion", "Android 8.0" },
                { "BatteryLevel", 30 },
                { "IsCharging", false },
                { "AvailableStorage", 52428800L }, // 50MB
                { "TotalStorage", 2147483648L }, // 2GB
                { "MemoryUsage", 83886080L }, // 80MB
                { "AvailableMemory", 209715200L }, // 200MB
                { "ScreenWidth", 720.0 },
                { "ScreenHeight", 1280.0 },
                { "ScreenDensity", 1.5 },
                { "Orientation", "Portrait" },
                { "LocationServiceAvailable", true },
                { "CameraAvailable", true }
            };
        }

        /// <summary>
        /// Creates a device profile representing a high-end device
        /// </summary>
        /// <returns>A dictionary containing device characteristics for a high-end device</returns>
        public static Dictionary<string, object> CreateHighEndDeviceProfile()
        {
            return new Dictionary<string, object>
            {
                { "DeviceModel", "High-End Android" },
                { "OsVersion", "Android 13.0" },
                { "BatteryLevel", 90 },
                { "IsCharging", true },
                { "AvailableStorage", 107374182400L }, // 100GB
                { "TotalStorage", 536870912000L }, // 500GB
                { "MemoryUsage", 209715200L }, // 200MB
                { "AvailableMemory", 8589934592L }, // 8GB
                { "ScreenWidth", 1440.0 },
                { "ScreenHeight", 3200.0 },
                { "ScreenDensity", 3.5 },
                { "Orientation", "Portrait" },
                { "LocationServiceAvailable", true },
                { "CameraAvailable", true }
            };
        }

        /// <summary>
        /// Simulates battery drain over time
        /// </summary>
        /// <param name="startLevel">Starting battery level (0-100)</param>
        /// <param name="drainPercentage">Amount to drain in percentage points</param>
        /// <param name="intervalMs">Time interval in milliseconds</param>
        /// <returns>A task that returns the final battery level after the simulated drain</returns>
        public static async Task<int> SimulateBatteryDrain(int startLevel, int drainPercentage, int intervalMs)
        {
            // Validate parameters
            startLevel = SimulateBatteryLevel(startLevel); // Clamp to valid range
            drainPercentage = drainPercentage < 0 ? 0 : drainPercentage;
            intervalMs = intervalMs < 0 ? 0 : intervalMs;
            
            // Calculate final level
            int finalLevel = startLevel - drainPercentage;
            finalLevel = finalLevel < 0 ? 0 : finalLevel;
            
            // Simulate time passing
            if (intervalMs > 0)
            {
                await Task.Delay(intervalMs);
            }
            
            return finalLevel;
        }

        /// <summary>
        /// Simulates a change in available storage space
        /// </summary>
        /// <param name="currentBytes">Current available storage in bytes</param>
        /// <param name="changeBytes">Change in bytes (positive for increase, negative for decrease)</param>
        /// <returns>The new available storage after the change</returns>
        public static long SimulateStorageChange(long currentBytes, long changeBytes)
        {
            long newStorage = currentBytes + changeBytes;
            return newStorage < 0 ? 0 : newStorage;
        }
    }
}