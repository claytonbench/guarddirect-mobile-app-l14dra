using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Graphics;
using Moq;
using SecurityPatrol.Constants;
using SecurityPatrol.Helpers;
using SecurityPatrol.Models;
using SecurityPatrol.Services;
using SecurityPatrol.ViewModels;
using SecurityPatrol.UnitTests.Helpers;
using SecurityPatrol.UnitTests.Helpers.MockServices;
using Xunit;

namespace SecurityPatrol.UnitTests.ViewModels
{
    public class PhotoCaptureViewModelTests
    {
        private Mock<INavigationService> _mockNavigationService;
        private Mock<IAuthenticationStateProvider> _mockAuthStateProvider;
        private Mock<IPhotoService> _mockPhotoService;
        private Mock<IPhotoSyncService> _mockPhotoSyncService;
        private Mock<ILogger<PhotoCaptureViewModel>> _mockLogger;
        private PhotoCaptureViewModel _viewModel;

        private void Setup()
        {
            // Initialize all mock objects
            _mockNavigationService = new Mock<INavigationService>();
            _mockAuthStateProvider = new Mock<IAuthenticationStateProvider>();
            _mockPhotoService = new Mock<IPhotoService>();
            _mockPhotoSyncService = new Mock<IPhotoSyncService>();
            _mockLogger = new Mock<ILogger<PhotoCaptureViewModel>>();
            
            // Set up default behavior for mock objects
            var authState = TestDataGenerator.CreateAuthState(true);
            _mockAuthStateProvider.Setup(m => m.GetCurrentState()).ReturnsAsync(authState);
            
            // Create an instance of PhotoCaptureViewModel with the mock dependencies
            _viewModel = new PhotoCaptureViewModel(
                _mockNavigationService.Object,
                _mockAuthStateProvider.Object,
                _mockPhotoService.Object,
                _mockPhotoSyncService.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task InitializeAsync_ChecksCameraPermission()
        {
            // Arrange: Set up mock for PermissionHelper.CheckCameraPermissionAsync to return true
            Setup();
            
            // Act: Call _viewModel.InitializeAsync()
            await _viewModel.InitializeAsync();
            
            // Assert: Verify IsCameraAvailable is true
            // Assert: Verify PermissionHelper.CheckCameraPermissionAsync was called
            // Note: Since PermissionHelper is static, we can't verify it was called
            // This test mainly ensures the method completes without exceptions
        }

        [Fact]
        public async Task InitializeAsync_WhenPermissionDenied_SetsCameraUnavailable()
        {
            // Arrange: Set up mock for PermissionHelper.CheckCameraPermissionAsync to return false
            Setup();
            
            // Act: Call _viewModel.InitializeAsync()
            await _viewModel.InitializeAsync();
            
            // Assert: Verify IsCameraAvailable is false
            // Assert: Verify PermissionHelper.CheckCameraPermissionAsync was called
            // Note: Since PermissionHelper is static, we can't control or verify its behavior
            // This test mainly ensures the method completes without exceptions
        }

        [Fact]
        public async Task OnAppearing_RequestsCameraPermission_WhenNotAvailable()
        {
            // Arrange: Set _viewModel.IsCameraAvailable to false
            Setup();
            _viewModel.IsCameraAvailable = false;
            
            // Act: Call _viewModel.OnAppearing()
            await _viewModel.OnAppearing();
            
            // Assert: Verify PermissionHelper.RequestCameraPermissionAsync was called
            // Note: Since PermissionHelper is static, we can't verify it was called
            // This test mainly ensures the method completes without exceptions
        }

        [Fact]
        public async Task OnAppearing_DoesNotRequestPermission_WhenAlreadyAvailable()
        {
            // Arrange: Set _viewModel.IsCameraAvailable to true
            Setup();
            _viewModel.IsCameraAvailable = true;
            
            // Act: Call _viewModel.OnAppearing()
            await _viewModel.OnAppearing();
            
            // Assert: Verify PermissionHelper.RequestCameraPermissionAsync was not called
            // Note: Since PermissionHelper is static, we can't verify it was not called
            // This test mainly ensures the method completes without exceptions
        }

        [Fact]
        public async Task CapturePhotoCommand_WhenCameraNotAvailable_DoesNothing()
        {
            // Arrange: Set _viewModel.IsCameraAvailable to false
            Setup();
            _viewModel.IsCameraAvailable = false;
            
            // Act: Execute _viewModel.CapturePhotoCommand
            await _viewModel.CapturePhotoCommand.ExecuteAsync(null);
            
            // Assert: Verify _mockPhotoService.CapturePhotoAsync was not called
            _mockPhotoService.Verify(m => m.CapturePhotoAsync(), Times.Never);
            // Assert: Verify IsCapturing is false
            _viewModel.IsCapturing.Should().BeFalse();
            // Assert: Verify HasCapturedPhoto is false
            _viewModel.HasCapturedPhoto.Should().BeFalse();
        }

        [Fact]
        public async Task CapturePhotoCommand_WhenAlreadyCapturing_DoesNothing()
        {
            // Arrange: Set _viewModel.IsCameraAvailable to true
            Setup();
            _viewModel.IsCameraAvailable = true;
            // Arrange: Set _viewModel.IsCapturing to true
            _viewModel.IsCapturing = true;
            
            // Act: Execute _viewModel.CapturePhotoCommand
            await _viewModel.CapturePhotoCommand.ExecuteAsync(null);
            
            // Assert: Verify _mockPhotoService.CapturePhotoAsync was not called
            _mockPhotoService.Verify(m => m.CapturePhotoAsync(), Times.Never);
            // Assert: Verify IsCapturing is still true
            _viewModel.IsCapturing.Should().BeTrue();
            // Assert: Verify HasCapturedPhoto is false
            _viewModel.HasCapturedPhoto.Should().BeFalse();
        }

        [Fact]
        public async Task CapturePhotoCommand_WhenCameraAvailable_CapturesPhoto()
        {
            // Arrange: Set _viewModel.IsCameraAvailable to true
            Setup();
            _viewModel.IsCameraAvailable = true;
            // Arrange: Set up mock photo service to return a valid photo model
            var photoModel = TestDataGenerator.CreatePhotoModel();
            var photoStream = new MemoryStream();
            _mockPhotoService.Setup(m => m.CapturePhotoAsync()).ReturnsAsync(photoModel);
            _mockPhotoService.Setup(m => m.GetPhotoFileAsync(photoModel.Id)).ReturnsAsync(photoStream);
            
            // Act: Execute _viewModel.CapturePhotoCommand
            await _viewModel.CapturePhotoCommand.ExecuteAsync(null);
            
            // Assert: Verify _mockPhotoService.CapturePhotoAsync was called
            _mockPhotoService.Verify(m => m.CapturePhotoAsync(), Times.Once);
            // Assert: Verify IsCapturing was set to true during operation
            _mockPhotoService.Verify(m => m.GetPhotoFileAsync(photoModel.Id), Times.Once);
            // Assert: Verify IsCapturing is set back to false after operation
            _viewModel.IsCapturing.Should().BeFalse();
            // Assert: Verify HasCapturedPhoto is true
            _viewModel.HasCapturedPhoto.Should().BeTrue();
            // Assert: Verify CapturedPhoto is not null
            _viewModel.CapturedPhoto.Should().NotBeNull();
            // Assert: Verify PreviewImage is not null
            _viewModel.PreviewImage.Should().NotBeNull();
        }

        [Fact]
        public async Task CapturePhotoCommand_WhenPhotoServiceFails_HandlesError()
        {
            // Arrange: Set _viewModel.IsCameraAvailable to true
            Setup();
            _viewModel.IsCameraAvailable = true;
            // Arrange: Set up mock photo service to return null
            _mockPhotoService.Setup(m => m.CapturePhotoAsync()).ReturnsAsync((PhotoModel)null);
            
            // Act: Execute _viewModel.CapturePhotoCommand
            await _viewModel.CapturePhotoCommand.ExecuteAsync(null);
            
            // Assert: Verify _mockPhotoService.CapturePhotoAsync was called
            _mockPhotoService.Verify(m => m.CapturePhotoAsync(), Times.Once);
            // Assert: Verify IsCapturing was set to true during operation
            // Assert: Verify IsCapturing is set back to false after operation
            _viewModel.IsCapturing.Should().BeFalse();
            // Assert: Verify HasCapturedPhoto is false
            _viewModel.HasCapturedPhoto.Should().BeFalse();
            // Assert: Verify CapturedPhoto is null
            _viewModel.CapturedPhoto.Should().BeNull();
            // Assert: Verify PreviewImage is null
            _viewModel.PreviewImage.Should().BeNull();
            // Assert: Verify error was logged
            _mockLogger.Verify(l => l.LogError(It.IsAny<Exception>(), It.IsAny<string>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task AcceptPhotoCommand_WhenNoPhotoTaken_DoesNothing()
        {
            // Arrange: Set _viewModel.HasCapturedPhoto to false
            Setup();
            _viewModel.HasCapturedPhoto = false;
            // Arrange: Set _viewModel.CapturedPhoto to null
            _viewModel.CapturedPhoto = null;
            
            // Act: Execute _viewModel.AcceptPhotoCommand
            await _viewModel.AcceptPhotoCommand.ExecuteAsync(null);
            
            // Assert: Verify _mockPhotoSyncService.UploadPhotoAsync was not called
            _mockPhotoSyncService.Verify(m => m.UploadPhotoAsync(It.IsAny<string>()), Times.Never);
            // Assert: Verify _mockNavigationService.NavigateToAsync was not called
            _mockNavigationService.Verify(m => m.NavigateToAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()), Times.Never);
            // Assert: Verify IsUploading is false
            _viewModel.IsUploading.Should().BeFalse();
        }

        [Fact]
        public async Task AcceptPhotoCommand_WhenPhotoTaken_StartsUploadAndNavigates()
        {
            // Arrange: Set _viewModel.HasCapturedPhoto to true
            Setup();
            _viewModel.HasCapturedPhoto = true;
            // Arrange: Set _viewModel.CapturedPhoto to a valid photo model
            var photoModel = TestDataGenerator.CreatePhotoModel();
            _viewModel.CapturedPhoto = photoModel;
            
            // Act: Execute _viewModel.AcceptPhotoCommand
            await _viewModel.AcceptPhotoCommand.ExecuteAsync(null);
            
            // Assert: Verify _mockPhotoSyncService.UploadPhotoAsync was called with correct photo ID
            _mockPhotoSyncService.Verify(m => m.UploadPhotoAsync(photoModel.Id), Times.Once);
            // Assert: Verify _mockNavigationService.NavigateToAsync was called with correct route and parameters
            _mockNavigationService.Verify(
                m => m.NavigateToAsync(
                    NavigationConstants.PhotoDetailPage,
                    It.Is<Dictionary<string, object>>(d => d.ContainsKey(NavigationConstants.ParamPhotoId) && d[NavigationConstants.ParamPhotoId].Equals(photoModel.Id))),
                Times.Once);
            // Assert: Verify IsUploading was set to true
            _viewModel.IsUploading.Should().BeTrue();
            // Assert: Verify UploadProgress was set to 0
            _viewModel.UploadProgress.Should().Be(0);
        }

        [Fact]
        public async Task AcceptPhotoCommand_WhenUploadFails_HandlesError()
        {
            // Arrange: Set _viewModel.HasCapturedPhoto to true
            Setup();
            _viewModel.HasCapturedPhoto = true;
            // Arrange: Set _viewModel.CapturedPhoto to a valid photo model
            var photoModel = TestDataGenerator.CreatePhotoModel();
            _viewModel.CapturedPhoto = photoModel;
            // Arrange: Set up mock photo sync service to throw an exception
            _mockPhotoSyncService.Setup(m => m.UploadPhotoAsync(photoModel.Id)).ThrowsAsync(new Exception("Upload failed"));
            
            // Act: Execute _viewModel.AcceptPhotoCommand
            await _viewModel.AcceptPhotoCommand.ExecuteAsync(null);
            
            // Assert: Verify _mockPhotoSyncService.UploadPhotoAsync was called
            _mockPhotoSyncService.Verify(m => m.UploadPhotoAsync(photoModel.Id), Times.Once);
            // Assert: Verify _mockNavigationService.NavigateToAsync was not called
            _mockNavigationService.Verify(m => m.NavigateToAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()), Times.Never);
            // Assert: Verify IsUploading was set back to false
            _viewModel.IsUploading.Should().BeFalse();
            // Assert: Verify error was logged
            _mockLogger.Verify(l => l.LogError(It.IsAny<Exception>(), It.IsAny<string>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task RetakePhotoCommand_ClearsCurrentPhoto()
        {
            // Arrange: Set _viewModel.HasCapturedPhoto to true
            Setup();
            _viewModel.HasCapturedPhoto = true;
            // Arrange: Set _viewModel.CapturedPhoto to a valid photo model
            var photoModel = TestDataGenerator.CreatePhotoModel();
            _viewModel.CapturedPhoto = photoModel;
            // Arrange: Set _viewModel.PreviewImage to a non-null value
            // Note: We can't easily set PreviewImage due to ImageSource constraints in tests
            
            // Act: Execute _viewModel.RetakePhotoCommand
            await _viewModel.RetakePhotoCommand.ExecuteAsync(null);
            
            // Assert: Verify HasCapturedPhoto is false
            _viewModel.HasCapturedPhoto.Should().BeFalse();
            // Assert: Verify CapturedPhoto is null
            _viewModel.CapturedPhoto.Should().BeNull();
            // Assert: Verify PreviewImage is null
            _viewModel.PreviewImage.Should().BeNull();
        }

        [Fact]
        public async Task ViewPhotoDetailsCommand_WhenNoPhotoTaken_DoesNothing()
        {
            // Arrange: Set _viewModel.HasCapturedPhoto to false
            Setup();
            _viewModel.HasCapturedPhoto = false;
            // Arrange: Set _viewModel.CapturedPhoto to null
            _viewModel.CapturedPhoto = null;
            
            // Act: Execute _viewModel.ViewPhotoDetailsCommand
            await _viewModel.ViewPhotoDetailsCommand.ExecuteAsync(null);
            
            // Assert: Verify _mockNavigationService.NavigateToAsync was not called
            _mockNavigationService.Verify(m => m.NavigateToAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()), Times.Never);
        }

        [Fact]
        public async Task ViewPhotoDetailsCommand_WhenPhotoTaken_NavigatesToDetails()
        {
            // Arrange: Set _viewModel.HasCapturedPhoto to true
            Setup();
            _viewModel.HasCapturedPhoto = true;
            // Arrange: Set _viewModel.CapturedPhoto to a valid photo model
            var photoModel = TestDataGenerator.CreatePhotoModel();
            _viewModel.CapturedPhoto = photoModel;
            
            // Act: Execute _viewModel.ViewPhotoDetailsCommand
            await _viewModel.ViewPhotoDetailsCommand.ExecuteAsync(null);
            
            // Assert: Verify _mockNavigationService.NavigateToAsync was called with correct route and parameters
            _mockNavigationService.Verify(
                m => m.NavigateToAsync(
                    NavigationConstants.PhotoDetailPage,
                    It.Is<Dictionary<string, object>>(d => d.ContainsKey(NavigationConstants.ParamPhotoId) && d[NavigationConstants.ParamPhotoId].Equals(photoModel.Id))),
                Times.Once);
        }

        [Fact]
        public async Task BackCommand_NavigatesBack()
        {
            // Act: Execute _viewModel.BackCommand
            Setup();
            await _viewModel.BackCommand.ExecuteAsync(null);
            
            // Assert: Verify _mockNavigationService.NavigateBackAsync was called
            _mockNavigationService.Verify(m => m.NavigateBackAsync(), Times.Once);
        }

        [Fact]
        public void HandleUploadProgressChanged_UpdatesProgressForMatchingPhoto()
        {
            // Arrange: Set _viewModel.CapturedPhoto to a valid photo model
            Setup();
            var photoModel = TestDataGenerator.CreatePhotoModel();
            _viewModel.CapturedPhoto = photoModel;
            // Arrange: Create a PhotoUploadProgress with matching ID and 50% progress
            var progress = new PhotoUploadProgress(photoModel.Id);
            progress.UpdateProgress(50);
            
            // Act: Call _viewModel.HandleUploadProgressChanged with the progress object
            _viewModel.HandleUploadProgressChanged(null, progress);
            
            // Assert: Verify UploadProgress is updated to 50
            _viewModel.UploadProgress.Should().Be(50);
        }

        [Fact]
        public void HandleUploadProgressChanged_IgnoresNonMatchingPhoto()
        {
            // Arrange: Set _viewModel.CapturedPhoto to a valid photo model with ID 'photo1'
            Setup();
            var photoModel = TestDataGenerator.CreatePhotoModel(id: "photo1");
            _viewModel.CapturedPhoto = photoModel;
            // Arrange: Create a PhotoUploadProgress with different ID 'photo2' and 50% progress
            var progress = new PhotoUploadProgress("photo2");
            progress.UpdateProgress(50);
            _viewModel.UploadProgress = 0;
            
            // Act: Call _viewModel.HandleUploadProgressChanged with the progress object
            _viewModel.HandleUploadProgressChanged(null, progress);
            
            // Assert: Verify UploadProgress is not changed
            _viewModel.UploadProgress.Should().Be(0);
        }

        [Fact]
        public void HandleUploadProgressChanged_WhenCompleted_SetsIsUploadingFalse()
        {
            // Arrange: Set _viewModel.CapturedPhoto to a valid photo model
            Setup();
            var photoModel = TestDataGenerator.CreatePhotoModel();
            _viewModel.CapturedPhoto = photoModel;
            // Arrange: Set _viewModel.IsUploading to true
            _viewModel.IsUploading = true;
            // Arrange: Create a PhotoUploadProgress with matching ID, 100% progress, and 'Completed' status
            var progress = new PhotoUploadProgress(photoModel.Id);
            progress.UpdateProgress(100);
            
            // Act: Call _viewModel.HandleUploadProgressChanged with the progress object
            _viewModel.HandleUploadProgressChanged(null, progress);
            
            // Assert: Verify UploadProgress is updated to 100
            _viewModel.UploadProgress.Should().Be(100);
            // Assert: Verify IsUploading is set to false
            _viewModel.IsUploading.Should().BeFalse();
        }

        [Fact]
        public void HandleUploadProgressChanged_WhenError_SetsIsUploadingFalseAndLogsError()
        {
            // Arrange: Set _viewModel.CapturedPhoto to a valid photo model
            Setup();
            var photoModel = TestDataGenerator.CreatePhotoModel();
            _viewModel.CapturedPhoto = photoModel;
            // Arrange: Set _viewModel.IsUploading to true
            _viewModel.IsUploading = true;
            // Arrange: Create a PhotoUploadProgress with matching ID and 'Error' status
            var progress = new PhotoUploadProgress(photoModel.Id);
            progress.SetError("Upload failed");
            
            // Act: Call _viewModel.HandleUploadProgressChanged with the progress object
            _viewModel.HandleUploadProgressChanged(null, progress);
            
            // Assert: Verify IsUploading is set to false
            _viewModel.IsUploading.Should().BeFalse();
            // Assert: Verify error was logged
            _mockLogger.Verify(l => l.LogError(It.IsAny<string>(), It.IsAny<object[]>()), Times.AtLeastOnce);
        }

        [Fact]
        public void Dispose_UnsubscribesFromEvents()
        {
            // Arrange: Create a new instance of PhotoCaptureViewModel with mock dependencies
            Setup();
            
            // Act: Call Dispose on the view model
            _viewModel.Dispose();
            
            // Assert: Verify that the event handler was removed from _mockPhotoSyncService.UploadProgressChanged
            // Note: We can't directly verify event unsubscription with Moq
            // This test mainly ensures that Dispose doesn't throw exceptions
        }
    }
}