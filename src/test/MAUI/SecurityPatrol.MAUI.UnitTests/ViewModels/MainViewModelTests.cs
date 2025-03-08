using System; // System 8.0+
using System.Collections.Generic; // System.Collections.Generic 8.0+
using System.Threading.Tasks; // System.Threading.Tasks 8.0+
using Xunit; // xunit 2.4.2
using Moq; // Moq 4.18.4
using FluentAssertions; // FluentAssertions 6.11.0
using SecurityPatrol.MAUI.UnitTests.Setup; // TestBase
using SecurityPatrol.ViewModels; // MainViewModel
using SecurityPatrol.Constants; // NavigationConstants
using SecurityPatrol.Services; // IAuthenticationStateProvider
using SecurityPatrol.Models; // ClockStatus, PatrolStatus, ClockStatusChangedEventArgs, ConnectivityChangedEventArgs, SyncStatusChangedEventArgs

namespace SecurityPatrol.MAUI.UnitTests.ViewModels
{
    /// <summary>
    /// Test class for MainViewModel that verifies the functionality of the main dashboard
    /// </summary>
    public class MainViewModelTests : TestBase
    {
        /// <summary>
        /// Mock for the authentication state provider
        /// </summary>
        public Mock<IAuthenticationStateProvider> MockAuthStateProvider { get; set; }

        /// <summary>
        /// Initializes a new instance of the MainViewModelTests class
        /// </summary>
        public MainViewModelTests()
        {
            // Initialize MockAuthStateProvider with a new instance of Mock<IAuthenticationStateProvider>
            MockAuthStateProvider = new Mock<IAuthenticationStateProvider>();

            // Setup MockAuthStateProvider to return a valid authentication state
            MockAuthStateProvider.Setup(x => x.GetCurrentState())
                .ReturnsAsync(AuthState.CreateAuthenticated("+15555555555"));
        }

        /// <summary>
        /// Creates an instance of MainViewModel with mocked dependencies for testing
        /// </summary>
        /// <returns>A configured MainViewModel instance for testing</returns>
        private MainViewModel CreateViewModel()
        {
            // Return new MainViewModel with mocked dependencies: MockNavigationService.Object, MockAuthStateProvider.Object, MockAuthService.Object, MockTimeTrackingService.Object, MockLocationService.Object, MockPatrolService.Object, MockSyncService.Object, MockNetworkService.Object
            return new MainViewModel(
                MockNavigationService.Object,
                MockAuthStateProvider.Object,
                MockAuthService.Object,
                MockTimeTrackingService.Object,
                MockLocationService.Object,
                MockPatrolService.Object,
                MockSyncService.Object,
                MockNetworkService.Object);
        }

        /// <summary>
        /// Verifies that the constructor initializes all properties correctly
        /// </summary>
        [Fact]
        public void Test_Constructor_InitializesProperties()
        {
            // Create a MainViewModel instance using CreateViewModel()
            var viewModel = CreateViewModel();

            // Assert that Title is 'Security Patrol'
            viewModel.Title.Should().Be("Security Patrol");

            // Assert that IsClockInActive is false
            viewModel.IsClockInActive.Should().BeFalse();

            // Assert that IsLocationTrackingActive is false
            viewModel.IsLocationTrackingActive.Should().BeFalse();

            // Assert that IsPatrolActive is false
            viewModel.IsPatrolActive.Should().BeFalse();

            // Assert that IsNetworkConnected is set to MockNetworkService.Object.IsConnected
            viewModel.IsNetworkConnected.Should().Be(MockNetworkService.Object.IsConnected);

            // Assert that PendingSyncItems is 0
            viewModel.PendingSyncItems.Should().Be(0);

            // Assert that PatrolCompletionPercentage is 0
            viewModel.PatrolCompletionPercentage.Should().Be(0);

            // Assert that VerifiedCheckpoints is 0
            viewModel.VerifiedCheckpoints.Should().Be(0);

            // Assert that TotalCheckpoints is 0
            viewModel.TotalCheckpoints.Should().Be(0);

            // Assert that IsSyncing is set to MockSyncService.Object.IsSyncing
            viewModel.IsSyncing.Should().Be(MockSyncService.Object.IsSyncing);

            // Assert that all navigation commands are not null
            viewModel.NavigateToTimeTrackingCommand.Should().NotBeNull();
            viewModel.NavigateToPatrolCommand.Should().NotBeNull();
            viewModel.NavigateToPhotoCaptureCommand.Should().NotBeNull();
            viewModel.NavigateToActivityReportCommand.Should().NotBeNull();
            viewModel.NavigateToSettingsCommand.Should().NotBeNull();

            // Assert that SyncNowCommand is not null
            viewModel.SyncNowCommand.Should().NotBeNull();

            // Assert that LogoutCommand is not null
            viewModel.LogoutCommand.Should().NotBeNull();
        }

        /// <summary>
        /// Verifies that InitializeAsync updates all status indicators
        /// </summary>
        [Fact]
        public async Task Test_InitializeAsync_UpdatesAllStatuses()
        {
            // Setup MockTimeTrackingService to return a ClockStatus with IsClocked = true
            MockTimeTrackingService.Setup(x => x.GetCurrentStatus())
                .ReturnsAsync(new ClockStatus { IsClocked = true });

            // Setup MockLocationService to return IsTracking = true
            MockLocationService.Setup(x => x.IsTracking)
                .Returns(true);

            // Setup MockPatrolService to return IsPatrolActive = true
            MockPatrolService.Setup(x => x.IsPatrolActive)
                .Returns(true);

            // Setup MockPatrolService to return a PatrolStatus with TotalCheckpoints = 10 and VerifiedCheckpoints = 5
            MockPatrolService.Setup(x => x.GetPatrolStatus(It.IsAny<int>()))
                .ReturnsAsync(new PatrolStatus { TotalCheckpoints = 10, VerifiedCheckpoints = 5 });

            // Setup MockSyncService to return a dictionary with pending items
            MockSyncService.Setup(x => x.GetSyncStatus())
                .ReturnsAsync(new Dictionary<string, int> { { "TimeRecord", 2 }, { "Photo", 3 } });

            // Setup MockNetworkService to return IsConnected = true
            MockNetworkService.Setup(x => x.IsConnected)
                .Returns(true);

            // Create a MainViewModel instance using CreateViewModel()
            var viewModel = CreateViewModel();

            // Call InitializeAsync() on the view model
            await viewModel.InitializeAsync();

            // Verify that MockNetworkService.StartMonitoring() was called
            MockNetworkService.Verify(x => x.StartMonitoring(), Times.Once);

            // Assert that IsClockInActive is true
            viewModel.IsClockInActive.Should().BeTrue();

            // Assert that IsLocationTrackingActive is true
            viewModel.IsLocationTrackingActive.Should().BeTrue();

            // Assert that IsPatrolActive is true
            viewModel.IsPatrolActive.Should().BeTrue();

            // Assert that IsNetworkConnected is true
            viewModel.IsNetworkConnected.Should().BeTrue();

            // Assert that PendingSyncItems is set to the sum of pending items
            viewModel.PendingSyncItems.Should().Be(5);

            // Assert that PatrolCompletionPercentage is 50
            viewModel.PatrolCompletionPercentage.Should().Be(50);

            // Assert that VerifiedCheckpoints is 5
            viewModel.VerifiedCheckpoints.Should().Be(5);

            // Assert that TotalCheckpoints is 10
            viewModel.TotalCheckpoints.Should().Be(10);
        }

        /// <summary>
        /// Verifies that OnAppearing refreshes all statuses and starts network monitoring
        /// </summary>
        [Fact]
        public void Test_OnAppearing_RefreshesStatusAndStartsMonitoring()
        {
            // Setup MockTimeTrackingService to return a ClockStatus with IsClocked = true
            MockTimeTrackingService.Setup(x => x.GetCurrentStatus())
                .ReturnsAsync(new ClockStatus { IsClocked = true });

            // Setup MockLocationService to return IsTracking = true
            MockLocationService.Setup(x => x.IsTracking)
                .Returns(true);

            // Setup MockPatrolService to return IsPatrolActive = true
            MockPatrolService.Setup(x => x.IsPatrolActive)
                .Returns(true);

            // Setup MockPatrolService to return a PatrolStatus with TotalCheckpoints = 10 and VerifiedCheckpoints = 5
            MockPatrolService.Setup(x => x.GetPatrolStatus(It.IsAny<int>()))
                .ReturnsAsync(new PatrolStatus { TotalCheckpoints = 10, VerifiedCheckpoints = 5 });

            // Setup MockSyncService to return a dictionary with pending items
            MockSyncService.Setup(x => x.GetSyncStatus())
                .ReturnsAsync(new Dictionary<string, int> { { "TimeRecord", 2 }, { "Photo", 3 } });

            // Setup MockNetworkService to return IsConnected = true
            MockNetworkService.Setup(x => x.IsConnected)
                .Returns(true);

            // Create a MainViewModel instance using CreateViewModel()
            var viewModel = CreateViewModel();

            // Call OnAppearing() on the view model
            viewModel.OnAppearing();

            // Verify that MockNetworkService.StartMonitoring() was called
            MockNetworkService.Verify(x => x.StartMonitoring(), Times.Exactly(2));

            // Assert that IsClockInActive is true
            viewModel.IsClockInActive.Should().BeTrue();

            // Assert that IsLocationTrackingActive is true
            viewModel.IsLocationTrackingActive.Should().BeTrue();

            // Assert that IsPatrolActive is true
            viewModel.IsPatrolActive.Should().BeTrue();

            // Assert that IsNetworkConnected is true
            viewModel.IsNetworkConnected.Should().BeTrue();

            // Assert that PendingSyncItems is set to the sum of pending items
            viewModel.PendingSyncItems.Should().Be(5);

            // Assert that PatrolCompletionPercentage is 50
            viewModel.PatrolCompletionPercentage.Should().Be(50);

            // Assert that VerifiedCheckpoints is 5
            viewModel.VerifiedCheckpoints.Should().Be(5);

            // Assert that TotalCheckpoints is 10
            viewModel.TotalCheckpoints.Should().Be(10);
        }

        /// <summary>
        /// Verifies that OnDisappearing stops network monitoring
        /// </summary>
        [Fact]
        public void Test_OnDisappearing_StopsNetworkMonitoring()
        {
            // Create a MainViewModel instance using CreateViewModel()
            var viewModel = CreateViewModel();

            // Call OnDisappearing() on the view model
            viewModel.OnDisappearing();

            // Verify that MockNetworkService.StopMonitoring() was called
            MockNetworkService.Verify(x => x.StopMonitoring(), Times.Once);
        }

        /// <summary>
        /// Verifies that NavigateToTimeTrackingCommand navigates to the time tracking page
        /// </summary>
        [Fact]
        public void Test_NavigateToTimeTrackingCommand_NavigatesToTimeTrackingPage()
        {
            // Create a MainViewModel instance using CreateViewModel()
            var viewModel = CreateViewModel();

            // Execute NavigateToTimeTrackingCommand
            viewModel.NavigateToTimeTrackingCommand.Execute(null);

            // Verify that MockNavigationService.NavigateToAsync was called with NavigationConstants.TimeTrackingPage
            MockNavigationService.Verify(x => x.NavigateToAsync(NavigationConstants.TimeTrackingPage, null), Times.Once);
        }

        /// <summary>
        /// Verifies that NavigateToPatrolCommand navigates to the patrol page
        /// </summary>
        [Fact]
        public void Test_NavigateToPatrolCommand_NavigatesToPatrolPage()
        {
            // Create a MainViewModel instance using CreateViewModel()
            var viewModel = CreateViewModel();

            // Execute NavigateToPatrolCommand
            viewModel.NavigateToPatrolCommand.Execute(null);

            // Verify that MockNavigationService.NavigateToAsync was called with NavigationConstants.PatrolPage
            MockNavigationService.Verify(x => x.NavigateToAsync(NavigationConstants.PatrolPage, null), Times.Once);
        }

        /// <summary>
        /// Verifies that NavigateToPhotoCaptureCommand navigates to the photo capture page
        /// </summary>
        [Fact]
        public void Test_NavigateToPhotoCaptureCommand_NavigatesToPhotoCapturePage()
        {
            // Create a MainViewModel instance using CreateViewModel()
            var viewModel = CreateViewModel();

            // Execute NavigateToPhotoCaptureCommand
            viewModel.NavigateToPhotoCaptureCommand.Execute(null);

            // Verify that MockNavigationService.NavigateToAsync was called with NavigationConstants.PhotoCapturePage
            MockNavigationService.Verify(x => x.NavigateToAsync(NavigationConstants.PhotoCapturePage, null), Times.Once);
        }

        /// <summary>
        /// Verifies that NavigateToActivityReportCommand navigates to the activity report page
        /// </summary>
        [Fact]
        public void Test_NavigateToActivityReportCommand_NavigatesToActivityReportPage()
        {
            // Create a MainViewModel instance using CreateViewModel()
            var viewModel = CreateViewModel();

            // Execute NavigateToActivityReportCommand
            viewModel.NavigateToActivityReportCommand.Execute(null);

            // Verify that MockNavigationService.NavigateToAsync was called with NavigationConstants.ActivityReportPage
            MockNavigationService.Verify(x => x.NavigateToAsync(NavigationConstants.ActivityReportPage, null), Times.Once);
        }

        /// <summary>
        /// Verifies that NavigateToSettingsCommand navigates to the settings page
        /// </summary>
        [Fact]
        public void Test_NavigateToSettingsCommand_NavigatesToSettingsPage()
        {
            // Create a MainViewModel instance using CreateViewModel()
            var viewModel = CreateViewModel();

            // Execute NavigateToSettingsCommand
            viewModel.NavigateToSettingsCommand.Execute(null);

            // Verify that MockNavigationService.NavigateToAsync was called with NavigationConstants.SettingsPage
            MockNavigationService.Verify(x => x.NavigateToAsync(NavigationConstants.SettingsPage, null), Times.Once);
        }

        /// <summary>
        /// Verifies that SyncNowCommand syncs data when network is connected
        /// </summary>
        [Fact]
        public async Task Test_SyncNowCommand_WithNetworkConnected_SyncsData()
        {
            // Setup MockNetworkService to return IsConnected = true
            MockNetworkService.Setup(x => x.IsConnected)
                .Returns(true);

            // Setup MockSyncService to return a successful SyncResult
            MockSyncService.Setup(x => x.SyncAll(default))
                .ReturnsAsync(new SyncResult { SuccessCount = 5, FailureCount = 0 });

            // Create a MainViewModel instance using CreateViewModel()
            var viewModel = CreateViewModel();

            // Execute SyncNowCommand
            await viewModel.SyncNowCommand.ExecuteAsync(null);

            // Verify that MockSyncService.SyncAll() was called
            MockSyncService.Verify(x => x.SyncAll(default), Times.Once);

            // Verify that MockSyncService.GetSyncStatus() was called
            MockSyncService.Verify(x => x.GetSyncStatus(), Times.Once);
        }

        /// <summary>
        /// Verifies that SyncNowCommand shows an error when network is disconnected
        /// </summary>
        [Fact]
        public async Task Test_SyncNowCommand_WithNetworkDisconnected_ShowsError()
        {
            // Setup MockNetworkService to return IsConnected = false
            MockNetworkService.Setup(x => x.IsConnected)
                .Returns(false);

            // Create a MainViewModel instance using CreateViewModel()
            var viewModel = CreateViewModel();

            // Execute SyncNowCommand
            await viewModel.SyncNowCommand.ExecuteAsync(null);

            // Verify that MockSyncService.SyncAll() was not called
            MockSyncService.Verify(x => x.SyncAll(default), Times.Never);

            // Assert that ErrorMessage is set and HasError is true
            viewModel.ErrorMessage.Should().Be("Cannot synchronize while offline. Please check your network connection.");
            viewModel.HasError.Should().BeTrue();
        }

        /// <summary>
        /// Verifies that LogoutCommand logs out and navigates to login page
        /// </summary>
        [Fact]
        public async Task Test_LogoutCommand_LogsOutAndNavigatesToLogin()
        {
            // Create a MainViewModel instance using CreateViewModel()
            var viewModel = CreateViewModel();

            // Execute LogoutCommand
            await viewModel.LogoutCommand.ExecuteAsync(null);

            // Verify that MockAuthService.Logout() was called
            MockAuthService.Verify(x => x.Logout(), Times.Once);

            // Verify that MockNavigationService.NavigateToRootAsync() was called
            MockNavigationService.Verify(x => x.NavigateToRootAsync(), Times.Once);
        }

        /// <summary>
        /// Verifies that OnTimeTrackingStatusChanged updates clock and location status
        /// </summary>
        [Fact]
        public void Test_OnTimeTrackingStatusChanged_UpdatesClockAndLocationStatus()
        {
            // Create a MainViewModel instance using CreateViewModel()
            var viewModel = CreateViewModel();

            // Create a ClockStatus with IsClocked = true
            var clockStatus = new ClockStatus { IsClocked = true };

            // Create ClockStatusChangedEventArgs with the ClockStatus
            var eventArgs = new ClockStatusChangedEventArgs(clockStatus);

            // Raise the StatusChanged event on MockTimeTrackingService with the event args
            MockTimeTrackingService.Raise(x => x.StatusChanged += null, eventArgs);

            // Assert that IsClockInActive is true
            viewModel.IsClockInActive.Should().BeTrue();

            // Verify that MockLocationService.IsTracking() was called
            MockLocationService.Verify(x => x.IsTracking, Times.Once);
        }

        /// <summary>
        /// Verifies that OnNetworkConnectivityChanged updates network status
        /// </summary>
        [Fact]
        public void Test_OnNetworkConnectivityChanged_UpdatesNetworkStatus()
        {
            // Create a MainViewModel instance using CreateViewModel()
            var viewModel = CreateViewModel();

            // Create ConnectivityChangedEventArgs with IsConnected = true
            var eventArgs = new ConnectivityChangedEventArgs(true, "WiFi", ConnectionQuality.Excellent);

            // Raise the ConnectivityChanged event on MockNetworkService with the event args
            MockNetworkService.Raise(x => x.ConnectivityChanged += null, eventArgs);

            // Assert that IsNetworkConnected is true
            viewModel.IsNetworkConnected.Should().BeTrue();

            // Verify that MockSyncService.GetSyncStatus() was called
            MockSyncService.Verify(x => x.GetSyncStatus(), Times.Once);
        }

        /// <summary>
        /// Verifies that OnSyncStatusChanged updates sync status
        /// </summary>
        [Fact]
        public async Task Test_OnSyncStatusChanged_UpdatesSyncStatus()
        {
            // Setup MockSyncService to return IsSyncing = true
            MockSyncService.Setup(x => x.IsSyncing)
                .Returns(true);

            // Setup MockSyncService to return a dictionary with pending items
            MockSyncService.Setup(x => x.GetSyncStatus())
                .ReturnsAsync(new Dictionary<string, int> { { "TimeRecord", 2 }, { "Photo", 3 } });

            // Create a MainViewModel instance using CreateViewModel()
            var viewModel = CreateViewModel();

            // Create SyncStatusChangedEventArgs with EntityType = 'TimeRecord' and Status = 'Completed'
            var eventArgs = new SyncStatusChangedEventArgs("TimeRecord", "Completed", 1, 2);

            // Raise the SyncStatusChanged event on MockSyncService with the event args
            MockSyncService.Raise(x => x.SyncStatusChanged += null, eventArgs);

            // Assert that IsSyncing is true
            viewModel.IsSyncing.Should().BeTrue();

            // Verify that MockSyncService.GetSyncStatus() was called
            MockSyncService.Verify(x => x.GetSyncStatus(), Times.Exactly(2));

            // Assert that PendingSyncItems is set to the sum of pending items
            viewModel.PendingSyncItems.Should().Be(5);
        }

        /// <summary>
        /// Verifies that Dispose unsubscribes from all events
        /// </summary>
        [Fact]
        public void Test_Dispose_UnsubscribesFromEvents()
        {
            // Create a MainViewModel instance using CreateViewModel()
            var viewModel = CreateViewModel();

            // Call Dispose() on the view model
            viewModel.Dispose();

            // Verify that MockNetworkService.StopMonitoring() was called
            MockNetworkService.Verify(x => x.StopMonitoring(), Times.Once);

            // Raise events on services to verify no exceptions are thrown and no handlers are called
            MockTimeTrackingService.Raise(x => x.StatusChanged += null, new ClockStatusChangedEventArgs(new ClockStatus()));
            MockNetworkService.Raise(x => x.ConnectivityChanged += null, new ConnectivityChangedEventArgs(true, "WiFi", ConnectionQuality.Excellent));
            MockSyncService.Raise(x => x.SyncStatusChanged += null, new SyncStatusChangedEventArgs("TimeRecord", "Completed", 1, 1));
        }
    }
}