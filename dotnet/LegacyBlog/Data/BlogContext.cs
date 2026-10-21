using System;
using System.Data.Entity;
using LegacyBlog.Models;
using Microsoft.AspNet.Identity.EntityFramework;

namespace LegacyBlog.Data
{
    public class BlogContext : IdentityDbContext<BlogUser>
    {
        public BlogContext() : base(Environment.GetEnvironmentVariable("BLOG_DOTNET_CONNECTION_STRING") ?? "name=Blog") { }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<SeedState> SeedStates { get; set; }

        protected override void OnModelCreating(DbModelBuilder model)
        {
            base.OnModelCreating(model);
            model.Entity<Post>().HasRequired(p => p.Category).WithMany().HasForeignKey(p => p.CategoryId).WillCascadeOnDelete(false);
            model.Entity<Comment>().HasRequired(c => c.Post).WithMany().HasForeignKey(c => c.PostId).WillCascadeOnDelete(true);
        }
    }
}
