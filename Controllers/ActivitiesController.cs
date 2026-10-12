using System.Text.Json;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers;

[Authorize]
public class ActivitiesController : CrmControllerBase
{
    public ActivitiesController(CrmDataStore store) : base(store) { }

    public IActionResult Index(string? type, string? status, DateTime? date, string? ownerId)
    {
        var rows = Scoped(Store.Activities);
        if (!string.IsNullOrWhiteSpace(type)) rows = rows.Where(x => x.ActivityType == type);
        if (!string.IsNullOrWhiteSpace(status)) rows = rows.Where(x => x.Status == status);
        if (date.HasValue) rows = rows.Where(x => x.ActivityDate.Date == date.Value.Date);
        if (!string.IsNullOrWhiteSpace(ownerId)) rows = rows.Where(x => x.AssignedTo == ownerId);
        ViewBag.Type = type;
        ViewBag.Status = status;
        ViewBag.Date = date?.ToString("yyyy-MM-dd");
        ViewBag.OwnerId = ownerId;
        AddFilterOwners(ownerId);
        return View(rows.OrderByDescending(x => x.ActivityDate).ToList());
    }

    public IActionResult Create()
    {
        AddOwners(); AddCustomers(); AddLeads();
        return View(new Activity { AssignedTo = CurrentUser?.Id ?? "" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Create(Activity model)
    {
        if (!ModelState.IsValid) { AddOwners(); AddCustomers(); AddLeads(); return View(model); }
        model.ActivityId = Store.NextActivityId();
        Store.Activities.Add(model);
        Audit("Create", "Activity", model.ActivityId.ToString(), newValue: JsonSerializer.Serialize(model));
        Store.Save();
        return RedirectToAction(nameof(Index));
    }
}
