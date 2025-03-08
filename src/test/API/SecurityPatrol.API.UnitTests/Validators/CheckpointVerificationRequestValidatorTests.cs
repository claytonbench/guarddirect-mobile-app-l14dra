using System;
using Xunit; // xunit 2.4.0
using FluentAssertions; // FluentAssertions 6.0.0
using FluentValidation.TestHelper; // FluentValidation.TestHelper 11.0.0
using SecurityPatrol.Application.Validators;
using SecurityPatrol.Core.Models;
using SecurityPatrol.Core.Constants;
using SecurityPatrol.API.UnitTests.Setup;
using SecurityPatrol.TestCommon.Constants;

namespace SecurityPatrol.API.UnitTests.Validators
{
    /// <summary>
    /// Contains unit tests for the CheckpointVerificationRequestValidator class to ensure proper validation of checkpoint verification requests.
    /// </summary>
    public class CheckpointVerificationRequestValidatorTests : TestBase
    {
        private readonly CheckpointVerificationRequestValidator validator;

        /// <summary>
        /// Initializes a new instance of the CheckpointVerificationRequestValidatorTests class
        /// </summary>
        public CheckpointVerificationRequestValidatorTests()
        {
            validator = new CheckpointVerificationRequestValidator();
        }

        [Fact]
        public void Test_CheckpointId_ShouldHaveError_WhenZero()
        {
            // Arrange
            var request = CreateValidRequest();
            request.CheckpointId = 0;

            // Act & Assert
            var result = validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.CheckpointId)
                  .WithErrorMessage(string.Format(ErrorMessages.Validation_Required, "CheckpointId"));
        }

        [Fact]
        public void Test_CheckpointId_ShouldHaveError_WhenNegative()
        {
            // Arrange
            var request = CreateValidRequest();
            request.CheckpointId = -1;

            // Act & Assert
            var result = validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.CheckpointId)
                  .WithErrorMessage(string.Format(ErrorMessages.Validation_Required, "CheckpointId"));
        }

        [Fact]
        public void Test_CheckpointId_ShouldNotHaveError_WhenPositive()
        {
            // Arrange
            var request = CreateValidRequest();
            request.CheckpointId = TestConstants.TestCheckpointId;

            // Act & Assert
            var result = validator.TestValidate(request);
            result.ShouldNotHaveValidationErrorFor(x => x.CheckpointId);
        }

        [Fact]
        public void Test_Timestamp_ShouldHaveError_WhenDefault()
        {
            // Arrange
            var request = CreateValidRequest();
            request.Timestamp = default;

            // Act & Assert
            var result = validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.Timestamp)
                  .WithErrorMessage(string.Format(ErrorMessages.Validation_Required, "Timestamp"));
        }

        [Fact]
        public void Test_Timestamp_ShouldNotHaveError_WhenValid()
        {
            // Arrange
            var request = CreateValidRequest();
            request.Timestamp = DateTime.UtcNow;

            // Act & Assert
            var result = validator.TestValidate(request);
            result.ShouldNotHaveValidationErrorFor(x => x.Timestamp);
        }

        [Fact]
        public void Test_Location_ShouldHaveError_WhenNull()
        {
            // Arrange
            var request = CreateValidRequest();
            request.Location = null;

            // Act & Assert
            var result = validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.Location)
                  .WithErrorMessage(string.Format(ErrorMessages.Validation_Required, "Location"));
        }

        [Theory]
        [InlineData(-91)]
        [InlineData(91)]
        public void Test_Location_Latitude_ShouldHaveError_WhenOutOfRange(double latitude)
        {
            // Arrange
            var request = CreateValidRequest();
            request.Location.Latitude = latitude;

            // Act & Assert
            var result = validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.Location.Latitude)
                  .WithErrorMessage(ErrorMessages.Location_InvalidCoordinates);
        }

        [Theory]
        [InlineData(-181)]
        [InlineData(181)]
        public void Test_Location_Longitude_ShouldHaveError_WhenOutOfRange(double longitude)
        {
            // Arrange
            var request = CreateValidRequest();
            request.Location.Longitude = longitude;

            // Act & Assert
            var result = validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.Location.Longitude)
                  .WithErrorMessage(ErrorMessages.Location_InvalidCoordinates);
        }

        [Fact]
        public void Test_Location_Coordinates_ShouldNotHaveError_WhenInRange()
        {
            // Arrange
            var request = CreateValidRequest();
            request.Location.Latitude = TestConstants.TestLatitude;    // Valid latitude
            request.Location.Longitude = TestConstants.TestLongitude;  // Valid longitude

            // Act & Assert
            var result = validator.TestValidate(request);
            result.ShouldNotHaveValidationErrorFor(x => x.Location.Latitude);
            result.ShouldNotHaveValidationErrorFor(x => x.Location.Longitude);
        }

        [Fact]
        public void Test_ValidRequest_ShouldPassValidation()
        {
            // Arrange
            var request = CreateValidRequest();

            // Act & Assert
            var result = validator.TestValidate(request);
            result.ShouldNotHaveAnyValidationErrors();
        }

        private CheckpointVerificationRequest CreateValidRequest()
        {
            return new CheckpointVerificationRequest
            {
                CheckpointId = TestConstants.TestCheckpointId,
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