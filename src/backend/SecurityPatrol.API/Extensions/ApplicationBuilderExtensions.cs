using Microsoft.AspNetCore.Builder; // v8.0.0
using Microsoft.AspNetCore.Hosting; // v8.0.0
using Microsoft.Extensions.Hosting; // v8.0.0
using Microsoft.Extensions.Configuration; // v8.0.0
using SecurityPatrol.API.Middleware;
using Swashbuckle.AspNetCore.SwaggerUI; // v6.5.0
using Swashbuckle.AspNetCore.Swagger; // v6.5.0

namespace SecurityPatrol.API.Extensions
{
    /// <summary>
    /// Static class containing extension methods for IApplicationBuilder to configure the middleware pipeline for the Security Patrol API.
    /// </summary>
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Configures the middleware pipeline for the Security Patrol API with all required middleware components in the correct order.
        /// </summary>
        /// <param name="app">The application builder instance.</param>
        /// <param name="env">The web host environment.</param>
        /// <returns>The application builder with middleware configured.</returns>
        public static IApplicationBuilder UseSecurityPatrolMiddleware(this IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Use exception handling middleware first to catch all exceptions
            app.UseApiExceptionHandler();
            
            // HTTPS redirection in non-development environments
            if (!env.IsDevelopment())
            {
                app.UseHttpsRedirection();
                
                // HSTS in production
                if (env.IsProduction())
                {
                    app.UseHsts();
                }
            }
            
            // Routing middleware
            app.UseRouting();
            
            // API key authentication middleware
            app.UseApiKeyAuthentication();
            
            // Request logging middleware
            app.UseRequestLogging();
            
            // CORS middleware
            app.UseCors("SecurityPatrolCorsPolicy");
            
            // Authentication and authorization middleware
            app.UseAuthentication();
            app.UseAuthorization();
            
            // Configure Swagger in development environment
            if (env.IsDevelopment())
            {
                app.UseSwaggerDocumentation();
            }
            
            // Endpoint routing with controller endpoints
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
            
            return app;
        }
        
        /// <summary>
        /// Configures Swagger and Swagger UI middleware for API documentation.
        /// </summary>
        /// <param name="app">The application builder instance.</param>
        /// <returns>The application builder with Swagger configured.</returns>
        public static IApplicationBuilder UseSwaggerDocumentation(this IApplicationBuilder app)
        {
            // Use Swagger middleware to generate the OpenAPI document
            app.UseSwagger(c =>
            {
                c.RouteTemplate = "api-docs/{documentName}/swagger.json";
            });
            
            // Use SwaggerUI middleware to serve the Swagger UI
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/api-docs/v1/swagger.json", "Security Patrol API v1");
                c.RoutePrefix = "api-docs";
                c.DocExpansion(DocExpansion.None);
                c.DefaultModelsExpandDepth(-1); // Hide models by default
                c.DisplayRequestDuration();
                
                // Enable OAuth2 security definition
                c.OAuthClientId("swagger-ui");
                c.OAuthAppName("Security Patrol API - Swagger UI");
            });
            
            return app;
        }
        
        /// <summary>
        /// Configures the global exception handling middleware.
        /// </summary>
        /// <param name="app">The application builder instance.</param>
        /// <returns>The application builder with exception handling configured.</returns>
        public static IApplicationBuilder UseApiExceptionHandler(this IApplicationBuilder app)
        {
            // Use the custom ExceptionHandlingMiddleware
            app.UseMiddleware<ExceptionHandlingMiddleware>();
            
            return app;
        }
        
        /// <summary>
        /// Configures the request logging middleware for monitoring and auditing.
        /// </summary>
        /// <param name="app">The application builder instance.</param>
        /// <returns>The application builder with request logging configured.</returns>
        public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
        {
            // Use the custom RequestLoggingMiddleware
            app.UseMiddleware<RequestLoggingMiddleware>();
            
            return app;
        }
        
        /// <summary>
        /// Configures the API key authentication middleware.
        /// </summary>
        /// <param name="app">The application builder instance.</param>
        /// <returns>The application builder with API key authentication configured.</returns>
        public static IApplicationBuilder UseApiKeyAuthentication(this IApplicationBuilder app)
        {
            // Use the custom ApiKeyAuthenticationMiddleware
            app.UseMiddleware<ApiKeyAuthenticationMiddleware>();
            
            return app;
        }
    }
}