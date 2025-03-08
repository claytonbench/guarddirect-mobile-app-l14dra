using System;

namespace SecurityPatrol.Models
{
    /// <summary>
    /// Represents the authentication state of a user in the application, 
    /// including authentication status, phone number, and timestamp.
    /// </summary>
    public class AuthState
    {
        /// <summary>
        /// Gets or sets a value indicating whether the user is authenticated.
        /// </summary>
        public bool IsAuthenticated { get; set; }

        /// <summary>
        /// Gets or sets the phone number of the authenticated user.
        /// </summary>
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the user was last authenticated.
        /// </summary>
        public DateTime LastAuthenticated { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthState"/> class with specified authentication parameters.
        /// </summary>
        /// <param name="isAuthenticated">Whether the user is authenticated.</param>
        /// <param name="phoneNumber">The user's phone number.</param>
        /// <param name="lastAuthenticated">The date and time when the user was last authenticated (optional).</param>
        public AuthState(bool isAuthenticated, string phoneNumber, DateTime? lastAuthenticated = null)
        {
            IsAuthenticated = isAuthenticated;
            PhoneNumber = phoneNumber;
            
            if (lastAuthenticated.HasValue)
            {
                LastAuthenticated = lastAuthenticated.Value;
            }
            else if (isAuthenticated)
            {
                LastAuthenticated = DateTime.UtcNow;
            }
            else
            {
                LastAuthenticated = DateTime.MinValue;
            }
        }

        /// <summary>
        /// Creates a new instance of AuthState representing an unauthenticated state.
        /// </summary>
        /// <returns>A new AuthState instance with IsAuthenticated set to false.</returns>
        public static AuthState CreateUnauthenticated()
        {
            return new AuthState(false, null);
        }

        /// <summary>
        /// Creates a new instance of AuthState representing an authenticated state 
        /// with the specified phone number.
        /// </summary>
        /// <param name="phoneNumber">The authenticated user's phone number.</param>
        /// <returns>A new AuthState instance with IsAuthenticated set to true and the specified phone number.</returns>
        /// <exception cref="ArgumentException">Thrown when phoneNumber is null or empty.</exception>
        public static AuthState CreateAuthenticated(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber))
            {
                throw new ArgumentException("Phone number cannot be null or empty for an authenticated state.", nameof(phoneNumber));
            }
            
            return new AuthState(true, phoneNumber, DateTime.UtcNow);
        }
    }
}