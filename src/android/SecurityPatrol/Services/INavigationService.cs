using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SecurityPatrol.Services
{
    /// <summary>
    /// Interface that defines navigation operations for the Security Patrol application.
    /// Provides methods for navigating between pages, passing parameters, and managing the navigation stack using MAUI Shell navigation.
    /// </summary>
    public interface INavigationService
    {
        /// <summary>
        /// Navigates to the specified route with optional parameters.
        /// </summary>
        /// <param name="route">The route to navigate to.</param>
        /// <param name="parameters">Optional parameters to pass to the destination page.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task NavigateToAsync(string route, Dictionary<string, object> parameters = null);
        
        /// <summary>
        /// Navigates back to the previous page.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task NavigateBackAsync();
        
        /// <summary>
        /// Navigates to the root/main page of the application.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task NavigateToRootAsync();
        
        /// <summary>
        /// Opens a page as a modal dialog with optional parameters.
        /// </summary>
        /// <param name="route">The route to the page to open as modal.</param>
        /// <param name="parameters">Optional parameters to pass to the modal page.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task NavigateToModalAsync(string route, Dictionary<string, object> parameters = null);
        
        /// <summary>
        /// Closes the current modal dialog.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task CloseModalAsync();
        
        /// <summary>
        /// Gets the current navigation route.
        /// </summary>
        /// <returns>The current route as a string.</returns>
        string GetCurrentRoute();
        
        /// <summary>
        /// Gets a parameter value from the current route.
        /// </summary>
        /// <param name="parameterName">The name of the parameter to retrieve.</param>
        /// <returns>The parameter value if found; otherwise, null.</returns>
        object GetRouteParameter(string parameterName);
    }
}