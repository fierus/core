using Core.Exceptions;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Linq;

namespace Core.Attributes
{
    public class ValidateModelStateAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                var errorList = context.ModelState.Values
                    .SelectMany(m => m.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                var errorText = string.Join(Environment.NewLine, errorList);

                throw new InvalidModelStateException(errorText);
            }
        }
    }
}