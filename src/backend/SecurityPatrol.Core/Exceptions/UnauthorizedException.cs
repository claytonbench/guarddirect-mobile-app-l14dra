using System;

namespace SecurityPatrol.Core.Exceptions
{
    /// <summary>
    /// Exception that is thrown when a user attempts to access a resource or perform an operation 
    /// without proper authentication or authorization, resulting in a 401 Unauthorized HTTP response.
    /// </summary>
    public class UnauthorizedException : ApiException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnauthorizedException"/> class with a default message.
        /// </summary>
        public UnauthorizedException() 
            : base(401, "You are not authorized to access this resource")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnauthorizedException"/> class with a custom message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public UnauthorizedException(string message) 
            : base(401, message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnauthorizedException"/> class with a custom message and details.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="details">Additional details about the exception.</param>
        public UnauthorizedException(string message, string details) 
            : base(401, message, details)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnauthorizedException"/> class with a custom message and inner exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public UnauthorizedException(string message, Exception innerException) 
            : base(401, message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnauthorizedException"/> class with a custom message, details, and inner exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="details">Additional details about the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public UnauthorizedException(string message, string details, Exception innerException) 
            : base(401, message, details, innerException)
        {
        }
    }
}