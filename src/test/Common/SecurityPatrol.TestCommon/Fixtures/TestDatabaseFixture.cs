using System;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Extensions.Logging;
using SQLite; // Version 1.8.116
using Xunit; // Version 2.4.2
using SecurityPatrol.Database;
using SecurityPatrol.Database.Entities;
using SecurityPatrol.TestCommon.Constants;
using SecurityPatrol.TestCommon.Data;

namespace SecurityPatrol.TestCommon.Fixtures
{
    /// <summary>
    /// Implementation of IDatabaseInitializer for testing purposes that uses an in-memory SQLite database.
    /// </summary>
    public class TestDatabaseInitializer : IDatabaseInitializer
    {
        private readonly ILogger<TestDatabaseInitializer> _logger;
        private SQLiteAsyncConnection _connection;
        private readonly string _databasePath;
        private bool _isInitialized;
        private readonly object _initializationLock = new object();

        /// <summary>
        /// Initializes a new instance of the TestDatabaseInitializer class with the specified logger.
        /// </summary>
        /// <param name="logger">The logger to use for diagnostics.</param>
        public TestDatabaseInitializer(ILogger<TestDatabaseInitializer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _isInitialized = false;
            _initializationLock = new object();
            _databasePath = ":memory:"; // Use in-memory database for tests
        }

        /// <summary>
        /// Initializes the in-memory database by creating tables and indexes.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task InitializeAsync()
        {
            if (_isInitialized)
                return;

            lock (_initializationLock)
            {
                if (_isInitialized)
                    return;

                _logger.LogInformation("Initializing test database");

                if (_connection == null)
                {
                    CreateDatabaseConnectionAsync().GetAwaiter().GetResult();
                }
            }

            await CreateTablesAsync();

            _isInitialized = true;
            _logger.LogInformation("Test database initialization complete");
        }

        /// <summary>
        /// Gets an initialized SQLite connection to the in-memory database.
        /// </summary>
        /// <returns>A task that returns an initialized SQLite connection</returns>
        public async Task<SQLiteAsyncConnection> GetConnectionAsync()
        {
            await InitializeAsync();
            return _connection;
        }

        /// <summary>
        /// Resets the in-memory database by dropping all tables and recreating them.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task ResetDatabaseAsync()
        {
            _logger.LogInformation("Resetting test database");

            try
            {
                // Drop all existing tables in reverse order of creation to respect foreign keys
                await _connection.ExecuteAsync("PRAGMA foreign_keys = OFF");

                // Drop tables using raw SQL for tables without entity classes
                await _connection.ExecuteAsync($"DROP TABLE IF EXISTS {Constants.DatabaseConstants.TableSyncQueue}");
                await _connection.ExecuteAsync($"DROP TABLE IF EXISTS {Constants.DatabaseConstants.TableCheckpointVerification}");

                // Drop tables using entity classes for the rest
                await _connection.DropTableAsync<CheckpointEntity>();
                await _connection.DropTableAsync<PatrolLocationEntity>();
                await _connection.DropTableAsync<ReportEntity>();
                await _connection.DropTableAsync<PhotoEntity>();
                await _connection.DropTableAsync<LocationRecordEntity>();
                await _connection.DropTableAsync<TimeRecordEntity>();
                await _connection.DropTableAsync<UserEntity>();

                await _connection.ExecuteAsync("PRAGMA foreign_keys = ON");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dropping tables during database reset");
            }

            // Recreate tables
            await CreateTablesAsync();

            _logger.LogInformation("Test database reset complete");
        }

        /// <summary>
        /// Applies any pending database migrations (not needed for in-memory test database).
        /// </summary>
        /// <param name="connection">The SQLite connection to use for applying migrations</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task ApplyMigrationsAsync(SQLiteAsyncConnection connection)
        {
            _logger.LogInformation("Migrations not needed for test database");
            await Task.CompletedTask;
        }

        /// <summary>
        /// Gets the current version of the database schema.
        /// </summary>
        /// <returns>A task that returns the current database version</returns>
        public async Task<double> GetDatabaseVersionAsync()
        {
            return await Task.FromResult((double)TestConstants.TestDatabaseVersion);
        }

        /// <summary>
        /// Creates all required tables in the in-memory database.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        private async Task CreateTablesAsync()
        {
            _logger.LogInformation("Creating tables in test database");

            try
            {
                // Enable foreign keys
                await _connection.ExecuteAsync("PRAGMA foreign_keys = ON");

                // Create all tables in order of dependencies
                await _connection.CreateTableAsync<UserEntity>();
                await _connection.CreateTableAsync<TimeRecordEntity>();
                await _connection.CreateTableAsync<LocationRecordEntity>();
                await _connection.CreateTableAsync<PhotoEntity>();
                await _connection.CreateTableAsync<ReportEntity>();
                await _connection.CreateTableAsync<PatrolLocationEntity>();
                await _connection.CreateTableAsync<CheckpointEntity>();

                // Create tables with foreign key dependencies using raw SQL since we don't have the entity classes
                // CheckpointVerification table
                await _connection.ExecuteAsync($@"
                    CREATE TABLE IF NOT EXISTS {Constants.DatabaseConstants.TableCheckpointVerification} (
                        {Constants.DatabaseConstants.ColumnId} INTEGER PRIMARY KEY AUTOINCREMENT,
                        {Constants.DatabaseConstants.ColumnUserId} TEXT NOT NULL,
                        {Constants.DatabaseConstants.ColumnCheckpointId} INTEGER NOT NULL,
                        {Constants.DatabaseConstants.ColumnTimestamp} TEXT NOT NULL,
                        {Constants.DatabaseConstants.ColumnLatitude} REAL NOT NULL,
                        {Constants.DatabaseConstants.ColumnLongitude} REAL NOT NULL,
                        {Constants.DatabaseConstants.ColumnIsSynced} INTEGER DEFAULT 0,
                        {Constants.DatabaseConstants.ColumnRemoteId} TEXT,
                        FOREIGN KEY({Constants.DatabaseConstants.ColumnCheckpointId}) 
                        REFERENCES {Constants.DatabaseConstants.TableCheckpoint}({Constants.DatabaseConstants.ColumnId})
                    )");

                // SyncQueue table
                await _connection.ExecuteAsync($@"
                    CREATE TABLE IF NOT EXISTS {Constants.DatabaseConstants.TableSyncQueue} (
                        {Constants.DatabaseConstants.ColumnId} INTEGER PRIMARY KEY AUTOINCREMENT,
                        {Constants.DatabaseConstants.ColumnEntityType} TEXT NOT NULL,
                        {Constants.DatabaseConstants.ColumnEntityId} TEXT NOT NULL,
                        {Constants.DatabaseConstants.ColumnPriority} INTEGER DEFAULT 0,
                        {Constants.DatabaseConstants.ColumnRetryCount} INTEGER DEFAULT 0,
                        {Constants.DatabaseConstants.ColumnLastAttempt} TEXT,
                        {Constants.DatabaseConstants.ColumnErrorMessage} TEXT
                    )");

                // Create indexes for performance
                await _connection.ExecuteAsync($"CREATE INDEX IF NOT EXISTS IX_TimeRecord_UserId ON {Constants.DatabaseConstants.TableTimeRecord}({Constants.DatabaseConstants.ColumnUserId})");
                await _connection.ExecuteAsync($"CREATE INDEX IF NOT EXISTS IX_TimeRecord_IsSynced ON {Constants.DatabaseConstants.TableTimeRecord}({Constants.DatabaseConstants.ColumnIsSynced})");
                await _connection.ExecuteAsync($"CREATE INDEX IF NOT EXISTS IX_LocationRecord_UserId_Timestamp ON {Constants.DatabaseConstants.TableLocationRecord}({Constants.DatabaseConstants.ColumnUserId}, {Constants.DatabaseConstants.ColumnTimestamp})");
                await _connection.ExecuteAsync($"CREATE INDEX IF NOT EXISTS IX_LocationRecord_IsSynced ON {Constants.DatabaseConstants.TableLocationRecord}({Constants.DatabaseConstants.ColumnIsSynced})");
                await _connection.ExecuteAsync($"CREATE INDEX IF NOT EXISTS IX_Photo_UserId ON {Constants.DatabaseConstants.TablePhoto}({Constants.DatabaseConstants.ColumnUserId})");
                await _connection.ExecuteAsync($"CREATE INDEX IF NOT EXISTS IX_Photo_IsSynced ON {Constants.DatabaseConstants.TablePhoto}({Constants.DatabaseConstants.ColumnIsSynced})");
                await _connection.ExecuteAsync($"CREATE INDEX IF NOT EXISTS IX_ActivityReport_UserId ON {Constants.DatabaseConstants.TableActivityReport}({Constants.DatabaseConstants.ColumnUserId})");
                await _connection.ExecuteAsync($"CREATE INDEX IF NOT EXISTS IX_ActivityReport_IsSynced ON {Constants.DatabaseConstants.TableActivityReport}({Constants.DatabaseConstants.ColumnIsSynced})");
                await _connection.ExecuteAsync($"CREATE INDEX IF NOT EXISTS IX_Checkpoint_LocationId ON {Constants.DatabaseConstants.TableCheckpoint}({Constants.DatabaseConstants.ColumnLocationId})");
                await _connection.ExecuteAsync($"CREATE INDEX IF NOT EXISTS IX_CheckpointVerification_CheckpointId ON {Constants.DatabaseConstants.TableCheckpointVerification}({Constants.DatabaseConstants.ColumnCheckpointId})");
                await _connection.ExecuteAsync($"CREATE INDEX IF NOT EXISTS IX_CheckpointVerification_IsSynced ON {Constants.DatabaseConstants.TableCheckpointVerification}({Constants.DatabaseConstants.ColumnIsSynced})");
                await _connection.ExecuteAsync($"CREATE INDEX IF NOT EXISTS IX_SyncQueue_EntityType_EntityId ON {Constants.DatabaseConstants.TableSyncQueue}({Constants.DatabaseConstants.ColumnEntityType}, {Constants.DatabaseConstants.ColumnEntityId})");
                await _connection.ExecuteAsync($"CREATE INDEX IF NOT EXISTS IX_SyncQueue_Priority_LastAttempt ON {Constants.DatabaseConstants.TableSyncQueue}({Constants.DatabaseConstants.ColumnPriority}, {Constants.DatabaseConstants.ColumnLastAttempt})");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating tables in test database");
                throw;
            }

            _logger.LogInformation("Table creation complete");
        }

        /// <summary>
        /// Creates a new SQLite connection to the in-memory database.
        /// </summary>
        /// <returns>A task that returns a new SQLite connection</returns>
        private async Task<SQLiteAsyncConnection> CreateDatabaseConnectionAsync()
        {
            _logger.LogInformation("Creating new SQLite connection for test database");

            try
            {
                var connection = new SQLiteAsyncConnection(_databasePath);
                await connection.EnableWriteAheadLoggingAsync();
                _connection = connection;
                return connection;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating SQLite connection");
                throw;
            }
        }
    }

    /// <summary>
    /// A test fixture that provides an isolated SQLite database for integration testing.
    /// Implements IDisposable to clean up resources after tests.
    /// </summary>
    public class TestDatabaseFixture : IDisposable
    {
        /// <summary>
        /// Gets the SQLite connection to the test database.
        /// </summary>
        public SQLiteAsyncConnection Connection { get; private set; }

        private readonly TestDatabaseInitializer _databaseInitializer;
        private readonly ILogger<TestDatabaseFixture> _logger;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the TestDatabaseFixture class and sets up the test database initializer.
        /// </summary>
        public TestDatabaseFixture()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<TestDatabaseFixture>();
            _databaseInitializer = new TestDatabaseInitializer(loggerFactory.CreateLogger<TestDatabaseInitializer>());
            _disposed = false;

            _logger.LogInformation("TestDatabaseFixture initialized");
        }

        /// <summary>
        /// Initializes the test database asynchronously.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task InitializeAsync()
        {
            await _databaseInitializer.InitializeAsync();
            Connection = await _databaseInitializer.GetConnectionAsync();
            _logger.LogInformation("Test database initialized");
        }

        /// <summary>
        /// Resets the test database to its initial state.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task ResetDatabaseAsync()
        {
            await _databaseInitializer.ResetDatabaseAsync();
            _logger.LogInformation("Test database reset complete");
        }

        /// <summary>
        /// Seeds the test database with a standard set of test data.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task SeedTestDataAsync()
        {
            await InitializeAsync();

            // Seed in logical order to maintain relationships
            await SeedUserDataAsync();
            await SeedPatrolDataAsync();
            await SeedTimeRecordDataAsync();
            await SeedPhotoDataAsync();
            await SeedReportDataAsync();

            _logger.LogInformation("Test data seeding complete");
        }

        /// <summary>
        /// Seeds the test database with user data.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task SeedUserDataAsync()
        {
            var user = MockDataGenerator.CreateUserEntity(1);
            await Connection.InsertAsync(user);

            _logger.LogInformation("User data seeding complete");
        }

        /// <summary>
        /// Seeds the test database with patrol locations and checkpoints.
        /// </summary>
        /// <param name="locationCount">The number of patrol locations to create.</param>
        /// <param name="checkpointsPerLocation">The number of checkpoints per location.</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task SeedPatrolDataAsync(int locationCount = 3, int checkpointsPerLocation = 5)
        {
            for (int i = 1; i <= locationCount; i++)
            {
                var patrolData = MockDataGenerator.CreatePatrolWithCheckpoints(i, $"Test Location {i}", checkpointsPerLocation);

                // Insert the location
                await Connection.InsertAsync(patrolData.Item1);

                // Insert the checkpoints
                foreach (var checkpoint in patrolData.Item2)
                {
                    await Connection.InsertAsync(checkpoint);
                }
            }

            _logger.LogInformation("Patrol data seeding complete: {LocationCount} locations with {CheckpointsPerLocation} checkpoints each",
                locationCount, checkpointsPerLocation);
        }

        /// <summary>
        /// Seeds the test database with time record data.
        /// </summary>
        /// <param name="recordPairCount">The number of clock in/out pairs to create.</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task SeedTimeRecordDataAsync(int recordPairCount = 5)
        {
            // Create clock in/out pairs
            for (int i = 0; i < recordPairCount; i++)
            {
                // Create clock in (odd IDs)
                var clockIn = MockDataGenerator.CreateTimeRecordEntity(i * 2 + 1, TestConstants.TestUserId, "ClockIn");
                await Connection.InsertAsync(clockIn);

                // Create clock out (even IDs)
                var clockOut = MockDataGenerator.CreateTimeRecordEntity(i * 2 + 2, TestConstants.TestUserId, "ClockOut");
                // Set the timestamp a few hours after clock in
                clockOut.Timestamp = clockIn.Timestamp.AddHours(8);
                await Connection.InsertAsync(clockOut);
            }

            _logger.LogInformation("Time record data seeding complete: {RecordPairCount} clock in/out pairs", recordPairCount);
        }

        /// <summary>
        /// Seeds the test database with photo data.
        /// </summary>
        /// <param name="photoCount">The number of photos to create.</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task SeedPhotoDataAsync(int photoCount = 5)
        {
            for (int i = 0; i < photoCount; i++)
            {
                var photo = MockDataGenerator.CreatePhotoEntity();
                await Connection.InsertAsync(photo);
            }

            _logger.LogInformation("Photo data seeding complete: {PhotoCount} photos", photoCount);
        }

        /// <summary>
        /// Seeds the test database with report data.
        /// </summary>
        /// <param name="reportCount">The number of reports to create.</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task SeedReportDataAsync(int reportCount = 5)
        {
            for (int i = 1; i <= reportCount; i++)
            {
                var report = MockDataGenerator.CreateReportEntity(i, TestConstants.TestUserId, $"Test Report {i}");
                await Connection.InsertAsync(report);
            }

            _logger.LogInformation("Report data seeding complete: {ReportCount} reports", reportCount);
        }

        /// <summary>
        /// Disposes the database fixture and cleans up resources.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                // Close and dispose the SQLite connection
                if (Connection != null)
                {
                    Connection.CloseAsync().Wait();
                    Connection = null;
                }

                _logger.LogInformation("TestDatabaseFixture disposed");
            }
        }
    }
}