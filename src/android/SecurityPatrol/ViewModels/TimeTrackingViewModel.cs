using System;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SecurityPatrol.Constants;
using SecurityPatrol.Models;
using SecurityPatrol.Services;
using SecurityPatrol.ViewModels;

namespace SecurityPatrol.ViewModels
{
    /// <summary>
    /// ViewModel for the time tracking page that handles clock in/out operations,
    /// displays current clock status, and provides navigation to time history.
    /// </summary>
    [ObservableObject]
    public partial class TimeTrackingViewModel : BaseViewModel
    {
        private readonly ITimeTrackingService _timeTrackingService;
        private readonly ILocationService _locationService;

        [ObservableProperty]
        private bool _isClockInEnabled;

        [ObservableProperty]
        private bool _isClockOutEnabled;

        [ObservableProperty]
        private string _clockStatusText;

        [ObservableProperty]
        private string _lastClockInText;

        [ObservableProperty]
        private string _lastClockOutText;

        [ObservableProperty]
        private ClockStatus _currentStatus;

        /// <summary>
        /// Command to execute the clock in operation.
        /// </summary>
        public IAsyncRelayCommand ClockInCommand { get; }

        /// <summary>
        /// Command to execute the clock out operation.
        /// </summary>
        public IAsyncRelayCommand ClockOutCommand { get; }

        /// <summary>
        /// Command to navigate to the time history page.
        /// </summary>
        public IAsyncRelayCommand ViewHistoryCommand { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeTrackingViewModel"/> class.
        /// </summary>
        /// <param name="navigationService">Service for navigation between pages.</param>
        /// <param name="authenticationStateProvider">Service for authentication state.</param>
        /// <param name="timeTrackingService">Service for time tracking operations.</param>
        /// <param name="locationService">Service for location operations.</param>
        public TimeTrackingViewModel(
            INavigationService navigationService,
            IAuthenticationStateProvider authenticationStateProvider,
            ITimeTrackingService timeTrackingService,
            ILocationService locationService)
            : base(navigationService, authenticationStateProvider)
        {
            _timeTrackingService = timeTrackingService ?? throw new ArgumentNullException(nameof(timeTrackingService));
            _locationService = locationService ?? throw new ArgumentNullException(nameof(locationService));

            Title = "Time Tracking";
            
            // Initialize properties
            CurrentStatus = new ClockStatus();
            ClockStatusText = "Loading...";
            LastClockInText = "Not available";
            LastClockOutText = "Not available";
            IsClockInEnabled = false;
            IsClockOutEnabled = false;

            // Initialize commands
            ClockInCommand = new AsyncRelayCommand(
                async () => await ExecuteWithBusyIndicator(PerformClockInAsync),
                () => IsClockInEnabled);

            ClockOutCommand = new AsyncRelayCommand(
                async () => await ExecuteWithBusyIndicator(PerformClockOutAsync),
                () => IsClockOutEnabled);

            ViewHistoryCommand = new AsyncRelayCommand(
                async () => await ExecuteWithBusyIndicator(ViewHistoryAsync));

            // Subscribe to status changed event
            _timeTrackingService.StatusChanged += OnStatusChanged;
        }

        /// <summary>
        /// Initializes the ViewModel by loading the current clock status.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            await LoadCurrentStatusAsync();
        }

        /// <summary>
        /// Loads the current clock status from the time tracking service.
        /// </summary>
        private async Task LoadCurrentStatusAsync()
        {
            // Check if user is authenticated
            if (!await IsUserAuthenticated())
            {
                return;
            }

            try
            {
                // Get current status from service
                var status = await _timeTrackingService.GetCurrentStatus();
                CurrentStatus = status;
                
                // Update UI based on current status
                UpdateStatusDisplay();
            }
            catch (Exception ex)
            {
                SetError($"Failed to load clock status: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the UI display properties based on the current clock status.
        /// </summary>
        private void UpdateStatusDisplay()
        {
            // Update status text
            ClockStatusText = CurrentStatus.IsClocked ? "Clocked In" : "Clocked Out";
            
            // Update last clock in/out times
            LastClockInText = CurrentStatus.LastClockInTime.HasValue
                ? CurrentStatus.LastClockInTime.Value.ToLocalTime().ToString("g")
                : "Not available";
                
            LastClockOutText = CurrentStatus.LastClockOutTime.HasValue
                ? CurrentStatus.LastClockOutTime.Value.ToLocalTime().ToString("g")
                : "Not available";
                
            // Update button states
            IsClockInEnabled = !CurrentStatus.IsClocked;
            IsClockOutEnabled = CurrentStatus.IsClocked;
        }

        /// <summary>
        /// Performs the clock in operation.
        /// </summary>
        private async Task PerformClockInAsync()
        {
            // Check if user is authenticated
            if (!await IsUserAuthenticated())
            {
                SetError("You must be logged in to clock in.");
                return;
            }

            try
            {
                // Get current location before clocking in
                var location = await _locationService.GetCurrentLocation();
                
                // Perform clock in through service
                var record = await _timeTrackingService.ClockIn();
                
                // Status will be updated via the StatusChanged event
            }
            catch (InvalidOperationException ex)
            {
                SetError(ex.Message);
            }
            catch (UnauthorizedAccessException)
            {
                SetError("Location permission is required for clock in. Please enable it in settings.");
            }
            catch (TimeoutException)
            {
                SetError("Unable to get your current location. Please try again.");
            }
            catch (Exception ex)
            {
                SetError($"Error during clock in: {ex.Message}");
            }
        }

        /// <summary>
        /// Performs the clock out operation.
        /// </summary>
        private async Task PerformClockOutAsync()
        {
            // Check if user is authenticated
            if (!await IsUserAuthenticated())
            {
                SetError("You must be logged in to clock out.");
                return;
            }

            try
            {
                // Get current location before clocking out
                var location = await _locationService.GetCurrentLocation();
                
                // Perform clock out through service
                var record = await _timeTrackingService.ClockOut();
                
                // Status will be updated via the StatusChanged event
            }
            catch (InvalidOperationException ex)
            {
                SetError(ex.Message);
            }
            catch (UnauthorizedAccessException)
            {
                SetError("Location permission is required for clock out. Please enable it in settings.");
            }
            catch (TimeoutException)
            {
                SetError("Unable to get your current location. Please try again.");
            }
            catch (Exception ex)
            {
                SetError($"Error during clock out: {ex.Message}");
            }
        }

        /// <summary>
        /// Navigates to the time history page.
        /// </summary>
        private async Task ViewHistoryAsync()
        {
            await NavigationService.NavigateToAsync(NavigationConstants.TimeHistoryPage);
        }

        /// <summary>
        /// Event handler for TimeTrackingService.StatusChanged event.
        /// </summary>
        private void OnStatusChanged(object sender, ClockStatusChangedEventArgs e)
        {
            CurrentStatus = e.Status;
            UpdateStatusDisplay();
        }

        /// <summary>
        /// Disposes of resources used by the ViewModel.
        /// </summary>
        public override void Dispose()
        {
            // Unsubscribe from events
            _timeTrackingService.StatusChanged -= OnStatusChanged;
            
            base.Dispose();
        }
    }
}