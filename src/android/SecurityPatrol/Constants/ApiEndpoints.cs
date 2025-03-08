using System; // System 8.0.0

namespace SecurityPatrol.Constants
{
    /// <summary>
    /// Contains constant values for all API endpoints used in the Security Patrol application.
    /// This class centralizes all endpoint URLs to ensure consistency across the application 
    /// and simplify maintenance when API routes change.
    /// </summary>
    public static class ApiEndpoints
    {
        /// <summary>
        /// The base URL of the API.
        /// </summary>
        public static string BaseUrl { get; private set; }

        /// <summary>
        /// The API version to use (e.g., "v1").
        /// </summary>
        public static string ApiVersion { get; private set; }

        /// <summary>
        /// Endpoint for requesting a verification code for phone authentication.
        /// </summary>
        public static string AuthVerify { get; private set; }

        /// <summary>
        /// Endpoint for validating a verification code during authentication.
        /// </summary>
        public static string AuthValidate { get; private set; }

        /// <summary>
        /// Endpoint for refreshing an authentication token.
        /// </summary>
        public static string AuthRefresh { get; private set; }

        /// <summary>
        /// Endpoint for recording clock in/out events.
        /// </summary>
        public static string TimeClock { get; private set; }

        /// <summary>
        /// Endpoint for retrieving clock history.
        /// </summary>
        public static string TimeHistory { get; private set; }

        /// <summary>
        /// Endpoint for uploading batches of location data.
        /// </summary>
        public static string LocationBatch { get; private set; }

        /// <summary>
        /// Endpoint for retrieving the current location.
        /// </summary>
        public static string LocationCurrent { get; private set; }

        /// <summary>
        /// Endpoint for uploading photos captured in the app.
        /// </summary>
        public static string PhotosUpload { get; private set; }

        /// <summary>
        /// Endpoint for managing photos.
        /// </summary>
        public static string Photos { get; private set; }

        /// <summary>
        /// Endpoint for submitting and retrieving activity reports.
        /// </summary>
        public static string Reports { get; private set; }

        /// <summary>
        /// Endpoint for retrieving available patrol locations.
        /// </summary>
        public static string PatrolLocations { get; private set; }

        /// <summary>
        /// Endpoint for retrieving checkpoints for a specific location.
        /// </summary>
        public static string PatrolCheckpoints { get; private set; }

        /// <summary>
        /// Endpoint for verifying checkpoint completion.
        /// </summary>
        public static string PatrolVerify { get; private set; }

        /// <summary>
        /// Static constructor that initializes the API endpoint constants.
        /// </summary>
        static ApiEndpoints()
        {
            // Base API configuration
            #if DEBUG
            BaseUrl = "https://api-dev.securitypatrol.com";
            #else
            BaseUrl = "https://api.securitypatrol.com";
            #endif
            
            ApiVersion = "v1";
            string baseApiPath = $"{BaseUrl}/api/{ApiVersion}";

            // Authentication endpoints
            AuthVerify = $"{baseApiPath}/auth/verify";
            AuthValidate = $"{baseApiPath}/auth/validate";
            AuthRefresh = $"{baseApiPath}/auth/refresh";

            // Time tracking endpoints
            TimeClock = $"{baseApiPath}/time/clock";
            TimeHistory = $"{baseApiPath}/time/history";

            // Location endpoints
            LocationBatch = $"{baseApiPath}/location/batch";
            LocationCurrent = $"{baseApiPath}/location/current";

            // Photo endpoints
            PhotosUpload = $"{baseApiPath}/photos/upload";
            Photos = $"{baseApiPath}/photos";

            // Report endpoints
            Reports = $"{baseApiPath}/reports";

            // Patrol endpoints
            PatrolLocations = $"{baseApiPath}/patrol/locations";
            PatrolCheckpoints = $"{baseApiPath}/patrol/checkpoints";
            PatrolVerify = $"{baseApiPath}/patrol/verify";
        }
    }
}