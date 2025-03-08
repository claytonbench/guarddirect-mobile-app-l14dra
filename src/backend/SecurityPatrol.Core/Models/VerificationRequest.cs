using System;
using System.ComponentModel.DataAnnotations;

namespace SecurityPatrol.Core.Models
{
    /// <summary>
    /// Represents a request to verify a user's identity using a phone number and verification code.
    /// This is used in the second step of authentication to validate the verification code sent to the user's phone.
    /// </summary>
    public class VerificationRequest
    {
        /// <summary>
        /// Gets or sets the phone number of the user attempting to authenticate.
        /// </summary>
        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number format")]
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Gets or sets the verification code received by the user via SMS.
        /// </summary>
        [Required(ErrorMessage = "Verification code is required")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Verification code must be 6 digits")]
        [RegularExpression(@"^\d+$", ErrorMessage = "Verification code must contain only digits")]
        public string Code { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="VerificationRequest"/> class.
        /// </summary>
        public VerificationRequest()
        {
            PhoneNumber = string.Empty;
            Code = string.Empty;
        }
    }
}