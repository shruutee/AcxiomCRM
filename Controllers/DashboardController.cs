using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers;

[Authorize]
public class DashboardController : CrmControllerBase
{
    public DashboardController(CrmDataStore store) : base(store)
    {
    }

    public IActionResult Index(string filter = "month", DateTime? start = null, DateTime? end = null)
    {
        var today = DateTime.Today;
        var (startDate, endDate, normalizedFilter) = ResolveDateRange(filter, start, end, today);
        var customers = Scoped(Store.Customers).ToList();
        var leads = Scoped(Store.Leads).ToList();
        var opportunities = Scoped(Store.Opportunities).ToList();
        var followUps = Scoped(Store.FollowUps).ToList();
        var periodOpportunities = opportunities
            .Where(x => x.ExpectedCloseDate.Date >= startDate && x.ExpectedCloseDate.Date <= endDate)
            .ToList();

        var model = new DashboardViewModel
        {
            DateFilter = normalizedFilter,
            StartDate = startDate,
            EndDate = endDate,
            TotalCustomers = customers.Count,
            TotalLeads = leads.Count,
            OpenLeads = leads.Count(x => x.Status is not ("Lost" or "Converted")),
            TotalOpportunities = opportunities.Count,
            OpenOpportunities = opportunities.Count(x => x.Status == "Open"),
            WonOpportunities = opportunities.Count(x => x.Stage == "Won" || x.Status == "Won"),
            LostOpportunities = opportunities.Count(x => x.Stage == "Lost" || x.Status == "Lost"),
            TotalPipelineValue = opportunities.Where(x => x.Status == "Open").Sum(x => x.Amount),
            PendingFollowUps = followUps.Count(x => x.Status == "Planned" && x.FollowUpDate.Date >= today),
            OverdueFollowUps = followUps.Count(x => x.Status == "Planned" && x.FollowUpDate.Date < today),
            LeadStatus = CrmLookups.LeadStatuses.ToDictionary(x => x, x => leads.Count(l => l.Status == x)),
            OpportunityPipeline = CrmLookups.OpportunityStages.ToDictionary(x => x, x => periodOpportunities.Count(o => o.Stage == x)),
            MonthlySales = periodOpportunities
                .Where(x => x.Stage == "Won" || x.Status == "Won")
                .GroupBy(x => new DateTime(x.ExpectedCloseDate.Year, x.ExpectedCloseDate.Month, 1).ToString("MMM yyyy"))
                .ToDictionary(x => x.Key, x => x.Sum(o => o.Amount))
        };

        return View(model);
    }

    private static (DateTime Start, DateTime End, string Filter) ResolveDateRange(string filter, DateTime? start, DateTime? end, DateTime today)
    {
        return filter?.ToLowerInvariant() switch
        {
            "today" => (today, today, "today"),
            "week" => (today.AddDays(-(int)today.DayOfWeek), today.AddDays(6 - (int)today.DayOfWeek), "week"),
            "custom" when start.HasValue && end.HasValue => (start.Value.Date, end.Value.Date, "custom"),
            _ => (new DateTime(today.Year, today.Month, 1), new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month)), "month")
        };
    }
}
