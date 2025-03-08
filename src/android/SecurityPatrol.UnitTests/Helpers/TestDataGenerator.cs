using System;
using System.Collections.Generic;
using System.Linq;
using SecurityPatrol.Models;
using SecurityPatrol.Database.Entities;

namespace SecurityPatrol.UnitTests.Helpers
{
    /// <summary>
    /// Static utility class that provides methods to generate test data for unit tests in the Security Patrol application.
    /// </summary>
    public static class TestDataGenerator
    {
        /// <summary>
        /// Private constructor to prevent instantiation of this utility class
        /// </summary>
        private TestDataGenerator()
        {
        }

        /// <summary>
        /// Creates an AuthState instance for testing
        /// </summary>
        /// <param name="isAuthenticated">Whether the state should be authenticated</param>
        /// <param name="phoneNumber">The phone number to use for authenticated state</param>
        /// <returns>An AuthState instance with the specified properties</returns>
        public static AuthState CreateAuthState(bool isAuthenticated, string phoneNumber = "+15551234567")
        {
            if (isAuthenticated)
                return AuthState.CreateAuthenticated(phoneNumber);
            else
                return AuthState.CreateUnauthenticated();
        }

        /// <summary>
        /// Creates a TimeRecordModel instance for testing
        /// </summary>
        /// <param name="id">The record ID</param>
        /// <param name="userId">The user ID</param>
        /// <param name="type">The record type (e.g., "ClockIn" or "ClockOut")</param>
        /// <param name="timestamp">The timestamp of the record</param>
        /// <param name="latitude">The latitude coordinate</param>
        /// <param name="longitude">The longitude coordinate</param>
        /// <param name="isSynced">Whether the record is synced</param>
        /// <returns>A TimeRecordModel instance with the specified properties</returns>
        public static TimeRecordModel CreateTimeRecord(
            int id = 1,
            string userId = "user1",
            string type = "ClockIn",
            DateTime? timestamp = null,
            double latitude = 34.0522,
            double longitude = -118.2437,
            bool isSynced = false)
        {
            var record = new TimeRecordModel
            {
                Id = id,
                UserId = userId,
                Type = type,
                Timestamp = timestamp ?? DateTime.UtcNow,
                Latitude = latitude,
                Longitude = longitude,
                IsSynced = isSynced
            };

            return record;
        }

        /// <summary>
        /// Creates a TimeRecordModel instance for a clock-in event
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="latitude">The latitude coordinate</param>
        /// <param name="longitude">The longitude coordinate</param>
        /// <param name="isSynced">Whether the record is synced</param>
        /// <returns>A TimeRecordModel instance configured as a clock-in event</returns>
        public static TimeRecordModel CreateClockInRecord(
            string userId = "user1",
            double latitude = 34.0522,
            double longitude = -118.2437,
            bool isSynced = false)
        {
            var record = TimeRecordModel.CreateClockIn(userId, latitude, longitude);
            record.IsSynced = isSynced;
            return record;
        }

        /// <summary>
        /// Creates a TimeRecordModel instance for a clock-out event
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="latitude">The latitude coordinate</param>
        /// <param name="longitude">The longitude coordinate</param>
        /// <param name="isSynced">Whether the record is synced</param>
        /// <returns>A TimeRecordModel instance configured as a clock-out event</returns>
        public static TimeRecordModel CreateClockOutRecord(
            string userId = "user1",
            double latitude = 34.0522,
            double longitude = -118.2437,
            bool isSynced = false)
        {
            var record = TimeRecordModel.CreateClockOut(userId, latitude, longitude);
            record.IsSynced = isSynced;
            return record;
        }

        /// <summary>
        /// Creates a LocationModel instance for testing
        /// </summary>
        /// <param name="id">The location ID</param>
        /// <param name="latitude">The latitude coordinate</param>
        /// <param name="longitude">The longitude coordinate</param>
        /// <param name="accuracy">The location accuracy in meters</param>
        /// <param name="timestamp">The timestamp of the location</param>
        /// <param name="isSynced">Whether the location is synced</param>
        /// <returns>A LocationModel instance with the specified properties</returns>
        public static LocationModel CreateLocationModel(
            int id = 1,
            double latitude = 34.0522,
            double longitude = -118.2437,
            double accuracy = 10.0,
            DateTime? timestamp = null,
            bool isSynced = false)
        {
            return new LocationModel
            {
                Id = id,
                Latitude = latitude,
                Longitude = longitude,
                Accuracy = accuracy,
                Timestamp = timestamp ?? DateTime.UtcNow,
                IsSynced = isSynced
            };
        }

        /// <summary>
        /// Creates a list of LocationModel instances for testing
        /// </summary>
        /// <param name="count">The number of instances to create</param>
        /// <param name="isSynced">Whether the locations are synced</param>
        /// <returns>A list of LocationModel instances</returns>
        public static List<LocationModel> CreateLocationModels(int count = 5, bool isSynced = false)
        {
            var locations = new List<LocationModel>();
            var random = new Random();

            for (int i = 1; i <= count; i++)
            {
                var (latitude, longitude) = GetRandomCoordinates();
                
                locations.Add(new LocationModel
                {
                    Id = i,
                    Latitude = latitude,
                    Longitude = longitude,
                    Accuracy = random.Next(5, 20),
                    Timestamp = DateTime.UtcNow.AddMinutes(-i),
                    IsSynced = isSynced
                });
            }

            return locations;
        }

        /// <summary>
        /// Creates a CheckpointModel instance for testing
        /// </summary>
        /// <param name="id">The checkpoint ID</param>
        /// <param name="locationId">The location ID this checkpoint belongs to</param>
        /// <param name="name">The name of the checkpoint</param>
        /// <param name="latitude">The latitude coordinate</param>
        /// <param name="longitude">The longitude coordinate</param>
        /// <param name="isVerified">Whether the checkpoint is verified</param>
        /// <returns>A CheckpointModel instance with the specified properties</returns>
        public static CheckpointModel CreateCheckpointModel(
            int id = 1,
            int locationId = 1,
            string name = "Test Checkpoint",
            double latitude = 34.0522,
            double longitude = -118.2437,
            bool isVerified = false)
        {
            var checkpoint = new CheckpointModel
            {
                Id = id,
                LocationId = locationId,
                Name = name,
                Latitude = latitude,
                Longitude = longitude,
                IsVerified = false
            };

            if (isVerified)
            {
                checkpoint.MarkAsVerified();
            }

            return checkpoint;
        }

        /// <summary>
        /// Creates a list of CheckpointModel instances for testing
        /// </summary>
        /// <param name="count">The number of instances to create</param>
        /// <param name="locationId">The location ID these checkpoints belong to</param>
        /// <param name="isVerified">Whether the checkpoints are verified</param>
        /// <returns>A list of CheckpointModel instances</returns>
        public static List<CheckpointModel> CreateCheckpointModels(int count = 5, int locationId = 1, bool isVerified = false)
        {
            var checkpoints = new List<CheckpointModel>();

            for (int i = 1; i <= count; i++)
            {
                var (latitude, longitude) = GetRandomCoordinates();
                
                var checkpoint = new CheckpointModel
                {
                    Id = i,
                    LocationId = locationId,
                    Name = $"Checkpoint {i}",
                    Latitude = latitude,
                    Longitude = longitude,
                    IsVerified = false
                };

                if (isVerified)
                {
                    checkpoint.MarkAsVerified();
                }

                checkpoints.Add(checkpoint);
            }

            return checkpoints;
        }

        /// <summary>
        /// Creates a PatrolStatus instance for testing
        /// </summary>
        /// <param name="locationId">The location ID</param>
        /// <param name="totalCheckpoints">The total number of checkpoints</param>
        /// <param name="verifiedCheckpoints">The number of verified checkpoints</param>
        /// <param name="startTime">The start time of the patrol</param>
        /// <param name="endTime">The end time of the patrol</param>
        /// <returns>A PatrolStatus instance with the specified properties</returns>
        public static PatrolStatus CreatePatrolStatus(
            int locationId = 1,
            int totalCheckpoints = 5,
            int verifiedCheckpoints = 0,
            DateTime? startTime = null,
            DateTime? endTime = null)
        {
            return new PatrolStatus
            {
                LocationId = locationId,
                TotalCheckpoints = totalCheckpoints,
                VerifiedCheckpoints = verifiedCheckpoints,
                StartTime = startTime ?? DateTime.UtcNow.AddHours(-1),
                EndTime = endTime
            };
        }

        /// <summary>
        /// Creates a PhotoModel instance for testing
        /// </summary>
        /// <param name="id">The photo ID</param>
        /// <param name="userId">The user ID</param>
        /// <param name="timestamp">The timestamp of the photo</param>
        /// <param name="latitude">The latitude coordinate</param>
        /// <param name="longitude">The longitude coordinate</param>
        /// <param name="filePath">The file path of the photo</param>
        /// <param name="isSynced">Whether the photo is synced</param>
        /// <param name="syncProgress">The sync progress percentage</param>
        /// <returns>A PhotoModel instance with the specified properties</returns>
        public static PhotoModel CreatePhotoModel(
            string id = "photo1",
            string userId = "user1",
            DateTime? timestamp = null,
            double latitude = 34.0522,
            double longitude = -118.2437,
            string filePath = "/test/photo.jpg",
            bool isSynced = false,
            int syncProgress = 0)
        {
            return new PhotoModel
            {
                Id = id,
                UserId = userId,
                Timestamp = timestamp ?? DateTime.UtcNow,
                Latitude = latitude,
                Longitude = longitude,
                FilePath = filePath,
                IsSynced = isSynced,
                SyncProgress = syncProgress
            };
        }

        /// <summary>
        /// Creates a list of PhotoModel instances for testing
        /// </summary>
        /// <param name="count">The number of instances to create</param>
        /// <param name="userId">The user ID</param>
        /// <param name="isSynced">Whether the photos are synced</param>
        /// <returns>A list of PhotoModel instances</returns>
        public static List<PhotoModel> CreatePhotoModels(int count = 5, string userId = "user1", bool isSynced = false)
        {
            var photos = new List<PhotoModel>();

            for (int i = 1; i <= count; i++)
            {
                var (latitude, longitude) = GetRandomCoordinates();
                
                photos.Add(new PhotoModel
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    Timestamp = DateTime.UtcNow.AddMinutes(-i),
                    Latitude = latitude,
                    Longitude = longitude,
                    FilePath = $"/test/photo_{i}.jpg",
                    IsSynced = isSynced,
                    SyncProgress = 0
                });
            }

            return photos;
        }

        /// <summary>
        /// Creates a ReportModel instance for testing
        /// </summary>
        /// <param name="id">The report ID</param>
        /// <param name="userId">The user ID</param>
        /// <param name="text">The report text</param>
        /// <param name="timestamp">The timestamp of the report</param>
        /// <param name="latitude">The latitude coordinate</param>
        /// <param name="longitude">The longitude coordinate</param>
        /// <param name="isSynced">Whether the report is synced</param>
        /// <returns>A ReportModel instance with the specified properties</returns>
        public static ReportModel CreateReportModel(
            int id = 1,
            string userId = "user1",
            string text = "Test report",
            DateTime? timestamp = null,
            double latitude = 34.0522,
            double longitude = -118.2437,
            bool isSynced = false)
        {
            return new ReportModel
            {
                Id = id,
                UserId = userId,
                Text = text,
                Timestamp = timestamp ?? DateTime.UtcNow,
                Latitude = latitude,
                Longitude = longitude,
                IsSynced = isSynced
            };
        }

        /// <summary>
        /// Creates a list of ReportModel instances for testing
        /// </summary>
        /// <param name="count">The number of instances to create</param>
        /// <param name="userId">The user ID</param>
        /// <param name="isSynced">Whether the reports are synced</param>
        /// <returns>A list of ReportModel instances</returns>
        public static List<ReportModel> CreateReportModels(int count = 5, string userId = "user1", bool isSynced = false)
        {
            var reports = new List<ReportModel>();

            for (int i = 1; i <= count; i++)
            {
                var (latitude, longitude) = GetRandomCoordinates();
                
                reports.Add(new ReportModel
                {
                    Id = i,
                    UserId = userId,
                    Text = $"Test report {i}",
                    Timestamp = DateTime.UtcNow.AddMinutes(-i),
                    Latitude = latitude,
                    Longitude = longitude,
                    IsSynced = isSynced
                });
            }

            return reports;
        }

        /// <summary>
        /// Creates a PatrolLocationEntity instance for testing
        /// </summary>
        /// <param name="id">The location ID</param>
        /// <param name="name">The name of the location</param>
        /// <param name="latitude">The latitude coordinate</param>
        /// <param name="longitude">The longitude coordinate</param>
        /// <param name="lastUpdated">The last updated timestamp</param>
        /// <param name="remoteId">The remote ID</param>
        /// <returns>A PatrolLocationEntity instance with the specified properties</returns>
        public static PatrolLocationEntity CreatePatrolLocationEntity(
            int id = 1,
            string name = "Test Location",
            double latitude = 34.0522,
            double longitude = -118.2437,
            DateTime? lastUpdated = null,
            string remoteId = "remote1")
        {
            return new PatrolLocationEntity
            {
                Id = id,
                Name = name,
                Latitude = latitude,
                Longitude = longitude,
                LastUpdated = lastUpdated ?? DateTime.UtcNow,
                RemoteId = remoteId
            };
        }

        /// <summary>
        /// Creates a list of PatrolLocationEntity instances for testing
        /// </summary>
        /// <param name="count">The number of instances to create</param>
        /// <returns>A list of PatrolLocationEntity instances</returns>
        public static List<PatrolLocationEntity> CreatePatrolLocationEntities(int count = 5)
        {
            var locations = new List<PatrolLocationEntity>();

            for (int i = 1; i <= count; i++)
            {
                var (latitude, longitude) = GetRandomCoordinates();
                
                locations.Add(new PatrolLocationEntity
                {
                    Id = i,
                    Name = $"Location {i}",
                    Latitude = latitude,
                    Longitude = longitude,
                    LastUpdated = DateTime.UtcNow.AddDays(-i),
                    RemoteId = i.ToString()
                });
            }

            return locations;
        }

        /// <summary>
        /// Generates random geographic coordinates for testing
        /// </summary>
        /// <returns>A tuple containing random latitude and longitude values</returns>
        private static (double latitude, double longitude) GetRandomCoordinates()
        {
            var random = new Random();
            double latitude = random.NextDouble() * 180 - 90;  // -90 to 90
            double longitude = random.NextDouble() * 360 - 180;  // -180 to 180
            return (latitude, longitude);
        }
    }
}