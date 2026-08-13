using System.ComponentModel.DataAnnotations;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Data.Entities;

/// <summary>
/// The Sisonke subscription (revenue) record for a Stokvel — separate from member contribution
/// accounting (see brief rule 5: never co-mingle the two ledgers). At most one row per StokvelId
/// may be in a "live" status (i.e. not Cancelled/Expired) — enforced by a filtered unique index,
/// not by application logic alone. All timestamps are UTC.
/// </summary>
public class OrganisationSubscription
{
    public Guid Id { get; set; }

    public Guid StokvelId { get; set; }
    public Stokvel Stokvel { get; set; } = default!;

    /// <summary>Null while LegacyUnsubscribed / before a plan has been selected.</summary>
    public Guid? SubscriptionPlanId { get; set; }
    public SubscriptionPlan? SubscriptionPlan { get; set; }

    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.LegacyUnsubscribed;

    public DateTime? TrialStartedAt { get; set; }

    public DateTime? TrialEndsAt { get; set; }

    /// <summary>
    /// True only after the stokvel explicitly completes the trial activation flow. Trial dates on
    /// their own do not prove opt-in. TermsAcceptedAt/TermsVersion/TermsAcceptedByUserId contain
    /// the versioned consent evidence and are intentionally reused rather than duplicated.
    /// </summary>
    public bool TrialOptedIn { get; set; }

    public DateTime? CurrentPeriodStartedAt { get; set; }

    public DateTime? CurrentPeriodEndsAt { get; set; }

    public bool CancelAtPeriodEnd { get; set; }

    public DateTime? CancelledAt { get; set; }

    public DateTime? SuspendedAt { get; set; }

    public SubscriptionProvider? Provider { get; set; }

    [MaxLength(100)]
    public string? ProviderCustomerCode { get; set; }

    [MaxLength(100)]
    public string? ProviderSubscriptionCode { get; set; }

    [MaxLength(100)]
    public string? ProviderPlanCode { get; set; }

    /// <summary>Paystack's email_token for this subscription — required (with ProviderSubscriptionCode) to disable it.</summary>
    [MaxLength(200)]
    public string? ProviderEmailToken { get; set; }

    /// <summary>
    /// A downgrade takes effect at period end, not immediately (see brief's proration policy).
    /// Set together; applied by a future billing-cycle rollover job (not built in this phase —
    /// there is no renewal/period-rollover job yet) when CurrentPeriodEndsAt is reached.
    /// </summary>
    public Guid? PendingPlanChangePlanId { get; set; }
    public SubscriptionPlan? PendingPlanChangePlan { get; set; }

    public DateTime? PendingPlanChangeEffectiveAt { get; set; }

    /// <summary>
    /// Set when the first debit of the current billing period fails (day 0 of dunning); cleared
    /// (null) the moment any charge succeeds. DunningJob computes every dunning action from
    /// "days since this timestamp", never from "days since the job last ran" — see brief.
    /// </summary>
    public DateTime? DunningStartedAt { get; set; }

    public DateTime? LastSuccessfulPaymentAt { get; set; }

    public DateTime? NextBillingAt { get; set; }

    public DateTime? GracePeriodEndsAt { get; set; }

    [MaxLength(256)]
    public string? BillingEmail { get; set; }

    public DateTime? TermsAcceptedAt { get; set; }

    [MaxLength(450)]
    public string? TermsAcceptedByUserId { get; set; }

    /// <summary>
    /// Identifies which revision of the subscription terms was accepted. The terms copy itself
    /// is configurable content pending South African commercial attorney review (CPA
    /// implications) before launch — not hard-coded here.
    /// </summary>
    [MaxLength(30)]
    public string? TermsVersion { get; set; }

    /// <summary>
    /// IP address the terms acceptance / plan selection was made from — part of the evidence
    /// trail required by the Phase 5 brief for legacy-migration disputes ("we did not agree to
    /// recurring billing", "we were charged without notice"). Best-effort; null if unavailable.
    /// </summary>
    [MaxLength(45)]
    public string? TermsAcceptedIpAddress { get; set; }

    /// <summary>Display-only billing identity captured on the card-authorisation step. Never card data (brief rule 1).</summary>
    [MaxLength(150)]
    public string? CardholderName { get; set; }

    [MaxLength(400)]
    public string? BillingAddress { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// App-managed optimistic concurrency token (see OnModelCreating: IsConcurrencyToken(), not
    /// IsRowVersion()). SQL Server's store-generated `rowversion` type has no SQLite equivalent —
    /// this app supports both providers (dev/tests run on SQLite). Every SubscriptionService
    /// mutation must reassign this to a fresh value before SaveChangesAsync — EF's concurrency
    /// check compares the row's current DB value against what was originally loaded, so
    /// reassigning here is what makes a lost-update conflict throw DbUpdateConcurrencyException.
    /// </summary>
    public byte[] RowVersion { get; set; } = Guid.NewGuid().ToByteArray();

    public ICollection<SubscriptionPaymentMethod> PaymentMethods { get; set; } = new List<SubscriptionPaymentMethod>();

    public ICollection<SubscriptionInvoice> Invoices { get; set; } = new List<SubscriptionInvoice>();

    public ICollection<SubscriptionPayment> Payments { get; set; } = new List<SubscriptionPayment>();

    public ICollection<SubscriptionEvent> Events { get; set; } = new List<SubscriptionEvent>();

    public ICollection<SubscriptionUsage> UsageRecords { get; set; } = new List<SubscriptionUsage>();
}
