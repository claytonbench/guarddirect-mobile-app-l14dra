using Microsoft.AspNetCore.Authentication.JwtBearer; // Microsoft.AspNetCore.Authentication.JwtBearer 8.0.0
using Microsoft.Extensions.Configuration; // Microsoft.Extensions.Configuration 8.0.0
using Microsoft.Extensions.DependencyInjection; // Microsoft.Extensions.DependencyInjection 8.0.0
using Microsoft.IdentityModel.Tokens; // Microsoft.IdentityModel.Tokens 7.0.0
using System; // System 8.0.0
using System.Text; // System.Text 8.0.0
using Microsoft.AspNetCore.Authorization; // Microsoft.AspNetCore.Authorization 8.0.0
using SecurityPatrol.Core.Constants; // For JWT claim types and roles constants

namespace SecurityPatrol.API.Extensions
{
    /// <summary>
    /// Static class containing extension methods for configuring JWT authentication and authorization policies for the Security Patrol API.
    /// </summary>
    public static class AuthenticationExtensions
    {
        /// <summary>
        /// Adds JWT bearer authentication to the service collection with appropriate configuration.
        /// </summary>
        /// <param name="services">The service collection to add JWT authentication to.</param>
        /// <param name="configuration">The configuration containing JWT settings.</param>
        /// <returns>The service collection with JWT authentication configured.</returns>
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            // Get JWT configuration settings from IConfiguration
            var jwtSection = configuration.GetSection("Jwt");
            var issuer = jwtSection["Issuer"] ?? throw new InvalidOperationException("JWT Issuer is not configured");
            var audience = jwtSection["Audience"] ?? throw new InvalidOperationException("JWT Audience is not configured");
            var secretKey = jwtSection["SecretKey"] ?? throw new InvalidOperationException("JWT Secret Key is not configured");

            // Create signing key using secret key
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            // Add authentication services to the service collection
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                // Configure JWT bearer authentication options
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    // Set token validation parameters (issuer, audience, signing key)
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = key,
                    // Set clock skew to zero for accurate token expiration
                    ClockSkew = TimeSpan.Zero
                };

                // Configure events for token validation failures
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception is SecurityTokenExpiredException)
                        {
                            context.Response.Headers.Add("Token-Expired", "true");
                        }
                        return System.Threading.Tasks.Task.CompletedTask;
                    }
                };
            });

            return services;
        }

        /// <summary>
        /// Adds authorization policies to the service collection for role-based access control.
        /// </summary>
        /// <param name="services">The service collection to add authorization policies to.</param>
        /// <returns>The service collection with authorization policies configured.</returns>
        public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
        {
            // Add authorization services to the service collection
            services.AddAuthorization(options =>
            {
                // Configure authorization options
                // Add 'SecurityPersonnel' policy requiring the security personnel role
                options.AddPolicy("SecurityPersonnel", policy =>
                {
                    // Configure policy to require claim of type Role with value SecurityPersonnel
                    policy.RequireClaim(ClaimTypes.Role, Roles.SecurityPersonnel);
                });

                // Set default policy to require authenticated users
                options.DefaultPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            });

            return services;
        }
    }
}