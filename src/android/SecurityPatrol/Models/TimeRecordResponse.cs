using System;
using Newtonsoft.Json; // v13.0+

namespace SecurityPatrol.Models
{
    /// <summary>
    /// Response model for clock in/out operations returned from the backend API.
    /// Contains the record ID and status of the time tracking operation.
    /// </summary>
    public class TimeRecordResponse
    {
        /// <summary>
        /// Gets or sets the unique identifier for the time record returned by the backend API.
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the status of the time record operation.
        /// Typical values include "success", "failed", or more specific error codes.
        /// </summary>
        [JsonProperty("status")]
        public string Status { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeRecordResponse"/> class.
        /// </summary>
        public TimeRecordResponse()
        {
            Id = string.Empty;
            Status = string.Empty;
        }

        /// <summary>
        /// Determines if the response indicates a successful operation.
        /// </summary>
        /// <returns>True if the operation was successful, false otherwise.</returns>
        public bool IsSuccess()
        {
            return !string.IsNullOrEmpty(Status) && 
                   string.Equals(Status, "success", StringComparison.OrdinalIgnoreCase);
        }
    }
}