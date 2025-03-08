using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging; // v8.0.0

namespace SecurityPatrol.Services
{
    /// <summary>
    /// Interface that defines the contract for telemetry and monitoring operations in the Security Patrol application.
    /// </summary>
    public interface ITelemetryService
    {
        /// <summary>
        /// Tracks a custom event with optional properties.
        /// </summary>
        /// <param name="eventName">The name of the event to track.</param>
        /// <param name="properties">Optional properties associated with the event.</param>
        void TrackEvent(string eventName, Dictionary<string, string> properties = null);

        /// <summary>
        /// Tracks a page view event.
        /// </summary>
        /// <param name="pageName">The name of the page being viewed.</param>
        void TrackPageView(string pageName);

        /// <summary>
        /// Tracks an exception with optional properties.
        /// </summary>
        /// <param name="exception">The exception to track.</param>
        /// <param name="properties">Optional properties associated with the exception.</param>
        void TrackException(Exception exception, Dictionary<string, string> properties = null);

        /// <summary>
        /// Tracks a custom metric with a name and value.
        /// </summary>
        /// <param name="metricName">The name of the metric to track.</param>
        /// <param name="metricValue">The value of the metric.</param>
        /// <param name="properties">Optional properties associated with the metric.</param>
        void TrackMetric(string metricName, double metricValue, Dictionary<string, string> properties = null);

        /// <summary>
        /// Tracks an API call with timing information.
        /// </summary>
        /// <param name="endpoint">The API endpoint called.</param>
        /// <param name="duration">The duration of the API call.</param>
        /// <param name="isSuccess">Whether the API call was successful.</param>
        /// <param name="responseCode">The response code from the API call.</param>
        void TrackApiCall(string endpoint, TimeSpan duration, bool isSuccess, string responseCode = null);

        /// <summary>
        /// Starts a new operation for tracking a sequence of related events.
        /// </summary>
        /// <param name="operationName">The name of the operation being started.</param>
        /// <returns>Operation ID that can be used to correlate related events.</returns>
        string StartOperation(string operationName);

        /// <summary>
        /// Stops an operation that was previously started.
        /// </summary>
        /// <param name="operationId">The operation ID returned from StartOperation.</param>
        void StopOperation(string operationId);

        /// <summary>
        /// Sets the user ID for subsequent telemetry events.
        /// </summary>
        /// <param name="userId">The user ID to associate with telemetry events.</param>
        void SetUserId(string userId);

        /// <summary>
        /// Sets a custom property that will be included with all subsequent telemetry events.
        /// </summary>
        /// <param name="key">The property key.</param>
        /// <param name="value">The property value.</param>
        void SetProperty(string key, string value);

        /// <summary>
        /// Flushes any queued telemetry events to ensure they are sent.
        /// </summary>
        /// <returns>Task representing the asynchronous operation.</returns>
        Task FlushAsync();

        /// <summary>
        /// Logs a message with the specified log level and optional exception.
        /// </summary>
        /// <param name="logLevel">The severity level of the log.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">Optional exception associated with the log.</param>
        void Log(LogLevel logLevel, string message, Exception exception = null);
    }
}