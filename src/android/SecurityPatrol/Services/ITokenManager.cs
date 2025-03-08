using System; // For basic .NET types - 8.0.0
using System.Threading.Tasks; // For Task-based asynchronous operations - 8.0.0

namespace SecurityPatrol.Services
{
    /// <summary>
    /// Interface that defines the contract for token management operations in the Security Patrol application.
    /// Handles secure storage, retrieval, and validation of authentication tokens.
    /// </summary>
    public interface ITokenManager
    {
        /// <summary>
        /// Securely stores the authentication token in the device's secure storage.
        /// </summary>
        /// <param name="token">The authentication token to store.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task StoreToken(string token);

        /// <summary>
        /// Retrieves the stored authentication token from secure storage.
        /// </summary>
        /// <returns>The authentication token if available, otherwise null.</returns>
        Task<string> RetrieveToken();

        /// <summary>
        /// Removes the stored authentication token from secure storage.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ClearToken();

        /// <summary>
        /// Checks if the stored token exists and is not expired.
        /// </summary>
        /// <returns>True if a valid token exists, otherwise false.</returns>
        Task<bool> IsTokenValid();

        /// <summary>
        /// Retrieves the expiration time of the stored token.
        /// </summary>
        /// <returns>The token expiration time if available, otherwise null.</returns>
        Task<DateTime?> GetTokenExpiryTime();

        /// <summary>
        /// Checks if the token is approaching its expiration time and should be refreshed.
        /// </summary>
        /// <returns>True if the token is expiring soon, otherwise false.</returns>
        Task<bool> IsTokenExpiringSoon();
    }
}