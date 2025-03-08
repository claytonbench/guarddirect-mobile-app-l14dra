using FluentValidation; // version 11.0.0
using SecurityPatrol.Core.Constants;
using SecurityPatrol.Core.Models;
using System.Text.RegularExpressions; // version 8.0.0

namespace SecurityPatrol.Application.Validators
{
    /// <summary>
    /// Validator for the VerificationRequest model that ensures the phone number is in a valid format
    /// and the verification code meets the required format for the verification process.
    /// </summary>
    public class VerificationRequestValidator : AbstractValidator<VerificationRequest>
    {
        /// <summary>
        /// Initializes a new instance of the VerificationRequestValidator class and defines the validation rules.
        /// </summary>
        public VerificationRequestValidator()
        {
            // Define validation rules for PhoneNumber
            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage(string.Format(ErrorMessages.Validation_Required, "phone number"))
                .Matches(@"^\+[1-9]\d{1,14}$").WithMessage(ErrorMessages.Auth_InvalidPhoneNumber)
                .WithName("phone number");

            // Define validation rules for verification code
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage(string.Format(ErrorMessages.Validation_Required, "verification code"))
                .Matches(@"^\d{6}$").WithMessage(ErrorMessages.Auth_InvalidVerificationCode)
                .WithName("verification code");
        }
    }
}