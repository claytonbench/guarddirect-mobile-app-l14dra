using System.Collections.Generic;
using Newtonsoft.Json;

namespace SecurityPatrol.Models
{
    /// <summary>
    /// Represents the response from the location synchronization API, containing information about 
    /// which location records were successfully processed and which ones failed. This enables the 
    /// client to implement targeted retry logic for failed records.
    /// </summary>
    public class LocationSyncResponse
    {
        /// <summary>
        /// Gets or sets the collection of location record IDs that were successfully synchronized.
        /// </summary>
        [JsonProperty("syncedIds")]
        public IEnumerable<int> SyncedIds { get; set; }

        /// <summary>
        /// Gets or sets the collection of location record IDs that failed to synchronize.
        /// </summary>
        [JsonProperty("failedIds")]
        public IEnumerable<int> FailedIds { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocationSyncResponse"/> class.
        /// </summary>
        public LocationSyncResponse()
        {
            SyncedIds = new List<int>();
            FailedIds = new List<int>();
        }

        /// <summary>
        /// Determines if there were any failures during synchronization.
        /// </summary>
        /// <returns>True if there are any failed IDs, false otherwise.</returns>
        public bool HasFailures()
        {
            return FailedIds != null && System.Linq.Enumerable.Any(FailedIds);
        }

        /// <summary>
        /// Gets the count of failed location records.
        /// </summary>
        /// <returns>The number of failed location records.</returns>
        public int GetFailureCount()
        {
            return FailedIds == null ? 0 : System.Linq.Enumerable.Count(FailedIds);
        }

        /// <summary>
        /// Gets the count of successfully synchronized location records.
        /// </summary>
        /// <returns>The number of successfully synchronized location records.</returns>
        public int GetSuccessCount()
        {
            return SyncedIds == null ? 0 : System.Linq.Enumerable.Count(SyncedIds);
        }
    }
}