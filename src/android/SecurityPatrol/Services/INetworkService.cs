using System;
using System.Threading.Tasks;
using SecurityPatrol.Models;

namespace SecurityPatrol.Services
{
    /// <summary>
    /// Defines types of network operations with different connectivity requirements
    /// </summary>
    public enum NetworkOperationType
    {
        /// <summary>
        /// Critical operations that should only be attempted with good connectivity
        /// </summary>
        Critical,

        /// <summary>
        /// Important operations that can be attempted with fair connectivity
        /// </summary>
        Important,

        /// <summary>
        /// Standard operations that require connectivity but are not time-sensitive
        /// </summary>
        Standard,

        /// <summary>
        /// Background operations that can be deferred if connectivity is poor
        /// </summary>
        Background,

        /// <summary>
        /// Operations that should be attempted regardless of connectivity state
        /// </summary>
        Required
    }

    /// <summary>
    /// Interface that defines the contract for network connectivity monitoring and management.
    /// Provides methods to check network status, monitor connectivity changes, and determine
    /// if network operations should be attempted based on current conditions.
    /// </summary>
    public interface INetworkService
    {
        /// <summary>
        /// Gets a value indicating whether the device is currently connected to a network.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Event that is triggered when the network connectivity status changes.
        /// </summary>
        event EventHandler<ConnectivityChangedEventArgs> ConnectivityChanged;

        /// <summary>
        /// Gets the current connection type (WiFi, Cellular, etc.).
        /// </summary>
        /// <returns>A string representation of the current connection type.</returns>
        string GetConnectionType();

        /// <summary>
        /// Estimates the quality of the current network connection.
        /// </summary>
        /// <returns>An enum value representing connection quality (Excellent, Good, Fair, Poor, None).</returns>
        ConnectionQuality GetConnectionQuality();

        /// <summary>
        /// Starts monitoring network connectivity changes.
        /// </summary>
        void StartMonitoring();

        /// <summary>
        /// Stops monitoring network connectivity changes.
        /// </summary>
        void StopMonitoring();

        /// <summary>
        /// Determines if a network operation should be attempted based on current connectivity.
        /// </summary>
        /// <param name="operationType">The type of operation to be performed.</param>
        /// <returns>True if the operation should be attempted, false otherwise.</returns>
        bool ShouldAttemptOperation(NetworkOperationType operationType);
    }
}