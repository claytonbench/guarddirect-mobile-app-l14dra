using System.Collections.Generic; // Version 8.0+
using Newtonsoft.Json; // Version 13.0+
using SecurityPatrol.Models; // Internal import

namespace SecurityPatrol.Models
{
    /// <summary>
    /// Model class that encapsulates a collection of location data points to be sent to the backend API in a single request.
    /// This optimizes network usage and server processing by batching multiple location updates together.
    /// </summary>
    public class LocationBatchRequest
    {
        /// <summary>
        /// Gets or sets the unique identifier of the user who generated these location points.
        /// </summary>
        [JsonProperty("userId")]
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the collection of location data points to be sent to the backend.
        /// </summary>
        [JsonProperty("locations")]
        public IEnumerable<LocationModel> Locations { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocationBatchRequest"/> class.
        /// </summary>
        public LocationBatchRequest()
        {
            Locations = new List<LocationModel>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocationBatchRequest"/> class with the specified user ID and locations.
        /// </summary>
        /// <param name="userId">The user ID associated with these location points.</param>
        /// <param name="locations">The collection of location data points.</param>
        public LocationBatchRequest(string userId, IEnumerable<LocationModel> locations)
        {
            UserId = userId;
            Locations = locations ?? new List<LocationModel>();
        }
    }
}