using System;
using System.Collections.Generic;
using SecurityPatrol.TestCommon.Constants;
using SecurityPatrol.Database.Entities;
using SecurityPatrol.Models;
using SecurityPatrol.Core.Entities;

namespace SecurityPatrol.TestCommon.Data
{
    /// <summary>
    /// Static class providing predefined test report data for use in unit, integration, and UI tests.
    /// </summary>
    public static class TestReports
    {
        // Mobile report entities
        public static ReportEntity DefaultMobileReportEntity { get; private set; }
        public static ReportEntity SyncedMobileReportEntity { get; private set; }
        public static ReportEntity LongTextMobileReportEntity { get; private set; }
        public static ReportEntity OldMobileReportEntity { get; private set; }
        
        // Mobile report models
        public static ReportModel DefaultMobileReportModel { get; private set; }
        public static ReportModel SyncedMobileReportModel { get; private set; }
        public static ReportModel LongTextMobileReportModel { get; private set; }
        public static ReportModel OldMobileReportModel { get; private set; }
        
        // Backend report entities
        public static Report DefaultBackendReport { get; private set; }
        public static Report LongTextBackendReport { get; private set; }
        public static Report OldBackendReport { get; private set; }
        
        // Collections for easy access
        public static List<ReportEntity> AllMobileReportEntities { get; private set; }
        public static List<ReportModel> AllMobileReportModels { get; private set; }
        public static List<Report> AllBackendReports { get; private set; }
        
        // API test models
        public static ReportRequest DefaultReportRequest { get; private set; }
        public static ReportResponse SuccessReportResponse { get; private set; }
        public static ReportResponse FailureReportResponse { get; private set; }
        
        /// <summary>
        /// Static constructor that initializes all test report data
        /// </summary>
        static TestReports()
        {
            // Initialize DefaultMobileReportEntity
            DefaultMobileReportEntity = new ReportEntity
            {
                Id = 1,
                UserId = TestConstants.TestUserId,
                Text = TestConstants.TestReportText,
                Timestamp = DateTime.UtcNow,
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude,
                IsSynced = false,
                RemoteId = null
            };
            
            // Initialize SyncedMobileReportEntity
            SyncedMobileReportEntity = new ReportEntity
            {
                Id = 2,
                UserId = TestConstants.TestUserId,
                Text = "Synced report text",
                Timestamp = DateTime.UtcNow,
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude,
                IsSynced = true,
                RemoteId = "report_2"
            };
            
            // Initialize LongTextMobileReportEntity
            LongTextMobileReportEntity = new ReportEntity
            {
                Id = 3,
                UserId = TestConstants.TestUserId,
                Text = new string('A', 500), // 500 characters
                Timestamp = DateTime.UtcNow,
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude,
                IsSynced = false,
                RemoteId = null
            };
            
            // Initialize OldMobileReportEntity
            OldMobileReportEntity = new ReportEntity
            {
                Id = 4,
                UserId = TestConstants.TestUserId,
                Text = "Old report text",
                Timestamp = DateTime.UtcNow.AddDays(-60),
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude,
                IsSynced = true,
                RemoteId = "report_4"
            };
            
            // Initialize mobile report models from entities
            DefaultMobileReportModel = ReportModel.FromEntity(DefaultMobileReportEntity);
            SyncedMobileReportModel = ReportModel.FromEntity(SyncedMobileReportEntity);
            LongTextMobileReportModel = ReportModel.FromEntity(LongTextMobileReportEntity);
            OldMobileReportModel = ReportModel.FromEntity(OldMobileReportEntity);
            
            // Initialize backend report entities
            DefaultBackendReport = new Report
            {
                Id = 1,
                Text = TestConstants.TestReportText,
                Timestamp = DateTime.UtcNow,
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude,
                UserId = TestConstants.TestUserId,
                IsSynced = false,
                RemoteId = null
            };
            
            LongTextBackendReport = new Report
            {
                Id = 2,
                Text = new string('A', 500), // 500 characters
                Timestamp = DateTime.UtcNow,
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude,
                UserId = TestConstants.TestUserId,
                IsSynced = false,
                RemoteId = null
            };
            
            OldBackendReport = new Report
            {
                Id = 3,
                Text = "Old report text",
                Timestamp = DateTime.UtcNow.AddDays(-60),
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude,
                UserId = TestConstants.TestUserId,
                IsSynced = true,
                RemoteId = "report_3"
            };
            
            // Initialize collections
            AllMobileReportEntities = new List<ReportEntity>
            {
                DefaultMobileReportEntity,
                SyncedMobileReportEntity,
                LongTextMobileReportEntity,
                OldMobileReportEntity
            };
            
            AllMobileReportModels = new List<ReportModel>
            {
                DefaultMobileReportModel,
                SyncedMobileReportModel,
                LongTextMobileReportModel,
                OldMobileReportModel
            };
            
            AllBackendReports = new List<Report>
            {
                DefaultBackendReport,
                LongTextBackendReport,
                OldBackendReport
            };
            
            // Initialize API test models
            DefaultReportRequest = new ReportRequest
            {
                Text = TestConstants.TestReportText,
                Timestamp = DateTime.UtcNow,
                Location = new LocationData
                {
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                }
            };
            
            SuccessReportResponse = new ReportResponse
            {
                Id = "report_123",
                Status = "success"
            };
            
            FailureReportResponse = new ReportResponse
            {
                Id = null,
                Status = "error"
            };
        }
        
        /// <summary>
        /// Gets a mobile report entity by its ID
        /// </summary>
        /// <param name="id">The ID to search for</param>
        /// <returns>The report entity with the specified ID, or null if not found</returns>
        public static ReportEntity GetMobileReportEntityById(int id)
        {
            return AllMobileReportEntities.Find(r => r.Id == id);
        }
        
        /// <summary>
        /// Gets a mobile report model by its ID
        /// </summary>
        /// <param name="id">The ID to search for</param>
        /// <returns>The report model with the specified ID, or null if not found</returns>
        public static ReportModel GetMobileReportModelById(int id)
        {
            return AllMobileReportModels.Find(r => r.Id == id);
        }
        
        /// <summary>
        /// Gets a backend report entity by its ID
        /// </summary>
        /// <param name="id">The ID to search for</param>
        /// <returns>The report entity with the specified ID, or null if not found</returns>
        public static Report GetBackendReportById(int id)
        {
            return AllBackendReports.Find(r => r.Id == id);
        }
        
        /// <summary>
        /// Creates a new mobile report entity with the specified parameters
        /// </summary>
        /// <param name="id">The ID for the report entity</param>
        /// <param name="userId">The user ID for the report entity</param>
        /// <param name="text">The text content of the report</param>
        /// <param name="timestamp">The timestamp for the report</param>
        /// <param name="isSynced">The sync status of the report</param>
        /// <returns>A new ReportEntity instance with the specified parameters</returns>
        public static ReportEntity CreateMobileReportEntity(
            int id, 
            string userId = null, 
            string text = null, 
            DateTime? timestamp = null, 
            bool isSynced = false)
        {
            var report = new ReportEntity
            {
                Id = id,
                UserId = userId ?? TestConstants.TestUserId,
                Text = text ?? TestConstants.TestReportText,
                Timestamp = timestamp ?? DateTime.UtcNow,
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude,
                IsSynced = isSynced,
                RemoteId = isSynced ? $"report_{id}" : null
            };
            
            return report;
        }
        
        /// <summary>
        /// Creates a new mobile report model with the specified parameters
        /// </summary>
        /// <param name="id">The ID for the report model</param>
        /// <param name="userId">The user ID for the report model</param>
        /// <param name="text">The text content of the report</param>
        /// <param name="timestamp">The timestamp for the report</param>
        /// <param name="isSynced">The sync status of the report</param>
        /// <returns>A new ReportModel instance with the specified parameters</returns>
        public static ReportModel CreateMobileReportModel(
            int id, 
            string userId = null, 
            string text = null, 
            DateTime? timestamp = null, 
            bool isSynced = false)
        {
            var report = new ReportModel
            {
                Id = id,
                UserId = userId ?? TestConstants.TestUserId,
                Text = text ?? TestConstants.TestReportText,
                Timestamp = timestamp ?? DateTime.UtcNow,
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude,
                IsSynced = isSynced,
                RemoteId = isSynced ? $"report_{id}" : null
            };
            
            return report;
        }
        
        /// <summary>
        /// Creates a new backend report entity with the specified parameters
        /// </summary>
        /// <param name="id">The ID for the report</param>
        /// <param name="userId">The user ID for the report</param>
        /// <param name="text">The text content of the report</param>
        /// <param name="timestamp">The timestamp for the report</param>
        /// <param name="isSynced">The sync status of the report</param>
        /// <returns>A new Report instance with the specified parameters</returns>
        public static Report CreateBackendReport(
            int id, 
            string userId = null, 
            string text = null, 
            DateTime? timestamp = null, 
            bool isSynced = false)
        {
            var report = new Report
            {
                Id = id,
                UserId = userId ?? TestConstants.TestUserId,
                Text = text ?? TestConstants.TestReportText,
                Timestamp = timestamp ?? DateTime.UtcNow,
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude,
                IsSynced = isSynced,
                RemoteId = isSynced ? $"report_{id}" : null
            };
            
            return report;
        }
        
        /// <summary>
        /// Creates a new report request object for API testing
        /// </summary>
        /// <param name="text">The text content of the report</param>
        /// <param name="timestamp">The timestamp for the report</param>
        /// <param name="latitude">The latitude for the report location</param>
        /// <param name="longitude">The longitude for the report location</param>
        /// <returns>A new ReportRequest instance with the specified parameters</returns>
        public static ReportRequest CreateReportRequest(
            string text = null, 
            DateTime? timestamp = null, 
            double? latitude = null, 
            double? longitude = null)
        {
            var request = new ReportRequest
            {
                Text = text ?? TestConstants.TestReportText,
                Timestamp = timestamp ?? DateTime.UtcNow,
                Location = new LocationData
                {
                    Latitude = latitude ?? TestConstants.TestLatitude,
                    Longitude = longitude ?? TestConstants.TestLongitude
                }
            };
            
            return request;
        }
        
        /// <summary>
        /// Creates a new report response object for API testing
        /// </summary>
        /// <param name="id">The ID returned from the API</param>
        /// <param name="status">The status returned from the API</param>
        /// <returns>A new ReportResponse instance with the specified parameters</returns>
        public static ReportResponse CreateReportResponse(string id, string status)
        {
            return new ReportResponse
            {
                Id = id,
                Status = status
            };
        }
        
        /// <summary>
        /// Creates a list of report entities for testing
        /// </summary>
        /// <param name="count">The number of entities to create</param>
        /// <param name="allSynced">Whether all entities should be marked as synced</param>
        /// <returns>A list of ReportEntity instances with test values</returns>
        public static List<ReportEntity> CreateReportEntities(int count, bool allSynced = false)
        {
            var reports = new List<ReportEntity>();
            
            for (int i = 1; i <= count; i++)
            {
                var report = CreateMobileReportEntity(
                    id: i,
                    text: $"{TestConstants.TestReportText} {i}",
                    isSynced: allSynced || i % 2 == 0 // If not allSynced, alternate synced/not synced
                );
                
                reports.Add(report);
            }
            
            return reports;
        }
        
        /// <summary>
        /// Creates a list of report models for testing
        /// </summary>
        /// <param name="count">The number of models to create</param>
        /// <param name="allSynced">Whether all models should be marked as synced</param>
        /// <returns>A list of ReportModel instances with test values</returns>
        public static List<ReportModel> CreateReportModels(int count, bool allSynced = false)
        {
            var reports = new List<ReportModel>();
            
            for (int i = 1; i <= count; i++)
            {
                var report = CreateMobileReportModel(
                    id: i,
                    text: $"{TestConstants.TestReportText} {i}",
                    isSynced: allSynced || i % 2 == 0 // If not allSynced, alternate synced/not synced
                );
                
                reports.Add(report);
            }
            
            return reports;
        }
    }
}