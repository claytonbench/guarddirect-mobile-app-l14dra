using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;
using FluentAssertions;
using BenchmarkDotNet.Attributes;
using SecurityPatrol.API.PerformanceTests.Setup;
using SecurityPatrol.Infrastructure.Persistence;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Infrastructure.Persistence.Repositories;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.TestCommon.Constants;

namespace SecurityPatrol.API.PerformanceTests.Database
{
    /// <summary>
    /// Performance tests for database operations in the Security Patrol API, focusing on repository methods and query performance.
    /// </summary>
    public class DatabasePerformanceTests : PerformanceTestBase
    {
        private readonly ITimeRecordRepository _timeRecordRepository;
        private readonly ILocationRecordRepository _locationRecordRepository;
        private readonly SecurityPatrolDbContext _dbContext;
        private readonly double DatabasePerformanceThreshold;

        /// <summary>
        /// Initializes a new instance of the DatabasePerformanceTests class with the test factory and output helper.
        /// </summary>
        /// <param name="outputHelper">The test output helper for logging test results.</param>
        public DatabasePerformanceTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
            // Get the database context from the test server's service provider
            var scope = Factory.Services.CreateScope();
            _dbContext = scope.ServiceProvider.GetRequiredService<SecurityPatrolDbContext>();
            
            // Create repositories
            _timeRecordRepository = new TimeRecordRepository(_dbContext);
            _locationRecordRepository = new LocationRecordRepository(_dbContext);
            
            // Set performance threshold for database operations (in milliseconds)
            DatabasePerformanceThreshold = 100; // Database operations should complete within 100ms
        }

        /// <summary>
        /// Tests the performance of retrieving all time records.
        /// </summary>
        [Fact]
        public async Task TimeRecordRepository_GetAllAsync_Performance()
        {
            // Measure the execution time
            var executionTime = await MeasureExecutionTime(() => _timeRecordRepository.GetAllAsync());
            
            // Assert that the execution time is within acceptable limits
            AssertPerformance(executionTime, DatabasePerformanceThreshold, nameof(TimeRecordRepository_GetAllAsync_Performance));
        }

        /// <summary>
        /// Tests the performance of retrieving time records for a specific user.
        /// </summary>
        [Fact]
        public async Task TimeRecordRepository_GetByUserIdAsync_Performance()
        {
            // Measure the execution time
            var executionTime = await MeasureExecutionTime(() => _timeRecordRepository.GetByUserIdAsync(TestConstants.TestUserId));
            
            // Assert that the execution time is within acceptable limits
            AssertPerformance(executionTime, DatabasePerformanceThreshold, nameof(TimeRecordRepository_GetByUserIdAsync_Performance));
        }

        /// <summary>
        /// Tests the performance of retrieving time records for a specific user within a date range.
        /// </summary>
        [Fact]
        public async Task TimeRecordRepository_GetByUserIdAndDateRangeAsync_Performance()
        {
            // Define a date range (last 30 days)
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-30);
            
            // Measure the execution time
            var executionTime = await MeasureExecutionTime(() => 
                _timeRecordRepository.GetByUserIdAndDateRangeAsync(TestConstants.TestUserId, startDate, endDate));
            
            // Assert that the execution time is within acceptable limits
            AssertPerformance(executionTime, DatabasePerformanceThreshold, nameof(TimeRecordRepository_GetByUserIdAndDateRangeAsync_Performance));
        }

        /// <summary>
        /// Tests the performance of adding a new time record.
        /// </summary>
        [Fact]
        public async Task TimeRecordRepository_AddAsync_Performance()
        {
            // Create a test time record
            var timeRecord = CreateTestTimeRecord();
            
            // Measure the execution time
            var executionTime = await MeasureExecutionTime(() => _timeRecordRepository.AddAsync(timeRecord));
            
            // Assert that the execution time is within acceptable limits
            AssertPerformance(executionTime, DatabasePerformanceThreshold, nameof(TimeRecordRepository_AddAsync_Performance));
        }

        /// <summary>
        /// Tests the performance of retrieving location records for a specific user.
        /// </summary>
        [Fact]
        public async Task LocationRecordRepository_GetByUserIdAsync_Performance()
        {
            // Measure the execution time
            var executionTime = await MeasureExecutionTime(() => 
                _locationRecordRepository.GetByUserIdAsync(TestConstants.TestUserId, 100));
            
            // Assert that the execution time is within acceptable limits
            AssertPerformance(executionTime, DatabasePerformanceThreshold, nameof(LocationRecordRepository_GetByUserIdAsync_Performance));
        }

        /// <summary>
        /// Tests the performance of retrieving location records for a specific user within a time range.
        /// </summary>
        [Fact]
        public async Task LocationRecordRepository_GetByUserIdAndTimeRangeAsync_Performance()
        {
            // Define a time range (last 24 hours)
            var endTime = DateTime.UtcNow;
            var startTime = endTime.AddHours(-24);
            
            // Measure the execution time
            var executionTime = await MeasureExecutionTime(() => 
                _locationRecordRepository.GetByUserIdAndTimeRangeAsync(TestConstants.TestUserId, startTime, endTime));
            
            // Assert that the execution time is within acceptable limits
            AssertPerformance(executionTime, DatabasePerformanceThreshold, nameof(LocationRecordRepository_GetByUserIdAndTimeRangeAsync_Performance));
        }

        /// <summary>
        /// Tests the performance of adding multiple location records in a batch operation.
        /// </summary>
        [Fact]
        public async Task LocationRecordRepository_AddRangeAsync_Performance()
        {
            // Create a list of test location records (50 records)
            var locationRecords = CreateTestLocationRecords(50);
            
            // Measure the execution time
            var executionTime = await MeasureExecutionTime(() => _locationRecordRepository.AddRangeAsync(locationRecords));
            
            // For batch operations, we allow a higher threshold (2x standard)
            AssertPerformance(executionTime, DatabasePerformanceThreshold * 2, nameof(LocationRecordRepository_AddRangeAsync_Performance));
        }

        /// <summary>
        /// Tests the performance of a raw SQL query for time records.
        /// </summary>
        [Fact]
        public async Task DbContext_RawQuery_TimeRecords_Performance()
        {
            // Measure the execution time of a raw query
            var executionTime = await MeasureExecutionTime(() => 
                _dbContext.TimeRecords.AsNoTracking().ToListAsync());
            
            // Assert that the execution time is within acceptable limits
            AssertPerformance(executionTime, DatabasePerformanceThreshold, nameof(DbContext_RawQuery_TimeRecords_Performance));
        }

        /// <summary>
        /// Tests the performance of a raw SQL query for location records.
        /// </summary>
        [Fact]
        public async Task DbContext_RawQuery_LocationRecords_Performance()
        {
            // Measure the execution time of a raw query
            var executionTime = await MeasureExecutionTime(() => 
                _dbContext.LocationRecords.AsNoTracking().Take(100).ToListAsync());
            
            // Assert that the execution time is within acceptable limits
            AssertPerformance(executionTime, DatabasePerformanceThreshold, nameof(DbContext_RawQuery_LocationRecords_Performance));
        }

        /// <summary>
        /// Tests the performance of a filtered query for time records.
        /// </summary>
        [Fact]
        public async Task DbContext_FilteredQuery_TimeRecords_Performance()
        {
            // Measure the execution time of a filtered query
            var executionTime = await MeasureExecutionTime(() => 
                _dbContext.TimeRecords.Where(t => t.UserId == TestConstants.TestUserId).AsNoTracking().ToListAsync());
            
            // Assert that the execution time is within acceptable limits
            AssertPerformance(executionTime, DatabasePerformanceThreshold, nameof(DbContext_FilteredQuery_TimeRecords_Performance));
        }

        /// <summary>
        /// Tests the performance of a filtered query for location records.
        /// </summary>
        [Fact]
        public async Task DbContext_FilteredQuery_LocationRecords_Performance()
        {
            // Measure the execution time of a filtered query
            var executionTime = await MeasureExecutionTime(() => 
                _dbContext.LocationRecords.Where(l => l.UserId == TestConstants.TestUserId).AsNoTracking().Take(100).ToListAsync());
            
            // Assert that the execution time is within acceptable limits
            AssertPerformance(executionTime, DatabasePerformanceThreshold, nameof(DbContext_FilteredQuery_LocationRecords_Performance));
        }

        /// <summary>
        /// Tests the performance of concurrent database queries.
        /// </summary>
        [Fact]
        public async Task DbContext_ConcurrentQueries_Performance()
        {
            // Create a function to execute for measuring concurrent performance
            Func<Task<List<TimeRecord>>> queryFunc = () => 
                _dbContext.TimeRecords.Where(t => t.UserId == TestConstants.TestUserId).AsNoTracking().ToListAsync();
            
            // Run 10 concurrent requests
            var results = await RunConcurrentRequests(queryFunc, 10);
            
            // Extract execution times
            var executionTimes = results.Select(r => r.elapsedMilliseconds).ToList();
            
            // Assert that the 95th percentile execution time is within acceptable limits (2x standard for concurrent operations)
            AssertPerformance(executionTimes, DatabasePerformanceThreshold * 2, nameof(DbContext_ConcurrentQueries_Performance));
        }

        /// <summary>
        /// Creates a test time record with predefined values.
        /// </summary>
        private TimeRecord CreateTestTimeRecord()
        {
            return new TimeRecord
            {
                UserId = TestConstants.TestUserId,
                Type = "in",
                Timestamp = DateTime.UtcNow,
                Latitude = 34.0522,
                Longitude = -118.2437
            };
        }

        /// <summary>
        /// Creates a list of test location records with predefined values.
        /// </summary>
        private List<LocationRecord> CreateTestLocationRecords(int count)
        {
            var records = new List<LocationRecord>();
            
            for (int i = 0; i < count; i++)
            {
                records.Add(new LocationRecord
                {
                    UserId = TestConstants.TestUserId,
                    Timestamp = DateTime.UtcNow.AddSeconds(-i),
                    Latitude = 34.0522 + (i * 0.0001),
                    Longitude = -118.2437 + (i * 0.0001),
                    Accuracy = 10.0
                });
            }
            
            return records;
        }
    }
}