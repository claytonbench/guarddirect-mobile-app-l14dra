using SQLite;
using System;

namespace SecurityPatrol.Database.Entities
{
    /// <summary>
    /// Represents an activity report in the SQLite database.
    /// Maps to the ActivityReport table and contains fields for storing report text, 
    /// timestamp, location data, and synchronization status.
    /// </summary>
    [Table("ActivityReport")]
    public class ReportEntity
    {
        /// <summary>
        /// The unique identifier for the report in the local database.
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>
        /// The unique identifier of the user who created the report.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// The content of the activity report.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// The date and time when the report was created.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// The latitude coordinate where the report was created.
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// The longitude coordinate where the report was created.
        /// </summary>
        public double Longitude { get; set; }

        /// <summary>
        /// Indicates whether this report has been synchronized with the backend.
        /// </summary>
        public bool IsSynced { get; set; }

        /// <summary>
        /// The identifier of this report in the remote/backend system.
        /// Only populated after successful synchronization.
        /// </summary>
        public string RemoteId { get; set; }

        /// <summary>
        /// Default constructor for the ReportEntity class.
        /// Initializes default values for a new report.
        /// </summary>
        public ReportEntity()
        {
            Timestamp = DateTime.UtcNow;
            IsSynced = false;
        }
    }
}