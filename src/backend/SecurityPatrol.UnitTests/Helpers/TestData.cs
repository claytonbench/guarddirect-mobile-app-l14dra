using System;
using System.Collections.Generic;
using System.Linq;
using SecurityPatrol.Core.Entities;

namespace SecurityPatrol.UnitTests.Helpers
{
    /// <summary>
    /// Static class providing test data for unit tests in the Security Patrol application.
    /// </summary>
    public static class TestData
    {
        /// <summary>
        /// Private constructor to prevent instantiation of static class.
        /// </summary>
        private TestData() { }

        /// <summary>
        /// Returns a list of test User entities for unit testing.
        /// </summary>
        /// <returns>A list of predefined User entities.</returns>
        public static List<User> GetTestUsers()
        {
            return new List<User>
            {
                new User
                {
                    Id = "user1",
                    PhoneNumber = "+15551234567",
                    LastAuthenticated = DateTime.UtcNow.AddDays(-1),
                    IsActive = true,
                    Created = DateTime.UtcNow.AddDays(-30),
                    CreatedBy = "system"
                },
                new User
                {
                    Id = "user2",
                    PhoneNumber = "+15559876543",
                    LastAuthenticated = DateTime.UtcNow.AddHours(-2),
                    IsActive = true,
                    Created = DateTime.UtcNow.AddDays(-15),
                    CreatedBy = "system"
                },
                new User
                {
                    Id = "user3",
                    PhoneNumber = "+15551112222",
                    LastAuthenticated = DateTime.UtcNow.AddDays(-10),
                    IsActive = false,
                    Created = DateTime.UtcNow.AddDays(-45),
                    CreatedBy = "system",
                    LastModified = DateTime.UtcNow.AddDays(-10),
                    LastModifiedBy = "admin"
                }
            };
        }

        /// <summary>
        /// Returns a specific test User entity by ID.
        /// </summary>
        /// <param name="id">The user ID to search for.</param>
        /// <returns>The User entity with the specified ID, or null if not found.</returns>
        public static User GetTestUserById(string id)
        {
            return GetTestUsers().FirstOrDefault(u => u.Id == id);
        }

        /// <summary>
        /// Returns a specific test User entity by phone number.
        /// </summary>
        /// <param name="phoneNumber">The phone number to search for.</param>
        /// <returns>The User entity with the specified phone number, or null if not found.</returns>
        public static User GetTestUserByPhoneNumber(string phoneNumber)
        {
            return GetTestUsers().FirstOrDefault(u => u.PhoneNumber == phoneNumber);
        }

        /// <summary>
        /// Returns a list of test TimeRecord entities for unit testing.
        /// </summary>
        /// <returns>A list of predefined TimeRecord entities.</returns>
        public static List<TimeRecord> GetTestTimeRecords()
        {
            return new List<TimeRecord>
            {
                new TimeRecord
                {
                    Id = 1,
                    UserId = "user1",
                    Type = "ClockIn",
                    Timestamp = DateTime.UtcNow.AddHours(-8),
                    Latitude = 40.7128,
                    Longitude = -74.0060,
                    IsSynced = true,
                    RemoteId = "remote-tr-1",
                    Created = DateTime.UtcNow.AddHours(-8),
                    CreatedBy = "user1"
                },
                new TimeRecord
                {
                    Id = 2,
                    UserId = "user1",
                    Type = "ClockOut",
                    Timestamp = DateTime.UtcNow.AddHours(-1),
                    Latitude = 40.7130,
                    Longitude = -74.0065,
                    IsSynced = true,
                    RemoteId = "remote-tr-2",
                    Created = DateTime.UtcNow.AddHours(-1),
                    CreatedBy = "user1"
                },
                new TimeRecord
                {
                    Id = 3,
                    UserId = "user2",
                    Type = "ClockIn",
                    Timestamp = DateTime.UtcNow.AddHours(-4),
                    Latitude = 34.0522,
                    Longitude = -118.2437,
                    IsSynced = true,
                    RemoteId = "remote-tr-3",
                    Created = DateTime.UtcNow.AddHours(-4),
                    CreatedBy = "user2"
                },
                new TimeRecord
                {
                    Id = 4,
                    UserId = "user2",
                    Type = "ClockOut",
                    Timestamp = DateTime.UtcNow.AddMinutes(-30),
                    Latitude = 34.0523,
                    Longitude = -118.2438,
                    IsSynced = false,
                    RemoteId = null,
                    Created = DateTime.UtcNow.AddMinutes(-30),
                    CreatedBy = "user2"
                }
            };
        }

        /// <summary>
        /// Returns a specific test TimeRecord entity by ID.
        /// </summary>
        /// <param name="id">The time record ID to search for.</param>
        /// <returns>The TimeRecord entity with the specified ID, or null if not found.</returns>
        public static TimeRecord GetTestTimeRecordById(int id)
        {
            return GetTestTimeRecords().FirstOrDefault(tr => tr.Id == id);
        }

        /// <summary>
        /// Returns a list of test LocationRecord entities for unit testing.
        /// </summary>
        /// <returns>A list of predefined LocationRecord entities.</returns>
        public static List<LocationRecord> GetTestLocationRecords()
        {
            return new List<LocationRecord>
            {
                new LocationRecord
                {
                    Id = 1,
                    UserId = "user1",
                    Timestamp = DateTime.UtcNow.AddHours(-7),
                    Latitude = 40.7128,
                    Longitude = -74.0060,
                    Accuracy = 10.5,
                    IsSynced = true,
                    RemoteId = "remote-loc-1",
                    Created = DateTime.UtcNow.AddHours(-7),
                    CreatedBy = "system"
                },
                new LocationRecord
                {
                    Id = 2,
                    UserId = "user1",
                    Timestamp = DateTime.UtcNow.AddHours(-6),
                    Latitude = 40.7135,
                    Longitude = -74.0070,
                    Accuracy = 8.2,
                    IsSynced = true,
                    RemoteId = "remote-loc-2",
                    Created = DateTime.UtcNow.AddHours(-6),
                    CreatedBy = "system"
                },
                new LocationRecord
                {
                    Id = 3,
                    UserId = "user2",
                    Timestamp = DateTime.UtcNow.AddHours(-3),
                    Latitude = 34.0522,
                    Longitude = -118.2437,
                    Accuracy = 12.7,
                    IsSynced = true,
                    RemoteId = "remote-loc-3",
                    Created = DateTime.UtcNow.AddHours(-3),
                    CreatedBy = "system"
                },
                new LocationRecord
                {
                    Id = 4,
                    UserId = "user2",
                    Timestamp = DateTime.UtcNow.AddHours(-2),
                    Latitude = 34.0530,
                    Longitude = -118.2445,
                    Accuracy = 9.3,
                    IsSynced = false,
                    RemoteId = null,
                    Created = DateTime.UtcNow.AddHours(-2),
                    CreatedBy = "system"
                }
            };
        }

        /// <summary>
        /// Returns a specific test LocationRecord entity by ID.
        /// </summary>
        /// <param name="id">The location record ID to search for.</param>
        /// <returns>The LocationRecord entity with the specified ID, or null if not found.</returns>
        public static LocationRecord GetTestLocationRecordById(int id)
        {
            return GetTestLocationRecords().FirstOrDefault(lr => lr.Id == id);
        }

        /// <summary>
        /// Returns a list of test PatrolLocation entities for unit testing.
        /// </summary>
        /// <returns>A list of predefined PatrolLocation entities.</returns>
        public static List<PatrolLocation> GetTestPatrolLocations()
        {
            var patrolLocations = new List<PatrolLocation>
            {
                new PatrolLocation
                {
                    Id = 1,
                    Name = "Office Complex A",
                    Latitude = 40.7128,
                    Longitude = -74.0060,
                    LastUpdated = DateTime.UtcNow.AddDays(-5),
                    RemoteId = "remote-pl-1",
                    Created = DateTime.UtcNow.AddDays(-30),
                    CreatedBy = "admin"
                },
                new PatrolLocation
                {
                    Id = 2,
                    Name = "Warehouse B",
                    Latitude = 34.0522,
                    Longitude = -118.2437,
                    LastUpdated = DateTime.UtcNow.AddDays(-3),
                    RemoteId = "remote-pl-2",
                    Created = DateTime.UtcNow.AddDays(-25),
                    CreatedBy = "admin"
                },
                new PatrolLocation
                {
                    Id = 3,
                    Name = "Shopping Center C",
                    Latitude = 41.8781,
                    Longitude = -87.6298,
                    LastUpdated = DateTime.UtcNow.AddDays(-1),
                    RemoteId = "remote-pl-3",
                    Created = DateTime.UtcNow.AddDays(-15),
                    CreatedBy = "admin"
                }
            };

            // Add checkpoints to each patrol location
            patrolLocations[0].Checkpoints = GetTestCheckpoints().Where(c => c.LocationId == 1).ToList();
            patrolLocations[1].Checkpoints = GetTestCheckpoints().Where(c => c.LocationId == 2).ToList();
            patrolLocations[2].Checkpoints = GetTestCheckpoints().Where(c => c.LocationId == 3).ToList();

            return patrolLocations;
        }

        /// <summary>
        /// Returns a specific test PatrolLocation entity by ID.
        /// </summary>
        /// <param name="id">The patrol location ID to search for.</param>
        /// <returns>The PatrolLocation entity with the specified ID, or null if not found.</returns>
        public static PatrolLocation GetTestPatrolLocationById(int id)
        {
            return GetTestPatrolLocations().FirstOrDefault(pl => pl.Id == id);
        }

        /// <summary>
        /// Returns a list of test Checkpoint entities for unit testing.
        /// </summary>
        /// <returns>A list of predefined Checkpoint entities.</returns>
        public static List<Checkpoint> GetTestCheckpoints()
        {
            return new List<Checkpoint>
            {
                new Checkpoint
                {
                    Id = 1,
                    LocationId = 1,
                    Name = "Main Entrance",
                    Latitude = 40.7129,
                    Longitude = -74.0061,
                    LastUpdated = DateTime.UtcNow.AddDays(-5),
                    RemoteId = "remote-cp-1",
                    Created = DateTime.UtcNow.AddDays(-30),
                    CreatedBy = "admin"
                },
                new Checkpoint
                {
                    Id = 2,
                    LocationId = 1,
                    Name = "Server Room",
                    Latitude = 40.7130,
                    Longitude = -74.0063,
                    LastUpdated = DateTime.UtcNow.AddDays(-5),
                    RemoteId = "remote-cp-2",
                    Created = DateTime.UtcNow.AddDays(-30),
                    CreatedBy = "admin"
                },
                new Checkpoint
                {
                    Id = 3,
                    LocationId = 1,
                    Name = "Parking Garage",
                    Latitude = 40.7127,
                    Longitude = -74.0065,
                    LastUpdated = DateTime.UtcNow.AddDays(-5),
                    RemoteId = "remote-cp-3",
                    Created = DateTime.UtcNow.AddDays(-30),
                    CreatedBy = "admin"
                },
                new Checkpoint
                {
                    Id = 4,
                    LocationId = 2,
                    Name = "Loading Dock",
                    Latitude = 34.0523,
                    Longitude = -118.2438,
                    LastUpdated = DateTime.UtcNow.AddDays(-3),
                    RemoteId = "remote-cp-4",
                    Created = DateTime.UtcNow.AddDays(-25),
                    CreatedBy = "admin"
                },
                new Checkpoint
                {
                    Id = 5,
                    LocationId = 2,
                    Name = "Storage Area A",
                    Latitude = 34.0525,
                    Longitude = -118.2440,
                    LastUpdated = DateTime.UtcNow.AddDays(-3),
                    RemoteId = "remote-cp-5",
                    Created = DateTime.UtcNow.AddDays(-25),
                    CreatedBy = "admin"
                },
                new Checkpoint
                {
                    Id = 6,
                    LocationId = 3,
                    Name = "North Entrance",
                    Latitude = 41.8782,
                    Longitude = -87.6299,
                    LastUpdated = DateTime.UtcNow.AddDays(-1),
                    RemoteId = "remote-cp-6",
                    Created = DateTime.UtcNow.AddDays(-15),
                    CreatedBy = "admin"
                },
                new Checkpoint
                {
                    Id = 7,
                    LocationId = 3,
                    Name = "Food Court",
                    Latitude = 41.8783,
                    Longitude = -87.6300,
                    LastUpdated = DateTime.UtcNow.AddDays(-1),
                    RemoteId = "remote-cp-7",
                    Created = DateTime.UtcNow.AddDays(-15),
                    CreatedBy = "admin"
                }
            };
        }

        /// <summary>
        /// Returns a specific test Checkpoint entity by ID.
        /// </summary>
        /// <param name="id">The checkpoint ID to search for.</param>
        /// <returns>The Checkpoint entity with the specified ID, or null if not found.</returns>
        public static Checkpoint GetTestCheckpointById(int id)
        {
            return GetTestCheckpoints().FirstOrDefault(cp => cp.Id == id);
        }

        /// <summary>
        /// Returns a list of test CheckpointVerification entities for unit testing.
        /// </summary>
        /// <returns>A list of predefined CheckpointVerification entities.</returns>
        public static List<CheckpointVerification> GetTestCheckpointVerifications()
        {
            return new List<CheckpointVerification>
            {
                new CheckpointVerification
                {
                    Id = 1,
                    UserId = "user1",
                    CheckpointId = 1,
                    Timestamp = DateTime.UtcNow.AddHours(-7),
                    Latitude = 40.7129,
                    Longitude = -74.0061,
                    IsSynced = true,
                    RemoteId = "remote-cv-1",
                    Created = DateTime.UtcNow.AddHours(-7),
                    CreatedBy = "user1"
                },
                new CheckpointVerification
                {
                    Id = 2,
                    UserId = "user1",
                    CheckpointId = 2,
                    Timestamp = DateTime.UtcNow.AddHours(-6),
                    Latitude = 40.7130,
                    Longitude = -74.0063,
                    IsSynced = true,
                    RemoteId = "remote-cv-2",
                    Created = DateTime.UtcNow.AddHours(-6),
                    CreatedBy = "user1"
                },
                new CheckpointVerification
                {
                    Id = 3,
                    UserId = "user1",
                    CheckpointId = 3,
                    Timestamp = DateTime.UtcNow.AddHours(-5),
                    Latitude = 40.7127,
                    Longitude = -74.0065,
                    IsSynced = true,
                    RemoteId = "remote-cv-3",
                    Created = DateTime.UtcNow.AddHours(-5),
                    CreatedBy = "user1"
                },
                new CheckpointVerification
                {
                    Id = 4,
                    UserId = "user2",
                    CheckpointId = 4,
                    Timestamp = DateTime.UtcNow.AddHours(-3),
                    Latitude = 34.0523,
                    Longitude = -118.2438,
                    IsSynced = true,
                    RemoteId = "remote-cv-4",
                    Created = DateTime.UtcNow.AddHours(-3),
                    CreatedBy = "user2"
                },
                new CheckpointVerification
                {
                    Id = 5,
                    UserId = "user2",
                    CheckpointId = 5,
                    Timestamp = DateTime.UtcNow.AddHours(-2),
                    Latitude = 34.0525,
                    Longitude = -118.2440,
                    IsSynced = false,
                    RemoteId = null,
                    Created = DateTime.UtcNow.AddHours(-2),
                    CreatedBy = "user2"
                }
            };
        }

        /// <summary>
        /// Returns a specific test CheckpointVerification entity by ID.
        /// </summary>
        /// <param name="id">The checkpoint verification ID to search for.</param>
        /// <returns>The CheckpointVerification entity with the specified ID, or null if not found.</returns>
        public static CheckpointVerification GetTestCheckpointVerificationById(int id)
        {
            return GetTestCheckpointVerifications().FirstOrDefault(cv => cv.Id == id);
        }

        /// <summary>
        /// Returns a list of test Photo entities for unit testing.
        /// </summary>
        /// <returns>A list of predefined Photo entities.</returns>
        public static List<Photo> GetTestPhotos()
        {
            return new List<Photo>
            {
                new Photo
                {
                    Id = 1,
                    UserId = "user1",
                    Timestamp = DateTime.UtcNow.AddHours(-6),
                    Latitude = 40.7129,
                    Longitude = -74.0061,
                    FilePath = "/storage/photos/photo1.jpg",
                    IsSynced = true,
                    RemoteId = "remote-photo-1",
                    Created = DateTime.UtcNow.AddHours(-6),
                    CreatedBy = "user1"
                },
                new Photo
                {
                    Id = 2,
                    UserId = "user1",
                    Timestamp = DateTime.UtcNow.AddHours(-5.5),
                    Latitude = 40.7130,
                    Longitude = -74.0063,
                    FilePath = "/storage/photos/photo2.jpg",
                    IsSynced = true,
                    RemoteId = "remote-photo-2",
                    Created = DateTime.UtcNow.AddHours(-5.5),
                    CreatedBy = "user1"
                },
                new Photo
                {
                    Id = 3,
                    UserId = "user2",
                    Timestamp = DateTime.UtcNow.AddHours(-2.5),
                    Latitude = 34.0525,
                    Longitude = -118.2440,
                    FilePath = "/storage/photos/photo3.jpg",
                    IsSynced = false,
                    RemoteId = null,
                    Created = DateTime.UtcNow.AddHours(-2.5),
                    CreatedBy = "user2"
                }
            };
        }

        /// <summary>
        /// Returns a specific test Photo entity by ID.
        /// </summary>
        /// <param name="id">The photo ID to search for.</param>
        /// <returns>The Photo entity with the specified ID, or null if not found.</returns>
        public static Photo GetTestPhotoById(int id)
        {
            return GetTestPhotos().FirstOrDefault(p => p.Id == id);
        }

        /// <summary>
        /// Returns a list of test Report entities for unit testing.
        /// </summary>
        /// <returns>A list of predefined Report entities.</returns>
        public static List<Report> GetTestReports()
        {
            return new List<Report>
            {
                new Report
                {
                    Id = 1,
                    UserId = "user1",
                    Text = "Found main entrance door unlocked during patrol. Secured the door and notified building management.",
                    Timestamp = DateTime.UtcNow.AddHours(-6.5),
                    Latitude = 40.7129,
                    Longitude = -74.0061,
                    IsSynced = true,
                    RemoteId = "remote-rep-1",
                    Created = DateTime.UtcNow.AddHours(-6.5),
                    CreatedBy = "user1"
                },
                new Report
                {
                    Id = 2,
                    UserId = "user1",
                    Text = "Suspicious activity observed in parking garage. Individual taking photos of security cameras. Approached individual who identified as contractor and verified credentials.",
                    Timestamp = DateTime.UtcNow.AddHours(-5.2),
                    Latitude = 40.7127,
                    Longitude = -74.0065,
                    IsSynced = true,
                    RemoteId = "remote-rep-2",
                    Created = DateTime.UtcNow.AddHours(-5.2),
                    CreatedBy = "user1"
                },
                new Report
                {
                    Id = 3,
                    UserId = "user2",
                    Text = "Water leak detected in storage area. Notified maintenance department and cordoned off the affected area.",
                    Timestamp = DateTime.UtcNow.AddHours(-2.2),
                    Latitude = 34.0525,
                    Longitude = -118.2440,
                    IsSynced = false,
                    RemoteId = null,
                    Created = DateTime.UtcNow.AddHours(-2.2),
                    CreatedBy = "user2"
                }
            };
        }

        /// <summary>
        /// Returns a specific test Report entity by ID.
        /// </summary>
        /// <param name="id">The report ID to search for.</param>
        /// <returns>The Report entity with the specified ID, or null if not found.</returns>
        public static Report GetTestReportById(int id)
        {
            return GetTestReports().FirstOrDefault(r => r.Id == id);
        }
    }
}