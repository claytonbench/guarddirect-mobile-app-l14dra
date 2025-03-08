using System;
using SecurityPatrol.Database.Entities;

namespace SecurityPatrol.Models
{
    /// <summary>
    /// Represents an item pending synchronization with the backend services.
    /// Contains properties for entity identification, priority, retry tracking, and error information.
    /// </summary>
    public class SyncItem
    {
        /// <summary>
        /// The type of entity being synchronized (e.g., "TimeRecord", "Photo").
        /// </summary>
        public string EntityType { get; set; }

        /// <summary>
        /// The unique identifier of the entity being synchronized.
        /// </summary>
        public string EntityId { get; set; }

        /// <summary>
        /// The priority of the sync item (higher values indicate higher priority).
        /// Used for ordering synchronization operations.
        /// </summary>
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
        public DateTime LastAttempt { get; set; }

        /// <summary>
        /// The error message from the last failed synchronization attempt.
        /// Provides diagnostic information for troubleshooting.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Initializes a new instance of the SyncItem class with the specified parameters.
        /// </summary>
        /// <param name="entityType">The type of entity being synchronized.</param>
        /// <param name="entityId">The unique identifier of the entity being synchronized.</param>
        /// <param name="priority">The priority of the sync item (higher values indicate higher priority).</param>
        public SyncItem(string entityType, string entityId, int priority)
        {
            EntityType = entityType;
            EntityId = entityId;
            Priority = priority;
            RetryCount = 0;
            LastAttempt = DateTime.MinValue;
            ErrorMessage = null;
        }

        /// <summary>
        /// Creates a SyncItem from a SyncQueueEntity database entity.
        /// </summary>
        /// <param name="entity">The SyncQueueEntity to convert from.</param>
        /// <returns>A new SyncItem populated with data from the entity.</returns>
        public static SyncItem FromEntity(SyncQueueEntity entity)
        {
            var syncItem = new SyncItem(entity.EntityType, entity.EntityId, entity.Priority)
            {
                RetryCount = entity.RetryCount,
                LastAttempt = entity.LastAttempt,
                ErrorMessage = entity.ErrorMessage
            };
            
            return syncItem;
        }

        /// <summary>
        /// Converts this SyncItem to a SyncQueueEntity database entity.
        /// </summary>
        /// <returns>A new SyncQueueEntity populated with data from this SyncItem.</returns>
        public SyncQueueEntity ToEntity()
        {
            var entity = new SyncQueueEntity
            {
                EntityType = this.EntityType,
                EntityId = this.EntityId,
                Priority = this.Priority,
                RetryCount = this.RetryCount,
                LastAttempt = this.LastAttempt,
                ErrorMessage = this.ErrorMessage
            };
            
            return entity;
        }

        /// <summary>
        /// Returns a string representation of the SyncItem.
        /// </summary>
        /// <returns>A string representation of the SyncItem.</returns>
        public override string ToString()
        {
            return $"SyncItem: {EntityType}:{EntityId}, Priority: {Priority}, RetryCount: {RetryCount}";
        }
    }
}