using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using SecurityPatrol.TestCommon.Constants;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.API.Controllers;

namespace SecurityPatrol.API.UnitTests.Setup
{
    /// <summary>
    /// Base class for API unit tests providing common setup, mock repositories, mock services, and utility methods
    /// to simplify test implementation and ensure consistency across test classes.
    /// </summary>
    public class TestBase
    {
        // Mock repositories and services
        protected Mock<IUserRepository> MockUserRepository { get; }
        protected Mock<IAuthenticationService> MockAuthenticationService { get; }
        protected Mock<IVerificationCodeService> MockVerificationCodeService { get; }
        protected Mock<ITokenService> MockTokenService { get; }
        protected Mock<ISmsService> MockSmsService { get; }
        protected Mock<ITimeRecordService> MockTimeRecordService { get; }
        protected Mock<ILocationService> MockLocationService { get; }
        protected Mock<IPatrolService> MockPatrolService { get; }
        protected Mock<IPhotoService> MockPhotoService { get; }
        protected Mock<IReportService> MockReportService { get; }

        /// <summary>
        /// Initializes a new instance of the TestBase class with mock repositories and services
        /// </summary>
        public TestBase()
        {
            // Initialize all mock repositories and services with new instances of Mock<T>
            MockUserRepository = new Mock<IUserRepository>();
            MockAuthenticationService = new Mock<IAuthenticationService>();
            MockVerificationCodeService = new Mock<IVerificationCodeService>();
            MockTokenService = new Mock<ITokenService>();
            MockSmsService = new Mock<ISmsService>();
            MockTimeRecordService = new Mock<ITimeRecordService>();
            MockLocationService = new Mock<ILocationService>();
            MockPatrolService = new Mock<IPatrolService>();
            MockPhotoService = new Mock<IPhotoService>();
            MockReportService = new Mock<IReportService>();
            
            // Configure default behaviors for mock repositories and services
        }

        /// <summary>
        /// Resets all mocks to their initial state
        /// </summary>
        protected void ResetMocks()
        {
            MockUserRepository.Reset();
            MockAuthenticationService.Reset();
            MockVerificationCodeService.Reset();
            MockTokenService.Reset();
            MockSmsService.Reset();
            MockTimeRecordService.Reset();
            MockLocationService.Reset();
            MockPatrolService.Reset();
            MockPhotoService.Reset();
            MockReportService.Reset();
        }

        /// <summary>
        /// Creates an instance of AuthController with mocked dependencies
        /// </summary>
        /// <returns>A new instance of AuthController with mocked dependencies</returns>
        protected AuthController CreateAuthController()
        {
            return new AuthController(
                MockAuthenticationService.Object,
                Mock.Of<ILogger<AuthController>>());
        }

        /// <summary>
        /// Creates an instance of TimeController with mocked dependencies
        /// </summary>
        /// <returns>A new instance of TimeController with mocked dependencies</returns>
        protected TimeController CreateTimeController()
        {
            return new TimeController(
                MockTimeRecordService.Object,
                Mock.Of<ICurrentUserService>(),
                Mock.Of<ILogger<TimeController>>());
        }

        /// <summary>
        /// Creates an instance of LocationController with mocked dependencies
        /// </summary>
        /// <returns>A new instance of LocationController with mocked dependencies</returns>
        protected LocationController CreateLocationController()
        {
            return new LocationController(
                MockLocationService.Object,
                Mock.Of<ICurrentUserService>(),
                Mock.Of<ILogger<LocationController>>());
        }

        /// <summary>
        /// Creates an instance of PatrolController with mocked dependencies
        /// </summary>
        /// <returns>A new instance of PatrolController with mocked dependencies</returns>
        protected PatrolController CreatePatrolController()
        {
            return new PatrolController(
                MockPatrolService.Object,
                Mock.Of<ICurrentUserService>(),
                Mock.Of<ILogger<PatrolController>>());
        }

        /// <summary>
        /// Creates an instance of PhotoController with mocked dependencies
        /// </summary>
        /// <returns>A new instance of PhotoController with mocked dependencies</returns>
        protected PhotoController CreatePhotoController()
        {
            return new PhotoController(
                MockPhotoService.Object,
                Mock.Of<ICurrentUserService>(),
                Mock.Of<ILogger<PhotoController>>());
        }

        /// <summary>
        /// Creates an instance of ReportController with mocked dependencies
        /// </summary>
        /// <returns>A new instance of ReportController with mocked dependencies</returns>
        protected ReportController CreateReportController()
        {
            return new ReportController(
                MockReportService.Object,
                Mock.Of<ICurrentUserService>(),
                Mock.Of<ILogger<ReportController>>());
        }

        /// <summary>
        /// Sets up the HTTP context for a controller with optional authorization header
        /// </summary>
        /// <param name="controller">The controller to configure</param>
        /// <param name="authToken">Optional authorization token to include in the request</param>
        protected void SetupHttpContext(ControllerBase controller, string authToken = null)
        {
            var httpContext = new DefaultHttpContext();
            
            if (!string.IsNullOrEmpty(authToken))
            {
                httpContext.Request.Headers["Authorization"] = $"Bearer {authToken}";
            }
            
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        /// <summary>
        /// Asserts that an action result is of the expected type and has the expected value
        /// </summary>
        /// <typeparam name="T">The expected type of the result value</typeparam>
        /// <param name="result">The action result to check</param>
        /// <param name="expectedValue">The expected value</param>
        protected void AssertActionResult<T>(IActionResult result, T expectedValue)
        {
            result.Should().NotBeNull();
            result.Should().BeOfType<ObjectResult>();
            
            var objectResult = (ObjectResult)result;
            objectResult.Value.Should().BeOfType<T>();
            objectResult.Value.Should().BeEquivalentTo(expectedValue);
        }

        /// <summary>
        /// Asserts that the specified action throws an exception of type T
        /// </summary>
        /// <typeparam name="T">The expected exception type</typeparam>
        /// <param name="action">The action that should throw an exception</param>
        /// <returns>A task representing the asynchronous operation</returns>
        protected async Task AssertExceptionAsync<T>(Func<Task> action) where T : Exception
        {
            try
            {
                await action();
                Assert.Fail($"Expected exception of type {typeof(T).Name} but no exception was thrown");
            }
            catch (Exception ex)
            {
                if (!(ex is T))
                {
                    Assert.Fail($"Expected exception of type {typeof(T).Name} but got {ex.GetType().Name}");
                }
                // If an exception of type T is thrown, the test passes
            }
        }
    }
}