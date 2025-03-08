using System;
using System.Threading.Tasks;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Core.Models;

namespace SecurityPatrol.Core.Interfaces
{
    /// <summary>
    /// Defines the interface for authentication operations in the Security Patrol application.
    /// This interface handles the two-step phone verification process, token management, and user authentication.
    /// </summary>
    public interface IAuthenticationService
    {
        /// <summary>
        /// Requests a verification code for the provided phone number.
        /// The code is sent via SMS to the user's phone.
        /// </summary>
        /// <param name="request">The authentication request containing the phone number.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a verification ID that can be used to validate the code.</returns>
        Task<string> RequestVerificationCodeAsync(AuthenticationRequest request);

        /// <summary>
        /// Verifies the code provided by the user against the code that was sent to their phone number.
        /// </summary>
        /// <param name="request">The verification request containing the phone number and code.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the authentication response with JWT token if verification is successful.</returns>
        Task<AuthenticationResponse> VerifyCodeAsync(VerificationRequest request);

        /// <summary>
        /// Refreshes an existing authentication token to extend the session without requiring re-verification.
        /// </summary>
        /// <param name="token">The current authentication token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the new authentication response with refreshed JWT token.</returns>
        Task<AuthenticationResponse> RefreshTokenAsync(string token);

        /// <summary>
        /// Retrieves a user by their phone number.
        /// </summary>
        /// <param name="phoneNumber">The phone number to search for.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the user if found, or null if not found.</returns>
        Task<User> GetUserByPhoneNumberAsync(string phoneNumber);

        /// <summary>
        /// Validates an authentication token to ensure it is valid and not expired.
        /// </summary>
        /// <param name="token">The authentication token to validate.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates whether the token is valid.</returns>
        Task<bool> ValidateTokenAsync(string token);
    }
}