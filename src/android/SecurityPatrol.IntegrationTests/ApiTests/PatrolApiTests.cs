using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using SecurityPatrol.IntegrationTests.Helpers;
using SecurityPatrol.Services;
using SecurityPatrol.Models;
using SecurityPatrol.Constants;

namespace SecurityPatrol.IntegrationTests.ApiTests
{
    public class PatrolApiTests : IDisposable
    {
        private readonly MockApiServer _mockApiServer;
        private readonly TestDatabaseInitializer _databaseInitializer;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PatrolApiTests> _logger;

        public PatrolApiTests()
        {
            // Initialize logger using LoggerFactory
            var loggerFactory = LoggerFactory.Create(builder => 
            {
                builder.AddConsole();
                builder.AddDebug();
            });
            _logger = loggerFactory.CreateLogger<PatrolApiTests>();
            
            // Initialize _databaseInitializer with new TestDatabaseInitializer
            _databaseInitializer = new TestDatabaseInitializer(loggerFactory.CreateLogger<TestDatabaseInitializer>());
            
            // Initialize _mockApiServer with new MockApiServer
            _mockApiServer = new MockApiServer(loggerFactory.CreateLogger<MockApiServer>());
            
            // Set up service provider with dependency injection
            _serviceProvider = SetupServices();
            
            // Start the mock API server
            _mockApiServer.Start();
        }

        public void Dispose()
        {
            // Stop the mock API server
            _mockApiServer.Stop();
            // Dispose the mock API server
            _mockApiServer.Dispose();
        }

        private IServiceProvider SetupServices()
        {
            // Create a new ServiceCollection
            var services = new ServiceCollection();
            
            // Add logging services
            services.AddLogging();
            
            // Add mock API server as singleton
            services.AddSingleton(_mockApiServer);
            
            // Add test database initializer as singleton
            services.AddSingleton(_databaseInitializer);
            
            // Add ILocationService mock as singleton
            var locationServiceMock = new Mock<ILocationService>();
            locationServiceMock.Setup(m => m.GetCurrentLocation())
                .ReturnsAsync(new LocationModel { Latitude = 37.7749, Longitude = -122.4194 });
            locationServiceMock.Setup(m => m.IsTracking).Returns(true);
            services.AddSingleton(locationServiceMock.Object);
            
            // Add IGeofenceService mock as singleton
            var geofenceServiceMock = new Mock<IGeofenceService>();
            geofenceServiceMock.Setup(m => m.StartMonitoring(It.IsAny<IEnumerable<CheckpointModel>>()))
                .Returns(Task.CompletedTask);
            geofenceServiceMock.Setup(m => m.StopMonitoring())
                .Returns(Task.CompletedTask);
            geofenceServiceMock.Setup(m => m.CheckProximity(It.IsAny<double>(), It.IsAny<double>()))
                .ReturnsAsync(new List<int> { 1, 2, 3 });
            services.AddSingleton(geofenceServiceMock.Object);
            
            // Add IMapService mock as singleton
            var mapServiceMock = new Mock<IMapService>();
            mapServiceMock.Setup(m => m.DisplayCheckpoints(It.IsAny<IEnumerable<CheckpointModel>>()))
                .Returns(Task.CompletedTask);
            mapServiceMock.Setup(m => m.ClearCheckpoints());
            mapServiceMock.Setup(m => m.HighlightCheckpoint(It.IsAny<int>(), It.IsAny<bool>()));
            mapServiceMock.Setup(m => m.UpdateCheckpointStatus(It.IsAny<int>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);
            services.AddSingleton(mapServiceMock.Object);
            
            // Add ICheckpointRepository mock as singleton
            var checkpointRepoMock = new Mock<ICheckpointRepository>();
            checkpointRepoMock.Setup(m => m.GetAllCheckpointsAsync())
                .ReturnsAsync(new List<CheckpointModel>());
            checkpointRepoMock.Setup(m => m.GetCheckpointsByLocationIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int locationId) => new List<CheckpointModel>
                {
                    new CheckpointModel { Id = 1, LocationId = locationId, Name = "Checkpoint 1", Latitude = 37.7749, Longitude = -122.4194 },
                    new CheckpointModel { Id = 2, LocationId = locationId, Name = "Checkpoint 2", Latitude = 37.7748, Longitude = -122.4195 }
                });
            checkpointRepoMock.Setup(m => m.SaveCheckpointStatusAsync(It.IsAny<CheckpointStatus>()))
                .ReturnsAsync(true);
            checkpointRepoMock.Setup(m => m.ClearCheckpointStatusesAsync(It.IsAny<int>()))
                .ReturnsAsync(0);
            checkpointRepoMock.Setup(m => m.GetCheckpointStatusesAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<CheckpointStatus>());
            services.AddSingleton(checkpointRepoMock.Object);
            
            // Add ApiService with mock server URL as transient
            services.AddTransient<IApiService>(sp => 
            {
                var httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri(_mockApiServer.GetBaseUrl());
                
                var tokenManagerMock = new Mock<ITokenManager>();
                tokenManagerMock.Setup(m => m.IsTokenValid()).ReturnsAsync(true);
                tokenManagerMock.Setup(m => m.RetrieveToken()).ReturnsAsync("mock_token");
                
                var networkServiceMock = new Mock<INetworkService>();
                networkServiceMock.Setup(m => m.IsConnected).Returns(true);
                networkServiceMock.Setup(m => m.ShouldAttemptOperation(It.IsAny<NetworkOperationType>())).Returns(true);
                
                var telemetryServiceMock = new Mock<ITelemetryService>();
                
                return new ApiService(
                    httpClient,
                    tokenManagerMock.Object,
                    networkServiceMock.Object,
                    telemetryServiceMock.Object);
            });
            
            // Add PatrolService as transient
            services.AddTransient<IPatrolService, PatrolService>();
            
            // Build and return the service provider
            return services.BuildServiceProvider();
        }

        private async Task InitializeTestAsync()
        {
            // Reset the database using _databaseInitializer.ResetDatabaseAsync()
            await _databaseInitializer.ResetDatabaseAsync();
            // Reset the mock API server mappings
            _mockApiServer.ResetMappings();
        }

        [Fact]
        public async Task Test_GetLocations_Success()
        {
            // Initialize test environment
            await InitializeTestAsync();
            
            // Create a list of sample location models
            var sampleLocations = new List<LocationModel>
            {
                new LocationModel { Id = 1, Name = "North Building", Latitude = 37.7749, Longitude = -122.4194 },
                new LocationModel { Id = 2, Name = "South Building", Latitude = 37.7639, Longitude = -122.4089 }
            };
            
            // Set up mock API server to return success for patrol/locations endpoint with sample locations
            _mockApiServer.SetupSuccessResponse(ApiEndpoints.PatrolLocations, sampleLocations);
            
            // Get PatrolService from service provider
            var patrolService = _serviceProvider.GetRequiredService<IPatrolService>();
            
            // Call GetLocations method
            var result = await patrolService.GetLocations();
            
            // Assert that the result is not null
            Assert.NotNull(result);
            // Assert that the result contains the expected number of locations
            Assert.Equal(2, ((List<LocationModel>)result).Count);
            // Assert that the API was called exactly once
            Assert.Equal(1, _mockApiServer.GetRequestCount(ApiEndpoints.PatrolLocations));
            
            // Verify the properties of returned locations match the sample data
            var locationList = new List<LocationModel>(result);
            Assert.Equal(sampleLocations[0].Id, locationList[0].Id);
            Assert.Equal(sampleLocations[0].Name, locationList[0].Name);
            Assert.Equal(sampleLocations[0].Latitude, locationList[0].Latitude);
            Assert.Equal(sampleLocations[0].Longitude, locationList[0].Longitude);
            
            Assert.Equal(sampleLocations[1].Id, locationList[1].Id);
            Assert.Equal(sampleLocations[1].Name, locationList[1].Name);
            Assert.Equal(sampleLocations[1].Latitude, locationList[1].Latitude);
            Assert.Equal(sampleLocations[1].Longitude, locationList[1].Longitude);
        }

        [Fact]
        public async Task Test_GetLocations_Failure()
        {
            // Initialize test environment
            await InitializeTestAsync();
            
            // Set up mock API server to return error for patrol/locations endpoint
            _mockApiServer.SetupErrorResponse(ApiEndpoints.PatrolLocations, 500, "Server error");
            
            // Get PatrolService from service provider
            var patrolService = _serviceProvider.GetRequiredService<IPatrolService>();
            
            // Call GetLocations method and expect exception
            await Assert.ThrowsAsync<InvalidOperationException>(() => patrolService.GetLocations());
            
            // Assert that the API was called exactly once
            Assert.Equal(1, _mockApiServer.GetRequestCount(ApiEndpoints.PatrolLocations));
        }

        [Fact]
        public async Task Test_GetCheckpoints_Success()
        {
            // Initialize test environment
            await InitializeTestAsync();
            
            // Create a list of sample checkpoint models for a specific location ID
            int locationId = 1;
            var sampleCheckpoints = new List<CheckpointModel>
            {
                new CheckpointModel { Id = 1, LocationId = locationId, Name = "Front Entrance", Latitude = 37.7749, Longitude = -122.4194 },
                new CheckpointModel { Id = 2, LocationId = locationId, Name = "Back Entrance", Latitude = 37.7746, Longitude = -122.4191 },
                new CheckpointModel { Id = 3, LocationId = locationId, Name = "Parking Lot", Latitude = 37.7752, Longitude = -122.4198 }
            };
            
            // Set up mock API server to return success for patrol/checkpoints endpoint with sample checkpoints
            string endpoint = $"{ApiEndpoints.PatrolCheckpoints}?locationId={locationId}";
            _mockApiServer.SetupSuccessResponse(endpoint, sampleCheckpoints);
            
            // Get PatrolService from service provider
            var patrolService = _serviceProvider.GetRequiredService<IPatrolService>();
            
            // Call GetCheckpoints method with location ID
            var result = await patrolService.GetCheckpoints(locationId);
            
            // Assert that the result is not null
            Assert.NotNull(result);
            // Assert that the result contains the expected number of checkpoints
            Assert.Equal(3, ((List<CheckpointModel>)result).Count);
            // Assert that the API was called exactly once
            Assert.Equal(1, _mockApiServer.GetRequestCount(endpoint));
            
            // Verify the properties of returned checkpoints match the sample data
            var checkpointList = new List<CheckpointModel>(result);
            for (int i = 0; i < checkpointList.Count; i++)
            {
                Assert.Equal(sampleCheckpoints[i].Id, checkpointList[i].Id);
                Assert.Equal(sampleCheckpoints[i].LocationId, checkpointList[i].LocationId);
                Assert.Equal(sampleCheckpoints[i].Name, checkpointList[i].Name);
                Assert.Equal(sampleCheckpoints[i].Latitude, checkpointList[i].Latitude);
                Assert.Equal(sampleCheckpoints[i].Longitude, checkpointList[i].Longitude);
            }
            
            // Verify that all checkpoints have the correct location ID
            foreach (var checkpoint in result)
            {
                Assert.Equal(locationId, checkpoint.LocationId);
            }
        }

        [Fact]
        public async Task Test_GetCheckpoints_Failure()
        {
            // Initialize test environment
            await InitializeTestAsync();
            
            // Set up mock API server to return error for patrol/checkpoints endpoint
            int locationId = 1;
            string endpoint = $"{ApiEndpoints.PatrolCheckpoints}?locationId={locationId}";
            _mockApiServer.SetupErrorResponse(endpoint, 500, "Server error");
            
            // Get PatrolService from service provider
            var patrolService = _serviceProvider.GetRequiredService<IPatrolService>();
            
            // Call GetCheckpoints method with location ID and expect exception
            await Assert.ThrowsAsync<InvalidOperationException>(() => patrolService.GetCheckpoints(locationId));
            
            // Assert that the API was called exactly once
            Assert.Equal(1, _mockApiServer.GetRequestCount(endpoint));
        }

        [Fact]
        public async Task Test_GetCheckpoints_InvalidLocationId()
        {
            // Initialize test environment
            await InitializeTestAsync();
            
            // Get PatrolService from service provider
            var patrolService = _serviceProvider.GetRequiredService<IPatrolService>();
            
            // Call GetCheckpoints method with invalid location ID (0 or negative)
            await Assert.ThrowsAsync<ArgumentException>(() => patrolService.GetCheckpoints(0));
            
            // Assert that the API was not called
            Assert.Equal(0, _mockApiServer.GetRequestCount(ApiEndpoints.PatrolCheckpoints));
        }

        [Fact]
        public async Task Test_VerifyCheckpoint_Success()
        {
            // Initialize test environment
            await InitializeTestAsync();
            
            // Set up mock API server to return success for patrol/verify endpoint
            _mockApiServer.SetupSuccessResponse(ApiEndpoints.PatrolVerify, new { status = "success" });
            
            // Set up mock location service to return a location near the checkpoint
            var locationServiceMock = new Mock<ILocationService>();
            locationServiceMock.Setup(m => m.GetCurrentLocation())
                .ReturnsAsync(new LocationModel { Latitude = 37.7749, Longitude = -122.4194 });
            
            // Set up mock geofence service to indicate the checkpoint is within proximity
            var geofenceServiceMock = new Mock<IGeofenceService>();
            geofenceServiceMock.Setup(m => m.CheckProximity(It.IsAny<double>(), It.IsAny<double>()))
                .ReturnsAsync(new List<int> { 1 });
            
            // Get PatrolService from service provider
            var patrolService = _serviceProvider.GetRequiredService<IPatrolService>();
            
            // Set up active patrol with sample checkpoints
            int locationId = 1;
            var sampleCheckpoints = new List<CheckpointModel>
            {
                new CheckpointModel { Id = 1, LocationId = locationId, Name = "Front Entrance", Latitude = 37.7749, Longitude = -122.4194 }
            };
            
            _mockApiServer.SetupSuccessResponse($"{ApiEndpoints.PatrolCheckpoints}?locationId={locationId}", sampleCheckpoints);
            await patrolService.StartPatrol(locationId);
            
            // Call VerifyCheckpoint method with checkpoint ID
            int checkpointId = 1;
            var result = await patrolService.VerifyCheckpoint(checkpointId);
            
            // Assert that the result is true
            Assert.True(result);
            
            // Assert that the API was called exactly once
            Assert.Equal(1, _mockApiServer.GetRequestCount(ApiEndpoints.PatrolVerify));
            
            // Verify the request body contains the correct checkpoint ID
            var requestBody = _mockApiServer.GetLastRequestBody(ApiEndpoints.PatrolVerify);
            Assert.Contains($"\"checkpointId\":{checkpointId}", requestBody);
        }

        [Fact]
        public async Task Test_VerifyCheckpoint_Failure()
        {
            // Initialize test environment
            await InitializeTestAsync();
            
            // Set up mock API server to return error for patrol/verify endpoint
            _mockApiServer.SetupErrorResponse(ApiEndpoints.PatrolVerify, 500, "Server error");
            
            // Set up mock location service to return a location near the checkpoint
            var locationServiceMock = new Mock<ILocationService>();
            locationServiceMock.Setup(m => m.GetCurrentLocation())
                .ReturnsAsync(new LocationModel { Latitude = 37.7749, Longitude = -122.4194 });
            
            // Set up mock geofence service to indicate the checkpoint is within proximity
            var geofenceServiceMock = new Mock<IGeofenceService>();
            geofenceServiceMock.Setup(m => m.CheckProximity(It.IsAny<double>(), It.IsAny<double>()))
                .ReturnsAsync(new List<int> { 1 });
            
            // Get PatrolService from service provider
            var patrolService = _serviceProvider.GetRequiredService<IPatrolService>();
            
            // Set up active patrol with sample checkpoints
            int locationId = 1;
            var sampleCheckpoints = new List<CheckpointModel>
            {
                new CheckpointModel { Id = 1, LocationId = locationId, Name = "Front Entrance", Latitude = 37.7749, Longitude = -122.4194 }
            };
            
            _mockApiServer.SetupSuccessResponse($"{ApiEndpoints.PatrolCheckpoints}?locationId={locationId}", sampleCheckpoints);
            await patrolService.StartPatrol(locationId);
            
            // Call VerifyCheckpoint method with checkpoint ID
            int checkpointId = 1;
            var result = await patrolService.VerifyCheckpoint(checkpointId);
            
            // Assert that the result is false
            Assert.False(result);
            
            // Assert that the API was called exactly once
            Assert.Equal(1, _mockApiServer.GetRequestCount(ApiEndpoints.PatrolVerify));
        }

        [Fact]
        public async Task Test_VerifyCheckpoint_NotInProximity()
        {
            // Initialize test environment
            await InitializeTestAsync();
            
            // Set up mock location service to return a location far from the checkpoint
            var locationServiceMock = new Mock<ILocationService>();
            locationServiceMock.Setup(m => m.GetCurrentLocation())
                .ReturnsAsync(new LocationModel { Latitude = 38.8897, Longitude = -77.0088 }); // Different city
            
            // Set up mock geofence service to indicate the checkpoint is not within proximity
            var geofenceServiceMock = new Mock<IGeofenceService>();
            geofenceServiceMock.Setup(m => m.CheckProximity(It.IsAny<double>(), It.IsAny<double>()))
                .ReturnsAsync(new List<int>()); // Empty list = no checkpoints in proximity
            
            // Get PatrolService from service provider
            var patrolService = _serviceProvider.GetRequiredService<IPatrolService>();
            
            // Set up active patrol with sample checkpoints
            int locationId = 1;
            var sampleCheckpoints = new List<CheckpointModel>
            {
                new CheckpointModel { Id = 1, LocationId = locationId, Name = "Front Entrance", Latitude = 37.7749, Longitude = -122.4194 }
            };
            
            _mockApiServer.SetupSuccessResponse($"{ApiEndpoints.PatrolCheckpoints}?locationId={locationId}", sampleCheckpoints);
            await patrolService.StartPatrol(locationId);
            
            // Call VerifyCheckpoint method with checkpoint ID
            int checkpointId = 1;
            var result = await patrolService.VerifyCheckpoint(checkpointId);
            
            // Assert that the result is false
            Assert.False(result);
            
            // Assert that the API was not called
            Assert.Equal(0, _mockApiServer.GetRequestCount(ApiEndpoints.PatrolVerify));
        }

        [Fact]
        public async Task Test_VerifyCheckpoint_NoActivePatrol()
        {
            // Initialize test environment
            await InitializeTestAsync();
            
            // Get PatrolService from service provider
            var patrolService = _serviceProvider.GetRequiredService<IPatrolService>();
            
            // Call VerifyCheckpoint method with checkpoint ID
            int checkpointId = 1;
            var result = await patrolService.VerifyCheckpoint(checkpointId);
            
            // Assert that the result is false
            Assert.False(result);
            
            // Assert that the API was not called
            Assert.Equal(0, _mockApiServer.GetRequestCount(ApiEndpoints.PatrolVerify));
        }

        [Fact]
        public async Task Test_VerifyCheckpoint_InvalidCheckpointId()
        {
            // Initialize test environment
            await InitializeTestAsync();
            
            // Get PatrolService from service provider
            var patrolService = _serviceProvider.GetRequiredService<IPatrolService>();
            
            // Set up active patrol with sample checkpoints
            int locationId = 1;
            var sampleCheckpoints = new List<CheckpointModel>
            {
                new CheckpointModel { Id = 1, LocationId = locationId, Name = "Front Entrance", Latitude = 37.7749, Longitude = -122.4194 }
            };
            
            _mockApiServer.SetupSuccessResponse($"{ApiEndpoints.PatrolCheckpoints}?locationId={locationId}", sampleCheckpoints);
            await patrolService.StartPatrol(locationId);
            
            // Call VerifyCheckpoint method with invalid checkpoint ID (0 or negative)
            await Assert.ThrowsAsync<ArgumentException>(() => patrolService.VerifyCheckpoint(0));
            
            // Assert that the API was not called
            Assert.Equal(0, _mockApiServer.GetRequestCount(ApiEndpoints.PatrolVerify));
        }

        [Fact]
        public async Task Test_StartPatrol_Success()
        {
            // Initialize test environment
            await InitializeTestAsync();
            
            // Create a list of sample checkpoint models for a specific location ID
            int locationId = 1;
            var sampleCheckpoints = new List<CheckpointModel>
            {
                new CheckpointModel { Id = 1, LocationId = locationId, Name = "Front Entrance", Latitude = 37.7749, Longitude = -122.4194 },
                new CheckpointModel { Id = 2, LocationId = locationId, Name = "Back Entrance", Latitude = 37.7746, Longitude = -122.4191 },
                new CheckpointModel { Id = 3, LocationId = locationId, Name = "Parking Lot", Latitude = 37.7752, Longitude = -122.4198 }
            };
            
            // Set up mock API server to return success for patrol/checkpoints endpoint with sample checkpoints
            string endpoint = $"{ApiEndpoints.PatrolCheckpoints}?locationId={locationId}";
            _mockApiServer.SetupSuccessResponse(endpoint, sampleCheckpoints);
            
            // Get PatrolService from service provider
            var patrolService = _serviceProvider.GetRequiredService<IPatrolService>();
            
            // Call StartPatrol method with location ID
            var result = await patrolService.StartPatrol(locationId);
            
            // Assert that the result is not null
            Assert.NotNull(result);
            // Assert that the result.LocationId matches the provided location ID
            Assert.Equal(locationId, result.LocationId);
            // Assert that the result.TotalCheckpoints matches the number of sample checkpoints
            Assert.Equal(sampleCheckpoints.Count, result.TotalCheckpoints);
            // Assert that the result.VerifiedCheckpoints is 0
            Assert.Equal(0, result.VerifiedCheckpoints);
            // Assert that the API was called exactly once
            Assert.Equal(1, _mockApiServer.GetRequestCount(endpoint));
            
            // Verify that the patrol service's IsPatrolActive property is true
            Assert.True(patrolService.IsPatrolActive);
            // Verify that the patrol service's CurrentLocationId property matches the provided location ID
            Assert.Equal(locationId, patrolService.CurrentLocationId);
        }

        [Fact]
        public async Task Test_StartPatrol_NoCheckpoints()
        {
            // Initialize test environment
            await InitializeTestAsync();
            
            // Set up mock API server to return empty array for patrol/checkpoints endpoint
            int locationId = 1;
            string endpoint = $"{ApiEndpoints.PatrolCheckpoints}?locationId={locationId}";
            _mockApiServer.SetupSuccessResponse(endpoint, new List<CheckpointModel>());
            
            // Get PatrolService from service provider
            var patrolService = _serviceProvider.GetRequiredService<IPatrolService>();
            
            // Call StartPatrol method with location ID and expect exception
            await Assert.ThrowsAsync<InvalidOperationException>(() => patrolService.StartPatrol(locationId));
            
            // Assert that the API was called exactly once
            Assert.Equal(1, _mockApiServer.GetRequestCount(endpoint));
            
            // Verify that the patrol service's IsPatrolActive property is false
            Assert.False(patrolService.IsPatrolActive);
        }

        [Fact]
        public async Task Test_EndPatrol_Success()
        {
            // Initialize test environment
            await InitializeTestAsync();
            
            // Create a list of sample checkpoint models for a specific location ID
            int locationId = 1;
            var sampleCheckpoints = new List<CheckpointModel>
            {
                new CheckpointModel { Id = 1, LocationId = locationId, Name = "Front Entrance", Latitude = 37.7749, Longitude = -122.4194 },
                new CheckpointModel { Id = 2, LocationId = locationId, Name = "Back Entrance", Latitude = 37.7746, Longitude = -122.4191 }
            };
            
            // Set up mock API server to return success for patrol/checkpoints endpoint with sample checkpoints
            string endpoint = $"{ApiEndpoints.PatrolCheckpoints}?locationId={locationId}";
            _mockApiServer.SetupSuccessResponse(endpoint, sampleCheckpoints);
            
            // Get PatrolService from service provider
            var patrolService = _serviceProvider.GetRequiredService<IPatrolService>();
            
            // Call StartPatrol method with location ID
            await patrolService.StartPatrol(locationId);
            
            // Verify that the patrol service's IsPatrolActive property is true
            Assert.True(patrolService.IsPatrolActive);
            
            // Call EndPatrol method
            var result = await patrolService.EndPatrol();
            
            // Assert that the result is not null
            Assert.NotNull(result);
            // Assert that the result.LocationId matches the provided location ID
            Assert.Equal(locationId, result.LocationId);
            // Assert that the result.EndTime is not null
            Assert.NotNull(result.EndTime);
            
            // Verify that the patrol service's IsPatrolActive property is false
            Assert.False(patrolService.IsPatrolActive);
            // Verify that the patrol service's CurrentLocationId property is null
            Assert.Null(patrolService.CurrentLocationId);
        }

        [Fact]
        public async Task Test_EndPatrol_NoActivePatrol()
        {
            // Initialize test environment
            await InitializeTestAsync();
            
            // Get PatrolService from service provider
            var patrolService = _serviceProvider.GetRequiredService<IPatrolService>();
            
            // Call EndPatrol method
            var result = await patrolService.EndPatrol();
            
            // Assert that the result is null
            Assert.Null(result);
        }

        [Fact]
        public async Task Test_GetPatrolStatus_ActivePatrol()
        {
            // Initialize test environment
            await InitializeTestAsync();
            
            // Create a list of sample checkpoint models for a specific location ID
            int locationId = 1;
            var sampleCheckpoints = new List<CheckpointModel>
            {
                new CheckpointModel { Id = 1, LocationId = locationId, Name = "Front Entrance", Latitude = 37.7749, Longitude = -122.4194 },
                new CheckpointModel { Id = 2, LocationId = locationId, Name = "Back Entrance", Latitude = 37.7746, Longitude = -122.4191 }
            };
            
            // Set up mock API server to return success for patrol/checkpoints endpoint with sample checkpoints
            string endpoint = $"{ApiEndpoints.PatrolCheckpoints}?locationId={locationId}";
            _mockApiServer.SetupSuccessResponse(endpoint, sampleCheckpoints);
            
            // Get PatrolService from service provider
            var patrolService = _serviceProvider.GetRequiredService<IPatrolService>();
            
            // Call StartPatrol method with location ID
            await patrolService.StartPatrol(locationId);
            
            // Call GetPatrolStatus method with the same location ID
            var result = await patrolService.GetPatrolStatus(locationId);
            
            // Assert that the result is not null
            Assert.NotNull(result);
            // Assert that the result.LocationId matches the provided location ID
            Assert.Equal(locationId, result.LocationId);
            // Assert that the result.TotalCheckpoints matches the number of sample checkpoints
            Assert.Equal(sampleCheckpoints.Count, result.TotalCheckpoints);
            // Assert that the result.VerifiedCheckpoints is 0
            Assert.Equal(0, result.VerifiedCheckpoints);
        }

        [Fact]
        public async Task Test_GetPatrolStatus_NoActivePatrol()
        {
            // Initialize test environment
            await InitializeTestAsync();
            
            // Create a list of sample checkpoint models for a specific location ID
            int locationId = 1;
            var sampleCheckpoints = new List<CheckpointModel>
            {
                new CheckpointModel { Id = 1, LocationId = locationId, Name = "Front Entrance", Latitude = 37.7749, Longitude = -122.4194 },
                new CheckpointModel { Id = 2, LocationId = locationId, Name = "Back Entrance", Latitude = 37.7746, Longitude = -122.4191 }
            };
            
            // Set up mock API server to return success for patrol/checkpoints endpoint with sample checkpoints
            string endpoint = $"{ApiEndpoints.PatrolCheckpoints}?locationId={locationId}";
            _mockApiServer.SetupSuccessResponse(endpoint, sampleCheckpoints);
            
            // Get PatrolService from service provider
            var patrolService = _serviceProvider.GetRequiredService<IPatrolService>();
            
            // Call GetPatrolStatus method with location ID
            var result = await patrolService.GetPatrolStatus(locationId);
            
            // Assert that the result is not null
            Assert.NotNull(result);
            // Assert that the result.LocationId matches the provided location ID
            Assert.Equal(locationId, result.LocationId);
            // Assert that the result.TotalCheckpoints matches the number of sample checkpoints
            Assert.Equal(sampleCheckpoints.Count, result.TotalCheckpoints);
            // Assert that the result.VerifiedCheckpoints is 0
            Assert.Equal(0, result.VerifiedCheckpoints);
            // Assert that the API was called exactly once
            Assert.Equal(1, _mockApiServer.GetRequestCount(endpoint));
        }

        [Fact]
        public async Task Test_CompletePatrol_AllCheckpointsVerified()
        {
            // Initialize test environment
            await InitializeTestAsync();
            
            // Create a list of sample checkpoint models for a specific location ID
            int locationId = 1;
            var sampleCheckpoints = new List<CheckpointModel>
            {
                new CheckpointModel { Id = 1, LocationId = locationId, Name = "Front Entrance", Latitude = 37.7749, Longitude = -122.4194 },
                new CheckpointModel { Id = 2, LocationId = locationId, Name = "Back Entrance", Latitude = 37.7746, Longitude = -122.4191 }
            };
            
            // Set up mock API server to return success for patrol/checkpoints endpoint with sample checkpoints
            string endpoint = $"{ApiEndpoints.PatrolCheckpoints}?locationId={locationId}";
            _mockApiServer.SetupSuccessResponse(endpoint, sampleCheckpoints);
            
            // Set up mock API server to return success for patrol/verify endpoint
            _mockApiServer.SetupSuccessResponse(ApiEndpoints.PatrolVerify, new { status = "success" });
            
            // Set up mock location service to return a location near the checkpoints
            var locationServiceMock = new Mock<ILocationService>();
            locationServiceMock.Setup(m => m.GetCurrentLocation())
                .ReturnsAsync(new LocationModel { Latitude = 37.7749, Longitude = -122.4194 });
            
            // Set up mock geofence service to indicate checkpoints are within proximity
            var geofenceServiceMock = new Mock<IGeofenceService>();
            geofenceServiceMock.Setup(m => m.CheckProximity(It.IsAny<double>(), It.IsAny<double>()))
                .ReturnsAsync(new List<int> { 1, 2 });
            
            // Get PatrolService from service provider
            var patrolService = _serviceProvider.GetRequiredService<IPatrolService>();
            
            // Call StartPatrol method with location ID
            await patrolService.StartPatrol(locationId);
            
            // Verify that the patrol service's IsPatrolActive property is true
            Assert.True(patrolService.IsPatrolActive);
            
            // For each checkpoint in the sample list, call VerifyCheckpoint method
            foreach (var checkpoint in sampleCheckpoints)
            {
                await patrolService.VerifyCheckpoint(checkpoint.Id);
            }
            
            // After verifying all checkpoints, verify that the patrol service's IsPatrolActive property is false
            Assert.False(patrolService.IsPatrolActive);
            
            // Verify that the patrol status shows all checkpoints verified
            var status = await patrolService.GetPatrolStatus(locationId);
            Assert.Equal(sampleCheckpoints.Count, status.VerifiedCheckpoints);
            Assert.Equal(sampleCheckpoints.Count, status.TotalCheckpoints);
            Assert.True(status.IsComplete());
        }
    }
}