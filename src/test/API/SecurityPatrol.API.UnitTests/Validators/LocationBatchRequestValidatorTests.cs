using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.TestHelper;
using Xunit;
using FluentAssertions;
using SecurityPatrol.Application.Validators;
using SecurityPatrol.Core.Models;
using SecurityPatrol.Core.Constants;
using SecurityPatrol.TestCommon.Constants;
using SecurityPatrol.TestCommon.Data;

namespace SecurityPatrol.API.UnitTests.Validators
{
    public class LocationBatchRequestValidatorTests
    {
        private readonly LocationBatchRequestValidator validator;

        public LocationBatchRequestValidatorTests()
        {
            validator = new LocationBatchRequestValidator();
        }

        [Fact]
        public void Should_Validate_Valid_Request()
        {
            // Arrange
            var request = new LocationBatchRequest
            {
                UserId = TestConstants.TestUserId,
                Locations = new List<LocationModel>
                {
                    new LocationModel
                    {
                        Latitude = TestConstants.TestLatitude,
                        Longitude = TestConstants.TestLongitude,
                        Accuracy = TestConstants.TestAccuracy,
                        Timestamp = DateTime.UtcNow.AddMinutes(-5)
                    }
                }
            };

            // Act & Assert
            var result = validator.TestValidate(request);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Should_Fail_When_UserId_Empty()
        {
            // Arrange
            var request = new LocationBatchRequest
            {
                UserId = string.Empty,
                Locations = new List<LocationModel>
                {
                    new LocationModel
                    {
                        Latitude = TestConstants.TestLatitude,
                        Longitude = TestConstants.TestLongitude,
                        Accuracy = TestConstants.TestAccuracy,
                        Timestamp = DateTime.UtcNow.AddMinutes(-5)
                    }
                }
            };

            // Act & Assert
            var result = validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.UserId)
                 .WithErrorMessage(string.Format(ErrorMessages.Validation_Required, "UserId"));
        }

        [Fact]
        public void Should_Fail_When_Locations_Null()
        {
            // Arrange
            var request = new LocationBatchRequest
            {
                UserId = TestConstants.TestUserId,
                Locations = null
            };

            // Act & Assert
            var result = validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.Locations);
        }

        [Fact]
        public void Should_Fail_When_Locations_Empty()
        {
            // Arrange
            var request = new LocationBatchRequest
            {
                UserId = TestConstants.TestUserId,
                Locations = new List<LocationModel>()
            };

            // Act & Assert
            var result = validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.Locations)
                 .WithErrorMessage(ErrorMessages.Location_BatchEmpty);
        }

        [Theory]
        [InlineData(-91)]
        [InlineData(91)]
        public void Should_Fail_When_Latitude_Invalid(double latitude)
        {
            // Arrange
            var request = new LocationBatchRequest
            {
                UserId = TestConstants.TestUserId,
                Locations = new List<LocationModel>
                {
                    new LocationModel
                    {
                        Latitude = latitude,
                        Longitude = TestConstants.TestLongitude,
                        Accuracy = TestConstants.TestAccuracy,
                        Timestamp = DateTime.UtcNow.AddMinutes(-5)
                    }
                }
            };

            // Act & Assert
            var result = validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor("Locations[0].Latitude")
                 .WithErrorMessage(ErrorMessages.Location_InvalidCoordinates);
        }

        [Theory]
        [InlineData(-181)]
        [InlineData(181)]
        public void Should_Fail_When_Longitude_Invalid(double longitude)
        {
            // Arrange
            var request = new LocationBatchRequest
            {
                UserId = TestConstants.TestUserId,
                Locations = new List<LocationModel>
                {
                    new LocationModel
                    {
                        Latitude = TestConstants.TestLatitude,
                        Longitude = longitude,
                        Accuracy = TestConstants.TestAccuracy,
                        Timestamp = DateTime.UtcNow.AddMinutes(-5)
                    }
                }
            };

            // Act & Assert
            var result = validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor("Locations[0].Longitude")
                 .WithErrorMessage(ErrorMessages.Location_InvalidCoordinates);
        }

        [Fact]
        public void Should_Fail_When_Accuracy_Invalid()
        {
            // Arrange
            var request = new LocationBatchRequest
            {
                UserId = TestConstants.TestUserId,
                Locations = new List<LocationModel>
                {
                    new LocationModel
                    {
                        Latitude = TestConstants.TestLatitude,
                        Longitude = TestConstants.TestLongitude,
                        Accuracy = -1, // Negative accuracy
                        Timestamp = DateTime.UtcNow.AddMinutes(-5)
                    }
                }
            };

            // Act & Assert
            var result = validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor("Locations[0].Accuracy")
                 .WithErrorMessage(string.Format(ErrorMessages.Validation_InvalidFormat, "Accuracy"));
        }

        [Fact]
        public void Should_Fail_When_Timestamp_Default()
        {
            // Arrange
            var request = new LocationBatchRequest
            {
                UserId = TestConstants.TestUserId,
                Locations = new List<LocationModel>
                {
                    new LocationModel
                    {
                        Latitude = TestConstants.TestLatitude,
                        Longitude = TestConstants.TestLongitude,
                        Accuracy = TestConstants.TestAccuracy,
                        Timestamp = default(DateTime) // Default timestamp
                    }
                }
            };

            // Act & Assert
            var result = validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor("Locations[0].Timestamp")
                 .WithErrorMessage(string.Format(ErrorMessages.Validation_Required, "Timestamp"));
        }

        [Fact]
        public void Should_Fail_When_Timestamp_Future()
        {
            // Arrange
            var request = new LocationBatchRequest
            {
                UserId = TestConstants.TestUserId,
                Locations = new List<LocationModel>
                {
                    new LocationModel
                    {
                        Latitude = TestConstants.TestLatitude,
                        Longitude = TestConstants.TestLongitude,
                        Accuracy = TestConstants.TestAccuracy,
                        Timestamp = DateTime.UtcNow.AddHours(1) // Future timestamp
                    }
                }
            };

            // Act & Assert
            var result = validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor("Locations[0].Timestamp")
                 .WithErrorMessage(string.Format(ErrorMessages.Validation_InvalidFormat, "Timestamp"));
        }

        [Fact]
        public void Should_Validate_Multiple_Locations()
        {
            // Arrange
            var request = new LocationBatchRequest
            {
                UserId = TestConstants.TestUserId,
                Locations = new List<LocationModel>
                {
                    new LocationModel
                    {
                        Latitude = TestConstants.TestLatitude,
                        Longitude = TestConstants.TestLongitude,
                        Accuracy = TestConstants.TestAccuracy,
                        Timestamp = DateTime.UtcNow.AddMinutes(-5)
                    },
                    new LocationModel
                    {
                        Latitude = TestConstants.TestLatitude + 0.01,
                        Longitude = TestConstants.TestLongitude + 0.01,
                        Accuracy = TestConstants.TestAccuracy + 5,
                        Timestamp = DateTime.UtcNow.AddMinutes(-10)
                    }
                }
            };

            // Act & Assert
            var result = validator.TestValidate(request);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Should_Fail_When_Any_Location_Invalid()
        {
            // Arrange
            var request = new LocationBatchRequest
            {
                UserId = TestConstants.TestUserId,
                Locations = new List<LocationModel>
                {
                    new LocationModel
                    {
                        Latitude = TestConstants.TestLatitude,
                        Longitude = TestConstants.TestLongitude,
                        Accuracy = TestConstants.TestAccuracy,
                        Timestamp = DateTime.UtcNow.AddMinutes(-5)
                    },
                    new LocationModel
                    {
                        Latitude = 100, // Invalid latitude
                        Longitude = TestConstants.TestLongitude,
                        Accuracy = TestConstants.TestAccuracy,
                        Timestamp = DateTime.UtcNow.AddMinutes(-10)
                    }
                }
            };

            // Act & Assert
            var result = validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor("Locations[1].Latitude")
                 .WithErrorMessage(ErrorMessages.Location_InvalidCoordinates);
        }
    }
}