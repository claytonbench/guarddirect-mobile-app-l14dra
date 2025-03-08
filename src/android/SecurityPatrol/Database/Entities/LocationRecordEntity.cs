using System;
using SQLite; // SQLite-net-pcl 1.8+
using SecurityPatrol.Constants;

namespace SecurityPatrol.Database.Entities
{
    /// <summary>
    /// Represents a geographic location data point in the local SQLite database.
    /// Contains properties for identification, coordinates, accuracy, timestamp,
    /// and synchronization status with the backend.
    /// </summary>
    [Table(DatabaseConstants.TableLocationRecord)]
    public class LocationRecordEntity
    {
        /// <summary>
        /// The unique identifier for the location record.
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>
        /// The user ID associated with this location record.
        /// </summary>
        [Indexed(Name = "IX_LocationRecord_UserId_Timestamp", Order = 1)]
        public string UserId { get; set; }

        /// <summary>
        /// The timestamp when this location was recorded.
        /// </summary>
        [Indexed(Name = "IX_LocationRecord_UserId_Timestamp", Order = 2)]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// The latitude coordinate of the location.
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// The longitude coordinate of the location.
        /// </summary>
        public double Longitude { get; set; }

        /// <summary>
        /// The accuracy of the location measurement in meters.
        /// </summary>
        public double Accuracy { get; set; }

        /// <summary>
        /// Indicates whether this location record has been synchronized with the backend.
        /// </summary>
        [Indexed(Name = "IX_LocationRecord_IsSynced")]
        public bool IsSynced { get; set; }

        /// <summary>
        /// The remote identifier assigned by the backend after synchronization.
        /// </summary>
        public string RemoteId { get; set; }
    }
}