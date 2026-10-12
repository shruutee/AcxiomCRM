using System.Text.Json;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers;

[Authorize]
public class CustomersController : CrmControllerBase
{
    public CustomersController(CrmDataStore store) : base(store) { }

    public IActionResult Index(string? search, string? status, string? ownerId)
    {
        var rows = Scoped(Store.Customers);
        if (!string.IsNullOrWhiteSpace(search))
        {
            rows = rows.Where(x => Contains(x.CustomerName, search) || Contains(x.Email, search) || Contains(x.Phone, search) || Contains(x.CompanyName, search));
        }
        if (!string.IsNullOrWhiteSpace(status)) rows = rows.Where(x => x.Status == status);
        if (!string.IsNullOrWhiteSpace(ownerId)) rows = rows.Where(x => x.AssignedTo == ownerId);
        ViewBag.Search = search;
        ViewBag.Status = status;
        ViewBag.OwnerId = ownerId;
        AddFilterOwners(ownerId);
        return View(rows.OrderByDescending(x => x.CreatedDate).ToList());
    }

    public IActionResult Details(int id) => CustomerView(id);

    public IActionResult Create()
    {
        AddOwners();
        return View(new Customer { CustomerCode = $"CUST-{Store.NextCustomerId() + 1000}", AssignedTo = CurrentUser?.Id ?? "" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Create(Customer model)
    {
        ValidateUnique(model);
        if (!ModelState.IsValid)
        {
            AddOwners();
            return View(model);
        }
        model.CustomerId = Store.NextCustomerId();
        model.CustomerCode = string.IsNullOrWhiteSpace(model.CustomerCode) ? $"CUST-{model.CustomerId + 1000}" : model.CustomerCode;
        model.CreatedBy = CurrentUser?.Id ?? "";
        Store.Customers.Add(model);
        Audit("Create", "Customer", model.CustomerId.ToString(), newValue: JsonSerializer.Serialize(model));
        Store.Save();
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Edit(int id)
    {
        var model = Store.Customers.FirstOrDefault(x => x.CustomerId == id);
        if (model is null || !CanAccess(model)) return NotFound();
        AddOwners();
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Edit(int id, Customer model)
    {
        var existing = Store.Customers.FirstOrDefault(x => x.CustomerId == id);
        if (existing is null || !CanAccess(existing)) return NotFound();
        ValidateUnique(model, id);
        if (!ModelState.IsValid)
        {
            AddOwners();
            return View(model);
        }
        var oldValue = JsonSerializer.Serialize(existing);
        existing.CustomerCode = model.CustomerCode;
        existing.CustomerName = model.CustomerName;
        existing.Email = model.Email;
        existing.Phone = model.Phone;
        existing.CompanyName = model.CompanyName;
        existing.Address = model.Address;
        existing.Notes = model.Notes;
        existing.City = model.City;
        existing.State = model.State;
        existing.Status = model.Status;
        existing.AssignedTo = model.AssignedTo;
        Audit("Update", "Customer", id.ToString(), oldValue, JsonSerializer.Serialize(existing));
        Store.Save();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Delete(int id)
    {
        var existing = Store.Customers.FirstOrDefault(x => x.CustomerId == id);
        if (existing is null || !CanAccess(existing)) return NotFound();
        existing.Status = "Inactive";
        Audit("Delete", "Customer", id.ToString(), newValue: "Deactivated");
        Store.Save();
        return RedirectToAction(nameof(Index));
    }

    private IActionResult CustomerView(int id)
    {
        var model = Store.Customers.FirstOrDefault(x => x.CustomerId == id);
        return model is null || !CanAccess(model) ? NotFound() : View(model);
    }

    private void ValidateUnique(Customer model, int? id = null)
    {
        if (Store.Customers.Any(x => x.CustomerId != id && x.Email.Equals(model.Email, StringComparison.OrdinalIgnoreCase)))
            ModelState.AddModelError(nameof(Customer.Email), "Email already exists.");
        if (Store.Customers.Any(x => x.CustomerId != id && x.Phone == model.Phone))
            ModelState.AddModelError(nameof(Customer.Phone), "Phone already exists.");
        if (Store.Customers.Any(x => x.CustomerId != id && x.CustomerName.Equals(model.CustomerName, StringComparison.OrdinalIgnoreCase) && x.CompanyName.Equals(model.CompanyName ?? "", StringComparison.OrdinalIgnoreCase)))
            ModelState.AddModelError(nameof(Customer.CustomerName), "Duplicate customer creation is not allowed.");
    }

    private static bool Contains(string value, string term) => value.Contains(term, StringComparison.OrdinalIgnoreCase);
}
