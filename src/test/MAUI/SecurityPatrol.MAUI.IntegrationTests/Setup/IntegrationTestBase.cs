using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection; // Version 8.0.0
using Microsoft.Extensions.Logging; // Version 8.0.0
using Xunit; // Version 2.4.2
using FluentAssertions; // Version 6.11.0
using SecurityPatrol.TestCommon.Fixtures;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Constants;
using SecurityPatrol.Services;
using SecurityPatrol.Models;

namespace SecurityPatrol.MAUI.IntegrationTests.Setup
{
    /// <summary>
    /// Base class for all integration tests in the Security Patrol MAUI application.
    /// Implements IAsyncLifetime for proper test lifecycle management.
    /// </summary>
    public abstract class IntegrationTestBase : IAsyncLifetime
    {
        /// <summary>
        /// Gets the ApiServerFixture instance for setting up mock API responses.
        /// </summary>
        protected ApiServerFixture ApiServer { get; private set; }

        /// <summary>
        /// Gets the TestDatabaseFixture instance for managing the test database.
        /// </summary>
        protected TestDatabaseFixture Database { get; private set; }

        /// <summary>
        /// Gets the logger for recording test execution details.
        /// </summary>
        protected ILogger<IntegrationTestBase> Logger { get; private set; }

        /// <summary>
        /// Gets the service provider for dependency injection.
        /// </summary>
        protected IServiceProvider ServiceProvider { get; private set; }

        /// <summary>
        /// Gets the authentication service for testing authentication-related functionality.
        /// </summary>
        protected IAuthenticationService AuthenticationService { get; private set; }

        /// <summary>
        /// Gets the time tracking service for testing time tracking-related functionality.
        /// </summary>
        protected ITimeTrackingService TimeTrackingService { get; private set; }

        /// <summary>
        /// Gets the location service for testing location-related functionality.
        /// </summary>
        protected ILocationService LocationService { get; private set; }

        /// <summary>
        /// Gets the patrol service for testing patrol-related functionality.
        /// </summary>
        protected IPatrolService PatrolService { get; private set; }

        /// <summary>
        /// Gets the photo service for testing photo-related functionality.
        /// </summary>
        protected IPhotoService PhotoService { get; private set; }

        /// <summary>
        /// Gets the report service for testing report-related functionality.
        /// </summary>
        protected IReportService ReportService { get; private set; }

        /// <summary>
        /// Gets the sync service for testing synchronization-related functionality.
        /// </summary>
        protected ISyncService SyncService { get; private set; }

        /// <summary>
        /// Initializes a new instance of the IntegrationTestBase class with test fixture setup.
        /// </summary>
        public IntegrationTestBase()
        {
            // Create a logger factory for testing
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddConsole()
                    .SetMinimumLevel(LogLevel.Information);
            });

            // Create a logger for the test class
            Logger = loggerFactory.CreateLogger<IntegrationTestBase>();

            // Initialize ApiServer with a new ApiServerFixture instance
            ApiServer = new ApiServerFixture();

            // Initialize Database with a new TestDatabaseFixture instance
            Database = new TestDatabaseFixture();

            Logger.LogInformation("IntegrationTestBase initialized");
        }

        /// <summary>
        /// Initializes the test environment asynchronously. This is called automatically by the xUnit test framework before each test.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public virtual async Task InitializeAsync()
        {
            Logger.LogInformation("Initializing test environment...");

            // Initialize the API server
            await ApiServer.InitializeAsync();

            // Initialize the test database
            await Database.InitializeAsync();

            // Seed the test database with standard test data
            await Database.SeedTestDataAsync();

            // Create service collection and register services
            var services = new ServiceCollection();
            services.AddSingleton(ApiServer.Server); // Register MockApiServer instance
            services.AddSingleton(Database.Connection); // Register SQLiteAsyncConnection instance
            RegisterServices(services);

            // Build the service provider
            ServiceProvider = services.BuildServiceProvider();

            // Initialize service references from the service provider
            await InitializeServicesAsync();

            // Setup mock API responses for common scenarios
            await SetupApiResponsesAsync();

            Logger.LogInformation("Test environment initialized successfully");
        }

        /// <summary>
        /// Cleans up the test environment asynchronously. This is called automatically by the xUnit test framework after each test.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public virtual async Task DisposeAsync()
        {
            Logger.LogInformation("Cleaning up test environment...");

            // Reset the test database to its initial state
            await Database.ResetDatabaseAsync();

            // Reset the API server to its default state
            ApiServer.ResetServer();

            Logger.LogInformation("Test environment cleanup complete");
        }

        /// <summary>
        /// Registers services in the dependency injection container for testing.
        /// </summary>
        /// <param name="services">The service collection to register services with</param>
        protected virtual void RegisterServices(IServiceCollection services)
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

            // Register all service implementations needed for testing
            services.AddTransient<IAuthenticationService, AuthenticationService>();
            services.AddTransient<ITimeTrackingService, TimeTrackingService>();
            services.AddTransient<ILocationService, LocationService>();
            services.AddTransient<IPatrolService, PatrolService>();
            services.AddTransient<IPhotoService, PhotoService>();
            services.AddTransient<IReportService, ReportService>();
            services.AddTransient<ISyncService, SyncService>();
            services.AddTransient<IDatabaseService, DatabaseService>();

            // Register any additional services required by the tests
        }

        /// <summary>
        /// Initializes service references from the service provider for use in tests.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        protected virtual async Task InitializeServicesAsync()
        {
            // Get AuthenticationService from ServiceProvider
            AuthenticationService = ServiceProvider.GetService<IAuthenticationService>();

            // Get TimeTrackingService from ServiceProvider
            TimeTrackingService = ServiceProvider.GetService<ITimeTrackingService>();

            // Get LocationService from ServiceProvider
            LocationService = ServiceProvider.GetService<ILocationService>();

            // Get PatrolService from ServiceProvider
            PatrolService = ServiceProvider.GetService<IPatrolService>();

            // Get PhotoService from ServiceProvider
            PhotoService = ServiceProvider.GetService<IPhotoService>();

            // Get ReportService from ServiceProvider
            ReportService = ServiceProvider.GetService<IReportService>();

            // Get SyncService from ServiceProvider
            SyncService = ServiceProvider.GetService<ISyncService>();

            Logger.LogInformation("Service references initialized successfully");
        }

        /// <summary>
        /// Sets up common API responses for integration tests.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        protected virtual async Task SetupApiResponsesAsync()
        {
            // Call SetupAuthenticationSuccessResponse() for authentication endpoints
            SetupAuthenticationSuccessResponse();

            // Call SetupTimeTrackingSuccessResponse() for time tracking endpoints
            SetupTimeTrackingSuccessResponse();

            // Call SetupLocationTrackingSuccessResponse() for location tracking endpoints
            SetupLocationTrackingSuccessResponse();

            // Call SetupPatrolSuccessResponse() for patrol endpoints
            SetupPatrolSuccessResponse();

            // Call SetupPhotoSuccessResponse() for photo endpoints
            SetupPhotoSuccessResponse();

            // Call SetupReportSuccessResponse() for report endpoints
            SetupReportSuccessResponse();

            Logger.LogInformation("API responses setup successfully");
        }

        /// <summary>
        /// Sets up successful authentication API responses.
        /// </summary>
        protected virtual void SetupAuthenticationSuccessResponse()
        {
            // Create AuthenticationResponse with token and expiry
            var authResponse = new AuthenticationResponse
            {
                Token = TestConstants.TestAuthToken,
                ExpiresAt = DateTime.UtcNow.AddHours(8)
            };

            // Call ApiServer.SetupSuccessResponse for /auth/verify endpoint
            ApiServer.SetupSuccessResponse("/auth/verify", new { VerificationId = Guid.NewGuid() });

            // Call ApiServer.SetupSuccessResponse for /auth/validate endpoint
            ApiServer.SetupSuccessResponse("/auth/validate", authResponse);

            // Call ApiServer.SetupSuccessResponse for /auth/refresh endpoint
            ApiServer.SetupSuccessResponse("/auth/refresh", authResponse);
        }

        /// <summary>
        /// Sets up successful time tracking API responses.
        /// </summary>
        protected virtual void SetupTimeTrackingSuccessResponse()
        {
            // Create TimeRecordResponse with success status
            var timeResponse = new TimeRecordResponse
            {
                Id = Guid.NewGuid().ToString(),
                Status = "success"
            };

            // Call ApiServer.SetupSuccessResponse for /time/clock endpoint
            ApiServer.SetupSuccessResponse("/time/clock", timeResponse);

            // Call ApiServer.SetupSuccessResponse for /time/history endpoint with sample time records
            ApiServer.SetupSuccessResponse("/time/history", new[]
            {
                new
                {
                    Id = Guid.NewGuid().ToString(),
                    Type = "clockIn",
                    Timestamp = DateTime.UtcNow.AddHours(-8).ToString("o"),
                    Location = new
                    {
                        Latitude = TestConstants.TestLatitude,
                        Longitude = TestConstants.TestLongitude
                    }
                },
                new
                {
                    Id = Guid.NewGuid().ToString(),
                    Type = "clockOut",
                    Timestamp = DateTime.UtcNow.AddHours(-1).ToString("o"),
                    Location = new
                    {
                        Latitude = TestConstants.TestLatitude + 0.01,
                        Longitude = TestConstants.TestLongitude - 0.01
                    }
                }
            });
        }

        /// <summary>
        /// Sets up successful location tracking API responses.
        /// </summary>
        protected virtual void SetupLocationTrackingSuccessResponse()
        {
            // Create LocationSyncResponse with success status
            var locationResponse = new LocationSyncResponse
            {
                SyncedIds = new[] { 1, 2, 3 },
                FailedIds = new int[] { }
            };

            // Call ApiServer.SetupSuccessResponse for /location/batch endpoint
            ApiServer.SetupSuccessResponse("/location/batch", locationResponse);

            // Call ApiServer.SetupSuccessResponse for /location/current endpoint with sample location
            ApiServer.SetupSuccessResponse("/location/current", new
            {
                Timestamp = DateTime.UtcNow.ToString("o"),
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude
            });
        }

        /// <summary>
        /// Sets up successful patrol API responses.
        /// </summary>
        protected virtual void SetupPatrolSuccessResponse()
        {
            // Call ApiServer.SetupSuccessResponse for /patrol/locations endpoint with sample locations
            ApiServer.SetupSuccessResponse("/patrol/locations", new[]
            {
                new
                {
                    Id = 1,
                    Name = "Office Building",
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                },
                new
                {
                    Id = 2,
                    Name = "Warehouse",
                    Latitude = TestConstants.TestLatitude + 0.05,
                    Longitude = TestConstants.TestLongitude - 0.05
                }
            });

            // Call ApiServer.SetupSuccessResponse for /patrol/checkpoints endpoint with sample checkpoints
            ApiServer.SetupSuccessResponse("/patrol/checkpoints", new[]
            {
                new
                {
                    Id = 101,
                    LocationId = 1,
                    Name = "Main Entrance",
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                },
                new
                {
                    Id = 102,
                    LocationId = 1,
                    Name = "East Wing",
                    Latitude = TestConstants.TestLatitude + 0.001,
                    Longitude = TestConstants.TestLongitude + 0.001
                },
                new
                {
                    Id = 103,
                    LocationId = 1,
                    Name = "Parking Lot",
                    Latitude = TestConstants.TestLatitude - 0.001,
                    Longitude = TestConstants.TestLongitude - 0.001
                }
            });

            // Call ApiServer.SetupSuccessResponse for /patrol/verify endpoint with success status
            ApiServer.SetupSuccessResponse("/patrol/verify", new { Status = "success" });
        }

        /// <summary>
        /// Sets up successful photo API responses.
        /// </summary>
        protected virtual void SetupPhotoSuccessResponse()
        {
            // Create PhotoUploadResponse with success status
            var photoResponse = new PhotoUploadResponse
            {
                Id = Guid.NewGuid().ToString(),
                Status = "success"
            };

            // Call ApiServer.SetupSuccessResponse for /photos/upload endpoint
            ApiServer.SetupSuccessResponse("/photos/upload", photoResponse);

            // Call ApiServer.SetupSuccessResponse for /photos endpoint with sample photos
            ApiServer.SetupSuccessResponse("/photos", new[]
            {
                new
                {
                    Id = Guid.NewGuid().ToString(),
                    Timestamp = DateTime.UtcNow.ToString("o"),
                    Location = new
                    {
                        Latitude = TestConstants.TestLatitude,
                        Longitude = TestConstants.TestLongitude
                    }
                }
            });
        }

        /// <summary>
        /// Sets up successful report API responses.
        /// </summary>
        protected virtual void SetupReportSuccessResponse()
        {
            // Create ReportResponse with success status
            var reportResponse = new
            {
                Id = Guid.NewGuid().ToString(),
                Status = "success"
            };

            // Call ApiServer.SetupSuccessResponse for /reports POST endpoint
            ApiServer.SetupSuccessResponse("/reports", reportResponse);

            // Call ApiServer.SetupSuccessResponse for /reports GET endpoint with sample reports
            ApiServer.SetupSuccessResponse("/reports", new[]
            {
                new
                {
                    Id = Guid.NewGuid().ToString(),
                    Text = "Test report 1",
                    Timestamp = DateTime.UtcNow.AddHours(-5).ToString("o")
                },
                new
                {
                    Id = Guid.NewGuid().ToString(),
                    Text = "Test report 2",
                    Timestamp = DateTime.UtcNow.AddHours(-2).ToString("o")
                }
            });
        }

        /// <summary>
        /// Sets up error API responses for testing error handling.
        /// </summary>
        /// <param name="endpoint">The API endpoint to configure</param>
        /// <param name="statusCode">The HTTP status code to return</param>
        /// <param name="errorMessage">The error message to return</param>
        protected virtual void SetupApiErrorResponse(string endpoint, int statusCode, string errorMessage)
        {
            // Call ApiServer.SetupErrorResponse with the provided parameters
            ApiServer.SetupErrorResponse(endpoint, statusCode, errorMessage);

            Logger.LogInformation("Setup error response for endpoint {Endpoint} with status code {StatusCode}", endpoint, statusCode);
        }

        /// <summary>
        /// Performs authentication for tests that require an authenticated user.
        /// </summary>
        /// <returns>A task that returns true if authentication was successful</returns>
        protected virtual async Task<bool> AuthenticateAsync()
        {
            // Setup authentication success responses if not already set
            SetupAuthenticationSuccessResponse();

            // Call AuthenticationService.RequestVerificationCode with test phone number
            bool requestSuccess = await AuthenticationService.RequestVerificationCode(TestConstants.TestPhoneNumber);

            // Call AuthenticationService.VerifyCode with test verification code
            bool verifySuccess = await AuthenticationService.VerifyCode(TestConstants.TestVerificationCode);

            // Return true if both operations succeed, otherwise false
            bool isAuthenticated = requestSuccess && verifySuccess;

            Logger.LogInformation("Authentication result: {IsAuthenticated}", isAuthenticated);
            return isAuthenticated;
        }

        /// <summary>
        /// Performs clock in operation for tests that require an active shift.
        /// </summary>
        /// <returns>A task that returns true if clock in was successful</returns>
        protected virtual async Task<bool> ClockInAsync()
        {
            // Setup time tracking success responses if not already set
            SetupTimeTrackingSuccessResponse();

            // Call TimeTrackingService.ClockIn()
            try
            {
                await TimeTrackingService.ClockIn();
                Logger.LogInformation("Clock in successful");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Clock in failed: {Message}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Performs clock out operation to end an active shift.
        /// </summary>
        /// <returns>A task that returns true if clock out was successful</returns>
        protected virtual async Task<bool> ClockOutAsync()
        {
            // Setup time tracking success responses if not already set
            SetupTimeTrackingSuccessResponse();

            // Call TimeTrackingService.ClockOut()
            try
            {
                await TimeTrackingService.ClockOut();
                Logger.LogInformation("Clock out successful");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Clock out failed: {Message}", ex.Message);
                return false;
            }
        }
    }
}