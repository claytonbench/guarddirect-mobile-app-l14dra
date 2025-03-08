using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SecurityPatrol.API.Controllers;
using SecurityPatrol.API.UnitTests.Setup;
using SecurityPatrol.Core.Exceptions;
using SecurityPatrol.Core.Models;
using SecurityPatrol.TestCommon.Constants;
using Xunit;

namespace SecurityPatrol.API.UnitTests.Controllers
{
    /// <summary>
    /// Contains unit tests for the AuthController class, verifying the functionality of authentication endpoints.
    /// </summary>
    public class AuthControllerTests : TestBase
    {
        /// <summary>
        /// Initializes a new instance of the AuthControllerTests class
        /// </summary>
        public AuthControllerTests() : base()
        {
            // Call base constructor to initialize mock services
        }

        /// <summary>
        /// Tests that the Verify endpoint returns a verification ID when a valid phone number is provided
        /// </summary>
        /// <returns>A task representing the asynchronous test operation</returns>
        [Fact]
        public async Task Verify_WithValidPhoneNumber_ReturnsVerificationId()
        {
            // Arrange: Create a valid AuthenticationRequest with TestPhoneNumber
            var request = new AuthenticationRequest { PhoneNumber = TestConstants.TestPhoneNumber };
            string expectedVerificationId = "test-verification-id";
            
            // Arrange: Setup MockAuthenticationService to return a verification ID when RequestVerificationCodeAsync is called
            MockAuthenticationService
                .Setup(x => x.RequestVerificationCodeAsync(It.IsAny<AuthenticationRequest>()))
                .ReturnsAsync(expectedVerificationId);
            
            // Arrange: Create an instance of AuthController using CreateAuthController
            var controller = CreateAuthController();
            
            // Act: Call controller.Verify with the request
            var result = await controller.Verify(request);
            
            // Assert: Verify that the result is a successful Result<string> containing the expected verification ID
            AssertActionResult(result.Result, new Result<string> { Data = expectedVerificationId, Succeeded = true });
            
            // Assert: Verify that MockAuthenticationService.RequestVerificationCodeAsync was called once with the correct parameters
            MockAuthenticationService.Verify(
                x => x.RequestVerificationCodeAsync(It.Is<AuthenticationRequest>(r => r.PhoneNumber == request.PhoneNumber)),
                Times.Once);
        }

        /// <summary>
        /// Tests that the Verify endpoint returns a validation error when an invalid phone number is provided
        /// </summary>
        /// <returns>A task representing the asynchronous test operation</returns>
        [Fact]
        public async Task Verify_WithInvalidPhoneNumber_ReturnsValidationError()
        {
            // Arrange: Create an invalid AuthenticationRequest with empty phone number
            var request = new AuthenticationRequest { PhoneNumber = "" };
            
            // Arrange: Setup MockAuthenticationService to throw ValidationException when RequestVerificationCodeAsync is called
            MockAuthenticationService
                .Setup(x => x.RequestVerificationCodeAsync(It.IsAny<AuthenticationRequest>()))
                .ThrowsAsync(new ValidationException("Invalid phone number"));
            
            // Arrange: Create an instance of AuthController using CreateAuthController
            var controller = CreateAuthController();
            
            // Act/Assert: Use AssertExceptionAsync to verify that calling controller.Verify throws ValidationException
            await AssertExceptionAsync<ValidationException>(() => controller.Verify(request));
        }

        /// <summary>
        /// Tests that the Validate endpoint returns an authentication response when a valid verification code is provided
        /// </summary>
        /// <returns>A task representing the asynchronous test operation</returns>
        [Fact]
        public async Task Validate_WithValidCode_ReturnsAuthenticationResponse()
        {
            // Arrange: Create a valid VerificationRequest with TestPhoneNumber and TestVerificationCode
            var request = new VerificationRequest
            {
                PhoneNumber = TestConstants.TestPhoneNumber,
                Code = TestConstants.TestVerificationCode
            };
            
            // Arrange: Create an expected AuthenticationResponse with TestAuthToken and expiration date
            var expectedResponse = new AuthenticationResponse
            {
                Token = TestConstants.TestAuthToken,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };
            
            // Arrange: Setup MockAuthenticationService to return the expected response when VerifyCodeAsync is called
            MockAuthenticationService
                .Setup(x => x.VerifyCodeAsync(It.IsAny<VerificationRequest>()))
                .ReturnsAsync(expectedResponse);
            
            // Arrange: Create an instance of AuthController using CreateAuthController
            var controller = CreateAuthController();
            
            // Act: Call controller.Validate with the request
            var result = await controller.Validate(request);
            
            // Assert: Verify that the result is a successful Result<AuthenticationResponse> containing the expected response
            AssertActionResult(result.Result, new Result<AuthenticationResponse> { Data = expectedResponse, Succeeded = true });
            
            // Assert: Verify that MockAuthenticationService.VerifyCodeAsync was called once with the correct parameters
            MockAuthenticationService.Verify(
                x => x.VerifyCodeAsync(It.Is<VerificationRequest>(r => 
                    r.PhoneNumber == request.PhoneNumber && r.Code == request.Code)),
                Times.Once);
        }

        /// <summary>
        /// Tests that the Validate endpoint returns an unauthorized error when an invalid verification code is provided
        /// </summary>
        /// <returns>A task representing the asynchronous test operation</returns>
        [Fact]
        public async Task Validate_WithInvalidCode_ReturnsUnauthorized()
        {
            // Arrange: Create a VerificationRequest with TestPhoneNumber and invalid code
            var request = new VerificationRequest
            {
                PhoneNumber = TestConstants.TestPhoneNumber,
                Code = "invalid"
            };
            
            // Arrange: Setup MockAuthenticationService to throw UnauthorizedException when VerifyCodeAsync is called
            MockAuthenticationService
                .Setup(x => x.VerifyCodeAsync(It.IsAny<VerificationRequest>()))
                .ThrowsAsync(new UnauthorizedException("Invalid verification code"));
            
            // Arrange: Create an instance of AuthController using CreateAuthController
            var controller = CreateAuthController();
            
            // Act/Assert: Use AssertExceptionAsync to verify that calling controller.Validate throws UnauthorizedException
            await AssertExceptionAsync<UnauthorizedException>(() => controller.Validate(request));
        }

        /// <summary>
        /// Tests that the Refresh endpoint returns a new authentication response when a valid token is provided
        /// </summary>
        /// <returns>A task representing the asynchronous test operation</returns>
        [Fact]
        public async Task Refresh_WithValidToken_ReturnsNewAuthenticationResponse()
        {
            // Arrange: Create an expected AuthenticationResponse with new token and expiration date
            var expectedResponse = new AuthenticationResponse
            {
                Token = "new-" + TestConstants.TestAuthToken,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };
            
            // Arrange: Setup MockAuthenticationService to return the expected response when RefreshTokenAsync is called
            MockAuthenticationService
                .Setup(x => x.RefreshTokenAsync(It.IsAny<string>()))
                .ReturnsAsync(expectedResponse);
            
            // Arrange: Create an instance of AuthController using CreateAuthController
            var controller = CreateAuthController();
            
            // Arrange: Setup HTTP context with Authorization header containing TestAuthToken
            SetupHttpContext(controller, TestConstants.TestAuthToken);
            
            // Act: Call controller.Refresh
            var result = await controller.Refresh();
            
            // Assert: Verify that the result is a successful Result<AuthenticationResponse> containing the expected response
            AssertActionResult(result.Result, new Result<AuthenticationResponse> { Data = expectedResponse, Succeeded = true });
            
            // Assert: Verify that MockAuthenticationService.RefreshTokenAsync was called once with the correct token
            MockAuthenticationService.Verify(x => x.RefreshTokenAsync(TestConstants.TestAuthToken), Times.Once);
        }

        /// <summary>
        /// Tests that the Refresh endpoint returns an unauthorized error when an invalid token is provided
        /// </summary>
        /// <returns>A task representing the asynchronous test operation</returns>
        [Fact]
        public async Task Refresh_WithInvalidToken_ReturnsUnauthorized()
        {
            // Arrange: Setup MockAuthenticationService to throw UnauthorizedException when RefreshTokenAsync is called
            MockAuthenticationService
                .Setup(x => x.RefreshTokenAsync(It.IsAny<string>()))
                .ThrowsAsync(new UnauthorizedException("Invalid or expired token"));
            
            // Arrange: Create an instance of AuthController using CreateAuthController
            var controller = CreateAuthController();
            
            // Arrange: Setup HTTP context with Authorization header containing invalid token
            SetupHttpContext(controller, "invalid-token");
            
            // Act/Assert: Use AssertExceptionAsync to verify that calling controller.Refresh throws UnauthorizedException
            await AssertExceptionAsync<UnauthorizedException>(() => controller.Refresh());
        }

        /// <summary>
        /// Tests that the Refresh endpoint returns an unauthorized error when no Authorization header is provided
        /// </summary>
        /// <returns>A task representing the asynchronous test operation</returns>
        [Fact]
        public async Task Refresh_WithMissingAuthorizationHeader_ReturnsUnauthorized()
        {
            // Arrange: Create an instance of AuthController using CreateAuthController
            var controller = CreateAuthController();
            
            // Arrange: Setup HTTP context without Authorization header
            SetupHttpContext(controller);
            
            // Act/Assert: Use AssertExceptionAsync to verify that calling controller.Refresh throws UnauthorizedException
            await AssertExceptionAsync<UnauthorizedException>(() => controller.Refresh());
        }
    }
}