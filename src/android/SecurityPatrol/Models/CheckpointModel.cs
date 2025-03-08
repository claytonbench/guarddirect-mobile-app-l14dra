using System;
using Newtonsoft.Json; // Version 13.0+
using SecurityPatrol.Database.Entities;
using SecurityPatrol.Helpers;

namespace SecurityPatrol.Models
{
    /// <summary>
    /// Represents a security patrol checkpoint with location coordinates, verification status, and proximity calculation capabilities.
    /// </summary>
    public class CheckpointModel
    {
        /// <summary>
        /// Gets or sets the unique identifier for this checkpoint.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the patrol location this checkpoint belongs to.
        /// </summary>
        public int LocationId { get; set; }

        /// <summary>
        /// Gets or sets the name or description of the checkpoint.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the latitude coordinate of the checkpoint.
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// Gets or sets the longitude coordinate of the checkpoint.
        /// </summary>
        public double Longitude { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this checkpoint has been verified during the patrol.
        /// </summary>
        public bool IsVerified { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this checkpoint was verified, or null if not verified.
        /// </summary>
        public DateTime? VerificationTime { get; set; }

        /// <summary>
        /// Gets or sets the remote identifier assigned by the backend system.
        /// </summary>
        public string RemoteId { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CheckpointModel"/> class.
        /// </summary>
        public CheckpointModel()
        {
            IsVerified = false;
            VerificationTime = null;
        }

        /// <summary>
        /// Creates a CheckpointModel from a CheckpointEntity.
        /// </summary>
        /// <param name="entity">The entity to convert from.</param>
        /// <returns>A new CheckpointModel populated with data from the entity.</returns>
        public static CheckpointModel FromEntity(CheckpointEntity entity)
        {
            if (entity == null)
                return null;

            return new CheckpointModel
            {
                Id = entity.Id,
                LocationId = entity.LocationId,
                Name = entity.Name,
                Latitude = entity.Latitude,
                Longitude = entity.Longitude,
                RemoteId = entity.RemoteId,
                IsVerified = false,
                VerificationTime = null
            };
        }

        /// <summary>
        /// Converts this model to a CheckpointEntity.
        /// </summary>
        /// <returns>A new CheckpointEntity populated with data from this model.</returns>
        public CheckpointEntity ToEntity()
        {
            return new CheckpointEntity
            {
                Id = this.Id,
                LocationId = this.LocationId,
                Name = this.Name,
                Latitude = this.Latitude,
                Longitude = this.Longitude,
                RemoteId = this.RemoteId,
                LastUpdated = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Marks this checkpoint as verified with the current timestamp.
        /// </summary>
        public void MarkAsVerified()
        {
            IsVerified = true;
            VerificationTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Calculates the distance between this checkpoint and the specified coordinates.
        /// </summary>
        /// <param name="latitude">The latitude coordinate to calculate distance to.</param>
        /// <param name="longitude">The longitude coordinate to calculate distance to.</param>
        /// <returns>Distance in meters.</returns>
        public double CalculateDistance(double latitude, double longitude)
        {
            return LocationHelper.CalculateDistance(this.Latitude, this.Longitude, latitude, longitude);
        }

        /// <summary>
        /// Determines if the specified coordinates are within the proximity threshold of this checkpoint.
        /// </summary>
        /// <param name="latitude">The latitude coordinate to check.</param>
        /// <param name="longitude">The longitude coordinate to check.</param>
        /// <param name="thresholdMeters">The proximity threshold in meters.</param>
        /// <returns>True if within proximity threshold, otherwise false.</returns>
        public bool IsWithinProximity(double latitude, double longitude, double thresholdMeters)
        {
            double distance = CalculateDistance(latitude, longitude);
            return distance <= thresholdMeters;
        }

        /// <summary>
        /// Creates a deep copy of the current CheckpointModel instance.
        /// </summary>
        /// <returns>A new CheckpointModel instance with the same property values.</returns>
        public CheckpointModel Clone()
        {
            return new CheckpointModel
            {
                Id = this.Id,
                LocationId = this.LocationId,
                Name = this.Name,
                Latitude = this.Latitude,
                Longitude = this.Longitude,
                IsVerified = this.IsVerified,
                VerificationTime = this.VerificationTime,
                RemoteId = this.RemoteId
            };
        }

        /// <summary>
        /// Returns a string representation of the checkpoint.
        /// </summary>
        /// <returns>A string in the format "Checkpoint {Id}: {Name}".</returns>
        public override string ToString()
        {
            return $"Checkpoint {Id}: {Name}";
        }
    }
}