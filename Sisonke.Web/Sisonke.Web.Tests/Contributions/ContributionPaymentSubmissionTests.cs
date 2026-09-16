using System.Text;
using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Sisonke.Web.Components.Pages.Contributions;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Services;
using Sisonke.Web.Services.Notifications;
using Sisonke.Web.Tests.TestSupport;

namespace Sisonke.Web.Tests.Contributions;

public sealed partial class ContributionPaymentSubmissionTests : IDisposable
{
    private readonly SharedCacheSqliteTestDatabase database = new();
    private readonly string storageRoot = Path.Combine(Path.GetTempPath(), "sisonke-proof-tests", Guid.NewGuid().ToString("N"));
    private readonly ContributionPaymentSubmissionService service;
    private readonly Guid stokvelId, contributionId, ownerId;
    private readonly string ownerUser = Guid.NewGuid().ToString(), treasurerUser = Guid.NewGuid().ToString();

    public ContributionPaymentSubmissionTests()
    {
        using var db = database.CreateContext();
        var stokvel = TestData.CreateStokvel(db);
        stokvelId = stokvel.Id;
        var member = AddMember(db, stokvel, ownerUser, SisonkeRole.Member);
        ownerId = member.Id;
        AddMember(db, stokvel, treasurerUser, SisonkeRole.Treasurer);
        var cycle = new ContributionCycle
        {
            Id = Guid.NewGuid(), TenantId = stokvel.TenantId, Name = "Monthly contribution",
            PeriodStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1),
            PeriodEnd = DateTime.Today, DueDate = DateTime.Today.AddDays(7)
        };
        db.ContributionCycles.Add(cycle);
        var contribution = new MemberContribution
        {
            Id = Guid.NewGuid(), TenantId = stokvel.TenantId, MemberId = member.Id,
            ContributionCycleId = cycle.Id, ExpectedAmount = 100, OutstandingAmount = 100
        };
        contributionId = contribution.Id;
        db.MemberContributions.Add(contribution);
        db.SaveChanges();
        var factory = new TestDbContextFactory(database);
        service = new(factory, new PaymentProofStorage(new FakeWebHostEnvironment { ContentRootPath = storageRoot }),
            new AuditLogService(factory, NullLogger<AuditLogService>.Instance),
            new NotificationEnqueuer(new NotificationEmailTemplateRenderer(new AppSettings(), NullLogger<NotificationEmailTemplateRenderer>.Instance)));
    }

    [Fact]
    public async Task OwnSubmissionPersistsProofAuditAndTreasurerNotificationWithoutChangingLedger()
    {
        var submission = await Submit();
        using var db = database.CreateContext();
        var contribution = await db.MemberContributions.SingleAsync();
        Assert.Equal(0, contribution.PaidAmount);
        Assert.Equal(100, contribution.OutstandingAmount);
        Assert.Equal(PaymentStatus.Unpaid, contribution.Status);
        Assert.Null(contribution.FullyPaidDate);
        Assert.Empty(db.Payments);
        Assert.Empty(db.ContributionPaymentAudits);
        Assert.Equal(ContributionPaymentSubmissionStatus.Pending, submission.Status);
        Assert.Single(db.PaymentProofDocuments);
        Assert.Contains(db.AuditLogEntries, a => a.ActionType == "ContributionPaymentProofSubmitted" && a.EntityId == submission.Id);
        Assert.Contains(db.NotificationMessages, n => n.Type == NotificationType.ContributionPaymentProofSubmitted);
        var proof = await service.OpenProofAsync(ownerUser, stokvelId, submission.Documents.Single().Id);
        await using var stream = proof.Stream;
        Assert.Equal("application/pdf", proof.ContentType);
        Assert.Equal(PdfFile.Bytes.Length, stream.Length);
    }

    [Fact]
    public async Task AnotherMemberCannotSubmitForOwner()
    {
        var user = AddActor(SisonkeRole.Member);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Submit(user));
    }

    [Fact]
    public async Task CrossTenantSubmissionBlocked()
    {
        using var db = database.CreateContext();
        var other = TestData.CreateStokvel(db);
        var user = Guid.NewGuid().ToString();
        AddMember(db, other, user, SisonkeRole.Member);
        db.SaveChanges();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.SubmitAsync(user, other.Id, contributionId, 50, DateTime.Today, null, null, new PdfFile()));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Submit(user));
    }

    [Theory]
    [InlineData("proof.exe", "application/pdf", "%PDF-1.7")]
    [InlineData("proof.pdf", "image/png", "%PDF-1.7")]
    [InlineData("proof.pdf", "application/pdf", "<script>not a PDF</script>")]
    [InlineData("proof.png", "image/png", "%PDF-1.7")]
    public async Task InvalidFilesRejected(string name, string contentType, string contents)
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => Submit(file: new PdfFile(name, contentType, Encoding.UTF8.GetBytes(contents))));
        using var db = database.CreateContext();
        Assert.Empty(db.ContributionPaymentSubmissions);
    }

    [Fact]
    public async Task OversizedFileRejectedBeforeOpeningStream() =>
        await Assert.ThrowsAsync<InvalidOperationException>(() => Submit(file: new PdfFile(size: PaymentProofStorage.MaxFileSize + 1)));

    [Fact]
    public async Task UnderreportedFileSizeRejected() =>
        await Assert.ThrowsAsync<InvalidOperationException>(() => Submit(file: new PdfFile(size: 1)));

    [Theory]
    [InlineData(SisonkeRole.Member)]
    [InlineData(SisonkeRole.Secretary)]
    [InlineData(SisonkeRole.Chairperson)]
    public async Task NonTreasurersCannotApproveRejectOrReadQueue(SisonkeRole role)
    {
        var submission = await Submit();
        var actor = AddActor(role);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ApproveAsync(actor, stokvelId, submission.Id));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.RejectAsync(actor, stokvelId, submission.Id, "Reason"));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetPendingAsync(actor, stokvelId));
        using var db = database.CreateContext();
        Assert.Empty(db.Payments);
        Assert.Equal(ContributionPaymentSubmissionStatus.Pending, db.ContributionPaymentSubmissions.Single().Status);
    }

    [Theory]
    [InlineData(40, 60, PaymentStatus.PartiallyPaid)]
    [InlineData(100, 0, PaymentStatus.Paid)]
    public async Task TreasurerApprovalUsesPostingPathWithPaymentAndFinancialAudit(decimal amount, decimal balance, PaymentStatus status)
    {
        var submission = await Submit(amount: amount);
        await service.ApproveAsync(treasurerUser, stokvelId, submission.Id);
        using var db = database.CreateContext();
        var contribution = db.MemberContributions.Single();
        var payment = db.Payments.Single();
        Assert.Equal(amount, contribution.PaidAmount);
        Assert.Equal(balance, contribution.OutstandingAmount);
        Assert.Equal(status, contribution.Status);
        Assert.Equal(amount, payment.Amount);
        Assert.Equal(DateTime.Today.AddDays(-1), payment.PaymentDate);
        Assert.Equal(treasurerUser, payment.CapturedByUserId);
        Assert.Equal(amount, db.ContributionPaymentAudits.Single().NewAmountPaid);
        Assert.Contains(db.AuditLogEntries, a => a.ActionType == "ContributionPaymentCaptured");
        Assert.Contains(db.AuditLogEntries, a => a.ActionType == "ContributionPaymentProofApproved");
        var saved = db.ContributionPaymentSubmissions.Single();
        Assert.Equal(payment.Id, saved.PaymentId);
        Assert.Equal(ContributionPaymentSubmissionStatus.Approved, saved.Status);
        Assert.Equal(treasurerUser, saved.ReviewedByUserId);
        Assert.NotNull(saved.ReviewedAt);
        Assert.Equal(status == PaymentStatus.Paid ? DateTime.Today : (DateTime?)null, contribution.FullyPaidDate);
        Assert.Contains(db.NotificationMessages, n => n.Type == NotificationType.ContributionPaymentProofApproved && n.RecipientMemberId == ownerId);
    }

    [Fact]
    public async Task RejectionLeavesBalanceUnchangedAndAllowsCorrectedSubmission()
    {
        var submission = await Submit();
        await service.RejectAsync(treasurerUser, stokvelId, submission.Id, "Reference does not match");
        using var db = database.CreateContext();
        Assert.Equal(0, db.MemberContributions.Single().PaidAmount);
        Assert.Equal(100, db.MemberContributions.Single().OutstandingAmount);
        Assert.Equal(PaymentStatus.Unpaid, db.MemberContributions.Single().Status);
        Assert.Empty(db.Payments);
        Assert.Empty(db.ContributionPaymentAudits);
        Assert.Contains(db.AuditLogEntries, a => a.ActionType == "ContributionPaymentProofRejected");
        Assert.Equal("Reference does not match", db.ContributionPaymentSubmissions.Single().RejectionReason);
        Assert.Contains(db.NotificationMessages, n => n.Type == NotificationType.ContributionPaymentProofRejected && n.Body.Contains("Reference does not match"));
        Assert.NotEqual(submission.Id, (await Submit()).Id);
    }

    [Fact]
    public async Task DuplicateApprovalAndRejectionAfterApprovalBlocked()
    {
        var submission = await Submit();
        await service.ApproveAsync(treasurerUser, stokvelId, submission.Id);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ApproveAsync(treasurerUser, stokvelId, submission.Id));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RejectAsync(treasurerUser, stokvelId, submission.Id, "Too late"));
        using var db = database.CreateContext();
        Assert.Single(db.Payments);
    }

    [Fact]
    public async Task RejectedSubmissionCannotBeApproved()
    {
        var submission = await Submit();
        await service.RejectAsync(treasurerUser, stokvelId, submission.Id, "Incorrect proof");
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ApproveAsync(treasurerUser, stokvelId, submission.Id));
        using var db = database.CreateContext();
        Assert.Empty(db.Payments);
    }

    [Fact]
    public async Task RejectionRequiresReason()
    {
        var submission = await Submit();
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RejectAsync(treasurerUser, stokvelId, submission.Id, "  "));
    }

    [Fact]
    public async Task CrossTenantAndOtherMemberDocumentAccessBlocked()
    {
        var submission = await Submit();
        var documentId = submission.Documents.Single().Id;
        var otherMember = AddActor(SisonkeRole.Member);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.OpenProofAsync(otherMember, stokvelId, documentId));
        using var db = database.CreateContext();
        var other = TestData.CreateStokvel(db);
        var otherUser = Guid.NewGuid().ToString();
        AddMember(db, other, otherUser, SisonkeRole.Treasurer);
        db.SaveChanges();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.OpenProofAsync(otherUser, other.Id, documentId));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.OpenProofAsync(otherUser, stokvelId, documentId));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ApproveAsync(otherUser, other.Id, submission.Id));
    }

    [Fact]
    public async Task DuplicatePendingSubmissionBlocked()
    {
        await Submit();
        await Assert.ThrowsAsync<InvalidOperationException>(() => Submit());
        using var db = database.CreateContext();
        Assert.Single(db.ContributionPaymentSubmissions);
    }

    [Fact]
    public async Task DownloadEndpointRequiresIdentityAndReturnsPrivateAttachment()
    {
        var submission = await Submit();
        var documentId = submission.Documents.Single().Id;
        var http = new DefaultHttpContext();
        var denied = await PaymentProofDownloadEndpoint.HandleAsync(stokvelId, documentId, http, service);
        Assert.Equal(401, ((IStatusCodeHttpResult)denied).StatusCode);
        http.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, ownerUser)], "Test"));
        var result = Assert.IsType<FileStreamHttpResult>(await PaymentProofDownloadEndpoint.HandleAsync(stokvelId, documentId, http, service));
        await using var stream = result.FileStream;
        Assert.Equal("proof.pdf", result.FileDownloadName);
        Assert.Equal("no-store, private", http.Response.Headers.CacheControl);
        Assert.Equal("nosniff", http.Response.Headers["X-Content-Type-Options"]);
        http.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, AddActor(SisonkeRole.Member))], "Test"));
        Assert.Equal(404, ((IStatusCodeHttpResult)await PaymentProofDownloadEndpoint.HandleAsync(stokvelId, documentId, http, service)).StatusCode);
    }

    [Fact]
    public async Task ConcurrentApprovalPostsExactlyOnce()
    {
        var submission = await Submit();
        using var start = new ManualResetEventSlim(false);
        Task<Exception?> Review() => Task.Run<Exception?>(async () =>
        {
            start.Wait();
            return await Record.ExceptionAsync(() => service.ApproveAsync(treasurerUser, stokvelId, submission.Id));
        });
        var first = Review();
        var second = Review();
        start.Set();
        var results = await Task.WhenAll(first, second);
        Assert.Single(results, r => r is null);
        Assert.IsType<InvalidOperationException>(results.Single(r => r is not null));
        using var db = database.CreateContext();
        Assert.Single(db.Payments);
        Assert.Equal(50, db.MemberContributions.Single().PaidAmount);
    }

    [Fact]
    public async Task ConcurrentSubmissionsHaveOnlyOnePendingRecord()
    {
        using var start = new ManualResetEventSlim(false);
        Task<Exception?> Upload() => Task.Run<Exception?>(async () =>
        {
            start.Wait();
            return await Record.ExceptionAsync(() => Submit());
        });
        var first = Upload();
        var second = Upload();
        start.Set();
        var results = await Task.WhenAll(first, second);
        Assert.Single(results, r => r is null);
        Assert.IsType<InvalidOperationException>(results.Single(r => r is not null));
        using var db = database.CreateContext();
        Assert.Single(db.ContributionPaymentSubmissions);
        Assert.Single(db.PaymentProofDocuments);
    }

    [Fact]
    public async Task TraversalFilenameIsReducedToDisplayNameAndStorageNameIsGenerated()
    {
        var submission = await Submit(file: new PdfFile("../../private/proof.pdf"));
        var document = submission.Documents.Single();
        Assert.Equal("proof.pdf", document.OriginalFileName);
        Assert.True(Guid.TryParseExact(Path.GetFileNameWithoutExtension(document.StoredFileName), "N", out _));
        using var db = database.CreateContext();
        db.PaymentProofDocuments.Single().StoredFileName = "../../private/proof.pdf";
        db.SaveChanges();
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.OpenProofAsync(ownerUser, stokvelId, document.Id));
    }

    [Theory]
    [InlineData(PaymentStatus.Exempted)]
    [InlineData(PaymentStatus.WrittenOff)]
    [InlineData(PaymentStatus.Paid)]
    public async Task IneligibleContributionCannotBeSubmitted(PaymentStatus status)
    {
        using var db = database.CreateContext();
        db.MemberContributions.Single().Status = status;
        db.SaveChanges();
        await Assert.ThrowsAsync<InvalidOperationException>(() => Submit());
    }

    [Fact]
    public async Task FailedApprovalRollsBackReviewAndAllPostingSideEffects()
    {
        var submission = await Submit();
        using (var db = database.CreateContext())
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER fail_proof_audit BEFORE INSERT ON AuditLogEntries WHEN NEW.ActionType = 'ContributionPaymentProofApproved' BEGIN SELECT RAISE(ABORT, 'Simulated audit failure'); END;");
        await Assert.ThrowsAsync<DbUpdateException>(() => service.ApproveAsync(treasurerUser, stokvelId, submission.Id));
        using var check = database.CreateContext();
        Assert.Empty(check.Payments);
        Assert.Empty(check.ContributionPaymentAudits);
        Assert.Equal(0, check.MemberContributions.Single().PaidAmount);
        Assert.Equal(ContributionPaymentSubmissionStatus.Pending, check.ContributionPaymentSubmissions.Single().Status);
        Assert.DoesNotContain(check.NotificationMessages, n => n.Type == NotificationType.ContributionPaymentProofApproved);
        Assert.DoesNotContain(check.AuditLogEntries, n => n.ActionType == "ContributionPaymentCaptured");
    }

    [Fact]
    public async Task ApprovalRechecksContributionEligibility()
    {
        var submission = await Submit();
        using (var db = database.CreateContext())
        {
            db.MemberContributions.Single().Status = PaymentStatus.WrittenOff;
            db.SaveChanges();
        }
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ApproveAsync(treasurerUser, stokvelId, submission.Id));
        using var check = database.CreateContext();
        Assert.Equal(ContributionPaymentSubmissionStatus.Pending, check.ContributionPaymentSubmissions.Single().Status);
        Assert.Empty(check.Payments);
    }

    [Fact]
    public async Task UnknownAccountAndInactiveStokvelBlocked()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Submit("missing"));
        using var db = database.CreateContext();
        db.Stokvels.Single().IsActive = false;
        db.SaveChanges();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Submit());
    }

    [Fact]
    public async Task AmbiguousStokvelOwnershipFailsClosed()
    {
        using var db = database.CreateContext();
        db.Stokvels.Add(new Stokvel { Id = Guid.NewGuid(), TenantId = db.Stokvels.Single().TenantId, Name = "Other stokvel" });
        db.SaveChanges();
        await Assert.ThrowsAsync<InvalidOperationException>(() => Submit());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1.001)]
    public async Task InvalidAmountsRejected(decimal amount) => await Assert.ThrowsAsync<InvalidOperationException>(() => Submit(amount: amount));

    [Fact]
    public async Task FuturePaymentDateRejected() => await Assert.ThrowsAsync<InvalidOperationException>(() =>
        service.SubmitAsync(ownerUser, stokvelId, contributionId, 10, DateTime.Today.AddDays(1), null, null, new PdfFile()));

    [Fact]
    public async Task ManualCaptureStillUsesOriginalDefaultsAndAudit()
    {
        using var db = database.CreateContext();
        var posting = new ContributionPaymentService(db, new MemberAccessService(db),
            new AuditLogService(new TestDbContextFactory(database), NullLogger<AuditLogService>.Instance));
        await posting.CaptureContributionPaymentAsync(contributionId, 25, "Manual", "Existing path", treasurerUser);
        Assert.Equal(25, db.MemberContributions.Single().PaidAmount);
        Assert.Equal(DateTime.Today, db.Payments.Single().PaymentDate);
        Assert.Single(db.ContributionPaymentAudits);
        Assert.Empty(db.ContributionPaymentSubmissions);
    }

    private Task<ContributionPaymentSubmission> Submit(string? user = null, decimal amount = 50, IBrowserFile? file = null) =>
        service.SubmitAsync(user ?? ownerUser, stokvelId, contributionId, amount, DateTime.Today.AddDays(-1), "REF", "Notes", file ?? new PdfFile());

    [Theory]
    [InlineData(0, 0, PaymentStatus.Unpaid, "No payment due")]
    [InlineData(0, 0, PaymentStatus.Late, "No payment due")]
    [InlineData(0, 0, PaymentStatus.PartiallyPaid, "No payment due")]
    [InlineData(100, 0, PaymentStatus.Late, "Paid")]
    [InlineData(100, 0, PaymentStatus.Unpaid, "Paid")]
    [InlineData(100, 100, PaymentStatus.Unpaid, "Overdue")]
    public async Task StatusQueriesAndStatementRespectActualOutstanding(decimal expected, decimal outstanding, PaymentStatus status, string label)
    {
        using var db = database.CreateContext();
        var contribution = db.MemberContributions.Include(c => c.ContributionCycle).Single();
        contribution.ExpectedAmount = expected;
        contribution.PaidAmount = expected - outstanding;
        contribution.OutstandingAmount = outstanding;
        contribution.Status = status;
        contribution.ContributionCycle.DueDate = DateTime.Today.AddDays(-1);
        await db.SaveChangesAsync();
        var posting = Posting(db);
        var treasurer = db.Members.Single(m => m.ApplicationUserId == treasurerUser);
        Assert.Equal(outstanding > 0 ? 1 : 0, await posting.MarkOverdueContributionsAsync(stokvelId, DateTime.Today.Year, DateTime.Today.Month, treasurer.Id));
        Assert.Equal(outstanding > 0 ? 1 : 0, (await posting.GetOverdueContributionsByMemberIdAsync(ownerId)).Count);
        Assert.Equal(outstanding > 0 ? 1 : 0, (await posting.GetOverdueContributionsByStokvelIdAsync(stokvelId)).Count);
        Assert.Equal(label, (await posting.GetContributionsByMemberIdAsync(ownerId)).Single().StatusLabel);
        var statement = await new FinanceReportService(db, posting, null!, null!).GetMemberFinancialStatementAsync(ownerId, stokvelId);
        Assert.Equal(label, statement!.Contributions.Single().Status);
        Assert.Equal(outstanding > 0 ? 1 : 0, statement.OverdueContributionCount);
        Assert.Equal(label, (await service.GetMemberContributionsAsync(ownerUser, stokvelId)).Single().StatusLabel);
    }

    [Fact]
    public void DashboardMessageDoesNotClaimUpToDateWhileOlderContributionsAreOverdue()
    {
        Assert.DoesNotContain("up to date", ContributionStatus.MemberMessage(100, 0, true));
        Assert.DoesNotContain("up to date", ContributionStatus.MemberMessage(0, 0, true));
        Assert.Equal("No payment due", ContributionStatus.MemberMessage(0, 0, false));
        Assert.Contains("up to date", ContributionStatus.MemberMessage(100, 0, false));
    }

    [Fact]
    public async Task GenerationRequiresRuleAndDoesNotCreateZeroObligations()
    {
        using var db = database.CreateContext();
        var period = DateTime.Today.AddMonths(1);
        Assert.Empty(await Posting(db).EnsureMonthlyContributionRecordsAsync(stokvelId, period.Year, period.Month));
        Assert.Single(db.ContributionCycles);
        Assert.Single(db.MemberContributions);
    }

    [Fact]
    public async Task GenerationUsesApplicableRuleAndExplicitlyRepairsOnlyZeroPlaceholders()
    {
        using var db = database.CreateContext();
        var contribution = db.MemberContributions.Single();
        contribution.ExpectedAmount = 0; contribution.OutstandingAmount = 0; contribution.Status = PaymentStatus.Late;
        var valid = new ContributionRule { Id = Guid.NewGuid(), TenantId = contribution.TenantId, Amount = 500, EffectiveFrom = DateTime.Today.AddMonths(-1) };
        db.ContributionRules.Add(valid);
        db.ContributionRules.Add(new ContributionRule { Id = Guid.NewGuid(), TenantId = contribution.TenantId, Amount = 999, EffectiveFrom = DateTime.Today.AddMonths(2) });
        await db.SaveChangesAsync();
        Assert.Equal(500, (await new ContributionService(db).GetActiveContributionRuleByStokvelIdAsync(stokvelId))!.Amount);
        var generated = await Posting(db).EnsureMonthlyContributionRecordsAsync(stokvelId, DateTime.Today.Year, DateTime.Today.Month);
        Assert.Equal(500, generated.Single(c => c.Id == contributionId).ExpectedAmount);
        Assert.Equal(500, generated.Single(c => c.Id == contributionId).OutstandingAmount);
        Assert.Empty(db.Payments);
        valid.Amount = 700;
        await db.SaveChangesAsync();
        generated = await Posting(db).EnsureMonthlyContributionRecordsAsync(stokvelId, DateTime.Today.Year, DateTime.Today.Month);
        Assert.Equal(500, generated.Single(c => c.Id == contributionId).ExpectedAmount);
    }

    [Fact]
    public async Task WholeRand270Then230PostsExactAmountsAndNotifiesBothParties()
    {
        using (var db = database.CreateContext())
        {
            var contribution = db.MemberContributions.Single();
            contribution.ExpectedAmount = 500; contribution.OutstandingAmount = 500;
            await db.SaveChangesAsync();
        }
        Assert.True(WholeRandAmount.TryParse("270", out var amount));
        Assert.Equal(270.00m, amount);
        var first = await Submit(amount: amount);
        using (var db = database.CreateContext())
        {
            Assert.Empty(db.Payments);
            Assert.Equal(0, db.MemberContributions.Single().PaidAmount);
            Assert.Equal(500, db.MemberContributions.Single().OutstandingAmount);
            var message = db.NotificationMessages.First(n => n.Type == NotificationType.ContributionPaymentProofSubmitted && n.Channel == NotificationChannel.Email);
            Assert.Contains("R270.00", message.Body);
            Assert.Contains("Verification is required", message.Body);
            Assert.Contains($"/treasurer-tasks/{stokvelId}#payment-proofs", message.Body);
        }
        await service.ApproveAsync(treasurerUser, stokvelId, first.Id);
        var memberView = (await service.GetMemberContributionsAsync(ownerUser, stokvelId)).Single();
        Assert.Equal(270, memberView.PaidAmount); Assert.Equal(230, memberView.OutstandingAmount);
        Assert.Equal("Partial", memberView.StatusLabel);
        var second = await Submit(amount: 230);
        await service.ApproveAsync(treasurerUser, stokvelId, second.Id);
        memberView = (await service.GetMemberContributionsAsync(ownerUser, stokvelId)).Single();
        Assert.Equal(500, memberView.PaidAmount); Assert.Equal(0, memberView.OutstandingAmount);
        Assert.Equal(PaymentStatus.Paid, memberView.Status);
        using var check = database.CreateContext();
        Assert.Equal(new[] { 230m, 270m }, check.Payments.Select(p => p.Amount).ToList().Order().ToArray());
        Assert.Equal(2, check.ContributionPaymentAudits.Count());
        Assert.Contains(check.NotificationMessages, n => n.Type == NotificationType.ContributionPaymentProofApproved && n.RecipientMemberId == ownerId);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("1.50")]
    [InlineData("1e2")]
    [InlineData("abc")]
    [InlineData("0")]
    public void CurrencyParserRejectsNonPositiveOrNonWholeRandInput(string input) => Assert.False(WholeRandAmount.TryParse(input, out _));

    [Theory]
    [InlineData(101)]
    [InlineData(450)]
    public async Task AmountAboveOutstandingRejectedServerSide(decimal amount)
    {
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => Submit(amount: amount));
        Assert.Equal("Amount paid cannot exceed the outstanding contribution amount.", error.Message);
        using var db = database.CreateContext();
        Assert.Empty(db.ContributionPaymentSubmissions);
        Assert.Empty(db.Payments);
    }

    [Fact]
    public async Task FractionalRandRejectedByServer() => await Assert.ThrowsAsync<InvalidOperationException>(() => Submit(amount: 1.50m));

    [Fact]
    public async Task ApprovalRechecksOutstandingAfterManualPayment()
    {
        var submission = await Submit(amount: 80);
        using (var db = database.CreateContext()) await Posting(db).CaptureContributionPaymentAsync(contributionId, 40, null, null, treasurerUser);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ApproveAsync(treasurerUser, stokvelId, submission.Id));
        Assert.Contains("cannot exceed", error.Message);
        using var check = database.CreateContext();
        Assert.Single(check.Payments);
        Assert.Equal(40, check.MemberContributions.Single().PaidAmount);
        Assert.Equal(ContributionPaymentSubmissionStatus.Pending, check.ContributionPaymentSubmissions.Single().Status);
    }

    [Fact]
    public async Task ManualCaptureDoesNotOverwriteApprovedProofFromStaleTrackedContext()
    {
        using var manual = database.CreateContext();
        _ = await manual.MemberContributions.SingleAsync();
        var submission = await Submit(amount: 40);
        await service.ApproveAsync(treasurerUser, stokvelId, submission.Id);
        await Posting(manual).CaptureContributionPaymentAsync(contributionId, 20, null, null, treasurerUser);
        using var check = database.CreateContext();
        Assert.Equal(60, check.MemberContributions.Single().PaidAmount);
        Assert.Equal(40, check.MemberContributions.Single().OutstandingAmount);
        Assert.Equal(2, check.Payments.Count());
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(100, true)]
    public void MemberPanelShowsActionOnlyWhenMoneyIsOutstanding(decimal outstanding, bool expectedAction)
    {
        using (var db = database.CreateContext())
        {
            var c = db.MemberContributions.Single(); c.ExpectedAmount = outstanding; c.OutstandingAmount = outstanding;
            db.SaveChanges();
        }
        using var ui = CreateUi();
        var component = ui.Render<MemberPaymentProofs>(p => p.Add(c => c.StokvelId, stokvelId));
        component.WaitForAssertion(() => Assert.Equal(expectedAction, component.FindAll("button").Any(b => b.TextContent.Contains("I've Paid"))));
        if (!expectedAction) Assert.Contains("No payment due", component.Markup);
    }

    [Fact]
    public async Task MemberPanelRefreshShowsApprovedBalanceAndCurrencyHasFixedAffixes()
    {
        using var ui = CreateUi();
        var component = ui.Render<MemberPaymentProofs>(p => p.Add(c => c.StokvelId, stokvelId));
        component.FindAll("button").Single(b => b.TextContent.Contains("I've Paid")).Click();
        Assert.Equal(new[] { "R", ".00" }, component.FindAll(".sisonke-currency-input .input-group-text").Select(e => e.TextContent).ToArray());
        component.Find("#proof-amount").Input("270");
        Assert.Equal("270", component.Find("#proof-amount").GetAttribute("value"));
        component.Find("#proof-amount").Input("-2");
        Assert.Equal("270", component.Find("#proof-amount").GetAttribute("value"));
        component.Find("form").Submit();
        Assert.Contains("Amount paid cannot exceed", component.Markup);
        component.FindAll("button").Single(b => b.TextContent == "Cancel").Click();
        var submission = await Submit(amount: 50);
        await service.ApproveAsync(treasurerUser, stokvelId, submission.Id);
        component.FindAll("button").Single(b => b.TextContent == "Refresh payment status").Click();
        component.WaitForAssertion(() => { Assert.Contains("Approved", component.Markup); Assert.Contains("Partial", component.Markup); Assert.Contains("50.00", component.Markup); });
    }

    private ContributionPaymentService Posting(ApplicationDbContext db) => new(db, new MemberAccessService(db),
        new AuditLogService(new TestDbContextFactory(database), NullLogger<AuditLogService>.Instance));

    private BunitContext CreateUi()
    {
        var ui = new BunitContext();
        ui.Services.AddSingleton(service);
        ui.Services.AddSingleton<MemberPaymentSubmissionService>(service);
        ui.Services.AddSingleton<AuthenticationStateProvider>(new ProofAuthentication(ownerUser));
        return ui;
    }

    private sealed class ProofAuthentication(string user) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(
            new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user)], "Test"))));
    }

    private string AddActor(SisonkeRole role)
    {
        using var db = database.CreateContext();
        var user = Guid.NewGuid().ToString();
        AddMember(db, db.Stokvels.Single(s => s.Id == stokvelId), user, role);
        db.SaveChanges();
        return user;
    }

    private static Member AddMember(ApplicationDbContext db, Stokvel stokvel, string userId, SisonkeRole role)
    {
        db.Users.Add(new ApplicationUser { Id = userId, UserName = userId });
        var member = TestData.CreateStokvelMember(db, stokvel, role: role);
        member.ApplicationUserId = userId;
        return member;
    }

    public void Dispose()
    {
        database.Dispose();
        // Each fixture owns only its randomly named temporary directory.
        if (Directory.Exists(storageRoot)) Directory.Delete(storageRoot, recursive: true);
    }

    private sealed class PdfFile(string name = "proof.pdf", string type = "application/pdf", byte[]? bytes = null, long? size = null) : IBrowserFile
    {
        public static readonly byte[] Bytes = Encoding.ASCII.GetBytes("%PDF-1.4\n1 0 obj<</Type/Catalog>>endobj\n%%EOF");
        public string Name => name;
        public DateTimeOffset LastModified => DateTimeOffset.UtcNow;
        public long Size => size ?? (bytes ?? Bytes).Length;
        public string ContentType => type;
        public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default) => new MemoryStream(bytes ?? Bytes);
    }
}

