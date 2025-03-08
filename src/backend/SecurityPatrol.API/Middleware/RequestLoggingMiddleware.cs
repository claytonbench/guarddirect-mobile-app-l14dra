using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SecurityPatrol.Core.Interfaces;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace SecurityPatrol.API.Middleware
{
    /// <summary>
    /// Middleware that logs HTTP requests and responses with timing information and user context.
    /// Provides comprehensive logging for monitoring, troubleshooting, and audit purposes.
    /// </summary>
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;
        private readonly ICurrentUserService _currentUserService;
        
        // Constants for configuration
        private const int MaxBodySizeToLog = 10 * 1024; // 10KB

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestLoggingMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="logger">The logger used to write log entries.</param>
        /// <param name="currentUserService">Service to get current user information.</param>
        public RequestLoggingMiddleware(
            RequestDelegate next,
            ILogger<RequestLoggingMiddleware> logger,
            ICurrentUserService currentUserService)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        }

        /// <summary>
        /// Processes HTTP requests by logging request details before and after executing the next middleware.
        /// </summary>
        /// <param name="context">The HTTP context for the current request.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var stopwatch = Stopwatch.StartNew();
            
            // Log the incoming request
            LogRequest(context);

            // Enable buffering for request body if possible
            if (context.Request.Body.CanRead && context.Request.Body.CanSeek)
            {
                context.Request.EnableBuffering();
            }

            try
            {
                // Call the next middleware in the pipeline
                await _next(context);
            }
            catch (Exception ex)
            {
                // Log exception details
                _logger.LogError(ex, "An unhandled exception occurred during request processing: {RequestMethod} {RequestPath}",
                    context.Request.Method, context.Request.Path);
                throw; // Re-throw to allow error handling middleware to process it
            }
            finally
            {
                stopwatch.Stop();
                // Log the completed request with response details
                LogResponse(context, stopwatch.Elapsed);
            }
        }

        /// <summary>
        /// Logs detailed information about the incoming HTTP request.
        /// </summary>
        /// <param name="context">The HTTP context containing the request.</param>
        private void LogRequest(HttpContext context)
        {
            var request = context.Request;
            var userId = _currentUserService.IsAuthenticated() ? _currentUserService.GetUserId() : null;
            var clientIp = GetClientIpAddress(context);
            
            // Create a structured log entry
            using (_logger.BeginScope(new
            {
                RequestId = context.TraceIdentifier,
                RequestMethod = request.Method,
                RequestPath = request.Path,
                RequestQueryString = request.QueryString.ToString(),
                ClientIp = clientIp,
                UserAgent = request.Headers.ContainsKey("User-Agent") ? request.Headers["User-Agent"].ToString() : string.Empty,
                UserId = userId,
                Timestamp = DateTimeOffset.UtcNow
            }))
            {
                _logger.LogInformation(
                    "Request: HTTP {RequestMethod} {RequestPath}{RequestQueryString} received from {ClientIp} by user {UserId}",
                    request.Method, 
                    request.Path, 
                    request.QueryString.ToString(),
                    clientIp,
                    userId ?? "anonymous");
                
                // Log request body if appropriate
                if (ShouldLogRequestBody(context))
                {
                    try
                    {
                        // Reset position to start
                        request.Body.Position = 0;
                        
                        // Read request body
                        using (var reader = new StreamReader(request.Body, leaveOpen: true))
                        {
                            var body = reader.ReadToEndAsync().Result;
                            _logger.LogDebug("Request Body: {RequestBody}", body);
                        }
                        
                        // Reset position to start for next middleware
                        request.Body.Position = 0;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error reading request body for logging");
                    }
                }
            }
        }

        /// <summary>
        /// Logs information about the HTTP response after processing.
        /// </summary>
        /// <param name="context">The HTTP context containing the response.</param>
        /// <param name="duration">The time taken to process the request.</param>
        private void LogResponse(HttpContext context, TimeSpan duration)
        {
            var statusCode = context.Response.StatusCode;
            var level = statusCode >= 500 ? LogLevel.Error :
                        statusCode >= 400 ? LogLevel.Warning :
                        LogLevel.Information;

            var userId = _currentUserService.IsAuthenticated() ? _currentUserService.GetUserId() : null;
            var durationMs = Math.Round(duration.TotalMilliseconds, 2);
            
            // Create a structured log entry
            using (_logger.BeginScope(new
            {
                RequestId = context.TraceIdentifier,
                StatusCode = statusCode,
                DurationMs = durationMs,
                UserId = userId,
                Timestamp = DateTimeOffset.UtcNow
            }))
            {
                _logger.Log(
                    level,
                    "Response: HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {DurationMs}ms",
                    context.Request.Method,
                    context.Request.Path,
                    statusCode,
                    durationMs);
                
                // Add warning for slow responses
                if (durationMs > 500) // More than 500ms is considered slow
                {
                    _logger.LogWarning(
                        "Slow response detected: {RequestMethod} {RequestPath} took {DurationMs}ms",
                        context.Request.Method,
                        context.Request.Path,
                        durationMs);
                }
            }
        }

        /// <summary>
        /// Determines whether the request body should be logged based on content type and size.
        /// </summary>
        /// <param name="context">The HTTP context containing the request.</param>
        /// <returns>True if the request body should be logged, false otherwise.</returns>
        private bool ShouldLogRequestBody(HttpContext context)
        {
            var request = context.Request;
            
            // Only log if there's a body
            if (!request.Body.CanRead || !request.Body.CanSeek)
                return false;

            // Check content type
            var contentType = request.ContentType?.ToLower() ?? string.Empty;
            bool isLoggableContentType = contentType.Contains("json") || 
                                       contentType.Contains("xml") || 
                                       contentType.Contains("form") ||
                                       contentType.Contains("text");
            
            if (!isLoggableContentType)
                return false;
                
            // Check content length
            if (request.ContentLength.HasValue && request.ContentLength.Value > MaxBodySizeToLog)
                return false;
                
            return true;
        }

        /// <summary>
        /// Extracts the client IP address from the HTTP context, handling proxies and forwarded headers.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns>The client IP address.</returns>
        private string GetClientIpAddress(HttpContext context)
        {
            string ip = null;
            
            // Try to get the forwarded header first (if the app is behind a proxy)
            if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
            {
                ip = forwardedFor.ToString().Split(',')[0].Trim();
            }
            
            // If no forwarded IP found, use the remote IP from the connection
            if (string.IsNullOrEmpty(ip) && context.Connection.RemoteIpAddress != null)
            {
                ip = context.Connection.RemoteIpAddress.ToString();
            }
            
            return ip ?? "unknown";
        }
    }
}