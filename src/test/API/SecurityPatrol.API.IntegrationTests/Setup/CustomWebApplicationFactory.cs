using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SecurityPatrol.API;
using SecurityPatrol.Infrastructure.Persistence;
using SecurityPatrol.TestCommon.Helpers;
using SecurityPatrol.TestCommon.Constants;
using SecurityPatrol.TestCommon.Data;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SecurityPatrol.API.IntegrationTests.Setup
{
    /// <summary>
    /// Custom WebApplicationFactory that configures a test server with in-memory database and test services for integration testing.
    /// </summary>
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        /// <summary>
        /// Gets the name of the in-memory database used for testing.
        /// </summary>
        public string DatabaseName { get; }
        
        private readonly ILogger<CustomWebApplicationFactory> _logger;

        /// <summary>
        /// Initializes a new instance of the CustomWebApplicationFactory class with a unique database name.
        /// </summary>
        public CustomWebApplicationFactory()
        {
            // Generate a unique database name using a GUID to ensure test isolation
            DatabaseName = $"InMemoryDb_{Guid.NewGuid()}";
            
            // Create a logger factory and get a logger for this class
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<CustomWebApplicationFactory>();
        }

        /// <summary>
        /// Configures the web host for testing, replacing services with test implementations.
        /// </summary>
        /// <param name="builder">The web host builder to configure.</param>
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            _logger.LogInformation("Configuring web host for testing");

            builder.ConfigureServices(services =>
            {
                // Find and remove the app's DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<SecurityPatrolDbContext>));
                
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database for testing
                services.AddDbContext<SecurityPatrolDbContext>(options =>
                {
                    options.UseInMemoryDatabase(DatabaseName);
                });

                // Add test authentication services
                services.AddTestAuthentication(
                    userId: TestConstants.TestUserId,
                    phoneNumber: TestConstants.TestPhoneNumber);

                // Create a service provider and seed the database
                var sp = services.BuildServiceProvider();
                using (var scope = sp.CreateScope())
                {
                    var scopedServices = scope.ServiceProvider;
                    SeedTestDatabase(scopedServices);
                }
            });

            // Configure the application to use the test environment
            builder.UseEnvironment("Test");
        }

        /// <summary>
        /// Seeds the in-memory database with test data for integration tests.
        /// </summary>
        /// <param name="serviceProvider">The service provider containing the database context.</param>
        private void SeedTestDatabase(IServiceProvider serviceProvider)
        {
            _logger.LogInformation("Seeding test database");

            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<SecurityPatrolDbContext>();

            // Ensure the database is created
            context.Database.EnsureCreated();

            // Check if the database is already seeded
            if (context.Users.Any())
            {
                _logger.LogInformation("Database already seeded - skipping seed operation");
                return;
            }

            // Add test users
            context.Users.AddRange(TestUsers.AllBackendUsers);

            // Add test patrol locations
            var locations = new List<PatrolLocation>
            {
                new PatrolLocation
                {
                    Id = 1,
                    Name = "Office Building A",
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude,
                    LastUpdated = DateTime.UtcNow,
                    RemoteId = "loc-001"
                },
                new PatrolLocation
                {
                    Id = 2,
                    Name = "Warehouse B",
                    Latitude = TestConstants.TestLatitude + 0.01,
                    Longitude = TestConstants.TestLongitude + 0.01,
                    LastUpdated = DateTime.UtcNow,
                    RemoteId = "loc-002"
                }
            };
            context.PatrolLocations.AddRange(locations);

            // Add test checkpoints for each location
            var checkpoints = new List<Checkpoint>();
            checkpoints.AddRange(CreateCheckpoints(1, TestConstants.TestLatitude, TestConstants.TestLongitude, 5));
            checkpoints.AddRange(CreateCheckpoints(2, TestConstants.TestLatitude + 0.01, TestConstants.TestLongitude + 0.01, 3));
            context.Checkpoints.AddRange(checkpoints);

            // Add test time records
            var timeRecords = new List<TimeRecord>
            {
                new TimeRecord
                {
                    Id = 1,
                    UserId = TestConstants.TestUserId,
                    Type = "ClockIn",
                    Timestamp = DateTime.UtcNow.AddHours(-8),
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude,
                    IsSynced = true,
                    RemoteId = "tr-001"
                },
                new TimeRecord
                {
                    Id = 2,
                    UserId = TestConstants.TestUserId,
                    Type = "ClockOut",
                    Timestamp = DateTime.UtcNow.AddHours(-4),
                    Latitude = TestConstants.TestLatitude + 0.001,
                    Longitude = TestConstants.TestLongitude + 0.001,
                    IsSynced = true,
                    RemoteId = "tr-002"
                }
            };
            context.TimeRecords.AddRange(timeRecords);

            // Add test photos (metadata only)
            var photos = new List<Photo>
            {
                new Photo
                {
                    Id = 1,
                    UserId = TestConstants.TestUserId,
                    Timestamp = DateTime.UtcNow.AddDays(-1),
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude,
                    FilePath = "/test/path/photo1.jpg"
                }
            };
            context.Photos.AddRange(photos);

            // Add test reports
            var reports = new List<Report>
            {
                new Report
                {
                    Id = 1,
                    UserId = TestConstants.TestUserId,
                    Text = TestConstants.TestReportText,
                    Timestamp = DateTime.UtcNow.AddDays(-1),
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude,
                    IsSynced = true,
                    RemoteId = "rep-001"
                }
            };
            context.Reports.AddRange(reports);

            // Add test checkpoint verifications
            var verifications = new List<CheckpointVerification>
            {
                new CheckpointVerification
                {
                    Id = 1,
                    UserId = TestConstants.TestUserId,
                    CheckpointId = TestConstants.TestCheckpointId,
                    Timestamp = DateTime.UtcNow.AddDays(-1),
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude,
                    IsSynced = true,
                    RemoteId = "cv-001"
                }
            };
            context.CheckpointVerifications.AddRange(verifications);

            // Save all changes to the in-memory database
            context.SaveChanges();
            _logger.LogInformation("Test database has been seeded successfully");
        }

        /// <summary>
        /// Creates test checkpoint entities for a specific location.
        /// </summary>
        /// <param name="locationId">The location ID to associate with the checkpoints.</param>
        /// <param name="baseLat">The base latitude for calculating checkpoint coordinates.</param>
        /// <param name="baseLng">The base longitude for calculating checkpoint coordinates.</param>
        /// <param name="count">The number of checkpoints to create.</param>
        /// <returns>A list of checkpoint entities.</returns>
        private List<Checkpoint> CreateCheckpoints(int locationId, double baseLat, double baseLng, int count)
        {
            var checkpoints = new List<Checkpoint>();
            
            for (int i = 1; i <= count; i++)
            {
                // Create a checkpoint with calculated coordinates
                var latOffset = (i * 0.0001) % 0.001;
                var lngOffset = (i * 0.0002) % 0.001;
                
                checkpoints.Add(new Checkpoint
                {
                    Id = 100 + (locationId * 10) + i,
                    LocationId = locationId,
                    Name = $"Checkpoint {locationId}-{i}",
                    Latitude = baseLat + latOffset,
                    Longitude = baseLng + lngOffset,
                    LastUpdated = DateTime.UtcNow,
                    RemoteId = $"cp-{locationId}-{i}"
                });
            }
            
            return checkpoints;
        }

        /// <summary>
        /// Disposes the web application factory and cleans up resources.
        /// </summary>
        /// <param name="disposing">Whether the factory is being disposed.</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            // If disposing is true, perform additional cleanup if needed
        }
    }
}