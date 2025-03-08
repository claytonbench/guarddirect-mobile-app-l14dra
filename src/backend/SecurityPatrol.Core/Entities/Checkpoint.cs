using System;
using System.Collections.Generic;

namespace SecurityPatrol.Core.Entities
{
    /// <summary>
    /// Represents a specific geographic location that security personnel must verify during their patrols.
    /// Inherits from AuditableEntity to track creation and modification information.
    /// </summary>
    public class Checkpoint : AuditableEntity
    {
        /// <summary>
        /// Gets or sets the unique identifier for the checkpoint.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the patrol location that this checkpoint belongs to.
        /// </summary>
        public int LocationId { get; set; }

        /// <summary>
        /// Gets or sets the name or description of the checkpoint.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the latitude coordinate of the checkpoint.
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// Gets or sets the longitude coordinate of the checkpoint.
        /// </summary>
        public double Longitude { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this checkpoint was last updated.
        /// </summary>
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// Gets or sets the remote identifier for this checkpoint from external systems.
        /// </summary>
        public string RemoteId { get; set; }

        /// <summary>
        /// Gets or sets the patrol location that this checkpoint belongs to.
        /// Navigation property for entity relationship.
        /// </summary>
        public PatrolLocation PatrolLocation { get; set; }

        /// <summary>
        /// Gets or sets the collection of checkpoint verifications associated with this checkpoint.
        /// Navigation property for entity relationship.
        /// </summary>
        public ICollection<CheckpointVerification> Verifications { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Checkpoint"/> class.
        /// </summary>
        public Checkpoint()
        {
            Verifications = new List<CheckpointVerification>();
            LastUpdated = DateTime.UtcNow;
        }
    }
}