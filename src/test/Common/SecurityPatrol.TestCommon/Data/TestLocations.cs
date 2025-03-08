using System; // System 8.0+
using System.Collections.Generic; // System.Collections.Generic 8.0+
using SecurityPatrol.TestCommon.Constants;
using SecurityPatrol.Database.Entities;
using SecurityPatrol.Models;

namespace SecurityPatrol.TestCommon.Data
{
    /// <summary>
    /// Static class providing predefined test location data for use in unit, integration, and UI tests 
    /// across the Security Patrol application.
    /// </summary>
    public static class TestLocations
    {
        /// <summary>
        /// Gets the default test patrol location entity.
        /// </summary>
        public static PatrolLocationEntity DefaultLocation { get; private set; }

        /// <summary>
        /// Gets a test patrol location entity representing a headquarters location.
        /// </summary>
        public static PatrolLocationEntity HeadquartersLocation { get; private set; }

        /// <summary>
        /// Gets a test patrol location entity representing a warehouse location.
        /// </summary>
        public static PatrolLocationEntity WarehouseLocation { get; private set; }

        /// <summary>
        /// Gets a test patrol location entity representing an office location.
        /// </summary>
        public static PatrolLocationEntity OfficeLocation { get; private set; }

        /// <summary>
        /// Gets a test patrol location entity representing a retail location.
        /// </summary>
        public static PatrolLocationEntity RetailLocation { get; private set; }

        /// <summary>
        /// Gets a list of all predefined test patrol location entities.
        /// </summary>
        public static List<PatrolLocationEntity> AllLocations { get; private set; }

        /// <summary>
        /// Gets the default test location model.
        /// </summary>
        public static LocationModel DefaultLocationModel { get; private set; }

        /// <summary>
        /// Gets a list of test location models.
        /// </summary>
        public static List<LocationModel> LocationModels { get; private set; }

        /// <summary>
        /// Static constructor that initializes all test location data.
        /// </summary>
        static TestLocations()
        {
            // Initialize default location with basic test values
            DefaultLocation = GetTestPatrolLocationEntity(
                TestConstants.TestLocationId, 
                "Default Test Location", 
                TestConstants.TestLatitude, 
                TestConstants.TestLongitude);

            // Initialize specific test locations
            HeadquartersLocation = GetTestPatrolLocationEntity(
                2, 
                "Headquarters", 
                TestConstants.TestLatitude + 0.01, 
                TestConstants.TestLongitude + 0.01);

            WarehouseLocation = GetTestPatrolLocationEntity(
                3, 
                "Warehouse", 
                TestConstants.TestLatitude - 0.01, 
                TestConstants.TestLongitude - 0.01);

            OfficeLocation = GetTestPatrolLocationEntity(
                4, 
                "Office Building", 
                TestConstants.TestLatitude + 0.02, 
                TestConstants.TestLongitude - 0.02);

            RetailLocation = GetTestPatrolLocationEntity(
                5, 
                "Retail Store", 
                TestConstants.TestLatitude - 0.02, 
                TestConstants.TestLongitude + 0.02);

            // Populate AllLocations with all defined test locations
            AllLocations = new List<PatrolLocationEntity>
            {
                DefaultLocation,
                HeadquartersLocation,
                WarehouseLocation,
                OfficeLocation,
                RetailLocation
            };

            // Initialize default location model
            DefaultLocationModel = GetTestLocationModel(
                TestConstants.TestLocationId,
                TestConstants.TestLatitude,
                TestConstants.TestLongitude);

            // Initialize location models list
            LocationModels = GenerateLocationModels(5);
        }

        /// <summary>
        /// Creates a new PatrolLocationEntity instance with specified test values.
        /// </summary>
        /// <param name="id">The identifier for the location.</param>
        /// <param name="name">The name of the location (defaults to "Test Location {id}" if null).</param>
        /// <param name="latitude">The latitude coordinate (defaults to TestLatitude if 0).</param>
        /// <param name="longitude">The longitude coordinate (defaults to TestLongitude if 0).</param>
        /// <returns>A PatrolLocationEntity instance with the specified test values.</returns>
        public static PatrolLocationEntity GetTestPatrolLocationEntity(int id, string name = null, double latitude = 0, double longitude = 0)
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
        /// Creates a new LocationModel instance with specified test values.
        /// </summary>
        /// <param name="id">The identifier for the location model.</param>
        /// <param name="latitude">The latitude coordinate (defaults to TestLatitude if 0).</param>
        /// <param name="longitude">The longitude coordinate (defaults to TestLongitude if 0).</param>
        /// <returns>A LocationModel instance with the specified test values.</returns>
        public static LocationModel GetTestLocationModel(int id, double latitude = 0, double longitude = 0)
        {
            return new LocationModel
            {
                Id = id,
                Latitude = latitude == 0 ? TestConstants.TestLatitude : latitude,
                Longitude = longitude == 0 ? TestConstants.TestLongitude : longitude,
                Accuracy = 10.0,
                Timestamp = DateTime.UtcNow,
                IsSynced = false,
                RemoteId = $"loc_{id}"
            };
        }

        /// <summary>
        /// Generates a list of patrol location entities with test values.
        /// </summary>
        /// <param name="count">The number of entities to generate.</param>
        /// <param name="baseLatitude">The base latitude to use (defaults to TestLatitude if 0).</param>
        /// <param name="baseLongitude">The base longitude to use (defaults to TestLongitude if 0).</param>
        /// <returns>A list of PatrolLocationEntity instances with test values.</returns>
        public static List<PatrolLocationEntity> GeneratePatrolLocations(int count, double baseLatitude = 0, double baseLongitude = 0)
        {
            var locations = new List<PatrolLocationEntity>();

            // Set default values if not provided
            if (baseLatitude == 0) baseLatitude = TestConstants.TestLatitude;
            if (baseLongitude == 0) baseLongitude = TestConstants.TestLongitude;

            for (int i = 1; i <= count; i++)
            {
                // Add small increments to create different but nearby locations
                double latitude = baseLatitude + (i * 0.005);
                double longitude = baseLongitude + (i * 0.005);

                locations.Add(GetTestPatrolLocationEntity(i, $"Generated Location {i}", latitude, longitude));
            }

            return locations;
        }

        /// <summary>
        /// Generates a list of location models with test values.
        /// </summary>
        /// <param name="count">The number of models to generate.</param>
        /// <param name="baseLatitude">The base latitude to use (defaults to TestLatitude if 0).</param>
        /// <param name="baseLongitude">The base longitude to use (defaults to TestLongitude if 0).</param>
        /// <returns>A list of LocationModel instances with test values.</returns>
        public static List<LocationModel> GenerateLocationModels(int count, double baseLatitude = 0, double baseLongitude = 0)
        {
            var models = new List<LocationModel>();

            // Set default values if not provided
            if (baseLatitude == 0) baseLatitude = TestConstants.TestLatitude;
            if (baseLongitude == 0) baseLongitude = TestConstants.TestLongitude;

            for (int i = 1; i <= count; i++)
            {
                // Add small increments to create different but nearby locations
                double latitude = baseLatitude + (i * 0.005);
                double longitude = baseLongitude + (i * 0.005);

                models.Add(GetTestLocationModel(i, latitude, longitude));
            }

            return models;
        }
    }
}