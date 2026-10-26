using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MvcLoveMoney.Data;
using MvcLoveMoney.Enums;
using MvcLoveMoney.Helpers;
using MvcLoveMoney.Models;
using MvcLoveMoney.ViewModels;
using System.Diagnostics;
using System.Globalization;

namespace MvcLoveMoney.Controllers
{
    public class HomeController : Controller
    {
        // Max slices in the pie; the rest is merged into "Other"
        private const int MaxPieSlices = 6;

        private readonly MvcLoveMoneyDbContext _context;

        public HomeController(MvcLoveMoneyDbContext context)
        {
            _context = context;
        }

        [Authorize]
        // GET: / or /Home/Index?months=12
        public async Task<IActionResult> Index(int months = 6)
        {
            if (months != 6 && months != 12)
                months = 6;

            var today = DateTime.Today;
            var currentMonth = new DateTime(today.Year, today.Month, 1);
            var firstMonth = currentMonth.AddMonths(-(months - 1));
            var nextMonth = currentMonth.AddMonths(1);

            // Only the logged-in user's transactions
            var userId = User.GetUserId();
            var myTransactions = _context.Transactions.Where(t => t.UserId == userId);

            var dashboardVM = new DashboardVM
            {
                MonthCount = months,
                CurrentMonth = currentMonth
            };

            // ===== Total balance (all time) =====
            var totalIncome = await myTransactions
                .Where(t => t.TransactionType == TransactionType.Income)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;
            var totalExpense = await myTransactions
                .Where(t => t.TransactionType == TransactionType.Expense)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;
            dashboardVM.TotalBalance = totalIncome - totalExpense;

            // ===== Income / expense per month (bar chart) =====
            dashboardVM.Months = await GetMonthSummariesAsync(myTransactions, firstMonth, currentMonth);

            // ===== Monthly comparison table (starts at the current month) =====
            dashboardVM.Comparison = await BuildComparisonAsync(myTransactions, currentMonth, currentMonth, months);

            // ===== Current month vs previous month (for the cards) =====
            var current = dashboardVM.Months[^1];
            var previous = dashboardVM.Months[^2];
            dashboardVM.MonthIncome = current.Income;
            dashboardVM.MonthExpense = current.Expense;
            dashboardVM.PrevMonthIncome = previous.Income;
            dashboardVM.PrevMonthExpense = previous.Expense;

            // ===== Expense by category (current month) =====
            var byCategory = await myTransactions
                .Where(t => t.TransactionType == TransactionType.Expense
                         && t.TransactionDate >= currentMonth && t.TransactionDate < nextMonth)
                .GroupBy(t => new { t.Category!.Name, t.Category.Icon, t.Category.ThemeColor })
                .Select(g => new CategorySliceVM
                {
                    Name = g.Key.Name,
                    Icon = g.Key.Icon,
                    Color = g.Key.ThemeColor,
                    Amount = g.Sum(t => t.Amount)
                })
                .OrderByDescending(c => c.Amount)
                .ToListAsync();

            if (byCategory.Count > MaxPieSlices)
            {
                var other = new CategorySliceVM
                {
                    Name = "Other",
                    Icon = "bi bi-three-dots",
                    Color = "#adb5bd",
                    Amount = byCategory.Skip(MaxPieSlices - 1).Sum(c => c.Amount)
                };
                byCategory = byCategory.Take(MaxPieSlices - 1).Append(other).ToList();
            }

            if (current.Expense > 0)
            {
                foreach (var slice in byCategory)
                    slice.Percent = slice.Amount / current.Expense * 100;
            }
            dashboardVM.ExpenseByCategory = byCategory;

            return View(dashboardVM);
        }

        // GET: Home/MonthlyComparison?end=2026-04&months=6
        // Called by Ajax when the user clicks the ‹ › arrows of the comparison table
        [Authorize]
        public async Task<IActionResult> MonthlyComparison(string? end, int months = 6)
        {
            if (months != 6 && months != 12)
                months = 6;

            var today = DateTime.Today;
            var currentMonth = new DateTime(today.Year, today.Month, 1);

            // "end" = last month shown (yyyy-MM). Invalid or in the future → current month
            var endMonth = currentMonth;
            if (DateTime.TryParseExact(end, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
                && parsed < currentMonth)
            {
                endMonth = parsed;
            }

            var userId = User.GetUserId();
            var myTransactions = _context.Transactions.Where(t => t.UserId == userId);

            var comparisonVM = await BuildComparisonAsync(myTransactions, endMonth, currentMonth, months);
            return PartialView("_MonthlyComparison", comparisonVM);
        }

        // ========== HELPERS ==========

        // Income / expense for every month from firstMonth to lastMonth (months with no data = 0)
        private static async Task<List<MonthSummaryVM>> GetMonthSummariesAsync(
            IQueryable<Transaction> transactions, DateTime firstMonth, DateTime lastMonth)
        {
            var afterLastMonth = lastMonth.AddMonths(1);

            // One row per month + type
            var monthlyTotals = await transactions
                .Where(t => t.TransactionDate >= firstMonth && t.TransactionDate < afterLastMonth)
                .GroupBy(t => new { t.TransactionDate.Year, t.TransactionDate.Month, t.TransactionType })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    g.Key.TransactionType,
                    Total = g.Sum(t => t.Amount)
                })
                .ToListAsync();

            decimal SumFor(DateTime month, TransactionType type) =>
                monthlyTotals
                    .Where(x => x.Year == month.Year && x.Month == month.Month && x.TransactionType == type)
                    .Sum(x => x.Total);

            var months = new List<MonthSummaryVM>();
            for (var month = firstMonth; month <= lastMonth; month = month.AddMonths(1))
            {
                months.Add(new MonthSummaryVM
                {
                    Month = month,
                    Income = SumFor(month, TransactionType.Income),
                    Expense = SumFor(month, TransactionType.Expense)
                });
            }
            return months;
        }

        // The comparison table: "monthCount" months ending at endMonth
        private static async Task<MonthlyComparisonVM> BuildComparisonAsync(
            IQueryable<Transaction> transactions, DateTime endMonth, DateTime currentMonth, int monthCount)
        {
            var startMonth = endMonth.AddMonths(-(monthCount - 1));

            // Load one extra month before the page so its first row also has "vs previous month"
            var months = await GetMonthSummariesAsync(transactions, startMonth.AddMonths(-1), endMonth);
            for (int i = 1; i < months.Count; i++)
                months[i].BalanceChange = months[i].Balance - months[i - 1].Balance;
            months.RemoveAt(0);

            // Older pages exist only if there are transactions before this page
            var firstDate = await transactions.MinAsync(t => (DateTime?)t.TransactionDate);

            return new MonthlyComparisonVM
            {
                Months = months,
                MonthCount = monthCount,
                StartMonth = startMonth,
                EndMonth = endMonth,
                CurrentMonth = currentMonth,
                HasPrevious = firstDate.HasValue && firstDate.Value < startMonth,
                HasNext = endMonth < currentMonth
            };
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
