using System;
using Xunit;
using FluentAssertions;
using FluentValidation.TestHelper;
using SecurityPatrol.Application.Validators;
using SecurityPatrol.Core.Models;
using SecurityPatrol.Core.Constants;
using SecurityPatrol.API.UnitTests.Setup;

namespace SecurityPatrol.API.UnitTests.Validators
{
    /// <summary>
    /// Contains unit tests for the ReportRequestValidator class to ensure proper validation of activity report submissions.
    /// </summary>
    public class ReportRequestValidatorTests : TestBase
    {
        private readonly ReportRequestValidator validator;

        /// <summary>
        /// Initializes a new instance of the ReportRequestValidatorTests class with a fresh validator instance
        /// </summary>
        public ReportRequestValidatorTests()
        {
            validator = new ReportRequestValidator();
        }

        [Fact]
        public void Should_Validate_Valid_Report()
        {
            // Arrange
            var report = new ReportRequest
            {
                Text = "This is a valid report",
                Timestamp = DateTime.UtcNow.AddMinutes(-5), // Slightly in the past
                Location = new LocationData
                {
                    Latitude = 34.0522,  // Los Angeles
                    Longitude = -118.2437
                }
            };

            // Act
            var result = validator.TestValidate(report);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Should_Fail_When_Text_Empty()
        {
            // Arrange
            var report = new ReportRequest
            {
                Text = "",
                Timestamp = DateTime.UtcNow,
                Location = new LocationData
                {
                    Latitude = 34.0522,
                    Longitude = -118.2437
                }
            };

            // Act
            var result = validator.TestValidate(report);

            // Assert
            result.ShouldHaveValidationErrorFor(r => r.Text)
                .WithErrorMessage(ErrorMessages.Report_TextRequired);
        }

        [Fact]
        public void Should_Fail_When_Text_Exceeds_MaxLength()
        {
            // Arrange
            var report = new ReportRequest
            {
                Text = new string('x', 501), // 501 characters
                Timestamp = DateTime.UtcNow,
                Location = new LocationData
                {
                    Latitude = 34.0522,
                    Longitude = -118.2437
                }
            };

            // Act
            var result = validator.TestValidate(report);

            // Assert
            result.ShouldHaveValidationErrorFor(r => r.Text)
                .WithErrorMessage(ErrorMessages.Report_TextTooLong);
        }

        [Fact]
        public void Should_Fail_When_Timestamp_Default()
        {
            // Arrange
            var report = new ReportRequest
            {
                Text = "Valid text",
                Timestamp = default,
                Location = new LocationData
                {
                    Latitude = 34.0522,
                    Longitude = -118.2437
                }
            };

            // Act
            var result = validator.TestValidate(report);

            // Assert
            result.ShouldHaveValidationErrorFor(r => r.Timestamp);
        }

        [Fact]
        public void Should_Fail_When_Timestamp_Future()
        {
            // Arrange
            var report = new ReportRequest
            {
                Text = "Valid text",
                Timestamp = DateTime.UtcNow.AddDays(1), // Future date
                Location = new LocationData
                {
                    Latitude = 34.0522,
                    Longitude = -118.2437
                }
            };

            // Act
            var result = validator.TestValidate(report);

            // Assert
            result.ShouldHaveValidationErrorFor(r => r.Timestamp);
        }

        [Fact]
        public void Should_Fail_When_Location_Null()
        {
            // Arrange
            var report = new ReportRequest
            {
                Text = "Valid text",
                Timestamp = DateTime.UtcNow,
                Location = null
            };

            // Act
            var result = validator.TestValidate(report);

            // Assert
            result.ShouldHaveValidationErrorFor(r => r.Location);
        }

        [Theory]
        [InlineData(-91)] // Below minimum
        [InlineData(91)]  // Above maximum
        public void Should_Fail_When_Latitude_OutOfRange(double latitude)
        {
            // Arrange
            var report = new ReportRequest
            {
                Text = "Valid text",
                Timestamp = DateTime.UtcNow,
                Location = new LocationData
                {
                    Latitude = latitude,
                    Longitude = 0
                }
            };

            // Act
            var result = validator.TestValidate(report);

            // Assert
            result.ShouldHaveValidationErrorFor(r => r.Location.Latitude)
                .WithErrorMessage(ErrorMessages.Location_InvalidCoordinates);
        }

        [Theory]
        [InlineData(-181)] // Below minimum
        [InlineData(181)]  // Above maximum
        public void Should_Fail_When_Longitude_OutOfRange(double longitude)
        {
            // Arrange
            var report = new ReportRequest
            {
                Text = "Valid text",
                Timestamp = DateTime.UtcNow,
                Location = new LocationData
                {
                    Latitude = 0,
                    Longitude = longitude
                }
            };

            // Act
            var result = validator.TestValidate(report);

            // Assert
            result.ShouldHaveValidationErrorFor(r => r.Location.Longitude)
                .WithErrorMessage(ErrorMessages.Location_InvalidCoordinates);
        }

        [Theory]
        [InlineData(0, 0)]        // Equator/Prime Meridian
        [InlineData(90, 180)]     // Max valid values
        [InlineData(-90, -180)]   // Min valid values
        [InlineData(45.5, -122.5)] // Random valid coordinates
        public void Should_Validate_Valid_Coordinates(double latitude, double longitude)
        {
            // Arrange
            var report = new ReportRequest
            {
                Text = "Valid text",
                Timestamp = DateTime.UtcNow.AddMinutes(-5), // Slightly in the past
                Location = new LocationData
                {
                    Latitude = latitude,
                    Longitude = longitude
                }
            };

            // Act
            var result = validator.TestValidate(report);

            // Assert
            result.ShouldNotHaveValidationErrorFor(r => r.Location.Latitude);
            result.ShouldNotHaveValidationErrorFor(r => r.Location.Longitude);
        }
    }
}