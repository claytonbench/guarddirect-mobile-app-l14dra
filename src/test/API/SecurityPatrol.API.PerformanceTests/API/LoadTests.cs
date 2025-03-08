using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;
using FluentAssertions;
using SecurityPatrol.API.PerformanceTests.Setup;
using SecurityPatrol.TestCommon.Constants;
using SecurityPatrol.Core.Models;

namespace SecurityPatrol.API.PerformanceTests.API
{
    /// <summary>
    /// Performance test class that evaluates the API's behavior under sustained load over time.
    /// </summary>
    public class LoadTests : PerformanceTestBase
    {
        private readonly double LoadTestThreshold;
        private readonly double ExtendedLoadTestThreshold;
        private readonly int StandardUserCount;
        private readonly int HighUserCount;
        private readonly int TestDurationSeconds;
        private readonly int ExtendedTestDurationSeconds;
        private readonly int RequestIntervalMilliseconds;

        /// <summary>
        /// Initializes a new instance of the LoadTests class with test output helper
        /// </summary>
        /// <param name="outputHelper">The test output helper for logging test results</param>
        public LoadTests(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {
            LoadTestThreshold = 1000.0; // 1 second threshold for standard tests
            ExtendedLoadTestThreshold = 1500.0; // 1.5 second threshold for extended tests
            StandardUserCount = 5; // 5 concurrent users for standard tests
            HighUserCount = 15; // 15 concurrent users for high load tests
            TestDurationSeconds = 30; // 30 seconds for standard tests
            ExtendedTestDurationSeconds = 120; // 2 minutes for extended tests
            RequestIntervalMilliseconds = 500; // 500ms between batches of requests
            
            // Set authentication token for the HTTP client
            SetAuthToken(TestConstants.TestAuthToken);
        }

        /// <summary>
        /// Tests the authentication endpoint under sustained load over a period of time
        /// </summary>
        [Fact]
        public async Task TestAuthenticationEndpointUnderSustainedLoad()
        {
            // Create authentication request
            var authRequest = new AuthenticationRequest
            {
                PhoneNumber = TestConstants.TestPhoneNumber
            };

            // Create a list to store execution times
            var executionTimes = new List<double>();
            
            // Start a stopwatch to measure total test duration
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            
            // Run test for specified duration
            while (stopwatch.Elapsed.TotalSeconds < TestDurationSeconds)
            {
                // Run concurrent requests
                var results = await RunConcurrentRequests<object>(
                    () => PostAsync<AuthenticationRequest, object>("auth/verify", authRequest),
                    StandardUserCount);
                
                // Collect execution times
                executionTimes.AddRange(results.Select(r => r.elapsedMilliseconds));
                
                // Wait before next batch
                await Task.Delay(RequestIntervalMilliseconds);
            }
            
            // Calculate performance statistics
            var stats = GetPerformanceStatistics(executionTimes);
            
            // Assert that performance meets the threshold
            AssertPerformance(executionTimes, LoadTestThreshold, "Authentication Endpoint");
            
            // Log performance statistics
            LogPerformanceStatistics("Authentication Endpoint Load Test", stats, LoadTestThreshold);
        }

        /// <summary>
        /// Tests the location batch endpoint under sustained load over a period of time
        /// </summary>
        [Fact]
        public async Task TestLocationBatchEndpointUnderSustainedLoad()
        {
            // Create location batch with test data
            var batchRequest = CreateTestLocationBatch(50);

            // Create a list to store execution times
            var executionTimes = new List<double>();
            
            // Start a stopwatch to measure total test duration
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            
            // Run test for specified duration
            while (stopwatch.Elapsed.TotalSeconds < TestDurationSeconds)
            {
                // Run concurrent requests
                var results = await RunConcurrentRequests<object>(
                    () => PostAsync<LocationBatchRequest, object>("location/batch", batchRequest),
                    StandardUserCount);
                
                // Collect execution times
                executionTimes.AddRange(results.Select(r => r.elapsedMilliseconds));
                
                // Wait before next batch
                await Task.Delay(RequestIntervalMilliseconds);
            }
            
            // Calculate performance statistics
            var stats = GetPerformanceStatistics(executionTimes);
            
            // Assert that performance meets the threshold
            AssertPerformance(executionTimes, LoadTestThreshold, "Location Batch Endpoint");
            
            // Log performance statistics
            LogPerformanceStatistics("Location Batch Endpoint Load Test", stats, LoadTestThreshold);
        }

        /// <summary>
        /// Tests the time clock endpoint under sustained load over a period of time
        /// </summary>
        [Fact]
        public async Task TestTimeClockEndpointUnderSustainedLoad()
        {
            // Create a time record request
            var clockInRequest = CreateTestTimeRecordRequest("ClockIn");

            // Create a list to store execution times
            var executionTimes = new List<double>();
            
            // Start a stopwatch to measure total test duration
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            
            // Run test for specified duration
            while (stopwatch.Elapsed.TotalSeconds < TestDurationSeconds)
            {
                // Run concurrent requests
                var results = await RunConcurrentRequests<object>(
                    () => PostAsync<TimeRecordRequest, object>("time/clock", clockInRequest),
                    StandardUserCount);
                
                // Collect execution times
                executionTimes.AddRange(results.Select(r => r.elapsedMilliseconds));
                
                // Wait before next batch
                await Task.Delay(RequestIntervalMilliseconds);
            }
            
            // Calculate performance statistics
            var stats = GetPerformanceStatistics(executionTimes);
            
            // Assert that performance meets the threshold
            AssertPerformance(executionTimes, LoadTestThreshold, "Time Clock Endpoint");
            
            // Log performance statistics
            LogPerformanceStatistics("Time Clock Endpoint Load Test", stats, LoadTestThreshold);
        }

        /// <summary>
        /// Tests the patrol verification endpoint under sustained load over a period of time
        /// </summary>
        [Fact]
        public async Task TestPatrolVerificationEndpointUnderSustainedLoad()
        {
            // Create a checkpoint verification request
            var verificationRequest = CreateTestCheckpointVerificationRequest();

            // Create a list to store execution times
            var executionTimes = new List<double>();
            
            // Start a stopwatch to measure total test duration
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            
            // Run test for specified duration
            while (stopwatch.Elapsed.TotalSeconds < TestDurationSeconds)
            {
                // Run concurrent requests
                var results = await RunConcurrentRequests<object>(
                    () => PostAsync<CheckpointVerificationRequest, object>("patrol/verify", verificationRequest),
                    StandardUserCount);
                
                // Collect execution times
                executionTimes.AddRange(results.Select(r => r.elapsedMilliseconds));
                
                // Wait before next batch
                await Task.Delay(RequestIntervalMilliseconds);
            }
            
            // Calculate performance statistics
            var stats = GetPerformanceStatistics(executionTimes);
            
            // Assert that performance meets the threshold
            AssertPerformance(executionTimes, LoadTestThreshold, "Patrol Verification Endpoint");
            
            // Log performance statistics
            LogPerformanceStatistics("Patrol Verification Endpoint Load Test", stats, LoadTestThreshold);
        }

        /// <summary>
        /// Tests the report submission endpoint under sustained load over a period of time
        /// </summary>
        [Fact]
        public async Task TestReportSubmissionEndpointUnderSustainedLoad()
        {
            // Create a report request
            var reportRequest = CreateTestReportRequest();

            // Create a list to store execution times
            var executionTimes = new List<double>();
            
            // Start a stopwatch to measure total test duration
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            
            // Run test for specified duration
            while (stopwatch.Elapsed.TotalSeconds < TestDurationSeconds)
            {
                // Run concurrent requests
                var results = await RunConcurrentRequests<object>(
                    () => PostAsync<ReportRequest, object>("reports", reportRequest),
                    StandardUserCount);
                
                // Collect execution times
                executionTimes.AddRange(results.Select(r => r.elapsedMilliseconds));
                
                // Wait before next batch
                await Task.Delay(RequestIntervalMilliseconds);
            }
            
            // Calculate performance statistics
            var stats = GetPerformanceStatistics(executionTimes);
            
            // Assert that performance meets the threshold
            AssertPerformance(executionTimes, LoadTestThreshold, "Report Submission Endpoint");
            
            // Log performance statistics
            LogPerformanceStatistics("Report Submission Endpoint Load Test", stats, LoadTestThreshold);
        }

        /// <summary>
        /// Tests multiple different endpoints under sustained load to simulate realistic usage patterns
        /// </summary>
        [Fact]
        public async Task TestMixedEndpointsUnderSustainedLoad()
        {
            // Create a list of different request functions
            var requestFunctions = CreateMixedRequestFunctions();

            // Create a list to store execution times
            var executionTimes = new List<double>();
            
            // Start a stopwatch to measure total test duration
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            
            // Create a random generator for selecting request functions
            var random = new Random();
            
            // Run test for specified duration
            while (stopwatch.Elapsed.TotalSeconds < TestDurationSeconds)
            {
                // Select a random request function
                var requestFunc = requestFunctions[random.Next(requestFunctions.Count)];
                
                // Run concurrent requests
                var results = await RunConcurrentRequests(requestFunc, StandardUserCount);
                
                // Collect execution times
                executionTimes.AddRange(results.Select(r => r.elapsedMilliseconds));
                
                // Wait before next batch
                await Task.Delay(RequestIntervalMilliseconds);
            }
            
            // Calculate performance statistics
            var stats = GetPerformanceStatistics(executionTimes);
            
            // Assert that performance meets the threshold
            AssertPerformance(executionTimes, LoadTestThreshold, "Mixed Endpoints");
            
            // Log performance statistics
            LogPerformanceStatistics("Mixed Endpoints Load Test", stats, LoadTestThreshold);
        }

        /// <summary>
        /// Tests critical endpoints under extended load over a longer period of time to evaluate system stability
        /// </summary>
        [Fact]
        public async Task TestExtendedLoadOnCriticalEndpoints()
        {
            // Create a list of critical request functions
            var criticalRequestFunctions = CreateCriticalRequestFunctions();

            // Create a list to store execution times
            var executionTimes = new List<double>();
            
            // Start a stopwatch to measure total test duration
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            
            // Create a random generator for selecting request functions
            var random = new Random();
            
            // Run test for extended duration
            while (stopwatch.Elapsed.TotalSeconds < ExtendedTestDurationSeconds)
            {
                // Select a random request function
                var requestFunc = criticalRequestFunctions[random.Next(criticalRequestFunctions.Count)];
                
                // Run concurrent requests
                var results = await RunConcurrentRequests(requestFunc, StandardUserCount);
                
                // Collect execution times
                executionTimes.AddRange(results.Select(r => r.elapsedMilliseconds));
                
                // Wait before next batch
                await Task.Delay(RequestIntervalMilliseconds);
            }
            
            // Calculate performance statistics
            var stats = GetPerformanceStatistics(executionTimes);
            
            // Assert that performance meets the extended threshold
            AssertPerformance(executionTimes, ExtendedLoadTestThreshold, "Extended Load on Critical Endpoints");
            
            // Log performance statistics
            LogPerformanceStatistics("Extended Load Test on Critical Endpoints", stats, ExtendedLoadTestThreshold);
        }

        /// <summary>
        /// Tests the location batch endpoint under high user load to evaluate system behavior at scale
        /// </summary>
        [Fact]
        public async Task TestHighUserLoadOnLocationBatchEndpoint()
        {
            // Create location batch with test data
            var batchRequest = CreateTestLocationBatch(50);

            // Create a list to store execution times
            var executionTimes = new List<double>();
            
            // Start a stopwatch to measure total test duration
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            
            // Run test for specified duration
            while (stopwatch.Elapsed.TotalSeconds < TestDurationSeconds)
            {
                // Run concurrent requests with high user count
                var results = await RunConcurrentRequests<object>(
                    () => PostAsync<LocationBatchRequest, object>("location/batch", batchRequest),
                    HighUserCount);
                
                // Collect execution times
                executionTimes.AddRange(results.Select(r => r.elapsedMilliseconds));
                
                // Wait longer between batches for high user count
                await Task.Delay(RequestIntervalMilliseconds * 2);
            }
            
            // Calculate performance statistics
            var stats = GetPerformanceStatistics(executionTimes);
            
            // Assert that performance meets the extended threshold
            AssertPerformance(executionTimes, ExtendedLoadTestThreshold, "High User Load on Location Batch Endpoint");
            
            // Log performance statistics
            LogPerformanceStatistics("High User Load on Location Batch Endpoint", stats, ExtendedLoadTestThreshold);
        }

        /// <summary>
        /// Tests system resource utilization under sustained load to identify potential bottlenecks
        /// </summary>
        [Fact]
        public async Task TestResourceUtilizationUnderSustainedLoad()
        {
            // This test would ideally integrate with resource monitoring tools
            // Since direct resource monitoring isn't available, we'll focus on performance degradation over time
            
            // Create a list of mixed request functions
            var requestFunctions = CreateMixedRequestFunctions();

            // Create a list to store execution times
            var executionTimes = new List<double>();
            
            // Start a stopwatch to measure total test duration
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            
            // Create a random generator for selecting request functions
            var random = new Random();
            
            // Run test for extended duration
            while (stopwatch.Elapsed.TotalSeconds < ExtendedTestDurationSeconds)
            {
                // Select a random request function
                var requestFunc = requestFunctions[random.Next(requestFunctions.Count)];
                
                // Run concurrent requests with high user count
                var results = await RunConcurrentRequests(requestFunc, HighUserCount);
                
                // Collect execution times
                executionTimes.AddRange(results.Select(r => r.elapsedMilliseconds));
                
                // Wait before next batch
                await Task.Delay(RequestIntervalMilliseconds);
            }
            
            // Calculate performance statistics
            var stats = GetPerformanceStatistics(executionTimes);
            
            // Log performance statistics
            LogPerformanceStatistics("Resource Utilization Under Sustained Load", stats, ExtendedLoadTestThreshold);
            
            // Document any performance degradation over time
            // We could analyze the execution times to see if they increase over time
            // For now, just assert the overall performance meets the threshold
            AssertPerformance(executionTimes, ExtendedLoadTestThreshold, "Resource Utilization Test");
        }

        /// <summary>
        /// Creates a test location batch with multiple location points
        /// </summary>
        /// <param name="count">The number of location points to create</param>
        /// <returns>A location batch request with test data</returns>
        private LocationBatchRequest CreateTestLocationBatch(int count)
        {
            var batch = new LocationBatchRequest();
            var locations = new List<LocationModel>();
            
            for (int i = 0; i < count; i++)
            {
                locations.Add(new LocationModel
                {
                    Latitude = TestConstants.TestLatitude + (i * 0.0001 % 0.01),
                    Longitude = TestConstants.TestLongitude + (i * 0.0001 % 0.01),
                    Accuracy = TestConstants.TestAccuracy,
                    Timestamp = DateTime.UtcNow.AddSeconds(-i)
                });
            }
            
            batch.Locations = locations;
            return batch;
        }

        /// <summary>
        /// Creates a test time record request for clock in/out operations
        /// </summary>
        /// <param name="type">The type of time record (ClockIn or ClockOut)</param>
        /// <returns>A time record request with test data</returns>
        private TimeRecordRequest CreateTestTimeRecordRequest(string type)
        {
            return new TimeRecordRequest
            {
                Type = type,
                Timestamp = DateTime.UtcNow,
                Location = new LocationModel
                {
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                }
            };
        }

        /// <summary>
        /// Creates a test report request with sample text and location
        /// </summary>
        /// <returns>A report request with test data</returns>
        private ReportRequest CreateTestReportRequest()
        {
            return new ReportRequest
            {
                Text = TestConstants.TestReportText,
                Timestamp = DateTime.UtcNow,
                Location = new LocationData
                {
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                }
            };
        }

        /// <summary>
        /// Creates a test checkpoint verification request
        /// </summary>
        /// <returns>A checkpoint verification request with test data</returns>
        private CheckpointVerificationRequest CreateTestCheckpointVerificationRequest()
        {
            return new CheckpointVerificationRequest
            {
                CheckpointId = TestConstants.TestCheckpointId,
                Timestamp = DateTime.UtcNow,
                Location = new LocationModel
                {
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                }
            };
        }

        /// <summary>
        /// Creates a list of mixed request functions for different endpoints
        /// </summary>
        /// <returns>A list of request functions for different endpoints</returns>
        private List<Func<Task<object>>> CreateMixedRequestFunctions()
        {
            var functions = new List<Func<Task<object>>>();
            
            // Add GET requests
            functions.Add(() => GetAsync<object>("time/history"));
            functions.Add(() => GetAsync<object>("patrol/locations"));
            functions.Add(() => GetAsync<object>("reports"));
            
            // Add POST requests
            functions.Add(() => PostAsync<LocationBatchRequest, object>("location/batch", CreateTestLocationBatch(10)));
            functions.Add(() => PostAsync<CheckpointVerificationRequest, object>("patrol/verify", CreateTestCheckpointVerificationRequest()));
            functions.Add(() => PostAsync<ReportRequest, object>("reports", CreateTestReportRequest()));
            
            return functions;
        }

        /// <summary>
        /// Creates a list of request functions for critical endpoints
        /// </summary>
        /// <returns>A list of request functions for critical endpoints</returns>
        private List<Func<Task<object>>> CreateCriticalRequestFunctions()
        {
            var functions = new List<Func<Task<object>>>();
            
            // Add critical endpoint requests
            functions.Add(() => PostAsync<AuthenticationRequest, object>("auth/verify", new AuthenticationRequest { PhoneNumber = TestConstants.TestPhoneNumber }));
            functions.Add(() => PostAsync<TimeRecordRequest, object>("time/clock", CreateTestTimeRecordRequest("ClockIn")));
            functions.Add(() => PostAsync<LocationBatchRequest, object>("location/batch", CreateTestLocationBatch(20)));
            functions.Add(() => PostAsync<CheckpointVerificationRequest, object>("patrol/verify", CreateTestCheckpointVerificationRequest()));
            
            return functions;
        }

        /// <summary>
        /// Runs a load test for the specified duration with the given request function and user count
        /// </summary>
        /// <param name="requestFunc">The function that executes the request</param>
        /// <param name="userCount">The number of concurrent users to simulate</param>
        /// <param name="durationSeconds">The duration of the test in seconds</param>
        /// <param name="intervalMilliseconds">The interval between batches of requests in milliseconds</param>
        /// <returns>List of execution times from the load test</returns>
        private async Task<List<double>> RunLoadTest(Func<Task<object>> requestFunc, int userCount, int durationSeconds, int intervalMilliseconds)
        {
            // Create a list to store execution times
            var executionTimes = new List<double>();
            
            // Start a stopwatch to measure total test duration
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            
            // Run test for specified duration
            while (stopwatch.Elapsed.TotalSeconds < durationSeconds)
            {
                // Run concurrent requests
                var results = await RunConcurrentRequests(requestFunc, userCount);
                
                // Extract execution times
                executionTimes.AddRange(results.Select(r => r.elapsedMilliseconds));
                
                // Wait before next batch
                await Task.Delay(intervalMilliseconds);
            }
            
            return executionTimes;
        }
    }
}