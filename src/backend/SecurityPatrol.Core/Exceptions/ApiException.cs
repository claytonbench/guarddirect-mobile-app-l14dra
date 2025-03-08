using System;

namespace SecurityPatrol.Core.Exceptions
{
    /// <summary>
    /// Base exception class for all API-related exceptions in the application,
    /// providing standardized error information including HTTP status code and optional details.
    /// </summary>
    public class ApiException : Exception
    {
        /// <summary>
        /// Gets the HTTP status code associated with this exception.
        /// </summary>
        public int StatusCode { get; }

        /// <summary>
        /// Gets additional details about the exception, if available.
        /// </summary>
        public string Details { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiException"/> class with a specified HTTP status code and message.
        /// </summary>
        /// <param name="statusCode">The HTTP status code associated with the error.</param>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public ApiException(int statusCode, string message) 
            : base(message)
        {
            StatusCode = statusCode;
            Details = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiException"/> class with a specified HTTP status code, message, and additional details.
        /// </summary>
        /// <param name="statusCode">The HTTP status code associated with the error.</param>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="details">Additional details about the exception.</param>
        public ApiException(int statusCode, string message, string details) 
            : base(message)
        {
            StatusCode = statusCode;
            Details = details;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiException"/> class with a specified HTTP status code, message, and inner exception.
        /// </summary>
        /// <param name="statusCode">The HTTP status code associated with the error.</param>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public ApiException(int statusCode, string message, Exception innerException) 
            : base(message, innerException)
        {
            StatusCode = statusCode;
            Details = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiException"/> class with a specified HTTP status code, message, details, and inner exception.
        /// </summary>
        /// <param name="statusCode">The HTTP status code associated with the error.</param>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="details">Additional details about the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public ApiException(int statusCode, string message, string details, Exception innerException) 
            : base(message, innerException)
        {
            StatusCode = statusCode;
            Details = details;
        }
    }
}