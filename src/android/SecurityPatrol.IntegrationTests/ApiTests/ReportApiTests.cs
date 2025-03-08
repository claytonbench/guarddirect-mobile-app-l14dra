using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Moq;
using SecurityPatrol.IntegrationTests.Helpers;
using SecurityPatrol.Services;
using SecurityPatrol.Models;
using SecurityPatrol.Constants;

namespace SecurityPatrol.IntegrationTests.ApiTests
{
    /// <summary>
    /// Integration tests for the report API functionality in the Security Patrol application.
    /// </summary>
    public class ReportApiTests : IDisposable
    {
        private readonly MockApiServer _mockApiServer;
        private readonly TestDatabaseInitializer _databaseInitializer;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ReportApiTests> _logger;

        /// <summary>
        /// Initializes a new instance of the ReportApiTests class and sets up the test environment.
        /// </summary>
        public ReportApiTests()
        {
            // Initialize logger
            var loggerFactory = LoggerFactory.Create(builder => 
            {
                builder
                    .AddConsole()
                    .SetMinimumLevel(LogLevel.Debug);
            });
            _logger = loggerFactory.CreateLogger<ReportApiTests>();

            // Initialize test database initializer
            _databaseInitializer = new TestDatabaseInitializer(loggerFactory.CreateLogger<TestDatabaseInitializer>());

            // Initialize mock API server
            _mockApiServer = new MockApiServer(loggerFactory.CreateLogger<MockApiServer>());
            _mockApiServer.Start();

            // Set up service provider with required services
            _serviceProvider = SetupServices();
        }

        /// <summary>
        /// Cleans up resources used by the tests.
        /// </summary>
        public void Dispose()
        {
            _mockApiServer.Stop();
            _mockApiServer.Dispose();
        }

        /// <summary>
        /// Sets up the service provider with required services for testing.
        /// </summary>
        /// <returns>The configured service provider</returns>
        private IServiceProvider SetupServices()
        {
            var services = new ServiceCollection();

            // Add logging
            services.AddLogging();

            // Add mock API server
            services.AddSingleton(_mockApiServer);
            services.AddSingleton(_databaseInitializer);

            // Mock authentication state provider
            var authStateProviderMock = new Mock<IAuthenticationStateProvider>();
            authStateProviderMock.Setup(p => p.GetCurrentState()).ReturnsAsync(new AuthState(true, "+15551234567"));
            services.AddSingleton(authStateProviderMock.Object);

            // Mock network service - default to connected
            var networkServiceMock = new Mock<INetworkService>();
            networkServiceMock.Setup(n => n.IsConnected).Returns(true);
            networkServiceMock.Setup(n => n.ShouldAttemptOperation(It.IsAny<NetworkOperationType>())).Returns(true);
            services.AddSingleton(networkServiceMock.Object);

            // Mock token manager
            var tokenManagerMock = new Mock<ITokenManager>();
            tokenManagerMock.Setup(t => t.IsTokenValid()).ReturnsAsync(true);
            tokenManagerMock.Setup(t => t.RetrieveToken()).ReturnsAsync("mock_token");
            services.AddSingleton(tokenManagerMock.Object);

            // Mock telemetry service
            var telemetryServiceMock = new Mock<ITelemetryService>();
            services.AddSingleton(telemetryServiceMock.Object);

            // Configure HttpClient with mock server URL
            services.AddSingleton<HttpClient>(new HttpClient { 
                BaseAddress = new Uri(_mockApiServer.GetBaseUrl()) 
            });
            
            // Add API service
            services.AddTransient<IApiService, ApiService>();

            // Initialize the database for testing
            _databaseInitializer.InitializeAsync().Wait();

            // Add database connection
            services.AddTransient(async sp => await _databaseInitializer.GetConnectionAsync());

            // Add report services using the real implementations
            services.AddTransient<IReportRepository, ReportRepository>();
            services.AddTransient<IReportSyncService, ReportSyncService>();
            services.AddTransient<IReportService, ReportService>();

            return services.BuildServiceProvider();
        }

        /// <summary>
        /// Initializes the test environment before each test.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        private async Task InitializeTestAsync()
        {
            await _databaseInitializer.ResetDatabaseAsync();
            _mockApiServer.ResetMappings();
        }

        [Fact]
        public async Task Test_CreateReport_Success()
        {
            // Arrange
            await InitializeTestAsync();
            var reportService = _serviceProvider.GetRequiredService<IReportService>();

            // Act
            var report = await reportService.CreateReportAsync("Test report text", 37.7749, -122.4194);

            // Assert
            Assert.NotNull(report);
            Assert.Equal("Test report text", report.Text);
            Assert.False(report.IsSynced);
        }

        [Fact]
        public async Task Test_SyncReport_Success()
        {
            // Arrange
            await InitializeTestAsync();
            _mockApiServer.SetupSuccessResponse(
                ExtractPathFromUrl(ApiEndpoints.Reports),
                new ReportResponse { Id = "report123", Status = "success" }
            );
            var reportService = _serviceProvider.GetRequiredService<IReportService>();

            // Act
            var report = await reportService.CreateReportAsync("Test report text", 37.7749, -122.4194);
            var syncResult = await reportService.SyncReportAsync(report.Id);

            // Assert
            Assert.True(syncResult);
            Assert.Equal(1, _mockApiServer.GetRequestCount(ExtractPathFromUrl(ApiEndpoints.Reports)));
            
            var updatedReport = await reportService.GetReportAsync(report.Id);
            Assert.True(updatedReport.IsSynced);
            Assert.Equal("report123", updatedReport.RemoteId);
        }

        [Fact]
        public async Task Test_SyncReport_Failure()
        {
            // Arrange
            await InitializeTestAsync();
            _mockApiServer.SetupErrorResponse(
                ExtractPathFromUrl(ApiEndpoints.Reports),
                500,
                "Internal Server Error"
            );
            var reportService = _serviceProvider.GetRequiredService<IReportService>();

            // Act
            var report = await reportService.CreateReportAsync("Test report text", 37.7749, -122.4194);
            var syncResult = await reportService.SyncReportAsync(report.Id);

            // Assert
            Assert.False(syncResult);
            Assert.Equal(1, _mockApiServer.GetRequestCount(ExtractPathFromUrl(ApiEndpoints.Reports)));
            
            var updatedReport = await reportService.GetReportAsync(report.Id);
            Assert.False(updatedReport.IsSynced);
            Assert.Null(updatedReport.RemoteId);
        }

        [Fact]
        public async Task Test_CreateAndSyncReport_ValidatesRequestFormat()
        {
            // Arrange
            await InitializeTestAsync();
            _mockApiServer.SetupSuccessResponse(
                ExtractPathFromUrl(ApiEndpoints.Reports),
                new ReportResponse { Id = "report123", Status = "success" }
            );
            var reportService = _serviceProvider.GetRequiredService<IReportService>();

            // Act
            var report = await reportService.CreateReportAsync("Test report text", 37.7749, -122.4194);
            await reportService.SyncReportAsync(report.Id);
            
            // Assert
            var requestBody = _mockApiServer.GetLastRequestBody(ExtractPathFromUrl(ApiEndpoints.Reports));
            var request = JsonSerializer.Deserialize<ReportRequest>(requestBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            Assert.Equal("Test report text", request.Text);
            Assert.Equal(37.7749, request.Location.Latitude);
            Assert.Equal(-122.4194, request.Location.Longitude);
        }

        [Fact]
        public async Task Test_SyncReport_UpdatesLocalModel()
        {
            // Arrange
            await InitializeTestAsync();
            _mockApiServer.SetupSuccessResponse(
                ExtractPathFromUrl(ApiEndpoints.Reports),
                new ReportResponse { Id = "report456", Status = "success" }
            );
            var reportService = _serviceProvider.GetRequiredService<IReportService>();

            // Act
            var report = await reportService.CreateReportAsync("Test report text", 37.7749, -122.4194);
            await reportService.SyncReportAsync(report.Id);
            
            // Assert
            var updatedReport = await reportService.GetReportAsync(report.Id);
            Assert.True(updatedReport.IsSynced);
            Assert.Equal("report456", updatedReport.RemoteId);
        }

        [Fact]
        public async Task Test_SyncReport_HandlesNetworkFailure()
        {
            // Arrange
            await InitializeTestAsync();
            
            // Mock network service to report no connectivity
            var networkServiceMock = new Mock<INetworkService>();
            networkServiceMock.Setup(n => n.IsConnected).Returns(false);
            
            // Create a new service collection for this specific test
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton(_mockApiServer);
            services.AddSingleton(_databaseInitializer);
            services.AddSingleton(networkServiceMock.Object); // Use offline network service
            
            // Add other required services from the main provider
            services.AddSingleton(_serviceProvider.GetService<IAuthenticationStateProvider>());
            services.AddSingleton(_serviceProvider.GetService<ITokenManager>());
            services.AddSingleton(_serviceProvider.GetService<ITelemetryService>());
            services.AddSingleton(_serviceProvider.GetService<HttpClient>());
            
            // Add API service and report services
            services.AddTransient<IApiService, ApiService>();
            services.AddTransient(async sp => await _databaseInitializer.GetConnectionAsync());
            services.AddTransient<IReportRepository, ReportRepository>();
            services.AddTransient<IReportSyncService, ReportSyncService>();
            services.AddTransient<IReportService, ReportService>();
            
            var localProvider = services.BuildServiceProvider();
            var reportService = localProvider.GetRequiredService<IReportService>();

            // Act
            var report = await reportService.CreateReportAsync("Test report text", 37.7749, -122.4194);
            var syncResult = await reportService.SyncReportAsync(report.Id);
            
            // Assert
            Assert.False(syncResult);
            Assert.Equal(0, _mockApiServer.GetRequestCount(ExtractPathFromUrl(ApiEndpoints.Reports)));
            
            var updatedReport = await reportService.GetReportAsync(report.Id);
            Assert.False(updatedReport.IsSynced);
        }

        [Fact]
        public async Task Test_CreateReport_WithInvalidText_ThrowsException()
        {
            // Arrange
            await InitializeTestAsync();
            var reportService = _serviceProvider.GetRequiredService<IReportService>();
            
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                reportService.CreateReportAsync("", 37.7749, -122.4194)
            );
            
            await Assert.ThrowsAsync<ArgumentException>(() => 
                reportService.CreateReportAsync(null, 37.7749, -122.4194)
            );
            
            // Create a string longer than the maximum allowed length
            string tooLongText = new string('x', AppConstants.ReportMaxLength + 1);
            
            await Assert.ThrowsAsync<ArgumentException>(() => 
                reportService.CreateReportAsync(tooLongText, 37.7749, -122.4194)
            );
        }

        /// <summary>
        /// Extracts the path and query from a full URL.
        /// </summary>
        /// <param name="url">The full URL.</param>
        /// <returns>The path and query part of the URL.</returns>
        private string ExtractPathFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return string.Empty;
            }

            try
            {
                var uri = new Uri(url);
                return uri.PathAndQuery;
            }
            catch (UriFormatException)
            {
                _logger.LogWarning("Invalid URL format: {Url}", url);
                return url; // Return the original string if it's not a valid URI
            }
        }
    }
}