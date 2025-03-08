using Microsoft.AspNetCore.Mvc.Filters;
using SecurityPatrol.Core.Exceptions;
using System.Collections.Generic;
using System.Linq;

namespace SecurityPatrol.API.Filters
{
    /// <summary>
    /// ASP.NET Core action filter that validates the model state before action execution 
    /// and throws a ValidationException when model validation fails.
    /// </summary>
    public class ValidateModelStateFilter : ActionFilterAttribute
    {
        /// <summary>
        /// Default constructor for the ValidateModelStateFilter class.
        /// </summary>
        public ValidateModelStateFilter()
        {
        }

        /// <summary>
        /// Executes before the action method is invoked, checking if the model state is valid
        /// and throwing a ValidationException if not.
        /// </summary>
        /// <param name="context">The context for action execution.</param>
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.ModelState.IsValid)
            {
                // Model state is valid, allow the action to proceed
                return;
            }

            // Model state is invalid, create a dictionary of validation errors
            var errors = context.ModelState
                .Where(kvp => kvp.Value.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );

            // Throw a new ValidationException with the error dictionary
            // This prevents the action from executing and the exception will be caught by ApiExceptionFilter
            throw new ValidationException("One or more validation errors occurred", errors);
        }

        /// <summary>
        /// Executes after the action method has been invoked, but performs no operations in this implementation.
        /// </summary>
        /// <param name="context">The context for action execution.</param>
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            // No operations performed, as validation happens in OnActionExecuting
            base.OnActionExecuted(context);
        }
    }
}