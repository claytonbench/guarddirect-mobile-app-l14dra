using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SecurityPatrol.TestCommon.Constants;
using SecurityPatrol.TestCommon.Helpers;
using SecurityPatrol.Database.Entities;
using SecurityPatrol.Models;

namespace SecurityPatrol.TestCommon.Data
{
    /// <summary>
    /// Static utility class that provides methods for generating mock data for testing purposes across the Security Patrol application.
    /// </summary>
    public static class MockDataGenerator
    {
        private static readonly Random Random = new Random();

        /// <summary>
        /// Creates a new UserEntity instance with test values
        /// </summary>
        /// <param name="id">The identifier for the entity</param>
        /// <param name="userId">The user identifier (defaults to TestUserId if null)</param>
        /// <param name="phoneNumber">The phone number (defaults to a generated number if null)</param>
        /// <returns>A UserEntity instance with the specified test values</returns>
        public static UserEntity CreateUserEntity(int id, string userId = null, string phoneNumber = null)
        {
            return new UserEntity
            {
                Id = id,
                UserId = userId ?? TestConstants.TestUserId,
                PhoneNumber = phoneNumber ?? GenerateRandomPhoneNumber(),
                LastAuthenticated = DateTime.UtcNow,
                AuthToken = TestConstants.TestAuthToken,
                TokenExpiry = DateTime.UtcNow.AddDays(1)
            };
        }

        /// <summary>
        /// Creates a new PatrolLocationEntity instance with test values
        /// </summary>
        /// <param name="id">The identifier for the entity</param>
        /// <param name="name">The name of the location (defaults to "Test Location {id}" if null)</param>
        /// <param name="latitude">The latitude coordinate (defaults to TestLatitude if 0)</param>
        /// <param name="longitude">The longitude coordinate (defaults to TestLongitude if 0)</param>
        /// <returns>A PatrolLocationEntity instance with the specified test values</returns>
        public static PatrolLocationEntity CreatePatrolLocationEntity(int id, string name = null, double latitude = 0, double longitude = 0)
        {
            return new PatrolLocationEntity
            {
                Id = id,
                Name = name ?? $"Test Location {id}",
                Latitude = latitude == 0 ? TestConstants.TestLatitude : latitude,
                Longitude = longitude == 0 ? TestConstants.TestLongitude : longitude,
                LastUpdated = DateTime.UtcNow,
                RemoteId = $"loc_{id}"
            };
        }

        /// <summary>
        /// Creates a new CheckpointEntity instance with test values
        /// </summary>
        /// <param name="id">The identifier for the entity</param>
        /// <param name="locationId">The location identifier this checkpoint belongs to</param>
        /// <param name="name">The name of the checkpoint (defaults to "Checkpoint {id}" if null)</param>
        /// <param name="latitude">The latitude coordinate (defaults to TestLatitude if 0)</param>
        /// <param name="longitude">The longitude coordinate (defaults to TestLongitude if 0)</param>
        /// <returns>A CheckpointEntity instance with the specified test values</returns>
        public static CheckpointEntity CreateCheckpointEntity(int id, int locationId, string name = null, double latitude = 0, double longitude = 0)
        {
            return new CheckpointEntity
            {
                Id = id,
                LocationId = locationId,
                Name = name ?? $"Checkpoint {id}",
                Latitude = latitude == 0 ? TestConstants.TestLatitude : latitude,
                Longitude = longitude == 0 ? TestConstants.TestLongitude : longitude,
                LastUpdated = DateTime.UtcNow,
                RemoteId = $"cp_{id}"
            };
        }

        /// <summary>
        /// Creates a new TimeRecordEntity instance with test values
        /// </summary>
        /// <param name="id">The identifier for the entity</param>
        /// <param name="userId">The user identifier (defaults to TestUserId if null)</param>
        /// <param name="type">The time record type (should be "ClockIn" or "ClockOut")</param>
        /// <param name="isSynced">Whether the record is synced with the backend</param>
        /// <returns>A TimeRecordEntity instance with the specified test values</returns>
        public static TimeRecordEntity CreateTimeRecordEntity(int id, string userId = null, string type = "ClockIn", bool isSynced = false)
        {
            return new TimeRecordEntity
            {
                Id = id,
                UserId = userId ?? TestConstants.TestUserId,
                Type = type,
                Timestamp = DateTime.UtcNow,
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude,
                IsSynced = isSynced,
                RemoteId = isSynced ? $"time_{id}" : null
            };
        }

        /// <summary>
        /// Creates a new PhotoEntity instance with test values
        /// </summary>
        /// <param name="id">The identifier for the entity (defaults to a new GUID string if null)</param>
        /// <param name="userId">The user identifier (defaults to TestUserId if null)</param>
        /// <param name="filePath">The file path (defaults to TestImagePath if null)</param>
        /// <param name="isSynced">Whether the photo is synced with the backend</param>
        /// <returns>A PhotoEntity instance with the specified test values</returns>
        public static PhotoEntity CreatePhotoEntity(string id = null, string userId = null, string filePath = null, bool isSynced = false)
        {
            id = id ?? Guid.NewGuid().ToString();
            
            return new PhotoEntity
            {
                Id = id,
                UserId = userId ?? TestConstants.TestUserId,
                Timestamp = DateTime.UtcNow,
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude,
                FilePath = filePath ?? TestConstants.TestImagePath,
                IsSynced = isSynced,
                SyncProgress = isSynced ? 100 : 0,
                RemoteId = isSynced ? $"photo_{id}" : null
            };
        }

        /// <summary>
        /// Creates a new ReportEntity instance with test values
        /// </summary>
        /// <param name="id">The identifier for the entity</param>
        /// <param name="userId">The user identifier (defaults to TestUserId if null)</param>
        /// <param name="text">The report text (defaults to TestReportText if null)</param>
        /// <param name="isSynced">Whether the report is synced with the backend</param>
        /// <returns>A ReportEntity instance with the specified test values</returns>
        public static ReportEntity CreateReportEntity(int id, string userId = null, string text = null, bool isSynced = false)
        {
            return new ReportEntity
            {
                Id = id,
                UserId = userId ?? TestConstants.TestUserId,
                Text = text ?? TestConstants.TestReportText,
                Timestamp = DateTime.UtcNow,
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude,
                IsSynced = isSynced,
                RemoteId = isSynced ? $"report_{id}" : null
            };
        }

        /// <summary>
        /// Creates a new LocationModel instance with test values
        /// </summary>
        /// <param name="id">The identifier for the model</param>
        /// <param name="latitude">The latitude coordinate (defaults to TestLatitude if 0)</param>
        /// <param name="longitude">The longitude coordinate (defaults to TestLongitude if 0)</param>
        /// <param name="accuracy">The accuracy in meters (defaults to 10.0 if 0)</param>
        /// <returns>A LocationModel instance with the specified test values</returns>
        public static LocationModel CreateLocationModel(int id, double latitude = 0, double longitude = 0, double accuracy = 0)
        {
            return new LocationModel
            {
                Id = id,
                Latitude = latitude == 0 ? TestConstants.TestLatitude : latitude,
                Longitude = longitude == 0 ? TestConstants.TestLongitude : longitude,
                Accuracy = accuracy == 0 ? 10.0 : accuracy,
                Timestamp = DateTime.UtcNow,
                IsSynced = false
            };
        }

        /// <summary>
        /// Creates a new CheckpointModel instance with test values
        /// </summary>
        /// <param name="id">The identifier for the model</param>
        /// <param name="locationId">The location identifier this checkpoint belongs to</param>
        /// <param name="name">The name of the checkpoint (defaults to "Checkpoint {id}" if null)</param>
        /// <param name="latitude">The latitude coordinate (defaults to TestLatitude if 0)</param>
        /// <param name="longitude">The longitude coordinate (defaults to TestLongitude if 0)</param>
        /// <param name="isVerified">Whether the checkpoint is verified</param>
        /// <returns>A CheckpointModel instance with the specified test values</returns>
        public static CheckpointModel CreateCheckpointModel(int id, int locationId, string name = null, double latitude = 0, double longitude = 0, bool isVerified = false)
        {
            return new CheckpointModel
            {
                Id = id,
                LocationId = locationId,
                Name = name ?? $"Checkpoint {id}",
                Latitude = latitude == 0 ? TestConstants.TestLatitude : latitude,
                Longitude = longitude == 0 ? TestConstants.TestLongitude : longitude,
                IsVerified = isVerified,
                VerificationTime = isVerified ? DateTime.UtcNow : null,
                RemoteId = $"cp_{id}"
            };
        }

        /// <summary>
        /// Creates a new PhotoModel instance with test values
        /// </summary>
        /// <param name="id">The identifier for the model (defaults to a new GUID string if null)</param>
        /// <param name="userId">The user identifier (defaults to TestUserId if null)</param>
        /// <param name="filePath">The file path (defaults to TestImagePath if null)</param>
        /// <param name="isSynced">Whether the photo is synced with the backend</param>
        /// <returns>A PhotoModel instance with the specified test values</returns>
        public static PhotoModel CreatePhotoModel(string id = null, string userId = null, string filePath = null, bool isSynced = false)
        {
            id = id ?? Guid.NewGuid().ToString();
            
            return new PhotoModel
            {
                Id = id,
                UserId = userId ?? TestConstants.TestUserId,
                Timestamp = DateTime.UtcNow,
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude,
                FilePath = filePath ?? TestConstants.TestImagePath,
                IsSynced = isSynced,
                SyncProgress = isSynced ? 100 : 0,
                RemoteId = isSynced ? $"photo_{id}" : null
            };
        }

        /// <summary>
        /// Creates a new ReportModel instance with test values
        /// </summary>
        /// <param name="id">The identifier for the model</param>
        /// <param name="text">The report text (defaults to TestReportText if null)</param>
        /// <param name="isSynced">Whether the report is synced with the backend</param>
        /// <returns>A ReportModel instance with the specified test values</returns>
        public static ReportModel CreateReportModel(int id, string text = null, bool isSynced = false)
        {
            return new ReportModel
            {
                Id = id,
                UserId = TestConstants.TestUserId,
                Text = text ?? TestConstants.TestReportText,
                Timestamp = DateTime.UtcNow,
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude,
                IsSynced = isSynced,
                RemoteId = isSynced ? $"report_{id}" : null
            };
        }

        /// <summary>
        /// Creates a new TimeRecordModel instance with test values
        /// </summary>
        /// <param name="id">The identifier for the model</param>
        /// <param name="type">The time record type (should be "ClockIn" or "ClockOut")</param>
        /// <param name="isSynced">Whether the record is synced with the backend</param>
        /// <returns>A TimeRecordModel instance with the specified test values</returns>
        public static TimeRecordModel CreateTimeRecordModel(int id, string type = "ClockIn", bool isSynced = false)
        {
            return new TimeRecordModel
            {
                Id = id,
                UserId = TestConstants.TestUserId,
                Type = type,
                Timestamp = DateTime.UtcNow,
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude,
                IsSynced = isSynced,
                RemoteId = isSynced ? $"time_{id}" : null
            };
        }

        /// <summary>
        /// Creates a list of ReportModel instances with test values
        /// </summary>
        /// <param name="count">The number of reports to create</param>
        /// <param name="allSynced">Whether all reports should be marked as synced</param>
        /// <returns>A list of ReportModel instances with test values</returns>
        public static List<ReportModel> CreateReportModels(int count, bool allSynced = false)
        {
            var reports = new List<ReportModel>();
            
            for (int i = 1; i <= count; i++)
            {
                bool isSynced = allSynced || (i % 2 == 0); // Alternate sync status if not all synced
                reports.Add(CreateReportModel(i, $"{TestConstants.TestReportText} {i}", isSynced));
            }
            
            return reports;
        }

        /// <summary>
        /// Creates a list of PhotoModel instances with test values
        /// </summary>
        /// <param name="count">The number of photos to create</param>
        /// <param name="allSynced">Whether all photos should be marked as synced</param>
        /// <returns>A list of PhotoModel instances with test values</returns>
        public static List<PhotoModel> CreatePhotoModels(int count, bool allSynced = false)
        {
            var photos = new List<PhotoModel>();
            
            for (int i = 1; i <= count; i++)
            {
                string id = Guid.NewGuid().ToString();
                bool isSynced = allSynced || (i % 2 == 0); // Alternate sync status if not all synced
                photos.Add(CreatePhotoModel(id, null, null, isSynced));
            }
            
            return photos;
        }

        /// <summary>
        /// Creates a list of TimeRecordModel instances representing a complete history of clock in/out events
        /// </summary>
        /// <param name="pairCount">The number of clock in/out pairs to create</param>
        /// <param name="allSynced">Whether all records should be marked as synced</param>
        /// <returns>A list of TimeRecordModel instances representing a clock history</returns>
        public static List<TimeRecordModel> CreateTimeRecordHistory(int pairCount, bool allSynced = false)
        {
            var records = new List<TimeRecordModel>();
            DateTime baseTime = DateTime.UtcNow.AddDays(-pairCount); // Start from past
            
            for (int i = 0; i < pairCount; i++)
            {
                // Create clock in
                var clockIn = CreateTimeRecordModel(i * 2 + 1, "ClockIn", allSynced);
                clockIn.Timestamp = baseTime.AddHours(i * 9); // 9 hour spacing between pairs
                records.Add(clockIn);
                
                // Create clock out
                var clockOut = CreateTimeRecordModel(i * 2 + 2, "ClockOut", allSynced);
                clockOut.Timestamp = baseTime.AddHours(i * 9 + 8); // 8 hour shift
                records.Add(clockOut);
            }
            
            return records;
        }

        /// <summary>
        /// Creates a PatrolLocationEntity with associated CheckpointEntity instances
        /// </summary>
        /// <param name="locationId">The identifier for the location</param>
        /// <param name="locationName">The name of the location</param>
        /// <param name="checkpointCount">The number of checkpoints to create</param>
        /// <returns>A tuple containing the patrol location and its checkpoints</returns>
        public static Tuple<PatrolLocationEntity, List<CheckpointEntity>> CreatePatrolWithCheckpoints(int locationId, string locationName, int checkpointCount)
        {
            var location = CreatePatrolLocationEntity(locationId, locationName);
            var checkpoints = new List<CheckpointEntity>();
            
            // Create checkpoints around the location
            for (int i = 1; i <= checkpointCount; i++)
            {
                // Calculate slightly different coordinates for each checkpoint
                double latOffset = (Random.NextDouble() - 0.5) * 0.01; // +/- 0.005 degrees
                double lonOffset = (Random.NextDouble() - 0.5) * 0.01; // +/- 0.005 degrees
                
                var checkpoint = CreateCheckpointEntity(
                    i, 
                    locationId, 
                    $"Checkpoint {i} at {locationName}", 
                    location.Latitude + latOffset, 
                    location.Longitude + lonOffset);
                
                checkpoints.Add(checkpoint);
            }
            
            return new Tuple<PatrolLocationEntity, List<CheckpointEntity>>(location, checkpoints);
        }

        /// <summary>
        /// Creates a location with associated checkpoint models for testing patrol functionality
        /// </summary>
        /// <param name="locationId">The identifier for the location</param>
        /// <param name="locationName">The name of the location</param>
        /// <param name="checkpointCount">The number of checkpoints to create</param>
        /// <param name="someVerified">Whether some checkpoints should be marked as verified</param>
        /// <returns>A tuple containing the patrol location and its checkpoint models</returns>
        public static Tuple<PatrolLocationEntity, List<CheckpointModel>> CreatePatrolWithCheckpointModels(int locationId, string locationName, int checkpointCount, bool someVerified = false)
        {
            var location = CreatePatrolLocationEntity(locationId, locationName);
            var checkpoints = new List<CheckpointModel>();
            
            // Create checkpoints around the location
            for (int i = 1; i <= checkpointCount; i++)
            {
                // Calculate slightly different coordinates for each checkpoint
                double latOffset = (Random.NextDouble() - 0.5) * 0.01; // +/- 0.005 degrees
                double lonOffset = (Random.NextDouble() - 0.5) * 0.01; // +/- 0.005 degrees
                
                bool isVerified = someVerified && (i % 2 == 0); // Every other checkpoint is verified if someVerified is true
                
                var checkpoint = CreateCheckpointModel(
                    i, 
                    locationId, 
                    $"Checkpoint {i} at {locationName}", 
                    location.Latitude + latOffset, 
                    location.Longitude + lonOffset,
                    isVerified);
                
                checkpoints.Add(checkpoint);
            }
            
            return new Tuple<PatrolLocationEntity, List<CheckpointModel>>(location, checkpoints);
        }

        /// <summary>
        /// Creates a test image file at the specified path for photo testing
        /// </summary>
        /// <param name="filePath">The path where the image should be saved</param>
        /// <param name="width">The width of the image in pixels</param>
        /// <param name="height">The height of the image in pixels</param>
        /// <returns>A task that returns true if the file was created successfully, false otherwise</returns>
        public static async Task<bool> CreateTestPhotoFileAsync(string filePath, int width = 640, int height = 480)
        {
            try
            {
                // Ensure directory exists
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // Generate and save test image
                return await TestImageGenerator.GenerateTestImageFileAsync(filePath, width, height);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Creates a complete set of related test data for comprehensive testing scenarios
        /// </summary>
        /// <returns>A container with all generated test data entities and models</returns>
        public static TestDataSet CreateCompleteTestDataSet()
        {
            var dataSet = new TestDataSet();
            
            // Create user
            dataSet.User = CreateUserEntity(1);
            
            // Create locations
            for (int i = 1; i <= 3; i++)
            {
                dataSet.Locations.Add(CreatePatrolLocationEntity(i, $"Test Location {i}"));
            }
            
            // Create checkpoints
            dataSet.CheckpointsByLocation = new Dictionary<int, List<CheckpointEntity>>();
            foreach (var location in dataSet.Locations)
            {
                var checkpointCount = Random.Next(3, 8); // Random number of checkpoints
                var checkpoints = new List<CheckpointEntity>();
                
                for (int j = 1; j <= checkpointCount; j++)
                {
                    // Create checkpoint with slight offset from location
                    double latOffset = (Random.NextDouble() - 0.5) * 0.01;
                    double lonOffset = (Random.NextDouble() - 0.5) * 0.01;
                    
                    var checkpoint = CreateCheckpointEntity(
                        dataSet.Checkpoints.Count + 1,
                        location.Id,
                        $"CP{j} at {location.Name}",
                        location.Latitude + latOffset,
                        location.Longitude + lonOffset);
                    
                    checkpoints.Add(checkpoint);
                    dataSet.Checkpoints.Add(checkpoint);
                }
                
                dataSet.CheckpointsByLocation.Add(location.Id, checkpoints);
            }
            
            // Create checkpoint models (with some verified)
            foreach (var checkpoint in dataSet.Checkpoints)
            {
                bool isVerified = Random.Next(2) == 0; // 50% chance of being verified
                var model = CreateCheckpointModel(
                    checkpoint.Id, 
                    checkpoint.LocationId, 
                    checkpoint.Name, 
                    checkpoint.Latitude, 
                    checkpoint.Longitude, 
                    isVerified);
                
                dataSet.CheckpointModels.Add(model);
            }
            
            // Create time records
            for (int i = 1; i <= 10; i++)
            {
                bool isSynced = i % 3 == 0; // Every third is synced
                string type = i % 2 == 0 ? "ClockOut" : "ClockIn";
                
                dataSet.TimeRecords.Add(CreateTimeRecordEntity(i, null, type, isSynced));
                dataSet.TimeRecordModels.Add(CreateTimeRecordModel(i, type, isSynced));
            }
            
            // Create photos
            for (int i = 1; i <= 10; i++)
            {
                string id = Guid.NewGuid().ToString();
                bool isSynced = i % 3 == 0; // Every third is synced
                
                dataSet.Photos.Add(CreatePhotoEntity(id, null, null, isSynced));
                dataSet.PhotoModels.Add(CreatePhotoModel(id, null, null, isSynced));
            }
            
            // Create reports
            for (int i = 1; i <= 10; i++)
            {
                bool isSynced = i % 3 == 0; // Every third is synced
                
                dataSet.Reports.Add(CreateReportEntity(i, null, $"Test Report {i}", isSynced));
                dataSet.ReportModels.Add(CreateReportModel(i, $"Test Report {i}", isSynced));
            }
            
            return dataSet;
        }

        /// <summary>
        /// Generates a random phone number string for testing
        /// </summary>
        /// <returns>A randomly generated phone number string</returns>
        private static string GenerateRandomPhoneNumber()
        {
            int areaCode = Random.Next(100, 999);
            int prefix = Random.Next(100, 999);
            int lineNumber = Random.Next(1000, 9999);
            
            return $"({areaCode}) {prefix}-{lineNumber}";
        }
    }

    /// <summary>
    /// Container class that holds complete sets of related test data for comprehensive testing scenarios
    /// </summary>
    public class TestDataSet
    {
        /// <summary>
        /// The test user entity
        /// </summary>
        public UserEntity User { get; set; }
        
        /// <summary>
        /// The collection of patrol location entities
        /// </summary>
        public List<PatrolLocationEntity> Locations { get; set; }
        
        /// <summary>
        /// The collection of checkpoint entities
        /// </summary>
        public List<CheckpointEntity> Checkpoints { get; set; }
        
        /// <summary>
        /// The collection of time record entities
        /// </summary>
        public List<TimeRecordEntity> TimeRecords { get; set; }
        
        /// <summary>
        /// The collection of photo entities
        /// </summary>
        public List<PhotoEntity> Photos { get; set; }
        
        /// <summary>
        /// The collection of report entities
        /// </summary>
        public List<ReportEntity> Reports { get; set; }
        
        /// <summary>
        /// The collection of checkpoint models
        /// </summary>
        public List<CheckpointModel> CheckpointModels { get; set; }
        
        /// <summary>
        /// The collection of time record models
        /// </summary>
        public List<TimeRecordModel> TimeRecordModels { get; set; }
        
        /// <summary>
        /// The collection of photo models
        /// </summary>
        public List<PhotoModel> PhotoModels { get; set; }
        
        /// <summary>
        /// The collection of report models
        /// </summary>
        public List<ReportModel> ReportModels { get; set; }
        
        /// <summary>
        /// Dictionary mapping location IDs to their checkpoints
        /// </summary>
        public Dictionary<int, List<CheckpointEntity>> CheckpointsByLocation { get; set; }
        
        /// <summary>
        /// Initializes a new instance of the TestDataSet class with empty collections
        /// </summary>
        public TestDataSet()
        {
            Locations = new List<PatrolLocationEntity>();
            Checkpoints = new List<CheckpointEntity>();
            TimeRecords = new List<TimeRecordEntity>();
            Photos = new List<PhotoEntity>();
            Reports = new List<ReportEntity>();
            CheckpointModels = new List<CheckpointModel>();
            TimeRecordModels = new List<TimeRecordModel>();
            PhotoModels = new List<PhotoModel>();
            ReportModels = new List<ReportModel>();
            CheckpointsByLocation = new Dictionary<int, List<CheckpointEntity>>();
        }
    }
}