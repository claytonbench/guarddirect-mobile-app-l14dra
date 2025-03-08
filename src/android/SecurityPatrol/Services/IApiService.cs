using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace SecurityPatrol.Services
{
    /// <summary>
    /// Interface defining the contract for API communication services in the Security Patrol application.
    /// Responsible for handling HTTP requests to backend services, managing authentication,
    /// serializing/deserializing data, and implementing resilient error handling with retry logic.
    /// </summary>
    public interface IApiService
    {
        /// <summary>
        /// Performs an HTTP GET request to the specified endpoint.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the response content to.</typeparam>
        /// <param name="endpoint">The API endpoint path.</param>
        /// <param name="queryParams">Optional query parameters.</param>
        /// <param name="requiresAuth">Whether the request requires authentication.</param>
        /// <returns>The deserialized response object of type T.</returns>
        Task<T> GetAsync<T>(string endpoint, Dictionary<string, string> queryParams = null, bool requiresAuth = true);

        /// <summary>
        /// Performs an HTTP POST request with JSON body to the specified endpoint.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the response content to.</typeparam>
        /// <param name="endpoint">The API endpoint path.</param>
        /// <param name="data">The data to be serialized to JSON and sent in the request body.</param>
        /// <param name="requiresAuth">Whether the request requires authentication.</param>
        /// <returns>The deserialized response object of type T.</returns>
        Task<T> PostAsync<T>(string endpoint, object data, bool requiresAuth = true);

        /// <summary>
        /// Performs an HTTP POST request with multipart form data to the specified endpoint.
        /// Used primarily for file uploads such as photos.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the response content to.</typeparam>
        /// <param name="endpoint">The API endpoint path.</param>
        /// <param name="content">The multipart form data content.</param>
        /// <param name="requiresAuth">Whether the request requires authentication.</param>
        /// <returns>The deserialized response object of type T.</returns>
        Task<T> PostMultipartAsync<T>(string endpoint, MultipartFormDataContent content, bool requiresAuth = true);

        /// <summary>
        /// Performs an HTTP PUT request with JSON body to the specified endpoint.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the response content to.</typeparam>
        /// <param name="endpoint">The API endpoint path.</param>
        /// <param name="data">The data to be serialized to JSON and sent in the request body.</param>
        /// <param name="requiresAuth">Whether the request requires authentication.</param>
        /// <returns>The deserialized response object of type T.</returns>
        Task<T> PutAsync<T>(string endpoint, object data, bool requiresAuth = true);

        /// <summary>
        /// Performs an HTTP DELETE request to the specified endpoint.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the response content to.</typeparam>
        /// <param name="endpoint">The API endpoint path.</param>
        /// <param name="requiresAuth">Whether the request requires authentication.</param>
        /// <returns>The deserialized response object of type T.</returns>
        Task<T> DeleteAsync<T>(string endpoint, bool requiresAuth = true);
    }
}