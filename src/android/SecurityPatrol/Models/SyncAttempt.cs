using System; // .NET 8.0+

namespace SecurityPatrol.Models
{
    /// <summary>
    /// Represents a single synchronization attempt for an entity. Used for tracking and auditing 
    /// synchronization operations, including success/failure status, timestamps, and error messages.
    /// </summary>
    public class SyncAttempt
    {
        /// <summary>
        /// Gets or sets the type of entity being synchronized.
        /// </summary>
        public string EntityType { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier of the entity being synchronized.
        /// </summary>
        public string EntityId { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the synchronization attempt occurred.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the synchronization attempt was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the error message if the synchronization attempt failed.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncAttempt"/> class with the specified parameters.
        /// </summary>
        /// <param name="entityType">The type of entity being synchronized.</param>
        /// <param name="entityId">The unique identifier of the entity being synchronized.</param>
        /// <param name="timestamp">The timestamp when the synchronization attempt occurred.</param>
        /// <param name="success">A value indicating whether the synchronization attempt was successful.</param>
        /// <param name="errorMessage">The error message if the synchronization attempt failed.</param>
        public SyncAttempt(string entityType, string entityId, DateTime timestamp, bool success, string errorMessage)
        {
            EntityType = entityType;
            EntityId = entityId;
            Timestamp = timestamp;
            Success = success;
            ErrorMessage = errorMessage;
        }

        /// <summary>
        /// Creates a new SyncAttempt with the current timestamp.
        /// </summary>
        /// <param name="entityType">The type of entity being synchronized.</param>
        /// <param name="entityId">The unique identifier of the entity being synchronized.</param>
        /// <param name="success">A value indicating whether the synchronization attempt was successful.</param>
        /// <param name="errorMessage">The error message if the synchronization attempt failed.</param>
        /// <returns>A new SyncAttempt instance with the current timestamp.</returns>
        public static SyncAttempt Create(string entityType, string entityId, bool success, string errorMessage = null)
        {
            return new SyncAttempt(entityType, entityId, DateTime.UtcNow, success, errorMessage);
        }

        /// <summary>
        /// Returns a string representation of the SyncAttempt.
        /// </summary>
        /// <returns>A string representation of the SyncAttempt.</returns>
        public override string ToString()
        {
            return $"[{Timestamp}] {EntityType}:{EntityId} - Success: {Success}, Error: {ErrorMessage}";
        }
    }
}