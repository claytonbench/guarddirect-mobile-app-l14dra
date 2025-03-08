using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using Xunit;
using Moq;
using FluentAssertions;
using AutoFixture;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SQLite;
using SecurityPatrol.TestCommon.Constants;
using SecurityPatrol.TestCommon.Data;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.Services;
using SecurityPatrol.Models;

namespace SecurityPatrol.MAUI.UnitTests.Setup
{
    /// <summary>
    /// Base class for all unit tests in the Security Patrol MAUI application, providing common setup and utility methods.
    /// </summary>
    public class TestBase
    {
        // Mock services
        protected Mock<IAuthenticationService> MockAuthService { get; private set; }
        protected Mock<IApiService> MockApiService { get; private set; }
        protected Mock<IDatabaseService> MockDatabaseService { get; private set; }
        protected Mock<ILocationService> MockLocationService { get; private set; }
        protected Mock<ITimeTrackingService> MockTimeTrackingService { get; private set; }
        protected Mock<IPatrolService> MockPatrolService { get; private set; }
        protected Mock<IPhotoService> MockPhotoService { get; private set; }
        protected Mock<IReportService> MockReportService { get; private set; }
        protected Mock<ISyncService> MockSyncService { get; private set; }
        protected Mock<INavigationService> MockNavigationService { get; private set; }
        protected Mock<ITokenManager> MockTokenManager { get; private set; }
        protected Mock<ISettingsService> MockSettingsService { get; private set; }
        
        // Service provider for dependency injection
        protected IServiceProvider ServiceProvider { get; private set; }
        
        // Service collection for registering services
        protected IServiceCollection Services { get; private set; }
        
        // AutoFixture for generating test data
        protected Fixture Fixture { get; private set; }
        
        /// <summary>
        /// Initializes a new instance of the TestBase class with common test setup
        /// </summary>
        public TestBase()
        {
            // Initialize AutoFixture for test data generation
            Fixture = new Fixture();
            
            // Initialize mock services
            MockAuthService = new Mock<IAuthenticationService>();
            MockApiService = new Mock<IApiService>();
            MockDatabaseService = new Mock<IDatabaseService>();
            MockLocationService = new Mock<ILocationService>();
            MockTimeTrackingService = new Mock<ITimeTrackingService>();
            MockPatrolService = new Mock<IPatrolService>();
            MockPhotoService = new Mock<IPhotoService>();
            MockReportService = new Mock<IReportService>();
            MockSyncService = new Mock<ISyncService>();
            MockNavigationService = new Mock<INavigationService>();
            MockTokenManager = new Mock<ITokenManager>();
            MockSettingsService = new Mock<ISettingsService>();
            
            // Setup common mock behaviors
            SetupMockAuthService();
            SetupMockApiService();
            SetupMockDatabaseService();
            
            // Setup service collection and provider
            SetupServiceCollection();
        }
        
        /// <summary>
        /// Sets up the service collection with mock services for dependency injection
        /// </summary>
        protected virtual void SetupServiceCollection()
        {
            Services = new ServiceCollection();
            
            // Register mock services
            Services.AddSingleton(MockAuthService.Object);
            Services.AddSingleton(MockApiService.Object);
            Services.AddSingleton(MockDatabaseService.Object);
            Services.AddSingleton(MockLocationService.Object);
            Services.AddSingleton(MockTimeTrackingService.Object);
            Services.AddSingleton(MockPatrolService.Object);
            Services.AddSingleton(MockPhotoService.Object);
            Services.AddSingleton(MockReportService.Object);
            Services.AddSingleton(MockSyncService.Object);
            Services.AddSingleton(MockNavigationService.Object);
            Services.AddSingleton(MockTokenManager.Object);
            Services.AddSingleton(MockSettingsService.Object);
            
            // Build service provider
            ServiceProvider = Services.BuildServiceProvider();
        }
        
        /// <summary>
        /// Configures the mock authentication service with default behaviors
        /// </summary>
        protected virtual void SetupMockAuthService()
        {
            // Setup default behaviors for authentication service
            MockAuthService.Setup(x => x.RequestVerificationCode(It.IsAny<string>()))
                .ReturnsAsync(true);
            
            MockAuthService.Setup(x => x.VerifyCode(It.IsAny<string>()))
                .ReturnsAsync(true);
            
            MockAuthService.Setup(x => x.GetAuthenticationState())
                .ReturnsAsync(AuthState.CreateAuthenticated(TestConstants.TestPhoneNumber));
            
            MockAuthService.Setup(x => x.Logout())
                .Returns(Task.CompletedTask);
            
            MockAuthService.Setup(x => x.RefreshToken())
                .ReturnsAsync(true);
        }
        
        /// <summary>
        /// Configures the mock API service with default behaviors
        /// </summary>
        protected virtual void SetupMockApiService()
        {
            // Setup default behaviors for API service
            MockApiService.Setup(x => x.GetAsync<It.IsAnyType>(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<bool>()))
                .ReturnsAsync(It.IsAny<object>());
            
            MockApiService.Setup(x => x.PostAsync<It.IsAnyType>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .ReturnsAsync(It.IsAny<object>());
            
            MockApiService.Setup(x => x.PostMultipartAsync<It.IsAnyType>(It.IsAny<string>(), It.IsAny<MultipartFormDataContent>(), It.IsAny<bool>()))
                .ReturnsAsync(It.IsAny<object>());
            
            MockApiService.Setup(x => x.PutAsync<It.IsAnyType>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .ReturnsAsync(It.IsAny<object>());
            
            MockApiService.Setup(x => x.DeleteAsync<It.IsAnyType>(It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(It.IsAny<object>());
        }
        
        /// <summary>
        /// Configures the mock database service with default behaviors
        /// </summary>
        protected virtual void SetupMockDatabaseService()
        {
            // Setup default behaviors for database service
            MockDatabaseService.Setup(x => x.InitializeAsync())
                .Returns(Task.CompletedTask);
            
            MockDatabaseService.Setup(x => x.GetConnection())
                .Returns(new SQLiteAsyncConnection(":memory:"));
        }
        
        /// <summary>
        /// Creates an authenticated AuthState with the specified phone number
        /// </summary>
        /// <param name="phoneNumber">The phone number to use (defaults to test constant)</param>
        /// <returns>An authenticated AuthState instance</returns>
        protected AuthState CreateAuthenticatedState(string phoneNumber = null)
        {
            return AuthState.CreateAuthenticated(phoneNumber ?? TestConstants.TestPhoneNumber);
        }
        
        /// <summary>
        /// Creates an unauthenticated AuthState
        /// </summary>
        /// <returns>An unauthenticated AuthState instance</returns>
        protected AuthState CreateUnauthenticatedState()
        {
            return AuthState.CreateUnauthenticated();
        }
        
        /// <summary>
        /// Configures the mock authentication service to return a specific authentication state
        /// </summary>
        /// <param name="isAuthenticated">Whether to set the state as authenticated</param>
        protected void SetupMockAuthenticationState(bool isAuthenticated)
        {
            var authState = isAuthenticated 
                ? CreateAuthenticatedState() 
                : CreateUnauthenticatedState();
                
            MockAuthService.Setup(x => x.GetAuthenticationState())
                .ReturnsAsync(authState);
        }
        
        /// <summary>
        /// Configures the mock authentication service to simulate authentication failures
        /// </summary>
        protected void SetupMockAuthenticationFailure()
        {
            MockAuthService.Setup(x => x.RequestVerificationCode(It.IsAny<string>()))
                .ReturnsAsync(false);
                
            MockAuthService.Setup(x => x.VerifyCode(It.IsAny<string>()))
                .ReturnsAsync(false);
        }
        
        /// <summary>
        /// Configures the mock authentication service to throw exceptions
        /// </summary>
        protected void SetupMockAuthenticationException()
        {
            MockAuthService.Setup(x => x.RequestVerificationCode(It.IsAny<string>()))
                .ThrowsAsync(new Exception("Test exception"));
                
            MockAuthService.Setup(x => x.VerifyCode(It.IsAny<string>()))
                .ThrowsAsync(new Exception("Test exception"));
                
            MockAuthService.Setup(x => x.GetAuthenticationState())
                .ThrowsAsync(new Exception("Test exception"));
        }
        
        /// <summary>
        /// Configures the mock API service to throw exceptions
        /// </summary>
        /// <param name="method">The HTTP method to setup exceptions for</param>
        protected void SetupMockApiException(string method)
        {
            switch (method.ToUpper())
            {
                case "GET":
                    MockApiService.Setup(x => x.GetAsync<It.IsAnyType>(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<bool>()))
                        .ThrowsAsync(new Exception("Test API exception"));
                    break;
                    
                case "POST":
                    MockApiService.Setup(x => x.PostAsync<It.IsAnyType>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                        .ThrowsAsync(new Exception("Test API exception"));
                    break;
                    
                case "PUT":
                    MockApiService.Setup(x => x.PutAsync<It.IsAnyType>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                        .ThrowsAsync(new Exception("Test API exception"));
                    break;
                    
                case "DELETE":
                    MockApiService.Setup(x => x.DeleteAsync<It.IsAnyType>(It.IsAny<string>(), It.IsAny<bool>()))
                        .ThrowsAsync(new Exception("Test API exception"));
                    break;
                    
                default:
                    throw new ArgumentException($"Unknown API method: {method}");
            }
        }
        
        /// <summary>
        /// Performs cleanup after test execution
        /// </summary>
        protected virtual void Cleanup()
        {
            // Reset all mocks
            MockAuthService.Reset();
            MockApiService.Reset();
            MockDatabaseService.Reset();
            MockLocationService.Reset();
            MockTimeTrackingService.Reset();
            MockPatrolService.Reset();
            MockPhotoService.Reset();
            MockReportService.Reset();
            MockSyncService.Reset();
            MockNavigationService.Reset();
            MockTokenManager.Reset();
            MockSettingsService.Reset();
        }
    }
}