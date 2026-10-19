using System.Data.Entity;
using System.Linq;
using LegacyBlog.Data;
using LegacyBlog.Models;

namespace LegacyBlog.Repositories
{
    public class BlogRepository
    {
        private readonly BlogContext db;
        public BlogRepository(BlogContext db) { this.db = db; }
        public IQueryable<Post> Posts { get { return db.Posts.Include(p => p.Category); } }
        public IQueryable<Category> Categories { get { return db.Categories; } }
        public IQueryable<Comment> Comments { get { return db.Comments.Include(c => c.Post); } }
        public void Add(Post value) { db.Posts.Add(value); }
        public void Add(Category value) { db.Categories.Add(value); }
        public void Add(Comment value) { db.Comments.Add(value); }
        public void Remove(Post value) { db.Posts.Remove(value); }
        public void Remove(Category value) { db.Categories.Remove(value); }
        public void Remove(Comment value) { db.Comments.Remove(value); }
        public void Save() { db.SaveChanges(); }
    }
}
