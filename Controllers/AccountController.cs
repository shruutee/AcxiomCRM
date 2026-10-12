using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly CrmDataStore _store;

    public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, CrmDataStore store)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _store = store;
    }

    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is not null && !user.IsActive)
        {
            ModelState.AddModelError("", "This account is inactive.");
            Audit(user, "Failed Login", "Inactive account");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            Audit(user, "Login", "User successfully logged in.");
            return LocalRedirect(returnUrl ?? Url.Action("Index", "Dashboard")!);
        }

        Audit(user, "Failed Login", result.IsLockedOut ? "Lockout event" : "Invalid credentials", "Failed");
        ModelState.AddModelError("", result.IsLockedOut ? "Account locked after repeated failed attempts. Try later." : "Invalid email or password.");
        return View(model);
    }

    [AllowAnonymous]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            EmailConfirmed = true,
            LockoutEnabled = true
        };
        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, CrmRoles.SalesExecutive);
            Audit(user, "Register", "New SalesExecutive account registered.");
            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction("Index", "Dashboard");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError("", error.Description);
        }
        return View(model);
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var user = await _userManager.GetUserAsync(User);
        await _signInManager.SignOutAsync();
        Audit(user, "Logout", "User logged out.");
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult Denied() => View();

    private void Audit(ApplicationUser? user, string action, string details, string result = "Success")
    {
        _store.AddAudit(user?.Id ?? "", user?.Email ?? "", action, "Authentication", user?.Id ?? "", "", details, HttpContext.Connection.RemoteIpAddress?.ToString() ?? "", result);
    }
}
