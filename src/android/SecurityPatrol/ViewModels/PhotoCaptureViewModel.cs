using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel; // Latest
using CommunityToolkit.Mvvm.Input; // Latest
using Microsoft.Extensions.Logging; // 8.0.0
using Microsoft.Maui.Graphics; // 8.0.0
using SecurityPatrol.Constants;
using SecurityPatrol.Helpers;
using SecurityPatrol.Models;
using SecurityPatrol.Services;
using SecurityPatrol.ViewModels;

namespace SecurityPatrol.ViewModels
{
    /// <summary>
    /// ViewModel for the photo capture screen that handles camera interaction, photo capture, preview, and saving functionality.
    /// Implements the MVVM pattern to separate UI logic from business logic.
    /// </summary>
    public partial class PhotoCaptureViewModel : BaseViewModel
    {
        private readonly ILogger<PhotoCaptureViewModel> _logger;

        #region Services

        /// <summary>
        /// Gets the photo service used for capturing and storing photos.
        /// </summary>
        protected IPhotoService PhotoService { get; }

        /// <summary>
        /// Gets the photo sync service used for uploading photos to the backend.
        /// </summary>
        protected IPhotoSyncService PhotoSyncService { get; }

        #endregion

        #region Properties

        [ObservableProperty]
        private bool _isCameraAvailable;

        [ObservableProperty]
        private bool _isCapturing;

        [ObservableProperty]
        private bool _hasCapturedPhoto;

        [ObservableProperty]
        private ImageSource _previewImage;

        [ObservableProperty]
        private PhotoModel _capturedPhoto;

        [ObservableProperty]
        private bool _isUploading;

        [ObservableProperty]
        private int _uploadProgress;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="PhotoCaptureViewModel"/> class with required services.
        /// </summary>
        /// <param name="navigationService">Service for navigation between pages.</param>
        /// <param name="authenticationStateProvider">Service for accessing authentication state.</param>
        /// <param name="photoService">Service for capturing and storing photos.</param>
        /// <param name="photoSyncService">Service for synchronizing photos with backend.</param>
        /// <param name="logger">Logger for diagnostic information.</param>
        /// <exception cref="ArgumentNullException">Thrown if any of the required services are null.</exception>
        public PhotoCaptureViewModel(
            INavigationService navigationService,
            IAuthenticationStateProvider authenticationStateProvider,
            IPhotoService photoService,
            IPhotoSyncService photoSyncService,
            ILogger<PhotoCaptureViewModel> logger) 
            : base(navigationService, authenticationStateProvider)
        {
            PhotoService = photoService ?? throw new ArgumentNullException(nameof(photoService));
            PhotoSyncService = photoSyncService ?? throw new ArgumentNullException(nameof(photoSyncService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Subscribe to upload progress events
            PhotoSyncService.UploadProgressChanged += HandleUploadProgressChanged;

            // Initialize properties
            _isCameraAvailable = false;
            _isCapturing = false;
            _hasCapturedPhoto = false;
            _isUploading = false;
            _uploadProgress = 0;

            Title = "Photo Capture";
        }

        #endregion

        #region Lifecycle Methods

        /// <summary>
        /// Initializes the ViewModel when navigated to.
        /// Checks camera permission and prepares for photo capture.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            try
            {
                // Check if camera permission is granted
                IsCameraAvailable = await PermissionHelper.CheckCameraPermissionAsync(_logger);

                if (!IsCameraAvailable)
                {
                    _logger.LogWarning("Camera permission not available on initialization");
                    SetError("Camera permission is required to capture photos");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during photo capture initialization");
                SetError($"Failed to initialize camera: {ex.Message}");
            }
        }

        /// <summary>
        /// Called when the page using this ViewModel appears.
        /// Attempts to request camera permission if not already granted.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task OnAppearing()
        {
            if (!IsCameraAvailable)
            {
                // Try requesting camera permission
                bool granted = await RequestCameraPermissionAsync();
                IsCameraAvailable = granted;

                if (!granted)
                {
                    _logger.LogWarning("User denied camera permission");
                    await DialogHelper.DisplayAlertAsync("Camera Required", 
                        "Camera permission is required to capture photos. Please grant this permission in settings.");
                }
            }
        }

        /// <summary>
        /// Called when the page using this ViewModel disappears.
        /// Performs any required cleanup.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task OnDisappearing()
        {
            // Clean up any resources when navigating away
            await Task.CompletedTask;
        }

        /// <summary>
        /// Disposes of resources used by the ViewModel.
        /// </summary>
        public override void Dispose()
        {
            // Unsubscribe from events to prevent memory leaks
            PhotoSyncService.UploadProgressChanged -= HandleUploadProgressChanged;
            
            base.Dispose();
        }

        #endregion

        #region Commands

        /// <summary>
        /// Command to capture a photo using the device camera.
        /// </summary>
        [RelayCommand]
        private async Task CapturePhotoAsync()
        {
            if (!IsCameraAvailable)
            {
                _logger.LogWarning("Attempted to capture photo without camera permission");
                await DialogHelper.DisplayAlertAsync("Camera Required", 
                    "Camera permission is required to capture photos.");
                return;
            }

            if (IsCapturing)
            {
                return; // Prevent multiple concurrent capture attempts
            }

            IsCapturing = true;
            ClearCapturedPhoto();

            try
            {
                await ExecuteWithBusyIndicator(async () =>
                {
                    _logger.LogInformation("Initiating photo capture");
                    
                    // Capture photo using the photo service
                    CapturedPhoto = await PhotoService.CapturePhotoAsync();
                    
                    if (CapturedPhoto != null)
                    {
                        _logger.LogInformation($"Photo captured successfully: {CapturedPhoto.Id}");
                        
                        // Convert file to image source for preview
                        var stream = await PhotoService.GetPhotoFileAsync(CapturedPhoto.Id);
                        if (stream != null)
                        {
                            PreviewImage = ImageSource.FromStream(() => stream);
                            HasCapturedPhoto = true;
                        }
                        else
                        {
                            _logger.LogWarning($"Failed to load preview for photo {CapturedPhoto.Id}");
                            throw new InvalidOperationException("Failed to load photo preview");
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Photo capture returned null");
                        throw new InvalidOperationException("Failed to capture photo");
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing photo");
                await DialogHelper.DisplayErrorAsync($"Failed to capture photo: {ex.Message}");
            }
            finally
            {
                IsCapturing = false;
            }
        }

        /// <summary>
        /// Command to accept the captured photo and initiate upload.
        /// </summary>
        [RelayCommand]
        private async Task AcceptPhotoAsync()
        {
            if (!HasCapturedPhoto || CapturedPhoto == null)
            {
                return;
            }

            IsUploading = true;
            UploadProgress = 0;

            try
            {
                await ExecuteWithBusyIndicator(async () =>
                {
                    _logger.LogInformation($"Accepting photo {CapturedPhoto.Id} and initiating upload");
                    
                    // Start the upload in the background
                    // The upload progress will be tracked via the UploadProgressChanged event
                    await PhotoSyncService.UploadPhotoAsync(CapturedPhoto.Id);
                    
                    // Navigate to the photo detail page
                    Dictionary<string, object> parameters = new Dictionary<string, object>
                    {
                        { NavigationConstants.ParamPhotoId, CapturedPhoto.Id }
                    };
                    
                    await NavigationService.NavigateToAsync(NavigationConstants.PhotoDetailPage, parameters);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error accepting photo {CapturedPhoto?.Id}");
                await DialogHelper.DisplayErrorAsync($"Failed to process photo: {ex.Message}");
                IsUploading = false;
            }
        }

        /// <summary>
        /// Command to discard the current photo and prepare to capture a new one.
        /// </summary>
        [RelayCommand]
        private async Task RetakePhotoAsync()
        {
            ClearCapturedPhoto();
            ClearError();
            await Task.CompletedTask;
        }

        /// <summary>
        /// Command to navigate to the photo detail page for the captured photo.
        /// </summary>
        [RelayCommand]
        private async Task ViewPhotoDetailsAsync()
        {
            if (!HasCapturedPhoto || CapturedPhoto == null)
            {
                return;
            }

            Dictionary<string, object> parameters = new Dictionary<string, object>
            {
                { NavigationConstants.ParamPhotoId, CapturedPhoto.Id }
            };
            
            await NavigationService.NavigateToAsync(NavigationConstants.PhotoDetailPage, parameters);
        }

        /// <summary>
        /// Command to navigate back to the previous screen.
        /// </summary>
        [RelayCommand]
        private async Task GoBackAsync()
        {
            await NavigationService.NavigateBackAsync();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Handles the upload progress changed event.
        /// Updates the UI with the current upload progress.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments containing upload progress information.</param>
        private void HandleUploadProgressChanged(object sender, PhotoUploadProgress e)
        {
            // Make sure we're tracking the progress for our captured photo
            if (CapturedPhoto == null || e.Id != CapturedPhoto.Id)
            {
                return;
            }

            UploadProgress = e.Progress;

            if (e.IsCompleted())
            {
                _logger.LogInformation($"Photo {e.Id} upload completed successfully");
                IsUploading = false;
            }
            else if (e.IsError())
            {
                _logger.LogError($"Photo {e.Id} upload failed: {e.ErrorMessage}");
                IsUploading = false;
                
                // Display error message on UI thread
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await DialogHelper.DisplayErrorAsync($"Upload failed: {e.ErrorMessage}");
                });
            }
        }

        /// <summary>
        /// Requests camera permission from the user.
        /// </summary>
        /// <returns>True if permission granted, false otherwise.</returns>
        private async Task<bool> RequestCameraPermissionAsync()
        {
            try
            {
                return await PermissionHelper.RequestCameraPermissionAsync(true, _logger);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting camera permission");
                return false;
            }
        }

        /// <summary>
        /// Clears the currently captured photo.
        /// </summary>
        private void ClearCapturedPhoto()
        {
            CapturedPhoto = null;
            PreviewImage = null;
            HasCapturedPhoto = false;
            IsUploading = false;
            UploadProgress = 0;
        }

        #endregion
    }
}