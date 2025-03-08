using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SecurityPatrol.Core.Models
{
    /// <summary>
    /// Represents a batch of location data points sent from the mobile application to the backend API 
    /// for efficient processing of continuous location tracking data.
    /// </summary>
    public class LocationBatchRequest
    {
        /// <summary>
        /// Gets or sets the user identifier for whom these location data points were collected.
        /// </summary>
        [JsonPropertyName("userId")]
        public string UserId { get; set; }
        
        /// <summary>
        /// Gets or sets the collection of location data points in this batch.
        /// </summary>
        [JsonPropertyName("locations")]
        public IEnumerable<LocationModel> Locations { get; set; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="LocationBatchRequest"/> class.
        /// </summary>
        public LocationBatchRequest()
        {
            Locations = new List<LocationModel>();
        }
    }
}