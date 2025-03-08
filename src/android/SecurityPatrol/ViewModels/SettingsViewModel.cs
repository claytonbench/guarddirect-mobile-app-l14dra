using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using SecurityPatrol.Constants;
using SecurityPatrol.Models;
using SecurityPatrol.Services;

namespace SecurityPatrol.ViewModels
{
    /// <summary>
    /// ViewModel for the Settings page that provides properties and commands for managing application settings, 
    /// user preferences, and authentication state.
    /// </summary>
    public class SettingsViewModel : BaseViewModel
    {
        private readonly ISettingsService SettingsService;
        private readonly IAuthenticationService AuthenticationService;

        private string _appVersion;
        /// <summary>
        /// Gets the current application version.
        /// </summary>
        public string AppVersion
        {
            get => _appVersion;
            private set => SetProperty(ref _appVersion, value);
        }

        private string _userPhoneNumber;
        /// <summary>
        /// Gets the phone number of the authenticated user.
        /// </summary>
        public string UserPhoneNumber
        {
            get => _userPhoneNumber;
            private set => SetProperty(ref _userPhoneNumber, value);
        }

        private bool _enableBackgroundTracking;
        /// <summary>
        /// Gets or sets whether background location tracking is enabled.
        /// </summary>
        public bool EnableBackgroundTracking
        {
            get => _enableBackgroundTracking;
            set => SetProperty(ref _enableBackgroundTracking, value);
        }

        private bool _enableOfflineMode;
        /// <summary>
        /// Gets or sets whether offline mode is enabled.
        /// </summary>
        public bool EnableOfflineMode
        {
            get => _enableOfflineMode;
            set => SetProperty(ref _enableOfflineMode, value);
        }

        private bool _enableTelemetry;
        /// <summary>
        /// Gets or sets whether telemetry collection is enabled.
        /// </summary>
        public bool EnableTelemetry
        {
            get => _enableTelemetry;
            set => SetProperty(ref _enableTelemetry, value);
        }

        private int _selectedLocationTrackingMode;
        /// <summary>
        /// Gets or sets the selected location tracking mode (0=Default, 1=Low Power, 2=High Accuracy).
        /// </summary>
        public int SelectedLocationTrackingMode
        {
            get => _selectedLocationTrackingMode;
            set => SetProperty(ref _selectedLocationTrackingMode, value);
        }

        private List<string> _locationTrackingModes;
        /// <summary>
        /// Gets the available location tracking modes.
        /// </summary>
        public List<string> LocationTrackingModes
        {
            get => _locationTrackingModes;
            private set => SetProperty(ref _locationTrackingModes, value);
        }

        /// <summary>
        /// Command to save the current settings.
        /// </summary>
        public ICommand SaveSettingsCommand { get; }

        /// <summary>
        /// Command to clear all local data and reset settings to defaults.
        /// </summary>
        public ICommand ClearDataCommand { get; }

        /// <summary>
        /// Command to log out the current user.
        /// </summary>
        public ICommand LogoutCommand { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsViewModel"/> class.
        /// </summary>
        /// <param name="navigationService">The navigation service.</param>
        /// <param name="authenticationStateProvider">The authentication state provider.</param>
        /// <param name="settingsService">The settings service.</param>
        /// <param name="authenticationService">The authentication service.</param>
        public SettingsViewModel(
            INavigationService navigationService,
            IAuthenticationStateProvider authenticationStateProvider,
            ISettingsService settingsService,
            IAuthenticationService authenticationService)
            : base(navigationService, authenticationStateProvider)
        {
            SettingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            AuthenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));

            Title = "Settings";
            AppVersion = AppConstants.AppVersion;
            UserPhoneNumber = string.Empty;

            // Initialize tracking modes
            LocationTrackingModes = new List<string>
            {
                "Default",
                "Low Power",
                "High Accuracy"
            };

            // Initialize default values
            EnableBackgroundTracking = true;
            EnableOfflineMode = false;
            EnableTelemetry = AppConstants.EnableTelemetry;
            SelectedLocationTrackingMode = 0; // Default

            // Initialize commands
            SaveSettingsCommand = new AsyncRelayCommand(SaveSettingsAsync);
            ClearDataCommand = new AsyncRelayCommand(ClearDataAsync);
            LogoutCommand = new AsyncRelayCommand(LogoutAsync);
        }

        /// <summary>
        /// Initializes the ViewModel by loading current settings and user information.
        /// </summary>
        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            await LoadUserInformation();
            await LoadSettings();
        }

        /// <summary>
        /// Loads user information from the authentication state.
        /// </summary>
        private async Task LoadUserInformation()
        {
            var authState = await AuthenticationService.GetAuthenticationState();
            if (authState?.IsAuthenticated == true)
            {
                UserPhoneNumber = authState.PhoneNumber;
            }
            else
            {
                UserPhoneNumber = "Not authenticated";
            }
        }

        /// <summary>
        /// Loads current settings from the settings service.
        /// </summary>
        private async Task LoadSettings()
        {
            // Since ISettingsService has a class constraint, we use string values
            // and convert them to the appropriate types
            
            string backgroundTrackingStr = SettingsService.GetValue<string>("EnableBackgroundTracking", "True");
            EnableBackgroundTracking = bool.TryParse(backgroundTrackingStr, out bool bgTracking) ? bgTracking : true;
            
            string offlineModeStr = SettingsService.GetValue<string>("EnableOfflineMode", "False");
            EnableOfflineMode = bool.TryParse(offlineModeStr, out bool offline) ? offline : false;
            
            string telemetryStr = SettingsService.GetValue<string>("EnableTelemetry", AppConstants.EnableTelemetry.ToString());
            EnableTelemetry = bool.TryParse(telemetryStr, out bool telemetry) ? telemetry : AppConstants.EnableTelemetry;
            
            string trackingModeStr = SettingsService.GetValue<string>("LocationTrackingMode", "0");
            SelectedLocationTrackingMode = int.TryParse(trackingModeStr, out int mode) ? mode : 0;
            
            // Ensure selected mode is valid
            if (SelectedLocationTrackingMode < 0 || SelectedLocationTrackingMode >= LocationTrackingModes.Count)
            {
                SelectedLocationTrackingMode = 0;
            }

            await Task.CompletedTask; // To satisfy the async method signature
        }

        /// <summary>
        /// Saves the current settings to the settings service.
        /// </summary>
        private async Task SaveSettingsAsync()
        {
            await ExecuteWithBusyIndicator(async () =>
            {
                // Save all settings as strings due to the class constraint on ISettingsService
                SettingsService.SetValue("EnableBackgroundTracking", EnableBackgroundTracking.ToString());
                SettingsService.SetValue("EnableOfflineMode", EnableOfflineMode.ToString());
                SettingsService.SetValue("EnableTelemetry", EnableTelemetry.ToString());
                SettingsService.SetValue("LocationTrackingMode", SelectedLocationTrackingMode.ToString());
                
                // Here we might want to notify other services about setting changes
                // For example, if background tracking is disabled, we might need to stop the tracking service
                
                await Task.CompletedTask; // To satisfy the async method signature
            });
        }

        /// <summary>
        /// Clears all local data and settings.
        /// </summary>
        private async Task ClearDataAsync()
        {
            // In a real app, you would show a confirmation dialog first
            bool confirmClear = await Application.Current.MainPage.DisplayAlert(
                "Clear Data", 
                "Are you sure you want to clear all settings and data? This action cannot be undone.", 
                "Yes", "No");
                
            if (!confirmClear)
                return;
            
            await ExecuteWithBusyIndicator(async () =>
            {
                // Clear all settings
                SettingsService.Clear();
                
                // Reset to defaults
                EnableBackgroundTracking = true;
                EnableOfflineMode = false;
                EnableTelemetry = AppConstants.EnableTelemetry;
                SelectedLocationTrackingMode = 0;
                
                await Application.Current.MainPage.DisplayAlert(
                    "Settings Reset", 
                    "All settings have been reset to their default values.", 
                    "OK");
            });
        }

        /// <summary>
        /// Logs out the current user and navigates to the login page.
        /// </summary>
        private async Task LogoutAsync()
        {
            // In a real app, you would show a confirmation dialog first
            bool confirmLogout = await Application.Current.MainPage.DisplayAlert(
                "Logout", 
                "Are you sure you want to log out?", 
                "Yes", "No");
                
            if (!confirmLogout)
                return;
            
            await ExecuteWithBusyIndicator(async () =>
            {
                // Logout
                await AuthenticationService.Logout();
                
                // Navigate to login page
                await NavigationService.NavigateToAsync(NavigationConstants.PhoneEntryPage);
            });
        }

        /// <summary>
        /// Called when the page appears on screen.
        /// </summary>
        public void OnAppearing()
        {
            // This method can be called when returning to the settings page
            // We might want to refresh user information or settings
        }

        /// <summary>
        /// Called when the page disappears from screen.
        /// </summary>
        public void OnDisappearing()
        {
            // This method can be used for cleanup if needed
        }
    }
}