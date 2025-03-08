using System;
using System.Text.Json.Serialization;

namespace SecurityPatrol.Core.Models
{
    /// <summary>
    /// Represents a response from the backend API after a photo upload operation.
    /// Contains information about the success status and the server-assigned identifier for the uploaded photo.
    /// </summary>
    public class PhotoUploadResponse
    {
        /// <summary>
        /// Gets or sets the server-assigned identifier for the uploaded photo.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the status of the photo upload operation.
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PhotoUploadResponse"/> class.
        /// </summary>
        public PhotoUploadResponse()
        {
            Id = string.Empty;
            Status = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PhotoUploadResponse"/> class with specified values.
        /// </summary>
        /// <param name="id">The server-assigned identifier for the uploaded photo.</param>
        /// <param name="status">The status of the photo upload operation.</param>
        public PhotoUploadResponse(string id, string status)
        {
            Id = id ?? string.Empty;
            Status = status ?? string.Empty;
        }
    }
}