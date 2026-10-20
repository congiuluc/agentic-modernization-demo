using System.Text;
using System.Text.RegularExpressions;
using LegacyBlog.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LegacyBlog.Data;

public static class Bootstrap
{
    public static void Initialize(BlogContext db)
    {
        db.Database.EnsureCreated(); // Never drops or migrates an existing SQL Server database.
        using var transaction = db.Database.BeginTransaction();
        if (!db.Users.Any())
        {
            var username = Environment.GetEnvironmentVariable("BLOG_ADMIN_USER") ?? "";
            var password = Environment.GetEnvironmentVariable("BLOG_ADMIN_PASSWORD") ?? "";
            if (!Regex.IsMatch(username, @"\A[A-Za-z0-9._-]{3,64}\z") || password.Length < 12 || Encoding.UTF8.GetByteCount(password) > 72)
                throw new InvalidOperationException("Set BLOG_ADMIN_USER (3-64 letters/digits/._-) and BLOG_ADMIN_PASSWORD (12-72 UTF-8 bytes) before first startup.");
            var user = new BlogUser { UserName = username, SecurityStamp = Guid.NewGuid().ToString() };
            user.PasswordHash = new PasswordHasher<BlogUser>(Options.Create(new PasswordHasherOptions { CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV2 })).HashPassword(user, password);
            db.Users.Add(user);
            var role = db.Roles.SingleOrDefault(r => r.Name == "Admin");
            if (role == null) { role = new BlogRole { Name = "Admin" }; db.Roles.Add(role); }
            db.UserRoles.Add(new BlogUserRole { User = user, Role = role });
            db.SaveChanges();
        }
        else
        {
            // Previously every successfully authenticated user received the Admin claim.
            // Reconcile each user independently: other role memberships may already exist.
            var role = db.Roles.SingleOrDefault(r => r.Name == "Admin");
            if (role == null) { role = new BlogRole { Name = "Admin" }; db.Roles.Add(role); }
            var usersWithoutAdmin = db.Users
                .Where(user => !db.UserRoles.Any(membership => membership.UserId == user.Id && membership.RoleId == role.Id))
                .ToList();
            foreach (var user in usersWithoutAdmin)
                db.UserRoles.Add(new BlogUserRole { User = user, Role = role });
            if (usersWithoutAdmin.Count > 0)
                db.SaveChanges();
        }
        if (!db.SeedStates.Any(s => s.Id == "sample-v1"))
        {
            if (!db.Categories.Any() && !db.Posts.Any())
            {
                var engineering = new Category { Name = "Engineering", Slug = "engineering" };
                var journal = new Category { Name = "Journal", Slug = "journal" };
                db.Categories.AddRange(engineering, journal);
                Post? first = null;
                for (var i = 1; i <= 14; i++)
                {
                    var created = new DateTime(2020, 1, 1, 12, 0, 0, DateTimeKind.Utc).AddDays(i);
                    var post = new Post
                    {
                        Title = i <= 12 ? "Legacy notes " + i : "Draft notes " + (i - 12),
                        Slug = i <= 12 ? "legacy-notes-" + i : "draft-notes-" + (i - 12),
                        Body = "A small engineering journal built with a traditional layered monolith.\n\nEntry " + i + " is ready for your next modernization exercise.",
                        Category = i % 2 == 0 ? journal : engineering,
                        Published = i <= 12, CreatedAt = created, UpdatedAt = created,
                        PublishedAt = i <= 12 ? created : null
                    };
                    db.Posts.Add(post);
                    first ??= post;
                }
                db.Comments.Add(new Comment { Post = first!, Author = "Sample reader", Body = "Welcome to the legacy blog.", Approved = true, CreatedAt = new DateTime(2020, 2, 1, 12, 0, 0, DateTimeKind.Utc) });
                db.Comments.Add(new Comment { Post = first!, Author = "Sample reader", Body = "A sample comment awaiting approval.", CreatedAt = new DateTime(2020, 2, 1, 12, 0, 0, DateTimeKind.Utc) });
            }
            db.SeedStates.Add(new SeedState { Id = "sample-v1" });
            db.SaveChanges();
        }
        transaction.Commit();
    }
}
