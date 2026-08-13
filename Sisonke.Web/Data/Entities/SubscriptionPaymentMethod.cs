using System.ComponentModel.DataAnnotations;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Data.Entities;

/// <summary>
/// Provider token/mandate metadata returned after hosted authorisation. Sisonke never stores card
/// numbers, CVVs, full bank credentials or online-banking credentials. The legacy Paystack
/// ProviderAuthorizationCode remains supported; new provider-neutral references are additive.
/// </summary>
public class SubscriptionPaymentMethod
{
    public Guid Id { get; set; }

    public Guid OrganisationSubscriptionId { get; set; }
    public OrganisationSubscription OrganisationSubscription { get; set; } = default!;

    public SubscriptionProvider Provider { get; set; }

    public SubscriptionPaymentMethodType PaymentMethodType { get; set; } = SubscriptionPaymentMethodType.Unknown;

    public MandateStatus MandateStatus { get; set; } = MandateStatus.None;

    [MaxLength(100)]
    public string? ProviderAuthorizationCode { get; set; }

    /// <summary>Provider-issued mandate reference; never a bank account number or credential.</summary>
    [MaxLength(150)]
    public string? ProviderMandateReference { get; set; }

    /// <summary>Provider-issued token/reference identifying the reusable payment method.</summary>
    [MaxLength(150)]
    public string? ProviderPaymentMethodReference { get; set; }

    /// <summary>Safe provider-supplied masked description for display, for example "Card ending 1234".</summary>
    [MaxLength(100)]
    public string? MaskedDisplay { get; set; }

    [MaxLength(30)]
    public string? CardBrand { get; set; }

    [MaxLength(4)]
    public string? Last4 { get; set; }

    public int? ExpiryMonth { get; set; }

    public int? ExpiryYear { get; set; }

    [MaxLength(100)]
    public string? Bank { get; set; }

    [MaxLength(150)]
    public string? CardholderName { get; set; }

    public bool IsDefault { get; set; }

    public bool IsReusable { get; set; }

    public DateTime AuthorisedAt { get; set; } = DateTime.UtcNow;

    public DateTime? MandateCreatedAt { get; set; }

    public DateTime? MandateActivatedAt { get; set; }

    public DateTime? RemovedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
