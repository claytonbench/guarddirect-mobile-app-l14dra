using Microsoft.Extensions.DependencyInjection; // Microsoft.Extensions.DependencyInjection 8.0.0
using Microsoft.Extensions.Configuration; // Microsoft.Extensions.Configuration 8.0.0
using Microsoft.AspNetCore.Mvc; // Microsoft.AspNetCore.Mvc 8.0.0
using Microsoft.AspNetCore.Cors; // Microsoft.AspNetCore.Cors 8.0.0
using Microsoft.AspNetCore.Hosting; // Microsoft.AspNetCore.Hosting 8.0.0
using Microsoft.OpenApi.Models; // Microsoft.OpenApi.Models 1.6.0
using Swashbuckle.AspNetCore.SwaggerGen; // Swashbuckle.AspNetCore.SwaggerGen 6.5.0
using System.Reflection; // System.Reflection 8.0.0

using SecurityPatrol.API.Filters; // For registering API-specific filters
using SecurityPatrol.API.Extensions; // For authentication and Swagger extensions
using SecurityPatrol.API.Swagger; // For Swagger configuration

namespace SecurityPatrol.API.Extensions
{
    /// <summary>
    /// Static class containing extension methods for IServiceCollection to configure services for the Security Patrol API.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds and configures all services required by the Security Patrol API.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="environment">The web hosting environment.</param>
        /// <returns>The service collection with all API services configured.</returns>
        public static IServiceCollection AddSecurityPatrolServices(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
        {
            // Configure controllers with API behavior options
            services.AddApiControllers();

            // Add global filters for exception handling, model validation, and HTTPS enforcement
            services.AddApiFilters(environment);

            // Configure JSON serialization
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    // Use camel case for property names
                    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                    // Ignore null values when serializing
                    options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
                });

            // Add CORS policy with appropriate origins, methods, and headers
            services.AddCorsPolicy(configuration);

            // Add JWT authentication using AddJwtAuthentication extension method
            services.AddJwtAuthentication(configuration);

            // Add authorization policies using AddAuthorizationPolicies extension method
            services.AddAuthorizationPolicies();

            // Add health checks for monitoring system health
            services.AddApiHealthChecks(configuration);

            // Add API versioning with URL path versioning
            services.AddApiVersioning();

            // Add Swagger generation using AddSwaggerGen extension method
            services.AddSwaggerGen(options =>
            {
                // Configure Swagger document
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Security Patrol API",
                    Version = "v1",
                    Description = "API for the Security Patrol mobile application",
                    Contact = new OpenApiContact
                    {
                        Name = "Security Patrol Team",
                        Email = "support@securitypatrol.example.com"
                    }
                });

                // Add security definition for JWT Bearer authentication
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                // Add security requirements filter
                options.OperationFilter<SecurityRequirementsOperationFilter>();
                options.OperationFilter<SwaggerDefaultValues>();

                // Include XML comments if available
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (System.IO.File.Exists(xmlPath))
                {
                    options.IncludeXmlComments(xmlPath);
                }
            });

            // Register API-specific services
            // Here we would register repositories, services, handlers, etc.
            // Services specific to the Security Patrol API functionality

            return services;
        }

        /// <summary>
        /// Configures MVC controllers with API-specific behavior options.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        /// <returns>The service collection with controllers configured.</returns>
        private static IServiceCollection AddApiControllers(this IServiceCollection services)
        {
            // Add controllers with views services
            services.AddControllersWithViews()
                .ConfigureApiBehaviorOptions(options =>
                {
                    // Set InvalidModelStateResponseFactory to return BadRequest with validation errors
                    options.InvalidModelStateResponseFactory = context =>
                    {
                        var problemDetails = new ValidationProblemDetails(context.ModelState)
                        {
                            Status = Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest,
                            Instance = context.HttpContext.Request.Path,
                            Title = "One or more validation errors occurred.",
                            Detail = "See the errors property for details."
                        };

                        return new BadRequestObjectResult(problemDetails)
                        {
                            ContentTypes = { "application/problem+json" }
                        };
                    };

                    // Enable SuppressModelStateInvalidFilter to use custom validation filter
                    options.SuppressModelStateInvalidFilter = true;
                    
                    // Enable SuppressMapClientErrors to handle client errors in filters
                    options.SuppressMapClientErrors = true;
                });

            return services;
        }

        /// <summary>
        /// Adds global filters for exception handling, model validation, and HTTPS enforcement.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        /// <param name="environment">The web hosting environment.</param>
        /// <returns>The service collection with filters configured.</returns>
        private static IServiceCollection AddApiFilters(this IServiceCollection services, IWebHostEnvironment environment)
        {
            // Configure MVC options to add global filters
            services.Configure<MvcOptions>(options =>
            {
                // Add ApiExceptionFilter for global exception handling
                options.Filters.Add<ApiExceptionFilter>();
                
                // Add ValidateModelStateFilter for model validation
                options.Filters.Add<ValidateModelStateFilter>();
                
                // Add RequireHttpsFilter for HTTPS enforcement in production
                if (!environment.IsDevelopment())
                {
                    options.Filters.Add<RequireHttpsFilter>();
                }
            });

            return services;
        }

        /// <summary>
        /// Configures CORS policy for the API to allow cross-origin requests from specified origins.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        /// <param name="configuration">The application configuration.</param>
        /// <returns>The service collection with CORS policy configured.</returns>
        private static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
        {
            // Get allowed origins from configuration
            var allowedOriginsSection = configuration.GetSection("Cors:AllowedOrigins");
            var allowedOrigins = allowedOriginsSection?.Get<string[]>();
            
            // Add CORS services to the service collection
            services.AddCors(options =>
            {
                // Configure default policy to allow specified origins
                options.AddDefaultPolicy(builder =>
                {
                    var corsBuilder = builder
                        // Allow common HTTP methods (GET, POST, PUT, DELETE)
                        .AllowAnyMethod()
                        // Allow common headers including Authorization
                        .AllowAnyHeader();
                    
                    if (allowedOrigins != null && allowedOrigins.Length > 0)
                    {
                        corsBuilder.WithOrigins(allowedOrigins);
                    }
                    else
                    {
                        corsBuilder.AllowAnyOrigin();
                    }
                    
                    // Allow credentials for authenticated requests
                    if (allowedOrigins != null && allowedOrigins.Length > 0 && !allowedOrigins.Contains("*"))
                    {
                        corsBuilder.AllowCredentials();
                    }
                });
            });

            return services;
        }

        /// <summary>
        /// Configures API versioning with URL path versioning strategy.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        /// <returns>The service collection with API versioning configured.</returns>
        private static IServiceCollection AddApiVersioning(this IServiceCollection services)
        {
            // Add API versioning services
            services.AddApiVersioning(options =>
            {
                // Set default API version to 1.0
                options.DefaultApiVersion = new ApiVersion(1, 0);
                
                // Report API versions in response headers
                options.ReportApiVersions = true;
                
                // Assume default version when not specified
                options.AssumeDefaultVersionWhenUnspecified = true;
            });

            // Add version explorer for API documentation
            services.AddVersionedApiExplorer(options =>
            {
                // Format version as 'v'major[.minor]
                options.GroupNameFormat = "'v'VVV";
                
                // Substitute the version in the URL path
                options.SubstituteApiVersionInUrl = true;
            });

            return services;
        }

        /// <summary>
        /// Configures health checks for monitoring system health.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        /// <param name="configuration">The application configuration.</param>
        /// <returns>The service collection with health checks configured.</returns>
        private static IServiceCollection AddApiHealthChecks(this IServiceCollection services, IConfiguration configuration)
        {
            // Add health checks services
            var healthChecks = services.AddHealthChecks();
            
            // Add check for database connection using connection string from configuration
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (!string.IsNullOrEmpty(connectionString))
            {
                healthChecks.AddSqlServer(
                    connectionString,
                    name: "database",
                    tags: new[] { "database", "sql" });
            }
            
            // Add check for SMS service availability
            var smsHealthUrl = configuration["SmsService:HealthCheckUrl"];
            if (!string.IsNullOrEmpty(smsHealthUrl))
            {
                healthChecks.AddUrlGroup(
                    new Uri(smsHealthUrl),
                    name: "sms-service",
                    tags: new[] { "service", "external" });
            }
            
            // Add check for storage service availability
            var storageHealthUrl = configuration["Storage:HealthCheckUrl"];
            if (!string.IsNullOrEmpty(storageHealthUrl))
            {
                healthChecks.AddUrlGroup(
                    new Uri(storageHealthUrl),
                    name: "storage-service",
                    tags: new[] { "service", "storage" });
            }

            return services;
        }
    }
}