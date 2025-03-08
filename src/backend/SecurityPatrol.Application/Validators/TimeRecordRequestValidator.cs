using FluentValidation; // FluentValidation 11.0.0
using SecurityPatrol.Core.Constants;
using SecurityPatrol.Core.Models;
using System;

namespace SecurityPatrol.Application.Validators
{
    /// <summary>
    /// Validator for the TimeRecordRequest model that ensures the time record type is valid, 
    /// the timestamp is reasonable, and the location coordinates are within valid ranges.
    /// </summary>
    public class TimeRecordRequestValidator : AbstractValidator<TimeRecordRequest>
    {
        /// <summary>
        /// Initializes a new instance of the TimeRecordRequestValidator class and defines the validation rules.
        /// </summary>
        public TimeRecordRequestValidator()
        {
            // Define rule for Type property
            RuleFor(x => x.Type)
                .NotEmpty().WithMessage(string.Format(ErrorMessages.Validation_Required, "Type"))
                .Must(type => string.Equals(type, "ClockIn", StringComparison.OrdinalIgnoreCase) || 
                              string.Equals(type, "ClockOut", StringComparison.OrdinalIgnoreCase))
                .WithMessage(ErrorMessages.TimeRecord_InvalidType);

            // Define rule for Timestamp property
            RuleFor(x => x.Timestamp)
                .NotEmpty().WithMessage(string.Format(ErrorMessages.Validation_Required, "Timestamp"))
                .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("Timestamp cannot be in the future");

            // Define rule for Location property
            RuleFor(x => x.Location)
                .NotNull().WithMessage(string.Format(ErrorMessages.Validation_Required, "Location"));

            // When Location is not null, validate its properties
            When(x => x.Location != null, () =>
            {
                // Define rules for Location.Latitude
                RuleFor(x => x.Location.Latitude)
                    .InclusiveBetween(-90, 90)
                    .WithMessage(ErrorMessages.Location_InvalidCoordinates);

                // Define rules for Location.Longitude
                RuleFor(x => x.Location.Longitude)
                    .InclusiveBetween(-180, 180)
                    .WithMessage(ErrorMessages.Location_InvalidCoordinates);
            });
        }
    }
}