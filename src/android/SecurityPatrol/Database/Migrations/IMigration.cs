using SQLite;
using System.Threading.Tasks;

namespace SecurityPatrol.Database.Migrations
{
    /// <summary>
    /// Interface that defines the contract for database migrations in the Security Patrol application.
    /// Each implementation represents a specific database schema version migration.
    /// </summary>
    public interface IMigration
    {
        /// <summary>
        /// Gets the version number of this migration.
        /// Versions should be sequential and incremental (e.g., 1.0, 1.1, 1.2).
        /// </summary>
        double Version { get; }

        /// <summary>
        /// Applies the migration to the database, updating the schema as needed.
        /// </summary>
        /// <param name="connection">The SQLite database connection to use for the migration</param>
        /// <returns>A task representing the asynchronous migration operation</returns>
        Task ApplyAsync(SQLiteAsyncConnection connection);
    }
}