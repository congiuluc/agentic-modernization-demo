using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace LegacyBlog.Controllers;

public class ErrorController(ILogger<ErrorController> logger) : Controller
{
    [Route("/failure")]
    public IActionResult Failure()
    {
        var error = HttpContext.Features.Get<IExceptionHandlerPathFeature>()?.Error;
        var sql = (error as DbUpdateException)?.GetBaseException() as SqlException;
        var status = sql != null && sql.Number is 2601 or 2627 or 547 ? 409 :
            error is BadHttpRequestException bad ? bad.StatusCode : 500;
        if (status >= 500) logger.LogError(error, "Blog request failed");
        else logger.LogWarning("Blog request rejected ({Status}): {Reason}", status, error?.Message);
        if (HttpContext.Features.Get<IStatusCodePagesFeature>() is { } pages) pages.Enabled = false;
        Response.StatusCode = status;
        return View("~/Views/Shared/Error.cshtml", status == 409
            ? "This change conflicts with existing data. Reload the form and try again."
            : status >= 500 ? "An unexpected error occurred. Check the server log."
            : error?.Message ?? "The request was rejected.");
    }

    [Route("/status/{code:int}")]
    public IActionResult Status(int code)
    {
        Response.StatusCode = code;
        return View("~/Views/Shared/Error.cshtml", code == 404 ? "The requested page was not found." : "The request was rejected.");
    }
}
