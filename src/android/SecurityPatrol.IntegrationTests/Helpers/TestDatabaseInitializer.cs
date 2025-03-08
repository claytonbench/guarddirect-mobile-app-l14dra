using Microsoft.Extensions.Logging;
using SecurityPatrol.Constants;
using SecurityPatrol.Database;
using SecurityPatrol.Database.Entities;
using SecurityPatrol.Database.Migrations;
using SQLite;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SecurityPatrol.IntegrationTests.Helpers
{
    /// <summary>
    /// Implements a specialized database initializer for integration tests in the Security Patrol application.
    /// This class provides methods for initializing a test database, resetting it between tests, and seeding it with test data.
    /// </summary>
    public class TestDatabaseInitializer : IDatabaseInitializer
    {
        private readonly ILogger<TestDatabaseInitializer> _logger;
        private SQLiteAsyncConnection _connection;
        private readonly MigrationManager _migrationManager;
        private readonly string _databasePath;
        private bool _isInitialized;
        private readonly object _initializationLock = new object();

        /// <summary>
        /// Helper class for querying table information from SQLite.
        /// </summary>
        private class TableInfoResult
        {
            public string Name { get; set; }
        }

        /// <summary>
        /// Initializes a new instance of the TestDatabaseInitializer class with the specified logger.
        /// </summary>
        /// <param name="logger">Logger for database operations</param>
        public TestDatabaseInitializer(ILogger<TestDatabaseInitializer> logger)
        {
            _logger = logger;
            _migrationManager = new MigrationManager(logger);
            _isInitialized = false;
            _databasePath = Path.Combine(Path.GetTempPath(), "SecurityPatrolTest.db");
        }

        /// <summary>
        /// Initializes the test database by creating tables and indexes if they don't exist and applying any pending migrations.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task InitializeAsync()
        {
            if (_isInitialized)
            {
                return;
            }

            lock (_initializationLock)
            {
                if (_isInitialized)
                {
                    return;
                }

                _logger?.LogInformation("Initializing test database at {Path}", _databasePath);
                
                try
                {
                    // Ensure database directory exists
                    EnsureDatabaseDirectoryExistsAsync().Wait();
                    
                    // Create database connection if not already created
                    if (_connection == null)
                    {
                        _connection = CreateDatabaseConnectionAsync().Result;
                    }
                    
                    // Create tables if they don't exist
                    CreateTablesAsync().Wait();
                    
                    // Apply any pending migrations
                    ApplyMigrationsAsync(_connection).Wait();
                    
                    _isInitialized = true;
                    _logger?.LogInformation("Test database initialization completed");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error initializing test database: {Message}", ex.Message);
                    throw;
                }
            }
        }

        /// <summary>
        /// Gets an initialized SQLite connection to the test database.
        /// </summary>
        /// <returns>A task that returns an initialized SQLite connection</returns>
        public async Task<SQLiteAsyncConnection> GetConnectionAsync()
        {
            await InitializeAsync();
            return _connection;
        }

        /// <summary>
        /// Resets the test database by dropping all tables and recreating them. Used to ensure a clean state between tests.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task ResetDatabaseAsync()
        {
            _logger?.LogInformation("Resetting test database");
            
            await InitializeAsync();
            
            try
            {
                // Drop all existing tables
                await _connection.ExecuteAsync("PRAGMA foreign_keys = OFF;");
                
                // Get all tables
                var tables = await _connection.QueryAsync<TableInfoResult>("SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';");
                
                foreach (var table in tables)
                {
                    await _connection.ExecuteAsync($"DROP TABLE IF EXISTS {table.Name};");
                }
                
                await _connection.ExecuteAsync("PRAGMA foreign_keys = ON;");
                
                // Create tables
                await CreateTablesAsync();
                
                // Reset database version
                await _connection.ExecuteAsync("CREATE TABLE IF NOT EXISTS Version (Id INTEGER PRIMARY KEY, VersionNumber REAL);");
                await _connection.ExecuteAsync("DELETE FROM Version;");
                await _connection.ExecuteAsync("INSERT INTO Version (Id, VersionNumber) VALUES (1, 0.0);");
                
                _logger?.LogInformation("Test database reset completed");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error resetting test database: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Applies any pending database migrations to update the schema to the latest version.
        /// </summary>
        /// <param name="connection">The SQLite database connection to use for applying migrations</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task ApplyMigrationsAsync(SQLiteAsyncConnection connection)
        {
            _logger?.LogInformation("Applying migrations to test database");
            
            try
            {
                double currentVersion = await GetDatabaseVersionAsync();
                
                // Apply migrations using the MigrationManager
                double newVersion = await _migrationManager.ApplyMigrationsAsync(connection, currentVersion);
                
                // Update database version
                await connection.ExecuteAsync("UPDATE Version SET VersionNumber = ? WHERE Id = 1", newVersion);
                
                _logger?.LogInformation("Database migrations applied. Version updated from {OldVersion} to {NewVersion}", 
                    currentVersion, newVersion);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error applying migrations: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Gets the current version of the test database schema.
        /// </summary>
        /// <returns>A task that returns the current database version</returns>
        public async Task<double> GetDatabaseVersionAsync()
        {
            await InitializeAsync();
            
            try
            {
                // Try to get the current version from the Version table
                var result = await _connection.ExecuteScalarAsync<string>("SELECT VersionNumber FROM Version WHERE Id = 1;");
                
                if (string.IsNullOrEmpty(result))
                {
                    // If no version found, create the table and set initial version to 0.0
                    await _connection.ExecuteAsync("CREATE TABLE IF NOT EXISTS Version (Id INTEGER PRIMARY KEY, VersionNumber REAL);");
                    await _connection.ExecuteAsync("INSERT INTO Version (Id, VersionNumber) VALUES (1, 0.0);");
                    return 0.0;
                }
                
                return double.Parse(result);
            }
            catch (SQLiteException)
            {
                // If the Version table doesn't exist, create it and return 0.0
                await _connection.ExecuteAsync("CREATE TABLE IF NOT EXISTS Version (Id INTEGER PRIMARY KEY, VersionNumber REAL);");
                await _connection.ExecuteAsync("INSERT INTO Version (Id, VersionNumber) VALUES (1, 0.0);");
                return 0.0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting database version: {Message}", ex.Message);
                return 0.0;
            }
        }

        /// <summary>
        /// Creates all required tables in the test database if they don't already exist.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        private async Task CreateTablesAsync()
        {
            _logger?.LogInformation("Creating tables in test database");
            
            try
            {
                // Create tables for all entity types
                await _connection.CreateTableAsync<UserEntity>();
                await _connection.CreateTableAsync<TimeRecordEntity>();
                await _connection.CreateTableAsync<LocationRecordEntity>();
                await _connection.CreateTableAsync<PhotoEntity>();
                await _connection.CreateTableAsync<ReportEntity>();
                await _connection.CreateTableAsync<PatrolLocationEntity>();
                await _connection.CreateTableAsync<CheckpointEntity>();
                await _connection.CreateTableAsync<CheckpointVerificationEntity>();
                await _connection.CreateTableAsync<SyncQueueEntity>();
                
                // Create version table
                await _connection.ExecuteAsync("CREATE TABLE IF NOT EXISTS Version (Id INTEGER PRIMARY KEY, VersionNumber REAL);");
                
                // Initialize version if not already set
                var versionExists = await _connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Version;");
                if (versionExists == 0)
                {
                    await _connection.ExecuteAsync("INSERT INTO Version (Id, VersionNumber) VALUES (1, 0.0);");
                }
                
                _logger?.LogInformation("Table creation completed");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating tables: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Creates a new SQLite connection to the test database.
        /// </summary>
        /// <returns>A task that returns a new SQLite connection</returns>
        private async Task<SQLiteAsyncConnection> CreateDatabaseConnectionAsync()
        {
            _logger?.LogInformation("Creating SQLite connection to test database at {Path}", _databasePath);
            
            var flags = SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.SharedCache;
            var connection = new SQLiteAsyncConnection(_databasePath, flags);
            
            // Enable foreign keys
            await connection.ExecuteAsync("PRAGMA foreign_keys = ON;");
            
            return connection;
        }

        /// <summary>
        /// Ensures that the directory for the test database file exists, creating it if necessary.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        private async Task EnsureDatabaseDirectoryExistsAsync()
        {
            var directory = Path.GetDirectoryName(_databasePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        /// <summary>
        /// Seeds the test database with predefined test data for integration tests.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task SeedTestDataAsync()
        {
            await InitializeAsync();
            
            _logger?.LogInformation("Seeding test database with test data");
            
            try
            {
                // Create test users
                var user1 = await CreateTestUserAsync("user1", "+15551234567");
                var user2 = await CreateTestUserAsync("user2", "+15557654321");
                
                // Create test time records
                await CreateTestTimeRecordAsync(user1.UserId, "ClockIn", DateTime.Now.AddDays(-1), true);
                await CreateTestTimeRecordAsync(user1.UserId, "ClockOut", DateTime.Now.AddDays(-1).AddHours(8), true);
                await CreateTestTimeRecordAsync(user1.UserId, "ClockIn", DateTime.Now, false);
                
                // Create test location records
                for (int i = 0; i < 10; i++)
                {
                    await CreateTestLocationRecordAsync(
                        user1.UserId, 
                        DateTime.Now.AddMinutes(-i * 5), 
                        37.7749 + (i * 0.001), 
                        -122.4194 + (i * 0.001), 
                        10.0, 
                        i < 5);
                }
                
                // Create test patrol locations
                var location1 = new PatrolLocationEntity
                {
                    Name = "Test Location 1",
                    Latitude = 37.7749,
                    Longitude = -122.4194,
                    LastUpdated = DateTime.Now,
                    RemoteId = "loc1"
                };
                
                var location2 = new PatrolLocationEntity
                {
                    Name = "Test Location 2",
                    Latitude = 37.7833,
                    Longitude = -122.4167,
                    LastUpdated = DateTime.Now,
                    RemoteId = "loc2"
                };
                
                await _connection.InsertAsync(location1);
                await _connection.InsertAsync(location2);
                
                // Create test checkpoints
                for (int i = 0; i < 5; i++)
                {
                    var checkpoint = new CheckpointEntity
                    {
                        LocationId = location1.Id,
                        Name = $"Checkpoint {i + 1}",
                        Latitude = location1.Latitude + (i * 0.001),
                        Longitude = location1.Longitude + (i * 0.001),
                        LastUpdated = DateTime.Now,
                        RemoteId = $"cp{i + 1}"
                    };
                    
                    await _connection.InsertAsync(checkpoint);
                    
                    // Create test checkpoint verifications for some checkpoints
                    if (i < 3)
                    {
                        var verification = new CheckpointVerificationEntity
                        {
                            UserId = user1.UserId,
                            CheckpointId = checkpoint.Id,
                            Timestamp = DateTime.Now.AddHours(-i),
                            Latitude = checkpoint.Latitude,
                            Longitude = checkpoint.Longitude,
                            IsSynced = i < 2,
                            RemoteId = i < 2 ? $"ver{i + 1}" : null
                        };
                        
                        await _connection.InsertAsync(verification);
                    }
                }
                
                // Create test photos
                for (int i = 0; i < 3; i++)
                {
                    var photo = new PhotoEntity
                    {
                        Id = Guid.NewGuid().ToString(),
                        UserId = user1.UserId,
                        Timestamp = DateTime.Now.AddHours(-i),
                        Latitude = 37.7749,
                        Longitude = -122.4194,
                        FilePath = $"/test/path/photo{i + 1}.jpg",
                        IsSynced = i < 2,
                        RemoteId = i < 2 ? $"photo{i + 1}" : null,
                        SyncProgress = i < 2 ? 100 : 0
                    };
                    
                    await _connection.InsertAsync(photo);
                }
                
                // Create test activity reports
                for (int i = 0; i < 3; i++)
                {
                    var report = new ReportEntity
                    {
                        UserId = user1.UserId,
                        Text = $"Test activity report {i + 1}",
                        Timestamp = DateTime.Now.AddHours(-i),
                        Latitude = 37.7749,
                        Longitude = -122.4194,
                        IsSynced = i < 2,
                        RemoteId = i < 2 ? $"report{i + 1}" : null
                    };
                    
                    await _connection.InsertAsync(report);
                }
                
                _logger?.LogInformation("Test data seeding completed");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error seeding test data: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Creates and inserts a test user into the database.
        /// </summary>
        /// <param name="userId">The user ID to assign</param>
        /// <param name="phoneNumber">The phone number to assign</param>
        /// <returns>A task that returns the created user entity</returns>
        private async Task<UserEntity> CreateTestUserAsync(string userId, string phoneNumber)
        {
            var user = new UserEntity
            {
                UserId = userId,
                PhoneNumber = phoneNumber,
                LastAuthenticated = DateTime.Now,
                AuthToken = "test_auth_token_" + userId,
                TokenExpiry = DateTime.Now.AddDays(1)
            };
            
            await _connection.InsertAsync(user);
            return user;
        }

        /// <summary>
        /// Creates and inserts a test time record into the database.
        /// </summary>
        /// <param name="userId">The user ID to associate with the record</param>
        /// <param name="type">The type of time record (ClockIn/ClockOut)</param>
        /// <param name="timestamp">The timestamp for the record</param>
        /// <param name="isSynced">Whether the record should be marked as synced</param>
        /// <returns>A task that returns the created time record entity</returns>
        private async Task<TimeRecordEntity> CreateTestTimeRecordAsync(string userId, string type, DateTime? timestamp = null, bool isSynced = false)
        {
            var timeRecord = new TimeRecordEntity
            {
                UserId = userId,
                Type = type,
                Timestamp = timestamp ?? DateTime.Now,
                Latitude = 37.7749,
                Longitude = -122.4194,
                IsSynced = isSynced,
                RemoteId = isSynced ? $"tr_{Guid.NewGuid()}" : null
            };
            
            await _connection.InsertAsync(timeRecord);
            return timeRecord;
        }

        /// <summary>
        /// Creates and inserts a test location record into the database.
        /// </summary>
        /// <param name="userId">The user ID to associate with the record</param>
        /// <param name="timestamp">The timestamp for the record</param>
        /// <param name="latitude">The latitude coordinate</param>
        /// <param name="longitude">The longitude coordinate</param>
        /// <param name="accuracy">The accuracy of the location in meters</param>
        /// <param name="isSynced">Whether the record should be marked as synced</param>
        /// <returns>A task that returns the created location record entity</returns>
        private async Task<LocationRecordEntity> CreateTestLocationRecordAsync(
            string userId, 
            DateTime? timestamp = null, 
            double latitude = 37.7749, 
            double longitude = -122.4194, 
            double accuracy = 10.0, 
            bool isSynced = false)
        {
            var locationRecord = new LocationRecordEntity
            {
                UserId = userId,
                Timestamp = timestamp ?? DateTime.Now,
                Latitude = latitude,
                Longitude = longitude,
                Accuracy = accuracy,
                IsSynced = isSynced,
                RemoteId = isSynced ? $"lr_{Guid.NewGuid()}" : null
            };
            
            await _connection.InsertAsync(locationRecord);
            return locationRecord;
        }
    }
}