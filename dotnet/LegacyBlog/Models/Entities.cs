using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LegacyBlog.Models
{
    // Identity 2 columns are mapped explicitly; do not let Identity Core add new columns.
    public class BlogUser
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserName { get; set; } = "";
        public string? Email { get; set; }
        public bool EmailConfirmed { get; set; }
        public string? PasswordHash { get; set; }
        public string? SecurityStamp { get; set; }
        public string? PhoneNumber { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public DateTime? LockoutEndDateUtc { get; set; }
        public bool LockoutEnabled { get; set; }
        public int AccessFailedCount { get; set; }
        public ICollection<BlogUserRole> Roles { get; set; } = new List<BlogUserRole>();
    }
    public class BlogRole
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "";
        public ICollection<BlogUserRole> Users { get; set; } = new List<BlogUserRole>();
    }
    public class BlogUserRole
    {
        public string UserId { get; set; } = "";
        public string RoleId { get; set; } = "";
        public BlogUser User { get; set; } = null!;
        public BlogRole Role { get; set; } = null!;
    }

    public class Category
    {
        public int Id { get; set; }
        [Required, MaxLength(100)] public string Name { get; set; } = "";
        [Required, MaxLength(150)] public string Slug { get; set; } = "";
        public ICollection<Post> Posts { get; set; } = new List<Post>();
    }

    public class Post
    {
        public int Id { get; set; }
        [Required, MaxLength(200)] public string Title { get; set; } = "";
        [Required, MaxLength(150)] public string Slug { get; set; } = "";
        [Required, MaxLength(50000)] public string Body { get; set; } = "";
        public bool Published { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? PublishedAt { get; set; }
        public int CategoryId { get; set; }
        public virtual Category Category { get; set; } = null!;
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }

    public class Comment
    {
        public int Id { get; set; }
        [Required, MaxLength(100)] public string Author { get; set; } = "";
        [Required, MaxLength(2000)] public string Body { get; set; } = "";
        public bool Approved { get; set; }
        public DateTime CreatedAt { get; set; }
        public int PostId { get; set; }
        public virtual Post Post { get; set; } = null!;
    }

    public class SeedState
    {
        [Key, MaxLength(32)] public string Id { get; set; } = "";
    }
}
