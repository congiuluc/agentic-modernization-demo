using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Web;
using System.Web.Mvc;
using LegacyBlog.Data;
using LegacyBlog.Repositories;
using LegacyBlog.Services;

namespace LegacyBlog.Controllers
{
    public abstract class BlogController : Controller
    {
        protected readonly BlogContext Db = new BlogContext();
        protected readonly BlogService Blogs;
        protected BlogController() { Blogs = new BlogService(new BlogRepository(Db)); }
        protected override void Dispose(bool disposing) { if (disposing) Db.Dispose(); base.Dispose(disposing); }

        protected override void OnException(ExceptionContext context)
        {
            var http = context.Exception as HttpException;
            var status = context.Exception is HttpAntiForgeryException ? 400 : (http == null ? 500 : http.GetHttpCode());
            var update = context.Exception as DbUpdateException;
            var sql = update == null ? null : update.GetBaseException() as SqlException;
            if (sql != null && (sql.Number == 2601 || sql.Number == 2627 || sql.Number == 547)) status = 409;
            if (status >= 500) Trace.TraceError("Blog request failed: {0}", context.Exception);
            else Trace.TraceWarning("Blog request rejected ({0}): {1}", status, context.Exception.Message);
            Response.StatusCode = status; Response.TrySkipIisCustomErrors = true;
            context.ExceptionHandled = true;
            context.Result = new ViewResult
            {
                ViewName = "~/Views/Shared/Error.cshtml",
                ViewData = new ViewDataDictionary<string>(status == 409 ? "This change conflicts with existing data. Reload the form and try again." : status >= 500 ? "An unexpected error occurred. Check the server log." : http == null ? "The request was rejected." : http.Message)
            };
        }
    }
}
