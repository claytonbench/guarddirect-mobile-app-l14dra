using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SecurityPatrol.API.IntegrationTests.Setup;
using SecurityPatrol.Core.Models;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.TestCommon.Constants;

namespace SecurityPatrol.API.IntegrationTests.API
{
    /// <summary>
    /// Integration tests for the report submission flow in the Security Patrol API, testing the end-to-end 
    /// functionality of creating, retrieving, updating, and deleting activity reports.
    /// </summary>
    public class ReportSubmissionFlowTests : IntegrationTestBase
    {
        /// <summary>
        /// Initializes a new instance of the ReportSubmissionFlowTests class.
        /// </summary>
        public ReportSubmissionFlowTests(CustomWebApplicationFactory factory) : base(factory)
        {
            // Authenticate the HTTP client with a test token for the tests
            AuthenticateClient(TestConstants.TestAuthToken);
        }

        /// <summary>
        /// Tests that a report can be successfully created through the API.
        /// </summary>
        [Fact]
        public async Task CanCreateReport()
        {
            // Arrange
            var request = new ReportRequest
            {
                Text = TestConstants.TestReportText,
                Timestamp = DateTime.UtcNow,
                Location = new LocationData
                {
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                }
            };

            // Act
            var response = await PostAsync<ReportRequest, ReportResponse>("/api/reports", request);

            // Assert
            response.Should().NotBeNull();
            response.Id.Should().NotBeNullOrEmpty();
            response.Status.Should().Be("Success");
        }

        /// <summary>
        /// Tests that reports can be successfully retrieved through the API.
        /// </summary>
        [Fact]
        public async Task CanRetrieveReports()
        {
            // Arrange - Create a test report to ensure there's at least one to retrieve
            await CreateTestReport();

            // Act
            var reports = await GetAsync<List<Report>>("/api/reports");

            // Assert
            reports.Should().NotBeNull();
            reports.Should().NotBeEmpty();
            reports.Should().Contain(r => r.Text == TestConstants.TestReportText);
        }

        /// <summary>
        /// Tests that a specific report can be retrieved by ID through the API.
        /// </summary>
        [Fact]
        public async Task CanRetrieveReportById()
        {
            // Arrange - Create a test report and extract its ID
            var testReport = await CreateTestReport();
            var reportId = testReport.Id;

            // Act
            var report = await GetAsync<Report>($"/api/reports/{reportId}");

            // Assert
            report.Should().NotBeNull();
            report.Id.ToString().Should().Be(reportId);
            report.Text.Should().Be(TestConstants.TestReportText);
            report.Latitude.Should().Be(TestConstants.TestLatitude);
            report.Longitude.Should().Be(TestConstants.TestLongitude);
        }

        /// <summary>
        /// Tests that a report can be successfully updated through the API.
        /// </summary>
        [Fact]
        public async Task CanUpdateReport()
        {
            // Arrange - Create a test report and prepare update data
            var testReport = await CreateTestReport();
            var reportId = testReport.Id;
            var updatedText = $"{TestConstants.TestReportText} - Updated";
            
            var updateRequest = new ReportRequest
            {
                Text = updatedText,
                Timestamp = DateTime.UtcNow,
                Location = new LocationData
                {
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                }
            };

            // Act
            var updateResponse = await PutAsync<ReportRequest, ReportResponse>($"/api/reports/{reportId}", updateRequest);
            var updatedReport = await GetAsync<Report>($"/api/reports/{reportId}");

            // Assert
            updateResponse.Should().NotBeNull();
            updateResponse.Status.Should().Be("Success");
            
            updatedReport.Should().NotBeNull();
            updatedReport.Text.Should().Be(updatedText);
        }

        /// <summary>
        /// Tests that a report can be successfully deleted through the API.
        /// </summary>
        [Fact]
        public async Task CanDeleteReport()
        {
            // Arrange - Create a test report and extract its ID
            var testReport = await CreateTestReport();
            var reportId = testReport.Id;

            // Act
            var deleteResult = await DeleteAsync<Result>($"/api/reports/{reportId}");
            
            // Assert
            deleteResult.Should().NotBeNull();
            deleteResult.Succeeded.Should().BeTrue();
            
            // Try to get the report after deletion - should result in 404
            Func<Task> getAfterDelete = async () => await GetAsync<Report>($"/api/reports/{reportId}");
            await getAfterDelete.Should().ThrowAsync<Exception>().Where(e => e.Message.Contains("404"));
        }

        /// <summary>
        /// Tests that report creation fails with appropriate error when invalid data is provided.
        /// </summary>
        [Fact]
        public async Task CannotCreateReportWithInvalidData()
        {
            // Arrange - Create a request with invalid data (empty text)
            var invalidRequest = new ReportRequest
            {
                Text = "", // Empty text should be invalid
                Timestamp = DateTime.UtcNow,
                Location = new LocationData
                {
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                }
            };

            // Act & Assert
            Func<Task> postInvalidRequest = async () => 
                await PostAsync<ReportRequest, ReportResponse>("/api/reports", invalidRequest);
            
            await postInvalidRequest.Should().ThrowAsync<Exception>()
                .Where(e => e.Message.Contains("400") && e.Message.Contains("text"));
        }

        /// <summary>
        /// Tests that reports can be filtered by date range through the API.
        /// </summary>
        [Fact]
        public async Task CanRetrieveReportsByDateRange()
        {
            // Arrange - Create test reports with different timestamps
            var oldReportRequest = new ReportRequest
            {
                Text = $"{TestConstants.TestReportText} - Old",
                Timestamp = DateTime.UtcNow.AddDays(-10),
                Location = new LocationData
                {
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                }
            };
            
            var newReportRequest = new ReportRequest
            {
                Text = $"{TestConstants.TestReportText} - New",
                Timestamp = DateTime.UtcNow,
                Location = new LocationData
                {
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                }
            };
            
            await PostAsync<ReportRequest, ReportResponse>("/api/reports", oldReportRequest);
            await PostAsync<ReportRequest, ReportResponse>("/api/reports", newReportRequest);
            
            // Define a date range that includes only the new report
            var startDate = DateTime.UtcNow.AddDays(-5).ToString("yyyy-MM-dd");
            var endDate = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd");

            // Act
            var reports = await GetAsync<List<Report>>($"/api/reports/range?startDate={startDate}&endDate={endDate}");

            // Assert
            reports.Should().NotBeNull();
            reports.Should().Contain(r => r.Text.Contains("New"));
            reports.Should().NotContain(r => r.Text.Contains("Old"));
        }

        /// <summary>
        /// Helper method to create a test report for use in other tests.
        /// </summary>
        /// <returns>The response from creating the test report.</returns>
        private async Task<ReportResponse> CreateTestReport()
        {
            var request = new ReportRequest
            {
                Text = TestConstants.TestReportText,
                Timestamp = DateTime.UtcNow,
                Location = new LocationData
                {
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                }
            };

            return await PostAsync<ReportRequest, ReportResponse>("/api/reports", request);
        }
    }
}