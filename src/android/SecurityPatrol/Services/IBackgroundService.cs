using System.Threading.Tasks;

namespace SecurityPatrol.Services
{
    /// <summary>
    /// Interface that defines the contract for background services in the Security Patrol application.
    /// This interface provides a standardized way to start, stop, and check the status of background
    /// processes that need to continue running even when the application is minimized, such as location tracking.
    /// </summary>
    public interface IBackgroundService
    {
        /// <summary>
        /// Starts the background service.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown when the service is already running.</exception>
        /// <exception cref="System.UnauthorizedAccessException">Thrown when required permissions are not granted.</exception>
        Task Start();

        /// <summary>
        /// Stops the background service.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown when the service is not running.</exception>
        Task Stop();

        /// <summary>
        /// Gets a value indicating whether the background service is currently running.
        /// </summary>
        /// <returns>True if the service is running, false otherwise.</returns>
        bool IsRunning();
    }
}