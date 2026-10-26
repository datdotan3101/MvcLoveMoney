using Microsoft.EntityFrameworkCore;

namespace MvcLoveMoney.ViewModels
{
    // Paging info for a table (used by Views/Shared/_Pagination.cshtml)
    public class PagedListVM
    {
        // Choices in the "per page" dropdown
        public static readonly int[] PageSizeOptions = { 10, 25, 50, 75, 100 };
        public const int DefaultPageSize = 10;

        public int Page { get; set; } = 1;              // 1-based
        public int PageSize { get; set; } = DefaultPageSize;
        public int TotalCount { get; set; }             // rows matching the filter (all pages)

        public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);

        // Page numbers to show: 1 … 4 5 [6] 7 8 … 20   (null = "…")
        public List<int?> PageNumbers()
        {
            var pages = new List<int?>();
            int from = Math.Max(2, Page - 2);
            int to = Math.Min(TotalPages - 1, Page + 2);

            pages.Add(1);
            if (from > 2) pages.Add(null);
            for (int i = from; i <= to; i++) pages.Add(i);
            if (to < TotalPages - 1) pages.Add(null);
            if (TotalPages > 1) pages.Add(TotalPages);

            return pages;
        }
    }

    // One page of rows + the paging info
    public class PagedListVM<T> : PagedListVM
    {
        public List<T> Items { get; set; } = new();

        // Counts the rows, then loads only the requested page from the database.
        // "source" must already be sorted. A page out of range goes to the nearest valid page.
        public static async Task<PagedListVM<T>> CreateAsync(IQueryable<T> source, int page, int pageSize)
        {
            if (!PageSizeOptions.Contains(pageSize))
                pageSize = DefaultPageSize;

            var result = new PagedListVM<T>
            {
                PageSize = pageSize,
                TotalCount = await source.CountAsync()
            };
            result.Page = Math.Clamp(page, 1, result.TotalPages);

            result.Items = await source
                .Skip((result.Page - 1) * result.PageSize)
                .Take(result.PageSize)
                .ToListAsync();

            return result;
        }
    }
}
