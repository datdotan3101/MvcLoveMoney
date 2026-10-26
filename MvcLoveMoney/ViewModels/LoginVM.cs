using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace MvcLoveMoney.ViewModels
{
    [Bind("Email,Password")]
    public class LoginVM
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

    }
}
