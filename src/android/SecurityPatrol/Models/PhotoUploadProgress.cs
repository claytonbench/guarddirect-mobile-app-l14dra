using System;

namespace SecurityPatrol.Models
{
    /// <summary>
    /// Represents the progress and status of a photo upload operation, including progress percentage, 
    /// current status, and any error messages.
    /// </summary>
    public class PhotoUploadProgress
    {
        /// <summary>
        /// Gets the unique identifier for this upload operation.
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// Gets the current progress percentage (0-100).
        /// </summary>
        public int Progress { get; private set; }

        /// <summary>
        /// Gets the current status of the upload operation (Pending, Uploading, Completed, Error, Cancelled).
        /// </summary>
        public string Status { get; private set; }

        /// <summary>
        /// Gets any error message associated with a failed upload.
        /// </summary>
        public string ErrorMessage { get; private set; }

        /// <summary>
        /// Initializes a new instance of the PhotoUploadProgress class.
        /// </summary>
        /// <param name="id">The unique identifier for this upload operation.</param>
        public PhotoUploadProgress(string id)
        {
            Id = id;
            Progress = 0;
            Status = "Pending";
            ErrorMessage = null;
        }

        /// <summary>
        /// Updates the progress percentage of the photo upload.
        /// </summary>
        /// <param name="progress">The new progress percentage (0-100).</param>
        public void UpdateProgress(int progress)
        {
            // Ensure progress is between 0 and 100
            Progress = Math.Clamp(progress, 0, 100);

            // Update status based on progress
            if (Progress == 0)
            {
                Status = "Pending";
            }
            else if (Progress > 0 && Progress < 100)
            {
                Status = "Uploading";
            }
            else if (Progress == 100)
            {
                Status = "Completed";
            }
        }

        /// <summary>
        /// Sets the upload status to error with an optional error message.
        /// </summary>
        /// <param name="errorMessage">The error message describing the failure.</param>
        public void SetError(string errorMessage)
        {
            Status = "Error";
            ErrorMessage = errorMessage;
        }

        /// <summary>
        /// Resets the progress to initial state for retry operations.
        /// </summary>
        public void Reset()
        {
            Progress = 0;
            Status = "Pending";
            ErrorMessage = null;
        }

        /// <summary>
        /// Sets the upload status to cancelled.
        /// </summary>
        public void Cancel()
        {
            Status = "Cancelled";
            Progress = 0;
        }

        /// <summary>
        /// Creates a copy of this PhotoUploadProgress instance.
        /// </summary>
        /// <returns>A new PhotoUploadProgress instance with the same values.</returns>
        public PhotoUploadProgress Clone()
        {
            var clone = new PhotoUploadProgress(Id);
            clone.Progress = Progress;
            clone.Status = Status;
            clone.ErrorMessage = ErrorMessage;
            return clone;
        }

        /// <summary>
        /// Determines if the upload has completed successfully.
        /// </summary>
        /// <returns>True if the upload is completed, false otherwise.</returns>
        public bool IsCompleted() => Status == "Completed";

        /// <summary>
        /// Determines if the upload has failed with an error.
        /// </summary>
        /// <returns>True if the upload has an error, false otherwise.</returns>
        public bool IsError() => Status == "Error";

        /// <summary>
        /// Determines if the upload has been cancelled.
        /// </summary>
        /// <returns>True if the upload was cancelled, false otherwise.</returns>
        public bool IsCancelled() => Status == "Cancelled";

        /// <summary>.
        /// Determines if the upload is currently in progress.
        /// </summary>
        /// <returns>True if the upload is in progress, false otherwise.</returns>
        public bool IsInProgress() => Status == "Uploading";

        /// <summary>
        /// Determines if the upload is pending (queued but not started).
        /// </summary>
        /// <returns>True if the upload is pending, false otherwise.</returns>
        public bool IsPending() => Status == "Pending";
    }
}