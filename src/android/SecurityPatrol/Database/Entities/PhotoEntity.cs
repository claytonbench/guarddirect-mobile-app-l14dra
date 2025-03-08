using System;
using SQLite;
using SecurityPatrol.Constants;

namespace SecurityPatrol.Database.Entities
{
    /// <summary>
    /// Represents a photo in the local SQLite database. Contains properties for identification,
    /// metadata, file path, location, and synchronization status with the backend.
    /// </summary>
    [Table(DatabaseConstants.TablePhoto)]
    public class PhotoEntity
    {
        /// <summary>
        /// Gets or sets the unique identifier for the photo.
        /// </summary>
        [PrimaryKey]
        [Column(DatabaseConstants.ColumnId)]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the user ID who captured the photo.
        /// </summary>
        [Indexed]
        [Column(DatabaseConstants.ColumnUserId)]
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the photo was captured.
        /// </summary>
        [Column(DatabaseConstants.ColumnTimestamp)]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the latitude where the photo was captured.
        /// </summary>
        [Column(DatabaseConstants.ColumnLatitude)]
        public double Latitude { get; set; }

        /// <summary>
        /// Gets or sets the longitude where the photo was captured.
        /// </summary>
        [Column(DatabaseConstants.ColumnLongitude)]
        public double Longitude { get; set; }

        /// <summary>
        /// Gets or sets the file path where the photo is stored on the device.
        /// </summary>
        [Column(DatabaseConstants.ColumnFilePath)]
        public string FilePath { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the photo has been synchronized with the backend.
        /// </summary>
        [Indexed]
        [Column(DatabaseConstants.ColumnIsSynced)]
        public bool IsSynced { get; set; }

        /// <summary>
        /// Gets or sets the remote identifier assigned by the backend after synchronization.
        /// </summary>
        [Column(DatabaseConstants.ColumnRemoteId)]
        public string RemoteId { get; set; }

        /// <summary>
        /// Gets or sets the synchronization progress as a percentage (0-100).
        /// Used to track progress during photo upload.
        /// </summary>
        [Column(DatabaseConstants.ColumnSyncProgress)]
        public int SyncProgress { get; set; }
    }
}