using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.Infrastructure.Services;
using SecurityPatrol.TestCommon.Constants;
using SecurityPatrol.API.UnitTests.Setup;
using Xunit;
using FluentAssertions;

namespace SecurityPatrol.API.UnitTests.Services
{
    /// <summary>
    /// Contains unit tests for the SmsService class to verify its functionality for sending SMS messages and verification codes.
    /// </summary>
    public class SmsServiceTests
    {
        private Mock<IOptions<SmsOptions>> _mockOptions;
        private Mock<ILogger<SmsService>> _mockLogger;
        private Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private HttpClient _httpClient;
        private SmsOptions _smsOptions;
        private SmsService _smsService;

        /// <summary>
        /// Initializes a new instance of the SmsServiceTests class with mocked dependencies.
        /// </summary>
        public SmsServiceTests()
        {
            _mockOptions = new Mock<IOptions<SmsOptions>>();
            _mockLogger = new Mock<ILogger<SmsService>>();
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();

            // Create HTTP client with mocked handler
            _httpClient = new HttpClient(_mockHttpMessageHandler.Object);

            // Create default SmsOptions
            _smsOptions = new SmsOptions
            {
                ApiKey = "test-api-key",
                ApiUrl = "https://api.test.com/",
                FromNumber = "+15551234567",
                VerificationMessageTemplate = "Your verification code is: {0}"
            };

            // Setup mock options to return our test options
            _mockOptions.Setup(x => x.Value).Returns(_smsOptions);

            // Initialize the service with mocked dependencies
            _smsService = new SmsService(_mockOptions.Object, _mockLogger.Object, _httpClient);
        }

        /// <summary>
        /// Sets up the test environment before each test.
        /// </summary>
        public void Setup()
        {
            // Reset all mocks
            _mockOptions.Reset();
            _mockLogger.Reset();
            _mockHttpMessageHandler.Reset();

            // Create default SmsOptions
            _smsOptions = new SmsOptions
            {
                ApiKey = "test-api-key",
                ApiUrl = "https://api.test.com/",
                FromNumber = "+15551234567",
                VerificationMessageTemplate = "Your verification code is: {0}"
            };

            // Setup mock options to return our test options
            _mockOptions.Setup(x => x.Value).Returns(_smsOptions);

            // Setup HTTP handler to return a successful response by default
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"success\":true}", Encoding.UTF8, "application/json")
                });

            // Initialize the service with mocked dependencies
            _smsService = new SmsService(_mockOptions.Object, _mockLogger.Object, _httpClient);
        }

        /// <summary>
        /// Tests that SendSmsAsync returns true when called with valid parameters and the HTTP request succeeds.
        /// </summary>
        [Fact]
        public async Task SendSmsAsync_WithValidParameters_ShouldReturnTrue()
        {
            // Arrange
            Setup();
            var testMessage = "Test message";
            
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"success\":true}", Encoding.UTF8, "application/json")
                })
                .Verifiable();

            // Act
            var result = await _smsService.SendSmsAsync(TestConstants.TestPhoneNumber, testMessage);

            // Assert
            result.Should().BeTrue();
            
            // Verify HTTP request was sent with the expected content
            _mockHttpMessageHandler
                .Protected()
                .Verify(
                    "SendAsync",
                    Times.Once(),
                    ItExpr.Is<HttpRequestMessage>(req => 
                        req.Method == HttpMethod.Post && 
                        req.RequestUri.ToString().Contains("sms/send")),
                    ItExpr.IsAny<CancellationToken>());
        }

        /// <summary>
        /// Tests that SendSmsAsync returns false when the HTTP request fails.
        /// </summary>
        [Fact]
        public async Task SendSmsAsync_WithHttpFailure_ShouldReturnFalse()
        {
            // Arrange
            Setup();
            var testMessage = "Test message";
            
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent("{\"error\":\"Bad request\"}", Encoding.UTF8, "application/json")
                })
                .Verifiable();

            // Act
            var result = await _smsService.SendSmsAsync(TestConstants.TestPhoneNumber, testMessage);

            // Assert
            result.Should().BeFalse();
            
            // Verify HTTP request was made
            _mockHttpMessageHandler
                .Protected()
                .Verify(
                    "SendAsync",
                    Times.Once(),
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>());
            
            // Verify error was logged
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to send SMS")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        /// <summary>
        /// Tests that SendSmsAsync returns false when an HTTP exception occurs.
        /// </summary>
        [Fact]
        public async Task SendSmsAsync_WithHttpException_ShouldReturnFalse()
        {
            // Arrange
            Setup();
            var testMessage = "Test message";
            
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Network error"))
                .Verifiable();

            // Act
            var result = await _smsService.SendSmsAsync(TestConstants.TestPhoneNumber, testMessage);

            // Assert
            result.Should().BeFalse();
            
            // Verify error was logged
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("HTTP request error")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        /// <summary>
        /// Tests that SendSmsAsync throws ArgumentNullException when phoneNumber is null.
        /// </summary>
        [Fact]
        public async Task SendSmsAsync_WithNullPhoneNumber_ShouldThrowArgumentNullException()
        {
            // Arrange
            Setup();
            var testMessage = "Test message";

            // Act & Assert
            await TestBase.AssertExceptionAsync<ArgumentNullException>(
                () => _smsService.SendSmsAsync(null, testMessage));
        }

        /// <summary>
        /// Tests that SendSmsAsync throws ArgumentNullException when message is null.
        /// </summary>
        [Fact]
        public async Task SendSmsAsync_WithNullMessage_ShouldThrowArgumentNullException()
        {
            // Arrange
            Setup();

            // Act & Assert
            await TestBase.AssertExceptionAsync<ArgumentNullException>(
                () => _smsService.SendSmsAsync(TestConstants.TestPhoneNumber, null));
        }

        /// <summary>
        /// Tests that SendVerificationCodeAsync returns true when called with valid parameters and the HTTP request succeeds.
        /// </summary>
        [Fact]
        public async Task SendVerificationCodeAsync_WithValidParameters_ShouldReturnTrue()
        {
            // Arrange
            Setup();
            var expectedMessage = string.Format(_smsOptions.VerificationMessageTemplate, TestConstants.TestVerificationCode);
            
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => 
                        req.Method == HttpMethod.Post && 
                        req.RequestUri.ToString().Contains("sms/send") &&
                        req.Content.ReadAsStringAsync().Result.Contains(JsonSerializer.Serialize(expectedMessage))),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"success\":true}", Encoding.UTF8, "application/json")
                })
                .Verifiable();

            // Act
            var result = await _smsService.SendVerificationCodeAsync(TestConstants.TestPhoneNumber, TestConstants.TestVerificationCode);

            // Assert
            result.Should().BeTrue();
            
            // Verify HTTP request was made with expected content
            _mockHttpMessageHandler
                .Protected()
                .Verify(
                    "SendAsync",
                    Times.Once(),
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>());
        }

        /// <summary>
        /// Tests that SendVerificationCodeAsync returns false when the HTTP request fails.
        /// </summary>
        [Fact]
        public async Task SendVerificationCodeAsync_WithHttpFailure_ShouldReturnFalse()
        {
            // Arrange
            Setup();
            
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent("{\"error\":\"Bad request\"}", Encoding.UTF8, "application/json")
                })
                .Verifiable();

            // Act
            var result = await _smsService.SendVerificationCodeAsync(TestConstants.TestPhoneNumber, TestConstants.TestVerificationCode);

            // Assert
            result.Should().BeFalse();
            
            // Verify HTTP request was made
            _mockHttpMessageHandler
                .Protected()
                .Verify(
                    "SendAsync",
                    Times.Once(),
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>());
            
            // Verify error was logged
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to send SMS")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        /// <summary>
        /// Tests that SendVerificationCodeAsync throws ArgumentNullException when phoneNumber is null.
        /// </summary>
        [Fact]
        public async Task SendVerificationCodeAsync_WithNullPhoneNumber_ShouldThrowArgumentNullException()
        {
            // Arrange
            Setup();

            // Act & Assert
            await TestBase.AssertExceptionAsync<ArgumentNullException>(
                () => _smsService.SendVerificationCodeAsync(null, TestConstants.TestVerificationCode));
        }

        /// <summary>
        /// Tests that SendVerificationCodeAsync throws ArgumentNullException when code is null.
        /// </summary>
        [Fact]
        public async Task SendVerificationCodeAsync_WithNullCode_ShouldThrowArgumentNullException()
        {
            // Arrange
            Setup();

            // Act & Assert
            await TestBase.AssertExceptionAsync<ArgumentNullException>(
                () => _smsService.SendVerificationCodeAsync(TestConstants.TestPhoneNumber, null));
        }
    }
}