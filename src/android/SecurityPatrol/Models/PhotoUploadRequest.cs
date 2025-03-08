using System; // .NET 8.0+

namespace SecurityPatrol.Models
{
    /// <summary>
    /// Represents a request to upload a photo to the backend API. Contains all necessary metadata about the photo being uploaded.
    /// </summary>
    public class PhotoUploadRequest
    {
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
        /// Gets or sets the user ID who captured the photo.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PhotoUploadRequest"/> class.
        /// </summary>
        public PhotoUploadRequest()
        {
            // Initialize with default values
            Timestamp = DateTime.Now;
        }

        /// <summary>
        /// Creates a PhotoUploadRequest from a PhotoModel.
        /// </summary>
        /// <param name="photoModel">The photo model to convert from.</param>
        /// <returns>A new PhotoUploadRequest instance with properties copied from the PhotoModel.</returns>
        /// <exception cref="ArgumentNullException">Thrown when photoModel is null.</exception>
        public static PhotoUploadRequest FromPhotoModel(PhotoModel photoModel)
        {
            if (photoModel == null)
            {
                throw new ArgumentNullException(nameof(photoModel));
            }

            return new PhotoUploadRequest
            {
                Timestamp = photoModel.Timestamp,
                Latitude = photoModel.Latitude,
                Longitude = photoModel.Longitude,
                UserId = photoModel.UserId
            };
        }
    }
}