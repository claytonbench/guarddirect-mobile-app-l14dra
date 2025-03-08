using System; // System.Text.Json 8.0+
using System.Text.Json.Serialization; // System.Text.Json 8.0+

namespace SecurityPatrol.Core.Models
{
    /// <summary>
    /// Request model for clock in/out operations sent to the backend API.
    /// This model encapsulates the data needed for time record operations including
    /// the record type (clock in/out), timestamp, and location information.
    /// </summary>
    public class TimeRecordRequest
    {
        /// <summary>
        /// Gets or sets the type of time record (e.g., "ClockIn" or "ClockOut").
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the time record was created.
        /// Always stored in UTC for consistent timezone handling.
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the location information where the time record was created.
        /// </summary>
        [JsonPropertyName("location")]
        public LocationModel Location { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeRecordRequest"/> class.
        /// </summary>
        public TimeRecordRequest()
        {
            Type = string.Empty;
            Timestamp = DateTime.UtcNow;
            Location = new LocationModel();
        }
    }

    /// <summary>
    /// Represents location information for a time record request.
    /// Contains latitude and longitude coordinates captured at the time of the clock in/out event.
    /// </summary>
    public class LocationModel
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
        /// Initializes a new instance of the <see cref="LocationModel"/> class.
        /// </summary>
        public LocationModel()
        {
            Latitude = 0.0;
            Longitude = 0.0;
        }
    }
}