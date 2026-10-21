using System.Linq;
using System.Web.Mvc;
using LegacyBlog.ViewModels;

namespace LegacyBlog.Controllers
{
    [Authorize(Roles = "Admin"), RoutePrefix("admin")]
    public class AdminController : BlogController
    {
        [HttpGet, Route("")]
        public ActionResult Index() { return View(); }
        [HttpGet, Route("posts")]
        public ActionResult Posts(string page = "1") { return View(Blogs.List(false, null, page)); }
        [HttpGet, Route("posts/create")]
        public ActionResult CreatePost() { return PostFormView(null, new PostForm()); }
        [HttpGet, Route("posts/{id:int}/edit")]
        public ActionResult EditPost([RouteId] int id)
        {
            var post = Blogs.Post(id);
            return PostFormView(id, new PostForm { Title = post.Title, Slug = post.Slug, Body = post.Body, CategoryId = post.CategoryId });
        }
        [HttpPost, ValidateAntiForgeryToken, Route("posts/create"), Route("posts/{id:int}/edit")]
        public ActionResult SavePost([RouteId] int? id, [Bind(Include = "Title,Slug,Body,CategoryId")] PostForm form)
        {
            if (id.HasValue) Blogs.Post(id.Value);
            if (Blogs.PostSlugTaken(form.Slug, id)) ModelState.AddModelError("Slug", "Slug already exists.");
            if (form.CategoryId.HasValue && !Blogs.Categories().Any(c => c.Id == form.CategoryId.Value)) ModelState.AddModelError("CategoryId", "Select an existing category.");
            if (!ModelState.IsValid) return PostFormView(id, form);
            Blogs.SavePost(id, form); return Redirect("/admin/posts");
        }
        private ActionResult PostFormView(int? id, PostForm form) { ViewBag.Id = id; ViewBag.Categories = Blogs.Categories(); return View("PostForm", form); }
        [HttpGet, Route("posts/{id:int}/delete")]
        public ActionResult DeletePost([RouteId] int id) { return View(Blogs.Post(id)); }
        [HttpPost, ValidateAntiForgeryToken, ActionName("DeletePost"), Route("posts/{id:int}/delete")]
        public ActionResult DeletePostConfirmed([RouteId] int id) { Blogs.DeletePost(id); return Redirect("/admin/posts"); }
        [HttpPost, ValidateAntiForgeryToken, Route("posts/{id:int}/publish")]
        public ActionResult Publish([RouteId] int id) { Blogs.Publish(id, true); return Redirect("/admin/posts"); }
        [HttpPost, ValidateAntiForgeryToken, Route("posts/{id:int}/unpublish")]
        public ActionResult Unpublish([RouteId] int id) { Blogs.Publish(id, false); return Redirect("/admin/posts"); }
        [HttpGet, Route("categories")]
        public ActionResult Categories() { return View("Categories", Blogs.Categories()); }
        [HttpGet, Route("categories/create")]
        public ActionResult CreateCategory() { return CategoryFormView(null, new CategoryForm()); }
        [HttpGet, Route("categories/{id:int}/edit")]
        public ActionResult EditCategory([RouteId] int id)
        {
            var category = Blogs.Category(id);
            return CategoryFormView(id, new CategoryForm { Name = category.Name, Slug = category.Slug });
        }
        [HttpPost, ValidateAntiForgeryToken, Route("categories/create"), Route("categories/{id:int}/edit")]
        public ActionResult SaveCategory([RouteId] int? id, [Bind(Include = "Name,Slug")] CategoryForm form)
        {
            if (id.HasValue) Blogs.Category(id.Value);
            if (Blogs.CategorySlugTaken(form.Slug, id)) ModelState.AddModelError("Slug", "Slug already exists.");
            if (!ModelState.IsValid) return CategoryFormView(id, form);
            Blogs.SaveCategory(id, form); return Redirect("/admin/categories");
        }
        private ActionResult CategoryFormView(int? id, CategoryForm form) { ViewBag.Id = id; return View("CategoryForm", form); }
        [HttpPost, ValidateAntiForgeryToken, Route("categories/{id:int}/delete")]
        public ActionResult DeleteCategory([RouteId] int id)
        {
            Blogs.Category(id);
            if (Blogs.CategoryInUse(id)) { ViewBag.Error = "This category is assigned to a post and cannot be deleted."; return Categories(); }
            Blogs.DeleteCategory(id); return Redirect("/admin/categories");
        }
        [HttpGet, Route("comments")]
        public ActionResult Comments() { return View(Blogs.PendingComments()); }
        [HttpPost, ValidateAntiForgeryToken, Route("comments/{id:int}/approve")]
        public ActionResult ApproveComment([RouteId] int id) { Blogs.Moderate(id, false); return Redirect("/admin/comments"); }
        [HttpPost, ValidateAntiForgeryToken, Route("comments/{id:int}/delete")]
        public ActionResult DeleteComment([RouteId] int id) { Blogs.Moderate(id, true); return Redirect("/admin/comments"); }
    }
}
