using System.Data;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Services.Notifications;

namespace Sisonke.Web.Services;

public class MemberPaymentSubmissionService(
    IDbContextFactory<ApplicationDbContext> factory,
    PaymentProofStorage storage,
    AuditLogService audit,
    NotificationEnqueuer notifications)
{
    public Task<ContributionPaymentSubmission> SubmitAsync(string userId, Guid stokvelId,
        Guid contributionId, decimal amount, DateTime paymentDate, string? reference, string? notes, IBrowserFile file)
        => SubmitAsync(userId, stokvelId, PaymentObligationType.Contribution, contributionId, amount, paymentDate, reference, notes, file);

    public async Task<ContributionPaymentSubmission> SubmitAsync(string userId, Guid stokvelId,
        PaymentObligationType type, Guid obligationId, decimal amount, DateTime paymentDate, string? reference, string? notes, IBrowserFile file)
    {
        await using var db = await factory.CreateDbContextAsync();
        var (stokvel, member) = await RequireMembershipAsync(db, userId, stokvelId);
        var handler = PaymentObligationHandlers.For(type);
        var obligation = await handler.ResolveAsync(db, stokvel.TenantId, member.Id, obligationId);
        handler.ValidateAmount(obligation, amount);
        if (paymentDate.Date < new DateTime(1900, 1, 1) || paymentDate.Date > DateTime.Today)
            throw new InvalidOperationException("Enter a valid payment date that is not in the future.");
        if (reference?.Length > 100 || notes?.Length > 1000)
            throw new InvalidOperationException("Reference is limited to 100 characters and notes to 1000 characters.");
        if (await db.ContributionPaymentSubmissions.AnyAsync(s => s.ObligationType == type &&
            (type == PaymentObligationType.Contribution ? s.MemberContributionId == obligationId : s.MemberFineId == obligationId) && s.Status == ContributionPaymentSubmissionStatus.Pending))
            throw new InvalidOperationException("This obligation already has a proof awaiting verification.");

        var document = await storage.StoreAsync(stokvel.TenantId, userId, file);
        var submission = new ContributionPaymentSubmission
        {
            TenantId = stokvel.TenantId, StokvelId = stokvel.Id, MemberId = member.Id,
            ObligationType = type,
            MemberContributionId = type == PaymentObligationType.Contribution ? obligationId : null,
            MemberFineId = type == PaymentObligationType.Fine ? obligationId : null,
            Amount = amount, PaymentDate = paymentDate.Date,
            PaymentReference = reference?.Trim(), Notes = notes?.Trim(), SubmittedByUserId = userId,
            Documents = [document]
        };
        db.ContributionPaymentSubmissions.Add(submission);
        StageAudit(db, submission, userId, AuditAction(type, "Submitted"));
        var treasurers = await db.Members.Where(m => m.TenantId == stokvel.TenantId &&
            m.Status == MemberStatus.Active && m.DefaultRole == SisonkeRole.Treasurer && m.ApplicationUserId != null).ToListAsync();
        foreach (var treasurer in treasurers)
            await notifications.EnqueueAsync(db, type == PaymentObligationType.Contribution ? NotificationType.ContributionPaymentProofSubmitted : NotificationType.MemberPaymentProofSubmitted, treasurer.Id,
                stokvel.Id, nameof(ContributionPaymentSubmission), submission.Id, "Proof of payment awaiting verification",
                $"{member.FullName} submitted a payment of R{amount:0.00} for {obligation.Description}. Verification is required.");
        // SaveChanges commits metadata, audit and outbox together. The filtered unique index also
        // prevents simultaneous submissions. A failed database write may leave a private orphan file.
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Proof could not be submitted. Refresh to check for an existing pending submission before retrying.", ex);
        }
        return submission;
    }

    public async Task<List<MemberContribution>> GetMemberContributionsAsync(string userId, Guid stokvelId)
    {
        await using var db = await factory.CreateDbContextAsync();
        var (stokvel, member) = await RequireMembershipAsync(db, userId, stokvelId);
        return await db.MemberContributions.AsNoTracking().Include(c => c.ContributionCycle)
            .Where(c => c.TenantId == stokvel.TenantId && c.MemberId == member.Id && c.ContributionCycle.TenantId == stokvel.TenantId)
            .OrderByDescending(c => c.ContributionCycle.PeriodStart).ToListAsync();
    }

    public async Task<List<ContributionPaymentSubmission>> GetMemberSubmissionsAsync(string userId, Guid stokvelId)
    {
        await using var db = await factory.CreateDbContextAsync();
        var (stokvel, member) = await RequireMembershipAsync(db, userId, stokvelId);
        return await Details(db).Where(s => s.TenantId == stokvel.TenantId && s.StokvelId == stokvelId && s.MemberId == member.Id)
            .OrderByDescending(s => s.SubmittedAt).ToListAsync();
    }

    public async Task<List<PaymentObligation>> GetMemberObligationsAsync(string userId, Guid stokvelId, PaymentObligationType type)
    {
        await using var db = await factory.CreateDbContextAsync();
        var (stokvel, member) = await RequireMembershipAsync(db, userId, stokvelId);
        _ = PaymentObligationHandlers.For(type); // Reject unknown types, never accept arbitrary entity names.
        if (type == PaymentObligationType.Contribution)
            return (await db.MemberContributions.AsNoTracking().Include(c => c.ContributionCycle)
                .Where(c => c.MemberId == member.Id && c.TenantId == stokvel.TenantId && c.ContributionCycle.TenantId == stokvel.TenantId)
                .OrderByDescending(c => c.ContributionCycle.PeriodStart).ToListAsync()).Select(ContributionPaymentObligationHandler.Describe).ToList();
        return (await db.MemberFines.AsNoTracking().Include(f => f.FineType)
            .Where(f => f.MemberId == member.Id && f.TenantId == stokvel.TenantId && f.FineType.TenantId == stokvel.TenantId)
            .OrderByDescending(f => f.FineDate).ToListAsync()).Select(FinePaymentObligationHandler.Describe).ToList();
    }

    public async Task<List<ContributionPaymentSubmission>> GetPendingAsync(string userId, Guid stokvelId)
    {
        await using var db = await factory.CreateDbContextAsync();
        var (stokvel, _) = await RequireMembershipAsync(db, userId, stokvelId, treasurer: true);
        return await Details(db).Where(s => s.TenantId == stokvel.TenantId && s.StokvelId == stokvelId && s.Status == ContributionPaymentSubmissionStatus.Pending)
            .OrderBy(s => s.SubmittedAt).ToListAsync();
    }

    public async Task<ContributionPaymentSubmission> GetDetailsAsync(string userId, Guid stokvelId, Guid submissionId)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await AuthorizeDetailsAsync(db, userId, stokvelId, submissionId);
    }

    public async Task<ProofDownload> OpenProofAsync(string userId, Guid stokvelId, Guid documentId)
    {
        await using var db = await factory.CreateDbContextAsync();
        var document = await db.PaymentProofDocuments.AsNoTracking().SingleOrDefaultAsync(d => d.Id == documentId)
            ?? throw new UnauthorizedAccessException("Proof is not available.");
        var submission = await AuthorizeDetailsAsync(db, userId, stokvelId, document.ContributionPaymentSubmissionId);
        if (document.TenantId != submission.TenantId)
            throw new UnauthorizedAccessException("Proof is not available.");
        return new ProofDownload(storage.OpenRead(document), document.ContentType, document.OriginalFileName);
    }

    public Task ApproveAsync(string userId, Guid stokvelId, Guid submissionId) => ReviewAsync(userId, stokvelId, submissionId, null);

    public Task RejectAsync(string userId, Guid stokvelId, Guid submissionId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 1000)
            throw new InvalidOperationException("A rejection reason of at most 1000 characters is required.");
        return ReviewAsync(userId, stokvelId, submissionId, reason.Trim());
    }

    private async Task ReviewAsync(string userId, Guid stokvelId, Guid submissionId, string? rejection)
    {
        await using var strategyDb = await factory.CreateDbContextAsync();
        await strategyDb.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            // A fresh context per retry prevents stale tracked balances or statuses.
            await using var db = await factory.CreateDbContextAsync();
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var (stokvel, _) = await RequireMembershipAsync(db, userId, stokvelId, treasurer: true);
            var status = rejection is null ? ContributionPaymentSubmissionStatus.Approved : ContributionPaymentSubmissionStatus.Rejected;
            var now = DateTime.UtcNow;
            // Claim the pending row inside the posting transaction. Competing review requests
            // cannot both proceed, even across application instances.
            var changed = await db.ContributionPaymentSubmissions.Where(s => s.Id == submissionId &&
                s.StokvelId == stokvelId && s.TenantId == stokvel.TenantId && s.Status == ContributionPaymentSubmissionStatus.Pending)
                .ExecuteUpdateAsync(setters => setters.SetProperty(s => s.Status, status)
                    .SetProperty(s => s.ReviewedByUserId, userId).SetProperty(s => s.ReviewedAt, now)
                    .SetProperty(s => s.UpdatedAt, now).SetProperty(s => s.RejectionReason, rejection));
            if (changed != 1)
                throw new InvalidOperationException("Submission is unavailable or has already been reviewed.");
            var submission = await db.ContributionPaymentSubmissions.Include(s => s.Member)
                .SingleAsync(s => s.Id == submissionId);
            if (submission.Member.TenantId != stokvel.TenantId)
                throw new UnauthorizedAccessException("Submission membership does not match this stokvel.");
            var handler = PaymentObligationHandlers.For(submission.ObligationType);
            var obligationId = submission.ObligationType == PaymentObligationType.Contribution ? submission.MemberContributionId : submission.MemberFineId;
            if (!obligationId.HasValue) throw new InvalidOperationException("Submission obligation is missing.");
            var obligation = await handler.ResolveAsync(db, stokvel.TenantId, submission.MemberId, obligationId.Value);
            if (rejection is null)
            {
                handler.ValidateAmount(obligation, submission.Amount);
                await handler.PostAsync(db, audit, submission, userId);
            }
            var action = AuditAction(submission.ObligationType, rejection is null ? "Approved" : "Rejected");
            StageAudit(db, submission, userId, action);
            var notificationType = submission.ObligationType == PaymentObligationType.Contribution
                ? (rejection is null ? NotificationType.ContributionPaymentProofApproved : NotificationType.ContributionPaymentProofRejected)
                : (rejection is null ? NotificationType.MemberPaymentProofApproved : NotificationType.MemberPaymentProofRejected);
            await notifications.EnqueueAsync(db, notificationType,
                submission.MemberId, stokvelId, nameof(ContributionPaymentSubmission), submission.Id,
                rejection is null ? "Proof of payment approved" : "Proof of payment rejected",
                rejection is null ? $"Your payment of R{submission.Amount:0.00} for {obligation.Description} has been verified."
                    : $"Your payment of R{submission.Amount:0.00} for {obligation.Description} was rejected: {rejection}");
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        });
    }

    private static IQueryable<ContributionPaymentSubmission> Details(ApplicationDbContext db) => db.ContributionPaymentSubmissions.AsNoTracking()
        .Include(s => s.Member).Include(s => s.MemberContribution).ThenInclude(c => c!.ContributionCycle)
        .Include(s => s.MemberFine).ThenInclude(f => f!.FineType).Include(s => s.Documents);

    private static async Task<ContributionPaymentSubmission> AuthorizeDetailsAsync(ApplicationDbContext db, string userId, Guid stokvelId, Guid id)
    {
        var (stokvel, member) = await RequireMembershipAsync(db, userId, stokvelId);
        var submission = await Details(db).SingleOrDefaultAsync(s => s.Id == id && s.StokvelId == stokvelId && s.TenantId == stokvel.TenantId);
        if (submission is null || (submission.MemberId != member.Id && !await new MemberAccessService(db).CanManagePaymentsAsync(userId, stokvelId)))
            throw new UnauthorizedAccessException("Submission is not available.");
        return submission;
    }

    private static async Task<(Stokvel, Member)> RequireMembershipAsync(ApplicationDbContext db, string userId, Guid stokvelId, bool treasurer = false)
    {
        if (string.IsNullOrWhiteSpace(userId) || !await db.Users.AnyAsync(u => u.Id == userId))
            throw new UnauthorizedAccessException("Sign in with your linked Sisonke account.");
        var stokvel = await db.Stokvels.SingleOrDefaultAsync(s => s.Id == stokvelId && s.IsActive && !s.IsDeleted)
            ?? throw new UnauthorizedAccessException("Stokvel is not available.");
        var access = new MemberAccessService(db);
        var member = await access.GetLinkedMemberForUserAsync(userId, stokvelId)
            ?? throw new UnauthorizedAccessException("A linked membership is required for this stokvel.");
        // The legacy monthly ledger has TenantId, not StokvelId. Fail closed when tenant mapping
        // is ambiguous instead of attributing the same contribution to a different stokvel.
        if (await db.Stokvels.CountAsync(s => s.TenantId == stokvel.TenantId) != 1)
            throw new InvalidOperationException("Payment obligation ownership is ambiguous for this tenant. Contact support.");
        if (treasurer && !await access.CanManagePaymentsAsync(userId, stokvelId))
            throw new UnauthorizedAccessException("Only the Treasurer can review payment proofs.");
        return (stokvel, member);
    }

    private static string AuditAction(PaymentObligationType type, string action) =>
        (type == PaymentObligationType.Contribution ? "ContributionPaymentProof" : "MemberPaymentProof") + action;

    private void StageAudit(ApplicationDbContext db, ContributionPaymentSubmission submission, string userId, string action) =>
        audit.Stage(db, userId, submission.StokvelId, action, nameof(ContributionPaymentSubmission), submission.Id,
            $"Type {submission.ObligationType}; obligation {submission.MemberContributionId ?? submission.MemberFineId}; member {submission.MemberId}; submission {submission.Id}.");
}

public sealed record ProofDownload(Stream Stream, string ContentType, string FileName);

