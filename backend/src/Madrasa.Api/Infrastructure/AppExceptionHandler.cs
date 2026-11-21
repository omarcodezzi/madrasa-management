using Madrasa.Application.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Madrasa.Api.Infrastructure;

/// <summary>Turns AppException into ProblemDetails with the Bangla title and per-field errors.</summary>
public class AppExceptionHandler(IProblemDetailsService problems) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        if (ex is not AppException app) return false;
        ctx.Response.StatusCode = app.Status;
        var pd = new ProblemDetails { Status = app.Status, Title = app.Message };
        if (app.FieldErrors is not null) pd.Extensions["errors"] = app.FieldErrors;
        return await problems.TryWriteAsync(new ProblemDetailsContext { HttpContext = ctx, ProblemDetails = pd });
    }
}
