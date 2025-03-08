using System;
using System.Collections.Generic;

namespace SecurityPatrol.Core.Entities
{
    /// <summary>
    /// Represents a geographic location where security patrols take place.
    /// Contains checkpoints that security personnel must verify during their patrols.
    /// Inherits from AuditableEntity to track creation and modification information.
    /// </summary>
    public class PatrolLocation : AuditableEntity
    {
        /// <summary>
        /// Gets or sets the unique identifier for the patrol location.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the patrol location.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the latitude coordinate of the patrol location.
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// Gets or sets the longitude coordinate of the patrol location.
        /// </summary>
        public double Longitude { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the patrol location data was last updated.
        /// </summary>
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// Gets or sets the remote identifier used to correlate with backend systems.
        /// </summary>
        public string RemoteId { get; set; }

        /// <summary>
        /// Gets or sets the collection of checkpoints associated with this patrol location.
        /// </summary>
        public ICollection<Checkpoint> Checkpoints { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PatrolLocation"/> class.
        /// </summary>
        public PatrolLocation()
        {
            Checkpoints = new List<Checkpoint>();
            LastUpdated = DateTime.UtcNow;
        }
    }
}