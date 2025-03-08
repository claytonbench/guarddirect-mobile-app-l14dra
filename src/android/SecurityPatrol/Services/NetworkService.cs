using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SecurityPatrol.Helpers;
using SecurityPatrol.Models;
using SecurityPatrol.Constants;

namespace SecurityPatrol.Services
{
    /// <summary>
    /// Implements the INetworkService interface to provide network connectivity monitoring 
    /// and management for the Security Patrol Application.
    /// </summary>
    public class NetworkService : INetworkService
    {
        private readonly ILogger<NetworkService> _logger;
        private bool _isMonitoring;

        /// <summary>
        /// Gets a value indicating whether the device is currently connected to a network.
        /// </summary>
        public bool IsConnected { get; private set; }

        /// <summary>
        /// Event that is triggered when the network connectivity status changes.
        /// </summary>
        public event EventHandler<ConnectivityChangedEventArgs> ConnectivityChanged;

        /// <summary>
        /// Initializes a new instance of the NetworkService class with required dependencies.
        /// </summary>
        /// <param name="logger">The logger instance used for logging network-related events.</param>
        public NetworkService(ILogger<NetworkService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            IsConnected = ConnectivityHelper.IsConnected();
            _isMonitoring = false;
            
            _logger.LogInformation("NetworkService initialized. Current connection status: {Status}", 
                IsConnected ? "Connected" : "Disconnected");
        }

        /// <summary>
        /// Gets the current connection type (WiFi, Cellular, etc.).
        /// </summary>
        /// <returns>A string representation of the current connection type.</returns>
        public string GetConnectionType()
        {
            return ConnectivityHelper.GetConnectionType();
        }

        /// <summary>
        /// Estimates the quality of the current network connection.
        /// </summary>
        /// <returns>An enum value representing connection quality (Excellent, Good, Fair, Poor, None).</returns>
        public ConnectionQuality GetConnectionQuality()
        {
            return ConnectivityHelper.GetConnectionQuality();
        }

        /// <summary>
        /// Starts monitoring network connectivity changes.
        /// </summary>
        public void StartMonitoring()
        {
            if (_isMonitoring)
            {
                _logger.LogDebug("Network monitoring is already active");
                return;
            }

            _logger.LogInformation("Starting network connectivity monitoring");
            ConnectivityHelper.RegisterForConnectivityChanges(HandleConnectivityChanged);
            _isMonitoring = true;
        }

        /// <summary>
        /// Stops monitoring network connectivity changes.
        /// </summary>
        public void StopMonitoring()
        {
            if (!_isMonitoring)
            {
                _logger.LogDebug("Network monitoring is not active");
                return;
            }

            _logger.LogInformation("Stopping network connectivity monitoring");
            ConnectivityHelper.UnregisterFromConnectivityChanges(HandleConnectivityChanged);
            _isMonitoring = false;
        }

        /// <summary>
        /// Determines if a network operation should be attempted based on current connectivity.
        /// </summary>
        /// <param name="operationType">The type of operation to be performed.</param>
        /// <returns>True if the operation should be attempted, false otherwise.</returns>
        public bool ShouldAttemptOperation(NetworkOperationType operationType)
        {
            var helperOperationType = ConvertToHelperOperationType(operationType);
            return ConnectivityHelper.ShouldAttemptOperation(helperOperationType);
        }

        /// <summary>
        /// Converts from the INetworkService NetworkOperationType to the ConnectivityHelper NetworkOperationType.
        /// </summary>
        /// <param name="operationType">The NetworkOperationType from INetworkService.</param>
        /// <returns>The corresponding NetworkOperationType from ConnectivityHelper.</returns>
        private Helpers.NetworkOperationType ConvertToHelperOperationType(NetworkOperationType operationType)
        {
            switch (operationType)
            {
                case NetworkOperationType.Critical:
                    return Helpers.NetworkOperationType.Authentication;
                case NetworkOperationType.Important:
                    return Helpers.NetworkOperationType.ClockEvent;
                case NetworkOperationType.Standard:
                    return Helpers.NetworkOperationType.ReportSync;
                case NetworkOperationType.Background:
                    return Helpers.NetworkOperationType.LocationSync;
                case NetworkOperationType.Required:
                    return Helpers.NetworkOperationType.Authentication;
                default:
                    return Helpers.NetworkOperationType.DataDownload;
            }
        }

        /// <summary>
        /// Handles connectivity change events from the ConnectivityHelper.
        /// </summary>
        /// <param name="args">The connectivity change event arguments.</param>
        private void HandleConnectivityChanged(ConnectivityChangedEventArgs args)
        {
            IsConnected = args.IsConnected;
            
            _logger.LogInformation(
                "Network connectivity changed: Connected={Connected}, Type={Type}, Quality={Quality}", 
                args.IsConnected, 
                args.ConnectionType, 
                args.ConnectionQuality);
            
            OnConnectivityChanged(args);
        }

        /// <summary>
        /// Raises the ConnectivityChanged event.
        /// </summary>
        /// <param name="args">The connectivity change event arguments.</param>
        protected virtual void OnConnectivityChanged(ConnectivityChangedEventArgs args)
        {
            ConnectivityChanged?.Invoke(this, args);
        }
    }
}