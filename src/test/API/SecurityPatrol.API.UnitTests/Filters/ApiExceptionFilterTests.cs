using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using SecurityPatrol.API.UnitTests.Setup;
using SecurityPatrol.API.Filters;
using SecurityPatrol.Core.Exceptions;

namespace SecurityPatrol.API.UnitTests.Filters
{
    public class ApiExceptionFilterTests
    {
        private readonly Mock<ILogger<ApiExceptionFilter>> _mockLogger;
        private readonly Mock<IHostEnvironment> _mockEnvironment;

        public ApiExceptionFilterTests()
        {
            _mockLogger = new Mock<ILogger<ApiExceptionFilter>>();
            _mockEnvironment = new Mock<IHostEnvironment>();
            _mockEnvironment.Setup(e => e.IsDevelopment()).Returns(false);
        }

        [Fact]
        public void HandleApiException_ShouldReturnCorrectStatusCode()
        {
            // Arrange
            var statusCode = 422; // Unprocessable Entity
            var exception = new ApiException(statusCode, "Test API exception");
            var context = CreateExceptionContext(exception);
            
            var filter = new ApiExceptionFilter(_mockLogger.Object, _mockEnvironment.Object);
            
            // Act
            filter.OnException(context);
            
            // Assert
            context.Result.Should().NotBeNull();
            context.ExceptionHandled.Should().BeTrue();
            
            var result = context.Result as ObjectResult;
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(statusCode);
            
            var problemDetails = result.Value as ProblemDetails;
            problemDetails.Should().NotBeNull();
            problemDetails.Title.Should().Be("Unprocessable Entity");
            problemDetails.Detail.Should().Be("Test API exception");
        }
        
        [Fact]
        public void HandleApiExceptionWithDetails_ShouldIncludeDetailsInResponse()
        {
            // Arrange
            var statusCode = 422; // Unprocessable Entity
            var exception = new ApiException(statusCode, "Test API exception", "Additional error details");
            var context = CreateExceptionContext(exception);
            
            var filter = new ApiExceptionFilter(_mockLogger.Object, _mockEnvironment.Object);
            
            // Act
            filter.OnException(context);
            
            // Assert
            context.Result.Should().NotBeNull();
            context.ExceptionHandled.Should().BeTrue();
            
            var result = context.Result as ObjectResult;
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(statusCode);
            
            var problemDetails = result.Value as ProblemDetails;
            problemDetails.Should().NotBeNull();
            problemDetails.Title.Should().Be("Unprocessable Entity");
            problemDetails.Detail.Should().Be("Test API exception");
            problemDetails.Extensions.Should().ContainKey("details");
            problemDetails.Extensions["details"].Should().Be("Additional error details");
        }
        
        [Fact]
        public void HandleValidationException_ShouldReturnBadRequest()
        {
            // Arrange
            var errors = new Dictionary<string, string[]>
            {
                { "PropertyName", new[] { "Error message 1", "Error message 2" } }
            };
            var exception = new ValidationException("Validation failed", errors);
            var context = CreateExceptionContext(exception);
            
            var filter = new ApiExceptionFilter(_mockLogger.Object, _mockEnvironment.Object);
            
            // Act
            filter.OnException(context);
            
            // Assert
            context.Result.Should().NotBeNull();
            context.ExceptionHandled.Should().BeTrue();
            
            var result = context.Result as ObjectResult;
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(400);
            
            var problemDetails = result.Value as ProblemDetails;
            problemDetails.Should().NotBeNull();
            problemDetails.Title.Should().Be("Bad Request");
            problemDetails.Detail.Should().Be("Validation failed");
            problemDetails.Extensions.Should().ContainKey("validationErrors");
            problemDetails.Extensions["validationErrors"].Should().BeEquivalentTo(errors);
        }
        
        [Fact]
        public void HandleNotFoundException_ShouldReturnNotFound()
        {
            // Arrange
            var exception = new NotFoundException("Resource not found");
            var context = CreateExceptionContext(exception);
            
            var filter = new ApiExceptionFilter(_mockLogger.Object, _mockEnvironment.Object);
            
            // Act
            filter.OnException(context);
            
            // Assert
            context.Result.Should().NotBeNull();
            context.ExceptionHandled.Should().BeTrue();
            
            var result = context.Result as ObjectResult;
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(404);
            
            var problemDetails = result.Value as ProblemDetails;
            problemDetails.Should().NotBeNull();
            problemDetails.Title.Should().Be("Not Found");
            problemDetails.Detail.Should().Be("Resource not found");
        }
        
        [Fact]
        public void HandleUnauthorizedException_ShouldReturnUnauthorized()
        {
            // Arrange
            var exception = new UnauthorizedException("Unauthorized access");
            var context = CreateExceptionContext(exception);
            
            var filter = new ApiExceptionFilter(_mockLogger.Object, _mockEnvironment.Object);
            
            // Act
            filter.OnException(context);
            
            // Assert
            context.Result.Should().NotBeNull();
            context.ExceptionHandled.Should().BeTrue();
            
            var result = context.Result as ObjectResult;
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(401);
            
            var problemDetails = result.Value as ProblemDetails;
            problemDetails.Should().NotBeNull();
            problemDetails.Title.Should().Be("Unauthorized");
            problemDetails.Detail.Should().Be("Unauthorized access");
        }
        
        [Fact]
        public void HandleGenericException_ShouldReturnInternalServerError()
        {
            // Arrange
            var exception = new Exception("Unexpected error");
            var context = CreateExceptionContext(exception);
            
            var filter = new ApiExceptionFilter(_mockLogger.Object, _mockEnvironment.Object);
            
            // Act
            filter.OnException(context);
            
            // Assert
            context.Result.Should().NotBeNull();
            context.ExceptionHandled.Should().BeTrue();
            
            var result = context.Result as ObjectResult;
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(500);
            
            var problemDetails = result.Value as ProblemDetails;
            problemDetails.Should().NotBeNull();
            problemDetails.Title.Should().Be("Internal Server Error");
            problemDetails.Detail.Should().Be("Unexpected error");
        }
        
        [Fact]
        public void InDevelopmentEnvironment_ShouldIncludeExceptionDetails()
        {
            // Arrange
            _mockEnvironment.Setup(e => e.IsDevelopment()).Returns(true);
            
            var exception = new Exception("Unexpected error");
            var context = CreateExceptionContext(exception);
            
            var filter = new ApiExceptionFilter(_mockLogger.Object, _mockEnvironment.Object);
            
            // Act
            filter.OnException(context);
            
            // Assert
            context.Result.Should().NotBeNull();
            context.ExceptionHandled.Should().BeTrue();
            
            var result = context.Result as ObjectResult;
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(500);
            
            var problemDetails = result.Value as ProblemDetails;
            problemDetails.Should().NotBeNull();
            problemDetails.Extensions.Should().ContainKey("exceptionType");
            problemDetails.Extensions.Should().ContainKey("stackTrace");
        }
        
        private ExceptionContext CreateExceptionContext(Exception exception)
        {
            var actionContext = new ActionContext
            {
                HttpContext = new DefaultHttpContext(),
                RouteData = new Microsoft.AspNetCore.Routing.RouteData(),
                ActionDescriptor = new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor()
            };
            
            return new ExceptionContext(actionContext, new List<IFilterMetadata>())
            {
                Exception = exception
            };
        }
    }
}