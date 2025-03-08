using System;

namespace SecurityPatrol.Core.Entities
{
    /// <summary>
    /// Represents a clock in/out event for a security personnel user.
    /// Inherits from AuditableEntity to track creation and modification information.
    /// </summary>
    public class TimeRecord : AuditableEntity
    {
        /// <summary>
        /// Gets or sets the unique identifier for the time record.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the user associated with this time record.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the user associated with this time record.
        /// This property enables Entity Framework to establish the relationship between User and TimeRecord.
        /// </summary>
        public User User { get; set; }

        /// <summary>
        /// Gets or sets the type of time record (e.g., "ClockIn", "ClockOut").
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the clock event occurred.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the latitude of the location where the clock event occurred.
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// Gets or sets the longitude of the location where the clock event occurred.
        /// </summary>
        public double Longitude { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this record has been synchronized with the backend.
        /// </summary>
        public bool IsSynced { get; set; }

        /// <summary>
        /// Gets or sets the identifier of this record in the backend system after synchronization.
        /// Will be null for records that have not yet been synchronized.
        /// </summary>
        public string RemoteId { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeRecord"/> class.
        /// </summary>
        public TimeRecord()
        {
            IsSynced = false;
            Type = string.Empty;
            RemoteId = null;
        }
    }
}