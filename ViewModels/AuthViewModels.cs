using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.ViewModels;

public class LoginViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = "";

    public bool RememberMe { get; set; }
}

public class RegisterViewModel
{
    [Required, StringLength(120)]
    public string FullName { get; set; } = "";

    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required, StringLength(100, MinimumLength = 8), DataType(DataType.Password)]
    public string Password { get; set; } = "";
}

public class UserAdminViewModel
{
    public string Id { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public bool IsActive { get; set; }
    public bool LockedOut { get; set; }
}

public class CreateUserViewModel
{
    [Required, StringLength(120)]
    public string FullName { get; set; } = "";

    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required, StringLength(100, MinimumLength = 8), DataType(DataType.Password)]
    public string Password { get; set; } = "Password@123";

    [Required]
    public string Role { get; set; } = "SalesExecutive";

    public bool IsActive { get; set; } = true;
}

public class DashboardViewModel
{
    public string DateFilter { get; set; } = "month";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalCustomers { get; set; }
    public int TotalLeads { get; set; }
    public int OpenLeads { get; set; }
    public int TotalOpportunities { get; set; }
    public int OpenOpportunities { get; set; }
    public int WonOpportunities { get; set; }
    public int LostOpportunities { get; set; }
    public int PendingFollowUps { get; set; }
    public int OverdueFollowUps { get; set; }
    public decimal TotalPipelineValue { get; set; }
    public Dictionary<string, int> LeadStatus { get; set; } = [];
    public Dictionary<string, int> OpportunityPipeline { get; set; } = [];
    public Dictionary<string, decimal> MonthlySales { get; set; } = [];
}
