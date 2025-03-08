using System; // Version 8.0.0
using System.Threading.Tasks; // Version 8.0.0
using System.Collections.Generic; // Version 8.0.0
using Microsoft.Extensions.DependencyInjection; // Version 8.0.0
using Xunit; // Version 2.4.2
using Xunit.Abstractions; // Version 2.0.3
using FluentAssertions; // Version 6.11.0
using SecurityPatrol.MAUI.PerformanceTests.Setup;
using SecurityPatrol.TestCommon.Helpers;
using SecurityPatrol.Services;
using SecurityPatrol.Models;

namespace SecurityPatrol.MAUI.PerformanceTests
{
    /// <summary>
    /// Performance tests for the location tracking functionality in the Security Patrol application.
    /// </summary>
    public class LocationTrackingPerformanceTests : PerformanceTestBase
    {
        private ILocationService _locationService;
        private ILocationRepository _locationRepository;
        private ILocationSyncService _locationSyncService;
        private LocationSimulator _locationSimulator;

        private const double MaxStartupTimeMs = 1000;
        private const double MaxTrackingStartTimeMs = 500;
        private const double MaxLocationProcessingTimeMs = 50;
        private const double MaxBatchProcessingTimeMs = 200;
        private const double MaxSyncTimeMs = 2000;
        private const long MaxMemoryUsageMB = 50;

        private const int LocationBatchSize = 50;
        private const int SimulationDurationMs = 5000;

        private List<LocationModel> _testLocations;

        /// <summary>
        /// Initializes a new instance of the LocationTrackingPerformanceTests class with test output helper.
        /// </summary>
        /// <param name="outputHelper">The test output helper for writing test results.</param>
        public LocationTrackingPerformanceTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
            // Call base constructor with outputHelper
            // Initialize constants for performance thresholds
            // Set MaxStartupTimeMs to 1000 (1 second)
            // Set MaxTrackingStartTimeMs to 500 (0.5 seconds)
            // Set MaxLocationProcessingTimeMs to 50 (50 milliseconds)
            // Set MaxBatchProcessingTimeMs to 200 (200 milliseconds)
            // Set MaxSyncTimeMs to 2000 (2 seconds)
            // Set MaxMemoryUsageMB to 50 (50 MB)
            // Set LocationBatchSize to 50
            // Set SimulationDurationMs to 5000 (5 seconds)
        }

        /// <summary>
        /// Initializes the test environment for location tracking performance tests.
        /// </summary>
        [PublicAPI]
        public override async Task InitializeAsync()
        {
            // Await base.InitializeAsync() to initialize the test environment
            await base.InitializeAsync();

            // Register services for dependency injection
            ServiceProvider = new ServiceCollection()
                .AddSingleton(ApiServer.Server)
                .AddSingleton(Database.Connection)
                .AddTransient<ILocationService, LocationService>()
                .AddTransient<ILocationRepository, LocationRepository>()
                .AddTransient<ILocationSyncService, LocationSyncService>()
                .BuildServiceProvider();

            // Initialize _locationService from service provider
            _locationService = ServiceProvider.GetService<ILocationService>();

            // Initialize _locationRepository from service provider
            _locationRepository = ServiceProvider.GetService<ILocationRepository>();

            // Initialize _locationSyncService from service provider
            _locationSyncService = ServiceProvider.GetService<ILocationSyncService>();

            // Initialize _locationSimulator with default parameters
            _locationSimulator = new LocationSimulator();

            // Subscribe to _locationSimulator.LocationChanged event
            _locationSimulator.LocationChanged += OnLocationChanged;

            // Generate test location data for performance tests
            _testLocations = GenerateTestLocations(LocationBatchSize * 5);

            // Log successful initialization
            Logger.LogInformation("Location tracking performance tests initialized successfully");
        }

        /// <summary>
        /// Cleans up resources after tests are complete.
        /// </summary>
        [PublicAPI]
        public override async Task CleanupAsync()
        {
            // Unsubscribe from _locationSimulator.LocationChanged event
            _locationSimulator.LocationChanged -= OnLocationChanged;

            // Stop location simulation if running
            _locationSimulator.StopSimulation();

            // Stop location tracking if active
            if (_locationService.IsTracking)
            {
                await _locationService.StopTracking();
            }

            // Dispose _locationService if it implements IDisposable
            if (_locationService is IDisposable disposableLocationService)
            {
                disposableLocationService.Dispose();
            }

            // Clear test data
            _testLocations.Clear();

            // Await base.CleanupAsync() to clean up base resources
            await base.CleanupAsync();

            // Log successful cleanup
            Logger.LogInformation("Location tracking performance tests cleanup completed successfully");
        }

        /// <summary>
        /// Tests the startup performance of the location service.
        /// </summary>
        [PublicAPI]
        [Fact]
        public async Task TestLocationServiceStartupPerformance()
        {
            // Log test start
            Logger.LogInformation("Starting TestLocationServiceStartupPerformance");

            // Measure execution time of creating a new LocationService instance
            double startupTime = await MeasureExecutionTimeAsync(async () =>
            {
                var locationService = ServiceProvider.GetService<ILocationService>();
                Assert.NotNull(locationService);
            }, "LocationService Startup");

            // Assert that startup time is below MaxStartupTimeMs threshold
            AssertPerformanceThreshold(startupTime, MaxStartupTimeMs, "LocationService Startup Time");

            // Measure memory usage of creating a new LocationService instance
            long memoryUsage = await MeasureMemoryUsageAsync(async () =>
            {
                var locationService = ServiceProvider.GetService<ILocationService>();
                Assert.NotNull(locationService);
            }, "LocationService Startup Memory");

            // Assert that memory usage is below MaxMemoryUsageMB threshold
            AssertMemoryThreshold(memoryUsage, MaxMemoryUsageMB, "LocationService Startup Memory");

            // Log test completion
            Logger.LogInformation("TestLocationServiceStartupPerformance completed");
        }

        /// <summary>
        /// Tests the performance of starting location tracking.
        /// </summary>
        [PublicAPI]
        [Fact]
        public async Task TestLocationTrackingStartPerformance()
        {
            // Log test start
            Logger.LogInformation("Starting TestLocationTrackingStartPerformance");

            // Ensure location tracking is stopped
            if (_locationService.IsTracking)
            {
                await _locationService.StopTracking();
            }

            // Measure execution time of starting location tracking
            double trackingStartTime = await MeasureExecutionTimeAsync(async () =>
            {
                await _locationService.StartTracking();
            }, "LocationTracking Start");

            // Assert that tracking start time is below MaxTrackingStartTimeMs threshold
            AssertPerformanceThreshold(trackingStartTime, MaxTrackingStartTimeMs, "LocationTracking Start Time");

            // Measure memory usage of starting location tracking
            long trackingStartMemory = await MeasureMemoryUsageAsync(async () =>
            {
                await _locationService.StartTracking();
            }, "LocationTracking Start Memory");

            // Assert that memory usage is below MaxMemoryUsageMB threshold
            AssertMemoryThreshold(trackingStartMemory, MaxMemoryUsageMB, "LocationTracking Start Memory");

            // Stop location tracking
            await _locationService.StopTracking();

            // Log test completion
            Logger.LogInformation("TestLocationTrackingStartPerformance completed");
        }

        /// <summary>
        /// Tests the performance of processing individual location updates.
        /// </summary>
        [PublicAPI]
        [Fact]
        public async Task TestLocationProcessingPerformance()
        {
            // Log test start
            Logger.LogInformation("Starting TestLocationProcessingPerformance");

            // Start location tracking
            await _locationService.StartTracking();

            // Start location simulation
            _locationSimulator.StartSimulation();

            // Run benchmark for processing individual location updates
            (double averageProcessingTime, long averageMemoryUsage) = await RunBenchmarkAsync(async () =>
            {
                // Simulate location update by emitting a LocationChanged event
                _locationSimulator.EmitLocationChanged(_testLocations[0]);
                await Task.Delay(1);
            }, "Location Processing", MeasurementIterations);

            // Assert that average processing time is below MaxLocationProcessingTimeMs threshold
            AssertPerformanceThreshold(averageProcessingTime, MaxLocationProcessingTimeMs, "Location Processing Time");

            // Assert that average memory usage is below MaxMemoryUsageMB threshold
            AssertMemoryThreshold(averageMemoryUsage, MaxMemoryUsageMB, "Location Processing Memory");

            // Stop location simulation
            _locationSimulator.StopSimulation();

            // Stop location tracking
            await _locationService.StopTracking();

            // Log test completion
            Logger.LogInformation("TestLocationProcessingPerformance completed");
        }

        /// <summary>
        /// Tests the performance of processing batches of location updates.
        /// </summary>
        [PublicAPI]
        [Fact]
        public async Task TestLocationBatchProcessingPerformance()
        {
            // Log test start
            Logger.LogInformation("Starting TestLocationBatchProcessingPerformance");

            // Prepare a batch of test locations
            List<LocationModel> batchLocations = GenerateTestLocations(LocationBatchSize);

            // Measure execution time of saving location batch
            double batchProcessingTime = await MeasureExecutionTimeAsync(async () =>
            {
                await _locationRepository.SaveLocationBatchAsync(batchLocations);
            }, "Location Batch Processing");

            // Assert that batch processing time is below MaxBatchProcessingTimeMs threshold
            AssertPerformanceThreshold(batchProcessingTime, MaxBatchProcessingTimeMs, "Location Batch Processing Time");

            // Measure memory usage of saving location batch
            long batchProcessingMemory = await MeasureMemoryUsageAsync(async () =>
            {
                await _locationRepository.SaveLocationBatchAsync(batchLocations);
            }, "Location Batch Processing Memory");

            // Assert that memory usage is below MaxMemoryUsageMB threshold
            AssertMemoryThreshold(batchProcessingMemory, MaxMemoryUsageMB, "Location Batch Processing Memory");

            // Log test completion
            Logger.LogInformation("TestLocationBatchProcessingPerformance completed");
        }

        /// <summary>
        /// Tests the performance of synchronizing location data with the backend.
        /// </summary>
        [PublicAPI]
        [Fact]
        public async Task TestLocationSyncPerformance()
        {
            // Log test start
            Logger.LogInformation("Starting TestLocationSyncPerformance");

            // Prepare test data for synchronization
            List<LocationModel> syncLocations = GenerateTestLocations(LocationBatchSize);
            await _locationRepository.SaveLocationBatchAsync(syncLocations);

            // Measure execution time of synchronizing locations
            double syncTime = await MeasureExecutionTimeAsync(async () =>
            {
                await _locationSyncService.SyncLocationsAsync(LocationBatchSize);
            }, "Location Sync");

            // Assert that sync time is below MaxSyncTimeMs threshold
            AssertPerformanceThreshold(syncTime, MaxSyncTimeMs, "Location Sync Time");

            // Measure memory usage of synchronizing locations
            long syncMemory = await MeasureMemoryUsageAsync(async () =>
            {
                await _locationSyncService.SyncLocationsAsync(LocationBatchSize);
            }, "Location Sync Memory");

            // Assert that memory usage is below MaxMemoryUsageMB threshold
            AssertMemoryThreshold(syncMemory, MaxMemoryUsageMB, "Location Sync Memory");

            // Log test completion
            Logger.LogInformation("TestLocationSyncPerformance completed");
        }

        /// <summary>
        /// Tests the performance impact of battery optimization settings.
        /// </summary>
        [PublicAPI]
        [Fact]
        public async Task TestBatteryOptimizationImpact()
        {
            // Log test start
            Logger.LogInformation("Starting TestBatteryOptimizationImpact");

            // Measure performance with battery optimization enabled
            (double processingTimeOptimized, long memoryUsageOptimized) = await MeasureBatteryOptimizationPerformance(true);

            // Measure performance with battery optimization disabled
            (double processingTimeNotOptimized, long memoryUsageNotOptimized) = await MeasureBatteryOptimizationPerformance(false);

            // Compare and assert that battery optimization improves performance
            Assert.True(processingTimeOptimized < processingTimeNotOptimized, "Battery optimization should improve processing time");
            Assert.True(memoryUsageOptimized < memoryUsageNotOptimized, "Battery optimization should reduce memory usage");

            // Log test completion
            Logger.LogInformation("TestBatteryOptimizationImpact completed");
        }

        /// <summary>
        /// Tests location tracking performance under low-resource device conditions.
        /// </summary>
        [PublicAPI]
        [Fact]
        public async Task TestLowResourceDevicePerformance()
        {
            // Log test start
            Logger.LogInformation("Starting TestLowResourceDevicePerformance");

            // Simulate low-resource environment
            SimulateLowResourceEnvironment();

            // Run location tracking performance tests
            double startupTime = await MeasureExecutionTimeAsync(async () =>
            {
                var locationService = ServiceProvider.GetService<ILocationService>();
                Assert.NotNull(locationService);
            }, "LocationService Startup Time (Low Resource)");

            // Assert that performance is acceptable even under constrained resources
            AssertPerformanceThreshold(startupTime, MaxStartupTimeMs * 2, "LocationService Startup Time (Low Resource)");

            // Reset environment simulation
            SimulateHighEndEnvironment();

            // Log test completion
            Logger.LogInformation("TestLowResourceDevicePerformance completed");
        }

        /// <summary>
        /// Tests performance under high-volume location tracking conditions.
        /// </summary>
        [PublicAPI]
        [Fact]
        public async Task TestHighVolumeLocationTracking()
        {
            // Log test start
            Logger.LogInformation("Starting TestHighVolumeLocationTracking");

            // Configure high-frequency location updates
            _locationSimulator.UpdateIntervalMs = 100; // 100ms interval

            // Start location tracking
            await _locationService.StartTracking();

            // Simulate rapid movement with many location updates
            _locationSimulator.SimulateRandomMovement(10, 100); // 100 points within 10 meters

            // Measure performance metrics during high-volume tracking
            double averageProcessingTime = GetAverageExecutionTime("Location Processing");
            long averageMemoryUsage = GetAverageMemoryUsage("Location Processing");

            // Assert that performance remains within acceptable thresholds
            AssertPerformanceThreshold(averageProcessingTime, MaxLocationProcessingTimeMs * 2, "Location Processing Time (High Volume)");
            AssertMemoryThreshold(averageMemoryUsage, MaxMemoryUsageMB * 2, "Location Processing Memory (High Volume)");

            // Stop location tracking
            await _locationService.StopTracking();

            // Log test completion
            Logger.LogInformation("TestHighVolumeLocationTracking completed");
        }

        /// <summary>
        /// Tests for memory leaks during extended continuous location tracking.
        /// </summary>
        [PublicAPI]
        [Fact]
        public async Task TestContinuousTrackingMemoryLeak()
        {
            // Log test start
            Logger.LogInformation("Starting TestContinuousTrackingMemoryLeak");

            // Record initial memory usage
            long initialMemory = GetCurrentMemoryUsage();

            // Start location tracking
            await _locationService.StartTracking();

            // Run extended simulation (multiple cycles)
            int simulationCycles = 3;
            for (int i = 0; i < simulationCycles; i++)
            {
                // Simulate location updates
                _locationSimulator.SimulateRandomMovement(5, 50); // 50 points within 5 meters
                await Task.Delay(SimulationDurationMs);

                // Periodically measure memory usage
                long currentMemory = GetCurrentMemoryUsage();
                Logger.LogInformation("Memory usage after cycle {Cycle}: {MemoryMB} MB", i + 1, currentMemory / (1024 * 1024));
            }

            // Stop location tracking
            await _locationService.StopTracking();

            // Force garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Measure final memory usage
            long finalMemory = GetCurrentMemoryUsage();
            long memoryGrowth = finalMemory - initialMemory;

            // Assert that memory usage growth is within acceptable limits
            double maxAcceptableGrowthMB = 10; // 10 MB
            AssertMemoryThreshold(memoryGrowth, maxAcceptableGrowthMB, "Continuous Tracking Memory Growth");

            // Log test completion
            Logger.LogInformation("TestContinuousTrackingMemoryLeak completed");
        }

        /// <summary>
        /// Handles location changed events from the simulator.
        /// </summary>
        [PublicAPI]
        private void OnLocationChanged(object sender, LocationChangedEventArgs e)
        {
            // Process the location update for performance measurement
            // Record timing and memory metrics
        }

        /// <summary>
        /// Generates test location data for performance tests.
        /// </summary>
        /// <param name="count">The number of locations to generate.</param>
        /// <returns>A list of test location models</returns>
        [PublicAPI]
        private List<LocationModel> GenerateTestLocations(int count)
        {
            // Create a new list to hold test locations
            List<LocationModel> locations = new List<LocationModel>();

            // Define a starting point (latitude and longitude)
            double startLatitude = 34.0522;
            double startLongitude = -118.2437;

            // Generate 'count' number of locations with small variations
            for (int i = 0; i < count; i++)
            {
                double latitude = startLatitude + (i * 0.0001); // Small variation
                double longitude = startLongitude - (i * 0.0001); // Small variation

                locations.Add(new LocationModel
                {
                    Latitude = latitude,
                    Longitude = longitude,
                    Accuracy = 5.0,
                    Timestamp = DateTime.UtcNow
                });
            }

            // Return the list of test locations
            return locations;
        }

        /// <summary>
        /// Measures performance metrics with specific battery optimization settings.
        /// </summary>
        /// <param name="optimizationEnabled">Whether battery optimization is enabled.</param>
        /// <returns>Performance metrics tuple</returns>
        [PublicAPI]
        private async Task<(double ProcessingTime, long MemoryUsage)> MeasureBatteryOptimizationPerformance(bool optimizationEnabled)
        {
            // Configure location service with specified optimization setting
            await _locationService.SetBatteryOptimization(optimizationEnabled);

            // Start location tracking
            await _locationService.StartTracking();

            // Run standardized location simulation
            (double averageTime, long averageMemory) = await RunBenchmarkAsync(async () =>
            {
                // Simulate location update by emitting a LocationChanged event
                _locationSimulator.EmitLocationChanged(_testLocations[0]);
                await Task.Delay(1);
            }, $"Location Processing (Battery Optimized = {optimizationEnabled})", MeasurementIterations);

            // Stop location tracking
            await _locationService.StopTracking();

            // Return the measured performance metrics
            return (averageTime, averageMemory);
        }
    }
}