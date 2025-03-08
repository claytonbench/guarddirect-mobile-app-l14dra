using System;
using System.Text.Json.Serialization;

namespace SecurityPatrol.Core.Models
{
    /// <summary>
    /// Response model for clock in/out operations returned from the backend API.
    /// This model encapsulates the server response data for time record operations including 
    /// the record ID and status, and is used to communicate the result of time tracking 
    /// operations back to the client.
    /// </summary>
    public class TimeRecordResponse
    {
        /// <summary>
        /// Gets or sets the record identifier returned from the server.
        /// This ID can be used for future reference to this specific time record.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the status of the operation (e.g., "success", "failed").
        /// This indicates whether the clock in/out operation was successfully processed by the server.
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; }

        /// <summary>
        /// Default constructor for TimeRecordResponse
        /// </summary>
        public TimeRecordResponse()
        {
            Id = string.Empty;
            Status = string.Empty;
        }

        /// <summary>
        /// Determines if the response indicates a successful operation
        /// </summary>
        /// <returns>True if the operation was successful, false otherwise</returns>
        public bool IsSuccess()
        {
            return string.Equals(Status, "success", StringComparison.OrdinalIgnoreCase);
        }
    }
}