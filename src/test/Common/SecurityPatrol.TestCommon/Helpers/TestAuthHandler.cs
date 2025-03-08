using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecurityPatrol.TestCommon.Constants;

namespace SecurityPatrol.TestCommon.Helpers
{
    /// <summary>
    /// Options for configuring the test authentication handler with test user credentials.
    /// </summary>
    public class TestAuthHandlerOptions : AuthenticationSchemeOptions
    {
        /// <summary>
        /// Gets or sets the user ID to use for test authentication.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the phone number to use for test authentication.
        /// </summary>
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestAuthHandlerOptions"/> class with default test values.
        /// </summary>
        public TestAuthHandlerOptions()
        {
            UserId = TestConstants.TestUserId;
            PhoneNumber = TestConstants.TestPhoneNumber;
        }
    }

    /// <summary>
    /// Authentication handler that bypasses normal authentication for testing, using predefined test user credentials.
    /// </summary>
    public class TestAuthHandler : AuthenticationHandler<TestAuthHandlerOptions>
    {
        private readonly ILogger<TestAuthHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestAuthHandler"/> class with required dependencies.
        /// </summary>
        /// <param name="options">The monitor for the options instance.</param>
        /// <param name="loggerFactory">The factory for creating loggers.</param>
        /// <param name="encoder">The URL encoder.</param>
        /// <param name="clock">The system clock.</param>
        public TestAuthHandler(
            IOptionsMonitor<TestAuthHandlerOptions> options,
            ILoggerFactory loggerFactory,
            UrlEncoder encoder,
            ISystemClock clock) 
            : base(options, loggerFactory, encoder, clock)
        {
            _logger = loggerFactory.CreateLogger<TestAuthHandler>();
        }

        /// <summary>
        /// Handles authentication by creating a ClaimsPrincipal with test user claims without validating any actual token.
        /// </summary>
        /// <returns>Authentication result with test user identity.</returns>
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            _logger.LogInformation("Authentication handled by TestAuthHandler");
            
            // Get user info from options
            var userId = Options.UserId;
            var phoneNumber = Options.PhoneNumber;
            
            // Create test claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.MobilePhone, phoneNumber),
                new Claim(ClaimTypes.Role, "SecurityPersonnel")
            };
            
            // Create identity and principal
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            
            // Create ticket
            var ticket = new AuthenticationTicket(principal, "Test");
            
            return await Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    /// <summary>
    /// Extension methods for adding test authentication to the service collection for testing.
    /// </summary>
    public static class TestAuthenticationExtensions
    {
        /// <summary>
        /// Adds test authentication services to the service collection with specified test user credentials.
        /// </summary>
        /// <param name="services">The service collection to add the authentication to.</param>
        /// <param name="userId">The user ID to use for test authentication.</param>
        /// <param name="phoneNumber">The phone number to use for test authentication.</param>
        /// <returns>The service collection with test authentication configured.</returns>
        public static IServiceCollection AddTestAuthentication(
            this IServiceCollection services,
            string userId = null,
            string phoneNumber = null)
        {
            services.Configure<TestAuthHandlerOptions>(options =>
            {
                if (!string.IsNullOrEmpty(userId))
                {
                    options.UserId = userId;
                }
                
                if (!string.IsNullOrEmpty(phoneNumber))
                {
                    options.PhoneNumber = phoneNumber;
                }
            });
            
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
                options.DefaultScheme = "Test";
            })
            .AddScheme<TestAuthHandlerOptions, TestAuthHandler>("Test", options => { });
            
            return services;
        }
    }
}