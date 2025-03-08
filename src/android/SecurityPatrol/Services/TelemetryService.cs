using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Extensions.Logging; // v8.0.0
using Microsoft.AppCenter; // v5.0.0
using Microsoft.AppCenter.Analytics; // v5.0.0
using Microsoft.AppCenter.Crashes; // v5.0.0
using SecurityPatrol.Constants;

namespace SecurityPatrol.Services
{
    /// <summary>
    /// Implementation of the ITelemetryService interface that provides telemetry and monitoring 
    /// capabilities for the Security Patrol application using Microsoft App Center.
    /// </summary>
    public class TelemetryService : ITelemetryService
    {
        private readonly ILogger<TelemetryService> _logger;
        private readonly Dictionary<string, string> _commonProperties;
        private readonly Dictionary<string, Dictionary<string, object>> _operationProperties;
        private bool _isInitialized;

        /// <summary>
        /// Initializes a new instance of the TelemetryService class and configures App Center if telemetry is enabled.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public TelemetryService(ILogger<TelemetryService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _commonProperties = new Dictionary<string, string>
            {
                { "AppName", AppConstants.AppName },
                { "AppVersion", AppConstants.AppVersion }
            };
            _operationProperties = new Dictionary<string, Dictionary<string, object>>();
            _isInitialized = false;

            if (AppConstants.EnableTelemetry)
            {
                InitializeAppCenter();
            }
        }

        /// <summary>
        /// Initializes Microsoft App Center with the appropriate services based on configuration.
        /// </summary>
        private void InitializeAppCenter()
        {
            if (_isInitialized)
                return;

            try
            {
                var services = new List<Type>();

                if (AppConstants.EnableTelemetry)
                {
                    services.Add(typeof(Analytics));
                }

                if (AppConstants.EnableCrashReporting)
                {
                    services.Add(typeof(Crashes));
                }

                // Note: In a real implementation, you would retrieve this from a secure configuration source
                // such as environment variables, Azure Key Vault, or platform-specific secure storage
                string appSecret = "your-app-center-secret";

                if (services.Count > 0)
                {
                    AppCenter.Start(appSecret, services.ToArray());
                    _isInitialized = true;
                    _logger.LogInformation("App Center initialized successfully with {ServiceCount} services", services.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize App Center");
                _isInitialized = false;
            }
        }

        /// <summary>
        /// Tracks a custom event with optional properties.
        /// </summary>
        /// <param name="eventName">The name of the event to track.</param>
        /// <param name="properties">Optional properties associated with the event.</param>
        public void TrackEvent(string eventName, Dictionary<string, string> properties = null)
        {
            if (!AppConstants.EnableTelemetry)
                return;

            try
            {
                var mergedProperties = MergeProperties(properties);
                Analytics.TrackEvent(eventName, mergedProperties);
                _logger.LogDebug("Tracked event: {EventName} with {PropertyCount} properties", 
                    eventName, mergedProperties?.Count ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to track event: {EventName}", eventName);
            }
        }

        /// <summary>
        /// Tracks a page view event.
        /// </summary>
        /// <param name="pageName">The name of the page being viewed.</param>
        public void TrackPageView(string pageName)
        {
            if (!AppConstants.EnableTelemetry)
                return;

            try
            {
                var properties = new Dictionary<string, string>
                {
                    { "PageName", pageName }
                };

                Analytics.TrackEvent("PageView", MergeProperties(properties));
                _logger.LogDebug("Tracked page view: {PageName}", pageName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to track page view: {PageName}", pageName);
            }
        }

        /// <summary>
        /// Tracks an exception with optional properties.
        /// </summary>
        /// <param name="exception">The exception to track.</param>
        /// <param name="properties">Optional properties associated with the exception.</param>
        public void TrackException(Exception exception, Dictionary<string, string> properties = null)
        {
            if (exception == null)
                return;

            if (!AppConstants.EnableCrashReporting)
            {
                _logger.LogError(exception, "Exception occurred but crash reporting is disabled");
                return;
            }

            try
            {
                var mergedProperties = MergeProperties(properties);
                Crashes.TrackError(exception, mergedProperties);
                _logger.LogError(exception, "Tracked exception with {PropertyCount} properties", 
                    mergedProperties?.Count ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to track exception: {ExceptionMessage}", exception.Message);
            }
        }

        /// <summary>
        /// Tracks a custom metric with a name and value.
        /// </summary>
        /// <param name="metricName">The name of the metric to track.</param>
        /// <param name="metricValue">The value of the metric.</param>
        /// <param name="properties">Optional properties associated with the metric.</param>
        public void TrackMetric(string metricName, double metricValue, Dictionary<string, string> properties = null)
        {
            if (!AppConstants.EnableTelemetry)
                return;

            try
            {
                var metricProperties = properties ?? new Dictionary<string, string>();
                metricProperties["MetricValue"] = metricValue.ToString();
                
                Analytics.TrackEvent(metricName, MergeProperties(metricProperties));
                _logger.LogDebug("Tracked metric: {MetricName} with value {MetricValue}", 
                    metricName, metricValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to track metric: {MetricName}", metricName);
            }
        }

        /// <summary>
        /// Tracks an API call with timing information.
        /// </summary>
        /// <param name="endpoint">The API endpoint called.</param>
        /// <param name="duration">The duration of the API call.</param>
        /// <param name="isSuccess">Whether the API call was successful.</param>
        /// <param name="responseCode">The response code from the API call.</param>
        public void TrackApiCall(string endpoint, TimeSpan duration, bool isSuccess, string responseCode = null)
        {
            if (!AppConstants.EnableTelemetry)
                return;

            try
            {
                var properties = new Dictionary<string, string>
                {
                    { "Endpoint", endpoint },
                    { "DurationMs", duration.TotalMilliseconds.ToString() },
                    { "Success", isSuccess.ToString() }
                };

                if (!string.IsNullOrEmpty(responseCode))
                {
                    properties["ResponseCode"] = responseCode;
                }

                Analytics.TrackEvent("ApiCall", MergeProperties(properties));
                _logger.LogDebug("Tracked API call: {Endpoint}, Duration: {DurationMs}ms, Success: {Success}, ResponseCode: {ResponseCode}",
                    endpoint, duration.TotalMilliseconds, isSuccess, responseCode ?? "N/A");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to track API call: {Endpoint}", endpoint);
            }
        }

        /// <summary>
        /// Starts a new operation for tracking a sequence of related events.
        /// </summary>
        /// <param name="operationName">The name of the operation being started.</param>
        /// <returns>Operation ID that can be used to correlate related events.</returns>
        public string StartOperation(string operationName)
        {
            if (string.IsNullOrEmpty(operationName))
                throw new ArgumentNullException(nameof(operationName));

            try
            {
                string operationId = Guid.NewGuid().ToString();
                
                _operationProperties[operationId] = new Dictionary<string, object>
                {
                    { "OperationName", operationName },
                    { "StartTime", DateTime.UtcNow }
                };

                var properties = new Dictionary<string, string>
                {
                    { "OperationId", operationId },
                    { "OperationName", operationName },
                    { "EventType", "OperationStart" }
                };

                TrackEvent("Operation", properties);
                
                return operationId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start operation: {OperationName}", operationName);
                return Guid.NewGuid().ToString(); // Return a new ID anyway to avoid null reference exceptions
            }
        }

        /// <summary>
        /// Stops an operation that was previously started.
        /// </summary>
        /// <param name="operationId">The operation ID returned from StartOperation.</param>
        public void StopOperation(string operationId)
        {
            if (string.IsNullOrEmpty(operationId))
                return;

            try
            {
                if (_operationProperties.TryGetValue(operationId, out var operationData))
                {
                    string operationName = operationData["OperationName"].ToString();
                    DateTime startTime = (DateTime)operationData["StartTime"];
                    TimeSpan duration = DateTime.UtcNow - startTime;

                    var properties = new Dictionary<string, string>
                    {
                        { "OperationId", operationId },
                        { "OperationName", operationName },
                        { "EventType", "OperationEnd" },
                        { "DurationMs", duration.TotalMilliseconds.ToString() }
                    };

                    TrackEvent("Operation", properties);
                    _operationProperties.Remove(operationId);
                    
                    _logger.LogDebug("Stopped operation: {OperationName}, ID: {OperationId}, Duration: {DurationMs}ms",
                        operationName, operationId, duration.TotalMilliseconds);
                }
                else
                {
                    _logger.LogWarning("Attempted to stop unknown operation with ID: {OperationId}", operationId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop operation with ID: {OperationId}", operationId);
            }
        }

        /// <summary>
        /// Sets the user ID for subsequent telemetry events.
        /// </summary>
        /// <param name="userId">The user ID to associate with telemetry events.</param>
        public void SetUserId(string userId)
        {
            if (!AppConstants.EnableTelemetry)
                return;

            try
            {
                AppCenter.SetUserId(userId);
                
                _commonProperties["UserId"] = userId; // Will add or update
                
                _logger.LogDebug("Set user ID: {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set user ID: {UserId}", userId);
            }
        }

        /// <summary>
        /// Sets a custom property that will be included with all subsequent telemetry events.
        /// </summary>
        /// <param name="key">The property key.</param>
        /// <param name="value">The property value.</param>
        public void SetProperty(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
                return;

            try
            {
                _commonProperties[key] = value; // Will add or update
                _logger.LogDebug("Set property: {PropertyKey}={PropertyValue}", key, value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set property: {PropertyKey}={PropertyValue}", key, value);
            }
        }

        /// <summary>
        /// Flushes any queued telemetry events to ensure they are sent.
        /// </summary>
        /// <returns>Task representing the asynchronous operation.</returns>
        public async Task FlushAsync()
        {
            if (!AppConstants.EnableTelemetry)
                return;

            try
            {
                // The App Center SDK doesn't have a direct flush method, but we can
                // temporarily disable and then re-enable Analytics to force a flush
                await Analytics.SetEnabledAsync(false);
                await Analytics.SetEnabledAsync(true);
                
                _logger.LogDebug("Flushed telemetry events");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to flush telemetry events");
            }
        }

        /// <summary>
        /// Logs a message with the specified log level and optional exception.
        /// </summary>
        /// <param name="logLevel">The severity level of the log.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">Optional exception associated with the log.</param>
        public void Log(LogLevel logLevel, string message, Exception exception = null)
        {
            try
            {
                switch (logLevel)
                {
                    case LogLevel.Trace:
                        _logger.LogTrace(exception, message);
                        break;
                    case LogLevel.Debug:
                        _logger.LogDebug(exception, message);
                        break;
                    case LogLevel.Information:
                        _logger.LogInformation(exception, message);
                        break;
                    case LogLevel.Warning:
                        _logger.LogWarning(exception, message);
                        break;
                    case LogLevel.Error:
                        _logger.LogError(exception, message);
                        if (exception != null && AppConstants.EnableCrashReporting)
                        {
                            TrackException(exception, new Dictionary<string, string> { { "LogMessage", message } });
                        }
                        break;
                    case LogLevel.Critical:
                        _logger.LogCritical(exception, message);
                        if (exception != null && AppConstants.EnableCrashReporting)
                        {
                            TrackException(exception, new Dictionary<string, string> { { "LogMessage", message } });
                        }
                        break;
                    default:
                        _logger.LogInformation(exception, message);
                        break;
                }
            }
            catch (Exception ex)
            {
                // Fallback to console if logger fails
                Console.WriteLine($"Logging failed: {ex.Message}. Original message: {message}");
            }
        }

        /// <summary>
        /// Merges the provided properties with common properties.
        /// </summary>
        /// <param name="properties">Properties to merge with common properties.</param>
        /// <returns>Merged properties dictionary.</returns>
        private Dictionary<string, string> MergeProperties(Dictionary<string, string> properties)
        {
            var result = new Dictionary<string, string>(_commonProperties);
            
            if (properties != null)
            {
                foreach (var kvp in properties)
                {
                    result[kvp.Key] = kvp.Value; // Will add or update
                }
            }
            
            return result;
        }
    }
}