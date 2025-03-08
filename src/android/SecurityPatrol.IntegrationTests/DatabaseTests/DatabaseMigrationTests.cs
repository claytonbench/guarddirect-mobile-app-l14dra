using FluentAssertions;
using Microsoft.Extensions.Logging;
using SecurityPatrol.Constants;
using SecurityPatrol.Database.Migrations;
using SecurityPatrol.IntegrationTests.Helpers;
using SQLite;
using System;
using System.Threading.Tasks;
using Xunit;

namespace SecurityPatrol.IntegrationTests.DatabaseTests
{
    /// <summary>
    /// Contains integration tests for database migrations in the Security Patrol application.
    /// </summary>
    public class DatabaseMigrationTests : IDisposable
    {
        private ILoggerFactory _loggerFactory;
        private ILogger<DatabaseMigrationTests> _logger;
        private TestDatabaseInitializer _databaseInitializer;
        private MigrationManager _migrationManager;

        /// <summary>
        /// Initializes a new instance of the DatabaseMigrationTests class with required dependencies.
        /// </summary>
        public DatabaseMigrationTests()
        {
            _loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });
            
            _logger = _loggerFactory.CreateLogger<DatabaseMigrationTests>();
            _databaseInitializer = new TestDatabaseInitializer(_loggerFactory.CreateLogger<TestDatabaseInitializer>());
            _migrationManager = new MigrationManager(_loggerFactory.CreateLogger<MigrationManager>());
        }

        /// <summary>
        /// Cleans up resources used by the test class.
        /// </summary>
        public void Dispose()
        {
            if (_loggerFactory is IDisposable disposable)
            {
                disposable.Dispose();
            }
            
            _loggerFactory = null;
            _logger = null;
            _databaseInitializer = null;
            _migrationManager = null;
        }

        /// <summary>
        /// Tests that the initial migration (1.0) creates all required tables in the database.
        /// </summary>
        [Fact]
        public async Task TestInitialMigration_CreatesAllTables()
        {
            // Arrange
            await _databaseInitializer.ResetDatabaseAsync();
            var connection = await _databaseInitializer.GetConnectionAsync();
            var migration = new Migration_1_0();
            
            // Act
            await migration.ApplyAsync(connection);
            
            // Assert
            (await VerifyTableExists(connection, DatabaseConstants.TableUser)).Should().BeTrue();
            (await VerifyTableExists(connection, DatabaseConstants.TableTimeRecord)).Should().BeTrue();
            (await VerifyTableExists(connection, DatabaseConstants.TableLocationRecord)).Should().BeTrue();
            (await VerifyTableExists(connection, DatabaseConstants.TablePhoto)).Should().BeTrue();
            (await VerifyTableExists(connection, DatabaseConstants.TableActivityReport)).Should().BeTrue();
            (await VerifyTableExists(connection, DatabaseConstants.TablePatrolLocation)).Should().BeTrue();
            (await VerifyTableExists(connection, DatabaseConstants.TableCheckpoint)).Should().BeTrue();
            (await VerifyTableExists(connection, DatabaseConstants.TableCheckpointVerification)).Should().BeTrue();
            (await VerifyTableExists(connection, DatabaseConstants.TableSyncQueue)).Should().BeTrue();
            
            // Verify that database version is updated
            await connection.ExecuteAsync("UPDATE Version SET VersionNumber = ? WHERE Id = 1", migration.Version);
            var version = await _databaseInitializer.GetDatabaseVersionAsync();
            version.Should().Be(migration.Version);
        }

        /// <summary>
        /// Tests that migration 1.1 correctly adds the SyncProgress column to the Photo table.
        /// </summary>
        [Fact]
        public async Task TestMigration_1_1_AddsSyncProgressColumn()
        {
            // Arrange
            await _databaseInitializer.ResetDatabaseAsync();
            var connection = await _databaseInitializer.GetConnectionAsync();
            
            // Apply initial migration first
            var migration1_0 = new Migration_1_0();
            await migration1_0.ApplyAsync(connection);
            await connection.ExecuteAsync("UPDATE Version SET VersionNumber = ? WHERE Id = 1", migration1_0.Version);
            
            // Act
            var migration1_1 = new Migration_1_1();
            await migration1_1.ApplyAsync(connection);
            await connection.ExecuteAsync("UPDATE Version SET VersionNumber = ? WHERE Id = 1", migration1_1.Version);
            
            // Assert
            (await VerifyColumnExists(connection, DatabaseConstants.TablePhoto, DatabaseConstants.ColumnSyncProgress)).Should().BeTrue();
            
            // Verify database version is updated
            var version = await _databaseInitializer.GetDatabaseVersionAsync();
            version.Should().Be(migration1_1.Version);
        }

        /// <summary>
        /// Tests that migration 1.1 correctly adds RemoteId columns to all entity tables.
        /// </summary>
        [Fact]
        public async Task TestMigration_1_1_AddsRemoteIdColumns()
        {
            // Arrange
            await _databaseInitializer.ResetDatabaseAsync();
            var connection = await _databaseInitializer.GetConnectionAsync();
            
            // Apply initial migration first
            var migration1_0 = new Migration_1_0();
            await migration1_0.ApplyAsync(connection);
            await connection.ExecuteAsync("UPDATE Version SET VersionNumber = ? WHERE Id = 1", migration1_0.Version);
            
            // Act
            var migration1_1 = new Migration_1_1();
            await migration1_1.ApplyAsync(connection);
            await connection.ExecuteAsync("UPDATE Version SET VersionNumber = ? WHERE Id = 1", migration1_1.Version);
            
            // Assert - Check RemoteId column in all entity tables
            (await VerifyColumnExists(connection, DatabaseConstants.TableUser, DatabaseConstants.ColumnRemoteId)).Should().BeTrue();
            (await VerifyColumnExists(connection, DatabaseConstants.TableTimeRecord, DatabaseConstants.ColumnRemoteId)).Should().BeTrue();
            (await VerifyColumnExists(connection, DatabaseConstants.TableLocationRecord, DatabaseConstants.ColumnRemoteId)).Should().BeTrue();
            (await VerifyColumnExists(connection, DatabaseConstants.TablePhoto, DatabaseConstants.ColumnRemoteId)).Should().BeTrue();
            (await VerifyColumnExists(connection, DatabaseConstants.TableActivityReport, DatabaseConstants.ColumnRemoteId)).Should().BeTrue();
            (await VerifyColumnExists(connection, DatabaseConstants.TablePatrolLocation, DatabaseConstants.ColumnRemoteId)).Should().BeTrue();
            (await VerifyColumnExists(connection, DatabaseConstants.TableCheckpoint, DatabaseConstants.ColumnRemoteId)).Should().BeTrue();
            (await VerifyColumnExists(connection, DatabaseConstants.TableCheckpointVerification, DatabaseConstants.ColumnRemoteId)).Should().BeTrue();
            
            // Verify database version is updated
            var version = await _databaseInitializer.GetDatabaseVersionAsync();
            version.Should().Be(migration1_1.Version);
        }

        /// <summary>
        /// Tests that the MigrationManager applies migrations in the correct order based on version numbers.
        /// </summary>
        [Fact]
        public async Task TestMigrationManager_AppliesMigrationsInOrder()
        {
            // Arrange
            await _databaseInitializer.ResetDatabaseAsync();
            var connection = await _databaseInitializer.GetConnectionAsync();
            
            // Get initial version (should be 0.0)
            var initialVersion = await _databaseInitializer.GetDatabaseVersionAsync();
            initialVersion.Should().Be(0.0);
            
            // Act
            // Use MigrationManager to apply all migrations
            var newVersion = await _migrationManager.ApplyMigrationsAsync(connection, initialVersion);
            
            // Assert
            // Final version should be the highest migration version (1.1)
            newVersion.Should().Be(1.1);
            
            // Verify all tables exist
            (await VerifyTableExists(connection, DatabaseConstants.TableUser)).Should().BeTrue();
            (await VerifyTableExists(connection, DatabaseConstants.TableTimeRecord)).Should().BeTrue();
            (await VerifyTableExists(connection, DatabaseConstants.TableLocationRecord)).Should().BeTrue();
            (await VerifyTableExists(connection, DatabaseConstants.TablePhoto)).Should().BeTrue();
            
            // Verify columns from migration 1.1 exist
            (await VerifyColumnExists(connection, DatabaseConstants.TablePhoto, DatabaseConstants.ColumnSyncProgress)).Should().BeTrue();
            (await VerifyColumnExists(connection, DatabaseConstants.TableUser, DatabaseConstants.ColumnRemoteId)).Should().BeTrue();
        }

        /// <summary>
        /// Tests that the MigrationManager only applies migrations with versions higher than the current database version.
        /// </summary>
        [Fact]
        public async Task TestMigrationManager_OnlyAppliesNewerMigrations()
        {
            // Arrange
            await _databaseInitializer.ResetDatabaseAsync();
            var connection = await _databaseInitializer.GetConnectionAsync();
            
            // Apply the initial migration directly
            var migration1_0 = new Migration_1_0();
            await migration1_0.ApplyAsync(connection);
            await connection.ExecuteAsync("UPDATE Version SET VersionNumber = ? WHERE Id = 1", migration1_0.Version);
            
            // Verify current version is 1.0
            var currentVersion = await _databaseInitializer.GetDatabaseVersionAsync();
            currentVersion.Should().Be(migration1_0.Version);
            
            // Act
            // Use MigrationManager to apply all migrations (should only apply 1.1 since 1.0 is already applied)
            var newVersion = await _migrationManager.ApplyMigrationsAsync(connection, currentVersion);
            
            // Assert
            // Final version should be 1.1
            newVersion.Should().Be(1.1);
            
            // Verify columns from migration 1.1 exist
            (await VerifyColumnExists(connection, DatabaseConstants.TablePhoto, DatabaseConstants.ColumnSyncProgress)).Should().BeTrue();
            (await VerifyColumnExists(connection, DatabaseConstants.TableUser, DatabaseConstants.ColumnRemoteId)).Should().BeTrue();
        }

        /// <summary>
        /// Tests that the DatabaseInitializer automatically applies all migrations when initializing the database.
        /// </summary>
        [Fact]
        public async Task TestDatabaseInitializer_AppliesMigrationsOnInitialization()
        {
            // Arrange
            await _databaseInitializer.ResetDatabaseAsync();
            
            // Act
            await _databaseInitializer.InitializeAsync();
            
            // Assert
            // Database version should be the highest migration version (1.1)
            var version = await _databaseInitializer.GetDatabaseVersionAsync();
            version.Should().Be(1.1);
            
            // Verify all tables exist
            var connection = await _databaseInitializer.GetConnectionAsync();
            (await VerifyTableExists(connection, DatabaseConstants.TableUser)).Should().BeTrue();
            (await VerifyTableExists(connection, DatabaseConstants.TableTimeRecord)).Should().BeTrue();
            (await VerifyTableExists(connection, DatabaseConstants.TablePhoto)).Should().BeTrue();
            
            // Verify columns from migration 1.1 exist
            (await VerifyColumnExists(connection, DatabaseConstants.TablePhoto, DatabaseConstants.ColumnSyncProgress)).Should().BeTrue();
            (await VerifyColumnExists(connection, DatabaseConstants.TableUser, DatabaseConstants.ColumnRemoteId)).Should().BeTrue();
        }

        /// <summary>
        /// Helper method to verify that a table exists in the database.
        /// </summary>
        private async Task<bool> VerifyTableExists(SQLiteAsyncConnection connection, string tableName)
        {
            var result = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=?", tableName);
            return result > 0;
        }

        /// <summary>
        /// Helper method to verify that a column exists in a table.
        /// </summary>
        private async Task<bool> VerifyColumnExists(SQLiteAsyncConnection connection, string tableName, string columnName)
        {
            var columns = await connection.QueryAsync<dynamic>($"PRAGMA table_info({tableName})");
            foreach (var column in columns)
            {
                // The PRAGMA table_info returns a "name" column with the column name
                if (column.name == columnName)
                {
                    return true;
                }
            }
            return false;
        }
    }
}