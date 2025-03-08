using System;

namespace SecurityPatrol.Core.Entities
{
    /// <summary>
    /// Represents a record of a security officer verifying their presence at a specific checkpoint during a patrol.
    /// Inherits from AuditableEntity to track creation and modification information.
    /// </summary>
    public class CheckpointVerification : AuditableEntity
    {
        /// <summary>
        /// Gets or sets the unique identifier for the checkpoint verification record.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the user who performed the checkpoint verification.
        /// Foreign key to the User entity.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the checkpoint that was verified.
        /// Foreign key to the Checkpoint entity.
        /// </summary>
        public int CheckpointId { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the checkpoint verification occurred.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the latitude coordinate where the verification was performed.
        /// Used to validate proximity to the checkpoint location.
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// Gets or sets the longitude coordinate where the verification was performed.
        /// Used to validate proximity to the checkpoint location.
        /// </summary>
        public double Longitude { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this verification record has been 
        /// synchronized with the backend system.
        /// </summary>
        public bool IsSynced { get; set; }

        /// <summary>
        /// Gets or sets the identifier assigned to this verification record by the backend system.
        /// Used for tracking synchronization status and avoiding duplicate records.
        /// </summary>
        public string RemoteId { get; set; }

        /// <summary>
        /// Gets or sets the user who performed the checkpoint verification.
        /// Navigation property for entity relationship.
        /// </summary>
        public User User { get; set; }

        /// <summary>
        /// Gets or sets the checkpoint that was verified.
        /// Navigation property for entity relationship.
        /// </summary>
        public Checkpoint Checkpoint { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CheckpointVerification"/> class.
        /// </summary>
        public CheckpointVerification()
        {
            Timestamp = DateTime.UtcNow;
            IsSynced = false;
        }
    }
}