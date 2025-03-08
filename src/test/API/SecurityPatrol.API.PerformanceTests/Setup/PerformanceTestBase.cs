using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;
using FluentAssertions;
using BenchmarkDotNet.Attributes;
using SecurityPatrol.API.IntegrationTests.Setup;
using SecurityPatrol.TestCommon.Constants;

namespace SecurityPatrol.API.PerformanceTests.Setup
{
    /// <summary>
    /// Base class for API performance tests providing infrastructure for measuring and validating API endpoint performance.
    /// </summary>
    public abstract class PerformanceTestBase : IClassFixture<CustomWebApplicationFactory>, IDisposable
    {
        protected readonly CustomWebApplicationFactory Factory;
        protected readonly HttpClient Client;
        protected readonly string DatabaseName;
        protected readonly JsonSerializerOptions JsonOptions;
        protected readonly ITestOutputHelper OutputHelper;
        protected readonly double DefaultPerformanceThreshold;

        /// <summary>
        /// Initializes a new instance of the PerformanceTestBase class with the test factory and output helper.
        /// </summary>
        /// <param name="outputHelper">The test output helper for logging test results.</param>
        protected PerformanceTestBase(ITestOutputHelper outputHelper)
        {
            OutputHelper = outputHelper ?? throw new ArgumentNullException(nameof(outputHelper));
            Factory = new CustomWebApplicationFactory();
            DatabaseName = Factory.DatabaseName;

            // Create and configure HTTP client
            Client = Factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("https://localhost/api/")
            });

            // Configure JSON serialization options
            JsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            // Set default authorization header
            SetAuthToken(TestConstants.TestAuthToken);

            // Set default performance threshold to 1000ms (1 second) per requirements
            DefaultPerformanceThreshold = 1000;
        }

        /// <summary>
        /// Sets the authentication token for the HTTP client.
        /// </summary>
        /// <param name="token">The authentication token.</param>
        protected void SetAuthToken(string token)
        {
            string authToken = string.IsNullOrEmpty(token) ? TestConstants.TestAuthToken : token;
            Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);
            OutputHelper.WriteLine($"Client authenticated with token: {authToken.Substring(0, Math.Min(10, authToken.Length))}...");
        }

        /// <summary>
        /// Measures the execution time of the specified function.
        /// </summary>
        /// <param name="action">The function to measure.</param>
        /// <returns>The execution time in milliseconds.</returns>
        protected async Task<double> MeasureExecutionTime(Func<Task> action)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            
            await action();
            
            stopwatch.Stop();
            return stopwatch.Elapsed.TotalMilliseconds;
        }

        /// <summary>
        /// Measures the execution time of the specified function that returns a value.
        /// </summary>
        /// <typeparam name="T">The type of result returned by the function.</typeparam>
        /// <param name="action">The function to measure.</param>
        /// <returns>The result and execution time.</returns>
        protected async Task<(T result, double elapsedMilliseconds)> MeasureExecutionTime<T>(Func<Task<T>> action)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            
            T result = await action();
            
            stopwatch.Stop();
            return (result, stopwatch.Elapsed.TotalMilliseconds);
        }

        /// <summary>
        /// Runs multiple concurrent requests using the specified function.
        /// </summary>
        /// <typeparam name="T">The type of result returned by the function.</typeparam>
        /// <param name="requestFunc">The function that executes the request.</param>
        /// <param name="concurrentUsers">The number of concurrent requests to execute.</param>
        /// <returns>The results and execution times for each request.</returns>
        protected async Task<List<(T result, double elapsedMilliseconds)>> RunConcurrentRequests<T>(Func<Task<T>> requestFunc, int concurrentUsers)
        {
            var tasks = new List<Task<(T result, double elapsedMilliseconds)>>();
            
            for (int i = 0; i < concurrentUsers; i++)
            {
                tasks.Add(MeasureExecutionTime(requestFunc));
            }
            
            await Task.WhenAll(tasks);
            
            return tasks.Select(t => t.Result).ToList();
        }

        /// <summary>
        /// Calculates performance statistics from a list of execution times.
        /// </summary>
        /// <param name="executionTimes">The list of execution times to analyze.</param>
        /// <returns>Performance statistics including min, max, average, 95th and 99th percentiles.</returns>
        protected (double min, double max, double avg, double p95, double p99) GetPerformanceStatistics(List<double> executionTimes)
        {
            if (executionTimes == null || executionTimes.Count == 0)
            {
                return (0, 0, 0, 0, 0);
            }

            var sortedTimes = executionTimes.OrderBy(t => t).ToList();
            
            double min = sortedTimes.First();
            double max = sortedTimes.Last();
            double avg = sortedTimes.Average();
            
            // Calculate percentiles
            int p95Index = (int)Math.Ceiling(sortedTimes.Count * 0.95) - 1;
            int p99Index = (int)Math.Ceiling(sortedTimes.Count * 0.99) - 1;
            
            p95Index = Math.Max(0, Math.Min(p95Index, sortedTimes.Count - 1));
            p99Index = Math.Max(0, Math.Min(p99Index, sortedTimes.Count - 1));
            
            double p95 = sortedTimes[p95Index];
            double p99 = sortedTimes[p99Index];
            
            return (min, max, avg, p95, p99);
        }

        /// <summary>
        /// Asserts that the execution time is within the specified threshold.
        /// </summary>
        /// <param name="executionTime">The execution time to check.</param>
        /// <param name="threshold">The threshold for acceptable performance.</param>
        /// <param name="operationName">The name of the operation being measured.</param>
        protected void AssertPerformance(double executionTime, double threshold, string operationName)
        {
            LogPerformanceResult(operationName, executionTime, threshold);
            
            executionTime.Should().BeLessThanOrEqualTo(threshold, 
                $"Performance test for '{operationName}' failed: execution time ({executionTime:F2}ms) exceeded threshold ({threshold:F2}ms)");
        }

        /// <summary>
        /// Asserts that the execution times are within the specified threshold.
        /// </summary>
        /// <param name="executionTimes">The execution times to check.</param>
        /// <param name="threshold">The threshold for acceptable performance.</param>
        /// <param name="operationName">The name of the operation being measured.</param>
        protected void AssertPerformance(List<double> executionTimes, double threshold, string operationName)
        {
            var stats = GetPerformanceStatistics(executionTimes);
            LogPerformanceStatistics(operationName, stats, threshold);
            
            // Assert that 95th percentile is below threshold
            stats.p95.Should().BeLessThanOrEqualTo(threshold, 
                $"Performance test for '{operationName}' failed: 95th percentile execution time ({stats.p95:F2}ms) exceeded threshold ({threshold:F2}ms)");
        }

        /// <summary>
        /// Sends a GET request to the specified endpoint and deserializes the response.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the response to.</typeparam>
        /// <param name="endpoint">The API endpoint.</param>
        /// <returns>The deserialized response.</returns>
        protected async Task<T> GetAsync<T>(string endpoint)
        {
            var response = await Client.GetAsync(endpoint);
            AssertSuccessStatusCode(response);
            
            return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        }

        /// <summary>
        /// Sends a POST request with the specified content to the endpoint and deserializes the response.
        /// </summary>
        /// <typeparam name="TRequest">The type of the request content.</typeparam>
        /// <typeparam name="TResponse">The type to deserialize the response to.</typeparam>
        /// <param name="endpoint">The API endpoint.</param>
        /// <param name="content">The content to send.</param>
        /// <returns>The deserialized response.</returns>
        protected async Task<TResponse> PostAsync<TRequest, TResponse>(string endpoint, TRequest content)
        {
            var json = JsonSerializer.Serialize(content, JsonOptions);
            var stringContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await Client.PostAsync(endpoint, stringContent);
            AssertSuccessStatusCode(response);
            
            return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions);
        }

        /// <summary>
        /// Sends a PUT request with the specified content to the endpoint and deserializes the response.
        /// </summary>
        /// <typeparam name="TRequest">The type of the request content.</typeparam>
        /// <typeparam name="TResponse">The type to deserialize the response to.</typeparam>
        /// <param name="endpoint">The API endpoint.</param>
        /// <param name="content">The content to send.</param>
        /// <returns>The deserialized response.</returns>
        protected async Task<TResponse> PutAsync<TRequest, TResponse>(string endpoint, TRequest content)
        {
            var json = JsonSerializer.Serialize(content, JsonOptions);
            var stringContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await Client.PutAsync(endpoint, stringContent);
            AssertSuccessStatusCode(response);
            
            return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions);
        }

        /// <summary>
        /// Sends a DELETE request to the specified endpoint and deserializes the response.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the response to.</typeparam>
        /// <param name="endpoint">The API endpoint.</param>
        /// <returns>The deserialized response.</returns>
        protected async Task<T> DeleteAsync<T>(string endpoint)
        {
            var response = await Client.DeleteAsync(endpoint);
            AssertSuccessStatusCode(response);
            
            return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        }

        /// <summary>
        /// Asserts that the HTTP response has a success status code.
        /// </summary>
        /// <param name="response">The HTTP response to check.</param>
        protected void AssertSuccessStatusCode(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }
            
            var content = response.Content.ReadAsStringAsync().Result;
            throw new Exception($"HTTP request failed with status code {response.StatusCode}. Content: {content}");
        }

        /// <summary>
        /// Logs performance test results to the test output.
        /// </summary>
        /// <param name="operationName">The name of the operation being measured.</param>
        /// <param name="executionTime">The execution time of the operation.</param>
        /// <param name="threshold">The threshold for acceptable performance.</param>
        protected void LogPerformanceResult(string operationName, double executionTime, double threshold)
        {
            OutputHelper.WriteLine($"Performance test: {operationName} - Time: {executionTime:F2}ms - Threshold: {threshold:F2}ms - {(executionTime <= threshold ? "PASSED" : "FAILED")}");
        }

        /// <summary>
        /// Logs performance statistics to the test output.
        /// </summary>
        /// <param name="operationName">The name of the operation being measured.</param>
        /// <param name="stats">The performance statistics.</param>
        /// <param name="threshold">The threshold for acceptable performance.</param>
        protected void LogPerformanceStatistics(string operationName, (double min, double max, double avg, double p95, double p99) stats, double threshold)
        {
            OutputHelper.WriteLine($"Performance statistics for {operationName}:");
            OutputHelper.WriteLine($"  Min: {stats.min:F2}ms");
            OutputHelper.WriteLine($"  Max: {stats.max:F2}ms");
            OutputHelper.WriteLine($"  Avg: {stats.avg:F2}ms");
            OutputHelper.WriteLine($"  95th Percentile: {stats.p95:F2}ms");
            OutputHelper.WriteLine($"  99th Percentile: {stats.p99:F2}ms");
            OutputHelper.WriteLine($"  Threshold: {threshold:F2}ms");
            OutputHelper.WriteLine($"  Result: {(stats.p95 <= threshold ? "PASSED" : "FAILED")}");
        }

        /// <summary>
        /// Disposes the HTTP client and other resources.
        /// </summary>
        public virtual void Dispose()
        {
            Client?.Dispose();
            Factory?.Dispose();
        }
    }
}