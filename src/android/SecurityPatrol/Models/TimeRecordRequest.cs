using Newtonsoft.Json;  // Version 13.0+
using System;  // Version 8.0+

namespace SecurityPatrol.Models
{
    /// <summary>
    /// Request model for clock in/out operations sent to the backend API.
    /// This model encapsulates the data needed for time record operations
    /// including the record type, timestamp, and location information.
    /// </summary>
    public class TimeRecordRequest
    {
        /// <summary>
        /// Gets or sets the type of time record (e.g., "ClockIn", "ClockOut").
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the clock event occurred.
        /// </summary>
        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the location information where the clock event occurred.
        /// </summary>
        [JsonProperty("location")]
        public LocationInfo Location { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeRecordRequest"/> class.
        /// </summary>
        public TimeRecordRequest()
        {
            Type = string.Empty;
            Timestamp = DateTime.UtcNow;
            Location = new LocationInfo();
        }

        /// <summary>
        /// Creates a TimeRecordRequest from a TimeRecordModel.
        /// </summary>
        /// <param name="model">The model to convert.</param>
        /// <returns>A new TimeRecordRequest populated with data from the model.</returns>
        /// <exception cref="ArgumentNullException">Thrown when model is null.</exception>
        public static TimeRecordRequest FromTimeRecordModel(TimeRecordModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            return new TimeRecordRequest
            {
                Type = model.Type,
                Timestamp = model.Timestamp,
                Location = new LocationInfo(model.Latitude, model.Longitude)
            };
        }
    }

    /// <summary>
    /// Represents location information for a time record request.
    /// </summary>
    public class LocationInfo
    {
        /// <summary>
        /// Gets or sets the latitude coordinate.
        /// </summary>
        [JsonProperty("latitude")]
        public double Latitude { get; set; }

        /// <summary>
        /// Gets or sets the longitude coordinate.
        /// </summary>
        [JsonProperty("longitude")]
        public double Longitude { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocationInfo"/> class.
        /// </summary>
        public LocationInfo()
        {
            Latitude = 0.0;
            Longitude = 0.0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocationInfo"/> class with specified coordinates.
        /// </summary>
        /// <param name="latitude">The latitude coordinate.</param>
        /// <param name="longitude">The longitude coordinate.</param>
        public LocationInfo(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }
    }
}