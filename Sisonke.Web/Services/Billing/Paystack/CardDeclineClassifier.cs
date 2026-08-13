namespace Sisonke.Web.Services.Billing.Paystack;

/// <summary>
/// Best-effort classification of a failed charge's gateway response text into "permanent" (never
/// retry — the card is closed/stolen/lost/restricted) vs "retryable" (insufficient funds etc).
/// Paystack does not document a structured decline-code taxonomy comparable to Stripe's
/// decline_code (confirmed during this phase's API research); this pattern-matches the
/// human-readable gateway_response/failure message instead. Verify against real Paystack
/// decline messages during sandbox testing — see docs/billing/PAYSTACK-TESTING.md — and extend
/// the phrase list rather than trusting it blindly.
/// </summary>
public static class CardDeclineClassifier
{
    private static readonly string[] PermanentDeclinePhrases =
    [
        "stolen", "lost card", "restricted card", "do not honour", "do not honor",
        "closed account", "invalid card", "expired card", "pick up card", "fraud"
    ];

    public static bool IsPermanentDecline(string? failureMessage)
    {
        if (string.IsNullOrWhiteSpace(failureMessage))
        {
            return false;
        }

        return PermanentDeclinePhrases.Any(phrase =>
            failureMessage.Contains(phrase, StringComparison.OrdinalIgnoreCase));
    }
}
