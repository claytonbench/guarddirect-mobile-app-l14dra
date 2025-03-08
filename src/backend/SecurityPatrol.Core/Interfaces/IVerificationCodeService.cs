using System;
using System.Threading.Tasks;

namespace SecurityPatrol.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for verification code operations in the Security Patrol application.
    /// This service is responsible for generating, storing, validating, and managing the lifecycle
    /// of verification codes used in the two-step phone number authentication process.
    /// </summary>
    public interface IVerificationCodeService
    {
        /// <summary>
        /// Generates a random verification code for the specified phone number.
        /// </summary>
        /// <param name="phoneNumber">The phone number for which to generate a verification code.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the generated verification code.</returns>
        Task<string> GenerateCodeAsync(string phoneNumber);

        /// <summary>
        /// Stores a verification code for the specified phone number.
        /// </summary>
        /// <param name="phoneNumber">The phone number associated with the verification code.</param>
        /// <param name="code">The verification code to store.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a verification ID that can be used to validate the code.</returns>
        Task<string> StoreCodeAsync(string phoneNumber, string code);

        /// <summary>
        /// Validates a verification code for the specified verification ID and code.
        /// </summary>
        /// <param name="verificationId">The unique verification ID associated with the code.</param>
        /// <param name="code">The verification code to validate.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates whether the code is valid.</returns>
        Task<bool> ValidateCodeAsync(string verificationId, string code);

        /// <summary>
        /// Gets the expiration time for a verification code associated with the specified verification ID.
        /// </summary>
        /// <param name="verificationId">The unique verification ID associated with the code.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the expiration time of the code, or null if no code exists.</returns>
        Task<DateTime?> GetCodeExpirationAsync(string verificationId);

        /// <summary>
        /// Clears all expired verification codes from storage.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task ClearExpiredCodesAsync();
    }
}