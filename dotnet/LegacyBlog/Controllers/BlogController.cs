using LegacyBlog.Services;
using Microsoft.AspNetCore.Mvc;

namespace LegacyBlog.Controllers;

public abstract class BlogController(BlogService blogs) : Controller
{
    protected BlogService Blogs { get; } = blogs;
}
