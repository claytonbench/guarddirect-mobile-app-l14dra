using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using Xunit;
using Moq;
using Moq.Protected;
using FluentAssertions;
using SecurityPatrol.MAUI.UnitTests.Setup;
using SecurityPatrol.Services;
using SecurityPatrol.Constants;

namespace SecurityPatrol.MAUI.UnitTests.Services
{
    /// <summary>
    /// Test class for the ApiService implementation that handles HTTP communication with backend services
    /// </summary>
    public class ApiServiceTests
    {
        private Mock<ITokenManager> mockTokenManager;
        private Mock<INetworkService> mockNetworkService;
        private Mock<ITelemetryService> mockTelemetryService;
        private HttpClient httpClient;
        private ApiService apiService;

        /// <summary>
        /// Initializes a new instance of the ApiServiceTests class with test setup
        /// </summary>
        public ApiServiceTests()
        {
            // Initialize mocks
            mockTokenManager = new Mock<ITokenManager>();
            mockNetworkService = new Mock<INetworkService>();
            mockTelemetryService = new Mock<ITelemetryService>();
            
            // Setup default mock behaviors
            SetupDefaultMocks();
            
            // Initialize HttpClient and ApiService
            httpClient = new HttpClient();
            apiService = new ApiService(httpClient, mockTokenManager.Object, mockNetworkService.Object, mockTelemetryService.Object);
        }

        /// <summary>
        /// Configures the default behaviors for mock dependencies
        /// </summary>
        private void SetupDefaultMocks()
        {
            // Setup network service
            mockNetworkService.Setup(x => x.IsConnected).Returns(true);
            mockNetworkService.Setup(x => x.ShouldAttemptOperation(It.IsAny<NetworkOperationType>())).Returns(true);
            
            // Setup token manager
            mockTokenManager.Setup(x => x.IsTokenValid()).ReturnsAsync(true);
            mockTokenManager.Setup(x => x.RetrieveToken()).ReturnsAsync("test-token");
            
            // Setup telemetry service
            mockTelemetryService.Setup(x => x.TrackApiCall(It.IsAny<string>(), It.IsAny<TimeSpan>(), 
                It.IsAny<bool>(), It.IsAny<string>())).Verifiable();
            mockTelemetryService.Setup(x => x.TrackException(It.IsAny<Exception>(), It.IsAny<Dictionary<string, string>>())).Verifiable();
        }

        /// <summary>
        /// Configures a mock HttpMessageHandler for testing HTTP responses
        /// </summary>
        private HttpClient ConfigureHttpClientHandler(HttpStatusCode statusCode, string content)
        {
            var handler = new Mock<HttpMessageHandler>();
            handler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(content, Encoding.UTF8, "application/json")
                });

            return new HttpClient(handler.Object);
        }

        /// <summary>
        /// Tests that GetAsync successfully processes a response with 200 status code
        /// </summary>
        [Fact]
        public async Task Test_GetAsync_Success()
        {
            // Arrange
            var testJson = "{\"id\":1,\"name\":\"Test\",\"isActive\":true}";
            httpClient = ConfigureHttpClientHandler(HttpStatusCode.OK, testJson);
            apiService = new ApiService(httpClient, mockTokenManager.Object, mockNetworkService.Object, mockTelemetryService.Object);
            
            // Act
            var result = await apiService.GetAsync<TestModel>("test-endpoint", null, false);
            
            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(1);
            result.Name.Should().Be("Test");
            result.IsActive.Should().BeTrue();
            
            mockTelemetryService.Verify(x => x.TrackApiCall(
                It.IsAny<string>(), 
                It.IsAny<TimeSpan>(), 
                true, 
                "200"), Times.Once);
        }

        /// <summary>
        /// Tests that GetAsync correctly appends query parameters to the URL
        /// </summary>
        [Fact]
        public async Task Test_GetAsync_WithQueryParameters()
        {
            // Arrange
            var queryParams = new Dictionary<string, string>
            {
                { "param1", "value1" },
                { "param2", "value2" }
            };
            
            var testJson = "{\"id\":1,\"name\":\"Test\",\"isActive\":true}";
            var handler = new Mock<HttpMessageHandler>();
            handler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(r => 
                        r.Method == HttpMethod.Get &&
                        r.RequestUri.ToString().Contains("param1=value1") && 
                        r.RequestUri.ToString().Contains("param2=value2")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(testJson, Encoding.UTF8, "application/json")
                });
            
            httpClient = new HttpClient(handler.Object);
            apiService = new ApiService(httpClient, mockTokenManager.Object, mockNetworkService.Object, mockTelemetryService.Object);
            
            // Act
            var result = await apiService.GetAsync<TestModel>("test-endpoint", queryParams, false);
            
            // Assert
            result.Should().NotBeNull();
            handler.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.RequestUri.ToString().Contains("param1=value1") && 
                    req.RequestUri.ToString().Contains("param2=value2")),
                ItExpr.IsAny<CancellationToken>());
        }

        /// <summary>
        /// Tests that GetAsync adds authentication token when requiresAuth is true
        /// </summary>
        [Fact]
        public async Task Test_GetAsync_WithAuthentication()
        {
            // Arrange
            var testJson = "{\"id\":1,\"name\":\"Test\",\"isActive\":true}";
            var handler = new Mock<HttpMessageHandler>();
            handler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(r => 
                        r.Headers.Authorization != null && 
                        r.Headers.Authorization.Scheme == "Bearer" && 
                        r.Headers.Authorization.Parameter == "test-token"),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(testJson, Encoding.UTF8, "application/json")
                });
            
            httpClient = new HttpClient(handler.Object);
            apiService = new ApiService(httpClient, mockTokenManager.Object, mockNetworkService.Object, mockTelemetryService.Object);
            
            // Act
            var result = await apiService.GetAsync<TestModel>("test-endpoint", null, true);
            
            // Assert
            result.Should().NotBeNull();
            handler.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Headers.Authorization != null && 
                    req.Headers.Authorization.Scheme == "Bearer" && 
                    req.Headers.Authorization.Parameter == "test-token"),
                ItExpr.IsAny<CancellationToken>());
        }

        /// <summary>
        /// Tests that GetAsync throws appropriate exception when network is unavailable
        /// </summary>
        [Fact]
        public async Task Test_GetAsync_NetworkUnavailable()
        {
            // Arrange
            mockNetworkService.Setup(x => x.IsConnected).Returns(false);
            
            // Act & Assert
            var exception = await Assert.ThrowsAsync<HttpRequestException>(() => 
                apiService.GetAsync<TestModel>("test-endpoint", null, false));
            
            exception.Message.Should().Contain(ErrorMessages.NetworkError);
        }

        /// <summary>
        /// Tests that GetAsync handles 401 Unauthorized response correctly
        /// </summary>
        [Fact]
        public async Task Test_GetAsync_UnauthorizedResponse()
        {
            // Arrange
            httpClient = ConfigureHttpClientHandler(HttpStatusCode.Unauthorized, "Unauthorized");
            apiService = new ApiService(httpClient, mockTokenManager.Object, mockNetworkService.Object, mockTelemetryService.Object);
            
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
                apiService.GetAsync<TestModel>("test-endpoint", null, false));
        }

        /// <summary>
        /// Tests that GetAsync handles 500 Server Error response correctly
        /// </summary>
        [Fact]
        public async Task Test_GetAsync_ServerError()
        {
            // Arrange
            httpClient = ConfigureHttpClientHandler(HttpStatusCode.InternalServerError, "Server Error");
            apiService = new ApiService(httpClient, mockTokenManager.Object, mockNetworkService.Object, mockTelemetryService.Object);
            
            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
                apiService.GetAsync<TestModel>("test-endpoint", null, false));
            
            exception.Message.Should().Be(ErrorMessages.ServerError);
        }

        /// <summary>
        /// Tests that GetAsync handles invalid JSON response correctly
        /// </summary>
        [Fact]
        public async Task Test_GetAsync_InvalidJson()
        {
            // Arrange
            var invalidJson = "This is not valid JSON";
            httpClient = ConfigureHttpClientHandler(HttpStatusCode.OK, invalidJson);
            apiService = new ApiService(httpClient, mockTokenManager.Object, mockNetworkService.Object, mockTelemetryService.Object);
            
            // Act & Assert
            await Assert.ThrowsAsync<JsonException>(() => 
                apiService.GetAsync<TestModel>("test-endpoint", null, false));
        }

        /// <summary>
        /// Tests that PostAsync successfully processes a response with 200 status code
        /// </summary>
        [Fact]
        public async Task Test_PostAsync_Success()
        {
            // Arrange
            var testJson = "{\"id\":1,\"name\":\"Test\",\"isActive\":true}";
            var testData = new { Name = "Test Post" };
            
            httpClient = ConfigureHttpClientHandler(HttpStatusCode.OK, testJson);
            apiService = new ApiService(httpClient, mockTokenManager.Object, mockNetworkService.Object, mockTelemetryService.Object);
            
            // Act
            var result = await apiService.PostAsync<TestModel>("test-endpoint", testData, false);
            
            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(1);
            result.Name.Should().Be("Test");
            result.IsActive.Should().BeTrue();
            mockTelemetryService.Verify(x => x.TrackApiCall(It.IsAny<string>(), It.IsAny<TimeSpan>(), true, "200"), Times.Once);
        }

        /// <summary>
        /// Tests that PostAsync adds authentication token when requiresAuth is true
        /// </summary>
        [Fact]
        public async Task Test_PostAsync_WithAuthentication()
        {
            // Arrange
            var testJson = "{\"id\":1,\"name\":\"Test\",\"isActive\":true}";
            var testData = new { Name = "Test Post" };
            
            var handler = new Mock<HttpMessageHandler>();
            handler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(r => 
                        r.Method == HttpMethod.Post &&
                        r.Headers.Authorization != null && 
                        r.Headers.Authorization.Scheme == "Bearer" && 
                        r.Headers.Authorization.Parameter == "test-token"),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(testJson, Encoding.UTF8, "application/json")
                });
            
            httpClient = new HttpClient(handler.Object);
            apiService = new ApiService(httpClient, mockTokenManager.Object, mockNetworkService.Object, mockTelemetryService.Object);
            
            // Act
            var result = await apiService.PostAsync<TestModel>("test-endpoint", testData, true);
            
            // Assert
            result.Should().NotBeNull();
            handler.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Headers.Authorization != null && 
                    req.Headers.Authorization.Scheme == "Bearer" && 
                    req.Headers.Authorization.Parameter == "test-token"),
                ItExpr.IsAny<CancellationToken>());
        }

        /// <summary>
        /// Tests that PostAsync throws appropriate exception when network is unavailable
        /// </summary>
        [Fact]
        public async Task Test_PostAsync_NetworkUnavailable()
        {
            // Arrange
            mockNetworkService.Setup(x => x.IsConnected).Returns(false);
            var testData = new { Name = "Test Post" };
            
            // Act & Assert
            var exception = await Assert.ThrowsAsync<HttpRequestException>(() => 
                apiService.PostAsync<TestModel>("test-endpoint", testData, false));
            
            exception.Message.Should().Contain(ErrorMessages.NetworkError);
        }

        /// <summary>
        /// Tests that PostMultipartAsync successfully processes a response with 200 status code
        /// </summary>
        [Fact]
        public async Task Test_PostMultipartAsync_Success()
        {
            // Arrange
            var testJson = "{\"id\":1,\"name\":\"Test\",\"isActive\":true}";
            var content = new MultipartFormDataContent();
            content.Add(new StringContent("test"), "name");
            
            httpClient = ConfigureHttpClientHandler(HttpStatusCode.OK, testJson);
            apiService = new ApiService(httpClient, mockTokenManager.Object, mockNetworkService.Object, mockTelemetryService.Object);
            
            // Act
            var result = await apiService.PostMultipartAsync<TestModel>("test-endpoint", content, false);
            
            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(1);
            result.Name.Should().Be("Test");
            result.IsActive.Should().BeTrue();
            mockTelemetryService.Verify(x => x.TrackApiCall(It.IsAny<string>(), It.IsAny<TimeSpan>(), true, "200"), Times.Once);
        }

        /// <summary>
        /// Tests that PutAsync successfully processes a response with 200 status code
        /// </summary>
        [Fact]
        public async Task Test_PutAsync_Success()
        {
            // Arrange
            var testJson = "{\"id\":1,\"name\":\"Test\",\"isActive\":true}";
            var testData = new { Name = "Test Put" };
            
            httpClient = ConfigureHttpClientHandler(HttpStatusCode.OK, testJson);
            apiService = new ApiService(httpClient, mockTokenManager.Object, mockNetworkService.Object, mockTelemetryService.Object);
            
            // Act
            var result = await apiService.PutAsync<TestModel>("test-endpoint", testData, false);
            
            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(1);
            result.Name.Should().Be("Test");
            result.IsActive.Should().BeTrue();
            mockTelemetryService.Verify(x => x.TrackApiCall(It.IsAny<string>(), It.IsAny<TimeSpan>(), true, "200"), Times.Once);
        }

        /// <summary>
        /// Tests that DeleteAsync successfully processes a response with 200 status code
        /// </summary>
        [Fact]
        public async Task Test_DeleteAsync_Success()
        {
            // Arrange
            var testJson = "{\"id\":1,\"name\":\"Test\",\"isActive\":true}";
            
            httpClient = ConfigureHttpClientHandler(HttpStatusCode.OK, testJson);
            apiService = new ApiService(httpClient, mockTokenManager.Object, mockNetworkService.Object, mockTelemetryService.Object);
            
            // Act
            var result = await apiService.DeleteAsync<TestModel>("test-endpoint", false);
            
            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(1);
            result.Name.Should().Be("Test");
            result.IsActive.Should().BeTrue();
            mockTelemetryService.Verify(x => x.TrackApiCall(It.IsAny<string>(), It.IsAny<TimeSpan>(), true, "200"), Times.Once);
        }

        /// <summary>
        /// Tests that API methods handle token refresh failure correctly
        /// </summary>
        [Fact]
        public async Task Test_TokenRefreshFailure()
        {
            // Arrange
            mockTokenManager.Setup(x => x.IsTokenValid()).ReturnsAsync(true);
            mockTokenManager.Setup(x => x.RetrieveToken()).ThrowsAsync(new UnauthorizedAccessException(ErrorMessages.UnauthorizedAccess));
            
            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
                apiService.GetAsync<TestModel>("test-endpoint", null, true));
            
            exception.Message.Should().Be(ErrorMessages.UnauthorizedAccess);
        }

        /// <summary>
        /// Tests that the retry policy correctly retries on server errors
        /// </summary>
        [Fact]
        public async Task Test_RetryPolicy_ServerError()
        {
            // Arrange
            var testJson = "{\"id\":1,\"name\":\"Test\",\"isActive\":true}";
            
            // Setup handler to return 503 twice, then 200
            var handlerMock = new Mock<HttpMessageHandler>();
            var sequence = handlerMock.Protected()
                .SetupSequence<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>());
            
            sequence.ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.ServiceUnavailable,
                Content = new StringContent("Service Unavailable")
            });
            
            sequence.ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.ServiceUnavailable,
                Content = new StringContent("Service Unavailable")
            });
            
            sequence.ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(testJson, Encoding.UTF8, "application/json")
            });
            
            httpClient = new HttpClient(handlerMock.Object);
            apiService = new ApiService(httpClient, mockTokenManager.Object, mockNetworkService.Object, mockTelemetryService.Object);
            
            // Act
            var result = await apiService.GetAsync<TestModel>("test-endpoint", null, false);
            
            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(1);
            
            // Verify the handler was called 3 times (initial + 2 retries)
            handlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(3),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        }

        /// <summary>
        /// Tests that the retry policy stops after maximum retries
        /// </summary>
        [Fact]
        public async Task Test_RetryPolicy_MaxRetriesExceeded()
        {
            // Arrange
            // Setup handler to always return 503
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.ServiceUnavailable,
                    Content = new StringContent("Service Unavailable")
                });
            
            httpClient = new HttpClient(handlerMock.Object);
            apiService = new ApiService(httpClient, mockTokenManager.Object, mockNetworkService.Object, mockTelemetryService.Object);
            
            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
                apiService.GetAsync<TestModel>("test-endpoint", null, false));
            
            exception.Message.Should().Be(ErrorMessages.ServerError);
            
            // Verify the handler was called 4 times (initial + 3 retries)
            handlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(4),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        }

        /// <summary>
        /// Tests that the BuildUrl method correctly appends query parameters
        /// </summary>
        [Fact]
        public void Test_BuildUrl_WithQueryParameters()
        {
            // Arrange
            var endpoint = "test-endpoint";
            var queryParams = new Dictionary<string, string>
            {
                { "param1", "value1" },
                { "param2", "value2" }
            };
            
            // Use reflection to access private BuildUrl method
            var method = typeof(ApiService).GetMethod("BuildUrl", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            // Act
            var result = method.Invoke(apiService, new object[] { endpoint, queryParams }) as string;
            
            // Assert
            result.Should().StartWith(endpoint);
            result.Should().Contain("param1=value1");
            result.Should().Contain("param2=value2");
        }

        /// <summary>
        /// Tests that the BuildUrl method handles empty query parameters correctly
        /// </summary>
        [Fact]
        public void Test_BuildUrl_WithEmptyQueryParameters()
        {
            // Arrange
            var endpoint = "test-endpoint";
            var queryParams = new Dictionary<string, string>();
            
            // Use reflection to access private BuildUrl method
            var method = typeof(ApiService).GetMethod("BuildUrl", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            // Act
            var result = method.Invoke(apiService, new object[] { endpoint, queryParams }) as string;
            
            // Assert
            result.Should().Be(endpoint);
        }

        /// <summary>
        /// Tests that the BuildUrl method handles null query parameters correctly
        /// </summary>
        [Fact]
        public void Test_BuildUrl_WithNullQueryParameters()
        {
            // Arrange
            var endpoint = "test-endpoint";
            
            // Use reflection to access private BuildUrl method
            var method = typeof(ApiService).GetMethod("BuildUrl", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            // Act
            var result = method.Invoke(apiService, new object[] { endpoint, null }) as string;
            
            // Assert
            result.Should().Be(endpoint);
        }

        /// <summary>
        /// Tests that the HandleResponse method does not throw for successful responses
        /// </summary>
        [Fact]
        public async Task Test_HandleResponse_SuccessfulResponse()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            
            // Use reflection to access private HandleResponse method
            var method = typeof(ApiService).GetMethod("HandleResponse", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            // Act & Assert - Should not throw
            var action = () => method.Invoke(apiService, new object[] { response });
            action.Should().NotThrow();
        }

        /// <summary>
        /// Tests that the HandleResponse method throws UnauthorizedAccessException for 401 responses
        /// </summary>
        [Fact]
        public async Task Test_HandleResponse_UnauthorizedResponse()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            
            // Use reflection to access private HandleResponse method
            var method = typeof(ApiService).GetMethod("HandleResponse", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            // Act & Assert
            var action = () => method.Invoke(apiService, new object[] { response });
            action.Should().Throw<TargetInvocationException>()
                .WithInnerException<UnauthorizedAccessException>()
                .WithMessage(ErrorMessages.UnauthorizedAccess);
        }

        /// <summary>
        /// Tests that the HandleResponse method throws KeyNotFoundException for 404 responses
        /// </summary>
        [Fact]
        public async Task Test_HandleResponse_NotFoundResponse()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.NotFound);
            response.RequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://test.com");
            
            // Use reflection to access private HandleResponse method
            var method = typeof(ApiService).GetMethod("HandleResponse", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            // Act & Assert
            var action = () => method.Invoke(apiService, new object[] { response });
            action.Should().Throw<TargetInvocationException>()
                .WithInnerException<KeyNotFoundException>();
        }

        /// <summary>
        /// Tests that the HandleResponse method throws ArgumentException for 400 responses
        /// </summary>
        [Fact]
        public async Task Test_HandleResponse_BadRequestResponse()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            response.Content = new StringContent("Bad Request Error");
            
            // Use reflection to access private HandleResponse method
            var method = typeof(ApiService).GetMethod("HandleResponse", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            // Act & Assert
            var action = () => method.Invoke(apiService, new object[] { response });
            action.Should().Throw<TargetInvocationException>()
                .WithInnerException<ArgumentException>();
        }

        /// <summary>
        /// Tests that the HandleResponse method throws InvalidOperationException for 5xx responses
        /// </summary>
        [Fact]
        public async Task Test_HandleResponse_ServerErrorResponse()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            
            // Use reflection to access private HandleResponse method
            var method = typeof(ApiService).GetMethod("HandleResponse", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            // Act & Assert
            var action = () => method.Invoke(apiService, new object[] { response });
            action.Should().Throw<TargetInvocationException>()
                .WithInnerException<InvalidOperationException>()
                .WithMessage(ErrorMessages.ServerError);
        }
    }

    /// <summary>
    /// Simple model class for testing API serialization and deserialization
    /// </summary>
    public class TestModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
    }
}