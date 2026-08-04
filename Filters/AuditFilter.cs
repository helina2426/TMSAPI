using Microsoft.AspNetCore.Mvc.Filters;

namespace TmsApi.Filters;

public class AuditFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var resultContext = await next();

        resultContext.HttpContext.Response.Headers["X-Audit"] =
            $"User={context.HttpContext.User.Identity?.Name ?? "anonymous"}";
    }
}