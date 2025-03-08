using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Xunit;
using FluentAssertions;
using SecurityPatrol.Core.Models;
using SecurityPatrol.Core.Entities;

namespace SecurityPatrol.IntegrationTests.Controllers
{
    /// <summary>
    /// Integration tests for the Photo Controller in the Security Patrol API
    /// </summary>
    public class PhotoControllerTests : TestBase
    {
        private readonly string _baseUrl;
        private readonly string _testAuthToken;

        /// <summary>
        /// Initializes a new instance of the PhotoControllerTests class with the test factory
        /// </summary>
        /// <param name="factory">The custom web application factory for testing</param>
        public PhotoControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
            _baseUrl = "api/v1/photos";
            _testAuthToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzZWN1cml0eV9wYXRyb2xfdXNlcl9pZCI6InRlc3QtdXNlci1pZCIsInNlY3VyaXR5X3BhdHJvbF9waG9uZV9udW1iZXIiOiIrMTU1NTU1NTU1NTUiLCJzZWN1cml0eV9wYXRyb2xfcm9sZSI6InNlY3VyaXR5X3BlcnNvbm5lbCIsIm5iZiI6MTY1MDAwMDAwMCwiZXhwIjoxNjgwMDAwMDAwLCJpYXQiOjE2NTAwMDAwMDB9.WElDwXL92yz4R8OJbFtGYn9-H9Zem988VcRYNPBYO8I";
            SetAuthToken(_testAuthToken);
        }

        /// <summary>
        /// Tests that uploading a photo with valid request data returns a successful response
        /// </summary>
        [Fact]
        public async Task Upload_WithValidRequest_ReturnsSuccess()
        {
            // Arrange
            var request = CreateTestPhotoUploadRequest();
            using var imageContent = new ByteArrayContent(CreateTestImageContent());
            imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");

            using var formData = new MultipartFormDataContent();
            formData.Add(imageContent, "image", "test-image.jpg");
            formData.Add(new StringContent(request.Timestamp.ToString("o")), "timestamp");
            formData.Add(new StringContent(request.Latitude.ToString()), "latitude");
            formData.Add(new StringContent(request.Longitude.ToString()), "longitude");
            formData.Add(new StringContent(request.UserId), "userId");

            // Act
            var response = await Client.PostAsync($"{_baseUrl}/upload", formData);

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            
            var result = await response.Content.ReadFromJsonAsync<Result<PhotoUploadResponse>>(JsonOptions);
            result.Should().NotBeNull();
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Id.Should().NotBeNullOrEmpty();
            result.Data.Status.Should().Be("success");
        }

        /// <summary>
        /// Tests that attempting to upload a photo without authentication returns an unauthorized response
        /// </summary>
        [Fact]
        public async Task Upload_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Arrange
            var request = CreateTestPhotoUploadRequest();
            using var client = Factory.CreateClient(); // Client with no auth token
            
            using var imageContent = new ByteArrayContent(CreateTestImageContent());
            imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");

            using var formData = new MultipartFormDataContent();
            formData.Add(imageContent, "image", "test-image.jpg");
            formData.Add(new StringContent(request.Timestamp.ToString("o")), "timestamp");
            formData.Add(new StringContent(request.Latitude.ToString()), "latitude");
            formData.Add(new StringContent(request.Longitude.ToString()), "longitude");
            formData.Add(new StringContent(request.UserId), "userId");

            // Act
            var response = await client.PostAsync($"{_baseUrl}/upload", formData);

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
        }

        /// <summary>
        /// Tests that uploading a photo with invalid request data returns a bad request response
        /// </summary>
        [Fact]
        public async Task Upload_WithInvalidRequest_ReturnsBadRequest()
        {
            // Arrange
            var request = new PhotoUploadRequest
            {
                // Invalid coordinates
                Latitude = 100, // Invalid latitude (out of range)
                Longitude = 200, // Invalid longitude (out of range)
                UserId = TestUserId
            };
            
            using var imageContent = new ByteArrayContent(CreateTestImageContent());
            imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");

            using var formData = new MultipartFormDataContent();
            formData.Add(imageContent, "image", "test-image.jpg");
            formData.Add(new StringContent(request.Timestamp.ToString("o")), "timestamp");
            formData.Add(new StringContent(request.Latitude.ToString()), "latitude");
            formData.Add(new StringContent(request.Longitude.ToString()), "longitude");
            formData.Add(new StringContent(request.UserId), "userId");

            // Act
            var response = await Client.PostAsync($"{_baseUrl}/upload", formData);

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        }

        /// <summary>
        /// Tests that uploading a photo without a file returns a bad request response
        /// </summary>
        [Fact]
        public async Task Upload_WithoutFile_ReturnsBadRequest()
        {
            // Arrange
            var request = CreateTestPhotoUploadRequest();

            using var formData = new MultipartFormDataContent();
            // No image file added
            formData.Add(new StringContent(request.Timestamp.ToString("o")), "timestamp");
            formData.Add(new StringContent(request.Latitude.ToString()), "latitude");
            formData.Add(new StringContent(request.Longitude.ToString()), "longitude");
            formData.Add(new StringContent(request.UserId), "userId");

            // Act
            var response = await Client.PostAsync($"{_baseUrl}/upload", formData);

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        }

        /// <summary>
        /// Tests that retrieving a photo by a valid ID returns the correct photo
        /// </summary>
        [Fact]
        public async Task GetById_WithValidId_ReturnsPhoto()
        {
            // Arrange
            var photoId = await UploadTestPhoto(37.7749, -122.4194);

            // Act
            var response = await Client.GetAsync($"{_baseUrl}/{photoId}");

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            
            var result = await response.Content.ReadFromJsonAsync<Result<Photo>>(JsonOptions);
            result.Should().NotBeNull();
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Id.ToString().Should().Be(photoId);
            result.Data.UserId.Should().Be(TestUserId);
            result.Data.Latitude.Should().BeApproximately(37.7749, 0.0001);
            result.Data.Longitude.Should().BeApproximately(-122.4194, 0.0001);
        }

        /// <summary>
        /// Tests that attempting to retrieve a photo with an invalid ID returns a not found response
        /// </summary>
        [Fact]
        public async Task GetById_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var invalidId = "9999"; // Assuming this ID doesn't exist

            // Act
            var response = await Client.GetAsync($"{_baseUrl}/{invalidId}");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        }

        /// <summary>
        /// Tests that retrieving a photo file by a valid ID returns the correct file stream
        /// </summary>
        [Fact]
        public async Task GetPhotoFile_WithValidId_ReturnsFileStream()
        {
            // Arrange
            var photoId = await UploadTestPhoto(37.7749, -122.4194);

            // Act
            var response = await Client.GetAsync($"{_baseUrl}/{photoId}/file");

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            response.Content.Headers.ContentType.MediaType.Should().StartWith("image/");
            
            var bytes = await response.Content.ReadAsByteArrayAsync();
            bytes.Should().NotBeEmpty();
        }

        /// <summary>
        /// Tests that retrieving the current user's photos returns the correct photos
        /// </summary>
        [Fact]
        public async Task GetMyPhotos_ReturnsUserPhotos()
        {
            // Arrange
            await UploadTestPhoto(37.7749, -122.4194);
            await UploadTestPhoto(37.7750, -122.4195);

            // Act
            var response = await Client.GetAsync($"{_baseUrl}/my");

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            
            var result = await response.Content.ReadFromJsonAsync<Result<IEnumerable<Photo>>>(JsonOptions);
            result.Should().NotBeNull();
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().NotBeEmpty();
            result.Data.All(p => p.UserId == TestUserId).Should().BeTrue();
        }

        /// <summary>
        /// Tests that retrieving paginated photos returns the correct page of photos
        /// </summary>
        [Fact]
        public async Task GetMyPhotosPaginated_ReturnsPaginatedPhotos()
        {
            // Arrange
            for (int i = 0; i < 5; i++)
            {
                await UploadTestPhoto(37.7749 + (i * 0.0001), -122.4194 - (i * 0.0001));
            }

            // Act
            var response = await Client.GetAsync($"{_baseUrl}/my/paginated?pageNumber=1&pageSize=3");

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            
            var result = await response.Content.ReadFromJsonAsync<Result<PaginatedList<Photo>>>(JsonOptions);
            result.Should().NotBeNull();
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Items.Should().NotBeEmpty();
            result.Data.PageNumber.Should().Be(1);
            result.Data.Items.Count.Should().BeLessOrEqualTo(3);
            result.Data.TotalPages.Should().BeGreaterOrEqualTo(2);
        }

        /// <summary>
        /// Tests that retrieving photos by location returns photos within the specified radius
        /// </summary>
        [Fact]
        public async Task GetPhotosByLocation_ReturnsNearbyPhotos()
        {
            // Arrange
            await UploadTestPhoto(37.7749, -122.4194); // Within radius
            await UploadTestPhoto(37.8749, -122.5194); // Outside radius (far away)

            // Act
            var response = await Client.GetAsync($"{_baseUrl}/location?latitude=37.7749&longitude=-122.4194&radiusInMeters=1000");

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            
            var result = await response.Content.ReadFromJsonAsync<Result<IEnumerable<Photo>>>(JsonOptions);
            result.Should().NotBeNull();
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().NotBeEmpty();
            
            // The first photo should be within radius (close to the specified coordinates)
            var firstPhoto = result.Data.First();
            var distance = CalculateDistance(37.7749, -122.4194, firstPhoto.Latitude, firstPhoto.Longitude);
            distance.Should().BeLessThan(1000); // less than 1000 meters
        }

        /// <summary>
        /// Tests that retrieving photos by date range returns photos within the specified dates
        /// </summary>
        [Fact]
        public async Task GetPhotosByDateRange_ReturnsPhotosInRange()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var yesterday = now.AddDays(-1);
            var tomorrow = now.AddDays(1);
            
            // Upload test photo
            await UploadTestPhoto(37.7749, -122.4194);
            
            // Act
            var response = await Client.GetAsync(
                $"{_baseUrl}/daterange?startDate={yesterday:yyyy-MM-ddTHH:mm:ss}&endDate={tomorrow:yyyy-MM-ddTHH:mm:ss}");

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            
            var result = await response.Content.ReadFromJsonAsync<Result<IEnumerable<Photo>>>(JsonOptions);
            result.Should().NotBeNull();
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().NotBeEmpty();
            
            // All photos should be within the date range
            result.Data.All(p => p.Timestamp >= yesterday && p.Timestamp <= tomorrow).Should().BeTrue();
        }

        /// <summary>
        /// Tests that deleting a photo with a valid ID returns a successful response
        /// </summary>
        [Fact]
        public async Task DeletePhoto_WithValidId_ReturnsSuccess()
        {
            // Arrange
            var photoId = await UploadTestPhoto(37.7749, -122.4194);

            // Act
            var response = await Client.DeleteAsync($"{_baseUrl}/{photoId}");

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            
            var result = await response.Content.ReadFromJsonAsync<Result>(JsonOptions);
            result.Should().NotBeNull();
            result.Succeeded.Should().BeTrue();
            
            // Verify the photo is deleted
            var getResponse = await Client.GetAsync($"{_baseUrl}/{photoId}");
            getResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        }

        /// <summary>
        /// Tests that attempting to delete a photo with an invalid ID returns a not found response
        /// </summary>
        [Fact]
        public async Task DeletePhoto_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var invalidId = "9999"; // Assuming this ID doesn't exist

            // Act
            var response = await Client.DeleteAsync($"{_baseUrl}/{invalidId}");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        }

        /// <summary>
        /// Helper method to create a test photo upload request with valid data
        /// </summary>
        /// <returns>A valid photo upload request for testing</returns>
        private PhotoUploadRequest CreateTestPhotoUploadRequest()
        {
            return new PhotoUploadRequest
            {
                Timestamp = DateTime.UtcNow,
                Latitude = 37.7749,
                Longitude = -122.4194,
                UserId = TestUserId
            };
        }

        /// <summary>
        /// Helper method to create test image content for photo upload tests
        /// </summary>
        /// <returns>Test image data as a byte array</returns>
        private byte[] CreateTestImageContent()
        {
            // Create a simple test image (1x1 pixel BMP)
            byte[] imageData = new byte[]
            {
                0x42, 0x4D, 0x3A, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x36, 0x00, 0x00, 0x00, 0x28, 0x00,
                0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x18, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0x00, 0x00
            };
            return imageData;
        }

        /// <summary>
        /// Helper method to upload a test photo and return its ID
        /// </summary>
        /// <param name="latitude">The latitude for the test photo</param>
        /// <param name="longitude">The longitude for the test photo</param>
        /// <returns>The ID of the uploaded test photo</returns>
        private async Task<string> UploadTestPhoto(double latitude, double longitude)
        {
            // Create request
            var request = new PhotoUploadRequest
            {
                Timestamp = DateTime.UtcNow,
                Latitude = latitude,
                Longitude = longitude,
                UserId = TestUserId
            };
            
            // Create image content
            using var imageContent = new ByteArrayContent(CreateTestImageContent());
            imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");

            // Create form data
            using var formData = new MultipartFormDataContent();
            formData.Add(imageContent, "image", "test-image.jpg");
            formData.Add(new StringContent(request.Timestamp.ToString("o")), "timestamp");
            formData.Add(new StringContent(request.Latitude.ToString()), "latitude");
            formData.Add(new StringContent(request.Longitude.ToString()), "longitude");
            formData.Add(new StringContent(request.UserId), "userId");

            // Post to upload endpoint
            var response = await Client.PostAsync($"{_baseUrl}/upload", formData);
            
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to upload test photo: {response.StatusCode}");
            }
            
            var result = await response.Content.ReadFromJsonAsync<Result<PhotoUploadResponse>>(JsonOptions);
            return result.Data.Id;
        }

        /// <summary>
        /// Calculates the distance between two sets of coordinates using the Haversine formula
        /// </summary>
        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            // Earth radius in meters
            const double earthRadius = 6371000;
            
            // Convert latitude and longitude from degrees to radians
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            
            // Haversine formula
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            
            return earthRadius * c; // Distance in meters
        }
        
        private double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }
    }
}