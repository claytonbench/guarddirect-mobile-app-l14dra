using System; // Version 8.0.0
using System.Threading.Tasks; // Version 8.0.0
using Microsoft.Extensions.Logging; // Version 8.0.0
using Xunit; // Version 2.4.2
using FluentAssertions; // Version 6.11.0
using SecurityPatrol.MAUI.IntegrationTests.Setup;
using SecurityPatrol.Database.Migrations;
using SQLiteNetExtensions; // Version 2.1.0
using SecurityPatrol.TestCommon.Fixtures;
using SecurityPatrol.Database.Entities;

namespace SecurityPatrol.MAUI.IntegrationTests.Database
{
    /// <summary>
    /// Integration tests for database migrations in the Security Patrol application.
    /// </summary>
    [public]
    public class MigrationIntegrationTests : IntegrationTestBase
    {
        private readonly ILogger<MigrationIntegrationTests> _logger;
        private readonly MigrationManager _migrationManager;

        /// <summary>
        /// Initializes a new instance of the MigrationIntegrationTests class.
        /// </summary>
        public MigrationIntegrationTests()
        {
            // Create a logger factory for testing
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddConsole()
                    .SetMinimumLevel(LogLevel.Information);
            });

            // Create a logger for the test class
            _logger = loggerFactory.CreateLogger<MigrationIntegrationTests>();

            // Initialize _migrationManager with a new MigrationManager instance
            _migrationManager = new MigrationManager(_logger);
        }

        /// <summary>
        /// Tests that the MigrationManager can be properly initialized.
        /// </summary>
        [Fact]
        [public]
        [async]
        public async Task TestMigrationManagerInitialization()
        {
            // Create a new MigrationManager instance
            var migrationManager = new MigrationManager();

            // Verify that the instance is not null
            migrationManager.Should().NotBeNull();

            // Log successful test completion
            _logger.LogInformation("TestMigrationManagerInitialization completed successfully");
        }

        /// <summary>
        /// Tests that migrations can be applied from an initial state (version 0.0).
        /// </summary>
        [Fact]
        [public]
        [async]
        public async Task TestApplyMigrationsFromInitialState()
        {
            // Reset the test database to ensure a clean state
            await Database.ResetDatabaseAsync();

            // Get the database connection from the test fixture
            var connection = Database.Connection;

            // Apply migrations starting from version 0.0
            double newVersion = await _migrationManager.ApplyMigrationsAsync(connection, 0.0);

            // Verify that the returned version is 1.1 (latest migration)
            newVersion.Should().Be(1.1);

            // Verify that the database schema includes the expected changes
            await VerifyMigration1_1Changes();

            // Log successful test completion
            _logger.LogInformation("TestApplyMigrationsFromInitialState completed successfully");
        }

        /// <summary>
        /// Tests that migrations can be applied from version 1.0 to 1.1.
        /// </summary>
        [Fact]
        [public]
        [async]
        public async Task TestApplyMigrationsFromVersion1_0()
        {
            // Reset the test database to ensure a clean state
            await Database.ResetDatabaseAsync();

            // Get the database connection from the test fixture
            var connection = Database.Connection;

            // Apply only the initial migration (1.0) manually
            var migration1_0 = new Migration_1_0();
            await migration1_0.ApplyAsync(connection);

            // Apply migrations starting from version 1.0
            double newVersion = await _migrationManager.ApplyMigrationsAsync(connection, 1.0);

            // Verify that the returned version is 1.1
            newVersion.Should().Be(1.1);

            // Verify that the database schema includes the expected changes from migration 1.1
            await VerifyMigration1_1Changes();

            // Log successful test completion
            _logger.LogInformation("TestApplyMigrationsFromVersion1_0 completed successfully");
        }

        /// <summary>
        /// Tests that no migrations are applied when the database is already at the latest version.
        /// </summary>
        [Fact]
        [public]
        [async]
        public async Task TestNoMigrationsAppliedWhenAlreadyAtLatestVersion()
        {
            // Reset the test database to ensure a clean state
            await Database.ResetDatabaseAsync();

            // Get the database connection from the test fixture
            var connection = Database.Connection;

            // Apply all migrations to get to the latest version
            double initialVersion = await _migrationManager.ApplyMigrationsAsync(connection, 0.0);

            // Apply migrations again starting from the latest version
            double newVersion = await _migrationManager.ApplyMigrationsAsync(connection, initialVersion);

            // Verify that the returned version is still 1.1 (no change)
            newVersion.Should().Be(1.1);

            // Log successful test completion
            _logger.LogInformation("TestNoMigrationsAppliedWhenAlreadyAtLatestVersion completed successfully");
        }

        /// <summary>
        /// Verifies that the changes from Migration 1.0 have been applied correctly.
        /// </summary>
        [private]
        [async]
        public async Task VerifyMigration1_0Changes()
        {
            // Get the database connection from the test fixture
            var connection = Database.Connection;

            // Query the database to check if all required tables exist
            var tableNames = new[] { "User", "TimeRecord", "LocationRecord", "Photo", "ActivityReport", "PatrolLocation", "Checkpoint", "CheckpointVerification", "SyncQueue" };
            foreach (var tableName in tableNames)
            {
                var tableInfo = await connection.GetTableInfoAsync(tableName);
                tableInfo.Should().NotBeNull($"Table '{tableName}' should exist");
            }

            // Verify that User table exists and has the expected columns
            var userTableInfo = await connection.GetTableInfoAsync("User");
            userTableInfo.Count.Should().BeGreaterThan(0);

            // Verify that TimeRecord table exists and has the expected columns
            var timeRecordTableInfo = await connection.GetTableInfoAsync("TimeRecord");
            timeRecordTableInfo.Count.Should().BeGreaterThan(0);

            // Verify that LocationRecord table exists and has the expected columns
            var locationRecordTableInfo = await connection.GetTableInfoAsync("LocationRecord");
            locationRecordTableInfo.Count.Should().BeGreaterThan(0);

            // Verify that Photo table exists and has the expected columns
            var photoTableInfo = await connection.GetTableInfoAsync("Photo");
            photoTableInfo.Count.Should().BeGreaterThan(0);

            // Verify that ActivityReport table exists and has the expected columns
            var activityReportTableInfo = await connection.GetTableInfoAsync("ActivityReport");
            activityReportTableInfo.Count.Should().BeGreaterThan(0);

            // Verify that PatrolLocation table exists and has the expected columns
            var patrolLocationTableInfo = await connection.GetTableInfoAsync("PatrolLocation");
            patrolLocationTableInfo.Count.Should().BeGreaterThan(0);

            // Verify that Checkpoint table exists and has the expected columns
            var checkpointTableInfo = await connection.GetTableInfoAsync("Checkpoint");
            checkpointTableInfo.Count.Should().BeGreaterThan(0);

            // Verify that CheckpointVerification table exists and has the expected columns
            var checkpointVerificationTableInfo = await connection.GetTableInfoAsync("CheckpointVerification");
            checkpointVerificationTableInfo.Count.Should().BeGreaterThan(0);

            // Verify that SyncQueue table exists and has the expected columns
            var syncQueueTableInfo = await connection.GetTableInfoAsync("SyncQueue");
            syncQueueTableInfo.Count.Should().BeGreaterThan(0);

            // Log verification results
            _logger.LogInformation("Migration 1.0 changes verified successfully");
        }

        /// <summary>
        /// Verifies that the changes from Migration 1.1 have been applied correctly.
        /// </summary>
        [private]
        [async]
        public async Task VerifyMigration1_1Changes()
        {
            // Get the database connection from the test fixture
            var connection = Database.Connection;

            // Query the database to check if the SyncProgress column exists in the Photo table
            var photoTableInfo = await connection.GetTableInfoAsync("Photo");
            var syncProgressColumn = photoTableInfo.FirstOrDefault(c => c.Name == "SyncProgress");
            syncProgressColumn.Should().NotBeNull("SyncProgress column should exist in Photo table");

            // Verify that the SyncProgress column has the expected default value (0)
            var photoEntity = new PhotoEntity();
            await connection.InsertAsync(photoEntity);
            var retrievedPhoto = await connection.GetAsync<PhotoEntity>(photoEntity.Id);
            retrievedPhoto.SyncProgress.Should().Be(0, "SyncProgress should have a default value of 0");

            // Query the database to check if RemoteId columns exist in all entity tables
            var entityTableNames = new[] { "User", "TimeRecord", "LocationRecord", "Photo", "ActivityReport", "PatrolLocation", "Checkpoint", "CheckpointVerification" };
            foreach (var tableName in entityTableNames)
            {
                var tableInfo = await connection.GetTableInfoAsync(tableName);
                var remoteIdColumn = tableInfo.FirstOrDefault(c => c.Name == "RemoteId");
                remoteIdColumn.Should().NotBeNull($"RemoteId column should exist in {tableName} table");
            }

            // Verify that RemoteId columns have the expected default value (null)
            var timeRecordEntity = new TimeRecordEntity();
            await connection.InsertAsync(timeRecordEntity);
            var retrievedTimeRecord = await connection.GetAsync<TimeRecordEntity>(timeRecordEntity.Id);
            retrievedTimeRecord.RemoteId.Should().BeNull("RemoteId should have a default value of null");

            // Insert test data and verify that the new columns can store and retrieve values correctly
            var photo = new PhotoEntity
            {
                Id = Guid.NewGuid().ToString(),
                UserId = "testuser",
                Timestamp = DateTime.UtcNow,
                Latitude = 0,
                Longitude = 0,
                FilePath = "testpath",
                IsSynced = false,
                SyncProgress = 50,
                RemoteId = "remote123"
            };
            await connection.InsertAsync(photo);
            var retrievedPhoto2 = await connection.GetAsync<PhotoEntity>(photo.Id);
            retrievedPhoto2.SyncProgress.Should().Be(50);
            retrievedPhoto2.RemoteId.Should().Be("remote123");

            // Log verification results
            _logger.LogInformation("Migration 1.1 changes verified successfully");
        }
    }
}