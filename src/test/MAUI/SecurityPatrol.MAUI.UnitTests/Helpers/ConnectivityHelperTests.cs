using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Maui.Networking;
using SecurityPatrol.MAUI.UnitTests.Setup;
using SecurityPatrol.Helpers;
using SecurityPatrol.Models;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Helpers;

namespace SecurityPatrol.MAUI.UnitTests.Helpers
{
    public class ConnectivityHelperTests : TestBase
    {
        private MockNetworkService _mockNetworkService;
        private bool _connectivityChangedEventRaised;
        private ConnectivityChangedEventArgs _lastConnectivityChangedEventArgs;

        public ConnectivityHelperTests()
        {
            _mockNetworkService = new MockNetworkService();
            _connectivityChangedEventRaised = false;
            _lastConnectivityChangedEventArgs = null;
        }

        public void Setup()
        {
            _connectivityChangedEventRaised = false;
            _lastConnectivityChangedEventArgs = null;
            _mockNetworkService.SetNetworkConnected(true);
            _mockNetworkService.SetConnectionType("WiFi");
            _mockNetworkService.SetConnectionQuality(ConnectionQuality.High);
        }

        public void Cleanup()
        {
            // Unregister any event handlers from ConnectivityHelper
            ConnectivityHelper.UnregisterFromConnectivityChanges(OnConnectivityChanged);
        }

        [Fact]
        public void IsConnected_ShouldReturnTrueWhenConnected()
        {
            // Arrange
            _mockNetworkService.SetNetworkConnected(true);
            
            // Act
            bool result = ConnectivityHelper.IsConnected();
            
            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsConnected_ShouldReturnFalseWhenDisconnected()
        {
            // Arrange
            _mockNetworkService.SetNetworkConnected(false);
            
            // Act
            bool result = ConnectivityHelper.IsConnected();
            
            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void GetConnectionType_ShouldReturnCorrectType()
        {
            // Arrange
            _mockNetworkService.SetConnectionType("WiFi");
            
            // Act
            string result = ConnectivityHelper.GetConnectionType();
            
            // Assert
            result.Should().Be("WiFi");
            
            // Test another connection type
            _mockNetworkService.SetConnectionType("Cellular");
            result = ConnectivityHelper.GetConnectionType();
            result.Should().Be("Cellular");
        }

        [Fact]
        public void GetConnectionQuality_ShouldReturnCorrectQuality()
        {
            // Arrange
            _mockNetworkService.SetConnectionQuality(ConnectionQuality.High);
            
            // Act
            ConnectionQuality result = ConnectivityHelper.GetConnectionQuality();
            
            // Assert
            result.Should().Be(ConnectionQuality.High);
            
            // Test different quality levels
            _mockNetworkService.SetConnectionQuality(ConnectionQuality.Medium);
            result = ConnectivityHelper.GetConnectionQuality();
            result.Should().Be(ConnectionQuality.Medium);
            
            _mockNetworkService.SetConnectionQuality(ConnectionQuality.Low);
            result = ConnectivityHelper.GetConnectionQuality();
            result.Should().Be(ConnectionQuality.Low);
            
            _mockNetworkService.SetConnectionQuality(ConnectionQuality.None);
            result = ConnectivityHelper.GetConnectionQuality();
            result.Should().Be(ConnectionQuality.None);
        }

        [Fact]
        public void GetConnectionQuality_ShouldReturnNoneWhenDisconnected()
        {
            // Arrange
            _mockNetworkService.SetNetworkConnected(false);
            
            // Act
            ConnectionQuality result = ConnectivityHelper.GetConnectionQuality();
            
            // Assert
            result.Should().Be(ConnectionQuality.None);
        }

        [Fact]
        public void RegisterForConnectivityChanges_ShouldReceiveEvents()
        {
            // Arrange
            ConnectivityHelper.RegisterForConnectivityChanges(OnConnectivityChanged);
            
            // Act
            _mockNetworkService.SimulateConnectivityChange();
            
            // Assert
            _connectivityChangedEventRaised.Should().BeTrue();
            _lastConnectivityChangedEventArgs.Should().NotBeNull();
            _lastConnectivityChangedEventArgs.IsConnected.Should().Be(_mockNetworkService.IsConnected);
            _lastConnectivityChangedEventArgs.ConnectionType.Should().Be(_mockNetworkService.GetConnectionType());
            _lastConnectivityChangedEventArgs.ConnectionQuality.Should().Be(_mockNetworkService.GetConnectionQuality());
        }

        [Fact]
        public void UnregisterFromConnectivityChanges_ShouldStopReceivingEvents()
        {
            // Arrange
            ConnectivityHelper.RegisterForConnectivityChanges(OnConnectivityChanged);
            _mockNetworkService.SimulateConnectivityChange();
            _connectivityChangedEventRaised.Should().BeTrue();
            
            // Reset flag for next test
            _connectivityChangedEventRaised = false;
            
            // Act
            ConnectivityHelper.UnregisterFromConnectivityChanges(OnConnectivityChanged);
            _mockNetworkService.SimulateConnectivityChange();
            
            // Assert
            _connectivityChangedEventRaised.Should().BeFalse();
        }

        [Fact]
        public void ShouldAttemptOperation_ShouldReturnFalseWhenDisconnected()
        {
            // Arrange
            _mockNetworkService.SetNetworkConnected(false);
            
            // Act & Assert
            ConnectivityHelper.ShouldAttemptOperation(NetworkOperationType.PhotoUpload).Should().BeFalse();
            ConnectivityHelper.ShouldAttemptOperation(NetworkOperationType.LocationSync).Should().BeFalse();
            ConnectivityHelper.ShouldAttemptOperation(NetworkOperationType.ReportSync).Should().BeFalse();
            // Authentication might be allowed even with no connectivity
            ConnectivityHelper.ShouldAttemptOperation(NetworkOperationType.Authentication).Should().BeTrue();
        }

        [Fact]
        public void ShouldAttemptOperation_ShouldConsiderConnectionQuality()
        {
            // Arrange
            _mockNetworkService.SetNetworkConnected(true);
            _mockNetworkService.SetConnectionQuality(ConnectionQuality.High);
            
            // Act & Assert - High quality should allow photo uploads
            ConnectivityHelper.ShouldAttemptOperation(NetworkOperationType.PhotoUpload).Should().BeTrue();
            
            // With low quality, photo uploads might not be recommended
            _mockNetworkService.SetConnectionQuality(ConnectionQuality.Low);
            ConnectivityHelper.ShouldAttemptOperation(NetworkOperationType.PhotoUpload).Should().BeFalse();
        }

        [Fact]
        public void ShouldAttemptOperation_ShouldAllowCriticalOperations()
        {
            // Arrange
            _mockNetworkService.SetNetworkConnected(true);
            _mockNetworkService.SetConnectionQuality(ConnectionQuality.Low);
            
            // Act & Assert - Even with low quality, critical operations should be allowed
            ConnectivityHelper.ShouldAttemptOperation(NetworkOperationType.Authentication).Should().BeTrue();
            ConnectivityHelper.ShouldAttemptOperation(NetworkOperationType.ClockEvent).Should().BeTrue();
        }

        [Fact]
        public void ConnectivityHelper_ShouldHandleNetworkChanges()
        {
            // Arrange
            ConnectivityHelper.RegisterForConnectivityChanges(OnConnectivityChanged);
            
            // Act
            NetworkConditionSimulator.SimulateNetworkLoss(_mockNetworkService);
            
            // Assert
            _connectivityChangedEventRaised.Should().BeTrue();
            _lastConnectivityChangedEventArgs.IsConnected.Should().BeFalse();
            
            // Reset and test network restoration
            _connectivityChangedEventRaised = false;
            NetworkConditionSimulator.SimulateNetworkRestoration(_mockNetworkService);
            
            // Assert
            _connectivityChangedEventRaised.Should().BeTrue();
            _lastConnectivityChangedEventArgs.IsConnected.Should().BeTrue();
        }

        [Fact]
        public void ConnectivityHelper_ShouldHandleQualityChanges()
        {
            // Arrange
            ConnectivityHelper.RegisterForConnectivityChanges(OnConnectivityChanged);
            
            // Act
            NetworkConditionSimulator.SimulateNetworkQualityChange(_mockNetworkService, ConnectionQuality.Low);
            
            // Assert
            _connectivityChangedEventRaised.Should().BeTrue();
            _lastConnectivityChangedEventArgs.ConnectionQuality.Should().Be(ConnectionQuality.Low);
            
            // Reset and test another quality change
            _connectivityChangedEventRaised = false;
            NetworkConditionSimulator.SimulateNetworkQualityChange(_mockNetworkService, ConnectionQuality.High);
            
            // Assert
            _connectivityChangedEventRaised.Should().BeTrue();
            _lastConnectivityChangedEventArgs.ConnectionQuality.Should().Be(ConnectionQuality.High);
        }

        [Fact]
        public void ConnectivityHelper_ShouldHandleConnectionTypeChanges()
        {
            // Arrange
            ConnectivityHelper.RegisterForConnectivityChanges(OnConnectivityChanged);
            
            // Act
            NetworkConditionSimulator.SimulateConnectionTypeChange(_mockNetworkService, "Cellular");
            
            // Assert
            _connectivityChangedEventRaised.Should().BeTrue();
            _lastConnectivityChangedEventArgs.ConnectionType.Should().Be("Cellular");
            
            // Reset and test another type change
            _connectivityChangedEventRaised = false;
            NetworkConditionSimulator.SimulateConnectionTypeChange(_mockNetworkService, "WiFi");
            
            // Assert
            _connectivityChangedEventRaised.Should().BeTrue();
            _lastConnectivityChangedEventArgs.ConnectionType.Should().Be("WiFi");
        }

        private void OnConnectivityChanged(ConnectivityChangedEventArgs args)
        {
            _connectivityChangedEventRaised = true;
            _lastConnectivityChangedEventArgs = args;
        }
    }
}