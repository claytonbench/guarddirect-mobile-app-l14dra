using System; // System package, version 8.0+

namespace SecurityPatrol.Constants
{
    /// <summary>
    /// Static class containing application-wide constant values used throughout the Security Patrol application.
    /// </summary>
    public static class AppConstants
    {
        /// <summary>
        /// The name of the application.
        /// </summary>
        public static readonly string AppName;
        
        /// <summary>
        /// The version of the application.
        /// </summary>
        public static readonly string AppVersion;
        
        /// <summary>
        /// The name of the SQLite database file.
        /// </summary>
        public static readonly string DatabaseName;
        
        /// <summary>
        /// The version of the database schema.
        /// </summary>
        public static readonly int DatabaseVersion;
        
        /// <summary>
        /// Default interval for location tracking in seconds.
        /// </summary>
        public static readonly int LocationTrackingIntervalDefault;
        
        /// <summary>
        /// Low power interval for location tracking in seconds (less frequent updates to save battery).
        /// </summary>
        public static readonly int LocationTrackingIntervalLowPower;
        
        /// <summary>
        /// High accuracy interval for location tracking in seconds (more frequent updates for better accuracy).
        /// </summary>
        public static readonly int LocationTrackingIntervalHighAccuracy;
        
        /// <summary>
        /// Number of location points to batch before sending to the API.
        /// </summary>
        public static readonly int LocationBatchSize;
        
        /// <summary>
        /// Maximum number of location points to store locally.
        /// </summary>
        public static readonly int LocationMaxStoredPoints;
        
        /// <summary>
        /// Threshold distance in meters to determine proximity to a location.
        /// </summary>
        public static readonly double LocationProximityThresholdMeters;
        
        /// <summary>
        /// Threshold distance in feet to determine proximity to a checkpoint.
        /// </summary>
        public static readonly double CheckpointProximityThresholdFeet;
        
        /// <summary>
        /// Quality setting for photo compression (0-100).
        /// </summary>
        public static readonly int PhotoCompressionQuality;
        
        /// <summary>
        /// Maximum storage space in MB allocated for photos.
        /// </summary>
        public static readonly int PhotoMaxStorageMB;
        
        /// <summary>
        /// Number of days to retain photos before automatic cleanup.
        /// </summary>
        public static readonly int PhotoRetentionDays;
        
        /// <summary>
        /// Maximum length of activity report text.
        /// </summary>
        public static readonly int ReportMaxLength;
        
        /// <summary>
        /// Number of days to retain activity reports before automatic cleanup.
        /// </summary>
        public static readonly int ReportRetentionDays;
        
        /// <summary>
        /// Number of days to retain time records before automatic cleanup.
        /// </summary>
        public static readonly int TimeRecordRetentionDays;
        
        /// <summary>
        /// Interval in minutes between sync attempts when online.
        /// </summary>
        public static readonly int SyncIntervalMinutes;
        
        /// <summary>
        /// Maximum number of retry attempts for failed synchronization.
        /// </summary>
        public static readonly int SyncRetryMaxAttempts;
        
        /// <summary>
        /// Initial delay in seconds before first retry attempt.
        /// </summary>
        public static readonly int SyncRetryInitialDelaySeconds;
        
        /// <summary>
        /// Number of consecutive failures before triggering circuit breaker.
        /// </summary>
        public static readonly int SyncCircuitBreakerThreshold;
        
        /// <summary>
        /// Time in minutes before resetting circuit breaker after triggering.
        /// </summary>
        public static readonly int SyncCircuitBreakerResetMinutes;
        
        /// <summary>
        /// Timeout in seconds for API requests.
        /// </summary>
        public static readonly int ApiTimeoutSeconds;
        
        /// <summary>
        /// Maximum number of retries for API requests.
        /// </summary>
        public static readonly int ApiMaxRetries;
        
        /// <summary>
        /// Expiry time in minutes for authentication tokens.
        /// </summary>
        public static readonly int AuthTokenExpiryMinutes;
        
        /// <summary>
        /// Expiry time in minutes for verification codes.
        /// </summary>
        public static readonly int VerificationCodeExpiryMinutes;
        
        /// <summary>
        /// Flag to enable/disable telemetry collection.
        /// </summary>
        public static readonly bool EnableTelemetry;
        
        /// <summary>
        /// Flag to enable/disable crash reporting.
        /// </summary>
        public static readonly bool EnableCrashReporting;
        
        /// <summary>
        /// Default date and time format string.
        /// </summary>
        public static readonly string DefaultDateTimeFormat;
        
        /// <summary>
        /// Default time format string.
        /// </summary>
        public static readonly string DefaultTimeFormat;
        
        /// <summary>
        /// Maximum storage space in MB for offline data.
        /// </summary>
        public static readonly int MaxOfflineStorageMB;
        
        /// <summary>
        /// Threshold in MB to trigger low storage warnings.
        /// </summary>
        public static readonly int LowStorageThresholdMB;
        
        /// <summary>
        /// Delay in minutes before restarting a background service after failure.
        /// </summary>
        public static readonly int BackgroundServiceRestartDelayMinutes;
        
        /// <summary>
        /// Maximum acceptable battery impact percentage per hour.
        /// </summary>
        public static readonly int MaxBatteryImpactPercent;
        
        /// <summary>
        /// Static constructor that initializes all constant values.
        /// </summary>
        static AppConstants()
        {
            AppName = "Security Patrol";
            AppVersion = "1.0.0";
            DatabaseName = "securitypatrol.db";
            DatabaseVersion = 1;
            
            // Location tracking settings
            LocationTrackingIntervalDefault = 60;            // 60 seconds
            LocationTrackingIntervalLowPower = 120;          // 2 minutes
            LocationTrackingIntervalHighAccuracy = 30;       // 30 seconds
            LocationBatchSize = 50;                          // 50 points per batch
            LocationMaxStoredPoints = 10000;                 // 10,000 points max
            LocationProximityThresholdMeters = 15.0;         // 15 meters
            CheckpointProximityThresholdFeet = 50.0;         // 50 feet
            
            // Photo settings
            PhotoCompressionQuality = 80;                    // 80% quality
            PhotoMaxStorageMB = 500;                         // 500 MB
            PhotoRetentionDays = 30;                         // 30 days
            
            // Report settings
            ReportMaxLength = 500;                           // 500 characters
            ReportRetentionDays = 90;                        // 90 days
            
            // Time record settings
            TimeRecordRetentionDays = 90;                    // 90 days
            
            // Synchronization settings
            SyncIntervalMinutes = 15;                        // 15 minutes
            SyncRetryMaxAttempts = 3;                        // 3 attempts
            SyncRetryInitialDelaySeconds = 5;                // 5 seconds
            SyncCircuitBreakerThreshold = 5;                 // 5 failures
            SyncCircuitBreakerResetMinutes = 30;             // 30 minutes
            
            // API settings
            ApiTimeoutSeconds = 30;                          // 30 seconds
            ApiMaxRetries = 3;                               // 3 retries
            
            // Authentication settings
            AuthTokenExpiryMinutes = 480;                    // 8 hours
            VerificationCodeExpiryMinutes = 10;              // 10 minutes
            
            // Telemetry settings
            EnableTelemetry = true;
            EnableCrashReporting = true;
            
            // Format settings
            DefaultDateTimeFormat = "yyyy-MM-dd HH:mm:ss";
            DefaultTimeFormat = "HH:mm:ss";
            
            // Storage settings
            MaxOfflineStorageMB = 1000;                      // 1 GB
            LowStorageThresholdMB = 100;                     // 100 MB
            
            // Service settings
            BackgroundServiceRestartDelayMinutes = 5;        // 5 minutes
            MaxBatteryImpactPercent = 15;                    // 15% per hour
        }
    }
}