using System;
using System.Threading.Tasks;
using SecurityPatrol.Models;

namespace SecurityPatrol.Services
{
    /// <summary>
    /// Interface defining the contract for a service that manages and provides access to the user's authentication state
    /// throughout the application. It serves as a centralized source of truth for authentication status 
    /// and notifies subscribers when the state changes.
    /// </summary>
    public interface IAuthenticationStateProvider
    {
        /// <summary>
        /// Event that is raised when the authentication state changes.
        /// </summary>
        event EventHandler StateChanged;

        /// <summary>
        /// Retrieves the current authentication state of the user.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation, containing the current authentication state.</returns>
        Task<AuthState> GetCurrentState();

        /// <summary>
        /// Updates the current authentication state and notifies subscribers.
        /// </summary>
        /// <param name="state">The new authentication state.</param>
        void UpdateState(AuthState state);

        /// <summary>
        /// Checks if the user is currently authenticated.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation, containing a boolean value 
        /// indicating whether the user is authenticated (true) or not (false).</returns>
        Task<bool> IsAuthenticated();

        /// <summary>
        /// Notifies subscribers that the authentication state has changed.
        /// </summary>
        void NotifyStateChanged();
    }
}