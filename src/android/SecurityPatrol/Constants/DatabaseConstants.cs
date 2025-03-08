using System;

namespace SecurityPatrol.Constants
{
    /// <summary>
    /// Static class containing constants for database operations including table names, 
    /// column names, and SQL statements for creating tables and indexes.
    /// </summary>
    public static class DatabaseConstants
    {
        // Database information
        public const string DatabaseName = "securitypatrol.db3";
        public const string DatabaseVersion = "1.3";
        public const string ConnectionString = "Filename=" + DatabaseName;

        // Table names
        public const string TableUser = "User";
        public const string TableTimeRecord = "TimeRecord";
        public const string TableLocationRecord = "LocationRecord";
        public const string TablePhoto = "Photo";
        public const string TableActivityReport = "ActivityReport";
        public const string TablePatrolLocation = "PatrolLocation";
        public const string TableCheckpoint = "Checkpoint";
        public const string TableCheckpointVerification = "CheckpointVerification";
        public const string TableSyncQueue = "SyncQueue";

        // Common column names
        public const string ColumnId = "Id";
        public const string ColumnUserId = "UserId";
        public const string ColumnTimestamp = "Timestamp";
        public const string ColumnLatitude = "Latitude";
        public const string ColumnLongitude = "Longitude";
        public const string ColumnIsSynced = "IsSynced";
        public const string ColumnRemoteId = "RemoteId";

        // User table columns
        public const string ColumnPhoneNumber = "PhoneNumber";
        public const string ColumnLastAuthenticated = "LastAuthenticated";
        public const string ColumnAuthToken = "AuthToken";
        public const string ColumnTokenExpiry = "TokenExpiry";

        // TimeRecord table columns
        public const string ColumnType = "Type";

        // LocationRecord table columns
        public const string ColumnAccuracy = "Accuracy";

        // Photo table columns
        public const string ColumnFilePath = "FilePath";
        public const string ColumnSyncProgress = "SyncProgress";

        // ActivityReport table columns
        public const string ColumnText = "Text";

        // PatrolLocation and Checkpoint table columns
        public const string ColumnName = "Name";
        public const string ColumnLocationId = "LocationId";
        public const string ColumnLastUpdated = "LastUpdated";

        // CheckpointVerification table columns
        public const string ColumnCheckpointId = "CheckpointId";

        // SyncQueue table columns
        public const string ColumnEntityType = "EntityType";
        public const string ColumnEntityId = "EntityId";
        public const string ColumnPriority = "Priority";
        public const string ColumnRetryCount = "RetryCount";
        public const string ColumnLastAttempt = "LastAttempt";
        public const string ColumnErrorMessage = "ErrorMessage";

        // SQL statements for creating tables
        public const string CreateTableUser = @"
            CREATE TABLE IF NOT EXISTS " + TableUser + @" (
                " + ColumnId + @" TEXT PRIMARY KEY,
                " + ColumnPhoneNumber + @" TEXT NOT NULL,
                " + ColumnLastAuthenticated + @" TEXT NOT NULL,
                " + ColumnAuthToken + @" TEXT,
                " + ColumnTokenExpiry + @" TEXT
            );";

        public const string CreateTableTimeRecord = @"
            CREATE TABLE IF NOT EXISTS " + TableTimeRecord + @" (
                " + ColumnId + @" INTEGER PRIMARY KEY AUTOINCREMENT,
                " + ColumnUserId + @" TEXT NOT NULL,
                " + ColumnType + @" TEXT NOT NULL,
                " + ColumnTimestamp + @" TEXT NOT NULL,
                " + ColumnLatitude + @" REAL NOT NULL,
                " + ColumnLongitude + @" REAL NOT NULL,
                " + ColumnIsSynced + @" INTEGER DEFAULT 0,
                " + ColumnRemoteId + @" TEXT
            );";

        public const string CreateTableLocationRecord = @"
            CREATE TABLE IF NOT EXISTS " + TableLocationRecord + @" (
                " + ColumnId + @" INTEGER PRIMARY KEY AUTOINCREMENT,
                " + ColumnUserId + @" TEXT NOT NULL,
                " + ColumnTimestamp + @" TEXT NOT NULL,
                " + ColumnLatitude + @" REAL NOT NULL,
                " + ColumnLongitude + @" REAL NOT NULL,
                " + ColumnAccuracy + @" REAL NOT NULL,
                " + ColumnIsSynced + @" INTEGER DEFAULT 0,
                " + ColumnRemoteId + @" TEXT
            );";

        public const string CreateTablePhoto = @"
            CREATE TABLE IF NOT EXISTS " + TablePhoto + @" (
                " + ColumnId + @" TEXT PRIMARY KEY,
                " + ColumnUserId + @" TEXT NOT NULL,
                " + ColumnTimestamp + @" TEXT NOT NULL,
                " + ColumnLatitude + @" REAL NOT NULL,
                " + ColumnLongitude + @" REAL NOT NULL,
                " + ColumnFilePath + @" TEXT NOT NULL,
                " + ColumnIsSynced + @" INTEGER DEFAULT 0,
                " + ColumnRemoteId + @" TEXT,
                " + ColumnSyncProgress + @" INTEGER DEFAULT 0
            );";

        public const string CreateTableActivityReport = @"
            CREATE TABLE IF NOT EXISTS " + TableActivityReport + @" (
                " + ColumnId + @" INTEGER PRIMARY KEY AUTOINCREMENT,
                " + ColumnUserId + @" TEXT NOT NULL,
                " + ColumnText + @" TEXT NOT NULL,
                " + ColumnTimestamp + @" TEXT NOT NULL,
                " + ColumnLatitude + @" REAL NOT NULL,
                " + ColumnLongitude + @" REAL NOT NULL,
                " + ColumnIsSynced + @" INTEGER DEFAULT 0,
                " + ColumnRemoteId + @" TEXT
            );";

        public const string CreateTablePatrolLocation = @"
            CREATE TABLE IF NOT EXISTS " + TablePatrolLocation + @" (
                " + ColumnId + @" INTEGER PRIMARY KEY AUTOINCREMENT,
                " + ColumnName + @" TEXT NOT NULL,
                " + ColumnLatitude + @" REAL NOT NULL,
                " + ColumnLongitude + @" REAL NOT NULL,
                " + ColumnLastUpdated + @" TEXT NOT NULL,
                " + ColumnRemoteId + @" TEXT
            );";

        public const string CreateTableCheckpoint = @"
            CREATE TABLE IF NOT EXISTS " + TableCheckpoint + @" (
                " + ColumnId + @" INTEGER PRIMARY KEY AUTOINCREMENT,
                " + ColumnLocationId + @" INTEGER NOT NULL,
                " + ColumnName + @" TEXT NOT NULL,
                " + ColumnLatitude + @" REAL NOT NULL,
                " + ColumnLongitude + @" REAL NOT NULL,
                " + ColumnLastUpdated + @" TEXT NOT NULL,
                " + ColumnRemoteId + @" TEXT,
                FOREIGN KEY(" + ColumnLocationId + @") REFERENCES " + TablePatrolLocation + @"(" + ColumnId + @")
            );";

        public const string CreateTableCheckpointVerification = @"
            CREATE TABLE IF NOT EXISTS " + TableCheckpointVerification + @" (
                " + ColumnId + @" INTEGER PRIMARY KEY AUTOINCREMENT,
                " + ColumnUserId + @" TEXT NOT NULL,
                " + ColumnCheckpointId + @" INTEGER NOT NULL,
                " + ColumnTimestamp + @" TEXT NOT NULL,
                " + ColumnLatitude + @" REAL NOT NULL,
                " + ColumnLongitude + @" REAL NOT NULL,
                " + ColumnIsSynced + @" INTEGER DEFAULT 0,
                " + ColumnRemoteId + @" TEXT,
                FOREIGN KEY(" + ColumnCheckpointId + @") REFERENCES " + TableCheckpoint + @"(" + ColumnId + @")
            );";

        public const string CreateTableSyncQueue = @"
            CREATE TABLE IF NOT EXISTS " + TableSyncQueue + @" (
                " + ColumnId + @" INTEGER PRIMARY KEY AUTOINCREMENT,
                " + ColumnEntityType + @" TEXT NOT NULL,
                " + ColumnEntityId + @" TEXT NOT NULL,
                " + ColumnPriority + @" INTEGER DEFAULT 0,
                " + ColumnRetryCount + @" INTEGER DEFAULT 0,
                " + ColumnLastAttempt + @" TEXT,
                " + ColumnErrorMessage + @" TEXT
            );";

        // SQL statements for creating indexes
        public const string CreateIndexTimeRecordUserId = @"
            CREATE INDEX IF NOT EXISTS IX_TimeRecord_UserId ON " + TableTimeRecord + @"(" + ColumnUserId + @");";

        public const string CreateIndexTimeRecordIsSynced = @"
            CREATE INDEX IF NOT EXISTS IX_TimeRecord_IsSynced ON " + TableTimeRecord + @"(" + ColumnIsSynced + @");";

        public const string CreateIndexLocationRecordUserIdTimestamp = @"
            CREATE INDEX IF NOT EXISTS IX_LocationRecord_UserId_Timestamp ON " + TableLocationRecord + @"(" + ColumnUserId + @", " + ColumnTimestamp + @");";

        public const string CreateIndexLocationRecordIsSynced = @"
            CREATE INDEX IF NOT EXISTS IX_LocationRecord_IsSynced ON " + TableLocationRecord + @"(" + ColumnIsSynced + @");";

        public const string CreateIndexPhotoUserId = @"
            CREATE INDEX IF NOT EXISTS IX_Photo_UserId ON " + TablePhoto + @"(" + ColumnUserId + @");";

        public const string CreateIndexPhotoIsSynced = @"
            CREATE INDEX IF NOT EXISTS IX_Photo_IsSynced ON " + TablePhoto + @"(" + ColumnIsSynced + @");";

        public const string CreateIndexActivityReportUserId = @"
            CREATE INDEX IF NOT EXISTS IX_ActivityReport_UserId ON " + TableActivityReport + @"(" + ColumnUserId + @");";

        public const string CreateIndexActivityReportIsSynced = @"
            CREATE INDEX IF NOT EXISTS IX_ActivityReport_IsSynced ON " + TableActivityReport + @"(" + ColumnIsSynced + @");";

        public const string CreateIndexCheckpointLocationId = @"
            CREATE INDEX IF NOT EXISTS IX_Checkpoint_LocationId ON " + TableCheckpoint + @"(" + ColumnLocationId + @");";

        public const string CreateIndexCheckpointVerificationCheckpointId = @"
            CREATE INDEX IF NOT EXISTS IX_CheckpointVerification_CheckpointId ON " + TableCheckpointVerification + @"(" + ColumnCheckpointId + @");";

        public const string CreateIndexCheckpointVerificationIsSynced = @"
            CREATE INDEX IF NOT EXISTS IX_CheckpointVerification_IsSynced ON " + TableCheckpointVerification + @"(" + ColumnIsSynced + @");";

        public const string CreateIndexSyncQueueEntityTypeEntityId = @"
            CREATE INDEX IF NOT EXISTS IX_SyncQueue_EntityType_EntityId ON " + TableSyncQueue + @"(" + ColumnEntityType + @", " + ColumnEntityId + @");";

        public const string CreateIndexSyncQueuePriorityLastAttempt = @"
            CREATE INDEX IF NOT EXISTS IX_SyncQueue_Priority_LastAttempt ON " + TableSyncQueue + @"(" + ColumnPriority + @", " + ColumnLastAttempt + @");";

        // SQL statements for schema migrations
        public const string AddColumnSyncProgress = @"
            ALTER TABLE " + TablePhoto + @" ADD COLUMN " + ColumnSyncProgress + @" INTEGER DEFAULT 0;";

        public const string AddColumnRemoteId = @"
            ALTER TABLE {0} ADD COLUMN " + ColumnRemoteId + @" TEXT;";

        // Private constructor to prevent instantiation
        private DatabaseConstants()
        {
        }
    }
}