using EmployeeManagement.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Web.Controllers;

[Authorize(Roles = "Admin")]
public class UserManagementController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;

    private readonly RoleManager<IdentityRole> _roleManager;

    public UserManagementController(
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    // GET: UserManagement
    public async Task<IActionResult> Index(string? searchString)
    {
        var users = await _userManager.Users
            .OrderBy(u => u.Email)
            .ToListAsync();

        var model = new List<UserListItemViewModel>();

        foreach (var user in users)
        {
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var keyword = searchString.Trim();

                if (!(user.Email?.Contains(
                          keyword,
                          StringComparison.OrdinalIgnoreCase) ?? false))
                {
                    continue;
                }
            }

            var roles = await _userManager.GetRolesAsync(user);

            var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);

            model.Add(new UserListItemViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Role = string.Join(", ", roles),
                IsLocked = lockoutEnd.HasValue &&
                           lockoutEnd.Value > DateTimeOffset.UtcNow
            });
        }

        ViewBag.SearchString = searchString;

        return View(model);
    }

    // GET: UserManagement/Create
    public IActionResult Create()
    {
        return View(new CreateUserViewModel());
    }

    // POST: UserManagement/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel model)
    {
        model.Email = model.Email.Trim();

        if (model.Role != "Admin" && model.Role != "HR")
        {
            ModelState.AddModelError(
                nameof(model.Role),
                "Invalid role.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var existingUser = await _userManager.FindByEmailAsync(model.Email);

        if (existingUser != null)
        {
            ModelState.AddModelError(
                nameof(model.Email),
                "This email is already registered.");

            return View(model);
        }

        var user = new IdentityUser
        {
            UserName = model.Email,
            Email = model.Email,
            EmailConfirmed = false
        };

        var createResult = await _userManager.CreateAsync(
            user,
            model.Password);

        if (!createResult.Succeeded)
        {
            foreach (var error in createResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        var roleResult = await _userManager.AddToRoleAsync(
            user,
            model.Role);

        if (!roleResult.Succeeded)
        {
            // Avoid leaving an account without its intended role.
            await _userManager.DeleteAsync(user);

            foreach (var error in roleResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        TempData["SuccessMessage"] =
            $"Account {model.Email} was created successfully.";

        return RedirectToAction(nameof(Index));
    }

    // GET: UserManagement/EditRole/id
    public async Task<IActionResult> EditRole(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        var user = await _userManager.FindByIdAsync(id);

        if (user == null)
        {
            return NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);

        return View(new EditUserRoleViewModel
        {
            Id = user.Id,
            Email = user.Email,
            Role = roles.FirstOrDefault() ?? "HR"
        });
    }

    // POST: UserManagement/EditRole
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditRole(EditUserRoleViewModel model)
    {
        if (model.Role != "Admin" && model.Role != "HR")
        {
            ModelState.AddModelError(
                nameof(model.Role),
                "Invalid role.");
        }

        var user = await _userManager.FindByIdAsync(model.Id);

        if (user == null)
        {
            return NotFound();
        }

        model.Email = user.Email;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var currentUserId = _userManager.GetUserId(User);

        // Prevent changing your own role.
        if (user.Id == currentUserId)
        {
            ModelState.AddModelError(
                string.Empty,
                "You cannot change your own role.");

            return View(model);
        }

        var oldRoles = await _userManager.GetRolesAsync(user);

        // Prevent removing the last Admin.
        if (oldRoles.Contains("Admin") && model.Role != "Admin")
        {
            var admins = await _userManager.GetUsersInRoleAsync("Admin");

            if (admins.Count <= 1)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Cannot remove the last Admin account.");

                return View(model);
            }
        }

        // Add the new role first.
        if (!oldRoles.Contains(model.Role))
        {
            var addResult = await _userManager.AddToRoleAsync(
                user,
                model.Role);

            if (!addResult.Succeeded)
            {
                foreach (var error in addResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(model);
            }
        }

        // Remove old roles after the new role was added.
        var rolesToRemove = oldRoles
            .Where(role => role != model.Role)
            .ToList();

        if (rolesToRemove.Count > 0)
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(
                user,
                rolesToRemove);

            if (!removeResult.Succeeded)
            {
                foreach (var error in removeResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(model);
            }
        }

        TempData["SuccessMessage"] =
            $"Role of {user.Email} was updated successfully.";

        return RedirectToAction(nameof(Index));
    }

    // POST: UserManagement/Lock/id
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Lock(string id)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user == null)
        {
            return NotFound();
        }

        var currentUserId = _userManager.GetUserId(User);

        if (user.Id == currentUserId)
        {
            TempData["ErrorMessage"] =
                "You cannot lock your own account.";

            return RedirectToAction(nameof(Index));
        }

        var roles = await _userManager.GetRolesAsync(user);

        if (roles.Contains("Admin"))
        {
            var admins = await _userManager.GetUsersInRoleAsync("Admin");

            if (admins.Count <= 1)
            {
                TempData["ErrorMessage"] =
                    "Cannot lock the last Admin account.";

                return RedirectToAction(nameof(Index));
            }
        }

        await _userManager.SetLockoutEnabledAsync(user, true);

        var result = await _userManager.SetLockoutEndDateAsync(
            user,
            DateTimeOffset.MaxValue);

        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = "Could not lock this account.";
        }
        else
        {
            TempData["SuccessMessage"] =
                $"Account {user.Email} was locked.";
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: UserManagement/Unlock/id
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlock(string id)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user == null)
        {
            return NotFound();
        }

        var result = await _userManager.SetLockoutEndDateAsync(
            user,
            null);

        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = "Could not unlock this account.";
        }
        else
        {
            TempData["SuccessMessage"] =
                $"Account {user.Email} was unlocked.";
        }

        return RedirectToAction(nameof(Index));
    }
}