using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging; // v8.0.0
using Polly; // v7.2.3
using SecurityPatrol.Constants;

namespace SecurityPatrol.Services
{
    /// <summary>
    /// Implementation of the IApiService interface that handles HTTP communication with backend services.
    /// This service manages API requests, authentication, serialization/deserialization, and implements
    /// resilient error handling with retry policies.
    /// </summary>
    public class ApiService : IApiService
    {
        private readonly HttpClient httpClient;
        private readonly ITokenManager tokenManager;
        private readonly INetworkService networkService;
        private readonly ITelemetryService telemetryService;
        private readonly JsonSerializerOptions jsonOptions;
        private readonly IAsyncPolicy<HttpResponseMessage> retryPolicy;

        /// <summary>
        /// Initializes a new instance of the ApiService class with required dependencies.
        /// </summary>
        /// <param name="httpClient">The HTTP client used for making requests.</param>
        /// <param name="tokenManager">The token manager for authentication.</param>
        /// <param name="networkService">The network service for connectivity checks.</param>
        /// <param name="telemetryService">The telemetry service for logging and monitoring.</param>
        public ApiService(
            HttpClient httpClient,
            ITokenManager tokenManager,
            INetworkService networkService,
            ITelemetryService telemetryService)
        {
            this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            this.tokenManager = tokenManager ?? throw new ArgumentNullException(nameof(tokenManager));
            this.networkService = networkService ?? throw new ArgumentNullException(nameof(networkService));
            this.telemetryService = telemetryService ?? throw new ArgumentNullException(nameof(telemetryService));
            
            // Configure HTTP client
            this.httpClient.BaseAddress = new Uri(ApiEndpoints.BaseUrl);
            this.httpClient.DefaultRequestHeaders.Accept.Clear();
            this.httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            
            // Configure JSON serialization options
            jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
            
            // Configure retry policy
            retryPolicy = ConfigureRetryPolicy();
        }
        
        /// <inheritdoc/>
        public async Task<T> GetAsync<T>(string endpoint, Dictionary<string, string> queryParams = null, bool requiresAuth = true)
        {
            return await ExecuteRequestAsync<T>(HttpMethod.Get, endpoint, null, queryParams, requiresAuth);
        }
        
        /// <inheritdoc/>
        public async Task<T> PostAsync<T>(string endpoint, object data, bool requiresAuth = true)
        {
            return await ExecuteRequestAsync<T>(HttpMethod.Post, endpoint, data, null, requiresAuth);
        }
        
        /// <inheritdoc/>
        public async Task<T> PostMultipartAsync<T>(string endpoint, MultipartFormDataContent content, bool requiresAuth = true)
        {
            if (!networkService.IsConnected)
            {
                telemetryService.Log(LogLevel.Warning, $"Network unavailable for multipart POST request to {endpoint}");
                throw new HttpRequestException(ErrorMessages.NetworkError);
            }

            if (!networkService.ShouldAttemptOperation(NetworkOperationType.Standard))
            {
                telemetryService.Log(LogLevel.Warning, $"Network quality insufficient for multipart POST request to {endpoint}");
                throw new HttpRequestException(ErrorMessages.NetworkError);
            }

            var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = content
            };

            await AddAuthenticationHeader(request, requiresAuth);

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var response = await retryPolicy.ExecuteAsync(() => httpClient.SendAsync(request));
                stopwatch.Stop();

                HandleResponse(response);

                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<T>(responseContent, jsonOptions);

                telemetryService.TrackApiCall(
                    endpoint,
                    stopwatch.Elapsed,
                    true,
                    ((int)response.StatusCode).ToString());

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                telemetryService.TrackException(ex, new Dictionary<string, string>
                {
                    { "Endpoint", endpoint },
                    { "Method", "POST (Multipart)" },
                    { "Duration", stopwatch.ElapsedMilliseconds.ToString() }
                });

                if (ex is HttpRequestException)
                {
                    throw;
                }
                else if (ex is TaskCanceledException)
                {
                    throw new TimeoutException(ErrorMessages.TimeoutError, ex);
                }
                else if (ex is UnauthorizedAccessException)
                {
                    throw;
                }
                else if (ex is JsonException)
                {
                    throw new InvalidOperationException($"Failed to deserialize response: {ex.Message}", ex);
                }
                else
                {
                    throw new InvalidOperationException(ErrorMessages.GenericError, ex);
                }
            }
        }
        
        /// <inheritdoc/>
        public async Task<T> PutAsync<T>(string endpoint, object data, bool requiresAuth = true)
        {
            return await ExecuteRequestAsync<T>(HttpMethod.Put, endpoint, data, null, requiresAuth);
        }
        
        /// <inheritdoc/>
        public async Task<T> DeleteAsync<T>(string endpoint, bool requiresAuth = true)
        {
            return await ExecuteRequestAsync<T>(HttpMethod.Delete, endpoint, null, null, requiresAuth);
        }
        
        /// <summary>
        /// Executes an HTTP request with the specified parameters.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the response to.</typeparam>
        /// <param name="method">The HTTP method.</param>
        /// <param name="endpoint">The API endpoint.</param>
        /// <param name="data">The data to send (for POST, PUT).</param>
        /// <param name="queryParams">Query parameters (for GET).</param>
        /// <param name="requiresAuth">Whether the request requires authentication.</param>
        /// <returns>The deserialized response.</returns>
        private async Task<T> ExecuteRequestAsync<T>(
            HttpMethod method,
            string endpoint,
            object data = null,
            Dictionary<string, string> queryParams = null,
            bool requiresAuth = true)
        {
            if (!networkService.IsConnected)
            {
                telemetryService.Log(LogLevel.Warning, $"Network unavailable for {method} request to {endpoint}");
                throw new HttpRequestException(ErrorMessages.NetworkError);
            }

            if (!networkService.ShouldAttemptOperation(NetworkOperationType.Standard))
            {
                telemetryService.Log(LogLevel.Warning, $"Network quality insufficient for {method} request to {endpoint}");
                throw new HttpRequestException(ErrorMessages.NetworkError);
            }

            var url = BuildUrl(endpoint, queryParams);
            var request = new HttpRequestMessage(method, url);

            // Add content for methods that support it
            if (data != null && (method == HttpMethod.Post || method == HttpMethod.Put))
            {
                var json = JsonSerializer.Serialize(data, jsonOptions);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            await AddAuthenticationHeader(request, requiresAuth);

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var response = await retryPolicy.ExecuteAsync(() => httpClient.SendAsync(request));
                stopwatch.Stop();

                HandleResponse(response);

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<T>(content, jsonOptions);

                telemetryService.TrackApiCall(
                    endpoint,
                    stopwatch.Elapsed,
                    true,
                    ((int)response.StatusCode).ToString());

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                telemetryService.TrackException(ex, new Dictionary<string, string>
                {
                    { "Endpoint", endpoint },
                    { "Method", method.ToString() },
                    { "Duration", stopwatch.ElapsedMilliseconds.ToString() }
                });

                if (ex is HttpRequestException)
                {
                    throw;
                }
                else if (ex is TaskCanceledException)
                {
                    throw new TimeoutException(ErrorMessages.TimeoutError, ex);
                }
                else if (ex is UnauthorizedAccessException)
                {
                    throw;
                }
                else if (ex is JsonException)
                {
                    throw new InvalidOperationException($"Failed to deserialize response: {ex.Message}", ex);
                }
                else
                {
                    throw new InvalidOperationException(ErrorMessages.GenericError, ex);
                }
            }
        }
        
        /// <summary>
        /// Adds the authentication token to the request headers if available.
        /// </summary>
        /// <param name="request">The HTTP request message to add the header to.</param>
        /// <param name="requiresAuth">Whether authentication is required for this request.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task AddAuthenticationHeader(HttpRequestMessage request, bool requiresAuth)
        {
            if (requiresAuth)
            {
                bool isTokenValid = await tokenManager.IsTokenValid();
                if (isTokenValid)
                {
                    string token = await tokenManager.RetrieveToken();
                    if (!string.IsNullOrEmpty(token))
                    {
                        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    }
                    else
                    {
                        telemetryService.Log(LogLevel.Error, "Authentication token required but not available.");
                        throw new UnauthorizedAccessException(ErrorMessages.UnauthorizedAccess);
                    }
                }
                else
                {
                    telemetryService.Log(LogLevel.Error, "Authentication token required but invalid or expired.");
                    throw new UnauthorizedAccessException(ErrorMessages.SessionExpired);
                }
            }
        }
        
        /// <summary>
        /// Builds a complete URL with query parameters.
        /// </summary>
        /// <param name="endpoint">The API endpoint path.</param>
        /// <param name="queryParams">Optional query parameters.</param>
        /// <returns>The complete URL with query parameters.</returns>
        private string BuildUrl(string endpoint, Dictionary<string, string> queryParams)
        {
            if (queryParams == null || !queryParams.Any())
            {
                return endpoint;
            }

            var queryString = string.Join("&", queryParams.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
            return $"{endpoint}?{queryString}";
        }
        
        /// <summary>
        /// Handles the HTTP response and throws appropriate exceptions for error status codes.
        /// </summary>
        /// <param name="response">The HTTP response message to handle.</param>
        private void HandleResponse(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            telemetryService.Log(LogLevel.Warning, $"API request failed with status code: {response.StatusCode}");

            switch (response.StatusCode)
            {
                case HttpStatusCode.Unauthorized: // 401
                    throw new UnauthorizedAccessException(ErrorMessages.UnauthorizedAccess);
                    
                case HttpStatusCode.Forbidden: // 403
                    throw new UnauthorizedAccessException(ErrorMessages.UnauthorizedAccess);
                    
                case HttpStatusCode.NotFound: // 404
                    throw new KeyNotFoundException($"Resource not found: {response.RequestMessage.RequestUri}");
                    
                case HttpStatusCode.BadRequest: // 400
                    {
                        string content = response.Content.ReadAsStringAsync().Result;
                        throw new ArgumentException($"Bad request: {content}");
                    }
                    
                default:
                    if ((int)response.StatusCode >= 500)
                    {
                        throw new InvalidOperationException(ErrorMessages.ServerError);
                    }
                    else
                    {
                        throw new HttpRequestException($"HTTP error: {(int)response.StatusCode} {response.ReasonPhrase}");
                    }
            }
        }
        
        /// <summary>
        /// Configures the retry policy for HTTP requests.
        /// </summary>
        /// <returns>The configured retry policy.</returns>
        private IAsyncPolicy<HttpResponseMessage> ConfigureRetryPolicy()
        {
            return Policy
                .Handle<HttpRequestException>()
                .Or<TaskCanceledException>() // For timeouts
                .OrResult<HttpResponseMessage>(response =>
                {
                    // Retry on these status codes
                    return response.StatusCode == HttpStatusCode.RequestTimeout || // 408
                           response.StatusCode == HttpStatusCode.TooManyRequests || // 429
                           response.StatusCode == HttpStatusCode.InternalServerError || // 500
                           response.StatusCode == HttpStatusCode.BadGateway || // 502
                           response.StatusCode == HttpStatusCode.ServiceUnavailable || // 503
                           response.StatusCode == HttpStatusCode.GatewayTimeout; // 504
                })
                .WaitAndRetryAsync(
                    3, // Number of retries
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff: 1s, 2s, 4s
                    (result, timeSpan, retryCount, context) =>
                    {
                        if (result.Exception != null)
                        {
                            telemetryService.Log(
                                LogLevel.Warning,
                                $"Retry {retryCount}/3 after {timeSpan.TotalSeconds}s due to: {result.Exception.Message}");
                        }
                        else
                        {
                            telemetryService.Log(
                                LogLevel.Warning,
                                $"Retry {retryCount}/3 after {timeSpan.TotalSeconds}s due to status code: {result.Result.StatusCode}");
                        }
                    });
        }
    }
}