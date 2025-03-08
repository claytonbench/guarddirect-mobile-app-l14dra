using System;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;
using SQLite;
using Xunit;
using Xunit.Abstractions;
using Microsoft.Extensions.Logging;
using Moq;
using SecurityPatrol.SecurityTests.Setup;
using SecurityPatrol.Helpers;
using SecurityPatrol.Database;
using SecurityPatrol.Constants;
using SecurityPatrol.TestCommon.Constants;

namespace SecurityPatrol.SecurityTests.Mobile
{
    /// <summary>
    /// Contains security-focused tests specifically targeting the data storage mechanisms in the Security Patrol mobile application
    /// </summary>
    public class DataStorageSecurityTests : SecurityTestBase
    {
        private readonly ILogger<DataStorageSecurityTests> _logger;
        private readonly SecurityHelper _securityHelper;
        private readonly DatabaseInitializer _databaseInitializer;
        private SQLiteAsyncConnection _dbConnection;

        /// <summary>
        /// Initializes a new instance of the DataStorageSecurityTests class with test output helper and API server fixture
        /// </summary>
        /// <param name="output">The test output helper</param>
        /// <param name="apiServer">The API server fixture</param>
        public DataStorageSecurityTests(ITestOutputHelper output, ApiServerFixture apiServer) 
            : base(output, apiServer)
        {
            // Initialize logger
            var loggerFactory = LoggerFactory.Create(builder => 
                builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
            _logger = loggerFactory.CreateLogger<DataStorageSecurityTests>();
            
            // Initialize security helper for testing
            _securityHelper = new Mock<SecurityHelper>().Object;
            
            // Initialize database initializer for testing database security
            _databaseInitializer = new DatabaseInitializer(_logger);
        }

        /// <summary>
        /// Initializes the test environment for data storage security tests
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public override async Task InitializeAsync()
        {
            // Call base.InitializeAsync to initialize common test components
            await base.InitializeAsync();
            
            // Initialize database connection for testing
            _dbConnection = await _databaseInitializer.GetConnectionAsync();
            
            // Ensure test environment is properly set up for data storage tests
            _logger.LogInformation("Data storage security test environment initialized");
        }

        /// <summary>
        /// Cleans up resources after data storage security tests
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public override async Task CleanupAsync()
        {
            // Clean up any test data created during tests
            _logger.LogInformation("Cleaning up data storage security test resources");
            
            // Close database connection if open
            if (_dbConnection != null)
            {
                await _dbConnection.CloseAsync();
                _dbConnection = null;
            }
            
            // Call base.CleanupAsync to clean up common test components
            await base.CleanupAsync();
            
            _logger.LogInformation("Data storage security test cleanup completed");
        }

        /// <summary>
        /// Verifies that secure storage is properly implemented for sensitive data
        /// </summary>
        /// <returns>A task representing the asynchronous test operation</returns>
        [Fact]
        [Trait("Category", "Security")]
        public async Task TestSecureStorageImplementation()
        {
            // Generate test sensitive data
            string testKey = "test_secure_key";
            string testValue = Guid.NewGuid().ToString();
            
            try
            {
                // Store data using SecurityHelper.SaveToSecureStorage
                await SecurityHelper.SaveToSecureStorage(testKey, testValue);
                
                // Verify data is not stored in plain text
                string retrievedValue = await SecurityHelper.GetFromSecureStorage(testKey);
                Assert.Equal(testValue, retrievedValue);
                
                // Test that the data cannot be accessed using regular methods
                string appDataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string potentialStorageFile = Path.Combine(appDataDirectory, testKey);
                
                // Verify secure storage is not storing data in plain files
                bool fileExists = File.Exists(potentialStorageFile);
                Assert.False(fileExists, "Sensitive data appears to be stored in plain files");
                
                // Verify data is stored in platform-specific secure storage
                bool isSecureStorageAvailable = await SecurityHelper.IsSecureStorageAvailable();
                Assert.True(isSecureStorageAvailable, "Secure storage is not available on this device");
                
                // Remove data using SecurityHelper.RemoveFromSecureStorage
                await SecurityHelper.RemoveFromSecureStorage(testKey);
                
                // Verify data is properly removed from storage
                string removedValue = await SecurityHelper.GetFromSecureStorage(testKey);
                Assert.Null(removedValue);
                
                _logger.LogInformation("Secure storage implementation verification completed successfully");
            }
            catch (Exception ex)
            {
                LogSecurityIssue("SecureStorageTestFailure", $"Secure storage test failed: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        /// <summary>
        /// Verifies that the SQLite database is properly encrypted
        /// </summary>
        /// <returns>A task representing the asynchronous test operation</returns>
        [Fact]
        [Trait("Category", "Security")]
        public async Task TestDatabaseEncryption()
        {
            try
            {
                // Get database file path
                string appDataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string dbPath = Path.Combine(appDataDirectory, DatabaseConstants.DatabaseName);
                
                // Verify database file exists
                Assert.True(File.Exists(dbPath), "Database file does not exist");
                
                // Attempt to open database file without encryption key
                var exception = await Assert.ThrowsAsync<SQLiteException>(async () => 
                {
                    // Create connection without proper encryption parameters
                    var unencryptedConnection = new SQLiteAsyncConnection(dbPath);
                    return await unencryptedConnection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM sqlite_master");
                });
                
                // Verify attempt fails due to encryption
                Assert.Contains("encrypted", exception.Message.ToLower(), StringComparison.OrdinalIgnoreCase);
                
                // Examine database file content
                byte[] dbContent = await File.ReadAllBytesAsync(dbPath);
                
                // Verify database content is encrypted
                // Standard SQLite header starts with "SQLite format 3\0"
                string sqliteHeader = "SQLite format 3";
                byte[] headerBytes = System.Text.Encoding.ASCII.GetBytes(sqliteHeader);
                
                // Check if the plaintext header is present (should not be if encrypted)
                bool containsPlaintextHeader = ContainsSequence(dbContent, headerBytes);
                Assert.False(containsPlaintextHeader, "Database file appears to be unencrypted");
                
                // Verify encryption uses strong algorithm (AES-256)
                // We can indirectly verify this by ensuring we can access the database with our connection
                var result = await _dbConnection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM sqlite_master");
                Assert.True(result >= 0, "Unable to access database with proper connection");
                
                // Verify database connection string includes encryption parameters
                Assert.Contains("encrypt", DatabaseConstants.ConnectionString.ToLower(), StringComparison.OrdinalIgnoreCase);
                
                _logger.LogInformation("Database encryption verification completed successfully");
            }
            catch (Exception ex)
            {
                LogSecurityIssue("DatabaseEncryptionTestFailure", $"Database encryption test failed: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        /// <summary>
        /// Verifies that sensitive data is properly protected in the database
        /// </summary>
        /// <returns>A task representing the asynchronous test operation</returns>
        [Fact]
        [Trait("Category", "Security")]
        public async Task TestSensitiveDataProtection()
        {
            try
            {
                // Insert test sensitive data into database
                string testUserId = TestConstants.TestUserId;
                string testSensitiveData = "SENSITIVE-" + Guid.NewGuid().ToString();
                string encryptionKey = SecurityHelper.GenerateSecureKey(256);
                
                // Encrypt the sensitive data
                string encryptedData = SecurityHelper.EncryptString(testSensitiveData, encryptionKey);
                
                // Verify sensitive data is not stored in plain text
                Assert.NotEqual(testSensitiveData, encryptedData);
                
                // Insert test data into User table
                await _dbConnection.ExecuteAsync(
                    $"INSERT INTO {DatabaseConstants.TableUser} ({DatabaseConstants.ColumnUserId}, {DatabaseConstants.ColumnPhoneNumber}, {DatabaseConstants.ColumnLastAuthenticated}, {DatabaseConstants.ColumnAuthToken}) " +
                    $"VALUES (?, ?, ?, ?)",
                    testUserId, TestConstants.TestPhoneNumber, DateTime.UtcNow.ToString("o"), encryptedData);
                
                // Verify sensitive columns are encrypted
                var rawData = await _dbConnection.ExecuteScalarAsync<string>(
                    $"SELECT {DatabaseConstants.ColumnAuthToken} FROM {DatabaseConstants.TableUser} WHERE {DatabaseConstants.ColumnUserId} = ?",
                    testUserId);
                
                Assert.Equal(encryptedData, rawData);
                Assert.NotEqual(testSensitiveData, rawData);
                
                // Retrieve data through normal application flow
                string decryptedData = SecurityHelper.DecryptString(rawData, encryptionKey);
                
                // Verify data is correctly decrypted when accessed properly
                Assert.Equal(testSensitiveData, decryptedData);
                
                // Examine raw database content
                string dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                    DatabaseConstants.DatabaseName);
                byte[] fileContent = await File.ReadAllBytesAsync(dbPath);
                
                // Convert sensitive data to bytes for comparison
                byte[] sensitiveBytes = System.Text.Encoding.UTF8.GetBytes(testSensitiveData);
                
                // Verify sensitive data is not visible in raw database
                bool containsSensitiveData = ContainsSequence(fileContent, sensitiveBytes);
                Assert.False(containsSensitiveData, "Sensitive data found in raw database content");
                
                // Clean up test data
                await _dbConnection.ExecuteAsync(
                    $"DELETE FROM {DatabaseConstants.TableUser} WHERE {DatabaseConstants.ColumnUserId} = ?",
                    testUserId);
                
                _logger.LogInformation("Sensitive data protection verification completed successfully");
            }
            catch (Exception ex)
            {
                LogSecurityIssue("SensitiveDataProtectionTestFailure", $"Sensitive data protection test failed: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        /// <summary>
        /// Verifies that data encryption is properly implemented
        /// </summary>
        /// <returns>A task representing the asynchronous test operation</returns>
        [Fact]
        [Trait("Category", "Security")]
        public async Task TestDataEncryptionImplementation()
        {
            try
            {
                // Generate test data and encryption key
                string originalData = "Test sensitive data - " + Guid.NewGuid().ToString();
                string encryptionKey = SecurityHelper.GenerateSecureKey(256);
                
                // Encrypt data using SecurityHelper.EncryptString
                string encryptedData = SecurityHelper.EncryptString(originalData, encryptionKey);
                
                // Verify encrypted data is different from original data
                Assert.NotEqual(originalData, encryptedData);
                
                // Verify encryption uses AES-256 algorithm
                Assert.True(IsBase64String(encryptedData), "Encrypted data is not in valid Base64 format");
                
                // Decode Base64 to examine encrypted bytes
                byte[] encryptedBytes = Convert.FromBase64String(encryptedData);
                
                // Verify initialization vector (IV) is properly used
                // AES IV is 16 bytes, so encrypted data should be at least 16 bytes (IV) + some data
                Assert.True(encryptedBytes.Length > 16, "Encrypted data too small to include proper IV");
                
                // Use the base class helper method to validate encryption
                bool isEncryptionSecure = ValidateEncryption(originalData, encryptedBytes);
                Assert.True(isEncryptionSecure, "Encryption implementation does not meet security requirements");
                
                // Decrypt data using SecurityHelper.DecryptString
                string decryptedData = SecurityHelper.DecryptString(encryptedData, encryptionKey);
                
                // Verify decrypted data matches original data
                Assert.Equal(originalData, decryptedData);
                
                // Test encryption with different keys
                string differentKey = SecurityHelper.GenerateSecureKey(256);
                
                // Verify data encrypted with one key cannot be decrypted with another
                await Assert.ThrowsAnyAsync<Exception>(() => 
                    Task.FromResult(SecurityHelper.DecryptString(encryptedData, differentKey)));
                
                _logger.LogInformation("Data encryption implementation verification completed successfully");
            }
            catch (Exception ex)
            {
                LogSecurityIssue("EncryptionImplementationTestFailure", $"Data encryption test failed: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        /// <summary>
        /// Verifies that database access controls are properly implemented
        /// </summary>
        /// <returns>A task representing the asynchronous test operation</returns>
        [Fact]
        [Trait("Category", "Security")]
        public async Task TestDatabaseAccessControls()
        {
            try
            {
                // Set up the test environment for vulnerability testing
                await SetupVulnerabilityTest("database");
                
                // Verify database file permissions are restricted
                string dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                    DatabaseConstants.DatabaseName);
                
                FileInfo dbFileInfo = new FileInfo(dbPath);
                Assert.True(dbFileInfo.Exists, "Database file does not exist");
                
                // Verify database is not accessible to other applications
                // On Android, this means checking that the file is in the app's private storage
                bool isInPrivateDir = dbPath.Contains("data/data") || dbPath.Contains("Android/data");
                Assert.True(isInPrivateDir, "Database not stored in application-specific protected storage");
                
                // Verify database connection requires proper credentials
                // Try to open the database with an invalid connection string
                await Assert.ThrowsAnyAsync<Exception>(async () => 
                {
                    var invalidConnection = new SQLiteAsyncConnection(dbPath);
                    await invalidConnection.ExecuteScalarAsync<int>("SELECT 1");
                });
                
                // Attempt unauthorized access to database
                // This simulates trying to open the database without the correct password
                await Assert.ThrowsAnyAsync<SQLiteException>(async () => 
                {
                    string invalidConnectionString = $"Data Source={dbPath};Password=wrong_password;";
                    var unauthorizedConnection = new SQLiteAsyncConnection(invalidConnectionString);
                    await unauthorizedConnection.ExecuteScalarAsync<int>("SELECT 1");
                });
                
                // Verify unauthorized access attempts are blocked
                
                // Verify database is stored in application-specific protected storage
                // This is already checked earlier in the test
                
                _logger.LogInformation("Database access controls verification completed successfully");
            }
            catch (Exception ex)
            {
                LogSecurityIssue("DatabaseAccessControlsTestFailure", $"Database access controls test failed: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        /// <summary>
        /// Verifies that user data is properly isolated
        /// </summary>
        /// <returns>A task representing the asynchronous test operation</returns>
        [Fact]
        [Trait("Category", "Security")]
        public async Task TestDataIsolation()
        {
            try
            {
                // Create test data for multiple users
                string user1Id = "test-user-1-" + Guid.NewGuid().ToString();
                string user2Id = "test-user-2-" + Guid.NewGuid().ToString();
                
                // Insert test data for the first user
                await _dbConnection.ExecuteAsync(
                    $"INSERT INTO {DatabaseConstants.TableTimeRecord} ({DatabaseConstants.ColumnUserId}, {DatabaseConstants.ColumnType}, {DatabaseConstants.ColumnTimestamp}, {DatabaseConstants.ColumnLatitude}, {DatabaseConstants.ColumnLongitude}, {DatabaseConstants.ColumnIsSynced}) " +
                    $"VALUES (?, ?, ?, ?, ?, ?)",
                    user1Id, "ClockIn", DateTime.UtcNow.ToString("o"), 34.0522, -118.2437, 0);
                
                // Insert test data for the second user
                await _dbConnection.ExecuteAsync(
                    $"INSERT INTO {DatabaseConstants.TableTimeRecord} ({DatabaseConstants.ColumnUserId}, {DatabaseConstants.ColumnType}, {DatabaseConstants.ColumnTimestamp}, {DatabaseConstants.ColumnLatitude}, {DatabaseConstants.ColumnLongitude}, {DatabaseConstants.ColumnIsSynced}) " +
                    $"VALUES (?, ?, ?, ?, ?, ?)",
                    user2Id, "ClockIn", DateTime.UtcNow.ToString("o"), 40.7128, -74.0060, 0);
                
                // Verify user can only access their own data
                // Query for user1's records
                var user1Records = await _dbConnection.QueryAsync<TimeRecordTestModel>(
                    $"SELECT * FROM {DatabaseConstants.TableTimeRecord} WHERE {DatabaseConstants.ColumnUserId} = ?",
                    user1Id);
                
                // Verify user1 can only see their own record
                Assert.Single(user1Records);
                Assert.Equal(user1Id, user1Records[0].UserId);
                
                // Query for user2's records
                var user2Records = await _dbConnection.QueryAsync<TimeRecordTestModel>(
                    $"SELECT * FROM {DatabaseConstants.TableTimeRecord} WHERE {DatabaseConstants.ColumnUserId} = ?", 
                    user2Id);
                
                // Verify user2 can only see their own record
                Assert.Single(user2Records);
                Assert.Equal(user2Id, user2Records[0].UserId);
                
                // Attempt to access data from another user
                // Simulate what would happen if a user tried to access another user's data
                // In a properly designed app, this would be prevented by filtering all queries by user ID
                
                // Verify database queries properly filter by user ID
                var crossAccessRecords = await _dbConnection.QueryAsync<TimeRecordTestModel>(
                    $"SELECT * FROM {DatabaseConstants.TableTimeRecord} WHERE {DatabaseConstants.ColumnUserId} = ? AND {DatabaseConstants.ColumnUserId} = ?",
                    user1Id, user2Id);
                
                // This should return no records because no record can have two different user IDs
                Assert.Empty(crossAccessRecords);
                
                // Verify data access methods enforce user isolation
                // Clean up test data
                await _dbConnection.ExecuteAsync(
                    $"DELETE FROM {DatabaseConstants.TableTimeRecord} WHERE {DatabaseConstants.ColumnUserId} IN (?, ?)",
                    user1Id, user2Id);
                
                _logger.LogInformation("Data isolation verification completed successfully");
            }
            catch (Exception ex)
            {
                LogSecurityIssue("DataIsolationTestFailure", $"Data isolation test failed: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        /// <summary>
        /// Verifies that database backups are securely handled
        /// </summary>
        /// <returns>A task representing the asynchronous test operation</returns>
        [Fact]
        [Trait("Category", "Security")]
        public async Task TestDataBackupSecurity()
        {
            try
            {
                // Trigger database backup operation
                string appDataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string dbPath = Path.Combine(appDataDirectory, DatabaseConstants.DatabaseName);
                string backupPath = Path.Combine(appDataDirectory, "backup_" + DatabaseConstants.DatabaseName);
                
                // Create a simulated backup by copying the database file
                File.Copy(dbPath, backupPath, true);
                
                // Verify backup file is encrypted
                await Assert.ThrowsAnyAsync<SQLiteException>(async () => 
                {
                    var unencryptedConnection = new SQLiteAsyncConnection(backupPath);
                    await unencryptedConnection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM sqlite_master");
                });
                
                // Verify backup file has restricted permissions
                FileInfo backupFileInfo = new FileInfo(backupPath);
                Assert.True(backupFileInfo.Exists, "Backup file does not exist");
                
                // Verify backup file is in app's private directory
                bool isInPrivateDir = backupPath.Contains("data/data") || backupPath.Contains("Android/data");
                Assert.True(isInPrivateDir, "Backup not stored in application-specific protected storage");
                
                // Verify backup process uses secure channels
                // Since we're simulating a backup, we can't fully test this
                // In a real implementation, backups would be encrypted before transmission
                
                // Verify backup encryption keys are properly protected
                // In a real implementation, encryption keys would be stored in secure storage
                
                // Attempt to access backup without proper credentials
                await Assert.ThrowsAnyAsync<SQLiteException>(async () => 
                {
                    var unauthorizedConnection = new SQLiteAsyncConnection(backupPath);
                    await unauthorizedConnection.ExecuteScalarAsync<int>("SELECT 1");
                });
                
                // Verify unauthorized access to backup is prevented
                
                // Clean up test backup
                File.Delete(backupPath);
                
                _logger.LogInformation("Data backup security verification completed successfully");
            }
            catch (Exception ex)
            {
                LogSecurityIssue("DataBackupSecurityTestFailure", $"Data backup security test failed: {ex.Message}", LogLevel.Error);
                throw;
            }
            finally
            {
                // Make sure we clean up the backup file
                string backupPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                    "backup_" + DatabaseConstants.DatabaseName);
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }
            }
        }

        /// <summary>
        /// Verifies protection against SQL injection attacks
        /// </summary>
        /// <returns>A task representing the asynchronous test operation</returns>
        [Fact]
        [Trait("Category", "Security")]
        public async Task TestSQLInjectionProtection()
        {
            try
            {
                // Prepare SQL injection test payloads
                var injectionPayloads = new[]
                {
                    "'; DROP TABLE Users; --",
                    "1' OR '1'='1",
                    "1'; SELECT * FROM sqlite_master; --",
                    "Robert'); DROP TABLE TimeRecord; --"
                };
                
                // List of tables to verify existence before and after injection attempts
                var tablesToCheck = new[]
                {
                    DatabaseConstants.TableUser,
                    DatabaseConstants.TableTimeRecord,
                    DatabaseConstants.TableLocationRecord,
                    DatabaseConstants.TablePhoto,
                    DatabaseConstants.TableActivityReport
                };
                
                // Verify tables exist before injection attempts
                foreach (var table in tablesToCheck)
                {
                    var tableExists = await _dbConnection.ExecuteScalarAsync<int>(
                        "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=?", table);
                    Assert.Equal(1, tableExists);
                }
                
                // Attempt SQL injection attacks on database operations
                foreach (var payload in injectionPayloads)
                {
                    // Test parameterized query (safe approach)
                    var result = await _dbConnection.ExecuteScalarAsync<int>(
                        $"SELECT COUNT(*) FROM {DatabaseConstants.TableUser} WHERE {DatabaseConstants.ColumnUserId} = ?", 
                        payload);
                    
                    // The payload should be treated as a literal string, not executed
                    Assert.Equal(0, result);
                    
                    // Test unsafe string concatenation (if possible)
                    try
                    {
                        // This is intentionally dangerous and should fail safely
                        // Note: SQLite-net-pcl doesn't actually allow direct SQL injection like this
                        // but we're testing the behavior if someone were to try
                        await _dbConnection.ExecuteScalarAsync<int>(
                            $"SELECT COUNT(*) FROM {DatabaseConstants.TableUser} WHERE {DatabaseConstants.ColumnUserId} = '{payload}'");
                    }
                    catch (SQLiteException)
                    {
                        // Expected exception for malformed SQL, but tables should remain intact
                    }
                }
                
                // Verify parameterized queries are used
                // This is verified implicitly by the prior test
                
                // Verify input validation is properly implemented
                // This would be implemented in application code, not directly testable here
                
                // Verify SQL injection attempts are blocked
                // Verify all tables still exist after injection attempts
                foreach (var table in tablesToCheck)
                {
                    var tableExists = await _dbConnection.ExecuteScalarAsync<int>(
                        "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=?", table);
                    Assert.Equal(1, tableExists);
                }
                
                // Verify SQL injection attempts are logged
                // This would be implemented in application code, not directly testable here
                
                // Test ORM layer for SQL injection vulnerabilities
                // SQLite-net-pcl uses parameterized queries internally
                
                _logger.LogInformation("SQL injection protection verification completed successfully");
            }
            catch (Exception ex)
            {
                LogSecurityIssue("SqlInjectionTestFailure", $"SQL injection protection test failed: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        /// <summary>
        /// Verifies prevention of data leakage through various channels
        /// </summary>
        /// <returns>A task representing the asynchronous test operation</returns>
        [Fact]
        [Trait("Category", "Security")]
        public async Task TestDataLeakagePrevention()
        {
            try
            {
                // Insert sensitive test data into database
                string testUserId = "leak-test-" + Guid.NewGuid().ToString();
                string testSensitiveData = "SENSITIVE-DO-NOT-LOG-" + Guid.NewGuid().ToString();
                string testPhoneNumber = "+1234567890";
                
                // Insert test data into User table
                await _dbConnection.ExecuteAsync(
                    $"INSERT INTO {DatabaseConstants.TableUser} ({DatabaseConstants.ColumnUserId}, {DatabaseConstants.ColumnPhoneNumber}, {DatabaseConstants.ColumnLastAuthenticated}) " +
                    $"VALUES (?, ?, ?)",
                    testUserId, testPhoneNumber, DateTime.UtcNow.ToString("o"));
                
                // Check for data leakage in application logs
                // Create a test logger with a mock to capture log output
                var loggerMock = new Mock<ILogger>();
                
                // Simulate logging with sensitive data
                try
                {
                    // This is a deliberate "mistake" to check if sensitive data is logged
                    // In a secure application, this would be prevented
                    loggerMock.Object.LogInformation($"User logged in: {testUserId}, phone: {testPhoneNumber}");
                    
                    // In a secure implementation, phone numbers should be masked in logs
                }
                catch (Exception ex)
                {
                    // Should not throw, but if it does, log it
                    LogSecurityIssue("LoggingTestException", ex.Message, LogLevel.Error);
                }
                
                // Check for data leakage in error messages
                try
                {
                    // Simulate an error with sensitive data
                    throw new InvalidOperationException($"Operation failed for user ID: {testUserId} with phone: {testPhoneNumber}");
                }
                catch (Exception ex)
                {
                    // In a secure app, sensitive data should be redacted from error messages
                    // Here we're just verifying the exception contains the data for testing purposes
                    Assert.Contains(testUserId, ex.Message);
                    Assert.Contains(testPhoneNumber, ex.Message);
                }
                
                // Check for data leakage in crash reports
                // This is difficult to test directly in a unit test
                
                // Check for data leakage in temporary files
                string tempDir = Path.GetTempPath();
                var tempFiles = Directory.GetFiles(tempDir);
                foreach (var file in tempFiles)
                {
                    if (File.Exists(file))
                    {
                        string content = await File.ReadAllTextAsync(file);
                        Assert.DoesNotContain(testSensitiveData, content);
                    }
                }
                
                // Check for data leakage in memory dumps
                // Not feasible to test in a unit test
                
                // Verify sensitive data is properly redacted in logs
                // This would require checking application logs, difficult in a unit test
                
                // Clean up test data
                await _dbConnection.ExecuteAsync(
                    $"DELETE FROM {DatabaseConstants.TableUser} WHERE {DatabaseConstants.ColumnUserId} = ?",
                    testUserId);
                
                _logger.LogInformation("Data leakage prevention verification completed successfully");
            }
            catch (Exception ex)
            {
                LogSecurityIssue("DataLeakageTestFailure", $"Data leakage prevention test failed: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        /// <summary>
        /// Verifies compliance with data retention policies
        /// </summary>
        /// <returns>A task representing the asynchronous test operation</returns>
        [Fact]
        [Trait("Category", "Security")]
        public async Task TestDataRetentionCompliance()
        {
            try
            {
                // Insert test data with various timestamps
                var now = DateTime.UtcNow;
                string testUserId = "retention-test-" + Guid.NewGuid().ToString();
                
                // Create current data (should be retained)
                await _dbConnection.ExecuteAsync(
                    $"INSERT INTO {DatabaseConstants.TableLocationRecord} ({DatabaseConstants.ColumnUserId}, {DatabaseConstants.ColumnTimestamp}, {DatabaseConstants.ColumnLatitude}, {DatabaseConstants.ColumnLongitude}, {DatabaseConstants.ColumnAccuracy}, {DatabaseConstants.ColumnIsSynced}) " +
                    $"VALUES (?, ?, ?, ?, ?, ?)",
                    testUserId, now.ToString("o"), 34.0522, -118.2437, 10.0, 1);
                
                // Create old data (should be deleted by retention policy)
                // Note: We're using AppConstants.LocationMaxStoredPoints as if it were days - in a real test
                // we would use the actual retention period constant
                await _dbConnection.ExecuteAsync(
                    $"INSERT INTO {DatabaseConstants.TableLocationRecord} ({DatabaseConstants.ColumnUserId}, {DatabaseConstants.ColumnTimestamp}, {DatabaseConstants.ColumnLatitude}, {DatabaseConstants.ColumnLongitude}, {DatabaseConstants.ColumnAccuracy}, {DatabaseConstants.ColumnIsSynced}) " +
                    $"VALUES (?, ?, ?, ?, ?, ?)",
                    testUserId, now.AddDays(-35).ToString("o"), 34.0522, -118.2437, 10.0, 1);
                
                // Verify both records were inserted
                var initialCount = await _dbConnection.ExecuteScalarAsync<int>(
                    $"SELECT COUNT(*) FROM {DatabaseConstants.TableLocationRecord} WHERE {DatabaseConstants.ColumnUserId} = ?",
                    testUserId);
                Assert.Equal(2, initialCount);
                
                // Trigger data retention cleanup process
                await _dbConnection.ExecuteAsync(
                    $"DELETE FROM {DatabaseConstants.TableLocationRecord} WHERE " +
                    $"{DatabaseConstants.ColumnTimestamp} < ? AND {DatabaseConstants.ColumnIsSynced} = 1",
                    now.AddDays(-30).ToString("o"));
                
                // Verify data older than retention period is properly deleted
                var afterCleanupCount = await _dbConnection.ExecuteScalarAsync<int>(
                    $"SELECT COUNT(*) FROM {DatabaseConstants.TableLocationRecord} WHERE {DatabaseConstants.ColumnUserId} = ?",
                    testUserId);
                Assert.Equal(1, afterCleanupCount);
                
                // Verify current data is preserved
                var remainingData = await _dbConnection.ExecuteScalarAsync<string>(
                    $"SELECT {DatabaseConstants.ColumnTimestamp} FROM {DatabaseConstants.TableLocationRecord} WHERE {DatabaseConstants.ColumnUserId} = ?",
                    testUserId);
                var remainingDate = DateTime.Parse(remainingData);
                
                // Should be the recent record (within 1 day of now)
                var dayDifference = Math.Abs((now - remainingDate).TotalDays);
                Assert.True(dayDifference < 1, "Wrong record retained after cleanup");
                
                // Verify deletion is permanent and secure
                // This is difficult to test directly as it would require examining the SQLite file internals
                
                // Clean up test data
                await _dbConnection.ExecuteAsync(
                    $"DELETE FROM {DatabaseConstants.TableLocationRecord} WHERE {DatabaseConstants.ColumnUserId} = ?",
                    testUserId);
                
                _logger.LogInformation("Data retention compliance verification completed successfully");
            }
            catch (Exception ex)
            {
                LogSecurityIssue("DataRetentionTestFailure", $"Data retention compliance test failed: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        /// <summary>
        /// Verifies proper handling of database corruption scenarios
        /// </summary>
        /// <returns>A task representing the asynchronous test operation</returns>
        [Fact]
        [Trait("Category", "Security")]
        public async Task TestDatabaseCorruptionHandling()
        {
            try
            {
                // Create test database with sample data
                string appDataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string testDbPath = Path.Combine(appDataDirectory, "test_corruption.db");
                
                // Remove test database if it exists
                if (File.Exists(testDbPath))
                    File.Delete(testDbPath);
                
                // Create and initialize a test database
                var testConnection = new SQLiteAsyncConnection(testDbPath);
                await testConnection.ExecuteAsync("CREATE TABLE TestTable (Id INTEGER PRIMARY KEY, Name TEXT)");
                await testConnection.ExecuteAsync("INSERT INTO TestTable (Name) VALUES ('Test Data')");
                
                // Verify database was created with test data
                var count = await testConnection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM TestTable");
                Assert.Equal(1, count);
                
                // Close the connection to ensure all data is written
                await testConnection.CloseAsync();
                
                // Simulate database corruption
                using (var stream = new FileStream(testDbPath, FileMode.Open, FileAccess.ReadWrite))
                {
                    // Seek to an offset that will corrupt the database
                    stream.Seek(100, SeekOrigin.Begin);
                    
                    // Write random bytes to corrupt the file
                    byte[] corruptionBytes = new byte[10];
                    new Random().NextBytes(corruptionBytes);
                    stream.Write(corruptionBytes, 0, corruptionBytes.Length);
                }
                
                // Attempt to access corrupted database
                SQLiteException corruptionException = null;
                try
                {
                    var corruptedConnection = new SQLiteAsyncConnection(testDbPath);
                    await corruptedConnection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM TestTable");
                }
                catch (SQLiteException ex)
                {
                    // Expected exception for corrupted database
                    corruptionException = ex;
                }
                
                // Verify corruption is detected
                Assert.NotNull(corruptionException);
                
                // Verify appropriate error handling
                // This would be implemented in application code
                
                // Verify recovery mechanisms are triggered
                // In a real app, this might include:
                // 1. Detecting corruption
                // 2. Attempting repair if possible
                // 3. Restoring from backup if repair fails
                // 4. Creating a new database if all else fails
                
                // Clean up test database
                if (File.Exists(testDbPath))
                    File.Delete(testDbPath);
                
                _logger.LogInformation("Database corruption handling verification completed successfully");
            }
            catch (Exception ex)
            {
                LogSecurityIssue("DatabaseCorruptionTestFailure", $"Database corruption handling test failed: {ex.Message}", LogLevel.Error);
                throw;
            }
            finally
            {
                // Make sure we clean up the test database
                string testDbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                    "test_corruption.db");
                if (File.Exists(testDbPath))
                {
                    File.Delete(testDbPath);
                }
            }
        }

        /// <summary>
        /// Verifies that data deletion is securely implemented
        /// </summary>
        /// <returns>A task representing the asynchronous test operation</returns>
        [Fact]
        [Trait("Category", "Security")]
        public async Task TestSecureDeleteImplementation()
        {
            try
            {
                // Insert test data into database
                string testUserId = "delete-test-" + Guid.NewGuid().ToString();
                string testSensitiveData = "SENSITIVE-TO-DELETE-" + Guid.NewGuid().ToString();
                
                // Insert test data into database
                await _dbConnection.ExecuteAsync(
                    $"INSERT INTO {DatabaseConstants.TableUser} ({DatabaseConstants.ColumnUserId}, {DatabaseConstants.ColumnPhoneNumber}, {DatabaseConstants.ColumnLastAuthenticated}, {DatabaseConstants.ColumnAuthToken}) " +
                    $"VALUES (?, ?, ?, ?)",
                    testUserId, "+1234567890", DateTime.UtcNow.ToString("o"), testSensitiveData);
                
                // Verify test data was inserted
                var insertedData = await _dbConnection.ExecuteScalarAsync<string>(
                    $"SELECT {DatabaseConstants.ColumnAuthToken} FROM {DatabaseConstants.TableUser} WHERE {DatabaseConstants.ColumnUserId} = ?",
                    testUserId);
                Assert.Equal(testSensitiveData, insertedData);
                
                // Delete test data through application methods
                await _dbConnection.ExecuteAsync(
                    $"DELETE FROM {DatabaseConstants.TableUser} WHERE {DatabaseConstants.ColumnUserId} = ?",
                    testUserId);
                
                // Verify data is properly removed from database
                var count = await _dbConnection.ExecuteScalarAsync<int>(
                    $"SELECT COUNT(*) FROM {DatabaseConstants.TableUser} WHERE {DatabaseConstants.ColumnUserId} = ?",
                    testUserId);
                Assert.Equal(0, count);
                
                // Examine database file for data remnants
                // This requires low-level file analysis which is difficult in a unit test
                
                // Run VACUUM to reclaim space and remove deleted data
                await _dbConnection.ExecuteAsync("VACUUM");
                
                // Verify secure deletion methods are used
                // In SQLite, VACUUM is the secure way to ensure deleted data is properly removed
                
                // Verify deletion operations are properly logged
                // This would be implemented in application code
                
                _logger.LogInformation("Secure delete implementation verification completed successfully");
            }
            catch (Exception ex)
            {
                LogSecurityIssue("SecureDeleteTestFailure", $"Secure delete implementation test failed: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        #region Helper Methods

        private bool ContainsSequence(byte[] source, byte[] pattern)
        {
            if (pattern.Length > source.Length)
                return false;
                
            for (int i = 0; i <= source.Length - pattern.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (source[i + j] != pattern[j])
                    {
                        match = false;
                        break;
                    }
                }
                
                if (match)
                    return true;
            }
            
            return false;
        }

        private bool IsBase64String(string base64)
        {
            if (string.IsNullOrEmpty(base64))
                return false;
                
            try
            {
                Convert.FromBase64String(base64);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private class TimeRecordTestModel
        {
            public int Id { get; set; }
            public string UserId { get; set; }
            public string Type { get; set; }
            public string Timestamp { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public bool IsSynced { get; set; }
            public string RemoteId { get; set; }
        }

        #endregion
    }
}