using FluentValidation;
using SecurityPatrol.Core.Constants;
using SecurityPatrol.Core.Models;
using System;
using System.Linq;

namespace SecurityPatrol.Application.Validators
{
    /// <summary>
    /// Validator for the LocationBatchRequest model that ensures the user ID is valid and the location batch 
    /// contains valid location data points.
    /// </summary>
    public class LocationBatchRequestValidator : AbstractValidator<LocationBatchRequest>
    {
        /// <summary>
        /// Initializes a new instance of the LocationBatchRequestValidator class and defines the validation rules.
        /// </summary>
        public LocationBatchRequestValidator()
        {
            // Validate UserId
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage(string.Format(ErrorMessages.Validation_Required, "UserId"));

            // Validate Locations collection
            RuleFor(x => x.Locations)
                .NotNull()
                .NotEmpty().WithMessage(ErrorMessages.Location_BatchEmpty);

            // Validate each location in the collection
            RuleForEach(x => x.Locations).ChildRules(location => {
                // Latitude should be between -90 and 90 degrees
                location.RuleFor(l => l.Latitude)
                    .InclusiveBetween(-90, 90)
                    .WithMessage(ErrorMessages.Location_InvalidCoordinates);

                // Longitude should be between -180 and 180 degrees
                location.RuleFor(l => l.Longitude)
                    .InclusiveBetween(-180, 180)
                    .WithMessage(ErrorMessages.Location_InvalidCoordinates);

                // Accuracy should be greater than 0
                location.RuleFor(l => l.Accuracy)
                    .GreaterThan(0)
                    .WithMessage(string.Format(ErrorMessages.Validation_InvalidFormat, "Accuracy"));

                // Timestamp should not be default/empty
                location.RuleFor(l => l.Timestamp)
                    .NotEqual(default(DateTime))
                    .WithMessage(string.Format(ErrorMessages.Validation_Required, "Timestamp"));

                // Timestamp should not be in the future
                location.RuleFor(l => l.Timestamp)
                    .LessThanOrEqualTo(DateTime.UtcNow)
                    .WithMessage(string.Format(ErrorMessages.Validation_InvalidFormat, "Timestamp"));
            });
        }
    }
}