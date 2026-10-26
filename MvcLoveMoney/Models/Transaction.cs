using MvcLoveMoney.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MvcLoveMoney.Models
{
    public class Transaction
    {
        public int Id { get; set; }

        [Display(Name = "Type")]
        public TransactionType TransactionType { get; set; } = TransactionType.Expense;

        public string Name { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Amount { get; set; }

        [Display(Name = "Date")]
        [DataType(DataType.Date)]
        public DateTime TransactionDate { get; set; } = DateTime.Now;

        public string? Note { get; set; }

        [Display(Name = "Category")]
        public int CategoryId { get; set; }
        public virtual Category? Category { get; set; }

        // Owner of this transaction
        public int UserId { get; set; }
        public virtual User? User { get; set; }
    }
}
