using LegacyBlog.Controllers;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LegacyBlog.Tests;

public class ErrorControllerTests
{
    [Theory]
    [InlineData(1555, 409)]
    [InlineData(2067, 409)]
    [InlineData(787, 409)]
    [InlineData(275, 409)]
    [InlineData(1299, 500)]
    public void SqliteConflictsHaveTheSameResponseAsSqlServerConstraints(int extendedCode, int expectedStatus)
    {
        var context = new DefaultHttpContext();
        context.Features.Set<IExceptionHandlerPathFeature>(new ExceptionHandlerFeature
        {
            Error = new DbUpdateException("Test constraint failure.", new SqliteException("Constraint failed.", 19, extendedCode)),
            Path = "/admin/posts/create"
        });
        var controller = new ErrorController(NullLogger<ErrorController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };

        var result = Assert.IsType<ViewResult>(controller.Failure());

        Assert.Equal(expectedStatus, context.Response.StatusCode);
        Assert.Equal(expectedStatus == 409
            ? "This change conflicts with existing data. Reload the form and try again."
            : "An unexpected error occurred. Check the server log.", result.Model);
    }
}
