using System; // System 8.0+

namespace SecurityPatrol.TestCommon.Constants
{
    /// <summary>
    /// Static class containing constant values used throughout the test projects for the Security Patrol application.
    /// </summary>
    public static class TestConstants
    {
        // Database constants
        public const string TestDatabaseFilename = "securitypatrol_test.db3";
        public const int TestDatabaseVersion = 1;
        
        // Authentication constants
        public const string TestPhoneNumber = "+15555555555";
        public const string TestVerificationCode = "123456";
        public const string TestUserId = "test-user-123";
        public const string TestAuthToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ0ZXN0LXVzZXItMTIzIiwibmFtZSI6IlRlc3QgVXNlciIsImlhdCI6MTUxNjIzOTAyMn0.yf1r8hsJG5I2SVmL7qkz9pF2WwSDNArZEpcs3DrQJVE";
        public const string TestRefreshToken = "refresh-token-test-123";
        
        // Location constants
        public const double TestLatitude = 34.0522;  // Los Angeles
        public const double TestLongitude = -118.2437;
        public const double TestAccuracy = 10.0;  // 10 meters
        
        // ID constants
        public const int TestLocationId = 1;
        public const int TestCheckpointId = 101;
        
        // Content constants
        public const string TestReportText = "This is a test activity report for testing purposes.";
        public const string TestImagePath = "test_image.jpg";
        
        // API constants
        public const string TestApiBaseUrl = "https://api.test.securitypatrol.local/";
        public const int TestTimeoutMilliseconds = 5000;  // 5 seconds
        public const int TestRetryCount = 3;
        public const int TestBatchSize = 10;
    }
}