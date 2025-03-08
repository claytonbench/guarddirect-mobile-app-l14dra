using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SecurityPatrol.Constants;
using SecurityPatrol.Models;
using SecurityPatrol.Services;

namespace SecurityPatrol.ViewModels
{
    /// <summary>
    /// ViewModel for the main dashboard page of the Security Patrol application.
    /// Provides properties and commands for displaying status information and
    /// navigating to different features of the application.
    /// </summary>
    public class MainViewModel : BaseViewModel
    {
        #region Properties

        private bool _isClockInActive;
        /// <summary>
        /// Gets or sets a value indicating whether the user is currently clocked in.
        /// </summary>
        public bool IsClockInActive
        {
            get => _isClockInActive;
            set => SetProperty(ref _isClockInActive, value);
        }

        private bool _isLocationTrackingActive;
        /// <summary>
        /// Gets or sets a value indicating whether location tracking is currently active.
        /// </summary>
        public bool IsLocationTrackingActive
        {
            get => _isLocationTrackingActive;
            set => SetProperty(ref _isLocationTrackingActive, value);
        }

        private bool _isPatrolActive;
        /// <summary>
        /// Gets or sets a value indicating whether a patrol is currently active.
        /// </summary>
        public bool IsPatrolActive
        {
            get => _isPatrolActive;
            set => SetProperty(ref _isPatrolActive, value);
        }

        private bool _isNetworkConnected;
        /// <summary>
        /// Gets or sets a value indicating whether the device is connected to a network.
        /// </summary>
        public bool IsNetworkConnected
        {
            get => _isNetworkConnected;
            set => SetProperty(ref _isNetworkConnected, value);
        }

        private int _pendingSyncItems;
        /// <summary>
        /// Gets or sets the number of items waiting to be synchronized.
        /// </summary>
        public int PendingSyncItems
        {
            get => _pendingSyncItems;
            set => SetProperty(ref _pendingSyncItems, value);
        }

        private double _patrolCompletionPercentage;
        /// <summary>
        /// Gets or sets the percentage of completed checkpoints in the active patrol.
        /// </summary>
        public double PatrolCompletionPercentage
        {
            get => _patrolCompletionPercentage;
            set => SetProperty(ref _patrolCompletionPercentage, value);
        }

        private int _verifiedCheckpoints;
        /// <summary>
        /// Gets or sets the number of verified checkpoints in the active patrol.
        /// </summary>
        public int VerifiedCheckpoints
        {
            get => _verifiedCheckpoints;
            set => SetProperty(ref _verifiedCheckpoints, value);
        }

        private int _totalCheckpoints;
        /// <summary>
        /// Gets or sets the total number of checkpoints in the active patrol.
        /// </summary>
        public int TotalCheckpoints
        {
            get => _totalCheckpoints;
            set => SetProperty(ref _totalCheckpoints, value);
        }

        private bool _isSyncing;
        /// <summary>
        /// Gets or sets a value indicating whether a sync operation is in progress.
        /// </summary>
        public bool IsSyncing
        {
            get => _isSyncing;
            set => SetProperty(ref _isSyncing, value);
        }

        #endregion

        #region Commands

        /// <summary>
        /// Gets the command to navigate to the time tracking page.
        /// </summary>
        public ICommand NavigateToTimeTrackingCommand { get; private set; }

        /// <summary>
        /// Gets the command to navigate to the patrol management page.
        /// </summary>
        public ICommand NavigateToPatrolCommand { get; private set; }

        /// <summary>
        /// Gets the command to navigate to the photo capture page.
        /// </summary>
        public ICommand NavigateToPhotoCaptureCommand { get; private set; }

        /// <summary>
        /// Gets the command to navigate to the activity report page.
        /// </summary>
        public ICommand NavigateToActivityReportCommand { get; private set; }

        /// <summary>
        /// Gets the command to navigate to the settings page.
        /// </summary>
        public ICommand NavigateToSettingsCommand { get; private set; }

        /// <summary>
        /// Gets the command to initiate manual synchronization.
        /// </summary>
        public ICommand SyncNowCommand { get; private set; }

        /// <summary>
        /// Gets the command to log out the current user.
        /// </summary>
        public ICommand LogoutCommand { get; private set; }

        #endregion

        #region Services

        private readonly IAuthenticationService AuthenticationService;
        private readonly ITimeTrackingService TimeTrackingService;
        private readonly ILocationService LocationService;
        private readonly IPatrolService PatrolService;
        private readonly ISyncService SyncService;
        private readonly INetworkService NetworkService;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="MainViewModel"/> class.
        /// </summary>
        /// <param name="navigationService">The navigation service.</param>
        /// <param name="authenticationStateProvider">The authentication state provider.</param>
        /// <param name="authenticationService">The authentication service.</param>
        /// <param name="timeTrackingService">The time tracking service.</param>
        /// <param name="locationService">The location service.</param>
        /// <param name="patrolService">The patrol service.</param>
        /// <param name="syncService">The synchronization service.</param>
        /// <param name="networkService">The network service.</param>
        public MainViewModel(
            INavigationService navigationService,
            IAuthenticationStateProvider authenticationStateProvider,
            IAuthenticationService authenticationService,
            ITimeTrackingService timeTrackingService,
            ILocationService locationService,
            IPatrolService patrolService,
            ISyncService syncService,
            INetworkService networkService)
            : base(navigationService, authenticationStateProvider)
        {
            AuthenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
            TimeTrackingService = timeTrackingService ?? throw new ArgumentNullException(nameof(timeTrackingService));
            LocationService = locationService ?? throw new ArgumentNullException(nameof(locationService));
            PatrolService = patrolService ?? throw new ArgumentNullException(nameof(patrolService));
            SyncService = syncService ?? throw new ArgumentNullException(nameof(syncService));
            NetworkService = networkService ?? throw new ArgumentNullException(nameof(networkService));

            Title = "Security Patrol";

            // Initialize properties
            IsClockInActive = false;
            IsLocationTrackingActive = false;
            IsPatrolActive = false;
            IsNetworkConnected = NetworkService.IsConnected;
            PendingSyncItems = 0;
            PatrolCompletionPercentage = 0;
            VerifiedCheckpoints = 0;
            TotalCheckpoints = 0;
            IsSyncing = SyncService.IsSyncing;

            // Initialize commands
            NavigateToTimeTrackingCommand = new RelayCommand(NavigateToTimeTracking);
            NavigateToPatrolCommand = new RelayCommand(NavigateToPatrol);
            NavigateToPhotoCaptureCommand = new RelayCommand(NavigateToPhotoCapture);
            NavigateToActivityReportCommand = new RelayCommand(NavigateToActivityReport);
            NavigateToSettingsCommand = new RelayCommand(NavigateToSettings);
            SyncNowCommand = new AsyncRelayCommand(SyncNow);
            LogoutCommand = new AsyncRelayCommand(Logout);

            // Subscribe to events
            TimeTrackingService.StatusChanged += OnTimeTrackingStatusChanged;
            NetworkService.ConnectivityChanged += OnNetworkConnectivityChanged;
            SyncService.SyncStatusChanged += OnSyncStatusChanged;
        }

        #endregion

        #region Lifecycle Methods

        /// <summary>
        /// Initializes the ViewModel when the page is navigated to.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            NetworkService.StartMonitoring();
            await UpdateClockStatus();
            UpdateLocationTrackingStatus();
            await UpdatePatrolStatus();
            await UpdateSyncStatus();
        }

        /// <summary>
        /// Called when the page appears on screen.
        /// </summary>
        public void OnAppearing()
        {
            RefreshAllStatus();
            NetworkService.StartMonitoring();
        }

        /// <summary>
        /// Called when the page disappears from screen.
        /// </summary>
        public void OnDisappearing()
        {
            NetworkService.StopMonitoring();
        }

        #endregion

        #region Status Update Methods

        /// <summary>
        /// Updates the clock status indicator.
        /// </summary>
        private async Task UpdateClockStatus()
        {
            var status = await TimeTrackingService.GetCurrentStatus();
            IsClockInActive = status.IsClocked;
        }

        /// <summary>
        /// Updates the location tracking status indicator.
        /// </summary>
        private void UpdateLocationTrackingStatus()
        {
            IsLocationTrackingActive = LocationService.IsTracking;
        }

        /// <summary>
        /// Updates the patrol status indicators.
        /// </summary>
        private async Task UpdatePatrolStatus()
        {
            IsPatrolActive = PatrolService.IsPatrolActive;

            if (IsPatrolActive && PatrolService.CurrentLocationId.HasValue)
            {
                var patrolStatus = await PatrolService.GetPatrolStatus(PatrolService.CurrentLocationId.Value);
                TotalCheckpoints = patrolStatus.TotalCheckpoints;
                VerifiedCheckpoints = patrolStatus.VerifiedCheckpoints;
                PatrolCompletionPercentage = patrolStatus.CalculateCompletionPercentage();
            }
            else
            {
                TotalCheckpoints = 0;
                VerifiedCheckpoints = 0;
                PatrolCompletionPercentage = 0;
            }
        }

        /// <summary>
        /// Updates the synchronization status indicators.
        /// </summary>
        private async Task UpdateSyncStatus()
        {
            var syncStatus = await SyncService.GetSyncStatus();
            int totalPending = 0;

            foreach (var count in syncStatus.Values)
            {
                totalPending += count;
            }

            PendingSyncItems = totalPending;
            IsSyncing = SyncService.IsSyncing;
        }

        /// <summary>
        /// Refreshes all status indicators.
        /// </summary>
        private async Task RefreshAllStatus()
        {
            await UpdateClockStatus();
            UpdateLocationTrackingStatus();
            await UpdatePatrolStatus();
            await UpdateSyncStatus();
        }

        #endregion

        #region Navigation Methods

        /// <summary>
        /// Navigates to the time tracking page.
        /// </summary>
        private void NavigateToTimeTracking()
        {
            NavigationService.NavigateToAsync(NavigationConstants.TimeTrackingPage);
        }

        /// <summary>
        /// Navigates to the patrol page.
        /// </summary>
        private void NavigateToPatrol()
        {
            NavigationService.NavigateToAsync(NavigationConstants.PatrolPage);
        }

        /// <summary>
        /// Navigates to the photo capture page.
        /// </summary>
        private void NavigateToPhotoCapture()
        {
            NavigationService.NavigateToAsync(NavigationConstants.PhotoCapturePage);
        }

        /// <summary>
        /// Navigates to the activity report page.
        /// </summary>
        private void NavigateToActivityReport()
        {
            NavigationService.NavigateToAsync(NavigationConstants.ActivityReportPage);
        }

        /// <summary>
        /// Navigates to the settings page.
        /// </summary>
        private void NavigateToSettings()
        {
            NavigationService.NavigateToAsync(NavigationConstants.SettingsPage);
        }

        #endregion

        #region Action Methods

        /// <summary>
        /// Initiates manual synchronization of all pending data.
        /// </summary>
        private async Task SyncNow()
        {
            if (!IsNetworkConnected)
            {
                SetError("Cannot synchronize while offline. Please check your network connection.");
                return;
            }

            await ExecuteWithBusyIndicator(async () =>
            {
                await SyncService.SyncAll();
                await UpdateSyncStatus();
            });
        }

        /// <summary>
        /// Logs out the current user.
        /// </summary>
        private async Task Logout()
        {
            await AuthenticationService.Logout();
            await NavigationService.NavigateToAsync(NavigationConstants.PhoneEntryPage);
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles changes in time tracking status.
        /// </summary>
        private void OnTimeTrackingStatusChanged(object sender, ClockStatusChangedEventArgs e)
        {
            IsClockInActive = e.Status.IsClocked;
            UpdateLocationTrackingStatus();
        }

        /// <summary>
        /// Handles changes in network connectivity.
        /// </summary>
        private void OnNetworkConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
        {
            IsNetworkConnected = e.IsConnected;
            
            // If connection was restored, update sync status
            if (e.IsConnected)
            {
                _ = UpdateSyncStatus();
            }
        }

        /// <summary>
        /// Handles changes in synchronization status.
        /// </summary>
        private async void OnSyncStatusChanged(object sender, SyncStatusChangedEventArgs e)
        {
            IsSyncing = SyncService.IsSyncing;
            await UpdateSyncStatus();
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Disposes of resources used by the ViewModel.
        /// </summary>
        public override void Dispose()
        {
            // Unsubscribe from events
            TimeTrackingService.StatusChanged -= OnTimeTrackingStatusChanged;
            NetworkService.ConnectivityChanged -= OnNetworkConnectivityChanged;
            SyncService.SyncStatusChanged -= OnSyncStatusChanged;

            // Stop network monitoring
            NetworkService.StopMonitoring();

            base.Dispose();
        }

        #endregion
    }
}