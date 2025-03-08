using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using SecurityPatrol.MAUI.UnitTests.Setup;

namespace SecurityPatrol.MAUI.UnitTests.Services
{
    /// <summary>
    /// Test class for the NetworkService implementation that handles network connectivity monitoring and management
    /// </summary>
    public class NetworkServiceTests : TestBase
    {
        private Mock<ILogger<NetworkService>> mockLogger;
        private NetworkService networkService;
        private bool eventWasRaised;
        private ConnectivityChangedEventArgs lastEventArgs;

        /// <summary>
        /// Initializes a new instance of the NetworkServiceTests class with test setup
        /// </summary>
        public NetworkServiceTests()
        {
            // Initialize mocks
            mockLogger = new Mock<ILogger<NetworkService>>();
            
            // Initialize the service with mocked dependencies
            networkService = new NetworkService(mockLogger.Object);
            
            // Setup for event testing
            eventWasRaised = false;
            lastEventArgs = null;
            
            // Setup ConnectivityHelper mock using MockHelpers
            SetupConnectivityHelperMock(true, "WiFi", ConnectionQuality.High);
        }

        /// <summary>
        /// Sets up mocks for the static ConnectivityHelper methods
        /// </summary>
        private void SetupConnectivityHelperMock(bool isConnected, string connectionType, ConnectionQuality connectionQuality)
        {
            // Mock ConnectivityHelper.IsConnected() to return specified value
            MockHelpers.SetupStatic(typeof(ConnectivityHelper), "IsConnected", () => isConnected);
            
            // Mock ConnectivityHelper.GetConnectionType() to return specified value
            MockHelpers.SetupStatic(typeof(ConnectivityHelper), "GetConnectionType", () => connectionType);
            
            // Mock ConnectivityHelper.GetConnectionQuality() to return specified value
            MockHelpers.SetupStatic(typeof(ConnectivityHelper), "GetConnectionQuality", () => connectionQuality);
            
            // Mock ConnectivityHelper.ShouldAttemptOperation() to return appropriate value
            MockHelpers.SetupStatic(typeof(ConnectivityHelper), "ShouldAttemptOperation", 
                (NetworkOperationType operationType) => 
                    connectionQuality != ConnectionQuality.None && 
                    (operationType == NetworkOperationType.Authentication || connectionQuality >= ConnectionQuality.Medium));
        }

        /// <summary>
        /// Event handler for ConnectivityChanged events during testing
        /// </summary>
        private void HandleConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
        {
            eventWasRaised = true;
            lastEventArgs = e;
        }

        /// <summary>
        /// Tests that the constructor properly initializes properties
        /// </summary>
        [Fact]
        public void Test_Constructor_InitializesProperties()
        {
            // Arrange - setup in constructor

            // Act - constructor already called in setup

            // Assert - verify IsConnected property is initialized correctly
            networkService.IsConnected.Should().BeTrue();
            
            // Assert - verify _isMonitoring field is initialized to false (using reflection)
            var isMonitoringField = typeof(NetworkService).GetField("_isMonitoring", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            isMonitoringField.GetValue(networkService).Should().Be(false);
        }

        /// <summary>
        /// Tests that the IsConnected property returns the correct value
        /// </summary>
        [Fact]
        public void Test_IsConnected_ReturnsCorrectValue()
        {
            // Arrange - setup in constructor for true value

            // Act
            bool result = networkService.IsConnected;

            // Assert
            result.Should().BeTrue();

            // Arrange - change mock to return false
            SetupConnectivityHelperMock(false, "None", ConnectionQuality.None);
            
            // Create new service instance to pick up the new mock value
            var newService = new NetworkService(mockLogger.Object);

            // Act
            result = newService.IsConnected;

            // Assert
            result.Should().BeFalse();
        }

        /// <summary>
        /// Tests that the GetConnectionType method returns the correct value
        /// </summary>
        [Fact]
        public void Test_GetConnectionType_ReturnsCorrectValue()
        {
            // Arrange - setup in constructor for "WiFi"

            // Act
            string result = networkService.GetConnectionType();

            // Assert
            result.Should().Be("WiFi");

            // Arrange - change mock to return "Cellular"
            SetupConnectivityHelperMock(true, "Cellular", ConnectionQuality.Medium);

            // Act
            result = networkService.GetConnectionType();

            // Assert
            result.Should().Be("Cellular");
        }

        /// <summary>
        /// Tests that the GetConnectionQuality method returns the correct value
        /// </summary>
        [Fact]
        public void Test_GetConnectionQuality_ReturnsCorrectValue()
        {
            // Arrange - setup in constructor for High quality

            // Act
            ConnectionQuality result = networkService.GetConnectionQuality();

            // Assert
            result.Should().Be(ConnectionQuality.High);

            // Arrange - change mock to return Low quality
            SetupConnectivityHelperMock(true, "Cellular", ConnectionQuality.Low);

            // Act
            result = networkService.GetConnectionQuality();

            // Assert
            result.Should().Be(ConnectionQuality.Low);
        }

        /// <summary>
        /// Tests that StartMonitoring registers for connectivity changes
        /// </summary>
        [Fact]
        public void Test_StartMonitoring_RegistersForConnectivityChanges()
        {
            // Arrange
            bool registerCalled = false;
            MockHelpers.SetupStatic(typeof(ConnectivityHelper), "RegisterForConnectivityChanges", 
                (Action<EventHandler<ConnectivityChangedEventArgs>>) (handler => { registerCalled = true; }));

            // Act
            networkService.StartMonitoring();

            // Assert
            registerCalled.Should().BeTrue();
            
            // Verify _isMonitoring is set to true
            var isMonitoringField = typeof(NetworkService).GetField("_isMonitoring", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            isMonitoringField.GetValue(networkService).Should().Be(true);
            
            // Verify logger was called with appropriate message
            mockLogger.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Started monitoring")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        /// <summary>
        /// Tests that StartMonitoring does nothing when already monitoring
        /// </summary>
        [Fact]
        public void Test_StartMonitoring_WhenAlreadyMonitoring_DoesNothing()
        {
            // Arrange
            int registerCallCount = 0;
            MockHelpers.SetupStatic(typeof(ConnectivityHelper), "RegisterForConnectivityChanges", 
                (Action<EventHandler<ConnectivityChangedEventArgs>>) (handler => { registerCallCount++; }));

            // Act
            networkService.StartMonitoring(); // First call
            networkService.StartMonitoring(); // Second call

            // Assert - RegisterForConnectivityChanges should be called only once
            registerCallCount.Should().Be(1);
        }

        /// <summary>
        /// Tests that StopMonitoring unregisters from connectivity changes
        /// </summary>
        [Fact]
        public void Test_StopMonitoring_UnregistersFromConnectivityChanges()
        {
            // Arrange
            bool unregisterCalled = false;
            MockHelpers.SetupStatic(typeof(ConnectivityHelper), "RegisterForConnectivityChanges", 
                (Action<EventHandler<ConnectivityChangedEventArgs>>) (handler => { }));
            MockHelpers.SetupStatic(typeof(ConnectivityHelper), "UnregisterFromConnectivityChanges", 
                () => { unregisterCalled = true; });
            
            networkService.StartMonitoring(); // Start monitoring first
            mockLogger.Invocations.Clear(); // Clear previous invocations

            // Act
            networkService.StopMonitoring();

            // Assert
            unregisterCalled.Should().BeTrue();
            
            // Verify _isMonitoring is set to false
            var isMonitoringField = typeof(NetworkService).GetField("_isMonitoring", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            isMonitoringField.GetValue(networkService).Should().Be(false);
            
            // Verify logger was called with appropriate message
            mockLogger.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Stopped monitoring")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        /// <summary>
        /// Tests that StopMonitoring does nothing when not monitoring
        /// </summary>
        [Fact]
        public void Test_StopMonitoring_WhenNotMonitoring_DoesNothing()
        {
            // Arrange
            bool unregisterCalled = false;
            MockHelpers.SetupStatic(typeof(ConnectivityHelper), "UnregisterFromConnectivityChanges", 
                () => { unregisterCalled = true; });

            // Act - call without starting first
            networkService.StopMonitoring();

            // Assert - UnregisterFromConnectivityChanges should not be called
            unregisterCalled.Should().BeFalse();
        }

        /// <summary>
        /// Tests that ShouldAttemptOperation returns the correct value
        /// </summary>
        [Fact]
        public void Test_ShouldAttemptOperation_ReturnsCorrectValue()
        {
            // Arrange - setup in constructor for high quality

            // Act - Authentication should always return true
            bool result = networkService.ShouldAttemptOperation(NetworkOperationType.Authentication);

            // Assert
            result.Should().BeTrue();

            // Arrange - change mock to return low quality
            SetupConnectivityHelperMock(true, "Cellular", ConnectionQuality.Low);

            // Act - PhotoUpload should return false on low quality
            result = networkService.ShouldAttemptOperation(NetworkOperationType.PhotoUpload);

            // Assert
            result.Should().BeFalse();
        }

        /// <summary>
        /// Tests that ConnectivityChanged event is raised when connectivity changes
        /// </summary>
        [Fact]
        public void Test_ConnectivityChanged_EventIsRaised()
        {
            // Arrange
            networkService.ConnectivityChanged += HandleConnectivityChanged;
            
            // Setup for simulating connectivity change
            EventHandler<ConnectivityChangedEventArgs> registeredHandler = null;
            MockHelpers.SetupStatic(typeof(ConnectivityHelper), "RegisterForConnectivityChanges", 
                (Action<EventHandler<ConnectivityChangedEventArgs>>) (handler => { registeredHandler = handler; }));
            
            // Start monitoring to register the handler
            networkService.StartMonitoring();

            // Act - simulate connectivity change by calling the registered handler
            var args = new ConnectivityChangedEventArgs(false, "None", ConnectionQuality.None);
            registeredHandler?.Invoke(networkService, args);

            // Assert
            eventWasRaised.Should().BeTrue();
            lastEventArgs.Should().NotBeNull();
            lastEventArgs.IsConnected.Should().BeFalse();
            lastEventArgs.ConnectionType.Should().Be("None");
            lastEventArgs.ConnectionQuality.Should().Be(ConnectionQuality.None);
        }

        /// <summary>
        /// Tests that OnConnectivityChanged updates the IsConnected property
        /// </summary>
        [Fact]
        public void Test_OnConnectivityChanged_UpdatesIsConnected()
        {
            // Arrange
            var onConnectivityChangedMethod = typeof(NetworkService).GetMethod("OnConnectivityChanged", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var args = new ConnectivityChangedEventArgs(false, "None", ConnectionQuality.None);

            // Act - simulate connectivity change to false
            onConnectivityChangedMethod.Invoke(networkService, new object[] { this, args });

            // Assert
            networkService.IsConnected.Should().BeFalse();

            // Act - simulate connectivity change to true
            args = new ConnectivityChangedEventArgs(true, "WiFi", ConnectionQuality.High);
            onConnectivityChangedMethod.Invoke(networkService, new object[] { this, args });

            // Assert
            networkService.IsConnected.Should().BeTrue();
        }

        /// <summary>
        /// Tests that OnConnectivityChanged logs the connectivity change
        /// </summary>
        [Fact]
        public void Test_OnConnectivityChanged_LogsConnectivityChange()
        {
            // Arrange
            var onConnectivityChangedMethod = typeof(NetworkService).GetMethod("OnConnectivityChanged", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var args = new ConnectivityChangedEventArgs(true, "WiFi", ConnectionQuality.High);

            // Act
            onConnectivityChangedMethod.Invoke(networkService, new object[] { this, args });

            // Assert
            mockLogger.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Connectivity changed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }
}