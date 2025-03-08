using System;  // Version 8.0.0
using System.Threading.Tasks;  // Version 8.0.0
using System.Collections.Generic;  // Version 8.0.0
using SecurityPatrol.Services;
using SecurityPatrol.Models;

namespace SecurityPatrol.UnitTests.Helpers.MockServices
{
    /// <summary>
    /// Mock implementation of ITimeTrackingService for unit testing that provides
    /// configurable responses for time tracking operations without requiring actual
    /// database or location services.
    /// </summary>
    public class MockTimeTrackingService : ITimeTrackingService
    {
        /// <summary>
        /// Event raised when the clock status changes.
        /// </summary>
        public event EventHandler<ClockStatusChangedEventArgs> StatusChanged;

        // Current state
        private ClockStatus _currentStatus;
        private List<TimeRecordModel> _timeRecords;

        // Configurable behaviors
        public bool ClockInResult { get; private set; }
        public bool ClockOutResult { get; private set; }
        public bool ShouldThrowException { get; private set; }
        public Exception ExceptionToThrow { get; private set; }

        // Call tracking
        public int ClockInCallCount { get; private set; }
        public int ClockOutCallCount { get; private set; }
        public int GetCurrentStatusCallCount { get; private set; }
        public int GetHistoryCallCount { get; private set; }

        // Current user configuration
        public string CurrentUserId { get; private set; }

        /// <summary>
        /// Initializes a new instance of the MockTimeTrackingService class with default settings.
        /// </summary>
        public MockTimeTrackingService()
        {
            _currentStatus = new ClockStatus();
            _timeRecords = new List<TimeRecordModel>();
            ClockInResult = true;
            ClockOutResult = true;
            ShouldThrowException = false;
            ExceptionToThrow = null;
            ClockInCallCount = 0;
            ClockOutCallCount = 0;
            GetCurrentStatusCallCount = 0;
            GetHistoryCallCount = 0;
            CurrentUserId = "test-user-id";
        }

        /// <summary>
        /// Mocks clocking in a user with the current timestamp and location.
        /// </summary>
        /// <returns>A mock time record for the clock-in event.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the user is already clocked in.</exception>
        public async Task<TimeRecordModel> ClockIn()
        {
            if (ShouldThrowException && ExceptionToThrow != null)
            {
                throw ExceptionToThrow;
            }

            if (_currentStatus.IsClocked)
            {
                throw new InvalidOperationException("User is already clocked in.");
            }

            ClockInCallCount++;

            if (!ClockInResult)
            {
                return null;
            }

            var record = TimeRecordModel.CreateClockIn(CurrentUserId, 0, 0);
            _timeRecords.Add(record);

            _currentStatus.IsClocked = true;
            _currentStatus.LastClockInTime = record.Timestamp;

            OnStatusChanged();

            return await Task.FromResult(record);
        }

        /// <summary>
        /// Mocks clocking out a user with the current timestamp and location.
        /// </summary>
        /// <returns>A mock time record for the clock-out event.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the user is not clocked in.</exception>
        public async Task<TimeRecordModel> ClockOut()
        {
            if (ShouldThrowException && ExceptionToThrow != null)
            {
                throw ExceptionToThrow;
            }

            if (!_currentStatus.IsClocked)
            {
                throw new InvalidOperationException("User is not clocked in.");
            }

            ClockOutCallCount++;

            if (!ClockOutResult)
            {
                return null;
            }

            var record = TimeRecordModel.CreateClockOut(CurrentUserId, 0, 0);
            _timeRecords.Add(record);

            _currentStatus.IsClocked = false;
            _currentStatus.LastClockOutTime = record.Timestamp;

            OnStatusChanged();

            return await Task.FromResult(record);
        }

        /// <summary>
        /// Mocks retrieving the current clock status.
        /// </summary>
        /// <returns>A clone of the current mock clock status.</returns>
        public async Task<ClockStatus> GetCurrentStatus()
        {
            if (ShouldThrowException && ExceptionToThrow != null)
            {
                throw ExceptionToThrow;
            }

            GetCurrentStatusCallCount++;
            
            // Return a clone to prevent external modification
            return await Task.FromResult(_currentStatus.Clone());
        }

        /// <summary>
        /// Mocks retrieving the time tracking history with a specified number of records.
        /// </summary>
        /// <param name="count">The maximum number of records to retrieve.</param>
        /// <returns>A collection of mock time records.</returns>
        /// <exception cref="ArgumentException">Thrown when count is less than or equal to zero.</exception>
        public async Task<IEnumerable<TimeRecordModel>> GetHistory(int count)
        {
            if (ShouldThrowException && ExceptionToThrow != null)
            {
                throw ExceptionToThrow;
            }

            if (count <= 0)
            {
                throw new ArgumentException("Count must be greater than zero.", nameof(count));
            }

            GetHistoryCallCount++;

            // Sort by newest first
            _timeRecords.Sort((a, b) => b.Timestamp.CompareTo(a.Timestamp));
            var records = _timeRecords.Take(Math.Min(count, _timeRecords.Count));

            return await Task.FromResult(records);
        }

        /// <summary>
        /// Raises the StatusChanged event with the current status.
        /// </summary>
        protected virtual void OnStatusChanged()
        {
            var status = _currentStatus.Clone();
            var args = new ClockStatusChangedEventArgs(status);
            StatusChanged?.Invoke(this, args);
        }

        /// <summary>
        /// Configures the result for the ClockIn method.
        /// </summary>
        /// <param name="result">The result value to return.</param>
        public void SetupClockInResult(bool result)
        {
            ClockInResult = result;
        }

        /// <summary>
        /// Configures the result for the ClockOut method.
        /// </summary>
        /// <param name="result">The result value to return.</param>
        public void SetupClockOutResult(bool result)
        {
            ClockOutResult = result;
        }

        /// <summary>
        /// Configures the current clock status.
        /// </summary>
        /// <param name="status">The status to set.</param>
        public void SetupCurrentStatus(ClockStatus status)
        {
            _currentStatus = status;
        }

        /// <summary>
        /// Configures the time records history.
        /// </summary>
        /// <param name="records">The records to set.</param>
        public void SetupTimeRecords(List<TimeRecordModel> records)
        {
            _timeRecords.Clear();
            _timeRecords.AddRange(records);
        }

        /// <summary>
        /// Configures an exception to be thrown by any method.
        /// </summary>
        /// <param name="exception">The exception to throw.</param>
        public void SetupException(Exception exception)
        {
            ShouldThrowException = true;
            ExceptionToThrow = exception;
        }

        /// <summary>
        /// Clears any configured exception.
        /// </summary>
        public void ClearException()
        {
            ShouldThrowException = false;
            ExceptionToThrow = null;
        }

        /// <summary>
        /// Configures the user ID to be used for time records.
        /// </summary>
        /// <param name="userId">The user ID to use.</param>
        public void SetupUserId(string userId)
        {
            CurrentUserId = userId;
        }

        /// <summary>
        /// Verifies that ClockIn was called.
        /// </summary>
        /// <returns>True if the method was called, otherwise false.</returns>
        public bool VerifyClockInCalled()
        {
            return ClockInCallCount > 0;
        }

        /// <summary>
        /// Verifies that ClockOut was called.
        /// </summary>
        /// <returns>True if the method was called, otherwise false.</returns>
        public bool VerifyClockOutCalled()
        {
            return ClockOutCallCount > 0;
        }

        /// <summary>
        /// Verifies that GetCurrentStatus was called.
        /// </summary>
        /// <returns>True if the method was called, otherwise false.</returns>
        public bool VerifyGetCurrentStatusCalled()
        {
            return GetCurrentStatusCallCount > 0;
        }

        /// <summary>
        /// Verifies that GetHistory was called.
        /// </summary>
        /// <returns>True if the method was called, otherwise false.</returns>
        public bool VerifyGetHistoryCalled()
        {
            return GetHistoryCallCount > 0;
        }

        /// <summary>
        /// Resets all configurations and call history.
        /// </summary>
        public void Reset()
        {
            _currentStatus = new ClockStatus();
            _timeRecords.Clear();
            ClockInResult = true;
            ClockOutResult = true;
            ShouldThrowException = false;
            ExceptionToThrow = null;
            ClockInCallCount = 0;
            ClockOutCallCount = 0;
            GetCurrentStatusCallCount = 0;
            GetHistoryCallCount = 0;
            CurrentUserId = "test-user-id";
        }
    }
}