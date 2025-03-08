using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using SecurityPatrol.Infrastructure.BackgroundJobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SecurityPatrol.API.Controllers
{
    /// <summary>
    /// Controller that provides endpoints for checking the health status of the Security Patrol application and its dependencies.
    /// </summary>
    [ApiController]
    [Route("api/health")]
    [ApiExplorerSettings(IgnoreApi = false)]
    public class HealthCheckController : ControllerBase
    {
        private readonly HealthCheckService _healthCheckService;
        private readonly ILogger<HealthCheckController> _logger;

        /// <summary>
        /// Initializes a new instance of the HealthCheckController with required dependencies
        /// </summary>
        /// <param name="healthCheckService">Service for executing health checks</param>
        /// <param name="logger">Logger for health check operations</param>
        /// <exception cref="ArgumentNullException">Thrown if any required dependency is null</exception>
        public HealthCheckController(
            HealthCheckService healthCheckService,
            ILogger<HealthCheckController> logger)
        {
            _healthCheckService = healthCheckService ?? throw new ArgumentNullException(nameof(healthCheckService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Endpoint that returns a simple health status indicating if the API is running
        /// </summary>
        /// <returns>OK response with a simple health status message</returns>
        [HttpGet]
        [ProducesResponseType(200)]
        [AllowAnonymous]
        public IActionResult GetHealth()
        {
            _logger.LogInformation("Basic health check requested");
            return Ok(new { status = "Healthy", message = "API is running" });
        }

        /// <summary>
        /// Endpoint that returns detailed health information about the application and its dependencies
        /// </summary>
        /// <returns>Action result containing detailed health status information</returns>
        [HttpGet("detailed")]
        [ProducesResponseType(200)]
        [ProducesResponseType(503)]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> GetDetailedHealth()
        {
            _logger.LogInformation("Detailed health check requested");

            try
            {
                var report = await _healthCheckService.CheckHealthAsync();
                
                var results = report.Entries.Select(entry => new HealthCheckResult(
                    componentName: entry.Key,
                    isHealthy: entry.Value.Status == HealthStatus.Healthy,
                    message: entry.Value.Description ?? MapHealthStatus(entry.Value.Status),
                    timestamp: DateTime.UtcNow
                )).ToList();
                
                var response = new
                {
                    overallStatus = MapHealthStatus(report.Status),
                    isHealthy = report.Status == HealthStatus.Healthy,
                    timestamp = DateTime.UtcNow,
                    components = results
                };

                return report.Status == HealthStatus.Healthy 
                    ? Ok(response) 
                    : StatusCode(503, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing detailed health check");
                return StatusCode(500, new { status = "Error", message = "Error executing health check" });
            }
        }

        /// <summary>
        /// Helper method to map ASP.NET Core HealthStatus to a string representation
        /// </summary>
        /// <param name="status">The health status to map</param>
        /// <returns>String representation of the health status</returns>
        private string MapHealthStatus(HealthStatus status)
        {
            return status switch
            {
                HealthStatus.Healthy => "Healthy",
                HealthStatus.Degraded => "Degraded",
                HealthStatus.Unhealthy => "Unhealthy",
                _ => "Unknown"
            };
        }
    }
}