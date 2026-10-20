using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using LegacyBlog.Models;

namespace LegacyBlog.ViewModels
{
    public class PostForm
    {
        [Required, StringLength(200)] public string Title { get; set; } = "";
        [Required, StringLength(150), RegularExpression(@"[a-z0-9]+(-[a-z0-9]+)*")] public string Slug { get; set; } = "";
        [Required, StringLength(50000)] public string Body { get; set; } = "";
        [Required, Range(1, int.MaxValue)] public int? CategoryId { get; set; }
    }
    public class CategoryForm
    {
        [Required, StringLength(100)] public string Name { get; set; } = "";
        [Required, StringLength(150), RegularExpression(@"[a-z0-9]+(-[a-z0-9]+)*")] public string Slug { get; set; } = "";
    }
    public class CommentForm
    {
        [Required, StringLength(100)] public string Author { get; set; } = "";
        [Required, StringLength(2000)] public string Body { get; set; } = "";
    }
    public class LoginForm
    {
        [Required, StringLength(64)] public string Username { get; set; } = "";
        [Required, StringLength(256), DataType(DataType.Password)] public string Password { get; set; } = "";
        public string? ReturnUrl { get; set; }
    }
    public class PostList
    {
        public List<Post> Posts { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public Category? Category { get; set; }
        public int Page { get; set; }
        public int Total { get; set; }
    }
    public class PostDetail
    {
        public Post Post { get; set; } = null!;
        public List<Comment> Comments { get; set; } = new();
        public CommentForm Form { get; set; } = new();
    }
}
