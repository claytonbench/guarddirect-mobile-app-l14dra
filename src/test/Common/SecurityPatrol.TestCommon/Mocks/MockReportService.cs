using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using SecurityPatrol.Services;
using SecurityPatrol.Models;
using SecurityPatrol.TestCommon.Data;
using SecurityPatrol.TestCommon.Constants;

namespace SecurityPatrol.TestCommon.Mocks
{
    /// <summary>
    /// Mock implementation of IReportService for testing purposes that simulates activity reporting functionality
    /// without accessing actual backend services.
    /// </summary>
    public class MockReportService : IReportService
    {
        private List<ReportModel> _reports;
        public bool ShouldSucceed { get; set; }
        public bool ShouldThrowException { get; set; }
        private int _nextId;
        public int SyncSuccessCount { get; private set; }
        public string LastCreatedReportText { get; private set; }
        public int LastDeletedReportId { get; private set; }

        /// <summary>
        /// Initializes a new instance of the MockReportService class with default test values
        /// </summary>
        public MockReportService()
        {
            _reports = new List<ReportModel>();
            ShouldSucceed = true;
            ShouldThrowException = false;
            _nextId = 1;
            SyncSuccessCount = 0;
            LastCreatedReportText = null;
            LastDeletedReportId = 0;
        }

        /// <summary>
        /// Simulates creating a new activity report with the specified text and location
        /// </summary>
        /// <param name="text">The content of the activity report</param>
        /// <param name="latitude">The latitude coordinate where the report was created</param>
        /// <param name="longitude">The longitude coordinate where the report was created</param>
        /// <returns>A task that returns the created report model</returns>
        public async Task<ReportModel> CreateReportAsync(string text, double latitude, double longitude)
        {
            if (ShouldThrowException)
                throw new Exception("Simulated exception in CreateReportAsync");

            if (string.IsNullOrEmpty(text))
                throw new ArgumentException("Text cannot be empty");

            if (!ShouldSucceed)
                throw new Exception("Simulated failure in CreateReportAsync");

            var report = MockDataGenerator.CreateReportModel(_nextId, text, false);
            report.Latitude = latitude;
            report.Longitude = longitude;
            _nextId++;
            _reports.Add(report);
            LastCreatedReportText = text;
            
            return await Task.FromResult(report);
        }

        /// <summary>
        /// Simulates retrieving an activity report by its ID
        /// </summary>
        /// <param name="id">The unique identifier of the report to retrieve</param>
        /// <returns>A task that returns the report with the specified ID, or null if not found</returns>
        public async Task<ReportModel> GetReportAsync(int id)
        {
            if (ShouldThrowException)
                throw new Exception("Simulated exception in GetReportAsync");

            if (id <= 0)
                throw new ArgumentException("ID must be greater than zero");

            var report = _reports.FirstOrDefault(r => r.Id == id);
            
            if (report == null)
                return await Task.FromResult<ReportModel>(null);

            return await Task.FromResult(report.Clone());
        }

        /// <summary>
        /// Simulates retrieving all activity reports
        /// </summary>
        /// <returns>A task that returns a collection of all reports</returns>
        public async Task<IEnumerable<ReportModel>> GetAllReportsAsync()
        {
            if (ShouldThrowException)
                throw new Exception("Simulated exception in GetAllReportsAsync");

            return await Task.FromResult(_reports.Select(r => r.Clone()).ToList());
        }

        /// <summary>
        /// Simulates retrieving the most recent activity reports up to the specified limit
        /// </summary>
        /// <param name="limit">The maximum number of reports to retrieve</param>
        /// <returns>A task that returns a collection of the most recent reports</returns>
        public async Task<IEnumerable<ReportModel>> GetRecentReportsAsync(int limit)
        {
            if (ShouldThrowException)
                throw new Exception("Simulated exception in GetRecentReportsAsync");

            if (limit <= 0)
                throw new ArgumentException("Limit must be greater than zero");

            var recent = _reports.OrderByDescending(r => r.Timestamp)
                                .Take(limit)
                                .Select(r => r.Clone())
                                .ToList();
            
            return await Task.FromResult(recent);
        }

        /// <summary>
        /// Simulates updating an existing activity report
        /// </summary>
        /// <param name="report">The report model with updated values</param>
        /// <returns>A task that returns true if the update was successful, false otherwise</returns>
        public async Task<bool> UpdateReportAsync(ReportModel report)
        {
            if (ShouldThrowException)
                throw new Exception("Simulated exception in UpdateReportAsync");

            if (report == null)
                throw new ArgumentNullException(nameof(report), "Report cannot be null");

            if (report.Id <= 0)
                throw new ArgumentException("Report ID must be greater than zero");

            if (!ShouldSucceed)
                return await Task.FromResult(false);

            var existingReport = _reports.FirstOrDefault(r => r.Id == report.Id);
            
            if (existingReport == null)
                return await Task.FromResult(false);

            existingReport.Text = report.Text;
            existingReport.Timestamp = report.Timestamp;
            existingReport.Latitude = report.Latitude;
            existingReport.Longitude = report.Longitude;
            existingReport.IsSynced = false; // Mark as not synced since it was updated
            
            return await Task.FromResult(true);
        }

        /// <summary>
        /// Simulates deleting an activity report
        /// </summary>
        /// <param name="id">The unique identifier of the report to delete</param>
        /// <returns>A task that returns true if the deletion was successful, false otherwise</returns>
        public async Task<bool> DeleteReportAsync(int id)
        {
            if (ShouldThrowException)
                throw new Exception("Simulated exception in DeleteReportAsync");

            if (id <= 0)
                throw new ArgumentException("ID must be greater than zero");

            if (!ShouldSucceed)
                return await Task.FromResult(false);

            var report = _reports.FirstOrDefault(r => r.Id == id);
            
            if (report == null)
                return await Task.FromResult(false);

            _reports.Remove(report);
            LastDeletedReportId = id;
            
            return await Task.FromResult(true);
        }

        /// <summary>
        /// Simulates synchronizing a specific report with the backend API
        /// </summary>
        /// <param name="id">The unique identifier of the report to synchronize</param>
        /// <returns>A task that returns true if the synchronization was successful, false otherwise</returns>
        public async Task<bool> SyncReportAsync(int id)
        {
            if (ShouldThrowException)
                throw new Exception("Simulated exception in SyncReportAsync");

            if (id <= 0)
                throw new ArgumentException("ID must be greater than zero");

            if (!ShouldSucceed)
                return await Task.FromResult(false);

            var report = _reports.FirstOrDefault(r => r.Id == id);
            
            if (report == null)
                return await Task.FromResult(false);

            report.IsSynced = true;
            
            if (string.IsNullOrEmpty(report.RemoteId))
                report.RemoteId = $"report_{id}";
                
            SyncSuccessCount++;
            
            return await Task.FromResult(true);
        }

        /// <summary>
        /// Simulates synchronizing all unsynchronized reports with the backend API
        /// </summary>
        /// <returns>A task that returns the number of successfully synchronized reports</returns>
        public async Task<int> SyncAllReportsAsync()
        {
            if (ShouldThrowException)
                throw new Exception("Simulated exception in SyncAllReportsAsync");

            if (!ShouldSucceed)
                return await Task.FromResult(0);

            var unsyncedReports = _reports.Where(r => !r.IsSynced).ToList();
            
            foreach (var report in unsyncedReports)
            {
                report.IsSynced = true;
                
                if (string.IsNullOrEmpty(report.RemoteId))
                    report.RemoteId = $"report_{report.Id}";
            }
            
            int syncCount = unsyncedReports.Count;
            SyncSuccessCount += syncCount;
            
            return await Task.FromResult(syncCount);
        }

        /// <summary>
        /// Simulates retrieving activity reports within the specified date range
        /// </summary>
        /// <param name="startDate">The start date of the range (inclusive)</param>
        /// <param name="endDate">The end date of the range (inclusive)</param>
        /// <returns>A task that returns a collection of reports within the date range</returns>
        public async Task<IEnumerable<ReportModel>> GetReportsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            if (ShouldThrowException)
                throw new Exception("Simulated exception in GetReportsByDateRangeAsync");

            if (startDate > endDate)
                throw new ArgumentException("Start date must be before or equal to end date");

            var filteredReports = _reports.Where(r => r.Timestamp >= startDate && r.Timestamp <= endDate)
                                        .Select(r => r.Clone())
                                        .ToList();
            
            return await Task.FromResult(filteredReports);
        }

        /// <summary>
        /// Simulates deleting synchronized reports older than the specified retention period
        /// </summary>
        /// <param name="retentionDays">The number of days to retain reports</param>
        /// <returns>A task that returns the number of deleted reports</returns>
        public async Task<int> CleanupOldReportsAsync(int retentionDays)
        {
            if (ShouldThrowException)
                throw new Exception("Simulated exception in CleanupOldReportsAsync");

            if (retentionDays <= 0)
                throw new ArgumentException("Retention days must be greater than zero");

            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
            var reportsToDelete = _reports.Where(r => r.IsSynced && r.Timestamp < cutoffDate).ToList();
            
            int count = reportsToDelete.Count;
            
            foreach (var report in reportsToDelete)
            {
                _reports.Remove(report);
            }
            
            return await Task.FromResult(count);
        }

        /// <summary>
        /// Sets predefined reports for testing scenarios
        /// </summary>
        /// <param name="reports">The list of reports to use</param>
        public void SetReports(List<ReportModel> reports)
        {
            _reports.Clear();
            _reports.AddRange(reports);
            
            // Make sure _nextId is greater than the highest ID in the provided reports
            if (reports.Any())
            {
                _nextId = reports.Max(r => r.Id) + 1;
            }
            else
            {
                _nextId = 1;
            }
        }

        /// <summary>
        /// Generates a sequence of reports for testing
        /// </summary>
        /// <param name="count">The number of reports to generate</param>
        /// <param name="allSynced">Whether all generated reports should be marked as synced</param>
        public void GenerateReports(int count, bool allSynced)
        {
            var reports = MockDataGenerator.CreateReportModels(count, allSynced);
            SetReports(reports);
        }
    }
}