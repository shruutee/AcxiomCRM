using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/opportunities")]
[Authorize]
public class OpportunitiesApiController : ControllerBase
{
    private readonly CrmDataStore _store;
    public OpportunitiesApiController(CrmDataStore store) => _store = store;

    [HttpGet]
    public IActionResult Get() => Ok(Scoped(_store.Opportunities).Select(ToDto));

    [HttpPost]
    public IActionResult Create(OpportunityDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        if (!Scoped(_store.Customers).Any(x => x.CustomerId == dto.CustomerId)) return NotFound(new { error = "Customer not found or not authorized." });
        if (dto.LeadId.HasValue && !Scoped(_store.Leads).Any(x => x.LeadId == dto.LeadId.Value)) return NotFound(new { error = "Lead not found or not authorized." });
        if (!CrmLookups.OpportunityStages.Contains(dto.Stage)) return BadRequest(new { error = "Opportunity stage is not valid." });
        if (!CrmLookups.OpportunityStatuses.Contains(dto.Status)) return BadRequest(new { error = "Opportunity status is not valid." });
        if (dto.Status == "Open" && dto.ExpectedCloseDate.Date < DateTime.Today) return BadRequest(new { error = "Expected Close Date cannot be in the past." });
        var user = CurrentUser;
        var opportunity = new Opportunity
        {
            OpportunityId = _store.NextOpportunityId(),
            OpportunityName = dto.OpportunityName,
            CustomerId = dto.CustomerId,
            LeadId = dto.LeadId,
            Amount = dto.Amount,
            Stage = dto.Stage,
            Probability = dto.Probability,
            ExpectedCloseDate = dto.ExpectedCloseDate,
            Status = dto.Status,
            Source = string.IsNullOrWhiteSpace(dto.Source) ? "Direct" : dto.Source,
            Notes = dto.Notes,
            AssignedTo = user?.Id ?? ""
        };
        AlignOutcome(opportunity);
        _store.Opportunities.Add(opportunity);
        _store.AddAudit(user?.Id ?? "", user?.Email ?? "", "Create", "Opportunity", opportunity.OpportunityId.ToString(), "", "API create", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "");
        _store.Save();
        return Created($"/api/opportunities/{opportunity.OpportunityId}", ToDto(opportunity));
    }

    private ApplicationUser? CurrentUser => _store.Users.FirstOrDefault(x => x.UserName == User.Identity?.Name);

    private IEnumerable<T> Scoped<T>(IEnumerable<T> records) where T : OwnedRecord => _store.Scope(records, CurrentUser);

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

    private static OpportunityDto ToDto(Opportunity x) => new(x.OpportunityId, x.OpportunityName, x.CustomerId, x.LeadId, x.Amount, x.Stage, x.Probability, x.ExpectedCloseDate, x.Status, x.Source, x.Notes, x.WeightedPipeline);
}

public record OpportunityDto(
    int OpportunityId,
    [property: Required, StringLength(140)] string OpportunityName,
    [property: Range(1, int.MaxValue)] int CustomerId,
    int? LeadId,
    [property: Range(0.01, 100000000)] decimal Amount,
    [property: Required] string Stage,
    [property: Range(0, 100)] int Probability,
    DateTime ExpectedCloseDate,
    [property: Required] string Status,
    string Source,
    [property: StringLength(600)] string Notes,
    decimal WeightedPipeline);
