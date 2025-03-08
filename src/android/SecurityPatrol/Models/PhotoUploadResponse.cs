using System;

namespace SecurityPatrol.Models
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
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the status of the upload operation.
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PhotoUploadResponse"/> class.
        /// </summary>
        public PhotoUploadResponse()
        {
            // Initialize properties with default values
            Id = string.Empty;
            Status = string.Empty;
        }

        /// <summary>
        /// Determines if the upload was successful based on the Status value.
        /// </summary>
        /// <returns>True if the upload was successful, false otherwise.</returns>
        public bool IsSuccess()
        {
            if (string.IsNullOrEmpty(Status))
            {
                return false;
            }

            // Check if Status equals 'Success' or 'Succeeded' (case-insensitive)
            return Status.Equals("Success", StringComparison.OrdinalIgnoreCase) || 
                   Status.Equals("Succeeded", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Updates a PhotoModel with the server response data.
        /// </summary>
        /// <param name="photoModel">The photo model to update.</param>
        /// <exception cref="ArgumentNullException">Thrown when photoModel is null.</exception>
        public void UpdatePhotoModel(PhotoModel photoModel)
        {
            if (photoModel == null)
            {
                throw new ArgumentNullException(nameof(photoModel));
            }

            photoModel.RemoteId = Id;
            photoModel.IsSynced = IsSuccess();
        }
    }
}