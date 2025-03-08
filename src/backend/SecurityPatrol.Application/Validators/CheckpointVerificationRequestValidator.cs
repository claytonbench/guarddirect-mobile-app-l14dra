using FluentValidation;
using SecurityPatrol.Core.Constants;
using SecurityPatrol.Core.Models;
using System;

namespace SecurityPatrol.Application.Validators
{
    /// <summary>
    /// Validator for the CheckpointVerificationRequest model that ensures the checkpoint ID, timestamp, and location coordinates are valid.
    /// </summary>
    public class CheckpointVerificationRequestValidator : AbstractValidator<CheckpointVerificationRequest>
    {
        /// <summary>
        /// Initializes a new instance of the CheckpointVerificationRequestValidator class and defines the validation rules.
        /// </summary>
        public CheckpointVerificationRequestValidator()
        {
            // Validate CheckpointId
            RuleFor(x => x.CheckpointId)
                .GreaterThan(0)
                .WithMessage(string.Format(ErrorMessages.Validation_Required, "CheckpointId"));

            // Validate Timestamp
            RuleFor(x => x.Timestamp)
                .NotEqual(default(DateTime))
                .WithMessage(string.Format(ErrorMessages.Validation_Required, "Timestamp"));

            // Validate Location
            RuleFor(x => x.Location)
                .NotNull()
                .WithMessage(string.Format(ErrorMessages.Validation_Required, "Location"));

            // Validate Location properties when Location is not null
            When(x => x.Location != null, () =>
            {
                RuleFor(x => x.Location.Latitude)
                    .InclusiveBetween(-90, 90)
                    .WithMessage(ErrorMessages.Location_InvalidCoordinates);

                RuleFor(x => x.Location.Longitude)
                    .InclusiveBetween(-180, 180)
                    .WithMessage(ErrorMessages.Location_InvalidCoordinates);
            });
        }
    }
}