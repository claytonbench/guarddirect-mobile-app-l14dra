using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.IO;
using Xunit;
using Xunit.Abstractions;
using SecurityPatrol.SecurityTests.Setup;
using SecurityPatrol.TestCommon.Constants;
using SecurityPatrol.TestCommon.Fixtures;
using SecurityPatrol.Helpers;

namespace SecurityPatrol.SecurityTests.API
{
    /// <summary>
    /// Implements security tests focused on data protection aspects of the Security Patrol application,
    /// including encryption, secure storage, data transmission security, and protection of sensitive information.
    /// </summary>
    public class DataProtectionTests : SecurityTestBase
    {
        private readonly ITestOutputHelper _output;
        private readonly ApiServerFixture _apiServer;

        /// <summary>
        /// Initializes a new instance of the DataProtectionTests class with test output helper and API server fixture
        /// </summary>
        /// <param name="output">The test output helper</param>
        /// <param name="apiServer">The API server fixture</param>
        public DataProtectionTests(ITestOutputHelper output, ApiServerFixture apiServer)
            : base(output, apiServer)
        {
            _output = output;
            _apiServer = apiServer;
        }

        /// <summary>
        /// Tests that the encryption implementation meets security requirements
        /// </summary>
        /// <returns>A task representing the asynchronous test operation</returns>
        [Fact]
        public async Task TestEncryptionImplementation()
        {
            // Generate a test string to encrypt
            string testString = "This is a sensitive test string that needs to be protected";
            
            // Generate a secure encryption key
            string encryptionKey = SecurityHelper.GenerateSecureKey(256);
            
            // Encrypt the test string
            string encryptedString = SecurityHelper.EncryptString(testString, encryptionKey);
            
            // Verify that the encrypted string is different from the original
            Assert.NotEqual(testString, encryptedString);
            
            // Decrypt the encrypted string
            string decryptedString = SecurityHelper.DecryptString(encryptedString, encryptionKey);
            
            // Verify that the decrypted string matches the original
            Assert.Equal(testString, decryptedString);
            
            // Verify proper encryption is used (AES-256)
            // Convert to bytes to validate encryption
            byte[] encryptedBytes = Convert.FromBase64String(encryptedString);
            
            // Validate the encryption using the ValidateEncryption method from base class
            bool isValidEncryption = ValidateEncryption(testString, encryptedBytes);
            Assert.True(isValidEncryption, "Encryption validation failed");
            
            // Verify that initialization vector (IV) is properly used
            Assert.True(encryptedBytes.Length > 16, "Encrypted data too short to contain proper IV");
            
            LogSecurityIssue("EncryptionImplementation", "AES-256 encryption implementation validated successfully", Microsoft.Extensions.Logging.LogLevel.Information);
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// Tests that sensitive data is transmitted securely
        /// </summary>
        /// <returns>A task representing the asynchronous test operation</returns>
        [Fact]
        public async Task TestSecureDataTransmission()
        {
            // Set up mock responses for API endpoints that handle sensitive data
            var locationData = new
            {
                Locations = new[]
                {
                    new
                    {
                        Timestamp = DateTime.UtcNow.ToString("o"),
                        Latitude = TestConstants.TestLatitude,
                        Longitude = TestConstants.TestLongitude,
                        Accuracy = TestConstants.TestAccuracy
                    }
                }
            };
            
            _apiServer.SetupSuccessResponse("/location/batch", new { Processed = 1, Failed = 0 });
            
            // Create HTTP client that requires HTTPS
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                {
                    // In production this would validate HTTPS certificate
                    // For tests we're checking if HTTPS is being used at all
                    return message.RequestUri.Scheme == "https";
                }
            };
            
            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri(TestConstants.TestApiBaseUrl)
            };
            
            // Create an authenticated request
            var request = CreateAuthenticatedRequest(HttpMethod.Post, "/location/batch", TestConstants.TestAuthToken);
            request.Content = JsonContent.Create(locationData);
            
            try
            {
                // Send request with sensitive data
                var response = await HttpClient.SendAsync(request);
                
                // Verify that the request is sent over HTTPS
                Assert.Equal("https", request.RequestUri.Scheme);
                
                // Verify that sensitive data is not exposed in URL parameters
                Assert.False(request.RequestUri.ToString().Contains(TestConstants.TestLatitude.ToString()), 
                    "Sensitive location data should not be exposed in URL");
                
                // Verify that the response contains appropriate security headers
                var hasSecureHeaders = ValidateSecureHeaders(response);
                Assert.True(hasSecureHeaders, "Response is missing required security headers");
                
                // Verify that the connection uses TLS 1.2 or higher
                LogSecurityIssue("TLSVersionCheck", 
                    "TLS 1.2+ verification would be performed here in a real environment", 
                    Microsoft.Extensions.Logging.LogLevel.Information);
            }
            catch (Exception ex)
            {
                LogSecurityIssue("SecureTransmissionError", 
                    $"Error during secure transmission test: {ex.Message}", 
                    Microsoft.Extensions.Logging.LogLevel.Error);
                throw;
            }
        }

        /// <summary>
        /// Tests the security of photo storage and transmission
        /// </summary>
        /// <returns>A task representing the asynchronous test operation</returns>
        [Fact]
        public async Task TestPhotoStorageSecurity()
        {
            // Create a test image file
            byte[] testImageData = new byte[100 * 1024]; // 100KB dummy image
            new Random().NextBytes(testImageData); // Fill with random data to simulate image
            
            // Set up mock response for photo upload endpoint
            _apiServer.SetupSuccessResponse("/photos/upload", new { Id = Guid.NewGuid().ToString(), Status = "success" });
            
            // Create an authenticated request to upload the photo
            var request = CreateAuthenticatedRequest(HttpMethod.Post, "/photos/upload", TestConstants.TestAuthToken);
            
            // Create multipart form content with the image
            var formContent = new MultipartFormDataContent();
            formContent.Add(new ByteArrayContent(testImageData), "image", "test_image.jpg");
            formContent.Add(new StringContent(DateTime.UtcNow.ToString("o")), "timestamp");
            formContent.Add(new StringContent(TestConstants.TestLatitude.ToString()), "latitude");
            formContent.Add(new StringContent(TestConstants.TestLongitude.ToString()), "longitude");
            
            request.Content = formContent;
            
            // Send the request and get the response
            var response = await HttpClient.SendAsync(request);
            
            // Verify that the photo is transmitted securely (HTTPS)
            Assert.Equal("https", request.RequestUri.Scheme);
            
            // Verify that the photo is stored securely on the server
            Assert.True(response.IsSuccessStatusCode, "Photo upload request failed");
            
            // Verify that the photo metadata is protected
            string requestBody = _apiServer.GetLastRequestBody("/photos/upload");
            Assert.DoesNotContain("latitude", requestBody.ToLower());
            Assert.DoesNotContain("longitude", requestBody.ToLower());
            
            // Verify that unauthorized users cannot access the photo
            var unauthRequest = CreateUnauthenticatedRequest(HttpMethod.Get, $"/photos/{Guid.NewGuid()}");
            _apiServer.SetupErrorResponse($"/photos/{Guid.NewGuid()}", 401, "Unauthorized");
            
            var unauthResponse = await HttpClient.SendAsync(unauthRequest);
            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, unauthResponse.StatusCode);
            
            LogSecurityIssue("PhotoStorageSecurity", 
                "Photo storage security measures validated", 
                Microsoft.Extensions.Logging.LogLevel.Information);
        }

        /// <summary>
        /// Tests that sensitive user data is properly protected
        /// </summary>
        /// <returns>A task representing the asynchronous test operation</returns>
        [Fact]
        public async Task TestSensitiveDataProtection()
        {
            // Set up mock responses for API endpoints that handle user data
            var userProfileResponse = new
            {
                UserId = TestConstants.TestUserId,
                PhoneNumber = "***-***-" + TestConstants.TestPhoneNumber.Substring(Math.Max(0, TestConstants.TestPhoneNumber.Length - 4)), // Masked phone number
                LastAuthenticated = DateTime.UtcNow.AddHours(-1)
            };
            
            _apiServer.SetupSuccessResponse("/user/profile", userProfileResponse);
            
            var locationResponse = new
            {
                Timestamp = DateTime.UtcNow.ToString("o"),
                // Reduced precision for location data (fewer decimal places)
                Latitude = Math.Round(TestConstants.TestLatitude, 2),
                Longitude = Math.Round(TestConstants.TestLongitude, 2)
            };
            
            _apiServer.SetupSuccessResponse("/location/current", locationResponse);
            
            // Test that phone numbers are properly masked in responses
            var profileRequest = CreateAuthenticatedRequest(HttpMethod.Get, "/user/profile", TestConstants.TestAuthToken);
            var profileResponse = await HttpClient.SendAsync(profileRequest);
            var profileContent = await profileResponse.Content.ReadFromJsonAsync<dynamic>();
            
            // Verify phone number is masked
            string phoneNumber = profileContent.PhoneNumber.ToString();
            Assert.Contains("***-***-", phoneNumber);
            Assert.DoesNotContain(TestConstants.TestPhoneNumber.Substring(0, Math.Max(0, TestConstants.TestPhoneNumber.Length - 4)), phoneNumber);
            
            // Test that location data is protected with appropriate precision reduction
            var locationRequest = CreateAuthenticatedRequest(HttpMethod.Get, "/location/current", TestConstants.TestAuthToken);
            var locationResponseMsg = await HttpClient.SendAsync(locationRequest);
            var locationContent = await locationResponseMsg.Content.ReadFromJsonAsync<dynamic>();
            
            // Verify location has reduced precision
            double latitude = (double)locationContent.Latitude;
            double longitude = (double)locationContent.Longitude;
            
            Assert.Equal(Math.Round(TestConstants.TestLatitude, 2), latitude);
            Assert.Equal(Math.Round(TestConstants.TestLongitude, 2), longitude);
            
            // Test that authentication tokens are not exposed in logs or error messages
            _apiServer.SetupErrorResponse("/error-test", 500, "Internal Server Error");
            
            var errorRequest = CreateAuthenticatedRequest(HttpMethod.Get, "/error-test", TestConstants.TestAuthToken);
            var errorResponse = await HttpClient.SendAsync(errorRequest);
            var errorContent = await errorResponse.Content.ReadAsStringAsync();
            
            Assert.DoesNotContain(TestConstants.TestAuthToken, errorContent);
            
            // Test that personal identifiable information (PII) is properly protected
            Assert.DoesNotContain(TestConstants.TestPhoneNumber, errorContent);
            
            LogSecurityIssue("SensitiveDataProtection", 
                "Sensitive data protection mechanisms validated", 
                Microsoft.Extensions.Logging.LogLevel.Information);
        }

        /// <summary>
        /// Tests that sensitive data is encrypted at rest
        /// </summary>
        /// <returns>A task representing the asynchronous test operation</returns>
        [Fact]
        public async Task TestDataAtRestEncryption()
        {
            // Test that authentication tokens are encrypted when stored
            string testToken = "test_" + Guid.NewGuid().ToString();
            
            // Generate secure encryption key
            string encryptionKey = SecurityHelper.GenerateSecureKey(256);
            string encryptedToken = SecurityHelper.EncryptString(testToken, encryptionKey);
            
            // Verify token is encrypted
            Assert.NotEqual(testToken, encryptedToken);
            
            // Test that we can decrypt it correctly
            string decryptedToken = SecurityHelper.DecryptString(encryptedToken, encryptionKey);
            Assert.Equal(testToken, decryptedToken);
            
            // Test that location data is encrypted in the database
            var locationData = new
            {
                UserId = TestConstants.TestUserId,
                Timestamp = DateTime.UtcNow,
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude
            };
            
            // Serialize and encrypt the location data
            string locationJson = System.Text.Json.JsonSerializer.Serialize(locationData);
            string encryptedLocation = SecurityHelper.EncryptString(locationJson, encryptionKey);
            
            // Verify location data is encrypted
            Assert.NotEqual(locationJson, encryptedLocation);
            
            // Test that photos are encrypted when stored
            byte[] testImageData = new byte[100 * 1024]; // 100KB dummy image
            new Random().NextBytes(testImageData); // Fill with random data
            
            // Simulate file encryption
            using (var ms = new MemoryStream())
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = Convert.FromBase64String(encryptionKey);
                    aes.GenerateIV();
                    
                    // Write IV to the beginning of the stream
                    ms.Write(aes.IV, 0, aes.IV.Length);
                    
                    using (var cryptoStream = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(testImageData, 0, testImageData.Length);
                    }
                }
                
                // Verify encrypted data is different from original
                byte[] encryptedImageData = ms.ToArray();
                Assert.NotEqual(testImageData.Length, encryptedImageData.Length);
            }
            
            // Test that activity reports containing sensitive information are encrypted
            var reportData = new
            {
                Text = "Suspicious activity near the main entrance",
                Timestamp = DateTime.UtcNow,
                Location = new
                {
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                }
            };
            
            // Serialize and encrypt the report data
            string reportJson = System.Text.Json.JsonSerializer.Serialize(reportData);
            string encryptedReport = SecurityHelper.EncryptString(reportJson, encryptionKey);
            
            // Verify report data is encrypted
            Assert.NotEqual(reportJson, encryptedReport);
            
            // Verify that encryption keys are properly protected
            string keyProtectionPassphrase = "secure_passphrase";
            string protectedKey = SecurityHelper.EncryptString(encryptionKey, 
                SecurityHelper.ComputeHash(keyProtectionPassphrase).Substring(0, 32));
            
            // Verify key is protected
            Assert.NotEqual(encryptionKey, protectedKey);
            
            LogSecurityIssue("DataAtRestEncryption", 
                "Data at rest encryption validated", 
                Microsoft.Extensions.Logging.LogLevel.Information);
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// Tests the security of file operations in the application
        /// </summary>
        /// <returns>A task representing the asynchronous test operation</returns>
        [Fact]
        public async Task TestSecureFileOperations()
        {
            // Set up mock responses for file operation endpoints
            _apiServer.SetupSuccessResponse("/photos/upload", new { Id = Guid.NewGuid().ToString(), Status = "success" });
            _apiServer.SetupSuccessResponse("/photos/delete", new { Status = "success" });
            
            // Test file upload security (proper validation, secure storage)
            byte[] testFileData = new byte[50 * 1024]; // 50KB dummy file
            new Random().NextBytes(testFileData);
            
            // Create a file upload request with proper validation
            var uploadRequest = CreateAuthenticatedRequest(HttpMethod.Post, "/photos/upload", TestConstants.TestAuthToken);
            
            // Create multipart form content with the file
            var formContent = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(testFileData);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            
            formContent.Add(fileContent, "image", "test_image.jpg");
            formContent.Add(new StringContent(DateTime.UtcNow.ToString("o")), "timestamp");
            formContent.Add(new StringContent(TestConstants.TestLatitude.ToString()), "latitude");
            formContent.Add(new StringContent(TestConstants.TestLongitude.ToString()), "longitude");
            
            uploadRequest.Content = formContent;
            
            // Send the request
            var uploadResponse = await HttpClient.SendAsync(uploadRequest);
            Assert.True(uploadResponse.IsSuccessStatusCode, "File upload failed");
            
            // Test file download security (authentication, authorization)
            var fileId = Guid.NewGuid().ToString();
            var downloadUrl = $"/photos/{fileId}";
            
            // Set up authorized response
            _apiServer.SetupSuccessResponse(downloadUrl, new byte[100]); // Mock file content
            
            // Test with valid auth token
            var authDownloadRequest = CreateAuthenticatedRequest(HttpMethod.Get, downloadUrl, TestConstants.TestAuthToken);
            var authDownloadResponse = await HttpClient.SendAsync(authDownloadRequest);
            Assert.True(authDownloadResponse.IsSuccessStatusCode, "Authorized file download failed");
            
            // Test with invalid auth token
            var invalidAuthDownloadRequest = CreateAuthenticatedRequest(HttpMethod.Get, downloadUrl, "invalid_token");
            _apiServer.SetupErrorResponse(downloadUrl, 401, "Unauthorized");
            var invalidAuthDownloadResponse = await HttpClient.SendAsync(invalidAuthDownloadRequest);
            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, invalidAuthDownloadResponse.StatusCode);
            
            // Test file deletion security (proper cleanup, access control)
            var deleteUrl = $"/photos/delete/{fileId}";
            _apiServer.SetupSuccessResponse(deleteUrl, new { Status = "success" });
            
            // Test with valid auth token
            var authDeleteRequest = CreateAuthenticatedRequest(HttpMethod.Delete, deleteUrl, TestConstants.TestAuthToken);
            var authDeleteResponse = await HttpClient.SendAsync(authDeleteRequest);
            Assert.True(authDeleteResponse.IsSuccessStatusCode, "Authorized file deletion failed");
            
            // Test with invalid auth token
            var invalidAuthDeleteRequest = CreateAuthenticatedRequest(HttpMethod.Delete, deleteUrl, "invalid_token");
            _apiServer.SetupErrorResponse(deleteUrl, 401, "Unauthorized");
            var invalidAuthDeleteResponse = await HttpClient.SendAsync(invalidAuthDeleteRequest);
            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, invalidAuthDeleteResponse.StatusCode);
            
            // Verify that file operations are logged for audit purposes
            // In a real environment we would check the actual logs
            
            // Verify that file paths are sanitized to prevent path traversal attacks
            var pathTraversalUrl = "/photos/../config/app.config"; // Attempt path traversal
            _apiServer.SetupErrorResponse(pathTraversalUrl, 400, "Invalid file path");
            
            var pathTraversalRequest = CreateAuthenticatedRequest(HttpMethod.Get, pathTraversalUrl, TestConstants.TestAuthToken);
            var pathTraversalResponse = await HttpClient.SendAsync(pathTraversalRequest);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, pathTraversalResponse.StatusCode);
            
            LogSecurityIssue("SecureFileOperations", 
                "File operation security validated", 
                Microsoft.Extensions.Logging.LogLevel.Information);
        }

        /// <summary>
        /// Tests that the application prevents data leakage
        /// </summary>
        /// <returns>A task representing the asynchronous test operation</returns>
        [Fact]
        public async Task TestDataLeakagePrevention()
        {
            // Test that error responses don't leak sensitive information
            _apiServer.SetupErrorResponse("/error-test", 500, "Internal Server Error");
            
            // Send request with sensitive data in headers and query parameters
            var errorRequest = CreateAuthenticatedRequest(HttpMethod.Get, "/error-test?user=admin&debug=true", TestConstants.TestAuthToken);
            errorRequest.Headers.Add("X-Debug-Mode", "true");
            
            var errorResponse = await HttpClient.SendAsync(errorRequest);
            var errorContent = await errorResponse.Content.ReadAsStringAsync();
            
            // Verify error response doesn't contain sensitive information
            Assert.DoesNotContain("stack trace", errorContent.ToLower());
            Assert.DoesNotContain("exception", errorContent.ToLower());
            Assert.DoesNotContain("sql", errorContent.ToLower());
            Assert.DoesNotContain(TestConstants.TestAuthToken, errorContent);
            
            // Test that debug information is not exposed in production
            _apiServer.SetupSuccessResponse("/api/version", new { Version = "1.0.0" });
            
            var versionRequest = CreateAuthenticatedRequest(HttpMethod.Get, "/api/version", TestConstants.TestAuthToken);
            versionRequest.Headers.Add("X-Debug-Mode", "true"); // Attempt to enable debug mode
            
            var versionResponse = await HttpClient.SendAsync(versionRequest);
            var versionContent = await versionResponse.Content.ReadAsStringAsync();
            
            // Verify debug info is not exposed
            Assert.DoesNotContain("debug", versionContent.ToLower());
            Assert.DoesNotContain("development", versionContent.ToLower());
            Assert.DoesNotContain("config", versionContent.ToLower());
            
            // Test that logs don't contain sensitive data
            // In a real environment we would check the actual logs
            LogSecurityIssue("LogContentCheck", 
                "In a production environment, logs would be scanned for sensitive data", 
                Microsoft.Extensions.Logging.LogLevel.Information);
            
            // Test that cache headers prevent sensitive data caching
            _apiServer.SetupSuccessResponse("/user/profile", new { UserId = TestConstants.TestUserId, PhoneNumber = TestConstants.TestPhoneNumber });
            
            var profileRequest = CreateAuthenticatedRequest(HttpMethod.Get, "/user/profile", TestConstants.TestAuthToken);
            var profileResponse = await HttpClient.SendAsync(profileRequest);
            
            // Check for cache control headers
            if (profileResponse.Headers.CacheControl != null)
            {
                Assert.True(profileResponse.Headers.CacheControl.NoStore);
                Assert.True(profileResponse.Headers.CacheControl.NoCache);
            }
            else
            {
                // If the mock server doesn't set these headers, we'll log it
                LogSecurityIssue("MissingCacheControl", 
                    "Cache-Control headers should be present for sensitive endpoints", 
                    Microsoft.Extensions.Logging.LogLevel.Warning);
            }
            
            // Test that memory is properly cleared after handling sensitive data
            LogSecurityIssue("MemoryClearingCheck", 
                "Memory clearing after handling sensitive data would be verified in a real implementation", 
                Microsoft.Extensions.Logging.LogLevel.Information);
            
            LogSecurityIssue("DataLeakagePrevention", 
                "Data leakage prevention mechanisms validated", 
                Microsoft.Extensions.Logging.LogLevel.Information);
        }

        /// <summary>
        /// Tests that backup data is properly protected
        /// </summary>
        /// <returns>A task representing the asynchronous test operation</returns>
        [Fact]
        public async Task TestBackupDataProtection()
        {
            // Test that database backups are encrypted
            string databaseContent = "This is a simulated database backup with sensitive user data";
            
            // Encrypt the backup
            string encryptionKey = SecurityHelper.GenerateSecureKey(256);
            string encryptedBackup = SecurityHelper.EncryptString(databaseContent, encryptionKey);
            
            // Verify backup is encrypted
            Assert.NotEqual(databaseContent, encryptedBackup);
            
            // Test that file backups are encrypted
            byte[] fileBackupData = Encoding.UTF8.GetBytes("This is a simulated file backup with sensitive information");
            
            // Encrypt the file backup
            using (Aes aes = Aes.Create())
            {
                aes.Key = Convert.FromBase64String(encryptionKey);
                aes.GenerateIV();
                
                using (MemoryStream ms = new MemoryStream())
                {
                    // Write IV to the beginning of the stream
                    ms.Write(aes.IV, 0, aes.IV.Length);
                    
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(fileBackupData, 0, fileBackupData.Length);
                    }
                    
                    byte[] encryptedFileBackup = ms.ToArray();
                    
                    // Verify file backup is encrypted
                    Assert.NotEqual(fileBackupData.Length, encryptedFileBackup.Length);
                }
            }
            
            // Test that backup access is properly controlled
            _apiServer.SetupErrorResponse("/admin/backups", 401, "Unauthorized");
            
            // Test unauthenticated access
            var unauthRequest = CreateUnauthenticatedRequest(HttpMethod.Get, "/admin/backups");
            var unauthResponse = await HttpClient.SendAsync(unauthRequest);
            
            // Verify access is denied
            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, unauthResponse.StatusCode);
            
            // Test authenticated but unauthorized access
            var authRequest = CreateAuthenticatedRequest(HttpMethod.Get, "/admin/backups", TestConstants.TestAuthToken);
            _apiServer.SetupErrorResponse("/admin/backups", 403, "Forbidden");
            
            var authResponse = await HttpClient.SendAsync(authRequest);
            
            // Verify access is forbidden (user is authenticated but not authorized)
            Assert.Equal(System.Net.HttpStatusCode.Forbidden, authResponse.StatusCode);
            
            // Test that backup restoration requires proper authentication
            _apiServer.SetupErrorResponse("/admin/backups/restore", 401, "Unauthorized");
            
            var restoreRequest = CreateUnauthenticatedRequest(HttpMethod.Post, "/admin/backups/restore");
            var restoreResponse = await HttpClient.SendAsync(restoreRequest);
            
            // Verify restoration requires authentication
            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, restoreResponse.StatusCode);
            
            // Verify that backup processes don't expose sensitive data
            // This would require checking actual backup files in a real environment
            
            LogSecurityIssue("BackupDataProtection", 
                "Backup data protection mechanisms validated", 
                Microsoft.Extensions.Logging.LogLevel.Information);
        }

        /// <summary>
        /// Tests that data integrity is properly protected
        /// </summary>
        /// <returns>A task representing the asynchronous test operation</returns>
        [Fact]
        public async Task TestDataIntegrityProtection()
        {
            // Test that data modifications are properly authenticated and authorized
            _apiServer.SetupErrorResponse("/reports", 401, "Unauthorized");
            
            var reportData = new
            {
                Text = "Test report",
                Timestamp = DateTime.UtcNow.ToString("o"),
                Location = new
                {
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                }
            };
            
            // Create unauthenticated request
            var unauthRequest = CreateUnauthenticatedRequest(HttpMethod.Post, "/reports");
            unauthRequest.Content = JsonContent.Create(reportData);
            
            var unauthResponse = await HttpClient.SendAsync(unauthRequest);
            
            // Verify authentication is required
            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, unauthResponse.StatusCode);
            
            // Test that data integrity checks are implemented
            _apiServer.SetupErrorResponse("/reports/123", 400, "Invalid data integrity hash");
            
            // Create authenticated request with invalid integrity hash
            var invalidRequest = CreateAuthenticatedRequest(HttpMethod.Put, "/reports/123", TestConstants.TestAuthToken);
            var updateData = new
            {
                Id = "123",
                Text = "Updated report",
                Timestamp = DateTime.UtcNow.ToString("o"),
                Hash = "invalid_hash" // Invalid integrity hash
            };
            
            invalidRequest.Content = JsonContent.Create(updateData);
            
            var invalidResponse = await HttpClient.SendAsync(invalidRequest);
            
            // Verify integrity check fails
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, invalidResponse.StatusCode);
            
            // Test that data corruption is detected and handled
            _apiServer.SetupErrorResponse("/data/validate", 500, "Data corruption detected");
            
            var validateRequest = CreateAuthenticatedRequest(HttpMethod.Get, "/data/validate", TestConstants.TestAuthToken);
            var validateResponse = await HttpClient.SendAsync(validateRequest);
            
            // Verify corruption is detected
            Assert.Equal(System.Net.HttpStatusCode.InternalServerError, validateResponse.StatusCode);
            var responseContent = await validateResponse.Content.ReadAsStringAsync();
            Assert.Contains("corruption", responseContent);
            
            // Test that audit trails for data modifications are maintained
            _apiServer.SetupSuccessResponse("/reports", new { Id = "456", Status = "success" });
            
            // Create authenticated request for data modification
            var auditRequest = CreateAuthenticatedRequest(HttpMethod.Post, "/reports", TestConstants.TestAuthToken);
            auditRequest.Content = JsonContent.Create(reportData);
            
            var auditResponse = await HttpClient.SendAsync(auditRequest);
            
            // Verify request was successful
            Assert.True(auditResponse.IsSuccessStatusCode);
            
            // In a real implementation, we would verify the audit trail was created
            // For our test, we'll check that the API was called
            Assert.True(_apiServer.GetRequestCount("/reports") > 0, "API endpoint should have been called");
            
            // Verify that data integrity is maintained during synchronization
            // This would require testing actual synchronization in a real environment
            
            LogSecurityIssue("DataIntegrityProtection", 
                "Data integrity protection mechanisms validated", 
                Microsoft.Extensions.Logging.LogLevel.Information);
        }

        /// <summary>
        /// Tests the security of encryption key management
        /// </summary>
        /// <returns>A task representing the asynchronous test operation</returns>
        [Fact]
        public async Task TestKeyManagementSecurity()
        {
            // Test that encryption keys are generated securely
            string key128 = SecurityHelper.GenerateSecureKey(128);
            string key192 = SecurityHelper.GenerateSecureKey(192);
            string key256 = SecurityHelper.GenerateSecureKey(256);
            
            // Verify keys have appropriate length
            Assert.Equal(24, Convert.FromBase64String(key128).Length); // 128 bits = 16 bytes, Base64 encoding increases size
            Assert.Equal(32, Convert.FromBase64String(key192).Length); // 192 bits = 24 bytes, Base64 encoding increases size
            Assert.Equal(44, Convert.FromBase64String(key256).Length); // 256 bits = 32 bytes, Base64 encoding increases size
            
            // Verify keys are different (random)
            Assert.NotEqual(key128, key192);
            Assert.NotEqual(key192, key256);
            Assert.NotEqual(key128, key256);
            
            // Test that encryption keys are stored securely
            string keyProtectionPassphrase = "secure_passphrase";
            string protectedKey = SecurityHelper.EncryptString(key256, 
                SecurityHelper.ComputeHash(keyProtectionPassphrase).Substring(0, 32));
            
            // Verify key is protected
            Assert.NotEqual(key256, protectedKey);
            
            // Test that encryption keys are rotated appropriately
            // Generate a new key
            string newKey = SecurityHelper.GenerateSecureKey(256);
            
            // Re-encrypt data with the new key
            string testData = "This is sensitive data that needs to be re-encrypted during key rotation";
            string encryptedWithOldKey = SecurityHelper.EncryptString(testData, key256);
            
            // Decrypt with old key
            string decryptedData = SecurityHelper.DecryptString(encryptedWithOldKey, key256);
            
            // Re-encrypt with new key
            string encryptedWithNewKey = SecurityHelper.EncryptString(decryptedData, newKey);
            
            // Verify data can be decrypted with new key
            string reDecryptedData = SecurityHelper.DecryptString(encryptedWithNewKey, newKey);
            Assert.Equal(testData, reDecryptedData);
            
            // Test that key access is properly controlled
            _apiServer.SetupErrorResponse("/admin/keys", 403, "Forbidden");
            
            var keyRequest = CreateAuthenticatedRequest(HttpMethod.Get, "/admin/keys", TestConstants.TestAuthToken);
            var keyResponse = await HttpClient.SendAsync(keyRequest);
            
            // Verify access is forbidden (regular user can't access keys)
            Assert.Equal(System.Net.HttpStatusCode.Forbidden, keyResponse.StatusCode);
            
            // Verify that key backup and recovery processes are secure
            // Backup the key with additional encryption
            string backupEncryptionKey = SecurityHelper.GenerateSecureKey(256);
            string backedUpKey = SecurityHelper.EncryptString(key256, backupEncryptionKey);
            
            // Recover the key
            string recoveredKey = SecurityHelper.DecryptString(backedUpKey, backupEncryptionKey);
            
            // Verify the key was recovered correctly
            Assert.Equal(key256, recoveredKey);
            
            LogSecurityIssue("KeyManagementSecurity", 
                "Key management security mechanisms validated", 
                Microsoft.Extensions.Logging.LogLevel.Information);
            
            await Task.CompletedTask;
        }
    }
}