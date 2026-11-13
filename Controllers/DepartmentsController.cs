using EmployeeManagement.Web.Data;
using EmployeeManagement.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
namespace EmployeeManagement.Web.Controllers;

[Authorize(Roles = "Admin,HR")]
public class DepartmentsController : Controller
{
    private readonly ApplicationDbContext _context;

    public DepartmentsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /Departments
    public async Task<IActionResult> Index(
    string? searchString,
    string? status,
    int page = 1)
    {
        const int pageSize = 5;

        var query = _context.Departments.AsQueryable();

        // Search
        if (!string.IsNullOrWhiteSpace(searchString))
        {
            searchString = searchString.Trim();

            query = query.Where(d =>
                d.Name.Contains(searchString));
        }

        // Filter status
        if (status == "Active")
        {
            query = query.Where(d => d.IsActive);
        }
        else if (status == "Inactive")
        {
            query = query.Where(d => !d.IsActive);
        }

        // Total records
        var totalItems = await query.CountAsync();

        // Pagination
        var totalPages = (int)Math.Ceiling(
            totalItems / (double)pageSize);

        page = Math.Max(1, page);

        if (totalPages > 0)
        {
            page = Math.Min(page, totalPages);
        }

        var departments = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.SearchString = searchString;
        ViewBag.Status = status;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalItems = totalItems;

        return View(departments);
    }

    // GET: /Departments/Create
    public IActionResult Create()
    {
        return View(new Department { IsActive = true });
    }

    // POST: /Departments/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Department department)
    {
        department.Name = department.Name.Trim();

        if (!ModelState.IsValid)
        {
            return View(department);
        }

        // Check duplicate department name
        var nameExists = await _context.Departments
            .AnyAsync(d => d.Name == department.Name);

        if (nameExists)
        {
            ModelState.AddModelError(
                nameof(department.Name),
                "A department with this name already exists."
            );

            return View(department);
        }

        // System-managed fields
        department.CreatedAt = DateTime.UtcNow;
        department.IsDeleted = false;
        department.DeletedAt = null;

        _context.Departments.Add(department);

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            "Department created successfully.";

        return RedirectToAction(nameof(Index));
    }

    // GET: /Departments/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var department = await _context.Departments.FindAsync(id);

        if (department == null)
        {
            return NotFound();
        }

        return View(department);
    }

    // POST: /Departments/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Department department)
    {
        if (id != department.Id)
        {
            return NotFound();
        }

        department.Name = department.Name.Trim();

        if (!ModelState.IsValid)
        {
            return View(department);
        }

        var existingDepartment = await _context.Departments
            .FindAsync(id);

        if (existingDepartment == null)
        {
            return NotFound();
        }

        // Check duplicate name, excluding the current department
        var nameExists = await _context.Departments
            .AnyAsync(d =>
                d.Id != id &&
                d.Name == department.Name
            );

        if (nameExists)
        {
            ModelState.AddModelError(
                nameof(department.Name),
                "A department with this name already exists."
            );

            return View(department);
        }

        existingDepartment.Name = department.Name;
        existingDepartment.Description = department.Description;
        existingDepartment.IsActive = department.IsActive;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            "Department updated successfully.";

        return RedirectToAction(nameof(Index));
    }
    // GET: /Departments/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var department = await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == id);

        if (department == null)
        {
            return NotFound();
        }

        return View(department);
    }

    // POST: /Departments/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var department = await _context.Departments.FindAsync(id);

        if (department == null)
        {
            return NotFound();
        }

        department.IsDeleted = true;
        department.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Department moved to trash.";

        return RedirectToAction(nameof(Index));
    }

    // GET: /Departments/Trash
    public async Task<IActionResult> Trash()
    {
        var deletedDepartments = await _context.Departments
            .IgnoreQueryFilters()
            .Where(d => d.IsDeleted)
            .OrderByDescending(d => d.DeletedAt)
            .ToListAsync();

        return View(deletedDepartments);
    }

    // POST: /Departments/Restore/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id)
    {
        var department = await _context.Departments
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.Id == id);

        if (department == null || !department.IsDeleted)
        {
            return NotFound();
        }

        // Check whether another non-deleted department uses this name
        var nameExists = await _context.Departments
            .AnyAsync(d =>
                d.Id != id &&
                d.Name == department.Name
            );

        if (nameExists)
        {
            TempData["ErrorMessage"] =
                $"Cannot restore '{department.Name}'. " +
                "Another department is already using this name.";

            return RedirectToAction(nameof(Trash));
        }

        department.IsDeleted = false;
        department.DeletedAt = null;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            "Department restored successfully.";

        return RedirectToAction(nameof(Trash));
    }
}