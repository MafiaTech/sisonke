namespace Sisonke.Web.Services.Billing.Paystack;

/// <summary>
/// Event names documented by Paystack at https://paystack.com/docs/payments/webhooks/ and
/// https://paystack.com/docs/payments/subscriptions/ (verified 2026-08-14). Test-mode deliveries
/// must still be compared with these mappings before live charging is enabled.
/// </summary>
public static class PaystackWebhookEventTypes
{
    public const string ChargeSuccess = "charge.success";
    public const string InvoiceCreate = "invoice.create";
    public const string InvoiceUpdate = "invoice.update";
    public const string InvoicePaymentFailed = "invoice.payment_failed";
    public const string SubscriptionCreate = "subscription.create";
    public const string SubscriptionDisable = "subscription.disable";

    public const string SubscriptionNotRenew = "subscription.not_renew";
    public const string SubscriptionExpiringCards = "subscription.expiring_cards";

    public const string RefundPending = "refund.pending";
    public const string RefundProcessed = "refund.processed";
    public const string RefundProcessing = "refund.processing";
    public const string RefundFailed = "refund.failed";
}
