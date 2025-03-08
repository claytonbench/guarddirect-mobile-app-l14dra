using System;
using System.Net.Http;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using SecurityPatrol.API.IntegrationTests.Setup;
using SecurityPatrol.Core.Models;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.TestCommon.Helpers;
using SecurityPatrol.TestCommon.Constants;

namespace SecurityPatrol.API.IntegrationTests.API
{
    /// <summary>
    /// Integration tests for the photo upload flow in the Security Patrol API, testing the complete process
    /// from authentication to photo upload and verification.
    /// </summary>
    public class PhotoUploadFlowTests : IntegrationTestBase
    {
        /// <summary>
        /// Initializes a new instance of the PhotoUploadFlowTests class with the test factory.
        /// </summary>
        /// <param name="factory">The factory that creates a test server with in-memory database.</param>
        public PhotoUploadFlowTests(CustomWebApplicationFactory factory) 
            : base(factory)
        {
            // Authenticate the client for subsequent requests
            AuthenticateClient();
        }

        [Fact]
        public async Task CanUploadPhoto_WithValidData_ReturnsSuccess()
        {
            // Arrange
            var request = new PhotoUploadRequest
            {
                UserId = TestConstants.TestUserId,
                Timestamp = DateTime.UtcNow,
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude
            };

            using var imageStream = await TestImageGenerator.GenerateTestImageAsync();
            using var content = new MultipartFormDataContent();
            
            // Add the request data as StringContent
            var requestJson = System.Text.Json.JsonSerializer.Serialize(request, JsonOptions);
            var requestContent = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");
            content.Add(requestContent, "request");
            
            // Add the image as StreamContent
            var imageContent = new StreamContent(imageStream);
            imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            content.Add(imageContent, "image", "test_image.jpg");

            // Act
            var response = await Client.PostAsync("/api/v1/photos/upload", content);

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = System.Text.Json.JsonSerializer.Deserialize<Result<PhotoUploadResponse>>(responseContent, JsonOptions);
            
            result.Should().NotBeNull();
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Id.Should().NotBeEmpty();
            result.Data.Status.Should().Be("Success");
        }

        [Fact]
        public async Task CanUploadPhoto_WithLargeImage_ReturnsSuccess()
        {
            // Arrange
            var request = new PhotoUploadRequest
            {
                UserId = TestConstants.TestUserId,
                Timestamp = DateTime.UtcNow,
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude
            };

            // Generate a large test image (5MB)
            using var imageStream = await TestImageGenerator.GenerateTestImageOfSizeAsync(5000);
            using var content = new MultipartFormDataContent();
            
            // Add the request data as StringContent
            var requestJson = System.Text.Json.JsonSerializer.Serialize(request, JsonOptions);
            var requestContent = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");
            content.Add(requestContent, "request");
            
            // Add the image as StreamContent
            var imageContent = new StreamContent(imageStream);
            imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            content.Add(imageContent, "image", "large_test_image.jpg");

            // Act
            var response = await Client.PostAsync("/api/v1/photos/upload", content);

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = System.Text.Json.JsonSerializer.Deserialize<Result<PhotoUploadResponse>>(responseContent, JsonOptions);
            
            result.Should().NotBeNull();
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Id.Should().NotBeEmpty();
        }

        [Fact]
        public async Task CanUploadPhoto_AndRetrieveById_ReturnsCorrectPhoto()
        {
            // Arrange
            var request = new PhotoUploadRequest
            {
                UserId = TestConstants.TestUserId,
                Timestamp = DateTime.UtcNow,
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude
            };

            using var imageStream = await TestImageGenerator.GenerateTestImageAsync();
            using var content = new MultipartFormDataContent();
            
            // Add the request data as StringContent
            var requestJson = System.Text.Json.JsonSerializer.Serialize(request, JsonOptions);
            var requestContent = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");
            content.Add(requestContent, "request");
            
            // Add the image as StreamContent
            var imageContent = new StreamContent(imageStream);
            imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            content.Add(imageContent, "image", "test_image.jpg");

            // Upload photo and get its ID
            var uploadResponse = await Client.PostAsync("/api/v1/photos/upload", content);
            uploadResponse.IsSuccessStatusCode.Should().BeTrue();

            var uploadResponseContent = await uploadResponse.Content.ReadAsStringAsync();
            var uploadResult = System.Text.Json.JsonSerializer.Deserialize<Result<PhotoUploadResponse>>(uploadResponseContent, JsonOptions);
            
            string photoId = uploadResult.Data.Id;

            // Act - Retrieve photo by ID
            var getResponse = await Client.GetAsync($"/api/v1/photos/{photoId}");

            // Assert
            getResponse.IsSuccessStatusCode.Should().BeTrue();

            var getResponseContent = await getResponse.Content.ReadAsStringAsync();
            var getResult = System.Text.Json.JsonSerializer.Deserialize<Result<Photo>>(getResponseContent, JsonOptions);
            
            getResult.Should().NotBeNull();
            getResult.Succeeded.Should().BeTrue();
            getResult.Data.Should().NotBeNull();
            getResult.Data.UserId.Should().Be(TestConstants.TestUserId);
            getResult.Data.Latitude.Should().Be(TestConstants.TestLatitude);
            getResult.Data.Longitude.Should().Be(TestConstants.TestLongitude);
            getResult.Data.FilePath.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task CanUploadPhoto_AndRetrieveFile_ReturnsCorrectImageFile()
        {
            // Arrange
            var request = new PhotoUploadRequest
            {
                UserId = TestConstants.TestUserId,
                Timestamp = DateTime.UtcNow,
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude
            };

            using var imageStream = await TestImageGenerator.GenerateTestImageAsync();
            using var content = new MultipartFormDataContent();
            
            // Add the request data as StringContent
            var requestJson = System.Text.Json.JsonSerializer.Serialize(request, JsonOptions);
            var requestContent = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");
            content.Add(requestContent, "request");
            
            // Add the image as StreamContent
            var imageContent = new StreamContent(imageStream);
            imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            content.Add(imageContent, "image", "test_image.jpg");

            // Upload photo and get its ID
            var uploadResponse = await Client.PostAsync("/api/v1/photos/upload", content);
            uploadResponse.IsSuccessStatusCode.Should().BeTrue();

            var uploadResponseContent = await uploadResponse.Content.ReadAsStringAsync();
            var uploadResult = System.Text.Json.JsonSerializer.Deserialize<Result<PhotoUploadResponse>>(uploadResponseContent, JsonOptions);
            
            string photoId = uploadResult.Data.Id;

            // Act - Retrieve photo file
            var getFileResponse = await Client.GetAsync($"/api/v1/photos/{photoId}/file");

            // Assert
            getFileResponse.IsSuccessStatusCode.Should().BeTrue();
            getFileResponse.Content.Headers.ContentType.MediaType.Should().StartWith("image/");

            var imageBytes = await getFileResponse.Content.ReadAsByteArrayAsync();
            imageBytes.Should().NotBeEmpty();
        }

        [Fact]
        public async Task CanUploadPhoto_WithMissingFile_ReturnsBadRequest()
        {
            // Arrange
            var request = new PhotoUploadRequest
            {
                UserId = TestConstants.TestUserId,
                Timestamp = DateTime.UtcNow,
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude
            };

            using var content = new MultipartFormDataContent();
            
            // Add only the request data as StringContent without an image file
            var requestJson = System.Text.Json.JsonSerializer.Serialize(request, JsonOptions);
            var requestContent = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");
            content.Add(requestContent, "request");

            // Act
            var response = await Client.PostAsync("/api/v1/photos/upload", content);

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CanUploadPhoto_WithInvalidRequest_ReturnsBadRequest()
        {
            // Arrange - Create invalid request with invalid coordinates
            var request = new PhotoUploadRequest
            {
                UserId = TestConstants.TestUserId,
                Timestamp = DateTime.UtcNow,
                Latitude = 100.0, // Invalid latitude (valid range: -90 to 90)
                Longitude = TestConstants.TestLongitude
            };

            using var imageStream = await TestImageGenerator.GenerateTestImageAsync();
            using var content = new MultipartFormDataContent();
            
            // Add the request data as StringContent
            var requestJson = System.Text.Json.JsonSerializer.Serialize(request, JsonOptions);
            var requestContent = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");
            content.Add(requestContent, "request");
            
            // Add the image as StreamContent
            var imageContent = new StreamContent(imageStream);
            imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            content.Add(imageContent, "image", "test_image.jpg");

            // Act
            var response = await Client.PostAsync("/api/v1/photos/upload", content);

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CanUploadPhoto_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Arrange
            var request = new PhotoUploadRequest
            {
                UserId = TestConstants.TestUserId,
                Timestamp = DateTime.UtcNow,
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude
            };

            using var imageStream = await TestImageGenerator.GenerateTestImageAsync();
            using var content = new MultipartFormDataContent();
            
            // Add the request data as StringContent
            var requestJson = System.Text.Json.JsonSerializer.Serialize(request, JsonOptions);
            var requestContent = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");
            content.Add(requestContent, "request");
            
            // Add the image as StreamContent
            var imageContent = new StreamContent(imageStream);
            imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            content.Add(imageContent, "image", "test_image.jpg");

            // Create a new client without authentication
            var unauthenticatedClient = Factory.CreateClient();

            // Act
            var response = await unauthenticatedClient.PostAsync("/api/v1/photos/upload", content);

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task CanUploadMultiplePhotos_AndRetrieveAll_ReturnsCorrectPhotos()
        {
            // Arrange - Upload multiple photos
            var request1 = new PhotoUploadRequest
            {
                UserId = TestConstants.TestUserId,
                Timestamp = DateTime.UtcNow.AddMinutes(-5),
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude
            };

            var request2 = new PhotoUploadRequest
            {
                UserId = TestConstants.TestUserId,
                Timestamp = DateTime.UtcNow,
                Latitude = TestConstants.TestLatitude + 0.001,
                Longitude = TestConstants.TestLongitude + 0.001
            };

            // Upload first photo
            using (var imageStream = await TestImageGenerator.GenerateTestImageAsync())
            using (var content = new MultipartFormDataContent())
            {
                var requestJson = System.Text.Json.JsonSerializer.Serialize(request1, JsonOptions);
                var requestContent = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");
                content.Add(requestContent, "request");
                
                var imageContent = new StreamContent(imageStream);
                imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                content.Add(imageContent, "image", "test_image1.jpg");

                var response = await Client.PostAsync("/api/v1/photos/upload", content);
                response.IsSuccessStatusCode.Should().BeTrue();
            }

            // Upload second photo
            using (var imageStream = await TestImageGenerator.GenerateTestImageAsync())
            using (var content = new MultipartFormDataContent())
            {
                var requestJson = System.Text.Json.JsonSerializer.Serialize(request2, JsonOptions);
                var requestContent = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");
                content.Add(requestContent, "request");
                
                var imageContent = new StreamContent(imageStream);
                imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                content.Add(imageContent, "image", "test_image2.jpg");

                var response = await Client.PostAsync("/api/v1/photos/upload", content);
                response.IsSuccessStatusCode.Should().BeTrue();
            }

            // Act - Retrieve all photos for the current user
            var getResponse = await Client.GetAsync("/api/v1/photos/my");

            // Assert
            getResponse.IsSuccessStatusCode.Should().BeTrue();

            var getResponseContent = await getResponse.Content.ReadAsStringAsync();
            var getResult = System.Text.Json.JsonSerializer.Deserialize<Result<System.Collections.Generic.IEnumerable<Photo>>>(getResponseContent, JsonOptions);
            
            getResult.Should().NotBeNull();
            getResult.Succeeded.Should().BeTrue();
            getResult.Data.Should().NotBeNull();
            getResult.Data.Should().HaveCountGreaterOrEqualTo(2);
        }
    }
}