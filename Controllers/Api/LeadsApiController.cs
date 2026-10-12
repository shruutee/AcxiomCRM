using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/leads")]
[Authorize]
public class LeadsApiController : ControllerBase
{
    private readonly CrmDataStore _store;
    public LeadsApiController(CrmDataStore store) => _store = store;

    [HttpGet]
    public IActionResult Get() => Ok(Scoped(_store.Leads).Select(ToDto));

    [HttpPost]
    public IActionResult Create(LeadDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        if (!CrmLookups.LeadStatuses.Contains(dto.Status)) return BadRequest(new { error = "Lead status is not valid." });
        if (!CrmLookups.LeadPriorities.Contains(dto.Priority)) return BadRequest(new { error = "Lead priority is not valid." });
        var user = CurrentUser;
        var lead = new Lead
        {
            LeadId = _store.NextLeadId(),
            LeadCode = $"LEAD-{_store.NextLeadId() + 2000}",
            LeadName = dto.LeadName,
            Email = dto.Email,
            Phone = dto.Phone,
            CompanyName = dto.CompanyName,
            Source = string.IsNullOrWhiteSpace(dto.Source) ? "Website" : dto.Source,
            Status = dto.Status,
            Priority = dto.Priority,
            ExpectedValue = dto.ExpectedValue,
            Notes = dto.Notes,
            AssignedTo = user?.Id ?? ""
        };
        lead.LeadId = _store.NextLeadId();
        lead.LeadCode = $"LEAD-{lead.LeadId + 2000}";
        _store.Leads.Add(lead);
        _store.AddAudit(user?.Id ?? "", user?.Email ?? "", "Create", "Lead", lead.LeadId.ToString(), "", "API create", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "");
        _store.Save();
        return Created($"/api/leads/{lead.LeadId}", ToDto(lead));
    }

    private ApplicationUser? CurrentUser => _store.Users.FirstOrDefault(x => x.UserName == User.Identity?.Name);

    private IEnumerable<T> Scoped<T>(IEnumerable<T> records) where T : OwnedRecord => _store.Scope(records, CurrentUser);

    private static LeadDto ToDto(Lead x) => new(x.LeadId, x.LeadName, x.Email, x.Phone, x.CompanyName, x.Source, x.Status, x.Priority, x.ExpectedValue, x.Notes);
}

public record LeadDto(
    int LeadId,
    [property: Required, StringLength(120)] string LeadName,
    [property: Required, EmailAddress, StringLength(160)] string Email,
    [property: Required, RegularExpression(@"^[6-9]\d{9}$")] string Phone,
    [property: StringLength(140)] string CompanyName,
    string Source,
    [property: Required] string Status,
    [property: Required] string Priority,
    [property: Range(0, 100000000)] decimal ExpectedValue,
    [property: StringLength(600)] string Notes);
