using SQLite;
using System;
using SecurityPatrol.Constants;

namespace SecurityPatrol.Database.Entities
{
    /// <summary>
    /// Represents a verification record of a security patrol checkpoint in the local SQLite database.
    /// Contains properties for identification, user reference, checkpoint reference, timestamp,
    /// location coordinates, and synchronization status.
    /// </summary>
    [Table(DatabaseConstants.TableCheckpointVerification)]
    public class CheckpointVerificationEntity
    {
        /// <summary>
        /// Gets or sets the unique identifier for the checkpoint verification record.
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the user identifier who performed the verification.
        /// </summary>
        [Indexed]
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the checkpoint identifier that was verified.
        /// </summary>
        [Indexed]
        public int CheckpointId { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the checkpoint was verified.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the latitude coordinate where the verification occurred.
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// Gets or sets the longitude coordinate where the verification occurred.
        /// </summary>
        public double Longitude { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this verification has been synchronized with the backend.
        /// </summary>
        [Indexed]
        public bool IsSynced { get; set; }

        /// <summary>
        /// Gets or sets the remote identifier assigned by the backend after synchronization.
        /// </summary>
        public string RemoteId { get; set; }

        /// <summary>
        /// Default constructor for the CheckpointVerificationEntity class
        /// </summary>
        public CheckpointVerificationEntity()
        {
            // Initialize properties with default values
            IsSynced = false;
        }
    }
}