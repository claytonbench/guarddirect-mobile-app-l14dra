using System; // System 8.0.0
using System.IO; // System.IO 8.0.0
using System.Linq; // System.Linq 8.0.0
using Xamarin.UITest; // Xamarin.UITest 3.2.9
using Xamarin.UITest.Configuration; // Xamarin.UITest 3.2.9
using SecurityPatrol.TestCommon.Constants;

namespace SecurityPatrol.MAUI.UITests.Setup
{
    /// <summary>
    /// Enum representing the platforms supported for UI testing.
    /// </summary>
    public enum Platform
    {
        Android,
        iOS
    }

    /// <summary>
    /// Static class responsible for initializing and configuring the application for UI testing.
    /// </summary>
    public static class AppInitializer
    {
        /// <summary>
        /// Starts the Security Patrol application for UI testing with the specified platform configuration.
        /// </summary>
        /// <param name="platform">The platform to initialize the app for</param>
        /// <returns>The initialized application instance for UI testing</returns>
        public static IApp StartApp(Platform platform)
        {
            IApp app;
            
            if (platform == Platform.Android)
            {
                app = ConfigureApp
                    .Android
                    .ApkFile(GetApkPath())
                    .EnableLocalScreenshots()
                    .PreferIdeSettings()
                    .StartApp();
            }
            else
            {
                // Note: iOS is not supported in this initial version as per technical specs
                // This is included as a placeholder for potential future iOS support
                app = ConfigureApp
                    .iOS
                    .EnableLocalScreenshots()
                    .PreferIdeSettings()
                    .StartApp();
            }
            
            ConfigureEnvironment(app);
            WaitForAppReady(app);
            
            return app;
        }
        
        /// <summary>
        /// Resets the application to its initial state for a clean test environment.
        /// </summary>
        /// <param name="app">The application instance to reset</param>
        public static void ResetApp(IApp app)
        {
            // Clear app data for a clean test state
            app.Invoke("clearAppData");
            
            // Restart the app
            app.Repl();
            
            // Wait for app to be ready again
            WaitForAppReady(app);
        }
        
        /// <summary>
        /// Gets the path to the Android APK file for testing.
        /// </summary>
        /// <returns>The path to the APK file</returns>
        private static string GetApkPath()
        {
            // Get the directory containing the test assembly
            var currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            
            // Common build output paths to check (debug and release)
            var possibleApkPaths = new[]
            {
                // Debug builds
                Path.Combine(currentDirectory, "..", "..", "..", "..", "..", "src", "MAUI", "SecurityPatrol.MAUI", "bin", "Debug", "net8.0-android", "com.securitypatrol.app.apk"),
                // Release builds
                Path.Combine(currentDirectory, "..", "..", "..", "..", "..", "src", "MAUI", "SecurityPatrol.MAUI", "bin", "Release", "net8.0-android", "com.securitypatrol.app.apk"),
                // CI/CD builds may place the APK in a different location
                Path.Combine(currentDirectory, "apk", "SecurityPatrol.apk"),
                // App Center builds
                Path.Combine(currentDirectory, "SecurityPatrol.apk")
            };
            
            // Find the first path that exists
            var apkPath = possibleApkPaths.FirstOrDefault(File.Exists);
            
            if (string.IsNullOrEmpty(apkPath))
            {
                throw new FileNotFoundException("Could not find the Security Patrol APK file. Ensure the app is built before running UI tests.");
            }
            
            return apkPath;
        }
        
        /// <summary>
        /// Configures the test environment with necessary settings and variables.
        /// </summary>
        /// <param name="app">The application instance to configure</param>
        private static void ConfigureEnvironment(IApp app)
        {
            // Set environment variables for testing
            app.Invoke("setTestMode", true);
            
            // Configure app to use mock services
            app.Invoke("setApiBaseUrl", TestConstants.TestApiBaseUrl);
            
            // Additional test environment configuration
            app.Invoke("enableOfflineMode", false); // Ensure online mode for tests by default
            app.Invoke("disableAnimations", true); // Disable animations for more reliable UI testing
            app.Invoke("setTestTimeout", TestConstants.TestTimeoutMilliseconds);
            app.Invoke("setMockLocationEnabled", true); // Use mock locations for testing
            app.Invoke("setMockLocation", TestConstants.TestLatitude, TestConstants.TestLongitude, TestConstants.TestAccuracy);
        }
        
        /// <summary>
        /// Waits for the application to be fully initialized and ready for testing.
        /// </summary>
        /// <param name="app">The application instance to wait for</param>
        private static void WaitForAppReady(IApp app)
        {
            // Wait for splash screen to disappear (if applicable)
            try
            {
                app.WaitForElement(c => c.Marked("SplashScreen"), timeout: TimeSpan.FromSeconds(5));
                app.WaitForNoElement(c => c.Marked("SplashScreen"), timeout: TimeSpan.FromSeconds(10));
            }
            catch
            {
                // Splash screen may not be present or may have already disappeared
                // Continue with initialization
            }
            
            // Wait for main app elements to be visible - handle different possible initial states
            try
            {
                // Try to wait for authentication screen elements
                app.WaitForElement(c => c.Marked("PhoneNumberEntry"), timeout: TimeSpan.FromSeconds(10));
                Console.WriteLine("Application initialized at authentication screen");
                return;
            }
            catch
            {
                // Authentication screen not found, continue checking other screens
            }
            
            try
            {
                // Check for verification code screen
                app.WaitForElement(c => c.Marked("VerificationCodeEntry"), timeout: TimeSpan.FromSeconds(5));
                Console.WriteLine("Application initialized at verification code screen");
                return;
            }
            catch
            {
                // Verification code screen not found, continue checking
            }
            
            try
            {
                // Check if we're already at the main app screen
                app.WaitForElement(c => c.Marked("MainTabControl"), timeout: TimeSpan.FromSeconds(5));
                Console.WriteLine("Application initialized at main screen");
                return;
            }
            catch
            {
                // Main screen not found, throw exception as we couldn't find any known screen
                throw new Exception("Could not determine the current application state. " +
                    "Application failed to initialize to a known state for testing.");
            }
        }
    }
}