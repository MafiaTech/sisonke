namespace Sisonke.Web.Services.Billing.Netcash;

public enum NetcashBankAccountType
{
    Current = 1,
    Savings = 2,
    Transmission = 3
}

/// <summary>
/// Transient payer input solely for authorising Sisonke platform subscription fees. Account and
/// identity values must never be persisted, logged or generalized into a stokvel banking feature.
/// Amount and beneficiary are deliberately absent and are determined server-side.
/// </summary>
public sealed record NetcashDebiCheckMandateRequest(
    string AccountHolderName,
    bool IsSouthAfricanId,
    string DebtorIdentification,
    string BankAccountName,
    string BranchCode,
    string BankAccountNumber,
    NetcashBankAccountType BankAccountType,
    string MobileNumber,
    string EmailAddress,
    bool ConsentAccepted,
    string TermsVersion);

/// <summary>Low-level provider request assembled only by the subscription mandate service.</summary>
public sealed record NetcashAuthenticationRequest(
    string AccountReference,
    string AccountHolderName,
    bool IsSouthAfricanId,
    string DebtorIdentification,
    string BankAccountName,
    string BranchCode,
    string BankAccountNumber,
    NetcashBankAccountType BankAccountType,
    string MobileNumber,
    string EmailAddress,
    decimal CollectionAmount,
    DateOnly FirstCollectionDate,
    int MonthlyCollectionDay);

public sealed record NetcashProviderResult(
    bool Success,
    string? ErrorCode,
    string? Status,
    string? ContractReference,
    string? Message)
{
    public static NetcashProviderResult Failed(string code, string message) =>
        new(false, code, null, null, message);
}

public sealed record NetcashMandateActionResult(
    bool Success,
    Data.Enums.MandateStatus MandateStatus,
    bool PaymentReady,
    string Message);
