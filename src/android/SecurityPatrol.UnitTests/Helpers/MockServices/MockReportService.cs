using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SecurityPatrol.Models;
using SecurityPatrol.Services;

namespace SecurityPatrol.UnitTests.Helpers.MockServices
{
    /// <summary>
    /// Mock implementation of the IReportService interface for unit testing.
    /// This class provides a configurable implementation of activity report functionality
    /// that can be used in unit tests without requiring actual backend services or database access.
    /// </summary>
    public class MockReportService : IReportService
    {
        private List<ReportModel> _reports;
        
        // Configuration flags for success/failure
        public bool ShouldCreateReportSucceed { get; set; }
        public bool ShouldUpdateReportSucceed { get; set; }
        public bool ShouldDeleteReportSucceed { get; set; }
        public bool ShouldSyncReportSucceed { get; set; }
        public bool ShouldSyncAllReportsSucceed { get; set; }
        
        // Exception properties for testing error handling
        public Exception CreateReportException { get; set; }
        public Exception GetReportException { get; set; }
        public Exception GetAllReportsException { get; set; }
        public Exception GetRecentReportsException { get; set; }
        public Exception UpdateReportException { get; set; }
        public Exception DeleteReportException { get; set; }
        public Exception SyncReportException { get; set; }
        public Exception SyncAllReportsException { get; set; }
        public Exception GetReportsByDateRangeException { get; set; }
        public Exception CleanupOldReportsException { get; set; }
        
        // State tracking properties
        public string DefaultUserId { get; set; }
        public int NextReportId { get; set; }
        public int SyncedReportCount { get; set; }
        
        /// <summary>
        /// Initializes a new instance of the MockReportService class with default settings.
        /// </summary>
        public MockReportService()
        {
            _reports = new List<ReportModel>();
            ShouldCreateReportSucceed = true;
            ShouldUpdateReportSucceed = true;
            ShouldDeleteReportSucceed = true;
            ShouldSyncReportSucceed = true;
            ShouldSyncAllReportsSucceed = true;
            DefaultUserId = "test-user";
            NextReportId = 1;
            SyncedReportCount = 0;
        }
        
        /// <summary>
        /// Mocks creating a new activity report.
        /// </summary>
        /// <param name="text">The content of the activity report.</param>
        /// <param name="latitude">The latitude coordinate where the report was created.</param>
        /// <param name="longitude">The longitude coordinate where the report was created.</param>
        /// <returns>A task that returns the created report model.</returns>
        public async Task<ReportModel> CreateReportAsync(string text, double latitude, double longitude)
        {
            if (CreateReportException != null)
                throw CreateReportException;
                
            if (!ShouldCreateReportSucceed)
                throw new InvalidOperationException("Report creation failed");
                
            if (string.IsNullOrEmpty(text))
                throw new ArgumentException("Report text cannot be empty");
                
            var report = new ReportModel
            {
                Id = NextReportId++,
                Text = text,
                Timestamp = DateTime.UtcNow,
                UserId = DefaultUserId,
                Latitude = latitude,
                Longitude = longitude,
                IsSynced = false
            };
            
            _reports.Add(report);
            return report;
        }
        
        /// <summary>
        /// Mocks retrieving an activity report by ID.
        /// </summary>
        /// <param name="id">The unique identifier of the report to retrieve.</param>
        /// <returns>A task that returns the report with the specified ID, or null if not found.</returns>
        public async Task<ReportModel> GetReportAsync(int id)
        {
            if (GetReportException != null)
                throw GetReportException;
                
            if (id <= 0)
                throw new ArgumentException("Invalid report ID");
                
            var report = _reports.FirstOrDefault(r => r.Id == id);
            return report?.Clone(); // Return a clone to prevent external modification
        }
        
        /// <summary>
        /// Mocks retrieving all activity reports.
        /// </summary>
        /// <returns>A task that returns a collection of all reports.</returns>
        public async Task<IEnumerable<ReportModel>> GetAllReportsAsync()
        {
            if (GetAllReportsException != null)
                throw GetAllReportsException;
                
            return _reports.Select(r => r.Clone()).ToList();
        }
        
        /// <summary>
        /// Mocks retrieving the most recent activity reports.
        /// </summary>
        /// <param name="limit">The maximum number of reports to retrieve.</param>
        /// <returns>A task that returns a collection of the most recent reports.</returns>
        public async Task<IEnumerable<ReportModel>> GetRecentReportsAsync(int limit)
        {
            if (GetRecentReportsException != null)
                throw GetRecentReportsException;
                
            if (limit <= 0)
                throw new ArgumentException("Limit must be greater than zero");
                
            return _reports
                .OrderByDescending(r => r.Timestamp)
                .Take(limit)
                .Select(r => r.Clone())
                .ToList();
        }
        
        /// <summary>
        /// Mocks updating an existing activity report.
        /// </summary>
        /// <param name="report">The report model with updated values.</param>
        /// <returns>A task that returns true if the update was successful, false otherwise.</returns>
        public async Task<bool> UpdateReportAsync(ReportModel report)
        {
            if (UpdateReportException != null)
                throw UpdateReportException;
                
            if (report == null)
                throw new ArgumentNullException(nameof(report));
                
            if (report.Id <= 0)
                throw new ArgumentException("Invalid report ID");
                
            if (string.IsNullOrEmpty(report.Text))
                throw new ArgumentException("Report text cannot be empty");
                
            if (!ShouldUpdateReportSucceed)
                return false;
                
            var existingReport = _reports.FirstOrDefault(r => r.Id == report.Id);
            if (existingReport == null)
                return false;
                
            existingReport.Text = report.Text;
            existingReport.Timestamp = report.Timestamp;
            existingReport.Latitude = report.Latitude;
            existingReport.Longitude = report.Longitude;
            existingReport.IsSynced = report.IsSynced;
            existingReport.RemoteId = report.RemoteId;
            
            return true;
        }
        
        /// <summary>
        /// Mocks deleting an activity report.
        /// </summary>
        /// <param name="id">The unique identifier of the report to delete.</param>
        /// <returns>A task that returns true if the deletion was successful, false otherwise.</returns>
        public async Task<bool> DeleteReportAsync(int id)
        {
            if (DeleteReportException != null)
                throw DeleteReportException;
                
            if (id <= 0)
                throw new ArgumentException("Invalid report ID");
                
            if (!ShouldDeleteReportSucceed)
                return false;
                
            var report = _reports.FirstOrDefault(r => r.Id == id);
            if (report == null)
                return false;
                
            _reports.Remove(report);
            return true;
        }
        
        /// <summary>
        /// Mocks synchronizing a specific report with the backend API.
        /// </summary>
        /// <param name="id">The unique identifier of the report to synchronize.</param>
        /// <returns>A task that returns true if the synchronization was successful, false otherwise.</returns>
        public async Task<bool> SyncReportAsync(int id)
        {
            if (SyncReportException != null)
                throw SyncReportException;
                
            if (id <= 0)
                throw new ArgumentException("Invalid report ID");
                
            if (!ShouldSyncReportSucceed)
                return false;
                
            var report = _reports.FirstOrDefault(r => r.Id == id);
            if (report == null)
                return false;
                
            report.IsSynced = true;
            if (string.IsNullOrEmpty(report.RemoteId))
                report.RemoteId = $"remote-{Guid.NewGuid()}";
                
            return true;
        }
        
        /// <summary>
        /// Mocks synchronizing all unsynchronized reports with the backend API.
        /// </summary>
        /// <returns>A task that returns the number of successfully synchronized reports.</returns>
        public async Task<int> SyncAllReportsAsync()
        {
            if (SyncAllReportsException != null)
                throw SyncAllReportsException;
                
            if (!ShouldSyncAllReportsSucceed)
                return 0;
                
            var unsyncedReports = _reports.Where(r => !r.IsSynced).ToList();
            foreach (var report in unsyncedReports)
            {
                report.IsSynced = true;
                if (string.IsNullOrEmpty(report.RemoteId))
                    report.RemoteId = $"remote-{Guid.NewGuid()}";
            }
            
            SyncedReportCount = unsyncedReports.Count;
            return unsyncedReports.Count;
        }
        
        /// <summary>
        /// Mocks retrieving activity reports within a date range.
        /// </summary>
        /// <param name="startDate">The start date of the range (inclusive).</param>
        /// <param name="endDate">The end date of the range (inclusive).</param>
        /// <returns>A task that returns a collection of reports within the date range.</returns>
        public async Task<IEnumerable<ReportModel>> GetReportsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            if (GetReportsByDateRangeException != null)
                throw GetReportsByDateRangeException;
                
            if (startDate > endDate)
                throw new ArgumentException("Start date must be before or equal to end date");
                
            return _reports
                .Where(r => r.Timestamp >= startDate && r.Timestamp <= endDate)
                .Select(r => r.Clone())
                .ToList();
        }
        
        /// <summary>
        /// Mocks deleting synchronized reports older than the specified retention period.
        /// </summary>
        /// <param name="retentionDays">The number of days to retain reports.</param>
        /// <returns>A task that returns the number of deleted reports.</returns>
        public async Task<int> CleanupOldReportsAsync(int retentionDays)
        {
            if (CleanupOldReportsException != null)
                throw CleanupOldReportsException;
                
            if (retentionDays <= 0)
                throw new ArgumentException("Retention days must be greater than zero");
                
            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
            var reportsToDelete = _reports
                .Where(r => r.IsSynced && r.Timestamp < cutoffDate)
                .ToList();
                
            var count = reportsToDelete.Count;
            foreach (var report in reportsToDelete)
            {
                _reports.Remove(report);
            }
            
            return count;
        }
        
        // Helper methods for test setup
        
        /// <summary>
        /// Configures the behavior of the CreateReportAsync method.
        /// </summary>
        /// <param name="shouldSucceed">Whether the method should succeed.</param>
        public void SetupCreateReport(bool shouldSucceed)
        {
            ShouldCreateReportSucceed = shouldSucceed;
            CreateReportException = null;
        }
        
        /// <summary>
        /// Configures the behavior of the UpdateReportAsync method.
        /// </summary>
        /// <param name="shouldSucceed">Whether the method should succeed.</param>
        public void SetupUpdateReport(bool shouldSucceed)
        {
            ShouldUpdateReportSucceed = shouldSucceed;
            UpdateReportException = null;
        }
        
        /// <summary>
        /// Configures the behavior of the DeleteReportAsync method.
        /// </summary>
        /// <param name="shouldSucceed">Whether the method should succeed.</param>
        public void SetupDeleteReport(bool shouldSucceed)
        {
            ShouldDeleteReportSucceed = shouldSucceed;
            DeleteReportException = null;
        }
        
        /// <summary>
        /// Configures the behavior of the SyncReportAsync method.
        /// </summary>
        /// <param name="shouldSucceed">Whether the method should succeed.</param>
        public void SetupSyncReport(bool shouldSucceed)
        {
            ShouldSyncReportSucceed = shouldSucceed;
            SyncReportException = null;
        }
        
        /// <summary>
        /// Configures the behavior of the SyncAllReportsAsync method.
        /// </summary>
        /// <param name="shouldSucceed">Whether the method should succeed.</param>
        public void SetupSyncAllReports(bool shouldSucceed)
        {
            ShouldSyncAllReportsSucceed = shouldSucceed;
            SyncAllReportsException = null;
        }
        
        /// <summary>
        /// Configures the CreateReportAsync method to throw an exception.
        /// </summary>
        /// <param name="exception">The exception to throw.</param>
        public void SetupCreateReportException(Exception exception)
        {
            CreateReportException = exception;
        }
        
        /// <summary>
        /// Configures the GetReportAsync method to throw an exception.
        /// </summary>
        /// <param name="exception">The exception to throw.</param>
        public void SetupGetReportException(Exception exception)
        {
            GetReportException = exception;
        }
        
        /// <summary>
        /// Configures the GetAllReportsAsync method to throw an exception.
        /// </summary>
        /// <param name="exception">The exception to throw.</param>
        public void SetupGetAllReportsException(Exception exception)
        {
            GetAllReportsException = exception;
        }
        
        /// <summary>
        /// Configures the GetRecentReportsAsync method to throw an exception.
        /// </summary>
        /// <param name="exception">The exception to throw.</param>
        public void SetupGetRecentReportsException(Exception exception)
        {
            GetRecentReportsException = exception;
        }
        
        /// <summary>
        /// Configures the UpdateReportAsync method to throw an exception.
        /// </summary>
        /// <param name="exception">The exception to throw.</param>
        public void SetupUpdateReportException(Exception exception)
        {
            UpdateReportException = exception;
        }
        
        /// <summary>
        /// Configures the DeleteReportAsync method to throw an exception.
        /// </summary>
        /// <param name="exception">The exception to throw.</param>
        public void SetupDeleteReportException(Exception exception)
        {
            DeleteReportException = exception;
        }
        
        /// <summary>
        /// Configures the SyncReportAsync method to throw an exception.
        /// </summary>
        /// <param name="exception">The exception to throw.</param>
        public void SetupSyncReportException(Exception exception)
        {
            SyncReportException = exception;
        }
        
        /// <summary>
        /// Configures the SyncAllReportsAsync method to throw an exception.
        /// </summary>
        /// <param name="exception">The exception to throw.</param>
        public void SetupSyncAllReportsException(Exception exception)
        {
            SyncAllReportsException = exception;
        }
        
        /// <summary>
        /// Configures the GetReportsByDateRangeAsync method to throw an exception.
        /// </summary>
        /// <param name="exception">The exception to throw.</param>
        public void SetupGetReportsByDateRangeException(Exception exception)
        {
            GetReportsByDateRangeException = exception;
        }
        
        /// <summary>
        /// Configures the CleanupOldReportsAsync method to throw an exception.
        /// </summary>
        /// <param name="exception">The exception to throw.</param>
        public void SetupCleanupOldReportsException(Exception exception)
        {
            CleanupOldReportsException = exception;
        }
        
        /// <summary>
        /// Configures the reports collection.
        /// </summary>
        /// <param name="reports">The reports to use.</param>
        public void SetupReports(List<ReportModel> reports)
        {
            _reports.Clear();
            _reports.AddRange(reports);
            
            // Update NextReportId to ensure it's greater than the highest ID in the reports
            if (reports.Any())
            {
                NextReportId = reports.Max(r => r.Id) + 1;
            }
            else
            {
                NextReportId = 1;
            }
        }
        
        /// <summary>
        /// Adds a report to the collection.
        /// </summary>
        /// <param name="report">The report to add.</param>
        public void AddReport(ReportModel report)
        {
            if (report == null)
                throw new ArgumentNullException(nameof(report));
                
            if (report.Id <= 0)
            {
                report.Id = NextReportId++;
            }
            
            _reports.Add(report);
            
            // Ensure NextReportId is greater than the added report's ID
            if (report.Id >= NextReportId)
            {
                NextReportId = report.Id + 1;
            }
        }
        
        /// <summary>
        /// Clears all reports from the collection.
        /// </summary>
        public void ClearReports()
        {
            _reports.Clear();
            NextReportId = 1;
        }
        
        /// <summary>
        /// Resets all configurations to default values.
        /// </summary>
        public void Reset()
        {
            _reports.Clear();
            ShouldCreateReportSucceed = true;
            ShouldUpdateReportSucceed = true;
            ShouldDeleteReportSucceed = true;
            ShouldSyncReportSucceed = true;
            ShouldSyncAllReportsSucceed = true;
            
            CreateReportException = null;
            GetReportException = null;
            GetAllReportsException = null;
            GetRecentReportsException = null;
            UpdateReportException = null;
            DeleteReportException = null;
            SyncReportException = null;
            SyncAllReportsException = null;
            GetReportsByDateRangeException = null;
            CleanupOldReportsException = null;
            
            NextReportId = 1;
            SyncedReportCount = 0;
        }
    }
}