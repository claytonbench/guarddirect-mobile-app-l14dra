using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using SecurityPatrol.Core.Entities;

namespace SecurityPatrol.Core.Models
{
    /// <summary>
    /// Represents the overall status of a patrol operation, including location, checkpoint counts, and completion metrics.
    /// Used for API communication between the backend services and mobile application for patrol status tracking.
    /// </summary>
    public class PatrolStatusModel
    {
        /// <summary>
        /// Gets or sets the identifier of the patrol location.
        /// </summary>
        [JsonPropertyName("locationId")]
        public int LocationId { get; set; }

        /// <summary>
        /// Gets or sets the total number of checkpoints in the patrol.
        /// </summary>
        [JsonPropertyName("totalCheckpoints")]
        public int TotalCheckpoints { get; set; }

        /// <summary>
        /// Gets or sets the number of checkpoints that have been verified.
        /// </summary>
        [JsonPropertyName("verifiedCheckpoints")]
        public int VerifiedCheckpoints { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the most recent checkpoint verification.
        /// </summary>
        [JsonPropertyName("lastVerificationTime")]
        public DateTime? LastVerificationTime { get; set; }

        /// <summary>
        /// Gets or sets the percentage of completion for the patrol (0-100).
        /// </summary>
        [JsonPropertyName("completionPercentage")]
        public double CompletionPercentage { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the patrol is complete (all checkpoints verified).
        /// </summary>
        [JsonPropertyName("isComplete")]
        public bool IsComplete { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PatrolStatusModel"/> class.
        /// </summary>
        public PatrolStatusModel()
        {
            LocationId = 0;
            TotalCheckpoints = 0;
            VerifiedCheckpoints = 0;
            LastVerificationTime = null;
            CompletionPercentage = 0;
            IsComplete = false;
        }

        /// <summary>
        /// Creates a PatrolStatusModel from patrol location, checkpoints, and verifications data.
        /// </summary>
        /// <param name="locationId">The identifier of the patrol location.</param>
        /// <param name="checkpoints">The collection of checkpoints for the patrol location.</param>
        /// <param name="verifications">The collection of checkpoint verifications.</param>
        /// <returns>A new PatrolStatusModel populated with the calculated status.</returns>
        public static PatrolStatusModel Create(int locationId, IEnumerable<Checkpoint> checkpoints, IEnumerable<CheckpointVerification> verifications)
        {
            var model = new PatrolStatusModel
            {
                LocationId = locationId,
                TotalCheckpoints = checkpoints?.Count() ?? 0
            };

            if (verifications != null && verifications.Any())
            {
                // Count unique verified checkpoints
                model.VerifiedCheckpoints = verifications
                    .Select(v => v.CheckpointId)
                    .Distinct()
                    .Count();

                // Get the most recent verification time
                model.LastVerificationTime = verifications
                    .OrderByDescending(v => v.Timestamp)
                    .FirstOrDefault()?.Timestamp;
            }

            // Calculate completion percentage
            model.CompletionPercentage = model.TotalCheckpoints > 0
                ? Math.Round((double)model.VerifiedCheckpoints / model.TotalCheckpoints * 100, 2)
                : 0;

            // Determine if patrol is complete
            model.IsComplete = model.TotalCheckpoints > 0 && 
                               model.VerifiedCheckpoints == model.TotalCheckpoints;

            return model;
        }
    }
}