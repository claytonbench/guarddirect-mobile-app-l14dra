using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection; // Version 8.0.0
using Microsoft.Extensions.Logging; // Version 8.0.0
using SecurityPatrol.E2ETests.Setup;
using SecurityPatrol.Models;
using SecurityPatrol.Services;
using SecurityPatrol.TestCommon.Constants; // TestConstants
using SecurityPatrol.TestCommon.Fixtures; // ApiServerFixture
using SecurityPatrol.TestCommon.Mocks; // MockAuthService
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Mocks;\n\nnamespace SecurityPatrol.E2ETests.Setup\n{\n    /// <summary>\n    /// Static class responsible for initializing and cleaning up the global test environment for end-to-end tests.\n    /// </summary>\n    public static class TestEnvironmentSetup\n    {\n        private static ApiServerFixture _apiServer;\n        private static TestDatabaseFixture _database;\n        private static ILoggerFactory _loggerFactory;\n        private static ILogger<TestEnvironmentSetup> _logger;\n        private static bool _isInitialized = false;\n        private static object _initializationLock = new object();\n\n        /// <summary>\n        /// Gets a value indicating whether the test environment has been initialized.\n        /// </summary>\n        public static bool IsInitialized => _isInitialized;\n\n        /// <summary>\n        /// Gets the ApiServerFixture instance for the test environment.\n        /// </summary>\n        public static ApiServerFixture ApiServer => _apiServer;\n\n        /// <summary>\n        /// Gets the TestDatabaseFixture instance for the test environment.\n        /// </summary>\n        public static TestDatabaseFixture Database => _database;\n\n        /// <summary>\n        /// Static constructor that initializes the static fields
        /// </summary>
        static TestEnvironmentSetup()\n        {\n            _initializationLock = new object();\n            _isInitialized = false;\n\n            // Create a logger factory\n            _loggerFactory = LoggerFactory.Create(builder =>\n            {\n                builder\n                    .AddConsole()\n                    .SetMinimumLevel(LogLevel.Information);\n            });\n\n            // Create a logger for TestEnvironmentSetup\n            _logger = _loggerFactory.CreateLogger<TestEnvironmentSetup>();\n\n            _logger.LogInformation(\"TestEnvironmentSetup static constructor called.\");\n        }\n\n        /// <summary>\n        /// Initializes the global test environment if it hasn't been initialized already\n        /// </summary>\n        public static void InitializeEnvironment()\n        {\n            if (_isInitialized)\n            {\n                return;\n            }\n\n            lock (_initializationLock)\n            {\n                // Double-check locking to ensure only one thread initializes\n                if (_isInitialized)\n                {\n                    return;\n                }\n\n                _logger.LogInformation(\"Initializing test environment...\");\n\n                // Initialize API server\n                _apiServer = new ApiServerFixture();\n                _apiServer.InitializeAsync().GetAwaiter().GetResult();\n\n                // Initialize database\n                _database = new TestDatabaseFixture();\n                _database.InitializeAsync().GetAwaiter().GetResult();\n\n                // Seed the database with test data\n                _database.SeedTestDataAsync().GetAwaiter().GetResult();\n\n                // Set public properties to the initialized instances\n                // ApiServer = _apiServer; // Already set in the property declaration\n                // Database = _database;   // Already set in the property declaration\n\n                _isInitialized = true;\n\n                _logger.LogInformation(\"Test environment initialized successfully.\");\n            }\n        }\n\n        /// <summary>\n        /// Cleans up the global test environment resources\n        /// </summary>\n        public static void CleanupEnvironment()\n        {\n            if (!_isInitialized)\n            {\n                return;\n            }\n\n            lock (_initializationLock)\n            {\n                // Double-check locking to ensure only one thread cleans up\n                if (!_isInitialized)\n                {\n                    return;\n                }\n\n                _logger.LogInformation(\"Cleaning up test environment...\");\n\n                // Dispose API server\n                _apiServer.DisposeAsync().GetAwaiter().GetResult();\n                _apiServer = null;\n\n                // Reset database\n                _database.ResetDatabaseAsync().GetAwaiter().GetResult();\n                _database = null;\n\n                _isInitialized = false;\n\n                _logger.LogInformation(\"Test environment cleanup completed.\");\n            }\n        }\n\n        /// <summary>\n        /// Creates and configures a service collection with mock services for testing\n        /// </summary>\n        /// <returns>A configured service collection with mock services</returns>\n        public static IServiceCollection CreateServiceCollection()\n        {\n            // Create a new ServiceCollection\n            var services = new ServiceCollection();\n\n            // Add logging services using _loggerFactory\n            services.AddSingleton(_loggerFactory);\n            services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));\n\n            // Register mock services for testing:\n            services.AddSingleton<IAuthenticationService, MockAuthService>();\n            services.AddSingleton<ILocationService, MockLocationService>();\n            services.AddSingleton<ITimeTrackingService, MockTimeTrackingService>();\n            services.AddSingleton<IPatrolService, MockPatrolService>();\n            services.AddSingleton<IPhotoService, MockPhotoService>();\n            services.AddSingleton<IReportService, MockReportService>();\n            services.AddSingleton<ISyncService, MockSyncService>();\n            services.AddSingleton<INetworkService, MockNetworkService>();\n\n            // Configure API service with the mock server URL\n            // services.Configure<ApiServiceOptions>(options =>  //TODO: Implement ApiServiceOptions\n            // {\n            //     options.BaseUrl = ApiServer.BaseUrl;\n            // });\n\n            return services;\n        }\n\n        /// <summary>\n        /// Creates a service provider from the configured service collection\n        /// </summary>\n        /// <returns>A service provider with registered mock services</returns>\n        public static IServiceProvider GetServiceProvider()\n        {\n            InitializeEnvironment();\n            var services = CreateServiceCollection();\n            return services.BuildServiceProvider();\n        }\n\n        /// <summary>\n        /// Configures the mock network service connectivity state\n        /// </summary>\n        /// <param name=\"isConnected\"></param>\n        public static void ConfigureNetworkConnectivity(bool isConnected)\n        {\n            InitializeEnvironment();\n            var serviceProvider = GetServiceProvider();\n            var mockNetworkService = serviceProvider.GetService<MockNetworkService>();\n            mockNetworkService.IsConnected = isConnected;\n            _logger.LogInformation(\"MockNetworkService configured with IsConnected = {IsConnected}\", isConnected);\n        }\n\n        /// <summary>\n        /// Configures the mock authentication service to succeed or fail\n        /// </summary>\n        /// <param name=\"shouldSucceed\"></param>\n        public static void ConfigureAuthenticationSuccess(bool shouldSucceed)\n        {\n            InitializeEnvironment();\n            var serviceProvider = GetServiceProvider();\n            var mockAuthService = serviceProvider.GetService<MockAuthService>();\n            mockAuthService.ShouldSucceed = shouldSucceed;\n            _logger.LogInformation(\"MockAuthService configured with ShouldSucceed = {ShouldSucceed}\", shouldSucceed);\n        }\n    }\n}\n