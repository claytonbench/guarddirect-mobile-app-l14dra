using System;
using System.Collections.Generic;

namespace SecurityPatrol.Models
{
    /// <summary>
    /// Represents the results of a synchronization operation, tracking success and failure counts,
    /// pending items, and detailed results by entity type.
    /// </summary>
    public class SyncResult
    {
        /// <summary>
        /// Gets or sets the count of successful synchronization operations.
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// Gets or sets the count of failed synchronization operations.
        /// </summary>
        public int FailureCount { get; set; }

        /// <summary>
        /// Gets or sets the count of pending synchronization operations.
        /// </summary>
        public int PendingCount { get; set; }

        /// <summary>
        /// Gets or sets the detailed results by entity type. The dictionary key is the entity type,
        /// and the value is a list of entity IDs with status information.
        /// </summary>
        public Dictionary<string, List<string>> EntityResults { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncResult"/> class.
        /// </summary>
        public SyncResult()
        {
            SuccessCount = 0;
            FailureCount = 0;
            PendingCount = 0;
            EntityResults = new Dictionary<string, List<string>>();
        }

        /// <summary>
        /// Adds a successful synchronization result for an entity.
        /// </summary>
        /// <param name="entityType">The type of entity that was synchronized.</param>
        /// <param name="entityId">The ID of the entity that was synchronized.</param>
        public void AddSuccess(string entityType, string entityId)
        {
            SuccessCount++;
            
            if (!EntityResults.ContainsKey(entityType))
            {
                EntityResults[entityType] = new List<string>();
            }
            
            EntityResults[entityType].Add($"success:{entityId}");
        }

        /// <summary>
        /// Adds a failed synchronization result for an entity.
        /// </summary>
        /// <param name="entityType">The type of entity that failed to synchronize.</param>
        /// <param name="entityId">The ID of the entity that failed to synchronize.</param>
        /// <param name="errorMessage">The error message describing why synchronization failed.</param>
        public void AddFailure(string entityType, string entityId, string errorMessage)
        {
            FailureCount++;
            
            if (!EntityResults.ContainsKey(entityType))
            {
                EntityResults[entityType] = new List<string>();
            }
            
            EntityResults[entityType].Add($"failure:{entityId}:{errorMessage}");
        }

        /// <summary>
        /// Merges another SyncResult into this one, combining counts and entity results.
        /// </summary>
        /// <param name="other">The other SyncResult to merge.</param>
        public void Merge(SyncResult other)
        {
            if (other == null)
                return;

            SuccessCount += other.SuccessCount;
            FailureCount += other.FailureCount;
            PendingCount += other.PendingCount;

            foreach (var entityType in other.EntityResults.Keys)
            {
                if (!EntityResults.ContainsKey(entityType))
                {
                    EntityResults[entityType] = new List<string>();
                }

                EntityResults[entityType].AddRange(other.EntityResults[entityType]);
            }
        }

        /// <summary>
        /// Gets the total count of processed items (success + failure).
        /// </summary>
        /// <returns>The total count of processed items.</returns>
        public int GetTotalCount()
        {
            return SuccessCount + FailureCount;
        }

        /// <summary>
        /// Calculates the success rate as a percentage.
        /// </summary>
        /// <returns>The success rate as a percentage (0-100).</returns>
        public double GetSuccessRate()
        {
            if (GetTotalCount() == 0)
                return 100.0; // No items means 100% success
            
            return (SuccessCount / (double)GetTotalCount()) * 100.0;
        }

        /// <summary>
        /// Returns a string representation of the SyncResult.
        /// </summary>
        /// <returns>A string representation of the SyncResult.</returns>
        public override string ToString()
        {
            return $"SyncResult: Success: {SuccessCount}, Failure: {FailureCount}, Pending: {PendingCount}, Success Rate: {GetSuccessRate():F1}%";
        }
    }
}