using Microsoft.AspNetCore.Mvc;
using MvcLoveMoney.Configs;
using MvcLoveMoney.Helpers;
using System.ComponentModel.DataAnnotations;

namespace MvcLoveMoney.ViewModels
{
    // Everything the Settings page needs: one sub-model per form
    public class SettingsVM
    {
        public ProfileVM Profile { get; set; } = new();
        public ChangePasswordVM ChangePassword { get; set; } = new();
        public SetPasswordVM SetPassword { get; set; } = new();

        // Display-only info
        public bool HasPassword { get; set; }
        public bool HasGoogle { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    [Bind("FullName,Email")]
    public class ProfileVM
    {
        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(MaxLengths.NameLength)]
        [Display(Name = "Full name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        [StringLength(MaxLengths.EmailLength)]
        public string Email { get; set; } = string.Empty;
    }

    // For users who already have a password
    [Bind("CurrentPassword,NewPassword,ConfirmPassword")]
    public class ChangePasswordVM
    {
        [Required(ErrorMessage = "Current password is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string CurrentPassword { get; set; } = string.Empty;

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

    // For users who signed up with Google and have no password yet
    [Bind("NewPassword,ConfirmPassword")]
    public class SetPasswordVM
    {
        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100)]
        [StrongPassword]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password.")]
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
