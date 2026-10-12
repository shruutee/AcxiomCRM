using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.Controllers;

public abstract class CrmControllerBase : Controller
{
    protected readonly CrmDataStore Store;

    protected CrmControllerBase(CrmDataStore store)
    {
        Store = store;
    }

    protected ApplicationUser? CurrentUser => Store.Users.FirstOrDefault(x => x.UserName == User.Identity?.Name);

    protected bool IsAdmin => CurrentUser is not null && Store.IsInRole(CurrentUser.Id, CrmRoles.Admin);
    protected bool IsManager => CurrentUser is not null && Store.IsInRole(CurrentUser.Id, CrmRoles.Manager);

    protected IEnumerable<T> Scoped<T>(IEnumerable<T> records) where T : OwnedRecord => Store.Scope(records, CurrentUser);

    protected void Audit(string action, string entity, string recordId, string oldValue = "", string newValue = "", string result = "Success")
    {
        var user = CurrentUser;
        Store.AddAudit(
            user?.Id ?? "",
            user?.Email ?? User.Identity?.Name ?? "anonymous",
            action,
            entity,
            recordId,
            oldValue,
            newValue,
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
            result);
    }

    protected void AddOwners()
    {
        ViewBag.Owners = new SelectList(Store.Users.Where(x => x.IsActive), "Id", "FullName");
    }

    protected void AddCustomers()
    {
        ViewBag.Customers = new SelectList(Scoped(Store.Customers), "CustomerId", "CustomerName");
    }

    protected void AddLeads()
    {
        ViewBag.Leads = new SelectList(Scoped(Store.Leads), "LeadId", "LeadName");
    }

    protected void AddOpportunities()
    {
        ViewBag.Opportunities = new SelectList(Scoped(Store.Opportunities), "OpportunityId", "OpportunityName");
    }

    protected void AddFilterOwners(string? selectedOwner = null)
    {
        ViewBag.OwnerOptions = new SelectList(Store.Users.Where(x => x.IsActive), "Id", "FullName", selectedOwner);
    }

    protected bool CanAccess(OwnedRecord record) => IsAdmin || IsManager || record.AssignedTo == CurrentUser?.Id;
}
