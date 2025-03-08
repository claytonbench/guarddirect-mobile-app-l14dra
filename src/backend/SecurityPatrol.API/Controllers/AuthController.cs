using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SecurityPatrol.API.Filters;
using SecurityPatrol.Core.Exceptions;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.Core.Models;
using System;
using System.Threading.Tasks;

namespace SecurityPatrol.API.Controllers
{
    /// <summary>
    /// API controller that handles authentication operations for the Security Patrol application,
    /// including phone number verification, code validation, and token refresh.
    /// </summary>
    [ApiController]
    [Route("api/v1/auth")]
    [ServiceFilter(typeof(ApiExceptionFilter))]
    public class AuthController : ControllerBase
    {
        private readonly IAuthenticationService _authService;
        private readonly ILogger<AuthController> _logger;

        /// <summary>
        /// Initializes a new instance of the AuthController class with required dependencies.
        /// </summary>
        /// <param name="authService">Service for handling authentication operations</param>
        /// <param name="logger">Logger for recording authentication events</param>
        public AuthController(IAuthenticationService authService, ILogger<AuthController> logger)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Initiates the first step of the authentication process by requesting a verification code
        /// for the provided phone number.
        /// </summary>
        /// <param name="request">The authentication request containing the phone number</param>
        /// <returns>A result containing a verification ID if successful, or error details if the request fails.</returns>
        [HttpPost("verify")]
        [ProducesResponseType(typeof(Result<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<Result<string>>> Verify(AuthenticationRequest request)
        {
            _logger.LogInformation("Verification request received for phone number: {PhoneNumber}", 
                request.PhoneNumber);
            
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                string verificationId = await _authService.RequestVerificationCodeAsync(request);
                return Ok(new Result<string> { Data = verificationId, Success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing verification request for phone number: {PhoneNumber}", 
                    request.PhoneNumber);
                throw; // Let the exception filter handle it
            }
        }

        /// <summary>
        /// Completes the second step of the authentication process by validating the verification code
        /// and issuing a JWT token.
        /// </summary>
        /// <param name="request">The verification request containing the verification ID and code</param>
        /// <returns>A result containing the authentication response with JWT token if successful, or error details if validation fails.</returns>
        [HttpPost("validate")]
        [ProducesResponseType(typeof(Result<AuthenticationResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<Result<AuthenticationResponse>>> Validate(VerificationRequest request)
        {
            _logger.LogInformation("Validation request received for verification ID: {VerificationId}", 
                request.VerificationId);
            
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                AuthenticationResponse response = await _authService.VerifyCodeAsync(request);
                return Ok(new Result<AuthenticationResponse> { Data = response, Success = true });
            }
            catch (UnauthorizedException)
            {
                // Let the exception filter handle it
                throw;
            }
            catch (ValidationException)
            {
                // Let the exception filter handle it
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing validation request for verification ID: {VerificationId}", 
                    request.VerificationId);
                throw; // Let the exception filter handle it
            }
        }

        /// <summary>
        /// Refreshes an existing authentication token to extend the session without requiring re-verification.
        /// </summary>
        /// <returns>A result containing the refreshed authentication response with new JWT token if successful, or error details if refresh fails.</returns>
        [HttpPost("refresh")]
        [ProducesResponseType(typeof(Result<AuthenticationResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<Result<AuthenticationResponse>>> Refresh()
        {
            _logger.LogInformation("Token refresh request received");
            
            try
            {
                string authHeader = Request.Headers["Authorization"];
                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    throw new UnauthorizedException("Invalid or missing authorization token");
                }

                string token = authHeader.Substring("Bearer ".Length).Trim();
                AuthenticationResponse response = await _authService.RefreshTokenAsync(token);
                return Ok(new Result<AuthenticationResponse> { Data = response, Success = true });
            }
            catch (UnauthorizedException)
            {
                // Let the exception filter handle it
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing token refresh request");
                throw; // Let the exception filter handle it
            }
        }
    }
}