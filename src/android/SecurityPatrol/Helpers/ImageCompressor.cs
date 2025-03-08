using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging; // v8.0+
using SkiaSharp; // v2.88.3

namespace SecurityPatrol.Helpers
{
    /// <summary>
    /// Helper class that provides image compression functionality to reduce the size of captured photos 
    /// while maintaining acceptable quality.
    /// </summary>
    public class ImageCompressor
    {
        private readonly ILogger<ImageCompressor> _logger;
        
        /// <summary>
        /// Default quality value for JPEG compression (0-100 scale where 100 is highest quality)
        /// </summary>
        public int DefaultQuality { get; } = 80;
        
        /// <summary>
        /// Default maximum width for image resizing
        /// </summary>
        public int DefaultMaxWidth { get; } = 1920;
        
        /// <summary>
        /// Default maximum height for image resizing
        /// </summary>
        public int DefaultMaxHeight { get; } = 1080;

        /// <summary>
        /// Initializes a new instance of the ImageCompressor class with the specified logger.
        /// </summary>
        /// <param name="logger">The logger instance for recording compression operations</param>
        public ImageCompressor(ILogger<ImageCompressor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Compresses an image stream to reduce its size while maintaining acceptable quality.
        /// </summary>
        /// <param name="imageStream">The input image stream to compress</param>
        /// <param name="quality">Optional quality setting (0-100, default is DefaultQuality)</param>
        /// <param name="maxWidth">Optional maximum width (default is DefaultMaxWidth)</param>
        /// <param name="maxHeight">Optional maximum height (default is DefaultMaxHeight)</param>
        /// <returns>A task that returns a stream containing the compressed image data</returns>
        public async Task<Stream> CompressImageAsync(Stream imageStream, int? quality = null, int? maxWidth = null, int? maxHeight = null)
        {
            _logger.LogInformation("Starting image compression");
            
            if (imageStream == null)
            {
                _logger.LogWarning("Null image stream provided for compression");
                return null;
            }

            // Use default values if parameters are not provided
            int qualityValue = quality ?? DefaultQuality;
            int maxWidthValue = maxWidth ?? DefaultMaxWidth;
            int maxHeightValue = maxHeight ?? DefaultMaxHeight;

            try
            {
                double originalSize = GetStreamSizeInKB(imageStream);
                _logger.LogDebug("Original image size: {OriginalSize} KB", originalSize);

                // Reset the position of the input stream to the beginning
                imageStream.Position = 0;

                // Load the image from the stream
                using var bitmap = SKBitmap.Decode(imageStream);
                
                if (bitmap == null)
                {
                    _logger.LogWarning("Failed to decode image for compression");
                    imageStream.Position = 0;
                    return imageStream;
                }

                // Calculate new dimensions
                var (newWidth, newHeight) = CalculateNewDimensions(bitmap.Width, bitmap.Height, maxWidthValue, maxHeightValue);
                
                // Check if we need to resize
                SKBitmap resizedBitmap = bitmap;
                bool needsResize = newWidth != bitmap.Width || newHeight != bitmap.Height;
                
                if (needsResize)
                {
                    _logger.LogDebug("Resizing image from {OriginalWidth}x{OriginalHeight} to {NewWidth}x{NewHeight}",
                        bitmap.Width, bitmap.Height, newWidth, newHeight);
                        
                    resizedBitmap = bitmap.Resize(new SKImageInfo(newWidth, newHeight), SKFilterQuality.High);
                }

                // Create an image from the bitmap and encode it as JPEG
                using SKImage image = SKImage.FromBitmap(resizedBitmap);
                using SKData data = image.Encode(SKEncodedImageFormat.Jpeg, qualityValue);
                
                // Clean up resized bitmap if we created a new one
                if (needsResize && resizedBitmap != bitmap)
                {
                    resizedBitmap.Dispose();
                }

                // Create a memory stream with the compressed data
                var resultStream = new MemoryStream();
                await Task.Run(() => {
                    using var stream = data.AsStream();
                    stream.CopyTo(resultStream);
                });

                resultStream.Position = 0;
                double compressedSize = GetStreamSizeInKB(resultStream);
                
                _logger.LogInformation("Image compressed: {OriginalSize:F2} KB -> {CompressedSize:F2} KB (Reduced by {ReductionPercentage}%)",
                    originalSize, compressedSize, (int)((1 - (compressedSize / originalSize)) * 100));
                
                resultStream.Position = 0;
                return resultStream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error compressing image: {Message}", ex.Message);
                
                // Reset the original stream and return it
                imageStream.Position = 0;
                return imageStream;
            }
        }

        /// <summary>
        /// Calculates new dimensions for an image while maintaining its aspect ratio.
        /// </summary>
        /// <param name="originalWidth">The original width of the image</param>
        /// <param name="originalHeight">The original height of the image</param>
        /// <param name="maxWidth">The maximum width constraint</param>
        /// <param name="maxHeight">The maximum height constraint</param>
        /// <returns>A tuple containing the new width and height</returns>
        private (int width, int height) CalculateNewDimensions(int originalWidth, int originalHeight, int maxWidth, int maxHeight)
        {
            // If image is already smaller than the max dimensions, keep original size
            if (originalWidth <= maxWidth && originalHeight <= maxHeight)
            {
                return (originalWidth, originalHeight);
            }

            // Calculate the aspect ratio
            double aspectRatio = (double)originalWidth / originalHeight;

            // Calculate potential new dimensions
            int newWidth = maxWidth;
            int newHeight = (int)(newWidth / aspectRatio);

            // If the new height exceeds the max height, scale based on height instead
            if (newHeight > maxHeight)
            {
                newHeight = maxHeight;
                newWidth = (int)(newHeight * aspectRatio);
            }

            return (newWidth, newHeight);
        }

        /// <summary>
        /// Gets the size of a stream in kilobytes.
        /// </summary>
        /// <param name="stream">The stream to measure</param>
        /// <returns>The size of the stream in kilobytes</returns>
        private double GetStreamSizeInKB(Stream stream)
        {
            long originalPosition = stream.Position;
            double sizeInKB = stream.Length / 1024.0;
            stream.Position = originalPosition;
            return sizeInKB;
        }
    }
}