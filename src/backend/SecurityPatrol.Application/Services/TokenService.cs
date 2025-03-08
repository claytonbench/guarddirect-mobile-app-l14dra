using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SecurityPatrol.Core.Constants;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.Core.Models;

namespace SecurityPatrol.Application.Services
{
    /// <summary>
    /// Implements the ITokenService interface to provide JWT token generation, validation, 
    /// and management for the Security Patrol application.
    /// </summary>
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<TokenService> _logger;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly string _secretKey;
        private readonly int _tokenExpirationMinutes;
        private readonly SymmetricSecurityKey _signingKey;
        private readonly SigningCredentials _signingCredentials;

        /// <summary>
        /// Initializes a new instance of the TokenService class with required dependencies.
        /// </summary>
        /// <param name="configuration">The configuration provider for accessing JWT settings.</param>
        /// <param name="logger">The logger for recording token-related events.</param>
        public TokenService(IConfiguration configuration, ILogger<TokenService> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Load JWT configuration settings
            _issuer = _configuration["JWT:Issuer"] ?? throw new InvalidOperationException("JWT:Issuer configuration is missing");
            _audience = _configuration["JWT:Audience"] ?? throw new InvalidOperationException("JWT:Audience configuration is missing");
            _secretKey = _configuration["JWT:SecretKey"] ?? throw new InvalidOperationException("JWT:SecretKey configuration is missing");
            
            // Default to 8 hours expiration if not specified
            if (!int.TryParse(_configuration["JWT:ExpirationMinutes"], out _tokenExpirationMinutes))
            {
                _tokenExpirationMinutes = 480; // 8 hours in minutes
            }

            _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            _signingCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);
            
            _logger.LogInformation("TokenService initialized with expiration time of {ExpirationMinutes} minutes", _tokenExpirationMinutes);
        }

        /// <summary>
        /// Generates a JWT token for the specified user with appropriate claims and expiration time.
        /// </summary>
        /// <param name="user">The user for whom to generate the token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains 
        /// the authentication response with JWT token and expiration time.</returns>
        public async Task<AuthenticationResponse> GenerateTokenAsync(User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            _logger.LogInformation("Generating token for user: {UserId}", user.Id);

            var expires = DateTime.UtcNow.AddMinutes(_tokenExpirationMinutes);

            // Create claims for the token
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.UserId, user.Id),
                new Claim(ClaimTypes.PhoneNumber, user.PhoneNumber),
                new Claim(ClaimTypes.Role, Roles.SecurityPersonnel)
            };

            var identity = new ClaimsIdentity(claims);

            var securityToken = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: identity.Claims,
                expires: expires,
                signingCredentials: _signingCredentials
            );

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.WriteToken(securityToken);

            _logger.LogInformation("Token generated successfully for user: {UserId}", user.Id);

            return new AuthenticationResponse
            {
                Token = token,
                ExpiresAt = expires
            };
        }

        /// <summary>
        /// Validates a JWT token to ensure it is properly formatted, not expired, and has a valid signature.
        /// </summary>
        /// <param name="token">The JWT token to validate.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates 
        /// whether the token is valid.</returns>
        public async Task<bool> ValidateTokenAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("Token validation failed: Token is null or empty");
                return false;
            }

            _logger.LogInformation("Validating token");

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                ValidIssuer = _issuer,
                ValidAudience = _audience,
                IssuerSigningKey = _signingKey,
                ClockSkew = TimeSpan.Zero // No tolerance for token expiration time
            };

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var validatedToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Token validation failed");
                return false;
            }
        }

        /// <summary>
        /// Refreshes an existing JWT token by generating a new token with updated expiration time.
        /// </summary>
        /// <param name="token">The existing JWT token to refresh.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains 
        /// the authentication response with refreshed JWT token and new expiration time.</returns>
        public async Task<AuthenticationResponse> RefreshTokenAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("Token refresh failed: Token is null or empty");
                return null;
            }

            _logger.LogInformation("Refreshing token");

            var principal = await GetPrincipalFromTokenAsync(token);
            if (principal == null)
            {
                _logger.LogWarning("Token refresh failed: Unable to extract principal from token");
                return null;
            }

            // Extract user ID and phone number from the token claims
            var userIdClaim = principal.FindFirst(claim => claim.Type == ClaimTypes.UserId);
            var phoneNumberClaim = principal.FindFirst(claim => claim.Type == ClaimTypes.PhoneNumber);

            if (userIdClaim == null || phoneNumberClaim == null)
            {
                _logger.LogWarning("Token refresh failed: Required claims are missing");
                return null;
            }

            // Create a minimal user object with just the necessary information for token generation
            var user = new User
            {
                Id = userIdClaim.Value,
                PhoneNumber = phoneNumberClaim.Value
            };

            // Generate a new token
            var newTokenResponse = await GenerateTokenAsync(user);
            
            _logger.LogInformation("Token refreshed successfully for user: {UserId}", user.Id);
            
            return newTokenResponse;
        }

        /// <summary>
        /// Extracts the ClaimsPrincipal from a JWT token for authentication and authorization purposes.
        /// </summary>
        /// <param name="token">The JWT token from which to extract the ClaimsPrincipal.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains 
        /// the ClaimsPrincipal extracted from the token if valid, or null if the token is invalid.</returns>
        public async Task<ClaimsPrincipal> GetPrincipalFromTokenAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("Get principal failed: Token is null or empty");
                return null;
            }

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = false, // We don't validate lifetime when extracting principal
                ValidIssuer = _issuer,
                ValidAudience = _audience,
                IssuerSigningKey = _signingKey
            };

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var validatedToken);
                return principal;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Get principal from token failed");
                return null;
            }
        }

        /// <summary>
        /// Extracts the user ID claim from a JWT token.
        /// </summary>
        /// <param name="token">The JWT token from which to extract the user ID.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains 
        /// the user ID extracted from the token if valid, or null if the token is invalid or 
        /// doesn't contain a user ID claim.</returns>
        public async Task<string> GetUserIdFromTokenAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("Get user ID failed: Token is null or empty");
                return null;
            }

            var principal = await GetPrincipalFromTokenAsync(token);
            if (principal == null)
            {
                _logger.LogWarning("Get user ID failed: Unable to extract principal from token");
                return null;
            }

            var userIdClaim = principal.FindFirst(claim => claim.Type == ClaimTypes.UserId);
            if (userIdClaim == null)
            {
                _logger.LogWarning("Get user ID failed: UserId claim not found in token");
                return null;
            }

            return userIdClaim.Value;
        }
    }
}