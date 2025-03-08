using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using SecurityPatrol.API.Extensions;
using SecurityPatrol.Application;
using SecurityPatrol.Infrastructure;
using System;
using System.Linq;
using System.Net.Mime;
using System.Text.Json;
using System.Threading.Tasks;

namespace SecurityPatrol.API
{
    /// <summary>
    /// Entry point for the Security Patrol API application that configures and runs the ASP.NET Core web host.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Entry point for the application that configures and runs the ASP.NET Core web host.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        /// <returns>Asynchronous task representing the application execution.</returns>
        public static async Task Main(string[] args)
        {
            // Create a WebApplicationBuilder with command line arguments
            var builder = WebApplication.CreateBuilder(args);

            // Configure application services using extension methods
            builder.Services.AddApplicationServices(builder.Configuration);
            builder.Services.AddInfrastructureServices(builder.Configuration);
            builder.Services.AddSecurityPatrolServices(builder.Configuration);

            // Build the WebApplication instance
            var app = builder.Build();

            // Configure the middleware pipeline using extension method
            app.UseSecurityPatrolMiddleware(app.Environment);

            // Map health check endpoints for monitoring
            ConfigureHealthCheckEndpoints(app);

            // Run the application and await its completion
            await app.RunAsync();
        }

        /// <summary>
        /// Configures health check endpoints for monitoring application health.
        /// </summary>
        /// <param name="app">The WebApplication instance.</param>
        private static void ConfigureHealthCheckEndpoints(WebApplication app)
        {
            // Map '/health' endpoint for basic health check
            app.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = WriteHealthCheckResponse
            });

            // Map '/health/ready' endpoint for readiness check
            app.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = healthCheck => healthCheck.Tags.Contains("ready"),
                ResponseWriter = WriteHealthCheckResponse
            });

            // Map '/health/live' endpoint for liveness check
            app.MapHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = healthCheck => healthCheck.Tags.Contains("live"),
                ResponseWriter = WriteHealthCheckResponse
            });
        }

        /// <summary>
        /// Writes a standardized JSON response for health check endpoints.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <param name="report">The health report.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private static Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
        {
            // Set content type to application/json
            context.Response.ContentType = MediaTypeNames.Application.Json;
            
            // Configure status codes based on health check results
            context.Response.StatusCode = report.Status == HealthStatus.Healthy ? 200 :
                                         report.Status == HealthStatus.Degraded ? 200 : 503;

            // Create response object with health check details
            var response = new
            {
                status = report.Status.ToString(),
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description,
                    duration = e.Value.Duration
                }),
                totalDuration = report.TotalDuration
            };

            // Serialize and write the JSON response
            return context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}