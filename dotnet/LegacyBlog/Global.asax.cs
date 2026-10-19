using System;
using System.Diagnostics;
using System.IO;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Web.Routing;
using LegacyBlog.App_Start;
using LegacyBlog.Data;

namespace LegacyBlog
{
    public class MvcApplication : HttpApplication
    {
        protected void Application_Start()
        {
            var logFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LegacyBlogDotNet");
            Directory.CreateDirectory(logFolder);
            Trace.Listeners.Add(new TextWriterTraceListener(Path.Combine(logFolder, "blog.log")));
            Trace.AutoFlush = true;
            AntiForgeryConfig.UniqueClaimTypeIdentifier = System.Security.Claims.ClaimTypes.NameIdentifier;
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            System.Data.Entity.Database.SetInitializer(new System.Data.Entity.CreateDatabaseIfNotExists<BlogContext>());
            using (var db = new BlogContext()) { db.Database.Initialize(false); Bootstrap.Initialize(db); }
        }

        protected void Application_Error()
        {
            var error = Server.GetLastError();
            Trace.TraceError("Unhandled blog request failure: {0}", error);
            Server.ClearError();
            Response.Clear();
            Response.StatusCode = error is HttpException http ? http.GetHttpCode() : 500;
            Response.TrySkipIisCustomErrors = true;
            Response.ContentType = "text/html";
            Response.Write("<!doctype html><html lang=\"en\"><head><title>Request failed</title></head><body><h1>We couldn't complete that request.</h1><p>Check the server log for details.</p><a href=\"/\">Back to the journal</a></body></html>");
            Context.ApplicationInstance.CompleteRequest();
        }
    }
}
