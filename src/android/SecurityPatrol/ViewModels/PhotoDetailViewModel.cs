using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using SecurityPatrol.Constants;
using SecurityPatrol.Models;
using SecurityPatrol.Services;

namespace SecurityPatrol.ViewModels
{
    /// <summary>
    /// ViewModel for the PhotoDetailPage that displays detailed information about a captured photo,
    /// including the image itself, metadata, and synchronization status.
    /// </summary>
    public class PhotoDetailViewModel : BaseViewModel
    {
        private readonly IPhotoService _photoService;
        private readonly IPhotoSyncService _photoSyncService;

        private string _photoId;
        /// <summary>
        /// Gets or sets the ID of the photo being displayed.
        /// </summary>
        public string PhotoId
        {
            get => _photoId;
            set => SetProperty(ref _photoId, value);
        }

        private PhotoModel _photo;
        /// <summary>
        /// Gets or sets the photo model being displayed.
        /// </summary>
        public PhotoModel Photo
        {
            get => _photo;
            set => SetProperty(ref _photo, value);
        }

        private ImageSource _photoImage;
        /// <summary>
        /// Gets or sets the image source for displaying the photo.
        /// </summary>
        public ImageSource PhotoImage
        {
            get => _photoImage;
            set => SetProperty(ref _photoImage, value);
        }

        private DateTime _captureDate;
        /// <summary>
        /// Gets or sets the date the photo was captured.
        /// </summary>
        public DateTime CaptureDate
        {
            get => _captureDate;
            set => SetProperty(ref _captureDate, value);
        }

        private string _locationText;
        /// <summary>
        /// Gets or sets the formatted location text where the photo was captured.
        /// </summary>
        public string LocationText
        {
            get => _locationText;
            set => SetProperty(ref _locationText, value);
        }

        private bool _isSynced;
        /// <summary>
        /// Gets or sets a value indicating whether the photo has been synchronized with the backend.
        /// </summary>
        public bool IsSynced
        {
            get => _isSynced;
            set => SetProperty(ref _isSynced, value);
        }

        private bool _isUploading;
        /// <summary>
        /// Gets or sets a value indicating whether the photo is currently being uploaded.
        /// </summary>
        public bool IsUploading
        {
            get => _isUploading;
            set => SetProperty(ref _isUploading, value);
        }

        private bool _hasUploadError;
        /// <summary>
        /// Gets or sets a value indicating whether there was an error during photo upload.
        /// </summary>
        public bool HasUploadError
        {
            get => _hasUploadError;
            set => SetProperty(ref _hasUploadError, value);
        }

        private int _uploadProgress;
        /// <summary>
        /// Gets or sets the upload progress percentage (0-100).
        /// </summary>
        public int UploadProgress
        {
            get => _uploadProgress;
            set => SetProperty(ref _uploadProgress, value);
        }

        private string _uploadStatus;
        /// <summary>
        /// Gets or sets the current upload status text.
        /// </summary>
        public string UploadStatus
        {
            get => _uploadStatus;
            set => SetProperty(ref _uploadStatus, value);
        }

        private string _uploadErrorMessage;
        /// <summary>
        /// Gets or sets the error message if upload failed.
        /// </summary>
        public string UploadErrorMessage
        {
            get => _uploadErrorMessage;
            set => SetProperty(ref _uploadErrorMessage, value);
        }

        /// <summary>
        /// Command to delete the current photo.
        /// </summary>
        public ICommand DeletePhotoCommand { get; }

        /// <summary>
        /// Command to retry uploading the photo if it previously failed.
        /// </summary>
        public ICommand RetryUploadCommand { get; }

        /// <summary>
        /// Command to cancel an ongoing photo upload.
        /// </summary>
        public ICommand CancelUploadCommand { get; }

        /// <summary>
        /// Command to navigate back to the previous page.
        /// </summary>
        public ICommand BackCommand { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PhotoDetailViewModel"/> class.
        /// </summary>
        /// <param name="navigationService">The navigation service.</param>
        /// <param name="authenticationStateProvider">The authentication state provider.</param>
        /// <param name="photoService">The photo service.</param>
        /// <param name="photoSyncService">The photo synchronization service.</param>
        public PhotoDetailViewModel(
            INavigationService navigationService,
            IAuthenticationStateProvider authenticationStateProvider,
            IPhotoService photoService,
            IPhotoSyncService photoSyncService)
            : base(navigationService, authenticationStateProvider)
        {
            _photoService = photoService ?? throw new ArgumentNullException(nameof(photoService));
            _photoSyncService = photoSyncService ?? throw new ArgumentNullException(nameof(photoSyncService));

            // Initialize commands
            DeletePhotoCommand = new AsyncRelayCommand(DeletePhotoAsync);
            RetryUploadCommand = new AsyncRelayCommand(RetryUploadAsync);
            CancelUploadCommand = new AsyncRelayCommand(CancelUploadAsync);
            BackCommand = new AsyncRelayCommand(GoBackAsync);

            // Subscribe to upload progress events
            _photoSyncService.UploadProgressChanged += HandleUploadProgressChanged;

            // Initialize properties
            Title = "Photo Details";
            _uploadProgress = 0;
            _uploadStatus = "Not uploaded";
            _isUploading = false;
            _hasUploadError = false;
            _isSynced = false;
        }

        /// <summary>
        /// Initializes the ViewModel by loading the photo and its details.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            if (string.IsNullOrEmpty(PhotoId))
            {
                return;
            }

            try
            {
                SetBusy(true);

                // Load the photo and its details
                Photo = await _photoService.GetPhotoAsync(PhotoId);
                if (Photo == null)
                {
                    SetError("Photo not found.");
                    return;
                }

                // Set photo metadata
                CaptureDate = Photo.Timestamp;
                LocationText = $"Lat: {Photo.Latitude:F6}, Long: {Photo.Longitude:F6}";
                IsSynced = Photo.IsSynced;

                // Load the photo image
                using (var photoStream = await _photoService.GetPhotoFileAsync(PhotoId))
                {
                    if (photoStream != null)
                    {
                        // Copy the stream data to a byte array
                        using (var memoryStream = new MemoryStream())
                        {
                            await photoStream.CopyToAsync(memoryStream);
                            byte[] imageData = memoryStream.ToArray();
                            
                            // Provide a new MemoryStream with the image data
                            PhotoImage = ImageSource.FromStream(() => new MemoryStream(imageData));
                        }
                    }
                    else
                    {
                        SetError("Failed to load photo image.");
                    }
                }

                // Update upload status
                await UpdateUploadStatus();
            }
            catch (Exception ex)
            {
                SetError($"Error loading photo: {ex.Message}");
            }
            finally
            {
                SetBusy(false);
            }
        }

        /// <summary>
        /// Called when the page using this ViewModel is navigated to.
        /// </summary>
        /// <param name="parameters">Navigation parameters.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public override async Task OnNavigatedTo(Dictionary<string, object> parameters = null)
        {
            if (parameters != null && parameters.TryGetValue(NavigationConstants.ParamPhotoId, out var photoIdObj))
            {
                PhotoId = photoIdObj.ToString();
            }

            await base.OnNavigatedTo(parameters);
        }

        /// <summary>
        /// Called when navigating away from the page using this ViewModel.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public override async Task OnNavigatedFrom()
        {
            await base.OnNavigatedFrom();

            // Unsubscribe from events
            _photoSyncService.UploadProgressChanged -= HandleUploadProgressChanged;
        }

        /// <summary>
        /// Called when the page appears on screen.
        /// </summary>
        public void OnAppearing()
        {
            // Update the upload status when the page appears
            _ = UpdateUploadStatus();
        }

        /// <summary>
        /// Called when the page disappears from screen.
        /// </summary>
        public void OnDisappearing()
        {
            // Perform any cleanup if needed
        }

        /// <summary>
        /// Updates the upload status and progress information for the photo.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task UpdateUploadStatus()
        {
            if (Photo == null || string.IsNullOrEmpty(PhotoId))
            {
                return;
            }

            try
            {
                var progress = await _photoSyncService.GetUploadProgressAsync(PhotoId);
                if (progress == null)
                {
                    IsUploading = false;
                    return;
                }

                UploadProgress = progress.Progress;
                UploadStatus = progress.Status;
                IsUploading = progress.IsInProgress();
                HasUploadError = progress.IsError();
                UploadErrorMessage = progress.ErrorMessage;
                
                // Update synced status (either from model or from completed progress)
                IsSynced = Photo.IsSynced || progress.IsCompleted();
            }
            catch (Exception ex)
            {
                SetError($"Error updating upload status: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles the UploadProgressChanged event from the PhotoSyncService.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="progress">The upload progress details.</param>
        private void HandleUploadProgressChanged(object sender, PhotoUploadProgress progress)
        {
            if (progress == null || progress.Id != PhotoId)
            {
                return;
            }

            UploadProgress = progress.Progress;
            UploadStatus = progress.Status;
            IsUploading = progress.IsInProgress();
            HasUploadError = progress.IsError();
            UploadErrorMessage = progress.ErrorMessage;
            
            // If upload completed, update the synced status
            if (progress.IsCompleted())
            {
                IsSynced = true;
            }
        }

        /// <summary>
        /// Deletes the current photo.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task DeletePhotoAsync()
        {
            if (Photo == null)
            {
                return;
            }

            // Ask for confirmation
            bool confirm = await Application.Current.MainPage.DisplayAlert(
                "Delete Photo", 
                "Are you sure you want to delete this photo?", 
                "Yes", "No");

            if (!confirm)
            {
                return;
            }

            await ExecuteWithBusyIndicator(async () =>
            {
                // If the photo is uploading, cancel the upload first
                if (IsUploading)
                {
                    await _photoSyncService.CancelUploadAsync(PhotoId);
                }

                // Delete the photo
                bool success = await _photoService.DeletePhotoAsync(PhotoId);
                if (success)
                {
                    // Navigate back to previous page
                    await NavigationService.NavigateBackAsync();
                }
                else
                {
                    throw new Exception("Failed to delete the photo.");
                }
            });
        }

        /// <summary>
        /// Retries uploading the photo if it previously failed.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task RetryUploadAsync()
        {
            if (Photo == null || IsSynced)
            {
                return;
            }

            await ExecuteWithBusyIndicator(async () =>
            {
                ClearError();
                bool success = await _photoSyncService.UploadPhotoAsync(PhotoId);
                if (success)
                {
                    await UpdateUploadStatus();
                }
                else
                {
                    throw new Exception("Failed to start photo upload.");
                }
            });
        }

        /// <summary>
        /// Cancels an ongoing photo upload.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task CancelUploadAsync()
        {
            if (Photo == null || !IsUploading)
            {
                return;
            }

            await ExecuteWithBusyIndicator(async () =>
            {
                await _photoSyncService.CancelUploadAsync(PhotoId);
                await UpdateUploadStatus();
            });
        }

        /// <summary>
        /// Navigates back to the previous page.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task GoBackAsync()
        {
            await NavigationService.NavigateBackAsync();
        }

        /// <summary>
        /// Releases resources used by the ViewModel.
        /// </summary>
        public override void Dispose()
        {
            // Unsubscribe from events
            _photoSyncService.UploadProgressChanged -= HandleUploadProgressChanged;
            
            base.Dispose();
        }
    }
}