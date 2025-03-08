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
    public class SettingsViewModelTests : TestBase
    {
        private Mock<IAuthenticationStateProvider> MockAuthStateProvider;

        public SettingsViewModelTests()
        {
            // Initialize test base and additional mocks
            MockAuthStateProvider = new Mock<IAuthenticationStateProvider>();
        }

        private SettingsViewModel Setup_ViewModel()
        {
            var viewModel = new SettingsViewModel(
                MockNavigationService.Object, 
                MockAuthStateProvider.Object, 
                MockSettingsService.Object, 
                MockAuthService.Object);
            
            viewModel.InitializeAsync();
            return viewModel;
        }

        [Fact]
        public void Test_Constructor_InitializesProperties()
        {
            // Arrange & Act
            var viewModel = new SettingsViewModel(
                MockNavigationService.Object, 
                MockAuthStateProvider.Object, 
                MockSettingsService.Object, 
                MockAuthService.Object);
            
            // Assert
            viewModel.AppVersion.Should().Be(AppConstants.AppVersion);
            viewModel.UserPhoneNumber.Should().BeEmpty();
            viewModel.LocationTrackingModes.Should().NotBeNull();
            viewModel.LocationTrackingModes.Count.Should().Be(3);
            viewModel.SelectedLocationTrackingMode.Should().Be(0);
            viewModel.EnableBackgroundTracking.Should().BeTrue();
            viewModel.EnableOfflineMode.Should().BeFalse();
            viewModel.EnableTelemetry.Should().Be(AppConstants.EnableTelemetry);
            viewModel.SaveSettingsCommand.Should().NotBeNull();
            viewModel.ClearDataCommand.Should().NotBeNull();
            viewModel.LogoutCommand.Should().NotBeNull();
        }

        [Fact]
        public async Task Test_InitializeAsync_LoadsUserInformation()
        {
            // Arrange
            MockAuthService.Setup(x => x.GetAuthenticationState())
                .ReturnsAsync(CreateAuthenticatedState("test@example.com"));
            
            // Act
            var viewModel = new SettingsViewModel(
                MockNavigationService.Object, 
                MockAuthStateProvider.Object, 
                MockSettingsService.Object, 
                MockAuthService.Object);
            await viewModel.InitializeAsync();
            
            // Assert
            viewModel.UserPhoneNumber.Should().Be("test@example.com");
            MockAuthService.Verify(x => x.GetAuthenticationState(), Times.Once());
        }

        [Fact]
        public async Task Test_InitializeAsync_LoadsSettings()
        {
            // Arrange
            MockSettingsService.Setup(x => x.GetValue<bool>("EnableBackgroundTracking", true))
                .ReturnsAsync(false);
            MockSettingsService.Setup(x => x.GetValue<bool>("EnableOfflineMode", false))
                .ReturnsAsync(true);
            MockSettingsService.Setup(x => x.GetValue<bool>("EnableTelemetry", AppConstants.EnableTelemetry))
                .ReturnsAsync(false);
            MockSettingsService.Setup(x => x.GetValue<int>("SelectedLocationTrackingMode", 0))
                .ReturnsAsync(2);
            
            // Act
            var viewModel = new SettingsViewModel(
                MockNavigationService.Object, 
                MockAuthStateProvider.Object, 
                MockSettingsService.Object, 
                MockAuthService.Object);
            await viewModel.InitializeAsync();
            
            // Assert
            viewModel.EnableBackgroundTracking.Should().BeFalse();
            viewModel.EnableOfflineMode.Should().BeTrue();
            viewModel.EnableTelemetry.Should().BeFalse();
            viewModel.SelectedLocationTrackingMode.Should().Be(2);
            
            MockSettingsService.Verify(x => x.GetValue<bool>("EnableBackgroundTracking", true), Times.Once());
            MockSettingsService.Verify(x => x.GetValue<bool>("EnableOfflineMode", false), Times.Once());
            MockSettingsService.Verify(x => x.GetValue<bool>("EnableTelemetry", AppConstants.EnableTelemetry), Times.Once());
            MockSettingsService.Verify(x => x.GetValue<int>("SelectedLocationTrackingMode", 0), Times.Once());
        }

        [Fact]
        public async Task Test_SaveSettingsCommand_SavesSettings()
        {
            // Arrange
            MockSettingsService.Setup(x => x.SetValue<bool>("EnableBackgroundTracking", It.IsAny<bool>()))
                .Returns(Task.CompletedTask);
            MockSettingsService.Setup(x => x.SetValue<bool>("EnableOfflineMode", It.IsAny<bool>()))
                .Returns(Task.CompletedTask);
            MockSettingsService.Setup(x => x.SetValue<bool>("EnableTelemetry", It.IsAny<bool>()))
                .Returns(Task.CompletedTask);
            MockSettingsService.Setup(x => x.SetValue<int>("SelectedLocationTrackingMode", It.IsAny<int>()))
                .Returns(Task.CompletedTask);
            
            var viewModel = Setup_ViewModel();
            
            // Act
            viewModel.EnableBackgroundTracking = false;
            viewModel.EnableOfflineMode = true;
            viewModel.EnableTelemetry = false;
            viewModel.SelectedLocationTrackingMode = 2;
            
            await viewModel.SaveSettingsCommand.ExecuteAsync(null);
            
            // Assert
            MockSettingsService.Verify(x => x.SetValue<bool>("EnableBackgroundTracking", false), Times.Once());
            MockSettingsService.Verify(x => x.SetValue<bool>("EnableOfflineMode", true), Times.Once());
            MockSettingsService.Verify(x => x.SetValue<bool>("EnableTelemetry", false), Times.Once());
            MockSettingsService.Verify(x => x.SetValue<int>("SelectedLocationTrackingMode", 2), Times.Once());
        }

        [Fact]
        public async Task Test_SaveSettingsCommand_HandlesException()
        {
            // Arrange
            MockSettingsService.Setup(x => x.SetValue<bool>("EnableBackgroundTracking", It.IsAny<bool>()))
                .ThrowsAsync(new Exception("Test exception"));
            
            var viewModel = Setup_ViewModel();
            
            // Act
            await viewModel.SaveSettingsCommand.ExecuteAsync(null);
            
            // Assert
            viewModel.HasError.Should().BeTrue();
            viewModel.ErrorMessage.Should().NotBeNull();
            MockSettingsService.Verify(x => x.SetValue<bool>("EnableBackgroundTracking", It.IsAny<bool>()), Times.Once());
        }

        [Fact]
        public async Task Test_ClearDataCommand_ClearsSettings()
        {
            // Arrange
            MockSettingsService.Setup(x => x.Clear()).Returns(Task.CompletedTask);
            
            var viewModel = Setup_ViewModel();
            viewModel.EnableBackgroundTracking = false;
            viewModel.EnableOfflineMode = true;
            viewModel.EnableTelemetry = false;
            viewModel.SelectedLocationTrackingMode = 2;
            
            // Act
            await viewModel.ClearDataCommand.ExecuteAsync(null);
            
            // Assert
            MockSettingsService.Verify(x => x.Clear(), Times.Once());
            viewModel.EnableBackgroundTracking.Should().BeTrue();
            viewModel.EnableOfflineMode.Should().BeFalse();
            viewModel.EnableTelemetry.Should().Be(AppConstants.EnableTelemetry);
            viewModel.SelectedLocationTrackingMode.Should().Be(0);
        }

        [Fact]
        public async Task Test_ClearDataCommand_HandlesException()
        {
            // Arrange
            MockSettingsService.Setup(x => x.Clear())
                .ThrowsAsync(new Exception("Test exception"));
            
            var viewModel = Setup_ViewModel();
            
            // Act
            await viewModel.ClearDataCommand.ExecuteAsync(null);
            
            // Assert
            viewModel.HasError.Should().BeTrue();
            viewModel.ErrorMessage.Should().NotBeNull();
            MockSettingsService.Verify(x => x.Clear(), Times.Once());
        }

        [Fact]
        public async Task Test_LogoutCommand_LogsOutAndNavigates()
        {
            // Arrange
            MockAuthService.Setup(x => x.Logout()).Returns(Task.CompletedTask);
            MockNavigationService.Setup(x => x.NavigateToAsync(NavigationConstants.PhoneEntryPage, null))
                .Returns(Task.CompletedTask);
            
            var viewModel = Setup_ViewModel();
            
            // Act
            await viewModel.LogoutCommand.ExecuteAsync(null);
            
            // Assert
            MockAuthService.Verify(x => x.Logout(), Times.Once());
            MockNavigationService.Verify(x => x.NavigateToAsync(NavigationConstants.PhoneEntryPage, null), Times.Once());
        }

        [Fact]
        public async Task Test_LogoutCommand_HandlesException()
        {
            // Arrange
            MockAuthService.Setup(x => x.Logout())
                .ThrowsAsync(new Exception("Test exception"));
            
            var viewModel = Setup_ViewModel();
            
            // Act
            await viewModel.LogoutCommand.ExecuteAsync(null);
            
            // Assert
            viewModel.HasError.Should().BeTrue();
            viewModel.ErrorMessage.Should().NotBeNull();
            MockAuthService.Verify(x => x.Logout(), Times.Once());
            MockNavigationService.Verify(x => x.NavigateToAsync(It.IsAny<string>(), null), Times.Never());
        }
    }
}