using System.Net;
using System.Text.RegularExpressions;
using LegacyBlog.Data;
using LegacyBlog.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace LegacyBlog.Tests;

public sealed class BlogTests : IDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private readonly WebApplicationFactory<Program> factory;
    private readonly HttpClient client;
    private readonly string? previousUser;
    private readonly string? previousPassword;
    private readonly string? previousConnection;
    private string? configuredSqlConnection;

    public BlogTests()
    {
        connection.Open();
        previousUser = Environment.GetEnvironmentVariable("BLOG_ADMIN_USER");
        previousPassword = Environment.GetEnvironmentVariable("BLOG_ADMIN_PASSWORD");
        previousConnection = Environment.GetEnvironmentVariable("BLOG_DOTNET_CONNECTION_STRING");
        Environment.SetEnvironmentVariable("BLOG_ADMIN_USER", "test-admin");
        Environment.SetEnvironmentVariable("BLOG_ADMIN_PASSWORD", "test-password-1234");
        Environment.SetEnvironmentVariable("BLOG_DOTNET_CONNECTION_STRING", "Server=localhost;Database=test;Integrated Security=true");
        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            using (var configuredServices = services.BuildServiceProvider())
            using (var sqlContext = configuredServices.GetRequiredService<BlogContext>())
            {
                Assert.True(sqlContext.Database.IsSqlServer());
                configuredSqlConnection = sqlContext.Database.GetConnectionString();
            }
            services.RemoveAll<DbContextOptions<BlogContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<BlogContext>>();
            services.AddDbContext<BlogContext>(options => options.UseSqlite(connection));
        }));
        client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    [Fact]
    public async Task PublicRoutesAndErrorStatuses()
    {
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/posts/legacy-notes-1")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Content/site.css")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/categories/engineering")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/posts/missing")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/categories/missing")).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync("/admin/posts/999/edit")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/?page=invalid")).StatusCode);
        Assert.Equal(HttpStatusCode.InternalServerError, (await client.GetAsync("/error")).StatusCode);
    }

    [Fact]
    public async Task AuthAntiforgeryAndSeedRemainStable()
    {
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync("/admin")).StatusCode);
        var response = await client.GetAsync("/account/login");
        var body = await response.Content.ReadAsStringAsync();
        var token = Regex.Match(body, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        Assert.NotEmpty(token);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/account/login", new FormUrlEncodedContent(new Dictionary<string, string> { ["Username"] = "test-admin", ["Password"] = "test-password-1234" }))).StatusCode);
        var login = await client.PostAsync("/account/login", new FormUrlEncodedContent(new Dictionary<string, string> {
            ["Username"] = "test-admin", ["Password"] = "test-password-1234", ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        Assert.Equal("/admin", login.Headers.Location?.OriginalString);
        Assert.Contains(login.Headers.GetValues("Set-Cookie"), c => c.StartsWith("LegacyBlogDotNet="));
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogContext>();
        Assert.Equal(14, db.Posts.Count());
        Assert.Equal(2, db.Comments.Count());
        Bootstrap.Initialize(db);
        Assert.Equal(14, db.Posts.Count());
        Assert.Equal(2, db.Comments.Count());
    }

    [Fact]
    public async Task AdminCanPublishAndModerateWithAntiForgery()
    {
        var loginHtml = await client.GetStringAsync("/account/login");
        var loginToken = Token(loginHtml);
        var login = await client.PostAsync("/account/login", Form(new()
        {
            ["Username"] = "test-admin", ["Password"] = "test-password-1234", ["__RequestVerificationToken"] = loginToken
        }));
        var cookie = login.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("LegacyBlogDotNet=")).Split(';')[0];
        client.DefaultRequestHeaders.Add("Cookie", cookie);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/admin")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/admin/posts/999/edit")).StatusCode);
        var category = await client.PostAsync("/admin/categories/create", Form(new()
        {
            ["Id"] = "1", ["Name"] = "New category", ["Slug"] = "new-category",
            ["__RequestVerificationToken"] = Token(await client.GetStringAsync("/admin/categories/create"))
        }));
        Assert.Equal(HttpStatusCode.Redirect, category.StatusCode);
        var formHtml = await client.GetStringAsync("/admin/posts/create");
        var token = Token(formHtml);
        var invalid = await client.PostAsync("/admin/posts/create", Form(new()
        {
            ["Title"] = "Example", ["Slug"] = "legacy-notes-1", ["Body"] = "Content",
            ["CategoryId"] = "1", ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.OK, invalid.StatusCode);
        var saved = await client.PostAsync("/admin/posts/create", Form(new()
        {
            ["Title"] = "New story", ["Slug"] = "new-story", ["Body"] = "Content",
            ["CategoryId"] = "1", ["Id"] = "1", ["Published"] = "true", ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.Redirect, saved.StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogContext>();
        Assert.Equal("Engineering", db.Categories.Single(c => c.Id == 1).Name);
        Assert.NotEqual(1, db.Categories.Single(c => c.Slug == "new-category").Id);
        Assert.Equal("Legacy notes 1", db.Posts.Single(p => p.Id == 1).Title);
        var post = db.Posts.Single(p => p.Slug == "new-story");
        Assert.False(post.Published);
        Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync($"/admin/posts/{post.Id}/publish", Form(new() { ["Id"] = "1", ["__RequestVerificationToken"] = token }))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/posts/new-story")).StatusCode);
        var detail = await client.GetStringAsync("/posts/new-story");
        Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync("/posts/new-story/comments", Form(new()
        {
            ["Author"] = "Reader", ["Body"] = "Great work", ["Slug"] = "legacy-notes-1", ["__RequestVerificationToken"] = Token(detail)
        }))).StatusCode);
        var comment = db.Comments.Single(c => c.PostId == post.Id);
        Assert.False(comment.Approved);
        Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync($"/admin/comments/{comment.Id}/approve", Form(new() { ["Id"] = "1", ["__RequestVerificationToken"] = token }))).StatusCode);
        db.Entry(comment).Reload();
        Assert.True(comment.Approved);
    }

    [Fact]
    public void ExistingIdentity2UserAndContentSurviveSeed()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogContext>();
        var user = db.Users.Single();
        var hash = user.PasswordHash;
        var post = db.Posts.First();
        post.Title = "Retained title";
        db.UserRoles.RemoveRange(db.UserRoles);
        db.SaveChanges();
        Bootstrap.Initialize(db);
        Assert.Equal(hash, db.Users.Single().PasswordHash);
        Assert.Equal("Retained title", db.Posts.Single(p => p.Id == post.Id).Title);
        Assert.Single(db.UserRoles);
        Assert.Equal("Admin", db.UserRoles.Include(r => r.Role).Single().Role.Name);
    }

    [Fact]
    public void PartialRoleMembershipsGainAdminWithoutLosingOtherRolesOrDuplicatingSeed()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogContext>();
        var seededAdmin = db.Users.Single();
        var editor = new BlogRole { Name = "Editor" };
        var editorUser = new BlogUser { UserName = "existing-editor" };
        var unassignedUser = new BlogUser { UserName = "existing-unassigned" };
        db.Users.AddRange(editorUser, unassignedUser);
        db.Roles.Add(editor);
        db.UserRoles.Add(new BlogUserRole { User = editorUser, Role = editor });
        db.SaveChanges();

        Bootstrap.Initialize(db);

        var adminId = db.Roles.Single(r => r.Name == "Admin").Id;
        Assert.Equal(3, db.Users.Count());
        Assert.Equal(3, db.UserRoles.Count(r => r.RoleId == adminId));
        Assert.Contains(db.UserRoles, r => r.UserId == seededAdmin.Id && r.RoleId == adminId);
        Assert.Contains(db.UserRoles, r => r.UserId == editorUser.Id && r.RoleId == adminId);
        Assert.Contains(db.UserRoles, r => r.UserId == unassignedUser.Id && r.RoleId == adminId);
        Assert.Contains(db.UserRoles, r => r.UserId == editorUser.Id && r.RoleId == editor.Id);
        Assert.Equal(14, db.Posts.Count());
        Assert.Equal(2, db.Comments.Count());

        Bootstrap.Initialize(db);

        Assert.Equal(2, db.Roles.Count());
        Assert.Equal(4, db.UserRoles.Count());
        Assert.Equal(3, db.UserRoles.Count(r => r.RoleId == adminId));
        Assert.Equal(14, db.Posts.Count());
        Assert.Equal(2, db.Comments.Count());
    }

    [Fact]
    public void ExistingNonAdminMembershipRecreatesMissingAdminRoleIdempotently()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogContext>();
        var user = db.Users.Single();
        db.UserRoles.RemoveRange(db.UserRoles);
        db.Roles.RemoveRange(db.Roles);
        var editor = new BlogRole { Name = "Editor" };
        db.Roles.Add(editor);
        db.UserRoles.Add(new BlogUserRole { User = user, Role = editor });
        db.SaveChanges();

        Bootstrap.Initialize(db);
        Bootstrap.Initialize(db);

        Assert.Equal(2, db.Roles.Count());
        Assert.Equal(2, db.UserRoles.Count());
        Assert.Equal(new[] { "Admin", "Editor" }, db.UserRoles.Include(r => r.Role).Select(r => r.Role.Name).OrderBy(name => name).ToArray());
    }

    [Fact]
    public void ExplicitSqlServerConnectionStillSupportsConfigurationOverride()
    {
        var configuration = factory.Services.GetRequiredService<IConfiguration>();
        Assert.Null(configuration.GetConnectionString("Blog"));
        var sqlConnection = new SqlConnectionStringBuilder(configuredSqlConnection);
        Assert.Equal("localhost", sqlConnection.DataSource);
        Assert.Equal("test", sqlConnection.InitialCatalog);
        Assert.True(sqlConnection.IntegratedSecurity);

        var overrideConfiguration = new ConfigurationBuilder()
            .AddConfiguration(configuration)
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Blog"] = "Server=production-sql;Database=Blog;Integrated Security=True"
            })
            .Build();
        Assert.Equal("Server=production-sql;Database=Blog;Integrated Security=True",
            overrideConfiguration.GetConnectionString("Blog"));
    }

    [Fact]
    public async Task LocalSqliteSeedsOnceAndRetainsDataAndLoginAcrossHostRestarts()
    {
        var directory = Path.Combine(Path.GetTempPath(), "blog-local-" + Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(directory, "blog.db");
        var username = Environment.GetEnvironmentVariable("BLOG_ADMIN_USER");
        var password = Environment.GetEnvironmentVariable("BLOG_ADMIN_PASSWORD");
        string? passwordHash = null;
        int[] postIds = [];
        try
        {
            using (var localFactory = LocalDatabaseFactory(databasePath))
            using (var browser = localFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }))
            {
                Assert.Equal(HttpStatusCode.OK, (await browser.GetAsync("/")).StatusCode);
                using var scope = localFactory.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<BlogContext>();
                Assert.True(db.Database.IsSqlite());
                Assert.Equal(2, db.Categories.Count());
                Assert.Equal(12, db.Posts.Count(p => p.Published));
                Assert.Equal(2, db.Posts.Count(p => !p.Published));
                Assert.Single(db.Comments.Where(c => c.Approved));
                Assert.Single(db.Comments.Where(c => !c.Approved));
                passwordHash = db.Users.Single().PasswordHash;
                db.Posts.Single(p => p.Slug == "legacy-notes-1").Title = "Retained local edit";
                db.Posts.Add(new Post
                {
                    Title = "Persistent local post", Slug = "persistent-local-post", Body = "Retained local content",
                    Category = db.Categories.First(), Published = true, CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow, PublishedAt = DateTime.UtcNow
                });
                db.SaveChanges();
                postIds = db.Posts.OrderBy(p => p.Id).Select(p => p.Id).ToArray();
            }
            Assert.True(File.Exists(databasePath));
            Environment.SetEnvironmentVariable("BLOG_ADMIN_USER", null);
            Environment.SetEnvironmentVariable("BLOG_ADMIN_PASSWORD", null);
            using var restarted = LocalDatabaseFactory(databasePath);
            using var client = restarted.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            Assert.Contains("Retained local content", await client.GetStringAsync("/posts/persistent-local-post"));
            Assert.Contains("Retained local edit", await client.GetStringAsync("/posts/legacy-notes-1"));
            using var restartedScope = restarted.Services.CreateScope();
            var persisted = restartedScope.ServiceProvider.GetRequiredService<BlogContext>();
            Assert.Equal(postIds, persisted.Posts.OrderBy(p => p.Id).Select(p => p.Id).ToArray());
            Assert.Equal(2, persisted.Categories.Count());
            Assert.Equal(2, persisted.Comments.Count());
            Assert.Single(persisted.SeedStates);
            Assert.Equal(passwordHash, persisted.Users.Single().PasswordHash);
            var login = await client.PostAsync("/account/login", Form(new()
            {
                ["Username"] = username ?? throw new InvalidOperationException("Missing test username."),
                ["Password"] = password ?? throw new InvalidOperationException("Missing test password."),
                ["__RequestVerificationToken"] = Token(await client.GetStringAsync("/account/login"))
            }));
            Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
            Assert.Equal("/admin", login.Headers.Location?.OriginalString);
        }
        finally
        {
            Environment.SetEnvironmentVariable("BLOG_ADMIN_USER", username);
            Environment.SetEnvironmentVariable("BLOG_ADMIN_PASSWORD", password);
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
    }

    private static WebApplicationFactory<Program> LocalDatabaseFactory(string path) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["BLOG_DOTNET_CONNECTION_STRING"] = null,
                    ["ConnectionStrings:Blog"] = null,
                    ["BLOG_DOTNET_DB_PATH"] = path
                })));

    [Fact]
    public void FreshLocalDatabaseStillRequiresAdministratorCredentials()
    {
        var directory = Path.Combine(Path.GetTempPath(), "blog-credentials-" + Guid.NewGuid().ToString("N"));
        var username = Environment.GetEnvironmentVariable("BLOG_ADMIN_USER");
        var password = Environment.GetEnvironmentVariable("BLOG_ADMIN_PASSWORD");
        try
        {
            Environment.SetEnvironmentVariable("BLOG_ADMIN_USER", null);
            Environment.SetEnvironmentVariable("BLOG_ADMIN_PASSWORD", null);
            using var local = LocalDatabaseFactory(Path.Combine(directory, "blog.db"));
            var error = Assert.Throws<InvalidOperationException>(() => local.CreateClient());
            Assert.Contains("BLOG_ADMIN_USER", error.Message);
            Assert.Contains("BLOG_ADMIN_PASSWORD", error.Message);
        }
        finally
        {
            Environment.SetEnvironmentVariable("BLOG_ADMIN_USER", username);
            Environment.SetEnvironmentVariable("BLOG_ADMIN_PASSWORD", password);
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
    }

    private static string Token(string html)
    {
        var value = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        Assert.NotEmpty(value);
        return value;
    }
    private static FormUrlEncodedContent Form(Dictionary<string, string> fields) => new(fields);

    [Fact]
    public void SqlServerFreshDatabaseCreatesAndSeedsWithoutDuplicates()
    {
        var server = Environment.GetEnvironmentVariable("BLOG_TEST_SQLSERVER_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(server)) return; // Opt-in: SQL Server need not be installed on CI.
        var databaseName = "LegacyBlogUpgradeTest_" + Guid.NewGuid().ToString("N");
        var builder = new SqlConnectionStringBuilder(server) { InitialCatalog = databaseName };
        Assert.True(builder.IntegratedSecurity); // Never use this test against a shared SQL login.
        using var db = new BlogContext(new DbContextOptionsBuilder<BlogContext>().UseSqlServer(builder.ConnectionString).Options);
        try
        {
            Bootstrap.Initialize(db);
            Bootstrap.Initialize(db);
            Assert.Equal(14, db.Posts.Count());
            Assert.Equal(2, db.Comments.Count());
            Assert.Single(db.UserRoles);
        }
        finally
        {
            db.Database.CloseConnection();
            using var master = new SqlConnection(new SqlConnectionStringBuilder(server) { InitialCatalog = "master" }.ConnectionString);
            master.Open();
            using var command = master.CreateCommand();
            command.CommandText = $"IF DB_ID('{databaseName}') IS NOT NULL BEGIN ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{databaseName}]; END";
            command.ExecuteNonQuery();
        }
    }

    public void Dispose()
    {
        client.Dispose();
        factory.Dispose();
        connection.Dispose();
        Environment.SetEnvironmentVariable("BLOG_ADMIN_USER", previousUser);
        Environment.SetEnvironmentVariable("BLOG_ADMIN_PASSWORD", previousPassword);
        Environment.SetEnvironmentVariable("BLOG_DOTNET_CONNECTION_STRING", previousConnection);
    }
}
