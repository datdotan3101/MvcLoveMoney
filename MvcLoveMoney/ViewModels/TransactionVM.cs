using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using MvcLoveMoney.Configs;
using MvcLoveMoney.Enums;
using System.ComponentModel.DataAnnotations;

namespace MvcLoveMoney.ViewModels
{
    [Bind("Id,TransactionType,Name,Amount,TransactionDate,Note,CategoryId")]
    public class TransactionVM
    {
        // ===== Form data =====
        public int Id { get; set; }

        [Display(Name = "Type")]
        public TransactionType TransactionType { get; set; } = TransactionType.Expense;

        [Display(Name = "Name")]
        [Required(ErrorMessage = "Please enter a name.")]
        [StringLength(MaxLengths.NameLength, ErrorMessage = "Name cannot exceed {1} characters.")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Amount")]
        [Required(ErrorMessage = "Please enter the amount.")]
        [Range(1, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
        public decimal? Amount { get; set; }

        [Display(Name = "Date")]
        [DataType(DataType.Date)]
        public DateTime TransactionDate { get; set; } = DateTime.Today;

        [Display(Name = "Note")]
        [StringLength(MaxLengths.NotesLength, ErrorMessage = "Note cannot exceed {1} characters.")]
        public string? Note { get; set; }

        [Display(Name = "Category")]
        [Required(ErrorMessage = "Please select a category.")]
        public int? CategoryId { get; set; }

        // ===== Display only (list / delete) =====
        [ValidateNever]
        public string CategoryName { get; set; } = string.Empty;

        [ValidateNever]
        public string CategoryIcon { get; set; } = string.Empty;

        [ValidateNever]
        public string CategoryThemeColor { get; set; } = string.Empty;

        // ===== Dropdown data =====
        [ValidateNever]
        public List<SelectListItem> Categories { get; set; } = new();
    }
}