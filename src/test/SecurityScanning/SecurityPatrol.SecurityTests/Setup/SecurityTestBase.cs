using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using Microsoft.Extensions.Logging;
using OWASP.ZAP.API.Client;
using Moq;
using SecurityPatrol.TestCommon.Constants;
using SecurityPatrol.TestCommon.Fixtures;
using SecurityPatrol.Helpers;
using SecurityPatrol.Services;

namespace SecurityPatrol.SecurityTests.Setup
{
    /// <summary>
    /// Base class for security-focused tests in the Security Patrol application,
    /// providing common functionality for testing security aspects of both API and mobile components.
    /// </summary>
    public abstract class SecurityTestBase
    {
        protected ITestOutputHelper Output { get; }
        protected ApiServerFixture ApiServer { get; }
        protected HttpClient HttpClient { get; }
        protected ILogger<SecurityTestBase> Logger { get; }
        protected TokenManager TokenManager { get; }
        protected IZapClient ZapClient { get; }
        protected bool EnableDynamicScanning { get; set; }
        protected string ReportOutputPath { get; set; }
        protected Dictionary<string, string> RequiredSecurityHeaders { get; }

        /// <summary>
        /// Initializes a new instance of the SecurityTestBase class with test output helper and API server fixture
        /// </summary>
        /// <param name="output">The test output helper</param>
        /// <param name="apiServer">The API server fixture</param>
        public SecurityTestBase(ITestOutputHelper output, ApiServerFixture apiServer)
        {
            Output = output ?? throw new ArgumentNullException(nameof(output));
            ApiServer = apiServer ?? throw new ArgumentNullException(nameof(apiServer));
            
            // Initialize HttpClient with base address from TestConstants
            HttpClient = new HttpClient
            {
                BaseAddress = new Uri(TestConstants.TestApiBaseUrl)
            };
            
            // Initialize Logger
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole().SetMinimumLevel(LogLevel.Debug);
            });
            Logger = loggerFactory.CreateLogger<SecurityTestBase>();
            
            // Initialize TokenManager with logger
            TokenManager = new TokenManager(Logger);
            
            // Initialize security testing settings
            EnableDynamicScanning = false; // Default to false, can be enabled by tests
            ReportOutputPath = "SecurityReports";
            
            // Define required security headers
            RequiredSecurityHeaders = new Dictionary<string, string>
            {
                { "Content-Security-Policy", "default-src 'self'" },
                { "X-Content-Type-Options", "nosniff" },
                { "X-Frame-Options", "DENY" },
                { "X-XSS-Protection", "1; mode=block" },
                { "Strict-Transport-Security", "max-age=31536000; includeSubDomains" }
            };
        }

        /// <summary>
        /// Initializes the security test environment asynchronously
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public virtual async Task InitializeAsync()
        {
            // Configure HttpClient with default headers
            HttpClient.DefaultRequestHeaders.Add("User-Agent", "SecurityPatrol-SecurityTests");
            HttpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            
            // Initialize ZAP client if enabled
            if (EnableDynamicScanning)
            {
                // TODO: Initialize ZAP client with appropriate configuration
                // ZapClient = new ZapClient(...);
            }
            
            // Set up any required test data or configurations
            
            Logger.LogInformation("Security test environment initialized");
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// Cleans up resources after security tests
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public virtual async Task CleanupAsync()
        {
            if (EnableDynamicScanning)
            {
                // Generate security reports
                await GenerateSecurityReport("FinalSecurityReport", new Dictionary<string, object>
                {
                    { "TestTime", DateTime.UtcNow },
                    { "TestResults", "Completed" }
                });
            }
            
            // Clean up any test data or resources
            
            Logger.LogInformation("Security test environment cleaned up");
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// Creates an HTTP request with authentication token
        /// </summary>
        /// <param name="method">The HTTP method</param>
        /// <param name="endpoint">The API endpoint</param>
        /// <param name="token">The authentication token (optional)</param>
        /// <returns>An authenticated HTTP request</returns>
        protected HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string endpoint, string token = null)
        {
            var request = new HttpRequestMessage(method, endpoint);
            
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Add("Authorization", $"Bearer {token}");
            }
            else
            {
                // Try to get token from TokenManager
                string storedToken = TokenManager.RetrieveToken().Result;
                if (!string.IsNullOrEmpty(storedToken))
                {
                    request.Headers.Add("Authorization", $"Bearer {storedToken}");
                }
            }
            
            return request;
        }

        /// <summary>
        /// Creates an HTTP request without authentication
        /// </summary>
        /// <param name="method">The HTTP method</param>
        /// <param name="endpoint">The API endpoint</param>
        /// <returns>An unauthenticated HTTP request</returns>
        protected HttpRequestMessage CreateUnauthenticatedRequest(HttpMethod method, string endpoint)
        {
            return new HttpRequestMessage(method, endpoint);
        }

        /// <summary>
        /// Validates that an HTTP response contains required security headers
        /// </summary>
        /// <param name="response">The HTTP response to validate</param>
        /// <returns>True if all required security headers are present</returns>
        protected bool ValidateSecureHeaders(HttpResponseMessage response)
        {
            bool allHeadersPresent = true;
            
            foreach (var requiredHeader in RequiredSecurityHeaders)
            {
                if (!response.Headers.Contains(requiredHeader.Key))
                {
                    allHeadersPresent = false;
                    LogSecurityIssue("MissingSecurityHeader", 
                        $"Response is missing required security header: {requiredHeader.Key}", 
                        LogLevel.Warning);
                }
                else if (!string.IsNullOrEmpty(requiredHeader.Value))
                {
                    string headerValue = response.Headers.GetValues(requiredHeader.Key).First();
                    if (!headerValue.Contains(requiredHeader.Value))
                    {
                        allHeadersPresent = false;
                        LogSecurityIssue("InvalidSecurityHeader", 
                            $"Security header {requiredHeader.Key} has invalid value. Expected to contain: {requiredHeader.Value}, Actual: {headerValue}", 
                            LogLevel.Warning);
                    }
                }
            }
            
            return allHeadersPresent;
        }

        /// <summary>
        /// Validates the security aspects of a JWT token
        /// </summary>
        /// <param name="token">The JWT token to validate</param>
        /// <returns>True if the token meets security requirements</returns>
        protected bool ValidateTokenSecurity(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                LogSecurityIssue("EmptyToken", "Token is null or empty", LogLevel.Error);
                return false;
            }
            
            try
            {
                // Check token structure
                string[] tokenParts = token.Split('.');
                if (tokenParts.Length != 3)
                {
                    LogSecurityIssue("InvalidTokenFormat", "Token does not have the correct JWT format (header.payload.signature)", LogLevel.Error);
                    return false;
                }
                
                // Parse token
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                
                // Check algorithm
                string algorithm = jwtToken.Header.Alg;
                if (algorithm != "HS256" && algorithm != "RS256")
                {
                    LogSecurityIssue("InsecureAlgorithm", $"Token uses potentially insecure algorithm: {algorithm}", LogLevel.Warning);
                    return false;
                }
                
                // Check expiration
                if (jwtToken.ValidTo < DateTime.UtcNow)
                {
                    LogSecurityIssue("ExpiredToken", $"Token is expired. Expiration: {jwtToken.ValidTo}, Current time: {DateTime.UtcNow}", LogLevel.Warning);
                    return false;
                }
                
                // Check required claims
                if (!jwtToken.Claims.Any(c => c.Type == "sub"))
                {
                    LogSecurityIssue("MissingSubjectClaim", "Token is missing required 'sub' claim", LogLevel.Warning);
                    return false;
                }
                
                if (!jwtToken.Claims.Any(c => c.Type == "iat"))
                {
                    LogSecurityIssue("MissingIssuedAtClaim", "Token is missing 'iat' claim", LogLevel.Warning);
                    return false;
                }
                
                if (!jwtToken.Claims.Any(c => c.Type == "exp"))
                {
                    LogSecurityIssue("MissingExpirationClaim", "Token is missing 'exp' claim", LogLevel.Warning);
                    return false;
                }
                
                // Further security checks can be added here
                
                return true;
            }
            catch (Exception ex)
            {
                LogSecurityIssue("TokenValidationError", $"Error validating token: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        /// <summary>
        /// Validates that encryption is properly implemented
        /// </summary>
        /// <param name="plainText">The original plain text</param>
        /// <param name="encryptedData">The encrypted data</param>
        /// <returns>True if encryption is properly implemented</returns>
        protected bool ValidateEncryption(string plainText, byte[] encryptedData)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                LogSecurityIssue("EmptyPlainText", "Plain text is null or empty", LogLevel.Error);
                return false;
            }
            
            if (encryptedData == null || encryptedData.Length == 0)
            {
                LogSecurityIssue("EmptyEncryptedData", "Encrypted data is null or empty", LogLevel.Error);
                return false;
            }
            
            try
            {
                // Check that encrypted data is different from plain text
                byte[] plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
                if (plainTextBytes.Length == encryptedData.Length && 
                    plainTextBytes.SequenceEqual(encryptedData))
                {
                    LogSecurityIssue("EncryptionNotApplied", "Encrypted data is identical to plain text", LogLevel.Error);
                    return false;
                }
                
                // Check for presence of initialization vector (IV)
                // AES encryption should include an IV at the beginning of the encrypted data
                if (encryptedData.Length <= 16) // IV for AES is 16 bytes
                {
                    LogSecurityIssue("MissingInitializationVector", "Encrypted data too short to contain a proper IV", LogLevel.Warning);
                    return false;
                }
                
                // Further encryption validation would depend on the specific implementation
                
                return true;
            }
            catch (Exception ex)
            {
                LogSecurityIssue("EncryptionValidationError", $"Error validating encryption: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        /// <summary>
        /// Sets up the environment for vulnerability testing
        /// </summary>
        /// <param name="vulnerabilityType">The type of vulnerability to test</param>
        /// <returns>True if setup was successful</returns>
        protected async Task<bool> SetupVulnerabilityTest(string vulnerabilityType)
        {
            try
            {
                switch (vulnerabilityType.ToLowerInvariant())
                {
                    case "sqli":
                    case "sqlinjection":
                        // Setup for SQL injection testing
                        ApiServer.SetupSuccessResponse("/api/vulnerable/sqli", new { message = "Test endpoint for SQL injection" });
                        break;
                        
                    case "xss":
                        // Setup for Cross-Site Scripting testing
                        ApiServer.SetupSuccessResponse("/api/vulnerable/xss", new { message = "Test endpoint for XSS" });
                        break;
                        
                    case "csrf":
                        // Setup for Cross-Site Request Forgery testing
                        ApiServer.SetupSuccessResponse("/api/vulnerable/csrf", new { message = "Test endpoint for CSRF" });
                        break;
                        
                    case "auth":
                    case "authentication":
                        // Setup for authentication bypass testing
                        ApiServer.SetupErrorResponse("/api/vulnerable/auth", 401, "Authentication required");
                        break;
                        
                    default:
                        LogSecurityIssue("UnknownVulnerabilityType", $"Unknown vulnerability type: {vulnerabilityType}", LogLevel.Warning);
                        return false;
                }
                
                await Task.CompletedTask; // Ensure method is async
                return true;
            }
            catch (Exception ex)
            {
                LogSecurityIssue("VulnerabilityTestSetupError", $"Error setting up vulnerability test: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        /// <summary>
        /// Runs an OWASP ZAP security scan against the specified target
        /// </summary>
        /// <param name="targetUrl">The URL to scan</param>
        /// <param name="activeScanning">Whether to perform active scanning</param>
        /// <returns>The results of the ZAP scan</returns>
        protected async Task<ZapScanResult> RunZapScan(string targetUrl, bool activeScanning = false)
        {
            if (!EnableDynamicScanning)
            {
                LogSecurityIssue("DynamicScanningDisabled", "Dynamic scanning is disabled. Enable it to run ZAP scans.", LogLevel.Warning);
                return null;
            }
            
            if (ZapClient == null)
            {
                LogSecurityIssue("ZapClientNotInitialized", "ZAP client is not initialized", LogLevel.Error);
                return null;
            }
            
            try
            {
                // This is a placeholder for the actual ZAP scan implementation
                // In a real implementation, we would:
                // 1. Configure the scan
                // 2. Start the spider to discover endpoints
                // 3. Run the active scan if requested
                // 4. Wait for completion
                // 5. Retrieve and return results
                
                var result = new ZapScanResult
                {
                    ScanId = Guid.NewGuid().ToString(),
                    ScanTime = DateTime.UtcNow,
                    AlertsHigh = 0,
                    AlertsMedium = 0,
                    AlertsLow = 0,
                    AlertsInformational = 0,
                    Alerts = new List<ZapAlertInfo>(),
                    ReportPath = Path.Combine(ReportOutputPath, $"zap_scan_{DateTime.Now:yyyyMMdd_HHmmss}.html")
                };
                
                await Task.CompletedTask; // Placeholder for async operations
                
                return result;
            }
            catch (Exception ex)
            {
                LogSecurityIssue("ZapScanError", $"Error running ZAP scan: {ex.Message}", LogLevel.Error);
                return null;
            }
        }

        /// <summary>
        /// Generates a security report based on test results
        /// </summary>
        /// <param name="reportName">The name of the report</param>
        /// <param name="results">The test results to include in the report</param>
        /// <returns>The path to the generated report</returns>
        protected async Task<string> GenerateSecurityReport(string reportName, Dictionary<string, object> results)
        {
            try
            {
                // Create report directory if it doesn't exist
                Directory.CreateDirectory(ReportOutputPath);
                
                // Format report name with timestamp
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string fileName = $"{reportName}_{timestamp}.json";
                string filePath = Path.Combine(ReportOutputPath, fileName);
                
                // Serialize results to JSON
                string jsonReport = System.Text.Json.JsonSerializer.Serialize(results, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });
                
                // Write to file
                await File.WriteAllTextAsync(filePath, jsonReport);
                
                Logger.LogInformation("Security report generated: {FilePath}", filePath);
                
                return filePath;
            }
            catch (Exception ex)
            {
                LogSecurityIssue("ReportGenerationError", $"Error generating security report: {ex.Message}", LogLevel.Error);
                return null;
            }
        }

        /// <summary>
        /// Tests an endpoint for SQL injection vulnerabilities
        /// </summary>
        /// <param name="endpoint">The endpoint to test</param>
        /// <param name="method">The HTTP method to use</param>
        /// <param name="payloads">The SQL injection payloads to test</param>
        /// <returns>True if the endpoint is vulnerable to SQL injection</returns>
        protected async Task<bool> TestForSqlInjection(string endpoint, HttpMethod method, Dictionary<string, string> payloads)
        {
            bool vulnerable = false;
            
            foreach (var payload in payloads)
            {
                try
                {
                    // Create a request with the SQL injection payload
                    var request = CreateUnauthenticatedRequest(method, endpoint);
                    
                    if (method == HttpMethod.Get)
                    {
                        // For GET requests, append the payload to the URL
                        var uri = new Uri(request.RequestUri, $"?{payload.Key}={Uri.EscapeDataString(payload.Value)}");
                        request.RequestUri = uri;
                    }
                    else
                    {
                        // For POST and other requests, add payload to the body
                        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                        {
                            { payload.Key, payload.Value }
                        });
                    }
                    
                    // Send the request
                    var response = await HttpClient.SendAsync(request);
                    
                    // Check the response for SQL error patterns
                    string responseBody = await response.Content.ReadAsStringAsync();
                    if (responseBody.Contains("SQL syntax") ||
                        responseBody.Contains("SQLite Error") ||
                        responseBody.Contains("ODBC SQL Server Driver") ||
                        responseBody.Contains("ORA-") ||
                        responseBody.Contains("syntax error") ||
                        responseBody.Contains("mysql_fetch_array()"))
                    {
                        vulnerable = true;
                        LogSecurityIssue("SqlInjectionVulnerability", 
                            $"Potential SQL injection vulnerability detected at {endpoint} with payload: {payload.Key}={payload.Value}", 
                            LogLevel.Critical);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Error testing for SQL injection at {Endpoint} with payload {Payload}", endpoint, payload);
                }
            }
            
            return vulnerable;
        }

        /// <summary>
        /// Tests an endpoint for Cross-Site Scripting (XSS) vulnerabilities
        /// </summary>
        /// <param name="endpoint">The endpoint to test</param>
        /// <param name="method">The HTTP method to use</param>
        /// <param name="payloads">The XSS payloads to test</param>
        /// <returns>True if the endpoint is vulnerable to XSS</returns>
        protected async Task<bool> TestForXss(string endpoint, HttpMethod method, Dictionary<string, string> payloads)
        {
            bool vulnerable = false;
            
            foreach (var payload in payloads)
            {
                try
                {
                    // Create a request with the XSS payload
                    var request = CreateUnauthenticatedRequest(method, endpoint);
                    
                    if (method == HttpMethod.Get)
                    {
                        // For GET requests, append the payload to the URL
                        var uri = new Uri(request.RequestUri, $"?{payload.Key}={Uri.EscapeDataString(payload.Value)}");
                        request.RequestUri = uri;
                    }
                    else
                    {
                        // For POST and other requests, add payload to the body
                        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                        {
                            { payload.Key, payload.Value }
                        });
                    }
                    
                    // Send the request
                    var response = await HttpClient.SendAsync(request);
                    
                    // Check the response for reflected XSS
                    string responseBody = await response.Content.ReadAsStringAsync();
                    
                    // Check if any script or HTML content is reflected unescaped
                    if (responseBody.Contains(payload.Value))
                    {
                        // Additional checks for unescaped content
                        bool isVulnerable = false;
                        
                        if (payload.Value.Contains("<script") && responseBody.Contains("<script") && !responseBody.Contains("&lt;script"))
                        {
                            isVulnerable = true;
                        }
                        else if (payload.Value.Contains("javascript:") && responseBody.Contains("javascript:") && !responseBody.Contains("&quot;javascript:"))
                        {
                            isVulnerable = true;
                        }
                        else if (payload.Value.Contains("onerror=") && responseBody.Contains("onerror=") && !responseBody.Contains("&quot;onerror="))
                        {
                            isVulnerable = true;
                        }
                        
                        if (isVulnerable)
                        {
                            vulnerable = true;
                            LogSecurityIssue("XssVulnerability", 
                                $"Potential XSS vulnerability detected at {endpoint} with payload: {payload.Key}={payload.Value}", 
                                LogLevel.Critical);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Error testing for XSS at {Endpoint} with payload {Payload}", endpoint, payload);
                }
            }
            
            return vulnerable;
        }

        /// <summary>
        /// Tests if an endpoint implements rate limiting
        /// </summary>
        /// <param name="endpoint">The endpoint to test</param>
        /// <param name="method">The HTTP method to use</param>
        /// <param name="requestCount">The number of requests to send</param>
        /// <param name="expectedThreshold">The expected threshold before rate limiting is applied</param>
        /// <returns>True if rate limiting is properly implemented</returns>
        protected async Task<bool> TestRateLimiting(string endpoint, HttpMethod method, int requestCount, int expectedThreshold)
        {
            int successCount = 0;
            int tooManyRequestsCount = 0;
            bool hasRetryAfterHeader = false;
            int firstRateLimitedRequestNumber = -1;
            
            for (int i = 0; i < requestCount; i++)
            {
                try
                {
                    // Create and send request
                    var request = CreateUnauthenticatedRequest(method, endpoint);
                    var response = await HttpClient.SendAsync(request);
                    
                    // Check for 429 Too Many Requests status code
                    if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        tooManyRequestsCount++;
                        
                        if (firstRateLimitedRequestNumber == -1)
                        {
                            firstRateLimitedRequestNumber = i + 1;
                        }
                        
                        // Check for Retry-After header
                        if (response.Headers.Contains("Retry-After"))
                        {
                            hasRetryAfterHeader = true;
                        }
                    }
                    else if (response.IsSuccessStatusCode)
                    {
                        successCount++;
                    }
                    
                    // Small delay to prevent overwhelming the server
                    await Task.Delay(50);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Error during rate limit testing at {Endpoint}, request #{RequestNumber}", endpoint, i + 1);
                }
            }
            
            bool hasRateLimiting = tooManyRequestsCount > 0;
            bool hasProperThreshold = firstRateLimitedRequestNumber > 0 && 
                                     Math.Abs(firstRateLimitedRequestNumber - expectedThreshold) <= Math.Max(3, expectedThreshold * 0.2); // Allow 20% margin of error
            bool hasProperImplementation = hasRateLimiting && hasRetryAfterHeader && hasProperThreshold;
            
            if (!hasRateLimiting)
            {
                LogSecurityIssue("NoRateLimiting", $"No rate limiting detected at {endpoint} after {requestCount} requests", LogLevel.Warning);
            }
            else if (!hasRetryAfterHeader)
            {
                LogSecurityIssue("IncompleteRateLimiting", $"Rate limiting detected at {endpoint} but missing Retry-After header", LogLevel.Warning);
            }
            else if (!hasProperThreshold)
            {
                LogSecurityIssue("ImproperRateLimitThreshold", 
                    $"Rate limit threshold appears incorrect. Expected around {expectedThreshold}, but got rate limited after {firstRateLimitedRequestNumber} requests", 
                    LogLevel.Warning);
            }
            else
            {
                Logger.LogInformation("Rate limiting properly implemented at {Endpoint} with threshold of approximately {Threshold} requests", 
                    endpoint, firstRateLimitedRequestNumber);
            }
            
            return hasProperImplementation;
        }

        /// <summary>
        /// Logs a security issue with appropriate severity
        /// </summary>
        /// <param name="issueType">The type of security issue</param>
        /// <param name="description">A description of the issue</param>
        /// <param name="severity">The severity level of the issue</param>
        protected void LogSecurityIssue(string issueType, string description, LogLevel severity)
        {
            string message = $"SECURITY ISSUE [{issueType}]: {description}";
            
            Logger.Log(severity, message);
            
            // For critical and error issues, also output to test output for immediate visibility
            if (severity == LogLevel.Critical || severity == LogLevel.Error)
            {
                Output.WriteLine(message);
            }
        }
    }

    /// <summary>
    /// Class representing the results of a ZAP security scan
    /// </summary>
    public class ZapScanResult
    {
        public int AlertsHigh { get; set; }
        public int AlertsMedium { get; set; }
        public int AlertsLow { get; set; }
        public int AlertsInformational { get; set; }
        public List<ZapAlertInfo> Alerts { get; set; } = new List<ZapAlertInfo>();
        public string ScanId { get; set; }
        public DateTime ScanTime { get; set; }
        public string ReportPath { get; set; }
    }

    /// <summary>
    /// Class representing a security alert from a ZAP scan
    /// </summary>
    public class ZapAlertInfo
    {
        public string Name { get; set; }
        public string Risk { get; set; }
        public string Confidence { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public string Solution { get; set; }
        public string Reference { get; set; }
    }
}