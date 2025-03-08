using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using FluentAssertions;
using BenchmarkDotNet.Attributes;
using SecurityPatrol.TestCommon.Constants;
using SecurityPatrol.Core.Models;

namespace SecurityPatrol.API.PerformanceTests.Controllers
{
    /// <summary>
    /// Test class that measures and validates the performance characteristics of API controllers in the Security Patrol application.
    /// </summary>
    public class ControllerPerformanceTests : PerformanceTestBase
    {
        // Performance thresholds for each controller in milliseconds
        private const double AuthControllerThreshold = 1000; // 1 second
        private const double TimeControllerThreshold = 1000;
        private const double LocationControllerThreshold = 1000;
        private const double PatrolControllerThreshold = 1000;
        private const double ReportControllerThreshold = 1000;
        private const double PhotoControllerThreshold = 1000;

        /// <summary>
        /// Initializes a new instance of the ControllerPerformanceTests class with test output helper
        /// </summary>
        /// <param name="outputHelper">The test output helper for logging test results</param>
        public ControllerPerformanceTests(ITestOutputHelper outputHelper) 
            : base(outputHelper)
        {
            // Base constructor initializes the test environment and client
        }

        [Fact]
        public async Task TestAuthControllerPerformance()
        {
            // Test the authentication request (verify endpoint)
            var authRequest = new AuthenticationRequest { PhoneNumber = TestConstants.TestPhoneNumber };
            var verifyTime = await MeasureExecutionTime(async () => 
                await PostAsync<AuthenticationRequest, Result>("auth/verify", authRequest));
            
            AssertPerformance(verifyTime, AuthControllerThreshold, "Auth Controller - Verify Endpoint");

            // Test the verification code validation (validate endpoint)
            var verificationRequest = new VerificationRequest
            {
                PhoneNumber = TestConstants.TestPhoneNumber,
                Code = TestConstants.TestVerificationCode
            };
            
            var (validateResult, validateTime) = await MeasureExecutionTime<Result<AuthenticationResponse>>(async () => 
                await PostAsync<VerificationRequest, Result<AuthenticationResponse>>("auth/validate", verificationRequest));
            
            AssertPerformance(validateTime, AuthControllerThreshold, "Auth Controller - Validate Endpoint");

            // If validation succeeded, extract token and test refresh endpoint
            if (validateResult?.Succeeded == true && validateResult.Data != null)
            {
                SetAuthToken(validateResult.Data.Token);

                // Test the token refresh endpoint
                var refreshTime = await MeasureExecutionTime(async () => 
                    await PostAsync<object, Result<AuthenticationResponse>>("auth/refresh", null));
                
                AssertPerformance(refreshTime, AuthControllerThreshold, "Auth Controller - Refresh Endpoint");
            }
        }

        [Fact]
        public async Task TestTimeControllerPerformance()
        {
            // Test clock-in operation
            var clockInRequest = new TimeRecordRequest
            {
                Type = "ClockIn",
                Timestamp = DateTime.UtcNow,
                Location = new LocationModel
                {
                    Latitude = TestConstants.TestLatitude, 
                    Longitude = TestConstants.TestLongitude,
                    Accuracy = TestConstants.TestAccuracy
                }
            };

            var clockInTime = await MeasureExecutionTime(async () => 
                await PostAsync<TimeRecordRequest, Result>("time", clockInRequest));
            
            AssertPerformance(clockInTime, TimeControllerThreshold, "Time Controller - Clock In Operation");

            // Test clock-out operation
            var clockOutRequest = new TimeRecordRequest
            {
                Type = "ClockOut",
                Timestamp = DateTime.UtcNow,
                Location = new LocationModel
                {
                    Latitude = TestConstants.TestLatitude, 
                    Longitude = TestConstants.TestLongitude,
                    Accuracy = TestConstants.TestAccuracy
                }
            };

            var clockOutTime = await MeasureExecutionTime(async () => 
                await PostAsync<TimeRecordRequest, Result>("time", clockOutRequest));
            
            AssertPerformance(clockOutTime, TimeControllerThreshold, "Time Controller - Clock Out Operation");

            // Test history retrieval
            var historyTime = await MeasureExecutionTime(async () => 
                await GetAsync<Result>("time/history"));
            
            AssertPerformance(historyTime, TimeControllerThreshold, "Time Controller - History Retrieval");

            // Test current status retrieval
            var statusTime = await MeasureExecutionTime(async () => 
                await GetAsync<Result>("time/status"));
            
            AssertPerformance(statusTime, TimeControllerThreshold, "Time Controller - Status Retrieval");

            // Test date range query
            var yesterday = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd");
            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var rangeTime = await MeasureExecutionTime(async () => 
                await GetAsync<Result>($"time/range?startDate={yesterday}&endDate={today}"));
            
            AssertPerformance(rangeTime, TimeControllerThreshold, "Time Controller - Date Range Query");
        }

        [Fact]
        public async Task TestLocationControllerPerformance()
        {
            // Test location batch upload
            var locations = CreateTestLocationBatch(10);
            var batchRequest = new LocationBatchRequest { Locations = locations };
            
            var batchTime = await MeasureExecutionTime(async () => 
                await PostAsync<LocationBatchRequest, Result>("location/batch", batchRequest));
            
            AssertPerformance(batchTime, LocationControllerThreshold, "Location Controller - Batch Upload");

            // Test current location retrieval
            var currentLocationTime = await MeasureExecutionTime(async () => 
                await GetAsync<Result>("location/current"));
            
            AssertPerformance(currentLocationTime, LocationControllerThreshold, "Location Controller - Current Location");

            // Test location history retrieval with pagination
            var historyTime = await MeasureExecutionTime(async () => 
                await GetAsync<Result>("location/history?page=1&pageSize=10"));
            
            AssertPerformance(historyTime, LocationControllerThreshold, "Location Controller - History Retrieval");
        }

        [Fact]
        public async Task TestPatrolControllerPerformance()
        {
            // Test patrol locations retrieval
            var locationsTime = await MeasureExecutionTime(async () => 
                await GetAsync<Result>("patrol/locations"));
            
            AssertPerformance(locationsTime, PatrolControllerThreshold, "Patrol Controller - Locations Retrieval");

            // Test checkpoint retrieval for a location
            var checkpointsTime = await MeasureExecutionTime(async () => 
                await GetAsync<Result>($"patrol/checkpoints?locationId={TestConstants.TestLocationId}"));
            
            AssertPerformance(checkpointsTime, PatrolControllerThreshold, "Patrol Controller - Checkpoints Retrieval");

            // Test checkpoint verification
            var verificationRequest = new CheckpointVerificationRequest
            {
                CheckpointId = TestConstants.TestCheckpointId,
                Timestamp = DateTime.UtcNow,
                Location = CreateTestLocationModel()
            };
            
            var verificationTime = await MeasureExecutionTime(async () => 
                await PostAsync<CheckpointVerificationRequest, Result>("patrol/verify", verificationRequest));
            
            AssertPerformance(verificationTime, PatrolControllerThreshold, "Patrol Controller - Checkpoint Verification");

            // Test patrol status retrieval
            var statusTime = await MeasureExecutionTime(async () => 
                await GetAsync<Result>($"patrol/status?locationId={TestConstants.TestLocationId}"));
            
            AssertPerformance(statusTime, PatrolControllerThreshold, "Patrol Controller - Status Retrieval");
        }

        [Fact]
        public async Task TestReportControllerPerformance()
        {
            // Test report creation
            var reportRequest = new ReportRequest
            {
                Text = TestConstants.TestReportText,
                Timestamp = DateTime.UtcNow,
                Location = new LocationModel 
                { 
                    Latitude = TestConstants.TestLatitude, 
                    Longitude = TestConstants.TestLongitude 
                }
            };
            
            var createTime = await MeasureExecutionTime(async () => 
                await PostAsync<ReportRequest, Result>("reports", reportRequest));
            
            AssertPerformance(createTime, ReportControllerThreshold, "Report Controller - Create Report");

            // Test reports retrieval
            var retrieveTime = await MeasureExecutionTime(async () => 
                await GetAsync<Result>("reports"));
            
            AssertPerformance(retrieveTime, ReportControllerThreshold, "Report Controller - Retrieve Reports");

            // Test paginated reports retrieval
            var paginatedTime = await MeasureExecutionTime(async () => 
                await GetAsync<Result>("reports?page=1&pageSize=10"));
            
            AssertPerformance(paginatedTime, ReportControllerThreshold, "Report Controller - Paginated Reports");
        }

        [Fact]
        public async Task TestPhotoControllerPerformance()
        {
            // Note: We can't easily test photo upload in this performance test due to multipart form data,
            // but we can test the retrieval endpoints

            // Test photos retrieval
            var retrieveTime = await MeasureExecutionTime(async () => 
                await GetAsync<Result>("photos"));
            
            AssertPerformance(retrieveTime, PhotoControllerThreshold, "Photo Controller - Retrieve Photos");

            // Test paginated photos retrieval
            var paginatedTime = await MeasureExecutionTime(async () => 
                await GetAsync<Result>("photos?page=1&pageSize=10"));
            
            AssertPerformance(paginatedTime, PhotoControllerThreshold, "Photo Controller - Paginated Photos");
        }

        [Fact]
        public async Task TestControllerConcurrentPerformance()
        {
            // Test concurrent requests to time history endpoint
            Func<Task<Result>> getTimeHistory = async () => await GetAsync<Result>("time/history");
            var timeResults = await RunConcurrentRequests(getTimeHistory, 10);
            
            var timeExecutionTimes = timeResults.ConvertAll(r => r.elapsedMilliseconds);
            var timeStats = GetPerformanceStatistics(timeExecutionTimes);
            
            AssertPerformance(timeExecutionTimes, TimeControllerThreshold, 
                "Time Controller - Concurrent History Requests");

            // Test concurrent requests to patrol locations endpoint
            Func<Task<Result>> getPatrolLocations = async () => await GetAsync<Result>("patrol/locations");
            var patrolResults = await RunConcurrentRequests(getPatrolLocations, 10);
            
            var patrolExecutionTimes = patrolResults.ConvertAll(r => r.elapsedMilliseconds);
            var patrolStats = GetPerformanceStatistics(patrolExecutionTimes);
            
            AssertPerformance(patrolExecutionTimes, PatrolControllerThreshold, 
                "Patrol Controller - Concurrent Locations Requests");

            // Test concurrent requests to reports endpoint
            Func<Task<Result>> getReports = async () => await GetAsync<Result>("reports");
            var reportResults = await RunConcurrentRequests(getReports, 10);
            
            var reportExecutionTimes = reportResults.ConvertAll(r => r.elapsedMilliseconds);
            var reportStats = GetPerformanceStatistics(reportExecutionTimes);
            
            AssertPerformance(reportExecutionTimes, ReportControllerThreshold, 
                "Report Controller - Concurrent Reports Requests");
        }

        /// <summary>
        /// Creates a test location model with predefined coordinates
        /// </summary>
        /// <returns>A location model with test coordinates</returns>
        private LocationModel CreateTestLocationModel()
        {
            return new LocationModel
            {
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude,
                Accuracy = TestConstants.TestAccuracy
            };
        }

        /// <summary>
        /// Creates a test batch of location models
        /// </summary>
        /// <param name="count">The number of location models to create</param>
        /// <returns>A list of location models with slightly varied coordinates</returns>
        private List<LocationModel> CreateTestLocationBatch(int count)
        {
            var locations = new List<LocationModel>();
            
            for (int i = 0; i < count; i++)
            {
                // Create slight variations in coordinates
                var latitude = TestConstants.TestLatitude + (i * 0.0001);
                var longitude = TestConstants.TestLongitude + (i * 0.0001);
                
                locations.Add(new LocationModel
                {
                    Latitude = latitude,
                    Longitude = longitude,
                    Accuracy = TestConstants.TestAccuracy,
                    Timestamp = DateTime.UtcNow.AddMinutes(-i) // Stagger timestamps
                });
            }
            
            return locations;
        }
    }
}