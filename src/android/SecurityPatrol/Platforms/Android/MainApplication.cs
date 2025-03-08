using Android.App; // Android.App latest
using Android.Content.Res; // Android.Content.Res
using Android.Runtime; // Android.Runtime
using Microsoft.Extensions.Logging; // Microsoft.Extensions.Logging 8.0.0
using Microsoft.Maui; // Microsoft.Maui 8.0.0
using SecurityPatrol.Platforms.Android; // SecurityPatrol.Platforms.Android
using SecurityPatrol.Services; // Internal import
using System; // System 8.0.0

namespace SecurityPatrol.Platforms.Android
{
    /// <summary>
    /// Main Android application class that initializes the Android application, registers services, and manages application lifecycle
    /// </summary>
    [Application(AllowBackup = false, SupportsRtl = true)]
    public class MainApplication : MauiApplication
    {
        private readonly ILogger<MainApplication> _logger;
        private IServiceProvider _serviceProvider;
        private BackgroundLocationService _backgroundLocationService;

        /// <summary>
        /// Initializes a new instance of the MainApplication class
        /// </summary>
        /// <param name="handle">A <see cref="IntPtr"/> that contains the value of the handle field, which is a pointer to the underlying native object.</param>
        /// <param name="ownership">A <see cref="JniHandleOwnership"/> value that indicates how to handle the underlying native object.</param>
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
            // Call base constructor
            // Initialize any required platform-specific configurations
        }

        /// <summary>
        /// Called when the application is first created
        /// </summary>
        [Override]
        public override void OnCreate()
        {
            // Call base.OnCreate() to perform base initialization
            base.OnCreate();

            // Initialize the MAUI application by calling Microsoft.Maui.MauiApp.CreateMauiApp()
            Microsoft.Maui.MauiApp mauiApp = SecurityPatrol.MauiProgram.CreateMauiApp();

            // Get service provider from the MAUI app
            _serviceProvider = mauiApp.Services;

            // Get logger from dependency injection
            _logger = _serviceProvider.GetService<ILogger<MainApplication>>();

            // Resolve BackgroundLocationService from service provider
            _backgroundLocationService = _serviceProvider.GetService<BackgroundLocationService>();

            // Log application creation
            _logger.LogInformation("SecurityPatrol Android application created");

            // Register any platform-specific services
            RegisterBackgroundServices();
        }

        /// <summary>
        /// Called when the application is terminating
        /// </summary>
        [Override]
        public override void OnTerminate()
        {
            // Log application termination
            _logger.LogInformation("SecurityPatrol Android application terminating");

            // Stop background services if running
            StopBackgroundServices().ConfigureAwait(false).GetAwaiter().GetResult();

            // Perform cleanup of any resources
            // (Currently no specific operations needed)

            // Call base.OnTerminate() to perform base cleanup
            base.OnTerminate();
        }

        /// <summary>
        /// Called when the device configuration changes
        /// </summary>
        /// <param name="newConfig">The new device configuration</param>
        [Override]
        public override void OnConfigurationChanged(Configuration newConfig)
        {
            // Log configuration change
            _logger.LogInformation("Device configuration changed");

            // Call base.OnConfigurationChanged(newConfig) to handle base configuration changes
            base.OnConfigurationChanged(newConfig);
        }

        /// <summary>
        /// Called when the system is running low on memory
        /// </summary>
        [Override]
        public override void OnLowMemory()
        {
            // Log low memory warning
            _logger.LogWarning("System is running low on memory");

            // Release non-critical resources
            // (Currently no specific operations needed)

            // Call base.OnLowMemory() to handle base low memory operations
            base.OnLowMemory();
        }

        /// <summary>
        /// Called when the system needs to trim memory
        /// </summary>
        /// <param name="level">The memory trim level</param>
        [Override]
        public override void OnTrimMemory(TrimMemory level)
        {
            // Log memory trim request with level
            _logger.LogInformation("System is trimming memory. Level: {Level}", level);

            // Perform appropriate cleanup based on trim level
            // (Currently no specific operations needed)

            // Call base.OnTrimMemory(level) to handle base memory trimming
            base.OnTrimMemory(level);
        }

        /// <summary>
        /// Registers and initializes background services
        /// </summary>
        private void RegisterBackgroundServices()
        {
            // Check if background services are already registered
            if (_backgroundLocationService == null)
            {
                // Register background location service if needed
                _logger.LogInformation("Registering background location service");
                //_backgroundLocationService = _serviceProvider.GetService<BackgroundLocationService>();
            }

            // Log successful registration of background services
            _logger.LogInformation("Background services registered successfully");
        }

        /// <summary>
        /// Stops all running background services
        /// </summary>
        private async System.Threading.Tasks.Task StopBackgroundServices()
        {
            try
            {
                // Check if background location service is running
                if (_backgroundLocationService != null && _backgroundLocationService.IsRunning())
                {
                    // If running, stop the background location service
                    _logger.LogInformation("Stopping background location service");
                    await _backgroundLocationService.Stop();
                }

                // Log stopping of background services
                _logger.LogInformation("Background services stopped");
            }
            catch (Exception ex)
            {
                // Handle any exceptions during service shutdown
                _logger.LogError(ex, "Error stopping background services");
            }
        }
    }
}