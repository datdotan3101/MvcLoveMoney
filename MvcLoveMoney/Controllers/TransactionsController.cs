using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MvcLoveMoney.Data;
using MvcLoveMoney.Enums;
using MvcLoveMoney.Models;
using MvcLoveMoney.ViewModels;

namespace MvcLoveMoney.Controllers
{
    public class TransactionsController : Controller
    {
        private readonly MvcLoveMoneyDbContext _context;

        public TransactionsController(MvcLoveMoneyDbContext context)
        {
            _context = context;
        }

        // ========== HELPERS ==========

        // Load categories for the dropdown
        private async Task LoadCategoriesAsync(TransactionVM transactionVM)
        {
            transactionVM.Categories = await _context.Categories
                .Where(t => t.CategoryType == transactionVM.TransactionType)
                .OrderBy(c => c.CategoryType)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),    // value sent to the server (CategoryId)
                    Text = c.Name               // text shown in the dropdown
                })
                .ToListAsync();
        }

        // Check that the category type matches the transaction type
        private async Task CheckCategoryTypeAsync(TransactionVM transactionVM)
        {
            // No category selected → [Required] on CategoryId already reports this
            if (transactionVM.CategoryId == null) return;

            var category = await _context.Categories.FindAsync(transactionVM.CategoryId.Value);

            if (category == null)
            {
                ModelState.AddModelError("", "The selected category does not exist.");
            }
            else if (category.CategoryType != transactionVM.TransactionType)
            {
                ModelState.AddModelError("",
                    $"Category '{category.Name}' is not an {transactionVM.TransactionType} category.");
            }
        }

        // ========== LIST ==========

        public IActionResult Index()
        {
            return View();
        }

        // Called by Ajax to re-render the table (with optional filter)
        public IActionResult Reload(TransactionType? type, int? categoryId, DateTime? fromDate, DateTime? toDate, string? keyword)
        {
            // If the user picks From > To, swap them
            if (fromDate.HasValue && toDate.HasValue && fromDate > toDate)
            {
                (fromDate, toDate) = (toDate, fromDate);
            }

            return ViewComponent("TransactionList", new { type, categoryId, fromDate, toDate, keyword });
        }

        // GET: Transactions/GetCategories?type=1
        // Called by Ajax when the user changes the Type dropdown
        public async Task<IActionResult> GetCategories(TransactionType? type)
        {
            var query = _context.Categories.AsQueryable();

            if (type.HasValue)
            {
                query = query.Where(c => c.CategoryType == type.Value);
            }

            var categories = await query                       
            .OrderBy(c => c.CategoryType)
            .ThenBy(c => c.Name)
            .Select(c => new { id = c.Id, name = c.Name })
            .ToListAsync();

            return Json(categories);
        }

        // GET: Transactions/GetSummary?fromDate=2026-10-01&toDate=2026-10-26
        // Returns total income and total expense in the date range
        public async Task<IActionResult> GetSummary(DateTime? fromDate, DateTime? toDate)
        {
            // If From > To, swap them
            if (fromDate.HasValue && toDate.HasValue && fromDate > toDate)
            {
                (fromDate, toDate) = (toDate, fromDate);
            }

            var query = _context.Transactions.AsQueryable();

            if (fromDate.HasValue)
            {
                var start = fromDate.Value.Date;
                query = query.Where(t => t.TransactionDate >= start);
            }

            if (toDate.HasValue)
            {
                var endExclusive = toDate.Value.Date.AddDays(1);
                query = query.Where(t => t.TransactionDate < endExclusive);
            }

            decimal income = await query
                .Where(t => t.TransactionType == TransactionType.Income)
                .SumAsync(t => t.Amount);

            decimal expense = await query
                .Where(t => t.TransactionType == TransactionType.Expense)
                .SumAsync(t => t.Amount);

            return Json(new { income, expense });
        }

        // ========== CREATE ==========

        // GET: Transactions/Create
        public async Task<IActionResult> Create()
        {
            var transactionVM = new TransactionVM();
            await LoadCategoriesAsync(transactionVM);
            return PartialView("TransactionForm", transactionVM);
        }

        // POST: Transactions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TransactionVM transactionVM)
        {
            transactionVM.Name = transactionVM.Name?.Trim() ?? string.Empty;
            await CheckCategoryTypeAsync(transactionVM);

            if (ModelState.IsValid)
            {
                try
                {
                    // Map ViewModel to entity
                    var newTransaction = new Transaction
                    {
                        TransactionType = transactionVM.TransactionType,
                        Name = transactionVM.Name,
                        Amount = transactionVM.Amount!.Value,
                        TransactionDate = transactionVM.TransactionDate,
                        Note = transactionVM.Note?.Trim(),
                        CategoryId = transactionVM.CategoryId!.Value
                    };
                    await _context.Transactions.AddAsync(newTransaction);
                    await _context.SaveChangesAsync();
                    return Json(new { isOK = true, message = "Created successfully" });
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("", "Could not save data. Please try again.");
                }
            }

            await LoadCategoriesAsync(transactionVM);     // reload dropdown on error
            return PartialView("TransactionForm", transactionVM);
        }

        // ========== EDIT ==========

        // GET: Transactions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var transactionVM = await _context.Transactions
                .Where(t => t.Id == id)
                .Select(t => new TransactionVM
                {
                    Id = t.Id,
                    TransactionType = t.TransactionType,
                    Name = t.Name,
                    Amount = t.Amount,
                    TransactionDate = t.TransactionDate,
                    Note = t.Note,
                    CategoryId = t.CategoryId
                })
                .SingleOrDefaultAsync();

            if (transactionVM == null) return NotFound();

            await LoadCategoriesAsync(transactionVM);
            return PartialView("TransactionForm", transactionVM);
        }

        // POST: Transactions/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(TransactionVM transactionVM)
        {
            transactionVM.Name = transactionVM.Name?.Trim() ?? string.Empty;
            await CheckCategoryTypeAsync(transactionVM);

            if (ModelState.IsValid)
            {
                var transaction = await _context.Transactions.FindAsync(transactionVM.Id);
                if (transaction == null) return NotFound();

                // Copy values from ViewModel to entity
                transaction.TransactionType = transactionVM.TransactionType;
                transaction.Name = transactionVM.Name;
                transaction.Amount = transactionVM.Amount!.Value;
                transaction.TransactionDate = transactionVM.TransactionDate;
                transaction.Note = transactionVM.Note?.Trim();
                transaction.CategoryId = transactionVM.CategoryId!.Value;

                try
                {
                    await _context.SaveChangesAsync();
                    return Json(new { isOK = true, message = "Updated successfully" });
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("", "Could not save data. Please try again.");
                }
            }

            await LoadCategoriesAsync(transactionVM);     // reload dropdown on error
            return PartialView("TransactionForm", transactionVM);
        }

        // ========== DELETE ==========

        // GET: Transactions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var transactionVM = await _context.Transactions
                .Where(t => t.Id == id)
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
                })
                .SingleOrDefaultAsync();

            if (transactionVM == null) return NotFound();

            return PartialView(transactionVM);           // → Views/Transactions/Delete.cshtml
        }

        // POST: Transactions/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var transaction = await _context.Transactions.FindAsync(id);
            if (transaction == null)
                return Json(new { isOK = false, message = "Transaction not found." });

            try
            {
                _context.Transactions.Remove(transaction);
                await _context.SaveChangesAsync();
                return Json(new { isOK = true, message = "Deleted successfully" });
            }
            catch (DbUpdateException)
            {
                return Json(new { isOK = false, message = "Could not delete data. Please try again." });
            }
        }
    }
}