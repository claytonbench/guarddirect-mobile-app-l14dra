using System;

namespace SecurityPatrol.Models
{
    /// <summary>
    /// Model class that represents the current clock status of a security patrol officer.
    /// </summary>
    public class ClockStatus
    {
        /// <summary>
        /// Gets or sets a value indicating whether the officer is currently clocked in.
        /// </summary>
        public bool IsClocked { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the last clock-in event.
        /// </summary>
        public DateTime? LastClockInTime { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the last clock-out event.
        /// </summary>
        public DateTime? LastClockOutTime { get; set; }

        /// <summary>
        /// Default constructor for ClockStatus.
        /// </summary>
        public ClockStatus()
        {
            IsClocked = false;
            LastClockInTime = null;
            LastClockOutTime = null;
        }

        /// <summary>
        /// Creates a deep copy of the current ClockStatus object.
        /// </summary>
        /// <returns>A new ClockStatus object with the same property values.</returns>
        public ClockStatus Clone()
        {
            return new ClockStatus
            {
                IsClocked = this.IsClocked,
                LastClockInTime = this.LastClockInTime,
                LastClockOutTime = this.LastClockOutTime
            };
        }
    }
}