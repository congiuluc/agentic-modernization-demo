using System.Security.Claims;
using LegacyBlog.Data;
using LegacyBlog.Models;
using LegacyBlog.Services;
using LegacyBlog.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LegacyBlog.Controllers;

public class AccountController(BlogService blogs, BlogContext db) : BlogController(blogs)
{
    [HttpGet("/account/login")]
    public IActionResult Login(string? returnUrl) => View(new LoginForm { ReturnUrl = returnUrl });

    [HttpPost("/account/login"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Login([Bind("Username,Password,ReturnUrl")] LoginForm form)
    {
        if (!ModelState.IsValid) return View(form);
        var user = await db.Users.SingleOrDefaultAsync(u => u.UserName == form.Username);
        var hasher = new PasswordHasher<BlogUser>(Options.Create(new PasswordHasherOptions { CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV2 }));
        if (user == null || string.IsNullOrEmpty(user.PasswordHash) ||
            hasher.VerifyHashedPassword(user, user.PasswordHash, form.Password) == PasswordVerificationResult.Failed ||
            (user.LockoutEnabled && user.LockoutEndDateUtc > DateTime.UtcNow))
        {
            ModelState.AddModelError("", "Invalid username or password.");
            return View(form);
        }
        var roles = await db.UserRoles.Where(r => r.UserId == user.Id).Select(r => r.Role.Name).ToListAsync();
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, user.Id), new(ClaimTypes.Name, user.UserName) };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)), new AuthenticationProperties { IsPersistent = false });
        return LocalRedirect(Url.IsLocalUrl(form.ReturnUrl) ? form.ReturnUrl! : "/admin");
    }

    [HttpPost("/account/logout"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/");
    }

    [HttpGet("/account/denied")]
    public IActionResult Denied()
    {
        Response.StatusCode = 403;
        return View("~/Views/Shared/Error.cshtml", "The request was rejected.");
    }
}
