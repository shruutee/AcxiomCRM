using System.Text.Json;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Identity;

namespace AcxiomCRM.Services;

public class CrmDataStore
{
    private readonly IWebHostEnvironment _environment;
    private readonly object _sync = new();
    private CrmDatabase _db = new();

    public CrmDataStore(IWebHostEnvironment environment)
    {
        _environment = environment;
        Load();
    }

    public string FilePath => Path.Combine(_environment.ContentRootPath, "App_Data", "crm-store.json");

    public IList<ApplicationUser> Users => _db.Users;
    public IList<IdentityRole> Roles => _db.Roles;
    public IList<UserRoleAssignment> UserRoles => _db.UserRoles;
    public IList<Customer> Customers => _db.Customers;
    public IList<Lead> Leads => _db.Leads;
    public IList<Opportunity> Opportunities => _db.Opportunities;
    public IList<FollowUp> FollowUps => _db.FollowUps;
    public IList<Activity> Activities => _db.Activities;
    public IList<AuditLog> AuditLogs => _db.AuditLogs;

    public void Save()
    {
        lock (_sync)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            var json = JsonSerializer.Serialize(_db, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }
    }

    public int NextCustomerId() => NextId(Customers.Select(x => x.CustomerId));
    public int NextLeadId() => NextId(Leads.Select(x => x.LeadId));
    public int NextOpportunityId() => NextId(Opportunities.Select(x => x.OpportunityId));
    public int NextFollowUpId() => NextId(FollowUps.Select(x => x.FollowUpId));
    public int NextActivityId() => NextId(Activities.Select(x => x.ActivityId));
    public int NextAuditId() => NextId(AuditLogs.Select(x => x.AuditLogId));

    public void AddAudit(string userId, string userName, string action, string entityName, string recordId, string oldValue, string newValue, string ipAddress, string result = "Success")
    {
        AuditLogs.Add(new AuditLog
        {
            AuditLogId = NextAuditId(),
            UserId = userId,
            UserName = userName,
            Action = action,
            EntityName = entityName,
            RecordId = recordId,
            OldValue = oldValue,
            NewValue = newValue,
            IpAddress = ipAddress,
            Result = result,
            CreatedDate = DateTime.UtcNow
        });
        Save();
    }

    public IEnumerable<T> Scope<T>(IEnumerable<T> records, ApplicationUser? user, bool teamScope = false) where T : OwnedRecord
    {
        if (user is null)
        {
            return Enumerable.Empty<T>();
        }

        if (IsInRole(user.Id, CrmRoles.Admin))
        {
            return records;
        }

        if (IsInRole(user.Id, CrmRoles.Manager) || teamScope)
        {
            return records;
        }

        return records.Where(x => x.AssignedTo == user.Id);
    }

    public bool IsInRole(string userId, string roleName) =>
        UserRoles.Any(x => x.UserId == userId && string.Equals(x.RoleName, roleName, StringComparison.OrdinalIgnoreCase));

    public string? PrimaryRole(string userId) => UserRoles.FirstOrDefault(x => x.UserId == userId)?.RoleName;

    private void Load()
    {
        if (!File.Exists(FilePath))
        {
            _db.Roles = CrmRoles.All.Select(x => new IdentityRole(x) { Id = x }).ToList();
            Save();
            return;
        }

        var json = File.ReadAllText(FilePath);
        _db = JsonSerializer.Deserialize<CrmDatabase>(json) ?? new CrmDatabase();
    }

    private static int NextId(IEnumerable<int> ids) => ids.DefaultIfEmpty(0).Max() + 1;
}

public class CrmDatabase
{
    public List<ApplicationUser> Users { get; set; } = [];
    public List<IdentityRole> Roles { get; set; } = [];
    public List<UserRoleAssignment> UserRoles { get; set; } = [];
    public List<Customer> Customers { get; set; } = [];
    public List<Lead> Leads { get; set; } = [];
    public List<Opportunity> Opportunities { get; set; } = [];
    public List<FollowUp> FollowUps { get; set; } = [];
    public List<Activity> Activities { get; set; } = [];
    public List<AuditLog> AuditLogs { get; set; } = [];
}

public class UserRoleAssignment
{
    public string UserId { get; set; } = "";
    public string RoleName { get; set; } = "";
}
