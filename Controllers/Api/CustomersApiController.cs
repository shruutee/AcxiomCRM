using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/customers")]
[Authorize]
public class CustomersApiController : ControllerBase
{
    private readonly CrmDataStore _store;

    public CustomersApiController(CrmDataStore store)
    {
        _store = store;
    }

    [HttpGet]
    public ActionResult<IEnumerable<CustomerDto>> Get() => Ok(Scoped(_store.Customers).Select(ToDto));

    [HttpGet("{id:int}")]
    public ActionResult<CustomerDto> Get(int id)
    {
        var customer = _store.Customers.FirstOrDefault(x => x.CustomerId == id);
        return customer is null || !CanAccess(customer) ? NotFound() : Ok(ToDto(customer));
    }

    [HttpPost]
    public ActionResult<CustomerDto> Create(CustomerDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        if (_store.Customers.Any(x => x.Email.Equals(dto.Email, StringComparison.OrdinalIgnoreCase) || x.Phone == dto.Phone)) return Conflict(new { error = "Duplicate customer email or phone." });
        var user = _store.Users.FirstOrDefault(x => x.UserName == User.Identity?.Name);
        var customer = new Customer
        {
            CustomerId = _store.NextCustomerId(),
            CustomerCode = $"CUST-{_store.NextCustomerId() + 1000}",
            CustomerName = dto.CustomerName,
            Email = dto.Email,
            Phone = dto.Phone,
            CompanyName = dto.CompanyName,
            City = dto.City,
            State = dto.State,
            Status = string.IsNullOrWhiteSpace(dto.Status) ? "Active" : dto.Status,
            Notes = dto.Notes,
            AssignedTo = user?.Id ?? "",
            CreatedBy = user?.Id ?? ""
        };
        _store.Customers.Add(customer);
        _store.AddAudit(user?.Id ?? "", user?.Email ?? "", "Create", "Customer", customer.CustomerId.ToString(), "", "API create", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "");
        _store.Save();
        return CreatedAtAction(nameof(Get), new { id = customer.CustomerId }, ToDto(customer));
    }

    [HttpPut("{id:int}")]
    public IActionResult Update(int id, CustomerDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var customer = _store.Customers.FirstOrDefault(x => x.CustomerId == id);
        if (customer is null || !CanAccess(customer)) return NotFound();
        if (_store.Customers.Any(x => x.CustomerId != id && (x.Email.Equals(dto.Email, StringComparison.OrdinalIgnoreCase) || x.Phone == dto.Phone))) return Conflict(new { error = "Duplicate customer email or phone." });
        customer.CustomerName = dto.CustomerName;
        customer.Email = dto.Email;
        customer.Phone = dto.Phone;
        customer.CompanyName = dto.CompanyName;
        customer.City = dto.City;
        customer.State = dto.State;
        customer.Status = dto.Status;
        customer.Notes = dto.Notes;
        _store.Save();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        var customer = _store.Customers.FirstOrDefault(x => x.CustomerId == id);
        if (customer is null || !CanAccess(customer)) return NotFound();
        customer.Status = "Inactive";
        _store.Save();
        return NoContent();
    }

    private ApplicationUser? CurrentUser => _store.Users.FirstOrDefault(x => x.UserName == User.Identity?.Name);

    private IEnumerable<T> Scoped<T>(IEnumerable<T> records) where T : OwnedRecord => _store.Scope(records, CurrentUser);

    private bool CanAccess(OwnedRecord record)
    {
        var user = CurrentUser;
        return user is not null && (_store.IsInRole(user.Id, CrmRoles.Admin) || _store.IsInRole(user.Id, CrmRoles.Manager) || record.AssignedTo == user.Id);
    }

    private static CustomerDto ToDto(Customer x) => new(x.CustomerId, x.CustomerName, x.Email, x.Phone, x.CompanyName, x.City, x.State, x.Status, x.Notes);
}

public record CustomerDto(
    int CustomerId,
    [property: Required, StringLength(120)] string CustomerName,
    [property: Required, EmailAddress, StringLength(160)] string Email,
    [property: Required, RegularExpression(@"^[6-9]\d{9}$")] string Phone,
    [property: StringLength(140)] string CompanyName,
    [property: StringLength(80)] string City,
    [property: StringLength(80)] string State,
    string Status,
    [property: StringLength(600)] string Notes);
