using Bunit;
using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Components.Payments;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Services;
using Sisonke.Web.Tests.TestSupport;

namespace Sisonke.Web.Tests.Contributions;

public sealed partial class ContributionPaymentSubmissionTests
{
    private Guid AddFine(FineStatus status = FineStatus.Unpaid, decimal amount = 50, Guid? memberId = null)
    {
        using var db = database.CreateContext();
        var member = db.Members.Single(m => m.Id == (memberId ?? ownerId));
        var type = new FineType { Id = Guid.NewGuid(), TenantId = member.TenantId, Name = "Late Coming Fine", DefaultAmount = 50 };
        db.FineTypes.Add(type);
        var fine = new MemberFine { Id = Guid.NewGuid(), TenantId = member.TenantId, MemberId = member.Id,
            FineTypeId = type.Id, Reason = "Late Coming", Amount = amount, Status = status };
        db.MemberFines.Add(fine);
        db.SaveChanges();
        return fine.Id;
    }

    private Task<ContributionPaymentSubmission> SubmitFine(Guid id, decimal amount = 50, string? user = null, Guid? targetStokvel = null) =>
        service.SubmitAsync(user ?? ownerUser, targetStokvel ?? stokvelId, PaymentObligationType.Fine, id, amount,
            DateTime.Today.AddDays(-1), "Bank transfer", "Fine proof", new PdfFile());

    [Fact]
    public async Task FineSubmissionIsPendingAndNotifiesTreasurerWithoutPosting()
    {
        var fineId = AddFine();
        var s = await SubmitFine(fineId);
        using var db = database.CreateContext();
        Assert.Equal(PaymentObligationType.Fine, s.ObligationType);
        Assert.Null(s.MemberContributionId);
        Assert.Equal(fineId, s.MemberFineId);
        Assert.Equal(FineStatus.Unpaid, db.MemberFines.Single().Status);
        Assert.Null(db.MemberFines.Single().PaidDate);
        Assert.Empty(db.Payments);
        Assert.Empty(db.ContributionPaymentAudits);
        Assert.Contains(db.NotificationMessages, n => n.Type == NotificationType.MemberPaymentProofSubmitted &&
            n.Body.Contains("R50.00") && n.Body.Contains("Late Coming Fine") && n.Body.Contains("#payment-proofs"));
        Assert.Contains(db.AuditLogEntries, a => a.ActionType == "MemberPaymentProofSubmitted" && a.EntityId == s.Id);
    }

    [Theory]
    [InlineData(FineStatus.Paid)]
    [InlineData(FineStatus.Cancelled)]
    [InlineData(FineStatus.Waived)]
    public async Task NonPayableFineCannotBeSubmitted(FineStatus status) =>
        await Assert.ThrowsAsync<InvalidOperationException>(() => SubmitFine(AddFine(status)));

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    [InlineData(20)]
    [InlineData(51)]
    [InlineData(50.5)]
    public async Task FineRequiresExactPositiveWholeRandAmount(decimal amount) =>
        await Assert.ThrowsAsync<InvalidOperationException>(() => SubmitFine(AddFine(), amount));

    [Fact]
    public async Task AnotherMembersFineCannotBeSubmitted()
    {
        var other = AddActor(SisonkeRole.Member);
        var fineId = AddFine();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => SubmitFine(fineId, user: other));
    }

    [Fact]
    public async Task CrossTenantFineAndDocumentAreBlocked()
    {
        var fineId = AddFine();
        var s = await SubmitFine(fineId);
        using var db = database.CreateContext();
        var other = TestData.CreateStokvel(db);
        var user = Guid.NewGuid().ToString();
        AddMember(db, other, user, SisonkeRole.Treasurer);
        db.SaveChanges();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => SubmitFine(fineId, user: user, targetStokvel: other.Id));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.OpenProofAsync(user, other.Id, s.Documents.Single().Id));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ApproveAsync(user, stokvelId, s.Id));
    }

    [Fact]
    public async Task SameTenantSecondStokvelFailsClosedForFines()
    {
        var id = AddFine();
        var s = await SubmitFine(id);
        using var db = database.CreateContext();
        var original = db.Stokvels.Single();
        var second = new Stokvel { Id = Guid.NewGuid(), TenantId = original.TenantId, Name = "Second stokvel", IsActive = true };
        db.Stokvels.Add(second); db.SaveChanges();
        await Assert.ThrowsAsync<InvalidOperationException>(() => SubmitFine(id, targetStokvel: second.Id));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.OpenProofAsync(ownerUser, second.Id, s.Documents.Single().Id));
    }

    [Fact]
    public async Task FineProofOwnerAndTreasurerCanReadButOtherMemberCannot()
    {
        var s = await SubmitFine(AddFine());
        var doc = s.Documents.Single().Id;
        var other = AddActor(SisonkeRole.Member);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.OpenProofAsync(other, stokvelId, doc));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetDetailsAsync(other, stokvelId, s.Id));
        using var ownerStream = (await service.OpenProofAsync(ownerUser, stokvelId, doc)).Stream;
        using var treasurerStream = (await service.OpenProofAsync(treasurerUser, stokvelId, doc)).Stream;
        Assert.True(ownerStream.Length > 0 && treasurerStream.Length > 0);
    }

    [Fact]
    public async Task FineApprovalUsesFinePostingAuditAndDateAndNotifiesMember()
    {
        var s = await SubmitFine(AddFine());
        await service.ApproveAsync(treasurerUser, stokvelId, s.Id);
        using var db = database.CreateContext();
        Assert.Equal(FineStatus.Paid, db.MemberFines.Single().Status);
        Assert.Equal(DateTime.Today.AddDays(-1), db.MemberFines.Single().PaidDate);
        Assert.Equal(ContributionPaymentSubmissionStatus.Approved, db.ContributionPaymentSubmissions.Single().Status);
        Assert.Contains(db.AuditLogEntries, a => a.ActionType == "FinePaid" && a.UserId == treasurerUser);
        Assert.Contains(db.AuditLogEntries, a => a.ActionType == "MemberPaymentProofApproved");
        Assert.Contains(db.NotificationMessages, n => n.Type == NotificationType.MemberPaymentProofApproved &&
            n.RecipientMemberId == ownerId && n.Body.Contains("R50.00") && n.Body.Contains("has been verified"));
        Assert.Empty(db.Payments); // Contributions' Payment table is not the fine ledger.
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ApproveAsync(treasurerUser, stokvelId, s.Id));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RejectAsync(treasurerUser, stokvelId, s.Id, "Too late"));
    }

    [Fact]
    public async Task FineRejectionNotifiesMemberAndAllowsCorrectedSubmission()
    {
        var id = AddFine();
        var s = await SubmitFine(id);
        await service.RejectAsync(treasurerUser, stokvelId, s.Id, "Wrong proof");
        using var db = database.CreateContext();
        Assert.Equal(FineStatus.Unpaid, db.MemberFines.Single().Status);
        Assert.Null(db.MemberFines.Single().PaidDate);
        Assert.Contains(db.NotificationMessages, n => n.Type == NotificationType.MemberPaymentProofRejected && n.Body.Contains("Wrong proof"));
        Assert.Contains(db.AuditLogEntries, a => a.ActionType == "MemberPaymentProofRejected");
        Assert.DoesNotContain(db.AuditLogEntries, a => a.ActionType == "FinePaid");
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ApproveAsync(treasurerUser, stokvelId, s.Id));
        Assert.Equal(ContributionPaymentSubmissionStatus.Pending, (await SubmitFine(id)).Status);
    }

    [Theory]
    [InlineData(SisonkeRole.Member)]
    [InlineData(SisonkeRole.Secretary)]
    [InlineData(SisonkeRole.Chairperson)]
    public async Task FineReviewsRequireTreasurer(SisonkeRole role)
    {
        var s = await SubmitFine(AddFine());
        var actor = AddActor(role);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ApproveAsync(actor, stokvelId, s.Id));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.RejectAsync(actor, stokvelId, s.Id, "No"));
    }

    [Fact]
    public async Task FineReviewAuditFailureRollsBackFineAndSubmission()
    {
        var s = await SubmitFine(AddFine());
        using (var db = database.CreateContext())
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER fail_fine_review BEFORE INSERT ON AuditLogEntries WHEN NEW.ActionType = 'MemberPaymentProofApproved' BEGIN SELECT RAISE(ABORT, 'Test failure'); END;");
        await Assert.ThrowsAsync<DbUpdateException>(() => service.ApproveAsync(treasurerUser, stokvelId, s.Id));
        using var check = database.CreateContext();
        Assert.Equal(FineStatus.Unpaid, check.MemberFines.Single().Status);
        Assert.Null(check.MemberFines.Single().PaidDate);
        Assert.Equal(ContributionPaymentSubmissionStatus.Pending, check.ContributionPaymentSubmissions.Single().Status);
        Assert.DoesNotContain(check.AuditLogEntries, a => a.ActionType == "FinePaid");
        Assert.DoesNotContain(check.NotificationMessages, n => n.Type == NotificationType.MemberPaymentProofApproved);
    }

    [Fact]
    public async Task FineApprovalRechecksCancelledFine()
    {
        var s = await SubmitFine(AddFine());
        using (var db = database.CreateContext()) { db.MemberFines.Single().Status = FineStatus.Cancelled; db.SaveChanges(); }
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ApproveAsync(treasurerUser, stokvelId, s.Id));
        using var check = database.CreateContext();
        Assert.Equal(ContributionPaymentSubmissionStatus.Pending, check.ContributionPaymentSubmissions.Single().Status);
    }

    [Fact]
    public async Task FineConcurrentApprovalPostsOnlyOneFineAudit()
    {
        var s = await SubmitFine(AddFine());
        using var start = new ManualResetEventSlim(false);
        Task<Exception?> Review() => Task.Run<Exception?>(async () => { start.Wait(); return await Record.ExceptionAsync(() => service.ApproveAsync(treasurerUser, stokvelId, s.Id)); });
        var first = Review(); var second = Review(); start.Set();
        var results = await Task.WhenAll(first, second);
        Assert.Single(results, r => r is null);
        using var db = database.CreateContext();
        Assert.Single(db.AuditLogEntries.Where(a => a.ActionType == "FinePaid"));
    }

    [Fact]
    public async Task UnifiedQueueContainsBothTypesAndAllowsSeparatePendingFines()
    {
        await Submit(); await SubmitFine(AddFine()); await SubmitFine(AddFine());
        var queue = await service.GetPendingAsync(treasurerUser, stokvelId);
        Assert.Equal(3, queue.Count);
        Assert.Contains(queue, s => s.ObligationType == PaymentObligationType.Contribution && s.PaymentFor.Contains("Contribution"));
        Assert.Equal(2, queue.Count(s => s.ObligationType == PaymentObligationType.Fine && s.PaymentFor.Contains("Late Coming")));
    }

    [Fact]
    public async Task FineDuplicatePendingAndUnknownTypeAreBlocked()
    {
        var id = AddFine(); await SubmitFine(id);
        await Assert.ThrowsAsync<InvalidOperationException>(() => SubmitFine(id));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SubmitAsync(ownerUser, stokvelId, (PaymentObligationType)99, id, 50, DateTime.Today, null, null, new PdfFile()));
    }

    [Fact]
    public void FineUsesSharedFormAndWholeRandInput()
    {
        AddFine();
        using var ui = CreateUi();
        var panel = ui.Render<MemberPaymentPanel>(p => p.Add(c => c.StokvelId, stokvelId).Add(c => c.ObligationType, PaymentObligationType.Fine));
        panel.WaitForAssertion(() => Assert.Contains("Late Coming", panel.Markup));
        panel.FindAll("button").Single(b => b.TextContent == "I've Paid").Click();
        Assert.Contains("Record a payment you have already made", panel.Markup);
        Assert.Equal("50", panel.Find("#fine-proof-amount").GetAttribute("value"));
        panel.Find("#fine-proof-amount").Input("20");
        panel.Find("form").Submit();
        Assert.Contains("must equal the full outstanding fine amount", panel.Markup);
    }

    [Theory]
    [InlineData(FineStatus.Paid)]
    [InlineData(FineStatus.Cancelled)]
    [InlineData(FineStatus.Waived)]
    public void NonPayableFineHasNoPaymentAction(FineStatus status)
    {
        AddFine(status);
        using var ui = CreateUi();
        var panel = ui.Render<MemberPaymentPanel>(p => p.Add(c => c.StokvelId, stokvelId).Add(c => c.ObligationType, PaymentObligationType.Fine));
        panel.WaitForAssertion(() => Assert.Contains(status.ToString(), panel.Markup));
        Assert.DoesNotContain(panel.FindAll("button"), b => b.TextContent == "I've Paid");
    }

    [Fact]
    public async Task FineAmountChangedWhilePendingCannotBeApproved()
    {
        var s = await SubmitFine(AddFine());
        using (var db = database.CreateContext()) { db.MemberFines.Single().Amount = 75; db.SaveChanges(); }
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ApproveAsync(treasurerUser, stokvelId, s.Id));
        using var check = database.CreateContext();
        Assert.Equal(FineStatus.Unpaid, check.MemberFines.Single().Status);
        Assert.Equal(ContributionPaymentSubmissionStatus.Pending, check.ContributionPaymentSubmissions.Single().Status);
    }

    [Fact]
    public async Task FineProofUsesSharedFileValidation()
    {
        var id = AddFine();
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SubmitAsync(ownerUser, stokvelId, PaymentObligationType.Fine,
            id, 50, DateTime.Today, null, null, new PdfFile("proof.exe")));
        using var db = database.CreateContext();
        Assert.Empty(db.ContributionPaymentSubmissions);
        Assert.Equal(FineStatus.Unpaid, db.MemberFines.Single().Status);
    }
}
