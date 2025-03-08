using System; // Version 8.0.0
using System.IO; // Version 8.0.0
using System.Threading.Tasks; // Version 8.0.0
using Microsoft.Extensions.DependencyInjection; // Version 8.0.0
using Microsoft.Extensions.Logging; // Version 8.0.0
using Xunit; // Version 2.4.2
using Xunit.Abstractions; // Version 2.0.3
using FluentAssertions; // Version 6.11.0
using SecurityPatrol.MAUI.PerformanceTests.Setup;
using SecurityPatrol.TestCommon.Helpers;
using SecurityPatrol.Services;
using SecurityPatrol.Helpers;
using SecurityPatrol.Models;
using SecurityPatrol.TestCommon.Mocks;

namespace SecurityPatrol.MAUI.PerformanceTests
{
    /// <summary>
    /// Performance tests for photo processing operations in the Security Patrol application.
    /// Tests the performance characteristics of image compression, photo capture, storage, and retrieval operations under various conditions and device profiles.
    /// </summary>
    public class PhotoProcessingPerformanceTests : PerformanceTestBase
    {
        private IPhotoService _photoService;
        private ImageCompressor _imageCompressor;
        private const int SmallImageSizeKB = 100;
        private const int MediumImageSizeKB = 500;
        private const int LargeImageSizeKB = 2000;
        private const double MaxCompressionTimeMs = 3000;
        private const double MaxCaptureTimeMs = 1500;
        private const double MaxRetrievalTimeMs = 500;
        private const double MaxMemoryUsageMB = 150;

        /// <summary>
        /// Initializes a new instance of the PhotoProcessingPerformanceTests class with test output helper.
        /// </summary>
        /// <param name="outputHelper">The test output helper for writing test results.</param>
        public PhotoProcessingPerformanceTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
            // Call base constructor with outputHelper
            // Set MeasurementIterations to 5 for reliable performance metrics
            MeasurementIterations = 5;
        }

        /// <summary>
        /// Initializes the test environment with required services.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        [PublicAPI]
        public override async Task InitializeAsync()
        {
            // Await base.InitializeAsync() to initialize test environment
            await base.InitializeAsync();

            // Register and resolve services for testing
            _photoService = MockPhotoService.Create();
            _imageCompressor = ServiceProvider.GetService<ImageCompressor>();

            // Log successful initialization
            Logger.LogInformation("PhotoProcessingPerformanceTests initialized successfully");
        }

        /// <summary>
        /// Tests the performance of image compression for different image sizes.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        [PublicAPI]
        [Fact]
        public async Task ImageCompressionPerformanceTest()
        {
            // Generate small test image using TestImageGenerator.GenerateTestImageOfSizeAsync(SmallImageSizeKB)
            using var smallImageStream = await TestImageGenerator.GenerateTestImageOfSizeAsync(SmallImageSizeKB);

            // Measure execution time of compressing small image
            var (smallCompressionTime, smallCompressionMemory, _) = await CompressImageAsync(smallImageStream, 80);

            // Measure memory usage of compressing small image
            // Assert compression time is below threshold
            AssertPerformanceThreshold(smallCompressionTime, MaxCompressionTimeMs, "Small Image Compression Time");

            // Assert memory usage is below threshold
            AssertMemoryThreshold(smallCompressionMemory, MaxMemoryUsageMB, "Small Image Compression Memory");

            // Generate medium test image using TestImageGenerator.GenerateTestImageOfSizeAsync(MediumImageSizeKB)
            using var mediumImageStream = await TestImageGenerator.GenerateTestImageOfSizeAsync(MediumImageSizeKB);

            // Measure execution time of compressing medium image
            var (mediumCompressionTime, mediumCompressionMemory, _) = await CompressImageAsync(mediumImageStream, 80);

            // Measure memory usage of compressing medium image
            // Assert compression time is below threshold
            AssertPerformanceThreshold(mediumCompressionTime, MaxCompressionTimeMs, "Medium Image Compression Time");

            // Assert memory usage is below threshold
            AssertMemoryThreshold(mediumCompressionMemory, MaxMemoryUsageMB, "Medium Image Compression Memory");

            // Generate large test image using TestImageGenerator.GenerateTestImageOfSizeAsync(LargeImageSizeKB)
            using var largeImageStream = await TestImageGenerator.GenerateTestImageOfSizeAsync(LargeImageSizeKB);

            // Measure execution time of compressing large image
            var (largeCompressionTime, largeCompressionMemory, _) = await CompressImageAsync(largeImageStream, 80);

            // Measure memory usage of compressing large image
            // Assert compression time is below threshold
            AssertPerformanceThreshold(largeCompressionTime, MaxCompressionTimeMs, "Large Image Compression Time");

            // Assert memory usage is below threshold
            AssertMemoryThreshold(largeCompressionMemory, MaxMemoryUsageMB, "Large Image Compression Memory");

            // Log performance test results
            LogPerformanceResults();
        }

        /// <summary>
        /// Tests the performance impact of different compression quality settings.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        [PublicAPI]
        [Fact]
        public async Task CompressionQualityPerformanceTest()
        {
            // Generate medium test image using TestImageGenerator.GenerateTestImageOfSizeAsync(MediumImageSizeKB)
            using var imageStream = await TestImageGenerator.GenerateTestImageOfSizeAsync(MediumImageSizeKB);

            // Measure execution time of compressing with low quality (25)
            var (lowQualityTime, _, lowQualitySize) = await CompressImageAsync(imageStream, 25);

            // Reset stream position for next compression
            imageStream.Position = 0;

            // Measure execution time of compressing with medium quality (50)
            var (mediumQualityTime, _, mediumQualitySize) = await CompressImageAsync(imageStream, 50);

            // Reset stream position for next compression
            imageStream.Position = 0;

            // Measure execution time of compressing with high quality (75)
            var (highQualityTime, _, highQualitySize) = await CompressImageAsync(imageStream, 75);

            // Reset stream position for next compression
            imageStream.Position = 0;

            // Measure execution time of compressing with maximum quality (100)
            var (maxQualityTime, _, maxQualitySize) = await CompressImageAsync(imageStream, 100);

            // Compare compression times and file sizes at different quality levels
            Logger.LogInformation("Compression Quality Comparison:");
            Logger.LogInformation("Low Quality (25): Time = {Time} ms, Size = {Size} bytes", lowQualityTime, lowQualitySize);
            Logger.LogInformation("Medium Quality (50): Time = {Time} ms, Size = {Size} bytes", mediumQualityTime, mediumQualitySize);
            Logger.LogInformation("High Quality (75): Time = {Time} ms, Size = {Size} bytes", highQualityTime, highQualitySize);
            Logger.LogInformation("Maximum Quality (100): Time = {Time} ms, Size = {Size} bytes", maxQualityTime, maxQualitySize);

            // Assert all compression times are below threshold
            AssertPerformanceThreshold(lowQualityTime, MaxCompressionTimeMs, "Low Quality Compression Time");
            AssertPerformanceThreshold(mediumQualityTime, MaxCompressionTimeMs, "Medium Quality Compression Time");
            AssertPerformanceThreshold(highQualityTime, MaxCompressionTimeMs, "High Quality Compression Time");
            AssertPerformanceThreshold(maxQualityTime, MaxCompressionTimeMs, "Maximum Quality Compression Time");

            // Log performance test results with quality comparison
            LogPerformanceResults();
        }

        /// <summary>
        /// Tests the performance of the photo capture process.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        [PublicAPI]
        [Fact]
        public async Task PhotoCapturePerformanceTest()
        {
            // Setup mock camera helper to return test image
            // Measure execution time of CapturePhotoAsync operation
            var (captureTime, captureMemory, _) = await CompressImageAsync(await TestImageGenerator.GenerateTestImageOfSizeAsync(MediumImageSizeKB), 80);

            // Measure memory usage of CapturePhotoAsync operation
            // Assert capture time is below MaxCaptureTimeMs threshold
            AssertPerformanceThreshold(captureTime, MaxCaptureTimeMs, "Photo Capture Time");

            // Assert memory usage is below MaxMemoryUsageMB threshold
            AssertMemoryThreshold(captureMemory, MaxMemoryUsageMB, "Photo Capture Memory");

            // Log performance test results
            LogPerformanceResults();
        }

        /// <summary>
        /// Tests the performance of photo retrieval operations.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        [PublicAPI]
        [Fact]
        public async Task PhotoRetrievalPerformanceTest()
        {
            // Setup test environment with multiple stored photos
            var testPhotos = await SetupTestPhotosAsync(5);

            // Measure execution time of GetStoredPhotosAsync operation
            var getStoredPhotosTime = await MeasureExecutionTimeAsync(async () => await _photoService.GetStoredPhotosAsync(), "GetStoredPhotosAsync");

            // Measure execution time of GetPhotoAsync operation for specific photo
            var getPhotoTime = await MeasureExecutionTimeAsync(async () => await _photoService.GetPhotoAsync(testPhotos[0].Id), "GetPhotoAsync");

            // Measure execution time of GetPhotoFileAsync operation for specific photo
            var getPhotoFileTime = await MeasureExecutionTimeAsync(async () =>
            {
                using var stream = await _photoService.GetPhotoFileAsync(testPhotos[0].Id);
            }, "GetPhotoFileAsync");

            // Assert all retrieval times are below MaxRetrievalTimeMs threshold
            AssertPerformanceThreshold(getStoredPhotosTime, MaxRetrievalTimeMs, "GetStoredPhotosAsync Time");
            AssertPerformanceThreshold(getPhotoTime, MaxRetrievalTimeMs, "GetPhotoAsync Time");
            AssertPerformanceThreshold(getPhotoFileTime, MaxRetrievalTimeMs, "GetPhotoFileAsync Time");

            // Log performance test results
            LogPerformanceResults();
        }

        /// <summary>
        /// Tests photo processing performance under simulated low-resource conditions.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        [PublicAPI]
        [Fact]
        public async Task LowResourceEnvironmentTest()
        {
            // Call SimulateLowResourceEnvironment() to simulate low-end device
            SimulateLowResourceEnvironment();

            // Generate medium test image
            using var imageStream = await TestImageGenerator.GenerateTestImageOfSizeAsync(MediumImageSizeKB);

            // Measure execution time of compression operation in low-resource environment
            var (compressionTime, compressionMemory, _) = await CompressImageAsync(imageStream, 80);

            // Measure memory usage of compression operation in low-resource environment
            // Assert performance is acceptable even in low-resource conditions
            AssertPerformanceThreshold(compressionTime, MaxCompressionTimeMs * 1.5, "Low Resource Compression Time"); // Allow 50% more time
            AssertMemoryThreshold(compressionMemory, MaxMemoryUsageMB, "Low Resource Compression Memory");

            // Log performance test results with resource constraint information
            LogPerformanceResults();
        }

        /// <summary>
        /// Tests the performance of batch processing multiple photos.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        [PublicAPI]
        [Fact]
        public async Task BatchProcessingPerformanceTest()
        {
            // Generate multiple test images of different sizes
            int numberOfImages = 5;
            List<Stream> imageStreams = new List<Stream>();
            for (int i = 0; i < numberOfImages; i++)
            {
                int size = MediumImageSizeKB + (i * 50); // Vary sizes slightly
                imageStreams.Add(await TestImageGenerator.GenerateTestImageOfSizeAsync(size));
            }

            // Measure execution time of processing all images in sequence
            var batchTime = await MeasureExecutionTimeAsync(async () =>
            {
                foreach (var stream in imageStreams)
                {
                    using (stream)
                    {
                        await CompressImageAsync(stream, 80);
                    }
                }
            }, "BatchCompression");

            // Measure memory usage during batch processing
            var batchMemory = await MeasureMemoryUsageAsync(async () =>
            {
                foreach (var stream in imageStreams)
                {
                    using (stream)
                    {
                        await CompressImageAsync(stream, 80);
                    }
                }
            }, "BatchCompression");

            // Assert batch processing time scales linearly with number of images
            AssertPerformanceThreshold(batchTime, MaxCompressionTimeMs * numberOfImages * 1.2, "Batch Compression Time"); // Allow 20% overhead

            // Assert memory usage remains within acceptable limits during batch processing
            AssertMemoryThreshold(batchMemory, MaxMemoryUsageMB * 2, "Batch Compression Memory"); // Allow double memory usage

            // Log performance test results
            LogPerformanceResults();
        }

        /// <summary>
        /// Analyzes the compression ratio achieved for different types of images.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        [PublicAPI]
        [Fact]
        public async Task CompressionRatioAnalysisTest()
        {
            // Generate test images with different characteristics (solid colors, complex patterns, etc.)
            using var solidColorImage = await TestImageGenerator.GenerateTestImageAsync(500, 500, System.Drawing.Color.Red, System.Drawing.Color.Black, "Solid");
            using var complexPatternImage = await TestImageGenerator.GenerateTestImageAsync(500, 500, System.Drawing.Color.White, System.Drawing.Color.Black, "Complex Pattern");

            // Compress each image type and measure original vs. compressed size
            var (_, _, solidColorCompressedSize) = await CompressImageAsync(solidColorImage, 80);
            var (_, _, complexPatternCompressedSize) = await CompressImageAsync(complexPatternImage, 80);

            // Calculate compression ratios for each image type
            double solidColorOriginalSize = GetStreamSizeInKB(solidColorImage) * 1024;
            double complexPatternOriginalSize = GetStreamSizeInKB(complexPatternImage) * 1024;

            double solidColorRatio = solidColorCompressedSize / solidColorOriginalSize;
            double complexPatternRatio = complexPatternCompressedSize / complexPatternOriginalSize;

            // Assert minimum compression ratio is achieved for each image type
            Assert.True(solidColorRatio < 0.5, "Solid color compression ratio should be less than 0.5");
            Assert.True(complexPatternRatio < 0.8, "Complex pattern compression ratio should be less than 0.8");

            // Log compression ratio analysis results
            Logger.LogInformation("Compression Ratio Analysis:");
            Logger.LogInformation("Solid Color Image: Original Size = {OriginalSize} bytes, Compressed Size = {CompressedSize} bytes, Ratio = {Ratio:F2}",
                solidColorOriginalSize, solidColorCompressedSize, solidColorRatio);
            Logger.LogInformation("Complex Pattern Image: Original Size = {OriginalSize} bytes, Compressed Size = {CompressedSize} bytes, Ratio = {Ratio:F2}",
                complexPatternOriginalSize, complexPatternCompressedSize, complexPatternRatio);
        }

        /// <summary>
        /// Helper method to compress an image and measure performance metrics.
        /// </summary>
        /// <param name="imageStream">The image stream to compress.</param>
        /// <param name="quality">The compression quality (0-100).</param>
        /// <returns>Performance metrics and result size</returns>
        private async Task<(double executionTime, long memoryUsage, long resultSize)> CompressImageAsync(Stream imageStream, int quality)
        {
            // Reset image stream position to beginning
            imageStream.Position = 0;

            // Measure execution time of _imageCompressor.CompressImageAsync(imageStream, quality)
            double executionTime = await MeasureExecutionTimeAsync(async () =>
            {
                using var compressedStream = await _imageCompressor.CompressImageAsync(imageStream, quality);
            }, $"CompressImageAsync (Quality = {quality})");

            // Measure memory usage during compression
            long memoryUsage = await MeasureMemoryUsageAsync(async () =>
            {
                using var compressedStream = await _imageCompressor.CompressImageAsync(imageStream, quality);
            }, $"CompressImageAsync (Quality = {quality})");

            // Get the size of the compressed image stream
            long resultSize = 0;
            using (var compressedStream = await _imageCompressor.CompressImageAsync(imageStream, quality))
            {
                if (compressedStream != null)
                {
                    resultSize = compressedStream.Length;
                }
            }

            // Return tuple with execution time, memory usage, and result size
            return (executionTime, memoryUsage, resultSize);
        }

        /// <summary>
        /// Helper method to set up test photos in the repository.
        /// </summary>
        /// <param name="count">The number of photos to set up.</param>
        /// <returns>List of created photo models</returns>
        private async Task<List<PhotoModel>> SetupTestPhotosAsync(int count)
        {
            // Create list to store photo models
            var photoModels = new List<PhotoModel>();

            // For each count, generate test image
            for (int i = 0; i < count; i++)
            {
                // Generate test image
                using var imageStream = await TestImageGenerator.GenerateTestImageAsync();

                // Compress the test image
                using var compressedStream = await _imageCompressor.CompressImageAsync(imageStream, 80);

                // Mock the photo capture process to store the image
                var photo = await _photoService.CapturePhotoAsync();

                // Add the photo model to the list
                photoModels.Add(photo);
            }

            // Return the list of photo models
            return photoModels;
        }

        /// <summary>
        /// Gets the size of a stream in kilobytes.
        /// </summary>
        /// <param name="stream">The stream to measure</param>
        /// <returns>The size of the stream in kilobytes</returns>
        private double GetStreamSizeInKB(Stream stream)
        {
            long originalPosition = stream.Position;
            stream.Position = 0;
            double sizeInKB = stream.Length / 1024.0;
            stream.Position = originalPosition;
            return sizeInKB;
        }
    }
}