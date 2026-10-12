using System.Text.Json;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers;

[Authorize]
public class OpportunitiesController : CrmControllerBase
{
    public OpportunitiesController(CrmDataStore store) : base(store) { }

    public IActionResult Index(string? search, string? stage, string? status, int? customerId, string? ownerId)
    {
        var rows = Scoped(Store.Opportunities);
        if (!string.IsNullOrWhiteSpace(search)) rows = rows.Where(x => x.OpportunityName.Contains(search, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(stage)) rows = rows.Where(x => x.Stage == stage);
        if (!string.IsNullOrWhiteSpace(status)) rows = rows.Where(x => x.Status == status);
        if (customerId.HasValue) rows = rows.Where(x => x.CustomerId == customerId.Value);
        if (!string.IsNullOrWhiteSpace(ownerId)) rows = rows.Where(x => x.AssignedTo == ownerId);
        ViewBag.Search = search;
        ViewBag.Stage = stage;
        ViewBag.Status = status;
        ViewBag.CustomerId = customerId;
        ViewBag.OwnerId = ownerId;
        AddCustomers();
        AddFilterOwners(ownerId);
        return View(rows.OrderByDescending(x => x.CreatedDate).ToList());
    }

    public IActionResult Details(int id)
    {
        var model = Store.Opportunities.FirstOrDefault(x => x.OpportunityId == id);
        return model is null || !CanAccess(model) ? NotFound() : View(model);
    }

    public IActionResult Create()
    {
        AddOwners(); AddCustomers(); AddLeads();
        return View(new Opportunity { AssignedTo = CurrentUser?.Id ?? "", ExpectedCloseDate = DateTime.Today.AddDays(30) });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Create(Opportunity model)
    {
        ValidateReferences(model);
        if (!ModelState.IsValid) { AddOwners(); AddCustomers(); AddLeads(); return View(model); }
        model.OpportunityId = Store.NextOpportunityId();
        AlignOutcome(model);
        Store.Opportunities.Add(model);
        Audit("Create", "Opportunity", model.OpportunityId.ToString(), newValue: JsonSerializer.Serialize(model));
        Store.Save();
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Edit(int id)
    {
        var model = Store.Opportunities.FirstOrDefault(x => x.OpportunityId == id);
        if (model is null || !CanAccess(model)) return NotFound();
        AddOwners(); AddCustomers(); AddLeads();
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Edit(int id, Opportunity model)
    {
        var existing = Store.Opportunities.FirstOrDefault(x => x.OpportunityId == id);
        if (existing is null || !CanAccess(existing)) return NotFound();
        ValidateReferences(model);
        if (!ModelState.IsValid) { AddOwners(); AddCustomers(); AddLeads(); return View(model); }
        var oldValue = JsonSerializer.Serialize(existing);
        AlignOutcome(model);
        existing.OpportunityName = model.OpportunityName;
        existing.CustomerId = model.CustomerId;
        existing.LeadId = model.LeadId;
        existing.Amount = model.Amount;
        existing.Stage = model.Stage;
        existing.Probability = model.Probability;
        existing.ExpectedCloseDate = model.ExpectedCloseDate;
        existing.Status = model.Status;
        existing.Source = model.Source;
        existing.Notes = model.Notes;
        existing.AssignedTo = model.AssignedTo;
        Audit("Update", "Opportunity", id.ToString(), oldValue, JsonSerializer.Serialize(existing));
        Store.Save();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Delete(int id)
    {
        var existing = Store.Opportunities.FirstOrDefault(x => x.OpportunityId == id);
        if (existing is null || !CanAccess(existing)) return NotFound();
        existing.Status = "Lost";
        existing.Stage = "Lost";
        Audit("Delete", "Opportunity", id.ToString(), newValue: "Marked Lost");
        Store.Save();
        return RedirectToAction(nameof(Index));
    }

    private void ValidateReferences(Opportunity model)
    {
        if (!Scoped(Store.Customers).Any(x => x.CustomerId == model.CustomerId))
        {
            ModelState.AddModelError(nameof(Opportunity.CustomerId), "Choose an authorized customer.");
        }

        if (model.LeadId.HasValue && !Scoped(Store.Leads).Any(x => x.LeadId == model.LeadId.Value))
        {
            ModelState.AddModelError(nameof(Opportunity.LeadId), "Choose an authorized lead.");
        }
    }

    private static void AlignOutcome(Opportunity model)
    {
        if (model.Stage == "Won")
        {
            model.Status = "Won";
            model.Probability = 100;
        }
        else if (model.Stage == "Lost")
        {
            model.Status = "Lost";
            model.Probability = 0;
        }
    }
}
