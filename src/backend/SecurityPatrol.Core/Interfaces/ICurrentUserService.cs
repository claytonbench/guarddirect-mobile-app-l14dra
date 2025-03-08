using System; // For DateTime - v8.0.0

namespace SecurityPatrol.Core.Interfaces
{
    /// <summary>
    /// Provides access to information about the currently authenticated user throughout the application.
    /// This abstraction allows other components to access user details without direct dependency on the HTTP context.
    /// </summary>
    public interface ICurrentUserService
    {
        /// <summary>
        /// Gets the ID of the currently authenticated user.
        /// </summary>
        /// <returns>The user ID of the currently authenticated user, or null if not authenticated.</returns>
        string GetUserId();
        
        /// <summary>
        /// Gets the phone number of the currently authenticated user.
        /// </summary>
        /// <returns>The phone number of the currently authenticated user, or null if not authenticated.</returns>
        string GetPhoneNumber();
        
        /// <summary>
        /// Determines whether the current request is from an authenticated user.
        /// </summary>
        /// <returns>True if the user is authenticated, false otherwise.</returns>
        bool IsAuthenticated();
        
        /// <summary>
        /// Gets the timestamp of when the user was last authenticated.
        /// </summary>
        /// <returns>The timestamp of the last successful authentication, or null if not authenticated.</returns>
        DateTime? GetLastAuthenticated();
    }
}