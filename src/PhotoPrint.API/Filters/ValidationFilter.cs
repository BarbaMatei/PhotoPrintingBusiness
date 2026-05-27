using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PhotoPrint.API.Filters;

public class ValidationFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ModelState.IsValid)
        {
            return;
        }

        var errors = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .SelectMany(entry => entry.Value!.Errors.Select(error => new
            {
                field = JsonNamingPolicy.CamelCase.ConvertName(entry.Key),
                message = error.ErrorMessage,
            }))
            .ToList();

        context.Result = new ObjectResult(new { errors }) { StatusCode = StatusCodes.Status422UnprocessableEntity };
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
