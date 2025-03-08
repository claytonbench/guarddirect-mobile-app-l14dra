using System; // Version 8.0+

namespace SecurityPatrol.Models
{
    /// <summary>
    /// Represents a geographic location data point with latitude, longitude, accuracy, and timestamp.
    /// Used for tracking user location during patrols, checkpoint verification, and synchronization with the backend API.
    /// </summary>
    public class LocationModel
    {
        /// <summary>
        /// Gets or sets the unique identifier for this location record in the local database.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the latitude coordinate in decimal degrees.
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// Gets or sets the longitude coordinate in decimal degrees.
        /// </summary>
        public double Longitude { get; set; }

        /// <summary>
        /// Gets or sets the accuracy of the location measurement in meters.
        /// </summary>
        public double Accuracy { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp when this location was recorded.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this location has been synchronized with the backend.
        /// </summary>
        public bool IsSynced { get; set; }

        /// <summary>
        /// Gets or sets the remote identifier assigned by the backend after synchronization.
        /// This will be null or empty for records that haven't been synchronized yet.
        /// </summary>
        public string RemoteId { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocationModel"/> class.
        /// </summary>
        public LocationModel()
        {
            Timestamp = DateTime.UtcNow;
            IsSynced = false;
        }

        /// <summary>
        /// Creates a LocationModel from a LocationRecordEntity.
        /// </summary>
        /// <param name="entity">The entity to convert from.</param>
        /// <returns>A new LocationModel populated with data from the entity, or null if the entity is null.</returns>
        public static LocationModel FromEntity(LocationRecordEntity entity)
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
        /// Converts this LocationModel to a LocationRecordEntity.
        /// </summary>
        /// <returns>A new LocationRecordEntity populated with data from this model.</returns>
        public LocationRecordEntity ToEntity()
        {
            return new LocationRecordEntity
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

        /// <summary>
        /// Creates a deep copy of this LocationModel.
        /// </summary>
        /// <returns>A new LocationModel with the same property values.</returns>
        public LocationModel Clone()
        {
            return new LocationModel
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