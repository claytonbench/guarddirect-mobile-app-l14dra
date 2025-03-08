using System;
using SecurityPatrol.Core.Exceptions;

namespace SecurityPatrol.Core.Exceptions
{
    /// <summary>
    /// Exception that is thrown when a requested resource cannot be found, resulting in a 404 Not Found HTTP response.
    /// </summary>
    public class NotFoundException : ApiException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NotFoundException"/> class with a default message.
        /// </summary>
        public NotFoundException() 
            : base(404, "The requested resource was not found")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotFoundException"/> class with a custom message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public NotFoundException(string message) 
            : base(404, message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotFoundException"/> class with a custom message and details.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="details">Additional details about the exception.</param>
        public NotFoundException(string message, string details) 
            : base(404, message, details)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotFoundException"/> class with a custom message and inner exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public NotFoundException(string message, Exception innerException) 
            : base(404, message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotFoundException"/> class with a custom message, details, and inner exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="details">Additional details about the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public NotFoundException(string message, string details, Exception innerException) 
            : base(404, message, details, innerException)
        {
        }
    }
}