using Sisonke.Web.Data.Entities;

namespace Sisonke.Web.Services.Billing;

public interface IInvoicePdfRenderer
{
    byte[] RenderInvoice(SubscriptionInvoice invoice, IReadOnlyList<SubscriptionInvoiceLine> lines, Stokvel stokvel);

    byte[] RenderReceipt(SubscriptionPayment payment, SubscriptionInvoice? invoice, Stokvel stokvel);
}
