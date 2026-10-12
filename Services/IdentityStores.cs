using AcxiomCRM.Models;
using Microsoft.AspNetCore.Identity;

namespace AcxiomCRM.Services;

public class ApplicationUserStore :
    IUserPasswordStore<ApplicationUser>,
    IUserEmailStore<ApplicationUser>,
    IUserRoleStore<ApplicationUser>,
    IUserLockoutStore<ApplicationUser>,
    IUserSecurityStampStore<ApplicationUser>
{
    private readonly CrmDataStore _store;

    public ApplicationUserStore(CrmDataStore store)
    {
        _store = store;
    }

    public Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        user.Id = string.IsNullOrWhiteSpace(user.Id) ? Guid.NewGuid().ToString("N") : user.Id;
        user.SecurityStamp = string.IsNullOrWhiteSpace(user.SecurityStamp) ? Guid.NewGuid().ToString("N") : user.SecurityStamp;
        user.ConcurrencyStamp = string.IsNullOrWhiteSpace(user.ConcurrencyStamp) ? Guid.NewGuid().ToString("N") : user.ConcurrencyStamp;
        user.NormalizedEmail = user.Email?.ToUpperInvariant();
        user.NormalizedUserName = user.UserName?.ToUpperInvariant();
        _store.Users.Add(user);
        _store.Save();
        return Task.FromResult(IdentityResult.Success);
    }

    public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        _store.Users.Remove(user);
        foreach (var assignment in _store.UserRoles.Where(x => x.UserId == user.Id).ToList())
        {
            _store.UserRoles.Remove(assignment);
        }
        _store.Save();
        return Task.FromResult(IdentityResult.Success);
    }

    public void Dispose()
    {
    }

    public Task<ApplicationUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        Task.FromResult(_store.Users.FirstOrDefault(x => x.NormalizedEmail == normalizedEmail));

    public Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken) =>
        Task.FromResult(_store.Users.FirstOrDefault(x => x.Id == userId));

    public Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) =>
        Task.FromResult(_store.Users.FirstOrDefault(x => x.NormalizedUserName == normalizedUserName));

    public Task<int> GetAccessFailedCountAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.AccessFailedCount);

    public Task<string?> GetEmailAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.Email);

    public Task<bool> GetEmailConfirmedAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.EmailConfirmed);

    public Task<bool> GetLockoutEnabledAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.LockoutEnabled);

    public Task<DateTimeOffset?> GetLockoutEndDateAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.LockoutEnd);

    public Task<string?> GetNormalizedEmailAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.NormalizedEmail);

    public Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.NormalizedUserName);

    public Task<string?> GetPasswordHashAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.PasswordHash);

    public Task<IList<string>> GetRolesAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        Task.FromResult((IList<string>)_store.UserRoles.Where(x => x.UserId == user.Id).Select(x => x.RoleName).ToList());

    public Task<string?> GetSecurityStampAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        Task.FromResult<string?>(user.SecurityStamp ?? "");

    public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.Id);

    public Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.UserName);

    public Task<IList<ApplicationUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
    {
        var ids = _store.UserRoles
            .Where(x => string.Equals(x.RoleName, roleName, StringComparison.OrdinalIgnoreCase))
            .Select(x => x.UserId)
            .ToHashSet();
        return Task.FromResult((IList<ApplicationUser>)_store.Users.Where(x => ids.Contains(x.Id)).ToList());
    }

    public Task<bool> HasPasswordAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        Task.FromResult(!string.IsNullOrWhiteSpace(user.PasswordHash));

    public Task<int> IncrementAccessFailedCountAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        user.AccessFailedCount++;
        _store.Save();
        return Task.FromResult(user.AccessFailedCount);
    }

    public Task<bool> IsInRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken) =>
        Task.FromResult(_store.IsInRole(user.Id, roleName));

    public Task RemoveFromRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken)
    {
        foreach (var assignment in _store.UserRoles.Where(x => x.UserId == user.Id && x.RoleName == roleName).ToList())
        {
            _store.UserRoles.Remove(assignment);
        }
        _store.Save();
        return Task.CompletedTask;
    }

    public Task ResetAccessFailedCountAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        user.AccessFailedCount = 0;
        _store.Save();
        return Task.CompletedTask;
    }

    public Task SetEmailAsync(ApplicationUser user, string? email, CancellationToken cancellationToken)
    {
        user.Email = email;
        return Task.CompletedTask;
    }

    public Task SetEmailConfirmedAsync(ApplicationUser user, bool confirmed, CancellationToken cancellationToken)
    {
        user.EmailConfirmed = confirmed;
        _store.Save();
        return Task.CompletedTask;
    }

    public Task SetLockoutEnabledAsync(ApplicationUser user, bool enabled, CancellationToken cancellationToken)
    {
        user.LockoutEnabled = enabled;
        _store.Save();
        return Task.CompletedTask;
    }

    public Task SetLockoutEndDateAsync(ApplicationUser user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken)
    {
        user.LockoutEnd = lockoutEnd;
        _store.Save();
        return Task.CompletedTask;
    }

    public Task SetNormalizedEmailAsync(ApplicationUser user, string? normalizedEmail, CancellationToken cancellationToken)
    {
        user.NormalizedEmail = normalizedEmail;
        return Task.CompletedTask;
    }

    public Task SetNormalizedUserNameAsync(ApplicationUser user, string? normalizedName, CancellationToken cancellationToken)
    {
        user.NormalizedUserName = normalizedName;
        return Task.CompletedTask;
    }

    public Task SetPasswordHashAsync(ApplicationUser user, string? passwordHash, CancellationToken cancellationToken)
    {
        user.PasswordHash = passwordHash;
        return Task.CompletedTask;
    }

    public Task SetSecurityStampAsync(ApplicationUser user, string stamp, CancellationToken cancellationToken)
    {
        user.SecurityStamp = stamp;
        return Task.CompletedTask;
    }

    public Task SetUserNameAsync(ApplicationUser user, string? userName, CancellationToken cancellationToken)
    {
        user.UserName = userName;
        return Task.CompletedTask;
    }

    public Task AddToRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken)
    {
        foreach (var assignment in _store.UserRoles.Where(x => x.UserId == user.Id).ToList())
        {
            _store.UserRoles.Remove(assignment);
        }
        _store.UserRoles.Add(new UserRoleAssignment { UserId = user.Id, RoleName = roleName });
        _store.Save();
        return Task.CompletedTask;
    }

    public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        user.ConcurrencyStamp = Guid.NewGuid().ToString("N");
        _store.Save();
        return Task.FromResult(IdentityResult.Success);
    }
}

public class ApplicationRoleStore : IRoleStore<IdentityRole>
{
    private readonly CrmDataStore _store;

    public ApplicationRoleStore(CrmDataStore store)
    {
        _store = store;
    }

    public Task<IdentityResult> CreateAsync(IdentityRole role, CancellationToken cancellationToken)
    {
        role.Id = string.IsNullOrWhiteSpace(role.Id) ? role.Name ?? Guid.NewGuid().ToString("N") : role.Id;
        role.NormalizedName = role.Name?.ToUpperInvariant();
        _store.Roles.Add(role);
        _store.Save();
        return Task.FromResult(IdentityResult.Success);
    }

    public Task<IdentityResult> DeleteAsync(IdentityRole role, CancellationToken cancellationToken)
    {
        _store.Roles.Remove(role);
        _store.Save();
        return Task.FromResult(IdentityResult.Success);
    }

    public void Dispose()
    {
    }

    public Task<IdentityRole?> FindByIdAsync(string roleId, CancellationToken cancellationToken) =>
        Task.FromResult(_store.Roles.FirstOrDefault(x => x.Id == roleId));

    public Task<IdentityRole?> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken) =>
        Task.FromResult(_store.Roles.FirstOrDefault(x => x.NormalizedName == normalizedRoleName || x.Name == normalizedRoleName));

    public Task<string?> GetNormalizedRoleNameAsync(IdentityRole role, CancellationToken cancellationToken) =>
        Task.FromResult(role.NormalizedName);

    public Task<string> GetRoleIdAsync(IdentityRole role, CancellationToken cancellationToken) =>
        Task.FromResult(role.Id);

    public Task<string?> GetRoleNameAsync(IdentityRole role, CancellationToken cancellationToken) =>
        Task.FromResult(role.Name);

    public Task SetNormalizedRoleNameAsync(IdentityRole role, string? normalizedName, CancellationToken cancellationToken)
    {
        role.NormalizedName = normalizedName;
        return Task.CompletedTask;
    }

    public Task SetRoleNameAsync(IdentityRole role, string? roleName, CancellationToken cancellationToken)
    {
        role.Name = roleName;
        return Task.CompletedTask;
    }

    public Task<IdentityResult> UpdateAsync(IdentityRole role, CancellationToken cancellationToken)
    {
        role.ConcurrencyStamp = Guid.NewGuid().ToString("N");
        _store.Save();
        return Task.FromResult(IdentityResult.Success);
    }
}
