using System;
using System.Collections.Generic;
using SecurityPatrol.TestCommon.Constants;
using SecurityPatrol.TestCommon.Data;
using SecurityPatrol.Database.Entities;
using SecurityPatrol.Models;

namespace SecurityPatrol.TestCommon.Data
{
    /// <summary>
    /// Static class providing predefined test checkpoint data for use in unit, integration, and UI tests
    /// across the Security Patrol application.
    /// </summary>
    public static class TestCheckpoints
    {
        // Individual test checkpoints
        public static CheckpointEntity DefaultCheckpoint { get; private set; }
        public static CheckpointEntity EntranceCheckpoint { get; private set; }
        public static CheckpointEntity LobbyCheckpoint { get; private set; }
        public static CheckpointEntity StairwellCheckpoint { get; private set; }
        public static CheckpointEntity RoofAccessCheckpoint { get; private set; }
        public static CheckpointEntity ParkingCheckpoint { get; private set; }

        // Collections of checkpoints for different locations
        public static List<CheckpointEntity> HeadquartersCheckpoints { get; private set; }
        public static List<CheckpointEntity> WarehouseCheckpoints { get; private set; }
        public static List<CheckpointEntity> OfficeCheckpoints { get; private set; }
        public static List<CheckpointEntity> RetailCheckpoints { get; private set; }
        public static List<CheckpointEntity> AllCheckpoints { get; private set; }

        // Checkpoint model data
        public static CheckpointModel DefaultCheckpointModel { get; private set; }
        public static List<CheckpointModel> CheckpointModels { get; private set; }

        // Checkpoint verification data
        public static CheckpointVerificationEntity DefaultVerification { get; private set; }

        /// <summary>
        /// Static constructor that initializes all test checkpoint data.
        /// </summary>
        static TestCheckpoints()
        {
            // Initialize default checkpoint with basic test values
            DefaultCheckpoint = GetTestCheckpointEntity(
                TestConstants.TestCheckpointId,
                TestConstants.TestLocationId,
                "Default Test Checkpoint",
                TestConstants.TestLatitude,
                TestConstants.TestLongitude);

            // Initialize specific test checkpoints
            EntranceCheckpoint = GetTestCheckpointEntity(
                102,
                TestConstants.TestLocationId,
                "Entrance Checkpoint",
                TestConstants.TestLatitude + 0.001,
                TestConstants.TestLongitude + 0.001);

            LobbyCheckpoint = GetTestCheckpointEntity(
                103,
                TestConstants.TestLocationId,
                "Lobby Checkpoint",
                TestConstants.TestLatitude + 0.002,
                TestConstants.TestLongitude + 0.002);

            StairwellCheckpoint = GetTestCheckpointEntity(
                104,
                TestConstants.TestLocationId,
                "Stairwell Checkpoint",
                TestConstants.TestLatitude + 0.003,
                TestConstants.TestLongitude + 0.003);

            RoofAccessCheckpoint = GetTestCheckpointEntity(
                105,
                TestConstants.TestLocationId,
                "Roof Access Checkpoint",
                TestConstants.TestLatitude + 0.004,
                TestConstants.TestLongitude + 0.004);

            ParkingCheckpoint = GetTestCheckpointEntity(
                106,
                TestConstants.TestLocationId,
                "Parking Checkpoint",
                TestConstants.TestLatitude + 0.005,
                TestConstants.TestLongitude + 0.005);

            // Create checkpoint collections for each location
            HeadquartersCheckpoints = GenerateCheckpoints(
                TestLocations.HeadquartersLocation.Id,
                5,
                TestLocations.HeadquartersLocation.Latitude,
                TestLocations.HeadquartersLocation.Longitude);

            WarehouseCheckpoints = GenerateCheckpoints(
                TestLocations.WarehouseLocation.Id,
                5,
                TestLocations.WarehouseLocation.Latitude,
                TestLocations.WarehouseLocation.Longitude);

            OfficeCheckpoints = GenerateCheckpoints(
                TestLocations.OfficeLocation.Id,
                5,
                TestLocations.OfficeLocation.Latitude,
                TestLocations.OfficeLocation.Longitude);

            RetailCheckpoints = GenerateCheckpoints(
                TestLocations.RetailLocation.Id,
                5,
                TestLocations.RetailLocation.Latitude,
                TestLocations.RetailLocation.Longitude);

            // Combine all checkpoints into a single list
            AllCheckpoints = new List<CheckpointEntity>();
            AllCheckpoints.Add(DefaultCheckpoint);
            AllCheckpoints.Add(EntranceCheckpoint);
            AllCheckpoints.Add(LobbyCheckpoint);
            AllCheckpoints.Add(StairwellCheckpoint);
            AllCheckpoints.Add(RoofAccessCheckpoint);
            AllCheckpoints.Add(ParkingCheckpoint);
            AllCheckpoints.AddRange(HeadquartersCheckpoints);
            AllCheckpoints.AddRange(WarehouseCheckpoints);
            AllCheckpoints.AddRange(OfficeCheckpoints);
            AllCheckpoints.AddRange(RetailCheckpoints);

            // Initialize checkpoint model data
            DefaultCheckpointModel = CheckpointModel.FromEntity(DefaultCheckpoint);
            CheckpointModels = GenerateCheckpointModels(TestConstants.TestLocationId, 5);

            // Initialize checkpoint verification data
            DefaultVerification = GetTestCheckpointVerification(
                1,
                TestConstants.TestUserId,
                TestConstants.TestCheckpointId,
                TestConstants.TestLatitude,
                TestConstants.TestLongitude,
                false);
        }

        /// <summary>
        /// Creates a new CheckpointEntity instance with specified test values.
        /// </summary>
        /// <param name="id">The identifier for the checkpoint.</param>
        /// <param name="locationId">The location identifier the checkpoint belongs to.</param>
        /// <param name="name">The name of the checkpoint (defaults to "Test Checkpoint {id}" if null).</param>
        /// <param name="latitude">The latitude coordinate (defaults to TestLatitude if 0).</param>
        /// <param name="longitude">The longitude coordinate (defaults to TestLongitude if 0).</param>
        /// <returns>A CheckpointEntity instance with the specified test values.</returns>
        public static CheckpointEntity GetTestCheckpointEntity(int id, int locationId = 0, string name = null, double latitude = 0, double longitude = 0)
        {
            return new CheckpointEntity
            {
                Id = id,
                LocationId = locationId == 0 ? TestConstants.TestLocationId : locationId,
                Name = name ?? $"Test Checkpoint {id}",
                Latitude = latitude == 0 ? TestConstants.TestLatitude : latitude,
                Longitude = longitude == 0 ? TestConstants.TestLongitude : longitude,
                LastUpdated = DateTime.UtcNow,
                RemoteId = $"cp_{id}"
            };
        }

        /// <summary>
        /// Creates a new CheckpointModel instance with specified test values.
        /// </summary>
        /// <param name="id">The identifier for the checkpoint model.</param>
        /// <param name="locationId">The location identifier the checkpoint belongs to.</param>
        /// <param name="name">The name of the checkpoint (defaults to "Test Checkpoint {id}" if null).</param>
        /// <param name="latitude">The latitude coordinate (defaults to TestLatitude if 0).</param>
        /// <param name="longitude">The longitude coordinate (defaults to TestLongitude if 0).</param>
        /// <param name="isVerified">Whether the checkpoint is verified.</param>
        /// <returns>A CheckpointModel instance with the specified test values.</returns>
        public static CheckpointModel GetTestCheckpointModel(int id, int locationId = 0, string name = null, double latitude = 0, double longitude = 0, bool isVerified = false)
        {
            var model = new CheckpointModel
            {
                Id = id,
                LocationId = locationId == 0 ? TestConstants.TestLocationId : locationId,
                Name = name ?? $"Test Checkpoint {id}",
                Latitude = latitude == 0 ? TestConstants.TestLatitude : latitude,
                Longitude = longitude == 0 ? TestConstants.TestLongitude : longitude,
                IsVerified = isVerified,
                RemoteId = $"cp_{id}"
            };

            if (isVerified)
            {
                model.VerificationTime = DateTime.UtcNow;
            }

            return model;
        }

        /// <summary>
        /// Creates a new CheckpointVerificationEntity instance with specified test values.
        /// </summary>
        /// <param name="id">The identifier for the verification.</param>
        /// <param name="userId">The user identifier (defaults to TestUserId if null).</param>
        /// <param name="checkpointId">The checkpoint identifier (defaults to TestCheckpointId if 0).</param>
        /// <param name="latitude">The latitude coordinate (defaults to TestLatitude if 0).</param>
        /// <param name="longitude">The longitude coordinate (defaults to TestLongitude if 0).</param>
        /// <param name="isSynced">Whether the verification is synced with the backend.</param>
        /// <returns>A CheckpointVerificationEntity instance with the specified test values.</returns>
        public static CheckpointVerificationEntity GetTestCheckpointVerification(int id, string userId = null, int checkpointId = 0, double latitude = 0, double longitude = 0, bool isSynced = false)
        {
            return new CheckpointVerificationEntity
            {
                Id = id,
                UserId = userId ?? TestConstants.TestUserId,
                CheckpointId = checkpointId == 0 ? TestConstants.TestCheckpointId : checkpointId,
                Timestamp = DateTime.UtcNow,
                Latitude = latitude == 0 ? TestConstants.TestLatitude : latitude,
                Longitude = longitude == 0 ? TestConstants.TestLongitude : longitude,
                IsSynced = isSynced,
                RemoteId = isSynced ? $"ver_{id}" : null
            };
        }

        /// <summary>
        /// Generates a list of checkpoint entities for a specific location with test values.
        /// </summary>
        /// <param name="locationId">The location identifier to associate with the checkpoints.</param>
        /// <param name="count">The number of checkpoints to generate.</param>
        /// <param name="baseLatitude">The base latitude for the first checkpoint.</param>
        /// <param name="baseLongitude">The base longitude for the first checkpoint.</param>
        /// <returns>A list of CheckpointEntity instances with test values.</returns>
        public static List<CheckpointEntity> GenerateCheckpoints(int locationId, int count, double baseLatitude = 0, double baseLongitude = 0)
        {
            var checkpoints = new List<CheckpointEntity>();

            // Set default values if not provided
            if (baseLatitude == 0) baseLatitude = TestConstants.TestLatitude;
            if (baseLongitude == 0) baseLongitude = TestConstants.TestLongitude;

            int startId = 200 + (locationId * 100); // Unique range for each location

            for (int i = 0; i < count; i++)
            {
                // Add small increments to create different but nearby locations
                double latitude = baseLatitude + (i * 0.0005);
                double longitude = baseLongitude + (i * 0.0005);

                string name;
                switch (i % 5)
                {
                    case 0:
                        name = $"Entrance {i + 1}";
                        break;
                    case 1:
                        name = $"Corridor {i + 1}";
                        break;
                    case 2:
                        name = $"Room {i + 1}";
                        break;
                    case 3:
                        name = $"Exit {i + 1}";
                        break;
                    case 4:
                        name = $"Special Area {i + 1}";
                        break;
                    default:
                        name = $"Checkpoint {i + 1}";
                        break;
                }

                checkpoints.Add(GetTestCheckpointEntity(startId + i, locationId, name, latitude, longitude));
            }

            return checkpoints;
        }

        /// <summary>
        /// Generates a list of checkpoint models with test values.
        /// </summary>
        /// <param name="locationId">The location identifier to associate with the checkpoints.</param>
        /// <param name="count">The number of checkpoint models to generate.</param>
        /// <param name="baseLatitude">The base latitude for the first checkpoint.</param>
        /// <param name="baseLongitude">The base longitude for the first checkpoint.</param>
        /// <returns>A list of CheckpointModel instances with test values.</returns>
        public static List<CheckpointModel> GenerateCheckpointModels(int locationId, int count, double baseLatitude = 0, double baseLongitude = 0)
        {
            var models = new List<CheckpointModel>();

            // Set default values if not provided
            if (baseLatitude == 0) baseLatitude = TestConstants.TestLatitude;
            if (baseLongitude == 0) baseLongitude = TestConstants.TestLongitude;

            int startId = 300 + (locationId * 100); // Unique range for each location

            for (int i = 0; i < count; i++)
            {
                // Add small increments to create different but nearby locations
                double latitude = baseLatitude + (i * 0.0005);
                double longitude = baseLongitude + (i * 0.0005);

                // Every third checkpoint is already verified
                bool isVerified = (i % 3 == 0);

                models.Add(GetTestCheckpointModel(startId + i, locationId, $"Model Checkpoint {i + 1}", latitude, longitude, isVerified));
            }

            return models;
        }

        /// <summary>
        /// Generates a list of checkpoint verification entities with test values.
        /// </summary>
        /// <param name="userId">The user identifier for the verifications.</param>
        /// <param name="checkpoints">The list of checkpoints to create verifications for.</param>
        /// <param name="allVerified">Whether all checkpoints should be verified.</param>
        /// <param name="allSynced">Whether all verifications should be marked as synced.</param>
        /// <returns>A list of CheckpointVerificationEntity instances with test values.</returns>
        public static List<CheckpointVerificationEntity> GenerateCheckpointVerifications(string userId = null, List<CheckpointEntity> checkpoints = null, bool allVerified = false, bool allSynced = false)
        {
            var verifications = new List<CheckpointVerificationEntity>();

            if (checkpoints == null || checkpoints.Count == 0)
            {
                return verifications;
            }

            userId = userId ?? TestConstants.TestUserId;

            for (int i = 0; i < checkpoints.Count; i++)
            {
                // Determine if this checkpoint should be verified
                bool shouldVerify = allVerified || (i % 3 == 0); // Every third checkpoint is verified by default

                if (shouldVerify)
                {
                    var verification = GetTestCheckpointVerification(
                        i + 1,
                        userId,
                        checkpoints[i].Id,
                        checkpoints[i].Latitude,
                        checkpoints[i].Longitude,
                        allSynced || (i % 2 == 0) // Half of the verifications are synced by default
                    );

                    verifications.Add(verification);
                }
            }

            return verifications;
        }
    }
}