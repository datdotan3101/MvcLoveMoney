using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MvcLoveMoney.Data;
using MvcLoveMoney.Helpers;
using MvcLoveMoney.Models;
using MvcLoveMoney.ViewModels;

namespace MvcLoveMoney.Controllers
{
    [Authorize]
    public class SettingsController : Controller
    {
        private readonly MvcLoveMoneyDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;

        public SettingsController(MvcLoveMoneyDbContext context, IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        // GET: Settings
        public async Task<IActionResult> Index()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return await SignOutMissingUserAsync();

            var settingsVM = BuildSettingsVM(user);
            settingsVM.Profile = new ProfileVM { FullName = user.FullName, Email = user.Email };
            return View(settingsVM);
        }

        // POST: Settings/UpdateProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile([Bind(Prefix = nameof(SettingsVM.Profile))] ProfileVM profileVM)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return await SignOutMissingUserAsync();

            var email = profileVM.Email.Trim().ToLowerInvariant();

            if (ModelState.IsValid &&
                await _context.Users.AnyAsync(u => u.Email == email && u.Id != user.Id))
            {
                ModelState.AddModelError("Profile.Email", "This email is already used by another account.");
            }

            if (!ModelState.IsValid)
            {
                var settingsVM = BuildSettingsVM(user);
                settingsVM.Profile = profileVM;
                return View(nameof(Index), settingsVM);
            }

            user.FullName = profileVM.FullName.Trim();
            user.Email = email;
            await _context.SaveChangesAsync();

            // Refresh the cookie so the header shows the new name
            await HttpContext.SignInUserAsync(user);

            TempData["SuccessMessage"] = "Your profile has been updated.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Settings/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword([Bind(Prefix = nameof(SettingsVM.ChangePassword))] ChangePasswordVM changePasswordVM)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return await SignOutMissingUserAsync();

            // No password yet → the user must use "Set password" instead
            if (user.PasswordHash == null)
                return RedirectToAction(nameof(Index));

            if (ModelState.IsValid &&
                _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, changePasswordVM.CurrentPassword)
                    == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError("ChangePassword.CurrentPassword", "Current password is incorrect.");
            }

            if (!ModelState.IsValid)
            {
                var settingsVM = BuildSettingsVM(user);
                settingsVM.Profile = new ProfileVM { FullName = user.FullName, Email = user.Email };
                return View(nameof(Index), settingsVM);
            }

            user.PasswordHash = _passwordHasher.HashPassword(user, changePasswordVM.NewPassword);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Your password has been changed.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Settings/SetPassword  → for Google accounts that have no password yet
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPassword([Bind(Prefix = nameof(SettingsVM.SetPassword))] SetPasswordVM setPasswordVM)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return await SignOutMissingUserAsync();

            // Already has a password → must go through "Change password" (needs the current one)
            if (user.PasswordHash != null)
                return RedirectToAction(nameof(Index));

            if (!ModelState.IsValid)
            {
                var settingsVM = BuildSettingsVM(user);
                settingsVM.Profile = new ProfileVM { FullName = user.FullName, Email = user.Email };
                return View(nameof(Index), settingsVM);
            }

            user.PasswordHash = _passwordHasher.HashPassword(user, setPasswordVM.NewPassword);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Password set. You can now also log in with your email and password.";
            return RedirectToAction(nameof(Index));
        }

        // Reads the logged-in user's Id from the cookie and loads them from the database
        private async Task<User?> GetCurrentUserAsync()
        {
            var userId = User.GetUserId();
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        }

        // The cookie points to a user that no longer exists → log out
        private async Task<IActionResult> SignOutMissingUserAsync()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        private static SettingsVM BuildSettingsVM(User user)
        {
            return new SettingsVM
            {
                HasPassword = user.PasswordHash != null,
                HasGoogle = user.GoogleId != null,
                CreatedAt = user.CreatedAt
            };
        }
    }
}
