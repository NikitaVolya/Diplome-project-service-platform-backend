using AdminPanel.Models;
using BLL.Admin.Interfaces;
using BLL.Admin.Models;
using Domain.Common;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminPanel.Controllers
{
    [Authorize(Policy = AppRoles.ModerationPolicy)]
    public class CategoriesController : AdminControllerBase
    {
        private readonly IAdminCategoryService _categories;
        private readonly IExcelExportService _excel;
        private readonly IAuditLogService _audit;

        public CategoriesController(
            IAdminCategoryService categories,
            IExcelExportService excel,
            IAuditLogService audit)
        {
            _categories = categories;
            _excel = excel;
            _audit = audit;
        }

        public async Task<IActionResult> Index([FromQuery] CategoryFilter filter, CancellationToken cancellationToken)
        {
            var model = new CategoriesViewModel
            {
                Filter = filter,
                Items = await _categories.GetTreeAsync(filter, cancellationToken)
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? parentId, CancellationToken cancellationToken)
        {
            var model = new CategoryFormViewModel
            {
                Category = new AdminCategoryEditModel { ParentCategoryId = parentId, IsActive = true },
                ParentOptions = await _categories.GetParentOptionsAsync(null, cancellationToken)
            };

            return View("Form", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdminCategoryEditModel category, CancellationToken cancellationToken)
        {
            var result = await _categories.CreateAsync(category, cancellationToken);
            await AuditAsync(_audit, AuditAction.Create, "Category", category.Id.ToString(), result);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View("Form", new CategoryFormViewModel
                {
                    Category = category,
                    ParentOptions = await _categories.GetParentOptionsAsync(null, cancellationToken)
                });
            }

            Flash(result);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var category = await _categories.GetForEditAsync(id, cancellationToken);
            if (category == null)
            {
                FlashError("Category not found.");
                return RedirectToAction(nameof(Index));
            }

            return View("Form", new CategoryFormViewModel
            {
                Category = category,
                ParentOptions = await _categories.GetParentOptionsAsync(id, cancellationToken)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AdminCategoryEditModel category, CancellationToken cancellationToken)
        {
            var result = await _categories.UpdateAsync(category, cancellationToken);
            await AuditAsync(_audit, AuditAction.Update, "Category", category.Id.ToString(), result);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View("Form", new CategoryFormViewModel
                {
                    Category = category,
                    ParentOptions = await _categories.GetParentOptionsAsync(category.Id, cancellationToken)
                });
            }

            Flash(result);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken)
        {
            var result = await _categories.ToggleActiveAsync(id, cancellationToken);
            Flash(result);
            await AuditAsync(_audit, AuditAction.Update, "Category", id.ToString(), result);

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Policy = AppRoles.AdminOnlyPolicy)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var result = await _categories.DeleteAsync(id, cancellationToken);
            Flash(result);
            await AuditAsync(_audit, AuditAction.Delete, "Category", id.ToString(), result, AuditSeverity.Warning);

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Export([FromQuery] CategoryFilter filter, CancellationToken cancellationToken)
        {
            var items = await _categories.GetTreeAsync(filter, cancellationToken);

            var headers = new[] { "Id", "Name", "Parent", "Description", "Sub-categories", "Orders", "Visible" };

            var rows = items.Select(c => (IReadOnlyList<object?>)new object?[]
            {
                c.Id,
                c.Name,
                c.ParentCategoryName ?? "—",
                c.Description,
                c.SubCategoriesCount,
                c.OrdersCount,
                c.IsActive
            });

            var file = _excel.Build("Categories", headers, rows);

            await AuditAsync(_audit, AuditAction.Export, "Category", null,
                $"Exported {items.Count} category(-ies) to Excel");

            return Xlsx(file, "servicehub-categories");
        }
    }
}
