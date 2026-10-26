using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MvcLoveMoney.Data;
using MvcLoveMoney.Enums;
using MvcLoveMoney.Helpers;
using MvcLoveMoney.Models;
using MvcLoveMoney.ViewModels;

namespace MvcLoveMoney.ViewComponents
{
    public class TransactionListViewComponent : ViewComponent
    {
        private readonly MvcLoveMoneyDbContext _context;

        // DI
        public TransactionListViewComponent(MvcLoveMoneyDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(TransactionType? type = null, int? categoryId = null,
            DateTime? fromDate = null, DateTime? toDate = null, string? keyword = null,
            string? sortBy = null, string? sortDir = null)
        {
            // Only the logged-in user's transactions
            var userId = UserClaimsPrincipal.GetUserId();
            var query = _context.Transactions.Where(t => t.UserId == userId);

            if(type.HasValue)
            {
                query = query.Where(t => t.TransactionType == type.Value);
            }

            if (categoryId.HasValue)
            {
                query = query.Where(t => t.CategoryId == categoryId.Value);
            }

            // Filter by start date (from the beginning of that day)
            if (fromDate.HasValue)
            {
                var start = fromDate.Value.Date;
                query = query.Where(t => t.TransactionDate >= start);
            }

            // Filter by end date (include the whole "to" day)
            if (toDate.HasValue)
            {
                var endExclusive = toDate.Value.Date.AddDays(1);
                query = query.Where(t => t.TransactionDate < endExclusive);
            }

            // Search by title (Name)
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var searchText = keyword.Trim();
                query = query.Where(t => t.Name.Contains(searchText));
            }

            // ===== Sort (clicked column header) =====
            bool ascending = sortDir == "asc";
            IOrderedQueryable<Transaction> ordered = sortBy switch
            {
                "name" => ascending ? query.OrderBy(t => t.Name) : query.OrderByDescending(t => t.Name),
                "category" => ascending ? query.OrderBy(t => t.Category!.Name) : query.OrderByDescending(t => t.Category!.Name),
                "date" => ascending ? query.OrderBy(t => t.TransactionDate) : query.OrderByDescending(t => t.TransactionDate),
                // Amount as shown in the table: income is +, expense is -
                "amount" => ascending
                    ? query.OrderBy(t => t.TransactionType == TransactionType.Income ? t.Amount : -t.Amount)
                    : query.OrderByDescending(t => t.TransactionType == TransactionType.Income ? t.Amount : -t.Amount),
                // No sort chosen: newest first
                _ => query.OrderByDescending(t => t.TransactionDate)
            };

            // Same value (e.g. same category) → newest first, so the order never jumps around
            ordered = ordered.ThenByDescending(t => t.TransactionDate).ThenByDescending(t => t.Id);

            // Tell the view which column is sorted, to draw the arrow
            ViewData["SortBy"] = sortBy is "name" or "category" or "date" or "amount" ? sortBy : null;
            ViewData["SortDir"] = ascending ? "asc" : "desc";

            var transactions = await ordered
                .Select(t => new TransactionVM
            {
                Id = t.Id,
                TransactionType = t.TransactionType,
                Name = t.Name,
                Amount = t.Amount,
                TransactionDate = t.TransactionDate,
                Note = t.Note,
                CategoryId = t.CategoryId,
                CategoryName = t.Category!.Name,          
                CategoryIcon = t.Category.Icon,
                CategoryThemeColor = t.Category.ThemeColor
            }).ToListAsync();

            return View(transactions);
        }
    }
}
