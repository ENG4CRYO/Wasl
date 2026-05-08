using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Results;
using Wasl.Application.Common;
using FluentValidation.Results;

namespace Wasl.api.Factories
{
   
    public class CustomResultFactory : IFluentValidationAutoValidationResultFactory
    {
        public async Task<IActionResult?> CreateActionResult(ActionExecutingContext context,
            ValidationProblemDetails validationProblemDetails,
            IDictionary<FluentValidation.IValidationContext,
                ValidationResult> validationResults)
        {
            var errors = context.ModelState
                .Where(e => e.Value.Errors.Count > 0)
                .ToDictionary(
                      kvp => kvp.Key,
                      kvp => kvp.Value.Errors
                    .Select(e => e.ErrorMessage)
                    .ToList()
                );


            var response = ApiResponse<object>.Failure("", errors);


            return new BadRequestObjectResult(response);
        }
    }
}