using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using SecurityPatrol.Constants;
using SecurityPatrol.Helpers;

namespace SecurityPatrol.Helpers
{
    /// <summary>
    /// Helper class that provides camera functionality for capturing photos in the Security Patrol application
    /// </summary>
    public class CameraHelper
    {
        private readonly ILogger<CameraHelper> _logger;
        private readonly string _tempPhotoDirectory;

        /// <summary>
        /// Initializes a new instance of the CameraHelper class with required dependencies
        /// </summary>
        /// <param name="logger">Logger for recording camera operations and errors</param>
        public CameraHelper(ILogger<CameraHelper> logger)
        {
            _logger = logger;
            
            // Set up the temporary directory for photo storage
            _tempPhotoDirectory = Path.Combine(FileSystem.CacheDirectory, "TempPhotos");
            
            // Ensure the directory exists
            if (!Directory.Exists(_tempPhotoDirectory))
            {
                Directory.CreateDirectory(_tempPhotoDirectory);
            }
        }
        
        /// <summary>
        /// Captures a photo using the device camera
        /// </summary>
        /// <returns>A stream containing the captured photo data, or null if capture failed</returns>
        public async Task<Stream> CapturePhotoAsync()
        {
            try
            {
                _logger.LogInformation("Starting photo capture operation");
                
                // Check camera permission first
                bool hasPermission = await CheckCameraPermissionAsync();
                if (!hasPermission)
                {
                    _logger.LogWarning("Cannot capture photo: Camera permission not granted");
                    return null;
                }

                // Capture photo using MediaPicker
                FileResult photo = await MediaPicker.CapturePhotoAsync(new MediaPickerOptions
                {
                    Title = "Take Patrol Photo"
                });

                // Check if photo was captured (could be null if user cancelled)
                if (photo == null)
                {
                    _logger.LogInformation("Photo capture cancelled by user");
                    return null;
                }

                // Open and return the photo stream
                Stream photoStream = await photo.OpenReadAsync();
                _logger.LogInformation($"Photo captured successfully: {photo.FileName}");
                
                return photoStream;
            }
            catch (FeatureNotSupportedException ex)
            {
                _logger.LogError(ex, "Camera not supported on this device");
                await DialogHelper.DisplayErrorAsync("Your device doesn't support camera functionality");
                return null;
            }
            catch (PermissionException ex)
            {
                _logger.LogError(ex, "Permission issue when accessing camera");
                await DialogHelper.DisplayErrorAsync("Camera permission is required to take photos");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing photo");
                await DialogHelper.DisplayErrorAsync("An error occurred while capturing the photo");
                return null;
            }
        }

        /// <summary>
        /// Checks if camera permission is granted
        /// </summary>
        /// <returns>True if camera permission is granted, false otherwise</returns>
        public async Task<bool> CheckCameraPermissionAsync()
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
        /// Requests camera permission from the user
        /// </summary>
        /// <returns>True if camera permission is granted, false otherwise</returns>
        public async Task<bool> RequestCameraPermissionAsync()
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

        /// <summary>
        /// Saves a photo stream to a temporary file
        /// </summary>
        /// <param name="photoStream">The stream containing the photo data</param>
        /// <returns>The path to the saved temporary file, or null if save failed</returns>
        public async Task<string> SavePhotoToTempFileAsync(Stream photoStream)
        {
            if (photoStream == null)
            {
                _logger.LogWarning("Cannot save photo: Stream is null");
                return null;
            }

            try
            {
                string filePath = GenerateTempFilePath();
                
                using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    await photoStream.CopyToAsync(fileStream);
                }
                
                _logger.LogInformation($"Photo saved to temporary file: {filePath}");
                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving photo to temporary file");
                await DialogHelper.DisplayErrorAsync("Failed to save the photo");
                return null;
            }
        }

        /// <summary>
        /// Opens a photo file as a stream
        /// </summary>
        /// <param name="filePath">The path to the photo file</param>
        /// <returns>A stream containing the photo data, or null if file could not be opened</returns>
        public async Task<Stream> GetPhotoStreamFromFileAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                _logger.LogWarning("Cannot get photo stream: File path is null or empty");
                return null;
            }

            try
            {
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning($"Photo file not found: {filePath}");
                    return null;
                }
                
                Stream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                _logger.LogInformation($"Photo file opened successfully: {filePath}");
                
                return fileStream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error opening photo file: {filePath}");
                return null;
            }
        }

        /// <summary>
        /// Deletes temporary photo files older than the specified age
        /// </summary>
        /// <param name="maxAge">The maximum age of files to keep</param>
        /// <returns>The number of files deleted</returns>
        public async Task<int> CleanupTempFilesAsync(TimeSpan maxAge)
        {
            try
            {
                _logger.LogInformation($"Cleaning up temporary photo files older than {maxAge.TotalHours} hours");
                
                DirectoryInfo dirInfo = new DirectoryInfo(_tempPhotoDirectory);
                FileInfo[] files = dirInfo.GetFiles("*.jpg");
                
                int deletedCount = 0;
                DateTime cutoffTime = DateTime.Now.Subtract(maxAge);
                
                foreach (FileInfo file in files)
                {
                    if (file.CreationTime < cutoffTime)
                    {
                        file.Delete();
                        deletedCount++;
                    }
                }
                
                _logger.LogInformation($"Deleted {deletedCount} temporary photo files");
                return deletedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up temporary photo files");
                return 0;
            }
        }

        /// <summary>
        /// Generates a unique temporary file path for a photo
        /// </summary>
        /// <returns>A unique file path in the temporary directory</returns>
        private string GenerateTempFilePath()
        {
            // Create a unique filename using GUID and timestamp
            string filename = $"photo_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N").Substring(0, 8)}.jpg";
            
            // Combine with temporary directory path
            return Path.Combine(_tempPhotoDirectory, filename);
        }
    }
}