using SecurityPatrol.Constants;
using SecurityPatrol.Database.Migrations;
using SQLite;
using System.Threading.Tasks;

namespace SecurityPatrol.Database.Migrations
{
    /// <summary>
    /// Implements database migration version 1.1 that adds SyncProgress column to the Photo table
    /// and RemoteId columns to all entity tables.
    /// </summary>
    public class Migration_1_1 : IMigration
    {
        /// <summary>
        /// Gets the version number of this migration.
        /// </summary>
        public double Version => 1.1;

        /// <summary>
        /// Initializes a new instance of the Migration_1_1 class.
        /// </summary>
        public Migration_1_1()
        {
            // Version is set via property initializer
        }

        /// <summary>
        /// Applies the migration by adding SyncProgress column to the Photo table
        /// and RemoteId columns to all entity tables.
        /// </summary>
        /// <param name="connection">The SQLite database connection to use for the migration</param>
        /// <returns>A task representing the asynchronous migration operation</returns>
        public async Task ApplyAsync(SQLiteAsyncConnection connection)
        {
            // Add SyncProgress column to Photo table
            await connection.ExecuteAsync(DatabaseConstants.AddColumnSyncProgress);
            
            // Add RemoteId columns to all entity tables
            await connection.ExecuteAsync(string.Format(DatabaseConstants.AddColumnRemoteId, DatabaseConstants.TableUser));
            await connection.ExecuteAsync(string.Format(DatabaseConstants.AddColumnRemoteId, DatabaseConstants.TableTimeRecord));
            await connection.ExecuteAsync(string.Format(DatabaseConstants.AddColumnRemoteId, DatabaseConstants.TableLocationRecord));
            await connection.ExecuteAsync(string.Format(DatabaseConstants.AddColumnRemoteId, DatabaseConstants.TablePhoto));
            await connection.ExecuteAsync(string.Format(DatabaseConstants.AddColumnRemoteId, DatabaseConstants.TableActivityReport));
            await connection.ExecuteAsync(string.Format(DatabaseConstants.AddColumnRemoteId, DatabaseConstants.TablePatrolLocation));
            await connection.ExecuteAsync(string.Format(DatabaseConstants.AddColumnRemoteId, DatabaseConstants.TableCheckpoint));
            await connection.ExecuteAsync(string.Format(DatabaseConstants.AddColumnRemoteId, DatabaseConstants.TableCheckpointVerification));
        }
    }
}