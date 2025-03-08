using System;

namespace SecurityPatrol.Models
{
    /// <summary>
    /// Represents the response from the authentication API after successful verification of a user's identity.
    /// Contains the JWT token and its expiration time, which are used for subsequent authenticated API requests.
    /// </summary>
    public class AuthenticationResponse
    {
        /// <summary>
        /// Gets or sets the JWT authentication token used for authenticating API requests.
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Gets or sets the expiration date and time of the authentication token.
        /// Used to determine when a token refresh is required.
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationResponse"/> class
        /// with default empty values.
        /// </summary>
        public AuthenticationResponse()
        {
            Token = string.Empty;
            ExpiresAt = DateTime.MinValue;
        }
    }
}