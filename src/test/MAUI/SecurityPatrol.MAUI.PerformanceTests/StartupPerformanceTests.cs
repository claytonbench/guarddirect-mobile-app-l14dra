using System; // Version 8.0.0
using System.Threading.Tasks; // Version 8.0.0
using System.Diagnostics; // Version 8.0.0
using Microsoft.Extensions.DependencyInjection; // Version 8.0.0
using Microsoft.Maui; // Version 8.0.0
using Microsoft.Maui.Hosting; // Version 8.0.0
using Xunit; // Version 2.4.2
using Xunit.Abstractions; // Version 2.0.3
using FluentAssertions; // Version 6.11.0
using SecurityPatrol.MAUI.PerformanceTests.Setup;
using SecurityPatrol.TestCommon.Helpers;

namespace SecurityPatrol.MAUI.PerformanceTests
{
    /// <summary>
    /// Contains performance tests for measuring and validating the startup time of the Security Patrol MAUI application.
    /// </summary>
    public class StartupPerformanceTests : PerformanceTestBase
    {
        private const double StartupTimeThresholdMs = 2000;
        private const double ServiceRegistrationTimeThresholdMs = 1000;
        private const double MemoryThresholdMB = 150;
        private readonly ITestOutputHelper _outputHelper;

        /// <summary>
        /// Initializes a new instance of the StartupPerformanceTests class with test output helper.
        /// </summary>
        /// <param name="outputHelper">The test output helper for writing test results.</param>
        public StartupPerformanceTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        /// <summary>
        /// Tests that the application startup time meets the performance threshold.
        /// </summary>
        [Fact]
        [Trait("Category", "StartupPerformance")]
        public async Task TestAppStartupPerformance()
        {
            _outputHelper.WriteLine("Starting TestAppStartupPerformance");
            double executionTime = await MeasureExecutionTimeAsync(() => Task.Run(() => SecurityPatrol.MauiProgram.CreateMauiApp()), "AppStartup");
            AssertPerformanceThreshold(executionTime, StartupTimeThresholdMs, "App Startup Time");
            _outputHelper.WriteLine($"TestAppStartupPerformance completed in {executionTime} ms");
        }

        /// <summary>
        /// Tests that the service registration time during application startup meets the performance threshold.
        /// </summary>
        [Fact]
        [Trait("Category", "StartupPerformance")]
        public async Task TestServiceRegistrationPerformance()
        {
            _outputHelper.WriteLine("Starting TestServiceRegistrationPerformance");
            var builder = MauiApp.CreateBuilder();
            double executionTime = await MeasureExecutionTimeAsync(() => Task.Run(() => ConfigureServicesWrapper(builder.Services)), "ServiceRegistration");
            AssertPerformanceThreshold(executionTime, ServiceRegistrationTimeThresholdMs, "Service Registration Time");
            _outputHelper.WriteLine($"TestServiceRegistrationPerformance completed in {executionTime} ms");
        }

        /// <summary>
        /// Tests that the memory usage during application startup meets the threshold.
        /// </summary>
        [Fact]
        [Trait("Category", "MemoryUsage")]
        public async Task TestStartupMemoryUsage()
        {
            _outputHelper.WriteLine("Starting TestStartupMemoryUsage");
            long memoryUsageBytes = await MeasureMemoryUsageAsync(() => Task.Run(() => SecurityPatrol.MauiProgram.CreateMauiApp()), "AppStartup");
            double memoryUsageMB = memoryUsageBytes / (1024 * 1024);
            AssertMemoryThreshold(memoryUsageBytes, MemoryThresholdMB, "App Startup Memory Usage");
            _outputHelper.WriteLine($"TestStartupMemoryUsage completed with {memoryUsageMB} MB");
        }

        /// <summary>
        /// Tests application startup performance under simulated low-resource device conditions.
        /// </summary>
        [Fact]
        [Trait("Category", "StartupPerformance")]
        public async Task TestStartupPerformanceOnLowResourceDevice()
        {
            _outputHelper.WriteLine("Starting TestStartupPerformanceOnLowResourceDevice");
            SimulateLowResourceEnvironment();
            double executionTime = await MeasureExecutionTimeAsync(() => Task.Run(() => SecurityPatrol.MauiProgram.CreateMauiApp()), "AppStartup");
            AssertPerformanceThreshold(executionTime, StartupTimeThresholdMs * 1.5, "App Startup Time (Low Resource)");
            _outputHelper.WriteLine($"TestStartupPerformanceOnLowResourceDevice completed in {executionTime} ms");
        }

        /// <summary>
        /// Tests application startup performance under simulated high-end device conditions.
        /// </summary>
        [Fact]
        [Trait("Category", "StartupPerformance")]
        public async Task TestStartupPerformanceOnHighEndDevice()
        {
            _outputHelper.WriteLine("Starting TestStartupPerformanceOnHighEndDevice");
            SimulateHighEndEnvironment();
            double executionTime = await MeasureExecutionTimeAsync(() => Task.Run(() => SecurityPatrol.MauiProgram.CreateMauiApp()), "AppStartup");
            AssertPerformanceThreshold(executionTime, StartupTimeThresholdMs * 0.75, "App Startup Time (High End)");
            _outputHelper.WriteLine($"TestStartupPerformanceOnHighEndDevice completed in {executionTime} ms");
        }

        /// <summary>
        /// Runs a comprehensive benchmark of application startup with multiple iterations.
        /// </summary>
        [Fact]
        [Trait("Category", "StartupPerformance")]
        public async Task BenchmarkAppStartup()
        {
            _outputHelper.WriteLine("Starting BenchmarkAppStartup");
            (double averageTime, long averageMemory) = await RunBenchmarkAsync(() => Task.Run(() => SecurityPatrol.MauiProgram.CreateMauiApp()), "AppStartup", 5);
            AssertPerformanceThreshold(averageTime, StartupTimeThresholdMs, "App Startup Time (Benchmark)");
            AssertMemoryThreshold(averageMemory, MemoryThresholdMB, "App Startup Memory Usage (Benchmark)");
            _outputHelper.WriteLine($"BenchmarkAppStartup completed with average time {averageTime} ms and average memory {averageMemory} bytes");
        }

        /// <summary>
        /// Tests application startup performance under simulated memory pressure conditions.
        /// </summary>
        [Fact]
        [Trait("Category", "StartupPerformance")]
        public async Task TestStartupUnderMemoryPressure()
        {
            _outputHelper.WriteLine("Starting TestStartupUnderMemoryPressure");
            DeviceInfoHelper.SimulateMemoryUsage(70);
            double executionTime = await MeasureExecutionTimeAsync(() => Task.Run(() => SecurityPatrol.MauiProgram.CreateMauiApp()), "AppStartup");
            AssertPerformanceThreshold(executionTime, StartupTimeThresholdMs * 1.25, "App Startup Time (Memory Pressure)");
            DeviceInfoHelper.SimulateMemoryUsage(0);
            _outputHelper.WriteLine($"TestStartupUnderMemoryPressure completed in {executionTime} ms");
        }

        /// <summary>
        /// Wrapper method for MauiProgram.CreateMauiApp to facilitate testing.
        /// </summary>
        private async Task CreateMauiAppWrapper()
        {
            SecurityPatrol.MauiProgram.CreateMauiApp();
            await Task.CompletedTask;
        }

        /// <summary>
        /// Wrapper method for testing service registration performance.
        /// </summary>
        private async Task ConfigureServicesWrapper(IServiceCollection services)
        {
            SecurityPatrol.MauiProgram.ConfigureServices(services);
            await Task.CompletedTask;
        }
    }
}