using System;
using Newtonsoft.Json; // Version 13.0+

namespace SecurityPatrol.Models
{
    /// <summary>
    /// Represents the overall status of a patrol operation, including location, checkpoint counts, and completion metrics.
    /// </summary>
    public class PatrolStatus
    {
        /// <summary>
        /// Gets or sets the identifier of the patrol location.
        /// </summary>
        [JsonProperty("locationId")]
        public int LocationId { get; set; }

        /// <summary>
        /// Gets or sets the total number of checkpoints in the patrol.
        /// </summary>
        [JsonProperty("totalCheckpoints")]
        public int TotalCheckpoints { get; set; }

        /// <summary>
        /// Gets or sets the number of checkpoints that have been verified.
        /// </summary>
        [JsonProperty("verifiedCheckpoints")]
        public int VerifiedCheckpoints { get; set; }

        /// <summary>
        /// Gets or sets the start time of the patrol operation.
        /// </summary>
        [JsonProperty("startTime")]
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets or sets the end time of the patrol operation. Null if the patrol is not complete.
        /// </summary>
        [JsonProperty("endTime")]
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Default constructor for the PatrolStatus class
        /// </summary>
        public PatrolStatus()
        {
            LocationId = 0;
            TotalCheckpoints = 0;
            VerifiedCheckpoints = 0;
            StartTime = DateTime.UtcNow;
            EndTime = null;
        }

        /// <summary>
        /// Calculates the percentage of checkpoints that have been verified
        /// </summary>
        /// <returns>Percentage of completion (0-100)</returns>
        public double CalculateCompletionPercentage()
        {
            if (TotalCheckpoints == 0)
                return 0;

            return ((double)VerifiedCheckpoints / TotalCheckpoints) * 100;
        }

        /// <summary>
        /// Determines if the patrol is complete (all checkpoints verified)
        /// </summary>
        /// <returns>True if all checkpoints are verified, otherwise false</returns>
        public bool IsComplete()
        {
            return TotalCheckpoints > 0 && VerifiedCheckpoints == TotalCheckpoints;
        }

        /// <summary>
        /// Updates the verified checkpoint count and optionally marks the patrol as complete
        /// </summary>
        /// <param name="verifiedCount">The number of verified checkpoints</param>
        /// <param name="isComplete">Whether the patrol is complete</param>
        public void UpdateProgress(int verifiedCount, bool isComplete = false)
        {
            VerifiedCheckpoints = verifiedCount;
            
            if (isComplete && EndTime == null)
            {
                EndTime = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Marks the patrol as complete with the current timestamp
        /// </summary>
        public void CompletePatrol()
        {
            EndTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Calculates the duration of the patrol
        /// </summary>
        /// <returns>Duration of the patrol</returns>
        public TimeSpan GetDuration()
        {
            return EndTime.HasValue 
                ? EndTime.Value - StartTime 
                : DateTime.UtcNow - StartTime;
        }

        /// <summary>
        /// Creates a deep copy of the current PatrolStatus instance
        /// </summary>
        /// <returns>A new PatrolStatus instance with the same property values</returns>
        public PatrolStatus Clone()
        {
            return new PatrolStatus
            {
                LocationId = this.LocationId,
                TotalCheckpoints = this.TotalCheckpoints,
                VerifiedCheckpoints = this.VerifiedCheckpoints,
                StartTime = this.StartTime,
                EndTime = this.EndTime
            };
        }
    }
}