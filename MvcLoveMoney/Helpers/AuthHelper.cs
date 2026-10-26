using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using MvcLoveMoney.Models;
using System.Security.Claims;

namespace MvcLoveMoney.Helpers
{
    public static class AuthHelper
    {
        // Id of the logged-in user (from the NameIdentifier claim); 0 when not logged in
        public static int GetUserId(this ClaimsPrincipal principal)
        {
            return int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
                ? userId
                : 0;
        }

        // Creates (or refreshes) the auth cookie that stores the user's claims
        public static async Task SignInUserAsync(this HttpContext httpContext, User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await httpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties { IsPersistent = false });
        }
    }
}
