using System; // System 8.0+
using System.Threading.Tasks; // System.Threading.Tasks 8.0+
using Microsoft.Extensions.DependencyInjection; // Microsoft.Extensions.DependencyInjection 8.0+
using Microsoft.Extensions.Logging; // Microsoft.Extensions.Logging 8.0+
using Microsoft.Maui; // Microsoft.Maui 8.0+
using Microsoft.Maui.Hosting; // Microsoft.Maui.Hosting 8.0+
using Xunit; // xunit 2.4.2
using SecurityPatrol.TestCommon.Fixtures; // ApiServerFixture, TestDatabaseFixture
using SecurityPatrol.TestCommon.Constants; // TestConstants
using SecurityPatrol.MAUI.IntegrationTests.Setup; // IntegrationTestBase

namespace SecurityPatrol.MAUI.IntegrationTests.Setup
{
    /// <summary>
    /// A test fixture that provides a configured MAUI application environment for integration testing.
    /// Implements IAsyncLifetime for proper test lifecycle management.
    /// </summary>
    public class MauiTestFixture : IAsyncLifetime
    {
        /// <summary>
        /// Gets the ApiServerFixture instance.
        /// </summary>
        public ApiServerFixture ApiServer { get; private set; }

        /// <summary>
        /// Gets the TestDatabaseFixture instance.
        /// </summary>
        public TestDatabaseFixture Database { get; private set; }

        /// <summary>
        /// Gets the IServiceProvider instance.
        /// </summary>
        public IServiceProvider ServiceProvider { get; private set; }

        private readonly ILogger<MauiTestFixture> _logger;
        private MauiApp _mauiApp;
        private bool _isInitialized;

        /// <summary>
        /// Initializes a new instance of the MauiTestFixture class.
        /// </summary>
        public MauiTestFixture()
        {
            // Create a logger factory for testing
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddConsole()
                    .SetMinimumLevel(LogLevel.Information);
            });

            // Create a logger for the fixture
            _logger = loggerFactory.CreateLogger<MauiTestFixture>();

            // Initialize ApiServer with a new ApiServerFixture instance
            ApiServer = new ApiServerFixture();

            // Initialize Database with a new TestDatabaseFixture instance
            Database = new TestDatabaseFixture();

            _isInitialized = false;

            _logger.LogInformation("MauiTestFixture initialized");
        }

        /// <summary>
        /// Initializes the MAUI test environment asynchronously. This is called automatically by the xUnit test framework before tests run.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task InitializeAsync()
        {
            _logger.LogInformation("Initializing MAUI test environment...");

            // Initialize the API server
            await ApiServer.InitializeAsync();

            // Initialize the test database
            await Database.InitializeAsync();

            // Create and configure the MAUI application builder
            _mauiApp = CreateMauiApp();

            // Extract the service provider from the MAUI application
            ServiceProvider = _mauiApp.Services;

            _isInitialized = true;

            _logger.LogInformation("MAUI test environment initialized successfully");
        }

        /// <summary>
        /// Cleans up the MAUI test environment asynchronously. This is called automatically by the xUnit test framework after tests complete.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task DisposeAsync()
        {
            _logger.LogInformation("Cleaning up MAUI test environment...");

            // Dispose the MAUI application if it exists
            if (_mauiApp != null)
            {
                ((IDisposable)_mauiApp).Dispose();
                _mauiApp = null;
            }

            // Dispose the API server
            await ApiServer.DisposeAsync();

            // Dispose the database
            Database.Dispose();

            _isInitialized = false;

            _logger.LogInformation("MAUI test environment cleanup complete");
        }

        /// <summary>
        /// Creates and configures a MAUI application for testing.
        /// </summary>
        /// <returns>The configured MAUI application instance</returns>
        private MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .ConfigureMauiHandlers((handlers) =>
                {
#if ANDROID
                    // This is required to fix the linker issue with Xamarin.Google.Android.Material
                    // https://github.com/dotnet/maui/issues/8578
                    handlers.AddHandler(typeof(Microsoft.Maui.Controls.ContentView), typeof(Microsoft.Maui.Platform.ContentViewHandler));
#endif
                })
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                })
                .Services.AddSingleton(ApiServer.Server) // Register MockApiServer instance
                .Services.AddSingleton(Database.Connection); // Register SQLiteAsyncConnection instance

            ConfigureServices(builder.Services);

            builder.Logging.AddConsole();

            return builder.Build();
        }

        /// <summary>
        /// Configures the dependency injection container with test-specific service implementations.
        /// </summary>
        /// <param name="services">The service collection to configure</param>
        private void ConfigureServices(IServiceCollection services)
        {
            // Configure the ApiService with the mock server URL
            services.AddHttpClient<IApiService, ApiService>(client =>
            {
                client.BaseAddress = new Uri(ApiServer.BaseUrl);
            });

            // Register the IApiService implementation with the ApiService
            services.AddTransient<IApiService, ApiService>();

            // Register the database connection from the TestDatabaseFixture
            services.AddSingleton(Database.Connection);

            // Register test implementations of all services
            // Configure services to use test fixtures instead of real implementations for external dependencies
        }

        /// <summary>
        /// Gets a service of the specified type from the service provider.
        /// </summary>
        /// <typeparam name="T">The type of the service to retrieve</typeparam>
        /// <returns>The requested service instance</returns>
        public T GetService<T>()
        {
            EnsureInitialized();
            return ServiceProvider.GetService<T>() ?? throw new InvalidOperationException($"Service of type {typeof(T)} not found.");
        }

        /// <summary>
        /// Ensures that the fixture is initialized before use.
        /// </summary>
        private void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("MauiTestFixture is not initialized. Call InitializeAsync() first.");
            }
            _logger.LogDebug("MauiTestFixture initialization check passed");
        }
    }
}