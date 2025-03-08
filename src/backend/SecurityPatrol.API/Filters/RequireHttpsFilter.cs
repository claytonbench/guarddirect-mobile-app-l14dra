using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;

namespace SecurityPatrol.API.Filters
{
    /// <summary>
    /// ASP.NET Core resource filter that enforces HTTPS for all API requests by redirecting HTTP requests to their HTTPS equivalent.
    /// </summary>
    public class RequireHttpsFilter : IResourceFilter
    {
        private readonly bool _permanentRedirect;

        /// <summary>
        /// Initializes a new instance of the RequireHttpsFilter class with an option to specify whether redirects should be permanent (301) or temporary (307).
        /// </summary>
        /// <param name="permanentRedirect">If true, uses permanent (301) redirects; otherwise uses temporary (307) redirects. Default is true.</param>
        public RequireHttpsFilter(bool permanentRedirect = true)
        {
            _permanentRedirect = permanentRedirect;
        }

        /// <summary>
        /// Executes before the resource is executed, checking if the request uses HTTPS and redirecting to HTTPS if not.
        /// </summary>
        /// <param name="context">The context for the resource being executed.</param>
        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            if (context.HttpContext.Request.IsHttps)
            {
                // Request is already using HTTPS, proceed normally
                return;
            }

            // Request is using HTTP, redirect to HTTPS
            var request = context.HttpContext.Request;
            var host = request.Host;
            var path = request.Path;
            var queryString = request.QueryString;

            // Build the HTTPS URL
            var httpsUrl = $"https://{host}{path}{queryString}";

            // Determine the appropriate redirect status code
            var statusCode = _permanentRedirect ? 
                StatusCodes.Status301MovedPermanently : 
                StatusCodes.Status307TemporaryRedirect;

            // Set the result to a redirect to the HTTPS URL
            context.Result = new RedirectResult(httpsUrl, _permanentRedirect);
        }

        /// <summary>
        /// Executes after the resource has been executed, but performs no operations in this implementation.
        /// </summary>
        /// <param name="context">The context for the resource that was executed.</param>
        public void OnResourceExecuted(ResourceExecutedContext context)
        {
            // No operations needed after execution
        }
    }
}