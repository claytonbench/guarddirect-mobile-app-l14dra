using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using SecurityPatrol.MAUI.UnitTests.Setup;
using SecurityPatrol.Helpers;
using SecurityPatrol.TestCommon.Helpers;

namespace SecurityPatrol.MAUI.UnitTests.Helpers
{
    public class ImageCompressorTests : TestBase
    {
        private readonly Mock<ILogger<ImageCompressor>> _mockLogger;
        private readonly ImageCompressor _imageCompressor;

        public ImageCompressorTests()
        {
            _mockLogger = new Mock<ILogger<ImageCompressor>>();
            _imageCompressor = new ImageCompressor(_mockLogger.Object);
        }

        [Fact]
        public async Task CompressImageAsync_WithValidImage_ReducesImageSize()
        {
            // Arrange
            var testImage = await TestImageGenerator.GenerateTestImageOfSizeAsync(500);
            double originalSize = GetStreamSizeInKB(testImage);
            
            // Act
            var compressedStream = await _imageCompressor.CompressImageAsync(testImage);
            double compressedSize = GetStreamSizeInKB(compressedStream);
            
            // Assert
            compressedSize.Should().BeLessThan(originalSize);
            _mockLogger.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Image compressed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), 
                Times.Once);
        }

        [Fact]
        public async Task CompressImageAsync_WithDifferentQualityLevels_AffectsOutputSize()
        {
            // Arrange
            var testImage = await TestImageGenerator.GenerateTestImageOfSizeAsync(500);
            
            // Act - compress with high quality
            var highQualityStream = await _imageCompressor.CompressImageAsync(testImage, quality: 90);
            double highQualitySize = GetStreamSizeInKB(highQualityStream);
            
            // Act - compress with low quality
            var lowQualityStream = await _imageCompressor.CompressImageAsync(testImage, quality: 50);
            double lowQualitySize = GetStreamSizeInKB(lowQualityStream);
            
            // Assert
            lowQualitySize.Should().BeLessThan(highQualitySize);
            _mockLogger.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Image compressed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), 
                Times.Exactly(2));
        }

        [Fact]
        public async Task CompressImageAsync_WithCustomDimensions_ResizesImage()
        {
            // Arrange
            var testImage = await TestImageGenerator.GenerateTestImageAsync(1920, 1080);
            int maxWidth = 800;
            int maxHeight = 600;
            
            // Act
            var resizedStream = await _imageCompressor.CompressImageAsync(testImage, maxWidth: maxWidth, maxHeight: maxHeight);
            
            // Analyze the resulting image
            resizedStream.Position = 0;
            var resultBitmap = SkiaSharp.SKBitmap.Decode(resizedStream);
            
            // Assert
            resultBitmap.Width.Should().BeLessThanOrEqualTo(maxWidth);
            resultBitmap.Height.Should().BeLessThanOrEqualTo(maxHeight);
            
            // Verify aspect ratio is maintained (within a small tolerance)
            double originalAspectRatio = 1920.0 / 1080.0;
            double newAspectRatio = (double)resultBitmap.Width / resultBitmap.Height;
            Math.Abs(originalAspectRatio - newAspectRatio).Should().BeLessThan(0.01);
            _mockLogger.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Image compressed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), 
                Times.Once);
        }

        [Fact]
        public async Task CompressImageAsync_WithNullStream_ThrowsArgumentNullException()
        {
            // Arrange & Act
            Func<Task> act = async () => await _imageCompressor.CompressImageAsync(null);
            
            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>();
            _mockLogger.Verify(l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Null image stream")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), 
                Times.Once);
        }

        [Fact]
        public async Task CompressImageAsync_WithInvalidImage_ReturnsOriginalStream()
        {
            // Arrange
            var invalidImageStream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 }); // Random bytes, not a valid image
            
            // Act
            var result = await _imageCompressor.CompressImageAsync(invalidImageStream);
            
            // Assert
            result.Should().BeSameAs(invalidImageStream);
            _mockLogger.Verify(l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Failed to decode image")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), 
                Times.Once);
        }

        [Fact]
        public void CalculateNewDimensions_MaintainsAspectRatio()
        {
            // Arrange
            int originalWidth = 1920;
            int originalHeight = 1080;
            int maxWidth = 800;
            int maxHeight = 600;
            
            // Act - using reflection to access private method
            var method = typeof(ImageCompressor).GetMethod("CalculateNewDimensions", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (ValueTuple<int, int>)method.Invoke(_imageCompressor, new object[] { originalWidth, originalHeight, maxWidth, maxHeight });
            
            // Assert
            double originalAspectRatio = (double)originalWidth / originalHeight;
            double newAspectRatio = (double)result.Item1 / result.Item2;
            
            Math.Abs(originalAspectRatio - newAspectRatio).Should().BeLessThan(0.01);
            result.Item1.Should().BeLessThanOrEqualTo(maxWidth);
            result.Item2.Should().BeLessThanOrEqualTo(maxHeight);
        }

        [Fact]
        public void CalculateNewDimensions_WithSmallerOriginal_RetainsOriginalSize()
        {
            // Arrange
            int originalWidth = 400;
            int originalHeight = 300;
            int maxWidth = 800;
            int maxHeight = 600;
            
            // Act - using reflection to access private method
            var method = typeof(ImageCompressor).GetMethod("CalculateNewDimensions", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (ValueTuple<int, int>)method.Invoke(_imageCompressor, new object[] { originalWidth, originalHeight, maxWidth, maxHeight });
            
            // Assert
            result.Item1.Should().Be(originalWidth);
            result.Item2.Should().Be(originalHeight);
        }

        [Fact]
        public void GetStreamSizeInKB_ReturnsCorrectSize()
        {
            // Arrange
            var stream = new MemoryStream(new byte[1024]); // 1 KB
            
            // Act - using reflection to access private method
            var method = typeof(ImageCompressor).GetMethod("GetStreamSizeInKB", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var size = (double)method.Invoke(_imageCompressor, new object[] { stream });
            
            // Assert
            size.Should().Be(1.0); // 1 KB
            stream.Position.Should().Be(0); // Verify that the stream position is reset
        }

        [Fact]
        public async Task CompressImageAsync_PreservesStreamPosition()
        {
            // Arrange
            var testImage = await TestImageGenerator.GenerateTestImageAsync();
            testImage.Position = 10; // Set a non-zero position
            long originalPosition = testImage.Position;
            
            // Act
            await _imageCompressor.CompressImageAsync(testImage);
            
            // Assert
            testImage.Position.Should().Be(originalPosition);
            _mockLogger.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Image compressed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), 
                Times.Once);
        }

        // Helper method to get stream size in KB
        private double GetStreamSizeInKB(Stream stream)
        {
            long originalPosition = stream.Position;
            double sizeInKB = stream.Length / 1024.0;
            stream.Position = originalPosition;
            return sizeInKB;
        }
    }
}