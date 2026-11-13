using EmployeeManagement.Web.Data;
using EmployeeManagement.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace EmployeeManagement.Web.Controllers;

[Authorize(Roles = "Admin,HR")]
public class EmployeesController : Controller
{
    private readonly ApplicationDbContext _context;

    public EmployeesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Employees
    public async Task<IActionResult> Index(
        string? searchString,
        int? departmentId,
        string? status,
        int page = 1)
    {
        const int pageSize = 5;

        var query = _context.Employees
            .Include(e => e.Department)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchString))
        {
            searchString = searchString.Trim();

            query = query.Where(e =>
                e.FullName.Contains(searchString) ||
                e.EmployeeCode.Contains(searchString) ||
                (e.Email != null && e.Email.Contains(searchString)));
        }

        if (departmentId.HasValue)
        {
            query = query.Where(e => e.DepartmentId == departmentId.Value);
        }

        if (status == "Active")
            query = query.Where(e => e.IsActive);
        else if (status == "Inactive")
            query = query.Where(e => !e.IsActive);

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        page = Math.Max(1, page);

        if (totalPages > 0)
            page = Math.Min(page, totalPages);

        var employees = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.SearchString = searchString;
        ViewBag.DepartmentId = departmentId;
        ViewBag.Status = status;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalItems = totalItems;

        await LoadDepartmentsAsync(departmentId);

        return View(employees);
    }

    // GET: Employees/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
            return NotFound();

        var employee = await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null)
            return NotFound();

        return View(employee);
    }

    // GET: Employees/Create
    public async Task<IActionResult> Create()
    {
        await LoadDepartmentsAsync();
        await LoadPositionsAsync();

        return View(new Employee
        {
            HireDate = DateTime.Today,
            IsActive = true
        });
    }

    // POST: Employees/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Employee employee)
    {
        NormalizeEmployeeText(employee);

        // Check duplicate EmployeeCode
        if (await _context.Employees.AnyAsync(e =>
            e.EmployeeCode == employee.EmployeeCode))
        {
            ModelState.AddModelError(
                nameof(employee.EmployeeCode),
                "Employee code already exists.");
        }

        // Validate Department
        var department = await _context.Departments
            .FirstOrDefaultAsync(d =>
                d.Id == employee.DepartmentId && d.IsActive);

        if (department == null)
        {
            ModelState.AddModelError(
                nameof(employee.DepartmentId),
                "Please select a valid active department.");
        }

        // Validate Position (optional)
        if (employee.PositionId.HasValue)
        {
            var position = await _context.Positions
                .FirstOrDefaultAsync(p =>
                    p.Id == employee.PositionId.Value && p.IsActive);

            if (position == null)
            {
                ModelState.AddModelError(
                    nameof(employee.PositionId),
                    "Please select a valid active position.");
            }
        }

        if (!ModelState.IsValid)
        {
            await LoadDepartmentsAsync(employee.DepartmentId);
            await LoadPositionsAsync(employee.PositionId);

            return View(employee);
        }

        employee.CreatedAt = DateTime.UtcNow;

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Employee created successfully.";

        return RedirectToAction(nameof(Index));
    }

    // GET: Employees/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
            return NotFound();

        var employee = await _context.Employees.FindAsync(id);

        if (employee == null)
            return NotFound();

        await LoadDepartmentsAsync(
            employee.DepartmentId,
            employee.DepartmentId);

        await LoadPositionsAsync(
            employee.PositionId,
            employee.PositionId);

        return View(employee);
    }

    // POST: Employees/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Employee employee)
    {
        if (id != employee.Id)
            return NotFound();

        var existingEmployee = await _context.Employees.FindAsync(id);

        if (existingEmployee == null)
            return NotFound();

        NormalizeEmployeeText(employee);

        // Check duplicate EmployeeCode
        if (await _context.Employees.AnyAsync(e =>
            e.EmployeeCode == employee.EmployeeCode &&
            e.Id != id))
        {
            ModelState.AddModelError(
                nameof(employee.EmployeeCode),
                "Employee code already exists.");
        }

        // Validate Department
        var department = await _context.Departments
            .FirstOrDefaultAsync(d =>
                d.Id == employee.DepartmentId &&
                (d.IsActive || d.Id == existingEmployee.DepartmentId));

        if (department == null)
        {
            ModelState.AddModelError(
                nameof(employee.DepartmentId),
                "Please select a valid department.");
        }

        // Validate Position (optional)
        if (employee.PositionId.HasValue)
        {
            var position = await _context.Positions
                .FirstOrDefaultAsync(p =>
                    p.Id == employee.PositionId.Value &&
                    (p.IsActive || p.Id == existingEmployee.PositionId));

            if (position == null)
            {
                ModelState.AddModelError(
                    nameof(employee.PositionId),
                    "Please select a valid position.");
            }
        }

        if (!ModelState.IsValid)
        {
            await LoadDepartmentsAsync(
                employee.DepartmentId,
                existingEmployee.DepartmentId);

            await LoadPositionsAsync(
                employee.PositionId,
                existingEmployee.PositionId);

            return View(employee);
        }

        // Update employee information
        existingEmployee.EmployeeCode = employee.EmployeeCode;
        existingEmployee.FullName = employee.FullName;
        existingEmployee.Email = employee.Email;
        existingEmployee.PhoneNumber = employee.PhoneNumber;
        existingEmployee.DateOfBirth = employee.DateOfBirth;
        existingEmployee.HireDate = employee.HireDate;
        existingEmployee.Salary = employee.Salary;
        existingEmployee.DepartmentId = employee.DepartmentId;
        existingEmployee.PositionId = employee.PositionId;
        existingEmployee.IsActive = employee.IsActive;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Employee updated successfully.";

        return RedirectToAction(nameof(Index));
    }

    // GET: Employees/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
            return NotFound();

        var employee = await _context.Employees
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null)
            return NotFound();

        return View(employee);
    }

    // POST: Employees/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var employee = await _context.Employees.FindAsync(id);

        if (employee == null)
            return NotFound();

        employee.IsDeleted = true;
        employee.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Employee moved to trash.";

        return RedirectToAction(nameof(Index));
    }

    // GET: Employees/Trash
    public async Task<IActionResult> Trash()
    {
        var deletedEmployees = await _context.Employees
            .IgnoreQueryFilters()
            .Include(e => e.Department)
            .Where(e => e.IsDeleted)
            .OrderByDescending(e => e.DeletedAt)
            .ToListAsync();

        return View(deletedEmployees);
    }

    // POST: Employees/Restore/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id)
    {
        var employee = await _context.Employees
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id == id && e.IsDeleted);

        if (employee == null)
            return NotFound();

        var duplicateCode = await _context.Employees
            .AnyAsync(e => e.EmployeeCode == employee.EmployeeCode);

        if (duplicateCode)
        {
            TempData["ErrorMessage"] =
                "Cannot restore employee because the employee code is already in use.";

            return RedirectToAction(nameof(Trash));
        }

        employee.IsDeleted = false;
        employee.DeletedAt = null;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Employee restored successfully.";

        return RedirectToAction(nameof(Trash));
    }

    // Normalize text fields to avoid null/whitespace issues.
    private static void NormalizeEmployeeText(Employee employee)
    {
        employee.EmployeeCode = (employee.EmployeeCode ?? string.Empty).Trim();
        employee.FullName = (employee.FullName ?? string.Empty).Trim();

        employee.Email = string.IsNullOrWhiteSpace(employee.Email)
            ? null
            : employee.Email.Trim();

        employee.PhoneNumber = string.IsNullOrWhiteSpace(employee.PhoneNumber)
            ? null
            : employee.PhoneNumber.Trim();
    }

    // Load department dropdown
    private async Task LoadDepartmentsAsync(
        int? selectedDepartmentId = null,
        int? includeDepartmentId = null)
    {
        var query = _context.Departments
            .Where(d => d.IsActive);

        if (includeDepartmentId.HasValue)
        {
            query = _context.Departments
                .Where(d => d.IsActive ||
                            d.Id == includeDepartmentId.Value);
        }

        var departments = await query
            .OrderBy(d => d.Name)
            .ToListAsync();

        ViewBag.Departments = new SelectList(
            departments,
            "Id",
            "Name",
            selectedDepartmentId);
    }

    private async Task LoadPositionsAsync(
        int? selectedPositionId = null,
        int? currentPositionId = null)
    {
        var positions = await _context.Positions
            .Where(p => p.IsActive || p.Id == currentPositionId)
            .OrderBy(p => p.Name)
            .ToListAsync();

        ViewBag.Positions = new SelectList(
            positions,
            "Id",
            "Name",
            selectedPositionId);
    }
}
