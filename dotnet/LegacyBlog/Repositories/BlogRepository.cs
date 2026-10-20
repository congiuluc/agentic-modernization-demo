using LegacyBlog.Data;
using LegacyBlog.Models;
using Microsoft.EntityFrameworkCore;

namespace LegacyBlog.Repositories;

public class BlogRepository(BlogContext db)
{
    public IQueryable<Post> Posts => db.Posts.Include(p => p.Category);
    public IQueryable<Category> Categories => db.Categories;
    public IQueryable<Comment> Comments => db.Comments.Include(c => c.Post);
    public void Add(Post value) => db.Posts.Add(value);
    public void Add(Category value) => db.Categories.Add(value);
    public void Add(Comment value) => db.Comments.Add(value);
    public void Remove(Post value) => db.Posts.Remove(value);
    public void Remove(Category value) => db.Categories.Remove(value);
    public void Remove(Comment value) => db.Comments.Remove(value);
    public void Save() => db.SaveChanges();
}
