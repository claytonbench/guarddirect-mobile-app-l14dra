using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Graphics;
using SecurityPatrol.ViewModels;
using SecurityPatrol.Models;
using SecurityPatrol.Helpers;
using SecurityPatrol.Constants;
using SecurityPatrol.MAUI.UnitTests.Setup;
using SecurityPatrol.TestCommon.Data;

namespace SecurityPatrol.MAUI.UnitTests.ViewModels
{
    public class PhotoCaptureViewModelTests : TestBase
    {
        private Mock<IPhotoSyncService> MockPhotoSyncService;
        private Mock<IAuthenticationStateProvider> MockAuthStateProvider;
        private Mock<ILogger<PhotoCaptureViewModel>> MockLogger;

        public PhotoCaptureViewModelTests()
        {
            // Initialize additional mocks needed for the PhotoCaptureViewModel
            MockPhotoSyncService = new Mock<IPhotoSyncService>();
            MockAuthStateProvider = new Mock<IAuthenticationStateProvider>();
            MockLogger = new Mock<ILogger<PhotoCaptureViewModel>>();

            // Setup authentication state
            SetupMockAuthenticationState(true);

            // Setup PhotoSyncService event handlers and methods
            MockPhotoSyncService.Setup(x => x.UploadPhotoAsync(It.IsAny<string>()))
                .ReturnsAsync(true);
        }

        public void Setup()
        {
            // Reset all mocks
            MockPhotoService.Reset();
            MockNavigationService.Reset();
            MockPhotoSyncService.Reset();
            MockAuthStateProvider.Reset();
            MockLogger.Reset();

            // Setup default behaviors
            SetupMockAuthenticationState(true);
            MockPhotoService.Setup(x => x.CapturePhotoAsync())
                .ReturnsAsync(MockDataGenerator.CreatePhotoModel());

            // Setup event handlers and methods for photo sync service
            MockPhotoSyncService.Setup(x => x.UploadPhotoAsync(It.IsAny<string>()))
                .ReturnsAsync(true);
        }

        private PhotoCaptureViewModel CreateViewModel()
        {
            return new PhotoCaptureViewModel(
                MockNavigationService.Object,
                MockAuthStateProvider.Object,
                MockPhotoService.Object,
                MockPhotoSyncService.Object,
                MockLogger.Object);
        }

        [Fact]
        public void Test_Constructor_InitializesProperties()
        {
            // Arrange & Act
            var viewModel = CreateViewModel();

            // Assert
            viewModel.IsCameraAvailable.Should().BeFalse();
            viewModel.IsCapturing.Should().BeFalse();
            viewModel.HasCapturedPhoto.Should().BeFalse();
            viewModel.PreviewImage.Should().BeNull();
            viewModel.CapturedPhoto.Should().BeNull();
            viewModel.IsUploading.Should().BeFalse();
            viewModel.UploadProgress.Should().Be(0);
            viewModel.CapturePhotoCommand.Should().NotBeNull();
            viewModel.AcceptPhotoCommand.Should().NotBeNull();
            viewModel.RetakePhotoCommand.Should().NotBeNull();
            viewModel.ViewPhotoDetailsCommand.Should().NotBeNull();
            viewModel.BackCommand.Should().NotBeNull();
        }

        [Fact]
        public async Task Test_InitializeAsync_ChecksCameraPermission()
        {
            // Setup PermissionHelper.CheckCameraPermissionAsync to return true
            Mock.Setup(() => PermissionHelper.CheckCameraPermissionAsync(It.IsAny<ILogger>()))
                .ReturnsAsync(true);

            // Create ViewModel and initialize
            var viewModel = CreateViewModel();
            await viewModel.InitializeAsync();

            // Assert camera is available
            viewModel.IsCameraAvailable.Should().BeTrue();

            // Setup PermissionHelper.CheckCameraPermissionAsync to return false
            Mock.Setup(() => PermissionHelper.CheckCameraPermissionAsync(It.IsAny<ILogger>()))
                .ReturnsAsync(false);

            // Create new ViewModel and initialize
            var viewModel2 = CreateViewModel();
            await viewModel2.InitializeAsync();

            // Assert camera is not available
            viewModel2.IsCameraAvailable.Should().BeFalse();
        }

        [Fact]
        public async Task Test_OnAppearing_RequestsCameraPermission()
        {
            // Setup PermissionHelper.CheckCameraPermissionAsync to return false
            Mock.Setup(() => PermissionHelper.CheckCameraPermissionAsync(It.IsAny<ILogger>()))
                .ReturnsAsync(false);

            // Setup PermissionHelper.RequestCameraPermissionAsync to return true
            Mock.Setup(() => PermissionHelper.RequestCameraPermissionAsync(true, It.IsAny<ILogger>()))
                .ReturnsAsync(true);

            // Create ViewModel and initialize
            var viewModel = CreateViewModel();
            await viewModel.InitializeAsync();

            // Assert camera is not available
            viewModel.IsCameraAvailable.Should().BeFalse();

            // Call OnAppearing which should request permission
            await viewModel.OnAppearing();

            // Assert camera is now available
            viewModel.IsCameraAvailable.Should().BeTrue();
        }

        [Fact]
        public async Task Test_CapturePhotoCommand_WhenCameraNotAvailable_DoesNothing()
        {
            // Setup PermissionHelper.CheckCameraPermissionAsync to return false
            Mock.Setup(() => PermissionHelper.CheckCameraPermissionAsync(It.IsAny<ILogger>()))
                .ReturnsAsync(false);

            // Create ViewModel and initialize
            var viewModel = CreateViewModel();
            await viewModel.InitializeAsync();

            // Assert camera is not available
            viewModel.IsCameraAvailable.Should().BeFalse();

            // Execute capture photo command
            await viewModel.CapturePhotoCommand.ExecuteAsync(null);

            // Verify photo service was not called
            MockPhotoService.Verify(x => x.CapturePhotoAsync(), Times.Never);
        }

        [Fact]
        public async Task Test_CapturePhotoCommand_WhenCameraAvailable_CapturesPhoto()
        {
            // Setup PermissionHelper.CheckCameraPermissionAsync to return true
            Mock.Setup(() => PermissionHelper.CheckCameraPermissionAsync(It.IsAny<ILogger>()))
                .ReturnsAsync(true);

            // Create a test photo
            var testPhoto = MockDataGenerator.CreatePhotoModel();
            MockPhotoService.Setup(x => x.CapturePhotoAsync())
                .ReturnsAsync(testPhoto);
            MockPhotoService.Setup(x => x.GetPhotoFileAsync(testPhoto.Id))
                .ReturnsAsync(new System.IO.MemoryStream());

            // Create ViewModel and initialize
            var viewModel = CreateViewModel();
            await viewModel.InitializeAsync();

            // Execute capture photo command
            await viewModel.CapturePhotoCommand.ExecuteAsync(null);

            // Assert IsCapturing was set to true during operation
            viewModel.HasCapturedPhoto.Should().BeTrue();
            viewModel.CapturedPhoto.Should().NotBeNull();
            viewModel.CapturedPhoto.Id.Should().Be(testPhoto.Id);
            viewModel.PreviewImage.Should().NotBeNull();
        }

        [Fact]
        public async Task Test_CapturePhotoCommand_WhenPhotoCaptureFails_SetsErrorState()
        {
            // Setup PermissionHelper.CheckCameraPermissionAsync to return true
            Mock.Setup(() => PermissionHelper.CheckCameraPermissionAsync(It.IsAny<ILogger>()))
                .ReturnsAsync(true);

            // Setup PhotoService to return null (failure)
            MockPhotoService.Setup(x => x.CapturePhotoAsync())
                .ReturnsAsync((PhotoModel)null);

            // Create ViewModel and initialize
            var viewModel = CreateViewModel();
            await viewModel.InitializeAsync();

            // Execute capture photo command
            await viewModel.CapturePhotoCommand.ExecuteAsync(null);

            // Assert
            viewModel.HasCapturedPhoto.Should().BeFalse();
            viewModel.CapturedPhoto.Should().BeNull();
            viewModel.IsCapturing.Should().BeFalse();
        }

        [Fact]
        public async Task Test_CapturePhotoCommand_WhenExceptionOccurs_HandlesGracefully()
        {
            // Setup PermissionHelper.CheckCameraPermissionAsync to return true
            Mock.Setup(() => PermissionHelper.CheckCameraPermissionAsync(It.IsAny<ILogger>()))
                .ReturnsAsync(true);

            // Setup PhotoService to throw an exception
            MockPhotoService.Setup(x => x.CapturePhotoAsync())
                .ThrowsAsync(new Exception("Test exception"));

            // Create ViewModel and initialize
            var viewModel = CreateViewModel();
            await viewModel.InitializeAsync();

            // Execute capture photo command
            await viewModel.CapturePhotoCommand.ExecuteAsync(null);

            // Assert
            viewModel.HasCapturedPhoto.Should().BeFalse();
            viewModel.CapturedPhoto.Should().BeNull();
            viewModel.IsCapturing.Should().BeFalse();
            MockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task Test_AcceptPhotoCommand_WhenNoCapturedPhoto_DoesNothing()
        {
            // Create ViewModel without captured photo
            var viewModel = CreateViewModel();
            viewModel.HasCapturedPhoto.Should().BeFalse();

            // Execute accept photo command
            await viewModel.AcceptPhotoCommand.ExecuteAsync(null);

            // Verify services were not called
            MockPhotoSyncService.Verify(x => x.UploadPhotoAsync(It.IsAny<string>()), Times.Never);
            MockNavigationService.Verify(
                x => x.NavigateToAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()), 
                Times.Never);
        }

        [Fact]
        public async Task Test_AcceptPhotoCommand_WhenPhotoIsCaptured_UploadsAndNavigates()
        {
            // Setup PermissionHelper.CheckCameraPermissionAsync to return true
            Mock.Setup(() => PermissionHelper.CheckCameraPermissionAsync(It.IsAny<ILogger>()))
                .ReturnsAsync(true);

            // Create a test photo
            var testPhoto = MockDataGenerator.CreatePhotoModel();
            MockPhotoService.Setup(x => x.CapturePhotoAsync())
                .ReturnsAsync(testPhoto);
            MockPhotoService.Setup(x => x.GetPhotoFileAsync(testPhoto.Id))
                .ReturnsAsync(new System.IO.MemoryStream());
            MockPhotoSyncService.Setup(x => x.UploadPhotoAsync(testPhoto.Id))
                .ReturnsAsync(true);

            // Create ViewModel and initialize
            var viewModel = CreateViewModel();
            await viewModel.InitializeAsync();

            // Capture photo
            await viewModel.CapturePhotoCommand.ExecuteAsync(null);
            viewModel.HasCapturedPhoto.Should().BeTrue();

            // Execute accept photo command
            await viewModel.AcceptPhotoCommand.ExecuteAsync(null);

            // Assert IsUploading was set to true during operation
            MockPhotoSyncService.Verify(x => x.UploadPhotoAsync(testPhoto.Id), Times.Once);
            MockNavigationService.Verify(
                x => x.NavigateToAsync(
                    NavigationConstants.PhotoDetailPage,
                    It.Is<Dictionary<string, object>>(
                        d => d.ContainsKey(NavigationConstants.ParamPhotoId) && 
                             d[NavigationConstants.ParamPhotoId].Equals(testPhoto.Id))),
                Times.Once);
        }

        [Fact]
        public async Task Test_AcceptPhotoCommand_WhenUploadFails_HandlesGracefully()
        {
            // Setup PermissionHelper.CheckCameraPermissionAsync to return true
            Mock.Setup(() => PermissionHelper.CheckCameraPermissionAsync(It.IsAny<ILogger>()))
                .ReturnsAsync(true);

            // Create a test photo
            var testPhoto = MockDataGenerator.CreatePhotoModel();
            MockPhotoService.Setup(x => x.CapturePhotoAsync())
                .ReturnsAsync(testPhoto);
            MockPhotoService.Setup(x => x.GetPhotoFileAsync(testPhoto.Id))
                .ReturnsAsync(new System.IO.MemoryStream());
            MockPhotoSyncService.Setup(x => x.UploadPhotoAsync(testPhoto.Id))
                .ThrowsAsync(new Exception("Upload failed"));

            // Create ViewModel and initialize
            var viewModel = CreateViewModel();
            await viewModel.InitializeAsync();

            // Capture photo
            await viewModel.CapturePhotoCommand.ExecuteAsync(null);

            // Execute accept photo command
            await viewModel.AcceptPhotoCommand.ExecuteAsync(null);

            // Assert
            MockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task Test_RetakePhotoCommand_ClearsCurrentPhoto()
        {
            // Setup PermissionHelper.CheckCameraPermissionAsync to return true
            Mock.Setup(() => PermissionHelper.CheckCameraPermissionAsync(It.IsAny<ILogger>()))
                .ReturnsAsync(true);

            // Create a test photo
            var testPhoto = MockDataGenerator.CreatePhotoModel();
            MockPhotoService.Setup(x => x.CapturePhotoAsync())
                .ReturnsAsync(testPhoto);
            MockPhotoService.Setup(x => x.GetPhotoFileAsync(testPhoto.Id))
                .ReturnsAsync(new System.IO.MemoryStream());

            // Create ViewModel and initialize
            var viewModel = CreateViewModel();
            await viewModel.InitializeAsync();

            // Capture photo
            await viewModel.CapturePhotoCommand.ExecuteAsync(null);
            viewModel.HasCapturedPhoto.Should().BeTrue();
            viewModel.CapturedPhoto.Should().NotBeNull();

            // Execute retake photo command
            await viewModel.RetakePhotoCommand.ExecuteAsync(null);

            // Assert
            viewModel.HasCapturedPhoto.Should().BeFalse();
            viewModel.CapturedPhoto.Should().BeNull();
            viewModel.PreviewImage.Should().BeNull();
        }

        [Fact]
        public async Task Test_ViewPhotoDetailsCommand_WhenNoCapturedPhoto_DoesNothing()
        {
            // Create ViewModel without captured photo
            var viewModel = CreateViewModel();
            viewModel.HasCapturedPhoto.Should().BeFalse();

            // Execute view photo details command
            await viewModel.ViewPhotoDetailsCommand.ExecuteAsync(null);

            // Verify navigation service was not called
            MockNavigationService.Verify(
                x => x.NavigateToAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()), 
                Times.Never);
        }

        [Fact]
        public async Task Test_ViewPhotoDetailsCommand_WhenPhotoIsCaptured_Navigates()
        {
            // Setup PermissionHelper.CheckCameraPermissionAsync to return true
            Mock.Setup(() => PermissionHelper.CheckCameraPermissionAsync(It.IsAny<ILogger>()))
                .ReturnsAsync(true);

            // Create a test photo
            var testPhoto = MockDataGenerator.CreatePhotoModel();
            MockPhotoService.Setup(x => x.CapturePhotoAsync())
                .ReturnsAsync(testPhoto);
            MockPhotoService.Setup(x => x.GetPhotoFileAsync(testPhoto.Id))
                .ReturnsAsync(new System.IO.MemoryStream());

            // Create ViewModel and initialize
            var viewModel = CreateViewModel();
            await viewModel.InitializeAsync();

            // Capture photo
            await viewModel.CapturePhotoCommand.ExecuteAsync(null);
            viewModel.HasCapturedPhoto.Should().BeTrue();

            // Execute view photo details command
            await viewModel.ViewPhotoDetailsCommand.ExecuteAsync(null);

            // Verify navigation
            MockNavigationService.Verify(
                x => x.NavigateToAsync(
                    NavigationConstants.PhotoDetailPage,
                    It.Is<Dictionary<string, object>>(
                        d => d.ContainsKey(NavigationConstants.ParamPhotoId) && 
                             d[NavigationConstants.ParamPhotoId].Equals(testPhoto.Id))),
                Times.Once);
        }

        [Fact]
        public async Task Test_BackCommand_NavigatesBack()
        {
            // Create ViewModel
            var viewModel = CreateViewModel();

            // Execute back command
            await viewModel.BackCommand.ExecuteAsync(null);

            // Verify navigation
            MockNavigationService.Verify(x => x.NavigateBackAsync(), Times.Once);
        }

        [Fact]
        public async Task Test_UploadProgressChanged_UpdatesProgressProperty()
        {
            // Setup PermissionHelper.CheckCameraPermissionAsync to return true
            Mock.Setup(() => PermissionHelper.CheckCameraPermissionAsync(It.IsAny<ILogger>()))
                .ReturnsAsync(true);

            // Create a test photo
            var testPhoto = MockDataGenerator.CreatePhotoModel();
            MockPhotoService.Setup(x => x.CapturePhotoAsync())
                .ReturnsAsync(testPhoto);
            MockPhotoService.Setup(x => x.GetPhotoFileAsync(testPhoto.Id))
                .ReturnsAsync(new System.IO.MemoryStream());

            // Create ViewModel and initialize
            var viewModel = CreateViewModel();
            await viewModel.InitializeAsync();

            // Capture photo
            await viewModel.CapturePhotoCommand.ExecuteAsync(null);

            // Create progress with 50% completion for the captured photo
            var progress = new PhotoUploadProgress(testPhoto.Id);
            progress.UpdateProgress(50);

            // Raise the event
            MockPhotoSyncService.Raise(m => m.UploadProgressChanged += null, progress);

            // Assert
            viewModel.UploadProgress.Should().Be(50);
            viewModel.IsUploading.Should().BeTrue();

            // Create progress with 100% completion and "Completed" status
            var completedProgress = new PhotoUploadProgress(testPhoto.Id);
            completedProgress.UpdateProgress(100);

            // Raise the event
            MockPhotoSyncService.Raise(m => m.UploadProgressChanged += null, completedProgress);

            // Assert
            viewModel.UploadProgress.Should().Be(100);
            viewModel.IsUploading.Should().BeFalse();
        }

        [Fact]
        public async Task Test_UploadProgressChanged_WhenErrorStatus_HandlesGracefully()
        {
            // Setup PermissionHelper.CheckCameraPermissionAsync to return true
            Mock.Setup(() => PermissionHelper.CheckCameraPermissionAsync(It.IsAny<ILogger>()))
                .ReturnsAsync(true);

            // Create a test photo
            var testPhoto = MockDataGenerator.CreatePhotoModel();
            MockPhotoService.Setup(x => x.CapturePhotoAsync())
                .ReturnsAsync(testPhoto);
            MockPhotoService.Setup(x => x.GetPhotoFileAsync(testPhoto.Id))
                .ReturnsAsync(new System.IO.MemoryStream());

            // Create ViewModel and initialize
            var viewModel = CreateViewModel();
            await viewModel.InitializeAsync();

            // Capture photo
            await viewModel.CapturePhotoCommand.ExecuteAsync(null);

            // Create progress with "Error" status for the captured photo
            var errorProgress = new PhotoUploadProgress(testPhoto.Id);
            errorProgress.SetError("Test error message");

            // Raise the event
            MockPhotoSyncService.Raise(m => m.UploadProgressChanged += null, errorProgress);

            // Assert
            viewModel.IsUploading.Should().BeFalse();
            MockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.AtLeastOnce);
        }
    }
}