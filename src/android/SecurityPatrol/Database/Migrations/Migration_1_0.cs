using SecurityPatrol.Constants;
using SecurityPatrol.Database.Migrations;
using SQLite;
using System.Threading.Tasks;

namespace SecurityPatrol.Database.Migrations
{
    /// <summary>
    /// Implements the initial database migration (version 1.0) that creates all required tables and indexes
    /// for the Security Patrol application.
    /// </summary>
    public class Migration_1_0 : IMigration
    {
        /// <summary>
        /// Gets the version number of this migration.
        /// </summary>
        public double Version => 1.0;

        /// <summary>
        /// Initializes a new instance of the Migration_1_0 class.
        /// </summary>
        public Migration_1_0()
        {
            // Version set via property initializer
        }

        /// <summary>
        /// Applies the migration by creating all required tables and indexes in the database.
        /// </summary>
        /// <param name="connection">The SQLite database connection to use for the migration</param>
        /// <returns>A task representing the asynchronous migration operation</returns>
        public async Task ApplyAsync(SQLiteAsyncConnection connection)
        {
            // Create all tables
            await connection.ExecuteAsync(DatabaseConstants.CreateTableUser);
            await connection.ExecuteAsync(DatabaseConstants.CreateTableTimeRecord);
            await connection.ExecuteAsync(DatabaseConstants.CreateTableLocationRecord);
            await connection.ExecuteAsync(DatabaseConstants.CreateTablePhoto);
            await connection.ExecuteAsync(DatabaseConstants.CreateTableActivityReport);
            await connection.ExecuteAsync(DatabaseConstants.CreateTablePatrolLocation);
            await connection.ExecuteAsync(DatabaseConstants.CreateTableCheckpoint);
            await connection.ExecuteAsync(DatabaseConstants.CreateTableCheckpointVerification);
            await connection.ExecuteAsync(DatabaseConstants.CreateTableSyncQueue);

            // Create all indexes
            await connection.ExecuteAsync(DatabaseConstants.CreateIndexTimeRecordUserId);
            await connection.ExecuteAsync(DatabaseConstants.CreateIndexTimeRecordIsSynced);
            await connection.ExecuteAsync(DatabaseConstants.CreateIndexLocationRecordUserIdTimestamp);
            await connection.ExecuteAsync(DatabaseConstants.CreateIndexLocationRecordIsSynced);
            await connection.ExecuteAsync(DatabaseConstants.CreateIndexPhotoUserId);
            await connection.ExecuteAsync(DatabaseConstants.CreateIndexPhotoIsSynced);
            await connection.ExecuteAsync(DatabaseConstants.CreateIndexActivityReportUserId);
            await connection.ExecuteAsync(DatabaseConstants.CreateIndexActivityReportIsSynced);
            await connection.ExecuteAsync(DatabaseConstants.CreateIndexCheckpointLocationId);
            await connection.ExecuteAsync(DatabaseConstants.CreateIndexCheckpointVerificationCheckpointId);
            await connection.ExecuteAsync(DatabaseConstants.CreateIndexCheckpointVerificationIsSynced);
            await connection.ExecuteAsync(DatabaseConstants.CreateIndexSyncQueueEntityTypeEntityId);
            await connection.ExecuteAsync(DatabaseConstants.CreateIndexSyncQueuePriorityLastAttempt);
        }
    }
}