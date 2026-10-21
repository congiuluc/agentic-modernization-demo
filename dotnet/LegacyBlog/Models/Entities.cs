using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNet.Identity.EntityFramework;

namespace LegacyBlog.Models
{
    public class BlogUser : IdentityUser { }

    public class Category
    {
        public int Id { get; set; }
        [Required, MaxLength(100)] public string Name { get; set; }
        [Required, MaxLength(150), Index(IsUnique = true)] public string Slug { get; set; }
    }

    public class Post
    {
        public int Id { get; set; }
        [Required, MaxLength(200)] public string Title { get; set; }
        [Required, MaxLength(150), Index(IsUnique = true)] public string Slug { get; set; }
        [Required, MaxLength(50000)] public string Body { get; set; }
        [Index("IX_Publication", 1)] public bool Published { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        [Index("IX_Publication", 2)] public DateTime? PublishedAt { get; set; }
        public int CategoryId { get; set; }
        public virtual Category Category { get; set; }
    }

    public class Comment
    {
        public int Id { get; set; }
        [Required, MaxLength(100)] public string Author { get; set; }
        [Required, MaxLength(2000)] public string Body { get; set; }
        public bool Approved { get; set; }
        public DateTime CreatedAt { get; set; }
        public int PostId { get; set; }
        public virtual Post Post { get; set; }
    }

    public class SeedState
    {
        [Key, MaxLength(32)] public string Id { get; set; }
    }
}
