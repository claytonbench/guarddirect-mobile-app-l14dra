using System;
using System.Text.Json.Serialization;
using SecurityPatrol.Core.Entities;

namespace SecurityPatrol.Core.Models
{
    /// <summary>
    /// Represents a geographic location data point with latitude, longitude, accuracy, and timestamp.
    /// Used for tracking user location during patrols and for API communication.
    /// </summary>
    public class LocationModel
    {
        /// <summary>
        /// Gets or sets the unique identifier for the location model.
        /// </summary>
        [JsonPropertyName("id")]
        public int Id { get; set; }
        
        /// <summary>
        /// Gets or sets the latitude coordinate of the GPS location.
        /// </summary>
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }
        
        /// <summary>
        /// Gets or sets the longitude coordinate of the GPS location.
        /// </summary>
        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }
        
        /// <summary>
        /// Gets or sets the accuracy of the GPS reading in meters.
        /// Lower values indicate more accurate readings.
        /// </summary>
        [JsonPropertyName("accuracy")]
        public double Accuracy { get; set; }
        
        /// <summary>
        /// Gets or sets the timestamp when this location point was recorded.
        /// Stored in UTC time for consistency across time zones.
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether this record has been
        /// synchronized with the backend API service.
        /// </summary>
        [JsonPropertyName("isSynced")]
        public bool IsSynced { get; set; }
        
        /// <summary>
        /// Gets or sets the remote identifier assigned by the backend after synchronization.
        /// Null or empty if the record hasn't been synced yet.
        /// </summary>
        [JsonPropertyName("remoteId")]
        public string RemoteId { get; set; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="LocationModel"/> class.
        /// </summary>
        public LocationModel()
        {
            // Initialize with current UTC time and not synced by default
            Timestamp = DateTime.UtcNow;
            IsSynced = false;
        }
        
        /// <summary>
        /// Creates a LocationModel from a LocationRecord entity.
        /// </summary>
        /// <param name="entity">The entity to convert from.</param>
        /// <returns>A new LocationModel populated with data from the entity, or null if the entity is null.</returns>
        public static LocationModel FromEntity(LocationRecord entity)
        {
            if (entity == null)
                return null;
                
            return new LocationModel
            {
                Id = entity.Id,
                Latitude = entity.Latitude,
                Longitude = entity.Longitude,
                Accuracy = entity.Accuracy,
                Timestamp = entity.Timestamp,
                IsSynced = entity.IsSynced,
                RemoteId = entity.RemoteId
            };
        }
        
        /// <summary>
        /// Converts this LocationModel to a LocationRecord entity.
        /// </summary>
        /// <returns>A new LocationRecord entity populated with data from this model.</returns>
        public LocationRecord ToEntity()
        {
            return new LocationRecord
            {
                Id = this.Id,
                Latitude = this.Latitude,
                Longitude = this.Longitude,
                Accuracy = this.Accuracy,
                Timestamp = this.Timestamp,
                IsSynced = this.IsSynced,
                RemoteId = this.RemoteId
            };
        }
    }
}