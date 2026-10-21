using LegacyBlog.Services;
using LegacyBlog.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LegacyBlog.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController(BlogService blogs) : BlogController(blogs)
{
    [HttpGet("/admin")]
    public IActionResult Index() => View();
    [HttpGet("/admin/posts")]
    public IActionResult Posts(string page = "1") => View(Blogs.List(false, null, page));
    [HttpGet("/admin/posts/create")]
    public IActionResult CreatePost() => PostFormView(null, new PostForm());
    [HttpGet("/admin/posts/{id:int}/edit")]
    public IActionResult EditPost([FromRoute] int id)
    {
        var post = Blogs.Post(id);
        return PostFormView(id, new PostForm { Title = post.Title, Slug = post.Slug, Body = post.Body, CategoryId = post.CategoryId });
    }
    [HttpPost("/admin/posts/create"), HttpPost("/admin/posts/{id:int}/edit"), ValidateAntiForgeryToken]
    public IActionResult SavePost([FromRoute] int? id, [Bind("Title,Slug,Body,CategoryId")] PostForm form)
    {
        if (id.HasValue) Blogs.Post(id.Value);
        if (Blogs.PostSlugTaken(form.Slug, id)) ModelState.AddModelError("Slug", "Slug already exists.");
        if (form.CategoryId.HasValue && !Blogs.Categories().Any(c => c.Id == form.CategoryId.Value)) ModelState.AddModelError("CategoryId", "Select an existing category.");
        if (!ModelState.IsValid) return PostFormView(id, form);
        Blogs.SavePost(id, form);
        return Redirect("/admin/posts");
    }
    private IActionResult PostFormView(int? id, PostForm form) { ViewBag.Id = id; ViewBag.Categories = Blogs.Categories(); return View("PostForm", form); }
    [HttpGet("/admin/posts/{id:int}/delete")]
    public IActionResult DeletePost([FromRoute] int id) => View(Blogs.Post(id));
    [HttpPost("/admin/posts/{id:int}/delete"), ValidateAntiForgeryToken]
    public IActionResult DeletePostConfirmed([FromRoute] int id) { Blogs.DeletePost(id); return Redirect("/admin/posts"); }
    [HttpPost("/admin/posts/{id:int}/publish"), ValidateAntiForgeryToken]
    public IActionResult Publish([FromRoute] int id) { Blogs.Publish(id, true); return Redirect("/admin/posts"); }
    [HttpPost("/admin/posts/{id:int}/unpublish"), ValidateAntiForgeryToken]
    public IActionResult Unpublish([FromRoute] int id) { Blogs.Publish(id, false); return Redirect("/admin/posts"); }
    [HttpGet("/admin/categories")]
    public IActionResult Categories() => View("Categories", Blogs.Categories());
    [HttpGet("/admin/categories/create")]
    public IActionResult CreateCategory() => CategoryFormView(null, new CategoryForm());
    [HttpGet("/admin/categories/{id:int}/edit")]
    public IActionResult EditCategory([FromRoute] int id)
    {
        var category = Blogs.Category(id);
        return CategoryFormView(id, new CategoryForm { Name = category.Name, Slug = category.Slug });
    }
    [HttpPost("/admin/categories/create"), HttpPost("/admin/categories/{id:int}/edit"), ValidateAntiForgeryToken]
    public IActionResult SaveCategory([FromRoute] int? id, [Bind("Name,Slug")] CategoryForm form)
    {
        if (id.HasValue) Blogs.Category(id.Value);
        if (Blogs.CategorySlugTaken(form.Slug, id)) ModelState.AddModelError("Slug", "Slug already exists.");
        if (!ModelState.IsValid) return CategoryFormView(id, form);
        Blogs.SaveCategory(id, form);
        return Redirect("/admin/categories");
    }
    private IActionResult CategoryFormView(int? id, CategoryForm form) { ViewBag.Id = id; return View("CategoryForm", form); }
    [HttpPost("/admin/categories/{id:int}/delete"), ValidateAntiForgeryToken]
    public IActionResult DeleteCategory([FromRoute] int id)
    {
        Blogs.Category(id);
        if (Blogs.CategoryInUse(id)) { ViewBag.Error = "This category is assigned to a post and cannot be deleted."; return Categories(); }
        Blogs.DeleteCategory(id);
        return Redirect("/admin/categories");
    }
    [HttpGet("/admin/comments")]
    public IActionResult Comments() => View(Blogs.PendingComments());
    [HttpPost("/admin/comments/{id:int}/approve"), ValidateAntiForgeryToken]
    public IActionResult ApproveComment([FromRoute] int id) { Blogs.Moderate(id, false); return Redirect("/admin/comments"); }
    [HttpPost("/admin/comments/{id:int}/delete"), ValidateAntiForgeryToken]
    public IActionResult DeleteComment([FromRoute] int id) { Blogs.Moderate(id, true); return Redirect("/admin/comments"); }
}
