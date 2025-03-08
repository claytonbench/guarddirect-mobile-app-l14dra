using System.Text.Json.Serialization; // .NET 8.0+

namespace SecurityPatrol.Core.Models
{
    /// <summary>
    /// Represents the data transfer object returned to the mobile application after a report submission.
    /// Contains the ID and status of the created or processed report for client-side tracking and synchronization.
    /// </summary>
    public class ReportResponse
    {
        /// <summary>
        /// Gets or sets the unique identifier of the report.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the status of the report processing operation.
        /// Typically "Success" for successful operations or an error message for failures.
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReportResponse"/> class.
        /// </summary>
        public ReportResponse()
        {
            Id = string.Empty;
            Status = string.Empty;
        }

        /// <summary>
        /// Creates a successful response with the specified report ID.
        /// </summary>
        /// <param name="id">The unique identifier of the created report.</param>
        /// <returns>A successful response with the specified ID.</returns>
        public static ReportResponse CreateSuccess(string id)
        {
            return new ReportResponse
            {
                Id = id,
                Status = "Success"
            };
        }
    }
}