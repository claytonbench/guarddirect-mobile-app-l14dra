using Android.App; // Android.App latest
using Android.Content.PM; // Android.Content.PM latest
using Android.OS; // Android.OS latest
using Microsoft.Extensions.Logging; // Microsoft.Extensions.Logging 8.0.0
using Microsoft.Maui; // Microsoft.Maui 8.0.0
using Microsoft.Maui.ApplicationModel; // Microsoft.Maui.ApplicationModel 8.0.0
using Microsoft.Maui.Controls; // Microsoft.Maui.Controls 8.0.0
using Microsoft.Maui.Controls.Hosting; // Microsoft.Maui.Controls.Hosting 8.0.0
using SecurityPatrol.Helpers; // Internal import
using SecurityPatrol.MauiProgram; // Internal import
using System; // System 8.0.0

namespace SecurityPatrol.Platforms.Android
{
    /// <summary>
    /// Main Android activity class for the Security Patrol application that handles activity lifecycle and runtime permissions
    /// </summary>
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        private ILogger<MainActivity> _logger;
        private IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the MainActivity class
        /// </summary>
        public MainActivity()
        {
            // LD1: Call base constructor
            // LD1: Initialize any required platform-specific configurations
        }

        /// <summary>
        /// Called when the activity is first created
        /// </summary>
        /// <param name="savedInstanceState">If the activity is being re-initialized after previously being shut down then this Bundle contains the data it most recently supplied in onSaveInstanceState(Bundle). Note: This value may be null.</param>
        [protected override]
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // LD1: Get logger and service provider from dependency injection
            _serviceProvider = MauiProgram.CreateMauiApp().Services;
            _logger = (ILogger<MainActivity>)_serviceProvider.GetService(typeof(ILogger<MainActivity>));

            // LD1: Log activity creation
            _logger.LogInformation("MainActivity created");

            // LD1: Initialize the MAUI application by calling Microsoft.Maui.MauiApp.CreateMauiApp()
            Microsoft.Maui.MauiApp.CreateMauiApp();

            // LD1: Request necessary permissions for the application
            RequestApplicationPermissionsAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Called when the activity resumes from a paused state
        /// </summary>
        [protected override]
        protected override void OnResume()
        {
            base.OnResume();

            // LD1: Log activity resume
            _logger.LogInformation("MainActivity resumed");

            // LD1: Check and request any permissions that might have been revoked while the app was paused
            RequestApplicationPermissionsAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Called when the activity is paused
        /// </summary>
        [protected override]
        protected override void OnPause()
        {
            // LD1: Log activity pause
            _logger.LogInformation("MainActivity paused");

            // LD1: Perform any necessary cleanup or state saving
            // LD1: (Currently no specific operations needed)

            base.OnPause();
        }

        /// <summary>
        /// Called when the activity is being destroyed
        /// </summary>
        [protected override]
        protected override void OnDestroy()
        {
            // LD1: Log activity destruction
            _logger.LogInformation("MainActivity destroyed");

            // LD1: Perform cleanup of any resources
            // LD1: (Currently no specific operations needed)

            base.OnDestroy();
        }

        /// <summary>
        /// Called when permission request results are received
        /// </summary>
        /// <param name="requestCode">The request code passed in RequestPermissions(String[], Int32)</param>
        /// <param name="permissions">The requested permissions. Never null</param>
        /// <param name="grantResults">The grant results for the corresponding permissions which is either PERMISSION_GRANTED or PERMISSION_DENIED. Never null</param>
        [public override]
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Android.Content.PM.Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            // LD1: Log permission request results
            _logger.LogInformation("Received permission request results");

            // LD1: Process permission results based on requestCode
            // LD1: Handle location permission results
            // LD1: Handle camera permission results
            // LD1: Handle storage permission results
            // LD1: Notify appropriate services about permission changes
        }

        /// <summary>
        /// Requests all necessary permissions for the application
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        private async Task RequestApplicationPermissionsAsync()
        {
            // LD1: Log permission request initiation
            _logger.LogInformation("Requesting application permissions");

            // LD1: Request location permissions using PermissionHelper.RequestLocationPermissionsAsync()
            await PermissionHelper.RequestLocationPermissionsAsync(true, _logger);

            // LD1: If location permissions granted, request background location permissions
            if (await PermissionHelper.CheckLocationPermissionsAsync(_logger))
            {
                await CheckAndRequestBackgroundLocationPermissionAsync();
            }

            // LD1: Request camera permissions using PermissionHelper.RequestCameraPermissionAsync()
            await PermissionHelper.RequestCameraPermissionAsync(true, _logger);

            // LD1: Request storage permissions using PermissionHelper.RequestStoragePermissionsAsync()
            await PermissionHelper.RequestStoragePermissionsAsync(true, _logger);

            // LD1: Log permission request completion
            _logger.LogInformation("Application permissions requested");
        }

        /// <summary>
        /// Checks and requests background location permission if needed
        /// </summary>
        /// <returns>True if permission is granted, false otherwise</returns>
        private async Task<bool> CheckAndRequestBackgroundLocationPermissionAsync()
        {
            // LD1: Check if Android version requires explicit background location permission (Android 10+)
            if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.Q)
            {
                // LD1: If not required, return true
                return true;
            }

            // LD1: Request background location permission using PermissionHelper.RequestBackgroundLocationPermissionAsync()
            return await PermissionHelper.RequestBackgroundLocationPermissionAsync(true, _logger);
        }
    }
}