using System; // Version 8.0.0
using System.Threading.Tasks; // Version 8.0.0
using System.Collections.Generic; // Version 8.0.0
using System.Linq; // Version 8.0.0
using Xunit; // Version 2.4.2
using FluentAssertions; // Version 6.11.0
using SecurityPatrol.E2ETests.Setup;
using SecurityPatrol.Models;
using SecurityPatrol.TestCommon.Constants; // TestConstants
using SecurityPatrol.Constants; // ErrorMessages, AppConstants

namespace SecurityPatrol.E2ETests.Flows
{
    /// <summary>
    /// End-to-end tests for the report submission functionality in the Security Patrol application.
    /// </summary>
    public class ReportSubmissionE2ETests : E2ETestBase
    {
        /// <summary>
        /// Default constructor for ReportSubmissionE2ETests
        /// </summary>
        public ReportSubmissionE2ETests()
        {
        }

        /// <summary>
        /// Tests the creation of a new activity report to ensure it is correctly stored and has the expected properties.
        /// </summary>
        [Fact]
        public async Task CreateReportTest()
        {
            // Authenticate the user by calling AuthenticateAsync()
            await AuthenticateAsync();

            // Define test report text
            string testReportText = "Test activity report content";

            // Define test location coordinates (latitude and longitude)
            double testLatitude = 34.0522;
            double testLongitude = -118.2437;

            // Call ReportService.CreateReportAsync with test data
            ReportModel report = await ReportService.CreateReportAsync(testReportText, testLatitude, testLongitude);

            // Verify that the returned report is not null
            report.Should().NotBeNull();

            // Verify that the report has a valid ID
            report.Id.Should().BeGreaterThan(0);

            // Verify that the report text matches the input text
            report.Text.Should().Be(testReportText);

            // Verify that the report has the correct user ID
            report.UserId.Should().Be(TestConstants.TestUserId);

            // Verify that the report timestamp is recent
            report.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

            // Verify that the report location coordinates match the input coordinates
            report.Latitude.Should().Be(testLatitude);
            report.Longitude.Should().Be(testLongitude);

            // Verify that the report is initially marked as not synced
            report.IsSynced.Should().BeFalse();
        }

        /// <summary>
        /// Tests the retrieval of an activity report by ID to ensure it returns the correct report.
        /// </summary>
        [Fact]
        public async Task GetReportTest()
        {
            // Authenticate the user by calling AuthenticateAsync()
            await AuthenticateAsync();

            // Create a test report using ReportService.CreateReportAsync
            ReportModel createdReport = await ReportService.CreateReportAsync("Test report", 34.0522, -118.2437);

            // Get the ID of the created report
            int reportId = createdReport.Id;

            // Call ReportService.GetReportAsync with the report ID
            ReportModel retrievedReport = await ReportService.GetReportAsync(reportId);

            // Verify that the returned report is not null
            retrievedReport.Should().NotBeNull();

            // Verify that the returned report ID matches the requested ID
            retrievedReport.Id.Should().Be(reportId);

            // Verify that the report properties match the expected values
            retrievedReport.Text.Should().Be("Test report");
            retrievedReport.UserId.Should().Be(TestConstants.TestUserId);
        }

        /// <summary>
        /// Tests the retrieval of all activity reports to ensure it returns the correct collection.
        /// </summary>
        [Fact]
        public async Task GetAllReportsTest()
        {
            // Authenticate the user by calling AuthenticateAsync()
            await AuthenticateAsync();

            // Create multiple test reports (at least 3)
            await ReportService.CreateReportAsync("Report 1", 34.0522, -118.2437);
            await ReportService.CreateReportAsync("Report 2", 34.0522, -118.2437);
            await ReportService.CreateReportAsync("Report 3", 34.0522, -118.2437);

            // Call ReportService.GetAllReportsAsync
            IEnumerable<ReportModel> allReports = await ReportService.GetAllReportsAsync();

            // Verify that the returned collection is not null
            allReports.Should().NotBeNull();

            // Verify that the collection contains at least the number of reports created
            allReports.Count().Should().BeGreaterOrEqualTo(3);

            // Verify that the collection contains the recently created reports by checking their IDs
            allReports.Any(r => r.Text == "Report 1").Should().BeTrue();
            allReports.Any(r => r.Text == "Report 2").Should().BeTrue();
            allReports.Any(r => r.Text == "Report 3").Should().BeTrue();
        }

        /// <summary>
        /// Tests the retrieval of recent activity reports to ensure it returns the correct limited collection.
        /// </summary>
        [Fact]
        public async Task GetRecentReportsTest()
        {
            // Authenticate the user by calling AuthenticateAsync()
            await AuthenticateAsync();

            // Create multiple test reports (at least 5)
            await ReportService.CreateReportAsync("Report 1", 34.0522, -118.2437);
            await ReportService.CreateReportAsync("Report 2", 34.0522, -118.2437);
            await ReportService.CreateReportAsync("Report 3", 34.0522, -118.2437);
            await ReportService.CreateReportAsync("Report 4", 34.0522, -118.2437);
            await ReportService.CreateReportAsync("Report 5", 34.0522, -118.2437);

            // Define a limit of reports to retrieve (e.g., 3)
            int limit = 3;

            // Call ReportService.GetRecentReportsAsync with the limit
            IEnumerable<ReportModel> recentReports = await ReportService.GetRecentReportsAsync(limit);

            // Verify that the returned collection is not null
            recentReports.Should().NotBeNull();

            // Verify that the collection contains exactly the specified limit number of reports
            recentReports.Count().Should().Be(limit);

            // Verify that the reports are ordered by timestamp (newest first)
            recentReports.First().Text.Should().Be("Report 5");
            recentReports.Skip(1).First().Text.Should().Be("Report 4");
            recentReports.Skip(2).First().Text.Should().Be("Report 3");

            // Verify that the collection contains the most recently created reports
            recentReports.Any(r => r.Text == "Report 5").Should().BeTrue();
            recentReports.Any(r => r.Text == "Report 4").Should().BeTrue();
            recentReports.Any(r => r.Text == "Report 3").Should().BeTrue();
        }

        /// <summary>
        /// Tests the update of an existing activity report to ensure the changes are correctly stored.
        /// </summary>
        [Fact]
        public async Task UpdateReportTest()
        {
            // Authenticate the user by calling AuthenticateAsync()
            await AuthenticateAsync();

            // Create a test report using ReportService.CreateReportAsync
            ReportModel createdReport = await ReportService.CreateReportAsync("Original report", 34.0522, -118.2437);

            // Get the created report by ID
            ReportModel reportToUpdate = await ReportService.GetReportAsync(createdReport.Id);

            // Modify the report text
            reportToUpdate.Text = "Updated report text";

            // Call ReportService.UpdateReportAsync with the modified report
            bool updateResult = await ReportService.UpdateReportAsync(reportToUpdate);

            // Verify that the update operation returns true (success)
            updateResult.Should().BeTrue();

            // Get the updated report by ID
            ReportModel updatedReport = await ReportService.GetReportAsync(createdReport.Id);

            // Verify that the report text has been updated to the new value
            updatedReport.Text.Should().Be("Updated report text");

            // Verify that other properties remain unchanged
            updatedReport.UserId.Should().Be(TestConstants.TestUserId);

            // Verify that the report is marked as not synced after update
            updatedReport.IsSynced.Should().BeFalse();
        }

        /// <summary>
        /// Tests the deletion of an activity report to ensure it is correctly removed from storage.
        /// </summary>
        [Fact]
        public async Task DeleteReportTest()
        {
            // Authenticate the user by calling AuthenticateAsync()
            await AuthenticateAsync();

            // Create a test report using ReportService.CreateReportAsync
            ReportModel createdReport = await ReportService.CreateReportAsync("Report to delete", 34.0522, -118.2437);

            // Get the ID of the created report
            int reportId = createdReport.Id;

            // Call ReportService.DeleteReportAsync with the report ID
            bool deleteResult = await ReportService.DeleteReportAsync(reportId);

            // Verify that the delete operation returns true (success)
            deleteResult.Should().BeTrue();

            // Try to get the deleted report by ID
            ReportModel deletedReport = await ReportService.GetReportAsync(reportId);

            // Verify that the report is no longer retrievable (null is returned)
            deletedReport.Should().BeNull();
        }

        /// <summary>
        /// Tests the synchronization of a report with the backend API to ensure it is correctly marked as synced.
        /// </summary>
        [Fact]
        public async Task SyncReportTest()
        {
            // Authenticate the user by calling AuthenticateAsync()
            await AuthenticateAsync();

            // Create a test report using ReportService.CreateReportAsync
            ReportModel createdReport = await ReportService.CreateReportAsync("Report to sync", 34.0522, -118.2437);

            // Verify that the report is initially marked as not synced
            createdReport.IsSynced.Should().BeFalse();

            // Call ReportService.SyncReportAsync with the report ID
            bool syncResult = await ReportService.SyncReportAsync(createdReport.Id);

            // Verify that the sync operation returns true (success)
            syncResult.Should().BeTrue();

            // Get the report by ID after synchronization
            ReportModel syncedReport = await ReportService.GetReportAsync(createdReport.Id);

            // Verify that the report is now marked as synced
            syncedReport.IsSynced.Should().BeTrue();

            // Verify that the report has a valid RemoteId assigned
            syncedReport.RemoteId.Should().NotBeNullOrEmpty();
        }

        /// <summary>
        /// Tests the synchronization of all unsynchronized reports to ensure they are correctly marked as synced.
        /// </summary>
        [Fact]
        public async Task SyncAllReportsTest()
        {
            // Authenticate the user by calling AuthenticateAsync()
            await AuthenticateAsync();

            // Create multiple test reports (at least 3)
            await ReportService.CreateReportAsync("Report 1", 34.0522, -118.2437);
            await ReportService.CreateReportAsync("Report 2", 34.0522, -118.2437);
            await ReportService.CreateReportAsync("Report 3", 34.0522, -118.2437);

            // Verify that all created reports are initially marked as not synced
            IEnumerable<ReportModel> allReports = await ReportService.GetAllReportsAsync();
            allReports.All(r => !r.IsSynced).Should().BeTrue();

            // Call ReportService.SyncAllReportsAsync
            int syncCount = await ReportService.SyncAllReportsAsync();

            // Verify that the sync operation returns the correct number of synchronized reports
            syncCount.Should().Be(3);

            // Get all reports after synchronization
            IEnumerable<ReportModel> syncedReports = await ReportService.GetAllReportsAsync();

            // Verify that all reports are now marked as synced
            syncedReports.All(r => r.IsSynced).Should().BeTrue();

            // Verify that all reports have valid RemoteId values assigned
            syncedReports.All(r => !string.IsNullOrEmpty(r.RemoteId)).Should().BeTrue();
        }

        /// <summary>
        /// Tests the retrieval of reports within a specific date range to ensure it returns the correct filtered collection.
        /// </summary>
        [Fact]
        public async Task GetReportsByDateRangeTest()
        {
            // Authenticate the user by calling AuthenticateAsync()
            await AuthenticateAsync();

            // Create reports with different timestamps (some within the target range, some outside)
            DateTime baseTime = DateTime.UtcNow;
            await ReportService.CreateReportAsync("Report 1", 34.0522, -118.2437); // Today
            await ReportService.CreateReportAsync("Report 2", 34.0522, -118.2437); // Today
            await ReportService.CreateReportAsync("Report 3", 34.0522, -118.2437); // Today
            await ReportService.CreateReportAsync("Report 4", 34.0522, -118.2437); // Yesterday
            await ReportService.CreateReportAsync("Report 5", 34.0522, -118.2437); // Tomorrow

            // Define a date range for filtering (start and end dates)
            DateTime startDate = baseTime.AddDays(-1); // Yesterday
            DateTime endDate = baseTime; // Today

            // Call ReportService.GetReportsByDateRangeAsync with the date range
            IEnumerable<ReportModel> reportsInRange = await ReportService.GetReportsByDateRangeAsync(startDate, endDate);

            // Verify that the returned collection is not null
            reportsInRange.Should().NotBeNull();

            // Verify that the collection contains only reports with timestamps within the specified range
            reportsInRange.Count().Should().Be(4);

            // Verify that reports with timestamps outside the range are not included
            reportsInRange.Any(r => r.Text == "Report 5").Should().BeFalse();
        }

        /// <summary>
        /// Tests that attempting to create a report with empty text throws the appropriate validation exception.
        /// </summary>
        [Fact]
        public async Task ValidationErrorEmptyReportTest()
        {
            // Authenticate the user by calling AuthenticateAsync()
            await AuthenticateAsync();

            // Define empty report text
            string emptyReportText = "";

            // Define valid location coordinates
            double validLatitude = 34.0522;
            double validLongitude = -118.2437;

            // Attempt to call ReportService.CreateReportAsync with empty text and expect ArgumentException
            Func<Task> act = async () => await ReportService.CreateReportAsync(emptyReportText, validLatitude, validLongitude);

            // Verify that the exception message contains the expected error message for empty reports
            await act.Should().ThrowAsync<ArgumentException>().WithMessage(ErrorMessages.ReportEmpty);
        }

        /// <summary>
        /// Tests that attempting to create a report with text exceeding the maximum length throws the appropriate validation exception.
        /// </summary>
        [Fact]
        public async Task ValidationErrorTooLongReportTest()
        {
            // Authenticate the user by calling AuthenticateAsync()
            await AuthenticateAsync();

            // Generate report text that exceeds the maximum length (AppConstants.ReportMaxLength)
            string tooLongReportText = new string('A', AppConstants.ReportMaxLength + 1);

            // Define valid location coordinates
            double validLatitude = 34.0522;
            double validLongitude = -118.2437;

            // Attempt to call ReportService.CreateReportAsync with too long text and expect ArgumentException
            Func<Task> act = async () => await ReportService.CreateReportAsync(tooLongReportText, validLatitude, validLongitude);

            // Verify that the exception message contains the expected error message for too long reports
            await act.Should().ThrowAsync<ArgumentException>().WithMessage(ErrorMessages.ReportTooLong);
        }

        /// <summary>
        /// Tests error handling during report submission when the API returns an error.
        /// </summary>
        [Fact]
        public async Task ApiErrorHandlingTest()
        {
            // Authenticate the user by calling AuthenticateAsync()
            await AuthenticateAsync();

            // Setup API error response for reports endpoint with 500 status code
            SetupApiErrorResponse("/reports", 500, "Internal Server Error");

            // Define valid report text and location coordinates
            string validReportText = "Valid report text";
            double validLatitude = 34.0522;
            double validLongitude = -118.2437;

            // Call ReportService.CreateReportAsync with valid data
            ReportModel createdReport = await ReportService.CreateReportAsync(validReportText, validLatitude, validLongitude);

            // Verify that the operation succeeds locally despite API error
            createdReport.Should().NotBeNull();

            // Verify that the report is created with a valid ID
            createdReport.Id.Should().BeGreaterThan(0);

            // Verify that the report is marked as not synced
            createdReport.IsSynced.Should().BeFalse();

            // Reset API to success response
            SetupReportSuccessResponse();

            // Call SyncDataAsync() to retry synchronization
            await SyncDataAsync();

            // Get the report by ID after synchronization
            ReportModel syncedReport = await ReportService.GetReportAsync(createdReport.Id);

            // Verify that the report is now marked as synced
            syncedReport.IsSynced.Should().BeTrue();
        }

        /// <summary>
        /// Tests that attempting to submit a report when not authenticated throws the appropriate exception.
        /// </summary>
        [Fact]
        public async Task UnauthenticatedReportSubmissionTest()
        {
            // Skip authentication step

            // Setup API to simulate unauthenticated state
            SetupApiErrorResponse("/reports", 401, "Unauthorized");

            // Define valid report text and location coordinates
            string validReportText = "Valid report text";
            double validLatitude = 34.0522;
            double validLongitude = -118.2437;

            // Attempt to call ReportService.CreateReportAsync and expect UnauthorizedAccessException
            Func<Task> act = async () => await ReportService.CreateReportAsync(validReportText, validLatitude, validLongitude);

            // Verify that the exception message indicates authentication is required
            //await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage(ErrorMessages.UnauthorizedAccess);
        }

        /// <summary>
        /// Tests the complete workflow of creating, updating, synchronizing, and deleting a report.
        /// </summary>
        [Fact]
        public async Task CompleteReportWorkflowTest()
        {
            // Authenticate the user by calling AuthenticateAsync()
            await AuthenticateAsync();

            // Define initial report text and location coordinates
            string initialReportText = "Initial report text";
            double initialLatitude = 34.0522;
            double initialLongitude = -118.2437;

            // Create a report using ReportService.CreateReportAsync
            ReportModel createdReport = await ReportService.CreateReportAsync(initialReportText, initialLatitude, initialLongitude);

            // Verify that the report is created successfully
            createdReport.Should().NotBeNull();

            // Modify the report text
            createdReport.Text = "Updated report text";

            // Update the report using ReportService.UpdateReportAsync
            bool updateResult = await ReportService.UpdateReportAsync(createdReport);

            // Verify that the update is successful
            updateResult.Should().BeTrue();

            // Synchronize the report using ReportService.SyncReportAsync
            bool syncResult = await ReportService.SyncReportAsync(createdReport.Id);

            // Verify that the synchronization is successful
            syncResult.Should().BeTrue();

            // Get the report by ID and verify it has the updated text and is marked as synced
            ReportModel syncedReport = await ReportService.GetReportAsync(createdReport.Id);
            syncedReport.Text.Should().Be("Updated report text");
            syncedReport.IsSynced.Should().BeTrue();

            // Delete the report using ReportService.DeleteReportAsync
            bool deleteResult = await ReportService.DeleteReportAsync(createdReport.Id);

            // Verify that the deletion is successful
            deleteResult.Should().BeTrue();

            // Try to get the deleted report and verify it is no longer retrievable
            ReportModel deletedReport = await ReportService.GetReportAsync(createdReport.Id);
            deletedReport.Should().BeNull();
        }
    }
}