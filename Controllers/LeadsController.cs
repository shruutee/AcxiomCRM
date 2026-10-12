using System.Text.Json;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers;

[Authorize]
public class LeadsController : CrmControllerBase
{
    public LeadsController(CrmDataStore store) : base(store) { }

    public IActionResult Index(string? search, string? status, string? ownerId)
    {
        var rows = Scoped(Store.Leads);
        if (!string.IsNullOrWhiteSpace(search)) rows = rows.Where(x => Contains(x.LeadName, search) || Contains(x.CompanyName, search) || Contains(x.Email, search));
        if (!string.IsNullOrWhiteSpace(status)) rows = rows.Where(x => x.Status == status);
        if (!string.IsNullOrWhiteSpace(ownerId)) rows = rows.Where(x => x.AssignedTo == ownerId);
        ViewBag.Search = search;
        ViewBag.Status = status;
        ViewBag.OwnerId = ownerId;
        AddFilterOwners(ownerId);
        return View(rows.OrderByDescending(x => x.CreatedDate).ToList());
    }

    public IActionResult Details(int id) => LeadView(id);

    public IActionResult Create()
    {
        AddOwners();
        return View(new Lead { LeadCode = $"LEAD-{Store.NextLeadId() + 2000}", AssignedTo = CurrentUser?.Id ?? "" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Create(Lead model)
    {
        if (!ModelState.IsValid) { AddOwners(); return View(model); }
        model.LeadId = Store.NextLeadId();
        model.LeadCode = string.IsNullOrWhiteSpace(model.LeadCode) ? $"LEAD-{model.LeadId + 2000}" : model.LeadCode;
        Store.Leads.Add(model);
        Audit("Create", "Lead", model.LeadId.ToString(), newValue: JsonSerializer.Serialize(model));
        Store.Save();
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Edit(int id)
    {
        var model = Store.Leads.FirstOrDefault(x => x.LeadId == id);
        if (model is null || !CanAccess(model)) return NotFound();
        AddOwners();
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Edit(int id, Lead model)
    {
        var existing = Store.Leads.FirstOrDefault(x => x.LeadId == id);
        if (existing is null || !CanAccess(existing)) return NotFound();
        if (!ModelState.IsValid) { AddOwners(); return View(model); }
        var oldValue = JsonSerializer.Serialize(existing);
        existing.LeadCode = model.LeadCode;
        existing.LeadName = model.LeadName;
        existing.Email = model.Email;
        existing.Phone = model.Phone;
        existing.CompanyName = model.CompanyName;
        existing.Source = model.Source;
        existing.Status = model.Status;
        existing.Priority = model.Priority;
        existing.Notes = model.Notes;
        existing.ExpectedValue = model.ExpectedValue;
        existing.AssignedTo = model.AssignedTo;
        Audit("Update", "Lead", id.ToString(), oldValue, JsonSerializer.Serialize(existing));
        Store.Save();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Convert(int id)
    {
        var lead = Store.Leads.FirstOrDefault(x => x.LeadId == id);
        if (lead is null || !CanAccess(lead)) return NotFound();
        if (lead.Status != "Qualified")
        {
            ModelState.AddModelError("", "Only qualified leads can be converted.");
            AddOwners();
            return View("Details", lead);
        }
        lead.Status = "Converted";
        var customer = new Customer { CustomerId = Store.NextCustomerId(), CustomerCode = $"CUST-{Store.NextCustomerId() + 1000}", CustomerName = lead.LeadName, Email = lead.Email, Phone = lead.Phone, CompanyName = lead.CompanyName, Status = "Active", AssignedTo = lead.AssignedTo, CreatedBy = CurrentUser?.Id ?? "" };
        Store.Customers.Add(customer);
        Store.Opportunities.Add(new Opportunity { OpportunityId = Store.NextOpportunityId(), OpportunityName = $"{lead.CompanyName} Opportunity", CustomerId = customer.CustomerId, LeadId = lead.LeadId, Amount = Math.Max(lead.ExpectedValue, 1), Probability = 50, Stage = "Qualification", Status = "Open", Source = "Lead Conversion", ExpectedCloseDate = DateTime.Today.AddDays(30), AssignedTo = lead.AssignedTo });
        Audit("Convert", "Lead", id.ToString(), newValue: $"Converted to customer {customer.CustomerId}");
        Store.Save();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Delete(int id)
    {
        var existing = Store.Leads.FirstOrDefault(x => x.LeadId == id);
        if (existing is null || !CanAccess(existing)) return NotFound();
        existing.Status = "Lost";
        Audit("Delete", "Lead", id.ToString(), newValue: "Marked Lost");
        Store.Save();
        return RedirectToAction(nameof(Index));
    }

    private IActionResult LeadView(int id)
    {
        var model = Store.Leads.FirstOrDefault(x => x.LeadId == id);
        return model is null || !CanAccess(model) ? NotFound() : View(model);
    }

    private static bool Contains(string value, string term) => value.Contains(term, StringComparison.OrdinalIgnoreCase);
}
