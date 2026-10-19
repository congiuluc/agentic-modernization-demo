using System;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Owin;

namespace LegacyBlog.App_Start
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = "BlogCookie",
                CookieName = "LegacyBlogDotNet",
                LoginPath = new PathString("/account/login"),
                CookieHttpOnly = true,
                CookieSecure = CookieSecureOption.SameAsRequest,
                CookieSameSite = SameSiteMode.Lax,
                ExpireTimeSpan = TimeSpan.FromMinutes(30),
                SlidingExpiration = false
            });
        }
    }
}
