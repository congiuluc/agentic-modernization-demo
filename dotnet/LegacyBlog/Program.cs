using LegacyBlog.Data;
using LegacyBlog.Repositories;
using LegacyBlog.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);
var connection = builder.Configuration["BLOG_DOTNET_CONNECTION_STRING"]
    ?? builder.Configuration.GetConnectionString("Blog")
    ?? throw new InvalidOperationException("Configure ConnectionStrings:Blog or BLOG_DOTNET_CONNECTION_STRING with a SQL Server connection string.");
builder.Services.AddDbContext<BlogContext>(options => options.UseSqlServer(connection));
builder.Services.AddScoped<BlogRepository>();
builder.Services.AddScoped<BlogService>();
builder.Services.AddControllersWithViews();
builder.Services.AddAntiforgery(options => options.Cookie.SameSite = SameSiteMode.Lax);
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.Cookie.Name = "LegacyBlogDotNet";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.LoginPath = "/account/login";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = false;
    options.AccessDeniedPath = "/account/denied";
});
builder.Services.AddAuthorization();
var app = builder.Build();
app.UseExceptionHandler("/failure");
app.UseStatusCodePagesWithReExecute("/status/{0}");
app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers.XFrameOptions = "DENY";
    await next(context);
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(app.Environment.ContentRootPath, "Content")),
    RequestPath = "/Content"
});
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
using (var scope = app.Services.CreateScope())
    Bootstrap.Initialize(scope.ServiceProvider.GetRequiredService<BlogContext>());
app.Run();

public partial class Program { }
