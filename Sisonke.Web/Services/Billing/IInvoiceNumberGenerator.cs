using Sisonke.Web.Data;

namespace Sisonke.Web.Services.Billing;

public interface IInvoiceNumberGenerator
{
    /// <summary>
    /// Allocates the next sequential number for the given context's pending transaction — call
    /// this after adding the SubscriptionInvoice to the context but before SaveChangesAsync, so
    /// the counter increment and the invoice row commit atomically together.
    /// </summary>
    Task<string> NextAsync(ApplicationDbContext context, CancellationToken ct = default);
}
