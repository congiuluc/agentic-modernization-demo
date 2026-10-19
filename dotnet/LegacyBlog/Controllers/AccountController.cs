using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using LegacyBlog.Data;
using LegacyBlog.ViewModels;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;

namespace LegacyBlog.Controllers
{
    public class AccountController : BlogController
    {
        [HttpGet, Route("account/login")]
        public ActionResult Login(string returnUrl) { return View(new LoginForm { ReturnUrl = returnUrl }); }
        [HttpPost, ValidateAntiForgeryToken, Route("account/login")]
        public ActionResult Login([Bind(Include = "Username,Password,ReturnUrl")] LoginForm form)
        {
            if (!ModelState.IsValid) return View(form);
            var manager = Bootstrap.UserManager(Db);
            var user = manager.Find(form.Username, form.Password);
            if (user == null) { ModelState.AddModelError("", "Invalid username or password."); return View(form); }
            var identity = manager.CreateIdentity(user, "BlogCookie");
            identity.AddClaim(new Claim(ClaimTypes.Role, "Admin"));
            Request.GetOwinContext().Authentication.SignIn(new AuthenticationProperties { IsPersistent = false }, identity);
            return Redirect(Url.IsLocalUrl(form.ReturnUrl) ? form.ReturnUrl : "/admin");
        }
        [HttpPost, ValidateAntiForgeryToken, Route("account/logout")]
        public ActionResult Logout() { Request.GetOwinContext().Authentication.SignOut("BlogCookie"); return Redirect("/"); }
    }
}
