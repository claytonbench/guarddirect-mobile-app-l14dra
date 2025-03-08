using System.Threading.Tasks;
using SecurityPatrol.Models;

namespace SecurityPatrol.Services
{
    /// <summary>
    /// Defines the contract for authentication services in the Security Patrol application.
    /// Handles phone number verification, authentication token management, and user session state.
    /// </summary>
    public interface IAuthenticationService
    {
        /// <summary>
        /// Requests a verification code to be sent to the specified phone number.
        /// </summary>
        /// <param name="phoneNumber">The phone number to send the verification code to.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a boolean value
        /// indicating whether the verification code was successfully requested.</returns>
        Task<bool> RequestVerificationCode(string phoneNumber);

        /// <summary>
        /// Verifies the code sent to the user's phone number and completes the authentication process.
        /// </summary>
        /// <param name="code">The verification code received by the user.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a boolean value
        /// indicating whether the verification was successful.</returns>
        Task<bool> VerifyCode(string code);

        /// <summary>
        /// Retrieves the current authentication state of the user.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the current authentication state.</returns>
        Task<AuthState> GetAuthenticationState();

        /// <summary>
        /// Logs out the current user by clearing authentication tokens and state.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task Logout();

        /// <summary>
        /// Attempts to refresh the authentication token when it's approaching expiration.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a boolean value
        /// indicating whether the token was successfully refreshed.</returns>
        Task<bool> RefreshToken();
    }
}