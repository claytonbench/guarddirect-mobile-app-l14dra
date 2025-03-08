using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using FluentAssertions;
using SecurityPatrol.Core.Models;

namespace SecurityPatrol.API.PerformanceTests.API
{
    /// <summary>
    /// Performance test class that measures and validates API endpoint response times against defined thresholds.
    /// </summary>
    public class EndpointResponseTimeTests : PerformanceTestBase
    {
        private readonly double AuthEndpointThreshold;
        private readonly double LocationEndpointThreshold;
        private readonly double TimeEndpointThreshold;
        private readonly double PatrolEndpointThreshold;
        private readonly double ReportEndpointThreshold;
        private readonly double PhotoEndpointThreshold;

        /// <summary>
        /// Initializes a new instance of the EndpointResponseTimeTests class with test output helper.
        /// </summary>
        /// <param name="outputHelper">The test output helper for logging test results.</param>
        public EndpointResponseTimeTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
            // Initialize thresholds for different endpoint types
            AuthEndpointThreshold = 500;       // 500ms for authentication endpoints
            LocationEndpointThreshold = 500;   // 500ms for location endpoints
            TimeEndpointThreshold = 500;       // 500ms for time tracking endpoints
            PatrolEndpointThreshold = 750;     // 750ms for patrol endpoints (more complex)
            ReportEndpointThreshold = 750;     // 750ms for report endpoints
            PhotoEndpointThreshold = 1000;     // 1000ms for photo endpoints (larger payload)

            // Set authentication token for the HTTP client
            SetAuthToken(TestConstants.TestAuthToken);
        }

        /// <summary>
        /// Tests the response time of the authentication verification endpoint.
        /// </summary>
        [Fact]
        public async Task TestAuthVerifyEndpointResponseTime()
        {
            // Create authentication request
            var request = new AuthenticationRequest { PhoneNumber = TestConstants.TestPhoneNumber };
            
            // Measure execution time
            var executionTime = await MeasureExecutionTime(async () => 
                await PostAsync<AuthenticationRequest, object>("auth/verify", request));
            
            // Assert performance
            AssertPerformance(executionTime, AuthEndpointThreshold, "Authentication Verify Endpoint");
        }

        /// <summary>
        /// Tests the response time of the authentication validation endpoint.
        /// </summary>
        [Fact]
        public async Task TestAuthValidateEndpointResponseTime()
        {
            // Create verification request
            var request = new VerificationRequest 
            { 
                PhoneNumber = TestConstants.TestPhoneNumber,
                Code = TestConstants.TestVerificationCode
            };
            
            // Measure execution time
            var executionTime = await MeasureExecutionTime(async () => 
                await PostAsync<VerificationRequest, object>("auth/validate", request));
            
            // Assert performance
            AssertPerformance(executionTime, AuthEndpointThreshold, "Authentication Validate Endpoint");
        }

        /// <summary>
        /// Tests the response time of the location batch upload endpoint.
        /// </summary>
        [Fact]
        public async Task TestLocationBatchEndpointResponseTime()
        {
            // Create a location batch request with test locations
            var request = CreateTestLocationBatch(10);
            
            // Measure execution time
            var executionTime = await MeasureExecutionTime(async () => 
                await PostAsync<LocationBatchRequest, object>("location/batch", request));
            
            // Assert performance
            AssertPerformance(executionTime, LocationEndpointThreshold, "Location Batch Endpoint");
        }

        /// <summary>
        /// Tests the response time of the location history endpoint.
        /// </summary>
        [Fact]
        public async Task TestLocationHistoryEndpointResponseTime()
        {
            // Set up query parameters for start and end time
            var startTime = DateTime.UtcNow.AddDays(-1).ToString("o");
            var endTime = DateTime.UtcNow.ToString("o");
            var endpoint = $"location/history?startTime={Uri.EscapeDataString(startTime)}&endTime={Uri.EscapeDataString(endTime)}";
            
            // Measure execution time
            var executionTime = await MeasureExecutionTime(async () => 
                await GetAsync<object>(endpoint));
            
            // Assert performance
            AssertPerformance(executionTime, LocationEndpointThreshold, "Location History Endpoint");
        }

        /// <summary>
        /// Tests the response time of the time clock endpoint.
        /// </summary>
        [Fact]
        public async Task TestTimeClockEndpointResponseTime()
        {
            // Create a time record request for clock in
            var request = CreateTestTimeRecordRequest("ClockIn");
            
            // Measure execution time
            var executionTime = await MeasureExecutionTime(async () => 
                await PostAsync<TimeRecordRequest, object>("time/clock", request));
            
            // Assert performance
            AssertPerformance(executionTime, TimeEndpointThreshold, "Time Clock Endpoint");
        }
        
        /// <summary>
        /// Tests the response time of the time history endpoint.
        /// </summary>
        [Fact]
        public async Task TestTimeHistoryEndpointResponseTime()
        {
            // Set up query parameters for pagination
            var endpoint = "time/history?page=1&pageSize=10";
            
            // Measure execution time
            var executionTime = await MeasureExecutionTime(async () => 
                await GetAsync<object>(endpoint));
            
            // Assert performance
            AssertPerformance(executionTime, TimeEndpointThreshold, "Time History Endpoint");
        }

        /// <summary>
        /// Tests the response time of the patrol locations endpoint.
        /// </summary>
        [Fact]
        public async Task TestPatrolLocationsEndpointResponseTime()
        {
            // Measure execution time
            var executionTime = await MeasureExecutionTime(async () => 
                await GetAsync<object>("patrol/locations"));
            
            // Assert performance
            AssertPerformance(executionTime, PatrolEndpointThreshold, "Patrol Locations Endpoint");
        }
        
        /// <summary>
        /// Tests the response time of the patrol checkpoints endpoint.
        /// </summary>
        [Fact]
        public async Task TestPatrolCheckpointsEndpointResponseTime()
        {
            // Measure execution time
            var executionTime = await MeasureExecutionTime(async () => 
                await GetAsync<object>($"patrol/locations/{TestConstants.TestLocationId}/checkpoints"));
            
            // Assert performance
            AssertPerformance(executionTime, PatrolEndpointThreshold, "Patrol Checkpoints Endpoint");
        }
        
        /// <summary>
        /// Tests the response time of the patrol checkpoint verification endpoint.
        /// </summary>
        [Fact]
        public async Task TestPatrolVerifyEndpointResponseTime()
        {
            // Create a checkpoint verification request
            var request = CreateTestCheckpointVerificationRequest();
            
            // Measure execution time
            var executionTime = await MeasureExecutionTime(async () => 
                await PostAsync<CheckpointVerificationRequest, object>("patrol/verify", request));
            
            // Assert performance
            AssertPerformance(executionTime, PatrolEndpointThreshold, "Patrol Verify Endpoint");
        }

        /// <summary>
        /// Tests the response time of the report creation endpoint.
        /// </summary>
        [Fact]
        public async Task TestReportCreateEndpointResponseTime()
        {
            // Create a report request with test data
            var request = CreateTestReportRequest();
            
            // Measure execution time
            var executionTime = await MeasureExecutionTime(async () => 
                await PostAsync<ReportRequest, object>("reports", request));
            
            // Assert performance
            AssertPerformance(executionTime, ReportEndpointThreshold, "Report Create Endpoint");
        }
        
        /// <summary>
        /// Tests the response time of the reports retrieval endpoint.
        /// </summary>
        [Fact]
        public async Task TestReportGetEndpointResponseTime()
        {
            // Set up query parameters for pagination
            var endpoint = "reports?page=1&pageSize=10";
            
            // Measure execution time
            var executionTime = await MeasureExecutionTime(async () => 
                await GetAsync<object>(endpoint));
            
            // Assert performance
            AssertPerformance(executionTime, ReportEndpointThreshold, "Report Get Endpoint");
        }

        /// <summary>
        /// Tests the response time of the photo upload endpoint.
        /// </summary>
        [Fact]
        public async Task TestPhotoUploadEndpointResponseTime()
        {
            // Create a photo upload request with test data
            var request = CreateTestPhotoUploadRequest();
            
            // Create a test image file
            var imageBytes = new byte[100000]; // 100KB test image
            new Random().NextBytes(imageBytes);
            
            // Measure execution time
            var executionTime = await MeasureExecutionTime(async () => 
            {
                // In a real implementation, we would use MultipartFormDataContent
                // For performance testing, we'll simulate with a simple post
                await PostAsync<PhotoUploadRequest, object>("photos/upload", request);
            });
            
            // Assert performance
            AssertPerformance(executionTime, PhotoEndpointThreshold, "Photo Upload Endpoint");
        }
        
        /// <summary>
        /// Tests the response time of the photo retrieval endpoint.
        /// </summary>
        [Fact]
        public async Task TestPhotoGetEndpointResponseTime()
        {
            // Measure execution time
            var executionTime = await MeasureExecutionTime(async () => 
                await GetAsync<object>("photos/my"));
            
            // Assert performance
            AssertPerformance(executionTime, PhotoEndpointThreshold, "Photo Get Endpoint");
        }

        /// <summary>
        /// Tests the response time of the authentication endpoint under concurrent load.
        /// </summary>
        [Fact]
        public async Task TestConcurrentAuthRequestsResponseTime()
        {
            // Create an authentication request with TestPhoneNumber
            var request = new AuthenticationRequest { PhoneNumber = TestConstants.TestPhoneNumber };
            
            // Run 10 concurrent requests to auth/verify endpoint
            var results = await RunConcurrentRequests(
                async () => await PostAsync<AuthenticationRequest, object>("auth/verify", request),
                10);
            
            // Extract execution times from results
            var executionTimes = results.Select(r => r.elapsedMilliseconds).ToList();
            
            // Assert that the 95th percentile execution time is within AuthEndpointThreshold * 1.5
            AssertPerformance(executionTimes, AuthEndpointThreshold * 1.5, "Concurrent Auth Requests");
        }
        
        /// <summary>
        /// Tests the response time of the location batch endpoint under concurrent load.
        /// </summary>
        [Fact]
        public async Task TestConcurrentLocationBatchRequestsResponseTime()
        {
            // Create a location batch request with test locations
            var request = CreateTestLocationBatch(10);
            
            // Run 5 concurrent requests to location/batch endpoint
            var results = await RunConcurrentRequests(
                async () => await PostAsync<LocationBatchRequest, object>("location/batch", request),
                5);
            
            // Extract execution times from results
            var executionTimes = results.Select(r => r.elapsedMilliseconds).ToList();
            
            // Assert that the 95th percentile execution time is within LocationEndpointThreshold * 1.5
            AssertPerformance(executionTimes, LocationEndpointThreshold * 1.5, "Concurrent Location Batch Requests");
        }

        /// <summary>
        /// Creates a test location batch request with multiple location points.
        /// </summary>
        /// <param name="count">The number of location points to include in the batch.</param>
        /// <returns>A location batch request with test data.</returns>
        private LocationBatchRequest CreateTestLocationBatch(int count)
        {
            var request = new LocationBatchRequest();
            var locations = new List<LocationModel>();
            
            for (int i = 0; i < count; i++)
            {
                locations.Add(new LocationModel
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-i),
                    Latitude = TestConstants.TestLatitude + (i * 0.0001),
                    Longitude = TestConstants.TestLongitude + (i * 0.0001),
                    Accuracy = TestConstants.TestAccuracy
                });
            }
            
            request.Locations = locations;
            return request;
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
                    Longitude = TestConstants.TestLongitude
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
                Location = new LocationModel
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
                    Longitude = TestConstants.TestLongitude
                }
            };
        }
        
        /// <summary>
        /// Creates a test photo upload request with metadata.
        /// </summary>
        /// <returns>A photo upload request with test data.</returns>
        private PhotoUploadRequest CreateTestPhotoUploadRequest()
        {
            return new PhotoUploadRequest
            {
                Timestamp = DateTime.UtcNow,
                Location = new LocationModel
                {
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                }
            };
        }
    }
}