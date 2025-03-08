using System; // .NET 8.0+ - Provides access to fundamental .NET types including DateTime
using System.Text.Json.Serialization; // .NET 8.0+ - Provides JSON serialization attributes for API communication

namespace SecurityPatrol.Core.Models
{
    /// <summary>
    /// Represents the data transfer object used for receiving activity report submissions from the mobile application.
    /// Contains the text content, timestamp, and location data required for report creation and validation.
    /// </summary>
    public class ReportRequest
    {
        /// <summary>
        /// Gets or sets the text content of the activity report.
        /// Must not be empty and must not exceed 500 characters.
        /// </summary>
        [JsonPropertyName("text")]
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the report was created.
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the location data where the report was created.
        /// </summary>
        [JsonPropertyName("location")]
        public LocationData Location { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReportRequest"/> class.
        /// </summary>
        public ReportRequest()
        {
            Text = string.Empty;
            Timestamp = DateTime.UtcNow;
            Location = new LocationData();
        }
    }

    /// <summary>
    /// Represents location coordinates for the report submission.
    /// </summary>
    public class LocationData
    {
        /// <summary>
        /// Gets or sets the latitude coordinate.
        /// </summary>
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        /// <summary>
        /// Gets or sets the longitude coordinate.
        /// </summary>
        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocationData"/> class.
        /// </summary>
        public LocationData()
        {
            Latitude = 0;
            Longitude = 0;
        }
    }
}