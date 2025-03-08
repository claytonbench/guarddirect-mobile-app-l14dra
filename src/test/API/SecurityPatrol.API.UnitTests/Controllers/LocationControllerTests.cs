using System; // System 8.0+
using System.Collections.Generic; // System.Collections.Generic 8.0+
using System.Linq; // System.Linq 8.0+
using System.Threading.Tasks; // System.Threading.Tasks 8.0+
using Microsoft.AspNetCore.Mvc; // Microsoft.AspNetCore.Mvc 8.0+
using Moq; // Moq 4.18.0
using Xunit; // Xunit 2.4.0
using FluentAssertions; // FluentAssertions 6.0.0
using SecurityPatrol.API.UnitTests.Setup;
using SecurityPatrol.TestCommon.Constants;
using SecurityPatrol.TestCommon.Data;
using SecurityPatrol.API.Controllers;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.Core.Models;

namespace SecurityPatrol.API.UnitTests.Controllers
{
    /// <summary>
    /// Contains unit tests for the LocationController class to verify its behavior for processing location data,
    /// retrieving location history, and accessing current location information.
    /// </summary>
    public class LocationControllerTests : TestBase
    {
        /// <summary>
        /// Initializes a new instance of the LocationControllerTests class with mock services
        /// </summary>
        public LocationControllerTests()
        {
            // Initialize MockCurrentUserService with a new instance of Mock<ICurrentUserService>
            MockCurrentUserService = new Mock<ICurrentUserService>();

            // Configure MockCurrentUserService.Setup(x => x.IsAuthenticated()).Returns(true) by default
            MockCurrentUserService.Setup(x => x.IsAuthenticated()).Returns(true);

            // Configure MockCurrentUserService.Setup(x => x.GetUserId()).Returns(TestConstants.TestUserId) by default
            MockCurrentUserService.Setup(x => x.GetUserId()).Returns(TestConstants.TestUserId);
        }

        /// <summary>
        /// Gets or sets the mock current user service.
        /// </summary>
        public Mock<ICurrentUserService> MockCurrentUserService { get; set; }

        /// <summary>
        /// Tests that BatchAsync returns an OK result with a LocationSyncResponse when provided with a valid request
        /// </summary>
        [Fact]
        public async Task BatchAsync_WithValidRequest_ReturnsOkResult()
        {
            // Arrange
            // Create a LocationBatchRequest with test data
            var request = new LocationBatchRequest
            {
                UserId = TestConstants.TestUserId,
                Locations = new[] { MockDataGenerator.CreateLocationModel(1) }
            };

            // Create a LocationSyncResponse with test data
            var response = new LocationSyncResponse
            {
                SyncedIds = new[] { 1 },
                FailedIds = Array.Empty<int>()
            };

            // Configure MockLocationService to return the test response when ProcessLocationBatchAsync is called
            MockLocationService.Setup(x => x.ProcessLocationBatchAsync(request)).ReturnsAsync(response);

            // Act
            // Create a LocationController using CreateLocationController
            var controller = CreateLocationController();

            // Set up the HTTP context with an authentication token
            SetupHttpContext(controller, TestConstants.TestAuthToken);

            // Call controller.BatchAsync with the test request
            var result = await controller.BatchAsync(request);

            // Assert
            // Assert that the result is an OkObjectResult containing the expected LocationSyncResponse
            AssertActionResult<LocationSyncResponse>(result, response);
        }

        /// <summary>
        /// Tests that BatchAsync returns a BadRequest result when provided with a null request
        /// </summary>
        [Fact]
        public async Task BatchAsync_WithNullRequest_ReturnsBadRequest()
        {
            // Arrange
            // Create a LocationController using CreateLocationController
            var controller = CreateLocationController();

            // Set up the HTTP context with an authentication token
            SetupHttpContext(controller, TestConstants.TestAuthToken);

            // Act
            // Call controller.BatchAsync with a null request
            var result = await controller.BatchAsync(null);

            // Assert
            // Assert that the result is a BadRequestObjectResult
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        /// <summary>
        /// Tests that BatchAsync returns an Unauthorized result when the user is not authenticated
        /// </summary>
        [Fact]
        public async Task BatchAsync_WithUnauthenticatedUser_ReturnsUnauthorized()
        {
            // Arrange
            // Configure MockCurrentUserService.Setup(x => x.IsAuthenticated()).Returns(false)
            MockCurrentUserService.Setup(x => x.IsAuthenticated()).Returns(false);

            // Create a LocationController with MockCurrentUserService.Object
            var controller = new LocationController(MockLocationService.Object, MockCurrentUserService.Object, Mock.Of<ILogger<LocationController>>());

            // Set up the HTTP context without an authentication token
            SetupHttpContext(controller);

            // Create a valid LocationBatchRequest
            var request = new LocationBatchRequest
            {
                UserId = TestConstants.TestUserId,
                Locations = new[] { MockDataGenerator.CreateLocationModel(1) }
            };

            // Act
            // Call controller.BatchAsync with the request
            var result = await controller.BatchAsync(request);

            // Assert
            // Assert that the result is an UnauthorizedResult
            Assert.IsType<UnauthorizedResult>(result.Result);
        }

        /// <summary>
        /// Tests that BatchAsync sets the UserId in the request from the current user if not provided
        /// </summary>
        [Fact]
        public async Task BatchAsync_WithNoUserIdInRequest_SetsUserIdFromCurrentUser()
        {
            // Arrange
            // Create a LocationBatchRequest with null UserId
            var request = new LocationBatchRequest
            {
                UserId = null,
                Locations = new[] { MockDataGenerator.CreateLocationModel(1) }
            };

            // Create a LocationSyncResponse with test data
            var response = new LocationSyncResponse
            {
                SyncedIds = new[] { 1 },
                FailedIds = Array.Empty<int>()
            };

            // Configure MockLocationService to return the test response when ProcessLocationBatchAsync is called
            MockLocationService.Setup(x => x.ProcessLocationBatchAsync(It.Is<LocationBatchRequest>(r => r.UserId == TestConstants.TestUserId))).ReturnsAsync(response);

            // Create a LocationController with MockCurrentUserService.Object
            var controller = new LocationController(MockLocationService.Object, MockCurrentUserService.Object, Mock.Of<ILogger<LocationController>>());

            // Set up the HTTP context with an authentication token
            SetupHttpContext(controller, TestConstants.TestAuthToken);

            // Act
            // Call controller.BatchAsync with the request
            var result = await controller.BatchAsync(request);

            // Assert
            // Verify that MockLocationService.Verify was called with a request where UserId equals TestConstants.TestUserId
            MockLocationService.Verify(x => x.ProcessLocationBatchAsync(It.Is<LocationBatchRequest>(r => r.UserId == TestConstants.TestUserId)), Times.Once);

            // Assert that the result is an OkObjectResult containing the expected LocationSyncResponse
            AssertActionResult<LocationSyncResponse>(result, response);
        }

        /// <summary>
        /// Tests that BatchAsync returns an Internal Server Error when the location service throws an exception
        /// </summary>
        [Fact]
        public async Task BatchAsync_WhenServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            // Create a LocationBatchRequest with test data
            var request = new LocationBatchRequest
            {
                UserId = TestConstants.TestUserId,
                Locations = new[] { MockDataGenerator.CreateLocationModel(1) }
            };

            // Configure MockLocationService to throw an Exception when ProcessLocationBatchAsync is called
            MockLocationService.Setup(x => x.ProcessLocationBatchAsync(request)).ThrowsAsync(new Exception("Test exception"));

            // Act
            // Create a LocationController using CreateLocationController
            var controller = CreateLocationController();

            // Set up the HTTP context with an authentication token
            SetupHttpContext(controller, TestConstants.TestAuthToken);

            // Call controller.BatchAsync with the request
            var result = await controller.BatchAsync(request);

            // Assert
            // Assert that the result is a StatusCodeResult with status code 500
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        /// <summary>
        /// Tests that GetHistoryAsync returns an OK result with location history when provided with valid parameters
        /// </summary>
        [Fact]
        public async Task GetHistoryAsync_WithValidParameters_ReturnsOkResult()
        {
            // Arrange
            // Create a list of LocationModel objects for test data
            var locationHistory = new List<LocationModel>
            {
                MockDataGenerator.CreateLocationModel(1),
                MockDataGenerator.CreateLocationModel(2)
            };

            // Configure MockLocationService to return the test data when GetLocationHistoryAsync is called
            MockLocationService.Setup(x => x.GetLocationHistoryAsync(TestConstants.TestUserId, It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(locationHistory);

            // Act
            // Create a LocationController using CreateLocationController
            var controller = CreateLocationController();

            // Set up the HTTP context with an authentication token
            SetupHttpContext(controller, TestConstants.TestAuthToken);

            // Define startTime and endTime parameters (e.g., DateTime.UtcNow.AddDays(-1) and DateTime.UtcNow)
            DateTime startTime = DateTime.UtcNow.AddDays(-1);
            DateTime endTime = DateTime.UtcNow;

            // Call controller.GetHistoryAsync with the parameters
            var result = await controller.GetHistoryAsync(startTime, endTime);

            // Assert
            // Assert that the result is an OkObjectResult containing the expected location history
            AssertActionResult<IEnumerable<LocationModel>>(result, locationHistory);
        }

        /// <summary>
        /// Tests that GetHistoryAsync returns a BadRequest result when the start time is later than the end time
        /// </summary>
        [Fact]
        public async Task GetHistoryAsync_WithInvalidTimeRange_ReturnsBadRequest()
        {
            // Arrange
            // Create a LocationController using CreateLocationController
            var controller = CreateLocationController();

            // Set up the HTTP context with an authentication token
            SetupHttpContext(controller, TestConstants.TestAuthToken);

            // Define invalid startTime and endTime parameters (e.g., DateTime.UtcNow and DateTime.UtcNow.AddDays(-1))
            DateTime startTime = DateTime.UtcNow;
            DateTime endTime = DateTime.UtcNow.AddDays(-1);

            // Act
            // Call controller.GetHistoryAsync with the parameters
            var result = await controller.GetHistoryAsync(startTime, endTime);

            // Assert
            // Assert that the result is a BadRequestObjectResult
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        /// <summary>
        /// Tests that GetHistoryAsync returns an Unauthorized result when the user is not authenticated
        /// </summary>
        [Fact]
        public async Task GetHistoryAsync_WithUnauthenticatedUser_ReturnsUnauthorized()
        {
            // Arrange
            // Configure MockCurrentUserService.Setup(x => x.IsAuthenticated()).Returns(false)
            MockCurrentUserService.Setup(x => x.IsAuthenticated()).Returns(false);

            // Create a LocationController with MockCurrentUserService.Object
            var controller = new LocationController(MockLocationService.Object, MockCurrentUserService.Object, Mock.Of<ILogger<LocationController>>());

            // Set up the HTTP context without an authentication token
            SetupHttpContext(controller);

            // Define valid startTime and endTime parameters
            DateTime startTime = DateTime.UtcNow.AddDays(-1);
            DateTime endTime = DateTime.UtcNow;

            // Act
            // Call controller.GetHistoryAsync with the parameters
            var result = await controller.GetHistoryAsync(startTime, endTime);

            // Assert
            // Assert that the result is an UnauthorizedResult
            Assert.IsType<UnauthorizedResult>(result.Result);
        }

        /// <summary>
        /// Tests that GetHistoryAsync returns an Internal Server Error when the location service throws an exception
        /// </summary>
        [Fact]
        public async Task GetHistoryAsync_WhenServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            // Configure MockLocationService to throw an Exception when GetLocationHistoryAsync is called
            MockLocationService.Setup(x => x.GetLocationHistoryAsync(TestConstants.TestUserId, It.IsAny<DateTime>(), It.IsAny<DateTime>())).ThrowsAsync(new Exception("Test exception"));

            // Act
            // Create a LocationController using CreateLocationController
            var controller = CreateLocationController();

            // Set up the HTTP context with an authentication token
            SetupHttpContext(controller, TestConstants.TestAuthToken);

            // Define valid startTime and endTime parameters
            DateTime startTime = DateTime.UtcNow.AddDays(-1);
            DateTime endTime = DateTime.UtcNow;

            // Call controller.GetHistoryAsync with the parameters
            var result = await controller.GetHistoryAsync(startTime, endTime);

            // Assert
            // Assert that the result is a StatusCodeResult with status code 500
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        /// <summary>
        /// Tests that GetCurrentAsync returns an OK result with the current location when the user is valid
        /// </summary>
        [Fact]
        public async Task GetCurrentAsync_WithValidUser_ReturnsOkResult()
        {
            // Arrange
            // Create a LocationModel for test data
            var location = MockDataGenerator.CreateLocationModel(1);

            // Configure MockLocationService to return the test data when GetLatestLocationAsync is called
            MockLocationService.Setup(x => x.GetLatestLocationAsync(TestConstants.TestUserId)).ReturnsAsync(location);

            // Act
            // Create a LocationController using CreateLocationController
            var controller = CreateLocationController();

            // Set up the HTTP context with an authentication token
            SetupHttpContext(controller, TestConstants.TestAuthToken);

            // Call controller.GetCurrentAsync()
            var result = await controller.GetCurrentAsync();

            // Assert
            // Assert that the result is an OkObjectResult containing the expected location
            AssertActionResult<LocationModel>(result, location);
        }

        /// <summary>
        /// Tests that GetCurrentAsync returns a NoContent result when no location is found for the user
        /// </summary>
        [Fact]
        public async Task GetCurrentAsync_WithNoLocation_ReturnsNoContent()
        {
            // Arrange
            // Configure MockLocationService to return null when GetLatestLocationAsync is called
            MockLocationService.Setup(x => x.GetLatestLocationAsync(TestConstants.TestUserId)).ReturnsAsync((LocationModel)null);

            // Act
            // Create a LocationController using CreateLocationController
            var controller = CreateLocationController();

            // Set up the HTTP context with an authentication token
            SetupHttpContext(controller, TestConstants.TestAuthToken);

            // Call controller.GetCurrentAsync()
            var result = await controller.GetCurrentAsync();

            // Assert
            // Assert that the result is a NoContentResult
            Assert.IsType<NoContentResult>(result.Result);
        }

        /// <summary>
        /// Tests that GetCurrentAsync returns an Unauthorized result when the user is not authenticated
        /// </summary>
        [Fact]
        public async Task GetCurrentAsync_WithUnauthenticatedUser_ReturnsUnauthorized()
        {
            // Arrange
            // Configure MockCurrentUserService.Setup(x => x.IsAuthenticated()).Returns(false)
            MockCurrentUserService.Setup(x => x.IsAuthenticated()).Returns(false);

            // Create a LocationController with MockCurrentUserService.Object
            var controller = new LocationController(MockLocationService.Object, MockCurrentUserService.Object, Mock.Of<ILogger<LocationController>>());

            // Set up the HTTP context without an authentication token
            SetupHttpContext(controller);

            // Act
            // Call controller.GetCurrentAsync()
            var result = await controller.GetCurrentAsync();

            // Assert
            // Assert that the result is an UnauthorizedResult
            Assert.IsType<UnauthorizedResult>(result.Result);
        }

        /// <summary>
        /// Tests that GetCurrentAsync returns an Internal Server Error when the location service throws an exception
        /// </summary>
        [Fact]
        public async Task GetCurrentAsync_WhenServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            // Configure MockLocationService to throw an Exception when GetLatestLocationAsync is called
            MockLocationService.Setup(x => x.GetLatestLocationAsync(TestConstants.TestUserId)).ThrowsAsync(new Exception("Test exception"));

            // Act
            // Create a LocationController using CreateLocationController
            var controller = CreateLocationController();

            // Set up the HTTP context with an authentication token
            SetupHttpContext(controller, TestConstants.TestAuthToken);

            // Call controller.GetCurrentAsync()
            var result = await controller.GetCurrentAsync();

            // Assert
            // Assert that the result is a StatusCodeResult with status code 500
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        /// <summary>
        /// Tests that GetCurrentByUserIdAsync returns an OK result with the current location when provided with a valid user ID
        /// </summary>
        [Fact]
        public async Task GetCurrentByUserIdAsync_WithValidUserId_ReturnsOkResult()
        {
            // Arrange
            // Create a LocationModel for test data
            var location = MockDataGenerator.CreateLocationModel(1);

            // Configure MockLocationService to return the test data when GetLatestLocationAsync is called with the specific user ID
            MockLocationService.Setup(x => x.GetLatestLocationAsync("validUserId")).ReturnsAsync(location);

            // Act
            // Create a LocationController using CreateLocationController
            var controller = CreateLocationController();

            // Set up the HTTP context with an authentication token
            SetupHttpContext(controller, TestConstants.TestAuthToken);

            // Call controller.GetCurrentByUserIdAsync with a valid user ID
            var result = await controller.GetCurrentByUserIdAsync("validUserId");

            // Assert
            // Assert that the result is an OkObjectResult containing the expected location
            AssertActionResult<LocationModel>(result, location);
        }

        /// <summary>
        /// Tests that GetCurrentByUserIdAsync returns a BadRequest result when provided with a null or empty user ID
        /// </summary>
        [Fact]
        public async Task GetCurrentByUserIdAsync_WithNullUserId_ReturnsBadRequest()
        {
            // Arrange
            // Create a LocationController using CreateLocationController
            var controller = CreateLocationController();

            // Set up the HTTP context with an authentication token
            SetupHttpContext(controller, TestConstants.TestAuthToken);

            // Act
            // Call controller.GetCurrentByUserIdAsync with a null or empty user ID
            var result = await controller.GetCurrentByUserIdAsync(null);

            // Assert
            // Assert that the result is a BadRequestObjectResult
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        /// <summary>
        /// Tests that GetCurrentByUserIdAsync returns a NoContent result when no location is found for the specified user
        /// </summary>
        [Fact]
        public async Task GetCurrentByUserIdAsync_WithNoLocation_ReturnsNoContent()
        {
            // Arrange
            // Configure MockLocationService to return null when GetLatestLocationAsync is called with any user ID
            MockLocationService.Setup(x => x.GetLatestLocationAsync(It.IsAny<string>())).ReturnsAsync((LocationModel)null);

            // Act
            // Create a LocationController using CreateLocationController
            var controller = CreateLocationController();

            // Set up the HTTP context with an authentication token
            SetupHttpContext(controller, TestConstants.TestAuthToken);

            // Call controller.GetCurrentByUserIdAsync with a valid user ID
            var result = await controller.GetCurrentByUserIdAsync("validUserId");

            // Assert
            // Assert that the result is a NoContentResult
            Assert.IsType<NoContentResult>(result.Result);
        }

        /// <summary>
        /// Tests that GetCurrentByUserIdAsync returns an Internal Server Error when the location service throws an exception
        /// </summary>
        [Fact]
        public async Task GetCurrentByUserIdAsync_WhenServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            // Configure MockLocationService to throw an Exception when GetLatestLocationAsync is called
            MockLocationService.Setup(x => x.GetLatestLocationAsync(It.IsAny<string>())).ThrowsAsync(new Exception("Test exception"));

            // Act
            // Create a LocationController using CreateLocationController
            var controller = CreateLocationController();

            // Set up the HTTP context with an authentication token
            SetupHttpContext(controller, TestConstants.TestAuthToken);

            // Call controller.GetCurrentByUserIdAsync with a valid user ID
            var result = await controller.GetCurrentByUserIdAsync("validUserId");

            // Assert
            // Assert that the result is a StatusCodeResult with status code 500
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }
    }
}