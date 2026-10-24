using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using MvcLoveMoney.Models;
using System.ComponentModel.DataAnnotations;

namespace MvcLoveMoney.ViewModels
{
    public class TransactionViewModel
    {
        [Required(ErrorMessage = "Please enter the amount.")]
        [Display(Name = "Amount")]
        public decimal Amount { get; set; }

        [Display(Name = "Date")]
        public DateTime Date { get; set; } = DateTime.Today;

        [Display(Name = "Note")]
        public string? Note { get; set; }

        [Required(ErrorMessage = "Please select a category")]
        [Display(Name = "Category")]
        public int? CategoryId { get; set; }                 // giá trị được chọn

        [ValidateNever]
        public List<Category> Categories { get; set; } = new();   // danh sách cho dropdown
    }
}
