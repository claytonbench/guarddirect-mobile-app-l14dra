using System;
using System.Threading.Tasks;
using SecurityPatrol.Models;

namespace SecurityPatrol.Services
{
    /// <summary>
    /// Interface that defines the contract for location tracking services in the Security Patrol application.
    /// This interface provides methods for starting and stopping GPS tracking, retrieving the current location,
    /// and notifying subscribers of location changes through events.
    /// </summary>
    public interface ILocationService
    {
        /// <summary>
        /// Starts continuous location tracking.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when location permissions are not granted.</exception>
        Task StartTracking();

        /// <summary>
        /// Stops continuous location tracking.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task StopTracking();

        /// <summary>
        /// Gets the current device location.
        /// </summary>
        /// <returns>A task that returns the current location.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when location permissions are not granted.</exception>
        /// <exception cref="TimeoutException">Thrown when unable to get location within timeout period.</exception>
        Task<LocationModel> GetCurrentLocation();

        /// <summary>
        /// Gets a value indicating whether location tracking is currently active.
        /// </summary>
        bool IsTracking { get; }

        /// <summary>
        /// Event that is raised when the device's location changes.
        /// </summary>
        event EventHandler<LocationChangedEventArgs> LocationChanged;
    }
}