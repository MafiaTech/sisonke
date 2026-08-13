using System.ComponentModel.DataAnnotations;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Data.Entities;

public class SubscriptionPayment
{
    public Guid Id { get; set; }

    public Guid OrganisationSubscriptionId { get; set; }
    public OrganisationSubscription OrganisationSubscription { get; set; } = default!;

    public Guid? SubscriptionInvoiceId { get; set; }
    public SubscriptionInvoice? SubscriptionInvoice { get; set; }

    public decimal Amount { get; set; }

    [MaxLength(3)]
    public string Currency { get; set; } = "ZAR";

    public SubscriptionPaymentStatus Status { get; set; } = SubscriptionPaymentStatus.Pending;

    /// <summary>Null for historic rows whose provider was only available through the parent subscription.</summary>
    public SubscriptionProvider? Provider { get; set; }

    /// <summary>Business reason for the charge. Null preserves the meaning of historic rows.</summary>
    public SubscriptionChargePurpose? ChargePurpose { get; set; }

    /// <summary>Paystack transaction reference — the idempotency key for this payment attempt.</summary>
    [Required]
    [MaxLength(100)]
    public string ProviderReference { get; set; } = string.Empty;

    /// <summary>
    /// Sisonke/provider request or idempotency reference when it differs from ProviderReference.
    /// Never contains card or bank credentials.
    /// </summary>
    [MaxLength(150)]
    public string? ProviderRequestReference { get; set; }

    [MaxLength(100)]
    public string? ProviderTransactionId { get; set; }

    public int AttemptNumber { get; set; } = 1;

    [MaxLength(50)]
    public string? FailureCode { get; set; }

    [MaxLength(500)]
    public string? FailureMessage { get; set; }

    public DateTime? PaidAt { get; set; }

    /// <summary>When the provider finished processing the attempt, regardless of outcome.</summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>When funds were reported settled. Null until provider reconciliation confirms settlement.</summary>
    public DateTime? SettledAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
