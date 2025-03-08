using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using SecurityPatrol.MAUI.UnitTests.Setup;
using SecurityPatrol.ViewModels;
using SecurityPatrol.Models;
using SecurityPatrol.Constants;

namespace SecurityPatrol.MAUI.UnitTests.ViewModels
{
    public class TimeTrackingViewModelTests : TestBase
    {
        private TimeTrackingViewModel ViewModel { get; set; }

        private void InitializeViewModel()
        {
            ViewModel = new TimeTrackingViewModel(
                MockNavigationService.Object,
                MockAuthService.Object,
                MockTimeTrackingService.Object,
                MockLocationService.Object);
        }

        [Fact]
        public async Task InitializeViewModel_WithAuthenticatedUser_ShouldLoadCurrentStatus()
        {
            // Arrange
            SetupMockAuthenticationState(isAuthenticated: true);
            
            var status = new ClockStatus
            {
                IsClocked = true,
                LastClockInTime = DateTime.UtcNow.AddHours(-2),
                LastClockOutTime = null
            };
            MockTimeTrackingService.Setup(s => s.GetCurrentStatus()).ReturnsAsync(status);
            
            // Act
            InitializeViewModel();
            await ViewModel.InitializeAsync();
            
            // Assert
            MockTimeTrackingService.Verify(s => s.GetCurrentStatus(), Times.Once);
            ViewModel.ClockStatusText.Should().Be("Clocked In");
            ViewModel.IsClockInEnabled.Should().BeFalse();
            ViewModel.IsClockOutEnabled.Should().BeTrue();
        }

        [Fact]
        public async Task InitializeViewModel_WithUnauthenticatedUser_ShouldNotLoadCurrentStatus()
        {
            // Arrange
            SetupMockAuthenticationState(isAuthenticated: false);
            
            // Act
            InitializeViewModel();
            await ViewModel.InitializeAsync();
            
            // Assert
            MockTimeTrackingService.Verify(s => s.GetCurrentStatus(), Times.Never);
        }

        [Fact]
        public async Task ClockInCommand_WithAuthenticatedUser_ShouldCallClockInService()
        {
            // Arrange
            SetupMockAuthenticationState(isAuthenticated: true);
            
            var location = new LocationModel
            {
                Latitude = 34.0522,
                Longitude = -118.2437
            };
            MockLocationService.Setup(s => s.GetCurrentLocation()).ReturnsAsync(location);
            
            var timeRecord = new TimeRecordModel
            {
                Id = 1,
                Type = "ClockIn",
                Timestamp = DateTime.UtcNow
            };
            MockTimeTrackingService.Setup(s => s.ClockIn()).ReturnsAsync(timeRecord);
            
            // Act
            InitializeViewModel();
            await ViewModel.ClockInCommand.ExecuteAsync(null);
            
            // Assert
            MockLocationService.Verify(s => s.GetCurrentLocation(), Times.Once);
            MockTimeTrackingService.Verify(s => s.ClockIn(), Times.Once);
        }

        [Fact]
        public async Task ClockInCommand_WithUnauthenticatedUser_ShouldNotCallClockInService()
        {
            // Arrange
            SetupMockAuthenticationState(isAuthenticated: false);
            
            // Act
            InitializeViewModel();
            await ViewModel.ClockInCommand.ExecuteAsync(null);
            
            // Assert
            MockLocationService.Verify(s => s.GetCurrentLocation(), Times.Never);
            MockTimeTrackingService.Verify(s => s.ClockIn(), Times.Never);
        }

        [Fact]
        public async Task ClockOutCommand_WithAuthenticatedUser_ShouldCallClockOutService()
        {
            // Arrange
            SetupMockAuthenticationState(isAuthenticated: true);
            
            var location = new LocationModel
            {
                Latitude = 34.0522,
                Longitude = -118.2437
            };
            MockLocationService.Setup(s => s.GetCurrentLocation()).ReturnsAsync(location);
            
            var timeRecord = new TimeRecordModel
            {
                Id = 2,
                Type = "ClockOut",
                Timestamp = DateTime.UtcNow
            };
            MockTimeTrackingService.Setup(s => s.ClockOut()).ReturnsAsync(timeRecord);
            
            // Act
            InitializeViewModel();
            await ViewModel.ClockOutCommand.ExecuteAsync(null);
            
            // Assert
            MockLocationService.Verify(s => s.GetCurrentLocation(), Times.Once);
            MockTimeTrackingService.Verify(s => s.ClockOut(), Times.Once);
        }

        [Fact]
        public async Task ClockOutCommand_WithUnauthenticatedUser_ShouldNotCallClockOutService()
        {
            // Arrange
            SetupMockAuthenticationState(isAuthenticated: false);
            
            // Act
            InitializeViewModel();
            await ViewModel.ClockOutCommand.ExecuteAsync(null);
            
            // Assert
            MockLocationService.Verify(s => s.GetCurrentLocation(), Times.Never);
            MockTimeTrackingService.Verify(s => s.ClockOut(), Times.Never);
        }

        [Fact]
        public async Task ViewHistoryCommand_ShouldNavigateToTimeHistoryPage()
        {
            // Arrange
            InitializeViewModel();
            
            // Act
            await ViewModel.ViewHistoryCommand.ExecuteAsync(null);
            
            // Assert
            MockNavigationService.Verify(s => s.NavigateToAsync(NavigationConstants.TimeHistoryPage), Times.Once);
        }

        [Fact]
        public async Task OnStatusChanged_ShouldUpdateUIProperties()
        {
            // Arrange
            InitializeViewModel();
            
            var newStatus = new ClockStatus
            {
                IsClocked = true,
                LastClockInTime = DateTime.UtcNow,
                LastClockOutTime = DateTime.UtcNow.AddHours(-8)
            };
            
            // Act
            // Raise the event using Moq
            MockTimeTrackingService.Raise(s => s.StatusChanged += null, 
                MockTimeTrackingService.Object, new ClockStatusChangedEventArgs(newStatus));
            
            // Assert
            ViewModel.CurrentStatus.Should().Be(newStatus);
            ViewModel.ClockStatusText.Should().Be("Clocked In");
            ViewModel.IsClockInEnabled.Should().BeFalse();
            ViewModel.IsClockOutEnabled.Should().BeTrue();
        }

        [Fact]
        public async Task UpdateStatusDisplay_WhenClockedIn_ShouldShowCorrectStatus()
        {
            // Arrange
            SetupMockAuthenticationState(isAuthenticated: true);
            
            var status = new ClockStatus
            {
                IsClocked = true,
                LastClockInTime = DateTime.UtcNow.AddHours(-2),
                LastClockOutTime = DateTime.UtcNow.AddDays(-1)
            };
            
            MockTimeTrackingService.Setup(s => s.GetCurrentStatus()).ReturnsAsync(status);
            
            // Act
            InitializeViewModel();
            await ViewModel.InitializeAsync();
            
            // Assert
            ViewModel.ClockStatusText.Should().Be("Clocked In");
            ViewModel.IsClockInEnabled.Should().BeFalse();
            ViewModel.IsClockOutEnabled.Should().BeTrue();
        }

        [Fact]
        public async Task UpdateStatusDisplay_WhenClockedOut_ShouldShowCorrectStatus()
        {
            // Arrange
            SetupMockAuthenticationState(isAuthenticated: true);
            
            var status = new ClockStatus
            {
                IsClocked = false,
                LastClockInTime = DateTime.UtcNow.AddDays(-1),
                LastClockOutTime = DateTime.UtcNow.AddHours(-2)
            };
            
            MockTimeTrackingService.Setup(s => s.GetCurrentStatus()).ReturnsAsync(status);
            
            // Act
            InitializeViewModel();
            await ViewModel.InitializeAsync();
            
            // Assert
            ViewModel.ClockStatusText.Should().Be("Clocked Out");
            ViewModel.IsClockInEnabled.Should().BeTrue();
            ViewModel.IsClockOutEnabled.Should().BeFalse();
        }

        [Fact]
        public void Dispose_ShouldUnsubscribeFromEvents()
        {
            // Arrange
            InitializeViewModel();
            
            // Act
            ViewModel.Dispose();
            
            // Assert
            // Verify StatusChanged event was unsubscribed by raising it again
            // and checking that UI properties don't change
            var initialState = ViewModel.ClockStatusText;
            var newStatus = new ClockStatus { IsClocked = !ViewModel.IsClockOutEnabled };
            
            MockTimeTrackingService.Raise(s => s.StatusChanged += null,
                MockTimeTrackingService.Object, new ClockStatusChangedEventArgs(newStatus));
                
            // If successfully unsubscribed, the status shouldn't change
            ViewModel.ClockStatusText.Should().Be(initialState);
        }
    }
}