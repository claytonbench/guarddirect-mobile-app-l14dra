using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SecurityPatrol.Core.Exceptions;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace SecurityPatrol.API.Middleware
{
    /// <summary>
    /// Middleware that catches unhandled exceptions in the request pipeline and transforms 
    /// them into standardized HTTP responses with appropriate status codes and error details.
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IHostEnvironment _environment;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionHandlingMiddleware"/> class with required dependencies.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="logger">The logger for recording exception details.</param>
        /// <param name="environment">The hosting environment information.</param>
        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger,
            IHostEnvironment environment)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        /// <summary>
        /// Processes HTTP requests by attempting to execute the request pipeline and catching any exceptions that occur.
        /// </summary>
        /// <param name="context">The current HTTP context.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception exception)
            {
                await HandleExceptionAsync(context, exception);
            }
        }

        /// <summary>
        /// Processes caught exceptions by converting them to appropriate HTTP responses with standardized error formats.
        /// </summary>
        /// <param name="context">The current HTTP context.</param>
        /// <param name="exception">The exception that was caught.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            int statusCode = GetStatusCode(exception);
            
            // Log the exception with appropriate severity
            LogException(exception, context, statusCode);
            
            // Create a standardized error response
            var response = CreateErrorResponse(exception, statusCode);
            
            // Set the response content type and status code
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;
            
            // Serialize and write the response
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = _environment.IsDevelopment()
            }));
        }

        /// <summary>
        /// Determines the appropriate HTTP status code based on the exception type.
        /// </summary>
        /// <param name="exception">The exception to evaluate.</param>
        /// <returns>The HTTP status code.</returns>
        private int GetStatusCode(Exception exception)
        {
            return exception switch
            {
                ApiException apiException => apiException.StatusCode,
                ValidationException => (int)HttpStatusCode.BadRequest,
                UnauthorizedException => (int)HttpStatusCode.Unauthorized,
                NotFoundException => (int)HttpStatusCode.NotFound,
                _ => (int)HttpStatusCode.InternalServerError
            };
        }

        /// <summary>
        /// Creates a standardized error response object based on the exception and environment.
        /// </summary>
        /// <param name="exception">The exception to convert.</param>
        /// <param name="statusCode">The HTTP status code.</param>
        /// <returns>The error response object.</returns>
        private object CreateErrorResponse(Exception exception, int statusCode)
        {
            var response = new
            {
                StatusCode = statusCode,
                Message = exception.Message
            };

            if (exception is ValidationException validationException)
            {
                return new
                {
                    response.StatusCode,
                    response.Message,
                    Errors = validationException.Errors
                };
            }

            if (exception is ApiException apiException && !string.IsNullOrEmpty(apiException.Details))
            {
                return new
                {
                    response.StatusCode,
                    response.Message,
                    Details = apiException.Details
                };
            }

            if (_environment.IsDevelopment())
            {
                return new
                {
                    response.StatusCode,
                    response.Message,
                    Exception = exception.GetType().Name,
                    StackTrace = exception.StackTrace
                };
            }

            return response;
        }

        /// <summary>
        /// Logs the exception with appropriate severity level and contextual information.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="context">The current HTTP context.</param>
        /// <param name="statusCode">The HTTP status code.</param>
        private void LogException(Exception exception, HttpContext context, int statusCode)
        {
            // Create structured logging data with contextual information
            var logData = new
            {
                RequestPath = context.Request.Path,
                RequestMethod = context.Request.Method,
                CorrelationId = context.TraceIdentifier,
                ExceptionType = exception.GetType().Name,
                ExceptionMessage = exception.Message
            };

            // Determine log level based on status code
            // 500+ errors are critical, 400-499 are errors, others are warnings
            if (statusCode >= 500)
            {
                _logger.LogCritical(exception, "Unhandled exception occurred: {RequestPath} ({RequestMethod})", 
                    context.Request.Path, context.Request.Method);
            }
            else if (statusCode >= 400)
            {
                _logger.LogError(exception, "Error processing request: {RequestPath} ({RequestMethod})",
                    context.Request.Path, context.Request.Method);
            }
            else
            {
                _logger.LogWarning(exception, "Warning during request processing: {RequestPath} ({RequestMethod})",
                    context.Request.Path, context.Request.Method);
            }
        }
    }
}