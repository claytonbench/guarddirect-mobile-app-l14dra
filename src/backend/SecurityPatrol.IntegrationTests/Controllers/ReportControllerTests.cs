using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Core.Models;
using SecurityPatrol.IntegrationTests.Helpers;
using Xunit;

namespace SecurityPatrol.IntegrationTests.Controllers
{
    /// <summary>
    /// Integration tests for the ReportController in the Security Patrol application
    /// </summary>
    public class ReportControllerTests : TestBase
    {
        private readonly CustomWebApplicationFactory Factory;

        /// <summary>
        /// Initializes a new instance of the ReportControllerTests class with the test factory
        /// </summary>
        /// <param name="factory">The custom web application factory for creating test servers</param>
        public ReportControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
            Factory = factory;
        }

        [Fact]
        public async Task CreateReport_WithValidRequest_ReturnsOk()
        {
            // Arrange
            Factory.ResetDatabase();
            await AuthenticateAsync();

            var request = CreateValidReportRequest();

            // Act
            var response = await Client.PostAsync("/api/reports", CreateJsonContent(request));

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ReportResponse>();
            result.Should().NotBeNull();
            result.Id.Should().NotBeNullOrEmpty();
            result.Status.Should().Be("Success");
        }

        [Fact]
        public async Task CreateReport_WithEmptyText_ReturnsBadRequest()
        {
            // Arrange
            await AuthenticateAsync();

            var request = new ReportRequest
            {
                Text = "",
                Timestamp = DateTime.UtcNow,
                Location = new LocationData
                {
                    Latitude = 37.7749,
                    Longitude = -122.4194
                }
            };

            // Act
            var response = await Client.PostAsync("/api/reports", CreateJsonContent(request));

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CreateReport_WithTextExceedingMaxLength_ReturnsBadRequest()
        {
            // Arrange
            await AuthenticateAsync();

            var request = new ReportRequest
            {
                Text = new string('A', 501), // 501 characters (exceeds 500 max)
                Timestamp = DateTime.UtcNow,
                Location = new LocationData
                {
                    Latitude = 37.7749,
                    Longitude = -122.4194
                }
            };

            // Act
            var response = await Client.PostAsync("/api/reports", CreateJsonContent(request));

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CreateReport_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Arrange
            var request = CreateValidReportRequest();

            // Act - Don't authenticate before calling the API
            var response = await Client.PostAsync("/api/reports", CreateJsonContent(request));

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetReports_WithValidRequest_ReturnsOkWithReports()
        {
            // Arrange
            Factory.ResetDatabase();
            await AuthenticateAsync();

            // Create multiple reports
            for (int i = 0; i < 3; i++)
            {
                var request = CreateValidReportRequest();
                await Client.PostAsync("/api/reports", CreateJsonContent(request));
            }

            // Act
            var response = await Client.GetAsync("/api/reports");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var reports = await response.Content.ReadFromJsonAsync<List<Report>>();
            reports.Should().NotBeEmpty();
            foreach (var report in reports)
            {
                report.UserId.Should().Be(TestUserId);
            }
        }

        [Fact]
        public async Task GetReports_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            Factory.ResetDatabase();
            await AuthenticateAsync();

            // Create 10 reports
            for (int i = 0; i < 10; i++)
            {
                var request = CreateValidReportRequest();
                request.Text = $"Report {i + 1}";
                await Client.PostAsync("/api/reports", CreateJsonContent(request));
            }

            // Act - Get second page with 5 items per page
            var response = await Client.GetAsync("/api/reports?pageNumber=2&pageSize=5");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var paginatedResult = await response.Content.ReadFromJsonAsync<PaginatedList<Report>>();
            paginatedResult.Should().NotBeNull();
            paginatedResult.PageNumber.Should().Be(2);
            paginatedResult.Items.Count.Should().BeLessThanOrEqualTo(5);
            foreach (var report in paginatedResult.Items)
            {
                report.UserId.Should().Be(TestUserId);
            }
        }

        [Fact]
        public async Task GetReports_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Act - Don't authenticate before calling the API
            var response = await Client.GetAsync("/api/reports");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetReportById_WithValidId_ReturnsOkWithReport()
        {
            // Arrange
            Factory.ResetDatabase();
            await AuthenticateAsync();

            // Create a report and extract its ID
            var createRequest = CreateValidReportRequest();
            var createResponse = await Client.PostAsync("/api/reports", CreateJsonContent(createRequest));
            var createResult = await createResponse.Content.ReadFromJsonAsync<ReportResponse>();
            var reportId = createResult.Id;

            // Act
            var response = await Client.GetAsync($"/api/reports/{reportId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var report = await response.Content.ReadFromJsonAsync<Report>();
            report.Should().NotBeNull();
            report.Id.ToString().Should().Be(reportId);
            report.UserId.Should().Be(TestUserId);
        }

        [Fact]
        public async Task GetReportById_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            await AuthenticateAsync();

            // Act - Use a non-existent ID
            var response = await Client.GetAsync("/api/reports/9999");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetReportById_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Arrange
            Factory.ResetDatabase();
            await AuthenticateAsync();

            // Create a report
            var createRequest = CreateValidReportRequest();
            var createResponse = await Client.PostAsync("/api/reports", CreateJsonContent(createRequest));
            var createResult = await createResponse.Content.ReadFromJsonAsync<ReportResponse>();
            var reportId = createResult.Id;

            // Clear authentication
            Client.DefaultRequestHeaders.Authorization = null;

            // Act - Try to get the report without authentication
            var response = await Client.GetAsync($"/api/reports/{reportId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetReportsByDateRange_WithValidRange_ReturnsOkWithReports()
        {
            // Arrange
            Factory.ResetDatabase();
            await AuthenticateAsync();

            // Create reports with different timestamps
            var now = DateTime.UtcNow;
            var timestamps = new List<DateTime>
            {
                now.AddDays(-5),
                now.AddDays(-3),
                now.AddDays(-1)
            };

            foreach (var timestamp in timestamps)
            {
                var request = CreateValidReportRequest();
                request.Timestamp = timestamp;
                await Client.PostAsync("/api/reports", CreateJsonContent(request));
            }

            // Act - Get reports within a date range that includes all created reports
            var startDate = now.AddDays(-6).ToString("o");
            var endDate = now.ToString("o");
            var response = await Client.GetAsync($"/api/reports/range?startDate={startDate}&endDate={endDate}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var reports = await response.Content.ReadFromJsonAsync<List<Report>>();
            reports.Should().NotBeEmpty();
            reports.Count.Should().Be(3);
            foreach (var report in reports)
            {
                report.UserId.Should().Be(TestUserId);
                report.Timestamp.Should().BeOnOrAfter(now.AddDays(-6));
                report.Timestamp.Should().BeOnOrBefore(now);
            }
        }

        [Fact]
        public async Task GetReportsByDateRange_WithInvalidRange_ReturnsBadRequest()
        {
            // Arrange
            await AuthenticateAsync();

            // Act - End date is before start date
            var startDate = DateTime.UtcNow.ToString("o");
            var endDate = DateTime.UtcNow.AddDays(-1).ToString("o");
            var response = await Client.GetAsync($"/api/reports/range?startDate={startDate}&endDate={endDate}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task UpdateReport_WithValidRequest_ReturnsOk()
        {
            // Arrange
            Factory.ResetDatabase();
            await AuthenticateAsync();

            // Create a report
            var createRequest = CreateValidReportRequest();
            var createResponse = await Client.PostAsync("/api/reports", CreateJsonContent(createRequest));
            var createResult = await createResponse.Content.ReadFromJsonAsync<ReportResponse>();
            var reportId = createResult.Id;

            // Prepare update request
            var updateRequest = CreateValidReportRequest();
            updateRequest.Text = "Updated report text";

            // Act
            var response = await Client.PutAsync($"/api/reports/{reportId}", CreateJsonContent(updateRequest));

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify the report was updated
            var getResponse = await Client.GetAsync($"/api/reports/{reportId}");
            var updatedReport = await getResponse.Content.ReadFromJsonAsync<Report>();
            updatedReport.Text.Should().Be("Updated report text");
        }

        [Fact]
        public async Task UpdateReport_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            await AuthenticateAsync();
            var updateRequest = CreateValidReportRequest();

            // Act - Use a non-existent ID
            var response = await Client.PutAsync("/api/reports/9999", CreateJsonContent(updateRequest));

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task UpdateReport_WithInvalidRequest_ReturnsBadRequest()
        {
            // Arrange
            Factory.ResetDatabase();
            await AuthenticateAsync();

            // Create a report
            var createRequest = CreateValidReportRequest();
            var createResponse = await Client.PostAsync("/api/reports", CreateJsonContent(createRequest));
            var createResult = await createResponse.Content.ReadFromJsonAsync<ReportResponse>();
            var reportId = createResult.Id;

            // Prepare invalid update request (empty text)
            var updateRequest = new ReportRequest
            {
                Text = "",
                Timestamp = DateTime.UtcNow,
                Location = new LocationData
                {
                    Latitude = 37.7749,
                    Longitude = -122.4194
                }
            };

            // Act
            var response = await Client.PutAsync($"/api/reports/{reportId}", CreateJsonContent(updateRequest));

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task DeleteReport_WithValidId_ReturnsOk()
        {
            // Arrange
            Factory.ResetDatabase();
            await AuthenticateAsync();

            // Create a report
            var createRequest = CreateValidReportRequest();
            var createResponse = await Client.PostAsync("/api/reports", CreateJsonContent(createRequest));
            var createResult = await createResponse.Content.ReadFromJsonAsync<ReportResponse>();
            var reportId = createResult.Id;

            // Act
            var response = await Client.DeleteAsync($"/api/reports/{reportId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify the report is deleted
            var getResponse = await Client.GetAsync($"/api/reports/{reportId}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task DeleteReport_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            await AuthenticateAsync();

            // Act - Use a non-existent ID
            var response = await Client.DeleteAsync("/api/reports/9999");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task DeleteReport_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Arrange
            Factory.ResetDatabase();
            await AuthenticateAsync();

            // Create a report
            var createRequest = CreateValidReportRequest();
            var createResponse = await Client.PostAsync("/api/reports", CreateJsonContent(createRequest));
            var createResult = await createResponse.Content.ReadFromJsonAsync<ReportResponse>();
            var reportId = createResult.Id;

            // Clear authentication
            Client.DefaultRequestHeaders.Authorization = null;

            // Act - Try to delete the report without authentication
            var response = await Client.DeleteAsync($"/api/reports/{reportId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        /// <summary>
        /// Helper method to authenticate the HTTP client for tests
        /// </summary>
        private async Task AuthenticateAsync()
        {
            // In a real test, we would send an authentication request
            // But for our integration tests, we're using the TestAuthHandler
            // which automatically authenticates with the TestUserId
            
            // Set the authentication token directly using the helper from base class
            SetAuthToken("test-token");
        }

        /// <summary>
        /// Helper method to create a valid report request for tests
        /// </summary>
        private ReportRequest CreateValidReportRequest()
        {
            return new ReportRequest
            {
                Text = "Test report with valid content",
                Timestamp = DateTime.UtcNow,
                Location = new LocationData
                {
                    Latitude = 37.7749,
                    Longitude = -122.4194
                }
            };
        }
    }
}