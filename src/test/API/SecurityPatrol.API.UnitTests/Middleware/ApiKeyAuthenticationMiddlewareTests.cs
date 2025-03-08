using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using SecurityPatrol.API.Middleware;
using SecurityPatrol.API.UnitTests.Setup;

namespace SecurityPatrol.API.UnitTests.Middleware
{
    /// <summary>
    /// Test class for ApiKeyAuthenticationMiddleware that validates API key authentication behavior.
    /// </summary>
    public class ApiKeyAuthenticationMiddlewareTests : TestBase
    {
        private readonly Mock<RequestDelegate> _mockNextMiddleware;
        private readonly Mock<ILogger<ApiKeyAuthenticationMiddleware>> _mockLogger;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly ApiKeyAuthenticationMiddleware _middleware;
        private readonly DefaultHttpContext _httpContext;
        private const string ApiKeyHeaderName = "X-API-Key";
        private const string ApiKeyConfigKey = "Authentication:ApiKey";
        private const string ValidApiKey = "valid-api-key-for-testing";

        /// <summary>
        /// Initializes a new instance of the ApiKeyAuthenticationMiddlewareTests class with mocked dependencies.
        /// </summary>
        public ApiKeyAuthenticationMiddlewareTests()
        {
            _mockNextMiddleware = new Mock<RequestDelegate>();
            _mockLogger = new Mock<ILogger<ApiKeyAuthenticationMiddleware>>();
            _mockConfiguration = new Mock<IConfiguration>();
            
            // Setup the configuration to return our valid API key
            _mockConfiguration
                .Setup(c => c[ApiKeyConfigKey])
                .Returns(ValidApiKey);

            _middleware = new ApiKeyAuthenticationMiddleware(
                _mockNextMiddleware.Object,
                _mockLogger.Object,
                _mockConfiguration.Object);

            _httpContext = new DefaultHttpContext();
        }

        /// <summary>
        /// Tests that the middleware calls the next middleware when a valid API key is provided.
        /// </summary>
        [Fact]
        public async Task InvokeAsync_WithValidApiKey_CallsNextMiddleware()
        {
            // Arrange
            _httpContext.Request.Headers[ApiKeyHeaderName] = ValidApiKey;

            // Act
            await _middleware.InvokeAsync(_httpContext);

            // Assert
            _mockNextMiddleware.Verify(next => next(_httpContext), Times.Once);
        }

        /// <summary>
        /// Tests that the middleware returns a 401 Unauthorized response when the API key header is missing.
        /// </summary>
        [Fact]
        public async Task InvokeAsync_WithMissingApiKey_Returns401Unauthorized()
        {
            // Act
            await _middleware.InvokeAsync(_httpContext);

            // Assert
            _httpContext.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
            _mockNextMiddleware.Verify(next => next(It.IsAny<HttpContext>()), Times.Never);
        }

        /// <summary>
        /// Tests that the middleware returns a 403 Forbidden response when an invalid API key is provided.
        /// </summary>
        [Fact]
        public async Task InvokeAsync_WithInvalidApiKey_Returns403Forbidden()
        {
            // Arrange
            _httpContext.Request.Headers[ApiKeyHeaderName] = "invalid-api-key";

            // Act
            await _middleware.InvokeAsync(_httpContext);

            // Assert
            _httpContext.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
            _mockNextMiddleware.Verify(next => next(It.IsAny<HttpContext>()), Times.Never);
        }

        /// <summary>
        /// Tests that the middleware skips API key validation and calls the next middleware for excluded paths.
        /// </summary>
        [Theory]
        [InlineData("/swagger")]
        [InlineData("/auth/verify")]
        [InlineData("/health")]
        public async Task InvokeAsync_WithExcludedPath_CallsNextMiddleware(string path)
        {
            // Arrange
            _httpContext.Request.Path = path;

            // Act
            await _middleware.InvokeAsync(_httpContext);

            // Assert
            _mockNextMiddleware.Verify(next => next(_httpContext), Times.Once);
        }

        /// <summary>
        /// Tests that the middleware requires API key validation for non-excluded paths.
        /// </summary>
        [Theory]
        [InlineData("/api/time")]
        [InlineData("/api/location")]
        [InlineData("/api/patrol")]
        public async Task InvokeAsync_WithNonExcludedPath_RequiresApiKey(string path)
        {
            // Arrange
            _httpContext.Request.Path = path;

            // Act
            await _middleware.InvokeAsync(_httpContext);

            // Assert
            _httpContext.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
            _mockNextMiddleware.Verify(next => next(It.IsAny<HttpContext>()), Times.Never);
        }

        /// <summary>
        /// Tests that the middleware propagates exceptions thrown by the next middleware in the pipeline.
        /// </summary>
        [Fact]
        public async Task InvokeAsync_WhenNextMiddlewareThrows_PropagatesException()
        {
            // Arrange
            _httpContext.Request.Headers[ApiKeyHeaderName] = ValidApiKey;
            var expectedException = new InvalidOperationException("Test exception");
            _mockNextMiddleware
                .Setup(next => next(It.IsAny<HttpContext>()))
                .ThrowsAsync(expectedException);

            // Act & Assert
            await AssertExceptionAsync<InvalidOperationException>(async () => 
                await _middleware.InvokeAsync(_httpContext));
        }
    }
}