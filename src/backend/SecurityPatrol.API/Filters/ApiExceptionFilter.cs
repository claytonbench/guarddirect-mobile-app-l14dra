using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using SecurityPatrol.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Net;

namespace SecurityPatrol.API.Filters
{
    /// <summary>
    /// ASP.NET Core exception filter that catches and handles API exceptions,
    /// transforming them into standardized HTTP responses with appropriate status codes
    /// and problem details format.
    /// </summary>
    public class ApiExceptionFilter : ExceptionFilterAttribute
    {
        private readonly ILogger<ApiExceptionFilter> _logger;
        private readonly IHostEnvironment _environment;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiExceptionFilter"/> class.
        /// </summary>
        /// <param name="logger">The logger instance for recording exception information.</param>
        /// <param name="environment">The host environment to determine if in development mode.</param>
        public ApiExceptionFilter(ILogger<ApiExceptionFilter> logger, IHostEnvironment environment)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        /// <summary>
        /// Executes when an unhandled exception occurs during action execution.
        /// </summary>
        /// <param name="context">The exception context containing information about the current request and exception.</param>
        public override void OnException(ExceptionContext context)
        {
            var exception = context.Exception;

            // Log the exception with appropriate severity level
            if (exception is NotFoundException)
            {
                _logger.LogWarning(exception, "Resource not found: {Message}", exception.Message);
            }
            else if (exception is ValidationException)
            {
                _logger.LogWarning(exception, "Validation error: {Message}", exception.Message);
            }
            else if (exception is UnauthorizedException)
            {
                _logger.LogWarning(exception, "Unauthorized access attempt: {Message}", exception.Message);
            }
            else if (exception is ApiException apiEx && apiEx.StatusCode < 500)
            {
                _logger.LogWarning(exception, "API error: {Message}", exception.Message);
            }
            else
            {
                _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);
            }

            // Determine the HTTP status code based on exception type
            int statusCode;
            if (exception is ValidationException)
            {
                statusCode = StatusCodes.Status400BadRequest;
            }
            else if (exception is NotFoundException)
            {
                statusCode = StatusCodes.Status404NotFound;
            }
            else if (exception is UnauthorizedException)
            {
                statusCode = StatusCodes.Status401Unauthorized;
            }
            else if (exception is ApiException apiException)
            {
                statusCode = apiException.StatusCode;
            }
            else
            {
                statusCode = StatusCodes.Status500InternalServerError;
            }

            // Create problem details with error information
            var problemDetails = CreateProblemDetails(
                exception,
                statusCode,
                _environment.IsDevelopment());

            // Set the response
            context.Result = new JsonResult(problemDetails)
            {
                StatusCode = statusCode
            };

            // Mark the exception as handled to prevent further processing
            context.ExceptionHandled = true;
        }

        /// <summary>
        /// Creates a standardized problem details object for API error responses.
        /// </summary>
        /// <param name="exception">The exception to convert.</param>
        /// <param name="statusCode">The HTTP status code to use.</param>
        /// <param name="includeDetails">Whether to include detailed exception information (for development environments).</param>
        /// <returns>A problem details object with standardized error information.</returns>
        private ProblemDetails CreateProblemDetails(Exception exception, int statusCode, bool includeDetails)
        {
            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = GetTitleForStatusCode(statusCode),
                Detail = exception.Message,
                Instance = Guid.NewGuid().ToString()
            };

            // Handle validation exception and include validation errors dictionary
            if (exception is ValidationException validationException)
            {
                // We're making an assumption that ValidationException has an Errors property
                // that contains validation errors. Adjust based on your actual implementation.
                var validationErrors = validationException.GetType().GetProperty("Errors")?.GetValue(validationException);
                if (validationErrors != null)
                {
                    problemDetails.Extensions["validationErrors"] = validationErrors;
                }
            }

            // Include details from ApiException
            if (exception is ApiException apiException && !string.IsNullOrEmpty(apiException.Details))
            {
                problemDetails.Extensions["details"] = apiException.Details;
            }

            // Include stack trace and exception type in development environment
            if (includeDetails)
            {
                problemDetails.Extensions["exceptionType"] = exception.GetType().Name;
                problemDetails.Extensions["stackTrace"] = exception.StackTrace;
                
                // Include inner exception if present
                if (exception.InnerException != null)
                {
                    problemDetails.Extensions["innerException"] = new
                    {
                        message = exception.InnerException.Message,
                        type = exception.InnerException.GetType().Name,
                        stackTrace = exception.InnerException.StackTrace
                    };
                }
            }

            return problemDetails;
        }

        /// <summary>
        /// Gets a title for the problem details based on the status code.
        /// </summary>
        /// <param name="statusCode">The HTTP status code.</param>
        /// <returns>A human-readable title for the given status code.</returns>
        private string GetTitleForStatusCode(int statusCode)
        {
            return statusCode switch
            {
                StatusCodes.Status400BadRequest => "Bad Request",
                StatusCodes.Status401Unauthorized => "Unauthorized",
                StatusCodes.Status403Forbidden => "Forbidden",
                StatusCodes.Status404NotFound => "Not Found",
                StatusCodes.Status409Conflict => "Conflict",
                StatusCodes.Status422UnprocessableEntity => "Unprocessable Entity",
                StatusCodes.Status500InternalServerError => "Internal Server Error",
                StatusCodes.Status503ServiceUnavailable => "Service Unavailable",
                _ => "An error occurred"
            };
        }
    }
}