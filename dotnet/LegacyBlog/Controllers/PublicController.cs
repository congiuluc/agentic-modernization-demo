using LegacyBlog.Services;
using LegacyBlog.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace LegacyBlog.Controllers;

public class PublicController(BlogService blogs) : BlogController(blogs)
{
    [HttpGet("/"), HttpGet("/categories/{slug}")]
    public IActionResult Index(string? slug, string page = "1") => View(Blogs.List(true, slug, page));
    [HttpGet("/posts/{slug}")]
    public IActionResult Detail(string slug) => DetailView(slug, new CommentForm());
    [HttpPost("/posts/{slug}/comments"), ValidateAntiForgeryToken]
    public IActionResult Comment(string slug, [Bind("Author,Body")] CommentForm form)
    {
        Blogs.PublicPost(slug);
        if (!ModelState.IsValid) return DetailView(slug, form);
        Blogs.Comment(slug, form);
        return Redirect($"/posts/{Uri.EscapeDataString(slug)}?submitted=1");
    }
    private IActionResult DetailView(string slug, CommentForm form)
    {
        var post = Blogs.PublicPost(slug);
        return View("Detail", new PostDetail { Post = post, Comments = Blogs.ApprovedComments(post.Id), Form = form });
    }
    [HttpGet("/error")]
    public IActionResult Error() { Response.StatusCode = 500; return View("~/Views/Shared/Error.cshtml", "An unexpected error occurred. Check the server log."); }
}
