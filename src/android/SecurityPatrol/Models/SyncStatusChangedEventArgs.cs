using System;

namespace SecurityPatrol.Models
{
    /// <summary>
    /// Event arguments for synchronization status change events.
    /// Contains information about the entity type being synchronized, the current status, and progress information.
    /// </summary>
    public class SyncStatusChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the type of entity being synchronized.
        /// </summary>
        public string EntityType { get; }

        /// <summary>
        /// Gets the current status of the synchronization operation.
        /// </summary>
        public string Status { get; }

        /// <summary>
        /// Gets the number of completed synchronization operations.
        /// </summary>
        public int CompletedCount { get; }

        /// <summary>
        /// Gets the total number of synchronization operations.
        /// </summary>
        public int TotalCount { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncStatusChangedEventArgs"/> class with the specified parameters.
        /// </summary>
        /// <param name="entityType">The type of entity being synchronized.</param>
        /// <param name="status">The current status of the synchronization operation.</param>
        /// <param name="completedCount">The number of completed synchronization operations.</param>
        /// <param name="totalCount">The total number of synchronization operations.</param>
        public SyncStatusChangedEventArgs(string entityType, string status, int completedCount, int totalCount)
        {
            EntityType = entityType;
            Status = status;
            CompletedCount = completedCount;
            TotalCount = totalCount;
        }

        /// <summary>
        /// Calculates the completion percentage of the synchronization operation.
        /// </summary>
        /// <returns>The completion percentage (0-100).</returns>
        public double GetCompletionPercentage()
        {
            if (TotalCount == 0)
                return 100.0; // No items means 100% complete

            return (CompletedCount / (double)TotalCount) * 100.0;
        }

        /// <summary>
        /// Returns a string representation of the <see cref="SyncStatusChangedEventArgs"/>.
        /// </summary>
        /// <returns>A string representation of the event arguments.</returns>
        public override string ToString()
        {
            return $"SyncStatus: {EntityType}, Status: {Status}, Progress: {CompletedCount}/{TotalCount} ({GetCompletionPercentage():F1}%)";
        }
    }
}