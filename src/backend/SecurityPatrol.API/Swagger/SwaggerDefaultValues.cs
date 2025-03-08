using Microsoft.AspNetCore.Mvc.ApiExplorer; // v8.0.0
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models; // v1.6.0
using Swashbuckle.AspNetCore.SwaggerGen; // v6.5.0
using System;
using System.Collections.Generic;
using System.Linq; // v8.0.0

namespace SecurityPatrol.API.Swagger
{
    /// <summary>
    /// An implementation of IOperationFilter that applies default values to Swagger operations in the OpenAPI specification.
    /// </summary>
    public class SwaggerDefaultValues : IOperationFilter
    {
        /// <summary>
        /// Applies default values to the Swagger operation based on API description metadata.
        /// </summary>
        /// <param name="operation">The OpenAPI operation to modify.</param>
        /// <param name="context">The context containing API description metadata.</param>
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var apiDescription = context.ApiDescription;

            // Add response types from API description if they're not already specified
            foreach (var responseType in apiDescription.SupportedResponseTypes)
            {
                var responseKey = responseType.StatusCode.ToString();
                
                // Add response if it doesn't exist
                if (!operation.Responses.ContainsKey(responseKey))
                {
                    operation.Responses.Add(responseKey, new OpenApiResponse
                    {
                        Description = responseType.StatusCode switch
                        {
                            200 => "Success",
                            201 => "Created",
                            204 => "No Content",
                            400 => "Bad Request",
                            401 => "Unauthorized",
                            403 => "Forbidden",
                            404 => "Not Found",
                            409 => "Conflict",
                            500 => "Internal Server Error",
                            _ => $"Status code {responseType.StatusCode}"
                        }
                    });
                }
                
                // Ensure content types are specified for responses with a return type
                var response = operation.Responses[responseKey];
                
                if (responseType.Type != null && 
                    (response.Content == null || !response.Content.Any()))
                {
                    response.Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new OpenApiMediaType
                        {
                            Schema = context.SchemaGenerator.GenerateSchema(
                                responseType.Type, 
                                context.SchemaRepository)
                        }
                    };
                }
            }
            
            // Set default 200 OK response if no responses defined
            if (!operation.Responses.Any())
            {
                operation.Responses.Add("200", new OpenApiResponse { Description = "Success" });
            }

            // Set operation as deprecated if the method has ObsoleteAttribute
            if (context.MethodInfo?.GetCustomAttributes(typeof(ObsoleteAttribute), false).Any() == true)
            {
                operation.Deprecated = true;
            }

            // Check for API version attributes that might mark the API as deprecated
            var apiVersionAttributes = context.MethodInfo?.GetCustomAttributes(true)
                .Where(attr => attr.GetType().Name.Contains("ApiVersion"))
                .ToList();

            if (apiVersionAttributes != null)
            {
                foreach (var attr in apiVersionAttributes)
                {
                    var deprecatedProperty = attr.GetType().GetProperty("Deprecated");
                    if (deprecatedProperty != null && 
                        deprecatedProperty.GetValue(attr) is bool isDeprecated && 
                        isDeprecated)
                    {
                        operation.Deprecated = true;
                        break;
                    }
                }
            }

            // Set parameter descriptions and default values
            foreach (var parameter in operation.Parameters)
            {
                var description = apiDescription.ParameterDescriptions
                    .FirstOrDefault(p => string.Equals(p.Name, parameter.Name, StringComparison.OrdinalIgnoreCase));
                
                if (description == null) continue;

                // Set parameter description if not already specified
                if (string.IsNullOrEmpty(parameter.Description))
                {
                    parameter.Description = description.ModelMetadata?.Description;
                }

                // Set default value if available
                if (parameter.Schema != null && 
                    parameter.Schema.Default == null && 
                    description.DefaultValue != null &&
                    description.DefaultValue != DBNull.Value)
                {
                    // Convert the default value to OpenApiAny based on the parameter type
                    parameter.Schema.Default = CreateOpenApiAnyFromDefaultValue(
                        description.DefaultValue, 
                        parameter.Schema.Type);
                }

                // Set required flag based on parameter description
                parameter.Required |= description.IsRequired;
            }

            // Remove parameters that are marked as obsolete or deprecated
            var parametersToRemove = operation.Parameters
                .Where(p => p.Extensions.ContainsKey("x-deprecated") && 
                           (bool)p.Extensions["x-deprecated"])
                .ToList();

            foreach (var parameter in parametersToRemove)
            {
                operation.Parameters.Remove(parameter);
            }
        }

        /// <summary>
        /// Creates an appropriate OpenApiAny instance based on the default value and schema type.
        /// </summary>
        /// <param name="defaultValue">The default value to convert.</param>
        /// <param name="schemaType">The OpenAPI schema type.</param>
        /// <returns>An OpenApiAny instance representing the default value.</returns>
        private OpenApiAny CreateOpenApiAnyFromDefaultValue(object defaultValue, string schemaType)
        {
            if (defaultValue == null) return null;

            return schemaType?.ToLowerInvariant() switch
            {
                "boolean" => new OpenApiBool(Convert.ToBoolean(defaultValue)),
                "integer" => new OpenApiInteger(Convert.ToInt32(defaultValue)),
                "number" => new OpenApiDouble(Convert.ToDouble(defaultValue)),
                "string" => new OpenApiString(defaultValue.ToString()),
                _ => new OpenApiString(defaultValue.ToString())
            };
        }
    }
}