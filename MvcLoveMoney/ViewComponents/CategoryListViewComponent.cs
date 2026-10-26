using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MvcLoveMoney.Data;
using MvcLoveMoney.Enums;
using MvcLoveMoney.Helpers;
using MvcLoveMoney.ViewModels;

namespace MvcLoveMoney.ViewComponents
{
    public class CategoryListViewComponent : ViewComponent
    {

        // Constructor
        private readonly MvcLoveMoneyDbContext _context;

        // DI
        public CategoryListViewComponent(MvcLoveMoneyDbContext context)
        {
            _context = context;
        }

        // InvokeAsync

        public async Task<IViewComponentResult> InvokeAsync(TransactionType? type = null,
            int page = 1, int pageSize = PagedListVM.DefaultPageSize)
        {
            // Only the logged-in user's categories
            var userId = UserClaimsPrincipal.GetUserId();
            var query = _context.Categories.Where(c => c.UserId == userId);
            if (type.HasValue)
            {
                query = query.Where(c => c.CategoryType == type.Value);
            }

            var rows = query
                .OrderBy(c => c.CategoryType)
                .ThenBy(c => c.Name)
                .ThenBy(c => c.Id)
                .Select(c => new CategoryVM
                {
                    Id = c.Id,
                    CategoryType = c.CategoryType,
                    Name = c.Name,
                    Icon = c.Icon,
                    ThemeColor = c.ThemeColor
                });

            // Paging: only the requested page is loaded
            var pagedList = await PagedListVM<CategoryVM>.CreateAsync(rows, page, pageSize);

            return View(pagedList); // → Views/Shared/Components/CategoryList/Default.cshtml
        }

    }
}
