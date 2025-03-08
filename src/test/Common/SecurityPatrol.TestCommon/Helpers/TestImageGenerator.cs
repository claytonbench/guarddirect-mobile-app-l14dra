using System;
using System.IO;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;

namespace SecurityPatrol.TestCommon.Helpers
{
    /// <summary>
    /// Utility class for generating test images with configurable properties for use in unit and integration tests
    /// </summary>
    public static class TestImageGenerator
    {
        /// <summary>
        /// Default width for generated test images
        /// </summary>
        public static int DefaultWidth { get; private set; }

        /// <summary>
        /// Default height for generated test images
        /// </summary>
        public static int DefaultHeight { get; private set; }

        /// <summary>
        /// Default background color for generated test images
        /// </summary>
        public static Color DefaultBackgroundColor { get; private set; }

        /// <summary>
        /// Default text color for generated test images
        /// </summary>
        public static Color DefaultTextColor { get; private set; }

        /// <summary>
        /// Default text to draw on generated test images
        /// </summary>
        public static string DefaultText { get; private set; }

        /// <summary>
        /// Default image format for generated test images
        /// </summary>
        public static ImageFormat DefaultImageFormat { get; private set; }

        /// <summary>
        /// Static constructor that initializes default values for image generation
        /// </summary>
        static TestImageGenerator()
        {
            DefaultWidth = 640;
            DefaultHeight = 480;
            DefaultBackgroundColor = Color.LightBlue;
            DefaultTextColor = Color.Black;
            DefaultText = "Test Image";
            DefaultImageFormat = ImageFormat.Jpeg;
        }

        /// <summary>
        /// Generates a test image with default properties and returns it as a memory stream
        /// </summary>
        /// <returns>A memory stream containing the generated test image</returns>
        public static async Task<MemoryStream> GenerateTestImageAsync()
        {
            return await GenerateTestImageAsync(DefaultWidth, DefaultHeight, DefaultBackgroundColor, DefaultTextColor, DefaultText);
        }

        /// <summary>
        /// Generates a test image with specified width and height and returns it as a memory stream
        /// </summary>
        /// <param name="width">Width of the image in pixels</param>
        /// <param name="height">Height of the image in pixels</param>
        /// <returns>A memory stream containing the generated test image</returns>
        public static async Task<MemoryStream> GenerateTestImageAsync(int width, int height)
        {
            return await GenerateTestImageAsync(width, height, DefaultBackgroundColor, DefaultTextColor, DefaultText);
        }

        /// <summary>
        /// Generates a test image with specified properties and returns it as a memory stream
        /// </summary>
        /// <param name="width">Width of the image in pixels</param>
        /// <param name="height">Height of the image in pixels</param>
        /// <param name="backgroundColor">Background color of the image</param>
        /// <param name="textColor">Color of the text overlay</param>
        /// <param name="text">Text to display in the image</param>
        /// <returns>A memory stream containing the generated test image</returns>
        public static async Task<MemoryStream> GenerateTestImageAsync(int width, int height, Color backgroundColor, Color textColor, string text)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentException("Width and height must be positive values");

            Bitmap bitmap = new Bitmap(width, height);
            Graphics graphics = Graphics.FromImage(bitmap);
            
            // Fill background
            graphics.Clear(backgroundColor);
            
            // Draw text
            using (Font font = new Font("Arial", 16, FontStyle.Bold))
            {
                SizeF textSize = graphics.MeasureString(text, font);
                float x = (width - textSize.Width) / 2;
                float y = (height - textSize.Height) / 2;
                
                using (Brush brush = new SolidBrush(textColor))
                {
                    graphics.DrawString(text, font, brush, x, y);
                }
            }
            
            // Save to memory stream
            MemoryStream stream = new MemoryStream();
            bitmap.Save(stream, DefaultImageFormat);
            stream.Position = 0; // Reset position to beginning of stream
            
            // Clean up
            graphics.Dispose();
            bitmap.Dispose();
            
            return await Task.FromResult(stream);
        }

        /// <summary>
        /// Generates a test image with a timestamp overlay and returns it as a memory stream
        /// </summary>
        /// <param name="timestamp">Timestamp to display in the image (uses current time if null)</param>
        /// <returns>A memory stream containing the generated test image with timestamp</returns>
        public static async Task<MemoryStream> GenerateTestImageWithTimestampAsync(DateTime? timestamp = null)
        {
            DateTime actualTimestamp = timestamp ?? DateTime.Now;
            string formattedTimestamp = actualTimestamp.ToString("yyyy-MM-dd HH:mm:ss");
            
            return await GenerateTestImageAsync(DefaultWidth, DefaultHeight, DefaultBackgroundColor, DefaultTextColor, formattedTimestamp);
        }

        /// <summary>
        /// Generates a test image with metadata text (user ID, location) and returns it as a memory stream
        /// </summary>
        /// <param name="userId">User ID to include in the metadata text</param>
        /// <param name="latitude">Latitude value to include in the metadata text</param>
        /// <param name="longitude">Longitude value to include in the metadata text</param>
        /// <returns>A memory stream containing the generated test image with metadata</returns>
        public static async Task<MemoryStream> GenerateTestImageWithMetadataAsync(string userId, double latitude, double longitude)
        {
            string metadataText = $"User: {userId}\nLocation: {latitude}, {longitude}\nTime: {DateTime.Now}";
            
            return await GenerateTestImageAsync(DefaultWidth, DefaultHeight, DefaultBackgroundColor, DefaultTextColor, metadataText);
        }

        /// <summary>
        /// Generates a test image with random background color and returns it as a memory stream
        /// </summary>
        /// <returns>A memory stream containing the generated test image with random color</returns>
        public static async Task<MemoryStream> GenerateRandomColoredTestImageAsync()
        {
            Random random = new Random();
            Color randomColor = Color.FromArgb(
                random.Next(256),  // R
                random.Next(256),  // G
                random.Next(256)   // B
            );
            
            return await GenerateTestImageAsync(DefaultWidth, DefaultHeight, randomColor, DefaultTextColor, DefaultText);
        }

        /// <summary>
        /// Generates a test image with a specific file size (approximate) and returns it as a memory stream
        /// </summary>
        /// <param name="targetSizeInKB">Approximate target size of the image in kilobytes</param>
        /// <returns>A memory stream containing the generated test image with approximate target size</returns>
        public static async Task<MemoryStream> GenerateTestImageOfSizeAsync(int targetSizeInKB)
        {
            if (targetSizeInKB <= 0)
                throw new ArgumentException("Target size must be positive");
                
            // This is a rough approximation - JPEG compression makes exact sizing difficult
            // Assuming ~0.1 KB per 1000 pixels at medium JPEG quality
            int pixelCount = targetSizeInKB * 10000;
            
            // Calculate dimensions to achieve approximate target size
            // Using 4:3 aspect ratio
            int height = (int)Math.Sqrt(pixelCount * 0.75);
            int width = (int)(height * 1.33);
            
            return await GenerateTestImageAsync(width, height, DefaultBackgroundColor, DefaultTextColor, DefaultText);
        }
    }
}