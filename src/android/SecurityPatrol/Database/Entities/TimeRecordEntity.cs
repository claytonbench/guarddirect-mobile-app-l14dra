using SQLite;
using System;

namespace SecurityPatrol.Database.Entities
{
    /// <summary>
    /// Represents a clock in/out event in the SQLite database.
    /// This entity stores all necessary information for time tracking
    /// including the event type, timestamp, location, and synchronization status.
    /// </summary>
    [Table(DatabaseConstants.TableTimeRecord)]
    public class TimeRecordEntity
    {
        /// <summary>
        /// Gets or sets the primary key for the time record.
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the user identifier associated with this time record.
        /// </summary>
        [Indexed]
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the type of time record (e.g., "ClockIn", "ClockOut").
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the clock event occurred.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the latitude coordinate where the clock event occurred.
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// Gets or sets the longitude coordinate where the clock event occurred.
        /// </summary>
        public double Longitude { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this record has been synchronized with the backend.
        /// </summary>
        [Indexed]
        public bool IsSynced { get; set; }

        /// <summary>
        /// Gets or sets the remote identifier assigned by the backend API after synchronization.
        /// This is used to map local records to their remote counterparts.
        /// </summary>
        public string RemoteId { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeRecordEntity"/> class.
        /// </summary>
        public TimeRecordEntity()
        {
            Id = 0;
            UserId = string.Empty;
            Type = string.Empty;
            Timestamp = DateTime.Now;
            Latitude = 0.0;
            Longitude = 0.0;
            IsSynced = false;
            RemoteId = string.Empty;
        }
    }
}