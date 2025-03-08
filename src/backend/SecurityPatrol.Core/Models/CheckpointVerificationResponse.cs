using System;
using System.Text.Json.Serialization;
using SecurityPatrol.Core.Entities;

namespace SecurityPatrol.Core.Models
{
    /// <summary>
    /// Represents a response to a checkpoint verification request.
    /// Contains the verification ID, checkpoint ID, timestamp of verification, and status information.
    /// Used for API communication between the backend services and mobile application.
    /// </summary>
    public class CheckpointVerificationResponse
    {
        /// <summary>
        /// Gets or sets the unique identifier for the checkpoint verification.
        /// </summary>
        [JsonPropertyName("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the checkpoint that was verified.
        /// </summary>
        [JsonPropertyName("checkpointId")]
        public int CheckpointId { get; set; }

        /// <summary>
        /// Gets or sets the name of the checkpoint that was verified.
        /// </summary>
        [JsonPropertyName("checkpointName")]
        public string CheckpointName { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the checkpoint was verified.
        /// </summary>
        [JsonPropertyName("verificationTime")]
        public DateTime VerificationTime { get; set; }

        /// <summary>
        /// Gets or sets the latitude coordinate where the verification occurred.
        /// </summary>
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        /// <summary>
        /// Gets or sets the longitude coordinate where the verification occurred.
        /// </summary>
        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the checkpoint was successfully verified.
        /// </summary>
        [JsonPropertyName("isVerified")]
        public bool IsVerified { get; set; }

        /// <summary>
        /// Gets or sets the status of the verification operation.
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CheckpointVerificationResponse"/> class.
        /// </summary>
        public CheckpointVerificationResponse()
        {
            // Initialize default values
            CheckpointName = string.Empty;
            VerificationTime = DateTime.UtcNow;
            IsVerified = true;
            Status = "Verified";
        }

        /// <summary>
        /// Creates a CheckpointVerificationResponse from a CheckpointVerification entity.
        /// </summary>
        /// <param name="entity">The CheckpointVerification entity to convert from.</param>
        /// <returns>A new CheckpointVerificationResponse populated with data from the entity.</returns>
        public static CheckpointVerificationResponse FromEntity(CheckpointVerification entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            return new CheckpointVerificationResponse
            {
                Id = entity.Id,
                CheckpointId = entity.CheckpointId,
                CheckpointName = entity.Checkpoint?.Name ?? string.Empty,
                VerificationTime = entity.Timestamp,
                Latitude = entity.Latitude,
                Longitude = entity.Longitude,
                IsVerified = true,
                Status = "Verified"
            };
        }
    }
}