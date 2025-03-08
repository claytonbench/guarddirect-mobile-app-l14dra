using System;

namespace SecurityPatrol.Models
{
    /// <summary>
    /// Event arguments class for clock status change events.
    /// This class is used to pass the updated clock status when the StatusChanged event
    /// is raised by the TimeTrackingService.
    /// </summary>
    public class ClockStatusChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the updated clock status.
        /// </summary>
        public ClockStatus Status { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClockStatusChangedEventArgs"/> class
        /// with the specified clock status.
        /// </summary>
        /// <param name="status">The updated clock status.</param>
        /// <exception cref="ArgumentNullException">Thrown if status is null.</exception>
        public ClockStatusChangedEventArgs(ClockStatus status)
            : base()
        {
            Status = status ?? throw new ArgumentNullException(nameof(status));
        }
    }
}