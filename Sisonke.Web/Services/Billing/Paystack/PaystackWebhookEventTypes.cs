namespace Sisonke.Web.Services.Billing.Paystack;

/// <summary>
/// Event names cross-referenced across Paystack's GitHub docs repo, an AsyncAPI mirror and
/// Postman docs on 2026-08-03 (paystack.com/docs itself blocked automated fetches that day).
/// Two are flagged low-confidence below — verify against Paystack's dashboard "send test
/// webhook" feature before relying on them; a wrong string here only means that event is
/// silently Ignored (see BillingWebhookProcessingService), never a processing error.
/// </summary>
public static class PaystackWebhookEventTypes
{
    public const string ChargeSuccess = "charge.success";
    public const string InvoiceCreate = "invoice.create";
    public const string InvoiceUpdate = "invoice.update";
    public const string InvoicePaymentFailed = "invoice.payment_failed";
    public const string SubscriptionCreate = "subscription.create";
    public const string SubscriptionDisable = "subscription.disable";

    /// <summary>Low confidence — sources disagreed between "subscription.not_renew" and "subscription.not_renewing".</summary>
    public const string SubscriptionNotRenew = "subscription.not_renew";

    /// <summary>Low confidence — present in some source lists, absent from others.</summary>
    public const string SubscriptionEnable = "subscription.enable";

    public const string RefundPending = "refund.pending";
    public const string RefundProcessed = "refund.processed";
    public const string RefundFailed = "refund.failed";
}
