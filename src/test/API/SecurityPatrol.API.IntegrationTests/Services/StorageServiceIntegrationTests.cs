using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using SecurityPatrol.API.IntegrationTests.Setup;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.TestCommon.Helpers;

namespace SecurityPatrol.API.IntegrationTests.Services
{
    /// <summary>
    /// Integration tests for the StorageService implementation to verify file storage operations.
    /// </summary>
    public class StorageServiceIntegrationTests : IntegrationTestBase
    {
        private readonly IStorageService _storageService;

        /// <summary>
        /// Initializes a new instance of the StorageServiceIntegrationTests class with the test factory.
        /// </summary>
        /// <param name="factory">The factory that creates a test server with configured services.</param>
        public StorageServiceIntegrationTests(CustomWebApplicationFactory factory)
            : base(factory)
        {
            _storageService = GetService<IStorageService>();
        }

        /// <summary>
        /// Tests that storing a valid image file returns a success result with a file path.
        /// </summary>
        [Fact]
        public async Task StoreFileAsync_WithValidImage_ShouldReturnSuccessResult()
        {
            // Arrange
            using var imageStream = await TestImageGenerator.GenerateTestImageAsync();
            var fileName = $"test-image-{Guid.NewGuid()}.jpg";
            var contentType = "image/jpeg";

            // Act
            var result = await _storageService.StoreFileAsync(imageStream, fileName, contentType);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNullOrEmpty();
            
            // Verify file exists
            var fileExists = await _storageService.FileExistsAsync(result.Data);
            fileExists.Should().BeTrue();
            
            // Cleanup
            await _storageService.DeleteFileAsync(result.Data);
        }

        /// <summary>
        /// Tests that attempting to store a null stream returns a failure result.
        /// </summary>
        [Fact]
        public async Task StoreFileAsync_WithNullStream_ShouldReturnFailureResult()
        {
            // Arrange
            var fileName = "test-image.jpg";
            var contentType = "image/jpeg";

            // Act
            var result = await _storageService.StoreFileAsync(null, fileName, contentType);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("null or invalid stream");
        }

        /// <summary>
        /// Tests that attempting to store a file with an empty file name returns a failure result.
        /// </summary>
        [Fact]
        public async Task StoreFileAsync_WithEmptyFileName_ShouldReturnFailureResult()
        {
            // Arrange
            using var imageStream = await TestImageGenerator.GenerateTestImageAsync();
            var fileName = string.Empty;
            var contentType = "image/jpeg";

            // Act
            var result = await _storageService.StoreFileAsync(imageStream, fileName, contentType);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("invalid file name");
        }

        /// <summary>
        /// Tests that retrieving an existing file returns a success result with the file stream.
        /// </summary>
        [Fact]
        public async Task GetFileAsync_WithExistingFile_ShouldReturnSuccessResult()
        {
            // Arrange
            using var imageStream = await TestImageGenerator.GenerateTestImageAsync();
            var fileName = $"test-image-{Guid.NewGuid()}.jpg";
            var contentType = "image/jpeg";
            
            var storeResult = await _storageService.StoreFileAsync(imageStream, fileName, contentType);
            storeResult.Succeeded.Should().BeTrue();
            var filePath = storeResult.Data;
            
            // Reset stream position for length comparison
            imageStream.Position = 0;
            var originalLength = imageStream.Length;

            // Act
            var result = await _storageService.GetFileAsync(filePath);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Length.Should().Be(originalLength);
            
            // Cleanup
            await _storageService.DeleteFileAsync(filePath);
        }

        /// <summary>
        /// Tests that attempting to retrieve a non-existing file returns a failure result.
        /// </summary>
        [Fact]
        public async Task GetFileAsync_WithNonExistingFile_ShouldReturnFailureResult()
        {
            // Arrange
            var nonExistingFilePath = $"/non-existing/file-{Guid.NewGuid()}.jpg";

            // Act
            var result = await _storageService.GetFileAsync(nonExistingFilePath);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("not found");
        }

        /// <summary>
        /// Tests that deleting an existing file returns a success result.
        /// </summary>
        [Fact]
        public async Task DeleteFileAsync_WithExistingFile_ShouldReturnSuccessResult()
        {
            // Arrange
            using var imageStream = await TestImageGenerator.GenerateTestImageAsync();
            var fileName = $"test-image-{Guid.NewGuid()}.jpg";
            var contentType = "image/jpeg";
            
            var storeResult = await _storageService.StoreFileAsync(imageStream, fileName, contentType);
            storeResult.Succeeded.Should().BeTrue();
            var filePath = storeResult.Data;

            // Act
            var result = await _storageService.DeleteFileAsync(filePath);

            // Assert
            result.Succeeded.Should().BeTrue();
            
            // Verify file no longer exists
            var fileExists = await _storageService.FileExistsAsync(filePath);
            fileExists.Should().BeFalse();
        }

        /// <summary>
        /// Tests that attempting to delete a non-existing file returns a failure result.
        /// </summary>
        [Fact]
        public async Task DeleteFileAsync_WithNonExistingFile_ShouldReturnFailureResult()
        {
            // Arrange
            var nonExistingFilePath = $"/non-existing/file-{Guid.NewGuid()}.jpg";

            // Act
            var result = await _storageService.DeleteFileAsync(nonExistingFilePath);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("not found");
        }

        /// <summary>
        /// Tests that checking if an existing file exists returns true.
        /// </summary>
        [Fact]
        public async Task FileExistsAsync_WithExistingFile_ShouldReturnTrue()
        {
            // Arrange
            using var imageStream = await TestImageGenerator.GenerateTestImageAsync();
            var fileName = $"test-image-{Guid.NewGuid()}.jpg";
            var contentType = "image/jpeg";
            
            var storeResult = await _storageService.StoreFileAsync(imageStream, fileName, contentType);
            storeResult.Succeeded.Should().BeTrue();
            var filePath = storeResult.Data;

            // Act
            var exists = await _storageService.FileExistsAsync(filePath);

            // Assert
            exists.Should().BeTrue();
            
            // Cleanup
            await _storageService.DeleteFileAsync(filePath);
        }

        /// <summary>
        /// Tests that checking if a non-existing file exists returns false.
        /// </summary>
        [Fact]
        public async Task FileExistsAsync_WithNonExistingFile_ShouldReturnFalse()
        {
            // Arrange
            var nonExistingFilePath = $"/non-existing/file-{Guid.NewGuid()}.jpg";

            // Act
            var exists = await _storageService.FileExistsAsync(nonExistingFilePath);

            // Assert
            exists.Should().BeFalse();
        }

        /// <summary>
        /// Tests that storing a large file is handled correctly.
        /// </summary>
        [Fact]
        public async Task StoreFileAsync_WithLargeFile_ShouldHandleCorrectly()
        {
            // Arrange
            using var largeImageStream = await TestImageGenerator.GenerateTestImageOfSizeAsync(5120); // ~5MB
            var fileName = $"large-test-image-{Guid.NewGuid()}.jpg";
            var contentType = "image/jpeg";

            // Act
            var storeResult = await _storageService.StoreFileAsync(largeImageStream, fileName, contentType);

            // Assert
            storeResult.Succeeded.Should().BeTrue();
            
            // Verify file exists
            var filePath = storeResult.Data;
            var fileExists = await _storageService.FileExistsAsync(filePath);
            fileExists.Should().BeTrue();
            
            // Verify file can be retrieved with correct size
            largeImageStream.Position = 0;
            var originalSize = largeImageStream.Length;
            
            var getResult = await _storageService.GetFileAsync(filePath);
            getResult.Succeeded.Should().BeTrue();
            getResult.Data.Length.Should().Be(originalSize);
            
            // Cleanup
            await _storageService.DeleteFileAsync(filePath);
        }
    }
}