using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;

namespace Sisonke.Web.Services;

public class SisonkeCurrentUserService(
    UserManager<ApplicationUser> userManager,
    ILogger<SisonkeCurrentUserService> logger)
{
    public const string EntraExternalIdProvider = "MicrosoftEntraExternalId";

    public async Task<ApplicationUser?> ResolveOrCreateEntraUserAsync(ClaimsPrincipal principal)
    {
        var objectId = FindFirstValue(principal,
            "oid",
            "http://schemas.microsoft.com/identity/claims/objectidentifier");
        var tenantId = FindFirstValue(principal,
            "tid",
            "http://schemas.microsoft.com/identity/claims/tenantid");
        var email = NormalizeEmail(FindFirstValue(principal,
            ClaimTypes.Email,
            "email",
            "emails",
            "preferred_username",
            "upn"));
        var fullName = FindFirstValue(principal, ClaimTypes.Name, "name");

        if (string.IsNullOrWhiteSpace(objectId))
        {
            logger.LogWarning("Cannot resolve Entra user because the oid claim is missing.");
            return null;
        }

        var user = await userManager.Users
            .FirstOrDefaultAsync(candidate =>
                candidate.ExternalAuthProvider == EntraExternalIdProvider &&
                candidate.ExternalObjectId == objectId);

        if (user is null && !string.IsNullOrWhiteSpace(email))
        {
            user = await userManager.FindByEmailAsync(email);
        }

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email ?? BuildFallbackUserName(objectId),
                Email = email,
                EmailConfirmed = !string.IsNullOrWhiteSpace(email),
                FullName = fullName,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = EntraExternalIdProvider
            };

            ApplyExternalMapping(user, tenantId, objectId, email);

            var createResult = await userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                logger.LogWarning(
                    "Could not create local user for Entra oid {ObjectId}: {Errors}",
                    objectId,
                    string.Join(" | ", createResult.Errors.Select(error => error.Description)));
                return null;
            }

            return user;
        }

        var changed = ApplyExternalMapping(user, tenantId, objectId, email);

        if (!string.IsNullOrWhiteSpace(email))
        {
            if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
            {
                user.Email = email;
                user.NormalizedEmail = userManager.NormalizeEmail(email);
                changed = true;
            }

            if (!string.Equals(user.UserName, email, StringComparison.OrdinalIgnoreCase))
            {
                user.UserName = email;
                user.NormalizedUserName = userManager.NormalizeName(email);
                changed = true;
            }

            if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                changed = true;
            }
        }

        if (!string.IsNullOrWhiteSpace(fullName) && string.IsNullOrWhiteSpace(user.FullName))
        {
            user.FullName = fullName;
            changed = true;
        }

        if (changed)
        {
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedBy = EntraExternalIdProvider;
            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                logger.LogWarning(
                    "Could not update external mapping for local user {UserId}: {Errors}",
                    user.Id,
                    string.Join(" | ", updateResult.Errors.Select(error => error.Description)));
            }
        }

        return user;
    }

    private static bool ApplyExternalMapping(ApplicationUser user, string? tenantId, string objectId, string? email)
    {
        var changed = false;

        changed |= SetIfChanged(user.ExternalAuthProvider, EntraExternalIdProvider, value => user.ExternalAuthProvider = value);
        changed |= SetIfChanged(user.ExternalTenantId, tenantId, value => user.ExternalTenantId = value);
        changed |= SetIfChanged(user.ExternalObjectId, objectId, value => user.ExternalObjectId = value);
        changed |= SetIfChanged(user.ExternalEmail, email, value => user.ExternalEmail = value);

        return changed;
    }

    private static bool SetIfChanged(string? currentValue, string? newValue, Action<string?> apply)
    {
        if (string.Equals(currentValue, newValue, StringComparison.Ordinal))
        {
            return false;
        }

        apply(newValue);
        return true;
    }

    private static string? NormalizeEmail(string? email)
    {
        var trimmed = email?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static string BuildFallbackUserName(string objectId)
    {
        var safeObjectId = objectId
            .Trim()
            .Replace(" ", string.Empty)
            .Replace(":", "-", StringComparison.Ordinal);

        return $"entra-{safeObjectId}@external.sisonke.local";
    }

    private static string? FindFirstValue(ClaimsPrincipal principal, params string[] claimTypes)
    {
        foreach (var claimType in claimTypes)
        {
            var value = principal.FindFirstValue(claimType);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }
}
