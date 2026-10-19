using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using LegacyBlog.Models;

namespace LegacyBlog.ViewModels
{
    public class PostForm
    {
        [AllowHtml, Required, StringLength(200)] public string Title { get; set; }
        [Required, StringLength(150), RegularExpression(@"[a-z0-9]+(-[a-z0-9]+)*")] public string Slug { get; set; }
        [AllowHtml, Required, StringLength(50000)] public string Body { get; set; }
        [Required, Range(1, int.MaxValue)] public int? CategoryId { get; set; }
    }
    public class CategoryForm
    {
        [AllowHtml, Required, StringLength(100)] public string Name { get; set; }
        [Required, StringLength(150), RegularExpression(@"[a-z0-9]+(-[a-z0-9]+)*")] public string Slug { get; set; }
    }
    public class CommentForm
    {
        [AllowHtml, Required, StringLength(100)] public string Author { get; set; }
        [AllowHtml, Required, StringLength(2000)] public string Body { get; set; }
    }
    public class LoginForm
    {
        [Required, StringLength(64)] public string Username { get; set; }
        [Required, StringLength(256), DataType(DataType.Password)] public string Password { get; set; }
        public string ReturnUrl { get; set; }
    }
    public class PostList
    {
        public List<Post> Posts { get; set; }
        public List<Category> Categories { get; set; }
        public Category Category { get; set; }
        public int Page { get; set; }
        public int Total { get; set; }
    }
    public class PostDetail
    {
        public Post Post { get; set; }
        public List<Comment> Comments { get; set; }
        public CommentForm Form { get; set; }
    }
}
