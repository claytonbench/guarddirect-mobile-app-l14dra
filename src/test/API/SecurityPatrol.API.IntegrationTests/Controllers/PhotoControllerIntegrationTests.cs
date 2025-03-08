using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Xunit;
using FluentAssertions;
using SecurityPatrol.API.IntegrationTests.Setup;
using SecurityPatrol.Core.Models;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.TestCommon.Constants;
using SecurityPatrol.TestCommon.Helpers;

namespace SecurityPatrol.API.IntegrationTests.Controllers
{
    /// <summary>
    /// Integration tests for the PhotoController in the Security Patrol API, verifying photo upload, retrieval, and management operations using real HTTP requests.
    /// </summary>
    public class PhotoControllerIntegrationTests : IntegrationTestBase
    {
        /// <summary>
        /// Initializes a new instance of the PhotoControllerIntegrationTests class with the test factory.
        /// </summary>
        /// <param name="factory">The factory that creates a test server with in-memory database.</param>
        public PhotoControllerIntegrationTests(CustomWebApplicationFactory factory) 
            : base(factory)
        {
        }

        /// <summary>
        /// Tests that the Upload endpoint returns a success response when provided with a valid photo upload request and file.
        /// </summary>
        [Fact]
        public async Task Upload_WithValidRequest_ReturnsSuccessResponse()
        {
            // Arrange
            AuthenticateClient(); // Add authentication token
            var request = new PhotoUploadRequest
            {
                Timestamp = DateTime.UtcNow,
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude,
                UserId = TestConstants.TestUserId
            };

            // Generate a test image
            using var imageStream = await TestImageGenerator.GenerateTestImageAsync();
            using var content = new MultipartFormDataContent();
            
            // Add JSON request part
            var jsonContent = JsonContent.Create(request);
            content.Add(jsonContent, "request");
            
            // Add file part
            var fileContent = new StreamContent(imageStream);
            content.Add(fileContent, "file", "test_image.jpg");

            // Act
            var response = await Client.PostAsync("/api/v1/photos/upload", content);

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            var result = await response.Content.ReadFromJsonAsync<Result<PhotoUploadResponse>>(JsonOptions);
            result.Should().NotBeNull();
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Id.Should().NotBeNullOrEmpty();
            result.Data.Status.Should().Be("Success");
        }
        
        /// <summary>
        /// Tests that the Upload endpoint returns unauthorized when no authentication token is provided.
        /// </summary>
        [Fact]
        public async Task Upload_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Arrange - do not authenticate the client
            var request = new PhotoUploadRequest
            {
                Timestamp = DateTime.UtcNow,
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude,
                UserId = TestConstants.TestUserId
            };

            using var imageStream = await TestImageGenerator.GenerateTestImageAsync();
            using var content = new MultipartFormDataContent();
            
            var jsonContent = JsonContent.Create(request);
            content.Add(jsonContent, "request");
            
            var fileContent = new StreamContent(imageStream);
            content.Add(fileContent, "file", "test_image.jpg");

            // Act
            var response = await Client.PostAsync("/api/v1/photos/upload", content);

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
        }
        
        /// <summary>
        /// Tests that the Upload endpoint returns a bad request when provided with an invalid photo upload request.
        /// </summary>
        [Fact]
        public async Task Upload_WithInvalidRequest_ReturnsBadRequest()
        {
            // Arrange
            AuthenticateClient();
            var request = new PhotoUploadRequest
            {
                Timestamp = DateTime.UtcNow,
                // Invalid coordinates (outside valid range)
                Latitude = 200,  // Invalid, should be between -90 and 90
                Longitude = -200, // Invalid, should be between -180 and 180
                UserId = TestConstants.TestUserId
            };

            using var imageStream = await TestImageGenerator.GenerateTestImageAsync();
            using var content = new MultipartFormDataContent();
            
            var jsonContent = JsonContent.Create(request);
            content.Add(jsonContent, "request");
            
            var fileContent = new StreamContent(imageStream);
            content.Add(fileContent, "file", "test_image.jpg");

            // Act
            var response = await Client.PostAsync("/api/v1/photos/upload", content);

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        }
        
        /// <summary>
        /// Tests that the Upload endpoint returns a bad request when no file is provided.
        /// </summary>
        [Fact]
        public async Task Upload_WithNoFile_ReturnsBadRequest()
        {
            // Arrange
            AuthenticateClient();
            var request = new PhotoUploadRequest
            {
                Timestamp = DateTime.UtcNow,
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude,
                UserId = TestConstants.TestUserId
            };

            using var content = new MultipartFormDataContent();
            
            var jsonContent = JsonContent.Create(request);
            content.Add(jsonContent, "request");
            // Deliberately not adding a file

            // Act
            var response = await Client.PostAsync("/api/v1/photos/upload", content);

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        }
        
        /// <summary>
        /// Tests that the GetById endpoint returns a photo when provided with a valid ID.
        /// </summary>
        [Fact]
        public async Task GetById_WithValidId_ReturnsPhoto()
        {
            // Arrange
            AuthenticateClient();
            
            // First upload a photo to get a valid photo ID
            var uploadId = await UploadTestPhoto();
            
            // Act
            var response = await Client.GetAsync($"/api/v1/photos/{uploadId}");
            
            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            var result = await response.Content.ReadFromJsonAsync<Result<Photo>>(JsonOptions);
            result.Should().NotBeNull();
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Id.Should().Be(uploadId);
            result.Data.UserId.Should().Be(TestConstants.TestUserId);
            result.Data.Latitude.Should().BeApproximately(TestConstants.TestLatitude, 0.0001);
            result.Data.Longitude.Should().BeApproximately(TestConstants.TestLongitude, 0.0001);
        }
        
        /// <summary>
        /// Tests that the GetById endpoint returns not found when provided with an invalid ID.
        /// </summary>
        [Fact]
        public async Task GetById_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            AuthenticateClient();
            
            // Act
            var response = await Client.GetAsync("/api/v1/photos/999999");
            
            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        }
        
        /// <summary>
        /// Tests that the GetPhotoFile endpoint returns a file stream when provided with a valid ID.
        /// </summary>
        [Fact]
        public async Task GetPhotoFile_WithValidId_ReturnsFileStream()
        {
            // Arrange
            AuthenticateClient();
            
            // First upload a photo to get a valid photo ID
            var uploadId = await UploadTestPhoto();
            
            // Act
            var response = await Client.GetAsync($"/api/v1/photos/{uploadId}/file");
            
            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            response.Content.Headers.ContentType.MediaType.Should().StartWith("image/");
            var content = await response.Content.ReadAsByteArrayAsync();
            content.Length.Should().BeGreaterThan(0);
        }
        
        /// <summary>
        /// Tests that the GetPhotoFile endpoint returns not found when provided with an invalid ID.
        /// </summary>
        [Fact]
        public async Task GetPhotoFile_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            AuthenticateClient();
            
            // Act
            var response = await Client.GetAsync("/api/v1/photos/999999/file");
            
            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        }
        
        /// <summary>
        /// Tests that the GetMyPhotos endpoint returns photos belonging to the authenticated user.
        /// </summary>
        [Fact]
        public async Task GetMyPhotos_ReturnsUserPhotos()
        {
            // Arrange
            AuthenticateClient();
            
            // Upload multiple test photos
            await UploadTestPhoto();
            await UploadTestPhoto();
            
            // Act
            var response = await Client.GetAsync("/api/v1/photos/my");
            
            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            var result = await response.Content.ReadFromJsonAsync<Result<IEnumerable<Photo>>>(JsonOptions);
            result.Should().NotBeNull();
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCountGreaterOrEqualTo(2);
            result.Data.Should().AllSatisfy(p => p.UserId.Should().Be(TestConstants.TestUserId));
        }
        
        /// <summary>
        /// Tests that the GetMyPhotosPaginated endpoint returns a paginated list of photos for the authenticated user.
        /// </summary>
        [Fact]
        public async Task GetMyPhotosPaginated_ReturnsPaginatedPhotos()
        {
            // Arrange
            AuthenticateClient();
            
            // Upload multiple test photos
            for (int i = 0; i < 6; i++)
            {
                await UploadTestPhoto();
            }
            
            // Act
            var response = await Client.GetAsync("/api/v1/photos/my/paginated?pageNumber=1&pageSize=5");
            
            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            var result = await response.Content.ReadFromJsonAsync<Result<PaginatedList<Photo>>>(JsonOptions);
            result.Should().NotBeNull();
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.PageNumber.Should().Be(1);
            result.Data.Items.Count.Should().BeLessOrEqualTo(5);
            result.Data.TotalCount.Should().BeGreaterOrEqualTo(6);
        }
        
        /// <summary>
        /// Tests that the GetPhotosByLocation endpoint returns photos within the specified radius of a location.
        /// </summary>
        [Fact]
        public async Task GetPhotosByLocation_ReturnsNearbyPhotos()
        {
            // Arrange
            AuthenticateClient();
            
            // Upload test photos at known locations
            await UploadTestPhoto(latitude: 34.0522, longitude: -118.2437);
            await UploadTestPhoto(latitude: 34.0523, longitude: -118.2438);
            
            // Act - use a radius that should include both photos
            var response = await Client.GetAsync("/api/v1/photos/location?latitude=34.0522&longitude=-118.2437&radiusInMeters=200");
            
            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            var result = await response.Content.ReadFromJsonAsync<Result<IEnumerable<Photo>>>(JsonOptions);
            result.Should().NotBeNull();
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCountGreaterOrEqualTo(2);
        }
        
        /// <summary>
        /// Tests that the GetPhotosByDateRange endpoint returns photos captured within the specified date range.
        /// </summary>
        [Fact]
        public async Task GetPhotosByDateRange_ReturnsPhotosInRange()
        {
            // Arrange
            AuthenticateClient();
            
            // Upload test photos with different timestamps
            var oldTime = DateTime.UtcNow.AddDays(-5);
            var newTime = DateTime.UtcNow;
            
            await UploadTestPhoto(timestamp: oldTime);
            await UploadTestPhoto(timestamp: newTime);
            
            // Act - get photos from the last 3 days
            var startDate = DateTime.UtcNow.AddDays(-3).ToString("o");
            var endDate = DateTime.UtcNow.AddDays(1).ToString("o");
            var response = await Client.GetAsync($"/api/v1/photos/daterange?startDate={startDate}&endDate={endDate}");
            
            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            var result = await response.Content.ReadFromJsonAsync<Result<IEnumerable<Photo>>>(JsonOptions);
            result.Should().NotBeNull();
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
            // Should only include photos from within the date range
            result.Data.Should().AllSatisfy(p => p.Timestamp.Should().BeOnOrAfter(DateTime.UtcNow.AddDays(-3)));
            result.Data.Should().AllSatisfy(p => p.Timestamp.Should().BeOnOrBefore(DateTime.UtcNow.AddDays(1)));
        }
        
        /// <summary>
        /// Tests that the DeletePhoto endpoint returns success when provided with a valid ID.
        /// </summary>
        [Fact]
        public async Task DeletePhoto_WithValidId_ReturnsSuccess()
        {
            // Arrange
            AuthenticateClient();
            
            // First upload a photo to get a valid photo ID
            var uploadId = await UploadTestPhoto();
            
            // Act
            var response = await Client.DeleteAsync($"/api/v1/photos/{uploadId}");
            
            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            var result = await response.Content.ReadFromJsonAsync<Result>(JsonOptions);
            result.Should().NotBeNull();
            result.Succeeded.Should().BeTrue();
            
            // Verify the photo is deleted by trying to retrieve it
            var getResponse = await Client.GetAsync($"/api/v1/photos/{uploadId}");
            getResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        }
        
        /// <summary>
        /// Tests that the DeletePhoto endpoint returns not found when provided with an invalid ID.
        /// </summary>
        [Fact]
        public async Task DeletePhoto_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            AuthenticateClient();
            
            // Act
            var response = await Client.DeleteAsync("/api/v1/photos/999999");
            
            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        }
        
        /// <summary>
        /// Tests the complete photo management flow from upload to retrieval to deletion.
        /// </summary>
        [Fact]
        public async Task CompletePhotoFlow_Success()
        {
            // Arrange
            AuthenticateClient();
            var request = new PhotoUploadRequest
            {
                Timestamp = DateTime.UtcNow,
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude,
                UserId = TestConstants.TestUserId
            };
            
            // Act & Assert - Step 1: Upload photo
            using var imageStream = await TestImageGenerator.GenerateTestImageAsync();
            using var content = new MultipartFormDataContent();
            
            var jsonContent = JsonContent.Create(request);
            content.Add(jsonContent, "request");
            
            var fileContent = new StreamContent(imageStream);
            content.Add(fileContent, "file", "test_image.jpg");
            
            var uploadResponse = await Client.PostAsync("/api/v1/photos/upload", content);
            uploadResponse.IsSuccessStatusCode.Should().BeTrue();
            
            var uploadResult = await uploadResponse.Content.ReadFromJsonAsync<Result<PhotoUploadResponse>>(JsonOptions);
            uploadResult.Succeeded.Should().BeTrue();
            var photoId = uploadResult.Data.Id;
            
            // Step 2: Get photo by ID
            var getResponse = await Client.GetAsync($"/api/v1/photos/{photoId}");
            getResponse.IsSuccessStatusCode.Should().BeTrue();
            
            var getResult = await getResponse.Content.ReadFromJsonAsync<Result<Photo>>(JsonOptions);
            getResult.Succeeded.Should().BeTrue();
            getResult.Data.Id.Should().Be(photoId);
            
            // Step 3: Get photo file
            var fileResponse = await Client.GetAsync($"/api/v1/photos/{photoId}/file");
            fileResponse.IsSuccessStatusCode.Should().BeTrue();
            fileResponse.Content.Headers.ContentType.MediaType.Should().StartWith("image/");
            
            // Step 4: Get my photos
            var myPhotosResponse = await Client.GetAsync("/api/v1/photos/my");
            myPhotosResponse.IsSuccessStatusCode.Should().BeTrue();
            
            var myPhotosResult = await myPhotosResponse.Content.ReadFromJsonAsync<Result<IEnumerable<Photo>>>(JsonOptions);
            myPhotosResult.Succeeded.Should().BeTrue();
            myPhotosResult.Data.Any(p => p.Id.ToString() == photoId).Should().BeTrue();
            
            // Step 5: Delete photo
            var deleteResponse = await Client.DeleteAsync($"/api/v1/photos/{photoId}");
            deleteResponse.IsSuccessStatusCode.Should().BeTrue();
            
            // Step 6: Verify deletion
            var verifyDeleteResponse = await Client.GetAsync($"/api/v1/photos/{photoId}");
            verifyDeleteResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        }
        
        #region Helper Methods
        
        /// <summary>
        /// Helper method to upload a test photo with optional custom parameters and return its ID.
        /// </summary>
        private async Task<int> UploadTestPhoto(double? latitude = null, double? longitude = null, DateTime? timestamp = null)
        {
            // Default values if not provided
            var request = new PhotoUploadRequest
            {
                Timestamp = timestamp ?? DateTime.UtcNow,
                Latitude = latitude ?? TestConstants.TestLatitude,
                Longitude = longitude ?? TestConstants.TestLongitude,
                UserId = TestConstants.TestUserId
            };
            
            using var imageStream = await TestImageGenerator.GenerateTestImageAsync();
            using var content = new MultipartFormDataContent();
            
            var jsonContent = JsonContent.Create(request);
            content.Add(jsonContent, "request");
            
            var fileContent = new StreamContent(imageStream);
            content.Add(fileContent, "file", "test_image.jpg");
            
            var response = await Client.PostAsync("/api/v1/photos/upload", content);
            response.IsSuccessStatusCode.Should().BeTrue();
            
            var result = await response.Content.ReadFromJsonAsync<Result<PhotoUploadResponse>>(JsonOptions);
            return int.Parse(result.Data.Id);
        }
        
        #endregion
    }
}