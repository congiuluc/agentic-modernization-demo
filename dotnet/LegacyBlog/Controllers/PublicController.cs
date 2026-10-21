using System.Web.Mvc;
using LegacyBlog.ViewModels;

namespace LegacyBlog.Controllers
{
    public class PublicController : BlogController
    {
        [HttpGet, Route(""), Route("categories/{slug}")]
        public ActionResult Index(string slug, string page = "1") { return View(Blogs.List(true, slug, page)); }
        [HttpGet, Route("posts/{slug}")]
        public ActionResult Detail(string slug) { return DetailView(slug, new CommentForm()); }
        [HttpPost, ValidateAntiForgeryToken, Route("posts/{slug}/comments")]
        public ActionResult Comment(string slug, [Bind(Include = "Author,Body")] CommentForm form)
        {
            Blogs.PublicPost(slug);
            if (!ModelState.IsValid) return DetailView(slug, form);
            Blogs.Comment(slug, form); return RedirectToAction("Detail", new { slug, submitted = 1 });
        }
        private ActionResult DetailView(string slug, CommentForm form)
        {
            var post = Blogs.PublicPost(slug);
            return View("Detail", new PostDetail { Post = post, Comments = Blogs.ApprovedComments(post.Id), Form = form });
        }
        [HttpGet, Route("error")]
        public ActionResult Error() { Response.StatusCode = 500; return View("~/Views/Shared/Error.cshtml", (object)"An unexpected error occurred. Check the server log."); }
    }
}
