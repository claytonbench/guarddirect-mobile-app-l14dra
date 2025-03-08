using System;
using SQLite;
using SecurityPatrol.Constants;

namespace SecurityPatrol.Database.Entities
{
    /// <summary>
    /// Represents a checkpoint location within a patrol route in the local SQLite database.
    /// This entity stores geographical coordinates and metadata for patrol verification points
    /// and is used for offline operation when network connectivity is unavailable.
    /// </summary>
    [Table(DatabaseConstants.TableCheckpoint)]
    public class CheckpointEntity
    {
        /// <summary>
        /// The unique local identifier for the checkpoint.
        /// </summary>
        [PrimaryKey, AutoIncrement]
        [Column(DatabaseConstants.ColumnId)]
        public int Id { get; set; }

        /// <summary>
        /// The reference to the parent patrol location this checkpoint belongs to.
        /// </summary>
        [Indexed]
        [Column(DatabaseConstants.ColumnLocationId)]
        public int LocationId { get; set; }

        /// <summary>
        /// The descriptive name or identifier of the checkpoint.
        /// </summary>
        [Column(DatabaseConstants.ColumnName)]
        public string Name { get; set; }

        /// <summary>
        /// The geographic latitude coordinate of the checkpoint.
        /// </summary>
        [Column(DatabaseConstants.ColumnLatitude)]
        public double Latitude { get; set; }

        /// <summary>
        /// The geographic longitude coordinate of the checkpoint.
        /// </summary>
        [Column(DatabaseConstants.ColumnLongitude)]
        public double Longitude { get; set; }

        /// <summary>
        /// The timestamp when this checkpoint data was last updated.
        /// </summary>
        [Column(DatabaseConstants.ColumnLastUpdated)]
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// The identifier used by the backend system for this checkpoint.
        /// Used for synchronization between local and remote data.
        /// </summary>
        [Column(DatabaseConstants.ColumnRemoteId)]
        public string RemoteId { get; set; }
    }
}