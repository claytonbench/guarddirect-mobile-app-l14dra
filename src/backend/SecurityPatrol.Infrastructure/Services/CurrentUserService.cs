using Microsoft.AspNetCore.Http; // v8.0.0
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.Core.Constants;
using System; // v8.0.0
using System.Linq; // v8.0.0

namespace SecurityPatrol.Infrastructure.Services
{
    /// <summary>
    /// Implements the ICurrentUserService interface to provide access to the current
    /// authenticated user's information from the HTTP context.
    /// </summary>
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the CurrentUserService class with the HTTP context accessor.
        /// </summary>
        /// <param name="httpContextAccessor">The HTTP context accessor.</param>
        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        /// <summary>
        /// Gets the ID of the currently authenticated user from the JWT claims.
        /// </summary>
        /// <returns>The user ID of the currently authenticated user, or null if not authenticated.</returns>
        public string GetUserId()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User == null)
            {
                return null;
            }

            return httpContext.User.Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.UserId)?.Value;
        }

        /// <summary>
        /// Gets the phone number of the currently authenticated user from the JWT claims.
        /// </summary>
        /// <returns>The phone number of the currently authenticated user, or null if not authenticated.</returns>
        public string GetPhoneNumber()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User == null)
            {
                return null;
            }

            return httpContext.User.Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.PhoneNumber)?.Value;
        }

        /// <summary>
        /// Determines whether the current request is from an authenticated user.
        /// </summary>
        /// <returns>True if the user is authenticated, false otherwise.</returns>
        public bool IsAuthenticated()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User == null)
            {
                return false;
            }

            return httpContext.User.Identity?.IsAuthenticated ?? false;
        }

        /// <summary>
        /// Gets the timestamp of when the user was last authenticated.
        /// </summary>
        /// <returns>The timestamp of the last successful authentication, or null if not authenticated.</returns>
        public DateTime? GetLastAuthenticated()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User == null)
            {
                return null;
            }

            var authTimeClaim = httpContext.User.Claims
                .FirstOrDefault(c => c.Type == "auth_time" || 
                                     c.Type == System.Security.Claims.ClaimTypes.AuthenticationInstant)?.Value;
            
            if (string.IsNullOrEmpty(authTimeClaim))
            {
                return null;
            }

            // Try standard DateTime format
            if (DateTime.TryParse(authTimeClaim, out DateTime authTime))
            {
                return authTime;
            }
            
            // Try Unix timestamp format (seconds since epoch)
            if (long.TryParse(authTimeClaim, out long unixTime))
            {
                return DateTimeOffset.FromUnixTimeSeconds(unixTime).DateTime;
            }
            
            return null;
        }
    }
}