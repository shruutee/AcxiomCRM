using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers;

[Authorize(Roles = CrmRoles.Admin)]
public class UsersController : CrmControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UsersController(CrmDataStore store, UserManager<ApplicationUser> userManager) : base(store)
    {
        _userManager = userManager;
    }

    public IActionResult Index(string? search)
    {
        var rows = Store.Users.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            rows = rows.Where(x => x.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) || (x.Email ?? "").Contains(search, StringComparison.OrdinalIgnoreCase));
        }
        ViewBag.Search = search;
        return View(rows.Select(x => new UserAdminViewModel
        {
            Id = x.Id,
            FullName = x.FullName,
            Email = x.Email ?? "",
            Role = Store.PrimaryRole(x.Id) ?? "",
            IsActive = x.IsActive,
            LockedOut = x.LockoutEnd > DateTimeOffset.UtcNow
        }).ToList());
    }

    public IActionResult Create() => View(new CreateUserViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = new ApplicationUser { UserName = model.Email, Email = model.Email, FullName = model.FullName, EmailConfirmed = true, LockoutEnabled = true, IsActive = model.IsActive };
        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, model.Role);
            Audit("Create", "User", user.Id, newValue: $"Role={model.Role}; Active={model.IsActive}");
            return RedirectToAction(nameof(Index));
        }
        foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();
        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);
        Audit("Security", "User", id, newValue: $"IsActive={user.IsActive}");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlock(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();
        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);
        Audit("Security", "User", id, newValue: "Account unlocked");
        return RedirectToAction(nameof(Index));
    }
}
