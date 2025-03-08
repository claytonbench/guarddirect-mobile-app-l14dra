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
    /// Contains unit tests for the AuthenticationRequestValidator class to ensure proper validation of phone numbers in authentication requests.
    /// </summary>
    public class AuthenticationRequestValidatorTests : TestBase
    {
        private readonly AuthenticationRequestValidator validator;

        /// <summary>
        /// Initializes a new instance of the AuthenticationRequestValidatorTests class with a fresh validator instance
        /// </summary>
        public AuthenticationRequestValidatorTests()
        {
            validator = new AuthenticationRequestValidator();
        }

        /// <summary>
        /// Tests that a valid phone number passes validation
        /// </summary>
        [Fact]
        public void Should_Validate_Valid_PhoneNumber()
        {
            // Arrange
            var request = new AuthenticationRequest { PhoneNumber = TestConstants.TestPhoneNumber };

            // Act & Assert
            validator.TestValidate(request).ShouldNotHaveAnyValidationErrors();
        }

        /// <summary>
        /// Tests that validation fails when phone number is empty
        /// </summary>
        [Fact]
        public void Should_Fail_When_PhoneNumber_Empty()
        {
            // Arrange
            var request = new AuthenticationRequest { PhoneNumber = string.Empty };

            // Act & Assert
            var result = validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.PhoneNumber)
                .WithErrorMessage(string.Format(ErrorMessages.Validation_Required, "Phone Number"));
        }

        /// <summary>
        /// Tests that validation fails when phone number format is invalid
        /// </summary>
        [Fact]
        public void Should_Fail_When_PhoneNumber_Invalid_Format()
        {
            // Arrange
            var request = new AuthenticationRequest { PhoneNumber = "12345" };

            // Act & Assert
            var result = validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.PhoneNumber)
                .WithErrorMessage(ErrorMessages.Auth_InvalidPhoneNumber);
        }

        /// <summary>
        /// Tests that validation fails when phone number is missing the plus sign prefix
        /// </summary>
        [Fact]
        public void Should_Fail_When_PhoneNumber_Missing_Plus()
        {
            // Arrange
            var request = new AuthenticationRequest { PhoneNumber = "12345678901" };

            // Act & Assert
            var result = validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.PhoneNumber)
                .WithErrorMessage(ErrorMessages.Auth_InvalidPhoneNumber);
        }

        /// <summary>
        /// Tests that validation fails when phone number contains invalid characters
        /// </summary>
        [Fact]
        public void Should_Fail_When_PhoneNumber_Contains_Invalid_Characters()
        {
            // Arrange
            var request = new AuthenticationRequest { PhoneNumber = "+123-456-7890" };

            // Act & Assert
            var result = validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.PhoneNumber)
                .WithErrorMessage(ErrorMessages.Auth_InvalidPhoneNumber);
        }

        /// <summary>
        /// Tests that validation passes for valid phone numbers with different country codes
        /// </summary>
        /// <param name="phoneNumber">The phone number to test</param>
        [Theory]
        [InlineData("+1234567890")]
        [InlineData("+441234567890")]
        [InlineData("+61412345678")]
        public void Should_Validate_Different_Country_Codes(string phoneNumber)
        {
            // Arrange
            var request = new AuthenticationRequest { PhoneNumber = phoneNumber };

            // Act & Assert
            validator.TestValidate(request).ShouldNotHaveAnyValidationErrors();
        }
    }
}