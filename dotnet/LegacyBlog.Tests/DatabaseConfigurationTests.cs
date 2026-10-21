using LegacyBlog.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace LegacyBlog.Tests;

public sealed class DatabaseConfigurationTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "blog-database-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void DefaultIsPersistentSqliteUnderTheContentRoot()
    {
        using var db = CreateContext(new Dictionary<string, string?>());
        Assert.True(db.Database.IsSqlite());
        var connection = new SqliteConnectionStringBuilder(db.Database.GetConnectionString());
        Assert.Equal(Path.Combine(root, ".local", "blog.db"), connection.DataSource);
        Assert.True(connection.ForeignKeys);
        Assert.True(Directory.Exists(Path.Combine(root, ".local")));
        db.Database.EnsureCreated();
        Assert.True(File.Exists(connection.DataSource));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CustomSqlitePathCanBeRelativeOrAbsolute(bool absolute)
    {
        var relativePath = Path.Combine("custom data", "blog.db");
        var expected = Path.Combine(root, relativePath);
        using var db = CreateContext(new Dictionary<string, string?>
        {
            ["BLOG_DOTNET_DB_PATH"] = absolute ? expected : relativePath
        });
        Assert.Equal(expected, new SqliteConnectionStringBuilder(db.Database.GetConnectionString()).DataSource);
        Assert.True(Directory.Exists(Path.GetDirectoryName(expected)));
    }

    [Theory]
    [InlineData("BLOG_DOTNET_CONNECTION_STRING")]
    [InlineData("ConnectionStrings:Blog")]
    public void ExplicitSqlServerConnectionDoesNotCreateALocalDatabase(string key)
    {
        const string connection = "Server=explicit-sql;Database=Blog;Integrated Security=True";
        using var db = CreateContext(new Dictionary<string, string?> { [key] = connection });
        Assert.True(db.Database.IsSqlServer());
        var configured = new SqlConnectionStringBuilder(db.Database.GetConnectionString());
        Assert.Equal("explicit-sql", configured.DataSource);
        Assert.Equal("Blog", configured.InitialCatalog);
        Assert.True(configured.IntegratedSecurity);
        Assert.False(Directory.Exists(root));
    }

    [Fact]
    public void EnvironmentStyleConnectionTakesPriorityOverNamedConnection()
    {
        const string connection = "Server=explicit-sql;Database=EnvironmentBlog;Integrated Security=True";
        using var db = CreateContext(new Dictionary<string, string?>
        {
            ["BLOG_DOTNET_CONNECTION_STRING"] = connection,
            ["ConnectionStrings:Blog"] = "Server=other-sql;Database=OtherBlog;Integrated Security=True"
        });
        Assert.True(db.Database.IsSqlServer());
        var configured = new SqlConnectionStringBuilder(db.Database.GetConnectionString());
        Assert.Equal("explicit-sql", configured.DataSource);
        Assert.Equal("EnvironmentBlog", configured.InitialCatalog);
        Assert.True(configured.IntegratedSecurity);
    }

    [Theory]
    [InlineData("BLOG_DOTNET_CONNECTION_STRING", "")]
    [InlineData("BLOG_DOTNET_CONNECTION_STRING", " ")]
    [InlineData("ConnectionStrings:Blog", "")]
    [InlineData("BLOG_DOTNET_DB_PATH", "")]
    [InlineData("BLOG_DOTNET_DB_PATH", " ")]
    public void EmptyExplicitConfigurationFailsInsteadOfChangingProvider(string key, string value)
    {
        Assert.Throws<InvalidOperationException>(() =>
        {
            using var db = CreateContext(new Dictionary<string, string?> { [key] = value });
        });
        Assert.False(Directory.Exists(root));
    }

    private BlogContext CreateContext(Dictionary<string, string?> values)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var options = new DbContextOptionsBuilder<BlogContext>();
        BlogDatabase.Configure(options, configuration, root);
        return new BlogContext(options.Options);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(root))
            Directory.Delete(root, true);
    }
}
