using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using LegacyBlog.Models;
using LegacyBlog.Repositories;
using LegacyBlog.ViewModels;

namespace LegacyBlog.Services
{
    public class BlogService
    {
        private readonly BlogRepository repository;
        public BlogService(BlogRepository repository) { this.repository = repository; }
        public List<Category> Categories() { return repository.Categories.OrderBy(c => c.Name).ThenBy(c => c.Id).ToList(); }
        public Category Category(int id) { return repository.Categories.FirstOrDefault(c => c.Id == id) ?? throw new HttpException(404, "Category not found."); }
        public Post Post(int id) { return repository.Posts.FirstOrDefault(p => p.Id == id) ?? throw new HttpException(404, "Post not found."); }
        public Post PublicPost(string slug) { return repository.Posts.FirstOrDefault(p => p.Slug == slug && p.Published) ?? throw new HttpException(404, "Post not found."); }
        public PostList List(bool publicOnly, string categorySlug, string pageText)
        {
            int page;
            if (string.IsNullOrEmpty(pageText)) pageText = "1";
            if (!pageText.All(c => c >= '0' && c <= '9') || !int.TryParse(pageText, NumberStyles.None, CultureInfo.InvariantCulture, out page) || page < 1 || page > int.MaxValue / 10)
                throw new HttpException(400, "Page must be a positive integer.");
            var category = categorySlug == null ? null : repository.Categories.FirstOrDefault(c => c.Slug == categorySlug);
            if (categorySlug != null && category == null) throw new HttpException(404, "Category not found.");
            var query = repository.Posts;
            if (publicOnly) query = query.Where(p => p.Published);
            if (category != null) query = query.Where(p => p.CategoryId == category.Id);
            var total = query.Count();
            if (page > 1 && ((long)page - 1) * 10 >= total) throw new HttpException(404, "Page not found.");
            return new PostList { Posts = query.OrderByDescending(p => p.PublishedAt).ThenByDescending(p => p.Id).Skip((page - 1) * 10).Take(10).ToList(), Page = page, Total = total, Category = category, Categories = Categories() };
        }
        public bool PostSlugTaken(string slug, int? id) { return repository.Posts.Any(p => p.Slug == slug && (!id.HasValue || p.Id != id.Value)); }
        public bool CategorySlugTaken(string slug, int? id) { return repository.Categories.Any(c => c.Slug == slug && (!id.HasValue || c.Id != id.Value)); }
        public void SavePost(int? id, PostForm form)
        {
            var category = Category(form.CategoryId.Value);
            if (PostSlugTaken(form.Slug, id)) throw new HttpException(409, "Slug already exists.");
            var post = id.HasValue ? Post(id.Value) : new Post { CreatedAt = DateTime.UtcNow };
            post.Title = form.Title.Trim(); post.Slug = form.Slug; post.Body = form.Body.Trim(); post.CategoryId = category.Id; post.UpdatedAt = DateTime.UtcNow;
            if (!id.HasValue) repository.Add(post);
            repository.Save();
        }
        public void Publish(int id, bool publish)
        {
            var post = Post(id);
            if (post.Published != publish) { post.Published = publish; post.PublishedAt = publish ? DateTime.UtcNow : (DateTime?)null; post.UpdatedAt = DateTime.UtcNow; repository.Save(); }
        }
        public void DeletePost(int id) { repository.Remove(Post(id)); repository.Save(); }
        public void SaveCategory(int? id, CategoryForm form)
        {
            if (CategorySlugTaken(form.Slug, id)) throw new HttpException(409, "Slug already exists.");
            var category = id.HasValue ? Category(id.Value) : new Category();
            category.Name = form.Name.Trim(); category.Slug = form.Slug;
            if (!id.HasValue) repository.Add(category);
            repository.Save();
        }
        public bool CategoryInUse(int id) { return repository.Posts.Any(p => p.CategoryId == id); }
        public void DeleteCategory(int id)
        {
            var category = Category(id);
            if (CategoryInUse(id)) throw new HttpException(409, "This category is assigned to a post and cannot be deleted.");
            repository.Remove(category); repository.Save();
        }
        public List<Comment> ApprovedComments(int postId) { return repository.Comments.Where(c => c.PostId == postId && c.Approved).OrderBy(c => c.Id).ToList(); }
        public List<Comment> PendingComments() { return repository.Comments.Where(c => !c.Approved).OrderByDescending(c => c.Id).ToList(); }
        public void Comment(string slug, CommentForm form)
        {
            var post = PublicPost(slug);
            repository.Add(new Comment { PostId = post.Id, Author = form.Author.Trim(), Body = form.Body.Trim(), CreatedAt = DateTime.UtcNow }); repository.Save();
        }
        public void Moderate(int id, bool delete)
        {
            var comment = repository.Comments.FirstOrDefault(c => c.Id == id) ?? throw new HttpException(404, "Comment not found.");
            if (delete) repository.Remove(comment); else comment.Approved = true;
            repository.Save();
        }
    }
}
