using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Core.Exceptions;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.Core.Models;

namespace SecurityPatrol.Application.Services
{
    /// <summary>
    /// Implements the IAuthenticationService interface to provide authentication functionality for the Security Patrol application
    /// using phone number verification and JWT tokens.
    /// </summary>
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IUserRepository _userRepository;
        private readonly IVerificationCodeService _verificationCodeService;
        private readonly ISmsService _smsService;
        private readonly ITokenService _tokenService;
        private readonly ILogger<AuthenticationService> _logger;

        /// <summary>
        /// Initializes a new instance of the AuthenticationService class with required dependencies.
        /// </summary>
        /// <param name="userRepository">Repository for user data operations</param>
        /// <param name="verificationCodeService">Service for verification code operations</param>
        /// <param name="smsService">Service for SMS messaging operations</param>
        /// <param name="tokenService">Service for JWT token operations</param>
        /// <param name="logger">Logger for diagnostic information</param>
        public AuthenticationService(
            IUserRepository userRepository,
            IVerificationCodeService verificationCodeService,
            ISmsService smsService,
            ITokenService tokenService,
            ILogger<AuthenticationService> logger)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _verificationCodeService = verificationCodeService ?? throw new ArgumentNullException(nameof(verificationCodeService));
            _smsService = smsService ?? throw new ArgumentNullException(nameof(smsService));
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Requests a verification code for the provided phone number. The code is sent via SMS to the user's phone.
        /// </summary>
        /// <param name="request">The authentication request containing the phone number</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a verification ID that can be used to validate the code.</returns>
        public async Task<string> RequestVerificationCodeAsync(AuthenticationRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.PhoneNumber))
                throw new ValidationException("Phone number is required");

            _logger.LogInformation("Verification code requested for phone number: {PhoneNumber}", request.PhoneNumber);

            // Generate a verification code
            string verificationCode = await _verificationCodeService.GenerateCodeAsync(request.PhoneNumber);
            
            // Store the verification code with the phone number for later validation
            string verificationId = await _verificationCodeService.StoreCodeAsync(request.PhoneNumber, verificationCode);
            
            // Send the verification code via SMS
            bool smsSent = await _smsService.SendVerificationCodeAsync(request.PhoneNumber, verificationCode);
            
            if (!smsSent)
            {
                _logger.LogWarning("Failed to send verification code SMS to {PhoneNumber}", request.PhoneNumber);
                throw new ApplicationException("Failed to send verification code. Please try again.");
            }
            
            _logger.LogInformation("Verification code sent successfully to {PhoneNumber}", request.PhoneNumber);
            
            // Return the verification ID for later use
            return verificationId;
        }

        /// <summary>
        /// Verifies the code provided by the user against the code that was sent to their phone number.
        /// </summary>
        /// <param name="request">The verification request containing the phone number and code</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the authentication response with JWT token if verification is successful.</returns>
        public async Task<AuthenticationResponse> VerifyCodeAsync(VerificationRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.PhoneNumber))
                throw new ValidationException("Phone number is required");

            if (string.IsNullOrWhiteSpace(request.Code))
                throw new ValidationException("Verification code is required");

            _logger.LogInformation("Verifying code for phone number: {PhoneNumber}", request.PhoneNumber);

            // The validation of code requires a verification ID, but we only have the phone number
            // The implementation of IVerificationCodeService should handle mapping between phone number and verification ID
            bool isValidCode = await _verificationCodeService.ValidateCodeAsync(request.PhoneNumber, request.Code);
            
            if (!isValidCode)
            {
                _logger.LogWarning("Invalid verification code provided for {PhoneNumber}", request.PhoneNumber);
                throw new UnauthorizedException("Invalid verification code");
            }
            
            // Get or create the user
            User user = await GetOrCreateUserAsync(request.PhoneNumber);
            
            // Update the user's last authentication timestamp
            await _userRepository.UpdateLastAuthenticatedAsync(user.Id, DateTime.UtcNow);
            
            // Generate a JWT token for the user
            AuthenticationResponse authResponse = await _tokenService.GenerateTokenAsync(user);
            
            _logger.LogInformation("User {UserId} successfully authenticated with phone number {PhoneNumber}", 
                user.Id, request.PhoneNumber);
            
            return authResponse;
        }

        /// <summary>
        /// Refreshes an existing authentication token to extend the session without requiring re-verification.
        /// </summary>
        /// <param name="token">The current authentication token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the new authentication response with refreshed JWT token.</returns>
        public async Task<AuthenticationResponse> RefreshTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentNullException(nameof(token));

            _logger.LogInformation("Token refresh requested");

            // Extract the user ID from the token
            string userId = await _tokenService.GetUserIdFromTokenAsync(token);
            
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Failed to extract user ID from token during refresh");
                throw new UnauthorizedException("Invalid token");
            }
            
            // Get the user from the repository
            User user = await _userRepository.GetByIdAsync(userId);
            
            if (user == null || !user.IsActive)
            {
                _logger.LogWarning("User {UserId} not found or inactive during token refresh", userId);
                throw new UnauthorizedException("User not found or inactive");
            }
            
            // Update the user's last authentication timestamp
            await _userRepository.UpdateLastAuthenticatedAsync(user.Id, DateTime.UtcNow);
            
            // Generate a new JWT token for the user
            AuthenticationResponse authResponse = await _tokenService.GenerateTokenAsync(user);
            
            _logger.LogInformation("Token refreshed successfully for user {UserId}", userId);
            
            return authResponse;
        }

        /// <summary>
        /// Retrieves a user by their phone number.
        /// </summary>
        /// <param name="phoneNumber">The phone number to search for</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the user if found, or null if not found.</returns>
        public async Task<User> GetUserByPhoneNumberAsync(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                throw new ArgumentNullException(nameof(phoneNumber));

            return await _userRepository.GetByPhoneNumberAsync(phoneNumber);
        }

        /// <summary>
        /// Validates an authentication token to ensure it is valid and not expired.
        /// </summary>
        /// <param name="token">The authentication token to validate</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates whether the token is valid.</returns>
        public async Task<bool> ValidateTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            return await _tokenService.ValidateTokenAsync(token);
        }

        /// <summary>
        /// Gets an existing user by phone number or creates a new user if one doesn't exist.
        /// </summary>
        /// <param name="phoneNumber">The phone number of the user</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the existing or newly created user.</returns>
        private async Task<User> GetOrCreateUserAsync(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                throw new ArgumentNullException(nameof(phoneNumber));

            // Check if user already exists
            bool userExists = await _userRepository.ExistsByPhoneNumberAsync(phoneNumber);
            
            if (userExists)
            {
                return await _userRepository.GetByPhoneNumberAsync(phoneNumber);
            }
            
            // Create a new user if one doesn't exist
            User user = new User
            {
                PhoneNumber = phoneNumber,
                IsActive = true,
                LastAuthenticated = DateTime.UtcNow
            };
            
            await _userRepository.AddAsync(user);
            
            _logger.LogInformation("New user created with ID {UserId} for phone number {PhoneNumber}", 
                user.Id, phoneNumber);
            
            return user;
        }
    }
}