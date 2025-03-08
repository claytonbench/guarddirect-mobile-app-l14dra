using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using FluentAssertions;
using SecurityPatrol.ViewModels;
using SecurityPatrol.UnitTests.Helpers.MockServices;
using SecurityPatrol.UnitTests.Helpers;
using SecurityPatrol.Models;
using SecurityPatrol.Constants;

namespace SecurityPatrol.UnitTests.ViewModels
{
    /// <summary>
    /// Test class for the ActivityReportViewModel that verifies its functionality for creating and submitting activity reports
    /// </summary>
    public class ActivityReportViewModelTests
    {
        private readonly ITestOutputHelper _outputHelper;
        private readonly MockNavigationService _navigationService;
        private readonly MockAuthenticationStateProvider _authStateProvider;
        private readonly MockReportService _reportService;
        private readonly MockLocationService _locationService;
        private ActivityReportViewModel _viewModel;

        /// <summary>
        /// Initializes a new instance of the ActivityReportViewModelTests class with test output helper
        /// </summary>
        /// <param name="outputHelper">Output helper for test logging</param>
        public ActivityReportViewModelTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
            InitializeMocks();
            CreateViewModel();
        }

        /// <summary>
        /// Initializes all mock services used in the tests
        /// </summary>
        private void InitializeMocks()
        {
            _navigationService = new MockNavigationService();
            _authStateProvider = new MockAuthenticationStateProvider();
            _reportService = new MockReportService();
            _locationService = new MockLocationService();
            
            // Set up default authenticated state
            var authState = TestDataGenerator.CreateAuthState(true, "+15551234567");
            _authStateProvider.UpdateState(authState);
            
            // Set up default location
            var location = TestDataGenerator.CreateLocationModel();
            _locationService.SetupCurrentLocation(location);
        }

        /// <summary>
        /// Creates a new instance of the ActivityReportViewModel with mock dependencies
        /// </summary>
        private void CreateViewModel()
        {
            _viewModel = new ActivityReportViewModel(
                _navigationService,
                _authStateProvider,
                _reportService,
                _locationService);
        }

        [Fact]
        public async Task Test_Constructor_InitializesProperties()
        {
            // Create a new view model to ensure a fresh state
            CreateViewModel();
            
            // Initialize the view model
            await _viewModel.InitializeAsync();
            
            // Verify property initialization
            _viewModel.ReportText.Should().BeEmpty();
            _viewModel.RemainingCharacters.Should().Be(AppConstants.ReportMaxLength);
            _viewModel.CanSubmit.Should().BeFalse();
            _viewModel.SubmitReportCommand.Should().NotBeNull();
            _viewModel.CancelCommand.Should().NotBeNull();
        }

        [Fact]
        public void Test_ReportTextChanged_UpdatesRemainingCharacters()
        {
            // Arrange
            string testText = "Test report text";
            
            // Act
            _viewModel.ReportText = testText;
            
            // Assert
            _viewModel.RemainingCharacters.Should().Be(AppConstants.ReportMaxLength - testText.Length);
        }

        [Fact]
        public void Test_ReportTextChanged_UpdatesCanSubmit()
        {
            // Arrange & Act - Empty text (invalid)
            _viewModel.ReportText = string.Empty;
            
            // Assert
            _viewModel.CanSubmit.Should().BeFalse();
            
            // Arrange & Act - Valid text
            _viewModel.ReportText = "Valid report text";
            
            // Assert
            _viewModel.CanSubmit.Should().BeTrue();
            
            // Arrange & Act - Text too long (invalid)
            _viewModel.ReportText = new string('A', AppConstants.ReportMaxLength + 1);
            
            // Assert
            _viewModel.CanSubmit.Should().BeFalse();
        }

        [Fact]
        public void Test_ReportTextChanged_SetsErrorMessage()
        {
            // Arrange & Act - Empty text
            _viewModel.ReportText = string.Empty;
            
            // Assert
            _viewModel.ErrorMessage.Should().Be(ErrorMessages.ReportEmpty);
            _viewModel.HasError.Should().BeTrue();
            
            // Arrange & Act - Valid text
            _viewModel.ReportText = "Valid report text";
            
            // Assert
            _viewModel.ErrorMessage.Should().BeNullOrEmpty();
            _viewModel.HasError.Should().BeFalse();
            
            // Arrange & Act - Text too long
            _viewModel.ReportText = new string('A', AppConstants.ReportMaxLength + 1);
            
            // Assert
            _viewModel.ErrorMessage.Should().Be(ErrorMessages.ReportTooLong);
            _viewModel.HasError.Should().BeTrue();
        }

        [Fact]
        public async Task Test_SubmitReportCommand_CreatesAndSyncsReport()
        {
            // Arrange
            var report = TestDataGenerator.CreateReportModel();
            _reportService.SetupCreateReport(true);
            _reportService.SetupSyncReport(true);
            var location = TestDataGenerator.CreateLocationModel();
            _locationService.SetupCurrentLocation(location);
            _viewModel.ReportText = "Test report text";
            
            // Act
            await _viewModel.SubmitReportCommand.ExecuteAsync(null);
            
            // Assert
            _locationService.VerifyGetCurrentLocationCalled().Should().BeTrue();
            _navigationService.NavigateToAsyncCalled.Should().BeTrue();
            _navigationService.LastNavigatedRoute.Should().Be(NavigationConstants.ReportListPage);
        }

        [Fact]
        public async Task Test_SubmitReportCommand_HandlesLocationServiceFailure()
        {
            // Arrange
            var report = TestDataGenerator.CreateReportModel();
            _reportService.SetupCreateReport(true);
            _reportService.SetupSyncReport(true);
            _locationService.SetupException(new Exception("Location service error"));
            _viewModel.ReportText = "Test report text";
            
            // Act
            await _viewModel.SubmitReportCommand.ExecuteAsync(null);
            
            // Assert
            _locationService.VerifyGetCurrentLocationCalled().Should().BeTrue();
            _navigationService.NavigateToAsyncCalled.Should().BeTrue();
            _navigationService.LastNavigatedRoute.Should().Be(NavigationConstants.ReportListPage);
        }

        [Fact]
        public async Task Test_SubmitReportCommand_HandlesReportCreationFailure()
        {
            // Arrange
            _reportService.SetupCreateReportException(new Exception("Report creation failed"));
            var location = TestDataGenerator.CreateLocationModel();
            _locationService.SetupCurrentLocation(location);
            _viewModel.ReportText = "Test report text";
            
            // Act
            await _viewModel.SubmitReportCommand.ExecuteAsync(null);
            
            // Assert
            _locationService.VerifyGetCurrentLocationCalled().Should().BeTrue();
            _navigationService.NavigateToAsyncCalled.Should().BeFalse();
            _viewModel.ErrorMessage.Should().Be(ErrorMessages.ReportSubmissionFailed);
            _viewModel.HasError.Should().BeTrue();
        }

        [Fact]
        public async Task Test_SubmitReportCommand_DoesNotSubmitInvalidReport()
        {
            // Arrange & Act - Empty text
            _viewModel.ReportText = string.Empty;
            await _viewModel.SubmitReportCommand.ExecuteAsync(null);
            
            // Assert
            _locationService.VerifyGetCurrentLocationCalled().Should().BeFalse();
            _viewModel.ErrorMessage.Should().Be(ErrorMessages.ReportEmpty);
            _viewModel.HasError.Should().BeTrue();
            
            // Arrange & Act - Text too long
            _viewModel.ReportText = new string('A', AppConstants.ReportMaxLength + 1);
            await _viewModel.SubmitReportCommand.ExecuteAsync(null);
            
            // Assert
            _locationService.VerifyGetCurrentLocationCalled().Should().BeFalse();
            _viewModel.ErrorMessage.Should().Be(ErrorMessages.ReportTooLong);
            _viewModel.HasError.Should().BeTrue();
        }

        [Fact]
        public async Task Test_CancelCommand_NavigatesBack()
        {
            // Act
            await _viewModel.CancelCommand.ExecuteAsync(null);
            
            // Assert
            _navigationService.NavigateBackAsyncCalled.Should().BeTrue();
        }

        [Fact]
        public async Task Test_InitializeAsync_ResetsForm()
        {
            // Arrange
            _viewModel.ReportText = "Test report text";
            
            // Act
            await _viewModel.InitializeAsync();
            
            // Assert
            _viewModel.ReportText.Should().BeEmpty();
            _viewModel.RemainingCharacters.Should().Be(AppConstants.ReportMaxLength);
            _viewModel.CanSubmit.Should().BeFalse();
            _viewModel.ErrorMessage.Should().BeNullOrEmpty();
            _viewModel.HasError.Should().BeFalse();
        }
    }
}