using System;
using System.Security.Claims;
using System.Threading.Tasks;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Core.Models;

namespace SecurityPatrol.Core.Interfaces
{
    /// <summary>
    /// Interface that defines the token service operations for generating, validating, and refreshing JWT tokens 
    /// used for authentication and authorization in the Security Patrol application.
    /// </summary>
    public interface ITokenService
    {
        /// <summary>
        /// Generates a JWT token for the specified user with appropriate claims and expiration time.
        /// </summary>
        /// <param name="user">The user for whom to generate the token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the authentication response with JWT token and expiration time.</returns>
        Task<AuthenticationResponse> GenerateTokenAsync(User user);

        /// <summary>
        /// Validates a JWT token to ensure it is properly formatted, not expired, and has a valid signature.
        /// </summary>
        /// <param name="token">The JWT token to validate.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates whether the token is valid.</returns>
        Task<bool> ValidateTokenAsync(string token);

        /// <summary>
        /// Refreshes an existing JWT token by generating a new token with updated expiration time.
        /// </summary>
        /// <param name="token">The existing JWT token to refresh.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the authentication response with refreshed JWT token and new expiration time.</returns>
        Task<AuthenticationResponse> RefreshTokenAsync(string token);

        /// <summary>
        /// Extracts the ClaimsPrincipal from a JWT token for authentication and authorization purposes.
        /// </summary>
        /// <param name="token">The JWT token from which to extract the ClaimsPrincipal.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the ClaimsPrincipal extracted from the token if valid, or null if the token is invalid.</returns>
        Task<ClaimsPrincipal> GetPrincipalFromTokenAsync(string token);

        /// <summary>
        /// Extracts the user ID claim from a JWT token.
        /// </summary>
        /// <param name="token">The JWT token from which to extract the user ID.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the user ID extracted from the token if valid, or null if the token is invalid or doesn't contain a user ID claim.</returns>
        Task<string> GetUserIdFromTokenAsync(string token);
    }
}