using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MvcLoveMoney.Configs;
using MvcLoveMoney.Data;
using MvcLoveMoney.Helpers;
using MvcLoveMoney.Models;
using MvcLoveMoney.Services;
using MvcLoveMoney.ViewModels;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

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
        private readonly IEmailService _emailService;
        private readonly ILogger<AccountController> _logger;
        private readonly IMemoryCache _cache;

        public AccountController(MvcLoveMoneyDbContext context, IPasswordHasher<User> passwordHasher,
            IEmailService emailService, ILogger<AccountController> logger, IMemoryCache cache)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _emailService = emailService;
            _logger = logger;
            _cache = cache;
        }

        // GET: Account/Register
        [AllowAnonymous]
        [RedirectIfAuthenticated]
        public IActionResult Register()
        {
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

            await HttpContext.SignInUserAsync(user);
            return RedirectToAction("Index", "Home");
        }

        // GET: Account/Login
        [AllowAnonymous]
        [RedirectIfAuthenticated]
        public IActionResult Login(string? returnUrl = null)
        {
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

            await HttpContext.SignInUserAsync(user);

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
            await HttpContext.SignInUserAsync(user);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        // ==================== FORGOT PASSWORD (email OTP, kept in memory cache) ====================

        // GET: Account/ForgotPassword
        [AllowAnonymous]
        [RedirectIfAuthenticated]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: Account/ForgotPassword  → email a 6-digit code
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordVM forgotPasswordVM)
        {
            if (!ModelState.IsValid)
                return View(forgotPasswordVM);

            var email = forgotPasswordVM.Email.Trim().ToLowerInvariant();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            // Unknown email: behave exactly the same, so nobody can find out which emails are registered
            if (user != null && GetResendWaitSeconds(email) == 0)
            {
                try
                {
                    await CreateAndSendOtpAsync(user);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Could not send the password reset email to {Email}", email);
                    ModelState.AddModelError(string.Empty, "We could not send the email right now. Please try again later.");
                    return View(forgotPasswordVM);
                }
            }

            TempData["SuccessMessage"] = $"If an account exists for {email}, a {OtpSettings.CodeLength}-digit code has been sent to it.";
            return RedirectToAction(nameof(VerifyOtp), new { email });
        }

        // GET: Account/VerifyOtp?email=...
        [AllowAnonymous]
        [RedirectIfAuthenticated]
        public IActionResult VerifyOtp(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return RedirectToAction(nameof(ForgotPassword));

            var verifyOtpVM = new VerifyOtpVM { Email = email.Trim().ToLowerInvariant() };
            verifyOtpVM.ResendWaitSeconds = GetResendWaitSeconds(verifyOtpVM.Email);
            return View(verifyOtpVM);
        }

        // POST: Account/VerifyOtp  → check the code
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOtp(VerifyOtpVM verifyOtpVM)
        {
            verifyOtpVM.Email = verifyOtpVM.Email.Trim().ToLowerInvariant();
            verifyOtpVM.ResendWaitSeconds = GetResendWaitSeconds(verifyOtpVM.Email);

            if (!ModelState.IsValid)
                return View(verifyOtpVM);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == verifyOtpVM.Email);
            _cache.TryGetValue(OtpCacheKey(verifyOtpVM.Email), out PasswordResetOtp? otp);

            // No code (never sent, expired, or already used)
            if (user == null || otp == null || otp.VerifiedAt != null)
            {
                ModelState.AddModelError(string.Empty, "This code is invalid or has expired. Please request a new one.");
                return View(verifyOtpVM);
            }

            if (otp.FailedAttempts >= OtpSettings.MaxFailedAttempts)
            {
                ModelState.AddModelError(string.Empty, "Too many wrong attempts. Please request a new code.");
                return View(verifyOtpVM);
            }

            if (_passwordHasher.VerifyHashedPassword(user, otp.CodeHash, verifyOtpVM.Code) == PasswordVerificationResult.Failed)
            {
                otp.FailedAttempts++;   // the object lives in the cache, so this is remembered

                int attemptsLeft = OtpSettings.MaxFailedAttempts - otp.FailedAttempts;
                ModelState.AddModelError(nameof(VerifyOtpVM.Code), attemptsLeft > 0
                    ? $"Incorrect code. {attemptsLeft} attempt(s) left."
                    : "Incorrect code. Please request a new one.");
                return View(verifyOtpVM);
            }

            // Correct code → issue a one-time token for the "new password" step
            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            otp.VerifiedAt = DateTime.UtcNow;
            otp.ResetTokenHash = HashToken(token);

            // The user now has ResetWindowMinutes to choose a new password
            _cache.Set(OtpCacheKey(verifyOtpVM.Email), otp, TimeSpan.FromMinutes(OtpSettings.ResetWindowMinutes));

            TempData["ResetEmail"] = verifyOtpVM.Email;
            TempData["ResetToken"] = token;
            return RedirectToAction(nameof(ResetPassword));
        }

        // GET: Account/ResetPassword  (only reachable right after a correct code)
        [AllowAnonymous]
        [RedirectIfAuthenticated]
        public IActionResult ResetPassword()
        {
            if (TempData["ResetEmail"] is not string email || TempData["ResetToken"] is not string token)
                return RedirectToAction(nameof(ForgotPassword));

            return View(new ResetPasswordVM { Email = email, Token = token });
        }

        // POST: Account/ResetPassword  → save the new password
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordVM resetPasswordVM)
        {
            if (!ModelState.IsValid)
                return View(resetPasswordVM);

            var email = resetPasswordVM.Email.Trim().ToLowerInvariant();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            _cache.TryGetValue(OtpCacheKey(email), out PasswordResetOtp? otp);

            // The code must have been verified, and the token must match
            if (user == null || otp?.ResetTokenHash == null ||
                !CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(otp.ResetTokenHash),
                    Encoding.UTF8.GetBytes(HashToken(resetPasswordVM.Token))))
            {
                TempData["ErrorMessage"] = "Your reset session has expired. Please request a new code.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            user.PasswordHash = _passwordHasher.HashPassword(user, resetPasswordVM.NewPassword);
            await _context.SaveChangesAsync();

            // One use only
            _cache.Remove(OtpCacheKey(email));

            TempData["SuccessMessage"] = "Your password has been reset. You can now log in with your new password.";
            return RedirectToAction(nameof(Login));
        }

        // ----- Forgot-password helpers -----

        private static string OtpCacheKey(string email) => $"password-reset:{email}";

        // Creates a new code (replacing any older one in the cache) and emails it
        private async Task CreateAndSendOtpAsync(User user)
        {
            var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

            var otp = new PasswordResetOtp
            {
                CodeHash = _passwordHasher.HashPassword(user, code),
                CreatedAt = DateTime.UtcNow
            };

            // Send first: if the email fails, the cache is not touched and the old code still works
            await _emailService.SendAsync(user.Email, $"Your MvcLoveMoney code: {code}", BuildOtpEmail(user.FullName, code));

            // The cache removes the entry by itself when the code expires
            _cache.Set(OtpCacheKey(user.Email), otp, TimeSpan.FromMinutes(OtpSettings.ExpiryMinutes));
        }

        // Seconds left before another code can be sent (0 = can resend now)
        private int GetResendWaitSeconds(string email)
        {
            if (!_cache.TryGetValue(OtpCacheKey(email), out PasswordResetOtp? otp) || otp == null)
                return 0;

            var wait = OtpSettings.ResendCooldownSeconds - (DateTime.UtcNow - otp.CreatedAt).TotalSeconds;
            return wait > 0 ? (int)Math.Ceiling(wait) : 0;
        }

        private static string HashToken(string token)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        }

        private static string BuildOtpEmail(string fullName, string code)
        {
            var name = System.Net.WebUtility.HtmlEncode(fullName);
            return $@"
<div style=""font-family:Arial,sans-serif;max-width:480px;margin:auto;color:#212529"">
  <h2 style=""margin-bottom:4px"">Reset your password</h2>
  <p>Hi {name},</p>
  <p>Use this code to reset your MvcLoveMoney password:</p>
  <p style=""font-size:32px;font-weight:bold;letter-spacing:8px;background:#f1f3f5;padding:16px;text-align:center;border-radius:8px"">{code}</p>
  <p>The code expires in {OtpSettings.ExpiryMinutes} minutes.</p>
  <p style=""color:#6c757d;font-size:13px"">If you did not ask to reset your password, you can ignore this email.</p>
</div>";
        }
    }
}
