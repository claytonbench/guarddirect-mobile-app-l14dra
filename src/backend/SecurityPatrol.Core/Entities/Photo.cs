using System;

namespace SecurityPatrol.Core.Entities
{
    /// <summary>
    /// Represents a photo captured by a security personnel during patrol.
    /// Inherits from AuditableEntity to track creation and modification information.
    /// </summary>
    public class Photo : AuditableEntity
    {
        /// <summary>
        /// Gets or sets the unique identifier for the photo.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the user who captured the photo.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the user who captured the photo.
        /// Navigation property for entity relationship.
        /// </summary>
        public User User { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the photo was captured.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the latitude coordinate where the photo was captured.
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// Gets or sets the longitude coordinate where the photo was captured.
        /// </summary>
        public double Longitude { get; set; }

        /// <summary>
        /// Gets or sets the file path where the photo is stored.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Photo"/> class.
        /// </summary>
        public Photo()
        {
            Timestamp = DateTime.UtcNow;
        }
    }
}