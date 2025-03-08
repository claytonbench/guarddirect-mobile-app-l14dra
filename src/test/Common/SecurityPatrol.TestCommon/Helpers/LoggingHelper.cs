using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Debug;
using Xunit.Abstractions;

namespace SecurityPatrol.TestCommon.Helpers
{
    /// <summary>
    /// Helper class that provides utilities for configuring and managing logging during tests.
    /// </summary>
    public static class LoggingHelper
    {
        // Private constructor to prevent instantiation
        private LoggingHelper()
        {
        }

        /// <summary>
        /// Creates a logger factory with console and debug providers configured.
        /// </summary>
        /// <returns>Configured logger factory instance</returns>
        public static ILoggerFactory CreateLogger()
        {
            return LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
            });
        }

        /// <summary>
        /// Creates a logger factory with console, debug, and xUnit test output providers configured.
        /// </summary>
        /// <param name="testOutputHelper">The xUnit test output helper</param>
        /// <returns>Configured logger factory instance</returns>
        public static ILoggerFactory CreateLogger(ITestOutputHelper testOutputHelper)
        {
            return LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
                builder.AddProvider(new XUnitLoggerProvider(testOutputHelper));
            });
        }

        /// <summary>
        /// Creates a typed logger with console and debug providers configured.
        /// </summary>
        /// <typeparam name="T">The type to use for the logger category</typeparam>
        /// <returns>Configured logger instance for the specified type</returns>
        public static ILogger<T> CreateLogger<T>()
        {
            var factory = CreateLogger();
            return factory.CreateLogger<T>();
        }

        /// <summary>
        /// Creates a typed logger with console, debug, and xUnit test output providers configured.
        /// </summary>
        /// <typeparam name="T">The type to use for the logger category</typeparam>
        /// <param name="testOutputHelper">The xUnit test output helper</param>
        /// <returns>Configured logger instance for the specified type</returns>
        public static ILogger<T> CreateLogger<T>(ITestOutputHelper testOutputHelper)
        {
            var factory = CreateLogger(testOutputHelper);
            return factory.CreateLogger<T>();
        }

        /// <summary>
        /// Creates a logger that captures log entries in memory for test verification.
        /// </summary>
        /// <returns>A tuple containing the logger and a list of captured log entries</returns>
        public static Tuple<ILogger, List<LogEntry>> CreateInMemoryLogger()
        {
            var logEntries = new List<LogEntry>();
            var provider = new InMemoryLoggerProvider(logEntries);
            var factory = LoggerFactory.Create(builder => builder.AddProvider(provider));
            var logger = factory.CreateLogger("TestLogger");
            return Tuple.Create(logger, logEntries);
        }

        /// <summary>
        /// Creates a typed logger that captures log entries in memory for test verification.
        /// </summary>
        /// <typeparam name="T">The type to use for the logger category</typeparam>
        /// <returns>A tuple containing the typed logger and a list of captured log entries</returns>
        public static Tuple<ILogger<T>, List<LogEntry>> CreateInMemoryLogger<T>()
        {
            var logEntries = new List<LogEntry>();
            var provider = new InMemoryLoggerProvider(logEntries);
            var factory = LoggerFactory.Create(builder => builder.AddProvider(provider));
            var logger = factory.CreateLogger<T>();
            return Tuple.Create(logger, logEntries);
        }

        /// <summary>
        /// Configures a logger factory with standard test settings.
        /// </summary>
        /// <param name="builder">The logging builder to configure</param>
        public static void ConfigureLogging(ILoggingBuilder builder)
        {
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddConsole();
            builder.AddDebug();
            
            // Configure filters for test-relevant namespaces
            builder.AddFilter("SecurityPatrol", LogLevel.Debug);
            builder.AddFilter("Microsoft", LogLevel.Warning);
            builder.AddFilter("System", LogLevel.Warning);
        }

        /// <summary>
        /// Configures a logger factory with standard test settings and xUnit output.
        /// </summary>
        /// <param name="builder">The logging builder to configure</param>
        /// <param name="testOutputHelper">The xUnit test output helper</param>
        public static void ConfigureLogging(ILoggingBuilder builder, ITestOutputHelper testOutputHelper)
        {
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddConsole();
            builder.AddDebug();
            builder.AddProvider(new XUnitLoggerProvider(testOutputHelper));
            
            // Configure filters for test-relevant namespaces
            builder.AddFilter("SecurityPatrol", LogLevel.Debug);
            builder.AddFilter("Microsoft", LogLevel.Warning);
            builder.AddFilter("System", LogLevel.Warning);
        }

        /// <summary>
        /// Logs the start of a test with standard formatting.
        /// </summary>
        /// <param name="logger">The logger to use</param>
        /// <param name="testName">The name of the test</param>
        public static void LogTestStart(ILogger logger, string testName)
        {
            logger.LogInformation("========== TEST START: {TestName} - {Timestamp} ==========", 
                testName, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        }

        /// <summary>
        /// Logs the end of a test with standard formatting.
        /// </summary>
        /// <param name="logger">The logger to use</param>
        /// <param name="testName">The name of the test</param>
        /// <param name="success">Whether the test was successful</param>
        public static void LogTestEnd(ILogger logger, string testName, bool success)
        {
            if (success)
            {
                logger.LogInformation("========== TEST END (SUCCESS): {TestName} - {Timestamp} ==========", 
                    testName, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            }
            else
            {
                logger.LogError("========== TEST END (FAILURE): {TestName} - {Timestamp} ==========", 
                    testName, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            }
        }

        /// <summary>
        /// Logs a test step with standard formatting.
        /// </summary>
        /// <param name="logger">The logger to use</param>
        /// <param name="stepDescription">The description of the step</param>
        public static void LogTestStep(ILogger logger, string stepDescription)
        {
            logger.LogInformation("STEP: {StepDescription}", stepDescription);
        }

        /// <summary>
        /// Logs a test assertion with standard formatting.
        /// </summary>
        /// <param name="logger">The logger to use</param>
        /// <param name="assertionDescription">The description of the assertion</param>
        public static void LogTestAssertion(ILogger logger, string assertionDescription)
        {
            logger.LogInformation("ASSERT: {AssertionDescription}", assertionDescription);
        }

        /// <summary>
        /// Saves captured log entries to a file for later analysis.
        /// </summary>
        /// <param name="logEntries">The log entries to save</param>
        /// <param name="filePath">The path to save the file to</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public static async Task SaveLogsToFile(List<LogEntry> logEntries, string filePath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            
            using (var writer = new StreamWriter(filePath, false))
            {
                foreach (var entry in logEntries)
                {
                    await writer.WriteLineAsync(entry.ToString());
                }
            }
        }
    }

    /// <summary>
    /// Represents a captured log entry for test verification.
    /// </summary>
    public class LogEntry
    {
        /// <summary>
        /// Gets the timestamp when the log entry was created.
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Gets the log level of the entry.
        /// </summary>
        public LogLevel Level { get; }

        /// <summary>
        /// Gets the category of the log entry.
        /// </summary>
        public string Category { get; }

        /// <summary>
        /// Gets the message of the log entry.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the exception associated with the log entry, if any.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Initializes a new instance of the LogEntry class.
        /// </summary>
        /// <param name="timestamp">The timestamp when the log entry was created</param>
        /// <param name="level">The log level of the entry</param>
        /// <param name="category">The category of the log entry</param>
        /// <param name="message">The message of the log entry</param>
        /// <param name="exception">The exception associated with the log entry, if any</param>
        public LogEntry(DateTime timestamp, LogLevel level, string category, string message, Exception exception)
        {
            Timestamp = timestamp;
            Level = level;
            Category = category;
            Message = message;
            Exception = exception;
        }

        /// <summary>
        /// Returns a string representation of the log entry.
        /// </summary>
        /// <returns>String representation of the log entry</returns>
        public override string ToString()
        {
            var result = $"[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level}] [{Category}]: {Message}";
            
            if (Exception != null)
            {
                result += $"{Environment.NewLine}Exception: {Exception.GetType().Name}: {Exception.Message}";
                result += $"{Environment.NewLine}{Exception.StackTrace}";
            }
            
            return result;
        }
    }

    /// <summary>
    /// A logger provider that captures log entries in memory for test verification.
    /// </summary>
    internal class InMemoryLoggerProvider : ILoggerProvider
    {
        private readonly List<LogEntry> _logEntries;

        /// <summary>
        /// Initializes a new instance of the InMemoryLoggerProvider class.
        /// </summary>
        /// <param name="logEntries">The list to capture log entries in</param>
        public InMemoryLoggerProvider(List<LogEntry> logEntries)
        {
            _logEntries = logEntries;
        }

        /// <summary>
        /// Creates a logger that captures log entries in the shared list.
        /// </summary>
        /// <param name="categoryName">The category name for the logger</param>
        /// <returns>Logger that captures entries in memory</returns>
        public ILogger CreateLogger(string categoryName)
        {
            return new InMemoryLogger(categoryName, _logEntries);
        }

        /// <summary>
        /// Disposes the provider resources.
        /// </summary>
        public void Dispose()
        {
            // No resources to dispose
        }
    }

    /// <summary>
    /// A logger that captures log entries in memory for test verification.
    /// </summary>
    internal class InMemoryLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly List<LogEntry> _logEntries;

        /// <summary>
        /// Initializes a new instance of the InMemoryLogger class.
        /// </summary>
        /// <param name="categoryName">The category name for the logger</param>
        /// <param name="logEntries">The list to capture log entries in</param>
        public InMemoryLogger(string categoryName, List<LogEntry> logEntries)
        {
            _categoryName = categoryName;
            _logEntries = logEntries;
        }

        /// <summary>
        /// Determines if logging is enabled for the specified log level.
        /// </summary>
        /// <param name="logLevel">The log level to check</param>
        /// <returns>True if logging is enabled, otherwise false</returns>
        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None;
        }

        /// <summary>
        /// Logs an entry to the in-memory collection.
        /// </summary>
        /// <typeparam name="TState">The type of the state</typeparam>
        /// <param name="logLevel">The log level</param>
        /// <param name="eventId">The event ID</param>
        /// <param name="state">The state</param>
        /// <param name="exception">The exception, if any</param>
        /// <param name="formatter">The formatter function</param>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (logLevel == LogLevel.None || formatter == null)
            {
                return;
            }

            var message = formatter(state, exception);
            var entry = new LogEntry(DateTime.Now, logLevel, _categoryName, message, exception);
            _logEntries.Add(entry);
        }

        /// <summary>
        /// Creates a scope for grouping log entries.
        /// </summary>
        /// <typeparam name="TState">The type of the state</typeparam>
        /// <param name="state">The state</param>
        /// <returns>A disposable object that ends the scope when disposed</returns>
        public IDisposable BeginScope<TState>(TState state)
        {
            // Scopes not implemented for in-memory logger
            return new NoOpDisposable();
        }

        private class NoOpDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }

    /// <summary>
    /// A logger provider that outputs logs to xUnit's test output.
    /// </summary>
    internal class XUnitLoggerProvider : ILoggerProvider
    {
        private readonly ITestOutputHelper _testOutputHelper;

        /// <summary>
        /// Initializes a new instance of the XUnitLoggerProvider class.
        /// </summary>
        /// <param name="testOutputHelper">The xUnit test output helper</param>
        public XUnitLoggerProvider(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        /// <summary>
        /// Creates a logger that outputs to xUnit's test output.
        /// </summary>
        /// <param name="categoryName">The category name for the logger</param>
        /// <returns>Logger that outputs to xUnit</returns>
        public ILogger CreateLogger(string categoryName)
        {
            return new XUnitLogger(categoryName, _testOutputHelper);
        }

        /// <summary>
        /// Disposes the provider resources.
        /// </summary>
        public void Dispose()
        {
            // No resources to dispose
        }
    }

    /// <summary>
    /// A logger that outputs logs to xUnit's test output.
    /// </summary>
    internal class XUnitLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly ITestOutputHelper _testOutputHelper;

        /// <summary>
        /// Initializes a new instance of the XUnitLogger class.
        /// </summary>
        /// <param name="categoryName">The category name for the logger</param>
        /// <param name="testOutputHelper">The xUnit test output helper</param>
        public XUnitLogger(string categoryName, ITestOutputHelper testOutputHelper)
        {
            _categoryName = categoryName;
            _testOutputHelper = testOutputHelper;
        }

        /// <summary>
        /// Determines if logging is enabled for the specified log level.
        /// </summary>
        /// <param name="logLevel">The log level to check</param>
        /// <returns>True if logging is enabled, otherwise false</returns>
        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None;
        }

        /// <summary>
        /// Logs an entry to the xUnit test output.
        /// </summary>
        /// <typeparam name="TState">The type of the state</typeparam>
        /// <param name="logLevel">The log level</param>
        /// <param name="eventId">The event ID</param>
        /// <param name="state">The state</param>
        /// <param name="exception">The exception, if any</param>
        /// <param name="formatter">The formatter function</param>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (logLevel == LogLevel.None || formatter == null)
            {
                return;
            }

            var message = formatter(state, exception);
            var logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{logLevel}] [{_categoryName}]: {message}";

            if (exception != null)
            {
                logLine += $"{Environment.NewLine}Exception: {exception.GetType().Name}: {exception.Message}";
                logLine += $"{Environment.NewLine}{exception.StackTrace}";
            }

            try
            {
                _testOutputHelper.WriteLine(logLine);
            }
            catch (Exception)
            {
                // If test has already completed, this might throw an exception
                // Just swallow it as we can't do much about it
            }
        }

        /// <summary>
        /// Creates a scope for grouping log entries.
        /// </summary>
        /// <typeparam name="TState">The type of the state</typeparam>
        /// <param name="state">The state</param>
        /// <returns>A disposable object that ends the scope when disposed</returns>
        public IDisposable BeginScope<TState>(TState state)
        {
            // Scopes not implemented for xUnit logger
            return new NoOpDisposable();
        }

        private class NoOpDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }
}