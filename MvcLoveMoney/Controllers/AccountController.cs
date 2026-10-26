using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MvcLoveMoney.Configs;
using MvcLoveMoney.Data;
using MvcLoveMoney.Models;
using MvcLoveMoney.ViewModels;
using System.Security.Claims;

namespace MvcLoveMoney.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        private readonly MvcLoveMoneyDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;

        public AccountController(MvcLoveMoneyDbContext context, IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        // GET: Account/Register
        [AllowAnonymous]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterVM registerVM)
        {
            if (!ModelState.IsValid)
                return View(registerVM);

            var email = registerVM.Email.Trim().ToLowerInvariant();

            if (await _context.Users.AnyAsync(u => u.Email == email))
            {
                ModelState.AddModelError(nameof(RegisterVM.Email), "This email is already registered.");
                return View(registerVM);
            }

            var user = new User
            {
                FullName = registerVM.FullName.Trim(),
                Email = email
            };
            user.PasswordHash = _passwordHasher.HashPassword(user, registerVM.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            await SignInUserAsync(user);
            return RedirectToAction("Index", "Home");
        }

        // GET: Account/Login
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVM loginVM, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View(loginVM);

            var email = loginVM.Email.Trim().ToLowerInvariant();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null || user.PasswordHash == null ||
                    _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, loginVM.Password)
                        == PasswordVerificationResult.Failed)
                {
                    ModelState.AddModelError(string.Empty, "Invalid email or password.");
                    return View(loginVM);
                }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        // POST: Account/Logout
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        // Creates the auth cookie that stores the user's claims
        private async Task SignInUserAsync(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties { IsPersistent = false });
        }

        // POST: Account/ExternalLogin  → send the user to Google
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string? returnUrl = null)
        {
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), new { returnUrl });
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, provider);
        }

        // GET: Account/ExternalLoginCallback  → Google sends the user back here
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null)
        {
            // 1. Read the info Google returned (stored in the temporary "External" cookie)
            var result = await HttpContext.AuthenticateAsync("External");
            if (!result.Succeeded || result.Principal == null)
            {
                TempData["ErrorMessage"] = "Google sign-in failed. Please try again.";
                return RedirectToAction(nameof(Login));
            }

            var googleId = result.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = result.Principal.FindFirstValue(ClaimTypes.Email)?.Trim().ToLowerInvariant();
            var fullName = result.Principal.FindFirstValue(ClaimTypes.Name) ?? email;

            if (string.IsNullOrEmpty(googleId) || string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Could not read your Google account information.";
                return RedirectToAction(nameof(Login));
            }

            // 2. Signed in with Google before?
            var user = await _context.Users.FirstOrDefaultAsync(u => u.GoogleId == googleId);

            if (user == null)
            {
                // 3. Already registered with the same email? → link Google to that account
                user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

                if (user != null)
                {
                    user.GoogleId = googleId;
                }
                else
                {
                    // 4. Brand-new user → create an account ("Register with Google")
                    if (fullName!.Length > MaxLengths.NameLength)
                        fullName = fullName.Substring(0, MaxLengths.NameLength);

                    user = new User
                    {
                        FullName = fullName,
                        Email = email,
                        GoogleId = googleId
                    };
                    _context.Users.Add(user);
                }

                await _context.SaveChangesAsync();
            }

            // 5. Remove the temporary cookie and sign in with OUR cookie
            await HttpContext.SignOutAsync("External");
            await SignInUserAsync(user);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }
    }
}
