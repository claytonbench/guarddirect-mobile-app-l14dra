using System;
using Xunit;
using FluentAssertions;
using FluentValidation.TestHelper;
using SecurityPatrol.Application.Validators;
using SecurityPatrol.Core.Models;
using SecurityPatrol.Core.Constants;
using SecurityPatrol.API.UnitTests.Setup;
using SecurityPatrol.TestCommon.Constants;

namespace SecurityPatrol.API.UnitTests.Validators
{
    /// <summary>
    /// Contains unit tests for the TimeRecordRequestValidator class to ensure proper validation of TimeRecordRequest objects.
    /// </summary>
    public class TimeRecordRequestValidatorTests : TestBase
    {
        private readonly TimeRecordRequestValidator validator;

        /// <summary>
        /// Initializes a new instance of the TimeRecordRequestValidatorTests class with a fresh validator instance
        /// </summary>
        public TimeRecordRequestValidatorTests()
        {
            validator = new TimeRecordRequestValidator();
        }

        /// <summary>
        /// Tests that a valid TimeRecordRequest passes validation
        /// </summary>
        [Fact]
        public void Should_Validate_Valid_TimeRecordRequest()
        {
            // Arrange
            var request = CreateValidRequest();

            // Act
            var result = validator.TestValidate(request);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        /// <summary>
        /// Tests that a valid clock out request passes validation
        /// </summary>
        [Fact]
        public void Should_Validate_Valid_ClockOut_Request()
        {
            // Arrange
            var request = CreateValidRequest();
            request.Type = "ClockOut";

            // Act
            var result = validator.TestValidate(request);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        /// <summary>
        /// Tests that validation fails when Type is empty
        /// </summary>
        [Fact]
        public void Should_Fail_When_Type_Is_Empty()
        {
            // Arrange
            var request = CreateValidRequest();
            request.Type = string.Empty;

            // Act
            var result = validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Type)
                .WithErrorMessage(string.Format(ErrorMessages.Validation_Required, "Type"));
        }

        /// <summary>
        /// Tests that validation fails when Type is not 'in' or 'out'
        /// </summary>
        [Fact]
        public void Should_Fail_When_Type_Is_Invalid()
        {
            // Arrange
            var request = CreateValidRequest();
            request.Type = "invalid";

            // Act
            var result = validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Type)
                .WithErrorMessage(ErrorMessages.TimeRecord_InvalidType);
        }

        /// <summary>
        /// Tests that validation fails when Timestamp is default/empty
        /// </summary>
        [Fact]
        public void Should_Fail_When_Timestamp_Is_Default()
        {
            // Arrange
            var request = CreateValidRequest();
            request.Timestamp = default;

            // Act
            var result = validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Timestamp);
        }

        /// <summary>
        /// Tests that validation fails when Timestamp is in the future
        /// </summary>
        [Fact]
        public void Should_Fail_When_Timestamp_Is_Future()
        {
            // Arrange
            var request = CreateValidRequest();
            request.Timestamp = DateTime.UtcNow.AddDays(1);

            // Act
            var result = validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Timestamp);
        }

        /// <summary>
        /// Tests that validation fails when Location is null
        /// </summary>
        [Fact]
        public void Should_Fail_When_Location_Is_Null()
        {
            // Arrange
            var request = CreateValidRequest();
            request.Location = null;

            // Act
            var result = validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Location);
        }

        /// <summary>
        /// Tests that validation fails when Latitude is outside valid range
        /// </summary>
        [Fact]
        public void Should_Fail_When_Latitude_Is_Invalid()
        {
            // Arrange
            var request = CreateValidRequest();
            request.Location.Latitude = 100; // Outside valid range of -90 to 90

            // Act
            var result = validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Location.Latitude)
                .WithErrorMessage(ErrorMessages.Location_InvalidCoordinates);
        }

        /// <summary>
        /// Tests that validation fails when Longitude is outside valid range
        /// </summary>
        [Fact]
        public void Should_Fail_When_Longitude_Is_Invalid()
        {
            // Arrange
            var request = CreateValidRequest();
            request.Location.Longitude = 200; // Outside valid range of -180 to 180

            // Act
            var result = validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Location.Longitude)
                .WithErrorMessage(ErrorMessages.Location_InvalidCoordinates);
        }

        /// <summary>
        /// Tests that validation accepts case-insensitive Type values
        /// </summary>
        [Theory]
        [InlineData("ClockIn")]
        [InlineData("clockin")]
        [InlineData("CLOCKIN")]
        [InlineData("ClockOut")]
        [InlineData("clockout")]
        [InlineData("CLOCKOUT")]
        public void Should_Accept_Case_Insensitive_Type(string typeValue)
        {
            // Arrange
            var request = CreateValidRequest();
            request.Type = typeValue;

            // Act
            var result = validator.TestValidate(request);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        /// <summary>
        /// Helper method to create a valid TimeRecordRequest for testing
        /// </summary>
        /// <returns>A valid TimeRecordRequest object</returns>
        private TimeRecordRequest CreateValidRequest()
        {
            return new TimeRecordRequest
            {
                Type = "ClockIn",
                Timestamp = DateTime.UtcNow,
                Location = new LocationModel
                {
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                }
            };
        }
    }
}