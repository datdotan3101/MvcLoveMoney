
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MvcLoveMoney.Data;
using MvcLoveMoney.Enums;
using MvcLoveMoney.Models;
using MvcLoveMoney.ViewModels;

public class CategoriesController : Controller
{
    private readonly MvcLoveMoneyDbContext _context;

    public CategoriesController(MvcLoveMoneyDbContext context)
    {
        _context = context;
    }

    // GET: CATEGORYS
    //public async Task<IActionResult> Index()
    //{
    //    return View(await _context.Categories.ToListAsync());
    //}

    public IActionResult Index()
    {
        return View();
    }


    // Ajax gọi để vẽ lại bảng
    public IActionResult Reload(TransactionType? type)
    {
        return ViewComponent("CategoryList", new { type });
    }

    // GET: CATEGORYS/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var category = await _context.Categories
            .FirstOrDefaultAsync(m => m.Id == id);
        if (category == null)
        {
            return NotFound();
        }

        return View(category);
    }


    // Kiểm tra đã có danh mục khác cùng Tên + Loại chưa
    private async Task CheckDuplicateAsync(CategoryVM categoryVM)
    {
        bool isDuplicate = await _context.Categories.AnyAsync(c =>
            c.Name == categoryVM.Name &&
            c.CategoryType == categoryVM.CategoryType &&
            c.Id != categoryVM.Id);               // bỏ qua chính nó (khi Edit)

        if (isDuplicate)
        {
            ModelState.AddModelError(nameof(Category.Name),
                "A category with this name already exists for this type.");
        }
    }

    // GET: CATEGORYS/Create
    public IActionResult Create()
    {
        return PartialView("CategoryForm", new CategoryVM());
    }

    // POST: CATEGORYS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryVM categoryVM)
    {
        categoryVM.Name = categoryVM.Name?.Trim() ?? string.Empty;

        await CheckDuplicateAsync(categoryVM);

        if (ModelState.IsValid)
        {
            //_context.Add(category);
            //await _context.SaveChangesAsync();
            //return RedirectToAction(nameof(Index));

            try
            {
                // Map ViewModel to entity
                var newCategory = new Category
                {
                    CategoryType = categoryVM.CategoryType,
                    Name = categoryVM.Name,
                    Icon = categoryVM.Icon,
                    ThemeColor = categoryVM.ThemeColor
                };
                await _context.Categories.AddAsync(newCategory);
                await _context.SaveChangesAsync();
                return Json(new { isOK = true, message = "Created successfully" });
            } 
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Could not save data. Please try again.");
            }
        }

        return PartialView("CategoryForm", categoryVM);

        //return View(category);
    }

    // GET: CATEGORYS/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var categoryVM = await _context.Categories
               .Where(c => c.Id == id)
               .Select(c => new CategoryVM
               {
                   Id = c.Id,
                   CategoryType = c.CategoryType,
                   Name = c.Name,
                   Icon = c.Icon,
                   ThemeColor = c.ThemeColor
               })
               .SingleOrDefaultAsync();

        if (categoryVM == null)
        {
            return NotFound();
        }

        return PartialView("CategoryForm", categoryVM);
        //return View(category);
    }

    // POST: CATEGORYS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CategoryVM categoryVM)
    {

        categoryVM.Name = categoryVM.Name?.Trim() ?? string.Empty;
        await CheckDuplicateAsync(categoryVM);

        if (ModelState.IsValid)
        {
            var category = await _context.Categories.FindAsync(categoryVM.Id);
            if (category == null) return NotFound();

            // Copy values from ViewModel to entity
            category.CategoryType = categoryVM.CategoryType;
            category.Name = categoryVM.Name;
            category.Icon = categoryVM.Icon;
            category.ThemeColor = categoryVM.ThemeColor;

            try
            {
                await _context.SaveChangesAsync();
                return Json(new { isOK = true, message = "Updated successfully" });
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError("", "Could not save data. Please try again.");
            }
        }
        return PartialView("CategoryForm", categoryVM);
        //return View(category);
    }

    // GET: CATEGORYS/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        //var category = await _context.Categories
        //    .FirstOrDefaultAsync(m => m.Id == id);

        var categoryVM = await _context.Categories
               .Where(c => c.Id == id)
               .Select(c => new CategoryVM
               {
                   Id = c.Id,
                   CategoryType = c.CategoryType,
                   Name = c.Name,
                   Icon = c.Icon,
                   ThemeColor = c.ThemeColor
               })
               .SingleOrDefaultAsync();


        if (categoryVM == null) return NotFound();

        return PartialView(categoryVM);
    }

    // POST: CATEGORYS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null)
            return Json(new { isOK = false, message = "Category not found." });

        // Kiểm tra danh mục có đang được dùng không
        bool isUsed = await _context.Transactions.AnyAsync(t => t.CategoryId == id);
        if (isUsed)
            return Json(new { isOK = false, message = "Cannot delete: this category is used by one or more transactions." });

        try
        {
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return Json(new { isOK = true, message = "Deleted successfully" });

        }
        catch (DbUpdateException)
        {
            return Json(new { isOK = false, message = "Could not delete data. Please try again." });
        }
    }

}
