using System;
using System.Text.Json.Serialization;
using SecurityPatrol.Core.Entities;

namespace SecurityPatrol.Core.Models
{
    /// <summary>
    /// Represents a checkpoint location within a patrol route. 
    /// Used for API communication between the backend services and mobile application for patrol management operations.
    /// </summary>
    public class CheckpointModel
    {
        /// <summary>
        /// Gets or sets the unique identifier for the checkpoint.
        /// </summary>
        [JsonPropertyName("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the patrol location that this checkpoint belongs to.
        /// </summary>
        [JsonPropertyName("locationId")]
        public int LocationId { get; set; }

        /// <summary>
        /// Gets or sets the name or description of the checkpoint.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the latitude coordinate of the checkpoint.
        /// </summary>
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        /// <summary>
        /// Gets or sets the longitude coordinate of the checkpoint.
        /// </summary>
        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the checkpoint has been verified.
        /// </summary>
        [JsonPropertyName("isVerified")]
        public bool IsVerified { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the checkpoint was verified.
        /// Null if the checkpoint has not been verified.
        /// </summary>
        [JsonPropertyName("verificationTime")]
        public DateTime? VerificationTime { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CheckpointModel"/> class.
        /// </summary>
        public CheckpointModel()
        {
            // Initialize with default values
            IsVerified = false;
            VerificationTime = null;
        }

        /// <summary>
        /// Creates a CheckpointModel from a Checkpoint entity.
        /// </summary>
        /// <param name="entity">The checkpoint entity to convert.</param>
        /// <param name="isVerified">Whether the checkpoint is verified.</param>
        /// <param name="verificationTime">The time of verification, if verified.</param>
        /// <returns>A new CheckpointModel populated with data from the entity.</returns>
        public static CheckpointModel FromEntity(Checkpoint entity, bool isVerified = false, DateTime? verificationTime = null)
        {
            if (entity == null)
            {
                return null;
            }

            return new CheckpointModel
            {
                Id = entity.Id,
                LocationId = entity.LocationId,
                Name = entity.Name,
                Latitude = entity.Latitude,
                Longitude = entity.Longitude,
                IsVerified = isVerified,
                VerificationTime = verificationTime
            };
        }

        /// <summary>
        /// Converts this CheckpointModel to a Checkpoint entity.
        /// </summary>
        /// <returns>A new Checkpoint entity populated with data from this model.</returns>
        public Checkpoint ToEntity()
        {
            return new Checkpoint
            {
                Id = this.Id,
                LocationId = this.LocationId,
                Name = this.Name,
                Latitude = this.Latitude,
                Longitude = this.Longitude
                // Note: IsVerified and VerificationTime are not part of the base Checkpoint entity
                // These would be handled as part of a CheckpointVerification entity
            };
        }
    }
}