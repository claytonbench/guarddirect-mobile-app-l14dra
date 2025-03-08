using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection; // Version 8.0.0
using Xunit; // Version 2.4.2
using Xunit.Abstractions; // Version 2.0.3
using FluentAssertions; // Version 6.10.0
using BenchmarkDotNet.Attributes; // Version 0.13.5
using SecurityPatrol.API.PerformanceTests.Setup;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.Application.Services;
using SecurityPatrol.Core.Models;
using SecurityPatrol.TestCommon.Constants;
using SecurityPatrol.TestCommon.Data;

namespace SecurityPatrol.API.PerformanceTests.Services
{
    /// <summary>
    /// Performance tests for service layer components in the Security Patrol API, measuring execution time of critical service methods against defined thresholds.
    /// </summary>
    [public]
    public class ServicePerformanceTests : PerformanceTestBase
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly ILocationService _locationService;
        private readonly IPatrolService _patrolService;
        private readonly ITimeRecordService _timeRecordService;
        private readonly IReportService _reportService;
        private readonly IPhotoService _photoService;
        private readonly double ServicePerformanceThreshold;

        /// <summary>
        /// Initializes a new instance of the ServicePerformanceTests class with the test factory and output helper.
        /// </summary>
        /// <param name="outputHelper">The test output helper for logging test results.</param>
        public ServicePerformanceTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
            // Call base constructor with outputHelper
            // Get service instances from the test server's service provider
            _authenticationService = Factory.Services.GetRequiredService<IAuthenticationService>();
            _locationService = Factory.Services.GetRequiredService<ILocationService>();
            _patrolService = Factory.Services.GetRequiredService<IPatrolService>();
            _timeRecordService = Factory.Services.GetRequiredService<ITimeRecordService>();
            _reportService = Factory.Services.GetRequiredService<IReportService>();
            _photoService = Factory.Services.GetRequiredService<IPhotoService>();

            // Set ServicePerformanceThreshold to 150 milliseconds for service operations
            ServicePerformanceThreshold = 150;
        }

        /// <summary>
        /// Tests the performance of the RequestVerificationCode method in the AuthenticationService.
        /// </summary>
        [public]
        [async]
        [Fact]
        public async Task AuthenticationService_RequestVerificationCode_Performance()
        {
            // Create an AuthenticationRequest with test phone number
            var request = new AuthenticationRequest { PhoneNumber = TestConstants.TestPhoneNumber };

            // Measure the execution time of _authenticationService.RequestVerificationCodeAsync(request)
            double executionTime = await MeasureExecutionTime(() => _authenticationService.RequestVerificationCodeAsync(request));

            // Assert that the execution time is within the ServicePerformanceThreshold
            AssertPerformance(executionTime, ServicePerformanceThreshold, "AuthenticationService.RequestVerificationCodeAsync");

            // Log the performance result
        }

        /// <summary>
        /// Tests the performance of the VerifyCode method in the AuthenticationService.
        /// </summary>
        [public]
        [async]
        [Fact]
        public async Task AuthenticationService_VerifyCode_Performance()
        {
            // Create a VerificationRequest with test phone number and verification code
            var request = new VerificationRequest { PhoneNumber = TestConstants.TestPhoneNumber, Code = TestConstants.TestVerificationCode };

            // Measure the execution time of _authenticationService.VerifyCodeAsync(request)
            double executionTime = await MeasureExecutionTime(() => _authenticationService.VerifyCodeAsync(request));

            // Assert that the execution time is within the ServicePerformanceThreshold
            AssertPerformance(executionTime, ServicePerformanceThreshold, "AuthenticationService.VerifyCodeAsync");

            // Log the performance result
        }

        /// <summary>
        /// Tests the performance of the RefreshToken method in the AuthenticationService.
        /// </summary>
        [public]
        [async]
        [Fact]
        public async Task AuthenticationService_RefreshToken_Performance()
        {
            // Measure the execution time of _authenticationService.RefreshTokenAsync(TestConstants.TestAuthToken)
            double executionTime = await MeasureExecutionTime(() => _authenticationService.RefreshTokenAsync(TestConstants.TestAuthToken));

            // Assert that the execution time is within the ServicePerformanceThreshold
            AssertPerformance(executionTime, ServicePerformanceThreshold, "AuthenticationService.RefreshTokenAsync");

            // Log the performance result
        }

        /// <summary>
        /// Tests the performance of the ValidateToken method in the AuthenticationService.
        /// </summary>
        [public]
        [async]
        [Fact]
        public async Task AuthenticationService_ValidateToken_Performance()
        {
            // Measure the execution time of _authenticationService.ValidateTokenAsync(TestConstants.TestAuthToken)
            double executionTime = await MeasureExecutionTime(() => _authenticationService.ValidateTokenAsync(TestConstants.TestAuthToken));

            // Assert that the execution time is within the ServicePerformanceThreshold / 2
            AssertPerformance(executionTime, ServicePerformanceThreshold / 2, "AuthenticationService.ValidateTokenAsync");

            // Log the performance result
        }

        /// <summary>
        /// Tests the performance of the ProcessLocationBatch method in the LocationService.
        /// </summary>
        [public]
        [async]
        [Fact]
        public async Task LocationService_ProcessLocationBatch_Performance()
        {
            // Create a LocationBatchRequest with 50 test location points
            var request = CreateTestLocationBatchRequest(50);

            // Measure the execution time of _locationService.ProcessLocationBatchAsync(request)
            double executionTime = await MeasureExecutionTime(() => _locationService.ProcessLocationBatchAsync(request));

            // Assert that the execution time is within the ServicePerformanceThreshold * 2
            AssertPerformance(executionTime, ServicePerformanceThreshold * 2, "LocationService.ProcessLocationBatchAsync");

            // Log the performance result
        }

        /// <summary>
        /// Tests the performance of the GetLocationHistory method in the LocationService.
        /// </summary>
        [public]
        [async]
        [Fact]
        public async Task LocationService_GetLocationHistory_Performance()
        {
            // Define a time range (start time = 24 hours ago, end time = now)
            DateTime endTime = DateTime.UtcNow;
            DateTime startTime = endTime.AddHours(-24);

            // Measure the execution time of _locationService.GetLocationHistoryAsync(TestConstants.TestUserId, startTime, endTime)
            double executionTime = await MeasureExecutionTime(() => _locationService.GetLocationHistoryAsync(TestConstants.TestUserId, startTime, endTime));

            // Assert that the execution time is within the ServicePerformanceThreshold
            AssertPerformance(executionTime, ServicePerformanceThreshold, "LocationService.GetLocationHistoryAsync");

            // Log the performance result
        }

        /// <summary>
        /// Tests the performance of the GetLatestLocation method in the LocationService.
        /// </summary>
        [public]
        [async]
        [Fact]
        public async Task LocationService_GetLatestLocation_Performance()
        {
            // Measure the execution time of _locationService.GetLatestLocationAsync(TestConstants.TestUserId)
            double executionTime = await MeasureExecutionTime(() => _locationService.GetLatestLocationAsync(TestConstants.TestUserId));

            // Assert that the execution time is within the ServicePerformanceThreshold / 2
            AssertPerformance(executionTime, ServicePerformanceThreshold / 2, "LocationService.GetLatestLocationAsync");

            // Log the performance result
        }

        /// <summary>
        /// Tests the performance of the GetLocations method in the PatrolService.
        /// </summary>
        [public]
        [async]
        [Fact]
        public async Task PatrolService_GetLocations_Performance()
        {
            // Measure the execution time of _patrolService.GetLocationsAsync()
            double executionTime = await MeasureExecutionTime(() => _patrolService.GetLocationsAsync());

            // Assert that the execution time is within the ServicePerformanceThreshold
            AssertPerformance(executionTime, ServicePerformanceThreshold, "PatrolService.GetLocationsAsync");

            // Log the performance result
        }

        /// <summary>
        /// Tests the performance of the GetCheckpointsByLocationId method in the PatrolService.
        /// </summary>
        [public]
        [async]
        [Fact]
        public async Task PatrolService_GetCheckpointsByLocationId_Performance()
        {
            // Measure the execution time of _patrolService.GetCheckpointsByLocationIdAsync(1)
            double executionTime = await MeasureExecutionTime(() => _patrolService.GetCheckpointsByLocationIdAsync(1));

            // Assert that the execution time is within the ServicePerformanceThreshold
            AssertPerformance(executionTime, ServicePerformanceThreshold, "PatrolService.GetCheckpointsByLocationIdAsync");

            // Log the performance result
        }

        /// <summary>
        /// Tests the performance of the VerifyCheckpoint method in the PatrolService.
        /// </summary>
        [public]
        [async]
        [Fact]
        public async Task PatrolService_VerifyCheckpoint_Performance()
        {
            // Create a CheckpointVerificationRequest with test data
            var request = CreateTestCheckpointVerificationRequest();

            // Measure the execution time of _patrolService.VerifyCheckpointAsync(request, TestConstants.TestUserId)
            double executionTime = await MeasureExecutionTime(() => _patrolService.VerifyCheckpointAsync(request, TestConstants.TestUserId));

            // Assert that the execution time is within the ServicePerformanceThreshold
            AssertPerformance(executionTime, ServicePerformanceThreshold, "PatrolService.VerifyCheckpointAsync");

            // Log the performance result
        }

        /// <summary>
        /// Tests the performance of the GetNearbyCheckpoints method in the PatrolService.
        /// </summary>
        [public]
        [async]
        [Fact]
        public async Task PatrolService_GetNearbyCheckpoints_Performance()
        {
            // Define test coordinates and radius
            double latitude = TestConstants.TestLatitude;
            double longitude = TestConstants.TestLongitude;
            double radius = 100;

            // Measure the execution time of _patrolService.GetNearbyCheckpointsAsync(latitude, longitude, radius)
            double executionTime = await MeasureExecutionTime(() => _patrolService.GetNearbyCheckpointsAsync(latitude, longitude, radius));

            // Assert that the execution time is within the ServicePerformanceThreshold
            AssertPerformance(executionTime, ServicePerformanceThreshold, "PatrolService.GetNearbyCheckpointsAsync");

            // Log the performance result
        }

        /// <summary>
        /// Tests the performance of the CreateTimeRecord method in the TimeRecordService.
        /// </summary>
        [public]
        [async]
        [Fact]
        public async Task TimeRecordService_CreateTimeRecord_Performance()
        {
            // Create a TimeRecordRequest with test data
            var request = CreateTestTimeRecordRequest();

            // Measure the execution time of _timeRecordService.CreateTimeRecordAsync(request, TestConstants.TestUserId)
            double executionTime = await MeasureExecutionTime(() => _timeRecordService.CreateTimeRecordAsync(request, TestConstants.TestUserId));

            // Assert that the execution time is within the ServicePerformanceThreshold
            AssertPerformance(executionTime, ServicePerformanceThreshold, "TimeRecordService.CreateTimeRecordAsync");

            // Log the performance result
        }

        /// <summary>
        /// Tests the performance of the GetTimeRecords method in the TimeRecordService.
        /// </summary>
        [public]
        [async]
        [Fact]
        public async Task TimeRecordService_GetTimeRecords_Performance()
        {
            // Measure the execution time of _timeRecordService.GetTimeRecordsAsync()
            double executionTime = await MeasureExecutionTime(() => _timeRecordService.GetTimeRecordsAsync());

            // Assert that the execution time is within the ServicePerformanceThreshold
            AssertPerformance(executionTime, ServicePerformanceThreshold, "TimeRecordService.GetTimeRecordsAsync");

            // Log the performance result
        }

        /// <summary>
        /// Tests the performance of the GetTimeRecordsByUserId method in the TimeRecordService.
        /// </summary>
        [public]
        [async]
        [Fact]
        public async Task TimeRecordService_GetTimeRecordsByUserId_Performance()
        {
            // Measure the execution time of _timeRecordService.GetTimeRecordsByUserIdAsync(TestConstants.TestUserId)
            double executionTime = await MeasureExecutionTime(() => _timeRecordService.GetTimeRecordsByUserIdAsync(TestConstants.TestUserId));

            // Assert that the execution time is within the ServicePerformanceThreshold
            AssertPerformance(executionTime, ServicePerformanceThreshold, "TimeRecordService.GetTimeRecordsByUserIdAsync");

            // Log the performance result
        }

        /// <summary>
        /// Tests the performance of the CreateReport method in the ReportService.
        /// </summary>
        [public]
        [async]
        [Fact]
        public async Task ReportService_CreateReport_Performance()
        {
            // Create a ReportRequest with test data
            var request = CreateTestReportRequest();

            // Measure the execution time of _reportService.CreateReportAsync(request, TestConstants.TestUserId)
            double executionTime = await MeasureExecutionTime(() => _reportService.CreateReportAsync(request, TestConstants.TestUserId));

            // Assert that the execution time is within the ServicePerformanceThreshold
            AssertPerformance(executionTime, ServicePerformanceThreshold, "ReportService.CreateReportAsync");

            // Log the performance result
        }

        /// <summary>
        /// Tests the performance of the GetReports method in the ReportService.
        /// </summary>
        [public]
        [async]
        [Fact]
        public async Task ReportService_GetReports_Performance()
        {
            // Measure the execution time of _reportService.GetReportsAsync(TestConstants.TestUserId)
            double executionTime = await MeasureExecutionTime(() => _reportService.GetReportsAsync(TestConstants.TestUserId));

            // Assert that the execution time is within the ServicePerformanceThreshold
            AssertPerformance(executionTime, ServicePerformanceThreshold, "ReportService.GetReportsAsync");

            // Log the performance result
        }

        /// <summary>
        /// Tests the performance of the UploadPhoto method in the PhotoService.
        /// </summary>
        [public]
        [async]
        [Fact]
        public async Task PhotoService_UploadPhoto_Performance()
        {
            // Create a PhotoUploadRequest with test image data
            var request = CreateTestPhotoUploadRequest();

            // Measure the execution time of _photoService.UploadPhotoAsync(request, TestConstants.TestUserId)
            double executionTime = await MeasureExecutionTime(() => {
                using (var stream = new System.IO.MemoryStream(MockDataGenerator.GenerateTestImage().Result.ToArray()))
                {
                    return _photoService.UploadPhotoAsync(request, stream, "image/jpeg");
                }
            });

            // Assert that the execution time is within the ServicePerformanceThreshold * 2
            AssertPerformance(executionTime, ServicePerformanceThreshold * 2, "PhotoService.UploadPhotoAsync");

            // Log the performance result
        }

        /// <summary>
        /// Tests the performance of the GetPhotos method in the PhotoService.
        /// </summary>
        [public]
        [async]
        [Fact]
        public async Task PhotoService_GetPhotos_Performance()
        {
            // Measure the execution time of _photoService.GetPhotosAsync(TestConstants.TestUserId)
            double executionTime = await MeasureExecutionTime(() => _photoService.GetPhotosAsync(TestConstants.TestUserId));

            // Assert that the execution time is within the ServicePerformanceThreshold
            AssertPerformance(executionTime, ServicePerformanceThreshold, "PhotoService.GetPhotosAsync");

            // Log the performance result
        }

        /// <summary>
        /// Tests the performance of the LocationService under concurrent load.
        /// </summary>
        [public]
        [async]
        [Fact]
        public async Task ConcurrentRequests_LocationBatch_Performance()
        {
            // Create a LocationBatchRequest with 10 test location points
            var request = CreateTestLocationBatchRequest(10);

            // Define a request function that calls _locationService.ProcessLocationBatchAsync
            Func<Task<LocationSyncResponse>> requestFunc = () => _locationService.ProcessLocationBatchAsync(request);

            // Run the request function concurrently with 10 parallel requests
            var results = await RunConcurrentRequests(requestFunc, 10);

            // Calculate performance statistics from the execution times
            var executionTimes = results.Select(r => r.elapsedMilliseconds).ToList();
            var stats = GetPerformanceStatistics(executionTimes);

            // Assert that the 95th percentile execution time is within the ServicePerformanceThreshold * 3
            AssertPerformance(executionTimes, ServicePerformanceThreshold * 3, "Concurrent LocationService.ProcessLocationBatchAsync");

            // Log the performance statistics
        }

        /// <summary>
        /// Tests the performance of the TimeRecordService under concurrent load.
        /// </summary>
        [public]
        [async]
        [Fact]
        public async Task ConcurrentRequests_TimeRecord_Performance()
        {
            // Create a TimeRecordRequest with test data
            var request = CreateTestTimeRecordRequest();

            // Define a request function that calls _timeRecordService.CreateTimeRecordAsync
            Func<Task<Result<TimeRecordResponse>>> requestFunc = () => _timeRecordService.CreateTimeRecordAsync(request, TestConstants.TestUserId);

            // Run the request function concurrently with 10 parallel requests
            var results = await RunConcurrentRequests(requestFunc, 10);

            // Calculate performance statistics from the execution times
            var executionTimes = results.Select(r => r.elapsedMilliseconds).ToList();
            var stats = GetPerformanceStatistics(executionTimes);

            // Assert that the 95th percentile execution time is within the ServicePerformanceThreshold * 2
            AssertPerformance(executionTimes, ServicePerformanceThreshold * 2, "Concurrent TimeRecordService.CreateTimeRecordAsync");

            // Log the performance statistics
        }

        /// <summary>
        /// Creates a test location batch request with the specified number of locations.
        /// </summary>
        /// <param name="count">The number of locations to create.</param>
        /// <returns>A location batch request with test data.</returns>
        [private]
        private LocationBatchRequest CreateTestLocationBatchRequest(int count)
        {
            // Use MockDataGenerator to generate a list of location models
            var locations = MockDataGenerator.GenerateLocationModels(count);

            // Create a new LocationBatchRequest with the generated locations
            var request = new LocationBatchRequest
            {
                UserId = TestConstants.TestUserId,
                Locations = locations
            };

            // Return the request
            return request;
        }

        /// <summary>
        /// Creates a test time record request with predefined values.
        /// </summary>
        /// <returns>A time record request with test data.</returns>
        [private]
        private TimeRecordRequest CreateTestTimeRecordRequest()
        {
            // Create a new TimeRecordRequest
            var request = new TimeRecordRequest();

            // Set Type to 'in'
            request.Type = "in";

            // Set Timestamp to DateTime.UtcNow
            request.Timestamp = DateTime.UtcNow;

            // Create a LocationModel with test coordinates
            var location = new LocationModel
            {
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude
            };

            // Set the Location property to the created LocationModel
            request.Location = location;

            // Return the request
            return request;
        }

        /// <summary>
        /// Creates a test checkpoint verification request with predefined values.
        /// </summary>
        /// <returns>A checkpoint verification request with test data.</returns>
        [private]
        private CheckpointVerificationRequest CreateTestCheckpointVerificationRequest()
        {
            // Create a new CheckpointVerificationRequest
            var request = new CheckpointVerificationRequest();

            // Set CheckpointId to 1
            request.CheckpointId = 1;

            // Set Timestamp to DateTime.UtcNow
            request.Timestamp = DateTime.UtcNow;

            // Create a LocationModel with test coordinates
            var location = new LocationModel
            {
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude
            };

            // Set the Location property to the created LocationModel
            request.Location = location;

            // Return the request
            return request;
        }

        /// <summary>
        /// Creates a test report request with predefined values.
        /// </summary>
        /// <returns>A report request with test data.</returns>
        [private]
        private ReportRequest CreateTestReportRequest()
        {
            // Create a new ReportRequest
            var request = new ReportRequest();

            // Set Text to a test report message
            request.Text = "Test report message";

            // Set Timestamp to DateTime.UtcNow
            request.Timestamp = DateTime.UtcNow;

            // Create a LocationModel with test coordinates
            var location = new LocationModel
            {
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude
            };

            // Set the Location property to the created LocationModel
            request.Location = location;

            // Return the request
            return request;
        }

        /// <summary>
        /// Creates a test photo upload request with predefined values.
        /// </summary>
        /// <returns>A photo upload request with test data.</returns>
        [private]
        private PhotoUploadRequest CreateTestPhotoUploadRequest()
        {
            // Create a new PhotoUploadRequest
            var request = new PhotoUploadRequest();

            // Use MockDataGenerator to generate test image data
            // Set ImageData to the generated image data
            // Set Timestamp to DateTime.UtcNow
            request.Timestamp = DateTime.UtcNow;

            // Create a LocationModel with test coordinates
            var location = new LocationModel
            {
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude
            };

            // Set the Location property to the created LocationModel
            // Return the request
            return request;
        }
    }
}