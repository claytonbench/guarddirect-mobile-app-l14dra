using System; // Version 8.0.0
using System.Threading.Tasks; // Version 8.0.0
using System.Collections.Generic; // Version 8.0.0
using Microsoft.Extensions.DependencyInjection; // Version 8.0.0
using Microsoft.Extensions.Logging; // Version 8.0.0
using SQLite; // Version 1.8.0
using Xunit; // Version 2.4.2
using Xunit.Abstractions; // Version 2.0.3
using FluentAssertions; // Version 6.11.0
using SecurityPatrol.MAUI.IntegrationTests.Setup;
using SecurityPatrol.Services;
using SecurityPatrol.Database;
using SecurityPatrol.TestCommon.Fixtures;
using SecurityPatrol.Database.Entities;

namespace SecurityPatrol.MAUI.IntegrationTests.Database
{
    /// <summary>
    /// Integration tests for database operations in the Security Patrol application.
    /// </summary>
    [Collection("IntegrationTests")]
    public class DatabaseIntegrationTests
    {
        private readonly ITestOutputHelper _outputHelper;
        private IDatabaseService _databaseService;
        private IDatabaseInitializer _databaseInitializer;

        /// <summary>
        /// Initializes a new instance of the DatabaseIntegrationTests class with the specified test output helper.
        /// </summary>
        /// <param name="outputHelper">The test output helper to use for logging.</param>
        public DatabaseIntegrationTests(ITestOutputHelper outputHelper)
        {
            // Store outputHelper in _outputHelper field
            _outputHelper = outputHelper;
        }

        /// <summary>
        /// Initializes the test environment before each test.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        [PublicAPI]
        public async override Task InitializeAsync()
        {
            // Call base.InitializeAsync() to set up the test environment
            await base.InitializeAsync();

            // Get IDatabaseService from ServiceProvider
            _databaseService = ServiceProvider.GetService<IDatabaseService>();

            // Store database service in _databaseService field
            _databaseService.Should().NotBeNull();

            // Get IDatabaseInitializer from ServiceProvider
            _databaseInitializer = ServiceProvider.GetService<IDatabaseInitializer>();

            // Store database initializer in _databaseInitializer field
            _databaseInitializer.Should().NotBeNull();

            // Assert that _databaseService is not null
            Assert.NotNull(_databaseService);

            // Assert that _databaseInitializer is not null
            Assert.NotNull(_databaseInitializer);
        }

        /// <summary>
        /// Tests that InitializeAsync creates the database with the correct schema.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        [PublicAPI]
        [Fact]
        public async Task DatabaseService_InitializeAsync_CreatesDatabase()
        {
            // Call Database.ResetDatabaseAsync() to reset the database to a clean state
            await Database.ResetDatabaseAsync();

            // Call _databaseService.InitializeAsync() to initialize the database
            await _databaseService.InitializeAsync();

            // Get the database connection using _databaseService.GetConnectionAsync()
            var connection = await _databaseService.GetConnectionAsync();

            // Assert that the connection is not null
            Assert.NotNull(connection);

            // Query the database to verify tables were created
            // Assert that User table exists
            var userTableInfo = await connection.GetTableInfoAsync("User");
            Assert.NotNull(userTableInfo);

            // Assert that TimeRecord table exists
            var timeRecordTableInfo = await connection.GetTableInfoAsync("TimeRecord");
            Assert.NotNull(timeRecordTableInfo);

            // Assert that LocationRecord table exists
            var locationRecordTableInfo = await connection.GetTableInfoAsync("LocationRecord");
            Assert.NotNull(locationRecordTableInfo);

            // Assert that Photo table exists
            var photoTableInfo = await connection.GetTableInfoAsync("Photo");
            Assert.NotNull(photoTableInfo);

            // Assert that Report table exists
            var reportTableInfo = await connection.GetTableInfoAsync("ActivityReport");
            Assert.NotNull(reportTableInfo);

            // Assert that PatrolLocation table exists
            var patrolLocationTableInfo = await connection.GetTableInfoAsync("PatrolLocation");
            Assert.NotNull(patrolLocationTableInfo);

            // Assert that Checkpoint table exists
            var checkpointTableInfo = await connection.GetTableInfoAsync("Checkpoint");
            Assert.NotNull(checkpointTableInfo);

            // Assert that CheckpointVerification table exists
            var checkpointVerificationTableInfo = await connection.GetTableInfoAsync("CheckpointVerification");
            Assert.NotNull(checkpointVerificationTableInfo);

            // Assert that SyncQueue table exists
            var syncQueueTableInfo = await connection.GetTableInfoAsync("SyncQueue");
            Assert.NotNull(syncQueueTableInfo);
        }

        /// <summary>
        /// Tests that GetConnectionAsync returns the same connection instance for multiple calls.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        [PublicAPI]
        [Fact]
        public async Task DatabaseService_GetConnectionAsync_ReturnsSameConnection()
        {
            // Call _databaseService.GetConnectionAsync() to get the first connection
            var connection1 = await _databaseService.GetConnectionAsync();

            // Assert that the first connection is not null
            Assert.NotNull(connection1);

            // Call _databaseService.GetConnectionAsync() to get the second connection
            var connection2 = await _databaseService.GetConnectionAsync();

            // Assert that the second connection is not null
            Assert.NotNull(connection2);

            // Assert that the first and second connections are the same instance
            Assert.Same(connection1, connection2);

            // This verifies the connection pooling behavior
        }

        /// <summary>
        /// Tests that ExecuteQueryAsync correctly executes a query and returns results.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        [PublicAPI]
        [Fact]
        public async Task DatabaseService_ExecuteQueryAsync_ReturnsResults()
        {
            // Call Database.SeedTestDataAsync() to seed the database with test data
            await Database.SeedTestDataAsync();

            // Create a SQL query to select users
            string query = "SELECT * FROM User";

            // Call _databaseService.ExecuteQueryAsync<UserEntity>(query) to execute the query
            var result = await _databaseService.ExecuteQueryAsync<UserEntity>(query);

            // Assert that the result is not null
            Assert.NotNull(result);

            // Assert that the result contains at least one user
            Assert.NotEmpty(result);

            // Assert that the user properties match the expected values
            Assert.Equal("test-user-123", result[0].UserId);
        }

        /// <summary>
        /// Tests that ExecuteNonQueryAsync correctly executes a command and modifies data.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        [PublicAPI]
        [Fact]
        public async Task DatabaseService_ExecuteNonQueryAsync_ModifiesData()
        {
            // Call Database.SeedTestDataAsync() to seed the database with test data
            await Database.SeedTestDataAsync();

            // Create a SQL command to update a user
            string command = "UPDATE User SET PhoneNumber = ? WHERE UserId = ?";
            object[] parameters = { "+19999999999", "test-user-123" };

            // Call _databaseService.ExecuteNonQueryAsync(command, parameters) to execute the command
            var result = await _databaseService.ExecuteNonQueryAsync(command, parameters);

            // Assert that the result is 1 (one row affected)
            Assert.Equal(1, result);

            // Create a SQL query to select the updated user
            string query = "SELECT * FROM User WHERE UserId = ?";
            object[] queryParameters = { "test-user-123" };

            // Call _databaseService.ExecuteQueryAsync<UserEntity>(query) to execute the query
            var updatedUser = await _databaseService.ExecuteQueryAsync<UserEntity>(query, queryParameters);

            // Assert that the user was updated with the new values
            Assert.Equal("+19999999999", updatedUser[0].PhoneNumber);
        }

        /// <summary>
        /// Tests that database transactions correctly commit changes.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        [PublicAPI]
        [Fact]
        public async Task DatabaseService_Transaction_CommitsChanges()
        {
            // Call Database.SeedTestDataAsync() to seed the database with test data
            await Database.SeedTestDataAsync();

            // Call _databaseService.BeginTransactionAsync() to begin a transaction
            await _databaseService.BeginTransactionAsync();

            // Create a SQL command to insert a new time record
            string command = "INSERT INTO TimeRecord (UserId, Type, Timestamp, Latitude, Longitude, IsSynced) VALUES (?, ?, ?, ?, ?, ?)";
            object[] parameters = { "test-user-123", "ClockIn", DateTime.UtcNow, 34.0522, -118.2437, 0 };

            // Call _databaseService.ExecuteNonQueryAsync(command, parameters) to execute the command
            await _databaseService.ExecuteNonQueryAsync(command, parameters);

            // Call _databaseService.CommitTransactionAsync() to commit the transaction
            await _databaseService.CommitTransactionAsync();

            // Create a SQL query to select the inserted time record
            string query = "SELECT * FROM TimeRecord WHERE UserId = ? AND Type = ?";
            object[] queryParameters = { "test-user-123", "ClockIn" };

            // Call _databaseService.ExecuteQueryAsync<TimeRecordEntity>(query) to execute the query
            var timeRecord = await _databaseService.ExecuteQueryAsync<TimeRecordEntity>(query, queryParameters);

            // Assert that the time record was inserted with the correct values
            Assert.NotEmpty(timeRecord);
        }

        /// <summary>
        /// Tests that database transactions correctly roll back changes when requested.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        [PublicAPI]
        [Fact]
        public async Task DatabaseService_Transaction_RollsBackChanges()
        {
            // Call Database.SeedTestDataAsync() to seed the database with test data
            await Database.SeedTestDataAsync();

            // Count existing time records
            string countQuery = "SELECT COUNT(*) FROM TimeRecord";
            var initialCountResult = await _databaseService.ExecuteQueryAsync<object>(countQuery);
            int initialCount = Convert.ToInt32(initialCountResult[0]);

            // Call _databaseService.BeginTransactionAsync() to begin a transaction
            await _databaseService.BeginTransactionAsync();

            // Create a SQL command to insert a new time record
            string command = "INSERT INTO TimeRecord (UserId, Type, Timestamp, Latitude, Longitude, IsSynced) VALUES (?, ?, ?, ?, ?, ?)";
            object[] parameters = { "test-user-123", "ClockIn", DateTime.UtcNow, 34.0522, -118.2437, 0 };

            // Call _databaseService.ExecuteNonQueryAsync(command, parameters) to execute the command
            await _databaseService.ExecuteNonQueryAsync(command, parameters);

            // Call _databaseService.RollbackTransactionAsync() to roll back the transaction
            await _databaseService.RollbackTransactionAsync();

            // Count time records after rollback
            var finalCountResult = await _databaseService.ExecuteQueryAsync<object>(countQuery);
            int finalCount = Convert.ToInt32(finalCountResult[0]);

            // Assert that the count is the same as before (insert was rolled back)
            Assert.Equal(initialCount, finalCount);
        }

        /// <summary>
        /// Tests that RunInTransactionAsync correctly commits changes when the action succeeds.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        [PublicAPI]
        [Fact]
        public async Task DatabaseService_RunInTransactionAsync_CommitsOnSuccess()
        {
            // Call Database.SeedTestDataAsync() to seed the database with test data
            await Database.SeedTestDataAsync();

            // Count existing location records
            string countQuery = "SELECT COUNT(*) FROM LocationRecord";
            var initialCountResult = await _databaseService.ExecuteQueryAsync<object>(countQuery);
            int initialCount = Convert.ToInt32(initialCountResult[0]);

            // Create an action that inserts a new location record
            async Task insertAction()
            {
                string command = "INSERT INTO LocationRecord (UserId, Timestamp, Latitude, Longitude, Accuracy, IsSynced) VALUES (?, ?, ?, ?, ?, ?)";
                object[] parameters = { "test-user-123", DateTime.UtcNow, 34.0522, -118.2437, 10.0, 0 };
                await _databaseService.ExecuteNonQueryAsync(command, parameters);
            }

            // Call _databaseService.RunInTransactionAsync(action) to run the action in a transaction
            await _databaseService.RunInTransactionAsync(insertAction);

            // Count location records after the transaction
            var finalCountResult = await _databaseService.ExecuteQueryAsync<object>(countQuery);
            int finalCount = Convert.ToInt32(finalCountResult[0]);

            // Assert that the count increased by 1 (insert was committed)
            Assert.Equal(initialCount + 1, finalCount);
        }

        /// <summary>
        /// Tests that RunInTransactionAsync correctly rolls back changes when the action throws an exception.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        [PublicAPI]
        [Fact]
        public async Task DatabaseService_RunInTransactionAsync_RollsBackOnException()
        {
            // Call Database.SeedTestDataAsync() to seed the database with test data
            await Database.SeedTestDataAsync();

            // Count existing location records
            string countQuery = "SELECT COUNT(*) FROM LocationRecord";
            var initialCountResult = await _databaseService.ExecuteQueryAsync<object>(countQuery);
            int initialCount = Convert.ToInt32(initialCountResult[0]);

            // Create an action that inserts a new location record and then throws an exception
            async Task insertAction()
            {
                string command = "INSERT INTO LocationRecord (UserId, Timestamp, Latitude, Longitude, Accuracy, IsSynced) VALUES (?, ?, ?, ?, ?, ?)";
                object[] parameters = { "test-user-123", DateTime.UtcNow, 34.0522, -118.2437, 10.0, 0 };
                await _databaseService.ExecuteNonQueryAsync(command, parameters);
                throw new Exception("Simulated exception");
            }

            // Call _databaseService.RunInTransactionAsync(action) and catch the exception
            Exception caughtException = await Assert.ThrowsAsync<Exception>(() => _databaseService.RunInTransactionAsync(insertAction));

            // Assert that an exception was thrown
            Assert.NotNull(caughtException);

            // Count location records after the transaction
            var finalCountResult = await _databaseService.ExecuteQueryAsync<object>(countQuery);
            int finalCount = Convert.ToInt32(finalCountResult[0]);

            // Assert that the count is the same as before (insert was rolled back)
            Assert.Equal(initialCount, finalCount);
        }

        /// <summary>
        /// Tests that CheckDatabaseIntegrityAsync returns true for an intact database.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        [PublicAPI]
        [Fact]
        public async Task DatabaseService_CheckDatabaseIntegrityAsync_ReturnsTrue()
        {
            // Call Database.SeedTestDataAsync() to seed the database with test data
            await Database.SeedTestDataAsync();

            // Call _databaseService.CheckDatabaseIntegrityAsync() to check database integrity
            bool isIntact = await _databaseService.CheckDatabaseIntegrityAsync();

            // Assert that the result is true (database is intact)
            Assert.True(isIntact);
        }

        /// <summary>
        /// Tests that the database service correctly handles concurrent operations.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        [PublicAPI]
        [Fact]
        public async Task DatabaseService_ConcurrentOperations_HandleCorrectly()
        {
            // Call Database.SeedTestDataAsync() to seed the database with test data
            await Database.SeedTestDataAsync();

            // Create a list of 10 concurrent query tasks
            var tasks = new List<Task<List<UserEntity>>>();
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(_databaseService.ExecuteQueryAsync<UserEntity>("SELECT * FROM User"));
            }

            // Await Task.WhenAll to run all queries concurrently
            var results = await Task.WhenAll(tasks);

            // Assert that all queries completed successfully
            Assert.All(results, r => Assert.NotEmpty(r));

            // Assert that all queries returned the expected results
            foreach (var result in results)
            {
                Assert.Equal("test-user-123", result[0].UserId);
            }

            // This verifies that concurrent database access is handled correctly
        }
    }
}