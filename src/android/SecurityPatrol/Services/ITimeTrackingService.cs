using System;  // Version 8.0+
using System.Threading.Tasks;  // Version 8.0+
using System.Collections.Generic;  // Version 8.0+
using SecurityPatrol.Models;

namespace SecurityPatrol.Services
{
    /// <summary>
    /// Interface that defines the contract for time tracking services in the Security Patrol application.
    /// This interface provides methods for clock in/out operations, retrieving current clock status,
    /// and accessing time tracking history. It also includes an event for notifying subscribers when the
    /// clock status changes.
    /// </summary>
    public interface ITimeTrackingService
    {
        /// <summary>
        /// Event that is raised when the clock status changes.
        /// </summary>
        event EventHandler<ClockStatusChangedEventArgs> StatusChanged;

        /// <summary>
        /// Records a clock-in event with the current timestamp and location.
        /// </summary>
        /// <returns>A task that returns the created time record.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the user is already clocked in.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown when the user is not authenticated.</exception>
        Task<TimeRecordModel> ClockIn();

        /// <summary>
        /// Records a clock-out event with the current timestamp and location.
        /// </summary>
        /// <returns>A task that returns the created time record.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the user is not clocked in.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown when the user is not authenticated.</exception>
        Task<TimeRecordModel> ClockOut();

        /// <summary>
        /// Gets the current clock status.
        /// </summary>
        /// <returns>A task that returns the current clock status.</returns>
        Task<ClockStatus> GetCurrentStatus();

        /// <summary>
        /// Gets the time tracking history with a specified number of records.
        /// </summary>
        /// <param name="count">The maximum number of records to retrieve.</param>
        /// <returns>A task that returns a collection of time records.</returns>
        /// <exception cref="ArgumentException">Thrown when count is less than or equal to zero.</exception>
        Task<IEnumerable<TimeRecordModel>> GetHistory(int count);
    }
}