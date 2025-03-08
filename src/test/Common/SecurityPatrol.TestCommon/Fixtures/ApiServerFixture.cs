using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;
using SecurityPatrol.TestCommon.Mocks;
using SecurityPatrol.TestCommon.Constants;

namespace SecurityPatrol.TestCommon.Fixtures
{
    /// <summary>
    /// A test fixture that provides a configured mock API server for integration testing.
    /// Implements IAsyncLifetime for proper test lifecycle management.
    /// </summary>
    public class ApiServerFixture : IAsyncLifetime, IDisposable
    {
        /// <summary>
        /// Gets the mock API server instance
        /// </summary>
        public MockApiServer Server { get; private set; }

        /// <summary>
        /// Gets the base URL of the mock API server
        /// </summary>
        public string BaseUrl { get; private set; }

        private readonly ILogger<ApiServerFixture> _logger;
        private bool _disposed;
        private readonly int _port;

        /// <summary>
        /// Initializes a new instance of the ApiServerFixture class with a default or specified port.
        /// </summary>
        /// <param name="port">The port to use for the mock server (default: random port)</param>
        public ApiServerFixture(int port = 0)
        {
            _port = port;
            
            // Create a logger factory and logger for the fixture
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddConsole()
                    .SetMinimumLevel(LogLevel.Information);
            });
            
            _logger = loggerFactory.CreateLogger<ApiServerFixture>();
            _disposed = false;
            
            _logger.LogInformation("ApiServerFixture initialized with port {Port}", _port == 0 ? "random" : _port.ToString());
        }

        /// <summary>
        /// Initializes the mock API server asynchronously. This is called automatically by the xUnit test framework before tests run.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task InitializeAsync()
        {
            _logger.LogInformation("Initializing mock API server...");
            
            try
            {
                // Create the mock server
                Server = new MockApiServer(_logger, _port);
                
                // Start the server
                Server.Start();
                
                // Get the base URL
                BaseUrl = Server.GetBaseUrl();
                
                _logger.LogInformation("Mock API server initialized at {BaseUrl}", BaseUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize mock API server");
                throw;
            }
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// Disposes the mock API server asynchronously. This is called automatically by the xUnit test framework after tests complete.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task DisposeAsync()
        {
            _logger.LogInformation("Disposing mock API server...");
            
            try
            {
                // Stop the server
                if (Server != null)
                {
                    Server.Stop();
                    Server = null;
                }
                
                _logger.LogInformation("Mock API server disposed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while disposing mock API server");
            }
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// Resets the mock server to its initial state, clearing all custom response mappings.
        /// </summary>
        public void ResetServer()
        {
            if (Server != null)
            {
                try
                {
                    Server.ResetMappings();
                    _logger.LogInformation("Mock API server reset to default state");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while resetting mock API server");
                    throw;
                }
            }
        }

        /// <summary>
        /// Sets up a successful response for the specified endpoint with the given response object.
        /// </summary>
        /// <param name="endpoint">The API endpoint to configure</param>
        /// <param name="responseObject">The object to return as JSON response</param>
        public void SetupSuccessResponse(string endpoint, object responseObject)
        {
            if (Server != null)
            {
                try
                {
                    Server.SetupSuccessResponse(endpoint, responseObject);
                    _logger.LogInformation("Setup success response for endpoint {Endpoint}", endpoint);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while setting up success response for endpoint {Endpoint}", endpoint);
                    throw;
                }
            }
        }

        /// <summary>
        /// Sets up an error response for the specified endpoint with the given status code and error message.
        /// </summary>
        /// <param name="endpoint">The API endpoint to configure</param>
        /// <param name="statusCode">The HTTP status code to return</param>
        /// <param name="errorMessage">The error message to return</param>
        public void SetupErrorResponse(string endpoint, int statusCode, string errorMessage)
        {
            if (Server != null)
            {
                try
                {
                    Server.SetupErrorResponse(endpoint, statusCode, errorMessage);
                    _logger.LogInformation("Setup error response for endpoint {Endpoint} with status code {StatusCode}", endpoint, statusCode);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while setting up error response for endpoint {Endpoint}", endpoint);
                    throw;
                }
            }
        }

        /// <summary>
        /// Sets up a delayed response for the specified endpoint to simulate network latency or timeouts.
        /// </summary>
        /// <param name="endpoint">The API endpoint to configure</param>
        /// <param name="responseObject">The object to return as JSON response</param>
        /// <param name="delayMilliseconds">The delay in milliseconds before responding</param>
        public void SetupDelayedResponse(string endpoint, object responseObject, int delayMilliseconds)
        {
            if (Server != null)
            {
                try
                {
                    Server.SetupDelayedResponse(endpoint, responseObject, delayMilliseconds);
                    _logger.LogInformation("Setup delayed response for endpoint {Endpoint} with delay {Delay}ms", endpoint, delayMilliseconds);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while setting up delayed response for endpoint {Endpoint}", endpoint);
                    throw;
                }
            }
        }

        /// <summary>
        /// Gets the number of requests received for a specific endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint to check</param>
        /// <returns>The number of requests received</returns>
        public int GetRequestCount(string endpoint)
        {
            if (Server != null)
            {
                try
                {
                    return Server.GetRequestCount(endpoint);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while getting request count for endpoint {Endpoint}", endpoint);
                    throw;
                }
            }
            
            return 0;
        }

        /// <summary>
        /// Gets the body of the last request received for a specific endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint to check</param>
        /// <returns>The body of the last request or null if no requests</returns>
        public string GetLastRequestBody(string endpoint)
        {
            if (Server != null)
            {
                try
                {
                    return Server.GetLastRequestBody(endpoint);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while getting last request body for endpoint {Endpoint}", endpoint);
                    throw;
                }
            }
            
            return null;
        }

        /// <summary>
        /// Disposes the fixture and releases resources.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                
                try
                {
                    if (Server != null)
                    {
                        Server.Stop();
                        Server = null;
                    }
                    
                    _logger.LogInformation("ApiServerFixture disposed");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while disposing ApiServerFixture");
                }
            }
        }
    }
}