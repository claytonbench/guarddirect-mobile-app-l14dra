using System;
using System.Collections.Generic;

namespace SecurityPatrol.Core.Exceptions
{
    /// <summary>
    /// Exception that is thrown when input data fails validation rules,
    /// resulting in a 400 Bad Request HTTP response with detailed validation error information.
    /// </summary>
    public class ValidationException : ApiException
    {
        /// <summary>
        /// Gets the dictionary of validation errors, where the key is the property name and the value is an array of error messages.
        /// </summary>
        public IDictionary<string, string[]> Errors { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException"/> class with a default message.
        /// </summary>
        public ValidationException()
            : base(400, "One or more validation errors occurred")
        {
            Errors = new Dictionary<string, string[]>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException"/> class with a custom message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public ValidationException(string message)
            : base(400, message)
        {
            Errors = new Dictionary<string, string[]>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException"/> class with a custom message and validation errors dictionary.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="errors">A dictionary of validation errors, where the key is the property name and the value is an array of error messages.</param>
        public ValidationException(string message, IDictionary<string, string[]> errors)
            : base(400, message)
        {
            Errors = errors ?? new Dictionary<string, string[]>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException"/> class with a custom message and inner exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public ValidationException(string message, Exception innerException)
            : base(400, message, innerException)
        {
            Errors = new Dictionary<string, string[]>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException"/> class with a custom message, validation errors dictionary, and inner exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="errors">A dictionary of validation errors, where the key is the property name and the value is an array of error messages.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public ValidationException(string message, IDictionary<string, string[]> errors, Exception innerException)
            : base(400, message, innerException)
        {
            Errors = errors ?? new Dictionary<string, string[]>();
        }
    }
}