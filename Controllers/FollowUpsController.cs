using System.Text.Json;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers;

[Authorize]
public class FollowUpsController : CrmControllerBase
{
    public FollowUpsController(CrmDataStore store) : base(store) { }

    public IActionResult Index(DateTime? date, string? status, string? ownerId, int? customerId, int? leadId)
    {
        var rows = Scoped(Store.FollowUps);
        if (date.HasValue) rows = rows.Where(x => x.FollowUpDate.Date == date.Value.Date);
        if (!string.IsNullOrWhiteSpace(status)) rows = rows.Where(x => x.Status == status);
        if (!string.IsNullOrWhiteSpace(ownerId)) rows = rows.Where(x => x.AssignedTo == ownerId);
        if (customerId.HasValue) rows = rows.Where(x => x.CustomerId == customerId.Value);
        if (leadId.HasValue) rows = rows.Where(x => x.LeadId == leadId.Value);
        ViewBag.Date = date?.ToString("yyyy-MM-dd");
        ViewBag.Status = status;
        ViewBag.OwnerId = ownerId;
        ViewBag.CustomerId = customerId;
        ViewBag.LeadId = leadId;
        AddFilterOwners(ownerId);
        AddCustomers();
        AddLeads();
        return View(rows.OrderBy(x => x.FollowUpDate).ToList());
    }

    public IActionResult Create()
    {
        AddOwners(); AddCustomers(); AddLeads(); AddOpportunities();
        return View(new FollowUp { AssignedTo = CurrentUser?.Id ?? "", FollowUpDate = DateTime.Today.AddDays(1) });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Create(FollowUp model)
    {
        ValidateReferences(model);
        if (!ModelState.IsValid) { AddOwners(); AddCustomers(); AddLeads(); AddOpportunities(); return View(model); }
        model.FollowUpId = Store.NextFollowUpId();
        Store.FollowUps.Add(model);
        Audit("Create", "FollowUp", model.FollowUpId.ToString(), newValue: JsonSerializer.Serialize(model));
        Store.Save();
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Edit(int id)
    {
        var model = Store.FollowUps.FirstOrDefault(x => x.FollowUpId == id);
        if (model is null || !CanAccess(model)) return NotFound();
        AddOwners(); AddCustomers(); AddLeads(); AddOpportunities();
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Edit(int id, FollowUp model)
    {
        var existing = Store.FollowUps.FirstOrDefault(x => x.FollowUpId == id);
        if (existing is null || !CanAccess(existing)) return NotFound();
        ValidateReferences(model);
        if (!ModelState.IsValid) { AddOwners(); AddCustomers(); AddLeads(); AddOpportunities(); return View(model); }
        var oldValue = JsonSerializer.Serialize(existing);
        existing.CustomerId = model.CustomerId;
        existing.LeadId = model.LeadId;
        existing.OpportunityId = model.OpportunityId;
        existing.FollowUpDate = model.FollowUpDate;
        existing.FollowUpType = model.FollowUpType;
        existing.Subject = model.Subject;
        existing.Remarks = model.Remarks;
        existing.Status = model.Status;
        existing.AssignedTo = model.AssignedTo;
        Audit("Update", "FollowUp", id.ToString(), oldValue, JsonSerializer.Serialize(existing));
        Store.Save();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Complete(int id)
    {
        var existing = Store.FollowUps.FirstOrDefault(x => x.FollowUpId == id);
        if (existing is null || !CanAccess(existing)) return NotFound();
        existing.Status = "Completed";
        Audit("Complete", "FollowUp", id.ToString());
        Store.Save();
        return RedirectToAction(nameof(Index));
    }

    private void ValidateReferences(FollowUp model)
    {
        if (model.CustomerId.HasValue && !Scoped(Store.Customers).Any(x => x.CustomerId == model.CustomerId.Value))
        {
            ModelState.AddModelError(nameof(FollowUp.CustomerId), "Choose an authorized customer.");
        }

        if (model.LeadId.HasValue && !Scoped(Store.Leads).Any(x => x.LeadId == model.LeadId.Value))
        {
            ModelState.AddModelError(nameof(FollowUp.LeadId), "Choose an authorized lead.");
        }

        if (model.OpportunityId.HasValue && !Scoped(Store.Opportunities).Any(x => x.OpportunityId == model.OpportunityId.Value))
        {
            ModelState.AddModelError(nameof(FollowUp.OpportunityId), "Choose an authorized opportunity.");
        }
    }
}
