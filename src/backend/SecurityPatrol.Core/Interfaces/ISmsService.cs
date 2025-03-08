using System.Threading.Tasks; // System.Threading.Tasks v8.0.0 - For async Task operations

namespace SecurityPatrol.Core.Interfaces
{
    /// <summary>
    /// Interface that defines the contract for SMS messaging services in the Security Patrol application.
    /// </summary>
    public interface ISmsService
    {
        /// <summary>
        /// Sends an SMS message to the specified phone number.
        /// </summary>
        /// <param name="phoneNumber">The recipient's phone number.</param>
        /// <param name="message">The message content to send.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates whether the SMS was sent successfully.</returns>
        Task<bool> SendSmsAsync(string phoneNumber, string message);

        /// <summary>
        /// Sends a verification code to the specified phone number.
        /// </summary>
        /// <param name="phoneNumber">The recipient's phone number.</param>
        /// <param name="code">The verification code to send.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates whether the verification code was sent successfully.</returns>
        Task<bool> SendVerificationCodeAsync(string phoneNumber, string code);
    }
}