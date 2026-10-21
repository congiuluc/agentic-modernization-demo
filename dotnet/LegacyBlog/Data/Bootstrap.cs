using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using LegacyBlog.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace LegacyBlog.Data
{
    public static class Bootstrap
    {
        public static UserManager<BlogUser> UserManager(BlogContext db)
        {
            var manager = new UserManager<BlogUser>(new UserStore<BlogUser>(db));
            manager.PasswordValidator = new PasswordValidator { RequiredLength = 12 };
            manager.UserValidator = new UserValidator<BlogUser>(manager) { AllowOnlyAlphanumericUserNames = false };
            return manager;
        }

        public static void Initialize(BlogContext db)
        {
            using (var transaction = db.Database.BeginTransaction())
            {
                if (!db.Users.Any())
                {
                    var username = Environment.GetEnvironmentVariable("BLOG_ADMIN_USER") ?? "";
                    var password = Environment.GetEnvironmentVariable("BLOG_ADMIN_PASSWORD") ?? "";
                    if (!Regex.IsMatch(username, @"\A[A-Za-z0-9._-]{3,64}\z") || password.Length < 12 || Encoding.UTF8.GetByteCount(password) > 72)
                        throw new InvalidOperationException("Set BLOG_ADMIN_USER (3-64 letters/digits/._-) and BLOG_ADMIN_PASSWORD (12-72 characters) before first startup.");
                    var manager = UserManager(db);
                    var result = manager.Create(new BlogUser { UserName = username }, password);
                    if (!result.Succeeded) throw new InvalidOperationException("Administrator initialization failed: " + string.Join("; ", result.Errors));
                }
                if (!db.SeedStates.Any(s => s.Id == "sample-v1"))
                {
                    var engineering = new Category { Name = "Engineering", Slug = "engineering" };
                    var journal = new Category { Name = "Journal", Slug = "journal" };
                    db.Categories.Add(engineering); db.Categories.Add(journal);
                    Post first = null;
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
                            PublishedAt = i <= 12 ? created : (DateTime?)null
                        };
                        db.Posts.Add(post); if (first == null) first = post;
                    }
                    db.Comments.Add(new Comment { Post = first, Author = "Sample reader", Body = "Welcome to the legacy blog.", Approved = true, CreatedAt = new DateTime(2020, 2, 1, 12, 0, 0, DateTimeKind.Utc) });
                    db.Comments.Add(new Comment { Post = first, Author = "Sample reader", Body = "A sample comment awaiting approval.", Approved = false, CreatedAt = new DateTime(2020, 2, 1, 12, 0, 0, DateTimeKind.Utc) });
                    db.SeedStates.Add(new SeedState { Id = "sample-v1" }); db.SaveChanges();
                }
                transaction.Commit();
            }
        }
    }
}
