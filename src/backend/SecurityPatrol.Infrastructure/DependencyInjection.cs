using Microsoft.EntityFrameworkCore; // Version 8.0.0
using Microsoft.Extensions.Configuration; // Version 8.0.0
using Microsoft.Extensions.DependencyInjection; // Version 8.0.0
using Microsoft.Extensions.Hosting; // Version 8.0.0
using Microsoft.Extensions.Options; // Version 8.0.0
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.Infrastructure.Services;
using SecurityPatrol.Infrastructure.Persistence;
using SecurityPatrol.Infrastructure.Persistence.Repositories;
using SecurityPatrol.Infrastructure.Persistence.Interceptors;
using SecurityPatrol.Infrastructure.BackgroundJobs;

namespace SecurityPatrol.Infrastructure
{
    /// <summary>
    /// Extension methods for registering infrastructure layer services in the dependency injection container.
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Extension method that adds infrastructure layer services to the dependency injection container.
        /// </summary>
        /// <param name="services">The service collection to add services to</param>
        /// <param name="configuration">The configuration containing service settings</param>
        /// <returns>The service collection with infrastructure services registered</returns>
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Register database context with SQL Server provider
            services.AddDbContext<SecurityPatrolDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("SecurityPatrolDatabase"),
                    sqlOptions => sqlOptions.MigrationsAssembly(typeof(SecurityPatrolDbContext).Assembly.FullName)));

            // Register database interceptors
            services.AddScoped<AuditableEntityInterceptor>();

            // Register infrastructure services
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddSingleton<IDateTime, DateTimeService>();
            services.AddScoped<ISmsService, SmsService>();
            services.AddScoped<IStorageService, StorageService>();

            // Register repositories
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ITimeRecordRepository, TimeRecordRepository>();
            services.AddScoped<ILocationRecordRepository, LocationRecordRepository>();
            services.AddScoped<IPatrolLocationRepository, PatrolLocationRepository>();
            services.AddScoped<ICheckpointRepository, CheckpointRepository>();
            services.AddScoped<ICheckpointVerificationRepository, CheckpointVerificationRepository>();
            services.AddScoped<IPhotoRepository, PhotoRepository>();
            services.AddScoped<IReportRepository, ReportRepository>();

            // Configure options from configuration sections
            services.Configure<SmsOptions>(configuration.GetSection("SmsService"));
            services.Configure<DataRetentionOptions>(configuration.GetSection("DataRetention"));
            services.Configure<HealthCheckOptions>(configuration.GetSection("HealthCheck"));

            // Register background services
            services.AddHostedService<DataRetentionJob>();
            services.AddHostedService<HealthCheckJob>();

            // Configure HttpClient for SMS service
            services.AddHttpClient<ISmsService, SmsService>(client =>
            {
                // Configure default client options if needed
                var smsOptions = configuration.GetSection("SmsService").Get<SmsOptions>();
                if (smsOptions?.BaseUrl != null)
                {
                    client.BaseAddress = new System.Uri(smsOptions.BaseUrl);
                }
            });

            return services;
        }
    }
}