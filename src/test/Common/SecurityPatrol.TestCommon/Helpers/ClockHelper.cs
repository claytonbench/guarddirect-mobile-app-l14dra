using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SecurityPatrol.Models;
using SecurityPatrol.Services;
using SecurityPatrol.TestCommon.Constants;
using SecurityPatrol.TestCommon.Data;
using SecurityPatrol.TestCommon.Mocks;

namespace SecurityPatrol.TestCommon.Helpers
{
    /// <summary>
    /// Static helper class that provides utility methods for testing clock-related functionality in the Security Patrol application.
    /// </summary>
    public static class ClockHelper
    {
        /// <summary>
        /// Sets up a mock time tracking service with a clocked-in state
        /// </summary>
        /// <param name="mockService">The mock service to set up</param>
        /// <param name="clockInTime">Optional specific clock-in time; defaults to 1 hour ago</param>
        public static void SetupClockedInState(MockTimeTrackingService mockService, DateTime? clockInTime = null)
        {
            clockInTime ??= DateTime.UtcNow.AddHours(-1);
            
            mockService.SetCurrentStatus(new ClockStatus
            {
                IsClocked = true,
                LastClockInTime = clockInTime,
                LastClockOutTime = null
            });
        }

        /// <summary>
        /// Sets up a mock time tracking service with a clocked-out state
        /// </summary>
        /// <param name="mockService">The mock service to set up</param>
        /// <param name="lastClockInTime">Optional last clock-in time; defaults to 8 hours ago</param>
        /// <param name="lastClockOutTime">Optional last clock-out time; defaults to now</param>
        public static void SetupClockedOutState(MockTimeTrackingService mockService, DateTime? lastClockInTime = null, DateTime? lastClockOutTime = null)
        {
            lastClockInTime ??= DateTime.UtcNow.AddHours(-8);
            lastClockOutTime ??= DateTime.UtcNow;
            
            mockService.SetCurrentStatus(new ClockStatus
            {
                IsClocked = false,
                LastClockInTime = lastClockInTime,
                LastClockOutTime = lastClockOutTime
            });
        }

        /// <summary>
        /// Sets up a mock time tracking service with a predefined history of time records
        /// </summary>
        /// <param name="mockService">The mock service to set up</param>
        /// <param name="daysOfHistory">Number of days of history to generate</param>
        /// <param name="endWithClockIn">Whether to end with a clock-in record (simulating currently clocked in)</param>
        /// <returns>The generated time records</returns>
        public static List<TimeRecordModel> SetupTimeRecordHistory(MockTimeTrackingService mockService, int daysOfHistory, bool endWithClockIn = false)
        {
            var timeRecords = new List<TimeRecordModel>();
            int recordId = 1;
            
            // Generate pairs of clock in/out records for each day
            for (int i = 0; i < daysOfHistory; i++)
            {
                var dayPair = TestTimeRecords.GenerateClockInOutPair(recordId, TestConstants.TestUserId, i);
                
                // Convert to models
                foreach (var entity in dayPair)
                {
                    timeRecords.Add(TimeRecordModel.FromEntity(entity));
                }
                
                recordId += 2;
            }
            
            // Add a final clock-in if requested (to simulate currently clocked in)
            if (endWithClockIn)
            {
                var clockInRecord = CreateClockInRecord(recordId, TestConstants.TestUserId, DateTime.UtcNow);
                timeRecords.Add(clockInRecord);
            }
            
            // Set the records in the mock service
            mockService.SetTimeRecords(timeRecords);
            
            // Update clock status based on the records
            var status = GetClockStatusFromRecords(timeRecords);
            mockService.SetCurrentStatus(status);
            
            return timeRecords;
        }

        /// <summary>
        /// Generates a sequence of alternating clock in and clock out records
        /// </summary>
        /// <param name="count">Number of records to generate</param>
        /// <param name="userId">Optional user ID</param>
        /// <param name="startTime">Optional start time for the first record</param>
        /// <param name="interval">Optional time interval between records</param>
        /// <returns>A list of alternating clock in/out records</returns>
        public static List<TimeRecordModel> GenerateClockInOutSequence(int count, string userId = null, DateTime? startTime = null, TimeSpan? interval = null)
        {
            userId ??= TestConstants.TestUserId;
            startTime ??= DateTime.UtcNow.AddHours(-count);
            interval ??= TimeSpan.FromHours(1);
            
            var records = new List<TimeRecordModel>();
            var currentTime = startTime.Value;
            
            for (int i = 0; i < count; i++)
            {
                var record = new TimeRecordModel
                {
                    Id = i + 1,
                    UserId = userId,
                    Type = i % 2 == 0 ? "ClockIn" : "ClockOut",
                    Timestamp = currentTime,
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                };
                
                records.Add(record);
                currentTime = currentTime.Add(interval.Value);
            }
            
            return records;
        }

        /// <summary>
        /// Simulates a clock-in operation with the specified time tracking service
        /// </summary>
        /// <param name="timeTrackingService">The time tracking service</param>
        /// <returns>The created clock-in record</returns>
        public static async Task<TimeRecordModel> SimulateClockIn(ITimeTrackingService timeTrackingService)
        {
            return await timeTrackingService.ClockIn();
        }

        /// <summary>
        /// Simulates a clock-out operation with the specified time tracking service
        /// </summary>
        /// <param name="timeTrackingService">The time tracking service</param>
        /// <returns>The created clock-out record</returns>
        public static async Task<TimeRecordModel> SimulateClockOut(ITimeTrackingService timeTrackingService)
        {
            return await timeTrackingService.ClockOut();
        }

        /// <summary>
        /// Simulates a complete shift with clock-in and clock-out operations
        /// </summary>
        /// <param name="timeTrackingService">The time tracking service</param>
        /// <param name="shiftDuration">Optional duration of the shift</param>
        /// <returns>A tuple containing the clock-in and clock-out records</returns>
        public static async Task<(TimeRecordModel clockIn, TimeRecordModel clockOut)> SimulateFullShift(
            ITimeTrackingService timeTrackingService, TimeSpan? shiftDuration = null)
        {
            shiftDuration ??= TimeSpan.FromHours(8);
            
            var clockInRecord = await SimulateClockIn(timeTrackingService);
            var clockOutRecord = await SimulateClockOut(timeTrackingService);
            
            return (clockInRecord, clockOutRecord);
        }

        /// <summary>
        /// Creates a clock-in record with the specified parameters
        /// </summary>
        /// <param name="id">The record ID</param>
        /// <param name="userId">Optional user ID</param>
        /// <param name="timestamp">Optional timestamp</param>
        /// <param name="latitude">Optional latitude</param>
        /// <param name="longitude">Optional longitude</param>
        /// <returns>A new clock-in record</returns>
        public static TimeRecordModel CreateClockInRecord(int id, string userId = null, DateTime? timestamp = null, 
            double latitude = 0, double longitude = 0)
        {
            userId ??= TestConstants.TestUserId;
            timestamp ??= DateTime.UtcNow.AddHours(-8); // Default to 8 hours ago
            latitude = latitude == 0 ? TestConstants.TestLatitude : latitude;
            longitude = longitude == 0 ? TestConstants.TestLongitude : longitude;
            
            return new TimeRecordModel
            {
                Id = id,
                UserId = userId,
                Type = "ClockIn",
                Timestamp = timestamp.Value,
                Latitude = latitude,
                Longitude = longitude
            };
        }

        /// <summary>
        /// Creates a clock-out record with the specified parameters
        /// </summary>
        /// <param name="id">The record ID</param>
        /// <param name="userId">Optional user ID</param>
        /// <param name="timestamp">Optional timestamp</param>
        /// <param name="latitude">Optional latitude</param>
        /// <param name="longitude">Optional longitude</param>
        /// <returns>A new clock-out record</returns>
        public static TimeRecordModel CreateClockOutRecord(int id, string userId = null, DateTime? timestamp = null,
            double latitude = 0, double longitude = 0)
        {
            userId ??= TestConstants.TestUserId;
            timestamp ??= DateTime.UtcNow; // Default to now
            latitude = latitude == 0 ? TestConstants.TestLatitude : latitude;
            longitude = longitude == 0 ? TestConstants.TestLongitude : longitude;
            
            return new TimeRecordModel
            {
                Id = id,
                UserId = userId,
                Type = "ClockOut",
                Timestamp = timestamp.Value,
                Latitude = latitude,
                Longitude = longitude
            };
        }

        /// <summary>
        /// Determines the current clock status based on a list of time records
        /// </summary>
        /// <param name="timeRecords">The time records to analyze</param>
        /// <returns>The derived clock status</returns>
        public static ClockStatus GetClockStatusFromRecords(List<TimeRecordModel> timeRecords)
        {
            var status = new ClockStatus();
            
            if (timeRecords == null || timeRecords.Count == 0)
            {
                return status; // Default not clocked in
            }
            
            // Sort records by timestamp in descending order (most recent first)
            var sortedRecords = timeRecords.OrderByDescending(r => r.Timestamp).ToList();
            
            // Most recent record determines if currently clocked in
            var mostRecentRecord = sortedRecords.First();
            status.IsClocked = mostRecentRecord.Type == "ClockIn";
            
            // Find the most recent clock in record for LastClockInTime
            var mostRecentClockIn = sortedRecords.FirstOrDefault(r => r.Type == "ClockIn");
            if (mostRecentClockIn != null)
            {
                status.LastClockInTime = mostRecentClockIn.Timestamp;
            }
            
            // Find the most recent clock out record for LastClockOutTime
            var mostRecentClockOut = sortedRecords.FirstOrDefault(r => r.Type == "ClockOut");
            if (mostRecentClockOut != null)
            {
                status.LastClockOutTime = mostRecentClockOut.Timestamp;
            }
            
            return status;
        }

        /// <summary>
        /// Calculates the duration of a shift based on clock-in and clock-out records
        /// </summary>
        /// <param name="clockInRecord">The clock-in record</param>
        /// <param name="clockOutRecord">The clock-out record</param>
        /// <returns>The duration of the shift</returns>
        /// <exception cref="ArgumentException">Thrown if records are invalid</exception>
        public static TimeSpan CalculateShiftDuration(TimeRecordModel clockInRecord, TimeRecordModel clockOutRecord)
        {
            if (clockInRecord.Type != "ClockIn")
            {
                throw new ArgumentException("First record must be a clock-in record", nameof(clockInRecord));
            }
            
            if (clockOutRecord.Type != "ClockOut")
            {
                throw new ArgumentException("Second record must be a clock-out record", nameof(clockOutRecord));
            }
            
            if (clockOutRecord.Timestamp <= clockInRecord.Timestamp)
            {
                throw new ArgumentException("Clock-out time must be after clock-in time");
            }
            
            return clockOutRecord.Timestamp - clockInRecord.Timestamp;
        }

        /// <summary>
        /// Waits for a clock status change event from the time tracking service
        /// </summary>
        /// <param name="timeTrackingService">The time tracking service</param>
        /// <param name="timeout">Optional timeout</param>
        /// <returns>A task that completes when the status changes, returning the new status</returns>
        public static async Task<ClockStatus> WaitForClockStatusChange(ITimeTrackingService timeTrackingService, TimeSpan? timeout = null)
        {
            timeout ??= TimeSpan.FromSeconds(5);
            
            var tcs = new TaskCompletionSource<ClockStatus>();
            
            void StatusChangedHandler(object sender, ClockStatusChangedEventArgs e)
            {
                tcs.TrySetResult(e.Status);
            }
            
            timeTrackingService.StatusChanged += StatusChangedHandler;
            
            using var cts = new CancellationTokenSource(timeout.Value);
            cts.Token.Register(() => tcs.TrySetException(new TimeoutException("Timed out waiting for clock status change")));
            
            try
            {
                return await tcs.Task;
            }
            finally
            {
                timeTrackingService.StatusChanged -= StatusChangedHandler;
            }
        }
    }
}