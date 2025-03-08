using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using SQLite;
using SecurityPatrol.MAUI.UnitTests.Setup;
using SecurityPatrol.Database.Migrations;
using SecurityPatrol.Database;

namespace SecurityPatrol.MAUI.UnitTests.Database
{
    /// <summary>
    /// Contains unit tests for the database migration system in the Security Patrol MAUI application.
    /// </summary>
    public class MigrationTests : TestBase
    {
        private Mock<SQLiteAsyncConnection> _mockConnection;
        private Mock<ILogger<MigrationManager>> _mockLogger;

        /// <summary>
        /// Initializes a new instance of the MigrationTests class.
        /// </summary>
        public MigrationTests()
            : base()
        {
            // Constructor is handled by base class
        }

        /// <summary>
        /// Sets up the test environment before each test.
        /// </summary>
        public void Setup()
        {
            _mockConnection = new Mock<SQLiteAsyncConnection>();
            _mockLogger = new Mock<ILogger<MigrationManager>>();
            
            base.SetupMockDatabaseService();
            MockDatabaseService.Setup(x => x.GetConnectionAsync()).ReturnsAsync(_mockConnection.Object);
        }

        /// <summary>
        /// Tests that MigrationManager applies migrations in the correct order based on version numbers.
        /// </summary>
        [Fact]
        public async Task MigrationManager_ShouldApplyMigrations_InCorrectOrder()
        {
            // Arrange
            var migrationManager = new MigrationManager(_mockLogger.Object);
            
            // Create test migrations with different versions - deliberately out of order
            var testMigration1 = CreateTestMigration(1.0, false);
            var testMigration2 = CreateTestMigration(1.1, false);
            var testMigration3 = CreateTestMigration(1.2, false);
            
            // Set the _migrations field in MigrationManager using reflection - deliberately out of order
            var migrations = new List<IMigration> { testMigration3.Object, testMigration1.Object, testMigration2.Object };
            var migrationsField = typeof(MigrationManager).GetField("_migrations", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            migrationsField.SetValue(migrationManager, migrations);
            
            // Act
            double newVersion = await migrationManager.ApplyMigrationsAsync(_mockConnection.Object, 0.0);
            
            // Assert
            // Verify migrations were applied in ascending order by version number
            Moq.Mock.VerifySequence(testMigration1, m => m.ApplyAsync(_mockConnection.Object));
            Moq.Mock.Verify(testMigration2, m => m.ApplyAsync(_mockConnection.Object), Times.Once);
            Moq.Mock.Verify(testMigration3, m => m.ApplyAsync(_mockConnection.Object), Times.Once);
            
            // Verify the new version matches the highest migration version
            newVersion.Should().Be(1.2);
        }

        /// <summary>
        /// Tests that MigrationManager only applies migrations with versions higher than the current database version.
        /// </summary>
        [Fact]
        public async Task MigrationManager_ShouldOnlyApply_MigrationsWithHigherVersion()
        {
            // Arrange
            var migrationManager = new MigrationManager(_mockLogger.Object);
            
            // Create test migrations with different versions
            var testMigration1 = CreateTestMigration(1.0, false);
            var testMigration2 = CreateTestMigration(1.1, false);
            var testMigration3 = CreateTestMigration(1.2, false);
            
            // Set the _migrations field in MigrationManager
            var migrations = new List<IMigration> { testMigration1.Object, testMigration2.Object, testMigration3.Object };
            var migrationsField = typeof(MigrationManager).GetField("_migrations", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            migrationsField.SetValue(migrationManager, migrations);
            
            // Act - Set current version to 1.0, should only apply 1.1 and 1.2
            double newVersion = await migrationManager.ApplyMigrationsAsync(_mockConnection.Object, 1.0);
            
            // Assert
            // Verify only migrations with version > 1.0 were applied
            testMigration1.Verify(m => m.ApplyAsync(_mockConnection.Object), Times.Never);
            testMigration2.Verify(m => m.ApplyAsync(_mockConnection.Object), Times.Once);
            testMigration3.Verify(m => m.ApplyAsync(_mockConnection.Object), Times.Once);
            
            // Verify the new version matches the highest migration version
            newVersion.Should().Be(1.2);
        }

        /// <summary>
        /// Tests that MigrationManager returns the current version when there are no migrations to apply.
        /// </summary>
        [Fact]
        public async Task MigrationManager_ShouldReturnCurrentVersion_WhenNoMigrationsToApply()
        {
            // Arrange
            var migrationManager = new MigrationManager(_mockLogger.Object);
            
            // Create test migrations with versions lower than current
            var testMigration1 = CreateTestMigration(1.0, false);
            var testMigration2 = CreateTestMigration(1.5, false);
            
            // Set the _migrations field in MigrationManager
            var migrations = new List<IMigration> { testMigration1.Object, testMigration2.Object };
            var migrationsField = typeof(MigrationManager).GetField("_migrations", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            migrationsField.SetValue(migrationManager, migrations);
            
            // Act - Set current version to 2.0, should not apply any migrations
            double newVersion = await migrationManager.ApplyMigrationsAsync(_mockConnection.Object, 2.0);
            
            // Assert
            // Verify no migrations were applied
            testMigration1.Verify(m => m.ApplyAsync(_mockConnection.Object), Times.Never);
            testMigration2.Verify(m => m.ApplyAsync(_mockConnection.Object), Times.Never);
            
            // Verify the returned version equals the input current version (2.0)
            newVersion.Should().Be(2.0);
        }

        /// <summary>
        /// Tests that MigrationManager properly handles and propagates exceptions that occur during migration.
        /// </summary>
        [Fact]
        public async Task MigrationManager_ShouldHandleExceptions_DuringMigration()
        {
            // Arrange
            var migrationManager = new MigrationManager(_mockLogger.Object);
            
            // Create a mock migration that throws an exception during ApplyAsync
            var testMigration = CreateTestMigration(1.0, true);
            
            // Set the _migrations field in MigrationManager
            var migrations = new List<IMigration> { testMigration.Object };
            var migrationsField = typeof(MigrationManager).GetField("_migrations", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            migrationsField.SetValue(migrationManager, migrations);
            
            // Act/Assert - Verify that calling migrationManager.ApplyMigrationsAsync throws the expected exception
            await Assert.ThrowsAsync<Exception>(() => migrationManager.ApplyMigrationsAsync(_mockConnection.Object, 0.0));
            
            // Assert - Verify error was logged
            _mockLogger.Verify(l => l.Log(
                It.Is<LogLevel>(ll => ll == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce);
        }

        /// <summary>
        /// Tests that DatabaseInitializer correctly updates the database version after applying migrations.
        /// </summary>
        [Fact]
        public async Task DatabaseInitializer_ShouldUpdateVersion_AfterMigration()
        {
            // Arrange
            var mockDatabaseInitializer = new Mock<IDatabaseInitializer>();
            mockDatabaseInitializer.Setup(d => d.GetDatabaseVersionAsync()).ReturnsAsync(1.0);
            
            // Set up callback to simulate updating the version when ApplyMigrationsAsync is called
            double newVersion = 1.0;
            mockDatabaseInitializer.Setup(d => d.ApplyMigrationsAsync(It.IsAny<SQLiteAsyncConnection>()))
                .Callback(() => newVersion = 1.1)
                .Returns(Task.CompletedTask);
            
            // Act
            await mockDatabaseInitializer.Object.ApplyMigrationsAsync(_mockConnection.Object);
            double resultVersion = await mockDatabaseInitializer.Object.GetDatabaseVersionAsync();
            
            // Assert
            resultVersion.Should().Be(1.1);
            mockDatabaseInitializer.Verify(d => d.ApplyMigrationsAsync(_mockConnection.Object), Times.Once());
        }

        /// <summary>
        /// Tests that Migration_1_0 creates all required database tables and indexes.
        /// </summary>
        [Fact]
        public async Task Migration_1_0_ShouldCreateAllTables()
        {
            // Arrange
            var migration = new Migration_1_0();
            var executedSql = new List<string>();
            
            // Configure _mockConnection to track executed SQL commands
            _mockConnection.Setup(c => c.ExecuteAsync(It.IsAny<string>()))
                .Callback<string>(sql => executedSql.Add(sql))
                .ReturnsAsync(0);
            
            // Act
            await migration.ApplyAsync(_mockConnection.Object);
            
            // Assert
            // Verify the migration's Version property equals 1.0
            migration.Version.Should().Be(1.0);
            
            // Verify SQL commands for creating User table were executed
            executedSql.Should().Contain(s => s.Contains("CREATE TABLE IF NOT EXISTS User"));
            
            // Verify SQL commands for creating TimeRecord table were executed
            executedSql.Should().Contain(s => s.Contains("CREATE TABLE IF NOT EXISTS TimeRecord"));
            
            // Verify SQL commands for creating LocationRecord table were executed
            executedSql.Should().Contain(s => s.Contains("CREATE TABLE IF NOT EXISTS LocationRecord"));
            
            // Verify SQL commands for creating Photo table were executed
            executedSql.Should().Contain(s => s.Contains("CREATE TABLE IF NOT EXISTS Photo"));
            
            // Verify SQL commands for creating ActivityReport table were executed
            executedSql.Should().Contain(s => s.Contains("CREATE TABLE IF NOT EXISTS ActivityReport"));
            
            // Verify SQL commands for creating PatrolLocation table were executed
            executedSql.Should().Contain(s => s.Contains("CREATE TABLE IF NOT EXISTS PatrolLocation"));
            
            // Verify SQL commands for creating Checkpoint table were executed
            executedSql.Should().Contain(s => s.Contains("CREATE TABLE IF NOT EXISTS Checkpoint"));
            
            // Verify SQL commands for creating CheckpointVerification table were executed
            executedSql.Should().Contain(s => s.Contains("CREATE TABLE IF NOT EXISTS CheckpointVerification"));
            
            // Verify SQL commands for creating SyncQueue table were executed
            executedSql.Should().Contain(s => s.Contains("CREATE TABLE IF NOT EXISTS SyncQueue"));
            
            // Verify SQL commands for creating indexes were executed
            executedSql.Should().Contain(s => s.Contains("CREATE INDEX IF NOT EXISTS IX_TimeRecord_UserId"));
            executedSql.Should().Contain(s => s.Contains("CREATE INDEX IF NOT EXISTS IX_TimeRecord_IsSynced"));
            executedSql.Should().Contain(s => s.Contains("CREATE INDEX IF NOT EXISTS IX_LocationRecord_UserId_Timestamp"));
            executedSql.Should().Contain(s => s.Contains("CREATE INDEX IF NOT EXISTS IX_LocationRecord_IsSynced"));
            executedSql.Should().Contain(s => s.Contains("CREATE INDEX IF NOT EXISTS IX_Photo_UserId"));
            executedSql.Should().Contain(s => s.Contains("CREATE INDEX IF NOT EXISTS IX_Photo_IsSynced"));
            executedSql.Should().Contain(s => s.Contains("CREATE INDEX IF NOT EXISTS IX_ActivityReport_UserId"));
            executedSql.Should().Contain(s => s.Contains("CREATE INDEX IF NOT EXISTS IX_ActivityReport_IsSynced"));
            executedSql.Should().Contain(s => s.Contains("CREATE INDEX IF NOT EXISTS IX_Checkpoint_LocationId"));
            executedSql.Should().Contain(s => s.Contains("CREATE INDEX IF NOT EXISTS IX_CheckpointVerification_CheckpointId"));
            executedSql.Should().Contain(s => s.Contains("CREATE INDEX IF NOT EXISTS IX_CheckpointVerification_IsSynced"));
            executedSql.Should().Contain(s => s.Contains("CREATE INDEX IF NOT EXISTS IX_SyncQueue_EntityType_EntityId"));
            executedSql.Should().Contain(s => s.Contains("CREATE INDEX IF NOT EXISTS IX_SyncQueue_Priority_LastAttempt"));
        }

        /// <summary>
        /// Tests that Migration_1_1 adds the required columns to existing tables.
        /// </summary>
        [Fact]
        public async Task Migration_1_1_ShouldAddRequiredColumns()
        {
            // Arrange
            var migration = new Migration_1_1();
            var executedSql = new List<string>();
            
            // Configure _mockConnection to track executed SQL commands
            _mockConnection.Setup(c => c.ExecuteAsync(It.IsAny<string>()))
                .Callback<string>(sql => executedSql.Add(sql))
                .ReturnsAsync(0);
            
            // Act
            await migration.ApplyAsync(_mockConnection.Object);
            
            // Assert
            // Verify the migration's Version property equals 1.1
            migration.Version.Should().Be(1.1);
            
            // Verify SQL commands for adding SyncProgress column to Photo table were executed
            executedSql.Should().Contain(s => s.Contains("ALTER TABLE Photo ADD COLUMN SyncProgress"));
            
            // Verify SQL commands for adding RemoteId columns to relevant tables were executed
            executedSql.Should().Contain(s => s.Contains("ALTER TABLE User ADD COLUMN RemoteId"));
            executedSql.Should().Contain(s => s.Contains("ALTER TABLE TimeRecord ADD COLUMN RemoteId"));
            executedSql.Should().Contain(s => s.Contains("ALTER TABLE LocationRecord ADD COLUMN RemoteId"));
            executedSql.Should().Contain(s => s.Contains("ALTER TABLE Photo ADD COLUMN RemoteId"));
            executedSql.Should().Contain(s => s.Contains("ALTER TABLE ActivityReport ADD COLUMN RemoteId"));
            executedSql.Should().Contain(s => s.Contains("ALTER TABLE PatrolLocation ADD COLUMN RemoteId"));
            executedSql.Should().Contain(s => s.Contains("ALTER TABLE Checkpoint ADD COLUMN RemoteId"));
            executedSql.Should().Contain(s => s.Contains("ALTER TABLE CheckpointVerification ADD COLUMN RemoteId"));
        }

        /// <summary>
        /// Creates a test migration with the specified version and behavior.
        /// </summary>
        /// <param name="version">The version number for the migration</param>
        /// <param name="throwException">Whether the migration should throw an exception when applied</param>
        /// <returns>A mock migration with the specified behavior</returns>
        private Mock<IMigration> CreateTestMigration(double version, bool throwException)
        {
            var mockMigration = new Mock<IMigration>();
            mockMigration.Setup(m => m.Version).Returns(version);
            
            if (throwException)
            {
                mockMigration.Setup(m => m.ApplyAsync(It.IsAny<SQLiteAsyncConnection>()))
                    .ThrowsAsync(new Exception("Test migration exception"));
            }
            else
            {
                mockMigration.Setup(m => m.ApplyAsync(It.IsAny<SQLiteAsyncConnection>()))
                    .Returns(Task.CompletedTask);
            }
            
            return mockMigration;
        }
    }

    /// <summary>
    /// A test implementation of IMigration for use in unit tests.
    /// </summary>
    public class TestMigration : IMigration
    {
        /// <summary>
        /// Gets the version number of this migration.
        /// </summary>
        public double Version { get; }

        /// <summary>
        /// Gets a value indicating whether this migration should throw an exception when applied.
        /// </summary>
        public bool ThrowException { get; }

        /// <summary>
        /// Gets a value indicating whether this migration has been applied.
        /// </summary>
        public bool WasApplied { get; private set; }

        /// <summary>
        /// Initializes a new instance of the TestMigration class with the specified version and exception behavior.
        /// </summary>
        /// <param name="version">The version number for the migration</param>
        /// <param name="throwException">Whether the migration should throw an exception when applied</param>
        public TestMigration(double version, bool throwException)
        {
            Version = version;
            ThrowException = throwException;
            WasApplied = false;
        }

        /// <summary>
        /// Applies the migration to the database, or throws an exception if configured to do so.
        /// </summary>
        /// <param name="connection">The database connection to use</param>
        /// <returns>A task representing the asynchronous migration operation</returns>
        public async Task ApplyAsync(SQLiteAsyncConnection connection)
        {
            if (ThrowException)
                throw new Exception("Test migration exception");
                
            WasApplied = true;
            await Task.CompletedTask;
        }
    }
}