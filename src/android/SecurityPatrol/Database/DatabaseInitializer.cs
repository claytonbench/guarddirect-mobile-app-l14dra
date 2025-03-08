using Microsoft.Extensions.Logging;
using SecurityPatrol.Constants;
using SecurityPatrol.Database.Entities;
using SecurityPatrol.Database.Migrations;
using SQLite; // SQLite-net-pcl v1.8+
using System;
using System.IO;
using System.Threading.Tasks;

namespace SecurityPatrol.Database
{
    /// <summary>
    /// Implements the IDatabaseInitializer interface to provide database initialization, migration, 
    /// and connection management for the Security Patrol application.
    /// </summary>
    public class DatabaseInitializer : IDatabaseInitializer
    {
        private readonly ILogger<DatabaseInitializer> _logger;
        private SQLiteAsyncConnection _connection;
        private readonly MigrationManager _migrationManager;
        private readonly string _databasePath;
        private bool _isInitialized;
        private readonly object _initializationLock = new object();

        /// <summary>
        /// Initializes a new instance of the DatabaseInitializer class with the specified logger.
        /// </summary>
        /// <param name="logger">The logger for database operations</param>
        public DatabaseInitializer(ILogger<DatabaseInitializer> logger)
        {
            _logger = logger;
            _migrationManager = new MigrationManager(logger);
            _isInitialized = false;
            
            // Determine the database path
            // Note: For a full cross-platform implementation, this should use FileSystem.AppDataDirectory
            // This implementation works for Android
            string appDataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _databasePath = Path.Combine(appDataDirectory, DatabaseConstants.DatabaseName);
            _logger?.LogInformation($"Database path set to: {_databasePath}");
        }

        /// <summary>
        /// Initializes the database by creating tables and indexes if they don't exist
        /// and applying any pending migrations.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task InitializeAsync()
        {
            if (_isInitialized)
            {
                return;
            }

            // Use a lock to prevent multiple initializations
            bool initializeTaskNeeded = false;
            lock (_initializationLock)
            {
                if (!_isInitialized)
                {
                    initializeTaskNeeded = true;
                    _logger?.LogInformation($"Initializing database at {_databasePath}");
                }
            }

            if (!initializeTaskNeeded)
            {
                return;
            }
            
            try
            {
                // Ensure directory exists
                await EnsureDatabaseDirectoryExistsAsync();
                
                // Create or get the database connection
                if (_connection == null)
                {
                    _connection = await CreateDatabaseConnectionAsync();
                }

                // Create tables if they don't exist
                await CreateTablesAsync();

                // Apply any pending migrations
                await ApplyMigrationsAsync(_connection);

                // Mark as initialized
                lock (_initializationLock)
                {
                    _isInitialized = true;
                }
                
                _logger?.LogInformation("Database initialization completed");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error initializing database");
                throw;
            }
        }

        /// <summary>
        /// Gets an initialized SQLite connection to the database.
        /// </summary>
        /// <returns>A task that returns an initialized SQLite connection</returns>
        public async Task<SQLiteAsyncConnection> GetConnectionAsync()
        {
            await InitializeAsync();
            return _connection;
        }

        /// <summary>
        /// Resets the database by dropping all tables and recreating them.
        /// Used primarily for testing or when a clean state is required.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task ResetDatabaseAsync()
        {
            _logger?.LogWarning("Resetting database");
            
            await InitializeAsync();

            // Drop all tables
            try {
                await _connection.ExecuteAsync("DROP TABLE IF EXISTS VersionInfo");
                await _connection.ExecuteAsync("DROP TABLE IF EXISTS " + DatabaseConstants.TableSyncQueue);
                await _connection.ExecuteAsync("DROP TABLE IF EXISTS " + DatabaseConstants.TableCheckpointVerification);
                await _connection.ExecuteAsync("DROP TABLE IF EXISTS " + DatabaseConstants.TableCheckpoint);
                await _connection.ExecuteAsync("DROP TABLE IF EXISTS " + DatabaseConstants.TablePatrolLocation);
                await _connection.ExecuteAsync("DROP TABLE IF EXISTS " + DatabaseConstants.TableActivityReport);
                await _connection.ExecuteAsync("DROP TABLE IF EXISTS " + DatabaseConstants.TablePhoto);
                await _connection.ExecuteAsync("DROP TABLE IF EXISTS " + DatabaseConstants.TableLocationRecord);
                await _connection.ExecuteAsync("DROP TABLE IF EXISTS " + DatabaseConstants.TableTimeRecord);
                await _connection.ExecuteAsync("DROP TABLE IF EXISTS " + DatabaseConstants.TableUser);
            }
            catch (SQLiteException ex)
            {
                _logger?.LogError(ex, "Error dropping tables");
                throw;
            }

            // Create tables
            await CreateTablesAsync();

            // Reset version information
            try
            {
                await _connection.ExecuteAsync("CREATE TABLE IF NOT EXISTS VersionInfo (Version REAL NOT NULL)");
                await _connection.ExecuteAsync("DELETE FROM VersionInfo");
                await _connection.ExecuteAsync("INSERT INTO VersionInfo (Version) VALUES (0.0)");
            }
            catch (SQLiteException ex)
            {
                _logger?.LogError(ex, "Error resetting version information");
                throw;
            }

            _logger?.LogInformation("Database reset completed");
        }

        /// <summary>
        /// Applies any pending database migrations to update the schema to the latest version.
        /// </summary>
        /// <param name="connection">The SQLite connection to use for applying migrations</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task ApplyMigrationsAsync(SQLiteAsyncConnection connection)
        {
            _logger?.LogInformation("Applying database migrations");
            
            // Get current database version
            double currentVersion = await GetDatabaseVersionDirectlyAsync();
            
            // Apply migrations using the migration manager
            double newVersion = await _migrationManager.ApplyMigrationsAsync(connection, currentVersion);
            
            // Update the database version if migrations were applied
            if (newVersion > currentVersion)
            {
                try
                {
                    await connection.ExecuteAsync("UPDATE VersionInfo SET Version = ?", newVersion);
                    _logger?.LogInformation($"Database version updated to {newVersion}");
                }
                catch (SQLiteException ex)
                {
                    _logger?.LogError(ex, $"Error updating database version to {newVersion}");
                    throw;
                }
            }
            
            _logger?.LogInformation("Migration process completed");
        }

        /// <summary>
        /// Gets the current version of the database schema.
        /// </summary>
        /// <returns>A task that returns the current database version</returns>
        public async Task<double> GetDatabaseVersionAsync()
        {
            await InitializeAsync();
            return await GetDatabaseVersionDirectlyAsync();
        }

        /// <summary>
        /// Creates all required tables in the database if they don't already exist.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        private async Task CreateTablesAsync()
        {
            _logger?.LogInformation("Creating database tables if they don't exist");
            
            try
            {
                // Create tables using the SQL statements from DatabaseConstants
                await _connection.ExecuteAsync(DatabaseConstants.CreateTableUser);
                await _connection.ExecuteAsync(DatabaseConstants.CreateTableTimeRecord);
                await _connection.ExecuteAsync(DatabaseConstants.CreateTableLocationRecord);
                await _connection.ExecuteAsync(DatabaseConstants.CreateTablePhoto);
                await _connection.ExecuteAsync(DatabaseConstants.CreateTableActivityReport);
                await _connection.ExecuteAsync(DatabaseConstants.CreateTablePatrolLocation);
                await _connection.ExecuteAsync(DatabaseConstants.CreateTableCheckpoint);
                await _connection.ExecuteAsync(DatabaseConstants.CreateTableCheckpointVerification);
                await _connection.ExecuteAsync(DatabaseConstants.CreateTableSyncQueue);

                // Create indexes
                await _connection.ExecuteAsync(DatabaseConstants.CreateIndexTimeRecordUserId);
                await _connection.ExecuteAsync(DatabaseConstants.CreateIndexTimeRecordIsSynced);
                await _connection.ExecuteAsync(DatabaseConstants.CreateIndexLocationRecordUserIdTimestamp);
                await _connection.ExecuteAsync(DatabaseConstants.CreateIndexLocationRecordIsSynced);
                await _connection.ExecuteAsync(DatabaseConstants.CreateIndexPhotoUserId);
                await _connection.ExecuteAsync(DatabaseConstants.CreateIndexPhotoIsSynced);
                await _connection.ExecuteAsync(DatabaseConstants.CreateIndexActivityReportUserId);
                await _connection.ExecuteAsync(DatabaseConstants.CreateIndexActivityReportIsSynced);
                await _connection.ExecuteAsync(DatabaseConstants.CreateIndexCheckpointLocationId);
                await _connection.ExecuteAsync(DatabaseConstants.CreateIndexCheckpointVerificationCheckpointId);
                await _connection.ExecuteAsync(DatabaseConstants.CreateIndexCheckpointVerificationIsSynced);
                await _connection.ExecuteAsync(DatabaseConstants.CreateIndexSyncQueueEntityTypeEntityId);
                await _connection.ExecuteAsync(DatabaseConstants.CreateIndexSyncQueuePriorityLastAttempt);

                // Create the version info table if it doesn't exist
                await _connection.ExecuteAsync("CREATE TABLE IF NOT EXISTS VersionInfo (Version REAL NOT NULL)");
                
                // Insert initial version if it doesn't exist
                var versionExists = await _connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM VersionInfo") > 0;
                if (!versionExists)
                {
                    await _connection.ExecuteAsync("INSERT INTO VersionInfo (Version) VALUES (0.0)");
                }

                _logger?.LogInformation("Database tables created successfully");
            }
            catch (SQLiteException ex)
            {
                _logger?.LogError(ex, "Error creating database tables");
                throw;
            }
        }

        /// <summary>
        /// Creates a new SQLite connection to the database.
        /// </summary>
        /// <returns>A task that returns a new SQLite connection</returns>
        private async Task<SQLiteAsyncConnection> CreateDatabaseConnectionAsync()
        {
            _logger?.LogInformation($"Creating database connection to {_databasePath}");
            
            try
            {
                var flags = SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache;
                var connection = new SQLiteAsyncConnection(_databasePath, flags);
                
                // Enable foreign keys
                await connection.ExecuteAsync("PRAGMA foreign_keys = ON");
                
                return connection;
            }
            catch (SQLiteException ex)
            {
                _logger?.LogError(ex, "Error creating database connection");
                throw;
            }
        }

        /// <summary>
        /// Ensures that the directory for the database file exists, creating it if necessary.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        private Task EnsureDatabaseDirectoryExistsAsync()
        {
            try
            {
                var directory = Path.GetDirectoryName(_databasePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    _logger?.LogInformation($"Created directory for database: {directory}");
                }
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to ensure database directory exists");
                throw;
            }
        }

        /// <summary>
        /// Gets the current version of the database schema directly without initialization checks.
        /// </summary>
        /// <returns>A task that returns the current database version</returns>
        private async Task<double> GetDatabaseVersionDirectlyAsync()
        {
            if (_connection == null)
            {
                await EnsureDatabaseDirectoryExistsAsync();
                _connection = await CreateDatabaseConnectionAsync();
            }

            try
            {
                // Try to get the current version
                var result = await _connection.ExecuteScalarAsync<double>("SELECT Version FROM VersionInfo LIMIT 1");
                return result;
            }
            catch (SQLiteException)
            {
                // If the table doesn't exist, create it and return version 0.0
                try
                {
                    await _connection.ExecuteAsync("CREATE TABLE IF NOT EXISTS VersionInfo (Version REAL NOT NULL)");
                    await _connection.ExecuteAsync("INSERT INTO VersionInfo (Version) VALUES (0.0)");
                    return 0.0;
                }
                catch (SQLiteException ex)
                {
                    _logger?.LogError(ex, "Error creating version information table");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting database version");
                return 0.0;
            }
        }
    }
}