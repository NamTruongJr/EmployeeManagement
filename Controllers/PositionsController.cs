
using EmployeeManagement.Web.Data;
using EmployeeManagement.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
namespace EmployeeManagement.Web.Controllers;

[Authorize(Roles = "Admin,HR")]
public class PositionsController : Controller
{
    private readonly ApplicationDbContext _context;

    public PositionsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Positions
    public async Task<IActionResult> Index(
        string? searchString,
        string? status,
        int page = 1)
    {
        const int pageSize = 5;

        var query = _context.Positions.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchString))
        {
            searchString = searchString.Trim();
            query = query.Where(p => p.Name.Contains(searchString));
        }

        if (status == "Active")
            query = query.Where(p => p.IsActive);
        else if (status == "Inactive")
            query = query.Where(p => !p.IsActive);
        else
            status = null;

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        page = Math.Max(1, page);

        if (totalPages > 0)
            page = Math.Min(page, totalPages);

        var positions = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.SearchString = searchString;
        ViewBag.Status = status;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalItems = totalItems;

        return View(positions);
    }

    // GET: Positions/Create
    public IActionResult Create()
    {
        return View(new Position());
    }

    // POST: Positions/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Position position)
    {
        position.Name = position.Name?.Trim() ?? string.Empty;

        var duplicate = await _context.Positions
            .AnyAsync(p => p.Name == position.Name);

        if (duplicate)
        {
            ModelState.AddModelError(
                nameof(position.Name),
                "This position name already exists.");
        }

        if (!ModelState.IsValid)
            return View(position);

        position.CreatedAt = DateTime.UtcNow;

        _context.Positions.Add(position);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Position created successfully.";

        return RedirectToAction(nameof(Index));
    }

    // GET: Positions/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
            return NotFound();

        var position = await _context.Positions.FindAsync(id);

        if (position == null)
            return NotFound();

        return View(position);
    }

    // POST: Positions/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Position position)
    {
        if (id != position.Id)
            return NotFound();

        var existing = await _context.Positions.FindAsync(id);

        if (existing == null)
            return NotFound();

        position.Name = position.Name?.Trim() ?? string.Empty;

        var duplicate = await _context.Positions
            .AnyAsync(p => p.Id != id && p.Name == position.Name);

        if (duplicate)
        {
            ModelState.AddModelError(
                nameof(position.Name),
                "This position name already exists.");
        }

        if (!ModelState.IsValid)
            return View(position);

        existing.Name = position.Name;
        existing.Description = position.Description;
        existing.IsActive = position.IsActive;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Position updated successfully.";

        return RedirectToAction(nameof(Index));
    }

    // GET: Positions/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
            return NotFound();

        var position = await _context.Positions
            .FirstOrDefaultAsync(p => p.Id == id);

        if (position == null)
            return NotFound();

        ViewBag.EmployeeCount = await _context.Employees
            .CountAsync(e => e.PositionId == id);

        return View(position);
    }

    // POST: Positions/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var position = await _context.Positions.FindAsync(id);

        if (position == null)
            return NotFound();

        var hasEmployees = await _context.Employees
            .AnyAsync(e => e.PositionId == id);

        if (hasEmployees)
        {
            TempData["ErrorMessage"] =
                "Cannot delete this position because employees are assigned to it. Reassign them first.";

            return RedirectToAction(nameof(Index));
        }

        position.IsDeleted = true;
        position.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Position moved to Trash.";

        return RedirectToAction(nameof(Index));
    }

    // GET: Positions/Trash
    public async Task<IActionResult> Trash()
    {
        var deletedPositions = await _context.Positions
            .IgnoreQueryFilters()
            .Where(p => p.IsDeleted)
            .OrderByDescending(p => p.DeletedAt)
            .ToListAsync();

        return View(deletedPositions);
    }

    // POST: Positions/Restore/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id)
    {
        var position = await _context.Positions
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == id && p.IsDeleted);

        if (position == null)
            return NotFound();

        var duplicate = await _context.Positions
            .AnyAsync(p => p.Name == position.Name);

        if (duplicate)
        {
            TempData["ErrorMessage"] =
                "Cannot restore because another active position has the same name.";

            return RedirectToAction(nameof(Trash));
        }

        position.IsDeleted = false;
        position.DeletedAt = null;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Position restored successfully.";

        return RedirectToAction(nameof(Trash));
    }
}
