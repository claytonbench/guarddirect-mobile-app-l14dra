using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging; // v8.0+
using SQLite; // SQLite-net-pcl v1.8+
using SecurityPatrol.Constants;
using SecurityPatrol.Database;

namespace SecurityPatrol.Services
{
    /// <summary>
    /// Implements the IDatabaseService interface to provide database operations for the Security Patrol application.
    /// This service manages database connections, transactions, and query execution, serving as a central point 
    /// for all database interactions in the application.
    /// </summary>
    public class DatabaseService : IDatabaseService
    {
        private readonly ILogger<DatabaseService> _logger;
        private readonly IDatabaseInitializer _databaseInitializer;
        private SQLiteAsyncConnection _connection;
        private readonly object _transactionLock = new object();
        private bool _isInTransaction;

        /// <summary>
        /// Initializes a new instance of the DatabaseService class with the specified logger and database initializer.
        /// </summary>
        /// <param name="logger">The logger instance for logging database operations and errors.</param>
        /// <param name="databaseInitializer">The database initializer for database setup and connection management.</param>
        public DatabaseService(ILogger<DatabaseService> logger, IDatabaseInitializer databaseInitializer)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _databaseInitializer = databaseInitializer ?? throw new ArgumentNullException(nameof(databaseInitializer));
        }

        /// <summary>
        /// Initializes the database by ensuring the database file exists and the schema is up-to-date.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InitializeAsync()
        {
            _logger.LogInformation("Initializing database: {DatabaseName}", DatabaseConstants.DatabaseName);
            
            try
            {
                await _databaseInitializer.InitializeAsync();
                _logger.LogInformation("Database initialization completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing database: {ErrorMessage}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Gets an initialized SQLite connection to the database. 
        /// The connection is maintained for the lifetime of the service for efficient connection pooling.
        /// </summary>
        /// <returns>A task that returns an initialized SQLite connection.</returns>
        public async Task<SQLiteAsyncConnection> GetConnectionAsync()
        {
            if (_connection == null)
            {
                _logger.LogDebug("Creating new database connection");
                _connection = await _databaseInitializer.GetConnectionAsync();
            }

            return _connection;
        }

        /// <summary>
        /// Executes a custom SQL query and returns the results.
        /// </summary>
        /// <typeparam name="T">The type of entity to map the results to.</typeparam>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="parameters">The parameters to use in the query.</param>
        /// <returns>A task that returns the query results.</returns>
        public async Task<List<T>> ExecuteQueryAsync<T>(string query, params object[] parameters)
        {
            _logger.LogDebug("Executing query: {Query} with {ParameterCount} parameters", query, parameters?.Length ?? 0);
            
            try
            {
                var connection = await GetConnectionAsync();
                var results = await connection.QueryAsync<T>(query, parameters);
                _logger.LogDebug("Query completed, returned {ResultCount} results", results.Count);
                return results;
            }
            catch (SQLiteException ex)
            {
                _logger.LogError(ex, "Error executing query: {Query}. Error: {ErrorMessage}", query, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Executes a custom SQL command that doesn't return results.
        /// </summary>
        /// <param name="query">The SQL command to execute.</param>
        /// <param name="parameters">The parameters to use in the command.</param>
        /// <returns>A task that returns the number of rows affected.</returns>
        public async Task<int> ExecuteNonQueryAsync(string query, params object[] parameters)
        {
            _logger.LogDebug("Executing non-query: {Query} with {ParameterCount} parameters", query, parameters?.Length ?? 0);
            
            try
            {
                var connection = await GetConnectionAsync();
                var result = await connection.ExecuteAsync(query, parameters);
                _logger.LogDebug("Non-query completed, affected {RowCount} rows", result);
                return result;
            }
            catch (SQLiteException ex)
            {
                _logger.LogError(ex, "Error executing non-query: {Query}. Error: {ErrorMessage}", query, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Begins a new database transaction.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task BeginTransactionAsync()
        {
            lock (_transactionLock)
            {
                if (_isInTransaction)
                {
                    throw new InvalidOperationException("A transaction is already in progress");
                }
            }

            _logger.LogDebug("Beginning database transaction");
            
            try
            {
                var connection = await GetConnectionAsync();
                await connection.BeginTransactionAsync();
                
                lock (_transactionLock)
                {
                    _isInTransaction = true;
                }
                
                _logger.LogDebug("Transaction started");
            }
            catch (SQLiteException ex)
            {
                _logger.LogError(ex, "Error beginning transaction: {ErrorMessage}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Commits the current database transaction.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task CommitTransactionAsync()
        {
            lock (_transactionLock)
            {
                if (!_isInTransaction)
                {
                    throw new InvalidOperationException("No active transaction to commit");
                }
            }

            _logger.LogDebug("Committing database transaction");
            
            try
            {
                var connection = await GetConnectionAsync();
                await connection.CommitAsync();
                
                lock (_transactionLock)
                {
                    _isInTransaction = false;
                }
                
                _logger.LogDebug("Transaction committed");
            }
            catch (SQLiteException ex)
            {
                _logger.LogError(ex, "Error committing transaction: {ErrorMessage}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Rolls back the current database transaction.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task RollbackTransactionAsync()
        {
            lock (_transactionLock)
            {
                if (!_isInTransaction)
                {
                    throw new InvalidOperationException("No active transaction to roll back");
                }
            }

            _logger.LogDebug("Rolling back database transaction");
            
            try
            {
                var connection = await GetConnectionAsync();
                await connection.RollbackAsync();
                
                lock (_transactionLock)
                {
                    _isInTransaction = false;
                }
                
                _logger.LogDebug("Transaction rolled back");
            }
            catch (SQLiteException ex)
            {
                _logger.LogError(ex, "Error rolling back transaction: {ErrorMessage}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Runs the specified action within a transaction. If the action completes successfully,
        /// the transaction is committed; otherwise, it is rolled back.
        /// </summary>
        /// <param name="action">The action to execute within the transaction.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task RunInTransactionAsync(Func<Task> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            _logger.LogDebug("Running action in transaction");
            
            await BeginTransactionAsync();
            
            try
            {
                await action();
                await CommitTransactionAsync();
                _logger.LogDebug("Transaction operation completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in transaction operation: {ErrorMessage}", ex.Message);
                await RollbackTransactionAsync();
                throw;
            }
        }

        /// <summary>
        /// Checks the integrity of the database and attempts to repair if issues are found.
        /// Uses SQLite's PRAGMA integrity_check command to identify corruption.
        /// </summary>
        /// <returns>A task that returns true if the database is intact or was successfully repaired, false otherwise.</returns>
        public async Task<bool> CheckDatabaseIntegrityAsync()
        {
            _logger.LogInformation("Checking database integrity");
            
            try
            {
                var connection = await GetConnectionAsync();
                
                // Run integrity check
                var results = await ExecuteQueryAsync<string>("PRAGMA integrity_check");
                
                // If result is single 'ok', database is intact
                if (results.Count == 1 && results[0].ToLowerInvariant() == "ok")
                {
                    _logger.LogInformation("Database integrity check passed");
                    return true;
                }
                
                // Log integrity issues
                _logger.LogWarning("Database integrity check failed with {IssueCount} issues", results.Count);
                foreach (var issue in results)
                {
                    _logger.LogWarning("Database integrity issue: {Issue}", issue);
                }
                
                // Attempt repair
                _logger.LogInformation("Attempting to repair database");
                await ExecuteNonQueryAsync("PRAGMA integrity_check(1)");
                
                // Check if repair was successful
                var verifyResults = await ExecuteQueryAsync<string>("PRAGMA integrity_check");
                bool repaired = verifyResults.Count == 1 && verifyResults[0].ToLowerInvariant() == "ok";
                
                if (repaired)
                {
                    _logger.LogInformation("Database repair successful");
                }
                else
                {
                    _logger.LogError("Database repair failed");
                }
                
                return repaired;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking database integrity: {ErrorMessage}", ex.Message);
                return false;
            }
        }
    }
}