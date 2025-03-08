using System;
using Newtonsoft.Json; // Newtonsoft.Json 13.0+

namespace SecurityPatrol.Models
{
    /// <summary>
    /// Represents the verification status of an individual checkpoint in a patrol, 
    /// including verification state and timestamp.
    /// </summary>
    public class CheckpointStatus
    {
        /// <summary>
        /// Gets or sets the unique identifier of the checkpoint.
        /// </summary>
        [JsonProperty("checkpointId")]
        public int CheckpointId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the checkpoint has been verified.
        /// </summary>
        [JsonProperty("isVerified")]
        public bool IsVerified { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the checkpoint was verified.
        /// Null if the checkpoint has not been verified.
        /// </summary>
        [JsonProperty("verificationTime")]
        public DateTime? VerificationTime { get; set; }

        /// <summary>
        /// Gets or sets the latitude where the verification occurred.
        /// </summary>
        [JsonProperty("latitude")]
        public double Latitude { get; set; }

        /// <summary>
        /// Gets or sets the longitude where the verification occurred.
        /// </summary>
        [JsonProperty("longitude")]
        public double Longitude { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CheckpointStatus"/> class.
        /// </summary>
        public CheckpointStatus()
        {
            IsVerified = false;
            VerificationTime = null;
            Latitude = 0.0;
            Longitude = 0.0;
        }

        /// <summary>
        /// Marks the checkpoint as verified with the current timestamp and location coordinates.
        /// </summary>
        /// <param name="latitude">The latitude where verification occurred.</param>
        /// <param name="longitude">The longitude where verification occurred.</param>
        public void MarkAsVerified(double latitude, double longitude)
        {
            IsVerified = true;
            VerificationTime = DateTime.UtcNow;
            Latitude = latitude;
            Longitude = longitude;
        }

        /// <summary>
        /// Creates a CheckpointStatus from a CheckpointVerificationEntity.
        /// </summary>
        /// <param name="entity">The entity containing verification data.</param>
        /// <returns>A new CheckpointStatus populated with data from the entity.</returns>
        public static CheckpointStatus FromVerification(CheckpointVerificationEntity entity)
        {
            return new CheckpointStatus
            {
                CheckpointId = entity.CheckpointId,
                IsVerified = true,
                VerificationTime = entity.Timestamp,
                Latitude = entity.Latitude,
                Longitude = entity.Longitude
            };
        }

        /// <summary>
        /// Creates a deep copy of the current CheckpointStatus instance.
        /// </summary>
        /// <returns>A new CheckpointStatus instance with the same property values.</returns>
        public CheckpointStatus Clone()
        {
            return new CheckpointStatus
            {
                CheckpointId = this.CheckpointId,
                IsVerified = this.IsVerified,
                VerificationTime = this.VerificationTime,
                Latitude = this.Latitude,
                Longitude = this.Longitude
            };
        }
    }
}