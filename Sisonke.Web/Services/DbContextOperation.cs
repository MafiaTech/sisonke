using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;

namespace Sisonke.Web.Services;

// Circuit services own a fresh context per operation. Explicit transaction callers borrow
// their existing context; disposing the operation must never dispose that caller's unit of work.
internal sealed class DbContextOperation(ApplicationDbContext context, bool ownsContext) : IAsyncDisposable
{
    public ApplicationDbContext Context { get; } = context;

    public static async ValueTask<DbContextOperation> OpenAsync(
        IDbContextFactory<ApplicationDbContext> factory, ApplicationDbContext? transactionContext) =>
        transactionContext is not null ? new(transactionContext, false)
            : new(await factory.CreateDbContextAsync(), true);

    public ValueTask DisposeAsync() => ownsContext ? Context.DisposeAsync() : ValueTask.CompletedTask;
}
