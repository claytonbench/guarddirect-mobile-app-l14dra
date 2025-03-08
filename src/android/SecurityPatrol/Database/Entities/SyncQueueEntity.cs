using SQLite;
using System;
using SecurityPatrol.Constants;

namespace SecurityPatrol.Database.Entities
{
    /// <summary>
    /// Represents an item in the synchronization queue stored in the local SQLite database.
    /// Contains properties for entity identification, priority, retry tracking, and error information.
    /// </summary>
    [Table(DatabaseConstants.TableSyncQueue)]
    public class SyncQueueEntity
    {
        /// <summary>
        /// The unique identifier for the sync queue item.
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>
        /// The type of entity being synchronized (e.g., "TimeRecord", "Photo").
        /// </summary>
        [Indexed(Name = "IX_SyncQueue_EntityType_EntityId", Order = 1)]
        public string EntityType { get; set; }

        /// <summary>
        /// The unique identifier of the entity being synchronized.
        /// </summary>
        [Indexed(Name = "IX_SyncQueue_EntityType_EntityId", Order = 2)]
        public string EntityId { get; set; }

        /// <summary>
        /// The priority of the sync item (higher values indicate higher priority).
        /// Used for ordering synchronization operations.
        /// </summary>
        [Indexed(Name = "IX_SyncQueue_Priority_LastAttempt", Order = 1)]
        public int Priority { get; set; }

        /// <summary>
        /// The number of times synchronization has been attempted for this item.
        /// Used for implementing retry policies with exponential backoff.
        /// </summary>
        public int RetryCount { get; set; }

        /// <summary>
        /// The timestamp of the last synchronization attempt.
        /// Used for scheduling retries and implementing backoff strategies.
        /// </summary>
        [Indexed(Name = "IX_SyncQueue_Priority_LastAttempt", Order = 2)]
        public DateTime LastAttempt { get; set; }

        /// <summary>
        /// The error message from the last failed synchronization attempt.
        /// Provides diagnostic information for troubleshooting.
        /// </summary>
        public string ErrorMessage { get; set; }
    }
}