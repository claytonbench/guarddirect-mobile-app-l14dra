using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xunit;
using FluentAssertions;
using SecurityPatrol.API.IntegrationTests.Setup;
using SecurityPatrol.Core.Models;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.TestCommon.Constants;

namespace SecurityPatrol.API.IntegrationTests.Controllers
{
    /// <summary>
    /// Integration tests for the ReportController in the Security Patrol API, verifying the activity reporting functionality using real HTTP requests.
    /// </summary>
    public class ReportControllerIntegrationTests : IntegrationTestBase
    {
        /// <summary>
        /// Initializes a new instance of the ReportControllerIntegrationTests class with the test factory.
        /// </summary>
        /// <param name="factory">The factory that creates a test server with in-memory database.</param>
        public ReportControllerIntegrationTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task CreateReport_WithValidData_ReturnsSuccess()
        {
            // Arrange
            AuthenticateClient();
            
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
            var response = await Client.PostAsJsonAsync("/api/reports", request);
            
            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<Result<ReportResponse>>(JsonOptions);
            
            result.Should().NotBeNull();
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Id.Should().NotBeEmpty();
        }
        
        [Fact]
        public async Task CreateReport_WithEmptyText_ReturnsBadRequest()
        {
            // Arrange
            AuthenticateClient();
            
            var request = new ReportRequest
            {
                Text = "",
                Timestamp = DateTime.UtcNow,
                Location = new LocationData
                {
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                }
            };
            
            // Act
            var response = await Client.PostAsJsonAsync("/api/reports", request);
            
            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
        
        [Fact]
        public async Task CreateReport_WithTooLongText_ReturnsBadRequest()
        {
            // Arrange
            AuthenticateClient();
            
            var request = new ReportRequest
            {
                Text = new string('A', 501), // Create a string that's 501 characters (over the 500 limit)
                Timestamp = DateTime.UtcNow,
                Location = new LocationData
                {
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                }
            };
            
            // Act
            var response = await Client.PostAsJsonAsync("/api/reports", request);
            
            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
        
        [Fact]
        public async Task CreateReport_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Arrange - deliberately not authenticating
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
            var response = await Client.PostAsJsonAsync("/api/reports", request);
            
            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
        
        [Fact]
        public async Task GetReports_WithValidPagination_ReturnsReports()
        {
            // Arrange
            AuthenticateClient();
            
            // Act
            var response = await Client.GetAsync("/api/reports?pageNumber=1&pageSize=10");
            
            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<PaginatedList<Report>>(JsonOptions);
            
            result.Should().NotBeNull();
            result.Items.Should().NotBeNull();
            result.PageNumber.Should().Be(1);
            result.TotalPages.Should().BeGreaterOrEqualTo(1);
        }
        
        [Fact]
        public async Task GetReports_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Act - deliberately not authenticating
            var response = await Client.GetAsync("/api/reports");
            
            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
        
        [Fact]
        public async Task GetReportById_WithValidId_ReturnsReport()
        {
            // Arrange
            AuthenticateClient();
            
            // First create a report to get a valid ID
            var createRequest = new ReportRequest
            {
                Text = TestConstants.TestReportText,
                Timestamp = DateTime.UtcNow,
                Location = new LocationData
                {
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                }
            };
            
            var createResponse = await Client.PostAsJsonAsync("/api/reports", createRequest);
            createResponse.EnsureSuccessStatusCode();
            var createResult = await createResponse.Content.ReadFromJsonAsync<Result<ReportResponse>>(JsonOptions);
            var reportId = createResult.Data.Id;
            
            // Act
            var response = await Client.GetAsync($"/api/reports/{reportId}");
            
            // Assert
            response.EnsureSuccessStatusCode();
            var report = await response.Content.ReadFromJsonAsync<Report>(JsonOptions);
            
            report.Should().NotBeNull();
            report.Id.ToString().Should().Be(reportId);
            report.Text.Should().Be(TestConstants.TestReportText);
        }
        
        [Fact]
        public async Task GetReportById_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            AuthenticateClient();
            
            // Act
            var response = await Client.GetAsync("/api/reports/999999"); // Using a non-existent ID
            
            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        
        [Fact]
        public async Task GetReportById_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Act - deliberately not authenticating
            var response = await Client.GetAsync("/api/reports/1");
            
            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
        
        [Fact]
        public async Task GetReportsByDateRange_WithValidRange_ReturnsReports()
        {
            // Arrange
            AuthenticateClient();
            var startDate = DateTime.UtcNow.AddDays(-7).ToString("o");
            var endDate = DateTime.UtcNow.ToString("o");
            
            // Act
            var response = await Client.GetAsync($"/api/reports/range?startDate={startDate}&endDate={endDate}");
            
            // Assert
            response.EnsureSuccessStatusCode();
            var reports = await response.Content.ReadFromJsonAsync<List<Report>>(JsonOptions);
            
            reports.Should().NotBeNull();
            // All reports should have timestamps within the specified range
            reports.Should().OnlyContain(r => r.Timestamp >= DateTime.Parse(startDate) && r.Timestamp <= DateTime.Parse(endDate));
        }
        
        [Fact]
        public async Task UpdateReport_WithValidData_ReturnsSuccess()
        {
            // Arrange
            AuthenticateClient();
            
            // First create a report to get a valid ID
            var createRequest = new ReportRequest
            {
                Text = TestConstants.TestReportText,
                Timestamp = DateTime.UtcNow,
                Location = new LocationData
                {
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                }
            };
            
            var createResponse = await Client.PostAsJsonAsync("/api/reports", createRequest);
            createResponse.EnsureSuccessStatusCode();
            var createResult = await createResponse.Content.ReadFromJsonAsync<Result<ReportResponse>>(JsonOptions);
            var reportId = createResult.Data.Id;
            
            // Create update request
            var updateRequest = new ReportRequest
            {
                Text = "Updated report text",
                Timestamp = DateTime.UtcNow,
                Location = new LocationData
                {
                    Latitude = TestConstants.TestLatitude + 0.001,
                    Longitude = TestConstants.TestLongitude + 0.001
                }
            };
            
            // Act
            var response = await Client.PutAsJsonAsync($"/api/reports/{reportId}", updateRequest);
            
            // Assert
            response.EnsureSuccessStatusCode();
            
            // Verify the update
            var getResponse = await Client.GetAsync($"/api/reports/{reportId}");
            getResponse.EnsureSuccessStatusCode();
            var updatedReport = await getResponse.Content.ReadFromJsonAsync<Report>(JsonOptions);
            
            updatedReport.Should().NotBeNull();
            updatedReport.Text.Should().Be("Updated report text");
        }
        
        [Fact]
        public async Task UpdateReport_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            AuthenticateClient();
            
            var updateRequest = new ReportRequest
            {
                Text = "Updated report text",
                Timestamp = DateTime.UtcNow,
                Location = new LocationData
                {
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                }
            };
            
            // Act
            var response = await Client.PutAsJsonAsync("/api/reports/999999", updateRequest);
            
            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        
        [Fact]
        public async Task DeleteReport_WithValidId_ReturnsSuccess()
        {
            // Arrange
            AuthenticateClient();
            
            // First create a report to get a valid ID
            var createRequest = new ReportRequest
            {
                Text = TestConstants.TestReportText,
                Timestamp = DateTime.UtcNow,
                Location = new LocationData
                {
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                }
            };
            
            var createResponse = await Client.PostAsJsonAsync("/api/reports", createRequest);
            createResponse.EnsureSuccessStatusCode();
            var createResult = await createResponse.Content.ReadFromJsonAsync<Result<ReportResponse>>(JsonOptions);
            var reportId = createResult.Data.Id;
            
            // Act
            var response = await Client.DeleteAsync($"/api/reports/{reportId}");
            
            // Assert
            response.EnsureSuccessStatusCode();
            
            // Verify the deletion
            var getResponse = await Client.GetAsync($"/api/reports/{reportId}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        
        [Fact]
        public async Task DeleteReport_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            AuthenticateClient();
            
            // Act
            var response = await Client.DeleteAsync("/api/reports/999999");
            
            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        
        [Fact]
        public async Task CompleteReportFlow_Success()
        {
            // Arrange
            AuthenticateClient();
            
            // Create a report
            var createRequest = new ReportRequest
            {
                Text = TestConstants.TestReportText,
                Timestamp = DateTime.UtcNow,
                Location = new LocationData
                {
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                }
            };
            
            // Act - Create
            var createResponse = await Client.PostAsJsonAsync("/api/reports", createRequest);
            createResponse.EnsureSuccessStatusCode();
            var createResult = await createResponse.Content.ReadFromJsonAsync<Result<ReportResponse>>(JsonOptions);
            var reportId = createResult.Data.Id;
            
            // Act - Get
            var getResponse = await Client.GetAsync($"/api/reports/{reportId}");
            getResponse.EnsureSuccessStatusCode();
            var report = await getResponse.Content.ReadFromJsonAsync<Report>(JsonOptions);
            report.Should().NotBeNull();
            report.Text.Should().Be(TestConstants.TestReportText);
            
            // Act - Update
            var updateRequest = new ReportRequest
            {
                Text = "Updated report text",
                Timestamp = DateTime.UtcNow,
                Location = new LocationData
                {
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                }
            };
            
            var updateResponse = await Client.PutAsJsonAsync($"/api/reports/{reportId}", updateRequest);
            updateResponse.EnsureSuccessStatusCode();
            
            // Verify update
            var getUpdatedResponse = await Client.GetAsync($"/api/reports/{reportId}");
            getUpdatedResponse.EnsureSuccessStatusCode();
            var updatedReport = await getUpdatedResponse.Content.ReadFromJsonAsync<Report>(JsonOptions);
            updatedReport.Should().NotBeNull();
            updatedReport.Text.Should().Be("Updated report text");
            
            // Act - Delete
            var deleteResponse = await Client.DeleteAsync($"/api/reports/{reportId}");
            deleteResponse.EnsureSuccessStatusCode();
            
            // Verify deletion
            var getFinalResponse = await Client.GetAsync($"/api/reports/{reportId}");
            getFinalResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
            
            // Assert - Full flow completed successfully
            createResponse.IsSuccessStatusCode.Should().BeTrue();
            getResponse.IsSuccessStatusCode.Should().BeTrue();
            updateResponse.IsSuccessStatusCode.Should().BeTrue();
            deleteResponse.IsSuccessStatusCode.Should().BeTrue();
            getFinalResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}