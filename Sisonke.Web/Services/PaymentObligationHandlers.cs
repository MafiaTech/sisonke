using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Services;

public sealed record PaymentObligation(Guid Id, PaymentObligationType Type, string Description,
    decimal Expected, decimal Paid, decimal Outstanding, DateTime Date, string Status, bool Payable);

public interface IPaymentObligationHandler
{
    Task<PaymentObligation> ResolveAsync(ApplicationDbContext db, Guid tenantId, Guid memberId, Guid id);
    void ValidateAmount(PaymentObligation obligation, decimal amount);
    Task PostAsync(ApplicationDbContext db, AuditLogService audit, ContributionPaymentSubmission submission, string actor);
}

public static class PaymentObligationHandlers
{
    public static IPaymentObligationHandler For(PaymentObligationType type) => type switch
    {
        PaymentObligationType.Contribution => new ContributionPaymentObligationHandler(),
        PaymentObligationType.Fine => new FinePaymentObligationHandler(),
        _ => throw new InvalidOperationException("Unsupported payment obligation type.")
    };

    public static void ValidateWholeRand(decimal amount)
    {
        if (amount <= 0 || amount > 9999999999999999m || decimal.Truncate(amount) != amount)
            throw new InvalidOperationException("Enter a whole-rand amount greater than zero.");
    }
}

public sealed class ContributionPaymentObligationHandler : IPaymentObligationHandler
{
    public async Task<PaymentObligation> ResolveAsync(ApplicationDbContext db, Guid tenantId, Guid memberId, Guid id)
    {
        var c = await db.MemberContributions.AsNoTracking().Include(c => c.ContributionCycle)
            .SingleOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId && c.MemberId == memberId && c.ContributionCycle.TenantId == tenantId)
            ?? throw new UnauthorizedAccessException("Contribution is not available for your membership.");
        return Describe(c);
    }

    public static PaymentObligation Describe(MemberContribution c) => new(c.Id, PaymentObligationType.Contribution,
        $"{c.ContributionCycle.PeriodStart:MMMM yyyy} Contribution", c.ExpectedAmount, c.PaidAmount, c.OutstandingAmount,
        c.ContributionCycle.DueDate, c.StatusLabel, c.OutstandingAmount > 0 && c.Status is not
            (PaymentStatus.Paid or PaymentStatus.Exempted or PaymentStatus.WrittenOff or PaymentStatus.Reversed));

    public void ValidateAmount(PaymentObligation obligation, decimal amount)
    {
        PaymentObligationHandlers.ValidateWholeRand(amount);
        if (!obligation.Payable) throw new InvalidOperationException("This contribution is no longer eligible for payment verification.");
        if (amount > obligation.Outstanding) throw new InvalidOperationException("Amount paid cannot exceed the outstanding contribution amount.");
    }

    public async Task PostAsync(ApplicationDbContext db, AuditLogService audit, ContributionPaymentSubmission s, string actor)
    {
        var result = await new ContributionPaymentService(db, new MemberAccessService(db), audit)
            .CaptureContributionPaymentAsync(s.MemberContributionId!.Value, s.Amount, s.PaymentReference, s.Notes, actor, s.PaymentDate, s.StokvelId);
        if (result is null) throw new InvalidOperationException("Payment could not be posted.");
        s.PaymentId = db.ChangeTracker.Entries<Payment>().Single().Entity.Id;
    }
}

public sealed class FinePaymentObligationHandler : IPaymentObligationHandler
{
    public async Task<PaymentObligation> ResolveAsync(ApplicationDbContext db, Guid tenantId, Guid memberId, Guid id)
    {
        var f = await db.MemberFines.AsNoTracking().Include(f => f.FineType)
            .SingleOrDefaultAsync(f => f.Id == id && f.TenantId == tenantId && f.MemberId == memberId && f.FineType.TenantId == tenantId)
            ?? throw new UnauthorizedAccessException("Fine is not available for your membership.");
        return Describe(f);
    }

    public static PaymentObligation Describe(MemberFine f) => new(f.Id, PaymentObligationType.Fine,
        $"{f.FineType.Name}: {f.Reason}", f.Amount, f.Status == FineStatus.Paid ? f.Amount : 0,
        f.Status == FineStatus.Unpaid ? f.Amount : 0, f.FineDate, f.Status.ToString(), f.Status == FineStatus.Unpaid && f.Amount > 0);

    public void ValidateAmount(PaymentObligation obligation, decimal amount)
    {
        PaymentObligationHandlers.ValidateWholeRand(amount);
        if (!obligation.Payable) throw new InvalidOperationException("This fine is no longer eligible for payment verification.");
        if (amount != obligation.Outstanding) throw new InvalidOperationException("Amount paid must equal the full outstanding fine amount. Partial fine payments are not supported.");
    }

    public async Task PostAsync(ApplicationDbContext db, AuditLogService audit, ContributionPaymentSubmission s, string actor)
    {
        var result = await new FineService(db, audit).MarkFineAsPaidAsync(s.MemberFineId!.Value, actor, s.StokvelId, s.PaymentDate);
        if (result is null) throw new InvalidOperationException("Fine payment could not be posted.");
    }
}
