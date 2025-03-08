using System; // Version 8.0.0
using System.Threading.Tasks; // Version 8.0.0
using System.Collections.Generic; // Version 8.0.0
using System.Linq; // Version 8.0.0
using Microsoft.Extensions.Logging; // Version 8.0.0
using Xunit; // Version 2.4.2
using Xunit.Abstractions; // Version 2.0.3
using FluentAssertions; // Version 6.11.0
using BenchmarkDotNet.Attributes; // Version 0.13.5
using SecurityPatrol.MAUI.PerformanceTests.Setup;
using SecurityPatrol.Services;
using SecurityPatrol.Database.Entities;
using SecurityPatrol.Database.Migrations;

namespace SecurityPatrol.MAUI.PerformanceTests
{
    /// <summary>
    /// Performance tests for database operations in the Security Patrol application.
    /// Measures and validates the performance of critical database operations including
    /// initialization, queries, transactions, and data access patterns.
    /// </summary>
    [public]
    public class DatabasePerformanceTests : PerformanceTestBase
    {
        private IDatabaseService _databaseService;
        private const double DatabaseInitializationThresholdMs = 1000;
        private const double QueryExecutionThresholdMs = 100;
        private const double TransactionThresholdMs = 200;
        private const double BatchInsertThresholdMs = 500;
        private const double MigrationThresholdMs = 1500;
        private const double DatabaseMemoryThresholdMB = 10;

        /// <summary>
        /// Initializes a new instance of the DatabasePerformanceTests class with the specified test output helper.
        /// </summary>
        /// <param name="outputHelper">The test output helper for writing test results.</param>
        public DatabasePerformanceTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
            // Call base constructor with outputHelper
            // Initialize performance threshold constants
            // Set DatabaseInitializationThresholdMs to 1000ms
            // Set QueryExecutionThresholdMs to 100ms
            // Set TransactionThresholdMs to 200ms
            // Set BatchInsertThresholdMs to 500ms
            // Set MigrationThresholdMs to 1500ms
            // Set DatabaseMemoryThresholdMB to 10MB
        }

        /// <summary>
        /// Initializes the test environment before each test.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        [public]
        [override]
        [async]
        public override async Task InitializeAsync()
        {
            // Call base.InitializeAsync() to set up the test environment
            await base.InitializeAsync();

            // Get IDatabaseService from ServiceProvider
            _databaseService = ServiceProvider.GetService<IDatabaseService>();

            // Store database service in _databaseService field
            // Assert that _databaseService is not null
            _databaseService.Should().NotBeNull("IDatabaseService should be resolved from ServiceProvider");

            // Reset the database to ensure a clean state for performance testing
            await _databaseService.InitializeAsync();
        }

        /// <summary>
        /// Tests the performance of database initialization.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        [public]
        [async]
        [Fact]
        public async Task TestDatabaseInitializationPerformance()
        {
            // Log test start
            Logger.LogInformation("Starting database initialization performance test");

            // Reset the database to ensure a clean state
            await _databaseService.InitializeAsync();

            // Measure execution time of _databaseService.InitializeAsync()
            double initializationTime = await MeasureExecutionTimeAsync(
                async () => await _databaseService.InitializeAsync(),
                "DatabaseInitialization");

            // Assert that initialization time is below DatabaseInitializationThresholdMs
            AssertPerformanceThreshold(initializationTime, DatabaseInitializationThresholdMs, "Database Initialization Time");

            // Measure memory usage during initialization
            long memoryUsage = await MeasureMemoryUsageAsync(
                async () => await _databaseService.InitializeAsync(),
                "DatabaseInitializationMemory");

            // Assert that memory usage is below DatabaseMemoryThresholdMB
            AssertMemoryThreshold(memoryUsage, DatabaseMemoryThresholdMB, "Database Initialization Memory");

            // Log test completion with performance metrics
            Logger.LogInformation(
                "Database initialization performance test completed. Initialization Time: {InitializationTime} ms, Memory Usage: {MemoryUsage} bytes",
                initializationTime, memoryUsage);
        }

        /// <summary>
        /// Tests the performance of database query operations.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        [public]
        [async]
        [Fact]
        public async Task TestQueryPerformance()
        {
            // Log test start
            Logger.LogInformation("Starting database query performance test");

            // Seed the database with test data
            await _databaseService.InitializeAsync();

            // Create a SQL query to select users
            string selectUsersQuery = "SELECT * FROM User";

            // Run benchmark for ExecuteQueryAsync with the query
            (double averageQueryTime, _) = await RunBenchmarkAsync(
                async () => await _databaseService.ExecuteQueryAsync<UserEntity>(selectUsersQuery),
                "SimpleQuery", 5);

            // Assert that average query time is below QueryExecutionThresholdMs
            AssertPerformanceThreshold(averageQueryTime, QueryExecutionThresholdMs, "Simple Query Time");

            // Create a more complex query with joins
            string complexQuery = @"
                SELECT U.UserId, T.Type, T.Timestamp
                FROM User U
                INNER JOIN TimeRecord T ON U.UserId = T.UserId
                WHERE U.UserId = 'test-user-123'
                ORDER BY T.Timestamp DESC";

            // Run benchmark for ExecuteQueryAsync with the complex query
            (double averageComplexQueryTime, _) = await RunBenchmarkAsync(
                async () => await _databaseService.ExecuteQueryAsync<dynamic>(complexQuery),
                "ComplexQuery", 5);

            // Assert that average complex query time is below QueryExecutionThresholdMs * 2
            AssertPerformanceThreshold(averageComplexQueryTime, QueryExecutionThresholdMs * 2, "Complex Query Time");

            // Log test completion with performance metrics
            Logger.LogInformation(
                "Database query performance test completed. Simple Query Time: {SimpleQueryTime} ms, Complex Query Time: {ComplexQueryTime} ms",
                averageQueryTime, averageComplexQueryTime);
        }

        /// <summary>
        /// Tests the performance of database transaction operations.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        [public]
        [async]
        [Fact]
        public async Task TestTransactionPerformance()
        {
            // Log test start
            Logger.LogInformation("Starting database transaction performance test");

            // Seed the database with test data
            await _databaseService.InitializeAsync();

            // Create a transaction operation that inserts, updates, and deletes records
            Func<Task> transactionOperation = CreateTransactionOperation();

            // Run benchmark for RunInTransactionAsync with the transaction operation
            (double averageTransactionTime, _) = await RunBenchmarkAsync(
                async () => await _databaseService.RunInTransactionAsync(transactionOperation),
                "TransactionOperation", 5);

            // Assert that average transaction time is below TransactionThresholdMs
            AssertPerformanceThreshold(averageTransactionTime, TransactionThresholdMs, "Transaction Time");

            // Log test completion with performance metrics
            Logger.LogInformation(
                "Database transaction performance test completed. Transaction Time: {TransactionTime} ms",
                averageTransactionTime);
        }

        /// <summary>
        /// Tests the performance of batch insert operations.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        [public]
        [async]
        [Fact]
        public async Task TestBatchInsertPerformance()
        {
            // Log test start
            Logger.LogInformation("Starting database batch insert performance test");

            // Reset the database to ensure a clean state
            await _databaseService.InitializeAsync();

            // Create a batch of 100 location records
            List<LocationRecordEntity> locationRecords = GenerateTestLocationRecords(100);

            // Create a batch insert operation using a transaction
            Func<Task> batchInsertOperation = CreateBatchInsertOperation(locationRecords);

            // Run benchmark for the batch insert operation
            (double averageBatchInsertTime, _) = await RunBenchmarkAsync(
                batchInsertOperation,
                "BatchInsertOperation", 5);

            // Assert that average batch insert time is below BatchInsertThresholdMs
            AssertPerformanceThreshold(averageBatchInsertTime, BatchInsertThresholdMs, "Batch Insert Time");

            // Measure memory usage during batch insert
            long memoryUsage = await MeasureMemoryUsageAsync(
                batchInsertOperation,
                "BatchInsertMemory");

            // Assert that memory usage is below DatabaseMemoryThresholdMB
            AssertMemoryThreshold(memoryUsage, DatabaseMemoryThresholdMB, "Batch Insert Memory");

            // Log test completion with performance metrics
            Logger.LogInformation(
                "Database batch insert performance test completed. Batch Insert Time: {BatchInsertTime} ms, Memory Usage: {MemoryUsage} bytes",
                averageBatchInsertTime, memoryUsage);
        }

        /// <summary>
        /// Tests the performance of database migration operations.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        [public]
        [async]
        [Fact]
        public async Task TestMigrationPerformance()
        {
            // Log test start
            Logger.LogInformation("Starting database migration performance test");

            // Reset the database to ensure a clean state
            await _databaseService.InitializeAsync();

            // Get database connection
            var connection = await _databaseService.GetConnectionAsync();

            // Create a MigrationManager instance
            var migrationManager = new MigrationManager(Logger);

            // Measure execution time of applying migrations from version 0.0
            double migrationTime = await MeasureExecutionTimeAsync(
                async () => await migrationManager.ApplyMigrationsAsync(connection, 0.0),
                "DatabaseMigration");

            // Assert that migration time is below MigrationThresholdMs
            AssertPerformanceThreshold(migrationTime, MigrationThresholdMs, "Database Migration Time");

            // Measure memory usage during migration
            long memoryUsage = await MeasureMemoryUsageAsync(
                async () => await migrationManager.ApplyMigrationsAsync(connection, 0.0),
                "DatabaseMigrationMemory");

            // Assert that memory usage is below DatabaseMemoryThresholdMB
            AssertMemoryThreshold(memoryUsage, DatabaseMemoryThresholdMB, "Database Migration Memory");

            // Log test completion with performance metrics
            Logger.LogInformation(
                "Database migration performance test completed. Migration Time: {MigrationTime} ms, Memory Usage: {MemoryUsage} bytes",
                migrationTime, memoryUsage);
        }

        /// <summary>
        /// Tests the performance impact of connection pooling.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        [public]
        [async]
        [Fact]
        public async Task TestConnectionPoolingPerformance()
        {
            // Log test start
            Logger.LogInformation("Starting database connection pooling performance test");

            // Measure execution time of first GetConnectionAsync call
            double firstConnectionTime = await MeasureExecutionTimeAsync(
                async () => await _databaseService.GetConnectionAsync(),
                "FirstConnection");

            // Measure execution time of subsequent GetConnectionAsync calls
            double subsequentConnectionTime = await MeasureExecutionTimeAsync(
                async () => await _databaseService.GetConnectionAsync(),
                "SubsequentConnection");

            // Assert that subsequent calls are significantly faster than the first call
            subsequentConnectionTime.Should().BeLessThan(firstConnectionTime / 2, "Subsequent connection should be faster due to pooling");

            // Run benchmark for 100 consecutive GetConnectionAsync calls
            (double averageConnectionTime, _) = await RunBenchmarkAsync(
                async () => await _databaseService.GetConnectionAsync(),
                "PooledConnectionBenchmark", 100);

            // Assert that average connection time is below 5ms for pooled connections
            AssertPerformanceThreshold(averageConnectionTime, 5, "Pooled Connection Time");

            // Log test completion with performance metrics
            Logger.LogInformation(
                "Database connection pooling performance test completed. First Connection Time: {FirstConnectionTime} ms, Subsequent Connection Time: {SubsequentConnectionTime} ms, Pooled Connection Time: {PooledConnectionTime} ms",
                firstConnectionTime, subsequentConnectionTime, averageConnectionTime);
        }

        /// <summary>
        /// Tests database performance under concurrent load.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        [public]
        [async]
        [Fact]
        public async Task TestDatabasePerformanceUnderLoad()
        {
            // Log test start
            Logger.LogInformation("Starting database performance under load test");

            // Seed the database with test data
            await _databaseService.InitializeAsync();

            // Create 10 concurrent query tasks
            List<Task> queryTasks = new List<Task>();
            for (int i = 0; i < 10; i++)
            {
                queryTasks.Add(Task.Run(async () =>
                {
                    string selectUsersQuery = "SELECT * FROM User";
                    await _databaseService.ExecuteQueryAsync<UserEntity>(selectUsersQuery);
                }));
            }

            // Measure execution time of running all queries concurrently
            double concurrentQueryTime = await MeasureExecutionTimeAsync(
                async () => await Task.WhenAll(queryTasks),
                "ConcurrentQueries");

            // Assert that total execution time is below QueryExecutionThresholdMs * 3
            AssertPerformanceThreshold(concurrentQueryTime, QueryExecutionThresholdMs * 3, "Concurrent Queries Time");

            // Create 10 concurrent insert tasks
            List<Task> insertTasks = new List<Task>();
            for (int i = 0; i < 10; i++)
            {
                insertTasks.Add(Task.Run(async () =>
                {
                    string insertQuery = "INSERT INTO User (UserId, PhoneNumber, LastAuthenticated) VALUES ('test-user-" + i + "', '+15551234567', '" + DateTime.UtcNow.ToString() + "')";
                    await _databaseService.ExecuteNonQueryAsync(insertQuery);
                }));
            }

            // Measure execution time of running all inserts concurrently
            double concurrentInsertTime = await MeasureExecutionTimeAsync(
                async () => await Task.WhenAll(insertTasks),
                "ConcurrentInserts");

            // Assert that total execution time is below TransactionThresholdMs * 3
            AssertPerformanceThreshold(concurrentInsertTime, TransactionThresholdMs * 3, "Concurrent Inserts Time");

            // Log test completion with performance metrics
            Logger.LogInformation(
                "Database performance under load test completed. Concurrent Queries Time: {ConcurrentQueryTime} ms, Concurrent Inserts Time: {ConcurrentInsertTime} ms",
                concurrentQueryTime, concurrentInsertTime);
        }

        /// <summary>
        /// Tests database performance in a simulated low-resource environment.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        [public]
        [async]
        [Fact]
        public async Task TestDatabasePerformanceWithLowResources()
        {
            // Log test start
            Logger.LogInformation("Starting database performance with low resources test");

            // Call SimulateLowResourceEnvironment() to simulate a low-resource device
            SimulateLowResourceEnvironment();

            // Seed the database with test data
            await _databaseService.InitializeAsync();

            // Run benchmark for basic query operations
            (double lowResourceQueryTime, _) = await RunBenchmarkAsync(
                async () => await _databaseService.ExecuteQueryAsync<UserEntity>("SELECT * FROM User"),
                "LowResourceQuery", 5);

            // Assert that performance is still acceptable though potentially degraded
            AssertPerformanceThreshold(lowResourceQueryTime, QueryExecutionThresholdMs * 2, "Low Resource Query Time");

            // Run benchmark for basic insert operations
            (double lowResourceInsertTime, _) = await RunBenchmarkAsync(
                async () => await _databaseService.ExecuteNonQueryAsync("INSERT INTO User (UserId, PhoneNumber, LastAuthenticated) VALUES ('low-resource-user', '+15551112222', '" + DateTime.UtcNow.ToString() + "')"),
                "LowResourceInsert", 5);

            // Assert that performance is still acceptable though potentially degraded
            AssertPerformanceThreshold(lowResourceInsertTime, TransactionThresholdMs * 2, "Low Resource Insert Time");

            // Log test completion with performance metrics
            Logger.LogInformation(
                "Database performance with low resources test completed. Low Resource Query Time: {LowResourceQueryTime} ms, Low Resource Insert Time: {LowResourceInsertTime} ms",
                lowResourceQueryTime, lowResourceInsertTime);
        }

        /// <summary>
        /// Creates a batch insert operation for performance testing.
        /// </summary>
        /// <param name="records">The list of location records to insert.</param>
        /// <returns>A function that performs the batch insert operation</returns>
        [private]
        private Func<Task> CreateBatchInsertOperation(List<LocationRecordEntity> records)
        {
            // Return an async function that:
            //   Begins a transaction
            //   Inserts all records in the batch
            //   Commits the transaction
            return async () =>
            {
                await _databaseService.RunInTransactionAsync(async () =>
                {
                    foreach (var record in records)
                    {
                        string insertQuery = @"
                            INSERT INTO LocationRecord (UserId, Timestamp, Latitude, Longitude, Accuracy, IsSynced)
                            VALUES (@UserId, @Timestamp, @Latitude, @Longitude, @Accuracy, @IsSynced)";
                        await _databaseService.ExecuteNonQueryAsync(insertQuery,
                            new
                            {
                                record.UserId,
                                record.Timestamp,
                                record.Latitude,
                                record.Longitude,
                                record.Accuracy,
                                record.IsSynced
                            });
                    }
                });
            };
        }

        /// <summary>
        /// Creates a complex transaction operation for performance testing.
        /// </summary>
        /// <returns>A function that performs the transaction operation</returns>
        [private]
        private Func<Task> CreateTransactionOperation()
        {
            // Return an async function that:
            //   Begins a transaction
            //   Inserts a new time record
            //   Updates an existing time record
            //   Deletes an old time record
            //   Commits the transaction
            return async () =>
            {
                await _databaseService.RunInTransactionAsync(async () =>
                {
                    // Insert a new time record
                    string insertQuery = @"
                        INSERT INTO TimeRecord (UserId, Type, Timestamp, Latitude, Longitude, IsSynced)
                        VALUES ('test-user-123', 'ClockIn', '" + DateTime.UtcNow.ToString() + "', 34.0522, -118.2437, 0)";
                    await _databaseService.ExecuteNonQueryAsync(insertQuery);

                    // Update an existing time record
                    string updateQuery = @"
                        UPDATE TimeRecord
                        SET IsSynced = 1
                        WHERE UserId = 'test-user-123' AND Type = 'ClockIn'";
                    await _databaseService.ExecuteNonQueryAsync(updateQuery);

                    // Delete an old time record
                    string deleteQuery = @"
                        DELETE FROM TimeRecord
                        WHERE UserId = 'test-user-123' AND Type = 'ClockOut'";
                    await _databaseService.ExecuteNonQueryAsync(deleteQuery);
                });
            };
        }

        /// <summary>
        /// Generates a list of test location records for performance testing.
        /// </summary>
        /// <param name="count">The number of records to generate.</param>
        /// <returns>A list of test location records</returns>
        [private]
        private List<LocationRecordEntity> GenerateTestLocationRecords(int count)
        {
            // Create a new list of LocationRecordEntity
            List<LocationRecordEntity> records = new List<LocationRecordEntity>();

            // For each count, add a new LocationRecordEntity with test data
            for (int i = 0; i < count; i++)
            {
                records.Add(new LocationRecordEntity
                {
                    UserId = "test-user-123",
                    Timestamp = DateTime.UtcNow,
                    Latitude = 34.0522 + i * 0.001,
                    Longitude = -118.2437 - i * 0.001,
                    Accuracy = 10,
                    IsSynced = false
                });
            }

            // Return the list of records
            return records;
        }
    }
}