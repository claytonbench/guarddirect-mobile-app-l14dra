using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using SecurityPatrol.Services;

namespace SecurityPatrol.UnitTests.Helpers.MockServices
{
    /// <summary>
    /// Mock implementation of IApiService for unit testing that provides configurable responses for API operations
    /// without making actual HTTP requests, allowing tests to run in isolation and with predictable responses.
    /// </summary>
    public class MockApiService : IApiService
    {
        // Dictionaries to store configured responses
        public Dictionary<string, object> GetResponses { get; private set; }
        public Dictionary<string, object> PostResponses { get; private set; }
        public Dictionary<string, object> PostMultipartResponses { get; private set; }
        public Dictionary<string, object> PutResponses { get; private set; }
        public Dictionary<string, object> DeleteResponses { get; private set; }
        
        // Dictionaries to store configured exceptions
        public Dictionary<string, Exception> GetExceptions { get; private set; }
        public Dictionary<string, Exception> PostExceptions { get; private set; }
        public Dictionary<string, Exception> PostMultipartExceptions { get; private set; }
        public Dictionary<string, Exception> PutExceptions { get; private set; }
        public Dictionary<string, Exception> DeleteExceptions { get; private set; }
        
        // Lists to track requests made
        public List<string> GetRequests { get; private set; }
        public List<string> PostRequests { get; private set; }
        public List<string> PostMultipartRequests { get; private set; }
        public List<string> PutRequests { get; private set; }
        public List<string> DeleteRequests { get; private set; }
        
        /// <summary>
        /// Initializes a new instance of the MockApiService class with default settings.
        /// </summary>
        public MockApiService()
        {
            // Initialize dictionaries for responses
            GetResponses = new Dictionary<string, object>();
            PostResponses = new Dictionary<string, object>();
            PostMultipartResponses = new Dictionary<string, object>();
            PutResponses = new Dictionary<string, object>();
            DeleteResponses = new Dictionary<string, object>();
            
            // Initialize dictionaries for exceptions
            GetExceptions = new Dictionary<string, Exception>();
            PostExceptions = new Dictionary<string, Exception>();
            PostMultipartExceptions = new Dictionary<string, Exception>();
            PutExceptions = new Dictionary<string, Exception>();
            DeleteExceptions = new Dictionary<string, Exception>();
            
            // Initialize lists for request tracking
            GetRequests = new List<string>();
            PostRequests = new List<string>();
            PostMultipartRequests = new List<string>();
            PutRequests = new List<string>();
            DeleteRequests = new List<string>();
        }
        
        /// <summary>
        /// Mocks an HTTP GET request to the specified endpoint.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the response content to.</typeparam>
        /// <param name="endpoint">The API endpoint path.</param>
        /// <param name="queryParams">Optional query parameters.</param>
        /// <param name="requiresAuth">Whether the request requires authentication.</param>
        /// <returns>The configured mock response for the endpoint.</returns>
        public async Task<T> GetAsync<T>(string endpoint, Dictionary<string, string> queryParams = null, bool requiresAuth = true)
        {
            // Record the request
            GetRequests.Add(endpoint);
            
            // Check if an exception is configured for this endpoint
            if (GetExceptions.ContainsKey(endpoint))
            {
                throw GetExceptions[endpoint];
            }
            
            // Check if a response is configured for this endpoint
            if (GetResponses.ContainsKey(endpoint) && GetResponses[endpoint] is T)
            {
                return (T)GetResponses[endpoint];
            }
            
            // If no response is configured, return default
            return default;
        }
        
        /// <summary>
        /// Mocks an HTTP POST request with JSON body to the specified endpoint.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the response content to.</typeparam>
        /// <param name="endpoint">The API endpoint path.</param>
        /// <param name="data">The data to be serialized to JSON and sent in the request body.</param>
        /// <param name="requiresAuth">Whether the request requires authentication.</param>
        /// <returns>The configured mock response for the endpoint.</returns>
        public async Task<T> PostAsync<T>(string endpoint, object data, bool requiresAuth = true)
        {
            // Record the request
            PostRequests.Add(endpoint);
            
            // Check if an exception is configured for this endpoint
            if (PostExceptions.ContainsKey(endpoint))
            {
                throw PostExceptions[endpoint];
            }
            
            // Check if a response is configured for this endpoint
            if (PostResponses.ContainsKey(endpoint) && PostResponses[endpoint] is T)
            {
                return (T)PostResponses[endpoint];
            }
            
            // If no response is configured, return default
            return default;
        }
        
        /// <summary>
        /// Mocks an HTTP POST request with multipart form data to the specified endpoint.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the response content to.</typeparam>
        /// <param name="endpoint">The API endpoint path.</param>
        /// <param name="content">The multipart form data content.</param>
        /// <param name="requiresAuth">Whether the request requires authentication.</param>
        /// <returns>The configured mock response for the endpoint.</returns>
        public async Task<T> PostMultipartAsync<T>(string endpoint, MultipartFormDataContent content, bool requiresAuth = true)
        {
            // Record the request
            PostMultipartRequests.Add(endpoint);
            
            // Check if an exception is configured for this endpoint
            if (PostMultipartExceptions.ContainsKey(endpoint))
            {
                throw PostMultipartExceptions[endpoint];
            }
            
            // Check if a response is configured for this endpoint
            if (PostMultipartResponses.ContainsKey(endpoint) && PostMultipartResponses[endpoint] is T)
            {
                return (T)PostMultipartResponses[endpoint];
            }
            
            // If no response is configured, return default
            return default;
        }
        
        /// <summary>
        /// Mocks an HTTP PUT request with JSON body to the specified endpoint.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the response content to.</typeparam>
        /// <param name="endpoint">The API endpoint path.</param>
        /// <param name="data">The data to be serialized to JSON and sent in the request body.</param>
        /// <param name="requiresAuth">Whether the request requires authentication.</param>
        /// <returns>The configured mock response for the endpoint.</returns>
        public async Task<T> PutAsync<T>(string endpoint, object data, bool requiresAuth = true)
        {
            // Record the request
            PutRequests.Add(endpoint);
            
            // Check if an exception is configured for this endpoint
            if (PutExceptions.ContainsKey(endpoint))
            {
                throw PutExceptions[endpoint];
            }
            
            // Check if a response is configured for this endpoint
            if (PutResponses.ContainsKey(endpoint) && PutResponses[endpoint] is T)
            {
                return (T)PutResponses[endpoint];
            }
            
            // If no response is configured, return default
            return default;
        }
        
        /// <summary>
        /// Mocks an HTTP DELETE request to the specified endpoint.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the response content to.</typeparam>
        /// <param name="endpoint">The API endpoint path.</param>
        /// <param name="requiresAuth">Whether the request requires authentication.</param>
        /// <returns>The configured mock response for the endpoint.</returns>
        public async Task<T> DeleteAsync<T>(string endpoint, bool requiresAuth = true)
        {
            // Record the request
            DeleteRequests.Add(endpoint);
            
            // Check if an exception is configured for this endpoint
            if (DeleteExceptions.ContainsKey(endpoint))
            {
                throw DeleteExceptions[endpoint];
            }
            
            // Check if a response is configured for this endpoint
            if (DeleteResponses.ContainsKey(endpoint) && DeleteResponses[endpoint] is T)
            {
                return (T)DeleteResponses[endpoint];
            }
            
            // If no response is configured, return default
            return default;
        }
        
        /// <summary>
        /// Configures a response for a GET request to a specific endpoint.
        /// </summary>
        /// <typeparam name="T">The type of the response.</typeparam>
        /// <param name="endpoint">The API endpoint path.</param>
        /// <param name="response">The response to return.</param>
        public void SetupGetResponse<T>(string endpoint, T response)
        {
            GetResponses[endpoint] = response;
            
            // Remove any exception that might be configured for this endpoint
            if (GetExceptions.ContainsKey(endpoint))
            {
                GetExceptions.Remove(endpoint);
            }
        }
        
        /// <summary>
        /// Configures a response for a POST request to a specific endpoint.
        /// </summary>
        /// <typeparam name="T">The type of the response.</typeparam>
        /// <param name="endpoint">The API endpoint path.</param>
        /// <param name="response">The response to return.</param>
        public void SetupPostResponse<T>(string endpoint, T response)
        {
            PostResponses[endpoint] = response;
            
            // Remove any exception that might be configured for this endpoint
            if (PostExceptions.ContainsKey(endpoint))
            {
                PostExceptions.Remove(endpoint);
            }
        }
        
        /// <summary>
        /// Configures a response for a multipart POST request to a specific endpoint.
        /// </summary>
        /// <typeparam name="T">The type of the response.</typeparam>
        /// <param name="endpoint">The API endpoint path.</param>
        /// <param name="response">The response to return.</param>
        public void SetupPostMultipartResponse<T>(string endpoint, T response)
        {
            PostMultipartResponses[endpoint] = response;
            
            // Remove any exception that might be configured for this endpoint
            if (PostMultipartExceptions.ContainsKey(endpoint))
            {
                PostMultipartExceptions.Remove(endpoint);
            }
        }
        
        /// <summary>
        /// Configures a response for a PUT request to a specific endpoint.
        /// </summary>
        /// <typeparam name="T">The type of the response.</typeparam>
        /// <param name="endpoint">The API endpoint path.</param>
        /// <param name="response">The response to return.</param>
        public void SetupPutResponse<T>(string endpoint, T response)
        {
            PutResponses[endpoint] = response;
            
            // Remove any exception that might be configured for this endpoint
            if (PutExceptions.ContainsKey(endpoint))
            {
                PutExceptions.Remove(endpoint);
            }
        }
        
        /// <summary>
        /// Configures a response for a DELETE request to a specific endpoint.
        /// </summary>
        /// <typeparam name="T">The type of the response.</typeparam>
        /// <param name="endpoint">The API endpoint path.</param>
        /// <param name="response">The response to return.</param>
        public void SetupDeleteResponse<T>(string endpoint, T response)
        {
            DeleteResponses[endpoint] = response;
            
            // Remove any exception that might be configured for this endpoint
            if (DeleteExceptions.ContainsKey(endpoint))
            {
                DeleteExceptions.Remove(endpoint);
            }
        }
        
        /// <summary>
        /// Configures an exception to be thrown for a GET request to a specific endpoint.
        /// </summary>
        /// <param name="endpoint">The API endpoint path.</param>
        /// <param name="exception">The exception to throw.</param>
        public void SetupGetException(string endpoint, Exception exception)
        {
            GetExceptions[endpoint] = exception;
            
            // Remove any response that might be configured for this endpoint
            if (GetResponses.ContainsKey(endpoint))
            {
                GetResponses.Remove(endpoint);
            }
        }
        
        /// <summary>
        /// Configures an exception to be thrown for a POST request to a specific endpoint.
        /// </summary>
        /// <param name="endpoint">The API endpoint path.</param>
        /// <param name="exception">The exception to throw.</param>
        public void SetupPostException(string endpoint, Exception exception)
        {
            PostExceptions[endpoint] = exception;
            
            // Remove any response that might be configured for this endpoint
            if (PostResponses.ContainsKey(endpoint))
            {
                PostResponses.Remove(endpoint);
            }
        }
        
        /// <summary>
        /// Configures an exception to be thrown for a multipart POST request to a specific endpoint.
        /// </summary>
        /// <param name="endpoint">The API endpoint path.</param>
        /// <param name="exception">The exception to throw.</param>
        public void SetupPostMultipartException(string endpoint, Exception exception)
        {
            PostMultipartExceptions[endpoint] = exception;
            
            // Remove any response that might be configured for this endpoint
            if (PostMultipartResponses.ContainsKey(endpoint))
            {
                PostMultipartResponses.Remove(endpoint);
            }
        }
        
        /// <summary>
        /// Configures an exception to be thrown for a PUT request to a specific endpoint.
        /// </summary>
        /// <param name="endpoint">The API endpoint path.</param>
        /// <param name="exception">The exception to throw.</param>
        public void SetupPutException(string endpoint, Exception exception)
        {
            PutExceptions[endpoint] = exception;
            
            // Remove any response that might be configured for this endpoint
            if (PutResponses.ContainsKey(endpoint))
            {
                PutResponses.Remove(endpoint);
            }
        }
        
        /// <summary>
        /// Configures an exception to be thrown for a DELETE request to a specific endpoint.
        /// </summary>
        /// <param name="endpoint">The API endpoint path.</param>
        /// <param name="exception">The exception to throw.</param>
        public void SetupDeleteException(string endpoint, Exception exception)
        {
            DeleteExceptions[endpoint] = exception;
            
            // Remove any response that might be configured for this endpoint
            if (DeleteResponses.ContainsKey(endpoint))
            {
                DeleteResponses.Remove(endpoint);
            }
        }
        
        /// <summary>
        /// Verifies that a GET request was made to the specified endpoint.
        /// </summary>
        /// <param name="endpoint">The API endpoint path.</param>
        /// <returns>True if the endpoint was called, otherwise false.</returns>
        public bool VerifyGetCalled(string endpoint)
        {
            return GetRequests.Contains(endpoint);
        }
        
        /// <summary>
        /// Verifies that a POST request was made to the specified endpoint.
        /// </summary>
        /// <param name="endpoint">The API endpoint path.</param>
        /// <returns>True if the endpoint was called, otherwise false.</returns>
        public bool VerifyPostCalled(string endpoint)
        {
            return PostRequests.Contains(endpoint);
        }
        
        /// <summary>
        /// Verifies that a multipart POST request was made to the specified endpoint.
        /// </summary>
        /// <param name="endpoint">The API endpoint path.</param>
        /// <returns>True if the endpoint was called, otherwise false.</returns>
        public bool VerifyPostMultipartCalled(string endpoint)
        {
            return PostMultipartRequests.Contains(endpoint);
        }
        
        /// <summary>
        /// Verifies that a PUT request was made to the specified endpoint.
        /// </summary>
        /// <param name="endpoint">The API endpoint path.</param>
        /// <returns>True if the endpoint was called, otherwise false.</returns>
        public bool VerifyPutCalled(string endpoint)
        {
            return PutRequests.Contains(endpoint);
        }
        
        /// <summary>
        /// Verifies that a DELETE request was made to the specified endpoint.
        /// </summary>
        /// <param name="endpoint">The API endpoint path.</param>
        /// <returns>True if the endpoint was called, otherwise false.</returns>
        public bool VerifyDeleteCalled(string endpoint)
        {
            return DeleteRequests.Contains(endpoint);
        }
        
        /// <summary>
        /// Resets all configurations and request history.
        /// </summary>
        public void Reset()
        {
            // Clear all response dictionaries
            GetResponses.Clear();
            PostResponses.Clear();
            PostMultipartResponses.Clear();
            PutResponses.Clear();
            DeleteResponses.Clear();
            
            // Clear all exception dictionaries
            GetExceptions.Clear();
            PostExceptions.Clear();
            PostMultipartExceptions.Clear();
            PutExceptions.Clear();
            DeleteExceptions.Clear();
            
            // Clear all request history lists
            GetRequests.Clear();
            PostRequests.Clear();
            PostMultipartRequests.Clear();
            PutRequests.Clear();
            DeleteRequests.Clear();
        }
    }
}