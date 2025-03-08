using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SecurityPatrol.Infrastructure.BackgroundJobs
{
    /// <summary>
    /// Configuration options for the health check job
    /// </summary>
    public class HealthCheckOptions
    {
        /// <summary>
        /// Gets or sets the interval in minutes between health checks
        /// </summary>
        public int IntervalMinutes { get; set; } = 15;
        
        /// <summary>
        /// Gets or sets whether to check database connectivity
        /// </summary>
        public bool CheckDatabase { get; set; } = true;
        
        /// <summary>
        /// Gets or sets whether to check storage service availability
        /// </summary>
        public bool CheckStorage { get; set; } = true;
        
        /// <summary>
        /// Gets or sets the path of a test file to check in storage
        /// </summary>
        public string TestFilePath { get; set; } = "health-check-test.txt";
    }

    /// <summary>
    /// Represents the result of a health check operation
    /// </summary>
    public class HealthCheckResult
    {
        /// <summary>
        /// Gets the name of the component that was checked
        /// </summary>
        public string ComponentName { get; }
        
        /// <summary>
        /// Gets whether the component is healthy
        /// </summary>
        public bool IsHealthy { get; }
        
        /// <summary>
        /// Gets a message describing the health check result
        /// </summary>
        public string Message { get; }
        
        /// <summary>
        /// Gets the timestamp when the health check was performed
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Initializes a new instance of the HealthCheckResult class
        /// </summary>
        /// <param name="componentName">The name of the component that was checked</param>
        /// <param name="isHealthy">Whether the component is healthy</param>
        /// <param name="message">A message describing the health check result</param>
        /// <param name="timestamp">The timestamp when the health check was performed</param>
        public HealthCheckResult(string componentName, bool isHealthy, string message, DateTime timestamp)
        {
            ComponentName = componentName;
            IsHealthy = isHealthy;
            Message = message;
            Timestamp = timestamp;
        }
    }

    /// <summary>
    /// Background job that periodically performs health checks on critical system components
    /// </summary>
    public class HealthCheckJob : IHostedService
    {
        private readonly SecurityPatrolDbContext _dbContext;
        private readonly IStorageService _storageService;
        private readonly IDateTime _dateTime;
        private readonly ILogger<HealthCheckJob> _logger;
        private readonly HealthCheckOptions _options;
        private Timer _timer;
        private int _isRunning;

        /// <summary>
        /// Initializes a new instance of the HealthCheckJob with required dependencies
        /// </summary>
        /// <param name="dbContext">The database context to check connectivity</param>
        /// <param name="storageService">The storage service to check availability</param>
        /// <param name="dateTime">Service to get current date and time</param>
        /// <param name="options">Configuration options for the health check job</param>
        /// <param name="logger">Logger for recording health check results and errors</param>
        public HealthCheckJob(
            SecurityPatrolDbContext dbContext,
            IStorageService storageService,
            IDateTime dateTime,
            IOptions<HealthCheckOptions> options,
            ILogger<HealthCheckJob> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _dateTime = dateTime ?? throw new ArgumentNullException(nameof(dateTime));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _isRunning = 0;
        }

        /// <summary>
        /// Starts the health check job when the application starts
        /// </summary>
        /// <param name="cancellationToken">A token that may be used to cancel the start operation</param>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Health check job is starting");
            
            // Create a timer that triggers the health check at the specified interval
            _timer = new Timer(
                ExecuteHealthChecksCallback,
                null,
                TimeSpan.Zero, // Start immediately
                TimeSpan.FromMinutes(_options.IntervalMinutes)); // Then run at the configured interval
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Stops the health check job when the application is shutting down
        /// </summary>
        /// <param name="cancellationToken">A token that may be used to cancel the stop operation</param>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Health check job is stopping");
            
            // Dispose the timer to stop executing checks
            _timer?.Dispose();
            _timer = null;
            
            return Task.CompletedTask;
        }

        private void ExecuteHealthChecksCallback(object state)
        {
            // Fire and forget - but ensure we catch and log any errors
            _ = ExecuteHealthChecksAsync().ContinueWith(task =>
            {
                if (task.IsFaulted && task.Exception != null)
                {
                    _logger.LogError(task.Exception, "Unhandled exception in health check execution");
                }
            });
        }

        /// <summary>
        /// Executes all configured health checks and logs the results
        /// </summary>
        public async Task ExecuteHealthChecksAsync()
        {
            // If already running, skip this execution (prevent overlapping executions)
            if (Interlocked.Exchange(ref _isRunning, 1) == 1)
                return;
            
            try
            {
                _logger.LogInformation("Starting health check execution");
                
                var results = new List<HealthCheckResult>();
                
                // Check database health if enabled
                if (_options.CheckDatabase)
                {
                    var dbResult = await CheckDatabaseHealthAsync();
                    results.Add(dbResult);
                }
                
                // Check storage service health if enabled
                if (_options.CheckStorage)
                {
                    var storageResult = await CheckStorageHealthAsync();
                    results.Add(storageResult);
                }
                
                // Log summary of results
                var healthyCount = results.Count(r => r.IsHealthy);
                _logger.LogInformation(
                    "Health check execution completed: {HealthyCount}/{TotalCount} components are healthy",
                    healthyCount, results.Count);
                
                // Log details for unhealthy components
                var unhealthyComponents = results.Where(r => !r.IsHealthy).ToList();
                if (unhealthyComponents.Any())
                {
                    _logger.LogWarning(
                        "Unhealthy components detected: {ComponentCount}",
                        unhealthyComponents.Count);
                    
                    foreach (var result in unhealthyComponents)
                    {
                        _logger.LogWarning(
                            "Component {ComponentName} is unhealthy: {Message}",
                            result.ComponentName, result.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during health check execution");
            }
            finally
            {
                // Reset the running flag
                Interlocked.Exchange(ref _isRunning, 0);
                _logger.LogInformation("Health check execution completed");
            }
        }

        /// <summary>
        /// Checks the health of the database connection
        /// </summary>
        /// <returns>The result of the database health check</returns>
        private async Task<HealthCheckResult> CheckDatabaseHealthAsync()
        {
            _logger.LogInformation("Checking database health");
            
            try
            {
                // Test if the database connection is working
                bool canConnect = await _dbContext.Database.CanConnectAsync();
                
                if (canConnect)
                {
                    return new HealthCheckResult(
                        "Database",
                        true,
                        "Database connection is healthy",
                        _dateTime.UtcNow());
                }
                else
                {
                    return new HealthCheckResult(
                        "Database",
                        false,
                        "Unable to connect to the database",
                        _dateTime.UtcNow());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking database health");
                
                return new HealthCheckResult(
                    "Database",
                    false,
                    $"Error checking database health: {ex.Message}",
                    _dateTime.UtcNow());
            }
        }

        /// <summary>
        /// Checks the health of the storage service
        /// </summary>
        /// <returns>The result of the storage service health check</returns>
        private async Task<HealthCheckResult> CheckStorageHealthAsync()
        {
            _logger.LogInformation("Checking storage service health");
            
            try
            {
                // Test if we can check file existence in the storage service
                // We don't expect the test file to actually exist, just verify that the service responds
                bool exists = await _storageService.FileExistsAsync(_options.TestFilePath);
                
                return new HealthCheckResult(
                    "Storage",
                    true,
                    $"Storage service is healthy. Test file {(exists ? "exists" : "does not exist")}",
                    _dateTime.UtcNow());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking storage service health");
                
                return new HealthCheckResult(
                    "Storage",
                    false,
                    $"Error checking storage service health: {ex.Message}",
                    _dateTime.UtcNow());
            }
        }
    }
}