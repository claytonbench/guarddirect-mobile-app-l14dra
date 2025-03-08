using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SecurityPatrol.IntegrationTests.Helpers;
using Xunit;

namespace SecurityPatrol.IntegrationTests
{
    /// <summary>
    /// Base class for integration tests providing common setup, HTTP client configuration, and utility methods
    /// </summary>
    public class TestBase : IClassFixture<CustomWebApplicationFactory>, IDisposable
    {
        /// <summary>
        /// Factory for creating the test server with in-memory database and authentication
        /// </summary>
        public CustomWebApplicationFactory Factory { get; }

        /// <summary>
        /// HTTP client for making requests to the test server
        /// </summary>
        public HttpClient Client { get; }

        /// <summary>
        /// JSON serialization options for consistent API communication
        /// </summary>
        public JsonSerializerOptions JsonOptions { get; }

        /// <summary>
        /// The test user ID used for authentication
        /// </summary>
        public string TestUserId { get; }

        /// <summary>
        /// The test phone number used for authentication
        /// </summary>
        public string TestPhoneNumber { get; }

        /// <summary>
        /// Initializes a new instance of the TestBase class with a configured test server and HTTP client
        /// </summary>
        /// <param name="factory">The custom web application factory to use for creating the test server</param>
        public TestBase(CustomWebApplicationFactory factory)
        {
            Factory = factory ?? throw new ArgumentNullException(nameof(factory));
            
            // Configure client with specific options
            Client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
            
            // Set default headers for JSON communication
            Client.DefaultRequestHeaders.Accept.Clear();
            Client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            
            // Configure JSON options for serialization/deserialization
            JsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            // Get test user information from the factory
            TestUserId = factory.TestUserId;
            TestPhoneNumber = factory.TestPhoneNumber;
            
            // Reset database to ensure a clean state for each test
            factory.ResetDatabase();
        }

        /// <summary>
        /// Sets the authentication token in the HTTP client's default headers
        /// </summary>
        /// <param name="token">The JWT token to use for authentication</param>
        public void SetAuthToken(string token)
        {
            // Clear any existing authorization header
            Client.DefaultRequestHeaders.Remove("Authorization");
            
            // Add the new authorization header with bearer token
            Client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        /// <summary>
        /// Creates StringContent with JSON serialized data for HTTP requests
        /// </summary>
        /// <typeparam name="T">The type of data to serialize</typeparam>
        /// <param name="data">The data object to serialize</param>
        /// <returns>JSON content ready for HTTP request</returns>
        public StringContent CreateJsonContent<T>(T data)
        {
            return new StringContent(
                JsonSerializer.Serialize(data, JsonOptions),
                System.Text.Encoding.UTF8,
                "application/json");
        }

        /// <summary>
        /// Performs an HTTP GET request and deserializes the response to the specified type
        /// </summary>
        /// <typeparam name="T">The type to deserialize the response to</typeparam>
        /// <param name="endpoint">The API endpoint to request</param>
        /// <returns>The deserialized response object</returns>
        public async Task<T> GetAsync<T>(string endpoint)
        {
            var response = await Client.GetAsync(endpoint);
            AssertSuccessStatusCode(response);
            return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        }

        /// <summary>
        /// Performs an HTTP POST request with the provided data and deserializes the response
        /// </summary>
        /// <typeparam name="TRequest">The type of the request data</typeparam>
        /// <typeparam name="TResponse">The type to deserialize the response to</typeparam>
        /// <param name="endpoint">The API endpoint to request</param>
        /// <param name="data">The data to send in the request</param>
        /// <returns>The deserialized response object</returns>
        public async Task<TResponse> PostAsync<TRequest, TResponse>(string endpoint, TRequest data)
        {
            var content = CreateJsonContent(data);
            var response = await Client.PostAsync(endpoint, content);
            AssertSuccessStatusCode(response);
            return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions);
        }

        /// <summary>
        /// Performs an HTTP PUT request with the provided data and deserializes the response
        /// </summary>
        /// <typeparam name="TRequest">The type of the request data</typeparam>
        /// <typeparam name="TResponse">The type to deserialize the response to</typeparam>
        /// <param name="endpoint">The API endpoint to request</param>
        /// <param name="data">The data to send in the request</param>
        /// <returns>The deserialized response object</returns>
        public async Task<TResponse> PutAsync<TRequest, TResponse>(string endpoint, TRequest data)
        {
            var content = CreateJsonContent(data);
            var response = await Client.PutAsync(endpoint, content);
            AssertSuccessStatusCode(response);
            return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions);
        }

        /// <summary>
        /// Performs an HTTP DELETE request and deserializes the response to the specified type
        /// </summary>
        /// <typeparam name="T">The type to deserialize the response to</typeparam>
        /// <param name="endpoint">The API endpoint to request</param>
        /// <returns>The deserialized response object</returns>
        public async Task<T> DeleteAsync<T>(string endpoint)
        {
            var response = await Client.DeleteAsync(endpoint);
            AssertSuccessStatusCode(response);
            return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        }

        /// <summary>
        /// Asserts that the HTTP response has a success status code
        /// </summary>
        /// <param name="response">The HTTP response message to check</param>
        protected void AssertSuccessStatusCode(HttpResponseMessage response)
        {
            response.IsSuccessStatusCode.Should().BeTrue(
                $"Expected successful status code but got {(int)response.StatusCode}: {response.Content.ReadAsStringAsync().Result}");
        }

        /// <summary>
        /// Disposes the HTTP client and factory resources
        /// </summary>
        public void Dispose()
        {
            Client?.Dispose();
            // Factory disposal is handled by xUnit
        }
    }
}