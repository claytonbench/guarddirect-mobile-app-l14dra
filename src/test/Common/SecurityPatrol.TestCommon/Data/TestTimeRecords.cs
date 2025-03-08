using System;
using System.Collections.Generic;
using SecurityPatrol.TestCommon.Constants;
using SecurityPatrol.Database.Entities;
using SecurityPatrol.Models;
using SecurityPatrol.Core.Entities;

namespace SecurityPatrol.TestCommon.Data
{
    /// <summary>
    /// Static class providing predefined test time record data for use in unit, integration, and UI tests across the Security Patrol application.
    /// </summary>
    public static class TestTimeRecords
    {
        // Predefined TimeRecordEntity instances
        public static TimeRecordEntity DefaultClockInEntity { get; private set; }
        public static TimeRecordEntity DefaultClockOutEntity { get; private set; }
        public static TimeRecordEntity SyncedClockInEntity { get; private set; }
        public static TimeRecordEntity SyncedClockOutEntity { get; private set; }
        public static TimeRecordEntity PendingSyncClockInEntity { get; private set; }
        public static TimeRecordEntity PendingSyncClockOutEntity { get; private set; }
        public static List<TimeRecordEntity> AllTimeRecordEntities { get; private set; }

        // Predefined TimeRecordModel instances
        public static TimeRecordModel DefaultClockInModel { get; private set; }
        public static TimeRecordModel DefaultClockOutModel { get; private set; }
        public static List<TimeRecordModel> AllTimeRecordModels { get; private set; }

        // Predefined backend TimeRecord instances
        public static TimeRecord DefaultBackendClockIn { get; private set; }
        public static TimeRecord DefaultBackendClockOut { get; private set; }
        public static List<TimeRecord> AllBackendTimeRecords { get; private set; }

        /// <summary>
        /// Static constructor that initializes all test time record data
        /// </summary>
        static TestTimeRecords()
        {
            // Initialize TimeRecordEntity instances
            DefaultClockInEntity = new TimeRecordEntity
            {
                Id = 1,
                UserId = TestConstants.TestUserId,
                Type = "ClockIn",
                Timestamp = DateTime.UtcNow.AddHours(-8),
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude,
                IsSynced = false,
                RemoteId = null
            };

            DefaultClockOutEntity = new TimeRecordEntity
            {
                Id = 2,
                UserId = TestConstants.TestUserId,
                Type = "ClockOut",
                Timestamp = DateTime.UtcNow,
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude,
                IsSynced = false,
                RemoteId = null
            };

            SyncedClockInEntity = new TimeRecordEntity
            {
                Id = 3,
                UserId = TestConstants.TestUserId,
                Type = "ClockIn",
                Timestamp = DateTime.UtcNow.AddDays(-1).AddHours(-8),
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude,
                IsSynced = true,
                RemoteId = "remote_1"
            };

            SyncedClockOutEntity = new TimeRecordEntity
            {
                Id = 4,
                UserId = TestConstants.TestUserId,
                Type = "ClockOut",
                Timestamp = DateTime.UtcNow.AddDays(-1),
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude,
                IsSynced = true,
                RemoteId = "remote_2"
            };

            PendingSyncClockInEntity = new TimeRecordEntity
            {
                Id = 5,
                UserId = TestConstants.TestUserId,
                Type = "ClockIn",
                Timestamp = DateTime.UtcNow.AddDays(-2).AddHours(-8),
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude,
                IsSynced = false,
                RemoteId = null
            };

            PendingSyncClockOutEntity = new TimeRecordEntity
            {
                Id = 6,
                UserId = TestConstants.TestUserId,
                Type = "ClockOut",
                Timestamp = DateTime.UtcNow.AddDays(-2),
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude,
                IsSynced = false,
                RemoteId = null
            };

            AllTimeRecordEntities = new List<TimeRecordEntity>
            {
                DefaultClockInEntity,
                DefaultClockOutEntity,
                SyncedClockInEntity,
                SyncedClockOutEntity,
                PendingSyncClockInEntity,
                PendingSyncClockOutEntity
            };

            // Initialize TimeRecordModel instances
            DefaultClockInModel = TimeRecordModel.FromEntity(DefaultClockInEntity);
            DefaultClockOutModel = TimeRecordModel.FromEntity(DefaultClockOutEntity);
            
            AllTimeRecordModels = new List<TimeRecordModel>
            {
                DefaultClockInModel,
                DefaultClockOutModel,
                TimeRecordModel.FromEntity(SyncedClockInEntity),
                TimeRecordModel.FromEntity(SyncedClockOutEntity),
                TimeRecordModel.FromEntity(PendingSyncClockInEntity),
                TimeRecordModel.FromEntity(PendingSyncClockOutEntity)
            };

            // Initialize backend TimeRecord instances
            DefaultBackendClockIn = new TimeRecord
            {
                Id = 1,
                UserId = TestConstants.TestUserId,
                User = TestUsers.DefaultBackendUser,
                Type = "ClockIn",
                Timestamp = DateTime.UtcNow.AddHours(-8),
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude,
                IsSynced = true,
                RemoteId = "remote_1"
            };

            DefaultBackendClockOut = new TimeRecord
            {
                Id = 2,
                UserId = TestConstants.TestUserId,
                User = TestUsers.DefaultBackendUser,
                Type = "ClockOut",
                Timestamp = DateTime.UtcNow,
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude,
                IsSynced = true,
                RemoteId = "remote_2"
            };

            AllBackendTimeRecords = new List<TimeRecord>
            {
                DefaultBackendClockIn,
                DefaultBackendClockOut
            };
        }

        /// <summary>
        /// Gets a time record entity by its ID
        /// </summary>
        /// <param name="id">The ID to search for</param>
        /// <returns>The time record entity with the specified ID, or null if not found</returns>
        public static TimeRecordEntity GetTimeRecordEntityById(int id)
        {
            return AllTimeRecordEntities.Find(r => r.Id == id);
        }

        /// <summary>
        /// Gets a time record model by its ID
        /// </summary>
        /// <param name="id">The ID to search for</param>
        /// <returns>The time record model with the specified ID, or null if not found</returns>
        public static TimeRecordModel GetTimeRecordModelById(int id)
        {
            return AllTimeRecordModels.Find(r => r.Id == id);
        }

        /// <summary>
        /// Gets a backend time record by its ID
        /// </summary>
        /// <param name="id">The ID to search for</param>
        /// <returns>The backend time record with the specified ID, or null if not found</returns>
        public static TimeRecord GetBackendTimeRecordById(int id)
        {
            return AllBackendTimeRecords.Find(r => r.Id == id);
        }

        /// <summary>
        /// Creates a new time record entity with the specified parameters
        /// </summary>
        /// <param name="id">The ID for the time record entity</param>
        /// <param name="userId">The user ID for the time record entity</param>
        /// <param name="type">The type for the time record entity (ClockIn or ClockOut)</param>
        /// <param name="timestamp">The timestamp for the time record entity</param>
        /// <param name="latitude">The latitude for the time record entity</param>
        /// <param name="longitude">The longitude for the time record entity</param>
        /// <param name="isSynced">Whether the time record entity is synced</param>
        /// <param name="remoteId">The remote ID for the time record entity</param>
        /// <returns>A new TimeRecordEntity instance with the specified parameters</returns>
        public static TimeRecordEntity CreateTimeRecordEntity(
            int id,
            string userId = null,
            string type = null,
            DateTime? timestamp = null,
            double latitude = 0,
            double longitude = 0,
            bool isSynced = false,
            string remoteId = null)
        {
            return new TimeRecordEntity
            {
                Id = id,
                UserId = userId ?? TestConstants.TestUserId,
                Type = type ?? "ClockIn",
                Timestamp = timestamp ?? DateTime.UtcNow,
                Latitude = latitude == 0 ? TestConstants.TestLatitude : latitude,
                Longitude = longitude == 0 ? TestConstants.TestLongitude : longitude,
                IsSynced = isSynced,
                RemoteId = remoteId
            };
        }

        /// <summary>
        /// Creates a new time record model with the specified parameters
        /// </summary>
        /// <param name="id">The ID for the time record model</param>
        /// <param name="userId">The user ID for the time record model</param>
        /// <param name="type">The type for the time record model (ClockIn or ClockOut)</param>
        /// <param name="timestamp">The timestamp for the time record model</param>
        /// <param name="latitude">The latitude for the time record model</param>
        /// <param name="longitude">The longitude for the time record model</param>
        /// <param name="isSynced">Whether the time record model is synced</param>
        /// <param name="remoteId">The remote ID for the time record model</param>
        /// <returns>A new TimeRecordModel instance with the specified parameters</returns>
        public static TimeRecordModel CreateTimeRecordModel(
            int id,
            string userId = null,
            string type = null,
            DateTime? timestamp = null,
            double latitude = 0,
            double longitude = 0,
            bool isSynced = false,
            string remoteId = null)
        {
            return new TimeRecordModel
            {
                Id = id,
                UserId = userId ?? TestConstants.TestUserId,
                Type = type ?? "ClockIn",
                Timestamp = timestamp ?? DateTime.UtcNow,
                Latitude = latitude == 0 ? TestConstants.TestLatitude : latitude,
                Longitude = longitude == 0 ? TestConstants.TestLongitude : longitude,
                IsSynced = isSynced,
                RemoteId = remoteId
            };
        }

        /// <summary>
        /// Creates a new backend time record with the specified parameters
        /// </summary>
        /// <param name="id">The ID for the time record</param>
        /// <param name="userId">The user ID for the time record</param>
        /// <param name="type">The type for the time record (ClockIn or ClockOut)</param>
        /// <param name="timestamp">The timestamp for the time record</param>
        /// <param name="latitude">The latitude for the time record</param>
        /// <param name="longitude">The longitude for the time record</param>
        /// <param name="isSynced">Whether the time record is synced</param>
        /// <param name="remoteId">The remote ID for the time record</param>
        /// <returns>A new TimeRecord instance with the specified parameters</returns>
        public static TimeRecord CreateBackendTimeRecord(
            int id,
            string userId = null,
            string type = null,
            DateTime? timestamp = null,
            double latitude = 0,
            double longitude = 0,
            bool isSynced = false,
            string remoteId = null)
        {
            return new TimeRecord
            {
                Id = id,
                UserId = userId ?? TestConstants.TestUserId,
                User = TestUsers.DefaultBackendUser,
                Type = type ?? "ClockIn",
                Timestamp = timestamp ?? DateTime.UtcNow,
                Latitude = latitude == 0 ? TestConstants.TestLatitude : latitude,
                Longitude = longitude == 0 ? TestConstants.TestLongitude : longitude,
                IsSynced = isSynced,
                RemoteId = remoteId
            };
        }

        /// <summary>
        /// Generates a list of time record entities with test values
        /// </summary>
        /// <param name="count">The number of records to generate</param>
        /// <param name="userId">The user ID for the records</param>
        /// <param name="alternateTypes">Whether to alternate between ClockIn and ClockOut types</param>
        /// <returns>A list of TimeRecordEntity instances with test values</returns>
        public static List<TimeRecordEntity> GenerateTimeRecordEntities(int count, string userId = null, bool alternateTypes = true)
        {
            var records = new List<TimeRecordEntity>();
            userId = userId ?? TestConstants.TestUserId;
            var baseTime = DateTime.UtcNow.AddDays(-count);

            for (int i = 1; i <= count; i++)
            {
                var type = alternateTypes ? (i % 2 == 1 ? "ClockIn" : "ClockOut") : "ClockIn";
                var timestamp = alternateTypes
                    ? (type == "ClockIn" ? baseTime.AddDays(i - 1).AddHours(-8) : baseTime.AddDays(i - 1))
                    : baseTime.AddDays(i - 1);

                records.Add(new TimeRecordEntity
                {
                    Id = i,
                    UserId = userId,
                    Type = type,
                    Timestamp = timestamp,
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude,
                    IsSynced = i % 3 == 0, // Every third record is synced
                    RemoteId = i % 3 == 0 ? $"remote_{i}" : null
                });
            }

            return records;
        }

        /// <summary>
        /// Generates a list of time record models with test values
        /// </summary>
        /// <param name="count">The number of records to generate</param>
        /// <param name="userId">The user ID for the records</param>
        /// <param name="alternateTypes">Whether to alternate between ClockIn and ClockOut types</param>
        /// <returns>A list of TimeRecordModel instances with test values</returns>
        public static List<TimeRecordModel> GenerateTimeRecordModels(int count, string userId = null, bool alternateTypes = true)
        {
            var records = new List<TimeRecordModel>();
            userId = userId ?? TestConstants.TestUserId;
            var baseTime = DateTime.UtcNow.AddDays(-count);

            for (int i = 1; i <= count; i++)
            {
                var type = alternateTypes ? (i % 2 == 1 ? "ClockIn" : "ClockOut") : "ClockIn";
                var timestamp = alternateTypes
                    ? (type == "ClockIn" ? baseTime.AddDays(i - 1).AddHours(-8) : baseTime.AddDays(i - 1))
                    : baseTime.AddDays(i - 1);

                records.Add(new TimeRecordModel
                {
                    Id = i,
                    UserId = userId,
                    Type = type,
                    Timestamp = timestamp,
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude,
                    IsSynced = i % 3 == 0, // Every third record is synced
                    RemoteId = i % 3 == 0 ? $"remote_{i}" : null
                });
            }

            return records;
        }

        /// <summary>
        /// Generates a pair of clock in and clock out records for a specific day
        /// </summary>
        /// <param name="startId">The starting ID for the records</param>
        /// <param name="userId">The user ID for the records</param>
        /// <param name="daysAgo">How many days ago the records should be for</param>
        /// <param name="isSynced">Whether the records are synced</param>
        /// <returns>A list containing a clock in and clock out pair</returns>
        public static List<TimeRecordEntity> GenerateClockInOutPair(int startId, string userId = null, int daysAgo = 0, bool isSynced = false)
        {
            userId = userId ?? TestConstants.TestUserId;
            var baseDate = DateTime.UtcNow.AddDays(-daysAgo);
            
            var clockIn = new TimeRecordEntity
            {
                Id = startId,
                UserId = userId,
                Type = "ClockIn",
                Timestamp = baseDate.AddHours(-8),
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude,
                IsSynced = isSynced,
                RemoteId = isSynced ? $"remote_{startId}" : null
            };
            
            var clockOut = new TimeRecordEntity
            {
                Id = startId + 1,
                UserId = userId,
                Type = "ClockOut",
                Timestamp = baseDate,
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude,
                IsSynced = isSynced,
                RemoteId = isSynced ? $"remote_{startId + 1}" : null
            };
            
            return new List<TimeRecordEntity> { clockIn, clockOut };
        }
    }
}