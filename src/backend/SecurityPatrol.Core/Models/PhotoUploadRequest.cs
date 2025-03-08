using System;
using System.Text.Json.Serialization;

namespace SecurityPatrol.Core.Models
{
    /// <summary>
    /// Represents a request to upload a photo to the backend API.
    /// Contains all necessary metadata about the photo being uploaded, including timestamp, location coordinates, and user information.
    /// </summary>
    public class PhotoUploadRequest
    {
        /// <summary>
        /// Gets or sets the timestamp when the photo was taken.
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the latitude coordinate where the photo was taken.
        /// </summary>
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        /// <summary>
        /// Gets or sets the longitude coordinate where the photo was taken.
        /// </summary>
        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier of the user who took the photo.
        /// </summary>
        [JsonPropertyName("userId")]
        public string UserId { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PhotoUploadRequest"/> class.
        /// </summary>
        public PhotoUploadRequest()
        {
            Timestamp = DateTime.UtcNow;
        }
    }
}