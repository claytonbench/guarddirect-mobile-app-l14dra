using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.Core.Models;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SecurityPatrol.Infrastructure.Services
{
    /// <summary>
    /// Implementation of the IStorageService interface that provides file storage operations for the Security Patrol application.
    /// </summary>
    public class StorageService : IStorageService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<StorageService> _logger;
        private readonly string _storageBasePath;

        /// <summary>
        /// Initializes a new instance of the StorageService class with required dependencies.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="logger">The logger for this service.</param>
        public StorageService(IConfiguration configuration, ILogger<StorageService> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Get storage path from configuration or use a default path
            _storageBasePath = _configuration["Storage:BasePath"] ?? 
                Path.Combine(Directory.GetCurrentDirectory(), "FileStorage");
            
            // Ensure the base storage directory exists
            if (!Directory.Exists(_storageBasePath))
            {
                Directory.CreateDirectory(_storageBasePath);
                _logger.LogInformation("Created storage directory at {StorageBasePath}", _storageBasePath);
            }
        }

        /// <summary>
        /// Stores a file in the storage system.
        /// </summary>
        /// <param name="fileStream">The stream containing the file content to store.</param>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="contentType">The MIME content type of the file.</param>
        /// <returns>A result containing the file path or identifier if successful.</returns>
        public async Task<Result<string>> StoreFileAsync(Stream fileStream, string fileName, string contentType)
        {
            try
            {
                if (fileStream == null || !fileStream.CanRead)
                {
                    return Result.Failure<string>("File stream is null or cannot be read");
                }

                if (string.IsNullOrEmpty(fileName))
                {
                    return Result.Failure<string>("File name cannot be null or empty");
                }

                if (string.IsNullOrEmpty(contentType))
                {
                    return Result.Failure<string>("Content type cannot be null or empty");
                }

                // Create a unique file path
                string filePath = Path.Combine(_storageBasePath, fileName);
                
                // Ensure the directory structure exists
                string directoryPath = Path.GetDirectoryName(filePath);
                EnsureDirectoryExists(directoryPath);

                // Create or overwrite the file
                using (var fileOutput = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    await fileStream.CopyToAsync(fileOutput);
                }

                _logger.LogInformation("File stored successfully at {FilePath}", filePath);
                return Result.Success(filePath, "File stored successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing file {FileName}: {ErrorMessage}", fileName, ex.Message);
                return Result.Failure<string>($"Error storing file: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves a file from the storage system.
        /// </summary>
        /// <param name="filePath">The path or identifier of the file to retrieve.</param>
        /// <returns>A result containing the file stream if successful.</returns>
        public async Task<Result<Stream>> GetFileAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    return Result.Failure<Stream>("File path cannot be null or empty");
                }

                if (!await FileExistsAsync(filePath))
                {
                    return Result.Failure<Stream>("File not found");
                }

                // Open the file for reading
                var fileStream = File.OpenRead(filePath);
                
                // Create a memory stream to copy the file contents
                var memoryStream = new MemoryStream();
                await fileStream.CopyToAsync(memoryStream);
                
                // Close the file stream now that we've copied the data to memory
                fileStream.Close();
                
                // Reset the memory stream position to the beginning
                memoryStream.Position = 0;

                _logger.LogInformation("File retrieved successfully from {FilePath}", filePath);
                return Result.Success<Stream>(memoryStream, "File retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving file from {FilePath}: {ErrorMessage}", filePath, ex.Message);
                return Result.Failure<Stream>($"Error retrieving file: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes a file from the storage system.
        /// </summary>
        /// <param name="filePath">The path or identifier of the file to delete.</param>
        /// <returns>A result indicating success or failure of the delete operation.</returns>
        public async Task<Result> DeleteFileAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    return Result.Failure("File path cannot be null or empty");
                }

                if (!await FileExistsAsync(filePath))
                {
                    return Result.Failure("File not found");
                }

                File.Delete(filePath);
                
                _logger.LogInformation("File deleted successfully from {FilePath}", filePath);
                return Result.Success("File deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file from {FilePath}: {ErrorMessage}", filePath, ex.Message);
                return Result.Failure($"Error deleting file: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if a file exists in the storage system.
        /// </summary>
        /// <param name="filePath">The path or identifier of the file to check.</param>
        /// <returns>True if the file exists, false otherwise.</returns>
        public Task<bool> FileExistsAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return Task.FromResult(false);
            }

            // File.Exists is synchronous but we wrap it in Task.FromResult to maintain the async interface
            return Task.FromResult(File.Exists(filePath));
        }

        /// <summary>
        /// Ensures that a directory exists, creating it if necessary.
        /// </summary>
        /// <param name="directoryPath">The directory path to ensure exists.</param>
        private void EnsureDirectoryExists(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                _logger.LogInformation("Created directory at {DirectoryPath}", directoryPath);
            }
        }
    }
}