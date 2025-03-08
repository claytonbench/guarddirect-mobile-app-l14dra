using System; // System package, version 8.0+
using System.Threading.Tasks; // System.Threading.Tasks package, version 8.0+
using System.Collections.Generic;
using Microsoft.Maui.Networking; // Microsoft.Maui.Networking package, version 8.0+
using Microsoft.Maui.Essentials; // Microsoft.Maui.Essentials package, version 8.0+
using Microsoft.Extensions.Logging; // Microsoft.Extensions.Logging package, version 8.0+
using SecurityPatrol.Models;
using SecurityPatrol.Constants;

namespace SecurityPatrol.Helpers
{
    /// <summary>
    /// Enumeration representing types of network operations
    /// </summary>
    public enum NetworkOperationType
    {
        /// <summary>
        /// Authentication operations (login, token refresh)
        /// </summary>
        Authentication,

        /// <summary>
        /// Clock in/out events
        /// </summary>
        ClockEvent,

        /// <summary>
        /// Location data synchronization
        /// </summary>
        LocationSync,

        /// <summary>
        /// Photo upload operations
        /// </summary>
        PhotoUpload,

        /// <summary>
        /// Activity report synchronization
        /// </summary>
        ReportSync,

        /// <summary>
        /// Checkpoint verification synchronization
        /// </summary>
        CheckpointSync,

        /// <summary>
        /// Data download operations (checkpoints, locations)
        /// </summary>
        DataDownload
    }

    /// <summary>
    /// Helper class that provides connectivity-related functionality for the Security Patrol Application
    /// </summary>
    public static class ConnectivityHelper
    {
        private static readonly ILogger _logger;
        private static readonly List<Action<ConnectivityChangedEventArgs>> _connectivityChangedHandlers;

        /// <summary>
        /// Static constructor that initializes the connectivity monitoring
        /// </summary>
        static ConnectivityHelper()
        {
            _connectivityChangedHandlers = new List<Action<ConnectivityChangedEventArgs>>();
            _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger(typeof(ConnectivityHelper).Name);
            
            // Subscribe to connectivity changes
            try
            {
                Connectivity.ConnectivityChanged += OnConnectivityChanged;
                _logger.LogInformation("ConnectivityHelper initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize ConnectivityHelper");
            }
        }

        /// <summary>
        /// Determines if the device currently has network connectivity
        /// </summary>
        /// <returns>True if the device has network connectivity, false otherwise</returns>
        public static bool IsConnected()
        {
            var networkAccess = Connectivity.NetworkAccess;
            return networkAccess == NetworkAccess.Internet || networkAccess == NetworkAccess.ConstrainedInternet;
        }

        /// <summary>
        /// Gets the current connection type (WiFi, Cellular, etc.)
        /// </summary>
        /// <returns>A string representation of the current connection type</returns>
        public static string GetConnectionType()
        {
            var profiles = Connectivity.ConnectionProfiles;
            
            if (profiles.Contains(ConnectionProfile.WiFi))
                return "WiFi";
            if (profiles.Contains(ConnectionProfile.Cellular))
                return "Cellular";
            if (profiles.Contains(ConnectionProfile.Ethernet))
                return "Ethernet";
            if (profiles.Contains(ConnectionProfile.Bluetooth))
                return "Bluetooth";
            
            return "Unknown";
        }

        /// <summary>
        /// Estimates the quality of the current network connection
        /// </summary>
        /// <returns>An enum value representing connection quality</returns>
        public static ConnectionQuality GetConnectionQuality()
        {
            if (!IsConnected())
                return ConnectionQuality.None;

            var connectionType = GetConnectionType();
            
            switch (connectionType)
            {
                case "WiFi":
                case "Ethernet":
                    // WiFi and Ethernet typically provide excellent connection quality
                    return ConnectionQuality.Excellent;
                
                case "Cellular":
                    // For cellular, try to determine the cellular network type (4G/LTE, 3G, etc.)
                    // This is a simplified approach as determining exact cellular type requires platform-specific code
                    return ConnectionQuality.Good;
                
                case "Bluetooth":
                    // Bluetooth typically provides fair connection quality
                    return ConnectionQuality.Fair;
                
                default:
                    // Default to poor if we can't determine the type
                    return ConnectionQuality.Poor;
            }
        }

        /// <summary>
        /// Registers a handler for connectivity change events
        /// </summary>
        /// <param name="handler">The handler to register</param>
        public static void RegisterForConnectivityChanges(Action<ConnectivityChangedEventArgs> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            
            lock (_connectivityChangedHandlers)
            {
                if (!_connectivityChangedHandlers.Contains(handler))
                {
                    _connectivityChangedHandlers.Add(handler);
                    _logger.LogDebug("Handler registered for connectivity changes");
                }
            }
        }

        /// <summary>
        /// Unregisters a handler from connectivity change events
        /// </summary>
        /// <param name="handler">The handler to unregister</param>
        public static void UnregisterFromConnectivityChanges(Action<ConnectivityChangedEventArgs> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            
            lock (_connectivityChangedHandlers)
            {
                if (_connectivityChangedHandlers.Contains(handler))
                {
                    _connectivityChangedHandlers.Remove(handler);
                    _logger.LogDebug("Handler unregistered from connectivity changes");
                }
            }
        }

        /// <summary>
        /// Determines if a network operation should be attempted based on current connectivity
        /// </summary>
        /// <param name="operationType">The type of network operation</param>
        /// <returns>True if the operation should be attempted, false otherwise</returns>
        public static bool ShouldAttemptOperation(NetworkOperationType operationType)
        {
            bool isConnected = IsConnected();
            if (!isConnected)
            {
                // If no connectivity, only allow authentication operations as they might help restore connectivity
                return operationType == NetworkOperationType.Authentication;
            }
            
            var quality = GetConnectionQuality();
            
            // Different operations have different quality requirements and timeout considerations
            switch (operationType)
            {
                case NetworkOperationType.Authentication:
                    // Authentication is critical, allow on any connection
                    // But use shorter timeout for authentication to prevent long waits
                    return true;
                
                case NetworkOperationType.ClockEvent:
                    // Clock events are important, allow on fair or better connection
                    return quality >= ConnectionQuality.Fair;
                
                case NetworkOperationType.PhotoUpload:
                    // Photo uploads are bandwidth intensive, require good connection
                    // May need longer timeouts, so check that against constants
                    return quality >= ConnectionQuality.Good && AppConstants.ApiTimeoutSeconds >= 60;
                
                case NetworkOperationType.LocationSync:
                    // Location sync is less bandwidth intensive but may involve many retries
                    return quality >= ConnectionQuality.Poor && 
                           AppConstants.SyncRetryMaxAttempts > 0;
                
                case NetworkOperationType.ReportSync:
                    // Reports are text-based, allow on fair or better connection
                    return quality >= ConnectionQuality.Fair;
                
                case NetworkOperationType.CheckpointSync:
                    // Checkpoint verifications are small, allow on poor or better
                    return quality >= ConnectionQuality.Poor;
                
                case NetworkOperationType.DataDownload:
                    // Data downloads may be large, require good or better connection
                    // and sufficient timeout
                    return quality >= ConnectionQuality.Good && 
                           AppConstants.ApiTimeoutSeconds >= 30;
                
                default:
                    // Default to requiring a fair connection
                    return quality >= ConnectionQuality.Fair;
            }
        }

        /// <summary>
        /// Handles connectivity change events from the MAUI Connectivity API
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private static void OnConnectivityChanged(object sender, EventArgs e)
        {
            bool isConnected = IsConnected();
            string connectionType = GetConnectionType();
            ConnectionQuality quality = GetConnectionQuality();
            
            _logger.LogInformation($"Connectivity changed: Connected={isConnected}, Type={connectionType}, Quality={quality}");
            
            var args = new ConnectivityChangedEventArgs(isConnected, connectionType, quality);
            NotifyHandlers(args);
        }

        /// <summary>
        /// Notifies all registered handlers of a connectivity change
        /// </summary>
        /// <param name="args">The event arguments</param>
        private static void NotifyHandlers(ConnectivityChangedEventArgs args)
        {
            List<Action<ConnectivityChangedEventArgs>> handlers;
            
            lock (_connectivityChangedHandlers)
            {
                // Create a copy of the handlers list to avoid modification during iteration
                handlers = new List<Action<ConnectivityChangedEventArgs>>(_connectivityChangedHandlers);
            }
            
            foreach (var handler in handlers)
            {
                try
                {
                    handler(args);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in connectivity change handler");
                }
            }
        }
    }
}