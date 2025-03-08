using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SecurityPatrol.TestCommon.Constants;
using SecurityPatrol.TestCommon.Helpers;
using SecurityPatrol.Core.Models;
using Xunit;
using FluentAssertions;

namespace SecurityPatrol.API.IntegrationTests.Setup
{
    /// <summary>
    /// Base class for API integration tests providing a configured test server, HTTP client, and utility methods.
    /// </summary>
    public abstract class IntegrationTestBase : IClassFixture<CustomWebApplicationFactory>
    {
        protected readonly CustomWebApplicationFactory Factory;
        protected readonly HttpClient Client;
        protected readonly string DatabaseName;
        protected readonly JsonSerializerOptions JsonOptions;

        /// <summary>
        /// Initializes a new instance of the IntegrationTestBase class with the test factory.
        /// </summary>
        /// <param name="factory">The factory that creates a test server with in-memory database.</param>
        public IntegrationTestBase(CustomWebApplicationFactory factory)
        {
            Factory = factory ?? throw new ArgumentNullException(nameof(factory));
            DatabaseName = factory.DatabaseName;
            Client = factory.CreateClient();
            
            // Configure JSON options for API communication
            JsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        /// <summary>
        /// Gets a service of the specified type from the test server's service provider.
        /// </summary>
        /// <typeparam name="T">The type of service to get.</typeparam>
        /// <returns>The requested service.</returns>
        protected T GetService<T>()
        {
            using var scope = Factory.Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<T>();
        }

        /// <summary>
        /// Adds authentication headers to the HTTP client for authenticated requests.
        /// </summary>
        /// <param name="token">The JWT token to use for authentication. If null, a default test token is used.</param>
        protected void AuthenticateClient(string token = null)
        {
            token = token ?? TestConstants.TestAuthToken;
            Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        /// <summary>
        /// Sends a GET request to the specified endpoint and deserializes the response.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the response to.</typeparam>
        /// <param name="endpoint">The API endpoint to send the request to.</param>
        /// <returns>The deserialized response.</returns>
        protected async Task<T> GetAsync<T>(string endpoint)
        {
            var response = await Client.GetAsync(endpoint);
            AssertSuccessStatusCode(response);
            return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        }

        /// <summary>
        /// Sends a POST request with the specified content to the endpoint and deserializes the response.
        /// </summary>
        /// <typeparam name="TRequest">The type of the request content.</typeparam>
        /// <typeparam name="TResponse">The type to deserialize the response to.</typeparam>
        /// <param name="endpoint">The API endpoint to send the request to.</param>
        /// <param name="content">The content to send with the request.</param>
        /// <returns>The deserialized response.</returns>
        protected async Task<TResponse> PostAsync<TRequest, TResponse>(string endpoint, TRequest content)
        {
            var json = JsonSerializer.Serialize(content, JsonOptions);
            var stringContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await Client.PostAsync(endpoint, stringContent);
            AssertSuccessStatusCode(response);
            return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions);
        }

        /// <summary>
        /// Sends a PUT request with the specified content to the endpoint and deserializes the response.
        /// </summary>
        /// <typeparam name="TRequest">The type of the request content.</typeparam>
        /// <typeparam name="TResponse">The type to deserialize the response to.</typeparam>
        /// <param name="endpoint">The API endpoint to send the request to.</param>
        /// <param name="content">The content to send with the request.</param>
        /// <returns>The deserialized response.</returns>
        protected async Task<TResponse> PutAsync<TRequest, TResponse>(string endpoint, TRequest content)
        {
            var json = JsonSerializer.Serialize(content, JsonOptions);
            var stringContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await Client.PutAsync(endpoint, stringContent);
            AssertSuccessStatusCode(response);
            return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions);
        }

        /// <summary>
        /// Sends a DELETE request to the specified endpoint and deserializes the response.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the response to.</typeparam>
        /// <param name="endpoint">The API endpoint to send the request to.</param>
        /// <returns>The deserialized response.</returns>
        protected async Task<T> DeleteAsync<T>(string endpoint)
        {
            var response = await Client.DeleteAsync(endpoint);
            AssertSuccessStatusCode(response);
            return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        }

        /// <summary>
        /// Asserts that the HTTP response has a success status code.
        /// </summary>
        /// <param name="response">The HTTP response to check.</param>
        /// <exception cref="Exception">Thrown when the response status code is not a success code.</exception>
        protected void AssertSuccessStatusCode(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode) 
                return;
            
            var content = response.Content.ReadAsStringAsync().Result;
            throw new Exception($"HTTP request failed with status code {response.StatusCode}: {content}");
        }

        /// <summary>
        /// Disposes the HTTP client and other resources.
        /// </summary>
        public virtual void Dispose()
        {
            Client?.Dispose();
        }
    }
}