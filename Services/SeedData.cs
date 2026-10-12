using AcxiomCRM.Models;
using Microsoft.AspNetCore.Identity;

namespace AcxiomCRM.Services;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var store = scope.ServiceProvider.GetRequiredService<CrmDataStore>();

        foreach (var role in CrmRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role) { Id = role, NormalizedName = role.ToUpperInvariant() });
            }
        }

        var admin = await EnsureUser(userManager, "admin@acxiomcrm.local", "Admin User", CrmRoles.Admin);
        var manager = await EnsureUser(userManager, "manager@acxiomcrm.local", "Manager User", CrmRoles.Manager);
        var sales = await EnsureUser(userManager, "sales@acxiomcrm.local", "Sales Executive", CrmRoles.SalesExecutive);

        if (!store.Customers.Any())
        {
            store.Customers.Add(new Customer { CustomerId = store.NextCustomerId(), CustomerCode = "CUST-1001", CustomerName = "Apex Retail", Email = "buyer@apex.example", Phone = "9876543210", CompanyName = "Apex Retail Pvt Ltd", City = "Bengaluru", State = "Karnataka", Status = "Active", AssignedTo = sales.Id, CreatedBy = admin.Id });
            store.Customers.Add(new Customer { CustomerId = store.NextCustomerId(), CustomerCode = "CUST-1002", CustomerName = "Northstar Logistics", Email = "ops@northstar.example", Phone = "9123456780", CompanyName = "Northstar Logistics", City = "Pune", State = "Maharashtra", Status = "Prospect", AssignedTo = sales.Id, CreatedBy = manager.Id });
            store.Leads.Add(new Lead { LeadId = store.NextLeadId(), LeadCode = "LEAD-2001", LeadName = "GreenGrid Systems", Email = "hello@greengrid.example", Phone = "9988776655", CompanyName = "GreenGrid Systems", Source = "Website", Status = "New", ExpectedValue = 120000, AssignedTo = sales.Id });
            store.Leads.Add(new Lead { LeadId = store.NextLeadId(), LeadCode = "LEAD-2002", LeadName = "BluePeak Foods", Email = "contact@bluepeak.example", Phone = "9090909090", CompanyName = "BluePeak Foods", Source = "Referral", Status = "Qualified", ExpectedValue = 350000, AssignedTo = sales.Id });
            store.Opportunities.Add(new Opportunity { OpportunityId = store.NextOpportunityId(), OpportunityName = "Apex Expansion", CustomerId = 1, Amount = 450000, Probability = 70, Stage = "Proposal", Status = "Open", ExpectedCloseDate = DateTime.Today.AddDays(30), AssignedTo = sales.Id });
            store.Opportunities.Add(new Opportunity { OpportunityId = store.NextOpportunityId(), OpportunityName = "Northstar Renewal", CustomerId = 2, Amount = 280000, Probability = 90, Stage = "Negotiation", Status = "Open", ExpectedCloseDate = DateTime.Today.AddDays(20), AssignedTo = sales.Id });
            store.FollowUps.Add(new FollowUp { FollowUpId = store.NextFollowUpId(), CustomerId = 1, FollowUpDate = DateTime.Today.AddDays(1), FollowUpType = "Call", Subject = "Proposal review", Status = "Planned", AssignedTo = sales.Id });
            store.Activities.Add(new Activity { ActivityId = store.NextActivityId(), CustomerId = 1, ActivityDate = DateTime.Today, ActivityType = "Email", Subject = "Sent proposal deck", Status = "Completed", AssignedTo = sales.Id });
            store.AddAudit(admin.Id, admin.Email ?? "", "Seed", "System", "initial", "", "Seed data created", "");
        }
    }

    private static async Task<ApplicationUser> EnsureUser(UserManager<ApplicationUser> userManager, string email, string name, string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = name,
                EmailConfirmed = true,
                LockoutEnabled = true,
                IsActive = true
            };
            var result = await userManager.CreateAsync(user, "Password@123");
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
            }
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            await userManager.AddToRoleAsync(user, role);
        }

        return user;
    }
}
