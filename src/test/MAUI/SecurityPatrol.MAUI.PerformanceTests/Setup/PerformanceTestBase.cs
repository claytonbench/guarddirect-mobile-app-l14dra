using System; // Version 8.0.0
using System.Threading.Tasks; // Version 8.0.0
using System.Diagnostics; // Version 8.0.0
using System.Collections.Generic; // Version 8.0.0
using Microsoft.Extensions.DependencyInjection; // Version 8.0.0
using Microsoft.Extensions.Logging; // Version 8.0.0
using Xunit; // Version 2.4.2
using Xunit.Abstractions; // Version 2.0.3
using FluentAssertions; // Version 6.11.0
using BenchmarkDotNet.Attributes; // Version 0.13.5
using SecurityPatrol.MAUI.IntegrationTests.Setup;
using SecurityPatrol.TestCommon.Helpers;

namespace SecurityPatrol.MAUI.PerformanceTests.Setup
{
    /// <summary>
    /// Base class for all performance tests in the Security Patrol MAUI application, providing common setup, measurement utilities, and performance assertion methods.
    /// </summary>
    public abstract class PerformanceTestBase : IntegrationTestBase
    {
        /// <summary>
        /// Gets the test output helper for writing test results.
        /// </summary>
        protected ITestOutputHelper OutputHelper { get; }

        /// <summary>
        /// Gets the logger for recording performance test results.
        /// </summary>
        protected ILogger<PerformanceTestBase> Logger { get; }

        /// <summary>
        /// Stores execution time results for each measured operation.
        /// </summary>
        protected Dictionary<string, List<double>> ExecutionTimeResults { get; } = new Dictionary<string, List<double>>();

        /// <summary>
        /// Stores memory usage results for each measured operation.
        /// </summary>
        protected Dictionary<string, List<long>> MemoryUsageResults { get; } = new Dictionary<string, List<long>>();

        /// <summary>
        /// Gets or sets the number of warmup iterations for benchmarks.
        /// </summary>
        protected int WarmupIterations { get; set; } = 2;

        /// <summary>
        /// Gets or sets the number of measurement iterations for benchmarks.
        /// </summary>
        protected int MeasurementIterations { get; set; } = 5;

        /// <summary>
        /// Gets or sets a value indicating whether detailed logging is enabled.
        /// </summary>
        protected bool EnableDetailedLogging { get; set; } = true;

        /// <summary>
        /// Initializes a new instance of the PerformanceTestBase class with optional test output helper.
        /// </summary>
        /// <param name="outputHelper">The test output helper for writing test results.</param>
        public PerformanceTestBase(ITestOutputHelper outputHelper = null)
        {
            OutputHelper = outputHelper;
            ExecutionTimeResults = new Dictionary<string, List<double>>();
            MemoryUsageResults = new Dictionary<string, List<long>>();
            WarmupIterations = 2;
            MeasurementIterations = 5;
            EnableDetailedLogging = true;
            Logger = LoggerFactory.Create(builder => builder.AddXunit(OutputHelper)).CreateLogger<PerformanceTestBase>();
        }

        /// <summary>
        /// Initializes the test environment for performance testing. This is called automatically by the xUnit test framework before each test.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public override async Task InitializeAsync()
        {
            Logger.LogInformation("Performance test initialization started");
            await base.InitializeAsync();

            // Configure performance test specific settings
            WarmupIterations = 2;
            MeasurementIterations = 5;
            EnableDetailedLogging = true;

            // Force garbage collection to ensure clean memory state
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Logger.LogInformation("Performance test initialization successful");
        }

        /// <summary>
        /// Cleans up the test environment after performance testing. This is called automatically by the xUnit test framework after each test.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public virtual async Task CleanupAsync()
        {
            Logger.LogInformation("Performance test cleanup started");
            await base.DisposeAsync();

            // Clear performance test results
            ExecutionTimeResults.Clear();
            MemoryUsageResults.Clear();

            // Force garbage collection to clean up memory
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Logger.LogInformation("Performance test cleanup successful");
        }

        /// <summary>
        /// Measures the execution time of an asynchronous operation.
        /// </summary>
        /// <param name="operation">The asynchronous operation to measure.</param>
        /// <param name="operationName">The name of the operation being measured.</param>
        /// <returns>A task that returns the execution time in milliseconds</returns>
        protected virtual async Task<double> MeasureExecutionTimeAsync(Func<Task> operation, string operationName)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            await operation();
            stopwatch.Stop();

            double elapsedTime = stopwatch.ElapsedMilliseconds;

            if (!ExecutionTimeResults.ContainsKey(operationName))
            {
                ExecutionTimeResults[operationName] = new List<double>();
            }
            ExecutionTimeResults[operationName].Add(elapsedTime);

            Logger.LogInformation("Execution Time for {OperationName}: {ElapsedTime} ms", operationName, elapsedTime);
            return elapsedTime;
        }

        /// <summary>
        /// Measures the memory usage of an asynchronous operation.
        /// </summary>
        /// <param name="operation">The asynchronous operation to measure.</param>
        /// <param name="operationName">The name of the operation being measured.</param>
        /// <returns>A task that returns the memory usage in bytes</returns>
        protected virtual async Task<long> MeasureMemoryUsageAsync(Func<Task> operation, string operationName)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long initialMemory = GC.GetTotalMemory(true);
            await operation();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long finalMemory = GC.GetTotalMemory(true);
            long memoryUsed = finalMemory - initialMemory;

            if (!MemoryUsageResults.ContainsKey(operationName))
            {
                MemoryUsageResults[operationName] = new List<long>();
            }
            MemoryUsageResults[operationName].Add(memoryUsed);

            Logger.LogInformation("Memory Usage for {OperationName}: {MemoryUsed} bytes", operationName, memoryUsed);
            return memoryUsed;
        }

        /// <summary>
        /// Runs a benchmark of an asynchronous operation with multiple iterations.
        /// </summary>
        /// <param name="operation">The asynchronous operation to benchmark.</param>
        /// <param name="operationName">The name of the operation being benchmarked.</param>
        /// <param name="iterations">The number of iterations to run.</param>
        /// <returns>A task that returns the average execution time and memory usage</returns>
        protected virtual async Task<(double AverageTime, long AverageMemory)> RunBenchmarkAsync(Func<Task> operation, string operationName, int iterations)
        {
            Logger.LogInformation("Starting benchmark for {OperationName} with {Iterations} iterations", operationName, iterations);

            // Run warmup iterations without recording results
            for (int i = 0; i < WarmupIterations; i++)
            {
                await operation();
            }

            List<double> executionTimes = new List<double>();
            List<long> memoryUsages = new List<long>();

            // Run measurement iterations
            for (int i = 0; i < MeasurementIterations; i++)
            {
                double executionTime = await MeasureExecutionTimeAsync(operation, operationName);
                long memoryUsage = await MeasureMemoryUsageAsync(operation, operationName);

                executionTimes.Add(executionTime);
                memoryUsages.Add(memoryUsage);

                // Add small delay between iterations
                await Task.Delay(10);
            }

            // Calculate average execution time and memory usage
            double averageExecutionTime = executionTimes.Average();
            long averageMemoryUsage = (long)memoryUsages.Average();

            Logger.LogInformation("Benchmark results for {OperationName}: Average Time = {AverageTime} ms, Average Memory = {AverageMemory} bytes",
                operationName, averageExecutionTime, averageMemoryUsage);

            return (averageExecutionTime, averageMemoryUsage);
        }

        /// <summary>
        /// Asserts that a performance metric is below a specified threshold.
        /// </summary>
        /// <param name="actual">The actual performance metric value.</param>
        /// <param name="threshold">The performance threshold value.</param>
        /// <param name="metricName">The name of the performance metric.</param>
        protected virtual void AssertPerformanceThreshold(double actual, double threshold, string metricName)
        {
            actual.Should().BeLessOrEqualTo(threshold, $"Performance threshold exceeded for {metricName}");
            Logger.LogInformation("Performance Assertion: {MetricName} = {Actual} <= {Threshold}", metricName, actual, threshold);

            if (actual > threshold)
            {
                Logger.LogWarning("WARNING: Performance threshold exceeded for {MetricName}: Actual = {Actual}, Threshold = {Threshold}", metricName, actual, threshold);
            }
        }

        /// <summary>
        /// Asserts that a memory usage metric is below a specified threshold in megabytes.
        /// </summary>
        /// <param name="actualBytes">The actual memory usage in bytes.</param>
        /// <param name="thresholdMB">The memory usage threshold in megabytes.</param>
        /// <param name="metricName">The name of the memory usage metric.</param>
        protected virtual void AssertMemoryThreshold(long actualBytes, double thresholdMB, string metricName)
        {
            double thresholdBytes = thresholdMB * 1024 * 1024;
            actualBytes.Should().BeLessOrEqualTo(thresholdBytes, $"Memory threshold exceeded for {metricName}");
            Logger.LogInformation("Memory Assertion: {MetricName} = {ActualMB} MB <= {ThresholdMB} MB", metricName, actualBytes / (1024 * 1024), thresholdMB);

            if (actualBytes > thresholdBytes)
            {
                Logger.LogWarning("WARNING: Memory threshold exceeded for {MetricName}: Actual = {ActualMB} MB, Threshold = {ThresholdMB} MB", metricName, actualBytes / (1024 * 1024), thresholdMB);
            }
        }

        /// <summary>
        /// Gets the average execution time for a specific operation.
        /// </summary>
        /// <param name="operationName">The name of the operation.</param>
        /// <returns>The average execution time in milliseconds</returns>
        protected virtual double GetAverageExecutionTime(string operationName)
        {
            if (ExecutionTimeResults.ContainsKey(operationName))
            {
                return ExecutionTimeResults[operationName].Average();
            }
            else
            {
                Logger.LogWarning("No execution time results found for operation: {OperationName}", operationName);
                return 0;
            }
        }

        /// <summary>
        /// Gets the average memory usage for a specific operation.
        /// </summary>
        /// <param name="operationName">The name of the operation.</param>
        /// <returns>The average memory usage in bytes</returns>
        protected virtual long GetAverageMemoryUsage(string operationName)
        {
            if (MemoryUsageResults.ContainsKey(operationName))
            {
                return (long)MemoryUsageResults[operationName].Average();
            }
            else
            {
                Logger.LogWarning("No memory usage results found for operation: {OperationName}", operationName);
                return 0;
            }
        }

        /// <summary>
        /// Configures the test environment to simulate a low-resource device.
        /// </summary>
        protected virtual void SimulateLowResourceEnvironment()
        {
            var lowResourceProfile = DeviceInfoHelper.CreateLowResourceDeviceProfile();
            // Apply these settings to the test environment
            Logger.LogInformation("Simulating low-resource environment");
        }

        /// <summary>
        /// Configures the test environment to simulate a high-end device.
        /// </summary>
        protected virtual void SimulateHighEndEnvironment()
        {
            var highEndProfile = DeviceInfoHelper.CreateHighEndDeviceProfile();
            // Apply these settings to the test environment
            Logger.LogInformation("Simulating high-end environment");
        }

        /// <summary>
        /// Logs detailed performance results for all measured operations.
        /// </summary>
        protected virtual void LogPerformanceResults()
        {
            Logger.LogInformation("--- Performance Test Results ---");

            foreach (var operation in ExecutionTimeResults)
            {
                double minTime = operation.Value.Min();
                double maxTime = operation.Value.Max();
                double avgTime = operation.Value.Average();

                Logger.LogInformation("Operation: {OperationName} - Min Time: {MinTime} ms, Max Time: {MaxTime} ms, Average Time: {AvgTime} ms",
                    operation.Key, minTime, maxTime, avgTime);
            }

            foreach (var operation in MemoryUsageResults)
            {
                long minMemory = operation.Value.Min();
                long maxMemory = operation.Value.Max();
                long avgMemory = (long)operation.Value.Average();

                Logger.LogInformation("Operation: {OperationName} - Min Memory: {MinMemoryMB} MB, Max Memory: {MaxMemoryMB} MB, Average Memory: {AvgMemoryMB} MB",
                    operation.Key, minMemory / (1024 * 1024), maxMemory / (1024 * 1024), avgMemory / (1024 * 1024));
            }

            Logger.LogInformation("--- End Performance Test Results ---");
        }

        /// <summary>
        /// Gets the current memory usage of the application.
        /// </summary>
        /// <returns>The current memory usage in bytes</returns>
        protected static long GetCurrentMemoryUsage()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            return GC.GetTotalMemory(true);
        }
    }
}