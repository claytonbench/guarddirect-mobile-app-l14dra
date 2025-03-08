using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.IO;
using Moq;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.Core.Models;
using SecurityPatrol.UnitTests.Helpers;

namespace SecurityPatrol.UnitTests.Helpers
{
    /// <summary>
    /// Static class providing factory methods to create mock service implementations for unit testing.
    /// </summary>
    public static class MockServices
    {
        /// <summary>
        /// Private constructor to prevent instantiation of static class.
        /// </summary>
        private MockServices() { }

        /// <summary>
        /// Creates a mock implementation of IAuthenticationService with predefined test data and behavior.
        /// </summary>
        /// <returns>A configured mock of IAuthenticationService.</returns>
        public static Mock<IAuthenticationService> CreateMockAuthenticationService()
        {
            var mockAuthService = new Mock<IAuthenticationService>();
            
            // Setup RequestVerificationCodeAsync to return a verification ID
            mockAuthService.Setup(s => s.RequestVerificationCodeAsync(It.IsAny<AuthenticationRequest>()))
                .ReturnsAsync("verification-123");
                
            // Setup VerifyCodeAsync to return a successful authentication response with token
            mockAuthService.Setup(s => s.VerifyCodeAsync(It.IsAny<VerificationRequest>()))
                .ReturnsAsync(new AuthenticationResponse 
                { 
                    Token = "test-jwt-token", 
                    ExpiresAt = DateTime.UtcNow.AddHours(1) 
                });
                
            // Setup RefreshTokenAsync to return a refreshed authentication response
            mockAuthService.Setup(s => s.RefreshTokenAsync(It.IsAny<string>()))
                .ReturnsAsync(new AuthenticationResponse 
                { 
                    Token = "refreshed-jwt-token", 
                    ExpiresAt = DateTime.UtcNow.AddHours(1) 
                });
                
            // Setup GetUserByPhoneNumberAsync to return a user by phone number from test data
            mockAuthService.Setup(s => s.GetUserByPhoneNumberAsync(It.IsAny<string>()))
                .Returns<string>(phoneNumber => Task.FromResult(TestData.GetTestUserByPhoneNumber(phoneNumber)));
                
            // Setup ValidateTokenAsync to return true for valid tokens
            mockAuthService.Setup(s => s.ValidateTokenAsync(It.IsAny<string>()))
                .ReturnsAsync(true);
                
            return mockAuthService;
        }
        
        /// <summary>
        /// Creates a mock implementation of ILocationService with predefined test data and behavior.
        /// </summary>
        /// <returns>A configured mock of ILocationService.</returns>
        public static Mock<ILocationService> CreateMockLocationService()
        {
            var mockLocationService = new Mock<ILocationService>();
            
            // Setup ProcessLocationBatchAsync to return a response with synced IDs
            mockLocationService.Setup(s => s.ProcessLocationBatchAsync(It.IsAny<LocationBatchRequest>()))
                .ReturnsAsync(new LocationSyncResponse
                {
                    SyncedIds = new List<int> { 1, 2, 3 },
                    FailedIds = new List<int>()
                });
                
            // Setup GetLocationHistoryAsync to return location records for a user within a time range
            mockLocationService.Setup(s => s.GetLocationHistoryAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns<string, DateTime, DateTime>((userId, startTime, endTime) => {
                    var locationRecords = TestData.GetTestLocationRecords()
                        .Where(l => l.UserId == userId && l.Timestamp >= startTime && l.Timestamp <= endTime)
                        .Select(l => new LocationModel
                        {
                            Id = l.Id,
                            Latitude = l.Latitude,
                            Longitude = l.Longitude,
                            Accuracy = l.Accuracy,
                            Timestamp = l.Timestamp,
                            IsSynced = l.IsSynced,
                            RemoteId = l.RemoteId
                        })
                        .ToList();
                    return Task.FromResult(locationRecords);
                });
                
            // Setup GetLatestLocationAsync to return the most recent location for a user
            mockLocationService.Setup(s => s.GetLatestLocationAsync(It.IsAny<string>()))
                .Returns<string>(userId => {
                    var latestLocation = TestData.GetTestLocationRecords()
                        .Where(l => l.UserId == userId)
                        .OrderByDescending(l => l.Timestamp)
                        .FirstOrDefault();
                    
                    if (latestLocation == null)
                        return Task.FromResult<LocationModel>(null);
                        
                    return Task.FromResult(new LocationModel
                    {
                        Id = latestLocation.Id,
                        Latitude = latestLocation.Latitude,
                        Longitude = latestLocation.Longitude,
                        Accuracy = latestLocation.Accuracy,
                        Timestamp = latestLocation.Timestamp,
                        IsSynced = latestLocation.IsSynced,
                        RemoteId = latestLocation.RemoteId
                    });
                });
                
            // Setup GetLocationsByUserIdAsync to return recent locations for a user
            mockLocationService.Setup(s => s.GetLocationsByUserIdAsync(It.IsAny<string>(), It.IsAny<int>()))
                .Returns<string, int>((userId, limit) => {
                    var locations = TestData.GetTestLocationRecords()
                        .Where(l => l.UserId == userId)
                        .OrderByDescending(l => l.Timestamp)
                        .Take(limit)
                        .Select(l => new LocationModel
                        {
                            Id = l.Id,
                            Latitude = l.Latitude,
                            Longitude = l.Longitude,
                            Accuracy = l.Accuracy,
                            Timestamp = l.Timestamp,
                            IsSynced = l.IsSynced,
                            RemoteId = l.RemoteId
                        })
                        .ToList();
                    return Task.FromResult((IEnumerable<LocationModel>)locations);
                });
                
            // Setup SyncPendingLocationsAsync to return a sync response
            mockLocationService.Setup(s => s.SyncPendingLocationsAsync(It.IsAny<int>()))
                .ReturnsAsync(new LocationSyncResponse
                {
                    SyncedIds = new List<int> { 4 },
                    FailedIds = new List<int>()
                });
                
            return mockLocationService;
        }
        
        /// <summary>
        /// Creates a mock implementation of ITimeRecordService with predefined test data and behavior.
        /// </summary>
        /// <returns>A configured mock of ITimeRecordService.</returns>
        public static Mock<ITimeRecordService> CreateMockTimeRecordService()
        {
            var mockTimeRecordService = new Mock<ITimeRecordService>();
            
            // Setup CreateTimeRecordAsync to return a successful result with time record response
            mockTimeRecordService.Setup(s => s.CreateTimeRecordAsync(It.IsAny<TimeRecordRequest>(), It.IsAny<string>()))
                .Returns<TimeRecordRequest, string>((request, userId) => Task.FromResult(
                    Result.Success(new TimeRecordResponse 
                    { 
                        Id = "time-record-123", 
                        Status = "success" 
                    })
                ));
                
            // Setup GetTimeRecordByIdAsync to return a time record by ID from test data
            mockTimeRecordService.Setup(s => s.GetTimeRecordByIdAsync(It.IsAny<int>()))
                .Returns<int>(id => {
                    var timeRecord = TestData.GetTestTimeRecordById(id);
                    return Task.FromResult(timeRecord != null 
                        ? Result.Success(timeRecord) 
                        : Result.Failure<SecurityPatrol.Core.Entities.TimeRecord>("Time record not found"));
                });
                
            // Setup GetTimeRecordHistoryAsync to return paginated time records for a user
            mockTimeRecordService.Setup(s => s.GetTimeRecordHistoryAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns<string, int, int>((userId, pageNumber, pageSize) => {
                    var timeRecords = TestData.GetTestTimeRecords()
                        .Where(t => t.UserId == userId)
                        .ToList();
                    var paginatedList = PaginatedList<SecurityPatrol.Core.Entities.TimeRecord>.Create(timeRecords, pageNumber, pageSize);
                    return Task.FromResult(Result.Success(paginatedList));
                });
                
            // Setup GetCurrentStatusAsync to return the current clock status for a user
            mockTimeRecordService.Setup(s => s.GetCurrentStatusAsync(It.IsAny<string>()))
                .Returns<string>(userId => {
                    var records = TestData.GetTestTimeRecords().Where(t => t.UserId == userId).OrderByDescending(t => t.Timestamp).ToList();
                    var lastRecord = records.FirstOrDefault();
                    string status = lastRecord != null && lastRecord.Type == "ClockIn" ? "in" : "out";
                    return Task.FromResult(Result.Success(status));
                });
                
            // Setup GetUnsyncedRecordsAsync to return unsynced time records
            mockTimeRecordService.Setup(s => s.GetUnsyncedRecordsAsync())
                .ReturnsAsync(() => {
                    var unsyncedRecords = TestData.GetTestTimeRecords().Where(t => !t.IsSynced).ToList();
                    return Result.Success<IEnumerable<SecurityPatrol.Core.Entities.TimeRecord>>(unsyncedRecords);
                });
                
            // Setup UpdateSyncStatusAsync to return a successful result
            mockTimeRecordService.Setup(s => s.UpdateSyncStatusAsync(It.IsAny<int>(), It.IsAny<bool>()))
                .ReturnsAsync(Result.Success());
                
            return mockTimeRecordService;
        }
        
        /// <summary>
        /// Creates a mock implementation of IPatrolService with predefined test data and behavior.
        /// </summary>
        /// <returns>A configured mock of IPatrolService.</returns>
        public static Mock<IPatrolService> CreateMockPatrolService()
        {
            var mockPatrolService = new Mock<IPatrolService>();
            
            // Setup GetLocationsAsync to return all patrol locations
            mockPatrolService.Setup(s => s.GetLocationsAsync())
                .ReturnsAsync(() => Result.Success<IEnumerable<SecurityPatrol.Core.Entities.PatrolLocation>>(TestData.GetTestPatrolLocations()));
                
            // Setup GetCheckpointsAsync to return checkpoints for a location
            mockPatrolService.Setup(s => s.GetCheckpointsByLocationIdAsync(It.IsAny<int>()))
                .Returns<int>(locationId => {
                    var checkpoints = TestData.GetTestCheckpoints()
                        .Where(c => c.LocationId == locationId)
                        .Select(c => new CheckpointModel
                        {
                            Id = c.Id,
                            LocationId = c.LocationId,
                            Name = c.Name,
                            Latitude = c.Latitude,
                            Longitude = c.Longitude,
                            IsVerified = false
                        })
                        .ToList();
                    return Task.FromResult(Result.Success<IEnumerable<CheckpointModel>>(checkpoints));
                });
                
            // Setup VerifyCheckpointAsync to return a successful result
            mockPatrolService.Setup(s => s.VerifyCheckpointAsync(It.IsAny<object>(), It.IsAny<string>()))
                .ReturnsAsync(Result.Success(new { Status = "success" }));
                
            // Setup GetPatrolStatusAsync to return patrol status for a location
            mockPatrolService.Setup(s => s.GetPatrolStatusAsync(It.IsAny<int>(), It.IsAny<string>()))
                .Returns<int, string>((locationId, userId) => {
                    var checkpoints = TestData.GetTestCheckpoints().Where(c => c.LocationId == locationId).ToList();
                    var verifications = TestData.GetTestCheckpointVerifications().Where(v => v.UserId == userId).ToList();
                    
                    var patrolStatus = new PatrolStatusModel
                    {
                        LocationId = locationId,
                        TotalCheckpoints = checkpoints.Count,
                        VerifiedCheckpoints = verifications.Count(v => checkpoints.Any(c => c.Id == v.CheckpointId))
                    };
                    
                    return Task.FromResult(Result.Success(patrolStatus));
                });
                
            // Setup GetUserVerificationsAsync to return checkpoint verifications for a user
            mockPatrolService.Setup(s => s.GetUserVerificationsAsync(It.IsAny<string>()))
                .Returns<string>(userId => {
                    var verifications = TestData.GetTestCheckpointVerifications()
                        .Where(v => v.UserId == userId)
                        .ToList();
                    return Task.FromResult(Result.Success<IEnumerable<SecurityPatrol.Core.Entities.CheckpointVerification>>(verifications));
                });
                
            // Setup IsCheckpointVerifiedAsync to check if a checkpoint is verified by a user
            mockPatrolService.Setup(s => s.IsCheckpointVerifiedAsync(It.IsAny<int>(), It.IsAny<string>()))
                .Returns<int, string>((checkpointId, userId) => {
                    var isVerified = TestData.GetTestCheckpointVerifications()
                        .Any(v => v.CheckpointId == checkpointId && v.UserId == userId);
                    return Task.FromResult(Result.Success(isVerified));
                });
                
            return mockPatrolService;
        }
        
        /// <summary>
        /// Creates a mock implementation of IPhotoService with predefined test data and behavior.
        /// </summary>
        /// <returns>A configured mock of IPhotoService.</returns>
        public static Mock<IPhotoService> CreateMockPhotoService()
        {
            var mockPhotoService = new Mock<IPhotoService>();
            
            // Setup UploadPhotoAsync to return a successful result
            mockPhotoService.Setup(s => s.UploadPhotoAsync(It.IsAny<PhotoUploadRequest>(), It.IsAny<Stream>(), It.IsAny<string>()))
                .ReturnsAsync(Result.Success(new PhotoUploadResponse { Id = "photo-123", Status = "success" }));
                
            // Setup GetPhotoAsync to return a photo by ID from test data
            mockPhotoService.Setup(s => s.GetPhotoAsync(It.IsAny<int>()))
                .Returns<int>(id => {
                    var photo = TestData.GetTestPhotoById(id);
                    return Task.FromResult(photo != null 
                        ? Result.Success(photo) 
                        : Result.Failure<SecurityPatrol.Core.Entities.Photo>("Photo not found"));
                });
                
            // Setup GetPhotoStreamAsync to return a stream for a photo
            mockPhotoService.Setup(s => s.GetPhotoStreamAsync(It.IsAny<int>()))
                .Returns<int>(id => {
                    var photo = TestData.GetTestPhotoById(id);
                    if (photo == null)
                        return Task.FromResult(Result.Failure<Stream>("Photo not found"));
                        
                    // Create a dummy stream for testing
                    var stream = new MemoryStream(new byte[] { 0, 1, 2, 3, 4 });
                    return Task.FromResult(Result.Success(stream as Stream));
                });
                
            // Setup GetPhotosByUserIdAsync to return photos for a user
            mockPhotoService.Setup(s => s.GetPhotosByUserIdAsync(It.IsAny<string>()))
                .Returns<string>(userId => {
                    var photos = TestData.GetTestPhotos().Where(p => p.UserId == userId).ToList();
                    return Task.FromResult(Result.Success<IEnumerable<SecurityPatrol.Core.Entities.Photo>>(photos));
                });
                
            // Setup GetPaginatedPhotosByUserIdAsync to return paginated photos for a user
            mockPhotoService.Setup(s => s.GetPaginatedPhotosByUserIdAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns<string, int, int>((userId, pageNumber, pageSize) => {
                    var photos = TestData.GetTestPhotos().Where(p => p.UserId == userId).ToList();
                    var paginatedList = PaginatedList<SecurityPatrol.Core.Entities.Photo>.Create(photos, pageNumber, pageSize);
                    return Task.FromResult(Result.Success(paginatedList));
                });
                
            // Setup DeletePhotoAsync to return a successful result
            mockPhotoService.Setup(s => s.DeletePhotoAsync(It.IsAny<int>()))
                .ReturnsAsync(Result.Success());
                
            return mockPhotoService;
        }
        
        /// <summary>
        /// Creates a mock implementation of IReportService with predefined test data and behavior.
        /// </summary>
        /// <returns>A configured mock of IReportService.</returns>
        public static Mock<IReportService> CreateMockReportService()
        {
            var mockReportService = new Mock<IReportService>();
            
            // Setup CreateReportAsync to return a successful result
            mockReportService.Setup(s => s.CreateReportAsync(It.IsAny<ReportRequest>(), It.IsAny<string>()))
                .ReturnsAsync(Result.Success(new ReportResponse { Id = "report-123", Status = "success" }));
                
            // Setup GetReportByIdAsync to return a report by ID from test data
            mockReportService.Setup(s => s.GetReportByIdAsync(It.IsAny<int>()))
                .Returns<int>(id => {
                    var report = TestData.GetTestReportById(id);
                    return Task.FromResult(report != null 
                        ? Result.Success(report) 
                        : Result.Failure<SecurityPatrol.Core.Entities.Report>("Report not found"));
                });
                
            // Setup GetReportsByUserIdAsync to return reports for a user
            mockReportService.Setup(s => s.GetReportsByUserIdAsync(It.IsAny<string>()))
                .Returns<string>(userId => {
                    var reports = TestData.GetTestReports().Where(r => r.UserId == userId).ToList();
                    return Task.FromResult(Result.Success<IEnumerable<SecurityPatrol.Core.Entities.Report>>(reports));
                });
                
            // Setup GetPaginatedReportsByUserIdAsync to return paginated reports for a user
            mockReportService.Setup(s => s.GetPaginatedReportsByUserIdAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns<string, int, int>((userId, pageNumber, pageSize) => {
                    var reports = TestData.GetTestReports().Where(r => r.UserId == userId).ToList();
                    var paginatedList = PaginatedList<SecurityPatrol.Core.Entities.Report>.Create(reports, pageNumber, pageSize);
                    return Task.FromResult(Result.Success(paginatedList));
                });
                
            // Setup GetAllReportsAsync to return all reports
            mockReportService.Setup(s => s.GetAllReportsAsync())
                .ReturnsAsync(() => Result.Success<IEnumerable<SecurityPatrol.Core.Entities.Report>>(TestData.GetTestReports()));
                
            // Setup GetUnsyncedReportsAsync to return unsynced reports
            mockReportService.Setup(s => s.GetUnsyncedReportsAsync())
                .ReturnsAsync(() => {
                    var unsyncedReports = TestData.GetTestReports().Where(r => !r.IsSynced).ToList();
                    return Result.Success<IEnumerable<SecurityPatrol.Core.Entities.Report>>(unsyncedReports);
                });
                
            // Setup UpdateSyncStatusAsync to return a successful result
            mockReportService.Setup(s => s.UpdateSyncStatusAsync(It.IsAny<int>(), It.IsAny<bool>()))
                .ReturnsAsync(Result.Success());
                
            return mockReportService;
        }
        
        /// <summary>
        /// Creates a mock implementation of ISmsService with predefined behavior.
        /// </summary>
        /// <returns>A configured mock of ISmsService.</returns>
        public static Mock<ISmsService> CreateMockSmsService()
        {
            var mockSmsService = new Mock<ISmsService>();
            
            // Setup SendSmsAsync to return true (success)
            mockSmsService.Setup(s => s.SendSmsAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);
                
            // Setup SendVerificationCodeAsync to return true (success)
            mockSmsService.Setup(s => s.SendVerificationCodeAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);
                
            return mockSmsService;
        }
        
        /// <summary>
        /// Creates a mock implementation of ITokenService with predefined behavior.
        /// </summary>
        /// <returns>A configured mock of ITokenService.</returns>
        public static Mock<ITokenService> CreateMockTokenService()
        {
            var mockTokenService = new Mock<ITokenService>();
            
            // Setup GenerateTokenAsync to return a new authentication response with token
            mockTokenService.Setup(s => s.GenerateTokenAsync(It.IsAny<SecurityPatrol.Core.Entities.User>()))
                .ReturnsAsync(new AuthenticationResponse 
                { 
                    Token = "test-jwt-token", 
                    ExpiresAt = DateTime.UtcNow.AddHours(1) 
                });
                
            // Setup ValidateTokenAsync to return true for valid tokens
            mockTokenService.Setup(s => s.ValidateTokenAsync(It.IsAny<string>()))
                .ReturnsAsync(true);
                
            // Setup RefreshTokenAsync to return a refreshed authentication response
            mockTokenService.Setup(s => s.RefreshTokenAsync(It.IsAny<string>()))
                .ReturnsAsync(new AuthenticationResponse 
                { 
                    Token = "refreshed-jwt-token", 
                    ExpiresAt = DateTime.UtcNow.AddHours(1) 
                });
                
            // Setup GetPrincipalFromTokenAsync to return a ClaimsPrincipal for valid tokens
            mockTokenService.Setup(s => s.GetPrincipalFromTokenAsync(It.IsAny<string>()))
                .ReturnsAsync(new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "user1"),
                    new Claim(ClaimTypes.Name, "+15551234567")
                })));
                
            // Setup GetUserIdFromTokenAsync to return a user ID from valid tokens
            mockTokenService.Setup(s => s.GetUserIdFromTokenAsync(It.IsAny<string>()))
                .ReturnsAsync("user1");
                
            return mockTokenService;
        }
        
        /// <summary>
        /// Creates a mock implementation of IVerificationCodeService with predefined behavior.
        /// </summary>
        /// <returns>A configured mock of IVerificationCodeService.</returns>
        public static Mock<IVerificationCodeService> CreateMockVerificationCodeService()
        {
            var mockVerificationCodeService = new Mock<IVerificationCodeService>();
            
            // Setup GenerateCodeAsync to return a 6-digit verification code
            mockVerificationCodeService.Setup(s => s.GenerateCodeAsync(It.IsAny<string>()))
                .ReturnsAsync("123456");
                
            // Setup ValidateCodeAsync to return true for valid codes
            mockVerificationCodeService.Setup(s => s.ValidateCodeAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);
                
            // Setup StoreCodeAsync to return a verification ID
            mockVerificationCodeService.Setup(s => s.StoreCodeAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("verification-123");
                
            // Setup GetCodeExpirationAsync to return an expiration time
            mockVerificationCodeService.Setup(s => s.GetCodeExpirationAsync(It.IsAny<string>()))
                .ReturnsAsync(DateTime.UtcNow.AddMinutes(10));
                
            // Setup ClearExpiredCodesAsync to complete successfully
            mockVerificationCodeService.Setup(s => s.ClearExpiredCodesAsync())
                .Returns(Task.CompletedTask);
                
            return mockVerificationCodeService;
        }
    }
}