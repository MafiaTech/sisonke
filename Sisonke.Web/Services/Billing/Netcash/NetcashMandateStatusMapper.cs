using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Services.Billing.Netcash;

/// <summary>Conservative mapping: unknown provider states remain Pending and never become payment-ready.</summary>
public static class NetcashMandateStatusMapper
{
    public static MandateStatus Map(string? providerStatus) => providerStatus?.Trim().ToUpperInvariant() switch
    {
        "ACCEPTED" => MandateStatus.Active,
        "REJECTED" => MandateStatus.Failed,
        "CANCELLED" or "CANCELED" => MandateStatus.Revoked,
        "EXPIRED" => MandateStatus.Expired,
        _ => MandateStatus.Pending
    };
}

