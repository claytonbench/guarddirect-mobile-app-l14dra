using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SecurityPatrol.Core.Interfaces;

namespace SecurityPatrol.Application.Services
{
    /// <summary>
    /// Implements the IVerificationCodeService interface to provide verification code functionality
    /// for the Security Patrol application's two-step authentication process.
    /// </summary>
    public class VerificationCodeService : IVerificationCodeService
    {
        private readonly ConcurrentDictionary<string, VerificationData> _verificationCodes;
        private readonly IDateTime _dateTime;
        private readonly ILogger<VerificationCodeService> _logger;

        /// <summary>
        /// The length of generated verification codes.
        /// </summary>
        public int CodeLength { get; } = 6;

        /// <summary>
        /// The time until a verification code expires.
        /// </summary>
        public TimeSpan CodeExpirationTime { get; } = TimeSpan.FromMinutes(10);

        /// <summary>
        /// Initializes a new instance of the VerificationCodeService class with required dependencies.
        /// </summary>
        /// <param name="dateTime">Service for date and time operations.</param>
        /// <param name="logger">Logger for capturing service operations.</param>
        /// <exception cref="ArgumentNullException">Thrown if any required dependency is null.</exception>
        public VerificationCodeService(IDateTime dateTime, ILogger<VerificationCodeService> logger)
        {
            _dateTime = dateTime ?? throw new ArgumentNullException(nameof(dateTime));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _verificationCodes = new ConcurrentDictionary<string, VerificationData>();
        }

        /// <inheritdoc/>
        public Task<string> GenerateCodeAsync(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber))
            {
                throw new ArgumentException("Phone number cannot be null or empty.", nameof(phoneNumber));
            }

            _logger.LogInformation("Generating verification code for phone number: {PhoneNumber}", 
                phoneNumber);

            using var generator = RandomNumberGenerator.Create();
            byte[] bytes = new byte[4];
            generator.GetBytes(bytes);
            uint number = BitConverter.ToUInt32(bytes, 0) % (uint)Math.Pow(10, CodeLength);
            string code = number.ToString().PadLeft(CodeLength, '0');

            _logger.LogInformation("Verification code generated successfully for {PhoneNumber}", 
                phoneNumber);

            return Task.FromResult(code);
        }

        /// <inheritdoc/>
        public Task<string> StoreCodeAsync(string phoneNumber, string code)
        {
            if (string.IsNullOrEmpty(phoneNumber))
            {
                throw new ArgumentException("Phone number cannot be null or empty.", nameof(phoneNumber));
            }
            
            if (string.IsNullOrEmpty(code))
            {
                throw new ArgumentException("Verification code cannot be null or empty.", nameof(code));
            }

            _logger.LogInformation("Storing verification code for phone number: {PhoneNumber}", 
                phoneNumber);

            string verificationId = Guid.NewGuid().ToString();
            DateTime expirationTime = _dateTime.UtcNow().Add(CodeExpirationTime);
            
            var verificationData = new VerificationData(phoneNumber, code, expirationTime);
            
            if (!_verificationCodes.TryAdd(verificationId, verificationData))
            {
                _logger.LogWarning("Failed to store verification code for phone number: {PhoneNumber}", 
                    phoneNumber);
                    
                // Try again with a new verification ID
                return StoreCodeAsync(phoneNumber, code);
            }

            _logger.LogInformation("Verification code stored successfully for {PhoneNumber} with ID: {VerificationId}", 
                phoneNumber, verificationId);

            return Task.FromResult(verificationId);
        }

        /// <inheritdoc/>
        public Task<bool> ValidateCodeAsync(string verificationId, string code)
        {
            if (string.IsNullOrEmpty(verificationId))
            {
                throw new ArgumentException("Verification ID cannot be null or empty.", nameof(verificationId));
            }
            
            if (string.IsNullOrEmpty(code))
            {
                throw new ArgumentException("Verification code cannot be null or empty.", nameof(code));
            }

            _logger.LogInformation("Validating verification code for ID: {VerificationId}", 
                verificationId);

            if (!_verificationCodes.TryGetValue(verificationId, out var verificationData))
            {
                _logger.LogWarning("Verification ID not found: {VerificationId}", 
                    verificationId);
                return Task.FromResult(false);
            }

            if (verificationData.ExpiresAt < _dateTime.UtcNow())
            {
                _logger.LogWarning("Verification code expired for ID: {VerificationId}", 
                    verificationId);
                
                // Remove expired code
                _verificationCodes.TryRemove(verificationId, out _);
                return Task.FromResult(false);
            }

            bool isValid = string.Equals(verificationData.Code, code, StringComparison.OrdinalIgnoreCase);
            
            if (isValid)
            {
                _logger.LogInformation("Verification code validated successfully for ID: {VerificationId}", 
                    verificationId);
            }
            else
            {
                _logger.LogWarning("Invalid verification code provided for ID: {VerificationId}", 
                    verificationId);
            }

            return Task.FromResult(isValid);
        }

        /// <inheritdoc/>
        public Task<DateTime?> GetCodeExpirationAsync(string verificationId)
        {
            if (string.IsNullOrEmpty(verificationId))
            {
                throw new ArgumentException("Verification ID cannot be null or empty.", nameof(verificationId));
            }

            if (_verificationCodes.TryGetValue(verificationId, out var verificationData))
            {
                return Task.FromResult<DateTime?>(verificationData.ExpiresAt);
            }

            return Task.FromResult<DateTime?>(null);
        }

        /// <inheritdoc/>
        public Task ClearExpiredCodesAsync()
        {
            _logger.LogInformation("Starting cleanup of expired verification codes");

            DateTime now = _dateTime.UtcNow();
            var expiredIds = _verificationCodes
                .Where(kvp => kvp.Value.ExpiresAt < now)
                .Select(kvp => kvp.Key)
                .ToList();

            int removedCount = 0;
            foreach (var id in expiredIds)
            {
                if (_verificationCodes.TryRemove(id, out _))
                {
                    removedCount++;
                }
            }

            _logger.LogInformation("Removed {Count} expired verification codes", removedCount);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Internal class that represents verification code data including the phone number,
        /// code, and expiration time.
        /// </summary>
        private class VerificationData
        {
            /// <summary>
            /// Gets the phone number associated with the verification code.
            /// </summary>
            public string PhoneNumber { get; }

            /// <summary>
            /// Gets the verification code.
            /// </summary>
            public string Code { get; }

            /// <summary>
            /// Gets the time when the verification code expires.
            /// </summary>
            public DateTime ExpiresAt { get; }

            /// <summary>
            /// Initializes a new instance of the VerificationData class.
            /// </summary>
            /// <param name="phoneNumber">The phone number associated with the verification code.</param>
            /// <param name="code">The verification code.</param>
            /// <param name="expiresAt">The time when the verification code expires.</param>
            public VerificationData(string phoneNumber, string code, DateTime expiresAt)
            {
                PhoneNumber = phoneNumber;
                Code = code;
                ExpiresAt = expiresAt;
            }
        }
    }
}