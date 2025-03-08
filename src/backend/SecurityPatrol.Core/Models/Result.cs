using System;
using System.Collections.Generic;

namespace SecurityPatrol.Core.Models
{
    /// <summary>
    /// Generic result class that encapsulates the outcome of operations,
    /// providing a standardized way to handle success and error states.
    /// </summary>
    public class Result
    {
        /// <summary>
        /// Gets a value indicating whether the operation succeeded.
        /// </summary>
        public bool Succeeded { get; private set; }

        /// <summary>
        /// Gets a message describing the result of the operation.
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        /// Gets a list of error messages when the operation fails.
        /// </summary>
        public List<string> Errors { get; private set; }

        /// <summary>
        /// Gets optional data associated with the result.
        /// </summary>
        public object Data { get; private set; }

        /// <summary>
        /// Private constructor to enforce the use of factory methods for creating Result instances.
        /// </summary>
        private Result()
        {
            Errors = new List<string>();
            Succeeded = false;
            Message = null;
            Data = null;
        }

        /// <summary>
        /// Creates a successful result with no data payload.
        /// </summary>
        /// <returns>A successful result with no data.</returns>
        public static Result Success()
        {
            return new Result { Succeeded = true };
        }

        /// <summary>
        /// Creates a successful result with a message.
        /// </summary>
        /// <param name="message">A message describing the successful operation.</param>
        /// <returns>A successful result with the specified message.</returns>
        public static Result Success(string message)
        {
            return new Result
            {
                Succeeded = true,
                Message = message
            };
        }

        /// <summary>
        /// Creates a successful result with a data payload.
        /// </summary>
        /// <param name="data">The data to include in the result.</param>
        /// <returns>A successful result containing the specified data.</returns>
        public static Result Success(object data)
        {
            return new Result
            {
                Succeeded = true,
                Data = data
            };
        }

        /// <summary>
        /// Creates a successful result with a data payload and message.
        /// </summary>
        /// <param name="data">The data to include in the result.</param>
        /// <param name="message">A message describing the successful operation.</param>
        /// <returns>A successful result containing the specified data and message.</returns>
        public static Result Success(object data, string message)
        {
            return new Result
            {
                Succeeded = true,
                Data = data,
                Message = message
            };
        }

        /// <summary>
        /// Creates a strongly-typed successful result with a data payload.
        /// </summary>
        /// <typeparam name="T">The type of the data payload.</typeparam>
        /// <param name="data">The data to include in the result.</param>
        /// <returns>A successful result containing the specified data.</returns>
        public static Result<T> Success<T>(T data)
        {
            return new Result<T>
            {
                Succeeded = true,
                Data = data
            };
        }

        /// <summary>
        /// Creates a strongly-typed successful result with a data payload and message.
        /// </summary>
        /// <typeparam name="T">The type of the data payload.</typeparam>
        /// <param name="data">The data to include in the result.</param>
        /// <param name="message">A message describing the successful operation.</param>
        /// <returns>A successful result containing the specified data and message.</returns>
        public static Result<T> Success<T>(T data, string message)
        {
            return new Result<T>
            {
                Succeeded = true,
                Data = data,
                Message = message
            };
        }

        /// <summary>
        /// Creates a failed result with no specific error.
        /// </summary>
        /// <returns>A failed result.</returns>
        public static Result Failure()
        {
            return new Result { Succeeded = false };
        }

        /// <summary>
        /// Creates a failed result with an error message.
        /// </summary>
        /// <param name="message">The error message describing why the operation failed.</param>
        /// <returns>A failed result with the specified error message.</returns>
        public static Result Failure(string message)
        {
            return new Result
            {
                Succeeded = false,
                Message = message
            };
        }

        /// <summary>
        /// Creates a failed result with multiple error messages.
        /// </summary>
        /// <param name="errors">A collection of error messages.</param>
        /// <returns>A failed result with the specified error messages.</returns>
        public static Result Failure(IEnumerable<string> errors)
        {
            var result = new Result { Succeeded = false };
            
            if (errors != null)
            {
                result.Errors.AddRange(errors);
            }
            
            return result;
        }

        /// <summary>
        /// Creates a failed result with an error message and multiple error details.
        /// </summary>
        /// <param name="message">The general error message.</param>
        /// <param name="errors">A collection of detailed error messages.</param>
        /// <returns>A failed result with the specified message and error details.</returns>
        public static Result Failure(string message, IEnumerable<string> errors)
        {
            var result = new Result
            {
                Succeeded = false,
                Message = message
            };
            
            if (errors != null)
            {
                result.Errors.AddRange(errors);
            }
            
            return result;
        }

        /// <summary>
        /// Creates a strongly-typed failed result with an error message.
        /// </summary>
        /// <typeparam name="T">The type of the data payload (which will be default).</typeparam>
        /// <param name="message">The error message describing why the operation failed.</param>
        /// <returns>A failed result with the specified error message.</returns>
        public static Result<T> Failure<T>(string message)
        {
            return new Result<T>
            {
                Succeeded = false,
                Message = message
            };
        }

        /// <summary>
        /// Creates a strongly-typed failed result with multiple error messages.
        /// </summary>
        /// <typeparam name="T">The type of the data payload (which will be default).</typeparam>
        /// <param name="errors">A collection of error messages.</param>
        /// <returns>A failed result with the specified error messages.</returns>
        public static Result<T> Failure<T>(IEnumerable<string> errors)
        {
            var result = new Result<T> { Succeeded = false };
            
            if (errors != null)
            {
                result.Errors.AddRange(errors);
            }
            
            return result;
        }

        /// <summary>
        /// Creates a strongly-typed failed result with an error message and multiple error details.
        /// </summary>
        /// <typeparam name="T">The type of the data payload (which will be default).</typeparam>
        /// <param name="message">The general error message.</param>
        /// <param name="errors">A collection of detailed error messages.</param>
        /// <returns>A failed result with the specified message and error details.</returns>
        public static Result<T> Failure<T>(string message, IEnumerable<string> errors)
        {
            var result = new Result<T>
            {
                Succeeded = false,
                Message = message
            };
            
            if (errors != null)
            {
                result.Errors.AddRange(errors);
            }
            
            return result;
        }
    }

    /// <summary>
    /// Generic result class with strongly-typed data that encapsulates the outcome 
    /// of operations, providing a standardized way to handle success and error states.
    /// </summary>
    /// <typeparam name="T">The type of the data payload.</typeparam>
    public class Result<T>
    {
        /// <summary>
        /// Gets a value indicating whether the operation succeeded.
        /// </summary>
        public bool Succeeded { get; internal set; }

        /// <summary>
        /// Gets a message describing the result of the operation.
        /// </summary>
        public string Message { get; internal set; }

        /// <summary>
        /// Gets a list of error messages when the operation fails.
        /// </summary>
        public List<string> Errors { get; internal set; }

        /// <summary>
        /// Gets the strongly-typed data payload associated with the result.
        /// </summary>
        public T Data { get; internal set; }

        /// <summary>
        /// Private constructor to enforce the use of factory methods for creating Result&lt;T&gt; instances.
        /// </summary>
        internal Result()
        {
            Errors = new List<string>();
            Succeeded = false;
            Message = null;
            Data = default(T);
        }
    }
}