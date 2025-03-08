using System;
using Newtonsoft.Json;

namespace SecurityPatrol.Models
{
    /// <summary>
    /// Represents the response returned from the backend API after a report submission operation.
    /// Contains the server-assigned identifier and status of the operation.
    /// </summary>
    public class ReportResponse
    {
        /// <summary>
        /// Gets or sets the server-assigned identifier for the report.
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the status of the report submission operation.
        /// </summary>
        [JsonProperty("status")]
        public string Status { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReportResponse"/> class.
        /// </summary>
        public ReportResponse()
        {
            Id = string.Empty;
            Status = string.Empty;
        }
    }
}