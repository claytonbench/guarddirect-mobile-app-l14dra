using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Infrastructure.Persistence;

namespace SecurityPatrol.IntegrationTests.Helpers
{
    /// <summary>
    /// Static utility class providing helper methods for integration testing
    /// </summary>
    public static class Utilities
    {
        // Predefined constants for test data
        private const string TestUserId = "11111111-1111-1111-1111-111111111111";
        private const string TestPhoneNumber = "+15555555555";

        /// <summary>
        /// Reinitializes the database with fresh test data for integration tests
        /// </summary>
        /// <param name="context">The database context</param>
        public static void ReinitializeDbForTests(SecurityPatrolDbContext context)
        {
            // Clear all existing data
            ClearDatabase(context);
            
            // Initialize with fresh test data
            InitializeTestData(context);
            
            // Save changes to the database
            context.SaveChanges();
        }

        /// <summary>
        /// Initializes the database with test data for integration tests
        /// </summary>
        /// <param name="context">The database context</param>
        public static void InitializeTestData(SecurityPatrolDbContext context)
        {
            // Add test user
            var user = CreateTestUser();
            context.Users.Add(user);
            
            // Add test patrol locations
            var patrolLocations = CreateTestPatrolLocations();
            context.PatrolLocations.AddRange(patrolLocations);
            
            // Add test checkpoints for each patrol location
            foreach (var location in patrolLocations)
            {
                var checkpoints = CreateTestCheckpoints(location);
                context.Checkpoints.AddRange(checkpoints);
            }
            
            // Save changes to the database
            context.SaveChanges();
        }

        /// <summary>
        /// Creates a test user with predefined ID and phone number
        /// </summary>
        /// <returns>A test user entity</returns>
        public static User CreateTestUser()
        {
            return new User
            {
                Id = TestUserId,
                PhoneNumber = TestPhoneNumber,
                IsActive = true,
                LastAuthenticated = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Creates a collection of test patrol locations
        /// </summary>
        /// <returns>A list of test patrol locations</returns>
        public static List<PatrolLocation> CreateTestPatrolLocations()
        {
            return new List<PatrolLocation>
            {
                new PatrolLocation
                {
                    Id = 1,
                    Name = "Office Building A",
                    Latitude = 37.7749,
                    Longitude = -122.4194
                },
                new PatrolLocation
                {
                    Id = 2,
                    Name = "Warehouse District",
                    Latitude = 37.7833,
                    Longitude = -122.4167
                },
                new PatrolLocation
                {
                    Id = 3,
                    Name = "Shopping Mall",
                    Latitude = 37.7900,
                    Longitude = -122.4000
                }
            };
        }

        /// <summary>
        /// Creates test checkpoints for a patrol location
        /// </summary>
        /// <param name="location">The patrol location</param>
        /// <returns>A list of test checkpoints for the location</returns>
        public static List<Checkpoint> CreateTestCheckpoints(PatrolLocation location)
        {
            var checkpoints = new List<Checkpoint>();
            
            switch (location.Id)
            {
                case 1: // Office Building A
                    checkpoints.Add(new Checkpoint 
                    { 
                        Id = 1, 
                        LocationId = location.Id,
                        Name = "Main Entrance", 
                        Latitude = location.Latitude + 0.001, 
                        Longitude = location.Longitude + 0.001 
                    });
                    checkpoints.Add(new Checkpoint 
                    { 
                        Id = 2, 
                        LocationId = location.Id,
                        Name = "Parking Garage", 
                        Latitude = location.Latitude - 0.001, 
                        Longitude = location.Longitude - 0.001 
                    });
                    checkpoints.Add(new Checkpoint 
                    { 
                        Id = 3, 
                        LocationId = location.Id,
                        Name = "Loading Dock", 
                        Latitude = location.Latitude + 0.002, 
                        Longitude = location.Longitude - 0.002 
                    });
                    break;
                
                case 2: // Warehouse District
                    checkpoints.Add(new Checkpoint 
                    { 
                        Id = 4, 
                        LocationId = location.Id,
                        Name = "North Gate", 
                        Latitude = location.Latitude + 0.001, 
                        Longitude = location.Longitude + 0.001 
                    });
                    checkpoints.Add(new Checkpoint 
                    { 
                        Id = 5, 
                        LocationId = location.Id,
                        Name = "South Gate", 
                        Latitude = location.Latitude - 0.001, 
                        Longitude = location.Longitude - 0.001 
                    });
                    break;
                
                case 3: // Shopping Mall
                    checkpoints.Add(new Checkpoint 
                    { 
                        Id = 6, 
                        LocationId = location.Id,
                        Name = "Food Court", 
                        Latitude = location.Latitude + 0.001, 
                        Longitude = location.Longitude + 0.001 
                    });
                    checkpoints.Add(new Checkpoint 
                    { 
                        Id = 7, 
                        LocationId = location.Id,
                        Name = "Main Entrance", 
                        Latitude = location.Latitude - 0.001, 
                        Longitude = location.Longitude - 0.001 
                    });
                    checkpoints.Add(new Checkpoint 
                    { 
                        Id = 8, 
                        LocationId = location.Id,
                        Name = "Parking Level 1", 
                        Latitude = location.Latitude + 0.002, 
                        Longitude = location.Longitude - 0.002 
                    });
                    checkpoints.Add(new Checkpoint 
                    { 
                        Id = 9, 
                        LocationId = location.Id,
                        Name = "Back Alley", 
                        Latitude = location.Latitude - 0.002, 
                        Longitude = location.Longitude + 0.002 
                    });
                    break;
            }
            
            return checkpoints;
        }

        /// <summary>
        /// Clears all data from the database tables
        /// </summary>
        /// <param name="context">The database context</param>
        public static void ClearDatabase(SecurityPatrolDbContext context)
        {
            // Remove all checkpoint verifications
            context.CheckpointVerifications.RemoveRange(context.CheckpointVerifications);
            
            // Remove all activity reports
            context.Reports.RemoveRange(context.Reports);
            
            // Remove all photos
            context.Photos.RemoveRange(context.Photos);
            
            // Remove all location records
            context.LocationRecords.RemoveRange(context.LocationRecords);
            
            // Remove all time records
            context.TimeRecords.RemoveRange(context.TimeRecords);
            
            // Remove all checkpoints
            context.Checkpoints.RemoveRange(context.Checkpoints);
            
            // Remove all patrol locations
            context.PatrolLocations.RemoveRange(context.PatrolLocations);
            
            // Remove all users
            context.Users.RemoveRange(context.Users);
            
            // Save changes to the database
            context.SaveChanges();
        }

        /// <summary>
        /// Creates StringContent with JSON serialized object for HTTP requests
        /// </summary>
        /// <param name="obj">The object to serialize</param>
        /// <returns>JSON content for HTTP request</returns>
        public static StringContent CreateJsonContent(object obj)
        {
            return new StringContent(
                JsonSerializer.Serialize(obj),
                Encoding.UTF8,
                "application/json");
        }

        /// <summary>
        /// Gets the predefined test user ID used in integration tests
        /// </summary>
        /// <returns>The test user ID</returns>
        public static string GetTestUserId()
        {
            return TestUserId;
        }

        /// <summary>
        /// Gets the predefined test phone number used in integration tests
        /// </summary>
        /// <returns>The test phone number</returns>
        public static string GetTestPhoneNumber()
        {
            return TestPhoneNumber;
        }
    }
}