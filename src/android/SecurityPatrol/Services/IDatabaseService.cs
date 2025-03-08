using System.Threading.Tasks;
using SQLite;

namespace SecurityPatrol.Services
{
    /// <summary>
    /// Interface that defines the contract for database operations in the Security Patrol application.
    /// Provides methods for database initialization, connection management, query execution, and transaction handling.
    /// </summary>
    public interface IDatabaseService
    {
        /// <summary>
        /// Initializes the database by ensuring the database file exists and the schema is up-to-date.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task InitializeAsync();

        /// <summary>
        /// Gets an initialized SQLite connection to the database.
        /// </summary>
        /// <returns>A task that returns an initialized SQLite connection.</returns>
        Task<SQLiteAsyncConnection> GetConnectionAsync();

        /// <summary>
        /// Executes a custom SQL query and returns the results.
        /// </summary>
        /// <typeparam name="T">The type of entity to map the results to.</typeparam>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="parameters">The parameters to use in the query.</param>
        /// <returns>A task that returns the query results.</returns>
        Task<List<T>> ExecuteQueryAsync<T>(string query, params object[] parameters);

        /// <summary>
        /// Executes a custom SQL command that doesn't return results.
        /// </summary>
        /// <param name="query">The SQL command to execute.</param>
        /// <param name="parameters">The parameters to use in the command.</param>
        /// <returns>A task that returns the number of rows affected.</returns>
        Task<int> ExecuteNonQueryAsync(string query, params object[] parameters);

        /// <summary>
        /// Begins a new database transaction.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task BeginTransactionAsync();

        /// <summary>
        /// Commits the current database transaction.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task CommitTransactionAsync();

        /// <summary>
        /// Rolls back the current database transaction.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RollbackTransactionAsync();

        /// <summary>
        /// Runs the specified action within a transaction.
        /// </summary>
        /// <param name="action">The action to execute within the transaction.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RunInTransactionAsync(Func<Task> action);

        /// <summary>
        /// Checks the integrity of the database and attempts to repair if issues are found.
        /// </summary>
        /// <returns>A task that returns true if the database is intact or was successfully repaired, false otherwise.</returns>
        Task<bool> CheckDatabaseIntegrityAsync();
    }
}