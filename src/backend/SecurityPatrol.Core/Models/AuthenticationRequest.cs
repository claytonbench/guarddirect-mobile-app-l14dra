using System;
using System.ComponentModel.DataAnnotations;

namespace SecurityPatrol.Core.Models
{
    /// <summary>
    /// Represents a request to authenticate a user by phone number.
    /// This is used in the first step of authentication to request a verification code.
    /// </summary>
    public class AuthenticationRequest
    {
        /// <summary>
        /// Gets or sets the phone number to be authenticated.
        /// Must be in international format with country code (e.g., +1234567890).
        /// </summary>
        [Required(ErrorMessage = "Phone number is required")]
        [RegularExpression(@"^\+[1-9]\d{1,14}$", ErrorMessage = "Please enter a valid phone number with country code (e.g., +1234567890)")]
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationRequest"/> class.
        /// </summary>
        public AuthenticationRequest()
        {
            PhoneNumber = string.Empty;
        }
    }
}