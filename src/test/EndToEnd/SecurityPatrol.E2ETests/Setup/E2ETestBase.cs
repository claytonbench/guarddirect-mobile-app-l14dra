using System; // Version 8.0.0
using System.Threading.Tasks; // Version 8.0.0
using Microsoft.Extensions.DependencyInjection; // Version 8.0.0
using Microsoft.Extensions.Logging; // Version 8.0.0
using Xunit; // Version 2.4.2
using FluentAssertions; // Version 6.11.0
using SecurityPatrol.E2ETests.Setup;
using SecurityPatrol.Services;
using SecurityPatrol.TestCommon.Fixtures; // ApiServerFixture
using SecurityPatrol.TestCommon.Fixtures; // TestDatabaseFixture
using SecurityPatrol.TestCommon.Constants; // TestConstants
using SecurityPatrol.TestCommon.Helpers; // TestAuthHandler

namespace SecurityPatrol.E2ETests.Setup
{
    /// <summary>
    /// Base class for all end-to-end tests in the Security Patrol application.
    /// Implements IAsyncLifetime for proper test lifecycle management.
    /// </summary>
    public abstract class E2ETestBase : IAsyncLifetime
    {
        /// <summary>
        /// Gets the mock API server fixture.
        /// </summary>
        protected ApiServerFixture ApiServer { get; private set; }

        /// <summary>
        /// Gets the test database fixture.
        /// </summary>
        protected TestDatabaseFixture Database { get; private set; }

        /// <summary>
        /// Gets the logger for the test class.
        /// </summary>
        protected ILogger<E2ETestBase> Logger { get; private set; }

        /// <summary>
        /// Gets the service provider for dependency injection.
        /// </summary>
        protected IServiceProvider ServiceProvider { get; private set; }

        /// <summary>
        /// Gets the authentication service.
        /// </summary>
        protected IAuthenticationService AuthenticationService { get; private set; }

        /// <summary>
        /// Gets the time tracking service.
        /// </summary>
        protected ITimeTrackingService TimeTrackingService { get; private set; }

        /// <summary>
        /// Gets the location service.
        /// </summary>
        protected ILocationService LocationService { get; private set; }

        /// <summary>
        /// Gets the patrol service.
        /// </summary>
        protected IPatrolService PatrolService { get; private set; }

        /// <summary>
        /// Gets the photo service.
        /// </summary>
        protected IPhotoService PhotoService { get; private set; }

        /// <summary>
        /// Gets the report service.
        /// </summary>
        protected IReportService ReportService { get; private set; }

        /// <summary>
        /// Gets the synchronization service.
        /// </summary>
        protected ISyncService SyncService { get; private set; }

        private bool _isDisposed;

        /// <summary>
        /// Initializes a new instance of the E2ETestBase class with test fixtures and logging setup.
        /// </summary>
        public E2ETestBase()
        {
            // Create a logger factory for testing
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddConsole()
                    .SetMinimumLevel(LogLevel.Information);
            });

            // Create a logger for the test class
            Logger = loggerFactory.CreateLogger<E2ETestBase>();

            // Initialize ApiServer with a new ApiServerFixture instance
            ApiServer = new ApiServerFixture();

            // Initialize Database with a new TestDatabaseFixture instance
            Database = new TestDatabaseFixture();

            _isDisposed = false;

            Logger.LogInformation("E2ETestBase initialized");
        }

        /// <summary>
        /// Initializes the test environment asynchronously. This is called automatically by the xUnit test framework before each test.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public virtual async Task InitializeAsync()
        {
            Logger.LogInformation("Test initialization start");

            // Ensure global test environment is initialized by calling TestEnvironmentSetup.InitializeEnvironment()
            TestEnvironmentSetup.InitializeEnvironment();

            // Initialize the API server by calling ApiServer.InitializeAsync()
            await ApiServer.InitializeAsync();

            // Initialize the test database by calling Database.InitializeAsync()
            await Database.InitializeAsync();

            // Seed the test database with standard test data using Database.SeedTestDataAsync()
            await Database.SeedTestDataAsync();

            // Create service collection and register services
            var services = new ServiceCollection();
            RegisterServices(services);

            // Build the service provider
            ServiceProvider = services.BuildServiceProvider();

            // Initialize service references from the service provider
            await InitializeServicesAsync();

            // Setup mock API responses for common scenarios
            await SetupApiResponsesAsync();

            Logger.LogInformation("Test initialization complete");
        }

        /// <summary>
        /// Cleans up the test environment asynchronously. This is called automatically by the xUnit test framework after each test.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public virtual async Task DisposeAsync()
        {
            // Check if already disposed
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            Logger.LogInformation("Test cleanup start");

            // Reset the test database to its initial state using Database.ResetDatabaseAsync()
            await Database.ResetDatabaseAsync();

            // Reset the API server to its default state
            ApiServer.ResetServer();

            // Dispose the service provider if not null
            if (ServiceProvider != null)
            {
                if (ServiceProvider is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                ServiceProvider = null;
            }

            Logger.LogInformation("Test cleanup completed");
        }

        /// <summary>
        /// Registers services in the dependency injection container for end-to-end testing.
        /// </summary>
        /// <param name="services">The service collection to add the services to.</param>
        protected virtual void RegisterServices(IServiceCollection services)
        {
            // Add logging services
            services.AddSingleton(LoggerFactory);
            services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));

            // Add test authentication using TestAuthHandler
            services.AddTestAuthentication();

            // Configure the ApiService with the mock server URL
            // services.Configure<ApiServiceOptions>(options =>  //TODO: Implement ApiServiceOptions
            // {
            //     options.BaseUrl = ApiServer.BaseUrl;
            // });

            // Register the database connection from the TestDatabaseFixture
            services.AddSingleton(Database.Connection);

            // Register all service implementations needed for testing
            services.AddSingleton<IAuthenticationService, MockAuthService>();
            services.AddSingleton<ITimeTrackingService, MockTimeTrackingService>();
            services.AddSingleton<ILocationService, MockLocationService>();
            services.AddSingleton<IPatrolService, MockPatrolService>();
            services.AddSingleton<IPhotoService, MockPhotoService>();
            services.AddSingleton<IReportService, MockReportService>();
            services.AddSingleton<ISyncService, MockSyncService>();

            // Register any additional services required by the tests
        }

        /// <summary>
        /// Initializes service references from the service provider for use in tests.
        /// </summary>
        protected virtual async Task InitializeServicesAsync()
        {
            AuthenticationService = ServiceProvider.GetService<IAuthenticationService>();
            TimeTrackingService = ServiceProvider.GetService<ITimeTrackingService>();
            LocationService = ServiceProvider.GetService<ILocationService>();
            PatrolService = ServiceProvider.GetService<IPatrolService>();
            PhotoService = ServiceProvider.GetService<IPhotoService>();
            ReportService = ServiceProvider.GetService<IReportService>();
            SyncService = ServiceProvider.GetService<ISyncService>();

            Logger.LogInformation("Services initialized");

            await Task.CompletedTask;
        }

        /// <summary>
        /// Sets up common API responses for end-to-end tests.
        /// </summary>
        protected virtual async Task SetupApiResponsesAsync()
        {
            SetupAuthenticationSuccessResponse();
            SetupTimeTrackingSuccessResponse();
            SetupLocationTrackingSuccessResponse();
            SetupPatrolSuccessResponse();
            SetupPhotoSuccessResponse();
            SetupReportSuccessResponse();

            Logger.LogInformation("API responses setup");

            await Task.CompletedTask;
        }

        /// <summary>
        /// Sets up successful authentication API responses.
        /// </summary>
        protected virtual void SetupAuthenticationSuccessResponse()
        {
            // Create authentication verification response with verification ID
            var authVerifyResponse = new
            {
                VerificationId = Guid.NewGuid().ToString()
            };

            // Create authentication validation response with token and expiry
            var authValidateResponse = new
            {
                Token = TestConstants.TestAuthToken,
                ExpiresAt = DateTime.UtcNow.AddHours(8).ToString("o")
            };

            // Create authentication refresh response with new token and expiry
            var authRefreshResponse = new
            {
                Token = "new_auth_token",
                ExpiresAt = DateTime.UtcNow.AddHours(8).ToString("o")
            };

            // Call ApiServer.SetupSuccessResponse for /auth/verify endpoint
            ApiServer.SetupSuccessResponse("/auth/verify", authVerifyResponse);

            // Call ApiServer.SetupSuccessResponse for /auth/validate endpoint
            ApiServer.SetupSuccessResponse("/auth/validate", authValidateResponse);

            // Call ApiServer.SetupSuccessResponse for /auth/refresh endpoint
            ApiServer.SetupSuccessResponse("/auth/refresh", authRefreshResponse);
        }

        /// <summary>
        /// Sets up successful time tracking API responses.
        /// </summary>
        protected virtual void SetupTimeTrackingSuccessResponse()
        {
            // Create time record response with success status
            var timeRecordResponse = new
            {
                Id = Guid.NewGuid().ToString(),
                Status = "success"
            };

            // Create time history response with sample time records
            var timeHistoryResponse = new[]
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
            };

            // Call ApiServer.SetupSuccessResponse for /time/clock endpoint
            ApiServer.SetupSuccessResponse("/time/clock", timeRecordResponse);

            // Call ApiServer.SetupSuccessResponse for /time/history endpoint
            ApiServer.SetupSuccessResponse("/time/history", timeHistoryResponse);
        }

        /// <summary>
        /// Sets up successful location tracking API responses.
        /// </summary>
        protected virtual void SetupLocationTrackingSuccessResponse()
        {
            // Create location sync response with success status
            var locationSyncResponse = new
            {
                Processed = 10,
                Failed = 0
            };

            // Create current location response with sample location
            var currentLocationResponse = new
            {
                Timestamp = DateTime.UtcNow.ToString("o"),
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude
            };

            // Call ApiServer.SetupSuccessResponse for /location/batch endpoint
            ApiServer.SetupSuccessResponse("/location/batch", locationSyncResponse);

            // Call ApiServer.SetupSuccessResponse for /location/current endpoint
            ApiServer.SetupSuccessResponse("/location/current", currentLocationResponse);
        }

        /// <summary>
        /// Sets up successful patrol API responses.
        /// </summary>
        protected virtual void SetupPatrolSuccessResponse()
        {
            // Create patrol locations response with sample locations
            var patrolLocationsResponse = new[]
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
            };

            // Create patrol checkpoints response with sample checkpoints
            var patrolCheckpointsResponse = new[]
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
            };

            // Create patrol verification response with success status
            var patrolVerifyResponse = new
            {
                Status = "success"
            };

            // Call ApiServer.SetupSuccessResponse for /patrol/locations endpoint
            ApiServer.SetupSuccessResponse("/patrol/locations", patrolLocationsResponse);

            // Call ApiServer.SetupSuccessResponse for /patrol/checkpoints endpoint
            ApiServer.SetupSuccessResponse("/patrol/checkpoints", patrolCheckpointsResponse);

            // Call ApiServer.SetupSuccessResponse for /patrol/verify endpoint
            ApiServer.SetupSuccessResponse("/patrol/verify", patrolVerifyResponse);
        }

        /// <summary>
        /// Sets up successful photo API responses.
        /// </summary>
        protected virtual void SetupPhotoSuccessResponse()
        {
            // Create photo upload response with success status
            var photoUploadResponse = new
            {
                Id = Guid.NewGuid().ToString(),
                Status = "success"
            };

            // Create photos list response with sample photos
            var photosListResponse = new[]
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
            };

            // Call ApiServer.SetupSuccessResponse for /photos/upload endpoint
            ApiServer.SetupSuccessResponse("/photos/upload", photoUploadResponse);

            // Call ApiServer.SetupSuccessResponse for /photos endpoint
            ApiServer.SetupSuccessResponse("/photos", photosListResponse);
        }

        /// <summary>
        /// Sets up successful report API responses.
        /// </summary>
        protected virtual void SetupReportSuccessResponse()
        {
            // Create report response with success status
            var reportResponse = new
            {
                Id = Guid.NewGuid().ToString(),
                Status = "success"
            };

            // Create reports list response with sample reports
            var reportsListResponse = new[]
            {
                new
                {
                    Id = Guid.NewGuid().ToString(),
                    Text = "Test report 1",
                    Timestamp = DateTime.UtcNow.ToString("o")
                },
                new
                {
                    Id = Guid.NewGuid().ToString(),
                    Text = "Test report 2",
                    Timestamp = DateTime.UtcNow.ToString("o")
                }
            };

            // Call ApiServer.SetupSuccessResponse for /reports POST endpoint
            ApiServer.SetupSuccessResponse("/reports", reportResponse);

            // Call ApiServer.SetupSuccessResponse for /reports GET endpoint
            ApiServer.SetupSuccessResponse("/reports", reportsListResponse);
        }

        /// <summary>
        /// Sets up error API responses for testing error handling.
        /// </summary>
        /// <param name="endpoint">The API endpoint to configure</param>
        /// <param name="statusCode">The HTTP status code to return</param>
        /// <param name="errorMessage">The error message to return</param>
        protected virtual void SetupApiErrorResponse(string endpoint, int statusCode, string errorMessage)
        {
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
            bool result = requestSuccess && verifySuccess;

            Logger.LogInformation("Authentication result: {Result}", result);
            return result;
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
            TimeRecordModel record = await TimeTrackingService.ClockIn();

            // Return true if operation succeeds, otherwise false
            bool result = record != null;

            Logger.LogInformation("Clock in result: {Result}", result);
            return result;
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
            TimeRecordModel record = await TimeTrackingService.ClockOut();

            // Return true if operation succeeds, otherwise false
            bool result = record != null;

            Logger.LogInformation("Clock out result: {Result}", result);
            return result;
        }

        /// <summary>
        /// Performs checkpoint verification for patrol testing.
        /// </summary>
        /// <param name="checkpointId">The checkpoint identifier</param>
        /// <returns>A task that returns true if verification was successful</returns>
        protected virtual async Task<bool> VerifyCheckpointAsync(int checkpointId)
        {
            // Setup patrol success responses if not already set
            SetupPatrolSuccessResponse();

            // Call PatrolService.VerifyCheckpoint(checkpointId)
            bool result = await PatrolService.VerifyCheckpoint(checkpointId);

            Logger.LogInformation("Verification result: {Result}", result);
            return result;
        }

        /// <summary>
        /// Captures a photo for photo testing.
        /// </summary>
        /// <returns>A task that returns true if photo capture was successful</returns>
        protected virtual async Task<bool> CapturePhotoAsync()
        {
            // Setup photo success responses if not already set
            SetupPhotoSuccessResponse();

            // Call PhotoService.CapturePhoto()
            PhotoModel photo = await PhotoService.CapturePhotoAsync();

            // Return true if operation succeeds, otherwise false
            bool result = photo != null;

            Logger.LogInformation("Photo capture result: {Result}", result);
            return result;
        }

        /// <summary>
        /// Creates an activity report for report testing.
        /// </summary>
        /// <param name="reportText">The report text</param>
        /// <returns>A task that returns true if report creation was successful</returns>
        protected virtual async Task<bool> CreateReportAsync(string reportText)
        {
            // Setup report success responses if not already set
            SetupReportSuccessResponse();

            // Call ReportService.CreateReport(reportText)
            ReportModel report = await ReportService.CreateReportAsync(reportText, TestConstants.TestLatitude, TestConstants.TestLongitude);

            // Return true if operation succeeds, otherwise false
            bool result = report != null;

            Logger.LogInformation("Report creation result: {Result}", result);
            return result;
        }

        /// <summary>
        /// Synchronizes all pending data with the backend.
        /// </summary>
        /// <returns>A task that returns true if synchronization was successful</returns>
        protected virtual async Task<bool> SyncDataAsync()
        {
            // Call SyncService.SyncAll()
            SyncResult syncResult = await SyncService.SyncAll();

            // Return true if operation succeeds, otherwise false
            bool result = syncResult.SuccessCount > 0;

            Logger.LogInformation("Synchronization result: {Result}", result);
            return result;
        }

        /// <summary>
        /// Executes a complete patrol flow from authentication to checkpoint verification.
        /// </summary>
        /// <returns>A task that returns true if the complete flow was successful</returns>
        protected virtual async Task<bool> ExecuteCompletePatrolFlowAsync()
        {
            // Call AuthenticateAsync() to authenticate the user
            bool authSuccess = await AuthenticateAsync();
            if (!authSuccess) return false;

            // Call ClockInAsync() to start a shift
            bool clockInSuccess = await ClockInAsync();
            if (!clockInSuccess) return false;

            // Get patrol locations using PatrolService.GetLocations()
            var locations = await PatrolService.GetLocations();
            if (locations == null || !locations.Any()) return false;

            // Get checkpoints for the first location using PatrolService.GetCheckpoints()
            var checkpoints = await PatrolService.GetCheckpoints(locations.First().Id);
            if (checkpoints == null || !checkpoints.Any()) return false;

            // Verify each checkpoint using VerifyCheckpointAsync()
            foreach (var checkpoint in checkpoints)
            {
                bool verifySuccess = await VerifyCheckpointAsync(checkpoint.Id);
                if (!verifySuccess) return false;
            }

            // Call ClockOutAsync() to end the shift
            bool clockOutSuccess = await ClockOutAsync();
            if (!clockOutSuccess) return false;

            // Call SyncDataAsync() to synchronize all data
            bool syncSuccess = await SyncDataAsync();
            if (!syncSuccess) return false;

            // Return true if all operations succeed, otherwise false
            Logger.LogInformation("Complete flow result: Success");
            return true;
        }
    }
}