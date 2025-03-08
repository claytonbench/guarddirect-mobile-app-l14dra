using FluentAssertions; // FluentAssertions v6.0.0
using Microsoft.EntityFrameworkCore; // Microsoft.EntityFrameworkCore v8.0.0
using Microsoft.EntityFrameworkCore.InMemory; // Microsoft.EntityFrameworkCore.InMemory v8.0.0
using Moq; // Moq v4.18.0
using SecurityPatrol.API.UnitTests.Setup;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.Core.Models;
using SecurityPatrol.TestCommon.Constants;
using SecurityPatrol.TestCommon.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit; // Xunit v2.4.0

namespace SecurityPatrol.API.UnitTests.Repositories
{
    /// <summary>
    /// Contains unit tests for the ReportRepository class to verify its data access operations.
    /// </summary>
    public class ReportRepositoryTests : TestBase, IAsyncDisposable
    {
        private readonly SecurityPatrolDbContext _dbContext;
        private readonly IReportRepository _repository;

        /// <summary>
        /// Initializes a new instance of the ReportRepositoryTests class with an in-memory database context and repository
        /// </summary>
        public ReportRepositoryTests()
        {
            // Create a new DbContextOptionsBuilder<SecurityPatrolDbContext> with a unique in-memory database name
            var options = new DbContextOptionsBuilder<SecurityPatrolDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            // Create a new SecurityPatrolDbContext with the options
            _dbContext = new SecurityPatrolDbContext(options, Mock.Of<AuditableEntityInterceptor>());

            // Create a new ReportRepository with the context
            _repository = new ReportRepository(_dbContext);

            // Seed the database with test reports
            SeedDatabase();
        }

        /// <summary>
        /// Seeds the in-memory database with test report data
        /// </summary>
        private void SeedDatabase()
        {
            // Add TestReports.AllBackendReports to _dbContext.Reports
            _dbContext.Reports.AddRange(TestReports.AllBackendReports);

            // Save changes to the database
            _dbContext.SaveChanges();
        }

        /// <summary>
        /// Cleans up resources after tests are complete
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            // Dispose the database context
            await _dbContext.DisposeAsync();
        }

        /// <summary>
        /// Tests that GetByIdAsync returns the correct report when given a valid ID
        /// </summary>
        [Fact]
        public async Task GetByIdAsync_WithValidId_ReturnsReport()
        {
            // Call _repository.GetByIdAsync with a valid report ID
            var report = await _repository.GetByIdAsync(TestReports.DefaultBackendReport.Id);

            // Assert that the returned report is not null
            report.Should().NotBeNull();

            // Assert that the returned report has the expected ID
            report.Id.Should().Be(TestReports.DefaultBackendReport.Id);

            // Assert that the returned report has the expected properties
            report.Text.Should().Be(TestReports.DefaultBackendReport.Text);
            report.UserId.Should().Be(TestReports.DefaultBackendReport.UserId);
        }

        /// <summary>
        /// Tests that GetByIdAsync returns null when given an invalid ID
        /// </summary>
        [Fact]
        public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Call _repository.GetByIdAsync with an invalid report ID (e.g., 999)
            var report = await _repository.GetByIdAsync(999);

            // Assert that the returned report is null
            report.Should().BeNull();
        }

        /// <summary>
        /// Tests that GetByRemoteIdAsync returns the correct report when given a valid remote ID
        /// </summary>
        [Fact]
        public async Task GetByRemoteIdAsync_WithValidRemoteId_ReturnsReport()
        {
            // Call _repository.GetByRemoteIdAsync with a valid remote ID (e.g., 'report_3')
            var report = await _repository.GetByRemoteIdAsync(TestReports.OldBackendReport.RemoteId);

            // Assert that the returned report is not null
            report.Should().NotBeNull();

            // Assert that the returned report has the expected remote ID
            report.RemoteId.Should().Be(TestReports.OldBackendReport.RemoteId);

            // Assert that the returned report has the expected properties
            report.Text.Should().Be(TestReports.OldBackendReport.Text);
            report.UserId.Should().Be(TestReports.OldBackendReport.UserId);
        }

        /// <summary>
        /// Tests that GetByRemoteIdAsync returns null when given an invalid remote ID
        /// </summary>
        [Fact]
        public async Task GetByRemoteIdAsync_WithInvalidRemoteId_ReturnsNull()
        {
            // Call _repository.GetByRemoteIdAsync with an invalid remote ID (e.g., 'nonexistent')
            var report = await _repository.GetByRemoteIdAsync("nonexistent");

            // Assert that the returned report is null
            report.Should().BeNull();
        }

        /// <summary>
        /// Tests that GetAllAsync returns all reports in the repository
        /// </summary>
        [Fact]
        public async Task GetAllAsync_ReturnsAllReports()
        {
            // Call _repository.GetAllAsync
            var reports = await _repository.GetAllAsync();

            // Assert that the returned collection is not null
            reports.Should().NotBeNull();

            // Assert that the returned collection contains the expected number of reports
            reports.Count().Should().Be(TestReports.AllBackendReports.Count);

            // Assert that the returned collection contains all expected reports
            reports.Should().BeEquivalentTo(TestReports.AllBackendReports, options => options.Excluding(r => r.User));

            // Assert that the returned reports are ordered by Timestamp descending
            reports.Should().BeInDescendingOrder(r => r.Timestamp);
        }

        /// <summary>
        /// Tests that GetPaginatedAsync returns the correct page of reports
        /// </summary>
        [Fact]
        public async Task GetPaginatedAsync_ReturnsCorrectPage()
        {
            // Call _repository.GetPaginatedAsync with pageNumber=1 and pageSize=2
            var paginatedList = await _repository.GetPaginatedAsync(pageNumber: 1, pageSize: 2);

            // Assert that the returned PaginatedList is not null
            paginatedList.Should().NotBeNull();

            // Assert that the returned PaginatedList.PageNumber is 1
            paginatedList.PageNumber.Should().Be(1);

            // Assert that the returned PaginatedList.Items contains 2 reports
            paginatedList.Items.Count.Should().Be(2);

            // Assert that the returned PaginatedList.TotalCount matches the total number of reports
            paginatedList.TotalCount.Should().Be(TestReports.AllBackendReports.Count);

            // Assert that the returned reports are ordered by Timestamp descending
            paginatedList.Items.Should().BeInDescendingOrder(r => r.Timestamp);
        }

        /// <summary>
        /// Tests that GetByUserIdAsync returns reports for a specific user
        /// </summary>
        [Fact]
        public async Task GetByUserIdAsync_WithValidUserId_ReturnsUserReports()
        {
            // Call _repository.GetByUserIdAsync with TestConstants.TestUserId
            var reports = await _repository.GetByUserIdAsync(TestConstants.TestUserId);

            // Assert that the returned collection is not null
            reports.Should().NotBeNull();

            // Assert that all returned reports have the expected UserId
            reports.Should().OnlyContain(r => r.UserId == TestConstants.TestUserId);

            // Assert that the returned reports are ordered by Timestamp descending
            reports.Should().BeInDescendingOrder(r => r.Timestamp);
        }

        /// <summary>
        /// Tests that GetByUserIdAsync returns an empty collection for an invalid user ID
        /// </summary>
        [Fact]
        public async Task GetByUserIdAsync_WithInvalidUserId_ReturnsEmptyCollection()
        {
            // Call _repository.GetByUserIdAsync with an invalid user ID (e.g., 'nonexistent')
            var reports = await _repository.GetByUserIdAsync("nonexistent");

            // Assert that the returned collection is not null
            reports.Should().NotBeNull();

            // Assert that the returned collection is empty
            reports.Should().BeEmpty();
        }

        /// <summary>
        /// Tests that GetPaginatedByUserIdAsync returns the correct page of reports for a specific user
        /// </summary>
        [Fact]
        public async Task GetPaginatedByUserIdAsync_ReturnsCorrectUserPage()
        {
            // Call _repository.GetPaginatedByUserIdAsync with TestConstants.TestUserId, pageNumber=1, and pageSize=2
            var paginatedList = await _repository.GetPaginatedByUserIdAsync(TestConstants.TestUserId, pageNumber: 1, pageSize: 2);

            // Assert that the returned PaginatedList is not null
            paginatedList.Should().NotBeNull();

            // Assert that the returned PaginatedList.PageNumber is 1
            paginatedList.PageNumber.Should().Be(1);

            // Assert that all returned reports have the expected UserId
            paginatedList.Items.Should().OnlyContain(r => r.UserId == TestConstants.TestUserId);

            // Assert that the returned reports are ordered by Timestamp descending
            paginatedList.Items.Should().BeInDescendingOrder(r => r.Timestamp);
        }

        /// <summary>
        /// Tests that GetUnsyncedAsync returns only unsynced reports
        /// </summary>
        [Fact]
        public async Task GetUnsyncedAsync_ReturnsUnsyncedReports()
        {
            // Call _repository.GetUnsyncedAsync
            var reports = await _repository.GetUnsyncedAsync();

            // Assert that the returned collection is not null
            reports.Should().NotBeNull();

            // Assert that all returned reports have IsSynced set to false
            reports.Should().OnlyContain(r => r.IsSynced == false);
        }

        /// <summary>
        /// Tests that GetByDateRangeAsync returns reports within the specified date range
        /// </summary>
        [Fact]
        public async Task GetByDateRangeAsync_ReturnsReportsInRange()
        {
            // Create startDate as DateTime.UtcNow.AddDays(-30)
            var startDate = DateTime.UtcNow.AddDays(-30);

            // Create endDate as DateTime.UtcNow
            var endDate = DateTime.UtcNow;

            // Call _repository.GetByDateRangeAsync with startDate and endDate
            var reports = await _repository.GetByDateRangeAsync(startDate, endDate);

            // Assert that the returned collection is not null
            reports.Should().NotBeNull();

            // Assert that all returned reports have Timestamp between startDate and endDate
            reports.Should().OnlyContain(r => r.Timestamp >= startDate && r.Timestamp <= endDate);

            // Assert that the returned reports are ordered by Timestamp descending
            reports.Should().BeInDescendingOrder(r => r.Timestamp);
        }

        /// <summary>
        /// Tests that AddAsync correctly adds a new report to the repository
        /// </summary>
        [Fact]
        public async Task AddAsync_AddsNewReport()
        {
            // Create a new Report with test data
            var newReport = new Report
            {
                Text = "New test report",
                Timestamp = DateTime.UtcNow,
                Latitude = 12.34,
                Longitude = 56.78,
                UserId = TestConstants.TestUserId,
                IsSynced = false,
                RemoteId = null
            };

            // Call _repository.AddAsync with the new report
            var addedReport = await _repository.AddAsync(newReport);

            // Assert that the returned report is not null
            addedReport.Should().NotBeNull();

            // Assert that the returned report has a non-zero ID
            addedReport.Id.Should().NotBe(0);

            // Assert that the returned report has the expected properties
            addedReport.Text.Should().Be("New test report");
            addedReport.UserId.Should().Be(TestConstants.TestUserId);

            // Call _repository.GetByIdAsync with the new report's ID
            var retrievedReport = await _repository.GetByIdAsync(addedReport.Id);

            // Assert that the report was successfully retrieved from the repository
            retrievedReport.Should().NotBeNull();
            retrievedReport.Id.Should().Be(addedReport.Id);
        }

        /// <summary>
        /// Tests that UpdateAsync correctly updates an existing report in the repository
        /// </summary>
        [Fact]
        public async Task UpdateAsync_UpdatesExistingReport()
        {
            // Call _repository.GetByIdAsync to get an existing report
            var existingReport = await _repository.GetByIdAsync(TestReports.DefaultBackendReport.Id);
            existingReport.Should().NotBeNull();

            // Modify the report's properties (e.g., Text, IsSynced)
            existingReport.Text = "Updated test report";
            existingReport.IsSynced = true;

            // Call _repository.UpdateAsync with the modified report
            await _repository.UpdateAsync(existingReport);

            // Call _repository.GetByIdAsync again to get the updated report
            var updatedReport = await _repository.GetByIdAsync(TestReports.DefaultBackendReport.Id);

            // Assert that the retrieved report has the updated properties
            updatedReport.Should().NotBeNull();
            updatedReport.Text.Should().Be("Updated test report");
            updatedReport.IsSynced.Should().Be(true);
        }

        /// <summary>
        /// Tests that DeleteAsync correctly removes a report from the repository
        /// </summary>
        [Fact]
        public async Task DeleteAsync_DeletesExistingReport()
        {
            // Call _repository.GetByIdAsync to verify a report exists
            var existingReport = await _repository.GetByIdAsync(TestReports.DefaultBackendReport.Id);
            existingReport.Should().NotBeNull();

            // Call _repository.DeleteAsync with the report's ID
            await _repository.DeleteAsync(TestReports.DefaultBackendReport.Id);

            // Call _repository.GetByIdAsync again with the same ID
            var deletedReport = await _repository.GetByIdAsync(TestReports.DefaultBackendReport.Id);

            // Assert that the returned report is null, indicating it was deleted
            deletedReport.Should().BeNull();
        }

        /// <summary>
        /// Tests that UpdateSyncStatusAsync correctly updates a report's sync status
        /// </summary>
        [Fact]
        public async Task UpdateSyncStatusAsync_UpdatesSyncStatus()
        {
            // Call _repository.GetByIdAsync to get an existing report
            var existingReport = await _repository.GetByIdAsync(TestReports.DefaultBackendReport.Id);
            existingReport.Should().NotBeNull();

            // Note the current IsSynced value
            var initialSyncStatus = existingReport.IsSynced;

            // Call _repository.UpdateSyncStatusAsync with the report's ID and the opposite sync status
            await _repository.UpdateSyncStatusAsync(TestReports.DefaultBackendReport.Id, !initialSyncStatus);

            // Call _repository.GetByIdAsync again to get the updated report
            var updatedReport = await _repository.GetByIdAsync(TestReports.DefaultBackendReport.Id);

            // Assert that the retrieved report's IsSynced value has been updated
            updatedReport.Should().NotBeNull();
            updatedReport.IsSynced.Should().Be(!initialSyncStatus);
        }

        /// <summary>
        /// Tests that CountAsync returns the correct number of reports
        /// </summary>
        [Fact]
        public async Task CountAsync_ReturnsCorrectCount()
        {
            // Call _repository.CountAsync
            var count = await _repository.CountAsync();

            // Assert that the returned count matches the expected number of reports
            count.Should().Be(TestReports.AllBackendReports.Count);
        }

        /// <summary>
        /// Tests that CountByUserIdAsync returns the correct number of reports for a specific user
        /// </summary>
        [Fact]
        public async Task CountByUserIdAsync_ReturnsCorrectUserCount()
        {
            // Call _repository.CountByUserIdAsync with TestConstants.TestUserId
            var count = await _repository.CountByUserIdAsync(TestConstants.TestUserId);

            // Assert that the returned count matches the expected number of reports for that user
            count.Should().Be(TestReports.AllBackendReports.Count(r => r.UserId == TestConstants.TestUserId));
        }
    }
}