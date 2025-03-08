using System; // Version 8.0+

namespace SecurityPatrol.Models
{
    /// <summary>
    /// Event arguments class that encapsulates location data for location change events.
    /// Used when notifying subscribers about changes in the user's location during tracking.
    /// </summary>
    public class LocationChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the location data associated with this event.
        /// </summary>
        public LocationModel Location { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocationChangedEventArgs"/> class with the specified location.
        /// </summary>
        /// <param name="location">The location data to encapsulate in this event.</param>
        public LocationChangedEventArgs(LocationModel location)
        {
            Location = location ?? throw new ArgumentNullException(nameof(location));
        }
    }
}