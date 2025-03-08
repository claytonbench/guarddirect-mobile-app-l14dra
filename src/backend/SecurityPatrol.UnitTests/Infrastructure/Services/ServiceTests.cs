using System;
using System.IO;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SecurityPatrol.Core.Constants;
using SecurityPatrol.Core.Models;
using SecurityPatrol.UnitTests.Helpers;
using Xunit;

namespace SecurityPatrol.UnitTests.Infrastructure.Services
{
    public class CurrentUserServiceTests
    {
        [Fact]
        public void GetUserId_WithAuthenticatedUser_ReturnsUserId()
        {
            // Arrange
            var userId = "user123";
            var claims = new[] { new Claim(ClaimTypes.UserId, userId) };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);
            
            var httpContext = new DefaultHttpContext { User = user };
            
            var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
            
            var service = new Infrastructure.Services.CurrentUserService(httpContextAccessorMock.Object);
            
            // Act
            var result = service.GetUserId();
            
            // Assert
            result.Should().Be(userId);
        }
        
        [Fact]
        public void GetUserId_WithUnauthenticatedUser_ReturnsNull()
        {
            // Arrange
            var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            httpContextAccessorMock.Setup(x => x.HttpContext).Returns(new DefaultHttpContext());
            
            var service = new Infrastructure.Services.CurrentUserService(httpContextAccessorMock.Object);
            
            // Act
            var result = service.GetUserId();
            
            // Assert
            result.Should().BeNull();
        }
        
        [Fact]
        public void GetPhoneNumber_WithAuthenticatedUser_ReturnsPhoneNumber()
        {
            // Arrange
            var phoneNumber = "+15551234567";
            var claims = new[] { new Claim(ClaimTypes.PhoneNumber, phoneNumber) };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);
            
            var httpContext = new DefaultHttpContext { User = user };
            
            var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
            
            var service = new Infrastructure.Services.CurrentUserService(httpContextAccessorMock.Object);
            
            // Act
            var result = service.GetPhoneNumber();
            
            // Assert
            result.Should().Be(phoneNumber);
        }
        
        [Fact]
        public void GetPhoneNumber_WithUnauthenticatedUser_ReturnsNull()
        {
            // Arrange
            var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            httpContextAccessorMock.Setup(x => x.HttpContext).Returns(new DefaultHttpContext());
            
            var service = new Infrastructure.Services.CurrentUserService(httpContextAccessorMock.Object);
            
            // Act
            var result = service.GetPhoneNumber();
            
            // Assert
            result.Should().BeNull();
        }
        
        [Fact]
        public void IsAuthenticated_WithAuthenticatedUser_ReturnsTrue()
        {
            // Arrange
            var identity = new ClaimsIdentity(new Claim[] { }, "TestAuth");
            var user = new ClaimsPrincipal(identity);
            
            var httpContext = new DefaultHttpContext { User = user };
            
            var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
            
            var service = new Infrastructure.Services.CurrentUserService(httpContextAccessorMock.Object);
            
            // Act
            var result = service.IsAuthenticated();
            
            // Assert
            result.Should().BeTrue();
        }
        
        [Fact]
        public void IsAuthenticated_WithUnauthenticatedUser_ReturnsFalse()
        {
            // Arrange
            var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            httpContextAccessorMock.Setup(x => x.HttpContext).Returns(new DefaultHttpContext());
            
            var service = new Infrastructure.Services.CurrentUserService(httpContextAccessorMock.Object);
            
            // Act
            var result = service.IsAuthenticated();
            
            // Assert
            result.Should().BeFalse();
        }
        
        [Fact]
        public void GetLastAuthenticated_WithAuthenticatedUser_ReturnsDateTime()
        {
            // Arrange
            var lastAuthTime = DateTime.UtcNow.AddMinutes(-5);
            var lastAuthStr = lastAuthTime.ToString("o"); // ISO 8601 format
            
            var claims = new[] { new Claim("auth_time", lastAuthStr) };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);
            
            var httpContext = new DefaultHttpContext { User = user };
            
            var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
            
            var service = new Infrastructure.Services.CurrentUserService(httpContextAccessorMock.Object);
            
            // Act
            var result = service.GetLastAuthenticated();
            
            // Assert
            result.Should().BeCloseTo(lastAuthTime, TimeSpan.FromSeconds(1));
        }
        
        [Fact]
        public void GetLastAuthenticated_WithUnauthenticatedUser_ReturnsNull()
        {
            // Arrange
            var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            httpContextAccessorMock.Setup(x => x.HttpContext).Returns(new DefaultHttpContext());
            
            var service = new Infrastructure.Services.CurrentUserService(httpContextAccessorMock.Object);
            
            // Act
            var result = service.GetLastAuthenticated();
            
            // Assert
            result.Should().BeNull();
        }
    }
    
    public class DateTimeServiceTests
    {
        [Fact]
        public void Now_ReturnsCurrentLocalDateTime()
        {
            // Arrange
            var service = new Infrastructure.Services.DateTimeService();
            
            // Act
            var result = service.Now;
            
            // Assert
            result.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }
        
        [Fact]
        public void UtcNow_ReturnsCurrentUtcDateTime()
        {
            // Arrange
            var service = new Infrastructure.Services.DateTimeService();
            
            // Act
            var result = service.UtcNow;
            
            // Assert
            result.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }
        
        [Fact]
        public void Today_ReturnsCurrentDateWithTimeComponentSetToMidnight()
        {
            // Arrange
            var service = new Infrastructure.Services.DateTimeService();
            
            // Act
            var result = service.Today;
            
            // Assert
            result.Date.Should().Be(DateTime.Today);
            result.Hour.Should().Be(0);
            result.Minute.Should().Be(0);
            result.Second.Should().Be(0);
            result.Millisecond.Should().Be(0);
        }
    }
    
    public class SmsServiceTests : TestBase
    {
        [Fact]
        public async Task SendSmsAsync_WithValidParameters_ReturnsTrue()
        {
            // Arrange
            var phoneNumber = "+15551234567";
            var message = "Test message";
            
            var smsOptions = new Infrastructure.Services.SmsOptions
            {
                ApiKey = "test-api-key",
                ApiUrl = "https://api.test.com/sms",
                FromNumber = "+15559876543"
            };
            
            var optionsMock = new Mock<IOptions<Infrastructure.Services.SmsOptions>>();
            optionsMock.Setup(x => x.Value).Returns(smsOptions);
            
            var mockLogger = CreateMockLogger<Infrastructure.Services.SmsService>();
            
            // Create a mock HttpClient handler that returns a successful response
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent("{\"success\":true}", Encoding.UTF8, "application/json")
                });
            
            var httpClient = new HttpClient(mockHandler.Object);
            
            var service = new Infrastructure.Services.SmsService(optionsMock.Object, mockLogger.Object, httpClient);
            
            // Act
            var result = await service.SendSmsAsync(phoneNumber, message);
            
            // Assert
            result.Should().BeTrue();
            mockHandler.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Post && 
                    req.RequestUri.ToString() == smsOptions.ApiUrl),
                ItExpr.IsAny<CancellationToken>());
        }
        
        [Fact]
        public async Task SendSmsAsync_WithApiFailure_ReturnsFalse()
        {
            // Arrange
            var phoneNumber = "+15551234567";
            var message = "Test message";
            
            var smsOptions = new Infrastructure.Services.SmsOptions
            {
                ApiKey = "test-api-key",
                ApiUrl = "https://api.test.com/sms",
                FromNumber = "+15559876543"
            };
            
            var optionsMock = new Mock<IOptions<Infrastructure.Services.SmsOptions>>();
            optionsMock.Setup(x => x.Value).Returns(smsOptions);
            
            var mockLogger = CreateMockLogger<Infrastructure.Services.SmsService>();
            
            // Create a mock HttpClient handler that returns an error response
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.BadRequest,
                    Content = new StringContent("{\"error\":\"Invalid number\"}", Encoding.UTF8, "application/json")
                });
            
            var httpClient = new HttpClient(mockHandler.Object);
            
            var service = new Infrastructure.Services.SmsService(optionsMock.Object, mockLogger.Object, httpClient);
            
            // Act
            var result = await service.SendSmsAsync(phoneNumber, message);
            
            // Assert
            result.Should().BeFalse();
            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to send SMS")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
        
        [Fact]
        public async Task SendSmsAsync_WithException_ReturnsFalse()
        {
            // Arrange
            var phoneNumber = "+15551234567";
            var message = "Test message";
            
            var smsOptions = new Infrastructure.Services.SmsOptions
            {
                ApiKey = "test-api-key",
                ApiUrl = "https://api.test.com/sms",
                FromNumber = "+15559876543"
            };
            
            var optionsMock = new Mock<IOptions<Infrastructure.Services.SmsOptions>>();
            optionsMock.Setup(x => x.Value).Returns(smsOptions);
            
            var mockLogger = CreateMockLogger<Infrastructure.Services.SmsService>();
            
            // Create a mock HttpClient handler that throws an exception
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Connection error"));
            
            var httpClient = new HttpClient(mockHandler.Object);
            
            var service = new Infrastructure.Services.SmsService(optionsMock.Object, mockLogger.Object, httpClient);
            
            // Act
            var result = await service.SendSmsAsync(phoneNumber, message);
            
            // Assert
            result.Should().BeFalse();
            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Exception when sending SMS")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
        
        [Fact]
        public async Task SendVerificationCodeAsync_WithValidParameters_ReturnsTrue()
        {
            // Arrange
            var phoneNumber = "+15551234567";
            var code = "123456";
            
            var smsOptions = new Infrastructure.Services.SmsOptions
            {
                ApiKey = "test-api-key",
                ApiUrl = "https://api.test.com/sms",
                FromNumber = "+15559876543",
                VerificationMessageTemplate = "Your verification code is: {0}"
            };
            
            var optionsMock = new Mock<IOptions<Infrastructure.Services.SmsOptions>>();
            optionsMock.Setup(x => x.Value).Returns(smsOptions);
            
            var mockLogger = CreateMockLogger<Infrastructure.Services.SmsService>();
            
            // Create a mock HttpClient handler that returns a successful response
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent("{\"success\":true}", Encoding.UTF8, "application/json")
                });
            
            var httpClient = new HttpClient(mockHandler.Object);
            
            var service = new Infrastructure.Services.SmsService(optionsMock.Object, mockLogger.Object, httpClient);
            
            // Act
            var result = await service.SendVerificationCodeAsync(phoneNumber, code);
            
            // Assert
            result.Should().BeTrue();
            mockHandler.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Post && 
                    req.RequestUri.ToString() == smsOptions.ApiUrl),
                ItExpr.IsAny<CancellationToken>());
        }
        
        [Fact]
        public async Task SendVerificationCodeAsync_WithInvalidParameters_ThrowsArgumentException()
        {
            // Arrange
            var smsOptions = new Infrastructure.Services.SmsOptions
            {
                ApiKey = "test-api-key",
                ApiUrl = "https://api.test.com/sms",
                FromNumber = "+15559876543",
                VerificationMessageTemplate = "Your verification code is: {0}"
            };
            
            var optionsMock = new Mock<IOptions<Infrastructure.Services.SmsOptions>>();
            optionsMock.Setup(x => x.Value).Returns(smsOptions);
            
            var mockLogger = CreateMockLogger<Infrastructure.Services.SmsService>();
            var httpClient = new HttpClient();
            
            var service = new Infrastructure.Services.SmsService(optionsMock.Object, mockLogger.Object, httpClient);
            
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => service.SendVerificationCodeAsync(null, "123456"));
            await Assert.ThrowsAsync<ArgumentException>(() => service.SendVerificationCodeAsync("", "123456"));
            await Assert.ThrowsAsync<ArgumentException>(() => service.SendVerificationCodeAsync("+15551234567", null));
            await Assert.ThrowsAsync<ArgumentException>(() => service.SendVerificationCodeAsync("+15551234567", ""));
        }
    }
    
    public class StorageServiceTests : TestBase
    {
        [Fact]
        public async Task StoreFileAsync_WithValidParameters_ReturnsSuccessResult()
        {
            // Arrange
            var fileName = "test-file.jpg";
            var contentType = "image/jpeg";
            var testStorage = Path.Combine(Path.GetTempPath(), "security-patrol-test");
            Directory.CreateDirectory(testStorage); // Ensure directory exists
            
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["Storage:Path"]).Returns(testStorage);
            
            var mockLogger = CreateMockLogger<Infrastructure.Services.StorageService>();
            
            var service = new Infrastructure.Services.StorageService(configMock.Object, mockLogger.Object);
            
            var fileContent = Encoding.UTF8.GetBytes("test file content");
            var fileStream = new MemoryStream(fileContent);
            
            try
            {
                // Act
                var result = await service.StoreFileAsync(fileStream, fileName, contentType);
                
                // Assert
                result.Succeeded.Should().BeTrue();
                var filePath = result.Data as string;
                filePath.Should().NotBeNullOrEmpty();
                File.Exists(filePath).Should().BeTrue();
                var storedContent = await File.ReadAllBytesAsync(filePath);
                storedContent.Should().BeEquivalentTo(fileContent);
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(testStorage))
                {
                    Directory.Delete(testStorage, true);
                }
            }
        }
        
        [Fact]
        public async Task StoreFileAsync_WithInvalidParameters_ReturnsFailureResult()
        {
            // Arrange
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["Storage:Path"]).Returns(Path.Combine(Path.GetTempPath(), "security-patrol-test"));
            
            var mockLogger = CreateMockLogger<Infrastructure.Services.StorageService>();
            
            var service = new Infrastructure.Services.StorageService(configMock.Object, mockLogger.Object);
            
            // Act & Assert
            var result1 = await service.StoreFileAsync(null, "test.jpg", "image/jpeg");
            result1.Succeeded.Should().BeFalse();
            
            var result2 = await service.StoreFileAsync(new MemoryStream(), null, "image/jpeg");
            result2.Succeeded.Should().BeFalse();
            
            var result3 = await service.StoreFileAsync(new MemoryStream(), "", "image/jpeg");
            result3.Succeeded.Should().BeFalse();
        }
        
        [Fact]
        public async Task GetFileAsync_WithExistingFile_ReturnsSuccessResult()
        {
            // Arrange
            var fileName = "test-file.jpg";
            var testStorage = Path.Combine(Path.GetTempPath(), "security-patrol-test");
            Directory.CreateDirectory(testStorage); // Ensure directory exists
            
            var filePath = Path.Combine(testStorage, fileName);
            var fileContent = Encoding.UTF8.GetBytes("test file content");
            await File.WriteAllBytesAsync(filePath, fileContent);
            
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["Storage:Path"]).Returns(testStorage);
            
            var mockLogger = CreateMockLogger<Infrastructure.Services.StorageService>();
            
            var service = new Infrastructure.Services.StorageService(configMock.Object, mockLogger.Object);
            
            try
            {
                // Act
                var result = await service.GetFileAsync(filePath);
                
                // Assert
                result.Succeeded.Should().BeTrue();
                var stream = result.Data as Stream;
                stream.Should().NotBeNull();
                
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                memoryStream.ToArray().Should().BeEquivalentTo(fileContent);
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(testStorage))
                {
                    Directory.Delete(testStorage, true);
                }
            }
        }
        
        [Fact]
        public async Task GetFileAsync_WithNonExistingFile_ReturnsFailureResult()
        {
            // Arrange
            var nonExistingFilePath = Path.Combine(Path.GetTempPath(), "non-existing-file.jpg");
            
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["Storage:Path"]).Returns(Path.GetTempPath());
            
            var mockLogger = CreateMockLogger<Infrastructure.Services.StorageService>();
            
            var service = new Infrastructure.Services.StorageService(configMock.Object, mockLogger.Object);
            
            // Act
            var result = await service.GetFileAsync(nonExistingFilePath);
            
            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("File not found");
        }
        
        [Fact]
        public async Task DeleteFileAsync_WithExistingFile_ReturnsSuccessResult()
        {
            // Arrange
            var fileName = "test-file.jpg";
            var testStorage = Path.Combine(Path.GetTempPath(), "security-patrol-test");
            Directory.CreateDirectory(testStorage); // Ensure directory exists
            
            var filePath = Path.Combine(testStorage, fileName);
            var fileContent = Encoding.UTF8.GetBytes("test file content");
            await File.WriteAllBytesAsync(filePath, fileContent);
            
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["Storage:Path"]).Returns(testStorage);
            
            var mockLogger = CreateMockLogger<Infrastructure.Services.StorageService>();
            
            var service = new Infrastructure.Services.StorageService(configMock.Object, mockLogger.Object);
            
            try
            {
                // Act
                var result = await service.DeleteFileAsync(filePath);
                
                // Assert
                result.Succeeded.Should().BeTrue();
                File.Exists(filePath).Should().BeFalse();
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(testStorage))
                {
                    Directory.Delete(testStorage, true);
                }
            }
        }
        
        [Fact]
        public async Task DeleteFileAsync_WithNonExistingFile_ReturnsFailureResult()
        {
            // Arrange
            var nonExistingFilePath = Path.Combine(Path.GetTempPath(), "non-existing-file.jpg");
            
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["Storage:Path"]).Returns(Path.GetTempPath());
            
            var mockLogger = CreateMockLogger<Infrastructure.Services.StorageService>();
            
            var service = new Infrastructure.Services.StorageService(configMock.Object, mockLogger.Object);
            
            // Act
            var result = await service.DeleteFileAsync(nonExistingFilePath);
            
            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("File not found");
        }
        
        [Fact]
        public async Task FileExistsAsync_WithExistingFile_ReturnsTrue()
        {
            // Arrange
            var fileName = "test-file.jpg";
            var testStorage = Path.Combine(Path.GetTempPath(), "security-patrol-test");
            Directory.CreateDirectory(testStorage); // Ensure directory exists
            
            var filePath = Path.Combine(testStorage, fileName);
            var fileContent = Encoding.UTF8.GetBytes("test file content");
            await File.WriteAllBytesAsync(filePath, fileContent);
            
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["Storage:Path"]).Returns(testStorage);
            
            var mockLogger = CreateMockLogger<Infrastructure.Services.StorageService>();
            
            var service = new Infrastructure.Services.StorageService(configMock.Object, mockLogger.Object);
            
            try
            {
                // Act
                var result = await service.FileExistsAsync(filePath);
                
                // Assert
                result.Should().BeTrue();
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(testStorage))
                {
                    Directory.Delete(testStorage, true);
                }
            }
        }
        
        [Fact]
        public async Task FileExistsAsync_WithNonExistingFile_ReturnsFalse()
        {
            // Arrange
            var nonExistingFilePath = Path.Combine(Path.GetTempPath(), "non-existing-file.jpg");
            
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["Storage:Path"]).Returns(Path.GetTempPath());
            
            var mockLogger = CreateMockLogger<Infrastructure.Services.StorageService>();
            
            var service = new Infrastructure.Services.StorageService(configMock.Object, mockLogger.Object);
            
            // Act
            var result = await service.FileExistsAsync(nonExistingFilePath);
            
            // Assert
            result.Should().BeFalse();
        }
    }
}