using System;
using System.Threading;
using System.Threading.Tasks;
using SecurityPatrol.Models;
using SecurityPatrol.Helpers;

namespace SecurityPatrol.TestCommon.Helpers
{
    /// <summary>
    /// Interface defining methods for controlling network conditions in tests
    /// </summary>
    public interface INetworkConditionControl
    {
        /// <summary>
        /// Sets whether the network is connected or not
        /// </summary>
        /// <param name="isConnected">Whether network is connected</param>
        void SetNetworkConnected(bool isConnected);

        /// <summary>
        /// Sets the quality of the network connection
        /// </summary>
        /// <param name="quality">Connection quality level</param>
        void SetConnectionQuality(ConnectionQuality quality);

        /// <summary>
        /// Sets the type of network connection (WiFi, Cellular, etc.)
        /// </summary>
        /// <param name="connectionType">Connection type string</param>
        void SetConnectionType(string connectionType);

        /// <summary>
        /// Simulates a connectivity change event
        /// </summary>
        void SimulateConnectivityChange();

        /// <summary>
        /// Sets whether a specific network operation type is allowed to succeed
        /// </summary>
        /// <param name="operationType">Type of network operation</param>
        /// <param name="isAllowed">Whether operation is allowed to succeed</param>
        void SetOperationAllowance(NetworkOperationType operationType, bool isAllowed);
    }

    /// <summary>
    /// Enumeration of predefined network scenarios for testing
    /// </summary>
    public enum NetworkScenario
    {
        /// <summary>
        /// Standard network scenario with typical condition changes
        /// </summary>
        Default = 0,

        /// <summary>
        /// Simulates rural area connectivity with intermittent and low quality connections
        /// </summary>
        RuralArea = 1,

        /// <summary>
        /// Simulates connectivity while in a moving vehicle with changing signal strength
        /// </summary>
        MovingVehicle = 2,

        /// <summary>
        /// Simulates connectivity inside buildings with signal degradation in different areas
        /// </summary>
        BuildingInterior = 3,

        /// <summary>
        /// Simulates network congestion with high latency but stable connection
        /// </summary>
        NetworkCongestion = 4
    }

    /// <summary>
    /// A utility class that provides methods to simulate various network conditions during testing
    /// </summary>
    public static class NetworkConditionSimulator
    {
        /// <summary>
        /// Simulates a complete loss of network connectivity
        /// </summary>
        /// <param name="networkControl">Network condition control interface</param>
        public static void SimulateNetworkLoss(INetworkConditionControl networkControl)
        {
            if (networkControl == null)
                throw new ArgumentNullException(nameof(networkControl));

            networkControl.SetNetworkConnected(false);
            Console.WriteLine("Network loss simulated");
        }

        /// <summary>
        /// Simulates the restoration of network connectivity after a loss
        /// </summary>
        /// <param name="networkControl">Network condition control interface</param>
        /// <param name="quality">Quality of the restored connection</param>
        public static void SimulateNetworkRestoration(
            INetworkConditionControl networkControl, 
            ConnectionQuality quality = ConnectionQuality.Good)
        {
            if (networkControl == null)
                throw new ArgumentNullException(nameof(networkControl));

            networkControl.SetNetworkConnected(true);
            networkControl.SetConnectionQuality(quality);
            networkControl.SimulateConnectivityChange();
            Console.WriteLine($"Network restored with quality: {quality}");
        }

        /// <summary>
        /// Simulates a change in network connection quality
        /// </summary>
        /// <param name="networkControl">Network condition control interface</param>
        /// <param name="quality">New connection quality level</param>
        public static void SimulateNetworkQualityChange(
            INetworkConditionControl networkControl, 
            ConnectionQuality quality)
        {
            if (networkControl == null)
                throw new ArgumentNullException(nameof(networkControl));

            networkControl.SetConnectionQuality(quality);
            networkControl.SimulateConnectivityChange();
            Console.WriteLine($"Network quality changed to: {quality}");
        }

        /// <summary>
        /// Simulates intermittent network connectivity with specified pattern
        /// </summary>
        /// <param name="networkControl">Network condition control interface</param>
        /// <param name="cycles">Number of on/off cycles to simulate</param>
        /// <param name="onDurationMs">Duration in milliseconds of 'connected' state per cycle</param>
        /// <param name="offDurationMs">Duration in milliseconds of 'disconnected' state per cycle</param>
        /// <returns>A task that completes when the simulation is finished</returns>
        public static async Task SimulateIntermittentConnectivity(
            INetworkConditionControl networkControl,
            int cycles = 5,
            int onDurationMs = 5000,
            int offDurationMs = 3000)
        {
            if (networkControl == null)
                throw new ArgumentNullException(nameof(networkControl));
            if (cycles <= 0)
                throw new ArgumentOutOfRangeException(nameof(cycles), "Cycles must be greater than zero");
            if (onDurationMs <= 0)
                throw new ArgumentOutOfRangeException(nameof(onDurationMs), "Duration must be greater than zero");
            if (offDurationMs <= 0)
                throw new ArgumentOutOfRangeException(nameof(offDurationMs), "Duration must be greater than zero");

            Console.WriteLine($"Starting intermittent connectivity simulation: {cycles} cycles, on={onDurationMs}ms, off={offDurationMs}ms");

            for (int i = 0; i < cycles; i++)
            {
                // Connected period
                networkControl.SetNetworkConnected(true);
                networkControl.SimulateConnectivityChange();
                Console.WriteLine($"Cycle {i+1}/{cycles}: Network connected");
                await Task.Delay(onDurationMs);

                // Disconnected period
                networkControl.SetNetworkConnected(false);
                networkControl.SimulateConnectivityChange();
                Console.WriteLine($"Cycle {i+1}/{cycles}: Network disconnected");
                await Task.Delay(offDurationMs);
            }

            // Restore connectivity at the end
            networkControl.SetNetworkConnected(true);
            networkControl.SimulateConnectivityChange();
            Console.WriteLine("Intermittent connectivity simulation completed");
        }

        /// <summary>
        /// Configures the network control to simulate network latency
        /// </summary>
        /// <param name="networkControl">Network condition control interface</param>
        /// <param name="latencyMs">Latency in milliseconds to simulate</param>
        public static void SimulateNetworkLatency(
            INetworkConditionControl networkControl, 
            int latencyMs)
        {
            if (networkControl == null)
                throw new ArgumentNullException(nameof(networkControl));
            if (latencyMs < 0)
                throw new ArgumentOutOfRangeException(nameof(latencyMs), "Latency cannot be negative");

            // This method would typically set a property on the network control
            // implementation that would delay responses by the specified amount
            // For this implementation, we just log that it's been configured
            Console.WriteLine($"Network latency simulation configured: {latencyMs}ms");

            // Adjust connection quality based on latency
            ConnectionQuality quality = ConnectionQuality.Excellent;
            if (latencyMs > 1000)
                quality = ConnectionQuality.Poor;
            else if (latencyMs > 500)
                quality = ConnectionQuality.Fair;
            else if (latencyMs > 100)
                quality = ConnectionQuality.Good;

            networkControl.SetConnectionQuality(quality);
            networkControl.SimulateConnectivityChange();
        }

        /// <summary>
        /// Configures specific network operation types to fail
        /// </summary>
        /// <param name="networkControl">Network condition control interface</param>
        /// <param name="operationType">Type of network operation to configure</param>
        /// <param name="shouldFail">Whether the operation should fail</param>
        public static void SimulateOperationFailure(
            INetworkConditionControl networkControl,
            NetworkOperationType operationType,
            bool shouldFail)
        {
            if (networkControl == null)
                throw new ArgumentNullException(nameof(networkControl));

            networkControl.SetOperationAllowance(operationType, !shouldFail);
            Console.WriteLine($"Operation failure simulation configured: {operationType} {(shouldFail ? "will fail" : "will succeed")}");
        }

        /// <summary>
        /// Simulates a change in network connection type (WiFi, Cellular, etc.)
        /// </summary>
        /// <param name="networkControl">Network condition control interface</param>
        /// <param name="connectionType">New connection type</param>
        public static void SimulateConnectionTypeChange(
            INetworkConditionControl networkControl,
            string connectionType)
        {
            if (networkControl == null)
                throw new ArgumentNullException(nameof(networkControl));
            if (string.IsNullOrEmpty(connectionType))
                throw new ArgumentException("Connection type cannot be null or empty", nameof(connectionType));

            networkControl.SetConnectionType(connectionType);
            networkControl.SimulateConnectivityChange();
            Console.WriteLine($"Connection type changed to: {connectionType}");
        }

        /// <summary>
        /// Simulates a complex network scenario with multiple condition changes
        /// </summary>
        /// <param name="networkControl">Network condition control interface</param>
        /// <param name="scenario">The network scenario to simulate</param>
        /// <returns>A task that completes when the scenario simulation is finished</returns>
        public static async Task SimulateNetworkScenario(
            INetworkConditionControl networkControl,
            NetworkScenario scenario)
        {
            if (networkControl == null)
                throw new ArgumentNullException(nameof(networkControl));

            Console.WriteLine($"Starting network scenario simulation: {scenario}");

            switch (scenario)
            {
                case NetworkScenario.RuralArea:
                    // Rural area has intermittent and low quality connectivity
                    networkControl.SetConnectionQuality(ConnectionQuality.Poor);
                    networkControl.SetConnectionType("Cellular");
                    networkControl.SimulateConnectivityChange();
                    await Task.Delay(2000);
                    
                    await SimulateIntermittentConnectivity(networkControl, 3, 10000, 5000);
                    break;

                case NetworkScenario.MovingVehicle:
                    // Moving vehicle has changing quality and connection types
                    networkControl.SetConnectionQuality(ConnectionQuality.Good);
                    networkControl.SetConnectionType("Cellular");
                    networkControl.SimulateConnectivityChange();
                    await Task.Delay(3000);
                    
                    networkControl.SetConnectionQuality(ConnectionQuality.Fair);
                    networkControl.SimulateConnectivityChange();
                    await Task.Delay(2000);
                    
                    networkControl.SetConnectionQuality(ConnectionQuality.Poor);
                    networkControl.SimulateConnectivityChange();
                    await Task.Delay(1500);
                    
                    networkControl.SetNetworkConnected(false);
                    networkControl.SimulateConnectivityChange();
                    await Task.Delay(3000);
                    
                    networkControl.SetNetworkConnected(true);
                    networkControl.SetConnectionQuality(ConnectionQuality.Poor);
                    networkControl.SimulateConnectivityChange();
                    await Task.Delay(2000);
                    
                    networkControl.SetConnectionType("WiFi");
                    networkControl.SetConnectionQuality(ConnectionQuality.Good);
                    networkControl.SimulateConnectivityChange();
                    break;

                case NetworkScenario.BuildingInterior:
                    // Building interior has signal degradation in different areas
                    networkControl.SetConnectionQuality(ConnectionQuality.Good);
                    networkControl.SetConnectionType("WiFi");
                    networkControl.SimulateConnectivityChange();
                    await Task.Delay(3000);
                    
                    // Moving to interior area with weaker signal
                    networkControl.SetConnectionQuality(ConnectionQuality.Fair);
                    networkControl.SimulateConnectivityChange();
                    await Task.Delay(2000);
                    
                    // Moving to deep interior with poor signal
                    networkControl.SetConnectionQuality(ConnectionQuality.Poor);
                    networkControl.SimulateConnectivityChange();
                    await Task.Delay(3000);
                    
                    // Moving to area with no WiFi coverage
                    networkControl.SetNetworkConnected(false);
                    networkControl.SimulateConnectivityChange();
                    await Task.Delay(2000);
                    
                    // Moving back to area with cellular coverage
                    networkControl.SetNetworkConnected(true);
                    networkControl.SetConnectionType("Cellular");
                    networkControl.SetConnectionQuality(ConnectionQuality.Poor);
                    networkControl.SimulateConnectivityChange();
                    await Task.Delay(2000);
                    
                    // Moving back to WiFi coverage area
                    networkControl.SetConnectionType("WiFi");
                    networkControl.SetConnectionQuality(ConnectionQuality.Good);
                    networkControl.SimulateConnectivityChange();
                    break;

                case NetworkScenario.NetworkCongestion:
                    // Network congestion has high latency but stable connection
                    networkControl.SetNetworkConnected(true);
                    networkControl.SetConnectionType("WiFi");
                    networkControl.SetConnectionQuality(ConnectionQuality.Fair);
                    networkControl.SimulateConnectivityChange();
                    
                    // Configure high latency
                    SimulateNetworkLatency(networkControl, 800);
                    await Task.Delay(5000);
                    
                    // Latency increases
                    SimulateNetworkLatency(networkControl, 1500);
                    await Task.Delay(5000);
                    
                    // Latency returns to normal
                    SimulateNetworkLatency(networkControl, 50);
                    break;

                case NetworkScenario.Default:
                default:
                    // Default scenario with standard sequence of changes
                    networkControl.SetNetworkConnected(true);
                    networkControl.SetConnectionType("WiFi");
                    networkControl.SetConnectionQuality(ConnectionQuality.Good);
                    networkControl.SimulateConnectivityChange();
                    await Task.Delay(2000);
                    
                    networkControl.SetConnectionQuality(ConnectionQuality.Fair);
                    networkControl.SimulateConnectivityChange();
                    await Task.Delay(2000);
                    
                    networkControl.SetNetworkConnected(false);
                    networkControl.SimulateConnectivityChange();
                    await Task.Delay(3000);
                    
                    networkControl.SetNetworkConnected(true);
                    networkControl.SetConnectionQuality(ConnectionQuality.Good);
                    networkControl.SimulateConnectivityChange();
                    break;
            }

            Console.WriteLine($"Network scenario simulation completed: {scenario}");
        }
    }
}