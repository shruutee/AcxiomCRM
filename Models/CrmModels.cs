using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace AcxiomCRM.Models;

public static class CrmRoles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string SalesExecutive = "SalesExecutive";

    public static readonly string[] All = [Admin, Manager, SalesExecutive];
}

public class ApplicationUser : IdentityUser
{
    [Required, StringLength(120)]
    public string FullName { get; set; } = "";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}

public abstract class OwnedRecord
{
    public string AssignedTo { get; set; } = "";
}

public class Customer : OwnedRecord
{
    public int CustomerId { get; set; }

    [Required, StringLength(20)]
    public string CustomerCode { get; set; } = "";

    [Required, StringLength(120)]
    public string CustomerName { get; set; } = "";

    [Required, EmailAddress, StringLength(160)]
    public string Email { get; set; } = "";

    [Required, RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Enter a valid 10-digit Indian mobile number.")]
    public string Phone { get; set; } = "";

    [StringLength(140)]
    public string CompanyName { get; set; } = "";

    [StringLength(250)]
    public string Address { get; set; } = "";

    [StringLength(600)]
    public string Notes { get; set; } = "";

    [StringLength(80)]
    public string City { get; set; } = "";

    [StringLength(80)]
    public string State { get; set; } = "";

    [Required]
    public string Status { get; set; } = "Active";

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public string CreatedBy { get; set; } = "";
}

public class Lead : OwnedRecord, IValidatableObject
{
    public int LeadId { get; set; }

    [Required, StringLength(20)]
    public string LeadCode { get; set; } = "";

    [Required, StringLength(120)]
    public string LeadName { get; set; } = "";

    [Required, EmailAddress, StringLength(160)]
    public string Email { get; set; } = "";

    [Required, RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Enter a valid 10-digit Indian mobile number.")]
    public string Phone { get; set; } = "";

    [StringLength(140)]
    public string CompanyName { get; set; } = "";

    [Required]
    public string Source { get; set; } = "Website";

    [Required]
    public string Status { get; set; } = "New";

    [Required]
    public string Priority { get; set; } = "Medium";

    [StringLength(600)]
    public string Notes { get; set; } = "";

    [Range(0, 100000000)]
    public decimal ExpectedValue { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!CrmLookups.LeadStatuses.Contains(Status))
        {
            yield return new ValidationResult("Lead status is not valid.", [nameof(Status)]);
        }

        if (!CrmLookups.LeadPriorities.Contains(Priority))
        {
            yield return new ValidationResult("Lead priority is not valid.", [nameof(Priority)]);
        }
    }
}

public class Opportunity : OwnedRecord, IValidatableObject
{
    public int OpportunityId { get; set; }

    [Required, StringLength(140)]
    public string OpportunityName { get; set; } = "";

    [Range(1, int.MaxValue, ErrorMessage = "Customer is required.")]
    public int CustomerId { get; set; }

    public int? LeadId { get; set; }

    [Range(0, 100000000)]
    public decimal Amount { get; set; }

    [Required]
    public string Stage { get; set; } = "Qualification";

    [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
    public int Probability { get; set; }

    [DataType(DataType.Date)]
    public DateTime ExpectedCloseDate { get; set; } = DateTime.Today.AddDays(14);

    [Required]
    public string Status { get; set; } = "Open";

    [Required]
    public string Source { get; set; } = "Direct";

    [StringLength(600)]
    public string Notes { get; set; } = "";

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public decimal WeightedPipeline => Amount * Probability / 100m;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!CrmLookups.OpportunityStages.Contains(Stage))
        {
            yield return new ValidationResult("Opportunity stage is not valid.", [nameof(Stage)]);
        }

        if (Status == "Open" && Amount <= 0)
        {
            yield return new ValidationResult("Opportunity Amount must be greater than 0.", [nameof(Amount)]);
        }

        if (Status == "Open" && ExpectedCloseDate.Date < DateTime.Today)
        {
            yield return new ValidationResult("Expected Close Date cannot be in the past.", [nameof(ExpectedCloseDate)]);
        }

        if (Stage is "Won" or "Lost" && Status == "Open")
        {
            yield return new ValidationResult("Won or lost opportunities must have a matching status.", [nameof(Status)]);
        }
    }
}

public class FollowUp : OwnedRecord, IValidatableObject
{
    public int FollowUpId { get; set; }

    public int? CustomerId { get; set; }

    public int? LeadId { get; set; }

    public int? OpportunityId { get; set; }

    [DataType(DataType.Date)]
    public DateTime FollowUpDate { get; set; } = DateTime.Today.AddDays(1);

    [Required]
    public string FollowUpType { get; set; } = "Call";

    [Required, StringLength(180)]
    public string Subject { get; set; } = "";

    [StringLength(500)]
    public string Remarks { get; set; } = "";

    [Required]
    public string Status { get; set; } = "Planned";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Status is "Planned" or "Missed" && FollowUpDate.Date < DateTime.Today)
        {
            yield return new ValidationResult("Follow-up date cannot be earlier than today.", [nameof(FollowUpDate)]);
        }

        if (CustomerId is null && LeadId is null && OpportunityId is null)
        {
            yield return new ValidationResult("Choose a related customer, lead, or opportunity.", [nameof(CustomerId)]);
        }
    }
}

public class Activity : OwnedRecord
{
    public int ActivityId { get; set; }

    [Required]
    public string ActivityType { get; set; } = "Call";

    [Required, StringLength(180)]
    public string Subject { get; set; } = "";

    [StringLength(800)]
    public string Description { get; set; } = "";

    [DataType(DataType.Date)]
    public DateTime ActivityDate { get; set; } = DateTime.Today;

    public int? CustomerId { get; set; }

    public int? LeadId { get; set; }

    [Required]
    public string Status { get; set; } = "Open";
}

public class AuditLog
{
    public int AuditLogId { get; set; }
    public string UserId { get; set; } = "";
    public string UserName { get; set; } = "";
    public string Action { get; set; } = "";
    public string EntityName { get; set; } = "";
    public string RecordId { get; set; } = "";
    public string OldValue { get; set; } = "";
    public string NewValue { get; set; } = "";
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string IpAddress { get; set; } = "";
    public string Result { get; set; } = "Success";
}

public static class CrmLookups
{
    public static readonly string[] CustomerStatuses = ["Active", "Inactive", "Prospect"];
    public static readonly string[] LeadStatuses = ["New", "Contacted", "Qualified", "Unqualified", "Converted", "Lost"];
    public static readonly string[] LeadSources = ["Website", "Referral", "Campaign", "Phone", "Email", "Event"];
    public static readonly string[] LeadPriorities = ["Low", "Medium", "High", "Critical"];
    public static readonly string[] OpportunityStages = ["Qualification", "Proposal", "Negotiation", "Won", "Lost"];
    public static readonly string[] OpportunityStatuses = ["Open", "Won", "Lost"];
    public static readonly string[] OpportunitySources = ["Direct", "Lead Conversion", "Referral", "Campaign", "Renewal", "Upsell"];
    public static readonly string[] FollowUpTypes = ["Call", "Meeting", "Email", "Task"];
    public static readonly string[] FollowUpStatuses = ["Planned", "Completed", "Missed", "Cancelled"];
    public static readonly string[] ActivityTypes = ["Call", "Meeting", "Email", "Task"];
    public static readonly string[] ActivityStatuses = ["Open", "Completed", "Cancelled"];
}
