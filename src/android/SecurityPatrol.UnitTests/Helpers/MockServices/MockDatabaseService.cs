using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SQLite;
using SecurityPatrol.Services;

namespace SecurityPatrol.UnitTests.Helpers.MockServices
{
    /// <summary>
    /// Mock implementation of IDatabaseService for unit testing that provides configurable responses for database operations
    /// without accessing an actual SQLite database, allowing tests to run in isolation and with predictable results.
    /// </summary>
    public class MockDatabaseService : IDatabaseService
    {
        // State tracking properties
        public bool IsInitialized { get; private set; }
        public SQLiteAsyncConnection Connection { get; private set; }
        public bool IsInTransaction { get; private set; }
        
        // Dictionaries to store configured results and exceptions
        public Dictionary<string, List<object>> QueryResults { get; private set; }
        public Dictionary<string, int> NonQueryResults { get; private set; }
        public Dictionary<string, Exception> QueryExceptions { get; private set; }
        public Dictionary<string, Exception> NonQueryExceptions { get; private set; }
        
        // Lists to track executed queries
        public List<string> ExecutedQueries { get; private set; }
        public List<string> ExecutedNonQueries { get; private set; }
        
        // Integrity check configuration
        public bool IntegrityCheckResult { get; private set; }
        public Exception IntegrityCheckException { get; private set; }
        
        // Transaction counters
        public int TransactionBeginCount { get; private set; }
        public int TransactionCommitCount { get; private set; }
        public int TransactionRollbackCount { get; private set; }
        
        /// <summary>
        /// Initializes a new instance of the MockDatabaseService class with default settings
        /// </summary>
        public MockDatabaseService()
        {
            IsInitialized = false;
            Connection = null;
            IsInTransaction = false;
            
            QueryResults = new Dictionary<string, List<object>>();
            NonQueryResults = new Dictionary<string, int>();
            QueryExceptions = new Dictionary<string, Exception>();
            NonQueryExceptions = new Dictionary<string, Exception>();
            
            ExecutedQueries = new List<string>();
            ExecutedNonQueries = new List<string>();
            
            IntegrityCheckResult = true;
            IntegrityCheckException = null;
            
            TransactionBeginCount = 0;
            TransactionCommitCount = 0;
            TransactionRollbackCount = 0;
        }
        
        /// <summary>
        /// Mocks the database initialization process
        /// </summary>
        /// <returns>A completed task</returns>
        public async Task InitializeAsync()
        {
            IsInitialized = true;
            return await Task.CompletedTask;
        }
        
        /// <summary>
        /// Mocks retrieving a SQLite connection
        /// </summary>
        /// <returns>A task that returns the mock SQLite connection</returns>
        public async Task<SQLiteAsyncConnection> GetConnectionAsync()
        {
            if (Connection == null)
            {
                // Create a mock connection if needed
                // Note: This won't be a real connection, but for testing it's sufficient
                Connection = new SQLiteAsyncConnection(":memory:");
            }
            return await Task.FromResult(Connection);
        }
        
        /// <summary>
        /// Mocks executing a SQL query that returns results
        /// </summary>
        /// <typeparam name="T">The type of entity to map the results to</typeparam>
        /// <param name="query">The SQL query to execute</param>
        /// <param name="parameters">The parameters to use in the query</param>
        /// <returns>A task that returns the configured mock results for the query</returns>
        public async Task<List<T>> ExecuteQueryAsync<T>(string query, params object[] parameters)
        {
            // Track the executed query
            ExecutedQueries.Add(query);
            
            // Check if an exception is configured for this query
            if (QueryExceptions.TryGetValue(query, out Exception exception))
            {
                throw exception;
            }
            
            // Check if results are configured for this query
            if (QueryResults.TryGetValue(query, out List<object> results))
            {
                // Try to cast the results to the requested type
                try
                {
                    return await Task.FromResult(results.Cast<T>().ToList());
                }
                catch (InvalidCastException)
                {
                    // If casting fails, return an empty list
                    return await Task.FromResult(new List<T>());
                }
            }
            
            // Default behavior: return an empty list
            return await Task.FromResult(new List<T>());
        }
        
        /// <summary>
        /// Mocks executing a SQL command that doesn't return results
        /// </summary>
        /// <param name="query">The SQL command to execute</param>
        /// <param name="parameters">The parameters to use in the command</param>
        /// <returns>A task that returns the configured number of affected rows</returns>
        public async Task<int> ExecuteNonQueryAsync(string query, params object[] parameters)
        {
            // Track the executed non-query
            ExecutedNonQueries.Add(query);
            
            // Check if an exception is configured for this query
            if (NonQueryExceptions.TryGetValue(query, out Exception exception))
            {
                throw exception;
            }
            
            // Check if a result is configured for this query
            if (NonQueryResults.TryGetValue(query, out int result))
            {
                return await Task.FromResult(result);
            }
            
            // Default behavior: return 0 affected rows
            return await Task.FromResult(0);
        }
        
        /// <summary>
        /// Mocks beginning a database transaction
        /// </summary>
        /// <returns>A completed task</returns>
        public async Task BeginTransactionAsync()
        {
            // If already in a transaction, throw an exception
            if (IsInTransaction)
            {
                throw new InvalidOperationException("Transaction already in progress");
            }
            
            IsInTransaction = true;
            TransactionBeginCount++;
            
            return await Task.CompletedTask;
        }
        
        /// <summary>
        /// Mocks committing a database transaction
        /// </summary>
        /// <returns>A completed task</returns>
        public async Task CommitTransactionAsync()
        {
            // If not in a transaction, throw an exception
            if (!IsInTransaction)
            {
                throw new InvalidOperationException("No transaction in progress");
            }
            
            IsInTransaction = false;
            TransactionCommitCount++;
            
            return await Task.CompletedTask;
        }
        
        /// <summary>
        /// Mocks rolling back a database transaction
        /// </summary>
        /// <returns>A completed task</returns>
        public async Task RollbackTransactionAsync()
        {
            // If not in a transaction, throw an exception
            if (!IsInTransaction)
            {
                throw new InvalidOperationException("No transaction in progress");
            }
            
            IsInTransaction = false;
            TransactionRollbackCount++;
            
            return await Task.CompletedTask;
        }
        
        /// <summary>
        /// Mocks running an action within a transaction
        /// </summary>
        /// <param name="action">The action to execute within the transaction</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task RunInTransactionAsync(Func<Task> action)
        {
            await BeginTransactionAsync();
            
            try
            {
                await action();
                await CommitTransactionAsync();
            }
            catch (Exception)
            {
                await RollbackTransactionAsync();
                throw;
            }
        }
        
        /// <summary>
        /// Mocks checking the integrity of the database
        /// </summary>
        /// <returns>A task that returns the configured integrity check result</returns>
        public async Task<bool> CheckDatabaseIntegrityAsync()
        {
            if (IntegrityCheckException != null)
            {
                throw IntegrityCheckException;
            }
            
            return await Task.FromResult(IntegrityCheckResult);
        }
        
        /// <summary>
        /// Configures a result for a specific query
        /// </summary>
        /// <typeparam name="T">The type of entities in the result list</typeparam>
        /// <param name="query">The SQL query to configure</param>
        /// <param name="result">The result to return for the query</param>
        public void SetupQueryResult<T>(string query, List<T> result)
        {
            // Store the result as a list of objects
            QueryResults[query] = result.Cast<object>().ToList();
            
            // Remove any exception configured for this query
            if (QueryExceptions.ContainsKey(query))
            {
                QueryExceptions.Remove(query);
            }
        }
        
        /// <summary>
        /// Configures a result for a specific non-query command
        /// </summary>
        /// <param name="query">The SQL command to configure</param>
        /// <param name="affectedRows">The number of affected rows to return</param>
        public void SetupNonQueryResult(string query, int affectedRows)
        {
            NonQueryResults[query] = affectedRows;
            
            // Remove any exception configured for this query
            if (NonQueryExceptions.ContainsKey(query))
            {
                NonQueryExceptions.Remove(query);
            }
        }
        
        /// <summary>
        /// Configures an exception to be thrown for a specific query
        /// </summary>
        /// <param name="query">The SQL query to configure</param>
        /// <param name="exception">The exception to throw</param>
        public void SetupQueryException(string query, Exception exception)
        {
            QueryExceptions[query] = exception;
            
            // Remove any result configured for this query
            if (QueryResults.ContainsKey(query))
            {
                QueryResults.Remove(query);
            }
        }
        
        /// <summary>
        /// Configures an exception to be thrown for a specific non-query command
        /// </summary>
        /// <param name="query">The SQL command to configure</param>
        /// <param name="exception">The exception to throw</param>
        public void SetupNonQueryException(string query, Exception exception)
        {
            NonQueryExceptions[query] = exception;
            
            // Remove any result configured for this query
            if (NonQueryResults.ContainsKey(query))
            {
                NonQueryResults.Remove(query);
            }
        }
        
        /// <summary>
        /// Configures the result for database integrity checks
        /// </summary>
        /// <param name="result">The result to return from integrity checks</param>
        public void SetupIntegrityCheckResult(bool result)
        {
            IntegrityCheckResult = result;
            IntegrityCheckException = null;
        }
        
        /// <summary>
        /// Configures an exception to be thrown during integrity checks
        /// </summary>
        /// <param name="exception">The exception to throw</param>
        public void SetupIntegrityCheckException(Exception exception)
        {
            IntegrityCheckException = exception;
        }
        
        /// <summary>
        /// Verifies that a specific query was executed
        /// </summary>
        /// <param name="query">The SQL query to verify</param>
        /// <returns>True if the query was executed, otherwise false</returns>
        public bool VerifyQueryExecuted(string query)
        {
            return ExecutedQueries.Contains(query);
        }
        
        /// <summary>
        /// Verifies that a specific non-query command was executed
        /// </summary>
        /// <param name="query">The SQL command to verify</param>
        /// <returns>True if the command was executed, otherwise false</returns>
        public bool VerifyNonQueryExecuted(string query)
        {
            return ExecutedNonQueries.Contains(query);
        }
        
        /// <summary>
        /// Resets all configurations and execution history
        /// </summary>
        public void Reset()
        {
            IsInitialized = false;
            Connection = null;
            IsInTransaction = false;
            
            QueryResults.Clear();
            NonQueryResults.Clear();
            QueryExceptions.Clear();
            NonQueryExceptions.Clear();
            
            ExecutedQueries.Clear();
            ExecutedNonQueries.Clear();
            
            IntegrityCheckResult = true;
            IntegrityCheckException = null;
            
            TransactionBeginCount = 0;
            TransactionCommitCount = 0;
            TransactionRollbackCount = 0;
        }
    }
}