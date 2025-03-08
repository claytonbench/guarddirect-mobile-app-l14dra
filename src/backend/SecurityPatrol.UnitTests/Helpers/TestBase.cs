using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using SecurityPatrol.Core.Interfaces;

namespace SecurityPatrol.UnitTests.Helpers
{
    /// <summary>
    /// Base class for unit tests providing common setup, mock repositories, mock services, and utility methods
    /// to simplify test implementation and ensure consistency across test classes.
    /// </summary>
    public abstract class TestBase
    {
        // Mock repositories
        protected Mock<IUserRepository> MockUserRepository { get; }
        protected Mock<ITimeRecordRepository> MockTimeRecordRepository { get; }
        protected Mock<ILocationRecordRepository> MockLocationRecordRepository { get; }
        protected Mock<ICheckpointRepository> MockCheckpointRepository { get; }
        protected Mock<ICheckpointVerificationRepository> MockCheckpointVerificationRepository { get; }
        protected Mock<IPatrolLocationRepository> MockPatrolLocationRepository { get; }
        protected Mock<IPhotoRepository> MockPhotoRepository { get; }
        protected Mock<IReportRepository> MockReportRepository { get; }

        // Mock services
        protected Mock<IAuthenticationService> MockAuthenticationService { get; }
        protected Mock<ILocationService> MockLocationService { get; }
        protected Mock<ITimeRecordService> MockTimeRecordService { get; }
        protected Mock<IPatrolService> MockPatrolService { get; }
        protected Mock<IPhotoService> MockPhotoService { get; }
        protected Mock<IReportService> MockReportService { get; }
        protected Mock<ISmsService> MockSmsService { get; }
        protected Mock<ITokenService> MockTokenService { get; }
        protected Mock<IVerificationCodeService> MockVerificationCodeService { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestBase"/> class with
        /// initialized mock repositories and services for unit testing.
        /// </summary>
        protected TestBase()
        {
            // Initialize all mock repositories
            MockUserRepository = MockRepositories.CreateMockUserRepository();
            MockTimeRecordRepository = MockRepositories.CreateMockTimeRecordRepository();
            MockLocationRecordRepository = MockRepositories.CreateMockLocationRecordRepository();
            MockCheckpointRepository = MockRepositories.CreateMockCheckpointRepository();
            MockCheckpointVerificationRepository = MockRepositories.CreateMockCheckpointVerificationRepository();
            MockPatrolLocationRepository = MockRepositories.CreateMockPatrolLocationRepository();
            MockPhotoRepository = MockRepositories.CreateMockPhotoRepository();
            MockReportRepository = MockRepositories.CreateMockReportRepository();

            // Initialize all mock services
            MockAuthenticationService = MockServices.CreateMockAuthenticationService();
            MockLocationService = MockServices.CreateMockLocationService();
            MockTimeRecordService = MockServices.CreateMockTimeRecordService();
            MockPatrolService = MockServices.CreateMockPatrolService();
            MockPhotoService = MockServices.CreateMockPhotoService();
            MockReportService = MockServices.CreateMockReportService();
            MockSmsService = MockServices.CreateMockSmsService();
            MockTokenService = MockServices.CreateMockTokenService();
            MockVerificationCodeService = MockServices.CreateMockVerificationCodeService();
        }

        /// <summary>
        /// Resets all mock objects to their initial state, clearing all setups and invocations.
        /// Call this method between tests if you need to reset the mock state.
        /// </summary>
        protected void ResetMocks()
        {
            // Reset repository mocks
            MockUserRepository.Reset();
            MockTimeRecordRepository.Reset();
            MockLocationRecordRepository.Reset();
            MockCheckpointRepository.Reset();
            MockCheckpointVerificationRepository.Reset();
            MockPatrolLocationRepository.Reset();
            MockPhotoRepository.Reset();
            MockReportRepository.Reset();

            // Reset service mocks
            MockAuthenticationService.Reset();
            MockLocationService.Reset();
            MockTimeRecordService.Reset();
            MockPatrolService.Reset();
            MockPhotoService.Reset();
            MockReportService.Reset();
            MockSmsService.Reset();
            MockTokenService.Reset();
            MockVerificationCodeService.Reset();
        }

        /// <summary>
        /// Asserts that the specified asynchronous function throws an exception of type TException.
        /// </summary>
        /// <typeparam name="TException">The type of exception expected to be thrown.</typeparam>
        /// <param name="testCode">The asynchronous function to test.</param>
        /// <returns>The thrown exception if it matches the expected type, allowing for additional assertions on the exception.</returns>
        protected async Task<TException> AssertExceptionAsync<TException>(Func<Task> testCode) where TException : Exception
        {
            try
            {
                await testCode();
                Assert.True(false, $"Expected {typeof(TException).Name} but no exception was thrown.");
                return null; // This line will never be reached, but is needed for compilation
            }
            catch (Exception ex)
            {
                if (ex is TException typedException)
                {
                    return typedException;
                }

                Assert.True(false, $"Expected {typeof(TException).Name} but got {ex.GetType().Name}: {ex.Message}");
                return null; // This line will never be reached, but is needed for compilation
            }
        }

        /// <summary>
        /// Creates a mock ILogger for testing components that require logging.
        /// </summary>
        /// <typeparam name="T">The type associated with the logger.</typeparam>
        /// <returns>A configured mock logger.</returns>
        protected Mock<ILogger<T>> CreateMockLogger<T>()
        {
            var mockLogger = new Mock<ILogger<T>>();
            
            // Set up basic logging methods
            mockLogger.Setup(
                m => m.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()
                )
            );

            return mockLogger;
        }
    }
}