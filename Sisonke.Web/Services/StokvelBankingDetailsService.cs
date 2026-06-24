using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Services;

public sealed class StokvelBankingDetailsService(IDbContextFactory<ApplicationDbContext> dbFactory)
{
    public async Task<StokvelBankingDetails?> GetActiveBankingDetailsAsync(Guid stokvelId)
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        return await context.StokvelBankingDetails
            .AsNoTracking()
            .Where(details => details.StokvelId == stokvelId && details.IsActive && details.IsPrimary)
            .OrderByDescending(details => details.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> CanManageBankingDetailsAsync(Guid stokvelId, string currentUserId)
    {
        if (string.IsNullOrWhiteSpace(currentUserId)) return false;
        await using var context = await dbFactory.CreateDbContextAsync();
        return await HasBankingManagementRoleAsync(context, stokvelId, currentUserId);
    }

    public async Task<BankingDetailsSaveResult> SaveBankingDetailsAsync(
        Guid stokvelId, StokvelBankingDetails model, string currentUserId)
    {
        var errors = Validate(model);
        if (errors.Count > 0) return BankingDetailsSaveResult.Failed(errors);

        await using var context = await dbFactory.CreateDbContextAsync();
        if (!await HasBankingManagementRoleAsync(context, stokvelId, currentUserId))
            return BankingDetailsSaveResult.Failed(["You are not authorised to manage banking details."]);

        var stokvelIsActive = await context.Stokvels
            .AsNoTracking()
            .AnyAsync(stokvel => stokvel.Id == stokvelId && stokvel.IsActive && !stokvel.IsDeleted);
        if (!stokvelIsActive)
            return BankingDetailsSaveResult.Failed(["Banking details cannot be changed for an inactive stokvel."]);

        await using var transaction = await context.Database.BeginTransactionAsync();
        var previous = await context.StokvelBankingDetails
            .Where(details => details.StokvelId == stokvelId && details.IsActive && details.IsPrimary)
            .ToListAsync();
        var now = DateTime.UtcNow;
        foreach (var details in previous)
        {
            details.IsActive = false;
            details.IsPrimary = false;
            details.UpdatedAt = now;
            details.UpdatedBy = currentUserId;
        }

        var created = CopyForCreate(stokvelId, model, currentUserId, now);
        context.StokvelBankingDetails.Add(created);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return BankingDetailsSaveResult.Succeeded(created);
    }

    public async Task<BankingDetailsSaveResult> UpdateBankingDetailsAsync(
        Guid id, StokvelBankingDetails model, string currentUserId)
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        var existing = await context.StokvelBankingDetails
            .AsNoTracking()
            .FirstOrDefaultAsync(details => details.Id == id && details.IsActive);
        if (existing is null) return BankingDetailsSaveResult.Failed(["Active banking details were not found."]);
        return await SaveBankingDetailsAsync(existing.StokvelId, model, currentUserId);
    }

    public static string MaskAccountNumber(string? accountNumber)
    {
        if (string.IsNullOrWhiteSpace(accountNumber)) return "Not captured";
        var value = accountNumber.Trim();
        var suffix = value.Length <= 4 ? value : value[^4..];
        return $"**** **** {suffix}";
    }

    private static async Task<bool> HasBankingManagementRoleAsync(
        ApplicationDbContext context, Guid stokvelId, string currentUserId)
    {
        return await context.Members.AnyAsync(member =>
            member.ApplicationUserId == currentUserId &&
            context.Stokvels.Any(stokvel => stokvel.Id == stokvelId && stokvel.TenantId == member.TenantId) &&
            (member.DefaultRole == SisonkeRole.Creator ||
             member.DefaultRole == SisonkeRole.PlatformSuperAdmin ||
             member.DefaultRole == SisonkeRole.PlatformSupportAdmin ||
             member.DefaultRole == SisonkeRole.StokvelAdmin ||
             member.DefaultRole == SisonkeRole.Chairperson ||
             member.DefaultRole == SisonkeRole.Secretary ||
             member.DefaultRole == SisonkeRole.Treasurer));
    }

    private static List<string> Validate(StokvelBankingDetails model)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(model.BankName)) errors.Add("Bank name is required.");
        if (string.IsNullOrWhiteSpace(model.AccountHolderName)) errors.Add("Account holder name is required.");
        if (string.IsNullOrWhiteSpace(model.AccountNumber)) errors.Add("Account number is required.");
        else if (!model.AccountNumber.All(char.IsDigit)) errors.Add("Account number must contain digits only.");
        if (!string.IsNullOrWhiteSpace(model.BranchCode) && !model.BranchCode.All(char.IsDigit))
            errors.Add("Branch code must contain digits only.");
        if (!Enum.IsDefined(model.AccountType)) errors.Add("Account type is required.");
        return errors;
    }

    private static StokvelBankingDetails CopyForCreate(
        Guid stokvelId, StokvelBankingDetails model, string currentUserId, DateTime now) => new()
        {
            Id = Guid.NewGuid(),
            StokvelId = stokvelId,
            BankName = model.BankName.Trim(),
            AccountHolderName = model.AccountHolderName.Trim(),
            AccountNumber = model.AccountNumber.Trim(),
            AccountType = model.AccountType,
            BranchCode = NullIfWhiteSpace(model.BranchCode),
            BranchName = NullIfWhiteSpace(model.BranchName),
            PaymentReferenceFormat = NullIfWhiteSpace(model.PaymentReferenceFormat),
            Notes = NullIfWhiteSpace(model.Notes),
            IsPrimary = true,
            IsActive = true,
            CreatedAt = now,
            CreatedBy = currentUserId
        };

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed record BankingDetailsSaveResult(bool Success, StokvelBankingDetails? Details, List<string> Errors)
{
    public static BankingDetailsSaveResult Succeeded(StokvelBankingDetails details) => new(true, details, []);
    public static BankingDetailsSaveResult Failed(List<string> errors) => new(false, null, errors);
}
