using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SecurityPatrol.Core.Entities;

namespace SecurityPatrol.Core.Models
{
    /// <summary>
    /// Represents a request to verify a checkpoint during a patrol.
    /// Contains the checkpoint ID, timestamp of verification, and the user's current location coordinates.
    /// </summary>
    public class CheckpointVerificationRequest
    {
        /// <summary>
        /// Gets or sets the ID of the checkpoint being verified.
        /// </summary>
        [Required]
        [JsonPropertyName("checkpointId")]
        public int CheckpointId { get; set; }
        
        /// <summary>
        /// Gets or sets the timestamp when the checkpoint was verified.
        /// </summary>
        [Required]
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// Gets or sets the location of the user when verifying the checkpoint.
        /// </summary>
        [Required]
        [JsonPropertyName("location")]
        public LocationModel Location { get; set; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="CheckpointVerificationRequest"/> class.
        /// </summary>
        public CheckpointVerificationRequest()
        {
            Timestamp = DateTime.UtcNow;
            Location = new LocationModel();
        }
        
        /// <summary>
        /// Validates if the request contains all required data and is properly formatted.
        /// </summary>
        /// <returns>True if the request is valid, otherwise false.</returns>
        public bool IsValid()
        {
            if (CheckpointId <= 0)
                return false;
                
            if (Timestamp == default)
                return false;
                
            if (Location == null)
                return false;
                
            if (Location.Latitude < -90 || Location.Latitude > 90)
                return false;
                
            if (Location.Longitude < -180 || Location.Longitude > 180)
                return false;
                
            return true;
        }
        
        /// <summary>
        /// Converts this request model to a CheckpointVerification entity.
        /// </summary>
        /// <param name="userId">The ID of the user making the verification.</param>
        /// <returns>A new CheckpointVerification entity populated with data from this request.</returns>
        public CheckpointVerification ToEntity(string userId)
        {
            return new CheckpointVerification
            {
                CheckpointId = this.CheckpointId,
                UserId = userId,
                Timestamp = this.Timestamp,
                Latitude = this.Location.Latitude,
                Longitude = this.Location.Longitude,
                IsSynced = true
            };
        }
    }
}