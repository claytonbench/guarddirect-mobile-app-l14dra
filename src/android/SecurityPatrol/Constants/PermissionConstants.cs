using System; // Version 8.0.0

namespace SecurityPatrol.Constants
{
    /// <summary>
    /// Static class containing constant string values for all permissions required by the application.
    /// Centralizes permission strings to ensure consistency across the application and simplify permission management.
    /// </summary>
    public static class PermissionConstants
    {
        /// <summary>
        /// Permission to access fine location when the app is in use.
        /// </summary>
        public static readonly string LocationWhenInUse;

        /// <summary>
        /// Permission to access location always, including in the background.
        /// </summary>
        public static readonly string LocationAlways;

        /// <summary>
        /// Permission to access the device camera for photo capture.
        /// </summary>
        public static readonly string Camera;

        /// <summary>
        /// Permission to read from external storage.
        /// </summary>
        public static readonly string ReadExternalStorage;

        /// <summary>
        /// Permission to write to external storage.
        /// </summary>
        public static readonly string WriteExternalStorage;

        /// <summary>
        /// Permission to access the internet for API communication.
        /// </summary>
        public static readonly string Internet;

        /// <summary>
        /// Permission to access network state information.
        /// </summary>
        public static readonly string AccessNetworkState;

        /// <summary>
        /// Permission to run foreground services for continuous operation.
        /// </summary>
        public static readonly string ForegroundService;

        /// <summary>
        /// Permission to acquire wake locks to keep the processor from sleeping.
        /// </summary>
        public static readonly string WakeLock;

        /// <summary>
        /// Permission to access precise location (GPS).
        /// </summary>
        public static readonly string AccessFineLocation;

        /// <summary>
        /// Permission to access approximate location (network-based).
        /// </summary>
        public static readonly string AccessCoarseLocation;

        /// <summary>
        /// Permission to access location in the background (when app is not in foreground).
        /// </summary>
        public static readonly string AccessBackgroundLocation;

        /// <summary>
        /// Permission to run location-based foreground services.
        /// </summary>
        public static readonly string ForegroundServiceLocation;

        /// <summary>
        /// Static constructor that initializes the permission constant values.
        /// </summary>
        static PermissionConstants()
        {
            LocationWhenInUse = "android.permission.ACCESS_FINE_LOCATION";
            LocationAlways = "android.permission.ACCESS_BACKGROUND_LOCATION";
            Camera = "android.permission.CAMERA";
            ReadExternalStorage = "android.permission.READ_EXTERNAL_STORAGE";
            WriteExternalStorage = "android.permission.WRITE_EXTERNAL_STORAGE";
            Internet = "android.permission.INTERNET";
            AccessNetworkState = "android.permission.ACCESS_NETWORK_STATE";
            ForegroundService = "android.permission.FOREGROUND_SERVICE";
            WakeLock = "android.permission.WAKE_LOCK";
            AccessFineLocation = "android.permission.ACCESS_FINE_LOCATION";
            AccessCoarseLocation = "android.permission.ACCESS_COARSE_LOCATION";
            AccessBackgroundLocation = "android.permission.ACCESS_BACKGROUND_LOCATION";
            ForegroundServiceLocation = "android.permission.FOREGROUND_SERVICE_LOCATION";
        }
    }
}