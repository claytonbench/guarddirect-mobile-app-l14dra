using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using Xunit;
using SecurityPatrol.Core.Models;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Infrastructure.Persistence;

namespace SecurityPatrol.IntegrationTests.Controllers
{
    /// <summary>
    /// Integration tests for the TimeController API endpoints in the Security Patrol application.
    /// Tests clock in/out functionality, time record history retrieval, and status checking against 
    /// a test server with in-memory database.
    /// </summary>
    public class TimeControllerTests : TestBase
    {
        /// <summary>
        /// Factory for creating the test server with in-memory database
        /// </summary>
        public CustomWebApplicationFactory Factory { get; }

        /// <summary>
        /// Initializes a new instance of the TimeControllerTests class with the test factory.
        /// </summary>
        /// <param name="factory">Factory for creating test server with in-memory database</param>
        public TimeControllerTests(CustomWebApplicationFactory factory)
            : base(factory)
        {
            Factory = factory;
            SetAuthToken("test-token"); // Set authentication token for test requests
        }

        /// <summary>
        /// Tests that the Clock endpoint successfully creates a clock-in record.
        /// </summary>
        [Fact]
        public async Task ClockIn_ShouldCreateTimeRecord()
        {
            // Arrange
            var request = new TimeRecordRequest
            {
                Type = "in",
                Timestamp = DateTime.UtcNow,
                Location = new LocationModel
                {
                    Latitude = 37.7749,
                    Longitude = -122.4194
                }
            };

            // Act
            var response = await PostAsync<TimeRecordRequest, TimeRecordResponse>("/api/time", request);

            // Assert
            response.Should().NotBeNull();
            response.Id.Should().NotBeEmpty();
            response.Status.Should().Be("success");

            // Verify record was created in the database
            using var context = Factory.CreateDbContext();
            var record = await context.TimeRecords
                .FirstOrDefaultAsync(t => t.UserId == TestUserId && t.Type == "in");

            record.Should().NotBeNull();
            record.Type.Should().Be("in");
        }

        /// <summary>
        /// Tests that the Clock endpoint successfully creates a clock-out record.
        /// </summary>
        [Fact]
        public async Task ClockOut_ShouldCreateTimeRecord()
        {
            // Arrange
            var request = new TimeRecordRequest
            {
                Type = "out",
                Timestamp = DateTime.UtcNow,
                Location = new LocationModel
                {
                    Latitude = 37.7749,
                    Longitude = -122.4194
                }
            };

            // Act
            var response = await PostAsync<TimeRecordRequest, TimeRecordResponse>("/api/time", request);

            // Assert
            response.Should().NotBeNull();
            response.Id.Should().NotBeEmpty();
            response.Status.Should().Be("success");

            // Verify record was created in the database
            using var context = Factory.CreateDbContext();
            var record = await context.TimeRecords
                .FirstOrDefaultAsync(t => t.UserId == TestUserId && t.Type == "out");

            record.Should().NotBeNull();
            record.Type.Should().Be("out");
        }

        /// <summary>
        /// Tests that the GetHistory endpoint returns time records for the authenticated user.
        /// </summary>
        [Fact]
        public async Task GetHistory_ShouldReturnTimeRecords()
        {
            // Arrange
            AddTestTimeRecords(5);

            // Act
            var response = await GetAsync<List<TimeRecord>>("/api/time/history");

            // Assert
            response.Should().NotBeNull();
            response.Should().NotBeEmpty();
            response.Should().AllSatisfy(r => r.UserId.Should().Be(TestUserId));
        }

        /// <summary>
        /// Tests that the GetHistory endpoint returns paginated results when page parameters are provided.
        /// </summary>
        [Fact]
        public async Task GetHistory_WithPagination_ShouldReturnPagedResults()
        {
            // Arrange
            AddTestTimeRecords(10);

            // Act
            var page1 = await GetAsync<List<TimeRecord>>("/api/time/history?pageNumber=1&pageSize=2");
            var page2 = await GetAsync<List<TimeRecord>>("/api/time/history?pageNumber=2&pageSize=2");

            // Assert
            page1.Should().NotBeNull();
            page1.Should().HaveCount(2);
            
            page2.Should().NotBeNull();
            page2.Should().HaveCount(2);

            // The records on page 2 should be different from page 1
            page2.Should().NotContain(r => page1.Any(pr => pr.Id == r.Id));
        }

        /// <summary>
        /// Tests that the GetStatus endpoint returns 'in' status after a clock-in event.
        /// </summary>
        [Fact]
        public async Task GetStatus_AfterClockIn_ShouldReturnClockedIn()
        {
            // Arrange
            using (var context = Factory.CreateDbContext())
            {
                context.TimeRecords.Add(new TimeRecord 
                { 
                    UserId = TestUserId, 
                    Type = "in", 
                    Timestamp = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();
            }

            // Act
            var response = await GetAsync<Dictionary<string, string>>("/api/time/status");

            // Assert
            response.Should().NotBeNull();
            response.Should().ContainKey("status");
            response["status"].Should().Be("in");
        }

        /// <summary>
        /// Tests that the GetStatus endpoint returns 'out' status after a clock-out event.
        /// </summary>
        [Fact]
        public async Task GetStatus_AfterClockOut_ShouldReturnClockedOut()
        {
            // Arrange
            using (var context = Factory.CreateDbContext())
            {
                context.TimeRecords.Add(new TimeRecord 
                { 
                    UserId = TestUserId, 
                    Type = "in", 
                    Timestamp = DateTime.UtcNow.AddHours(-1) 
                });
                context.TimeRecords.Add(new TimeRecord 
                { 
                    UserId = TestUserId, 
                    Type = "out", 
                    Timestamp = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();
            }

            // Act
            var response = await GetAsync<Dictionary<string, string>>("/api/time/status");

            // Assert
            response.Should().NotBeNull();
            response.Should().ContainKey("status");
            response["status"].Should().Be("out");
        }

        /// <summary>
        /// Tests that the GetByDateRange endpoint returns only records within the specified date range.
        /// </summary>
        [Fact]
        public async Task GetByDateRange_ShouldReturnRecordsInRange()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var startDate = now.AddDays(-5).Date; // Use Date to avoid time component
            var endDate = now.AddDays(-2).Date;

            using (var context = Factory.CreateDbContext())
            {
                // Add records with different dates
                context.TimeRecords.Add(new TimeRecord 
                { 
                    UserId = TestUserId, 
                    Type = "in", 
                    Timestamp = now.AddDays(-6) // Before range
                });
                context.TimeRecords.Add(new TimeRecord 
                { 
                    UserId = TestUserId, 
                    Type = "in", 
                    Timestamp = now.AddDays(-4) // Within range
                });
                context.TimeRecords.Add(new TimeRecord 
                { 
                    UserId = TestUserId, 
                    Type = "out", 
                    Timestamp = now.AddDays(-3) // Within range
                });
                context.TimeRecords.Add(new TimeRecord 
                { 
                    UserId = TestUserId, 
                    Type = "in", 
                    Timestamp = now.AddDays(-1) // After range
                });
                await context.SaveChangesAsync();
            }

            // Act
            var response = await GetAsync<List<TimeRecord>>($"/api/time/range?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");

            // Assert
            response.Should().NotBeNull();
            response.Should().HaveCount(2);
            response.Should().AllSatisfy(r => 
                r.Timestamp.Date.Should().BeOnOrAfter(startDate) &&
                r.Timestamp.Date.Should().BeOnOrBefore(endDate)
            );
        }

        /// <summary>
        /// Tests that the Clock endpoint returns a bad request response when the request is invalid.
        /// </summary>
        [Fact]
        public async Task Clock_WithInvalidRequest_ShouldReturnBadRequest()
        {
            // Arrange
            var request = new TimeRecordRequest
            {
                Type = "", // Invalid empty type
                Timestamp = DateTime.UtcNow,
                Location = new LocationModel
                {
                    Latitude = 37.7749,
                    Longitude = -122.4194
                }
            };

            // Act - Using direct HttpClient for testing error responses
            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(request),
                System.Text.Encoding.UTF8,
                "application/json");
            
            var response = await Client.PostAsync("/api/time", content);

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        }

        /// <summary>
        /// Helper method to add test time records to the database for testing.
        /// </summary>
        /// <param name="count">Number of records to create</param>
        private void AddTestTimeRecords(int count)
        {
            using var context = Factory.CreateDbContext();
            
            var records = new List<TimeRecord>();
            for (int i = 0; i < count; i++)
            {
                records.Add(new TimeRecord
                {
                    UserId = TestUserId,
                    Type = i % 2 == 0 ? "in" : "out",
                    Timestamp = DateTime.UtcNow.AddHours(-i),
                    Latitude = 37.7749,
                    Longitude = -122.4194
                });
            }
            
            context.TimeRecords.AddRange(records);
            context.SaveChanges();
        }
    }
}