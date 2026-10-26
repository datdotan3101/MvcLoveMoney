using Microsoft.AspNetCore.Mvc;
using MvcLoveMoney.Configs;
using MvcLoveMoney.Enums;
using MvcLoveMoney.Helpers;
using System.ComponentModel.DataAnnotations;

namespace MvcLoveMoney.ViewModels
{
    [Bind("Id,CategoryType,Name,Icon,ThemeColor")]
    public class CategoryVM
    {
        // Sent to the browser encrypted (IIdProtector), decrypted again when the form is posted
        [ModelBinder(typeof(ProtectedIdModelBinder))]
        public int Id { get; set; }

        [Display(Name = "Type")]
        public TransactionType CategoryType { get; set; } = TransactionType.Expense;

        [Display(Name = "Name")]
        [Required(ErrorMessage = "Please enter a category name.")]
        [StringLength(MaxLengths.NameLength, ErrorMessage = "Name cannot exceed {1} characters.")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Icon")]
        [Required(ErrorMessage = "Please choose an icon.")]
        [StringLength(MaxLengths.CategoryIcon)]
        public string Icon { get; set; } = "bx bx-home";

        [Display(Name = "Theme color")]
        [Required(ErrorMessage = "Please choose a color.")]
        [StringLength(MaxLengths.ThemeColor)]
        public string ThemeColor { get; set; } = "#0d6efd";

    }
}
