using System;
using System.ComponentModel.DataAnnotations;

namespace SecurityPatrol.Models
{
    /// <summary>
    /// Represents a request to initiate the authentication process by sending a phone number 
    /// to the authentication API. This is used in the first step of the two-step authentication 
    /// process where a verification code is requested to be sent to the user's phone number.
    /// </summary>
    public class AuthenticationRequest
    {
        /// <summary>
        /// The phone number to authenticate with. Should include country code (e.g., +12345678901).
        /// This is the primary identifier for the user in the authentication process.
        /// </summary>
        [Required(ErrorMessage = "Phone number is required")]
        [RegularExpression(@"^\+[1-9]\d{1,14}$", ErrorMessage = "Please enter a valid phone number with country code (e.g., +12345678901)")]
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Default constructor that initializes an empty authentication request
        /// </summary>
        public AuthenticationRequest()
        {
            PhoneNumber = string.Empty;
        }
    }
}