using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Xunit; // Version 2.4.2
using FluentAssertions; // Version 6.11.0
using Newtonsoft.Json; // Version 13.0.0
using SecurityPatrol.MAUI.IntegrationTests.Setup;
using SecurityPatrol.Models;
using SecurityPatrol.Services;

namespace SecurityPatrol.MAUI.IntegrationTests.ApiIntegration
{
    /// <summary>
    /// Integration tests for the Report API functionality in the Security Patrol application.
    /// </summary>
    [public]
    public class ReportApiTests : IntegrationTestBase
    {
        /// <summary>
        /// Initializes a new instance of the ReportApiTests class.
        /// </summary>
        public ReportApiTests()
        {
            // Call base constructor to initialize the test environment
        }

        /// <summary>
        /// Tests that a report can be successfully created and synchronized with the API.
        /// </summary>
        [Fact]
        [public]
        [async]
        public async Task CreateReport_Success_Test()
        {
            // Authenticate the user using AuthenticateAsync()
            await AuthenticateAsync();

            // Setup a success response for the report API endpoint
            SetupReportSuccessResponse();

            // Create a test report text
            string reportText = "Test report content";

            // Define test location coordinates
            double latitude = 34.0522;
            double longitude = -118.2437;

            // Call ReportService.CreateReportAsync with the test data
            ReportModel report = await ReportService.CreateReportAsync(reportText, latitude, longitude);

            // Assert that the returned report is not null
            report.Should().NotBeNull();

            // Assert that the report text matches the input
            report.Text.Should().Be(reportText);

            // Assert that the report location matches the input
            report.Latitude.Should().Be(latitude);
            report.Longitude.Should().Be(longitude);

            // Assert that the report is marked as synced
            report.IsSynced.Should().BeTrue();

            // Verify that the API was called with the correct request data
            string requestBody = ApiServer.GetLastRequestBody("/reports");
            requestBody.Should().NotBeNullOrEmpty();

            var request = JsonConvert.DeserializeObject<ReportRequest>(requestBody);
            request.Should().NotBeNull();
            request.Text.Should().Be(reportText);
            request.Location.Latitude.Should().Be(latitude);
            request.Location.Longitude.Should().Be(longitude);
        }

        /// <summary>
        /// Tests that a report is created locally but not marked as synced when the API call fails.
        /// </summary>
        [Fact]
        [public]
        [async]
        public async Task CreateReport_ApiFailure_Test()
        {
            // Authenticate the user using AuthenticateAsync()
            await AuthenticateAsync();

            // Setup an error response for the report API endpoint
            SetupReportErrorResponse(500, "Internal Server Error");

            // Create a test report text
            string reportText = "Test report content";

            // Define test location coordinates
            double latitude = 34.0522;
            double longitude = -118.2437;

            // Call ReportService.CreateReportAsync with the test data
            ReportModel report = await ReportService.CreateReportAsync(reportText, latitude, longitude);

            // Assert that the returned report is not null
            report.Should().NotBeNull();

            // Assert that the report text matches the input
            report.Text.Should().Be(reportText);

            // Assert that the report location matches the input
            report.Latitude.Should().Be(latitude);
            report.Longitude.Should().Be(longitude);

            // Assert that the report is not marked as synced
            report.IsSynced.Should().BeFalse();

            // Verify that the API was called but returned an error
            ApiServer.GetRequestCount("/reports").Should().Be(1);
        }

        /// <summary>
        /// Tests that a report can be successfully retrieved by ID.
        /// </summary>
        [Fact]
        [public]
        [async]
        public async Task GetReport_Success_Test()
        {
            // Authenticate the user using AuthenticateAsync()
            await AuthenticateAsync();

            // Create a test report using ReportService.CreateReportAsync
            ReportModel createdReport = await ReportService.CreateReportAsync("Initial report text", 34.0522, -118.2437);

            // Get the ID of the created report
            int reportId = createdReport.Id;

            // Call ReportService.GetReportAsync with the report ID
            ReportModel retrievedReport = await ReportService.GetReportAsync(reportId);

            // Assert that the returned report is not null
            retrievedReport.Should().NotBeNull();

            // Assert that the report ID matches the expected ID
            retrievedReport.Id.Should().Be(reportId);

            // Assert that the report text matches the expected text
            retrievedReport.Text.Should().Be("Initial report text");
        }

        /// <summary>
        /// Tests that all reports can be successfully retrieved.
        /// </summary>
        [Fact]
        [public]
        [async]
        public async Task GetAllReports_Success_Test()
        {
            // Authenticate the user using AuthenticateAsync()
            await AuthenticateAsync();

            // Create multiple test reports using ReportService.CreateReportAsync
            await ReportService.CreateReportAsync("Report 1", 34.0522, -118.2437);
            await ReportService.CreateReportAsync("Report 2", 34.0522, -118.2437);
            await ReportService.CreateReportAsync("Report 3", 34.0522, -118.2437);

            // Call ReportService.GetAllReportsAsync
            IEnumerable<ReportModel> allReports = await ReportService.GetAllReportsAsync();

            // Assert that the returned collection is not null
            allReports.Should().NotBeNull();

            // Assert that the collection contains at least the number of reports created
            allReports.Count().Should().BeGreaterOrEqualTo(3);

            // Assert that the collection contains the created reports by matching IDs
            foreach (var report in allReports)
            {
                report.Should().NotBeNull();
            }
        }

        /// <summary>
        /// Tests that a report can be successfully updated and synchronized with the API.
        /// </summary>
        [Fact]
        [public]
        [async]
        public async Task UpdateReport_Success_Test()
        {
            // Authenticate the user using AuthenticateAsync()
            await AuthenticateAsync();

            // Create a test report using ReportService.CreateReportAsync
            ReportModel createdReport = await ReportService.CreateReportAsync("Original report text", 34.0522, -118.2437);

            // Setup a success response for the report update API endpoint
            SetupReportSuccessResponse();

            // Modify the report text
            string modifiedText = "Modified report text";
            createdReport.Text = modifiedText;

            // Call ReportService.UpdateReportAsync with the modified report
            bool updateResult = await ReportService.UpdateReportAsync(createdReport);

            // Assert that the update operation returns true
            updateResult.Should().BeTrue();

            // Retrieve the updated report using ReportService.GetReportAsync
            ReportModel updatedReport = await ReportService.GetReportAsync(createdReport.Id);

            // Assert that the report text matches the modified text
            updatedReport.Text.Should().Be(modifiedText);

            // Assert that the report is marked as synced
            updatedReport.IsSynced.Should().BeTrue();

            // Verify that the API was called with the correct request data
            ApiServer.GetRequestCount("/reports").Should().Be(2);
            string requestBody = ApiServer.GetLastRequestBody("/reports");
            requestBody.Should().NotBeNullOrEmpty();

            var request = JsonConvert.DeserializeObject<ReportRequest>(requestBody);
            request.Should().NotBeNull();
            request.Text.Should().Be(modifiedText);
        }

        /// <summary>
        /// Tests that a report is updated locally but not marked as synced when the API call fails.
        /// </summary>
        [Fact]
        [public]
        [async]
        public async Task UpdateReport_ApiFailure_Test()
        {
            // Authenticate the user using AuthenticateAsync()
            await AuthenticateAsync();

            // Create a test report using ReportService.CreateReportAsync
            ReportModel createdReport = await ReportService.CreateReportAsync("Original report text", 34.0522, -118.2437);

            // Setup an error response for the report update API endpoint
            SetupReportErrorResponse(500, "Internal Server Error");

            // Modify the report text
            string modifiedText = "Modified report text";
            createdReport.Text = modifiedText;

            // Call ReportService.UpdateReportAsync with the modified report
            bool updateResult = await ReportService.UpdateReportAsync(createdReport);

            // Assert that the update operation returns true (local update succeeds)
            updateResult.Should().BeTrue();

            // Retrieve the updated report using ReportService.GetReportAsync
            ReportModel updatedReport = await ReportService.GetReportAsync(createdReport.Id);

            // Assert that the report text matches the modified text
            updatedReport.Text.Should().Be(modifiedText);

            // Assert that the report is not marked as synced
            updatedReport.IsSynced.Should().BeFalse();

            // Verify that the API was called but returned an error
            ApiServer.GetRequestCount("/reports").Should().Be(2);
        }

        /// <summary>
        /// Tests that a report can be successfully deleted and the deletion synchronized with the API.
        /// </summary>
        [Fact]
        [public]
        [async]
        public async Task DeleteReport_Success_Test()
        {
            // Authenticate the user using AuthenticateAsync()
            await AuthenticateAsync();

            // Create a test report using ReportService.CreateReportAsync
            ReportModel createdReport = await ReportService.CreateReportAsync("Report to delete", 34.0522, -118.2437);

            // Get the ID of the created report
            int reportId = createdReport.Id;

            // Setup a success response for the report deletion API endpoint
            SetupReportSuccessResponse();

            // Call ReportService.DeleteReportAsync with the report ID
            bool deleteResult = await ReportService.DeleteReportAsync(reportId);

            // Assert that the delete operation returns true
            deleteResult.Should().BeTrue();

            // Call ReportService.GetReportAsync with the deleted report ID
            ReportModel deletedReport = await ReportService.GetReportAsync(reportId);

            // Assert that the returned report is null (deleted)
            deletedReport.Should().BeNull();

            // Verify that the API was called with the correct request data
            ApiServer.GetRequestCount("/reports").Should().Be(2);
        }

        /// <summary>
        /// Tests that a report is deleted locally even when the API call for deletion fails.
        /// </summary>
        [Fact]
        [public]
        [async]
        public async Task DeleteReport_ApiFailure_Test()
        {
            // Authenticate the user using AuthenticateAsync()
            await AuthenticateAsync();

            // Create a test report using ReportService.CreateReportAsync
            ReportModel createdReport = await ReportService.CreateReportAsync("Report to delete", 34.0522, -118.2437);

            // Get the ID of the created report
            int reportId = createdReport.Id;

            // Setup an error response for the report deletion API endpoint
            SetupReportErrorResponse(500, "Internal Server Error");

            // Call ReportService.DeleteReportAsync with the report ID
            bool deleteResult = await ReportService.DeleteReportAsync(reportId);

            // Assert that the delete operation returns true (local deletion succeeds)
            deleteResult.Should().BeTrue();

            // Call ReportService.GetReportAsync with the deleted report ID
            ReportModel deletedReport = await ReportService.GetReportAsync(reportId);

            // Assert that the returned report is null (deleted locally)
            deletedReport.Should().BeNull();

            // Verify that the API was called but returned an error
            ApiServer.GetRequestCount("/reports").Should().Be(2);
        }

        /// <summary>
        /// Tests that an unsynchronized report can be successfully synchronized with the API.
        /// </summary>
        [Fact]
        [public]
        [async]
        public async Task SyncReport_Success_Test()
        {
            // Authenticate the user using AuthenticateAsync()
            await AuthenticateAsync();

            // Setup an error response for the initial report creation API endpoint
            SetupReportErrorResponse(500, "Initial sync failure");

            // Create a test report that will fail to sync initially
            ReportModel createdReport = await ReportService.CreateReportAsync("Report to sync", 34.0522, -118.2437);

            // Assert that the report is not marked as synced
            createdReport.IsSynced.Should().BeFalse();

            // Setup a success response for the report sync API endpoint
            SetupReportSuccessResponse();

            // Call ReportService.SyncReportAsync with the report ID
            bool syncResult = await ReportService.SyncReportAsync(createdReport.Id);

            // Assert that the sync operation returns true
            syncResult.Should().BeTrue();

            // Retrieve the report using ReportService.GetReportAsync
            ReportModel syncedReport = await ReportService.GetReportAsync(createdReport.Id);

            // Assert that the report is now marked as synced
            syncedReport.IsSynced.Should().BeTrue();

            // Verify that the API was called with the correct request data
            ApiServer.GetRequestCount("/reports").Should().Be(2);
        }

        /// <summary>
        /// Tests that all unsynchronized reports can be successfully synchronized with the API.
        /// </summary>
        [Fact]
        [public]
        [async]
        public async Task SyncAllReports_Success_Test()
        {
            // Authenticate the user using AuthenticateAsync()
            await AuthenticateAsync();

            // Setup an error response for the initial report creation API endpoint
            SetupReportErrorResponse(500, "Initial sync failure");

            // Create multiple test reports that will fail to sync initially
            await ReportService.CreateReportAsync("Report 1", 34.0522, -118.2437);
            await ReportService.CreateReportAsync("Report 2", 34.0522, -118.2437);
            await ReportService.CreateReportAsync("Report 3", 34.0522, -118.2437);

            // Setup a success response for the report sync API endpoint
            SetupReportSuccessResponse();

            // Call ReportService.SyncAllReportsAsync
            int syncedCount = await ReportService.SyncAllReportsAsync();

            // Assert that the sync operation returns the correct number of synced reports
            syncedCount.Should().Be(3);

            // Retrieve all reports using ReportService.GetAllReportsAsync
            IEnumerable<ReportModel> allReports = await ReportService.GetAllReportsAsync();

            // Assert that all the test reports are now marked as synced
            foreach (var report in allReports)
            {
                report.IsSynced.Should().BeTrue();
            }

            // Verify that the API was called for each report with the correct request data
            ApiServer.GetRequestCount("/reports").Should().Be(6);
        }

        /// <summary>
        /// Sets up a successful response for the report API endpoint.
        /// </summary>
        [private]
        public void SetupReportSuccessResponse()
        {
            // Create a ReportResponse with a success status
            var reportResponse = new ReportResponse
            {
                Id = Guid.NewGuid().ToString(),
                Status = "success"
            };

            // Configure ApiServer to return the success response for the reports endpoint
            ApiServer.SetupSuccessResponse("/reports", reportResponse);
        }

        /// <summary>
        /// Sets up an error response for the report API endpoint.
        /// </summary>
        /// <param name="statusCode">The HTTP status code to return</param>
        /// <param name="errorMessage">The error message to return</param>
        [private]
        public void SetupReportErrorResponse(int statusCode, string errorMessage)
        {
            // Configure ApiServer to return an error response with the specified status code and message for the reports endpoint
            ApiServer.SetupErrorResponse("/reports", statusCode, errorMessage);
        }
    }
}