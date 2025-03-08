using Microsoft.Maui.Controls; // Microsoft.Maui.Controls 8.0.0
using Microsoft.Maui.Controls.Xaml; // Microsoft.Maui.Controls.Xaml 8.0.0
using System; // System 8.0.0
using System.Threading.Tasks; // System.Threading.Tasks 8.0.0
using SecurityPatrol.Services; // Internal import
using SecurityPatrol.Constants; // Internal import
using SecurityPatrol.Models; // Internal import
using SecurityPatrol.Database; // Internal import

namespace SecurityPatrol
{
    /// <summary>
    /// The main application class that serves as the entry point for the Security Patrol mobile application
    /// </summary>
    public partial class App : Application
    {
        private readonly IAuthenticationStateProvider _authStateProvider;
        private readonly INavigationService _navigationService;
        private readonly ISyncService _syncService;
        private readonly INetworkService _networkService;
        private readonly IDatabaseInitializer _databaseInitializer;

        /// <summary>
        /// Initializes a new instance of the App class with required services
        /// </summary>
        /// <param name="authStateProvider">The authentication state provider.</param>
        /// <param name="navigationService">The navigation service.</param>
        /// <param name="syncService">The synchronization service.</param>
        /// <param name="networkService">The network service.</param>
        /// <param name="databaseInitializer">The database initializer.</param>
        public App(
            IAuthenticationStateProvider authStateProvider,
            INavigationService navigationService,
            ISyncService syncService,
            INetworkService networkService,
            IDatabaseInitializer databaseInitializer)
        {
            // LD1: Store injected services in private fields
            _authStateProvider = authStateProvider ?? throw new ArgumentNullException(nameof(authStateProvider));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _syncService = syncService ?? throw new ArgumentNullException(nameof(syncService));
            _networkService = networkService ?? throw new ArgumentNullException(nameof(networkService));
            _databaseInitializer = databaseInitializer ?? throw new ArgumentNullException(nameof(databaseInitializer));

            // LD1: Call InitializeComponent() to load XAML resources
            InitializeComponent();

            // LD1: Subscribe to authentication state changes
            _authStateProvider.StateChanged += OnAuthenticationStateChanged;

            // LD1: Subscribe to network connectivity changes
            _networkService.ConnectivityChanged += OnConnectivityChanged;

            // LD1: Initialize the database
            InitializeDatabaseAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Called when the application starts
        /// </summary>
        protected override void OnStart()
        {
            // LD1: Call base.OnStart()
            base.OnStart();

            // LD1: Start network monitoring
            _networkService.StartMonitoring();

            // LD1: Check authentication state and navigate accordingly
            CheckAuthenticationStateAsync().ConfigureAwait(false);

            // LD1: Attempt to synchronize data if network is available
            SyncDataAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Called when the application goes to sleep (background)
        /// </summary>
        protected override void OnSleep()
        {
            // LD1: Call base.OnSleep()
            base.OnSleep();

            // LD1: Perform any cleanup or state saving operations
            // (Currently no specific operations needed)
        }

        /// <summary>
        /// Called when the application resumes from sleep
        /// </summary>
        protected override void OnResume()
        {
            // LD1: Call base.OnResume()
            base.OnResume();

            // LD1: Check authentication state and navigate if needed
            CheckAuthenticationStateAsync().ConfigureAwait(false);

            // LD1: Attempt to synchronize data if network is available
            SyncDataAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Initializes the local database
        /// </summary>
        private async Task InitializeDatabaseAsync()
        {
            try
            {
                // LD1: Call _databaseInitializer.InitializeAsync() to initialize the database
                await _databaseInitializer.InitializeAsync();
            }
            catch (Exception ex)
            {
                // LD1: Handle any exceptions during initialization
                Console.WriteLine($"Database initialization failed: {ex.Message}");
                // Consider logging the exception or displaying an error message to the user
            }
        }

        /// <summary>
        /// Checks the current authentication state and navigates accordingly
        /// </summary>
        private async Task CheckAuthenticationStateAsync()
        {
            try
            {
                // LD1: Check if user is authenticated using _authStateProvider.IsAuthenticated()
                bool isAuthenticated = await _authStateProvider.IsAuthenticated();

                // LD1: If authenticated, navigate to MainPage
                if (isAuthenticated)
                {
                    await _navigationService.NavigateToAsync(NavigationConstants.MainPage);
                }
                // LD1: If not authenticated, navigate to PhoneEntryPage
                else
                {
                    await _navigationService.NavigateToAsync(NavigationConstants.PhoneEntryPage);
                }
            }
            catch (Exception ex)
            {
                // LD1: Handle any exceptions during navigation
                Console.WriteLine($"Navigation failed: {ex.Message}");
                // Consider logging the exception or displaying an error message to the user
            }
        }

        /// <summary>
        /// Event handler for authentication state changes
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnAuthenticationStateChanged(object sender, EventArgs e)
        {
            // LD1: Call CheckAuthenticationStateAsync() to update navigation based on new state
            CheckAuthenticationStateAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Event handler for network connectivity changes
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ConnectivityChangedEventArgs"/> instance containing the event data.</param>
        private void OnConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
        {
            // LD1: If connectivity is restored, attempt to synchronize pending data
            if (e.IsConnected)
            {
                SyncDataAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Attempts to synchronize data with the backend
        /// </summary>
        private async Task SyncDataAsync()
        {
            try
            {
                // LD1: Check if network is connected using _networkService.IsConnected()
                if (_networkService.IsConnected)
                {
                    // LD1: If connected, call _syncService.SyncAll() to synchronize all pending data
                    await _syncService.SyncAll();
                }
            }
            catch (Exception ex)
            {
                // LD1: Handle any exceptions during synchronization
                Console.WriteLine($"Data synchronization failed: {ex.Message}");
                // Consider logging the exception or displaying an error message to the user
            }
        }
    }
}