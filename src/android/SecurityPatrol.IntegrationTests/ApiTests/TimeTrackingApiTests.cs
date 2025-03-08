using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Moq;
using SecurityPatrol.IntegrationTests.Helpers;
using SecurityPatrol.Services;
using SecurityPatrol.Models;
using SecurityPatrol.Constants;

namespace SecurityPatrol.IntegrationTests.ApiTests
{
    /// <summary>
    /// Integration tests for the time tracking API functionality in the Security Patrol application
    /// </summary>
    public class TimeTrackingApiTests : IDisposable
    {
        private readonly MockApiServer _mockApiServer;
        private readonly TestAuthenticationHandler _authHandler;
        private readonly TestDatabaseInitializer _dbInitializer;
        private readonly ITimeTrackingService _timeTrackingService;
        private readonly ITimeTrackingSyncService _syncService;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the TimeTrackingApiTests class with test dependencies
        /// </summary>
        public TimeTrackingApiTests()
        {
            // Create a mock logger factory for testing
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            
            // Create and start the mock API server
            _mockApiServer = new MockApiServer(loggerFactory.CreateLogger<MockApiServer>());
            _mockApiServer.Start();

            // Set up a service collection for dependency injection
            var services = new ServiceCollection();
            
            // Register logging
            services.AddLogging(builder => builder.AddConsole());
            
            // Register the mock API server
            services.AddSingleton(_mockApiServer);
            
            // Register authentication services
            services.AddSingleton<IAuthenticationStateProvider>(provider => {
                var mock = new Mock<IAuthenticationStateProvider>();
                mock.Setup(p => p.GetCurrentState()).ReturnsAsync(AuthState.CreateAuthenticated("+15551234567"));
                mock.Setup(p => p.IsAuthenticated()).ReturnsAsync(true);
                return mock.Object;
            });
            
            services.AddSingleton<TestAuthenticationHandler>();
            services.AddSingleton<IAuthenticationService>(provider => 
                provider.GetRequiredService<TestAuthenticationHandler>());
            
            // Register database initializer
            services.AddSingleton<TestDatabaseInitializer>();
            services.AddSingleton<IDatabaseInitializer>(provider => 
                provider.GetRequiredService<TestDatabaseInitializer>());
            
            // Register location service mock
            services.AddSingleton<ILocationService>(provider => {
                var mock = new Mock<ILocationService>();
                mock.Setup(s => s.GetCurrentLocation()).ReturnsAsync(new LocationModel {
                    Latitude = 37.7749,
                    Longitude = -122.4194,
                    Accuracy = 10.0,
                    Timestamp = DateTime.UtcNow
                });
                mock.Setup(s => s.StartTracking()).Returns(Task.CompletedTask);
                mock.Setup(s => s.StopTracking()).Returns(Task.CompletedTask);
                return mock.Object;
            });
            
            // Register time record repository mock
            services.AddSingleton<ITimeRecordRepository>(provider => {
                var mock = new Mock<ITimeRecordRepository>();
                var records = new List<TimeRecordModel>();
                
                mock.Setup(r => r.SaveTimeRecordAsync(It.IsAny<TimeRecordModel>()))
                    .ReturnsAsync((TimeRecordModel record) => {
                        if (record.Id == 0)
                            record.Id = records.Count + 1;
                        
                        var existing = records.FirstOrDefault(r => r.Id == record.Id);
                        if (existing != null)
                            records.Remove(existing);
                            
                        records.Add(record);
                        return record.Id;
                    });
                    
                mock.Setup(r => r.GetTimeRecordsAsync(It.IsAny<int>()))
                    .ReturnsAsync((int count) => records.OrderByDescending(r => r.Timestamp).Take(count).ToList());
                    
                mock.Setup(r => r.GetTimeRecordByIdAsync(It.IsAny<int>()))
                    .ReturnsAsync((int id) => records.FirstOrDefault(r => r.Id == id));
                    
                mock.Setup(r => r.GetLatestClockInEventAsync())
                    .ReturnsAsync(() => records.Where(r => r.IsClockIn()).OrderByDescending(r => r.Timestamp).FirstOrDefault());
                    
                mock.Setup(r => r.GetLatestClockOutEventAsync())
                    .ReturnsAsync(() => records.Where(r => r.IsClockOut()).OrderByDescending(r => r.Timestamp).FirstOrDefault());
                    
                mock.Setup(r => r.UpdateSyncStatusAsync(It.IsAny<int>(), It.IsAny<bool>()))
                    .ReturnsAsync((int id, bool isSynced) => {
                        var record = records.FirstOrDefault(r => r.Id == id);
                        if (record != null) {
                            record.IsSynced = isSynced;
                            return 1;
                        }
                        return 0;
                    });
                    
                mock.Setup(r => r.UpdateRemoteIdAsync(It.IsAny<int>(), It.IsAny<string>()))
                    .ReturnsAsync((int id, string remoteId) => {
                        var record = records.FirstOrDefault(r => r.Id == id);
                        if (record != null) {
                            record.RemoteId = remoteId;
                            return 1;
                        }
                        return 0;
                    });
                    
                mock.Setup(r => r.GetPendingRecordsAsync())
                    .ReturnsAsync(() => records.Where(r => !r.IsSynced).ToList());
                    
                return mock.Object;
            });
            
            // Register required services for TimeTrackingSyncService
            services.AddSingleton<INetworkService>(provider => {
                var mock = new Mock<INetworkService>();
                mock.Setup(s => s.IsConnected).Returns(true);
                mock.Setup(s => s.ShouldAttemptOperation(It.IsAny<NetworkOperationType>())).Returns(true);
                return mock.Object;
            });
            
            services.AddSingleton<ITelemetryService>(provider => {
                var mock = new Mock<ITelemetryService>();
                // Implement Log method to prevent null reference exceptions
                mock.Setup(t => t.Log(It.IsAny<LogLevel>(), It.IsAny<string>(), It.IsAny<Exception>()));
                return mock.Object;
            });
            
            // Register API service
            services.AddSingleton<IApiService>(provider => {
                var mock = new Mock<IApiService>();
                // The actual API calls will be handled by MockApiServer
                return mock.Object;
            });
            
            // Register the services we're testing
            services.AddSingleton<ITimeTrackingService, TimeTrackingService>();
            services.AddSingleton<ITimeTrackingSyncService, TimeTrackingSyncService>();
            
            // Build the service provider
            _serviceProvider = services.BuildServiceProvider();
            
            // Resolve the required services
            _authHandler = _serviceProvider.GetRequiredService<TestAuthenticationHandler>();
            _dbInitializer = _serviceProvider.GetRequiredService<TestDatabaseInitializer>();
            _timeTrackingService = _serviceProvider.GetRequiredService<ITimeTrackingService>();
            _syncService = _serviceProvider.GetRequiredService<ITimeTrackingSyncService>();
            
            // Initialize the test database
            _dbInitializer.InitializeAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Cleans up resources used by the tests
        /// </summary>
        public void Dispose()
        {
            _mockApiServer.Stop();
            // Dispose any other resources as needed
        }

        /// <summary>
        /// Sets up a test user for authentication in tests
        /// </summary>
        private async Task SetupTestUser()
        {
            // Set up a test user for authentication
            await _authHandler.RequestVerificationCode("+15551234567");
            await _authHandler.VerifyCode("123456"); // Default code in TestAuthenticationHandler
        }

        /// <summary>
        /// Tests that ClockIn sends the correct request to the API and returns a valid time record
        /// </summary>
        [Fact]
        public async Task ClockIn_SendsCorrectRequest_ReturnsTimeRecord()
        {
            // Arrange
            await SetupTestUser();
            
            // Set up a success response for the time clock endpoint
            var responseObj = new TimeRecordResponse { 
                Id = Guid.NewGuid().ToString(),
                Status = "success" 
            };
            _mockApiServer.SetupSuccessResponse(ApiEndpoints.TimeClock, responseObj);
            
            // Act
            var result = await _timeTrackingService.ClockIn();
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal("ClockIn", result.Type);
            
            // Verify API call was made
            var requestCount = _mockApiServer.GetRequestCount(ApiEndpoints.TimeClock);
            Assert.Equal(1, requestCount);
            
            // Verify request body
            var requestBody = _mockApiServer.GetLastRequestBody(ApiEndpoints.TimeClock);
            Assert.NotEmpty(requestBody);
            
            var request = JsonSerializer.Deserialize<TimeRecordRequest>(requestBody, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
            Assert.NotNull(request);
            Assert.Equal("ClockIn", request.Type);
            Assert.NotNull(request.Location);
            
            // Verify current status updated
            var status = await _timeTrackingService.GetCurrentStatus();
            Assert.True(status.IsClocked);
        }

        /// <summary>
        /// Tests that ClockOut sends the correct request to the API and returns a valid time record
        /// </summary>
        [Fact]
        public async Task ClockOut_SendsCorrectRequest_ReturnsTimeRecord()
        {
            // Arrange
            await SetupTestUser();
            
            // Clock in first to ensure the user is in the correct state
            var clockInResponseObj = new TimeRecordResponse { 
                Id = Guid.NewGuid().ToString(),
                Status = "success" 
            };
            _mockApiServer.SetupSuccessResponse(ApiEndpoints.TimeClock, clockInResponseObj);
            await _timeTrackingService.ClockIn();
            
            // Set up a success response for the clock out operation
            var clockOutResponseObj = new TimeRecordResponse { 
                Id = Guid.NewGuid().ToString(),
                Status = "success" 
            };
            _mockApiServer.SetupSuccessResponse(ApiEndpoints.TimeClock, clockOutResponseObj);
            
            // Act
            var result = await _timeTrackingService.ClockOut();
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal("ClockOut", result.Type);
            
            // Verify API call was made
            var requestCount = _mockApiServer.GetRequestCount(ApiEndpoints.TimeClock);
            Assert.Equal(2, requestCount); // One for clock in, one for clock out
            
            // Verify request body for the clock out operation
            var requestBody = _mockApiServer.GetLastRequestBody(ApiEndpoints.TimeClock);
            Assert.NotEmpty(requestBody);
            
            var request = JsonSerializer.Deserialize<TimeRecordRequest>(requestBody, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
            Assert.NotNull(request);
            Assert.Equal("ClockOut", request.Type);
            Assert.NotNull(request.Location);
            
            // Verify current status updated
            var status = await _timeTrackingService.GetCurrentStatus();
            Assert.False(status.IsClocked);
        }

        /// <summary>
        /// Tests that GetHistory sends the correct request to the API and returns time records
        /// </summary>
        [Fact]
        public async Task GetHistory_SendsCorrectRequest_ReturnsTimeRecords()
        {
            // Arrange
            await SetupTestUser();
            
            // Set up a success response for the time history endpoint
            var responseObj = new[] {
                new { 
                    id = Guid.NewGuid().ToString(),
                    type = "clockIn",
                    timestamp = DateTime.UtcNow.AddHours(-8).ToString("o"),
                    location = new { latitude = 37.7749, longitude = -122.4194 }
                },
                new { 
                    id = Guid.NewGuid().ToString(),
                    type = "clockOut",
                    timestamp = DateTime.UtcNow.AddHours(-7).ToString("o"),
                    location = new { latitude = 37.7749, longitude = -122.4194 }
                }
            };
            _mockApiServer.SetupSuccessResponse(ApiEndpoints.TimeHistory, responseObj);
            
            // Act
            var result = await _timeTrackingService.GetHistory(10);
            
            // Assert
            Assert.NotNull(result);
            var records = result.ToList();
            Assert.Equal(2, records.Count);
            
            // Verify API call was made
            var requestCount = _mockApiServer.GetRequestCount(ApiEndpoints.TimeHistory);
            Assert.Equal(1, requestCount);
        }

        /// <summary>
        /// Tests that ClockIn throws an exception when the API returns an error
        /// </summary>
        [Fact]
        public async Task ClockIn_ApiError_ThrowsException()
        {
            // Arrange
            await SetupTestUser();
            
            // Set up an error response for the time clock endpoint
            _mockApiServer.SetupErrorResponse(ApiEndpoints.TimeClock, 500, "Internal server error");
            
            // Act/Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _timeTrackingService.ClockIn());
            Assert.Contains("error", exception.Message, StringComparison.OrdinalIgnoreCase);
            
            // Verify that the current status still shows not clocked in
            var status = await _timeTrackingService.GetCurrentStatus();
            Assert.False(status.IsClocked);
        }

        /// <summary>
        /// Tests that ClockOut throws an exception when the API returns an error
        /// </summary>
        [Fact]
        public async Task ClockOut_ApiError_ThrowsException()
        {
            // Arrange
            await SetupTestUser();
            
            // Clock in first to ensure the user is in the correct state
            var clockInResponseObj = new TimeRecordResponse { 
                Id = Guid.NewGuid().ToString(),
                Status = "success" 
            };
            _mockApiServer.SetupSuccessResponse(ApiEndpoints.TimeClock, clockInResponseObj);
            await _timeTrackingService.ClockIn();
            
            // Set up an error response for the clock out operation
            _mockApiServer.SetupErrorResponse(ApiEndpoints.TimeClock, 500, "Internal server error");
            
            // Act/Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _timeTrackingService.ClockOut());
            Assert.Contains("error", exception.Message, StringComparison.OrdinalIgnoreCase);
            
            // Verify that the current status still shows clocked in
            var status = await _timeTrackingService.GetCurrentStatus();
            Assert.True(status.IsClocked);
        }

        /// <summary>
        /// Tests that SyncRecord sends the correct request to the API and updates the record's sync status
        /// </summary>
        [Fact]
        public async Task SyncRecord_SendsCorrectRequest_UpdatesRecordStatus()
        {
            // Arrange
            await SetupTestUser();
            
            // Create a time record that needs synchronization
            var timeRecord = new TimeRecordModel {
                Id = 1,
                UserId = "+15551234567",
                Type = "ClockIn",
                Timestamp = DateTime.UtcNow,
                Latitude = 37.7749,
                Longitude = -122.4194,
                IsSynced = false
            };
            
            // Save the record to the repository
            var repository = _serviceProvider.GetRequiredService<ITimeRecordRepository>();
            await repository.SaveTimeRecordAsync(timeRecord);
            
            // Set up a success response for the time clock endpoint
            var responseObj = new TimeRecordResponse { 
                Id = Guid.NewGuid().ToString(),
                Status = "success" 
            };
            _mockApiServer.SetupSuccessResponse(ApiEndpoints.TimeClock, responseObj);
            
            // Act
            var result = await _syncService.SyncRecordAsync(timeRecord);
            
            // Assert
            Assert.True(result);
            
            // Verify API call was made
            var requestCount = _mockApiServer.GetRequestCount(ApiEndpoints.TimeClock);
            Assert.Equal(1, requestCount);
            
            // Verify request body
            var requestBody = _mockApiServer.GetLastRequestBody(ApiEndpoints.TimeClock);
            Assert.NotEmpty(requestBody);
            
            // Verify the record's sync status was updated
            var updatedRecord = await repository.GetTimeRecordByIdAsync(1);
            Assert.True(updatedRecord.IsSynced);
            Assert.Equal(responseObj.Id, updatedRecord.RemoteId);
        }
    }
}