using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.TestCommon.Constants;

namespace SecurityPatrol.TestCommon.Mocks
{
    /// <summary>
    /// Mock implementation of ISmsService for testing purposes that simulates SMS messaging functionality without sending actual SMS messages.
    /// </summary>
    public class MockSmsService : ISmsService
    {
        /// <summary>
        /// Collection of all SMS messages sent by this mock service.
        /// </summary>
        public List<SmsMessage> SentMessages { get; private set; }

        /// <summary>
        /// Controls whether the SMS operations should succeed or fail. Set to false to simulate failures.
        /// </summary>
        public bool ShouldSucceed { get; set; }

        /// <summary>
        /// Controls whether the SMS operations should throw exceptions. Set to true to simulate exception scenarios.
        /// </summary>
        public bool ShouldThrowException { get; set; }

        /// <summary>
        /// Gets the phone number used in the most recent SMS operation.
        /// </summary>
        public string LastPhoneNumber { get; private set; }

        /// <summary>
        /// Gets the message content used in the most recent SMS operation.
        /// </summary>
        public string LastMessage { get; private set; }

        /// <summary>
        /// Gets the verification code used in the most recent verification code operation.
        /// </summary>
        public string LastVerificationCode { get; private set; }

        /// <summary>
        /// Initializes a new instance of the MockSmsService class with default test values.
        /// </summary>
        public MockSmsService()
        {
            SentMessages = new List<SmsMessage>();
            ShouldSucceed = true;
            ShouldThrowException = false;
            LastPhoneNumber = null;
            LastMessage = null;
            LastVerificationCode = null;
        }

        /// <summary>
        /// Simulates sending an SMS message to the specified phone number.
        /// </summary>
        /// <param name="phoneNumber">The recipient's phone number.</param>
        /// <param name="message">The message content to send.</param>
        /// <returns>A task that returns true if the SMS was successfully sent, otherwise false.</returns>
        public async Task<bool> SendSmsAsync(string phoneNumber, string message)
        {
            if (ShouldThrowException)
            {
                throw new Exception("Simulated exception in SendSmsAsync");
            }

            LastPhoneNumber = phoneNumber;
            LastMessage = message;

            SentMessages.Add(new SmsMessage(phoneNumber, message, DateTime.Now));

            return await Task.FromResult(ShouldSucceed);
        }

        /// <summary>
        /// Simulates sending a verification code to the specified phone number.
        /// </summary>
        /// <param name="phoneNumber">The recipient's phone number.</param>
        /// <param name="code">The verification code to send.</param>
        /// <returns>A task that returns true if the verification code was successfully sent, otherwise false.</returns>
        public async Task<bool> SendVerificationCodeAsync(string phoneNumber, string code)
        {
            if (ShouldThrowException)
            {
                throw new Exception("Simulated exception in SendVerificationCodeAsync");
            }

            LastPhoneNumber = phoneNumber;
            LastVerificationCode = code;

            string message = $"Your verification code is: {code}";
            return await SendSmsAsync(phoneNumber, message);
        }

        /// <summary>
        /// Resets the mock service state for a fresh test scenario.
        /// </summary>
        public void Reset()
        {
            SentMessages.Clear();
            ShouldSucceed = true;
            ShouldThrowException = false;
            LastPhoneNumber = null;
            LastMessage = null;
            LastVerificationCode = null;
        }

        /// <summary>
        /// Retrieves all messages sent to a specific phone number.
        /// </summary>
        /// <param name="phoneNumber">The phone number to filter by.</param>
        /// <returns>A list of SMS messages sent to the specified phone number.</returns>
        public List<SmsMessage> GetSentMessagesForPhoneNumber(string phoneNumber)
        {
            return SentMessages.FindAll(m => m.PhoneNumber == phoneNumber);
        }

        /// <summary>
        /// Checks if a verification code has been sent to the specified phone number.
        /// </summary>
        /// <param name="phoneNumber">The phone number to check.</param>
        /// <param name="code">The verification code to look for.</param>
        /// <returns>True if the verification code has been sent to the phone number, otherwise false.</returns>
        public bool HasSentVerificationCode(string phoneNumber, string code)
        {
            var messages = GetSentMessagesForPhoneNumber(phoneNumber);
            return messages.Exists(m => m.Message.Contains(code));
        }
    }

    /// <summary>
    /// Represents an SMS message sent by the mock service for testing verification.
    /// </summary>
    public class SmsMessage
    {
        /// <summary>
        /// Gets the recipient's phone number.
        /// </summary>
        public string PhoneNumber { get; }

        /// <summary>
        /// Gets the message content.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the timestamp when the message was sent.
        /// </summary>
        public DateTime SentAt { get; }

        /// <summary>
        /// Initializes a new instance of the SmsMessage class.
        /// </summary>
        /// <param name="phoneNumber">The recipient's phone number.</param>
        /// <param name="message">The message content.</param>
        /// <param name="sentAt">The timestamp when the message was sent.</param>
        public SmsMessage(string phoneNumber, string message, DateTime sentAt)
        {
            PhoneNumber = phoneNumber;
            Message = message;
            SentAt = sentAt;
        }
    }
}