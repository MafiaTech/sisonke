namespace Sisonke.Web.Data;

/// <summary>
/// Version identifier for the subscription terms shown on the onboarding/legacy-migration
/// card-authorisation step. Recorded on OrganisationSubscription.TermsVersion at acceptance.
///
/// LEGAL REVIEW REQUIRED (brief rule 10): the 60-day introductory subscription period, CPA
/// applicability, fixed-term/renewal notice rules and month-to-month continuation wording behind
/// this version must be reviewed by a South African commercial attorney before launch. This
/// constant is a version tag only — it does not contain the terms copy itself.
/// </summary>
public static class BillingTermsVersion
{
    /// <summary>Stable identifier for the consent document; legal copy is maintained separately.</summary>
    public const string TrialAndRecurringBillingConsentDocumentCode = "TRIAL_AND_RECURRING_BILLING_CONSENT";

    /// <summary>Placeholder version pending South African commercial/legal review.</summary>
    public const string CurrentTrialAndRecurringBillingConsentVersion = "2026-08-04-v1";

    /// <summary>
    /// Versioned product-purpose wording, pending final legal copy review. Keep this specific to
    /// Sisonke's own SaaS fees; it must never authorize movement of stokvel/member funds.
    /// </summary>
    public const string SubscriptionMandatePurposeStatement =
        "This authorization is only for Sisonke platform subscription fees.";

    /// <summary>Backward-compatible alias used by the current onboarding flow.</summary>
    public const string Current = CurrentTrialAndRecurringBillingConsentVersion;
}
