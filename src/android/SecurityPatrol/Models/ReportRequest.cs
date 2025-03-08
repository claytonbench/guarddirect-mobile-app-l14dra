using System;
using Newtonsoft.Json; // v13.0+
using SecurityPatrol.Models;

namespace SecurityPatrol.Models
{
    /// <summary>
    /// Represents the data transfer object used for sending activity report submissions to the backend API.
    /// Contains the text content, timestamp, and location data required for report creation.
    /// </summary>
    public class ReportRequest
    {
        /// <summary>
        /// The content of the activity report.
        /// </summary>
        [JsonProperty("text")]
        public string Text { get; set; }

        /// <summary>
        /// The date and time when the report was created.
        /// </summary>
        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// The location data where the report was created.
        /// </summary>
        [JsonProperty("location")]
        public LocationData Location { get; set; }

        /// <summary>
        /// Default constructor for the ReportRequest class.
        /// Initializes default values for a new report request.
        /// </summary>
        public ReportRequest()
        {
            Timestamp = DateTime.UtcNow;
            Location = new LocationData();
        }

        /// <summary>
        /// Creates a ReportRequest from a ReportModel.
        /// </summary>
        /// <param name="model">The ReportModel to convert.</param>
        /// <returns>A new ReportRequest instance with properties copied from the model.</returns>
        /// <exception cref="ArgumentNullException">Thrown if model is null.</exception>
        public static ReportRequest FromReportModel(ReportModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            return new ReportRequest
            {
                Text = model.Text,
                Timestamp = model.Timestamp,
                Location = new LocationData
                {
                    Latitude = model.Latitude,
                    Longitude = model.Longitude
                }
            };
        }
    }

    /// <summary>
    /// Represents location coordinates for the report submission.
    /// </summary>
    public class LocationData
    {
        /// <summary>
        /// The latitude coordinate where the report was created.
        /// </summary>
        [JsonProperty("latitude")]
        public double Latitude { get; set; }

        /// <summary>
        /// The longitude coordinate where the report was created.
        /// </summary>
        [JsonProperty("longitude")]
        public double Longitude { get; set; }

        /// <summary>
        /// Default constructor for the LocationData class.
        /// Initializes default values for location coordinates.
        /// </summary>
        public LocationData()
        {
            Latitude = 0;
            Longitude = 0;
        }
    }
}