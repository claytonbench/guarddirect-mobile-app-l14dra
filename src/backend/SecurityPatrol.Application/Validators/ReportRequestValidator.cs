using FluentValidation; // v11.0.0 - Framework for building strongly-typed validation rules
using SecurityPatrol.Core.Constants;
using SecurityPatrol.Core.Models;
using System; // .NET 8.0.0 - For DateTime and other basic types

namespace SecurityPatrol.Application.Validators
{
    /// <summary>
    /// Validator for the ReportRequest model that ensures the report text is valid and the location data is properly formatted.
    /// </summary>
    public class ReportRequestValidator : AbstractValidator<ReportRequest>
    {
        /// <summary>
        /// Initializes a new instance of the ReportRequestValidator class and defines the validation rules.
        /// </summary>
        public ReportRequestValidator()
        {
            // Validate Text property
            RuleFor(x => x.Text)
                .NotEmpty().WithMessage(ErrorMessages.Report_TextRequired)
                .MaximumLength(500).WithMessage(ErrorMessages.Report_TextTooLong);

            // Validate Timestamp property
            RuleFor(x => x.Timestamp)
                .NotEqual(default(DateTime)).WithMessage(string.Format(ErrorMessages.Validation_Required, "Timestamp"))
                .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("Timestamp cannot be in the future.");

            // Validate Location property
            RuleFor(x => x.Location)
                .NotNull().WithMessage(string.Format(ErrorMessages.Validation_Required, "Location"));

            // Validate Location.Latitude and Longitude when Location is not null
            When(x => x.Location != null, () =>
            {
                RuleFor(x => x.Location.Latitude)
                    .InclusiveBetween(-90, 90).WithMessage(ErrorMessages.Location_InvalidCoordinates);

                RuleFor(x => x.Location.Longitude)
                    .InclusiveBetween(-180, 180).WithMessage(ErrorMessages.Location_InvalidCoordinates);
            });
        }
    }
}