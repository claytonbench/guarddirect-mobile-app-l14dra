using System;

namespace SecurityPatrol.Core.Entities
{
    /// <summary>
    /// Represents a GPS location data point captured during security patrol operations.
    /// Inherits from AuditableEntity to track creation and modification information.
    /// </summary>
    public class LocationRecord : AuditableEntity
    {
        /// <summary>
        /// Gets or sets the unique identifier for the location record.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the user ID who owns this location record.
        /// This creates a foreign key relationship with the User entity.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the latitude coordinate of the GPS location.
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// Gets or sets the longitude coordinate of the GPS location.
        /// </summary>
        public double Longitude { get; set; }

        /// <summary>
        /// Gets or sets the accuracy of the GPS reading in meters.
        /// Lower values indicate more accurate readings.
        /// </summary>
        public double Accuracy { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this location point was recorded.
        /// Stored in UTC time for consistency across time zones.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this record has been
        /// synchronized with the backend API service.
        /// </summary>
        public bool IsSynced { get; set; }

        /// <summary>
        /// Gets or sets the remote identifier assigned by the backend after synchronization.
        /// Null or empty if the record hasn't been synced yet.
        /// </summary>
        public string RemoteId { get; set; }

        /// <summary>
        /// Gets or sets the user who owns this location record.
        /// This is the navigation property for the UserId foreign key.
        /// </summary>
        public User User { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocationRecord"/> class.
        /// </summary>
        public LocationRecord()
        {
            // Initialize with current UTC time and not synced by default
            Timestamp = DateTime.UtcNow;
            IsSynced = false;
        }
    }
}