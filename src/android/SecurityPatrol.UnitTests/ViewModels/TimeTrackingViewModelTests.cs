using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.Extensions.DependencyInjection;
using SecurityPatrol.ViewModels;
using SecurityPatrol.Models;
using SecurityPatrol.Services;
using SecurityPatrol.UnitTests.Helpers.MockServices;
using SecurityPatrol.Constants;

namespace SecurityPatrol.UnitTests.ViewModels
{
    /// <summary>
    /// Unit tests for the TimeTrackingViewModel class that verify the functionality of clock in/out operations,
    /// status display, and navigation to history page. Tests use mock services to isolate the ViewModel and
    /// ensure it behaves correctly under various conditions.
    /// </summary>
    public class TimeTrackingViewModelTests
    {
        private readonly MockTimeTrackingService _timeTrackingService;
        private readonly MockLocationService _locationService;
        private readonly TimeTrackingViewModel _viewModel;

        /// <summary>
        /// Initializes a new instance of the TimeTrackingViewModelTests class with all required mock services.
        /// </summary>
        public TimeTrackingViewModelTests()
        {
            // Initialize mock services
            _timeTrackingService = new MockTimeTrackingService();
            _locationService = new MockLocationService();
            
            // Set up default location
            _locationService.SetupCurrentLocation(new LocationModel { Latitude = 34.0522, Longitude = -118.2437 });
            
            // Create mocks for navigation and auth services
            var navigationServiceMock = new Mock<INavigationService>();
            var authStateProviderMock = new Mock<IAuthenticationStateProvider>();
            
            // Set up default authentication state
            authStateProviderMock.Setup(x => x.IsAuthenticated()).ReturnsAsync(true);
            
            // Create the view model with mock services
            _viewModel = new TimeTrackingViewModel(
                navigationServiceMock.Object,
                authStateProviderMock.Object,
                _timeTrackingService,
                _locationService);
        }

        /// <summary>
        /// Tests that the ViewModel loads the current clock status during initialization.
        /// </summary>
        [Fact]
        public async Task InitializeViewModel_ShouldLoadCurrentStatus()
        {
            // Arrange
            var status = new ClockStatus
            {
                IsClocked = true,
                LastClockInTime = DateTime.Now.AddHours(-2),
                LastClockOutTime = null
            };
            _timeTrackingService.SetupCurrentStatus(status);

            // Act
            await _viewModel.InitializeAsync();

            // Assert
            Assert.True(_timeTrackingService.VerifyGetCurrentStatusCalled());
            Assert.Equal("Clocked In", _viewModel.ClockStatusText);
            Assert.Equal(status.LastClockInTime.Value.ToLocalTime().ToString("g"), _viewModel.LastClockInText);
            Assert.Equal("Not available", _viewModel.LastClockOutText);
            Assert.False(_viewModel.IsClockInEnabled);
            Assert.True(_viewModel.IsClockOutEnabled);
        }

        /// <summary>
        /// Tests that executing the ClockInCommand calls the ClockIn method on the time tracking service.
        /// </summary>
        [Fact]
        public async Task ClockInCommand_WhenExecuted_ShouldCallClockIn()
        {
            // Arrange
            _timeTrackingService.SetupCurrentStatus(new ClockStatus { IsClocked = false });
            await _viewModel.InitializeAsync();

            // Act
            await _viewModel.ClockInCommand.ExecuteAsync(null);

            // Assert
            Assert.True(_locationService.VerifyGetCurrentLocationCalled());
            Assert.True(_timeTrackingService.VerifyClockInCalled());
        }

        /// <summary>
        /// Tests that the ViewModel properly handles exceptions thrown during clock in.
        /// </summary>
        [Fact]
        public async Task ClockInCommand_WhenServiceThrowsException_ShouldHandleError()
        {
            // Arrange
            _timeTrackingService.SetupCurrentStatus(new ClockStatus { IsClocked = false });
            _timeTrackingService.SetupException(new InvalidOperationException("Test error"));
            await _viewModel.InitializeAsync();

            // Act
            await _viewModel.ClockInCommand.ExecuteAsync(null);

            // Assert
            Assert.True(_viewModel.HasError);
            Assert.Contains("Test error", _viewModel.ErrorMessage);
        }

        /// <summary>
        /// Tests that executing the ClockOutCommand calls the ClockOut method on the time tracking service.
        /// </summary>
        [Fact]
        public async Task ClockOutCommand_WhenExecuted_ShouldCallClockOut()
        {
            // Arrange
            _timeTrackingService.SetupCurrentStatus(new ClockStatus { IsClocked = true });
            await _viewModel.InitializeAsync();

            // Act
            await _viewModel.ClockOutCommand.ExecuteAsync(null);

            // Assert
            Assert.True(_locationService.VerifyGetCurrentLocationCalled());
            Assert.True(_timeTrackingService.VerifyClockOutCalled());
        }

        /// <summary>
        /// Tests that the ViewModel properly handles exceptions thrown during clock out.
        /// </summary>
        [Fact]
        public async Task ClockOutCommand_WhenServiceThrowsException_ShouldHandleError()
        {
            // Arrange
            _timeTrackingService.SetupCurrentStatus(new ClockStatus { IsClocked = true });
            _timeTrackingService.SetupException(new InvalidOperationException("Test error"));
            await _viewModel.InitializeAsync();

            // Act
            await _viewModel.ClockOutCommand.ExecuteAsync(null);

            // Assert
            Assert.True(_viewModel.HasError);
            Assert.Contains("Test error", _viewModel.ErrorMessage);
        }

        /// <summary>
        /// Tests that executing the ViewHistoryCommand navigates to the time history page.
        /// </summary>
        [Fact]
        public async Task ViewHistoryCommand_WhenExecuted_ShouldNavigateToHistoryPage()
        {
            // Arrange
            var navigationServiceMock = new Mock<INavigationService>();
            var authStateProviderMock = new Mock<IAuthenticationStateProvider>();
            authStateProviderMock.Setup(x => x.IsAuthenticated()).ReturnsAsync(true);
            
            var viewModel = new TimeTrackingViewModel(
                navigationServiceMock.Object,
                authStateProviderMock.Object,
                _timeTrackingService,
                _locationService);

            // Act
            await viewModel.ViewHistoryCommand.ExecuteAsync(null);

            // Assert
            navigationServiceMock.Verify(x => x.NavigateToAsync(NavigationConstants.TimeHistoryPage), Times.Once);
        }

        /// <summary>
        /// Tests that the ViewModel updates its properties when the clock status changes.
        /// </summary>
        [Fact]
        public async Task OnStatusChanged_ShouldUpdateViewModelProperties()
        {
            // Arrange
            await _viewModel.InitializeAsync();
            var newStatus = new ClockStatus
            {
                IsClocked = true,
                LastClockInTime = DateTime.Now,
                LastClockOutTime = DateTime.Now.AddHours(-4)
            };

            // Act
            _timeTrackingService.StatusChanged?.Invoke(this, new ClockStatusChangedEventArgs(newStatus));

            // Assert
            Assert.Equal("Clocked In", _viewModel.ClockStatusText);
            Assert.Equal(newStatus.LastClockInTime.Value.ToLocalTime().ToString("g"), _viewModel.LastClockInText);
            Assert.Equal(newStatus.LastClockOutTime.Value.ToLocalTime().ToString("g"), _viewModel.LastClockOutText);
            Assert.False(_viewModel.IsClockInEnabled);
            Assert.True(_viewModel.IsClockOutEnabled);
        }

        /// <summary>
        /// Tests that the ViewModel unsubscribes from events when disposed.
        /// </summary>
        [Fact]
        public void Dispose_ShouldUnsubscribeFromEvents()
        {
            // Arrange
            var initialStatus = new ClockStatus
            {
                IsClocked = false,
                LastClockInTime = null,
                LastClockOutTime = null
            };
            _timeTrackingService.SetupCurrentStatus(initialStatus);
            _viewModel.InitializeAsync().Wait();
            
            // Get initial values
            var initialClockStatusText = _viewModel.ClockStatusText;
            
            // Act
            _viewModel.Dispose();
            
            // Raise event after disposal - properties should not update
            var newStatus = new ClockStatus
            {
                IsClocked = true,
                LastClockInTime = DateTime.Now,
                LastClockOutTime = null
            };
            _timeTrackingService.StatusChanged?.Invoke(this, new ClockStatusChangedEventArgs(newStatus));
            
            // Assert
            Assert.Equal(initialClockStatusText, _viewModel.ClockStatusText);
        }
    }
}