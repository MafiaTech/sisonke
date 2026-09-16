namespace Sisonke.Web.Data.Enums;

/// <summary>Provider-neutral payment method classification. Values are persisted.</summary>
public enum SubscriptionPaymentMethodType
{
    Unknown = 0,
    Card = 1,
    DebitOrder = 2,
    DebiCheck = 3,
    BankTransfer = 4,
    Other = 5
}
