using System;
using System.Collections.Generic;
using SecurityPatrol.Models;
using SecurityPatrol.Services;
using SecurityPatrol.Helpers;
using SecurityPatrol.TestCommon.Helpers;

namespace SecurityPatrol.TestCommon.Mocks
{
    /// <summary>
    /// Mock implementation of INetworkService that simulates network connectivity for testing purposes
    /// </summary>
    public class MockNetworkService : INetworkService, INetworkConditionControl
    {
        /// <summary>
        /// Gets a value indicating whether the device is currently connected to a network.
        /// </summary>
        public bool IsConnected { get; private set; }
        
        private string _connectionType;
        private ConnectionQuality _connectionQuality;
        private bool _isMonitoring;
        private Dictionary<NetworkOperationType, bool> _operationAllowances;

        /// <summary>
        /// Event that is triggered when the network connectivity status changes.
        /// </summary>
        public event EventHandler<ConnectivityChangedEventArgs> ConnectivityChanged;

        /// <summary>
        /// Initializes a new instance of the MockNetworkService class with default values
        /// </summary>
        public MockNetworkService()
        {
            // Set IsConnected to true by default
            IsConnected = true;
            // Set _connectionType to 'WiFi' by default
            _connectionType = "WiFi";
            // Set _connectionQuality to ConnectionQuality.High by default
            _connectionQuality = ConnectionQuality.High;
            // Set _isMonitoring to false
            _isMonitoring = false;
            // Initialize _operationAllowances dictionary with all operation types allowed by default
            _operationAllowances = new Dictionary<NetworkOperationType, bool>();
            
            foreach (NetworkOperationType type in Enum.GetValues(typeof(NetworkOperationType)))
            {
                _operationAllowances[type] = true;
            }
        }

        /// <summary>
        /// Gets the current simulated connection type
        /// </summary>
        /// <returns>The current connection type (WiFi, Cellular, etc.)</returns>
        public string GetConnectionType()
        {
            // Return the current _connectionType value
            return _connectionType;
        }

        /// <summary>
        /// Gets the current simulated connection quality
        /// </summary>
        /// <returns>The current connection quality</returns>
        public ConnectionQuality GetConnectionQuality()
        {
            // Return the current _connectionQuality value
            return _connectionQuality;
        }

        /// <summary>
        /// Simulates starting network connectivity monitoring
        /// </summary>
        public void StartMonitoring()
        {
            // Set _isMonitoring to true
            _isMonitoring = true;
        }

        /// <summary>
        /// Simulates stopping network connectivity monitoring
        /// </summary>
        public void StopMonitoring()
        {
            // Set _isMonitoring to false
            _isMonitoring = false;
        }

        /// <summary>
        /// Determines if a network operation should be attempted based on simulated conditions
        /// </summary>
        /// <param name="operationType">Type of operation to be performed</param>
        /// <returns>True if the operation should be attempted, false otherwise</returns>
        public bool ShouldAttemptOperation(NetworkOperationType operationType)
        {
            // If IsConnected is false, return false
            if (!IsConnected)
                return false;

            // If _operationAllowances contains the operationType, return its value
            if (_operationAllowances.TryGetValue(operationType, out bool isAllowed))
                return isAllowed;

            // Otherwise return true by default
            return true;
        }

        /// <summary>
        /// Sets the simulated network connection status
        /// </summary>
        /// <param name="isConnected">Whether network is connected</param>
        public void SetNetworkConnected(bool isConnected)
        {
            // If IsConnected already equals isConnected, return immediately (no change)
            if (IsConnected == isConnected)
                return;

            // Set IsConnected to isConnected parameter value
            IsConnected = isConnected;
            
            // If _isMonitoring is true, call SimulateConnectivityChange() to notify subscribers
            if (_isMonitoring)
                SimulateConnectivityChange();
        }

        /// <summary>
        /// Sets the simulated connection type
        /// </summary>
        /// <param name="connectionType">Connection type string</param>
        public void SetConnectionType(string connectionType)
        {
            // If _connectionType already equals connectionType, return immediately (no change)
            if (_connectionType == connectionType)
                return;

            // Set _connectionType to connectionType parameter value
            _connectionType = connectionType;
            
            // If _isMonitoring is true, call SimulateConnectivityChange() to notify subscribers
            if (_isMonitoring)
                SimulateConnectivityChange();
        }

        /// <summary>
        /// Sets the simulated connection quality
        /// </summary>
        /// <param name="quality">Connection quality level</param>
        public void SetConnectionQuality(ConnectionQuality quality)
        {
            // If _connectionQuality already equals quality, return immediately (no change)
            if (_connectionQuality == quality)
                return;

            // Set _connectionQuality to quality parameter value
            _connectionQuality = quality;
            
            // If _isMonitoring is true, call SimulateConnectivityChange() to notify subscribers
            if (_isMonitoring)
                SimulateConnectivityChange();
        }

        /// <summary>
        /// Sets whether a specific network operation type is allowed to succeed
        /// </summary>
        /// <param name="operationType">Type of network operation</param>
        /// <param name="isAllowed">Whether operation is allowed to succeed</param>
        public void SetOperationAllowance(NetworkOperationType operationType, bool isAllowed)
        {
            // Set the value for operationType in the _operationAllowances dictionary to isAllowed
            _operationAllowances[operationType] = isAllowed;
        }

        /// <summary>
        /// Simulates a connectivity change event and notifies subscribers
        /// </summary>
        public void SimulateConnectivityChange()
        {
            // Create a new ConnectivityChangedEventArgs with current IsConnected, _connectionType, and _connectionQuality values
            var args = new ConnectivityChangedEventArgs(IsConnected, _connectionType, _connectionQuality);
            
            // Call OnConnectivityChanged with the event args to notify subscribers
            OnConnectivityChanged(args);
        }

        /// <summary>
        /// Raises the ConnectivityChanged event
        /// </summary>
        /// <param name="args">Event arguments</param>
        protected virtual void OnConnectivityChanged(ConnectivityChangedEventArgs args)
        {
            // Create a local copy of the ConnectivityChanged event handler
            var handler = ConnectivityChanged;
            
            // If the handler is not null, invoke it with this instance and args
            handler?.Invoke(this, args);
        }
    }
}