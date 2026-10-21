using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LegacyBlog.Data;

public static class BlogDatabase
{
    public static void Configure(DbContextOptionsBuilder options, IConfiguration configuration, string contentRoot)
    {
        var sqlConnection = configuration["BLOG_DOTNET_CONNECTION_STRING"]
            ?? configuration.GetConnectionString("Blog");
        if (sqlConnection is not null)
        {
            if (string.IsNullOrWhiteSpace(sqlConnection))
                throw new InvalidOperationException("The configured SQL Server connection string must not be empty.");
            options.UseSqlServer(sqlConnection);
            return;
        }

        var localPath = configuration["BLOG_DOTNET_DB_PATH"] ?? Path.Combine(".local", "blog.db");
        if (string.IsNullOrWhiteSpace(localPath))
            throw new InvalidOperationException("BLOG_DOTNET_DB_PATH must name a SQLite database file.");
        var databasePath = Path.GetFullPath(localPath, contentRoot);
        var directory = Path.GetDirectoryName(databasePath)
            ?? throw new InvalidOperationException("The SQLite database path must have a parent directory.");
        Directory.CreateDirectory(directory);
        options.UseSqlite(new SqliteConnectionStringBuilder { DataSource = databasePath, ForeignKeys = true }.ToString());
    }
}
