using Microsoft.Extensions.Logging; // 8.0.0
using Microsoft.Maui.Controls; // 8.0.0
using System; // 8.0.0
using System.Threading.Tasks; // 8.0.0
using SecurityPatrol.ViewModels;
using SecurityPatrol.Helpers;

namespace SecurityPatrol.Views
{
    /// <summary>
    /// Page class that provides the UI for capturing photos within the Security Patrol application.
    /// Handles page lifecycle events and coordinates camera permissions with the PhotoCaptureViewModel.
    /// </summary>
    public partial class PhotoCapturePage : ContentPage
    {
        private readonly ILogger<PhotoCapturePage> _logger;
        
        /// <summary>
        /// Gets the ViewModel instance from the BindingContext.
        /// </summary>
        private PhotoCaptureViewModel ViewModel => BindingContext as PhotoCaptureViewModel;

        /// <summary>
        /// Initializes a new instance of the PhotoCapturePage class with required dependencies.
        /// </summary>
        /// <param name="logger">Logger for diagnostic information.</param>
        /// <exception cref="ArgumentNullException">Thrown if logger is null.</exception>
        public PhotoCapturePage(ILogger<PhotoCapturePage> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            InitializeComponent();
            
            _logger.LogInformation("PhotoCapturePage initialized");
        }

        /// <summary>
        /// Called when the page appears on screen. Initializes the ViewModel and checks camera permissions.
        /// </summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            
            _logger.LogInformation("PhotoCapturePage appearing");
            
            // Initialize the ViewModel
            if (ViewModel != null)
            {
                await ViewModel.OnAppearing();
            }
            else
            {
                _logger.LogWarning("ViewModel is null on page appearing");
            }
            
            // Check camera permission
            if (!await CheckCameraPermissionAsync())
            {
                await RequestCameraPermissionAsync();
            }
        }

        /// <summary>
        /// Called when the page disappears from screen. Performs cleanup on the ViewModel.
        /// </summary>
        protected override async void OnDisappearing()
        {
            base.OnDisappearing();
            
            _logger.LogInformation("PhotoCapturePage disappearing");
            
            // Clean up the ViewModel
            if (ViewModel != null)
            {
                await ViewModel.OnDisappearing();
            }
        }

        /// <summary>
        /// Checks if camera permission is granted using the PermissionHelper.
        /// </summary>
        /// <returns>True if camera permission is granted, false otherwise.</returns>
        private async Task<bool> CheckCameraPermissionAsync()
        {
            try
            {
                _logger.LogInformation("Checking camera permission");
                return await PermissionHelper.CheckCameraPermissionAsync(_logger);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking camera permission");
                return false;
            }
        }

        /// <summary>
        /// Requests camera permission from the user with a permission rationale dialog.
        /// </summary>
        /// <returns>True if camera permission is granted, false otherwise.</returns>
        private async Task<bool> RequestCameraPermissionAsync()
        {
            try
            {
                _logger.LogInformation("Requesting camera permission");
                return await PermissionHelper.RequestCameraPermissionAsync(true, _logger);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting camera permission");
                return false;
            }
        }
    }
}