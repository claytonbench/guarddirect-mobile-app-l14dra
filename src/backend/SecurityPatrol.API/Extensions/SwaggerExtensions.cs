using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using System.Reflection;
using System.IO;
using System;
using SecurityPatrol.API.Swagger;

namespace SecurityPatrol.API.Extensions
{
    /// <summary>
    /// Static class containing extension methods for configuring Swagger and OpenAPI documentation in the Security Patrol API.
    /// </summary>
    public static class SwaggerExtensions
    {
        /// <summary>
        /// Adds Swagger generation services to the service collection with proper configuration for the Security Patrol API.
        /// </summary>
        /// <param name="services">The service collection to add Swagger services to.</param>
        /// <returns>The service collection with Swagger generation configured.</returns>
        public static IServiceCollection AddSwaggerGen(this IServiceCollection services)
        {
            services.AddSwaggerGen(options =>
            {
                // Set document title, version, and description
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Security Patrol API",
                    Version = "v1",
                    Description = "API for Security Patrol mobile application",
                    TermsOfService = new Uri("https://example.com/terms"),
                    Contact = new OpenApiContact
                    {
                        Name = "Security Patrol Support",
                        Email = "support@securitypatrol.example.com",
                        Url = new Uri("https://securitypatrol.example.com/support")
                    },
                    License = new OpenApiLicense
                    {
                        Name = "Use under proprietary license",
                        Url = new Uri("https://securitypatrol.example.com/license")
                    }
                });

                // Configure XML documentation
                AddSwaggerXmlComments(options);

                // Add security definition for JWT Bearer authentication
                AddSwaggerSecurityDefinition(options);

                // Add operation filters
                options.OperationFilter<SecurityRequirementsOperationFilter>();
                options.OperationFilter<SwaggerDefaultValues>();

                // Configure API versioning support if provider is available
                var provider = services.BuildServiceProvider().GetService<IApiVersionDescriptionProvider>();
                if (provider != null)
                {
                    ConfigureSwaggerOptions(options, provider);
                }
            });

            return services;
        }

        /// <summary>
        /// Configures Swagger options for API versioning support.
        /// </summary>
        /// <param name="options">The Swagger generation options.</param>
        /// <param name="provider">The API version description provider.</param>
        private static void ConfigureSwaggerOptions(SwaggerGenOptions options, IApiVersionDescriptionProvider provider)
        {
            // Create a swagger document for each discovered API version
            foreach (var description in provider.ApiVersionDescriptions)
            {
                options.SwaggerDoc(
                    description.GroupName,
                    new OpenApiInfo
                    {
                        Title = $"Security Patrol API {description.ApiVersion}",
                        Version = description.ApiVersion.ToString(),
                        Description = "API for Security Patrol mobile application",
                        TermsOfService = new Uri("https://example.com/terms"),
                        Contact = new OpenApiContact
                        {
                            Name = "Security Patrol Support",
                            Email = "support@securitypatrol.example.com",
                            Url = new Uri("https://securitypatrol.example.com/support")
                        },
                        License = new OpenApiLicense
                        {
                            Name = "Use under proprietary license",
                            Url = new Uri("https://securitypatrol.example.com/license")
                        }
                    });
            }
        }

        /// <summary>
        /// Adds JWT Bearer security definition to Swagger options.
        /// </summary>
        /// <param name="options">The Swagger generation options.</param>
        private static void AddSwaggerSecurityDefinition(SwaggerGenOptions options)
        {
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n" +
                              "Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\n" +
                              "Example: \"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });
        }

        /// <summary>
        /// Configures XML documentation file inclusion for Swagger.
        /// </summary>
        /// <param name="options">The Swagger generation options.</param>
        private static void AddSwaggerXmlComments(SwaggerGenOptions options)
        {
            // Get the assembly containing the API controllers
            var assembly = Assembly.GetExecutingAssembly();

            // Construct the XML documentation file path
            var xmlFile = $"{assembly.GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

            // Include XML comments if the file exists
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
                
                // Additionally include XML comments for model classes if there's a separate file
                var xmlModelFile = $"{assembly.GetName().Name}.Models.xml";
                var xmlModelPath = Path.Combine(AppContext.BaseDirectory, xmlModelFile);
                
                if (File.Exists(xmlModelPath))
                {
                    options.IncludeXmlComments(xmlModelPath);
                }
            }
        }
    }
}