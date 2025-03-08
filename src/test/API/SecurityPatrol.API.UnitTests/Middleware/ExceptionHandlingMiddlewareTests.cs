using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;
using FluentAssertions;
using System.IO;
using SecurityPatrol.API.Middleware;
using SecurityPatrol.Core.Exceptions;
using SecurityPatrol.API.UnitTests.Setup;

namespace SecurityPatrol.API.UnitTests.Middleware
{
    public class ExceptionHandlingMiddlewareTests : TestBase
    {
        private readonly Mock<RequestDelegate> _nextMock;
        private readonly Mock<ILogger<ExceptionHandlingMiddleware>> _loggerMock;
        private readonly Mock<IHostEnvironment> _environmentMock;
        private readonly ExceptionHandlingMiddleware _middleware;
        private readonly DefaultHttpContext _httpContext;

        public ExceptionHandlingMiddlewareTests()
        {
            // Initialize mocks
            _nextMock = new Mock<RequestDelegate>();
            _loggerMock = new Mock<ILogger<ExceptionHandlingMiddleware>>();
            _environmentMock = new Mock<IHostEnvironment>();
            
            // By default, set environment to non-development
            _environmentMock.Setup(e => e.IsDevelopment()).Returns(false);
            
            // Create middleware instance with mocked dependencies
            _middleware = new ExceptionHandlingMiddleware(
                _nextMock.Object,
                _loggerMock.Object,
                _environmentMock.Object);
                
            // Set up HTTP context with memory stream to capture responses
            _httpContext = new DefaultHttpContext();
            _httpContext.Response.Body = new MemoryStream();
        }

        [Fact]
        public async Task InvokeAsync_NoException_CallsNext()
        {
            // Arrange
            _nextMock.Setup(next => next(_httpContext)).Returns(Task.CompletedTask);
            
            // Act
            await _middleware.InvokeAsync(_httpContext);
            
            // Assert
            _nextMock.Verify(next => next(_httpContext), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_ApiException_ReturnsCorrectStatusCodeAndResponse()
        {
            // Arrange
            var apiException = new ApiException(422, "Test API exception");
            _nextMock.Setup(next => next(_httpContext)).ThrowsAsync(apiException);
            
            // Act
            await _middleware.InvokeAsync(_httpContext);
            
            // Assert
            _httpContext.Response.StatusCode.Should().Be(422);
            _httpContext.Response.ContentType.Should().Be("application/json");
            
            // Reset the position to read from the beginning
            _httpContext.Response.Body.Position = 0;
            
            // Read the response and deserialize
            using var streamReader = new StreamReader(_httpContext.Response.Body);
            var responseBody = await streamReader.ReadToEndAsync();
            var response = JsonSerializer.Deserialize<dynamic>(responseBody, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
            ((int)response.statusCode).Should().Be(422);
            ((string)response.message).Should().Be("Test API exception");
        }

        [Fact]
        public async Task InvokeAsync_ApiExceptionWithDetails_IncludesDetailsInResponse()
        {
            // Arrange
            var apiException = new ApiException(422, "Test API exception", "Additional details");
            _nextMock.Setup(next => next(_httpContext)).ThrowsAsync(apiException);
            
            // Act
            await _middleware.InvokeAsync(_httpContext);
            
            // Assert
            _httpContext.Response.StatusCode.Should().Be(422);
            
            // Reset the position to read from the beginning
            _httpContext.Response.Body.Position = 0;
            
            // Read the response and deserialize
            using var streamReader = new StreamReader(_httpContext.Response.Body);
            var responseBody = await streamReader.ReadToEndAsync();
            var response = JsonSerializer.Deserialize<dynamic>(responseBody, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
            ((int)response.statusCode).Should().Be(422);
            ((string)response.message).Should().Be("Test API exception");
            ((string)response.details).Should().Be("Additional details");
        }

        [Fact]
        public async Task InvokeAsync_ValidationException_ReturnsCorrectStatusCodeAndResponse()
        {
            // Arrange
            var errors = new Dictionary<string, string[]>
            {
                { "Field1", new[] { "Error 1", "Error 2" } },
                { "Field2", new[] { "Error 3" } }
            };
            var validationException = new ValidationException("Validation failed", errors);
            _nextMock.Setup(next => next(_httpContext)).ThrowsAsync(validationException);
            
            // Act
            await _middleware.InvokeAsync(_httpContext);
            
            // Assert
            _httpContext.Response.StatusCode.Should().Be(400);
            
            // Reset the position to read from the beginning
            _httpContext.Response.Body.Position = 0;
            
            // Read the response and deserialize
            using var streamReader = new StreamReader(_httpContext.Response.Body);
            var responseBody = await streamReader.ReadToEndAsync();
            var response = JsonSerializer.Deserialize<dynamic>(responseBody, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
            ((int)response.statusCode).Should().Be(400);
            ((string)response.message).Should().Be("Validation failed");
            
            // Check that errors property exists and has the right structure
            ((JsonElement)response.errors).ValueKind.Should().Be(JsonValueKind.Object);
            var errorsElement = (JsonElement)response.errors;
            
            errorsElement.TryGetProperty("Field1", out var field1Errors).Should().BeTrue();
            field1Errors.GetArrayLength().Should().Be(2);
            
            errorsElement.TryGetProperty("Field2", out var field2Errors).Should().BeTrue();
            field2Errors.GetArrayLength().Should().Be(1);
        }

        [Fact]
        public async Task InvokeAsync_UnauthorizedException_ReturnsCorrectStatusCodeAndResponse()
        {
            // Arrange
            var unauthorizedException = new UnauthorizedException("Unauthorized access");
            _nextMock.Setup(next => next(_httpContext)).ThrowsAsync(unauthorizedException);
            
            // Act
            await _middleware.InvokeAsync(_httpContext);
            
            // Assert
            _httpContext.Response.StatusCode.Should().Be(401);
            
            // Reset the position to read from the beginning
            _httpContext.Response.Body.Position = 0;
            
            // Read the response and deserialize
            using var streamReader = new StreamReader(_httpContext.Response.Body);
            var responseBody = await streamReader.ReadToEndAsync();
            var response = JsonSerializer.Deserialize<dynamic>(responseBody, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
            ((int)response.statusCode).Should().Be(401);
            ((string)response.message).Should().Be("Unauthorized access");
        }

        [Fact]
        public async Task InvokeAsync_NotFoundException_ReturnsCorrectStatusCodeAndResponse()
        {
            // Arrange
            var notFoundException = new NotFoundException("Resource not found");
            _nextMock.Setup(next => next(_httpContext)).ThrowsAsync(notFoundException);
            
            // Act
            await _middleware.InvokeAsync(_httpContext);
            
            // Assert
            _httpContext.Response.StatusCode.Should().Be(404);
            
            // Reset the position to read from the beginning
            _httpContext.Response.Body.Position = 0;
            
            // Read the response and deserialize
            using var streamReader = new StreamReader(_httpContext.Response.Body);
            var responseBody = await streamReader.ReadToEndAsync();
            var response = JsonSerializer.Deserialize<dynamic>(responseBody, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
            ((int)response.statusCode).Should().Be(404);
            ((string)response.message).Should().Be("Resource not found");
        }

        [Fact]
        public async Task InvokeAsync_GenericException_ReturnsInternalServerErrorAndResponse()
        {
            // Arrange
            var exception = new Exception("Unexpected error");
            _nextMock.Setup(next => next(_httpContext)).ThrowsAsync(exception);
            
            // Act
            await _middleware.InvokeAsync(_httpContext);
            
            // Assert
            _httpContext.Response.StatusCode.Should().Be(500);
            
            // Reset the position to read from the beginning
            _httpContext.Response.Body.Position = 0;
            
            // Read the response and deserialize
            using var streamReader = new StreamReader(_httpContext.Response.Body);
            var responseBody = await streamReader.ReadToEndAsync();
            var response = JsonSerializer.Deserialize<dynamic>(responseBody, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
            ((int)response.statusCode).Should().Be(500);
            ((string)response.message).Should().Be("Unexpected error");
        }

        [Fact]
        public async Task InvokeAsync_DevelopmentEnvironment_IncludesStackTraceInResponse()
        {
            // Arrange
            _environmentMock.Setup(e => e.IsDevelopment()).Returns(true);
            
            // Recreate middleware with updated environment mock
            var middleware = new ExceptionHandlingMiddleware(
                _nextMock.Object,
                _loggerMock.Object,
                _environmentMock.Object);
                
            var exception = new Exception("Unexpected error");
            _nextMock.Setup(next => next(_httpContext)).ThrowsAsync(exception);
            
            // Act
            await middleware.InvokeAsync(_httpContext);
            
            // Reset the position to read from the beginning
            _httpContext.Response.Body.Position = 0;
            
            // Read the response and deserialize
            using var streamReader = new StreamReader(_httpContext.Response.Body);
            var responseBody = await streamReader.ReadToEndAsync();
            var response = JsonSerializer.Deserialize<dynamic>(responseBody, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
            // Assert that stack trace and exception type are included
            ((string)response.stackTrace).Should().NotBeNullOrEmpty();
            ((string)response.exception).Should().Be("Exception");
        }

        [Fact]
        public async Task InvokeAsync_LogsExceptionWithAppropriateLevel()
        {
            // Arrange
            var exception = new Exception("Unexpected error");
            _nextMock.Setup(next => next(_httpContext)).ThrowsAsync(exception);
            
            // Act
            await _middleware.InvokeAsync(_httpContext);
            
            // Assert
            // Verify that logger was called with critical level for 500 errors
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Critical,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }
}