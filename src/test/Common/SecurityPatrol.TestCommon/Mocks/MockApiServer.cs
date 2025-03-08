using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using WireMock.Net.StandAlone;
using WireMock.Server;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Types;
using SecurityPatrol.TestCommon.Constants;

namespace SecurityPatrol.TestCommon.Mocks
{
    /// <summary>
    /// A mock HTTP server implementation for integration testing that simulates backend API responses.
    /// </summary>
    public class MockApiServer : IDisposable
    {
        private WireMockServer _server;
        private readonly int _port;
        private bool _isRunning;
        private readonly ILogger<MockApiServer> _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly Dictionary<string, RequestMapping> _requestMappings;
        private readonly ConcurrentDictionary<string, object> _testData;

        /// <summary>
        /// Initializes a new instance of the MockApiServer class with the specified logger and port.
        /// </summary>
        /// <param name="logger">The logger for recording server operations</param>
        /// <param name="port">The port on which the server will listen (default: 9876)</param>
        public MockApiServer(ILogger<MockApiServer> logger, int port = 9876)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _port = port;
            _isRunning = false;
            _requestMappings = new Dictionary<string, RequestMapping>();
            _testData = new ConcurrentDictionary<string, object>();
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
            
            _server = WireMockServer.Start(new WireMockServerSettings
            {
                Port = _port,
                UseSSL = false
            });
        }

        /// <summary>
        /// Starts the mock API server if it's not already running.
        /// </summary>
        public void Start()
        {
            if (!_isRunning)
            {
                _logger.LogInformation("Starting mock API server on port {Port}", _port);
                
                if (_server == null || !_server.IsStarted)
                {
                    _server = WireMockServer.Start(new WireMockServerSettings
                    {
                        Port = _port,
                        UseSSL = false
                    });
                }
                
                SetupDefaultEndpoints();
                _isRunning = true;
                _logger.LogInformation("Mock API server started at {Url}", GetBaseUrl());
            }
            else
            {
                _logger.LogWarning("Mock API server is already running");
            }
        }

        /// <summary>
        /// Stops the mock API server if it's running.
        /// </summary>
        public void Stop()
        {
            if (_isRunning)
            {
                _logger.LogInformation("Stopping mock API server");
                _server?.Stop();
                _isRunning = false;
                _logger.LogInformation("Mock API server stopped");
            }
            else
            {
                _logger.LogWarning("Mock API server is not running");
            }
        }

        /// <summary>
        /// Gets the base URL of the mock API server.
        /// </summary>
        /// <returns>The base URL of the mock server</returns>
        public string GetBaseUrl()
        {
            return $"http://localhost:{_port}";
        }

        /// <summary>
        /// Resets all request mappings to their default state.
        /// </summary>
        public void ResetMappings()
        {
            _logger.LogInformation("Resetting all request mappings");
            _server.Reset();
            SetupDefaultEndpoints();
            _requestMappings.Clear();
            _logger.LogInformation("Request mappings reset to defaults");
        }

        /// <summary>
        /// Sets up a successful response for the specified endpoint with the given response object.
        /// </summary>
        /// <param name="endpoint">The API endpoint to configure</param>
        /// <param name="responseObject">The object to return as JSON response</param>
        public void SetupSuccessResponse(string endpoint, object responseObject)
        {
            string responseJson = JsonSerializer.Serialize(responseObject, _jsonOptions);
            
            _server
                .Given(Request.Create().WithPath(endpoint).UsingAnyMethod())
                .RespondWith(Response.Create()
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(responseJson));
            
            _requestMappings[endpoint] = new RequestMapping(
                endpoint,
                (int)HttpStatusCode.OK,
                responseJson,
                0);
            
            _logger.LogInformation("Setup success response for endpoint {Endpoint}", endpoint);
        }

        /// <summary>
        /// Sets up an error response for the specified endpoint with the given status code and error message.
        /// </summary>
        /// <param name="endpoint">The API endpoint to configure</param>
        /// <param name="statusCode">The HTTP status code to return</param>
        /// <param name="errorMessage">The error message to return</param>
        public void SetupErrorResponse(string endpoint, int statusCode, string errorMessage)
        {
            var errorResponse = new { error = true, message = errorMessage };
            string responseJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
            
            _server
                .Given(Request.Create().WithPath(endpoint).UsingAnyMethod())
                .RespondWith(Response.Create()
                    .WithStatusCode(statusCode)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(responseJson));
            
            _requestMappings[endpoint] = new RequestMapping(
                endpoint,
                statusCode,
                responseJson,
                0);
            
            _logger.LogInformation("Setup error response for endpoint {Endpoint} with status code {StatusCode}", endpoint, statusCode);
        }

        /// <summary>
        /// Sets up a delayed response for the specified endpoint to simulate network latency or timeouts.
        /// </summary>
        /// <param name="endpoint">The API endpoint to configure</param>
        /// <param name="responseObject">The object to return as JSON response</param>
        /// <param name="delayMilliseconds">The delay in milliseconds before responding</param>
        public void SetupDelayedResponse(string endpoint, object responseObject, int delayMilliseconds)
        {
            string responseJson = JsonSerializer.Serialize(responseObject, _jsonOptions);
            
            _server
                .Given(Request.Create().WithPath(endpoint).UsingAnyMethod())
                .RespondWith(Response.Create()
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(responseJson)
                    .WithDelay(TimeSpan.FromMilliseconds(delayMilliseconds)));
            
            _requestMappings[endpoint] = new RequestMapping(
                endpoint,
                (int)HttpStatusCode.OK,
                responseJson,
                delayMilliseconds);
            
            _logger.LogInformation("Setup delayed response for endpoint {Endpoint} with delay {Delay}ms", endpoint, delayMilliseconds);
        }

        /// <summary>
        /// Sets up a custom response for the specified endpoint with the given response body and status code.
        /// </summary>
        /// <param name="endpoint">The API endpoint to configure</param>
        /// <param name="responseBody">The response body (object will be serialized to JSON)</param>
        /// <param name="statusCode">The HTTP status code to return</param>
        public void SetupCustomResponse(string endpoint, object responseBody, int statusCode)
        {
            string responseJson;
            
            if (responseBody is string stringBody)
            {
                responseJson = stringBody;
            }
            else
            {
                responseJson = JsonSerializer.Serialize(responseBody, _jsonOptions);
            }
            
            _server
                .Given(Request.Create().WithPath(endpoint).UsingAnyMethod())
                .RespondWith(Response.Create()
                    .WithStatusCode(statusCode)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(responseJson));
            
            _requestMappings[endpoint] = new RequestMapping(
                endpoint,
                statusCode,
                responseJson,
                0);
            
            _logger.LogInformation("Setup custom response for endpoint {Endpoint} with status code {StatusCode}", endpoint, statusCode);
        }

        /// <summary>
        /// Stores test-specific data for use in mock responses or test verification.
        /// </summary>
        /// <param name="key">The key to identify the data</param>
        /// <param name="value">The value to store</param>
        public void StoreTestData(string key, object value)
        {
            _testData[key] = value;
            _logger.LogInformation("Stored test data with key {Key}", key);
        }

        /// <summary>
        /// Retrieves test-specific data previously stored.
        /// </summary>
        /// <param name="key">The key of the data to retrieve</param>
        /// <returns>The stored data, or null if not found</returns>
        public object GetTestData(string key)
        {
            if (_testData.TryGetValue(key, out var value))
            {
                return value;
            }
            
            _logger.LogWarning("Test data with key {Key} not found", key);
            return null;
        }

        /// <summary>
        /// Clears all test-specific data.
        /// </summary>
        public void ClearTestData()
        {
            _testData.Clear();
            _logger.LogInformation("All test data cleared");
        }

        /// <summary>
        /// Sets up the authentication verification endpoint with default success response.
        /// </summary>
        private void SetupAuthVerifyEndpoint()
        {
            var response = new
            {
                VerificationId = Guid.NewGuid().ToString()
            };
            
            _server
                .Given(Request.Create().WithPath("/auth/verify").UsingPost())
                .RespondWith(Response.Create()
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(JsonSerializer.Serialize(response, _jsonOptions)));
            
            _requestMappings["/auth/verify"] = new RequestMapping(
                "/auth/verify",
                (int)HttpStatusCode.OK,
                JsonSerializer.Serialize(response, _jsonOptions),
                0);
            
            _logger.LogInformation("Setup default auth verify endpoint");
        }

        /// <summary>
        /// Sets up the authentication validation endpoint with default success response.
        /// </summary>
        private void SetupAuthValidateEndpoint()
        {
            var response = new
            {
                Token = TestConstants.TestAuthToken,
                ExpiresAt = DateTime.UtcNow.AddHours(8).ToString("o")
            };
            
            _server
                .Given(Request.Create().WithPath("/auth/validate").UsingPost())
                .RespondWith(Response.Create()
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(JsonSerializer.Serialize(response, _jsonOptions)));
            
            _requestMappings["/auth/validate"] = new RequestMapping(
                "/auth/validate",
                (int)HttpStatusCode.OK,
                JsonSerializer.Serialize(response, _jsonOptions),
                0);
            
            _logger.LogInformation("Setup default auth validate endpoint");
        }

        /// <summary>
        /// Sets up the authentication refresh endpoint with default success response.
        /// </summary>
        private void SetupAuthRefreshEndpoint()
        {
            var response = new
            {
                Token = TestConstants.TestAuthToken,
                RefreshToken = TestConstants.TestRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(8).ToString("o")
            };
            
            _server
                .Given(Request.Create().WithPath("/auth/refresh").UsingPost())
                .RespondWith(Response.Create()
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(JsonSerializer.Serialize(response, _jsonOptions)));
            
            _requestMappings["/auth/refresh"] = new RequestMapping(
                "/auth/refresh",
                (int)HttpStatusCode.OK,
                JsonSerializer.Serialize(response, _jsonOptions),
                0);
            
            _logger.LogInformation("Setup default auth refresh endpoint");
        }

        /// <summary>
        /// Sets up the time clock endpoint with default success response.
        /// </summary>
        private void SetupTimeClockEndpoint()
        {
            var response = new
            {
                Id = Guid.NewGuid().ToString(),
                Status = "success"
            };
            
            _server
                .Given(Request.Create().WithPath("/time/clock").UsingPost())
                .RespondWith(Response.Create()
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(JsonSerializer.Serialize(response, _jsonOptions)));
            
            _requestMappings["/time/clock"] = new RequestMapping(
                "/time/clock",
                (int)HttpStatusCode.OK,
                JsonSerializer.Serialize(response, _jsonOptions),
                0);
            
            _logger.LogInformation("Setup default time clock endpoint");
        }

        /// <summary>
        /// Sets up the time history endpoint with default success response.
        /// </summary>
        private void SetupTimeHistoryEndpoint()
        {
            var now = DateTime.UtcNow;
            var response = new[]
            {
                new
                {
                    Id = Guid.NewGuid().ToString(),
                    Type = "clockIn",
                    Timestamp = now.AddHours(-8).ToString("o"),
                    Location = new
                    {
                        Latitude = TestConstants.TestLatitude,
                        Longitude = TestConstants.TestLongitude
                    }
                },
                new
                {
                    Id = Guid.NewGuid().ToString(),
                    Type = "clockOut",
                    Timestamp = now.AddHours(-1).ToString("o"),
                    Location = new
                    {
                        Latitude = TestConstants.TestLatitude + 0.01,
                        Longitude = TestConstants.TestLongitude - 0.01
                    }
                }
            };
            
            _server
                .Given(Request.Create().WithPath("/time/history").UsingGet())
                .RespondWith(Response.Create()
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(JsonSerializer.Serialize(response, _jsonOptions)));
            
            _requestMappings["/time/history"] = new RequestMapping(
                "/time/history",
                (int)HttpStatusCode.OK,
                JsonSerializer.Serialize(response, _jsonOptions),
                0);
            
            _logger.LogInformation("Setup default time history endpoint");
        }

        /// <summary>
        /// Sets up the location batch endpoint with default success response.
        /// </summary>
        private void SetupLocationBatchEndpoint()
        {
            var response = new
            {
                Processed = 10,
                Failed = 0
            };
            
            _server
                .Given(Request.Create().WithPath("/location/batch").UsingPost())
                .RespondWith(Response.Create()
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(JsonSerializer.Serialize(response, _jsonOptions)));
            
            _requestMappings["/location/batch"] = new RequestMapping(
                "/location/batch",
                (int)HttpStatusCode.OK,
                JsonSerializer.Serialize(response, _jsonOptions),
                0);
            
            _logger.LogInformation("Setup default location batch endpoint");
        }

        /// <summary>
        /// Sets up the photos upload endpoint with default success response.
        /// </summary>
        private void SetupPhotosUploadEndpoint()
        {
            var response = new
            {
                Id = Guid.NewGuid().ToString(),
                Status = "success"
            };
            
            _server
                .Given(Request.Create().WithPath("/photos/upload").UsingPost())
                .RespondWith(Response.Create()
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(JsonSerializer.Serialize(response, _jsonOptions)));
            
            _requestMappings["/photos/upload"] = new RequestMapping(
                "/photos/upload",
                (int)HttpStatusCode.OK,
                JsonSerializer.Serialize(response, _jsonOptions),
                0);
            
            _logger.LogInformation("Setup default photos upload endpoint");
        }

        /// <summary>
        /// Sets up the reports endpoint with default success response.
        /// </summary>
        private void SetupReportsEndpoint()
        {
            // POST response (create report)
            var postResponse = new
            {
                Id = Guid.NewGuid().ToString(),
                Status = "success"
            };
            
            _server
                .Given(Request.Create().WithPath("/reports").UsingPost())
                .RespondWith(Response.Create()
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(JsonSerializer.Serialize(postResponse, _jsonOptions)));
            
            // GET response (retrieve reports)
            var now = DateTime.UtcNow;
            var getResponse = new[]
            {
                new
                {
                    Id = Guid.NewGuid().ToString(),
                    Text = "Test report 1",
                    Timestamp = now.AddHours(-5).ToString("o")
                },
                new
                {
                    Id = Guid.NewGuid().ToString(),
                    Text = "Test report 2",
                    Timestamp = now.AddHours(-2).ToString("o")
                }
            };
            
            _server
                .Given(Request.Create().WithPath("/reports").UsingGet())
                .RespondWith(Response.Create()
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(JsonSerializer.Serialize(getResponse, _jsonOptions)));
            
            _requestMappings["/reports"] = new RequestMapping(
                "/reports",
                (int)HttpStatusCode.OK,
                "Multiple handlers for GET/POST",
                0);
            
            _logger.LogInformation("Setup default reports endpoints");
        }

        /// <summary>
        /// Sets up the patrol locations endpoint with default success response.
        /// </summary>
        private void SetupPatrolLocationsEndpoint()
        {
            var response = new[]
            {
                new
                {
                    Id = 1,
                    Name = "Office Building",
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                },
                new
                {
                    Id = 2,
                    Name = "Warehouse",
                    Latitude = TestConstants.TestLatitude + 0.05,
                    Longitude = TestConstants.TestLongitude - 0.05
                }
            };
            
            _server
                .Given(Request.Create().WithPath("/patrol/locations").UsingGet())
                .RespondWith(Response.Create()
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(JsonSerializer.Serialize(response, _jsonOptions)));
            
            _requestMappings["/patrol/locations"] = new RequestMapping(
                "/patrol/locations",
                (int)HttpStatusCode.OK,
                JsonSerializer.Serialize(response, _jsonOptions),
                0);
            
            _logger.LogInformation("Setup default patrol locations endpoint");
        }

        /// <summary>
        /// Sets up the patrol checkpoints endpoint with default success response.
        /// </summary>
        private void SetupPatrolCheckpointsEndpoint()
        {
            var response = new[]
            {
                new
                {
                    Id = 101,
                    LocationId = 1,
                    Name = "Main Entrance",
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                },
                new
                {
                    Id = 102,
                    LocationId = 1,
                    Name = "East Wing",
                    Latitude = TestConstants.TestLatitude + 0.001,
                    Longitude = TestConstants.TestLongitude + 0.001
                },
                new
                {
                    Id = 103,
                    LocationId = 1,
                    Name = "Parking Lot",
                    Latitude = TestConstants.TestLatitude - 0.001,
                    Longitude = TestConstants.TestLongitude - 0.001
                }
            };
            
            _server
                .Given(Request.Create().WithPath("/patrol/checkpoints").UsingGet())
                .RespondWith(Response.Create()
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(JsonSerializer.Serialize(response, _jsonOptions)));
            
            _requestMappings["/patrol/checkpoints"] = new RequestMapping(
                "/patrol/checkpoints",
                (int)HttpStatusCode.OK,
                JsonSerializer.Serialize(response, _jsonOptions),
                0);
            
            _logger.LogInformation("Setup default patrol checkpoints endpoint");
        }

        /// <summary>
        /// Sets up the patrol verify endpoint with default success response.
        /// </summary>
        private void SetupPatrolVerifyEndpoint()
        {
            var response = new
            {
                Status = "success"
            };
            
            _server
                .Given(Request.Create().WithPath("/patrol/verify").UsingPost())
                .RespondWith(Response.Create()
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(JsonSerializer.Serialize(response, _jsonOptions)));
            
            _requestMappings["/patrol/verify"] = new RequestMapping(
                "/patrol/verify",
                (int)HttpStatusCode.OK,
                JsonSerializer.Serialize(response, _jsonOptions),
                0);
            
            _logger.LogInformation("Setup default patrol verify endpoint");
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
        /// <param name="endpoint">The endpoint to check</param>
        /// <returns>The number of requests received</returns>
        public int GetRequestCount(string endpoint)
        {
            var requestCount = _server.LogEntries.Count(x => x.RequestMessage.Path == endpoint);
            return requestCount;
        }

        /// <summary>
        /// Gets the body of the last request received for a specific endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint to check</param>
        /// <returns>The body of the last request, or null if no requests</returns>
        public string GetLastRequestBody(string endpoint)
        {
            var lastRequest = _server.LogEntries
                .Where(x => x.RequestMessage.Path == endpoint)
                .OrderByDescending(x => x.RequestMessage.DateTime)
                .FirstOrDefault();
            
            return lastRequest?.RequestMessage?.Body;
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
            
            _server?.Dispose();
            _server = null;
            _testData.Clear();
            
            _logger.LogInformation("MockApiServer disposed");
        }
    }

    /// <summary>
    /// A class that represents a request mapping configuration for the mock server.
    /// </summary>
    internal class RequestMapping
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