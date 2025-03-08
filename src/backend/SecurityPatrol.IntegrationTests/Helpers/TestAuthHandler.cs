using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using SecurityPatrol.Core.Constants;

namespace SecurityPatrol.IntegrationTests.Helpers
{
    /// <summary>
    /// Options for configuring the test authentication handler with test user credentials.
    /// </summary>
    public class TestAuthHandlerOptions : AuthenticationSchemeOptions
    {
        /// <summary>
        /// The user ID to use for test authentication.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// The phone number to use for test authentication.
        /// </summary>
        public string PhoneNumber { get; set; }
    }

    /// <summary>
    /// Authentication handler that bypasses normal JWT authentication for integration testing,
    /// using predefined test user credentials.
    /// </summary>
    public class TestAuthHandler : AuthenticationHandler<TestAuthHandlerOptions>
    {
        private readonly ILogger<TestAuthHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestAuthHandler"/> class with required dependencies.
        /// </summary>
        /// <param name="options">The monitor for options instances.</param>
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
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            _logger.LogInformation("Authenticating using test authentication handler");

            // Get test user credentials from options
            var userId = Options.UserId ?? "test-user-id";
            var phoneNumber = Options.PhoneNumber ?? "+15555555555";

            // Create claims for test user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.UserId, userId),
                new Claim(ClaimTypes.PhoneNumber, phoneNumber),
                new Claim(ClaimTypes.Role, Roles.SecurityPersonnel)
            };

            // Create ClaimsIdentity with 'Test' authentication type
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);

            // Create AuthenticationTicket with 'Test' authentication scheme
            var ticket = new AuthenticationTicket(principal, "Test");
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    /// <summary>
    /// Extension methods for adding test authentication to the service collection for integration testing.
    /// </summary>
    public static class TestAuthenticationExtensions
    {
        /// <summary>
        /// Adds test authentication services to the service collection with specified test user credentials.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="userId">The user ID to use for test authentication.</param>
        /// <param name="phoneNumber">The phone number to use for test authentication.</param>
        /// <returns>The service collection with test authentication configured.</returns>
        public static IServiceCollection AddTestAuthentication(
            this IServiceCollection services,
            string userId = "test-user-id",
            string phoneNumber = "+15555555555")
        {
            // Configure TestAuthHandlerOptions with userId and phoneNumber
            services.Configure<TestAuthHandlerOptions>(options =>
            {
                options.UserId = userId;
                options.PhoneNumber = phoneNumber;
            });

            // Add authentication services to the service collection
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