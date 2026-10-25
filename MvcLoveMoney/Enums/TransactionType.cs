using System.ComponentModel.DataAnnotations;

namespace MvcLoveMoney.Enums
{
    public enum TransactionType
    {
        [Display(Name = "Expense")]
        Expense = 1,
        [Display(Name = "Income")]
        Income = 2
    }
}
