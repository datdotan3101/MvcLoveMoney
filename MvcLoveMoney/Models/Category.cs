using MvcLoveMoney.Enums;
using System.ComponentModel.DataAnnotations;

namespace MvcLoveMoney.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Display(Name = "Type")]
        public TransactionType CategoryType { get; set; } = TransactionType.Expense;

        public string Name { get; set; } = string.Empty;

        [Display(Name = "Icon")]
        public string Icon { get; set; } = "bi-tag";

        [Display(Name = "Theme color")]
        public string ThemeColor { get; set; } = "#0d6efd";

        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}

