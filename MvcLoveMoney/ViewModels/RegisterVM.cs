using Microsoft.AspNetCore.Mvc;
using MvcLoveMoney.Configs;
using System.ComponentModel.DataAnnotations;

namespace MvcLoveMoney.ViewModels
{
    [Bind("FullName,Email,Password,ConfirmPassword")]
    public class RegisterVM
    {
        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(MaxLengths.NameLength)]
        [Display(Name = "Full name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        [StringLength(MaxLengths.EmailLength)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least {2} characters.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password.")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
