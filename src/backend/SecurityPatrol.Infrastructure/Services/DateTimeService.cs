using System; // Version: 8.0.0
using SecurityPatrol.Core.Interfaces;

namespace SecurityPatrol.Infrastructure.Services
{
    /// <summary>
    /// Service that provides access to system date and time information, implementing the IDateTime interface.
    /// This service ensures consistent timestamp handling throughout the application and supports
    /// audit trails, entity timestamps, and time-dependent operations.
    /// </summary>
    public class DateTimeService : IDateTime
    {
        /// <summary>
        /// Gets the current local date and time.
        /// </summary>
        /// <returns>A <see cref="DateTime"/> representing the current local date and time.</returns>
        public DateTime Now()
        {
            return DateTime.Now;
        }

        /// <summary>
        /// Gets the current UTC date and time.
        /// </summary>
        /// <returns>A <see cref="DateTime"/> representing the current UTC date and time.</returns>
        /// <remarks>
        /// UTC time should be used for all timestamps that may be accessed across different time zones
        /// or that require absolute time references.
        /// </remarks>
        public DateTime UtcNow()
        {
            return DateTime.UtcNow;
        }

        /// <summary>
        /// Gets the current date with time component set to midnight.
        /// </summary>
        /// <returns>A <see cref="DateTime"/> representing the current date with time set to 00:00:00.</returns>
        /// <remarks>
        /// This is useful for date-only comparisons or when the time component is not relevant.
        /// </remarks>
        public DateTime Today()
        {
            return DateTime.Today;
        }
    }
}