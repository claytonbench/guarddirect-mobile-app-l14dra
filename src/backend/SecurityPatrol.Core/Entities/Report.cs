using System;

namespace SecurityPatrol.Core.Entities
{
    /// <summary>
    /// Represents an activity report created by security personnel during patrols.
    /// Inherits from AuditableEntity to track creation and modification information.
    /// </summary>
    public class Report : AuditableEntity
    {
        /// <summary>
        /// Gets or sets the unique identifier for the report.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the content text of the report.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the report was created.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the latitude coordinate where the report was created.
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// Gets or sets the longitude coordinate where the report was created.
        /// </summary>
        public double Longitude { get; set; }

        /// <summary>
        /// Gets or sets the user identifier who created this report.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the user who created this report.
        /// Navigation property for the relationship with User entity.
        /// </summary>
        public User User { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this report has been synchronized with the backend.
        /// </summary>
        public bool IsSynced { get; set; }

        /// <summary>
        /// Gets or sets the identifier of this report in the remote system after synchronization.
        /// </summary>
        public string RemoteId { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Report"/> class.
        /// </summary>
        public Report()
        {
            IsSynced = false;
            Timestamp = DateTime.UtcNow;
        }
    }
}