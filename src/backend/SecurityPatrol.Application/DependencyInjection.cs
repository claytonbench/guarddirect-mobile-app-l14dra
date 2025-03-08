using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SecurityPatrol.Application.Behaviors;
using SecurityPatrol.Application.Services;
using SecurityPatrol.Core.Interfaces;
using System.Reflection;

namespace SecurityPatrol.Application
{
    /// <summary>
    /// Contains extension methods for registering application layer services in the dependency injection container.
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Adds application layer services to the dependency injection container.
        /// </summary>
        /// <param name="services">The service collection to add services to.</param>
        /// <param name="configuration">The configuration instance.</param>
        /// <returns>The service collection with application services registered.</returns>
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Register MediatR with all request handlers from the current assembly
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
            
            // Register FluentValidation validators from the current assembly
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
            
            // Register behaviors for cross-cutting concerns
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            
            // Register application services
            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddScoped<ILocationService, LocationService>();
            services.AddScoped<IPatrolService, PatrolService>();
            services.AddScoped<IPhotoService, PhotoService>();
            services.AddScoped<IReportService, ReportService>();
            services.AddScoped<ITimeRecordService, TimeRecordService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IVerificationCodeService, VerificationCodeService>();
            
            return services;
        }
    }
}