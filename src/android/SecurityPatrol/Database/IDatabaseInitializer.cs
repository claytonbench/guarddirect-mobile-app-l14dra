using System.Threading.Tasks;
using SQLite; // SQLite-net-pcl v1.8+

namespace SecurityPatrol.Database
{
    /// <summary>
    /// Interface that defines the contract for database initialization, migration, 
    /// and connection management in the Security Patrol application.
    /// </summary>
    public interface IDatabaseInitializer
    {
        /// <summary>
        /// Initializes the database by creating tables and indexes if they don't exist 
        /// and applying any pending migrations.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        Task InitializeAsync();

        /// <summary>
        /// Gets an initialized SQLite connection to the database.
        /// </summary>
        /// <returns>A task that returns an initialized SQLite connection</returns>
        Task<SQLiteAsyncConnection> GetConnectionAsync();

        /// <summary>
        /// Resets the database by dropping all tables and recreating them.
        /// Used primarily for testing or when a clean state is required.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        Task ResetDatabaseAsync();

        /// <summary>
        /// Applies any pending database migrations to update the schema to the latest version.
        /// </summary>
        /// <param name="connection">The SQLite connection to use for applying migrations</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task ApplyMigrationsAsync(SQLiteAsyncConnection connection);

        /// <summary>
        /// Gets the current version of the database schema.
        /// </summary>
        /// <returns>A task that returns the current database version</returns>
        Task<double> GetDatabaseVersionAsync();
    }
}