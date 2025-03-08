using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using SecurityPatrol.Application.Validators;
using SecurityPatrol.Core.Models;
using SecurityPatrol.Core.Constants;
using SecurityPatrol.UnitTests.Helpers;

namespace SecurityPatrol.UnitTests.Application.Validators
{
    public class ValidatorTests : TestBase
    {
        // Tests for AuthenticationRequestValidator
        [Fact]
        public async Task AuthenticationRequestValidator_ValidPhoneNumber_ShouldPass()
        {
            // Arrange
            var validator = new AuthenticationRequestValidator();
            var request = new AuthenticationRequest { PhoneNumber = "+12345678901" };
            
            // Act
            var result = await validator.ValidateAsync(request);
            
            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public async Task AuthenticationRequestValidator_EmptyPhoneNumber_ShouldFail()
        {
            // Arrange
            var validator = new AuthenticationRequestValidator();
            var request = new AuthenticationRequest { PhoneNumber = string.Empty };
            
            // Act
            var result = await validator.ValidateAsync(request);
            
            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage.Contains(ErrorMessages.Validation_Required.Replace("{0}", "PhoneNumber")));
        }

        [Fact]
        public async Task AuthenticationRequestValidator_InvalidPhoneNumber_ShouldFail()
        {
            // Arrange
            var validator = new AuthenticationRequestValidator();
            var request = new AuthenticationRequest { PhoneNumber = "1234567890" }; // Missing + prefix
            
            // Act
            var result = await validator.ValidateAsync(request);
            
            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage.Contains(ErrorMessages.Auth_InvalidPhoneNumber));
        }

        // Tests for VerificationRequestValidator
        [Fact]
        public async Task VerificationRequestValidator_ValidRequest_ShouldPass()
        {
            // Arrange
            var validator = new VerificationRequestValidator();
            var request = new VerificationRequest 
            { 
                PhoneNumber = "+12345678901",
                Code = "123456"
            };
            
            // Act
            var result = await validator.ValidateAsync(request);
            
            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public async Task VerificationRequestValidator_EmptyCode_ShouldFail()
        {
            // Arrange
            var validator = new VerificationRequestValidator();
            var request = new VerificationRequest 
            { 
                PhoneNumber = "+12345678901",
                Code = string.Empty
            };
            
            // Act
            var result = await validator.ValidateAsync(request);
            
            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage.Contains(ErrorMessages.Validation_Required.Replace("{0}", "Code")));
        }

        [Fact]
        public async Task VerificationRequestValidator_InvalidCode_ShouldFail()
        {
            // Arrange
            var validator = new VerificationRequestValidator();
            var request = new VerificationRequest 
            { 
                PhoneNumber = "+12345678901",
                Code = "12345" // Should be 6 digits
            };
            
            // Act
            var result = await validator.ValidateAsync(request);
            
            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage.Contains(ErrorMessages.Auth_InvalidVerificationCode));
        }

        // Tests for TimeRecordRequestValidator
        [Fact]
        public async Task TimeRecordRequestValidator_ValidRequest_ShouldPass()
        {
            // Arrange
            var validator = new TimeRecordRequestValidator();
            var request = new TimeRecordRequest 
            { 
                Type = "in",
                Timestamp = DateTime.UtcNow.AddMinutes(-5),
                Location = new LocationModel 
                { 
                    Latitude = 40.7128,
                    Longitude = -74.0060
                }
            };
            
            // Act
            var result = await validator.ValidateAsync(request);
            
            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public async Task TimeRecordRequestValidator_EmptyType_ShouldFail()
        {
            // Arrange
            var validator = new TimeRecordRequestValidator();
            var request = new TimeRecordRequest 
            { 
                Type = string.Empty,
                Timestamp = DateTime.UtcNow,
                Location = new LocationModel 
                { 
                    Latitude = 40.7128,
                    Longitude = -74.0060
                }
            };
            
            // Act
            var result = await validator.ValidateAsync(request);
            
            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage.Contains(ErrorMessages.Validation_Required.Replace("{0}", "Type")));
        }

        [Fact]
        public async Task TimeRecordRequestValidator_InvalidType_ShouldFail()
        {
            // Arrange
            var validator = new TimeRecordRequestValidator();
            var request = new TimeRecordRequest 
            { 
                Type = "invalid",
                Timestamp = DateTime.UtcNow,
                Location = new LocationModel 
                { 
                    Latitude = 40.7128,
                    Longitude = -74.0060
                }
            };
            
            // Act
            var result = await validator.ValidateAsync(request);
            
            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage.Contains(ErrorMessages.TimeRecord_InvalidType));
        }

        [Fact]
        public async Task TimeRecordRequestValidator_FutureTimestamp_ShouldFail()
        {
            // Arrange
            var validator = new TimeRecordRequestValidator();
            var request = new TimeRecordRequest 
            { 
                Type = "in",
                Timestamp = DateTime.UtcNow.AddMinutes(5), // Future timestamp
                Location = new LocationModel 
                { 
                    Latitude = 40.7128,
                    Longitude = -74.0060
                }
            };
            
            // Act
            var result = await validator.ValidateAsync(request);
            
            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage.Contains("future"));
        }

        [Fact]
        public async Task TimeRecordRequestValidator_InvalidLocation_ShouldFail()
        {
            // Arrange
            var validator = new TimeRecordRequestValidator();
            var request = new TimeRecordRequest 
            { 
                Type = "in",
                Timestamp = DateTime.UtcNow,
                Location = new LocationModel 
                { 
                    Latitude = 100, // Invalid latitude (outside valid range)
                    Longitude = -74.0060
                }
            };
            
            // Act
            var result = await validator.ValidateAsync(request);
            
            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage.Contains(ErrorMessages.Location_InvalidCoordinates));
        }

        // Tests for LocationBatchRequestValidator
        [Fact]
        public async Task LocationBatchRequestValidator_ValidRequest_ShouldPass()
        {
            // Arrange
            var validator = new LocationBatchRequestValidator();
            var request = new LocationBatchRequest 
            { 
                UserId = "user1",
                Locations = new List<LocationModel> 
                { 
                    new LocationModel 
                    { 
                        Latitude = 40.7128,
                        Longitude = -74.0060,
                        Timestamp = DateTime.UtcNow,
                        Accuracy = 10.5
                    }
                }
            };
            
            // Act
            var result = await validator.ValidateAsync(request);
            
            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public async Task LocationBatchRequestValidator_EmptyLocations_ShouldFail()
        {
            // Arrange
            var validator = new LocationBatchRequestValidator();
            var request = new LocationBatchRequest 
            { 
                UserId = "user1",
                Locations = new List<LocationModel>()
            };
            
            // Act
            var result = await validator.ValidateAsync(request);
            
            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage.Contains(ErrorMessages.Location_BatchEmpty));
        }

        [Fact]
        public async Task LocationBatchRequestValidator_InvalidLocations_ShouldFail()
        {
            // Arrange
            var validator = new LocationBatchRequestValidator();
            var request = new LocationBatchRequest 
            { 
                UserId = "user1",
                Locations = new List<LocationModel> 
                { 
                    new LocationModel 
                    { 
                        Latitude = 100, // Invalid latitude (outside valid range)
                        Longitude = -74.0060,
                        Timestamp = DateTime.UtcNow,
                        Accuracy = 10.5
                    }
                }
            };
            
            // Act
            var result = await validator.ValidateAsync(request);
            
            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage.Contains(ErrorMessages.Location_InvalidCoordinates));
        }

        // Tests for ReportRequestValidator
        [Fact]
        public async Task ReportRequestValidator_ValidRequest_ShouldPass()
        {
            // Arrange
            var validator = new ReportRequestValidator();
            var request = new ReportRequest 
            { 
                Text = "This is a valid report",
                Timestamp = DateTime.UtcNow,
                Location = new LocationData 
                { 
                    Latitude = 40.7128,
                    Longitude = -74.0060
                }
            };
            
            // Act
            var result = await validator.ValidateAsync(request);
            
            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public async Task ReportRequestValidator_EmptyText_ShouldFail()
        {
            // Arrange
            var validator = new ReportRequestValidator();
            var request = new ReportRequest 
            { 
                Text = string.Empty,
                Timestamp = DateTime.UtcNow,
                Location = new LocationData 
                { 
                    Latitude = 40.7128,
                    Longitude = -74.0060
                }
            };
            
            // Act
            var result = await validator.ValidateAsync(request);
            
            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage.Contains(ErrorMessages.Report_TextRequired));
        }

        [Fact]
        public async Task ReportRequestValidator_TextTooLong_ShouldFail()
        {
            // Arrange
            var validator = new ReportRequestValidator();
            var request = new ReportRequest 
            { 
                Text = new string('A', 501), // Text exceeding maximum length of 500 characters
                Timestamp = DateTime.UtcNow,
                Location = new LocationData 
                { 
                    Latitude = 40.7128,
                    Longitude = -74.0060
                }
            };
            
            // Act
            var result = await validator.ValidateAsync(request);
            
            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage.Contains(ErrorMessages.Report_TextTooLong));
        }

        // Tests for CheckpointVerificationRequestValidator
        [Fact]
        public async Task CheckpointVerificationRequestValidator_ValidRequest_ShouldPass()
        {
            // Arrange
            var validator = new CheckpointVerificationRequestValidator();
            var request = new CheckpointVerificationRequest 
            { 
                CheckpointId = 1,
                Timestamp = DateTime.UtcNow,
                Location = new LocationData 
                { 
                    Latitude = 40.7128,
                    Longitude = -74.0060
                }
            };
            
            // Act
            var result = await validator.ValidateAsync(request);
            
            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public async Task CheckpointVerificationRequestValidator_InvalidCheckpointId_ShouldFail()
        {
            // Arrange
            var validator = new CheckpointVerificationRequestValidator();
            var request = new CheckpointVerificationRequest 
            { 
                CheckpointId = 0, // Invalid ID (must be positive)
                Timestamp = DateTime.UtcNow,
                Location = new LocationData 
                { 
                    Latitude = 40.7128,
                    Longitude = -74.0060
                }
            };
            
            // Act
            var result = await validator.ValidateAsync(request);
            
            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage.Contains("checkpoint"));
        }

        [Fact]
        public async Task CheckpointVerificationRequestValidator_FutureTimestamp_ShouldFail()
        {
            // Arrange
            var validator = new CheckpointVerificationRequestValidator();
            var request = new CheckpointVerificationRequest 
            { 
                CheckpointId = 1,
                Timestamp = DateTime.UtcNow.AddMinutes(5), // Future timestamp
                Location = new LocationData 
                { 
                    Latitude = 40.7128,
                    Longitude = -74.0060
                }
            };
            
            // Act
            var result = await validator.ValidateAsync(request);
            
            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage.Contains("future"));
        }
    }
}