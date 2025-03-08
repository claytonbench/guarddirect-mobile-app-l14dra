using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Maui.Controls;

namespace SecurityPatrol.Services
{
    /// <summary>
    /// Implementation of INavigationService that provides navigation functionality using MAUI Shell.
    /// Handles page navigation, parameter passing, and navigation stack management.
    /// </summary>
    public class NavigationService : INavigationService
    {
        /// <summary>
        /// Navigates to the specified route with optional parameters.
        /// </summary>
        /// <param name="route">The route to navigate to.</param>
        /// <param name="parameters">Optional parameters to pass to the destination page.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task NavigateToAsync(string route, Dictionary<string, object> parameters = null)
        {
            if (string.IsNullOrEmpty(route))
                throw new ArgumentException("Route cannot be null or empty", nameof(route));

            string navigationRoute = route;
            
            if (parameters != null && parameters.Count > 0)
            {
                string queryString = BuildQueryString(parameters);
                navigationRoute += queryString;
            }

            await Shell.Current.GoToAsync(navigationRoute);
        }

        /// <summary>
        /// Navigates back to the previous page.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task NavigateBackAsync()
        {
            await Shell.Current.GoToAsync("..");
        }

        /// <summary>
        /// Navigates to the root/main page of the application.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task NavigateToRootAsync()
        {
            await Shell.Current.GoToAsync("//");
        }

        /// <summary>
        /// Opens a page as a modal dialog with optional parameters.
        /// </summary>
        /// <param name="route">The route to the page to open as modal.</param>
        /// <param name="parameters">Optional parameters to pass to the modal page.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task NavigateToModalAsync(string route, Dictionary<string, object> parameters = null)
        {
            if (string.IsNullOrEmpty(route))
                throw new ArgumentException("Route cannot be null or empty", nameof(route));

            string navigationRoute = route;
            
            if (parameters != null && parameters.Count > 0)
            {
                string queryString = BuildQueryString(parameters);
                navigationRoute += queryString;
            }

            // In .NET MAUI 8.0+, there is an overload of GoToAsync that takes a boolean parameter
            // to indicate whether the navigation should be modal
            await Shell.Current.GoToAsync(navigationRoute, true);
        }

        /// <summary>
        /// Closes the current modal dialog.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task CloseModalAsync()
        {
            await Shell.Current.GoToAsync("..");
        }

        /// <summary>
        /// Gets the current navigation route.
        /// </summary>
        /// <returns>The current route as a string.</returns>
        public string GetCurrentRoute()
        {
            var location = Shell.Current.CurrentState.Location;
            // Extract just the route part (without query parameters)
            return location.OriginalString.Split('?')[0];
        }

        /// <summary>
        /// Gets a parameter value from the current route.
        /// </summary>
        /// <param name="parameterName">The name of the parameter to retrieve.</param>
        /// <returns>The parameter value if found; otherwise, null.</returns>
        public object GetRouteParameter(string parameterName)
        {
            if (string.IsNullOrEmpty(parameterName))
                throw new ArgumentException("Parameter name cannot be null or empty", nameof(parameterName));

            var queryString = Shell.Current.CurrentState.Location.Query;
            
            if (string.IsNullOrEmpty(queryString))
                return null;

            // Parse the query string
            var queryDictionary = HttpUtility.ParseQueryString(queryString);
            
            if (queryDictionary[parameterName] != null)
                return queryDictionary[parameterName];
            
            return null;
        }

        /// <summary>
        /// Builds a query string from a dictionary of parameters.
        /// </summary>
        /// <param name="parameters">Dictionary of parameters to convert to a query string.</param>
        /// <returns>Formatted query string.</returns>
        private string BuildQueryString(Dictionary<string, object> parameters)
        {
            if (parameters == null || parameters.Count == 0)
                return string.Empty;

            var queryBuilder = new StringBuilder("?");
            bool isFirst = true;

            foreach (var parameter in parameters)
            {
                if (!isFirst)
                    queryBuilder.Append("&");
                
                queryBuilder.Append(HttpUtility.UrlEncode(parameter.Key));
                queryBuilder.Append("=");
                queryBuilder.Append(HttpUtility.UrlEncode(parameter.Value?.ToString() ?? string.Empty));
                
                isFirst = false;
            }

            return queryBuilder.ToString();
        }
    }
}