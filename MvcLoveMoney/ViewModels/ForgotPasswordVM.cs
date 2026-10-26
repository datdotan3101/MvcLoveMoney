using Microsoft.AspNetCore.Mvc;
using MvcLoveMoney.Configs;
using MvcLoveMoney.Helpers;
using System.ComponentModel.DataAnnotations;

namespace MvcLoveMoney.ViewModels
{
    // Step 1: enter the email
    [Bind("Email")]
    public class ForgotPasswordVM
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        [StringLength(MaxLengths.EmailLength)]
        public string Email { get; set; } = string.Empty;
    }

    // Step 2: enter the code from the email
    [Bind("Email,Code")]
    public class VerifyOtpVM
    {
        [Required]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter the code.")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "The code has 6 digits.")]
        [Display(Name = "Verification code")]
        public string Code { get; set; } = string.Empty;

        // Seconds until "Resend code" is allowed (display only)
        public int ResendWaitSeconds { get; set; }
    }

    // Step 3: choose a new password
    [Bind("Email,Token,NewPassword,ConfirmPassword")]
    public class ResetPasswordVM
    {
        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required.")]
        [StringLength(100)]
        [StrongPassword]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your new password.")]
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm new password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
