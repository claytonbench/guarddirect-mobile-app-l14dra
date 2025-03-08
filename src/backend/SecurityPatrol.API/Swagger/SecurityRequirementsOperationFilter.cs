using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SecurityPatrol.API.Swagger
{
    /// <summary>
    /// An implementation of IOperationFilter that adds security requirements to Swagger operations based on authorization attributes.
    /// </summary>
    public class SecurityRequirementsOperationFilter : IOperationFilter
    {
        /// <summary>
        /// Applies security requirements to the Swagger operation based on authorization attributes.
        /// </summary>
        /// <param name="operation">The OpenAPI operation.</param>
        /// <param name="context">The operation filter context.</param>
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // Get the controller and action method descriptors
            var controllerType = context.MethodInfo.DeclaringType;
            var actionMethod = context.MethodInfo;
            
            // Check if there's any Authorize attribute on the controller or action
            var hasAuthorizeAttributeOnController = HasAuthorizeAttribute(controllerType);
            var hasAuthorizeAttributeOnAction = HasAuthorizeAttribute(actionMethod);

            if (!hasAuthorizeAttributeOnController && !hasAuthorizeAttributeOnAction)
                return; // No authorization required

            // Get all Authorize attributes (from both controller and action)
            var controllerAttributes = controllerType.GetCustomAttributes(true);
            var actionAttributes = actionMethod.GetCustomAttributes(true);
            
            var authorizeAttributes = controllerAttributes.OfType<AuthorizeAttribute>()
                .Concat(actionAttributes.OfType<AuthorizeAttribute>());

            // Create security requirement
            var securityRequirement = new OpenApiSecurityRequirement
            {
                {
                    GetSecuritySchemeReference(),
                    new List<string>()
                }
            };

            // Add the security requirement to the operation
            operation.Security ??= new List<OpenApiSecurityRequirement>();
            operation.Security.Add(securityRequirement);

            // Check if there are specific policy requirements
            var policies = authorizeAttributes
                .Where(attr => !string.IsNullOrWhiteSpace(attr.Policy))
                .Select(attr => attr.Policy)
                .Distinct();

            if (policies.Any())
            {
                // Add the policies to the operation description for better documentation
                if (string.IsNullOrWhiteSpace(operation.Description))
                {
                    operation.Description = "Security policies required: " + string.Join(", ", policies);
                }
                else
                {
                    operation.Description += "\r\n\r\nSecurity policies required: " + string.Join(", ", policies);
                }
            }
        }

        /// <summary>
        /// Determines if the given member info has an Authorize attribute.
        /// </summary>
        /// <param name="memberInfo">The member to check.</param>
        /// <returns>True if the member has an Authorize attribute, otherwise false.</returns>
        private bool HasAuthorizeAttribute(MemberInfo memberInfo)
        {
            if (memberInfo == null)
                return false;
            
            return memberInfo.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any();
        }

        /// <summary>
        /// Creates a reference to the JWT Bearer security scheme defined in the OpenAPI document.
        /// </summary>
        /// <returns>The security scheme reference for JWT Bearer authentication.</returns>
        private OpenApiSecurityScheme GetSecuritySchemeReference()
        {
            return new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            };
        }
    }
}