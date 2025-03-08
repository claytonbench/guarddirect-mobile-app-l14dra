// System 8.0+
// System.EventArgs 8.0+

using System;

namespace SecurityPatrol.Models
{
    /// <summary>
    /// Represents the quality level of a network connection.
    /// </summary>
    public enum ConnectionQuality
    {
        /// <summary>
        /// No network connection is available.
        /// </summary>
        None,

        /// <summary>
        /// Poor connection quality with high latency and low bandwidth.
        /// </summary>
        Poor,

        /// <summary>
        /// Fair connection quality with moderate latency and bandwidth.
        /// </summary>
        Fair,

        /// <summary>
        /// Good connection quality with low latency and good bandwidth.
        /// </summary>
        Good,

        /// <summary>
        /// Excellent connection quality with very low latency and high bandwidth.
        /// </summary>
        Excellent
    }

    /// <summary>
    /// Provides event data for network connectivity change events.
    /// </summary>
    public class ConnectivityChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets a value indicating whether the device is currently connected to the network.
        /// </summary>
        public bool IsConnected { get; private set; }

        /// <summary>
        /// Gets the type of connection (e.g., "WiFi", "Cellular", "Ethernet", etc.).
        /// </summary>
        public string ConnectionType { get; private set; }

        /// <summary>
        /// Gets the quality of the current network connection.
        /// </summary>
        public ConnectionQuality ConnectionQuality { get; private set; }

        /// <summary>
        /// Gets the UTC timestamp when the connectivity change was detected.
        /// </summary>
        public DateTime Timestamp { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectivityChangedEventArgs"/> class.
        /// </summary>
        /// <param name="isConnected">Whether the device is currently connected to a network.</param>
        /// <param name="connectionType">The type of connection (e.g., "WiFi", "Cellular").</param>
        /// <param name="connectionQuality">The quality of the current network connection.</param>
        public ConnectivityChangedEventArgs(bool isConnected, string connectionType, ConnectionQuality connectionQuality)
            : base()
        {
            IsConnected = isConnected;
            ConnectionType = connectionType ?? string.Empty;
            ConnectionQuality = connectionQuality;
            Timestamp = DateTime.UtcNow;
        }
    }
}