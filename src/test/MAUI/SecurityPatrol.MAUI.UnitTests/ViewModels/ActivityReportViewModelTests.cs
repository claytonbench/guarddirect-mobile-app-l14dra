using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using SecurityPatrol.MAUI.UnitTests.Setup;
using SecurityPatrol.ViewModels;
using SecurityPatrol.Services;
using SecurityPatrol.Models;
using SecurityPatrol.Constants;

namespace SecurityPatrol.MAUI.UnitTests.ViewModels
{
    public class ActivityReportViewModelTests : TestBase
    {
        private Mock<IAuthenticationStateProvider> MockAuthStateProvider;
        private ActivityReportViewModel ViewModel;

        public ActivityReportViewModelTests()
        {
            // Call base constructor to initialize TestBase
            MockAuthStateProvider = new Mock<IAuthenticationStateProvider>();
            
            // Setup MockAuthStateProvider to return an authenticated state
            var authState = AuthState.CreateAuthenticated("123-456-7890");
            MockAuthStateProvider.Setup(x => x.GetCurrentState()).ReturnsAsync(authState);
            
            // Initialize ViewModel with mocked dependencies
            InitializeViewModel();
        }

        private void InitializeViewModel()
        {
            ViewModel = new ActivityReportViewModel(
                MockNavigationService.Object,
                MockAuthStateProvider.Object,
                MockReportService.Object,
                MockLocationService.Object);
        }

        [Fact]
        public async Task Test_Initialize_SetsDefaultValues()
        {
            // Initialize the ViewModel
            await ViewModel.InitializeAsync();
            
            // Assert that ReportText is empty
            ViewModel.ReportText.Should().BeEmpty();
            // Assert that RemainingCharacters equals AppConstants.ReportMaxLength
            ViewModel.RemainingCharacters.Should().Be(AppConstants.ReportMaxLength);
            // Assert that CanSubmit is false
            ViewModel.CanSubmit.Should().BeFalse();
        }

        [Fact]
        public void Test_ReportTextChanged_UpdatesRemainingCharacters()
        {
            // Initialize the ViewModel
            string testText = "This is a test report";
            
            // Set ViewModel.ReportText to a test string
            ViewModel.ReportText = testText;
            
            // Assert that RemainingCharacters equals AppConstants.ReportMaxLength minus the length of the test string
            ViewModel.RemainingCharacters.Should().Be(AppConstants.ReportMaxLength - testText.Length);
        }

        [Fact]
        public void Test_ReportTextChanged_UpdatesCanSubmit_Valid()
        {
            // Initialize the ViewModel
            string validText = "This is a valid report text";
            
            // Set ViewModel.ReportText to a valid test string
            ViewModel.ReportText = validText;
            
            // Assert that CanSubmit is true
            ViewModel.CanSubmit.Should().BeTrue();
        }

        [Fact]
        public void Test_ReportTextChanged_UpdatesCanSubmit_Empty()
        {
            // Initialize the ViewModel
            
            // Set ViewModel.ReportText to an empty string
            ViewModel.ReportText = string.Empty;
            
            // Assert that CanSubmit is false
            ViewModel.CanSubmit.Should().BeFalse();
            // Assert that Error property contains ErrorMessages.ReportEmpty
            ViewModel.ErrorMessage.Should().Be(ErrorMessages.ReportEmpty);
        }

        [Fact]
        public void Test_ReportTextChanged_UpdatesCanSubmit_TooLong()
        {
            // Initialize the ViewModel
            
            // Set ViewModel.ReportText to a string longer than AppConstants.ReportMaxLength
            ViewModel.ReportText = new string('A', AppConstants.ReportMaxLength + 1);
            
            // Assert that CanSubmit is false
            ViewModel.CanSubmit.Should().BeFalse();
            // Assert that Error property contains ErrorMessages.ReportTooLong
            ViewModel.ErrorMessage.Should().Be(ErrorMessages.ReportTooLong);
        }

        [Fact]
        public async Task Test_SubmitReport_Success()
        {
            // Initialize the ViewModel
            
            // Setup MockLocationService to return a valid location
            var location = new LocationModel { Latitude = 34.0522, Longitude = -118.2437 };
            MockLocationService.Setup(x => x.GetCurrentLocation()).ReturnsAsync(location);
            
            // Setup MockReportService to successfully create a report
            var report = new ReportModel { Id = 1, Text = "Test report" };
            MockReportService.Setup(x => x.CreateReportAsync(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double>()))
                .ReturnsAsync(report);
            
            // Setup MockReportService to successfully sync the report
            MockReportService.Setup(x => x.SyncReportAsync(It.IsAny<int>())).ReturnsAsync(true);
            
            // Set ViewModel.ReportText to a valid test string
            ViewModel.ReportText = "Test report";
            
            // Execute ViewModel.SubmitReportCommand
            await ViewModel.SubmitReportCommand.ExecuteAsync(null);
            
            // Verify that MockReportService.CreateReportAsync was called with the correct parameters
            MockReportService.Verify(x => x.CreateReportAsync("Test report", 34.0522, -118.2437), Times.Once);
            // Verify that MockReportService.SyncReportAsync was called
            MockReportService.Verify(x => x.SyncReportAsync(1), Times.Once);
            // Verify that MockNavigationService.NavigateToAsync was called with NavigationConstants.ReportListPage
            MockNavigationService.Verify(x => x.NavigateToAsync(NavigationConstants.ReportListPage), Times.Once);
        }

        [Fact]
        public async Task Test_SubmitReport_EmptyText_Fails()
        {
            // Initialize the ViewModel
            
            // Set ViewModel.ReportText to an empty string
            ViewModel.ReportText = string.Empty;
            
            // Execute ViewModel.SubmitReportCommand
            await ViewModel.SubmitReportCommand.ExecuteAsync(null);
            
            // Verify that MockReportService.CreateReportAsync was not called
            MockReportService.Verify(x => x.CreateReportAsync(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double>()), Times.Never);
            // Assert that Error property contains ErrorMessages.ReportEmpty
            ViewModel.ErrorMessage.Should().Be(ErrorMessages.ReportEmpty);
        }

        [Fact]
        public async Task Test_SubmitReport_LocationServiceFails()
        {
            // Initialize the ViewModel
            
            // Setup MockLocationService to throw an exception
            MockLocationService.Setup(x => x.GetCurrentLocation()).ThrowsAsync(new Exception("Location service error"));
            
            // Setup MockReportService to successfully create a report
            var report = new ReportModel { Id = 1, Text = "Test report" };
            MockReportService.Setup(x => x.CreateReportAsync(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double>()))
                .ReturnsAsync(report);
            
            // Set ViewModel.ReportText to a valid test string
            ViewModel.ReportText = "Test report";
            
            // Execute ViewModel.SubmitReportCommand
            await ViewModel.SubmitReportCommand.ExecuteAsync(null);
            
            // Verify that MockReportService.CreateReportAsync was called with default coordinates (0,0)
            MockReportService.Verify(x => x.CreateReportAsync("Test report", 0, 0), Times.Once);
            // Verify that MockNavigationService.NavigateToAsync was called with NavigationConstants.ReportListPage
            MockNavigationService.Verify(x => x.NavigateToAsync(NavigationConstants.ReportListPage), Times.Once);
        }

        [Fact]
        public async Task Test_SubmitReport_ReportServiceFails()
        {
            // Initialize the ViewModel
            
            // Setup MockLocationService to return a valid location
            var location = new LocationModel { Latitude = 34.0522, Longitude = -118.2437 };
            MockLocationService.Setup(x => x.GetCurrentLocation()).ReturnsAsync(location);
            
            // Setup MockReportService to throw an exception
            MockReportService.Setup(x => x.CreateReportAsync(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double>()))
                .ThrowsAsync(new Exception("Report service error"));
            
            // Set ViewModel.ReportText to a valid test string
            ViewModel.ReportText = "Test report";
            
            // Execute ViewModel.SubmitReportCommand
            await ViewModel.SubmitReportCommand.ExecuteAsync(null);
            
            // Assert that Error property contains ErrorMessages.ReportSubmissionFailed
            ViewModel.ErrorMessage.Should().Be(ErrorMessages.ReportSubmissionFailed);
            // Verify that MockNavigationService.NavigateToAsync was not called
            MockNavigationService.Verify(x => x.NavigateToAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Test_CancelCommand_NavigatesBack()
        {
            // Initialize the ViewModel
            
            // Execute ViewModel.CancelCommand
            await ViewModel.CancelCommand.ExecuteAsync(null);
            
            // Verify that MockNavigationService.NavigateBackAsync was called
            MockNavigationService.Verify(x => x.NavigateBackAsync(), Times.Once);
        }
    }
}