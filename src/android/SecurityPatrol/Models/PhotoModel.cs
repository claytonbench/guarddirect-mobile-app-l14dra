using System;
using SecurityPatrol.Database.Entities;

namespace SecurityPatrol.Models
{
    /// <summary>
    /// Represents a photo captured within the Security Patrol application,
    /// including metadata and synchronization status.
    /// </summary>
    public class PhotoModel
    {
        /// <summary>
        /// Gets or sets the unique identifier for the photo.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the user ID who captured the photo.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the photo was captured.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the latitude where the photo was captured.
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// Gets or sets the longitude where the photo was captured.
        /// </summary>
        public double Longitude { get; set; }

        /// <summary>
        /// Gets or sets the file path where the photo is stored on the device.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the photo has been synchronized with the backend.
        /// </summary>
        public bool IsSynced { get; set; }

        /// <summary>
        /// Gets or sets the remote identifier assigned by the backend after synchronization.
        /// </summary>
        public string RemoteId { get; set; }

        /// <summary>
        /// Gets or sets the synchronization progress as a percentage (0-100).
        /// Used to track progress during photo upload.
        /// </summary>
        public int SyncProgress { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PhotoModel"/> class.
        /// </summary>
        public PhotoModel()
        {
            Timestamp = DateTime.Now;
            IsSynced = false;
            SyncProgress = 0;
        }

        /// <summary>
        /// Creates a PhotoModel from a PhotoEntity.
        /// </summary>
        /// <param name="entity">The entity to convert from.</param>
        /// <returns>A new PhotoModel instance with properties copied from the PhotoEntity.</returns>
        /// <exception cref="ArgumentNullException">Thrown when entity is null.</exception>
        public static PhotoModel FromEntity(PhotoEntity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            return new PhotoModel
            {
                Id = entity.Id,
                UserId = entity.UserId,
                Timestamp = entity.Timestamp,
                Latitude = entity.Latitude,
                Longitude = entity.Longitude,
                FilePath = entity.FilePath,
                IsSynced = entity.IsSynced,
                RemoteId = entity.RemoteId,
                SyncProgress = entity.SyncProgress
            };
        }

        /// <summary>
        /// Converts this PhotoModel to a PhotoEntity.
        /// </summary>
        /// <returns>A new PhotoEntity instance with properties copied from this PhotoModel.</returns>
        public PhotoEntity ToEntity()
        {
            return new PhotoEntity
            {
                Id = Id,
                UserId = UserId,
                Timestamp = Timestamp,
                Latitude = Latitude,
                Longitude = Longitude,
                FilePath = FilePath,
                IsSynced = IsSynced,
                RemoteId = RemoteId,
                SyncProgress = SyncProgress
            };
        }
    }
}