using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Services.Billing;

public sealed class SubscriptionPaymentOptions
{
    public SubscriptionProvider DefaultProvider { get; set; } = SubscriptionProvider.Paystack;
    public bool TestMode { get; set; }
}
