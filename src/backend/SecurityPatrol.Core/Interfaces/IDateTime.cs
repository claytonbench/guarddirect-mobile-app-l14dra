using System;

namespace SecurityPatrol.Core.Interfaces
{
    /// <summary>
    /// Provides an abstraction for date and time operations throughout the application.
    /// This interface enables consistent timestamp handling across the application and
    /// facilitates unit testing by allowing system time to be substituted with controlled values.
    /// </summary>
    /// <remarks>
    /// This abstraction is particularly useful for:
    /// - Creating consistent timestamps for audit logging
    /// - Supporting entity tracking with creation and modification timestamps
    /// - Enabling deterministic testing of time-dependent functionality
    /// </remarks>
    public interface IDateTime
    {
        /// <summary>
        /// Gets the current local date and time.
        /// </summary>
        /// <returns>A <see cref="DateTime"/> representing the current local date and time.</returns>
        DateTime Now();

        /// <summary>
        /// Gets the current UTC date and time.
        /// </summary>
        /// <returns>A <see cref="DateTime"/> representing the current UTC date and time.</returns>
        /// <remarks>
        /// UTC time should be used for all timestamps that may be accessed across different time zones
        /// or that require absolute time references.
        /// </remarks>
        DateTime UtcNow();

        /// <summary>
        /// Gets the current date with the time component set to midnight.
        /// </summary>
        /// <returns>A <see cref="DateTime"/> representing the current date with time set to 00:00:00.</returns>
        /// <remarks>
        /// This is useful for date-only comparisons or when the time component is not relevant.
        /// </remarks>
        DateTime Today();
    }
}