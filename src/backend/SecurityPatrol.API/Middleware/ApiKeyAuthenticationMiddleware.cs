using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace SecurityPatrol.API.Middleware
{
    /// <summary>
    /// Middleware that validates API requests using an API key header to provide a simple authentication mechanism.
    /// </summary>
    public class ApiKeyAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiKeyAuthenticationMiddleware> _logger;
        private readonly IConfiguration _configuration;
        private const string ApiKeyHeaderName = "X-API-Key";
        private const string ApiKeyConfigKey = "Authentication:ApiKey";

        /// <summary>
        /// Initializes a new instance of the ApiKeyAuthenticationMiddleware class with required dependencies.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline</param>
        /// <param name="logger">Logger for authentication events</param>
        /// <param name="configuration">Configuration to access API key settings</param>
        public ApiKeyAuthenticationMiddleware(
            RequestDelegate next,
            ILogger<ApiKeyAuthenticationMiddleware> logger,
            IConfiguration configuration)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Processes HTTP requests by validating the API key header before allowing the request to proceed.
        /// </summary>
        /// <param name="context">The HTTP context for the current request</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            // Check if the path is excluded from API key validation
            if (IsPathExcluded(context.Request.Path))
            {
                await _next(context);
                return;
            }

            // Try to get the API key from the request header
            if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
            {
                _logger.LogWarning("API key was not provided for request to {Path}", context.Request.Path);
                await HandleUnauthorizedRequest(context, (int)HttpStatusCode.Unauthorized, "API key is missing");
                return;
            }

            // Get the expected API key from configuration
            var apiKey = _configuration[ApiKeyConfigKey];
            
            // If API key is not configured, log error and continue
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogError("API key is not configured in application settings. API key authentication is disabled.");
                await _next(context);
                return;
            }
            
            // Check if the API key is valid
            if (!string.Equals(extractedApiKey, apiKey, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Invalid API key provided for request to {Path}", context.Request.Path);
                await HandleUnauthorizedRequest(context, (int)HttpStatusCode.Forbidden, "Invalid API key");
                return;
            }

            // API key is valid, continue to the next middleware
            _logger.LogDebug("API key authentication successful for request to {Path}", context.Request.Path);
            await _next(context);
        }

        /// <summary>
        /// Determines if the request path should be excluded from API key validation.
        /// </summary>
        /// <param name="path">The request path to check</param>
        /// <returns>True if the path is excluded, false otherwise</returns>
        private bool IsPathExcluded(PathString path)
        {
            // Don't require API key for Swagger UI and API documentation
            if (path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            
            // Don't require API key for authentication endpoints
            if (path.StartsWithSegments("/auth", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            
            // Don't require API key for health checks
            if (path.Equals("/health", StringComparison.OrdinalIgnoreCase) || 
                path.Equals("/healthz", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Handles unauthorized requests by setting appropriate status code and response.
        /// </summary>
        /// <param name="context">The HTTP context for the current request</param>
        /// <param name="statusCode">The HTTP status code to return</param>
        /// <param name="message">The error message</param>
        /// <returns>A task representing the asynchronous operation</returns>
        private async Task HandleUnauthorizedRequest(HttpContext context, int statusCode, string message)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;

            var problemDetails = new
            {
                status = statusCode,
                title = message,
                type = statusCode == 401 
                    ? "https://tools.ietf.org/html/rfc7235#section-3.1" 
                    : "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                detail = statusCode == 401 
                    ? $"Please provide a valid API key in the {ApiKeyHeaderName} header." 
                    : "The API key provided is invalid."
            };

            var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });
            
            await context.Response.WriteAsync(json);
        }
    }
}