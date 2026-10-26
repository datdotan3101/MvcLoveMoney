using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MvcLoveMoney.Data;
using MvcLoveMoney.Enums;
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
            DateTime? fromDate = null, DateTime? toDate = null, string? keyword = null) 
        {
            var query = _context.Transactions.AsQueryable();

            if(type.HasValue)
            {
                query = query.Where(t => t.TransactionType == type.Value);
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

            var transactions = await query
                .OrderByDescending(t => t.TransactionDate)
                .Select(t => new TransactionVM
            {
                Id = t.Id,
                TransactionType = t.TransactionType,
                Name = t.Name,
                Amount = t.Amount,
                TransactionDate = t.TransactionDate,
                Note = t.Note,
                CategoryId = t.CategoryId,
                CategoryName = t.Category!.Name,          // EF Core joins Categories automatically
                CategoryIcon = t.Category.Icon,
                CategoryThemeColor = t.Category.ThemeColor
            }).ToListAsync();

            return View(transactions);
        }
    }
}
