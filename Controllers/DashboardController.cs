
using EmployeeManagement.Web.Data;
using EmployeeManagement.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace EmployeeManagement.Web.Controllers;

[Authorize(Roles = "Admin,HR")]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;

    public DashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var totalEmployees = await _context.Employees.CountAsync();

        var activeEmployees = await _context.Employees
            .CountAsync(e => e.IsActive);

        var inactiveEmployees = await _context.Employees
            .CountAsync(e => !e.IsActive);

        var totalDepartments = await _context.Departments.CountAsync();

        // Employee statistics by department
        var departmentStats = await (
            from employee in _context.Employees
            join department in _context.Departments
                on employee.DepartmentId equals department.Id
            where employee.IsActive
            group employee by department.Name into groupResult
            select new DepartmentEmployeeStat
            {
                DepartmentName = groupResult.Key,
                EmployeeCount = groupResult.Count()
            }
        ).OrderByDescending(x => x.EmployeeCount)
         .ToListAsync();

        // Latest employees
        var recentEmployees = await _context.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .OrderByDescending(e => e.CreatedAt)
            .Take(5)
            .ToListAsync();

        var viewModel = new DashboardViewModel
        {
            TotalEmployees = totalEmployees,
            ActiveEmployees = activeEmployees,
            InactiveEmployees = inactiveEmployees,
            TotalDepartments = totalDepartments,
            EmployeesByDepartment = departmentStats,
            RecentEmployees = recentEmployees
        };

        return View(viewModel);
    }
}
