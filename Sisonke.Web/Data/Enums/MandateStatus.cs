namespace Sisonke.Web.Data.Enums;

/// <summary>Lifecycle of a provider-hosted recurring-payment mandate. Values are persisted.</summary>
public enum MandateStatus
{
    None = 0,
    Pending = 1,
    Active = 2,
    Failed = 3,
    Revoked = 4,
    Expired = 5
}
