using System; // System 8.0.0
using System.Net; // System.Net 8.0.0
using System.Net.Http; // System.Net.Http 8.0.0
using System.Threading; // System.Threading 8.0.0
using System.Threading.Tasks; // System.Threading.Tasks 8.0.0
using System.Text; // System.Text 8.0.0
using System.Text.Json; // System.Text.Json 8.0.0
using System.Collections.Generic; // System.Collections.Generic 8.0.0
using System.Linq; // System.Collections.Generic 8.0.0
using Microsoft.Extensions.Logging; // Microsoft.Extensions.Logging 8.0.0
using WireMock.Net; // WireMock.Net 1.5.13
using WireMock.Net.StandAlone; // WireMock.Net.StandAlone 1.5.13
using SecurityPatrol.Constants;

namespace SecurityPatrol.IntegrationTests.Helpers
{
    /// <summary>
    /// A mock HTTP server implementation for integration testing that simulates backend API responses.
    /// </summary>
    public class MockApiServer : IDisposable
    {
        private readonly WireMockServer _server;
        private readonly int _port;
        private bool _isRunning;
        private readonly ILogger<MockApiServer> _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly Dictionary<string, RequestMapping> _requestMappings;

        /// <summary>
        /// Initializes a new instance of the MockApiServer class with the specified logger and port.
        /// </summary>
        /// <param name="logger">The logger to use for logging server operations.</param>
        /// <param name="port">The port to listen on (default is 9000).</param>
        public MockApiServer(ILogger<MockApiServer> logger, int port = 9000)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _port = port;
            _isRunning = false;
            _requestMappings = new Dictionary<string, RequestMapping>();
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
            
            _server = WireMockServer.Start(new WireMockServerSettings
            {
                Port = _port,
                StartAdminInterface = true
            });
        }

        /// <summary>
        /// Starts the mock API server if it's not already running.
        /// </summary>
        public void Start()
        {
            if (_isRunning)
            {
                _logger.LogInformation("Mock API server is already running on port {Port}", _port);
                return;
            }
            
            _isRunning = true;
            SetupDefaultEndpoints();
            
            _logger.LogInformation("Mock API server started on {Url}", GetBaseUrl());
        }

        /// <summary>
        /// Stops the mock API server if it's running.
        /// </summary>
        public void Stop()
        {
            if (!_isRunning)
            {
                _logger.LogInformation("Mock API server is not running");
                return;
            }
            
            _server.Stop();
            _isRunning = false;
            
            _logger.LogInformation("Mock API server stopped");
        }

        /// <summary>
        /// Gets the base URL of the mock API server.
        /// </summary>
        /// <returns>The base URL of the mock server.</returns>
        public string GetBaseUrl()
        {
            return $"http://localhost:{_port}";
        }

        /// <summary>
        /// Resets all request mappings to their default state.
        /// </summary>
        public void ResetMappings()
        {
            _server.Reset();
            SetupDefaultEndpoints();
            _requestMappings.Clear();
            
            _logger.LogInformation("Mock API server mappings reset to defaults");
        }

        /// <summary>
        /// Sets up a successful response for the specified endpoint with the given response object.
        /// </summary>
        /// <param name="endpoint">The API endpoint path (e.g., "/api/v1/auth/verify").</param>
        /// <param name="responseObject">The object to serialize as the response body.</param>
        public void SetupSuccessResponse(string endpoint, object responseObject)
        {
            var responseJson = JsonSerializer.Serialize(responseObject, _jsonOptions);
            
            _server
                .Given(WireMock.RequestBuilders.Request.Create().WithPath(endpoint).UsingAnyMethod())
                .RespondWith(WireMock.ResponseBuilders.Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(responseJson));
            
            _requestMappings[endpoint] = new RequestMapping(endpoint, 200, responseJson, 0);
            
            _logger.LogInformation("Set up success response for endpoint {Endpoint}", endpoint);
        }

        /// <summary>
        /// Sets up an error response for the specified endpoint with the given status code and error message.
        /// </summary>
        /// <param name="endpoint">The API endpoint path.</param>
        /// <param name="statusCode">The HTTP status code to return.</param>
        /// <param name="errorMessage">The error message to include in the response.</param>
        public void SetupErrorResponse(string endpoint, int statusCode, string errorMessage)
        {
            var errorResponse = new { error = errorMessage };
            var responseJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
            
            _server
                .Given(WireMock.RequestBuilders.Request.Create().WithPath(endpoint).UsingAnyMethod())
                .RespondWith(WireMock.ResponseBuilders.Response.Create()
                    .WithStatusCode(statusCode)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(responseJson));
            
            _requestMappings[endpoint] = new RequestMapping(endpoint, statusCode, responseJson, 0);
            
            _logger.LogInformation("Set up error response for endpoint {Endpoint} with status code {StatusCode}", endpoint, statusCode);
        }

        /// <summary>
        /// Sets up a delayed response for the specified endpoint to simulate network latency or timeouts.
        /// </summary>
        /// <param name="endpoint">The API endpoint path.</param>
        /// <param name="responseObject">The object to serialize as the response body.</param>
        /// <param name="delayMilliseconds">The delay in milliseconds before responding.</param>
        public void SetupDelayedResponse(string endpoint, object responseObject, int delayMilliseconds)
        {
            var responseJson = JsonSerializer.Serialize(responseObject, _jsonOptions);
            
            _server
                .Given(WireMock.RequestBuilders.Request.Create().WithPath(endpoint).UsingAnyMethod())
                .RespondWith(WireMock.ResponseBuilders.Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(responseJson)
                    .WithDelay(TimeSpan.FromMilliseconds(delayMilliseconds)));
            
            _requestMappings[endpoint] = new RequestMapping(endpoint, 200, responseJson, delayMilliseconds);
            
            _logger.LogInformation("Set up delayed response for endpoint {Endpoint} with delay {Delay}ms", endpoint, delayMilliseconds);
        }

        /// <summary>
        /// Sets up the authentication verification endpoint with default success response.
        /// </summary>
        private void SetupAuthVerifyEndpoint()
        {
            var responseObj = new 
            { 
                verificationId = Guid.NewGuid().ToString()
            };
            
            string path = ExtractPathFromUrl(ApiEndpoints.AuthVerify);
            SetupSuccessResponse(path, responseObj);
        }

        /// <summary>
        /// Sets up the authentication validation endpoint with default success response.
        /// </summary>
        private void SetupAuthValidateEndpoint()
        {
            var responseObj = new 
            { 
                token = $"mock_token_{Guid.NewGuid()}",
                expiresAt = DateTime.UtcNow.AddHours(8).ToString("o")
            };
            
            string path = ExtractPathFromUrl(ApiEndpoints.AuthValidate);
            SetupSuccessResponse(path, responseObj);
        }

        /// <summary>
        /// Sets up the authentication refresh endpoint with default success response.
        /// </summary>
        private void SetupAuthRefreshEndpoint()
        {
            var responseObj = new 
            { 
                token = $"mock_refreshed_token_{Guid.NewGuid()}",
                expiresAt = DateTime.UtcNow.AddHours(8).ToString("o")
            };
            
            string path = ExtractPathFromUrl(ApiEndpoints.AuthRefresh);
            SetupSuccessResponse(path, responseObj);
        }

        /// <summary>
        /// Sets up the time clock endpoint with default success response.
        /// </summary>
        private void SetupTimeClockEndpoint()
        {
            var responseObj = new 
            { 
                id = Guid.NewGuid().ToString(),
                status = "success"
            };
            
            string path = ExtractPathFromUrl(ApiEndpoints.TimeClock);
            SetupSuccessResponse(path, responseObj);
        }

        /// <summary>
        /// Sets up the time history endpoint with default success response.
        /// </summary>
        private void SetupTimeHistoryEndpoint()
        {
            var responseObj = new[] 
            {
                new 
                { 
                    id = Guid.NewGuid().ToString(),
                    type = "clockIn",
                    timestamp = DateTime.UtcNow.AddHours(-8).ToString("o"),
                    location = new { latitude = 37.7749, longitude = -122.4194 }
                },
                new 
                { 
                    id = Guid.NewGuid().ToString(),
                    type = "clockOut",
                    timestamp = DateTime.UtcNow.AddHours(-1).ToString("o"),
                    location = new { latitude = 37.7749, longitude = -122.4194 }
                }
            };
            
            string path = ExtractPathFromUrl(ApiEndpoints.TimeHistory);
            SetupSuccessResponse(path, responseObj);
        }

        /// <summary>
        /// Sets up the location batch endpoint with default success response.
        /// </summary>
        private void SetupLocationBatchEndpoint()
        {
            var responseObj = new 
            { 
                processed = 10,
                failed = 0
            };
            
            string path = ExtractPathFromUrl(ApiEndpoints.LocationBatch);
            SetupSuccessResponse(path, responseObj);
        }

        /// <summary>
        /// Sets up the photos upload endpoint with default success response.
        /// </summary>
        private void SetupPhotosUploadEndpoint()
        {
            var responseObj = new 
            { 
                id = Guid.NewGuid().ToString(),
                status = "success"
            };
            
            string path = ExtractPathFromUrl(ApiEndpoints.PhotosUpload);
            SetupSuccessResponse(path, responseObj);
        }

        /// <summary>
        /// Sets up the reports endpoint with default success response.
        /// </summary>
        private void SetupReportsEndpoint()
        {
            var responseObj = new 
            { 
                id = Guid.NewGuid().ToString(),
                status = "success"
            };
            
            string path = ExtractPathFromUrl(ApiEndpoints.Reports);
            SetupSuccessResponse(path, responseObj);
        }

        /// <summary>
        /// Sets up the patrol locations endpoint with default success response.
        /// </summary>
        private void SetupPatrolLocationsEndpoint()
        {
            var responseObj = new[] 
            {
                new 
                { 
                    id = 1,
                    name = "North Building",
                    latitude = 37.7749,
                    longitude = -122.4194
                },
                new 
                { 
                    id = 2,
                    name = "South Building",
                    latitude = 37.7639,
                    longitude = -122.4089
                }
            };
            
            string path = ExtractPathFromUrl(ApiEndpoints.PatrolLocations);
            SetupSuccessResponse(path, responseObj);
        }

        /// <summary>
        /// Sets up the patrol checkpoints endpoint with default success response.
        /// </summary>
        private void SetupPatrolCheckpointsEndpoint()
        {
            var responseObj = new[] 
            {
                new 
                { 
                    id = 1,
                    locationId = 1,
                    name = "Front Entrance",
                    latitude = 37.7749,
                    longitude = -122.4194
                },
                new 
                { 
                    id = 2,
                    locationId = 1,
                    name = "Back Entrance",
                    latitude = 37.7746,
                    longitude = -122.4191
                },
                new 
                { 
                    id = 3,
                    locationId = 1,
                    name = "Parking Lot",
                    latitude = 37.7752,
                    longitude = -122.4198
                }
            };
            
            string path = ExtractPathFromUrl(ApiEndpoints.PatrolCheckpoints);
            SetupSuccessResponse(path, responseObj);
        }

        /// <summary>
        /// Sets up the patrol verify endpoint with default success response.
        /// </summary>
        private void SetupPatrolVerifyEndpoint()
        {
            var responseObj = new 
            { 
                status = "success"
            };
            
            string path = ExtractPathFromUrl(ApiEndpoints.PatrolVerify);
            SetupSuccessResponse(path, responseObj);
        }

        /// <summary>
        /// Sets up all default endpoint mappings with success responses.
        /// </summary>
        private void SetupDefaultEndpoints()
        {
            SetupAuthVerifyEndpoint();
            SetupAuthValidateEndpoint();
            SetupAuthRefreshEndpoint();
            SetupTimeClockEndpoint();
            SetupTimeHistoryEndpoint();
            SetupLocationBatchEndpoint();
            SetupPhotosUploadEndpoint();
            SetupReportsEndpoint();
            SetupPatrolLocationsEndpoint();
            SetupPatrolCheckpointsEndpoint();
            SetupPatrolVerifyEndpoint();
        }

        /// <summary>
        /// Gets the number of requests received for a specific endpoint.
        /// </summary>
        /// <param name="endpoint">The API endpoint path.</param>
        /// <returns>The number of requests received.</returns>
        public int GetRequestCount(string endpoint)
        {
            return _server.LogEntries
                .Count(l => l.RequestMessage.Path == endpoint);
        }

        /// <summary>
        /// Gets the body of the last request received for a specific endpoint.
        /// </summary>
        /// <param name="endpoint">The API endpoint path.</param>
        /// <returns>The body of the last request or null if no requests.</returns>
        public string GetLastRequestBody(string endpoint)
        {
            var logEntry = _server.LogEntries
                .Where(l => l.RequestMessage.Path == endpoint)
                .OrderByDescending(l => l.RequestMessage.CreatedAt)
                .FirstOrDefault();
            
            return logEntry?.RequestMessage.Body;
        }

        /// <summary>
        /// Extracts the path and query from a full URL.
        /// </summary>
        /// <param name="url">The full URL.</param>
        /// <returns>The path and query part of the URL.</returns>
        private string ExtractPathFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return string.Empty;
            }

            try
            {
                var uri = new Uri(url);
                return uri.PathAndQuery;
            }
            catch (UriFormatException)
            {
                _logger.LogWarning("Invalid URL format: {Url}", url);
                return url; // Return the original string if it's not a valid URI
            }
        }

        /// <summary>
        /// Disposes the mock server and releases resources.
        /// </summary>
        public void Dispose()
        {
            if (_isRunning)
            {
                Stop();
            }
            
            _server.Dispose();
            _logger.LogInformation("Mock API server disposed");
        }

        /// <summary>
        /// A class that represents a request mapping configuration for the mock server.
        /// </summary>
        private class RequestMapping
        {
            public string Endpoint { get; }
            public int StatusCode { get; }
            public string ResponseBody { get; }
            public int DelayMilliseconds { get; }

            public RequestMapping(string endpoint, int statusCode, string responseBody, int delayMilliseconds)
            {
                Endpoint = endpoint;
                StatusCode = statusCode;
                ResponseBody = responseBody;
                DelayMilliseconds = delayMilliseconds;
            }
        }
    }
}