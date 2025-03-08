using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using SecurityPatrol.Services;
using SecurityPatrol.Models;
using SecurityPatrol.TestCommon.Constants;
using SecurityPatrol.TestCommon.Data;

namespace SecurityPatrol.TestCommon.Mocks
{
    /// <summary>
    /// Mock implementation of ITimeTrackingService for testing purposes that simulates time tracking functionality
    /// without accessing actual repositories or backend services.
    /// </summary>
    public class MockTimeTrackingService : ITimeTrackingService
    {
        /// <summary>
        /// Event that is raised when the clock status changes.
        /// </summary>
        public event EventHandler<ClockStatusChangedEventArgs> StatusChanged;

        private ClockStatus _currentStatus;
        private List<TimeRecordModel> _timeRecords;

        /// <summary>
        /// Gets or sets a value indicating whether service operations should succeed.
        /// </summary>
        public bool ShouldSucceed { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether service operations should throw exceptions.
        /// </summary>
        public bool ShouldThrowException { get; set; }

        /// <summary>
        /// Gets the last clock-in record created by the mock service.
        /// </summary>
        public TimeRecordModel LastClockInRecord { get; private set; }

        /// <summary>
        /// Gets the last clock-out record created by the mock service.
        /// </summary>
        public TimeRecordModel LastClockOutRecord { get; private set; }

        /// <summary>
        /// Initializes a new instance of the MockTimeTrackingService class with default test values.
        /// </summary>
        public MockTimeTrackingService()
        {
            _currentStatus = new ClockStatus();
            _timeRecords = new List<TimeRecordModel>();
            ShouldSucceed = true;
            ShouldThrowException = false;
            LastClockInRecord = null;
            LastClockOutRecord = null;
        }

        /// <summary>
        /// Simulates recording a clock-in event with the current timestamp and location.
        /// </summary>
        /// <returns>A task that returns the created time record.</returns>
        /// <exception cref="Exception">Thrown when ShouldThrowException is true.</exception>
        /// <exception cref="InvalidOperationException">Thrown when ShouldSucceed is false or when already clocked in.</exception>
        public async Task<TimeRecordModel> ClockIn()
        {
            if (ShouldThrowException)
            {
                throw new Exception("Simulated exception in ClockIn");
            }

            if (!ShouldSucceed)
            {
                throw new InvalidOperationException("Simulated failure in ClockIn");
            }

            if (_currentStatus.IsClocked)
            {
                throw new InvalidOperationException("Already clocked in");
            }

            var record = TimeRecordModel.CreateClockIn(
                TestConstants.TestUserId,
                TestConstants.TestLatitude,
                TestConstants.TestLongitude);

            record.Id = _timeRecords.Count + 1;
            _timeRecords.Add(record);
            LastClockInRecord = record;

            _currentStatus.IsClocked = true;
            _currentStatus.LastClockInTime = record.Timestamp;
            
            OnStatusChanged();

            return record;
        }

        /// <summary>
        /// Simulates recording a clock-out event with the current timestamp and location.
        /// </summary>
        /// <returns>A task that returns the created time record.</returns>
        /// <exception cref="Exception">Thrown when ShouldThrowException is true.</exception>
        /// <exception cref="InvalidOperationException">Thrown when ShouldSucceed is false or when not clocked in.</exception>
        public async Task<TimeRecordModel> ClockOut()
        {
            if (ShouldThrowException)
            {
                throw new Exception("Simulated exception in ClockOut");
            }

            if (!ShouldSucceed)
            {
                throw new InvalidOperationException("Simulated failure in ClockOut");
            }

            if (!_currentStatus.IsClocked)
            {
                throw new InvalidOperationException("Not clocked in");
            }

            var record = TimeRecordModel.CreateClockOut(
                TestConstants.TestUserId,
                TestConstants.TestLatitude,
                TestConstants.TestLongitude);

            record.Id = _timeRecords.Count + 1;
            _timeRecords.Add(record);
            LastClockOutRecord = record;

            _currentStatus.IsClocked = false;
            _currentStatus.LastClockOutTime = record.Timestamp;
            
            OnStatusChanged();

            return record;
        }

        /// <summary>
        /// Gets the current clock status.
        /// </summary>
        /// <returns>A task that returns the current clock status.</returns>
        /// <exception cref="Exception">Thrown when ShouldThrowException is true.</exception>
        public async Task<ClockStatus> GetCurrentStatus()
        {
            if (ShouldThrowException)
            {
                throw new Exception("Simulated exception in GetCurrentStatus");
            }

            return _currentStatus.Clone();
        }

        /// <summary>
        /// Gets the time tracking history with a specified number of records.
        /// </summary>
        /// <param name="count">The maximum number of records to retrieve.</param>
        /// <returns>A task that returns a collection of time records.</returns>
        /// <exception cref="Exception">Thrown when ShouldThrowException is true.</exception>
        /// <exception cref="ArgumentException">Thrown when count is less than or equal to zero.</exception>
        public async Task<IEnumerable<TimeRecordModel>> GetHistory(int count)
        {
            if (ShouldThrowException)
            {
                throw new Exception("Simulated exception in GetHistory");
            }

            if (count <= 0)
            {
                throw new ArgumentException("Count must be greater than zero", nameof(count));
            }

            return _timeRecords.Take(count);
        }

        /// <summary>
        /// Raises the StatusChanged event with the current status.
        /// </summary>
        protected virtual void OnStatusChanged()
        {
            var statusClone = _currentStatus.Clone();
            var args = new ClockStatusChangedEventArgs(statusClone);
            StatusChanged?.Invoke(this, args);
        }

        /// <summary>
        /// Sets the current clock status for testing scenarios.
        /// </summary>
        /// <param name="status">The clock status to set.</param>
        public void SetCurrentStatus(ClockStatus status)
        {
            if (status == null)
            {
                throw new ArgumentNullException(nameof(status));
            }

            _currentStatus = status;
            OnStatusChanged();
        }

        /// <summary>
        /// Sets the time records collection for testing scenarios.
        /// </summary>
        /// <param name="records">The time records to set.</param>
        public void SetTimeRecords(IEnumerable<TimeRecordModel> records)
        {
            _timeRecords.Clear();
            if (records != null)
            {
                _timeRecords.AddRange(records);
            }
        }

        /// <summary>
        /// Resets the mock service to its initial state.
        /// </summary>
        public void Reset()
        {
            _currentStatus = new ClockStatus();
            _timeRecords.Clear();
            ShouldSucceed = true;
            ShouldThrowException = false;
            LastClockInRecord = null;
            LastClockOutRecord = null;
        }
    }
}