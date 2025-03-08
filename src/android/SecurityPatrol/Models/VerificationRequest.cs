using System;
using System.ComponentModel.DataAnnotations;

namespace SecurityPatrol.Models
{
    /// <summary>
    /// Represents a request to verify a user's identity using a phone number and verification code.
    /// This is used in the second step of the two-step authentication process where the verification
    /// code received via SMS is validated against the previously submitted phone number.
    /// </summary>
    public class VerificationRequest
    {
        /// <summary>
        /// Gets or sets the phone number associated with the verification request.
        /// This should match the phone number that was used to request the verification code.
        /// Must be in a valid international format (e.g., +1234567890).
        /// </summary>
        [Required(ErrorMessage = "Phone number is required")]
        [RegularExpression(@"^\+[1-9]\d{1,14}$", ErrorMessage = "Please enter a valid phone number with country code")]
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Gets or sets the verification code sent to the user's phone.
        /// Typically a 6-digit numeric code delivered via SMS.
        /// </summary>
        [Required(ErrorMessage = "Verification code is required")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Verification code must be 6 digits")]
        public string VerificationCode { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="VerificationRequest"/> class with empty properties.
        /// </summary>
        public VerificationRequest()
        {
            PhoneNumber = string.Empty;
            VerificationCode = string.Empty;
        }
    }
}