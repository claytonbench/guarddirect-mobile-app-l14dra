using FluentValidation;
using SecurityPatrol.Core.Constants;
using SecurityPatrol.Core.Models;
using System.Text.RegularExpressions;

namespace SecurityPatrol.Application.Validators
{
    /// <summary>
    /// Validator for the AuthenticationRequest model that ensures the phone number is in a valid format
    /// for the authentication process. This validator is used in the first step of the two-step
    /// authentication process where a user provides their phone number to request a verification code.
    /// </summary>
    public class AuthenticationRequestValidator : AbstractValidator<AuthenticationRequest>
    {
        /// <summary>
        /// Initializes a new instance of the AuthenticationRequestValidator class and defines the validation rules.
        /// </summary>
        public AuthenticationRequestValidator()
        {
            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage(string.Format(ErrorMessages.Validation_Required, "Phone Number"))
                .Matches(@"^\+[1-9]\d{1,14}$").WithMessage(ErrorMessages.Auth_InvalidPhoneNumber);
        }
    }
}