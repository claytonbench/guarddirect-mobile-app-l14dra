using System; // Version 8.0.0
using System.Threading.Tasks; // Version 8.0.0
using System.Collections.Generic; // Version 8.0.0
using System.Linq; // Version 8.0.0
using Microsoft.Extensions.DependencyInjection; // Version 8.0.0
using Microsoft.Extensions.Logging; // Version 8.0.0
using Xunit; // Version 2.4.2
using Xunit.Abstractions; // Version 2.0.3
using FluentAssertions; // Version 6.11.0
using SecurityPatrol.MAUI.PerformanceTests.Setup;
using SecurityPatrol.TestCommon.Helpers;
using SecurityPatrol.Services;

namespace SecurityPatrol.MAUI.PerformanceTests
{
    /// <summary>
    /// Tests that measure and validate memory usage across different components of the Security Patrol application.
    /// </summary>
    public class MemoryUsageTests : PerformanceTestBase
    {
        private ILocationService _locationService;
        private IPhotoService _photoService;
        private ITimeTrackingService _timeTrackingService;
        private IPatrolService _patrolService;

        private const double AppStartupMemoryThresholdMB = 50;
        private const double LocationTrackingMemoryThresholdMB = 20;
        private const double PhotoCaptureMemoryThresholdMB = 30;
        private const double TimeTrackingMemoryThresholdMB = 10;
        private const double PatrolManagementMemoryThresholdMB = 25;
        private const double LowMemoryThresholdMB = 150;
        private const double MemoryLeakThresholdMB = 5;

        /// <summary>
        /// Initializes a new instance of the MemoryUsageTests class with the specified test output helper.
        /// </summary>
        /// <param name="outputHelper">The test output helper for writing test results.</param>
        public MemoryUsageTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
            // Call base constructor with outputHelper
            // Initialize memory threshold constants
            // Set AppStartupMemoryThresholdMB to 50MB
            // Set LocationTrackingMemoryThresholdMB to 20MB
            // Set PhotoCaptureMemoryThresholdMB to 30MB
            // Set TimeTrackingMemoryThresholdMB to 10MB
            // Set PatrolManagementMemoryThresholdMB to 25MB
            // Set LowMemoryThresholdMB to 150MB (total app memory)
            // Set MemoryLeakThresholdMB to 5MB (for leak detection)
        }

        /// <summary>
        /// Initializes the test environment before each test.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public override async Task InitializeAsync()
        {
            // Call base.InitializeAsync() to set up the test environment
            await base.InitializeAsync();

            // Get required services from ServiceProvider
            _locationService = ServiceProvider.GetService<ILocationService>();
            _photoService = ServiceProvider.GetService<IPhotoService>();
            _timeTrackingService = ServiceProvider.GetService<ITimeTrackingService>();
            _patrolService = ServiceProvider.GetService<IPatrolService>();

            // Assert that all services are not null
            Assert.NotNull(_locationService);
            Assert.NotNull(_photoService);
            Assert.NotNull(_timeTrackingService);
            Assert.NotNull(_patrolService);

            // Force garbage collection to ensure clean memory state
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        /// <summary>
        /// Tests the memory usage during application startup.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        [Fact]
        public async Task TestAppStartupMemoryUsage()
        {
            // Log test start
            Logger.LogInformation("Starting TestAppStartupMemoryUsage");

            // Get current memory usage as baseline
            long baselineMemory = GetCurrentMemoryUsage();

            // Simulate app startup by accessing key services
            await _locationService.GetCurrentLocation();
            await _photoService.GetStoredPhotosAsync();
            await _timeTrackingService.GetHistory(10);
            await _patrolService.GetLocations();

            // Get memory usage after startup
            long startupMemory = GetCurrentMemoryUsage();

            // Calculate memory difference
            long memoryDifference = startupMemory - baselineMemory;
            double memoryDifferenceMB = memoryDifference / (1024.0 * 1024.0);

            // Assert that memory usage is below AppStartupMemoryThresholdMB
            AssertMemoryThreshold(memoryDifference, AppStartupMemoryThresholdMB, "App Startup");

            // Log test completion with memory metrics
            Logger.LogInformation("TestAppStartupMemoryUsage completed. Memory Usage: {MemoryDifferenceMB} MB", memoryDifferenceMB);
        }

        /// <summary>
        /// Tests the memory usage during location tracking operations.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        [Fact]
        public async Task TestLocationTrackingMemoryUsage()
        {
            // Log test start
            Logger.LogInformation("Starting TestLocationTrackingMemoryUsage");

            // Create location tracking operation that starts tracking, gets location, and stops tracking
            Func<Task> locationTrackingOperation = async () =>
            {
                await _locationService.StartTracking();
                await _locationService.GetCurrentLocation();
                await _locationService.StopTracking();
            };

            // Measure memory usage of the location tracking operation
            long memoryUsage = await MeasureMemoryUsageAsync(locationTrackingOperation, "Location Tracking");
            double memoryUsageMB = memoryUsage / (1024.0 * 1024.0);

            // Assert that memory usage is below LocationTrackingMemoryThresholdMB
            AssertMemoryThreshold(memoryUsage, LocationTrackingMemoryThresholdMB, "Location Tracking");

            // Run benchmark for continuous location tracking (multiple iterations)
            (double averageTime, long averageMemory) = await RunBenchmarkAsync(locationTrackingOperation, "Continuous Location Tracking", 5);

            // Assert that average memory usage remains stable (no significant increase over time)
            AssertMemoryThreshold(averageMemory, LocationTrackingMemoryThresholdMB, "Continuous Location Tracking (Average)");

            // Log test completion with memory metrics
            Logger.LogInformation("TestLocationTrackingMemoryUsage completed. Memory Usage: {MemoryUsageMB} MB", memoryUsageMB);
        }

        /// <summary>
        /// Tests the memory usage during photo capture and processing operations.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        [Fact]
        public async Task TestPhotoCaptureMemoryUsage()
        {
            // Log test start
            Logger.LogInformation("Starting TestPhotoCaptureMemoryUsage");

            // Create photo capture operation that captures and processes a photo
            Func<Task> photoCaptureOperation = CreatePhotoCaptureOperation();

            // Measure memory usage of the photo capture operation
            long memoryUsage = await MeasureMemoryUsageAsync(photoCaptureOperation, "Photo Capture");
            double memoryUsageMB = memoryUsage / (1024.0 * 1024.0);

            // Assert that memory usage is below PhotoCaptureMemoryThresholdMB
            AssertMemoryThreshold(memoryUsage, PhotoCaptureMemoryThresholdMB, "Photo Capture");

            // Create photo retrieval operation that loads stored photos
            Func<Task> photoRetrievalOperation = CreatePhotoRetrievalOperation();

            // Measure memory usage of the photo retrieval operation
            long retrievalMemoryUsage = await MeasureMemoryUsageAsync(photoRetrievalOperation, "Photo Retrieval");
            double retrievalMemoryUsageMB = retrievalMemoryUsage / (1024.0 * 1024.0);

            // Assert that memory usage is below PhotoCaptureMemoryThresholdMB
            AssertMemoryThreshold(retrievalMemoryUsage, PhotoCaptureMemoryThresholdMB, "Photo Retrieval");

            // Log test completion with memory metrics
            Logger.LogInformation("TestPhotoCaptureMemoryUsage completed. Memory Usage: {MemoryUsageMB} MB, Retrieval Memory Usage: {RetrievalMemoryUsageMB} MB", memoryUsageMB, retrievalMemoryUsageMB);
        }

        /// <summary>
        /// Tests the memory usage during time tracking operations.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        [Fact]
        public async Task TestTimeTrackingMemoryUsage()
        {
            // Log test start
            Logger.LogInformation("Starting TestTimeTrackingMemoryUsage");

            // Create time tracking operation that performs clock in and clock out
            Func<Task> timeTrackingOperation = CreateTimeTrackingOperation();

            // Measure memory usage of the time tracking operation
            long memoryUsage = await MeasureMemoryUsageAsync(timeTrackingOperation, "Time Tracking");
            double memoryUsageMB = memoryUsage / (1024.0 * 1024.0);

            // Assert that memory usage is below TimeTrackingMemoryThresholdMB
            AssertMemoryThreshold(memoryUsage, TimeTrackingMemoryThresholdMB, "Time Tracking");

            // Create history retrieval operation that loads time tracking history
            Func<Task> historyRetrievalOperation = CreateHistoryRetrievalOperation();

            // Measure memory usage of the history retrieval operation
            long historyMemoryUsage = await MeasureMemoryUsageAsync(historyRetrievalOperation, "History Retrieval");
            double historyMemoryUsageMB = historyMemoryUsage / (1024.0 * 1024.0);

            // Assert that memory usage is below TimeTrackingMemoryThresholdMB
            AssertMemoryThreshold(historyMemoryUsage, TimeTrackingMemoryThresholdMB, "History Retrieval");

            // Log test completion with memory metrics
            Logger.LogInformation("TestTimeTrackingMemoryUsage completed. Memory Usage: {MemoryUsageMB} MB, History Memory Usage: {HistoryMemoryUsageMB} MB", memoryUsageMB, historyMemoryUsageMB);
        }

        /// <summary>
        /// Tests the memory usage during patrol management operations.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        [Fact]
        public async Task TestPatrolManagementMemoryUsage()
        {
            // Log test start
            Logger.LogInformation("Starting TestPatrolManagementMemoryUsage");

            // Create patrol data loading operation that retrieves locations and checkpoints
            Func<Task> patrolDataLoadingOperation = CreatePatrolDataLoadingOperation();

            // Measure memory usage of the patrol data loading operation
            long dataLoadingMemoryUsage = await MeasureMemoryUsageAsync(patrolDataLoadingOperation, "Patrol Data Loading");
            double dataLoadingMemoryUsageMB = dataLoadingMemoryUsage / (1024.0 * 1024.0);

            // Assert that memory usage is below PatrolManagementMemoryThresholdMB
            AssertMemoryThreshold(dataLoadingMemoryUsage, PatrolManagementMemoryThresholdMB, "Patrol Data Loading");

            // Create patrol management operation that starts and ends a patrol
            Func<Task> patrolManagementOperation = CreatePatrolManagementOperation();

            // Measure memory usage of the patrol management operation
            long managementMemoryUsage = await MeasureMemoryUsageAsync(patrolManagementOperation, "Patrol Management");
            double managementMemoryUsageMB = managementMemoryUsage / (1024.0 * 1024.0);

            // Assert that memory usage is below PatrolManagementMemoryThresholdMB
            AssertMemoryThreshold(managementMemoryUsage, PatrolManagementMemoryThresholdMB, "Patrol Management");

            // Log test completion with memory metrics
            Logger.LogInformation("TestPatrolManagementMemoryUsage completed. Data Loading Memory Usage: {DataLoadingMemoryUsageMB} MB, Management Memory Usage: {ManagementMemoryUsageMB} MB", dataLoadingMemoryUsageMB, managementMemoryUsageMB);
        }

        /// <summary>
        /// Tests the application's memory usage behavior under low memory conditions.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        [Fact]
        public async Task TestMemoryUsageUnderLowMemoryConditions()
        {
            // Log test start
            Logger.LogInformation("Starting TestMemoryUsageUnderLowMemoryConditions");

            // Call SimulateLowResourceEnvironment() to simulate a low-memory device
            SimulateLowResourceEnvironment();

            // Use DeviceInfoHelper to simulate limited available memory
            DeviceInfoHelper.SimulateAvailableMemory((long)(LowMemoryThresholdMB * 1024 * 1024));

            // Create a composite operation that exercises multiple app features
            Func<Task> compositeOperation = CreateCompositeOperation();

            // Measure memory usage of the composite operation
            long memoryUsage = await MeasureMemoryUsageAsync(compositeOperation, "Composite Operation (Low Memory)");
            double memoryUsageMB = memoryUsage / (1024.0 * 1024.0);

            // Assert that memory usage is below LowMemoryThresholdMB
            AssertMemoryThreshold(memoryUsage, LowMemoryThresholdMB, "Composite Operation (Low Memory)");

            // Verify that the application adapts to low memory conditions

            // Log test completion with memory metrics
            Logger.LogInformation("TestMemoryUsageUnderLowMemoryConditions completed. Memory Usage: {MemoryUsageMB} MB", memoryUsageMB);
        }

        /// <summary>
        /// Tests for potential memory leaks by performing repeated operations and measuring memory growth.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        [Fact]
        public async Task TestMemoryLeakDetection()
        {
            // Log test start
            Logger.LogInformation("Starting TestMemoryLeakDetection");

            // Force garbage collection to establish baseline memory usage
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Record initial memory usage
            long initialMemory = GC.GetTotalMemory(true);

            // Perform a series of operations that could potentially cause memory leaks
            Func<Task> potentialMemoryLeakOperation = CreatePotentialMemoryLeakOperation(10);
            await potentialMemoryLeakOperation();

            // Force garbage collection after operations
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Record final memory usage
            long finalMemory = GC.GetTotalMemory(true);

            // Calculate memory difference
            long memoryDifference = finalMemory - initialMemory;
            double memoryDifferenceMB = memoryDifference / (1024.0 * 1024.0);

            // Assert that memory growth is below MemoryLeakThresholdMB
            AssertMemoryThreshold(memoryDifference, MemoryLeakThresholdMB, "Memory Leak");

            // Log test completion with memory metrics
            Logger.LogInformation("TestMemoryLeakDetection completed. Memory Growth: {MemoryDifferenceMB} MB", memoryDifferenceMB);
        }

        /// <summary>
        /// Tests that memory is properly reclaimed after high-memory operations.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        [Fact]
        public async Task TestMemoryReclamationAfterHighUsage()
        {
            // Log test start
            Logger.LogInformation("Starting TestMemoryReclamationAfterHighUsage");

            // Record initial memory usage
            long initialMemory = GC.GetTotalMemory(true);

            // Perform memory-intensive operations (photo capture, patrol data loading)
            Func<Task> memoryIntensiveOperation = CreateMemoryIntensiveOperation();
            long peakMemory = await MeasureMemoryUsageAsync(memoryIntensiveOperation, "Memory Intensive Operation");
            double peakMemoryMB = peakMemory / (1024.0 * 1024.0);

            // Force garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Wait for memory reclamation
            await Task.Delay(TimeSpan.FromSeconds(5));

            // Record final memory usage
            long finalMemory = GC.GetTotalMemory(true);
            double finalMemoryMB = finalMemory / (1024.0 * 1024.0);

            // Assert that final memory usage is significantly lower than peak usage
            Assert.True(finalMemory < peakMemory * 0.8, $"Final memory usage ({finalMemoryMB} MB) should be significantly lower than peak usage ({peakMemoryMB} MB)");

            // Assert that final memory usage is close to initial memory usage
            Assert.True(Math.Abs(finalMemory - initialMemory) < (10 * 1024 * 1024), $"Final memory usage ({finalMemoryMB} MB) should be close to initial memory usage ({initialMemory / (1024.0 * 1024.0)} MB)");

            // Log test completion with memory metrics
            Logger.LogInformation("TestMemoryReclamationAfterHighUsage completed. Initial Memory: {InitialMemoryMB} MB, Peak Memory: {PeakMemoryMB} MB, Final Memory: {FinalMemoryMB} MB", initialMemory / (1024.0 * 1024.0), peakMemoryMB, finalMemoryMB);
        }

        #region Operation Creation Methods

        /// <summary>
        /// Creates a location tracking operation for memory testing.
        /// </summary>
        /// <returns>A function that performs location tracking operations</returns>
        private Func<Task> CreateLocationTrackingOperation()
        {
            // Return an async function that:
            // Starts location tracking
            // Gets current location
            // Stops location tracking
            return async () =>
            {
                await _locationService.StartTracking();
                await _locationService.GetCurrentLocation();
                await _locationService.StopTracking();
            };
        }

        /// <summary>
        /// Creates a photo capture operation for memory testing.
        /// </summary>
        /// <returns>A function that performs photo capture operations</returns>
        private Func<Task> CreatePhotoCaptureOperation()
        {
            // Return an async function that:
            // Captures a photo
            // Processes the photo
            return async () =>
            {
                await _photoService.CapturePhotoAsync();
            };
        }

        /// <summary>
        /// Creates a photo retrieval operation for memory testing.
        /// </summary>
        /// <returns>A function that performs photo retrieval operations</returns>
        private Func<Task> CreatePhotoRetrievalOperation()
        {
            // Return an async function that:
            // Gets stored photos
            // If photos exist, retrieves the first photo file
            return async () =>
            {
                var photos = await _photoService.GetStoredPhotosAsync();
                if (photos.Any())
                {
                    await _photoService.GetPhotoFileAsync(photos.First().Id);
                }
            };
        }

        /// <summary>
        /// Creates a time tracking operation for memory testing.
        /// </summary>
        /// <returns>A function that performs time tracking operations</returns>
        private Func<Task> CreateTimeTrackingOperation()
        {
            // Return an async function that:
            // Performs clock in
            // Waits briefly
            // Performs clock out
            return async () =>
            {
                await _timeTrackingService.ClockIn();
                await Task.Delay(100);
                await _timeTrackingService.ClockOut();
            };
        }

        /// <summary>
        /// Creates a history retrieval operation for memory testing.
        /// </summary>
        /// <returns>A function that performs history retrieval operations</returns>
        private Func<Task> CreateHistoryRetrievalOperation()
        {
            // Return an async function that:
            // Retrieves time tracking history (last 50 records)
            return async () =>
            {
                await _timeTrackingService.GetHistory(50);
            };
        }

        /// <summary>
        /// Creates a patrol data loading operation for memory testing.
        /// </summary>
        /// <returns>A function that performs patrol data loading operations</returns>
        private Func<Task> CreatePatrolDataLoadingOperation()
        {
            // Return an async function that:
            // Gets all patrol locations
            // For the first location, get all checkpoints
            return async () =>
            {
                var locations = await _patrolService.GetLocations();
                if (locations.Any())
                {
                    await _patrolService.GetCheckpoints(locations.First().Id);
                }
            };
        }

        /// <summary>
        /// Creates a patrol management operation for memory testing.
        /// </summary>
        /// <returns>A function that performs patrol management operations</returns>
        private Func<Task> CreatePatrolManagementOperation()
        {
            // Return an async function that:
            // Gets all patrol locations
            // If locations exist, starts a patrol for the first location
            // Waits briefly
            // Ends the patrol
            return async () =>
            {
                var locations = await _patrolService.GetLocations();
                if (locations.Any())
                {
                    await _patrolService.StartPatrol(locations.First().Id);
                    await Task.Delay(100);
                    await _patrolService.EndPatrol();
                }
            };
        }

        /// <summary>
        /// Creates a composite operation that exercises multiple app features for memory testing.
        /// </summary>
        /// <returns>A function that performs multiple operations</returns>
        private Func<Task> CreateCompositeOperation()
        {
            // Return an async function that:
            // Performs location tracking operation
            // Performs time tracking operation
            // Performs patrol data loading operation
            return async () =>
            {
                await CreateLocationTrackingOperation()();
                await CreateTimeTrackingOperation()();
                await CreatePatrolDataLoadingOperation()();
            };
        }

        /// <summary>
        /// Creates a memory-intensive operation for testing memory reclamation.
        /// </summary>
        /// <returns>A function that performs memory-intensive operations</returns>
        private Func<Task> CreateMemoryIntensiveOperation()
        {
            // Return an async function that:
            // Captures multiple photos in sequence
            // Loads patrol data with all checkpoints
            // Retrieves and processes time history
            return async () =>
            {
                // Capture multiple photos
                for (int i = 0; i < 3; i++)
                {
                    await _photoService.CapturePhotoAsync();
                }

                // Load patrol data
                var locations = await _patrolService.GetLocations();
                if (locations.Any())
                {
                    await _patrolService.GetCheckpoints(locations.First().Id);
                }

                // Retrieve and process time history
                await _timeTrackingService.GetHistory(50);
            };
        }

        /// <summary>
        /// Creates an operation that could potentially cause memory leaks for testing.
        /// </summary>
        /// <param name="iterations">The number of iterations</param>
        /// <returns>A function that performs repeated operations</returns>
        private Func<Task> CreatePotentialMemoryLeakOperation(int iterations)
        {
            // Return an async function that:
            // For the specified number of iterations:
            // Performs location tracking operation
            // Performs patrol data loading operation
            // Performs time tracking operation
            return async () =>
            {
                for (int i = 0; i < iterations; i++)
                {
                    await CreateLocationTrackingOperation()();
                    await CreatePatrolDataLoadingOperation()();
                    await CreateTimeTrackingOperation()();
                }
            };
        }

        #endregion
    }
}