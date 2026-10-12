using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers;

[Authorize(Roles = CrmRoles.Admin)]
public class AuditController : CrmControllerBase
{
    public AuditController(CrmDataStore store) : base(store) { }

    public IActionResult Index(string? userName, string? entityName, string? action)
    {
        var rows = Store.AuditLogs.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(userName)) rows = rows.Where(x => x.UserName.Contains(userName, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(entityName)) rows = rows.Where(x => x.EntityName.Contains(entityName, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(action)) rows = rows.Where(x => x.Action.Contains(action, StringComparison.OrdinalIgnoreCase));
        ViewBag.UserName = userName;
        ViewBag.EntityName = entityName;
        ViewBag.Action = action;
        return View(rows.OrderByDescending(x => x.CreatedDate).Take(200).ToList());
    }
}
