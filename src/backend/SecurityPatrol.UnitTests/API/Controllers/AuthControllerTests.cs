using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using SecurityPatrol.UnitTests.Helpers;
using SecurityPatrol.API.Controllers;
using SecurityPatrol.Core.Models;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.Core.Exceptions;

namespace SecurityPatrol.UnitTests.API.Controllers
{
    /// <summary>
    /// Contains unit tests for the AuthController class to verify authentication functionality
    /// </summary>
    public class AuthControllerTests : TestBase
    {
        private AuthController _controller;
        private Mock<ILogger<AuthController>> _mockLogger;

        /// <summary>
        /// Initializes a new instance of the AuthControllerTests class with test dependencies
        /// </summary>
        public AuthControllerTests()
        {
            _mockLogger = new Mock<ILogger<AuthController>>();
            _controller = new AuthController(MockAuthenticationService.Object, _mockLogger.Object);
        }

        /// <summary>
        /// Sets up the test environment before each test
        /// </summary>
        private void Setup()
        {
            ResetMocks();
            _mockLogger = new Mock<ILogger<AuthController>>();
            _controller = new AuthController(MockAuthenticationService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task Verify_WithValidRequest_ReturnsOkResult()
        {
            // Arrange
            Setup();
            var request = new AuthenticationRequest { PhoneNumber = "+15551234567" };
            string expectedVerificationId = "verification-123";

            MockAuthenticationService
                .Setup(s => s.RequestVerificationCodeAsync(request))
                .ReturnsAsync(expectedVerificationId);

            // Act
            var result = await _controller.Verify(request);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var responseValue = okResult.Value.Should().BeAssignableTo<Result<string>>().Subject;
            responseValue.Data.Should().Be(expectedVerificationId);
            responseValue.Success.Should().BeTrue();

            MockAuthenticationService.Verify(s => s.RequestVerificationCodeAsync(request), Times.Once);
        }

        [Fact]
        public async Task Verify_WithInvalidRequest_ReturnsBadRequest()
        {
            // Arrange
            Setup();
            var request = new AuthenticationRequest { PhoneNumber = "invalid-phone" };
            _controller.ModelState.AddModelError("PhoneNumber", "Invalid phone number format");

            // Act
            var result = await _controller.Verify(request);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();

            MockAuthenticationService.Verify(s => s.RequestVerificationCodeAsync(It.IsAny<AuthenticationRequest>()), Times.Never);
        }

        [Fact]
        public async Task Verify_WhenServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            Setup();
            var request = new AuthenticationRequest { PhoneNumber = "+15551234567" };
            MockAuthenticationService
                .Setup(s => s.RequestVerificationCodeAsync(request))
                .ThrowsAsync(new Exception("Test exception"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(async () => await _controller.Verify(request));

            MockAuthenticationService.Verify(s => s.RequestVerificationCodeAsync(request), Times.Once);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Validate_WithValidRequest_ReturnsOkResult()
        {
            // Arrange
            Setup();
            var request = new VerificationRequest { PhoneNumber = "+15551234567", Code = "123456" };
            var expectedResponse = new AuthenticationResponse 
            { 
                Token = "test-jwt-token", 
                ExpiresAt = DateTime.UtcNow.AddHours(1) 
            };

            MockAuthenticationService
                .Setup(s => s.VerifyCodeAsync(request))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.Validate(request);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var responseValue = okResult.Value.Should().BeAssignableTo<Result<AuthenticationResponse>>().Subject;
            responseValue.Data.Should().Be(expectedResponse);
            responseValue.Success.Should().BeTrue();

            MockAuthenticationService.Verify(s => s.VerifyCodeAsync(request), Times.Once);
        }

        [Fact]
        public async Task Validate_WithInvalidRequest_ReturnsBadRequest()
        {
            // Arrange
            Setup();
            var request = new VerificationRequest { PhoneNumber = "invalid-phone", Code = "123" };
            _controller.ModelState.AddModelError("PhoneNumber", "Invalid phone number format");
            _controller.ModelState.AddModelError("Code", "Code must be 6 digits");

            // Act
            var result = await _controller.Validate(request);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();

            MockAuthenticationService.Verify(s => s.VerifyCodeAsync(It.IsAny<VerificationRequest>()), Times.Never);
        }

        [Fact]
        public async Task Validate_WhenServiceThrowsUnauthorizedException_ReturnsUnauthorized()
        {
            // Arrange
            Setup();
            var request = new VerificationRequest { PhoneNumber = "+15551234567", Code = "123456" };
            MockAuthenticationService
                .Setup(s => s.VerifyCodeAsync(request))
                .ThrowsAsync(new UnauthorizedException("Invalid verification code"));

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedException>(async () => await _controller.Validate(request));

            MockAuthenticationService.Verify(s => s.VerifyCodeAsync(request), Times.Once);
        }

        [Fact]
        public async Task Validate_WhenServiceThrowsValidationException_ReturnsBadRequest()
        {
            // Arrange
            Setup();
            var request = new VerificationRequest { PhoneNumber = "+15551234567", Code = "123456" };
            MockAuthenticationService
                .Setup(s => s.VerifyCodeAsync(request))
                .ThrowsAsync(new ValidationException("Validation failed"));

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(async () => await _controller.Validate(request));

            MockAuthenticationService.Verify(s => s.VerifyCodeAsync(request), Times.Once);
        }

        [Fact]
        public async Task Validate_WhenServiceThrowsGenericException_ReturnsInternalServerError()
        {
            // Arrange
            Setup();
            var request = new VerificationRequest { PhoneNumber = "+15551234567", Code = "123456" };
            MockAuthenticationService
                .Setup(s => s.VerifyCodeAsync(request))
                .ThrowsAsync(new Exception("Test exception"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(async () => await _controller.Validate(request));

            MockAuthenticationService.Verify(s => s.VerifyCodeAsync(request), Times.Once);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Refresh_WithValidToken_ReturnsOkResult()
        {
            // Arrange
            Setup();
            string token = "valid-token";
            var expectedResponse = new AuthenticationResponse 
            { 
                Token = "refreshed-jwt-token", 
                ExpiresAt = DateTime.UtcNow.AddHours(1) 
            };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext()
            };
            _controller.HttpContext.Request.Headers["Authorization"] = $"Bearer {token}";

            MockAuthenticationService
                .Setup(s => s.RefreshTokenAsync(token))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.Refresh();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var responseValue = okResult.Value.Should().BeAssignableTo<Result<AuthenticationResponse>>().Subject;
            responseValue.Data.Should().Be(expectedResponse);
            responseValue.Success.Should().BeTrue();

            MockAuthenticationService.Verify(s => s.RefreshTokenAsync(token), Times.Once);
        }

        [Fact]
        public async Task Refresh_WithMissingAuthorizationHeader_ReturnsUnauthorized()
        {
            // Arrange
            Setup();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext()
            };
            // No Authorization header is set

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedException>(async () => await _controller.Refresh());

            MockAuthenticationService.Verify(s => s.RefreshTokenAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Refresh_WithInvalidAuthorizationFormat_ReturnsUnauthorized()
        {
            // Arrange
            Setup();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext()
            };
            _controller.HttpContext.Request.Headers["Authorization"] = "InvalidFormat token";

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedException>(async () => await _controller.Refresh());

            MockAuthenticationService.Verify(s => s.RefreshTokenAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Refresh_WhenServiceThrowsUnauthorizedException_ReturnsUnauthorized()
        {
            // Arrange
            Setup();
            string token = "invalid-token";
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext()
            };
            _controller.HttpContext.Request.Headers["Authorization"] = $"Bearer {token}";

            MockAuthenticationService
                .Setup(s => s.RefreshTokenAsync(token))
                .ThrowsAsync(new UnauthorizedException("Invalid or expired token"));

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedException>(async () => await _controller.Refresh());

            MockAuthenticationService.Verify(s => s.RefreshTokenAsync(token), Times.Once);
        }

        [Fact]
        public async Task Refresh_WhenServiceThrowsGenericException_ReturnsInternalServerError()
        {
            // Arrange
            Setup();
            string token = "valid-token";
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext()
            };
            _controller.HttpContext.Request.Headers["Authorization"] = $"Bearer {token}";

            MockAuthenticationService
                .Setup(s => s.RefreshTokenAsync(token))
                .ThrowsAsync(new Exception("Test exception"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(async () => await _controller.Refresh());

            MockAuthenticationService.Verify(s => s.RefreshTokenAsync(token), Times.Once);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }
}