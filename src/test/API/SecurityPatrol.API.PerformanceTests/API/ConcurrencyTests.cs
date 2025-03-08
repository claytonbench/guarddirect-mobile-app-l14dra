using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using FluentAssertions;
using SecurityPatrol.API.PerformanceTests.Setup;
using SecurityPatrol.TestCommon.Constants;
using SecurityPatrol.Core.Models;

namespace SecurityPatrol.API.PerformanceTests.API
{
    /// <summary>
    /// Performance test class that evaluates the API's behavior under concurrent load by simulating 
    /// multiple simultaneous users and requests. Tests measure response times, error rates, and system 
    /// stability when handling concurrent operations across different endpoints.
    /// </summary>
    public class ConcurrencyTests : PerformanceTestBase
    {
        private readonly double ConcurrencyThreshold;
        private readonly double HighConcurrencyThreshold;
        private readonly int StandardConcurrentUsers;
        private readonly int HighConcurrentUsers;
        private readonly int StressConcurrentUsers;

        /// <summary>
        /// Initializes a new instance of the ConcurrencyTests class with test output helper.
        /// </summary>
        /// <param name="outputHelper">The test output helper for logging test results.</param>
        public ConcurrencyTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
            // Initialize thresholds and concurrent user counts
            ConcurrencyThreshold = 1000; // 1 second as per SLA requirements
            HighConcurrencyThreshold = 1500; // 1.5 seconds for high concurrency tests
            StandardConcurrentUsers = 10;
            HighConcurrentUsers = 25;
            StressConcurrentUsers = 50;

            // Set authentication token for API requests
            SetAuthToken(TestConstants.TestAuthToken);
        }

        /// <summary>
        /// Tests the authentication endpoint under concurrent load with multiple users requesting verification codes simultaneously.
        /// </summary>
        [Fact]
        public async Task TestConcurrentAuthenticationRequests()
        {
            // Create an authentication request with test phone number
            var request = new AuthenticationRequest
            {
                PhoneNumber = TestConstants.TestPhoneNumber
            };

            // Run concurrent requests to the auth/verify endpoint
            var results = await RunConcurrentRequests<object>(
                () => PostAsync<AuthenticationRequest, object>("v1/auth/verify", request),
                StandardConcurrentUsers);

            // Extract execution times from results
            var executionTimes = results.Select(r => r.elapsedMilliseconds).ToList();

            // Calculate performance statistics and validate against threshold
            AssertPerformance(executionTimes, ConcurrencyThreshold, "Authentication Endpoint - Request Verification Code");

            // Log detailed performance statistics
            LogPerformanceStatistics("Authentication Endpoint - Request Verification Code", executionTimes, ConcurrencyThreshold);
        }

        /// <summary>
        /// Tests the verification endpoint under concurrent load with multiple users validating verification codes simultaneously.
        /// </summary>
        [Fact]
        public async Task TestConcurrentVerificationRequests()
        {
            // Create a verification request with test phone number and verification code
            var request = new VerificationRequest
            {
                PhoneNumber = TestConstants.TestPhoneNumber,
                Code = TestConstants.TestVerificationCode
            };

            // Run concurrent requests to the auth/validate endpoint
            var results = await RunConcurrentRequests<object>(
                () => PostAsync<VerificationRequest, object>("v1/auth/validate", request),
                StandardConcurrentUsers);

            // Extract execution times from results
            var executionTimes = results.Select(r => r.elapsedMilliseconds).ToList();

            // Calculate performance statistics and validate against threshold
            AssertPerformance(executionTimes, ConcurrencyThreshold, "Verification Endpoint - Validate Code");

            // Log detailed performance statistics
            LogPerformanceStatistics("Verification Endpoint - Validate Code", executionTimes, ConcurrencyThreshold);
        }

        /// <summary>
        /// Tests the location batch endpoint under concurrent load with multiple users submitting location batches simultaneously.
        /// </summary>
        [Fact]
        public async Task TestConcurrentLocationBatchRequests()
        {
            // Create a location batch with 50 location points
            var request = CreateTestLocationBatch(50);

            // Run concurrent requests to the location batch endpoint
            var results = await RunConcurrentRequests<object>(
                () => PostAsync<LocationBatchRequest, object>("v1/location/batch", request),
                StandardConcurrentUsers);

            // Extract execution times from results
            var executionTimes = results.Select(r => r.elapsedMilliseconds).ToList();

            // Calculate performance statistics and validate against threshold
            AssertPerformance(executionTimes, ConcurrencyThreshold, "Location Batch Endpoint");

            // Log detailed performance statistics
            LogPerformanceStatistics("Location Batch Endpoint", executionTimes, ConcurrencyThreshold);
        }

        /// <summary>
        /// Tests the time clock endpoint under concurrent load with multiple users clocking in/out simultaneously.
        /// </summary>
        [Fact]
        public async Task TestConcurrentTimeClockRequests()
        {
            // Create a time record request for clock in
            var request = CreateTestTimeRecordRequest("ClockIn");

            // Run concurrent requests to the time/clock endpoint
            var results = await RunConcurrentRequests<object>(
                () => PostAsync<TimeRecordRequest, object>("v1/time/clock", request),
                StandardConcurrentUsers);

            // Extract execution times from results
            var executionTimes = results.Select(r => r.elapsedMilliseconds).ToList();

            // Calculate performance statistics and validate against threshold
            AssertPerformance(executionTimes, ConcurrencyThreshold, "Time Clock Endpoint");

            // Log detailed performance statistics
            LogPerformanceStatistics("Time Clock Endpoint", executionTimes, ConcurrencyThreshold);
        }

        /// <summary>
        /// Tests the patrol verification endpoint under concurrent load with multiple users verifying checkpoints simultaneously.
        /// </summary>
        [Fact]
        public async Task TestConcurrentPatrolVerificationRequests()
        {
            // Create a checkpoint verification request
            var request = CreateTestCheckpointVerificationRequest();

            // Run concurrent requests to the patrol/verify endpoint
            var results = await RunConcurrentRequests<object>(
                () => PostAsync<CheckpointVerificationRequest, object>("v1/patrol/verify", request),
                StandardConcurrentUsers);

            // Extract execution times from results
            var executionTimes = results.Select(r => r.elapsedMilliseconds).ToList();

            // Calculate performance statistics and validate against threshold
            AssertPerformance(executionTimes, ConcurrencyThreshold, "Patrol Verification Endpoint");

            // Log detailed performance statistics
            LogPerformanceStatistics("Patrol Verification Endpoint", executionTimes, ConcurrencyThreshold);
        }

        /// <summary>
        /// Tests the report submission endpoint under concurrent load with multiple users submitting reports simultaneously.
        /// </summary>
        [Fact]
        public async Task TestConcurrentReportSubmissionRequests()
        {
            // Create a report request with test data
            var request = CreateTestReportRequest();

            // Run concurrent requests to the reports endpoint
            var results = await RunConcurrentRequests<object>(
                () => PostAsync<ReportRequest, object>("v1/reports", request),
                StandardConcurrentUsers);

            // Extract execution times from results
            var executionTimes = results.Select(r => r.elapsedMilliseconds).ToList();

            // Calculate performance statistics and validate against threshold
            AssertPerformance(executionTimes, ConcurrencyThreshold, "Report Submission Endpoint");

            // Log detailed performance statistics
            LogPerformanceStatistics("Report Submission Endpoint", executionTimes, ConcurrencyThreshold);
        }

        /// <summary>
        /// Tests the authentication endpoint under high concurrent load to evaluate system behavior at scale.
        /// </summary>
        [Fact]
        public async Task TestHighConcurrencyAuthenticationRequests()
        {
            // Create an authentication request with test phone number
            var request = new AuthenticationRequest
            {
                PhoneNumber = TestConstants.TestPhoneNumber
            };

            // Run high concurrent requests to the auth/verify endpoint
            var results = await RunConcurrentRequests<object>(
                () => PostAsync<AuthenticationRequest, object>("v1/auth/verify", request),
                HighConcurrentUsers);

            // Extract execution times from results
            var executionTimes = results.Select(r => r.elapsedMilliseconds).ToList();

            // Calculate performance statistics and validate against high concurrency threshold
            AssertPerformance(executionTimes, HighConcurrencyThreshold, "High Concurrency Authentication Endpoint");

            // Log detailed performance statistics
            LogPerformanceStatistics("High Concurrency Authentication Endpoint", executionTimes, HighConcurrencyThreshold);
        }

        /// <summary>
        /// Tests the location batch endpoint under high concurrent load to evaluate system behavior at scale.
        /// </summary>
        [Fact]
        public async Task TestHighConcurrencyLocationBatchRequests()
        {
            // Create a location batch with 50 location points
            var request = CreateTestLocationBatch(50);

            // Run high concurrent requests to the location batch endpoint
            var results = await RunConcurrentRequests<object>(
                () => PostAsync<LocationBatchRequest, object>("v1/location/batch", request),
                HighConcurrentUsers);

            // Extract execution times from results
            var executionTimes = results.Select(r => r.elapsedMilliseconds).ToList();

            // Calculate performance statistics and validate against high concurrency threshold
            AssertPerformance(executionTimes, HighConcurrencyThreshold, "High Concurrency Location Batch Endpoint");

            // Log detailed performance statistics
            LogPerformanceStatistics("High Concurrency Location Batch Endpoint", executionTimes, HighConcurrencyThreshold);
        }

        /// <summary>
        /// Tests multiple different endpoints under concurrent load to simulate realistic usage patterns.
        /// </summary>
        [Fact]
        public async Task TestMixedConcurrentRequests()
        {
            // Create a list of different request functions for various endpoints
            var requestFunctions = CreateMixedRequestFunctions();

            // Prepare to collect all execution times
            var allExecutionTimes = new List<double>();

            // Run each request function concurrently with StandardConcurrentUsers
            foreach (var requestFunc in requestFunctions)
            {
                // Run the specific request function concurrently
                var results = await RunConcurrentRequests(requestFunc, StandardConcurrentUsers);
                
                // Add execution times to the combined list
                allExecutionTimes.AddRange(results.Select(r => r.elapsedMilliseconds));
            }

            // Calculate performance statistics and validate against threshold
            AssertPerformance(allExecutionTimes, ConcurrencyThreshold, "Mixed Endpoint Requests");

            // Log detailed performance statistics
            LogPerformanceStatistics("Mixed Endpoint Requests", allExecutionTimes, ConcurrencyThreshold);
        }

        /// <summary>
        /// Tests critical endpoints under extreme concurrent load to identify breaking points.
        /// </summary>
        [Fact]
        public async Task TestStressConcurrencyRequests()
        {
            // Create a list of critical request functions
            var requestFunctions = CreateCriticalRequestFunctions();

            // Prepare to collect all execution times
            var allExecutionTimes = new List<double>();
            
            // Track failures
            var failureCount = 0;

            // Run each request function concurrently with StressConcurrentUsers
            foreach (var requestFunc in requestFunctions)
            {
                try
                {
                    // Run the specific request function concurrently
                    var results = await RunConcurrentRequests(requestFunc, StressConcurrentUsers);
                    
                    // Add execution times to the combined list
                    allExecutionTimes.AddRange(results.Select(r => r.elapsedMilliseconds));
                }
                catch (Exception ex)
                {
                    failureCount++;
                    OutputHelper.WriteLine($"Stress test failure: {ex.Message}");
                }
            }

            // Log performance statistics regardless of test success/failure
            var stats = GetPerformanceStatistics(allExecutionTimes);
            
            OutputHelper.WriteLine($"Stress Test Results:");
            OutputHelper.WriteLine($"Total Requests: {allExecutionTimes.Count}");
            OutputHelper.WriteLine($"Failed Functions: {failureCount}");
            OutputHelper.WriteLine($"Min Response Time: {stats.min:F2}ms");
            OutputHelper.WriteLine($"Max Response Time: {stats.max:F2}ms");
            OutputHelper.WriteLine($"Avg Response Time: {stats.avg:F2}ms");
            OutputHelper.WriteLine($"95th Percentile: {stats.p95:F2}ms");
            OutputHelper.WriteLine($"99th Percentile: {stats.p99:F2}ms");
            
            // Document system behavior under extreme load
            OutputHelper.WriteLine("Stress test reveals system behavior under extreme load conditions.");
            OutputHelper.WriteLine("This test is intended to identify breaking points, not for pass/fail validation.");
        }

        #region Helper Methods

        /// <summary>
        /// Creates a test location batch with multiple location points.
        /// </summary>
        /// <param name="count">The number of location points to create.</param>
        /// <returns>A location batch request with test data.</returns>
        private LocationBatchRequest CreateTestLocationBatch(int count)
        {
            var batch = new LocationBatchRequest
            {
                UserId = TestConstants.TestUserId,
                Locations = new List<LocationModel>()
            };

            var locations = new List<LocationModel>();
            for (int i = 0; i < count; i++)
            {
                locations.Add(new LocationModel
                {
                    Latitude = TestConstants.TestLatitude + (0.0001 * i),
                    Longitude = TestConstants.TestLongitude + (0.0001 * i),
                    Accuracy = TestConstants.TestAccuracy,
                    Timestamp = DateTime.UtcNow.AddSeconds(-i)
                });
            }

            batch.Locations = locations;
            return batch;
        }

        /// <summary>
        /// Creates a test time record request for clock in/out operations.
        /// </summary>
        /// <param name="type">The type of time record (ClockIn or ClockOut).</param>
        /// <returns>A time record request with test data.</returns>
        private TimeRecordRequest CreateTestTimeRecordRequest(string type)
        {
            return new TimeRecordRequest
            {
                Type = type,
                Timestamp = DateTime.UtcNow,
                Location = new LocationModel
                {
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude,
                    Accuracy = TestConstants.TestAccuracy
                }
            };
        }

        /// <summary>
        /// Creates a test report request with sample text and location.
        /// </summary>
        /// <returns>A report request with test data.</returns>
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
        /// Creates a test checkpoint verification request.
        /// </summary>
        /// <returns>A checkpoint verification request with test data.</returns>
        private CheckpointVerificationRequest CreateTestCheckpointVerificationRequest()
        {
            return new CheckpointVerificationRequest
            {
                CheckpointId = TestConstants.TestCheckpointId,
                Timestamp = DateTime.UtcNow,
                Location = new LocationModel
                {
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude,
                    Accuracy = TestConstants.TestAccuracy
                }
            };
        }

        /// <summary>
        /// Creates a list of mixed request functions for different endpoints.
        /// </summary>
        /// <returns>A list of request functions for different endpoints.</returns>
        private List<Func<Task<object>>> CreateMixedRequestFunctions()
        {
            var functions = new List<Func<Task<object>>>
            {
                // GET requests
                () => GetAsync<object>("v1/time/history"),
                () => GetAsync<object>("v1/patrol/locations"),
                () => GetAsync<object>("v1/reports"),

                // POST requests
                () => PostAsync<LocationBatchRequest, object>("v1/location/batch", CreateTestLocationBatch(10)),
                () => PostAsync<CheckpointVerificationRequest, object>("v1/patrol/verify", CreateTestCheckpointVerificationRequest())
            };

            return functions;
        }

        /// <summary>
        /// Creates a list of request functions for critical endpoints.
        /// </summary>
        /// <returns>A list of request functions for critical endpoints.</returns>
        private List<Func<Task<object>>> CreateCriticalRequestFunctions()
        {
            var functions = new List<Func<Task<object>>>
            {
                // Critical authentication endpoints
                () => PostAsync<AuthenticationRequest, object>("v1/auth/verify", 
                    new AuthenticationRequest { PhoneNumber = TestConstants.TestPhoneNumber }),
                
                // Critical time tracking endpoints
                () => PostAsync<TimeRecordRequest, object>("v1/time/clock", 
                    CreateTestTimeRecordRequest("ClockIn")),
                
                // Critical location tracking endpoints
                () => PostAsync<LocationBatchRequest, object>("v1/location/batch", 
                    CreateTestLocationBatch(20)),
                
                // Critical patrol verification endpoints
                () => PostAsync<CheckpointVerificationRequest, object>("v1/patrol/verify", 
                    CreateTestCheckpointVerificationRequest())
            };

            return functions;
        }

        /// <summary>
        /// Logs performance statistics to the test output.
        /// </summary>
        /// <param name="testName">The name of the test.</param>
        /// <param name="executionTimes">The list of execution times.</param>
        /// <param name="threshold">The performance threshold for the test.</param>
        private void LogPerformanceStatistics(string testName, List<double> executionTimes, double threshold)
        {
            var stats = GetPerformanceStatistics(executionTimes);
            
            OutputHelper.WriteLine($"Performance statistics for {testName}:");
            OutputHelper.WriteLine($"  Concurrent Users: {executionTimes.Count}");
            OutputHelper.WriteLine($"  Min: {stats.min:F2}ms");
            OutputHelper.WriteLine($"  Max: {stats.max:F2}ms");
            OutputHelper.WriteLine($"  Avg: {stats.avg:F2}ms");
            OutputHelper.WriteLine($"  95th Percentile: {stats.p95:F2}ms");
            OutputHelper.WriteLine($"  99th Percentile: {stats.p99:F2}ms");
            OutputHelper.WriteLine($"  Threshold: {threshold:F2}ms");
            OutputHelper.WriteLine($"  Result: {(stats.p95 <= threshold ? "PASSED" : "FAILED")}");
            OutputHelper.WriteLine(string.Empty);
        }

        #endregion
    }
}