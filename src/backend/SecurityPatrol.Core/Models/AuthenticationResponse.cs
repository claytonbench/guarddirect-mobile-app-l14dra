using System;

namespace SecurityPatrol.Core.Models
{
    /// <summary>
    /// Represents the response returned after successful authentication, containing the JWT token and its expiration time.
    /// </summary>
    public class AuthenticationResponse
    {
        /// <summary>
        /// Gets or sets the JWT token used for API authentication.
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the token expires.
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Default constructor that initializes an empty authentication response.
        /// </summary>
        public AuthenticationResponse()
        {
            Token = string.Empty;
            ExpiresAt = DateTime.MinValue;
        }
    }
}