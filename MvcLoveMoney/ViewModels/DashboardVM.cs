namespace MvcLoveMoney.ViewModels
{
    public class DashboardVM
    {
        // How many months the bar chart / comparison table covers (6 or 12)
        public int MonthCount { get; set; }
        public DateTime CurrentMonth { get; set; }

        // ===== Cards =====
        public decimal TotalBalance { get; set; }       // all income - all expense, all time
        public decimal MonthIncome { get; set; }
        public decimal MonthExpense { get; set; }
        public decimal MonthBalance => MonthIncome - MonthExpense;

        public decimal PrevMonthIncome { get; set; }
        public decimal PrevMonthExpense { get; set; }
        public decimal PrevMonthBalance => PrevMonthIncome - PrevMonthExpense;

        // ===== Charts =====
        public List<MonthSummaryVM> Months { get; set; } = new();          // oldest → newest
        public List<CategorySliceVM> ExpenseByCategory { get; set; } = new(); // current month, biggest first

        // ===== Monthly comparison table (has its own ‹ › paging) =====
        public MonthlyComparisonVM Comparison { get; set; } = new();

        // % change vs the previous month; null when the previous month is 0 (no base to compare)
        public static decimal? PercentChange(decimal current, decimal previous)
        {
            if (previous == 0) return null;
            return (current - previous) / Math.Abs(previous) * 100;
        }
    }

    public class MonthSummaryVM
    {
        public DateTime Month { get; set; }
        public decimal Income { get; set; }
        public decimal Expense { get; set; }
        public decimal Balance => Income - Expense;

        // Balance minus the previous month's balance (only filled for the comparison table)
        public decimal? BalanceChange { get; set; }
    }

    public class MonthlyComparisonVM
    {
        public List<MonthSummaryVM> Months { get; set; } = new();   // oldest → newest
        public int MonthCount { get; set; }
        public DateTime StartMonth { get; set; }
        public DateTime EndMonth { get; set; }
        public DateTime CurrentMonth { get; set; }

        // Can the user page to older / newer months?
        public bool HasPrevious { get; set; }
        public bool HasNext { get; set; }
    }

    public class CategorySliceVM
    {
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal Percent { get; set; }
    }
}
