using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace AcxiomCRM.Controllers;

[Authorize]
public class ReportsController : CrmControllerBase
{
    public ReportsController(CrmDataStore store) : base(store) { }

    public IActionResult Index(string? ownerId, string? stage, DateTime? from, DateTime? to, string? export)
    {
        var customers = Scoped(Store.Customers).ToList();
        var leads = Scoped(Store.Leads).ToList();
        var opportunities = Scoped(Store.Opportunities).ToList();
        var followUps = Scoped(Store.FollowUps).ToList();

        if (!string.IsNullOrWhiteSpace(ownerId))
        {
            customers = customers.Where(x => x.AssignedTo == ownerId).ToList();
            leads = leads.Where(x => x.AssignedTo == ownerId).ToList();
            opportunities = opportunities.Where(x => x.AssignedTo == ownerId).ToList();
            followUps = followUps.Where(x => x.AssignedTo == ownerId).ToList();
        }

        if (!string.IsNullOrWhiteSpace(stage))
        {
            opportunities = opportunities.Where(x => x.Stage == stage).ToList();
        }

        if (from.HasValue)
        {
            opportunities = opportunities.Where(x => x.ExpectedCloseDate.Date >= from.Value.Date).ToList();
            followUps = followUps.Where(x => x.FollowUpDate.Date >= from.Value.Date).ToList();
        }

        if (to.HasValue)
        {
            opportunities = opportunities.Where(x => x.ExpectedCloseDate.Date <= to.Value.Date).ToList();
            followUps = followUps.Where(x => x.FollowUpDate.Date <= to.Value.Date).ToList();
        }

        if (export == "csv")
        {
            return ExportPipeline(opportunities);
        }

        ViewBag.Customers = customers.GroupBy(x => x.Status).ToDictionary(x => x.Key, x => x.Count());
        ViewBag.Leads = leads.GroupBy(x => x.Status).ToDictionary(x => x.Key, x => x.Count());
        ViewBag.FollowUps = followUps.GroupBy(x => x.Status).ToDictionary(x => x.Key, x => x.Count());
        ViewBag.PipelineByStage = opportunities.GroupBy(x => x.Stage).ToDictionary(x => x.Key, x => x.Sum(o => o.WeightedPipeline));
        ViewBag.Conversion = new { Converted = leads.Count(x => x.Status == "Converted"), Lost = leads.Count(x => x.Status == "Lost"), Open = leads.Count(x => x.Status is not ("Converted" or "Lost")) };
        ViewBag.UserActivity = Store.AuditLogs.GroupBy(x => x.UserName).ToDictionary(x => x.Key, x => x.Count());
        ViewBag.OwnerId = ownerId;
        ViewBag.Stage = stage;
        ViewBag.From = from?.ToString("yyyy-MM-dd");
        ViewBag.To = to?.ToString("yyyy-MM-dd");
        AddFilterOwners(ownerId);
        return View(opportunities);
    }

    private IActionResult ExportPipeline(IEnumerable<Opportunity> opportunities)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Opportunity,Stage,Status,Amount,Probability,WeightedPipeline,ExpectedCloseDate");
        foreach (var item in opportunities)
        {
            csv.AppendLine($"{Escape(item.OpportunityName)},{Escape(item.Stage)},{Escape(item.Status)},{item.Amount},{item.Probability},{item.WeightedPipeline},{item.ExpectedCloseDate:yyyy-MM-dd}");
        }

        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "pipeline-report.csv");
    }

    private static string Escape(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
}
